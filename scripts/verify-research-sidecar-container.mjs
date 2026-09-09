import { execFile } from "node:child_process";
import { readFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { promisify } from "node:util";
import { fileURLToPath, pathToFileURL } from "node:url";
import { randomBytes } from "node:crypto";

const execFileAsync = promisify(execFile);
const SCRIPT_DIRECTORY = dirname(fileURLToPath(import.meta.url));
const REPOSITORY_ROOT = resolve(SCRIPT_DIRECTORY, "..");
const DOCKERFILE_PATH = resolve(REPOSITORY_ROOT, "backend/research-sidecar/Dockerfile");
const DOCKERIGNORE_PATH = resolve(REPOSITORY_ROOT, "backend/research-sidecar/.dockerignore");

export const FORBIDDEN_DISTRIBUTIONS = Object.freeze([
  "google-genai",
  "huggingface-hub",
  "openai",
  "pip",
  "setuptools",
  "tooluniverse",
]);

export const REQUIRED_DARK_ENVIRONMENT = Object.freeze({
  host: "0.0.0.0",
  service_token_configured: true,
  allow_insecure_dev_auth: false,
  global_kill_switch: true,
  tooluniverse_enabled: false,
  hosted_fallback_enabled: false,
  local_inference_enabled: false,
  gpu_enabled: false,
  max_concurrent_research_jobs: 1,
});

const ALLOWED_OPTIONS = new Set([
  "image",
  "container-name",
  "host-port",
  "health-timeout-seconds",
]);
const SAFE_IMAGE = /^[A-Za-z0-9][A-Za-z0-9._/@:-]{0,254}$/;
const SAFE_CONTAINER_NAME = /^[A-Za-z0-9][A-Za-z0-9_.-]{0,62}$/;
const SHA256_REFERENCE = /@sha256:[a-f0-9]{64}$/;
const TERMINAL_JOB_STATES = new Set([
  "completed",
  "failed",
  "cancelled",
  "partial",
  "rejected_by_policy",
]);

export class ContainerContractError extends Error {
  constructor(message) {
    super(message);
    this.name = "ContainerContractError";
  }
}

function outcome(errors) {
  return { ok: errors.length === 0, errors };
}

function hasControlCharacters(value) {
  return /[\u0000-\u001f\u007f]/u.test(value);
}

function parseBoundedInteger(raw, option, minimum, maximum) {
  if (!/^[0-9]+$/.test(raw)) {
    throw new ContainerContractError(`--${option} must be an integer.`);
  }
  const value = Number(raw);
  if (!Number.isSafeInteger(value) || value < minimum || value > maximum) {
    throw new ContainerContractError(
      `--${option} must be between ${minimum} and ${maximum}.`,
    );
  }
  return value;
}

export function parseCliArgs(argv) {
  if (!Array.isArray(argv)) {
    throw new ContainerContractError("Arguments must be an array.");
  }

  const values = new Map();
  for (let index = 0; index < argv.length; index += 2) {
    const option = argv[index];
    const value = argv[index + 1];
    if (typeof option !== "string" || !option.startsWith("--") || option === "--") {
      throw new ContainerContractError("Every argument must be a named option.");
    }
    const name = option.slice(2);
    if (!ALLOWED_OPTIONS.has(name)) {
      throw new ContainerContractError(`Unknown option --${sanitizeDiagnostic(name)}.`);
    }
    if (values.has(name)) {
      throw new ContainerContractError(`Duplicate option --${name}.`);
    }
    if (
      typeof value !== "string" ||
      value.length === 0 ||
      value.trim() !== value ||
      value.startsWith("--") ||
      hasControlCharacters(value)
    ) {
      throw new ContainerContractError(`Malformed value for --${name}.`);
    }
    values.set(name, value);
  }

  if (!values.has("image")) {
    throw new ContainerContractError("Missing required --image.");
  }
  const image = values.get("image");
  if (!SAFE_IMAGE.test(image)) {
    throw new ContainerContractError("--image is not a safe local image reference.");
  }

  const containerName = values.get("container-name");
  if (containerName !== undefined && !SAFE_CONTAINER_NAME.test(containerName)) {
    throw new ContainerContractError("--container-name has an unsafe shape.");
  }

  return {
    image,
    containerName,
    hostPort:
      values.get("host-port") === undefined
        ? undefined
        : parseBoundedInteger(values.get("host-port"), "host-port", 1024, 65535),
    healthTimeoutSeconds:
      values.get("health-timeout-seconds") === undefined
        ? 45
        : parseBoundedInteger(
            values.get("health-timeout-seconds"),
            "health-timeout-seconds",
            1,
            180,
          ),
  };
}

export function sanitizeDiagnostic(value, sensitiveValues = []) {
  let text = String(value ?? "unknown error")
    .replace(/[\r\n]+/gu, " ")
    .replace(/\x1b\[[0-9;]*m/gu, "")
    .replace(/::/gu, "--")
    .slice(0, 600);
  for (const sensitive of sensitiveValues) {
    if (typeof sensitive === "string" && sensitive.length > 0) {
      text = text.split(sensitive).join("[REDACTED]");
    }
  }
  return text;
}

export function evaluateDockerfileContract(contents) {
  if (typeof contents !== "string") return outcome(["Dockerfile is not text."]);
  const errors = [];
  const firstInstruction = contents.match(/^FROM\s+(\S+)/m)?.[1];
  if (
    firstInstruction !==
    "ghcr.io/astral-sh/uv:python3.12-bookworm-slim@sha256:e5b65587bce7de595f299855d7385fe7fca39b8a74baa261ba1b7147afa78e58"
  ) {
    errors.push("Base image is not pinned to the verified OCI digest.");
  }
  if (!SHA256_REFERENCE.test(firstInstruction ?? "")) {
    errors.push("Base image reference lacks a valid sha256 digest.");
  }
  if (!/^ARG INCLUDE_TOOLUNIVERSE=false$/m.test(contents)) {
    errors.push("Production build does not default to the no-extra path.");
  }
  if (!/false\) uv sync --locked --no-dev --no-install-project ;;/.test(contents)) {
    errors.push("Dependency-only no-extra sync is missing.");
  }
  if (!/false\) uv sync --locked --no-dev ;;/.test(contents)) {
    errors.push("Project no-extra sync is missing.");
  }
  if (
    !/true\) uv sync --locked --no-dev --extra tooluniverse --no-install-project ;;/.test(
      contents,
    )
  ) {
    errors.push("Dependency-only ToolUniverse source contract is missing.");
  }
  if (!/true\) uv sync --locked --no-dev --extra tooluniverse ;;/.test(contents)) {
    errors.push("Project ToolUniverse source contract is missing.");
  }
  if (/--extra\s+["']?\$[A-Za-z_{]/.test(contents) || /^ARG TOOLUNIVERSE_EXTRA=/m.test(contents)) {
    errors.push("Unsafe variable-valued --extra construction is present.");
  }
  if (!/^USER biostack$/m.test(contents)) errors.push("Image user is not biostack.");
  if (!/^EXPOSE 8080$/m.test(contents)) errors.push("Image does not expose port 8080.");
  if (!/^CMD \["python", "-m", "biostack_research_sidecar"\]$/m.test(contents)) {
    errors.push("Image command differs from the production sidecar command.");
  }
  return outcome(errors);
}

export function evaluateDockerignoreContract(contents) {
  if (typeof contents !== "string") return outcome([".dockerignore is not text."]);
  const lines = new Set(
    contents
      .split(/\r?\n/u)
      .map((line) => line.trim())
      .filter((line) => line.length > 0 && !line.startsWith("#")),
  );
  const errors = [];
  for (const pattern of [
    ".env",
    ".env.*",
    ".git",
    ".venv",
    "__pycache__",
    ".pytest_cache",
    ".ruff_cache",
    ".mypy_cache",
    "tests",
    "artifacts",
    "docs",
  ]) {
    if (!lines.has(pattern)) errors.push(`Missing build-context exclusion: ${pattern}.`);
  }
  return outcome(errors);
}

export function parseImageInspect(output) {
  let parsed;
  try {
    parsed = JSON.parse(output);
  } catch {
    throw new ContainerContractError("Docker image inspection was not valid JSON.");
  }
  if (!Array.isArray(parsed) || parsed.length !== 1 || typeof parsed[0] !== "object") {
    throw new ContainerContractError("Docker image inspection had an unexpected shape.");
  }
  const config = parsed[0]?.Config;
  if (!config || typeof config !== "object") {
    throw new ContainerContractError("Docker image inspection omitted Config.");
  }
  return {
    user: config.User,
    command: config.Cmd,
    exposedPorts: config.ExposedPorts,
    workingDirectory: config.WorkingDir,
  };
}

export function evaluateImageConfiguration(observation) {
  const errors = [];
  const user = observation?.user;
  if (
    typeof user !== "string" ||
    user.length === 0 ||
    ["0", "0:0", "root", "root:root"].includes(user.toLowerCase())
  ) {
    errors.push("Image runtime user is not explicitly non-root.");
  }
  if (
    !Array.isArray(observation?.command) ||
    JSON.stringify(observation.command) !==
      JSON.stringify(["python", "-m", "biostack_research_sidecar"])
  ) {
    errors.push("Image command is not the exact production command.");
  }
  const ports = observation?.exposedPorts;
  if (
    !ports ||
    typeof ports !== "object" ||
    Array.isArray(ports) ||
    Object.keys(ports).length !== 1 ||
    !Object.hasOwn(ports, "8080/tcp")
  ) {
    errors.push("Image does not expose exactly 8080/tcp.");
  }
  if (observation?.workingDirectory !== "/app") {
    errors.push("Image working directory is not /app.");
  }
  if (!Number.isInteger(observation?.runtimeUid) || observation.runtimeUid <= 0) {
    errors.push("Observed runtime UID is not non-root.");
  }
  return outcome(errors);
}

export function evaluatePackageCensus(distributions) {
  if (
    !Array.isArray(distributions) ||
    distributions.some((name) => typeof name !== "string" || name.length === 0)
  ) {
    return outcome(["Package census is malformed."]);
  }
  const normalized = new Set(distributions.map((name) => name.toLowerCase().replaceAll("_", "-")));
  return outcome(
    FORBIDDEN_DISTRIBUTIONS.filter((name) => normalized.has(name)).map(
      (name) => `Forbidden distribution is installed: ${name}.`,
    ),
  );
}

export function evaluateFilesystemCensus(observation) {
  if (!observation || typeof observation !== "object" || Array.isArray(observation)) {
    return outcome(["Filesystem census is malformed."]);
  }
  const errors = [];
  for (const key of ["env_paths", "tests_paths", "git_paths"]) {
    if (!Array.isArray(observation[key])) {
      errors.push(`Filesystem census omitted ${key}.`);
    } else if (observation[key].length > 0) {
      errors.push(`Image contains forbidden ${key.replace("_paths", "")} content.`);
    }
  }
  return outcome(errors);
}

export function evaluateDarkEnvironment(observation) {
  if (!observation || typeof observation !== "object" || Array.isArray(observation)) {
    return outcome(["Dark environment observation is malformed."]);
  }
  const errors = [];
  for (const [key, expected] of Object.entries(REQUIRED_DARK_ENVIRONMENT)) {
    if (!Object.hasOwn(observation, key)) {
      errors.push(`Dark environment omitted ${key}.`);
    } else if (observation[key] !== expected) {
      errors.push(`Dark environment has an unsafe ${key} value.`);
    }
  }
  return outcome(errors);
}

export function evaluateHealthObservation(status, body) {
  const errors = [];
  if (status !== 200) errors.push("Health endpoint did not return HTTP 200.");
  if (!body || typeof body !== "object" || Array.isArray(body)) {
    errors.push("Health response is malformed.");
    return outcome(errors);
  }
  for (const [key, expected] of Object.entries({
    status: "disabled",
    service: "biostack-research-sidecar",
    global_kill_switch: true,
    tooluniverse_enabled: false,
    max_concurrent_research_jobs: 1,
  })) {
    if (body[key] !== expected) errors.push(`Health response has an unsafe ${key} value.`);
  }
  return outcome(errors);
}

export function evaluateAuthObservations(observation) {
  const errors = [];
  if (observation?.missingStatus !== 401) errors.push("Missing token was not denied.");
  if (observation?.wrongStatus !== 401) errors.push("Wrong token was not denied.");
  if (observation?.correctStatus !== 200) errors.push("Correct token was not accepted.");
  const body = observation?.correctBody;
  if (!body || typeof body !== "object" || Array.isArray(body)) {
    errors.push("Authenticated workflow response is malformed.");
  } else {
    if (body.tooluniverse_enabled !== false) {
      errors.push("Authenticated response does not keep ToolUniverse disabled.");
    }
    const workflows = body.allowed_workflows;
    if (!Array.isArray(workflows) || !workflows.includes("resolve_compound_identity")) {
      errors.push("Authenticated response omits the allowlisted workflow.");
    }
    if (Array.isArray(workflows) && workflows.includes("execute_any_tool")) {
      errors.push("Authenticated response exposes arbitrary tool execution.");
    }
  }
  return outcome(errors);
}

export function evaluateRejectionObservations(observation) {
  const errors = [];
  if (observation?.privacyStatus !== 422) {
    errors.push("Private-shaped payload was not rejected with HTTP 422.");
  }
  if (
    !["unknown_request_fields", "privacy_boundary_violation"].includes(
      observation?.privacyCode,
    )
  ) {
    errors.push("Private-shaped payload did not fail at the privacy boundary.");
  }
  if (![400, 422].includes(observation?.workflowStatus)) {
    errors.push("Arbitrary workflow was not rejected before admission.");
  }
  if (
    !["workflow_not_allowlisted", "schema_validation_failed"].includes(
      observation?.workflowCode,
    )
  ) {
    errors.push("Arbitrary workflow rejection did not have a fail-closed code.");
  }
  return outcome(errors);
}

export function evaluateKillSwitchObservations(observation) {
  const errors = [];
  if (observation?.submissionStatus !== 202) {
    errors.push("Kill-switch request was not honestly admitted with HTTP 202.");
  }
  if (observation?.submittedState !== "queued") {
    errors.push("Kill-switch request did not begin in queued state.");
  }
  if (observation?.terminalState !== "rejected_by_policy") {
    errors.push("Kill-switch request did not terminate as rejected_by_policy.");
  }
  if (observation?.errorCode !== "global_kill_switch") {
    errors.push("Kill-switch terminal state omitted the global-kill reason.");
  }
  return outcome(errors);
}

export function evaluateDocsRoutes(observation) {
  if (!observation || typeof observation !== "object" || Array.isArray(observation)) {
    return outcome(["Documentation-route observation is malformed."]);
  }
  const errors = [];
  for (const route of ["/docs", "/redoc", "/openapi.json"]) {
    if (observation[route] !== 404) errors.push(`${route} did not return HTTP 404.`);
  }
  return outcome(errors);
}

export function evaluateLeakage(text, sensitiveValues) {
  if (typeof text !== "string" || !Array.isArray(sensitiveValues)) {
    return outcome(["Leakage observation is malformed."]);
  }
  const errors = [];
  for (const value of sensitiveValues) {
    if (typeof value !== "string" || value.length < 8) {
      errors.push("Sensitive marker is malformed.");
    } else if (text.includes(value)) {
      errors.push("Captured logs or verifier output contain a sensitive marker.");
    }
  }
  return outcome(errors);
}

export function buildContainerCreateArguments({ image, name, hostPort, token }) {
  const binding = `127.0.0.1:${hostPort ?? ""}:8080`;
  return [
    "create",
    "--name",
    name,
    "--publish",
    binding,
    "--env",
    "BIOSTACK_RESEARCH_HOST=0.0.0.0",
    "--env",
    `BIOSTACK_RESEARCH_SERVICE_TOKEN=${token}`,
    "--env",
    "BIOSTACK_RESEARCH_ALLOW_INSECURE_DEV_AUTH=false",
    "--env",
    "BIOSTACK_RESEARCH_GLOBAL_KILL_SWITCH=true",
    "--env",
    "BIOSTACK_RESEARCH_TOOLUNIVERSE_ENABLED=false",
    "--env",
    "BIOSTACK_RESEARCH_HOSTED_FALLBACK_ENABLED=false",
    "--env",
    "BIOSTACK_RESEARCH_LOCAL_INFERENCE_ENABLED=false",
    "--env",
    "BIOSTACK_RESEARCH_GPU_ENABLED=false",
    "--env",
    "BIOSTACK_RESEARCH_MAX_CONCURRENT_RESEARCH_JOBS=1",
    image,
  ];
}

export function buildContainerCleanupArguments(name) {
  return ["rm", "--force", name];
}

export function parseOwnedContainerId(output) {
  const value = typeof output === "string" ? output.trim() : "";
  if (!/^[a-f0-9]{64}$/u.test(value)) {
    throw new ContainerContractError("Docker create did not return one full container ID.");
  }
  return value;
}

function assertOutcome(label, result) {
  if (!result.ok) {
    throw new ContainerContractError(`${label}: ${result.errors.join(" ")}`);
  }
}

async function runProcess(command, args, { timeoutMs = 60_000, sensitive = [] } = {}) {
  try {
    const result = await execFileAsync(command, args, {
      encoding: "utf8",
      maxBuffer: 4 * 1024 * 1024,
      timeout: timeoutMs,
      windowsHide: true,
    });
    return { stdout: result.stdout ?? "", stderr: result.stderr ?? "" };
  } catch (error) {
    const detail = sanitizeDiagnostic(error?.stderr || error?.message, sensitive);
    throw new ContainerContractError(`${command} failed: ${detail}`);
  }
}

async function readContractFile(path, label) {
  try {
    return await readFile(path, "utf8");
  } catch (error) {
    throw new ContainerContractError(
      `Unable to read ${label}: ${sanitizeDiagnostic(error?.message)}.`,
    );
  }
}

async function dockerJson(args, label, sensitive = []) {
  const { stdout } = await runProcess("docker", args, { sensitive });
  try {
    return JSON.parse(stdout);
  } catch {
    throw new ContainerContractError(`${label} was not valid JSON.`);
  }
}

async function inspectStaticImage(image) {
  const dockerfile = await readContractFile(DOCKERFILE_PATH, "Dockerfile");
  const dockerignore = await readContractFile(DOCKERIGNORE_PATH, ".dockerignore");
  assertOutcome("Dockerfile contract failed", evaluateDockerfileContract(dockerfile));
  assertOutcome(
    "Build-context contract failed",
    evaluateDockerignoreContract(dockerignore),
  );

  const { stdout: inspectOutput } = await runProcess("docker", ["image", "inspect", image]);
  const observation = parseImageInspect(inspectOutput);

  const runtimeScript = [
    "import json, os",
    "print(json.dumps({'runtimeUid': os.geteuid()}))",
  ].join("; ");
  const runtime = await dockerJson(
    ["run", "--rm", "--entrypoint", "/app/.venv/bin/python", image, "-c", runtimeScript],
    "Runtime identity census",
  );
  observation.runtimeUid = runtime.runtimeUid;
  assertOutcome("Image configuration failed", evaluateImageConfiguration(observation));

  const packageScript = [
    "import importlib.metadata as m, json",
    "print(json.dumps(sorted({d.metadata['Name'].lower().replace('_','-') for d in m.distributions() if d.metadata.get('Name')})))",
  ].join("; ");
  const packages = await dockerJson(
    ["run", "--rm", "--entrypoint", "/app/.venv/bin/python", image, "-c", packageScript],
    "Package census",
  );
  assertOutcome("Package census failed", evaluatePackageCensus(packages));

  const filesystemScript = [
    "from pathlib import Path",
    "import json",
    "root=Path('/app')",
    "result={'env_paths':[str(p) for p in root.glob('.env*')], 'tests_paths':[str(root/'tests')] if (root/'tests').exists() else [], 'git_paths':[str(p) for p in root.rglob('.git')]} ",
    "print(json.dumps(result, sort_keys=True))",
  ].join("; ");
  const filesystem = await dockerJson(
    ["run", "--rm", "--entrypoint", "/app/.venv/bin/python", image, "-c", filesystemScript],
    "Filesystem census",
  );
  assertOutcome("Filesystem census failed", evaluateFilesystemCensus(filesystem));
}

async function requestJson(url, { method = "GET", token, body } = {}) {
  const headers = { Accept: "application/json" };
  if (token !== undefined) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined) headers["Content-Type"] = "application/json";
  let response;
  try {
    response = await fetch(url, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      redirect: "manual",
      signal: AbortSignal.timeout(5_000),
    });
  } catch {
    throw new ContainerContractError("Local HTTP request failed.");
  }
  const text = await readBoundedResponseText(response);
  let parsed = null;
  if (text.length > 0) {
    try {
      parsed = JSON.parse(text);
    } catch {
      throw new ContainerContractError("Local HTTP response was not valid JSON.");
    }
  }
  return { status: response.status, body: parsed };
}

export async function readBoundedResponseText(response, maximumBytes = 65_536) {
  if (
    !response ||
    typeof response !== "object" ||
    !Number.isSafeInteger(maximumBytes) ||
    maximumBytes < 1
  ) {
    throw new ContainerContractError("HTTP response reader arguments are malformed.");
  }
  const declared = response.headers?.get?.("content-length");
  if (declared !== null && declared !== undefined) {
    if (!/^[0-9]+$/u.test(declared) || Number(declared) > maximumBytes) {
      throw new ContainerContractError("Local HTTP response exceeded the size limit.");
    }
  }
  if (response.body === null) return "";
  if (typeof response.body?.getReader !== "function") {
    throw new ContainerContractError("Local HTTP response body is not readable.");
  }

  const reader = response.body.getReader();
  const chunks = [];
  let total = 0;
  while (true) {
    let item;
    try {
      item = await reader.read();
    } catch {
      throw new ContainerContractError("Local HTTP response body read failed.");
    }
    const { done, value } = item;
    if (done) break;
    if (!(value instanceof Uint8Array)) {
      await reader.cancel();
      throw new ContainerContractError("Local HTTP response chunk was malformed.");
    }
    total += value.byteLength;
    if (total > maximumBytes) {
      await reader.cancel();
      throw new ContainerContractError("Local HTTP response exceeded the size limit.");
    }
    chunks.push(value);
  }
  const joined = new Uint8Array(total);
  let offset = 0;
  for (const chunk of chunks) {
    joined.set(chunk, offset);
    offset += chunk.byteLength;
  }
  try {
    return new TextDecoder("utf-8", { fatal: true }).decode(joined);
  } catch {
    throw new ContainerContractError("Local HTTP response was not valid UTF-8.");
  }
}

async function waitForHealth(baseUrl, timeoutSeconds) {
  const deadline = Date.now() + timeoutSeconds * 1_000;
  while (Date.now() < deadline) {
    try {
      const response = await requestJson(`${baseUrl}/health`);
      if (response.status === 200) return response;
    } catch (error) {
      if (!(error instanceof ContainerContractError)) throw error;
    }
    await new Promise((resolveDelay) => setTimeout(resolveDelay, 250));
  }
  throw new ContainerContractError("Timed out waiting for local dark health.");
}

async function waitForTerminal(baseUrl, jobId, token) {
  const deadline = Date.now() + 10_000;
  while (Date.now() < deadline) {
    const response = await requestJson(
      `${baseUrl}/internal/v1/research/jobs/${encodeURIComponent(jobId)}`,
      { token },
    );
    if (response.status !== 200 || !response.body || typeof response.body !== "object") {
      throw new ContainerContractError("Job-status response was malformed.");
    }
    if (TERMINAL_JOB_STATES.has(response.body.status)) return response.body;
    await new Promise((resolveDelay) => setTimeout(resolveDelay, 100));
  }
  throw new ContainerContractError("Timed out waiting for kill-switch terminal state.");
}

function createContainerName() {
  return `biostack-p01-${process.pid}-${randomBytes(8).toString("hex")}`;
}

function createSyntheticToken() {
  return `p01-local-only-${randomBytes(24).toString("hex")}`;
}

async function resolvePublishedPort(name) {
  const { stdout } = await runProcess("docker", ["port", name, "8080/tcp"]);
  const lines = stdout.trim().split(/\r?\n/u);
  if (lines.length !== 1) {
    throw new ContainerContractError("Docker returned an ambiguous host-port mapping.");
  }
  const match = /^127\.0\.0\.1:([0-9]+)$/u.exec(lines[0]);
  if (!match) {
    throw new ContainerContractError("Container port is not bound exclusively to 127.0.0.1.");
  }
  const port = Number(match[1]);
  if (!Number.isInteger(port) || port < 1 || port > 65535) {
    throw new ContainerContractError("Docker returned an invalid host port.");
  }
  return port;
}

async function inspectDarkEnvironment(name) {
  const script = [
    "import json",
    "from biostack_research_sidecar.config import Settings",
    "s=Settings()",
    "print(json.dumps({'host':s.host,'service_token_configured':bool(s.service_token.strip()),'allow_insecure_dev_auth':s.allow_insecure_dev_auth,'global_kill_switch':s.global_kill_switch,'tooluniverse_enabled':s.tooluniverse_enabled,'hosted_fallback_enabled':s.hosted_fallback_enabled,'local_inference_enabled':s.local_inference_enabled,'gpu_enabled':s.gpu_enabled,'max_concurrent_research_jobs':s.max_concurrent_research_jobs},sort_keys=True))",
  ].join("; ");
  return dockerJson(
    ["exec", name, "/app/.venv/bin/python", "-c", script],
    "Dark environment census",
  );
}

async function verifyRuntime(options) {
  const name = options.containerName ?? createContainerName();
  const token = createSyntheticToken();
  const wrongToken = createSyntheticToken();
  const privateMarker = `p01-private-${randomBytes(8).toString("hex")}`;
  const publicMarker = `P01Compound-${randomBytes(6).toString("hex")}`;
  const arbitraryWorkflow = "execute_any_tool";
  const sensitive = [token, wrongToken, privateMarker, publicMarker, arbitraryWorkflow];
  let ownedContainerId;
  let publicOutput = "P01 local container contract passed.";

  try {
    const created = await runProcess(
      "docker",
      buildContainerCreateArguments({
        image: options.image,
        name,
        hostPort: options.hostPort,
        token,
      }),
      { timeoutMs: 30_000, sensitive },
    );
    ownedContainerId = parseOwnedContainerId(created.stdout);
    await runProcess("docker", ["start", ownedContainerId], {
      timeoutMs: 30_000,
      sensitive,
    });

    const port = await resolvePublishedPort(ownedContainerId);
    const baseUrl = `http://127.0.0.1:${port}`;
    const health = await waitForHealth(baseUrl, options.healthTimeoutSeconds);
    assertOutcome(
      "Health contract failed",
      evaluateHealthObservation(health.status, health.body),
    );

    const darkEnvironment = await inspectDarkEnvironment(ownedContainerId);
    assertOutcome(
      "Dark environment contract failed",
      evaluateDarkEnvironment(darkEnvironment),
    );

    const missing = await requestJson(`${baseUrl}/internal/v1/workflows`);
    const wrong = await requestJson(`${baseUrl}/internal/v1/workflows`, {
      token: wrongToken,
    });
    const correct = await requestJson(`${baseUrl}/internal/v1/workflows`, { token });
    assertOutcome(
      "Authentication contract failed",
      evaluateAuthObservations({
        missingStatus: missing.status,
        wrongStatus: wrong.status,
        correctStatus: correct.status,
        correctBody: correct.body,
      }),
    );

    const privacy = await requestJson(`${baseUrl}/internal/v1/research/jobs`, {
      method: "POST",
      token,
      body: {
        subject_name: "P01Compound",
        workflow: "resolve_compound_identity",
        patient_id: privateMarker,
        data_classification: "public_scientific",
      },
    });
    const arbitrary = await requestJson(`${baseUrl}/internal/v1/research/jobs`, {
      method: "POST",
      token,
      body: {
        subject_name: "P01Compound",
        workflow: arbitraryWorkflow,
        data_classification: "public_scientific",
      },
    });
    assertOutcome(
      "Admission-boundary contract failed",
      evaluateRejectionObservations({
        privacyStatus: privacy.status,
        privacyCode: privacy.body?.detail?.code,
        workflowStatus: arbitrary.status,
        workflowCode: arbitrary.body?.detail?.code,
      }),
    );

    const submitted = await requestJson(`${baseUrl}/internal/v1/research/jobs`, {
      method: "POST",
      token,
      body: {
        subject_name: publicMarker,
        workflow: "resolve_compound_identity",
        data_classification: "public_scientific",
        local_inference_permitted: false,
        hosted_inference_permitted: false,
        execution: {
          mode: "cpu_only",
          allow_gpu: false,
          allow_cpu_fallback: true,
          allow_hosted_fallback: false,
          maximum_execution_duration_seconds: 10,
        },
      },
    });
    const jobId = submitted.body?.job_id;
    if (typeof jobId !== "string" || jobId.length === 0) {
      throw new ContainerContractError("Kill-switch submission omitted job_id.");
    }
    const terminal = await waitForTerminal(baseUrl, jobId, token);
    assertOutcome(
      "Kill-switch lifecycle contract failed",
      evaluateKillSwitchObservations({
        submissionStatus: submitted.status,
        submittedState: submitted.body?.status,
        terminalState: terminal.status,
        errorCode: terminal.error_code,
      }),
    );

    const docs = {};
    for (const route of ["/docs", "/redoc", "/openapi.json"]) {
      const response = await requestJson(`${baseUrl}${route}`);
      docs[route] = response.status;
    }
    assertOutcome("Documentation-route contract failed", evaluateDocsRoutes(docs));

    const { stdout: logs, stderr: logErrors } = await runProcess(
      "docker",
      ["logs", ownedContainerId],
      { sensitive },
    );
    assertOutcome(
      "Log/output hygiene failed",
      evaluateLeakage(`${logs}\n${logErrors}\n${publicOutput}`, sensitive),
    );

    return { status: "passed", checks: 9 };
  } finally {
    if (ownedContainerId !== undefined) {
      try {
        await runProcess("docker", buildContainerCleanupArguments(ownedContainerId), {
          timeoutMs: 30_000,
          sensitive,
        });
      } catch (error) {
        if (error instanceof ContainerContractError) {
          throw new ContainerContractError("Owned test-container cleanup failed.");
        }
        throw error;
      }
    }
  }
}

export async function main(argv = process.argv.slice(2)) {
  const options = parseCliArgs(argv);
  await inspectStaticImage(options.image);
  const result = await verifyRuntime(options);
  console.log(JSON.stringify(result));
}

if (import.meta.url === pathToFileURL(process.argv[1]).href) {
  main().catch((error) => {
    console.error(`research-sidecar container contract failed: ${sanitizeDiagnostic(error?.message)}`);
    process.exitCode = 1;
  });
}
