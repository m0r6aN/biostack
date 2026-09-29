import { execFile } from "node:child_process";
import { mkdtemp, readFile, rm } from "node:fs/promises";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { promisify } from "node:util";
import { fileURLToPath, pathToFileURL } from "node:url";
import { randomBytes } from "node:crypto";

const execFileAsync = promisify(execFile);
const SCRIPT_DIRECTORY = dirname(fileURLToPath(import.meta.url));
const REPOSITORY_ROOT = resolve(SCRIPT_DIRECTORY, "..");
const DOCKERFILE_PATH = resolve(REPOSITORY_ROOT, "backend/research-sidecar/Dockerfile");
const DOCKERIGNORE_PATH = resolve(REPOSITORY_ROOT, "backend/research-sidecar/.dockerignore");
export const OWNERSHIP_LABEL_KEY = "io.biostack.p01.owner";
const OWNERSHIP_RECONCILIATION_ATTEMPTS = 60;
const OWNERSHIP_RECONCILIATION_DELAY_MS = 100;
export const DIRECT_CLI_TIMEOUT_MS = 150_000;
export const CLEANUP_RESERVE_MS = 15_000;
export const SIGNAL_SHUTDOWN_TIMEOUT_MS = 15_000;
export const OUTER_RECOVERY_RESERVE_MS = 5_000;
const DEADLINE_PHASES = new Set([
  "work",
  "reconcile",
  "cleanup",
  "outerReconcile",
  "outerCleanup",
]);

export const FORBIDDEN_DISTRIBUTIONS = Object.freeze([
  "google-genai",
  "hatchling",
  "huggingface-hub",
  "openai",
  "packaging",
  "pathspec",
  "pip",
  "pluggy",
  "setuptools",
  "tomlkit",
  "tooluniverse",
  "trove-classifiers",
  "wheel",
]);

export const REQUIRED_DISTRIBUTIONS = Object.freeze([
  "annotated-doc",
  "annotated-types",
  "anyio",
  "biostack-research-sidecar",
  "certifi",
  "click",
  "fastapi",
  "h11",
  "httpcore",
  "httptools",
  "httpx",
  "idna",
  "pydantic",
  "pydantic-core",
  "pydantic-settings",
  "pyyaml",
  "python-dotenv",
  "starlette",
  "typing-extensions",
  "typing-inspection",
  "uvicorn",
  "uvloop",
  "watchfiles",
  "websockets",
]);

export const REQUIRED_IMAGE_ENVIRONMENT = Object.freeze([
  "PATH=/app/.venv/bin:/usr/local/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin",
  "LANG=C.UTF-8",
  "GPG_KEY=7169605F62C751356D054A26A821E680E5FA6305",
  "PYTHON_VERSION=3.12.12",
  "PYTHON_SHA256=fb85a13414b028c49ba18bbd523c2d055a30b56b18b92ce454ea2c51edc656c4",
  "PYTHONDONTWRITEBYTECODE=1",
  "PYTHONUNBUFFERED=1",
]);

export const ALLOWED_WORKFLOWS = Object.freeze([
  "refresh_evidence_packet",
  "research_adverse_events",
  "research_compound_evidence",
  "research_mechanisms_and_targets",
  "research_pathways",
  "research_published_regimens",
  "resolve_compound_identity",
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
  log_level: "warning",
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

export function createExecutionControl({
  timeoutMs = DIRECT_CLI_TIMEOUT_MS,
  cleanupReserveMs = CLEANUP_RESERVE_MS,
  now = () => performance.now(),
} = {}) {
  if (
    !Number.isSafeInteger(timeoutMs) ||
    !Number.isSafeInteger(cleanupReserveMs) ||
    timeoutMs < 1 ||
    cleanupReserveMs < 1 ||
    cleanupReserveMs >= timeoutMs ||
    typeof now !== "function"
  ) {
    throw new ContainerContractError("Execution deadline arguments are malformed.");
  }
  const outerRecoveryReserveMs = Math.max(
    1,
    Math.min(OUTER_RECOVERY_RESERVE_MS, Math.floor(cleanupReserveMs / 3)),
  );
  let deadline = now() + timeoutMs;
  const abortController = new AbortController();
  let terminationSignal;
  let pendingClaim;

  const control = {
    abortSignal: abortController.signal,
    get deadline() {
      return deadline;
    },
    get cleanupReserveMs() {
      return cleanupReserveMs;
    },
    get terminationSignal() {
      return terminationSignal;
    },
    get pendingClaim() {
      return pendingClaim;
    },
    requestTermination(signal) {
      if (!terminationSignal) {
        terminationSignal = signal === "SIGINT" ? "SIGINT" : "SIGTERM";
        deadline = Math.min(deadline, now() + SIGNAL_SHUTDOWN_TIMEOUT_MS);
        abortController.abort();
      }
    },
    throwIfTerminated() {
      if (terminationSignal) {
        throw new ContainerContractError(`Verifier received ${terminationSignal}.`);
      }
    },
    registerPendingClaim(claim) {
      control.throwIfTerminated();
      if (
        pendingClaim ||
        !claim ||
        typeof claim !== "object" ||
        !SAFE_CONTAINER_NAME.test(claim.name ?? "") ||
        !/^[a-f0-9]{48}$/u.test(claim.owner ?? "") ||
        typeof claim.cidFile !== "string" ||
        claim.cidFile.length === 0
      ) {
        throw new ContainerContractError("Pending ownership claim is malformed or overlapping.");
      }
      pendingClaim = claim;
    },
    releasePendingClaim(claim) {
      if (pendingClaim !== claim) {
        throw new ContainerContractError("Pending ownership claim release disagreed.");
      }
      pendingClaim = undefined;
    },
    capTimeout(requestedMs, phase = "work") {
      if (!Number.isSafeInteger(requestedMs) || requestedMs < 1) {
        throw new ContainerContractError("Subprocess timeout is malformed.");
      }
      if (phase === "work") control.throwIfTerminated();
      if (!DEADLINE_PHASES.has(phase)) {
        throw new ContainerContractError("Subprocess deadline phase is malformed.");
      }
      const reserve =
        phase === "work"
          ? cleanupReserveMs
          : phase === "reconcile"
            ? Math.ceil((cleanupReserveMs * 2) / 3)
            : phase === "cleanup"
              ? outerRecoveryReserveMs
              : phase === "outerReconcile"
                ? Math.ceil(outerRecoveryReserveMs / 2)
                : 0;
      const remaining = Math.floor(deadline - now() - reserve);
      if (remaining < 1) {
        throw new ContainerContractError(`Global verifier deadline exhausted during ${phase}.`);
      }
      return Math.min(requestedMs, remaining);
    },
  };
  return control;
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
  const input = String(value ?? "unknown error");
  let text = "";
  for (let index = 0; index < input.length; index += 1) {
    const code = input.charCodeAt(index);
    if (code === 0x1b && input[index + 1] === "[") {
      index += 2;
      while (index < input.length && input.charCodeAt(index) >= 0x20 && input.charCodeAt(index) <= 0x3f) index += 1;
      continue;
    }
    if (code === 0x1b && ["]", "P", "X", "^", "_"].includes(input[index + 1])) {
      const introducer = input[index + 1];
      index += 2;
      while (index < input.length) {
        if (introducer === "]" && input.charCodeAt(index) === 0x07) break;
        if (input.charCodeAt(index) === 0x1b && input[index + 1] === "\\") {
          index += 1;
          break;
        }
        index += 1;
      }
      continue;
    }
    if (code === 0x9b) {
      index += 1;
      while (index < input.length && input.charCodeAt(index) >= 0x20 && input.charCodeAt(index) <= 0x3f) index += 1;
      continue;
    }
    if ([0x90, 0x98, 0x9d, 0x9e, 0x9f].includes(code)) {
      index += 1;
      while (index < input.length) {
        if (code === 0x9d && input.charCodeAt(index) === 0x07) break;
        if (input.charCodeAt(index) === 0x9c) break;
        if (input.charCodeAt(index) === 0x1b && input[index + 1] === "\\") {
          index += 1;
          break;
        }
        index += 1;
      }
      continue;
    }
    if (code === 0x1b) {
      if (index + 1 < input.length) index += 1;
      continue;
    }
    if (code === 0x0a || code === 0x0d || code === 0x09) {
      text += " ";
    } else if ((code >= 0x20 && code <= 0x7e) || code >= 0xa0) {
      text += input[index];
    }
  }
  text = text
    .replace(/[\p{Cc}\p{Cf}\p{Zl}\p{Zp}]/gu, "")
    .replace(/::/gu, "--");
  for (const sensitive of sensitiveValues) {
    if (typeof sensitive === "string" && sensitive.length > 0) {
      text = text.split(sensitive).join("[REDACTED]");
    }
  }
  return text.slice(0, 600);
}

export function isDirectExecution(moduleUrl, argvEntry) {
  return (
    typeof moduleUrl === "string" &&
    typeof argvEntry === "string" &&
    moduleUrl === pathToFileURL(argvEntry).href
  );
}

export function evaluateDockerfileContract(contents) {
  if (typeof contents !== "string") return outcome(["Dockerfile is not text."]);
  const errors = [];
  const fromInstructions = [...contents.matchAll(/^FROM\s+(\S+)(?:\s+AS\s+(\S+))?$/gim)];
  const firstInstruction = fromInstructions[0]?.[1];
  if (
    firstInstruction !==
    "ghcr.io/astral-sh/uv:python3.12-bookworm-slim@sha256:e5b65587bce7de595f299855d7385fe7fca39b8a74baa261ba1b7147afa78e58"
  ) {
    errors.push("Base image is not pinned to the verified OCI digest.");
  }
  if (!SHA256_REFERENCE.test(firstInstruction ?? "")) {
    errors.push("Base image reference lacks a valid sha256 digest.");
  }
  if (fromInstructions.length !== 2 || fromInstructions[0]?.[2]?.toLowerCase() !== "builder") {
    errors.push("Dockerfile does not have the exact two-stage builder/runtime shape.");
  }
  if (
    fromInstructions[1]?.[1] !==
      "python:3.12.12-slim-bookworm@sha256:593bd06efe90efa80dc4eee3948be7c0fde4134606dd40d8dd8dbcade98e669c" ||
    fromInstructions[1]?.[2]?.toLowerCase() !== "runtime"
  ) {
    errors.push("Runtime image is not pinned to the verified Python OCI digest.");
  }
  if (!/^ARG INCLUDE_TOOLUNIVERSE=false$/m.test(contents)) {
    errors.push("Production build does not default to the no-extra path.");
  }
  if (!/false\) uv sync --locked --no-dev --no-install-project ;;/.test(contents)) {
    errors.push("Dependency-only no-extra sync is missing.");
  }
  if (
    !/true\) uv sync --locked --no-dev --extra tooluniverse --no-install-project ;;/.test(
      contents,
    )
  ) {
    errors.push("Dependency-only ToolUniverse source contract is missing.");
  }
  if (/--extra\s+["']?\$[A-Za-z_{]/.test(contents) || /^ARG TOOLUNIVERSE_EXTRA=/m.test(contents)) {
    errors.push("Unsafe variable-valued --extra construction is present.");
  }
  const copyInstructions = [...contents.matchAll(/^COPY\s+(.+)$/gimu)].map(
    (match) => match[1].trim(),
  );
  const requiredCopyInstructions = [
    "pyproject.toml uv.lock ./",
    "README.md ./",
    "src ./src",
    "--from=builder --chown=0:0 /app /app",
  ];
  if (
    copyInstructions.length !== requiredCopyInstructions.length ||
    requiredCopyInstructions.some(
      (instruction) => copyInstructions.filter((candidate) => candidate === instruction).length !== 1,
    )
  ) {
    errors.push("Docker build copy graph is not the exact allowlisted metadata and source set.");
  }
  for (const pattern of [
    /hatchling==1\.32\.0[\s\S]+P01_BUILD_BACKEND_REQUIREMENTS_END/u,
    /uv pip install[\s\S]+--no-deps[\s\S]+--only-binary=:all:[\s\S]+--require-hashes/u,
    /UV_NO_INDEX=1 \/tmp\/p01-build-backend-venv\/bin\/python -m hatchling build/u,
    /uv pip install[\s\S]+--python \/app\/\.venv\/bin\/python[\s\S]+--no-deps[\s\S]+--no-index[\s\S]+biostack_research_sidecar-0\.1\.0-py3-none-any\.whl/u,
    /rm -rf \/tmp\/p01-build-backend-venv \/tmp\/p01-build-backend-requirements\.txt \/tmp\/p01-dist/u,
  ]) {
    if (!pattern.test(contents)) {
      errors.push("Hash-locked isolated build-backend closure is incomplete.");
      break;
    }
  }
  if (!/^COPY --from=builder --chown=0:0 \/app \/app$/m.test(contents)) {
    errors.push("Runtime application tree is not copied with root ownership.");
  }
  if (!/chmod -R go-w \/app/.test(contents)) {
    errors.push("Runtime application tree is not made non-writable to the service user.");
  }
  const dockerfileTokens = new Set(contents.split(/\s+/u).map((token) => token.replace(/\\$/u, "")));
  for (const installerPath of [
    "/usr/local/lib/python3.12/ensurepip",
    "/usr/local/lib/python3.12/site-packages/pip",
    "/usr/local/bin/pip",
    "/usr/local/bin/pip3",
    "/app/.venv/lib/python3.12/site-packages/pip",
    "/app/.venv/bin/pip",
    "/app/.venv/bin/pip3",
  ]) {
    if (!dockerfileTokens.has(installerPath)) {
      errors.push(`Runtime installer removal omits ${installerPath}.`);
    }
  }
  if (!/^USER biostack$/m.test(contents)) errors.push("Image user is not biostack.");
  if (!/^EXPOSE 8080$/m.test(contents)) errors.push("Image does not expose port 8080.");
  if (!/^CMD \["python", "-m", "biostack_research_sidecar"\]$/m.test(contents)) {
    errors.push("Image command differs from the production sidecar command.");
  }
  if (/^\s*ENTRYPOINT(?:\s|\[)/imu.test(contents)) {
    errors.push("Dockerfile must not define an ENTRYPOINT.");
  }
  return outcome(errors);
}

export function evaluateDockerignoreContract(contents) {
  if (typeof contents !== "string") return outcome([".dockerignore is not text."]);
  const lines = contents
    .split(/\r?\n/u)
    .map((line) => line.trim())
    .filter((line) => line.length > 0 && !line.startsWith("#"));
  const required = ["*", "!pyproject.toml", "!uv.lock", "!README.md", "!src/", "!src/**"];
  const errors = [];
  for (const pattern of required) {
    if (lines.filter((line) => line === pattern).length !== 1) {
      errors.push(`Build-context allowlist must contain exactly one ${pattern}.`);
    }
  }
  const unexpected = lines.filter((line) => !required.includes(line));
  if (unexpected.length > 0) {
    errors.push(`Unsafe build-context pattern or re-inclusion: ${unexpected.sort().join(", ")}.`);
  }
  if (JSON.stringify(lines) !== JSON.stringify(required)) {
    errors.push("Build-context rules are not the exact ordered default-deny allowlist.");
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
    imageId: parsed[0]?.Id,
    user: config.User,
    entrypoint: config.Entrypoint ?? null,
    command: config.Cmd,
    environment: config.Env,
    exposedPorts: config.ExposedPorts,
    workingDirectory: config.WorkingDir,
  };
}

export function requireImmutableImageId(value) {
  if (typeof value !== "string" || !/^sha256:[a-f0-9]{64}$/u.test(value)) {
    throw new ContainerContractError("Inspected image ID is not an immutable sha256 identity.");
  }
  return value;
}

export async function executeAfterImageValidation(imageId, execute) {
  const immutableImage = requireImmutableImageId(imageId);
  if (typeof execute !== "function") {
    throw new ContainerContractError("Image census executor is malformed.");
  }
  return execute(immutableImage);
}

export async function inspectLocalImageBeforeExecution({ image, inspect, execute }) {
  if (typeof inspect !== "function") {
    throw new ContainerContractError("Local image inspector is malformed.");
  }
  const inspected = await inspect(["image", "inspect", image]);
  const observation = parseImageInspect(inspected?.stdout);
  return executeAfterImageValidation(observation.imageId, (immutableImage) =>
    execute(immutableImage, observation),
  );
}

export function evaluateImageConfiguration(observation) {
  const errors = [];
  if (typeof observation?.imageId !== "string" || !/^sha256:[a-f0-9]{64}$/u.test(observation.imageId)) {
    errors.push("Inspected image ID is not an immutable sha256 identity.");
  }
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
  if (
    observation?.entrypoint !== null &&
    observation?.entrypoint !== undefined &&
    (!Array.isArray(observation.entrypoint) || observation.entrypoint.length !== 0)
  ) {
    errors.push("Image entrypoint must be null or empty.");
  }
  const environment = observation?.environment;
  if (!Array.isArray(environment)) {
    errors.push("Image baked environment is malformed.");
  } else {
    const expected = new Map(
      REQUIRED_IMAGE_ENVIRONMENT.map((entry) => {
        const separator = entry.indexOf("=");
        return [entry.slice(0, separator), entry.slice(separator + 1)];
      }),
    );
    const actual = new Map();
    for (const entry of environment) {
      if (typeof entry !== "string" || !/^[A-Za-z_][A-Za-z0-9_]*=.*$/u.test(entry)) {
        errors.push("Image baked environment contains a malformed entry.");
        continue;
      }
      const separator = entry.indexOf("=");
      const name = entry.slice(0, separator);
      const value = entry.slice(separator + 1);
      if (actual.has(name)) {
        errors.push(`Image baked environment contains duplicate variable: ${name}.`);
        continue;
      }
      actual.set(name, value);
      if (
        !expected.has(name) &&
        (/(?:^|_)(?:OPENAI|ANTHROPIC|CEREBRAS|AZURE|AWS|GOOGLE|GCP|HUGGINGFACE|HF)(?:_|$)/iu.test(
          name,
        ) ||
          /(?:^|_)(?:API_?KEY|TOKEN|SECRET|PASSWORD|CREDENTIALS?)(?:_|$)/iu.test(name))
      ) {
        errors.push(`Image baked environment contains provider or credential-shaped variable: ${name}.`);
      }
      if (!expected.has(name)) {
        errors.push(`Image baked environment contains unexpected variable: ${name}.`);
      } else if (expected.get(name) !== value) {
        errors.push(`Image baked environment value differs for ${name}.`);
      }
    }
    for (const name of expected.keys()) {
      if (!actual.has(name)) errors.push(`Image baked environment is missing variable: ${name}.`);
    }
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
    distributions.some(
      (name) =>
        typeof name !== "string" ||
        name.trim() !== name ||
        !/^[A-Za-z0-9](?:[A-Za-z0-9._-]*[A-Za-z0-9])?$/u.test(name),
    )
  ) {
    return outcome(["Package census contains nameless or invalid distribution metadata."]);
  }
  if (distributions.length === 0) return outcome(["Package census is empty."]);
  const canonical = distributions.map((name) => name.toLowerCase().replace(/[-_.]+/gu, "-"));
  const normalized = new Set(canonical);
  const duplicateNames = [...new Set(canonical.filter((name, index) => canonical.indexOf(name) !== index))];
  const errors = duplicateNames.map(
    (name) => `Duplicate canonical distribution identity is installed: ${name}.`,
  );
  errors.push(...FORBIDDEN_DISTRIBUTIONS.filter((name) => normalized.has(name)).map(
    (name) => `Forbidden distribution is installed: ${name}.`,
  ));
  for (const name of REQUIRED_DISTRIBUTIONS) {
    if (!normalized.has(name)) errors.push(`Required distribution is missing: ${name}.`);
  }
  for (const name of normalized) {
    if (!REQUIRED_DISTRIBUTIONS.includes(name)) {
      errors.push(`Unexpected distribution is installed: ${name}.`);
    }
  }
  return outcome(errors);
}

export function evaluateFilesystemCensus(observation) {
  if (!observation || typeof observation !== "object" || Array.isArray(observation)) {
    return outcome(["Filesystem census is malformed."]);
  }
  const errors = [];
  for (const key of [
    "env_paths",
    "tests_paths",
    "git_paths",
    "credential_paths",
    "installer_paths",
  ]) {
    if (!Array.isArray(observation[key])) {
      errors.push(`Filesystem census omitted ${key}.`);
    } else if (observation[key].length > 0) {
      errors.push(`Image contains forbidden ${key.replace("_paths", "")} content.`);
    }
  }
  if (!Array.isArray(observation.python_probes) || observation.python_probes.length !== 2) {
    errors.push("Filesystem census omitted the two Python installer probes.");
  } else {
    const expected = new Set(["system", "venv"]);
    for (const probe of observation.python_probes) {
      if (!probe || typeof probe !== "object" || !expected.delete(probe.name)) {
        errors.push("Filesystem census has malformed Python probe identity.");
        continue;
      }
      for (const moduleName of ["pip_module", "ensurepip_module"]) {
        if (probe[moduleName] !== false) {
          errors.push(`Python ${probe.name} probe exposes ${moduleName}.`);
        }
      }
      for (const executableName of ["pip_path", "pip3_path", "uv_path", "uvx_path"]) {
        if (probe[executableName] !== null) {
          errors.push(`Python ${probe.name} PATH exposes ${executableName}.`);
        }
      }
    }
    if (expected.size > 0) errors.push("Filesystem census omitted a required Python probe identity.");
  }
  for (const [key, expected] of Object.entries({
    app_owner_uid: 0,
    venv_owner_uid: 0,
    app_writable: false,
    venv_writable: false,
  })) {
    if (observation[key] !== expected) errors.push(`Filesystem census has unsafe ${key}.`);
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
    if (
      !Array.isArray(workflows) ||
      workflows.some((name) => typeof name !== "string") ||
      new Set(workflows).size !== workflows.length ||
      JSON.stringify([...workflows].sort()) !== JSON.stringify([...ALLOWED_WORKFLOWS].sort())
    ) errors.push("Authenticated response does not expose exactly the seven allowlisted workflows.");
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

function ownershipLabel(owner) {
  return `${OWNERSHIP_LABEL_KEY}=${owner}`;
}

export function buildContainerCreateArguments({
  image,
  name,
  hostPort,
  token,
  cidFile,
  owner,
}) {
  const binding = `127.0.0.1:${hostPort ?? ""}:8080`;
  return [
    "create",
    "--pull=never",
    "--name",
    name,
    "--cidfile",
    cidFile,
    "--label",
    ownershipLabel(owner),
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
    "--env",
    "BIOSTACK_RESEARCH_LOG_LEVEL=warning",
    image,
  ];
}

export function buildCensusCreateArguments({
  image,
  name,
  cidFile,
  owner,
  entrypoint,
  command,
}) {
  return [
    "create",
    "--pull=never",
    "--name",
    name,
    "--cidfile",
    cidFile,
    "--label",
    ownershipLabel(owner),
    "--entrypoint",
    entrypoint,
    image,
    ...command,
  ];
}

export function buildContainerCleanupArguments(name) {
  return ["rm", "--force", name];
}

export function buildOwnershipLookupArguments(name, owner) {
  return [
    "container",
    "ls",
    "--all",
    "--no-trunc",
    "--filter",
    `name=^/${name}$`,
    "--filter",
    `label=${ownershipLabel(owner)}`,
    "--format",
    "{{.ID}}",
  ];
}

export function buildOwnershipInspectArguments(id) {
  return [
    "container",
    "inspect",
    "--format",
    `{{index .Config.Labels "${OWNERSHIP_LABEL_KEY}"}}`,
    id,
  ];
}

export function parseOwnedContainerId(output) {
  const value = typeof output === "string" ? output.trim() : "";
  if (!/^[a-f0-9]{64}$/u.test(value)) {
    throw new ContainerContractError("Docker create did not return one full container ID.");
  }
  return value;
}

function optionalOwnedContainerId(output) {
  const value = typeof output === "string" ? output.trim() : "";
  return value.length === 0 ? undefined : parseOwnedContainerId(value);
}

function ownedContainerIds(output) {
  if (typeof output !== "string") {
    throw new ContainerContractError("Docker ownership lookup was malformed.");
  }
  const rows = output
    .split(/\r?\n/u)
    .map((row) => row.trim())
    .filter(Boolean);
  return rows.map((row) => parseOwnedContainerId(row));
}

export async function reconcileOwnedContainer({
  createOutput,
  readCidFile,
  findExactNameAndLabel,
  hasExpectedLabel,
  delay = (milliseconds) => new Promise((resolveDelay) => setTimeout(resolveDelay, milliseconds)),
  capDelayMs = (milliseconds) => milliseconds,
  attempts = OWNERSHIP_RECONCILIATION_ATTEMPTS,
  delayMs = OWNERSHIP_RECONCILIATION_DELAY_MS,
}) {
  if (
    !Number.isInteger(attempts) ||
    attempts < 1 ||
    typeof delay !== "function" ||
    typeof capDelayMs !== "function"
  ) {
    throw new ContainerContractError("Ownership reconciliation arguments are malformed.");
  }
  const candidates = new Set();
  let stdoutId;
  try {
    stdoutId = optionalOwnedContainerId(createOutput);
    if (stdoutId) candidates.add(stdoutId);
  } catch {
    // Malformed create output is not ownership proof; cidfile/label evidence may arrive later.
  }

  for (let attempt = 0; attempt < attempts; attempt += 1) {
    let cidId;
    try {
      cidId = optionalOwnedContainerId(await readCidFile());
      if (cidId) candidates.add(cidId);
    } catch (error) {
      if (error?.code !== "ENOENT") throw error;
    }
    const nameIds = ownedContainerIds(await findExactNameAndLabel());
    for (const id of nameIds) candidates.add(id);

    const labeled = [];
    for (const id of candidates) {
      if (await hasExpectedLabel(id)) labeled.push(id);
    }
    if (candidates.size > 1 || labeled.length > 1) {
      const error = new ContainerContractError("Docker ownership proofs disagreed.");
      const uniqueLabeledId = labeled.length === 1 ? labeled[0] : undefined;
      if (uniqueLabeledId) error.cleanupContainerId = uniqueLabeledId;
      throw error;
    }
    if (labeled.length === 1) return labeled[0];
    if (attempt + 1 < attempts) await delay(capDelayMs(delayMs));
  }
  return undefined;
}

function asContractError(error, sensitive) {
  const result = new ContainerContractError(sanitizeDiagnostic(error?.message, sensitive));
  if (Array.isArray(error?.diagnostics)) {
    result.diagnostics = error.diagnostics.map((detail) =>
      sanitizeDiagnostic(detail, sensitive));
  }
  if (/^[a-f0-9]{64}$/u.test(error?.cleanupContainerId ?? "")) {
    result.cleanupContainerId = error.cleanupContainerId;
  }
  return result;
}

export async function executeOwnedContainerLifecycle({
  create,
  resolveOwnership,
  start,
  operate,
  cleanup,
  sensitive = [],
  control,
  claim,
}) {
  if (control) control.registerPendingClaim(claim);
  let releaseClaim = false;
  let createResult;
  let primaryError;
  try {
    try {
      createResult = await create();
    } catch (error) {
      primaryError = asContractError(error, sensitive);
    }
    if (typeof createResult?.stdout === "string" && createResult.stdout.trim().length > 0) {
      try {
        parseOwnedContainerId(createResult.stdout);
      } catch (error) {
        primaryError ??= asContractError(error, sensitive);
      }
    }
    if (control?.terminationSignal) {
      primaryError ??= new ContainerContractError(
        `Verifier received ${control.terminationSignal}.`,
      );
    }

    let ownedContainerId;
    try {
      ownedContainerId = await resolveOwnership(createResult?.stdout ?? "");
    } catch (error) {
      const reconciliationError = asContractError(error, sensitive);
      ownedContainerId = reconciliationError.cleanupContainerId;
      if (primaryError) {
        primaryError = new ContainerContractError(
          `${primaryError.message} Ownership reconciliation also failed: ${reconciliationError.message}`,
        );
      } else {
        primaryError = reconciliationError;
      }
    }
    if (ownedContainerId && claim) claim.ownedContainerId = ownedContainerId;
    if (!ownedContainerId && !primaryError) {
      primaryError = new ContainerContractError("Docker create returned no labeled ownership proof.");
    }
    if (control?.terminationSignal) {
      primaryError ??= new ContainerContractError(
        `Verifier received ${control.terminationSignal}.`,
      );
    }

    let result;
    if (!primaryError) {
      try {
        control?.throwIfTerminated();
        const startResult = await start(ownedContainerId);
        control?.throwIfTerminated();
        result = await operate(ownedContainerId, startResult);
      } catch (error) {
        primaryError = asContractError(error, sensitive);
      }
    }

    let cleanupError;
    if (ownedContainerId) {
      try {
        await cleanup(ownedContainerId);
        releaseClaim = true;
      } catch (error) {
        cleanupError = asContractError(error, sensitive);
      }
    }
    if (control?.terminationSignal) {
      primaryError ??= new ContainerContractError(
        `Verifier received ${control.terminationSignal}.`,
      );
    }
    if (primaryError && cleanupError) {
      const combinedError = new ContainerContractError(
        `${primaryError.message} Cleanup also failed: ${cleanupError.message}`,
      );
      combinedError.diagnostics = [
        `Primary verification failure: ${primaryError.message}`,
        `Lifecycle cleanup failure: ${cleanupError.message}`,
      ];
      throw combinedError;
    }
    if (primaryError) throw primaryError;
    if (cleanupError) {
      throw new ContainerContractError(
        `Owned test-container cleanup failed: ${cleanupError.message}`,
      );
    }
    return result;
  } finally {
    if (control && releaseClaim) control.releasePendingClaim(claim);
  }
}

function assertOutcome(label, result) {
  if (!result.ok) {
    throw new ContainerContractError(`${label}: ${result.errors.join(" ")}`);
  }
}

async function runProcess(
  command,
  args,
  { timeoutMs = 60_000, sensitive = [], control, phase = "work" } = {},
) {
  try {
    const boundedTimeoutMs = control ? control.capTimeout(timeoutMs, phase) : timeoutMs;
    const result = await execFileAsync(command, args, {
      encoding: "utf8",
      maxBuffer: 4 * 1024 * 1024,
      timeout: boundedTimeoutMs,
      signal: phase === "work" ? control?.abortSignal : undefined,
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

async function dockerJson(args, label, sensitive = [], control, phase = "work") {
  const { stdout } = await runProcess("docker", args, { sensitive, control, phase });
  try {
    return JSON.parse(stdout);
  } catch {
    throw new ContainerContractError(`${label} was not valid JSON.`);
  }
}

function createOwnershipMarker() {
  return randomBytes(24).toString("hex");
}

async function readCidFileValue(cidFile) {
  try {
    return await readFile(cidFile, "utf8");
  } catch (error) {
    if (error?.code === "ENOENT") return "";
    throw error;
  }
}

async function findExactNamedLabeledContainer(
  name,
  owner,
  sensitive,
  control,
  phase = "reconcile",
) {
  const { stdout } = await runProcess(
    "docker",
    buildOwnershipLookupArguments(name, owner),
    { sensitive, control, phase, timeoutMs: 3_000 },
  );
  return stdout;
}

async function containerHasExpectedLabel(id, owner, sensitive, control, phase = "reconcile") {
  try {
    const { stdout } = await runProcess(
      "docker",
      buildOwnershipInspectArguments(id),
      { sensitive, control, phase, timeoutMs: 3_000 },
    );
    return stdout.trim() === owner;
  } catch (error) {
    if (phase === "reconcile" && /No such (?:object|container)/iu.test(error?.message ?? "")) {
      return false;
    }
    throw error;
  }
}

async function resolveDockerOwnership({
  createOutput,
  cidFile,
  name,
  owner,
  sensitive,
  control,
  phase = "reconcile",
}) {
  return reconcileOwnedContainer({
    createOutput,
    readCidFile: () => readCidFileValue(cidFile),
    findExactNameAndLabel: () =>
      findExactNamedLabeledContainer(name, owner, sensitive, control, phase),
    hasExpectedLabel: (id) =>
      containerHasExpectedLabel(id, owner, sensitive, control, phase),
    capDelayMs: (milliseconds) => control?.capTimeout(milliseconds, phase) ?? milliseconds,
  });
}

async function cleanupOwnedDockerContainer(
  id,
  owner,
  sensitive,
  control,
  phase = "cleanup",
) {
  try {
    if (!(await containerHasExpectedLabel(id, owner, sensitive, control, phase))) {
      throw new ContainerContractError(
        "Container ownership label changed; cleanup was withheld.",
      );
    }
  } catch (error) {
    if (/No such (?:object|container)/iu.test(error?.message ?? "")) return;
    throw error;
  }
  return runProcess("docker", buildContainerCleanupArguments(id), {
    timeoutMs: 10_000,
    sensitive,
    control,
    phase,
  });
}

export async function recoverPendingClaim(
  control,
  {
    resolveOwnership = ({ claim, sensitive }) =>
      resolveDockerOwnership({
        createOutput: "",
        cidFile: claim.cidFile,
        name: claim.name,
        owner: claim.owner,
        sensitive,
        control,
        phase: "outerReconcile",
      }),
    cleanup = ({ id, claim, sensitive }) =>
      cleanupOwnedDockerContainer(
        id,
        claim.owner,
        sensitive,
        control,
        "outerCleanup",
      ),
  } = {},
) {
  const claim = control?.pendingClaim;
  if (!claim) return { status: "no-pending-claim" };
  const sensitive = Array.isArray(claim.sensitive) ? claim.sensitive : [claim.owner];
  let ownedContainerId = claim.ownedContainerId;
  if (!ownedContainerId) {
    ownedContainerId = await resolveOwnership({ claim, sensitive });
    if (ownedContainerId) claim.ownedContainerId = ownedContainerId;
  }
  if (ownedContainerId) await cleanup({ id: ownedContainerId, claim, sensitive });
  control.releasePendingClaim(claim);
  return { status: ownedContainerId ? "cleaned" : "absent" };
}

async function runOwnedCensus({ image, label, entrypoint, command, control }) {
  const name = createContainerName(`census-${label}`);
  const owner = createOwnershipMarker();
  const sensitive = [owner];
  const ownershipDirectory = await mkdtemp(join(tmpdir(), "biostack-p01-census-"));
  const cidFile = join(ownershipDirectory, "container.cid");
  const claim = { name, owner, cidFile, sensitive };
  try {
    return await executeOwnedContainerLifecycle({
      sensitive,
      control,
      claim,
      create: () =>
        runProcess(
          "docker",
          buildCensusCreateArguments({
            image,
            name,
            cidFile,
            owner,
            entrypoint,
            command,
          }),
          { timeoutMs: 30_000, sensitive, control },
        ),
      resolveOwnership: (createOutput) =>
        resolveDockerOwnership({ createOutput, cidFile, name, owner, sensitive, control }),
      start: (id) =>
        runProcess("docker", ["start", "--attach", id], {
          timeoutMs: 30_000,
          sensitive,
          control,
        }),
      operate: (_id, startResult) => {
        try {
          return JSON.parse(startResult.stdout);
        } catch {
          throw new ContainerContractError(`${label} was not valid JSON.`);
        }
      },
      cleanup: (id) => cleanupOwnedDockerContainer(id, owner, sensitive, control),
    });
  } finally {
    await rm(ownershipDirectory, { recursive: true, force: true });
  }
}

async function inspectStaticImage(image, control) {
  const dockerfile = await readContractFile(DOCKERFILE_PATH, "Dockerfile");
  const dockerignore = await readContractFile(DOCKERIGNORE_PATH, ".dockerignore");
  assertOutcome("Dockerfile contract failed", evaluateDockerfileContract(dockerfile));
  assertOutcome(
    "Build-context contract failed",
    evaluateDockerignoreContract(dockerignore),
  );

  return inspectLocalImageBeforeExecution({
    image,
    inspect: (args) => runProcess("docker", args, { control }),
    execute: async (immutableImage, observation) => {
  const runtimeScript = [
    "import json, os",
    "print(json.dumps({'runtimeUid': os.geteuid()}))",
  ].join("; ");
  const runtime = await runOwnedCensus({
    image: immutableImage,
    label: "Runtime identity census",
    entrypoint: "/app/.venv/bin/python",
    command: ["-c", runtimeScript],
    control,
  });
  observation.runtimeUid = runtime.runtimeUid;
  assertOutcome("Image configuration failed", evaluateImageConfiguration(observation));

  const packageScript = [
    "import importlib.metadata as m, json",
    "print(json.dumps([d.metadata.get('Name') for d in m.distributions()]))",
  ].join("; ");
  const packages = await runOwnedCensus({
    image: immutableImage,
    label: "Package census",
    entrypoint: "/app/.venv/bin/python",
    command: ["-c", packageScript],
    control,
  });
  assertOutcome("Package census failed", evaluatePackageCensus(packages));

  const filesystemScript = [
    "from pathlib import Path",
    "import json, os, subprocess",
    "root=Path('/app')",
    "venv=root/'.venv'",
    "probe_code=\"import importlib.util,json,shutil; print(json.dumps({'pip_module':importlib.util.find_spec('pip') is not None,'ensurepip_module':importlib.util.find_spec('ensurepip') is not None,'pip_path':shutil.which('pip'),'pip3_path':shutil.which('pip3'),'uv_path':shutil.which('uv'),'uvx_path':shutil.which('uvx')}))\"",
    "def probe(name, executable, path):\n env=dict(os.environ,PATH=path); run=subprocess.run([executable,'-c',probe_code],env=env,text=True,capture_output=True,check=True); return {'name':name,**json.loads(run.stdout)}",
    "installer_candidates=[Path('/usr/local/bin/pip'),Path('/usr/local/bin/pip3'),Path('/usr/local/bin/pip3.12'),venv/'bin/pip',venv/'bin/pip3',venv/'bin/pip3.12',Path('/usr/local/lib/python3.12/ensurepip'),Path('/usr/local/lib/python3.12/site-packages/pip')]",
    "credential_candidates=[root/'p01-untracked-credential.json',root/'credentials.json',root/'service-account.json']",
    "result={'env_paths':[str(p) for p in root.glob('.env*')], 'tests_paths':[str(root/'tests')] if (root/'tests').exists() else [], 'git_paths':[str(p) for p in root.rglob('.git')], 'credential_paths':[str(p) for p in credential_candidates if p.exists()], 'installer_paths':[str(p) for p in installer_candidates if p.exists()], 'python_probes':[probe('system','/usr/local/bin/python','/usr/local/bin:/usr/bin:/bin'),probe('venv','/app/.venv/bin/python','/app/.venv/bin:/usr/local/bin:/usr/bin:/bin')], 'app_owner_uid':root.stat().st_uid, 'venv_owner_uid':venv.stat().st_uid, 'app_writable':os.access(root,os.W_OK), 'venv_writable':os.access(venv,os.W_OK)}",
    "print(json.dumps(result, sort_keys=True))",
  ].join("\n");
  const filesystem = await runOwnedCensus({
    image: immutableImage,
    label: "Filesystem census",
    entrypoint: "/app/.venv/bin/python",
    command: ["-c", filesystemScript],
    control,
  });
  assertOutcome("Filesystem census failed", evaluateFilesystemCensus(filesystem));
  return immutableImage;
    },
  });
}

async function requestJson(url, { method = "GET", token, body, control } = {}) {
  control?.throwIfTerminated();
  const headers = { Accept: "application/json" };
  if (token !== undefined) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined) headers["Content-Type"] = "application/json";
  let response;
  try {
    const timeoutSignal = AbortSignal.timeout(control?.capTimeout(5_000, "work") ?? 5_000);
    response = await fetch(url, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      redirect: "manual",
      signal: control
        ? AbortSignal.any([timeoutSignal, control.abortSignal])
        : timeoutSignal,
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

async function waitForHealth(baseUrl, timeoutSeconds, control) {
  const deadline = Date.now() + timeoutSeconds * 1_000;
  while (Date.now() < deadline) {
    try {
      const response = await requestJson(`${baseUrl}/health`, { control });
      if (response.status === 200) return response;
    } catch (error) {
      if (!(error instanceof ContainerContractError)) throw error;
    }
    await new Promise((resolveDelay) =>
      setTimeout(resolveDelay, control?.capTimeout(250, "work") ?? 250));
  }
  throw new ContainerContractError("Timed out waiting for local dark health.");
}

async function waitForTerminal(baseUrl, jobId, token, control) {
  const deadline = Date.now() + 10_000;
  while (Date.now() < deadline) {
    const response = await requestJson(
      `${baseUrl}/internal/v1/research/jobs/${encodeURIComponent(jobId)}`,
      { token, control },
    );
    if (response.status !== 200 || !response.body || typeof response.body !== "object") {
      throw new ContainerContractError("Job-status response was malformed.");
    }
    if (TERMINAL_JOB_STATES.has(response.body.status)) return response.body;
    await new Promise((resolveDelay) =>
      setTimeout(resolveDelay, control?.capTimeout(100, "work") ?? 100));
  }
  throw new ContainerContractError("Timed out waiting for kill-switch terminal state.");
}

function createContainerName() {
  return `biostack-p01-${process.pid}-${randomBytes(8).toString("hex")}`;
}

function createSyntheticToken() {
  return `p01-local-only-${randomBytes(24).toString("hex")}`;
}

async function resolvePublishedPort(name, sensitive, control) {
  const { stdout } = await runProcess("docker", ["port", name, "8080/tcp"], {
    sensitive,
    control,
  });
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

async function inspectDarkEnvironment(name, sensitive, control) {
  const script = [
    "import json",
    "from biostack_research_sidecar.config import Settings",
    "s=Settings()",
    "print(json.dumps({'host':s.host,'service_token_configured':bool(s.service_token.strip()),'allow_insecure_dev_auth':s.allow_insecure_dev_auth,'global_kill_switch':s.global_kill_switch,'tooluniverse_enabled':s.tooluniverse_enabled,'hosted_fallback_enabled':s.hosted_fallback_enabled,'local_inference_enabled':s.local_inference_enabled,'gpu_enabled':s.gpu_enabled,'max_concurrent_research_jobs':s.max_concurrent_research_jobs,'log_level':s.log_level.lower()},sort_keys=True))",
  ].join("; ");
  return dockerJson(
    ["exec", name, "/app/.venv/bin/python", "-c", script],
    "Dark environment census",
    sensitive,
    control,
  );
}

async function verifyRuntime(options, control) {
  const name = options.containerName ?? createContainerName();
  const owner = createOwnershipMarker();
  const token = createSyntheticToken();
  const wrongToken = createSyntheticToken();
  const marker = (prefix) => `${prefix}-${randomBytes(12).toString("hex")}`;
  const privateMarker = marker("p01-private");
  const privacySubject = marker("P01Privacy");
  const privacyRequestId = marker("p01-privacy-request");
  const privacyCorrelationId = marker("p01-privacy-correlation");
  const arbitrarySubject = marker("P01Arbitrary");
  const arbitraryRequestId = marker("p01-arbitrary-request");
  const arbitraryCorrelationId = marker("p01-arbitrary-correlation");
  const publicMarker = marker("P01Kill");
  const killRequestId = marker("p01-kill-request");
  const killCorrelationId = marker("p01-kill-correlation");
  const arbitraryWorkflow = "execute_any_tool";
  const sensitive = [
    token,
    wrongToken,
    privateMarker,
    privacySubject,
    privacyRequestId,
    privacyCorrelationId,
    arbitrarySubject,
    arbitraryRequestId,
    arbitraryCorrelationId,
    publicMarker,
    killRequestId,
    killCorrelationId,
    arbitraryWorkflow,
    owner,
  ];
  let publicOutput = "P01 local container contract passed.";
  const ownershipDirectory = await mkdtemp(join(tmpdir(), "biostack-p01-"));
  const cidFile = join(ownershipDirectory, "container.cid");
  const claim = { name, owner, cidFile, sensitive };

  try {
    return await executeOwnedContainerLifecycle({
      sensitive,
      control,
      claim,
      create: () => runProcess(
        "docker",
        buildContainerCreateArguments({
          image: options.image,
          name,
          hostPort: options.hostPort,
          token,
          cidFile,
          owner,
        }),
        { timeoutMs: 30_000, sensitive, control },
      ),
      resolveOwnership: (createOutput) =>
        resolveDockerOwnership({ createOutput, cidFile, name, owner, sensitive, control }),
      start: (ownedContainerId) => runProcess("docker", ["start", ownedContainerId], {
        timeoutMs: 30_000,
        sensitive,
        control,
      }),
      cleanup: (ownedContainerId) =>
        cleanupOwnedDockerContainer(ownedContainerId, owner, sensitive, control),
      operate: async (ownedContainerId) => {
    const port = await resolvePublishedPort(ownedContainerId, sensitive, control);
    const baseUrl = `http://127.0.0.1:${port}`;
    const health = await waitForHealth(baseUrl, options.healthTimeoutSeconds, control);
    assertOutcome(
      "Health contract failed",
      evaluateHealthObservation(health.status, health.body),
    );

    const darkEnvironment = await inspectDarkEnvironment(ownedContainerId, sensitive, control);
    assertOutcome(
      "Dark environment contract failed",
      evaluateDarkEnvironment(darkEnvironment),
    );

    const missing = await requestJson(`${baseUrl}/internal/v1/workflows`, { control });
    const wrong = await requestJson(`${baseUrl}/internal/v1/workflows`, {
      token: wrongToken,
      control,
    });
    const correct = await requestJson(`${baseUrl}/internal/v1/workflows`, {
      token,
      control,
    });
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
      control,
      body: {
        research_request_id: privacyRequestId,
        subject_name: privacySubject,
        workflow: "resolve_compound_identity",
        patient_id: privateMarker,
        data_classification: "public_scientific",
        correlation_id: privacyCorrelationId,
      },
    });
    const arbitrary = await requestJson(`${baseUrl}/internal/v1/research/jobs`, {
      method: "POST",
      token,
      control,
      body: {
        research_request_id: arbitraryRequestId,
        subject_name: arbitrarySubject,
        workflow: arbitraryWorkflow,
        data_classification: "public_scientific",
        correlation_id: arbitraryCorrelationId,
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
      control,
      body: {
        research_request_id: killRequestId,
        subject_name: publicMarker,
        workflow: "resolve_compound_identity",
        data_classification: "public_scientific",
        correlation_id: killCorrelationId,
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
    sensitive.push(jobId);
    const terminal = await waitForTerminal(baseUrl, jobId, token, control);
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
      const response = await requestJson(`${baseUrl}${route}`, { control });
      docs[route] = response.status;
    }
    assertOutcome("Documentation-route contract failed", evaluateDocsRoutes(docs));

    const { stdout: logs, stderr: logErrors } = await runProcess(
      "docker",
      ["logs", ownedContainerId],
      { sensitive, control },
    );
    assertOutcome(
      "Log/output hygiene failed",
      evaluateLeakage(`${logs}\n${logErrors}\n${publicOutput}`, sensitive),
    );

    return { status: "passed", checks: 9, imageId: options.image };
      },
    });
  } finally {
    await rm(ownershipDirectory, { recursive: true, force: true });
  }
}

export async function main(
  argv = process.argv.slice(2),
  { control = createExecutionControl() } = {},
) {
  const options = parseCliArgs(argv);
  control.throwIfTerminated();
  const immutableImage = await inspectStaticImage(options.image, control);
  control.throwIfTerminated();
  const result = await verifyRuntime({ ...options, image: immutableImage }, control);
  control.throwIfTerminated();
  console.log(JSON.stringify(result));
}

export async function runDirectCli(
  argv = process.argv.slice(2),
  {
    control = createExecutionControl(),
    mainFunction = main,
    processObject = process,
    writeError = (message) => console.error(message),
    recoverFunction = recoverPendingClaim,
  } = {},
) {
  const handlers = new Map(
    ["SIGTERM", "SIGINT"].map((signal) => [
      signal,
      () => control.requestTermination(signal),
    ]),
  );
  for (const [signal, handler] of handlers) processObject.on(signal, handler);
  try {
    await mainFunction(argv, { control });
    control.throwIfTerminated();
    return 0;
  } catch (error) {
    const sensitive = control.pendingClaim?.sensitive ?? [];
    const primaryError = asContractError(error, sensitive);
    let recoveryError;
    if (control.pendingClaim) {
      try {
        await recoverFunction(control);
      } catch (caught) {
        recoveryError = asContractError(caught, sensitive);
      }
    }
    const diagnostics = primaryError.diagnostics ?? [primaryError.message];
    for (const [index, diagnostic] of diagnostics.entries()) {
      const label = index === 0 ? "failed" : "diagnostic";
      writeError(
        `research-sidecar container contract ${label}: ${sanitizeDiagnostic(diagnostic, sensitive)}`,
      );
    }
    if (recoveryError) {
      writeError(
        `research-sidecar outer ownership recovery failed: ${sanitizeDiagnostic(recoveryError.message, sensitive)}`,
      );
    }
    return 1;
  } finally {
    for (const [signal, handler] of handlers) processObject.off(signal, handler);
  }
}

if (isDirectExecution(import.meta.url, process.argv[1])) {
  runDirectCli().then((exitCode) => {
    process.exitCode = exitCode;
  });
}
