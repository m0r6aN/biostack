import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

import {
  FORBIDDEN_DISTRIBUTIONS,
  REQUIRED_DARK_ENVIRONMENT,
  buildContainerCleanupArguments,
  buildContainerCreateArguments,
  evaluateAuthObservations,
  evaluateDarkEnvironment,
  evaluateDockerfileContract,
  evaluateDockerignoreContract,
  evaluateDocsRoutes,
  evaluateFilesystemCensus,
  evaluateHealthObservation,
  evaluateImageConfiguration,
  evaluateKillSwitchObservations,
  evaluateLeakage,
  evaluatePackageCensus,
  evaluateRejectionObservations,
  parseCliArgs,
  parseImageInspect,
  parseOwnedContainerId,
  readBoundedResponseText,
  sanitizeDiagnostic,
} from "./verify-research-sidecar-container.mjs";

const dockerfile = readFileSync(
  new URL("../backend/research-sidecar/Dockerfile", import.meta.url),
  "utf8",
);
const dockerignore = readFileSync(
  new URL("../backend/research-sidecar/.dockerignore", import.meta.url),
  "utf8",
);

function assertFails(result, pattern) {
  assert.equal(result.ok, false);
  assert.match(result.errors.join(" "), pattern);
}

function validImageObservation(overrides = {}) {
  return {
    user: "biostack",
    command: ["python", "-m", "biostack_research_sidecar"],
    exposedPorts: { "8080/tcp": {} },
    workingDirectory: "/app",
    runtimeUid: 999,
    ...overrides,
  };
}

function validAuthObservation(overrides = {}) {
  return {
    missingStatus: 401,
    wrongStatus: 401,
    correctStatus: 200,
    correctBody: {
      tooluniverse_enabled: false,
      allowed_workflows: ["resolve_compound_identity"],
    },
    ...overrides,
  };
}

test("argument parser accepts a safe local image and bounded overrides", () => {
  assert.deepEqual(
    parseCliArgs([
      "--image",
      "biostack-research-sidecar:p01",
      "--container-name",
      "p01.contract_test-1",
      "--host-port",
      "18080",
      "--health-timeout-seconds",
      "30",
    ]),
    {
      image: "biostack-research-sidecar:p01",
      containerName: "p01.contract_test-1",
      hostPort: 18080,
      healthTimeoutSeconds: 30,
    },
  );
});

test("argument parser generates defaults only for optional runtime values", () => {
  assert.deepEqual(parseCliArgs(["--image", "local/image@sha256:" + "a".repeat(64)]), {
    image: "local/image@sha256:" + "a".repeat(64),
    containerName: undefined,
    hostPort: undefined,
    healthTimeoutSeconds: 45,
  });
});

for (const [name, argv, pattern] of [
  ["non-array input", null, /array/],
  ["positional argument", ["image", "local:test"], /named option/],
  ["bare option", ["--", "local:test"], /named option/],
  ["unknown option", ["--image", "local:test", "--wat", "x"], /Unknown option/],
  ["duplicate option", ["--image", "one:test", "--image", "two:test"], /Duplicate/],
  ["missing required image", [], /Missing required/],
  ["missing value", ["--image"], /Malformed value/],
  ["empty value", ["--image", ""], /Malformed value/],
  ["leading whitespace", ["--image", " local:test"], /Malformed value/],
  ["trailing whitespace", ["--image", "local:test "], /Malformed value/],
  ["option-shaped value", ["--image", "--wrong"], /Malformed value/],
  ["control character", ["--image", "local:test\nspoof"], /Malformed value/],
  ["unsafe image shell marker", ["--image", "local:test;whoami"], /safe local image/],
  ["unsafe image path space", ["--image", "local image:test"], /safe local image/],
  ["unsafe container wildcard", ["--image", "local:test", "--container-name", "p01*"], /unsafe shape/],
  ["unsafe container slash", ["--image", "local:test", "--container-name", "p01/other"], /unsafe shape/],
  ["non-integer port", ["--image", "local:test", "--host-port", "12.5"], /integer/],
  ["low port", ["--image", "local:test", "--host-port", "1023"], /between/],
  ["high port", ["--image", "local:test", "--host-port", "65536"], /between/],
  ["non-integer timeout", ["--image", "local:test", "--health-timeout-seconds", "x"], /integer/],
  ["zero timeout", ["--image", "local:test", "--health-timeout-seconds", "0"], /between/],
  ["excessive timeout", ["--image", "local:test", "--health-timeout-seconds", "181"], /between/],
]) {
  test(`argument parser rejects ${name}`, () => {
    assert.throws(() => parseCliArgs(argv), pattern);
  });
}

test("diagnostic sanitizer removes line, annotation, terminal, and secret injection", () => {
  const secret = "p01-local-sensitive-value";
  const result = sanitizeDiagnostic(`bad\n::error::\x1b[31m${secret}\x1b[0m`, [secret]);
  assert.equal(result.includes("\n"), false);
  assert.equal(result.includes("::"), false);
  assert.equal(result.includes("\x1b"), false);
  assert.equal(result.includes(secret), false);
  assert.match(result, /\[REDACTED\]/);
});

test("Dockerfile contract accepts the checked-in production Dockerfile", () => {
  assert.deepEqual(evaluateDockerfileContract(dockerfile), { ok: true, errors: [] });
});

for (const [name, mutate, pattern] of [
  ["floating base", (value) => value.replace(/@sha256:[a-f0-9]{64}/, ""), /verified OCI|sha256/],
  ["wrong base digest", (value) => value.replace(/e5b65587[a-f0-9]+/, "f".repeat(64)), /verified OCI/],
  ["provider-extra default", (value) => value.replace("ARG INCLUDE_TOOLUNIVERSE=false", "ARG INCLUDE_TOOLUNIVERSE=true"), /no-extra/],
  ["dependency-only no-extra", (value) => value.replace("false) uv sync --locked --no-dev --no-install-project ;;", "false) uv sync --locked --no-dev --extra tooluniverse --no-install-project ;;"), /Dependency-only no-extra/],
  ["project no-extra", (value) => value.replace("false) uv sync --locked --no-dev ;;", "false) uv sync --locked --no-dev --extra tooluniverse ;;"), /Project no-extra/],
  ["dependency optional contract", (value) => value.replace("true) uv sync --locked --no-dev --extra tooluniverse --no-install-project ;;", "true) uv sync --locked --no-dev --no-install-project ;;"), /Dependency-only ToolUniverse/],
  ["project optional contract", (value) => value.replace("true) uv sync --locked --no-dev --extra tooluniverse ;;", "true) uv sync --locked --no-dev ;;"), /Project ToolUniverse/],
  ["unsafe empty extra", (value) => value + "\nRUN uv sync --extra ${TOOLUNIVERSE_EXTRA}\n", /Unsafe variable/],
  ["unsafe quoted empty extra", (value) => value + '\nRUN uv sync --extra "${TOOLUNIVERSE_EXTRA}"\n', /Unsafe variable/],
  ["root user", (value) => value.replace("USER biostack", "USER root"), /user/],
  ["wrong port", (value) => value.replace("EXPOSE 8080", "EXPOSE 80"), /port/],
  ["wrong command", (value) => value.replace('CMD ["python", "-m", "biostack_research_sidecar"]', 'CMD ["sh"]'), /command/],
]) {
  test(`Dockerfile mutation binds ${name}`, () => {
    assertFails(evaluateDockerfileContract(mutate(dockerfile)), pattern);
  });
}

test("dockerignore contract accepts the checked-in exclusions", () => {
  assert.deepEqual(evaluateDockerignoreContract(dockerignore), { ok: true, errors: [] });
});

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
  test(`dockerignore mutation binds ${pattern}`, () => {
    const mutated = dockerignore
      .split(/\r?\n/)
      .filter((line) => line.trim() !== pattern)
      .join("\n");
    assertFails(evaluateDockerignoreContract(mutated), new RegExp(pattern.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")));
  });
}

test("dockerignore rejects malformed non-text input", () => {
  assertFails(evaluateDockerignoreContract(null), /not text/);
});

test("image inspect parser accepts one Config object", () => {
  assert.deepEqual(
    parseImageInspect(
      JSON.stringify([
        {
          Config: {
            User: "biostack",
            Cmd: ["python", "-m", "biostack_research_sidecar"],
            ExposedPorts: { "8080/tcp": {} },
            WorkingDir: "/app",
          },
        },
      ]),
    ),
    {
      user: "biostack",
      command: ["python", "-m", "biostack_research_sidecar"],
      exposedPorts: { "8080/tcp": {} },
      workingDirectory: "/app",
    },
  );
});

for (const [name, value, pattern] of [
  ["invalid JSON", "not-json", /valid JSON/],
  ["non-array JSON", "{}", /unexpected shape/],
  ["empty array", "[]", /unexpected shape/],
  ["multiple images", "[{},{}]", /unexpected shape/],
  ["missing Config", "[{}]", /omitted Config/],
]) {
  test(`image inspect parser rejects ${name}`, () => {
    assert.throws(() => parseImageInspect(value), pattern);
  });
}

test("image configuration accepts exact non-root runtime contract", () => {
  assert.deepEqual(evaluateImageConfiguration(validImageObservation()), {
    ok: true,
    errors: [],
  });
});

for (const [name, overrides, pattern] of [
  ["empty user", { user: "" }, /non-root/],
  ["named root user", { user: "root" }, /non-root/],
  ["numeric root user", { user: "0:0" }, /non-root/],
  ["wrong command", { command: ["sh"] }, /command/],
  ["missing command", { command: undefined }, /command/],
  ["missing port", { exposedPorts: {} }, /8080/],
  ["extra port", { exposedPorts: { "8080/tcp": {}, "9000/tcp": {} } }, /8080/],
  ["wrong working directory", { workingDirectory: "/" }, /working directory/],
  ["root runtime uid", { runtimeUid: 0 }, /UID/],
  ["malformed runtime uid", { runtimeUid: "999" }, /UID/],
]) {
  test(`image configuration mutation binds ${name}`, () => {
    assertFails(evaluateImageConfiguration(validImageObservation(overrides)), pattern);
  });
}

test("package census accepts a provider-SDK-free environment", () => {
  assert.deepEqual(evaluatePackageCensus(["fastapi", "pydantic-settings"]), {
    ok: true,
    errors: [],
  });
});

for (const distribution of FORBIDDEN_DISTRIBUTIONS) {
  test(`package census mutation binds forbidden ${distribution}`, () => {
    assertFails(evaluatePackageCensus(["fastapi", distribution]), new RegExp(distribution));
  });
}

test("package census normalizes underscore names and fails closed on malformed rows", () => {
  assertFails(evaluatePackageCensus(["huggingface_hub"]), /huggingface-hub/);
  assertFails(evaluatePackageCensus("fastapi"), /malformed/);
  assertFails(evaluatePackageCensus([""]), /malformed/);
});

test("filesystem census accepts an empty forbidden-path inventory", () => {
  assert.deepEqual(
    evaluateFilesystemCensus({ env_paths: [], tests_paths: [], git_paths: [] }),
    { ok: true, errors: [] },
  );
});

for (const key of ["env_paths", "tests_paths", "git_paths"]) {
  test(`filesystem census mutation binds present ${key}`, () => {
    const value = { env_paths: [], tests_paths: [], git_paths: [], [key]: ["/app/bad"] };
    assertFails(evaluateFilesystemCensus(value), /forbidden/);
  });
  test(`filesystem census mutation binds missing ${key}`, () => {
    const value = { env_paths: [], tests_paths: [], git_paths: [] };
    delete value[key];
    assertFails(evaluateFilesystemCensus(value), new RegExp(key));
  });
}

test("filesystem census rejects malformed top-level input", () => {
  assertFails(evaluateFilesystemCensus([]), /malformed/);
});

test("dark environment accepts every explicit fail-closed value", () => {
  assert.deepEqual(evaluateDarkEnvironment({ ...REQUIRED_DARK_ENVIRONMENT }), {
    ok: true,
    errors: [],
  });
});

for (const [key, expected] of Object.entries(REQUIRED_DARK_ENVIRONMENT)) {
  test(`dark environment mutation binds wrong ${key}`, () => {
    const wrong = typeof expected === "boolean" ? !expected : expected === 1 ? 2 : "127.0.0.1";
    assertFails(evaluateDarkEnvironment({ ...REQUIRED_DARK_ENVIRONMENT, [key]: wrong }), new RegExp(key));
  });
  test(`dark environment mutation binds missing ${key}`, () => {
    const mutated = { ...REQUIRED_DARK_ENVIRONMENT };
    delete mutated[key];
    assertFails(evaluateDarkEnvironment(mutated), new RegExp(key));
  });
}

test("dark environment rejects malformed top-level input", () => {
  assertFails(evaluateDarkEnvironment(null), /malformed/);
});

const healthyBody = {
  status: "disabled",
  service: "biostack-research-sidecar",
  global_kill_switch: true,
  tooluniverse_enabled: false,
  max_concurrent_research_jobs: 1,
};

test("health evaluator accepts the exact dark response", () => {
  assert.deepEqual(evaluateHealthObservation(200, healthyBody), { ok: true, errors: [] });
});

test("health mutation binds HTTP status", () => {
  assertFails(evaluateHealthObservation(503, healthyBody), /HTTP 200/);
});

for (const [key, expected] of Object.entries(healthyBody)) {
  test(`health mutation binds ${key}`, () => {
    const wrong = typeof expected === "boolean" ? !expected : typeof expected === "number" ? 2 : "wrong";
    assertFails(evaluateHealthObservation(200, { ...healthyBody, [key]: wrong }), new RegExp(key));
  });
}

test("health evaluator rejects malformed body", () => {
  assertFails(evaluateHealthObservation(200, []), /malformed/);
});

test("auth evaluator accepts two denials and one bounded success", () => {
  assert.deepEqual(evaluateAuthObservations(validAuthObservation()), { ok: true, errors: [] });
});

for (const [name, mutate, pattern] of [
  ["missing-token denial", (value) => ({ ...value, missingStatus: 200 }), /Missing token/],
  ["wrong-token denial", (value) => ({ ...value, wrongStatus: 200 }), /Wrong token/],
  ["correct-token success", (value) => ({ ...value, correctStatus: 401 }), /Correct token/],
  ["response shape", (value) => ({ ...value, correctBody: null }), /malformed/],
  ["workflow list type", (value) => ({ ...value, correctBody: { ...value.correctBody, allowed_workflows: 7 } }), /allowlisted/],
  ["ToolUniverse disabled", (value) => ({ ...value, correctBody: { ...value.correctBody, tooluniverse_enabled: true } }), /ToolUniverse/],
  ["allowlisted workflow", (value) => ({ ...value, correctBody: { ...value.correctBody, allowed_workflows: [] } }), /allowlisted/],
  ["arbitrary-tool absence", (value) => ({ ...value, correctBody: { ...value.correctBody, allowed_workflows: ["resolve_compound_identity", "execute_any_tool"] } }), /arbitrary/],
]) {
  test(`auth mutation binds ${name}`, () => {
    assertFails(evaluateAuthObservations(mutate(validAuthObservation())), pattern);
  });
}

const rejectionObservation = {
  privacyStatus: 422,
  privacyCode: "unknown_request_fields",
  workflowStatus: 422,
  workflowCode: "schema_validation_failed",
};

test("admission evaluator accepts privacy and arbitrary-workflow rejection", () => {
  assert.deepEqual(evaluateRejectionObservations(rejectionObservation), {
    ok: true,
    errors: [],
  });
});

for (const [key, value, pattern] of [
  ["privacyStatus", 202, /Private-shaped/],
  ["privacyCode", "other", /privacy boundary/],
  ["workflowStatus", 202, /Arbitrary workflow/],
  ["workflowCode", "other", /fail-closed code/],
]) {
  test(`admission mutation binds ${key}`, () => {
    assertFails(evaluateRejectionObservations({ ...rejectionObservation, [key]: value }), pattern);
  });
}

const killObservation = {
  submissionStatus: 202,
  submittedState: "queued",
  terminalState: "rejected_by_policy",
  errorCode: "global_kill_switch",
};

test("kill-switch evaluator accepts honest 202-to-terminal rejection", () => {
  assert.deepEqual(evaluateKillSwitchObservations(killObservation), {
    ok: true,
    errors: [],
  });
});

for (const [key, value, pattern] of [
  ["submissionStatus", 403, /HTTP 202/],
  ["submittedState", "rejected_by_policy", /queued/],
  ["terminalState", "partial", /rejected_by_policy/],
  ["errorCode", "other", /global-kill/],
]) {
  test(`kill-switch mutation binds ${key}`, () => {
    assertFails(evaluateKillSwitchObservations({ ...killObservation, [key]: value }), pattern);
  });
}

const docsObservation = { "/docs": 404, "/redoc": 404, "/openapi.json": 404 };

test("docs evaluator accepts three closed routes", () => {
  assert.deepEqual(evaluateDocsRoutes(docsObservation), { ok: true, errors: [] });
});

for (const route of Object.keys(docsObservation)) {
  test(`docs mutation binds ${route}`, () => {
    assertFails(evaluateDocsRoutes({ ...docsObservation, [route]: 200 }), new RegExp(route.replace(".", "\\.")));
  });
}

test("docs evaluator rejects malformed input", () => {
  assertFails(evaluateDocsRoutes(null), /malformed/);
});

test("leakage evaluator accepts clean output and binds each sensitive marker", () => {
  assert.deepEqual(evaluateLeakage("safe fixed output", ["token-123456", "request-123456"]), {
    ok: true,
    errors: [],
  });
  assertFails(evaluateLeakage("contains token-123456", ["token-123456"]), /sensitive marker/);
  assertFails(evaluateLeakage("contains request-123456", ["request-123456"]), /sensitive marker/);
});

test("leakage evaluator fails closed on malformed markers and observations", () => {
  assertFails(evaluateLeakage(null, []), /malformed/);
  assertFails(evaluateLeakage("safe", ["short"]), /marker is malformed/);
});

test("Docker argument builders preserve hostile-looking values as single arguments", () => {
  const token = "literal $(whoami);`hostname`\n::error::";
  const args = buildContainerCreateArguments({
    image: "local/image:test",
    name: "p01-safe-name",
    hostPort: 18080,
    token,
  });
  assert.deepEqual(args.slice(0, 6), [
    "create",
    "--name",
    "p01-safe-name",
    "--publish",
    "127.0.0.1:18080:8080",
    "--env",
  ]);
  assert.equal(args.includes(`BIOSTACK_RESEARCH_SERVICE_TOKEN=${token}`), true);
  assert.equal(args.at(-1), "local/image:test");
});

test("ephemeral port binding and cleanup stay confined to one exact name", () => {
  const args = buildContainerCreateArguments({
    image: "local:test",
    name: "p01-exact",
    hostPort: undefined,
    token: "p01-local-only-token",
  });
  assert.equal(args[4], "127.0.0.1::8080");
});

test("cleanup ownership is established only from one full Docker container ID", () => {
  const id = "a".repeat(64);
  assert.equal(parseOwnedContainerId(`${id}\n`), id);
  for (const hostile of ["", "abc", `${id}\n${"b".repeat(64)}`, "g".repeat(64), null]) {
    assert.throws(() => parseOwnedContainerId(hostile), /full container ID/);
  }
  assert.deepEqual(buildContainerCleanupArguments(id), ["rm", "--force", id]);
});

test("HTTP response reader accepts bounded UTF-8 and rejects size or header abuse", async () => {
  assert.equal(await readBoundedResponseText(new Response("safe"), 8), "safe");
  await assert.rejects(
    readBoundedResponseText(new Response("123456789"), 8),
    /size limit/,
  );
  await assert.rejects(
    readBoundedResponseText(
      new Response("safe", { headers: { "content-length": "not-a-number" } }),
      8,
    ),
    /size limit/,
  );
  await assert.rejects(readBoundedResponseText(null, 8), /malformed/);
  await assert.rejects(readBoundedResponseText(new Response("safe"), 0), /malformed/);
  await assert.rejects(
    readBoundedResponseText(new Response(new Uint8Array([0xff])), 8),
    /valid UTF-8/,
  );
});

test("implementation uses execFile argument arrays and contains no shell escape hatch", () => {
  const source = readFileSync(
    new URL("./verify-research-sidecar-container.mjs", import.meta.url),
    "utf8",
  );
  assert.match(source, /execFileAsync\(command, args/);
  assert.doesNotMatch(source, /\bshell\s*:/);
  assert.doesNotMatch(source, /\bexecSync\s*\(/);
  assert.doesNotMatch(source, /(?:cmd\.exe|powershell|sh),?\s*["'](?:\/c|-c)/i);
});

test("CI is path-scoped, read-only, bounded, and contains every deterministic gate", () => {
  const workflow = readFileSync(
    new URL("../.github/workflows/research-sidecar-ci.yml", import.meta.url),
    "utf8",
  );
  assert.match(workflow, /pull_request:\s*\n\s+paths:/);
  assert.match(workflow, /push:\s*\n\s+paths:/);
  for (const path of [
    "backend/research-sidecar/\\*\\*",
    "scripts/verify-research-sidecar-container\\.mjs",
    "scripts/verify-research-sidecar-container\\.test\\.mjs",
    "\\.github/workflows/research-sidecar-ci\\.yml",
  ]) {
    assert.match(workflow, new RegExp(path));
  }
  assert.match(workflow, /permissions:\s*\n\s+contents: read/);
  assert.doesNotMatch(workflow, /id-token:\s*write/);
  assert.match(workflow, /timeout-minutes: 30/);
  assert.match(workflow, /python-version: "3\.12"/);
  assert.match(workflow, /node-version: "22"/);
  assert.match(workflow, /uv==0\.9\.7/);
  assert.match(workflow, /pip-audit==2\.9\.0/);
  assert.match(workflow, /uv sync --frozen --extra dev/);
  assert.match(workflow, /pytest[\s\\]+\n\s+tests/);
  assert.match(workflow, /--junitxml/);
  assert.match(workflow, /"tests": 54, "failures": 0, "errors": 0, "skipped": 1/);
  assert.match(workflow, /test_runner_terminal_state_reproduction/);
  assert.match(workflow, /test_request_constraint_reproduction/);
  assert.match(workflow, /uv export/);
  assert.match(workflow, /--no-dev/);
  assert.match(workflow, /--no-emit-project/);
  assert.match(workflow, /pip-audit[\s\\]+\n\s+--strict/);
  assert.match(workflow, /node --test scripts\/verify-research-sidecar-container\.test\.mjs/);
  assert.match(workflow, /GITLEAKS_VERSION: "8\.24\.3"/);
  assert.match(workflow, /gitleaks" dir/);
  assert.match(workflow, /docker build/);
  assert.match(workflow, /--tag biostack-research-sidecar:p01-ci/);
  assert.match(workflow, /node scripts\/verify-research-sidecar-container\.mjs/);
  assert.match(workflow, /--image biostack-research-sidecar:p01-ci/);
});

test("CI contains no deployment, registry-push, provider, or environment-dump action", () => {
  const workflow = readFileSync(
    new URL("../.github/workflows/research-sidecar-ci.yml", import.meta.url),
    "utf8",
  );
  for (const forbidden of [
    /azure\/login/i,
    /\baz\s+(?:login|acr|containerapp)\b/i,
    /docker\s+push/i,
    /tooluniverse_enabled=true/i,
    /hosted_fallback_enabled=true/i,
    /local_inference_enabled=true/i,
    /gpu_enabled=true/i,
    /\b(?:printenv|env)\s*$/im,
  ]) {
    assert.doesNotMatch(workflow, forbidden);
  }
});
