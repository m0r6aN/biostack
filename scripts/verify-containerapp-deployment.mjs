import { execFileSync } from "node:child_process";
import { existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { pathToFileURL } from "node:url";

const TERMINAL_RUNNING_STATES = new Set([
  "ActivationFailed",
  "DeactivationFailed",
  "Failed",
]);

export function evaluateRevision(snapshot, expectedImage) {
  const required = [
    "latestRevisionName",
    "latestReadyRevisionName",
    "image",
    "healthState",
    "provisioningState",
    "runningState",
  ];
  for (const key of required) {
    if (typeof snapshot?.[key] !== "string" || snapshot[key].length === 0) {
      return { status: "failed", reason: `Azure response is missing ${key}.` };
    }
  }

  if (snapshot.image !== expectedImage) {
    return {
      status: "pending",
      reason: `Latest template image is ${snapshot.image}; waiting for ${expectedImage}.`,
    };
  }

  if (
    snapshot.provisioningState === "Failed" ||
    TERMINAL_RUNNING_STATES.has(snapshot.runningState)
  ) {
    return {
      status: "failed",
      reason: `Revision ${snapshot.latestRevisionName} entered ${snapshot.provisioningState}/${snapshot.runningState}.`,
    };
  }

  const isReady =
    snapshot.latestReadyRevisionName === snapshot.latestRevisionName &&
    snapshot.healthState === "Healthy" &&
    snapshot.provisioningState === "Provisioned";

  return isReady
    ? { status: "ready", reason: `Revision ${snapshot.latestRevisionName} is ready.` }
    : {
        status: "pending",
        reason: `Revision ${snapshot.latestRevisionName} is ${snapshot.healthState}/${snapshot.provisioningState}/${snapshot.runningState}; latest ready is ${snapshot.latestReadyRevisionName}.`,
      };
}

function parseArgs(argv) {
  const values = new Map();
  for (let index = 0; index < argv.length; index += 2) {
    const key = argv[index];
    const value = argv[index + 1];
    if (!key?.startsWith("--") || !value) {
      throw new Error(`Invalid argument near ${key ?? "end of command"}.`);
    }
    values.set(key.slice(2), value);
  }

  for (const key of ["name", "resource-group", "expected-image", "health-path"]) {
    if (!values.has(key)) throw new Error(`Missing required --${key}.`);
  }

  const timeoutSeconds = Number(values.get("timeout-seconds") ?? "300");
  const pollSeconds = Number(values.get("poll-seconds") ?? "10");
  if (!Number.isFinite(timeoutSeconds) || timeoutSeconds <= 0) {
    throw new Error("--timeout-seconds must be positive.");
  }
  if (!Number.isFinite(pollSeconds) || pollSeconds <= 0) {
    throw new Error("--poll-seconds must be positive.");
  }

  return {
    name: values.get("name"),
    resourceGroup: values.get("resource-group"),
    expectedImage: values.get("expected-image"),
    healthPath: values.get("health-path"),
    timeoutSeconds,
    pollSeconds,
  };
}

function azJson(args) {
  let command = ["az", []];
  if (process.platform === "win32") {
    const azCommandPath = execFileSync("where.exe", ["az.cmd"], {
      encoding: "utf8",
      stdio: ["ignore", "pipe", "inherit"],
    })
      .trim()
      .split(/\r?\n/, 1)[0];
    const azureCliPython = resolve(dirname(azCommandPath), "..", "python.exe");
    if (!existsSync(azureCliPython)) {
      throw new Error(`Azure CLI Python runtime not found beside ${azCommandPath}.`);
    }
    command = [azureCliPython, ["-IBm", "azure.cli"]];
  }
  const output = execFileSync(
    command[0],
    [...command[1], ...args, "--output", "json"],
    {
      encoding: "utf8",
      stdio: ["ignore", "pipe", "inherit"],
    },
  );
  return JSON.parse(output);
}

function readSnapshot(name, resourceGroup) {
  const app = azJson([
    "containerapp",
    "show",
    "--name",
    name,
    "--resource-group",
    resourceGroup,
  ]);
  const latestRevisionName = app.properties?.latestRevisionName;
  const revision = azJson([
    "containerapp",
    "revision",
    "show",
    "--name",
    name,
    "--resource-group",
    resourceGroup,
    "--revision",
    latestRevisionName,
  ]);

  return {
    fqdn: app.properties?.configuration?.ingress?.fqdn,
    latestRevisionName,
    latestReadyRevisionName: app.properties?.latestReadyRevisionName,
    image: revision.properties?.template?.containers?.[0]?.image,
    healthState: revision.properties?.healthState,
    provisioningState: revision.properties?.provisioningState,
    runningState: revision.properties?.runningState,
  };
}

const delay = (milliseconds) =>
  new Promise((resolve) => setTimeout(resolve, milliseconds));

async function verifyHealth(fqdn, healthPath) {
  if (typeof fqdn !== "string" || fqdn.length === 0) {
    throw new Error("Azure response is missing the ingress FQDN.");
  }
  const url = new URL(healthPath, `https://${fqdn}`);
  const response = await fetch(url, {
    redirect: "manual",
    signal: AbortSignal.timeout(30_000),
  });
  if (response.status < 200 || response.status >= 300) {
    throw new Error(`Pinned-ready smoke check returned HTTP ${response.status}.`);
  }
  console.log(`Pinned-ready smoke check returned HTTP ${response.status}.`);
}

export async function main(argv = process.argv.slice(2)) {
  const options = parseArgs(argv);
  const deadline = Date.now() + options.timeoutSeconds * 1_000;

  while (Date.now() < deadline) {
    const snapshot = readSnapshot(options.name, options.resourceGroup);
    const result = evaluateRevision(snapshot, options.expectedImage);

    if (result.status === "failed") throw new Error(result.reason);
    console.log(result.reason);
    if (result.status === "ready") {
      await verifyHealth(snapshot.fqdn, options.healthPath);
      return;
    }

    await delay(options.pollSeconds * 1_000);
  }

  throw new Error(
    `Timed out waiting for ${options.name} to make the expected revision ready.`,
  );
}

if (import.meta.url === pathToFileURL(process.argv[1]).href) {
  main().catch((error) => {
    console.error(error.message);
    process.exitCode = 1;
  });
}
