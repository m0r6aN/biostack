import assert from "node:assert/strict";
import { spawn, spawnSync } from "node:child_process";
import { once } from "node:events";
import { chmodSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import test from "node:test";

import {
  ALLOWED_WORKFLOWS,
  CLEANUP_RESERVE_MS,
  DIRECT_CLI_TIMEOUT_MS,
  OUTER_RECOVERY_RESERVE_MS,
  FORBIDDEN_DISTRIBUTIONS,
  OWNERSHIP_LABEL_KEY,
  REQUIRED_DARK_ENVIRONMENT,
  REQUIRED_DISTRIBUTIONS,
  REQUIRED_IMAGE_ENVIRONMENT,
  SIGNAL_SHUTDOWN_TIMEOUT_MS,
  buildContainerCleanupArguments,
  buildContainerCreateArguments,
  buildCensusCreateArguments,
  buildOwnershipInspectArguments,
  buildOwnershipLookupArguments,
  createExecutionControl,
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
  executeAfterImageValidation,
  executeOwnedContainerLifecycle,
  inspectLocalImageBeforeExecution,
  isDirectExecution,
  parseCliArgs,
  parseImageInspect,
  parseOwnedContainerId,
  reconcileOwnedContainer,
  recoverPendingClaim,
  readBoundedResponseText,
  requireImmutableImageId,
  runDirectCli,
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
const pyproject = readFileSync(
  new URL("../backend/research-sidecar/pyproject.toml", import.meta.url),
  "utf8",
);
const workflow = readFileSync(
  new URL("../.github/workflows/research-sidecar-ci.yml", import.meta.url),
  "utf8",
);

function extractWorkflowRunBody(stepName) {
  const lines = workflow.split(/\r?\n/u);
  const marker = `      - name: ${stepName}`;
  const stepIndex = lines.indexOf(marker);
  assert.notEqual(stepIndex, -1, `missing workflow step: ${stepName}`);
  assert.equal(lines[stepIndex + 1], "        run: |");
  const body = [];
  for (let index = stepIndex + 2; index < lines.length; index += 1) {
    const line = lines[index];
    if (line.startsWith("      - name:")) break;
    if (line.length === 0) {
      body.push("");
    } else {
      assert.equal(line.startsWith("          "), true, `unexpected YAML indentation: ${line}`);
      body.push(line.slice(10));
    }
  }
  return `${body.join("\n")}\n`;
}

function toBashPath(value) {
  if (process.platform !== "win32") return value;
  return value
    .replace(/^([A-Za-z]):/u, (_match, drive) => `/mnt/${drive.toLowerCase()}`)
    .replaceAll("\\", "/");
}

function runLiteralTermFixture({ scenario, nonce }) {
  const directory = mkdtempSync(join(tmpdir(), "biostack-p01-term-mock-"));
  const bashDirectory = toBashPath(directory);
  const fullId = "a".repeat(64);
  const owner = "b".repeat(48);
  const dockerMock = `#!/usr/bin/env bash
set -eu
printf 'docker' >> "\${MOCK_DIR}/commands.log"
printf '\\t%s' "$@" >> "\${MOCK_DIR}/commands.log"
printf '\\n' >> "\${MOCK_DIR}/commands.log"
if [[ "$1 $2 $3" == "container ls --all" ]]; then
  count=0
  [[ ! -f "\${MOCK_DIR}/ls-count" ]] || count="$(cat "\${MOCK_DIR}/ls-count")"
  count=$((count + 1))
  printf '%s' "\${count}" > "\${MOCK_DIR}/ls-count"
  if [[ "\${MOCK_SCENARIO}" == "query-failure" ]] && [[ "\${count}" -eq 1 ]]; then
    exit 42
  fi
  if [[ "\${MOCK_SCENARIO}" == "final-name-failure" ]] && [[ "\${count}" -eq 4 ]]; then
    exit 42
  fi
  if [[ "\${MOCK_SCENARIO}" == "final-label-failure" ]] && [[ "\${count}" -eq 5 ]]; then
    exit 42
  fi
  if [[ "\${MOCK_SCENARIO}" == "collision" ]] && [[ "\${count}" -eq 1 ]]; then
    printf '%s\\n' '${fullId}'
    exit 0
  fi
  if [[ "\${count}" -eq 3 ]]; then
    printf '%s\\n' '${fullId}'
  elif [[ "\${MOCK_SCENARIO}" == "trap-cleanup" ]] && [[ "\${count}" -eq 4 ]]; then
    printf '%s\\n' '${fullId}'
  fi
  exit 0
fi
if [[ "$1 $2 $3" == "container inspect --format" ]]; then
  if [[ "$4" == *'.Name'* ]]; then
    printf '/%s\\n' "$(cat "\${MOCK_DIR}/fixture-name")"
  else
    printf '%s\\n' '${owner}'
  fi
  exit 0
fi
if [[ "$1 $2 $3" == "container rm --force" ]]; then
  printf 'removed=%s\\n' "$4" >> "\${MOCK_DIR}/removed.log"
  exit 0
fi
exit 90
`;
  const nodeMock = `#!/usr/bin/env bash
set -eu
if [[ "\${1:-}" == "-e" ]]; then
  printf '%s' "\${MOCK_NONCE}"
  exit 0
fi
previous=""
for argument in "$@"; do
  if [[ "\${previous}" == "--container-name" ]]; then
    printf '%s' "\${argument}" > "\${MOCK_DIR}/fixture-name"
  fi
  previous="\${argument}"
done
printf '%s\\n' 'node-started' > "\${MOCK_DIR}/node-started"
if [[ "\${MOCK_SCENARIO}" == "success" ]]; then
  trap 'docker container rm --force ${fullId} >/dev/null 2>&1; exit 1' TERM INT
else
  trap 'exit 1' TERM INT
fi
while :; do sleep 0.05; done
`;
  const timeoutMock = `#!/usr/bin/env bash
set -eu
shift
exec "$@"
`;
  try {
    for (const [name, contents] of [
      ["docker", dockerMock],
      ["node", nodeMock],
      ["timeout", timeoutMock],
    ]) {
      const path = join(directory, name);
      writeFileSync(path, contents, "utf8");
      chmodSync(path, 0o755);
    }
    const result = spawnSync("bash", ["-s"], {
      encoding: "utf8",
      env: process.env,
      input: [
        `export MOCK_DIR=${JSON.stringify(bashDirectory)}`,
        `export MOCK_NONCE=${JSON.stringify(nonce)}`,
        `export MOCK_SCENARIO=${JSON.stringify(scenario)}`,
        `export RUNNER_TEMP=${JSON.stringify(bashDirectory)}`,
        `export PATH=${JSON.stringify(`${bashDirectory}:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin`)}`,
        extractWorkflowRunBody("Prove catchable TERM cleans the verifier-owned container"),
      ].join("\n"),
      shell: false,
      timeout: 10_000,
      windowsHide: true,
    });
    const readOptional = (name) => {
      try {
        return readFileSync(join(directory, name), "utf8");
      } catch {
        return "";
      }
    };
    return {
      ...result,
      commands: readOptional("commands.log"),
      fixtureName: readOptional("fixture-name"),
      nodeStarted: readOptional("node-started"),
      removed: readOptional("removed.log"),
      fullId,
    };
  } finally {
    rmSync(directory, { recursive: true, force: true });
  }
}

function extractBuildBackendManifest(contents) {
  const match = /# P01_BUILD_BACKEND_REQUIREMENTS_BEGIN\r?\n(?<body>[\s\S]*?)\r?\n\s*# P01_BUILD_BACKEND_REQUIREMENTS_END/u.exec(contents);
  assert.notEqual(match, null);
  return match.groups.body
    .split(/\r?\n/u)
    .map((line) => line.trim())
    .filter(Boolean)
    .join("\n");
}

function assertFails(result, pattern) {
  assert.equal(result.ok, false);
  assert.match(result.errors.join(" "), pattern);
}

function validImageObservation(overrides = {}) {
  return {
    imageId: `sha256:${"a".repeat(64)}`,
    user: "biostack",
    entrypoint: null,
    command: ["python", "-m", "biostack_research_sidecar"],
    environment: [...REQUIRED_IMAGE_ENVIRONMENT],
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
      allowed_workflows: [...ALLOWED_WORKFLOWS],
    },
    ...overrides,
  };
}

function validFilesystemObservation(overrides = {}) {
  const cleanProbe = (name) => ({
    name,
    pip_module: false,
    ensurepip_module: false,
    pip_path: null,
    pip3_path: null,
    uv_path: null,
    uvx_path: null,
  });
  return {
    env_paths: [],
    tests_paths: [],
    git_paths: [],
    credential_paths: [],
    installer_paths: [],
    python_probes: [cleanProbe("system"), cleanProbe("venv")],
    app_owner_uid: 0,
    venv_owner_uid: 0,
    app_writable: false,
    venv_writable: false,
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

test("diagnostic sanitizer redacts a token before the truncation boundary", () => {
  const secret = "p01-local-only-boundary-sensitive-value";
  const result = sanitizeDiagnostic(`${"x".repeat(590)}${secret}after`, [secret]);
  assert.equal(result.length, 600);
  assert.match(result, /\[REDACTED\]$/);
  assert.equal(result.includes(secret), false);
  assert.equal(result.includes("p01-local-"), false);
  assert.equal(result.includes("sensitive-value"), false);
});

test("diagnostic sanitizer strips CSI, OSC, C0, C1, DEL, and annotation controls", () => {
  const hostile = [
    "safe",
    "\x1b[2Jerase",
    "\x1b[Hhome",
    "\x1b]0;title\x07osc-bel",
    "\x1b]8;;https://invalid.example\x1b\\osc-st",
    "\x1bPpayload\x1b\\dcs",
    "\x1b^payload\x1b\\pm",
    "\u009b31mc1-csi",
    "\u009dtitle\u009cc1-osc",
    "\u0090payload\u009cc1-dcs",
    "\u0000\u0008\u007f\u0085\u2028\u2029\u202e",
    "::warning::",
  ].join("");
  const result = sanitizeDiagnostic(hostile);
  assert.doesNotMatch(result, /[\p{Cc}\p{Cf}\p{Zl}\p{Zp}\x1b]/u);
  assert.equal(result.includes("::"), false);
  assert.match(result, /safe/);
});

test("direct-execution predicate is false when an ESM import has no argv entry", () => {
  assert.equal(isDirectExecution(import.meta.url, undefined), false);
  assert.equal(isDirectExecution(import.meta.url, null), false);
});

test("one execution deadline reserves cleanup time and caps every phase", () => {
  let now = 1_000;
  const control = createExecutionControl({
    timeoutMs: 100_000,
    cleanupReserveMs: CLEANUP_RESERVE_MS,
    now: () => now,
  });
  assert.equal(control.capTimeout(200_000, "work"), 85_000);
  control.requestTermination("SIGTERM");
  assert.equal(control.deadline, now + SIGNAL_SHUTDOWN_TIMEOUT_MS);
  assert.throws(() => control.capTimeout(1, "work"), /SIGTERM/);
  assert.equal(control.capTimeout(200_000, "reconcile"), 5_000);
  assert.equal(control.capTimeout(200_000, "cleanup"), 10_000);
  assert.equal(control.capTimeout(200_000, "outerReconcile"), 12_500);
  assert.equal(control.capTimeout(200_000, "outerCleanup"), 15_000);
  now += 5_001;
  assert.throws(() => control.capTimeout(1, "reconcile"), /deadline/);
  assert.equal(control.capTimeout(200_000, "cleanup"), 4_999);
  assert.equal(OUTER_RECOVERY_RESERVE_MS, 5_000);
});

test("execution control registers one exact pending claim before work", () => {
  const control = createExecutionControl({ timeoutMs: 100, cleanupReserveMs: 20 });
  const claim = {
    name: "biostack-p01-pending",
    owner: "a".repeat(48),
    cidFile: "/tmp/p01.cid",
  };
  control.registerPendingClaim(claim);
  assert.equal(control.pendingClaim, claim);
  assert.throws(() => control.registerPendingClaim(claim), /overlapping/);
  control.releasePendingClaim(claim);
  assert.equal(control.pendingClaim, undefined);
  control.requestTermination("SIGINT");
  assert.throws(() => control.registerPendingClaim(claim), /SIGINT/);
});

test("direct CLI installs bounded TERM and INT handlers and returns nonzero", async () => {
  for (const signal of ["SIGTERM", "SIGINT"]) {
    const handlers = new Map();
    const fakeProcess = {
      on(name, handler) {
        handlers.set(name, handler);
      },
      off(name, handler) {
        assert.equal(handlers.get(name), handler);
        handlers.delete(name);
      },
    };
    const control = createExecutionControl({ timeoutMs: 100, cleanupReserveMs: 20 });
    const errors = [];
    const code = await runDirectCli([], {
      control,
      processObject: fakeProcess,
      writeError: (message) => errors.push(message),
      mainFunction: async () => handlers.get(signal)(),
    });
    assert.equal(code, 1);
    assert.equal(control.terminationSignal, signal);
    assert.equal(handlers.size, 0);
    assert.match(errors.join(" "), new RegExp(signal));
  }
});

test("module imports safely from a real ESM eval context without argv[1]", () => {
  const moduleUrl = new URL("./verify-research-sidecar-container.mjs", import.meta.url).href;
  const child = spawnSync(
    process.execPath,
    ["--input-type=module", "--eval", `await import(${JSON.stringify(moduleUrl)})`],
    {
      encoding: "utf8",
      shell: false,
      timeout: 10_000,
      windowsHide: true,
    },
  );
  assert.equal(child.error, undefined);
  assert.equal(child.status, 0, child.stderr);
  assert.equal(child.stdout, "");
  assert.equal(child.stderr, "");
});

test("Dockerfile contract accepts the checked-in production Dockerfile", () => {
  assert.deepEqual(evaluateDockerfileContract(dockerfile), { ok: true, errors: [] });
});

test("build backend is hash-locked, audited, and isolated from the runtime image", () => {
  assert.match(pyproject, /^requires = \["hatchling==1\.32\.0"\]$/m);
  assert.match(dockerfile, /^FROM ghcr\.io\/astral-sh\/uv:[^\s]+@sha256:[a-f0-9]{64} AS builder$/m);
  const dockerManifest = extractBuildBackendManifest(dockerfile);
  const workflowManifest = extractBuildBackendManifest(workflow);
  assert.equal(workflowManifest, dockerManifest);
  for (const identity of [
    "hatchling==1.32.0",
    "packaging==26.3",
    "pathspec==1.1.1",
    "pluggy==1.6.0",
    "tomlkit==0.15.1",
    "trove-classifiers==2026.6.1.19",
  ]) {
    assert.equal((dockerManifest.match(new RegExp(`^${identity.replaceAll(".", "\\.")} `, "m")) ?? []).length, 1);
  }
  assert.equal((dockerManifest.match(/--hash=sha256:[a-f0-9]{64}/gu) ?? []).length, 12);
  assert.match(dockerfile, /--only-binary=:all:/);
  assert.match(dockerfile, /UV_NO_INDEX=1 .*python -m hatchling build/);
  assert.match(dockerfile, /--no-index[\s\S]+biostack_research_sidecar-0\.1\.0-py3-none-any\.whl/);
  assert.match(workflow, /Audit the hash-locked build-backend closure/);
  assert.match(workflow, /pip-audit[\s\S]+--strict[\s\S]+--no-deps[\s\S]+p01-build-backend-requirements\.txt/);
});

for (const [name, mutate, pattern] of [
  ["floating base", (value) => value.replace(/@sha256:[a-f0-9]{64}/, ""), /verified OCI|sha256/],
  ["wrong base digest", (value) => value.replace(/e5b65587[a-f0-9]+/, "f".repeat(64)), /verified OCI/],
  ["missing builder stage", (value) => value.replace(" AS builder", ""), /two-stage/],
  ["floating runtime", (value) => value.replace(/python:3\.12\.12-slim-bookworm@sha256:[a-f0-9]{64} AS runtime/, "python:3.12-slim AS runtime"), /Runtime image/],
  ["runtime ownership", (value) => value.replace("COPY --from=builder --chown=0:0 /app /app", "COPY --from=builder /app /app"), /root ownership/],
  ["runtime write protection", (value) => value.replace("chmod -R go-w /app", "true"), /non-writable/],
  ["system ensurepip removal", (value) => value.replace("/usr/local/lib/python3.12/ensurepip", "/tmp/removed"), /installer removal/],
  ["system pip removal", (value) => value.replace("/usr/local/bin/pip", "/tmp/pip"), /installer removal/],
  ["venv pip removal", (value) => value.replace("/app/.venv/bin/pip", "/tmp/venv-pip"), /installer removal/],
  ["provider-extra default", (value) => value.replace("ARG INCLUDE_TOOLUNIVERSE=false", "ARG INCLUDE_TOOLUNIVERSE=true"), /no-extra/],
  ["dependency-only no-extra", (value) => value.replace("false) uv sync --locked --no-dev --no-install-project ;;", "false) uv sync --locked --no-dev --extra tooluniverse --no-install-project ;;"), /Dependency-only no-extra/],
  ["dependency optional contract", (value) => value.replace("true) uv sync --locked --no-dev --extra tooluniverse --no-install-project ;;", "true) uv sync --locked --no-dev --no-install-project ;;"), /Dependency-only ToolUniverse/],
  ["broad build-context copy", (value) => value.replace("COPY src ./src", "COPY . ."), /copy graph/],
  ["credential-shaped build-context copy", (value) => value.replace("COPY src ./src", "COPY src ./src\nCOPY service-account.json ./"), /copy graph/],
  ["wildcard build-context copy", (value) => value.replace("COPY src ./src", "COPY *.json ./"), /copy graph/],
  ["missing source copy", (value) => value.replace("COPY src ./src", ""), /copy graph/],
  ["missing build hashes", (value) => value.replace("--require-hashes", "--no-verify-hashes"), /build-backend closure/],
  ["source build artifact", (value) => value.replace("--only-binary=:all:", "--only-binary=:none:"), /build-backend closure/],
  ["isolated project build", (value) => value.replace("UV_NO_INDEX=1", "UV_INDEX_URL=https://example.invalid/simple"), /build-backend closure/],
  ["project dependency resolution", (value) => value.replace("    --no-index \\", "    --index-url https://example.invalid/simple \\") , /build-backend closure/],
  ["retained builder tooling", (value) => value.replace("rm -rf /tmp/p01-build-backend-venv", "true #"), /build-backend closure/],
  ["unsafe empty extra", (value) => value + "\nRUN uv sync --extra ${TOOLUNIVERSE_EXTRA}\n", /Unsafe variable/],
  ["unsafe quoted empty extra", (value) => value + '\nRUN uv sync --extra "${TOOLUNIVERSE_EXTRA}"\n', /Unsafe variable/],
  ["root user", (value) => value.replace("USER biostack", "USER root"), /user/],
  ["wrong port", (value) => value.replace("EXPOSE 8080", "EXPOSE 80"), /port/],
  ["wrong command", (value) => value.replace('CMD ["python", "-m", "biostack_research_sidecar"]', 'CMD ["sh"]'), /command/],
  ["explicit entrypoint", (value) => `${value}\nENTRYPOINT ["python"]\n`, /ENTRYPOINT/],
]) {
  test(`Dockerfile mutation binds ${name}`, () => {
    assertFails(evaluateDockerfileContract(mutate(dockerfile)), pattern);
  });
}

test("dockerignore contract accepts the exact default-deny allowlist", () => {
  assert.deepEqual(evaluateDockerignoreContract(dockerignore), { ok: true, errors: [] });
});

for (const pattern of [
  "*",
  "!pyproject.toml",
  "!uv.lock",
  "!README.md",
  "!src/",
  "!src/**",
]) {
  test(`dockerignore rejects missing allowlist rule ${pattern}`, () => {
    const mutated = dockerignore
      .split(/\r?\n/)
      .filter((line) => line.trim() !== pattern)
      .join("\n");
    assertFails(evaluateDockerignoreContract(mutated), /exactly one|exact ordered/);
  });
}

test("dockerignore rejects malformed non-text input", () => {
  assertFails(evaluateDockerignoreContract(null), /not text/);
});

for (const unsafe of ["!docs", "!artifacts", "!tests", "!.env", "!nested/**", "*.md", ".git"]) {
  test(`dockerignore rejects broader pattern ${unsafe}`, () => {
    assertFails(evaluateDockerignoreContract(`${dockerignore}\n${unsafe}\n`), /pattern|re-inclusion|exact ordered/);
  });
}

test("dockerignore rejects duplicate and reordered allowlist rules", () => {
  assertFails(evaluateDockerignoreContract(`${dockerignore}\n!src/**\n`), /exactly one|exact ordered/);
  const reordered = dockerignore.split(/\r?\n/u).filter(Boolean).reverse().join("\n");
  assertFails(evaluateDockerignoreContract(reordered), /exact ordered/);
});

test("image inspect parser accepts one Config object", () => {
  assert.deepEqual(
    parseImageInspect(
      JSON.stringify([
        {
          Id: `sha256:${"a".repeat(64)}`,
          Config: {
            User: "biostack",
            Entrypoint: null,
            Cmd: ["python", "-m", "biostack_research_sidecar"],
            Env: [...REQUIRED_IMAGE_ENVIRONMENT],
            ExposedPorts: { "8080/tcp": {} },
            WorkingDir: "/app",
          },
        },
      ]),
    ),
    {
      imageId: `sha256:${"a".repeat(64)}`,
      user: "biostack",
      entrypoint: null,
      command: ["python", "-m", "biostack_research_sidecar"],
      environment: [...REQUIRED_IMAGE_ENVIRONMENT],
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

test("immutable image identity is validated before any census execution", async () => {
  const valid = `sha256:${"a".repeat(64)}`;
  assert.equal(requireImmutableImageId(valid), valid);
  let executions = 0;
  await assert.rejects(
    executeAfterImageValidation("unvalidated-local-tag:latest", async () => {
      executions += 1;
    }),
    /immutable sha256/,
  );
  assert.equal(executions, 0);
  assert.equal(
    await executeAfterImageValidation(valid, async (image) => {
      executions += 1;
      return image;
    }),
    valid,
  );
  assert.equal(executions, 1);
});

test("missing or malformed local images cause zero pull and zero execution", async () => {
  let executions = 0;
  let inspectArguments;
  await assert.rejects(
    inspectLocalImageBeforeExecution({
      image: "missing-local-image:p01",
      inspect: async (args) => {
        inspectArguments = args;
        throw new Error("No such image");
      },
      execute: async () => { executions += 1; },
    }),
    /No such image/,
  );
  assert.deepEqual(inspectArguments, ["image", "inspect", "missing-local-image:p01"]);
  assert.equal(executions, 0);

  await assert.rejects(
    inspectLocalImageBeforeExecution({
      image: "malformed-local-image:p01",
      inspect: async () => ({
        stdout: JSON.stringify([{ Id: "not-a-digest", Config: {} }]),
      }),
      execute: async () => { executions += 1; },
    }),
    /immutable sha256/,
  );
  assert.equal(executions, 0);
});

test("image configuration accepts exact non-root runtime contract", () => {
  assert.deepEqual(evaluateImageConfiguration(validImageObservation()), {
    ok: true,
    errors: [],
  });
});

for (const [name, overrides, pattern] of [
  ["missing immutable image identity", { imageId: undefined }, /immutable/],
  ["malformed immutable image identity", { imageId: "sha256:nope" }, /immutable/],
  ["empty user", { user: "" }, /non-root/],
  ["named root user", { user: "root" }, /non-root/],
  ["numeric root user", { user: "0:0" }, /non-root/],
  ["malicious entrypoint", { entrypoint: ["/bin/sh", "-c", "env"] }, /entrypoint/],
  ["malformed entrypoint", { entrypoint: "python" }, /entrypoint/],
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

test("image configuration accepts an explicitly empty entrypoint", () => {
  assert.deepEqual(evaluateImageConfiguration(validImageObservation({ entrypoint: [] })), {
    ok: true,
    errors: [],
  });
});

for (const required of REQUIRED_IMAGE_ENVIRONMENT) {
  const name = required.slice(0, required.indexOf("="));
  test(`image environment exact allowlist rejects missing ${name}`, () => {
    assertFails(
      evaluateImageConfiguration(validImageObservation({
        environment: REQUIRED_IMAGE_ENVIRONMENT.filter((entry) => !entry.startsWith(`${name}=`)),
      })),
      new RegExp(`missing variable: ${name}`),
    );
  });
}

for (const unexpected of [
  "OPENAI_API_KEY=synthetic",
  "ANTHROPIC_TOKEN=synthetic",
  "AWS_SECRET_ACCESS_KEY=synthetic",
  "ARBITRARY_EXTRA=synthetic",
]) {
  const name = unexpected.slice(0, unexpected.indexOf("="));
  test(`image environment rejects unexpected ${name}`, () => {
    const result = evaluateImageConfiguration(validImageObservation({
      environment: [...REQUIRED_IMAGE_ENVIRONMENT, unexpected],
    }));
    assertFails(result, new RegExp(`unexpected variable: ${name}`));
    if (name !== "ARBITRARY_EXTRA") {
      assert.match(result.errors.join(" "), /provider or credential-shaped/);
    }
  });
}

test("image environment rejects duplicate, malformed, and changed values", () => {
  assertFails(
    evaluateImageConfiguration(validImageObservation({
      environment: [...REQUIRED_IMAGE_ENVIRONMENT, REQUIRED_IMAGE_ENVIRONMENT[0]],
    })),
    /duplicate variable: PATH/,
  );
  assertFails(
    evaluateImageConfiguration(validImageObservation({
      environment: [...REQUIRED_IMAGE_ENVIRONMENT, "NOT-AN-ENV"],
    })),
    /malformed entry/,
  );
  assertFails(
    evaluateImageConfiguration(validImageObservation({ environment: "PATH=/tmp" })),
    /environment is malformed/,
  );
  assertFails(
    evaluateImageConfiguration(validImageObservation({
      environment: REQUIRED_IMAGE_ENVIRONMENT.map((entry) =>
        entry.startsWith("LANG=") ? "LANG=unsafe" : entry,
      ),
    })),
    /value differs for LANG/,
  );
});

test("package census accepts a provider-SDK-free environment", () => {
  assert.equal(REQUIRED_DISTRIBUTIONS.length, 24);
  assert.deepEqual(evaluatePackageCensus([...REQUIRED_DISTRIBUTIONS]), {
    ok: true,
    errors: [],
  });
});

for (const distribution of FORBIDDEN_DISTRIBUTIONS) {
  test(`package census mutation binds forbidden ${distribution}`, () => {
    assertFails(evaluatePackageCensus([...REQUIRED_DISTRIBUTIONS, distribution]), new RegExp(distribution));
  });
}

test("package census normalizes names and fails closed on nameless or invalid metadata", () => {
  assertFails(evaluatePackageCensus([...REQUIRED_DISTRIBUTIONS, "huggingface_hub"]), /huggingface-hub/);
  assert.deepEqual(
    evaluatePackageCensus(REQUIRED_DISTRIBUTIONS.map((name) => name.replaceAll("-", "_"))),
    { ok: true, errors: [] },
  );
  assertFails(evaluatePackageCensus("fastapi"), /nameless|invalid/);
  for (const invalid of [null, "", " fastapi", "fast api", "fastapi/"]) {
    assertFails(evaluatePackageCensus([...REQUIRED_DISTRIBUTIONS, invalid]), /nameless|invalid/);
  }
  for (const [duplicate, expected] of [
    ["FastAPI", "fastapi"],
    ["pydantic_settings", "pydantic-settings"],
    ["Pydantic.Settings", "pydantic-settings"],
    ["PYDANTIC---SETTINGS", "pydantic-settings"],
  ]) {
    assertFails(
      evaluatePackageCensus([...REQUIRED_DISTRIBUTIONS, duplicate]),
      new RegExp(`Duplicate canonical distribution identity.*${expected}`),
    );
  }
  assertFails(evaluatePackageCensus([]), /empty/);
});

for (const missing of REQUIRED_DISTRIBUTIONS) {
  test(`package census exact allowlist rejects missing ${missing}`, () => {
    assertFails(
      evaluatePackageCensus(REQUIRED_DISTRIBUTIONS.filter((name) => name !== missing)),
      new RegExp(`Required distribution is missing: ${missing.replaceAll("-", "\\-")}`),
    );
  });
}

for (const unexpected of ["anthropic", "boto3", "azure-ai-ml", "arbitrary-extra"]) {
  test(`package census exact allowlist rejects unexpected ${unexpected}`, () => {
    assertFails(
      evaluatePackageCensus([...REQUIRED_DISTRIBUTIONS, unexpected]),
      new RegExp(`Unexpected distribution.*${unexpected}`),
    );
  });
}

test("package census accepts reviewed distribution spelling aliases", () => {
  const aliased = REQUIRED_DISTRIBUTIONS.map((name) => ({
    pyyaml: "PyYAML",
    "pydantic-core": "pydantic_core",
    "typing-extensions": "typing_extensions",
  })[name] ?? name);
  assert.deepEqual(evaluatePackageCensus(aliased), { ok: true, errors: [] });
});

for (const [duplicate, canonical] of [
  ["PYYAML", "pyyaml"],
  ["pydantic.core", "pydantic-core"],
  ["TYPING___EXTENSIONS", "typing-extensions"],
]) {
  test(`package census rejects stale alias duplicate ${duplicate}`, () => {
    assertFails(
      evaluatePackageCensus([...REQUIRED_DISTRIBUTIONS, duplicate]),
      new RegExp(`Duplicate canonical distribution identity.*${canonical}`),
    );
  });
}

test("filesystem census accepts an empty forbidden-path inventory", () => {
  assert.deepEqual(
    evaluateFilesystemCensus(validFilesystemObservation()),
    { ok: true, errors: [] },
  );
});

for (const key of ["env_paths", "tests_paths", "git_paths", "credential_paths", "installer_paths"]) {
  test(`filesystem census mutation binds present ${key}`, () => {
    const value = validFilesystemObservation({ [key]: ["/app/bad"] });
    assertFails(evaluateFilesystemCensus(value), /forbidden/);
  });
  test(`filesystem census mutation binds missing ${key}`, () => {
    const value = validFilesystemObservation();
    delete value[key];
    assertFails(evaluateFilesystemCensus(value), new RegExp(key));
  });
}

for (const [key, value] of [["app_owner_uid", 999], ["venv_owner_uid", 999], ["app_writable", true], ["venv_writable", true]]) {
  test(`filesystem census binds unsafe ${key}`, () => {
    const observation = validFilesystemObservation({ [key]: value });
    assertFails(evaluateFilesystemCensus(observation), new RegExp(key));
  });
}

for (const [name, field, value] of [
  ["system", "pip_module", true],
  ["system", "ensurepip_module", true],
  ["system", "pip_path", "/usr/local/bin/pip"],
  ["system", "pip3_path", "/usr/local/bin/pip3"],
  ["system", "uv_path", "/usr/local/bin/uv"],
  ["venv", "pip_module", true],
  ["venv", "ensurepip_module", true],
  ["venv", "pip_path", "/app/.venv/bin/pip"],
  ["venv", "uvx_path", "/app/.venv/bin/uvx"],
]) {
  test(`filesystem census binds ${name} ${field}`, () => {
    const observation = validFilesystemObservation();
    observation.python_probes.find((probe) => probe.name === name)[field] = value;
    assertFails(evaluateFilesystemCensus(observation), new RegExp(field));
  });
}

test("filesystem census requires exactly one system and one venv probe", () => {
  assertFails(evaluateFilesystemCensus(validFilesystemObservation({ python_probes: [] })), /two Python/);
  const duplicate = validFilesystemObservation();
  duplicate.python_probes[1].name = "system";
  assertFails(evaluateFilesystemCensus(duplicate), /identity/);
});

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
  ["extra workflow absence", (value) => ({ ...value, correctBody: { ...value.correctBody, allowed_workflows: [...ALLOWED_WORKFLOWS, "execute_any_tool"] } }), /exactly/],
  ["duplicate workflow absence", (value) => ({ ...value, correctBody: { ...value.correctBody, allowed_workflows: [...ALLOWED_WORKFLOWS, ALLOWED_WORKFLOWS[0]] } }), /exactly/],
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
    cidFile: "/private/p01.cid",
    owner: "owner-marker",
  });
  assert.deepEqual(args.slice(0, 11), [
    "create",
    "--pull=never",
    "--name",
    "p01-safe-name",
    "--cidfile",
    "/private/p01.cid",
    "--label",
    `${OWNERSHIP_LABEL_KEY}=owner-marker`,
    "--publish",
    "127.0.0.1:18080:8080",
    "--env",
  ]);
  assert.equal(args.includes(`BIOSTACK_RESEARCH_SERVICE_TOKEN=${token}`), true);
  assert.equal(args.includes("BIOSTACK_RESEARCH_LOG_LEVEL=warning"), true);
  assert.equal(args.at(-1), "local/image:test");
});

test("ephemeral port binding and cleanup stay confined to one exact name", () => {
  const args = buildContainerCreateArguments({
    image: "local:test",
    name: "p01-exact",
    hostPort: undefined,
    token: "p01-local-only-token",
    cidFile: "/private/p01.cid",
    owner: "owner-marker",
  });
  assert.equal(args[9], "127.0.0.1::8080");
});

test("static census create is named, labeled, cidfile-owned, and never pulls", () => {
  const args = buildCensusCreateArguments({
    image: `sha256:${"a".repeat(64)}`,
    name: "biostack-p01-census-exact",
    cidFile: "/private/census.cid",
    owner: "census-owner",
    entrypoint: "/app/.venv/bin/python",
    command: ["-c", "print('{}')"],
  });
  assert.deepEqual(args.slice(0, 10), [
    "create",
    "--pull=never",
    "--name",
    "biostack-p01-census-exact",
    "--cidfile",
    "/private/census.cid",
    "--label",
    `${OWNERSHIP_LABEL_KEY}=census-owner`,
    "--entrypoint",
    "/app/.venv/bin/python",
  ]);
  assert.equal(args.at(-3), `sha256:${"a".repeat(64)}`);
});

test("cleanup ownership is established only from one full Docker container ID", () => {
  const id = "a".repeat(64);
  assert.equal(parseOwnedContainerId(`${id}\n`), id);
  for (const hostile of ["", "abc", `${id}\n${"b".repeat(64)}`, "g".repeat(64), null]) {
    assert.throws(() => parseOwnedContainerId(hostile), /full container ID/);
  }
  assert.deepEqual(buildContainerCleanupArguments(id), ["rm", "--force", id]);
  assert.deepEqual(buildOwnershipLookupArguments("p01-exact", "owner-exact"), [
    "container",
    "ls",
    "--all",
    "--no-trunc",
    "--filter",
    "name=^/p01-exact$",
    "--filter",
    `label=${OWNERSHIP_LABEL_KEY}=owner-exact`,
    "--format",
    "{{.ID}}",
  ]);
  assert.deepEqual(buildOwnershipInspectArguments(id), [
    "container",
    "inspect",
    "--format",
    `{{index .Config.Labels "${OWNERSHIP_LABEL_KEY}"}}`,
    id,
  ]);
});

test("ownership reconciliation accepts one full ID with the expected label", async () => {
  const id = "1".repeat(64);
  assert.equal(
    await reconcileOwnedContainer({
      createOutput: id,
      readCidFile: async () => id,
      findExactNameAndLabel: async () => id,
      hasExpectedLabel: async (candidate) => candidate === id,
      delay: async () => {},
      attempts: 2,
    }),
    id,
  );
});

test("ownership reconciliation waits for delayed cidfile and label proof", async () => {
  const id = "2".repeat(64);
  let reads = 0;
  let delays = 0;
  const result = await reconcileOwnedContainer({
    createOutput: "malformed",
    readCidFile: async () => (++reads < 3 ? "" : id),
    findExactNameAndLabel: async () => "",
    hasExpectedLabel: async (candidate) => candidate === id,
    delay: async () => { delays += 1; },
    attempts: 4,
    delayMs: 1,
  });
  assert.equal(result, id);
  assert.equal(delays, 2);
});

test("ownership reconciliation withholds collision and no-proof cleanup", async () => {
  const unrelated = "3".repeat(64);
  const result = await reconcileOwnedContainer({
    createOutput: unrelated,
    readCidFile: async () => "",
    findExactNameAndLabel: async () => "",
    hasExpectedLabel: async () => false,
    delay: async () => {},
    attempts: 2,
  });
  assert.equal(result, undefined);
});

test("ownership reconciliation fails closed on disagreeing full IDs", async () => {
  const stdoutId = "4".repeat(64);
  const cidId = "5".repeat(64);
  let error;
  try {
    await reconcileOwnedContainer({
      createOutput: stdoutId,
      readCidFile: async () => cidId,
      findExactNameAndLabel: async () => cidId,
      hasExpectedLabel: async (candidate) => candidate === cidId,
      delay: async () => {},
      attempts: 1,
    });
  } catch (caught) {
    error = caught;
  }
  assert.match(error?.message ?? "", /proofs disagreed/);
  assert.equal(error.cleanupContainerId, cidId);
});

test("ownership disagreement retains any one unique expected-label ID for cleanup only", async () => {
  const stdoutId = "4".repeat(64);
  const ownedId = "5".repeat(64);
  for (const [cidOutput, nameOutput] of [[ownedId, ""], ["", ownedId]]) {
    let error;
    try {
      await reconcileOwnedContainer({
        createOutput: stdoutId,
        readCidFile: async () => cidOutput,
        findExactNameAndLabel: async () => nameOutput,
        hasExpectedLabel: async (candidate) => candidate === ownedId,
        attempts: 1,
      });
    } catch (caught) {
      error = caught;
    }
    assert.match(error?.message ?? "", /proofs disagreed/);
    assert.equal(error.cleanupContainerId, ownedId);
  }
});

test("ownership disagreement with multiple expected-label IDs withholds cleanup", async () => {
  const stdoutId = "4".repeat(64);
  const otherId = "5".repeat(64);
  let error;
  try {
    await reconcileOwnedContainer({
      createOutput: stdoutId,
      readCidFile: async () => otherId,
      findExactNameAndLabel: async () => otherId,
      hasExpectedLabel: async () => true,
      attempts: 1,
    });
  } catch (caught) {
    error = caught;
  }
  assert.match(error?.message ?? "", /proofs disagreed/);
  assert.equal(error.cleanupContainerId, undefined);
});

test("forced timeout reconciliation cleans the delayed labeled container", async () => {
  const id = "6".repeat(64);
  const labeledContainers = new Map();
  let attempts = 0;
  const events = [];
  await assert.rejects(
    executeOwnedContainerLifecycle({
      sensitive: ["random-owner-label"],
      create: async () => {
        events.push("create-timeout");
        throw new Error("forced subprocess timeout");
      },
      resolveOwnership: (createOutput) =>
        reconcileOwnedContainer({
          createOutput,
          readCidFile: async () => {
            attempts += 1;
            if (attempts === 2) labeledContainers.set(id, "random-owner-label");
            return attempts >= 2 ? id : "";
          },
          findExactNameAndLabel: async () => (labeledContainers.has(id) ? id : ""),
          hasExpectedLabel: async (candidate) =>
            labeledContainers.get(candidate) === "random-owner-label",
          delay: async () => {},
          attempts: 3,
        }),
      start: async () => events.push("unexpected-start"),
      operate: async () => events.push("unexpected-operate"),
      cleanup: async (candidate) => {
        assert.equal(labeledContainers.get(candidate), "random-owner-label");
        labeledContainers.delete(candidate);
        events.push("cleanup");
      },
    }),
    /forced subprocess timeout/,
  );
  assert.equal(labeledContainers.size, 0);
  assert.deepEqual(events, ["create-timeout", "cleanup"]);
});

function lifecycleHarness({ createResult, createError, ownership = "", operationError, cleanupError }) {
  const events = [];
  return {
    events,
    lifecycle: {
      sensitive: ["p01-secret-marker"],
      create: async () => {
        events.push("create");
        if (createError) throw createError;
        return createResult;
      },
      resolveOwnership: async () => {
        events.push("ownership");
        return ownership;
      },
      start: async (id) => events.push(`start:${id}`),
      operate: async (id) => {
        events.push(`operate:${id}`);
        if (operationError) throw operationError;
        return "green";
      },
      cleanup: async (id) => {
        events.push(`cleanup:${id}`);
        if (cleanupError) throw cleanupError;
      },
    },
  };
}

test("owned lifecycle starts, operates, and cleans only the proven container ID", async () => {
  const id = "a".repeat(64);
  const harness = lifecycleHarness({ createResult: { stdout: `${id}\n` }, ownership: id });
  assert.equal(await executeOwnedContainerLifecycle(harness.lifecycle), "green");
  assert.deepEqual(harness.events, ["create", "ownership", `start:${id}`, `operate:${id}`, `cleanup:${id}`]);
});

test("owned lifecycle cleans cidfile proof after malformed create output", async () => {
  const id = "b".repeat(64);
  const harness = lifecycleHarness({ createResult: { stdout: "malformed" }, ownership: id });
  await assert.rejects(executeOwnedContainerLifecycle(harness.lifecycle), /full container ID/);
  assert.deepEqual(harness.events, ["create", "ownership", `cleanup:${id}`]);
});

test("owned lifecycle cleans cidfile proof after create timeout", async () => {
  const id = "c".repeat(64);
  const harness = lifecycleHarness({
    createError: new Error("timeout p01-secret-marker"),
    ownership: id,
  });
  await assert.rejects(executeOwnedContainerLifecycle(harness.lifecycle), /\[REDACTED\]/);
  assert.deepEqual(harness.events, ["create", "ownership", `cleanup:${id}`]);
});

test("owned lifecycle preserves primary and cleanup failures", async () => {
  const id = "d".repeat(64);
  const harness = lifecycleHarness({
    createResult: { stdout: id },
    ownership: id,
    operationError: new Error("primary failure"),
    cleanupError: new Error("cleanup failure"),
  });
  await assert.rejects(
    executeOwnedContainerLifecycle(harness.lifecycle),
    /primary failure.*Cleanup also failed: cleanup failure/,
  );
});

test("cleanup failure retains the pending claim until bounded outer recovery succeeds", async () => {
  const id = "d".repeat(64);
  const owner = "b".repeat(48);
  const claim = { name: "biostack-p01-recovery", owner, cidFile: "/tmp/recovery.cid" };
  const control = createExecutionControl({ timeoutMs: 10_000, cleanupReserveMs: 3_000 });
  await assert.rejects(
    executeOwnedContainerLifecycle({
      control,
      claim,
      create: async () => ({ stdout: id }),
      resolveOwnership: async () => id,
      start: async () => {},
      operate: async () => { throw new Error("primary lifecycle failure"); },
      cleanup: async () => { throw new Error("transient lifecycle cleanup failure"); },
    }),
    /primary lifecycle failure.*Cleanup also failed.*transient lifecycle cleanup failure/,
  );
  assert.equal(control.pendingClaim, claim);
  assert.equal(claim.ownedContainerId, id);
  let recovered;
  assert.deepEqual(
    await recoverPendingClaim(control, {
      resolveOwnership: async () => { throw new Error("known ID should bypass reconciliation"); },
      cleanup: async ({ id: candidate }) => { recovered = candidate; },
    }),
    { status: "cleaned" },
  );
  assert.equal(recovered, id);
  assert.equal(control.pendingClaim, undefined);
});

test("initial no-proof reconciliation retains a claim until outer verified absence", async () => {
  const claim = {
    name: "biostack-p01-absent",
    owner: "c".repeat(48),
    cidFile: "/tmp/absent.cid",
  };
  const control = createExecutionControl({ timeoutMs: 10_000, cleanupReserveMs: 3_000 });
  let outerAttempts = 0;
  const code = await runDirectCli([], {
    control,
    writeError: () => {},
    mainFunction: async () => executeOwnedContainerLifecycle({
      control,
      claim,
      create: async () => { throw new Error("create failed"); },
      resolveOwnership: async () => undefined,
      start: async () => { throw new Error("unexpected start"); },
      operate: async () => { throw new Error("unexpected operation"); },
      cleanup: async () => { throw new Error("unexpected cleanup"); },
    }),
    recoverFunction: (activeControl) => recoverPendingClaim(activeControl, {
      resolveOwnership: async () => {
        outerAttempts += 1;
        return undefined;
      },
      cleanup: async () => { throw new Error("unexpected outer cleanup"); },
    }),
  });
  assert.equal(code, 1);
  assert.equal(outerAttempts, 1);
  assert.equal(control.pendingClaim, undefined);
});

test("container appearing only during outer recovery is label-proven and cleaned", async () => {
  const id = "f".repeat(64);
  const owner = "e".repeat(48);
  const claim = {
    name: "biostack-p01-outer-late-create",
    owner,
    cidFile: "/tmp/outer-late-create.cid",
    sensitive: [owner],
  };
  const containers = new Map();
  const control = createExecutionControl({ timeoutMs: 10_000, cleanupReserveMs: 3_000 });
  const code = await runDirectCli([], {
    control,
    writeError: () => {},
    mainFunction: async () => executeOwnedContainerLifecycle({
      control,
      claim,
      create: async () => { throw new Error("create timed out before receipt"); },
      resolveOwnership: async () => undefined,
      start: async () => { throw new Error("unexpected start"); },
      operate: async () => { throw new Error("unexpected operation"); },
      cleanup: async () => { throw new Error("unexpected lifecycle cleanup"); },
    }),
    recoverFunction: async (activeControl) => {
      containers.set(id, owner);
      return recoverPendingClaim(activeControl, {
        resolveOwnership: async ({ claim: pending }) =>
          containers.get(id) === pending.owner ? id : undefined,
        cleanup: async ({ id: candidate, claim: pending }) => {
          assert.equal(containers.get(candidate), pending.owner);
          containers.delete(candidate);
        },
      });
    },
  });
  assert.equal(code, 1);
  assert.equal(containers.size, 0);
  assert.equal(control.pendingClaim, undefined);
});

test("direct CLI preserves lifecycle and outer cleanup diagnostics when recovery fails", async () => {
  const id = "e".repeat(64);
  const owner = "d".repeat(48);
  const claim = {
    name: "biostack-p01-double-cleanup-failure",
    owner,
    cidFile: "/tmp/double-cleanup.cid",
  };
  const control = createExecutionControl({ timeoutMs: 10_000, cleanupReserveMs: 3_000 });
  const diagnostics = [];
  let recoveryAttempts = 0;
  const code = await runDirectCli([], {
    control,
    writeError: (message) => diagnostics.push(message),
    mainFunction: async () =>
      executeOwnedContainerLifecycle({
        control,
        claim,
        create: async () => ({ stdout: id }),
        resolveOwnership: async () => id,
        start: async () => {},
        operate: async () => { throw new Error("primary-operation-diagnostic"); },
        cleanup: async () => { throw new Error("lifecycle-cleanup-diagnostic"); },
      }),
    recoverFunction: async () => {
      recoveryAttempts += 1;
      throw new Error("outer-cleanup-diagnostic");
    },
  });
  assert.equal(code, 1);
  assert.equal(recoveryAttempts, 1);
  assert.equal(control.pendingClaim, claim);
  assert.match(diagnostics.join(" "), /primary-operation-diagnostic/);
  assert.match(diagnostics.join(" "), /lifecycle-cleanup-diagnostic/);
  assert.match(diagnostics.join(" "), /outer-cleanup-diagnostic/);
});

test("owned lifecycle withholds cleanup without unambiguous ownership", async () => {
  const noProof = lifecycleHarness({ createError: new Error("timeout"), ownership: "" });
  await assert.rejects(executeOwnedContainerLifecycle(noProof.lifecycle), /timeout/);
  assert.deepEqual(noProof.events, ["create", "ownership"]);

  const mismatch = lifecycleHarness({ createResult: { stdout: "e".repeat(64) } });
  mismatch.lifecycle.resolveOwnership = async () => {
    mismatch.events.push("ownership");
    throw new Error("proofs disagreed");
  };
  await assert.rejects(executeOwnedContainerLifecycle(mismatch.lifecycle), /proofs disagreed/);
  assert.deepEqual(mismatch.events, ["create", "ownership"]);
});

test("owned lifecycle preserves disagreement but cleans unique cid/name/label proof", async () => {
  const stdoutId = "7".repeat(64);
  const ownedId = "8".repeat(64);
  const events = [];
  await assert.rejects(
    executeOwnedContainerLifecycle({
      create: async () => ({ stdout: stdoutId }),
      resolveOwnership: (createOutput) =>
        reconcileOwnedContainer({
          createOutput,
          readCidFile: async () => ownedId,
          findExactNameAndLabel: async () => ownedId,
          hasExpectedLabel: async (candidate) => candidate === ownedId,
          attempts: 1,
        }),
      start: async () => events.push("unexpected-start"),
      operate: async () => events.push("unexpected-operate"),
      cleanup: async (candidate) => events.push(`cleanup:${candidate}`),
    }),
    /proofs disagreed/,
  );
  assert.deepEqual(events, [`cleanup:${ownedId}`]);
});

async function runChildSignalPhase(phase, context) {
  const moduleUrl = new URL("./verify-research-sidecar-container.mjs", import.meta.url).href;
  const childSource = `
    import {
      createExecutionControl,
      executeOwnedContainerLifecycle,
      runDirectCli,
    } from ${JSON.stringify(moduleUrl)};
    const id = "9".repeat(64);
    const owner = "a".repeat(48);
    const phase = ${JSON.stringify(phase)};
    const containers = new Map();
    const claim = { name: "biostack-p01-term-child", owner, cidFile: "/tmp/p01-term.cid" };
    const control = createExecutionControl({ timeoutMs: 10_000, cleanupReserveMs: 2_000 });
    if (process.platform === "win32") {
      process.stdin.once("data", () => process.emit("SIGTERM"));
    }
    const waitForSignal = (rejectOnSignal = true) => {
      process.stdout.write("PHASE=" + phase + "\\n");
      return new Promise((resolve, reject) => {
        const keepalive = setInterval(() => {}, 1_000);
        const finish = () => {
          clearInterval(keepalive);
          if (rejectOnSignal) reject(new Error("signal interrupted " + phase));
          else resolve();
        };
        if (control.abortSignal.aborted) finish();
        else control.abortSignal.addEventListener("abort", finish, { once: true });
      });
    };
    const mainFunction = async (_argv, { control: activeControl }) => {
      if (phase === "before-create") await waitForSignal();
      return executeOwnedContainerLifecycle({
        control: activeControl,
        claim,
        sensitive: [owner],
        create: async () => {
          containers.set(id, owner);
          if (phase === "create") await waitForSignal();
          return { stdout: id };
        },
        resolveOwnership: async () => {
          if (phase === "reconcile") await waitForSignal(false);
          return id;
        },
        start: async () => {
          if (phase === "start") await waitForSignal();
        },
        operate: async () => {
          if (phase === "operate") await waitForSignal();
        },
        cleanup: async (candidate) => {
          if (phase === "cleanup" || phase === "cleanup-error") {
            await waitForSignal(false);
          }
          if (phase === "cleanup-error") throw new Error("first cleanup failed");
          if (containers.get(candidate) !== owner) throw new Error("ownership changed");
          containers.delete(candidate);
        },
      });
    };
    const exitCode = await runDirectCli([], {
      control,
      mainFunction,
      writeError: (message) => process.stderr.write(message + "\\n"),
      recoverFunction: async (activeControl) => {
        const pending = activeControl.pendingClaim;
        if (!pending) return;
        if (pending.ownedContainerId) containers.delete(pending.ownedContainerId);
        activeControl.releasePendingClaim(pending);
      },
    });
    process.stdout.write("LEFT=" + containers.size + "\\n");
    process.stdout.write("PENDING=" + (control.pendingClaim ? 1 : 0) + "\\n");
    process.exitCode = exitCode;
  `;
  const child = spawn(
    process.execPath,
    ["--input-type=module", "--eval", childSource],
    { encoding: "utf8", shell: false, stdio: ["pipe", "pipe", "pipe"], windowsHide: true },
  );
  context.after(() => {
    if (child.exitCode === null && child.signalCode === null) child.kill("SIGKILL");
  });
  let stdout = "";
  let stderr = "";
  child.stdout.setEncoding("utf8");
  child.stderr.setEncoding("utf8");
  child.stderr.on("data", (chunk) => { stderr += chunk; });
  const created = new Promise((resolveCreated) => {
    child.stdout.on("data", (chunk) => {
      stdout += chunk;
      if (stdout.includes(`PHASE=${phase}\n`)) resolveCreated();
    });
  });
  await Promise.race([
    created,
    new Promise((_resolve, reject) => {
      const timer = setTimeout(
        () => reject(new Error(`child ${phase} marker timed out`)),
        5_000,
      );
      timer.unref();
    }),
  ]);
  if (process.platform === "win32") child.stdin.end("TERM\n");
  else {
    assert.equal(child.kill("SIGTERM"), true);
    child.stdin.end();
  }
  const [exitCode, exitSignal] = await once(child, "exit");
  assert.equal(exitSignal, null);
  assert.equal(exitCode, 1);
  assert.match(stderr, /SIGTERM|signal interrupted|first cleanup failed/);
  assert.match(stdout, /LEFT=0/);
  assert.match(stdout, /PENDING=0/);
}

for (const phase of [
  "before-create",
  "create",
  "reconcile",
  "start",
  "operate",
  "cleanup",
  "cleanup-error",
]) {
  test(`direct child TERM during ${phase} exits exactly 1 with no pending claim`, { timeout: 15_000 }, async (context) => {
    await runChildSignalPhase(phase, context);
  });
}

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
  assert.doesNotMatch(source, /["']run["']\s*,\s*["']--rm["']/u);
  assert.equal((source.match(/--pull=never/g) ?? []).length, 2);
  assert.equal((source.match(/await runOwnedCensus\(/g) ?? []).length, 3);
  assert.match(source, /d\.metadata\.get\('Name'\) for d in m\.distributions\(\)/);
  assert.doesNotMatch(source, /m\.distributions\(\) if d\.metadata/u);
  assert.doesNotMatch(source, /subject_name:\s*["']P01Compound["']/);
  assert.match(source, /resolvePublishedPort\(ownedContainerId, sensitive, control\)/);
  assert.match(source, /inspectDarkEnvironment\(ownedContainerId, sensitive, control\)/);
  assert.match(source, /sensitive\.push\(jobId\)/);
  assert.match(source, /registerPendingClaim\(claim\)/);
  assert.match(source, /requestTermination\(signal\)/);
  assert.match(source, /control \? control\.capTimeout\(timeoutMs, phase\)/);
  assert.match(source, /phase === "work" \? control\?\.abortSignal/);
  for (const field of ["research_request_id", "subject_name", "correlation_id"]) {
    assert.equal((source.match(new RegExp(`${field}:`, "g")) ?? []).length, 3);
  }
});

test("parcel documentation records exact P02 and P03 log-level custody", () => {
  const parcels = readFileSync(
    new URL("../backend/research-sidecar/docs/PARCELS.md", import.meta.url),
    "utf8",
  );
  assert.match(parcels, /P02 must set\s+`BIOSTACK_RESEARCH_LOG_LEVEL=warning`/);
  assert.match(parcels, /static assertion for that exact name\/value/);
  assert.match(parcels, /P03 must verify the effective deployed\s+revision reports `log_level: warning`/);
  assert.match(parcels, /commit, local-image, pushed-digest, and effective-revision custody/);
  assert.match(parcels, /node:22@sha256:c601a46abb4d2ab80a9dc3da208d50d1122642d53f17a101926ace71e5a9bf1c/);
  assert.match(parcels, /282 verifier\s+mutation tests passed/);
  assert.match(parcels, /--tmpfs \/tmp:rw,exec,nosuid,size=64m/);
  for (const hardening of [
    "--pull=never",
    "--network none",
    "--read-only",
    "--cap-drop ALL",
    "--security-opt no-new-privileges",
  ]) {
    assert.match(parcels, new RegExp(hardening.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")));
  }
  assert.match(parcels, /hard-late residual/);
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
  assert.match(workflow, /runs-on: ubuntu-24\.04/);
  assert.match(workflow, /actions\/checkout@11d5960a326750d5838078e36cf38b85af677262/);
  assert.match(workflow, /actions\/setup-python@a26af69be951a213d495a4c3e4e4022e16d87065/);
  assert.match(workflow, /actions\/setup-node@49933ea5288caeca8642d1e84afbd3f7d6820020/);
  assert.doesNotMatch(workflow, /uses:\s*[^\s]+@v\d+/);
  const actionIdentities = [...workflow.matchAll(/^\s*uses:\s*\S+@([a-f0-9]+)(?:\s|$)/gmu)];
  assert.equal(actionIdentities.length, 3);
  assert.equal(actionIdentities.every((match) => match[1].length === 40), true);
  assert.match(workflow, /python-version: "3\.12\.12"/);
  assert.match(workflow, /node-version: "22\.23\.1"/);
  assert.match(workflow, /uv==0\.9\.7/);
  assert.match(workflow, /pip-audit==2\.9\.0/);
  assert.match(workflow, /--require-hashes/);
  assert.match(workflow, /uv==0\.9\.7 --hash=sha256:8cf6bc2482d1293cc630f66b862b494c09acda9b7faff7307ef52667a2b3ad49/);
  assert.match(workflow, /pip-audit==2\.9\.0 --hash=sha256:348b16e60895749a0839875d7cc27ebd692e1584ebe5d5cb145941c8e25a80bd/);
  const bootstrap = /requirements = """\\\r?\n(?<rows>.*?)\r?\n\s+"""/su.exec(workflow);
  assert.notEqual(bootstrap, null);
  const bootstrapRows = bootstrap.groups.rows
    .split(/\r?\n/u)
    .map((row) => row.trim())
    .filter(Boolean);
  assert.equal(bootstrapRows.length, 28);
  for (const row of bootstrapRows) {
    assert.match(row, /^[a-z0-9][a-z0-9-]*==[^\s]+ --hash=sha256:[a-f0-9]{64}$/u);
  }
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
  assert.match(workflow, /Audit the hash-locked build-backend closure/);
  assert.match(
    workflow,
    /--no-deps[\s\\]+\n\s+--requirement "\$\{RUNNER_TEMP\}\/p01-build-backend-requirements\.txt"/,
  );
  assert.match(workflow, /node --test scripts\/verify-research-sidecar-container\.test\.mjs/);
  assert.match(workflow, /GITLEAKS_VERSION: "8\.24\.3"/);
  assert.match(workflow, /9991e0b2903da4c8f6122b5c3186448b927a5da4deef1fe45271c3793f4ee29c/);
  assert.match(workflow, /sha256sum --check --strict/);
  assert.match(workflow, /gitleaks" dir/);
  assert.match(workflow, /docker build/);
  assert.match(workflow, /--tag biostack-research-sidecar:p01-ci/);
  assert.match(workflow, /p01-untracked-credential\.json/);
  assert.match(workflow, /FROM scratch/);
  assert.match(workflow, /COPY p01-untracked-credential\.json \/p01-untracked-credential\.json/);
  assert.match(workflow, /context_negative_status/);
  assert.match(workflow, /--pull=false/);
  assert.match(workflow, /--network=none/);
  assert.match(workflow, /test ! -e \/app\/p01-untracked-credential\.json/);
  assert.match(workflow, /Prove catchable TERM cleans the verifier-owned container/);
  assert.match(workflow, /randomBytes\(16\)\.toString\("hex"\)/);
  assert.match(workflow, /fixture_name="biostack-p01-ci-term-\$\{invocation_nonce\}"/);
  assert.doesNotMatch(workflow, /fixture_name="biostack-p01-ci-term-\$\{GITHUB_RUN_ID\}/);
  assert.match(workflow, /preflight-name/);
  assert.match(workflow, /preflight-nonce/);
  assert.match(workflow, /name=\$\{invocation_nonce\}/);
  assert.match(workflow, /query_container_ids/);
  assert.match(workflow, /timeout 10s docker container ls/);
  assert.match(workflow, /final-name/);
  assert.match(workflow, /final-label/);
  assert.match(workflow, /final absence proof was uncertain/);
  assert.match(workflow, /kill -TERM "\$\{verifier_pid\}"/);
  assert.match(workflow, /timeout 20s tail --pid="\$\{verifier_pid\}" -f \/dev\/null/);
  assert.match(workflow, /io\.biostack\.p01\.owner/);
  assert.match(workflow, /--container-name "\$\{fixture_name\}"/);
  assert.match(workflow, /current_owner[^\n]+owner_marker/);
  assert.doesNotMatch(workflow, /docker (?:container )?prune/);
  assert.doesNotMatch(workflow, /docker container rm --force "\$\{fixture_name\}"/);
  assert.match(workflow, /term_status[^\n]+-ne 1/);
  assert.match(workflow, /node scripts\/verify-research-sidecar-container\.mjs/);
  assert.match(workflow, /--image biostack-research-sidecar:p01-ci/);
  assert.match(workflow, /timeout --signal=TERM --kill-after=20s 3m/);
  assert.equal(
    workflow.indexOf("Prove catchable TERM cleans the verifier-owned container") <
      workflow.indexOf("Verify the local dark container contract"),
    true,
  );
  assert.equal(DIRECT_CLI_TIMEOUT_MS + CLEANUP_RESERVE_MS < 180_000, true);
  assert.equal(SIGNAL_SHUTDOWN_TIMEOUT_MS < 20_000, true);
});

test("literal TERM Bash rejects a Docker preflight query failure before starting or cleaning", () => {
  const result = runLiteralTermFixture({ scenario: "query-failure", nonce: "1".repeat(32) });
  assert.equal(result.error, undefined);
  assert.notEqual(result.status, 0);
  assert.match(result.stderr, /Docker query failed during preflight-name/);
  assert.equal(result.nodeStarted, "");
  assert.equal(result.removed, "");
});

test("literal TERM Bash rejects a pre-existing valid-ID collision without adoption or removal", () => {
  const result = runLiteralTermFixture({ scenario: "collision", nonce: "2".repeat(32) });
  assert.equal(result.error, undefined);
  assert.notEqual(result.status, 0);
  assert.match(result.stderr, /invocation identity already exists; refusing adoption/);
  assert.equal(result.nodeStarted, "");
  assert.equal(result.removed, "");
});

test("literal TERM Bash uses unpredictable invocation identity and verifies both final absences", () => {
  const firstNonce = "3".repeat(32);
  const secondNonce = "4".repeat(32);
  const first = runLiteralTermFixture({ scenario: "success", nonce: firstNonce });
  const second = runLiteralTermFixture({ scenario: "success", nonce: secondNonce });
  for (const [result, nonce] of [[first, firstNonce], [second, secondNonce]]) {
    assert.equal(result.error, undefined);
    assert.equal(result.status, 0, result.stderr);
    assert.equal(result.fixtureName, `biostack-p01-ci-term-${nonce}`);
    assert.match(result.commands, new RegExp(`name=${nonce}`));
    assert.match(result.commands, /label=io\.biostack\.p01\.owner=/);
    assert.equal(result.removed, `removed=${result.fullId}\n`);
  }
  assert.notEqual(first.fixtureName, second.fixtureName);
});

test("literal TERM Bash trap cleanup targets only its attributed exact full ID", () => {
  const result = runLiteralTermFixture({ scenario: "trap-cleanup", nonce: "5".repeat(32) });
  assert.equal(result.error, undefined);
  assert.notEqual(result.status, 0);
  assert.match(result.stderr, /left an owned container/);
  assert.equal(result.removed, `removed=${result.fullId}\n`);
  const removals = result.commands
    .split(/\r?\n/u)
    .filter((line) => line.includes("container\trm\t--force"));
  assert.equal(removals.length, 1);
  assert.equal(removals[0], `docker\tcontainer\trm\t--force\t${result.fullId}`);
});

for (const [scenario, phase] of [
  ["final-name-failure", "final-name"],
  ["final-label-failure", "final-label"],
]) {
  test(`literal TERM Bash fails closed when ${phase} query errors`, () => {
    const result = runLiteralTermFixture({ scenario, nonce: "6".repeat(32) });
    assert.equal(result.error, undefined);
    assert.notEqual(result.status, 0);
    assert.match(result.stderr, new RegExp(`Docker query failed during ${phase}`));
    assert.match(result.stderr, /final absence proof was uncertain/);
    assert.equal(result.removed, `removed=${result.fullId}\n`);
  });
}

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
