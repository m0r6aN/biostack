import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

import { evaluateRevision } from "./verify-containerapp-deployment.mjs";

const expectedImage = "registry.example/biostack-api:immutable-sha";

function readySnapshot(overrides = {}) {
  return {
    latestRevisionName: "api--0000072",
    latestReadyRevisionName: "api--0000072",
    image: expectedImage,
    healthState: "Healthy",
    provisioningState: "Provisioned",
    runningState: "Running",
    ...overrides,
  };
}

test("accepts only the exact healthy latest-ready revision", () => {
  assert.deepEqual(evaluateRevision(readySnapshot(), expectedImage), {
    status: "ready",
    reason: "Revision api--0000072 is ready.",
  });
});

test("rejects the observed activation-failed candidate immediately", () => {
  const result = evaluateRevision(
    readySnapshot({
      latestReadyRevisionName: "api--0000055",
      healthState: "Unhealthy",
      runningState: "ActivationFailed",
    }),
    expectedImage,
  );

  assert.equal(result.status, "failed");
  assert.match(result.reason, /ActivationFailed/);
});

test("waits when the previous revision remains latest-ready", () => {
  const result = evaluateRevision(
    readySnapshot({
      latestReadyRevisionName: "api--0000055",
      healthState: "Unknown",
      runningState: "Processing",
    }),
    expectedImage,
  );

  assert.equal(result.status, "pending");
  assert.match(result.reason, /latest ready is api--0000055/);
});

test("waits for Azure to expose the exact immutable image", () => {
  const result = evaluateRevision(
    readySnapshot({ image: "registry.example/biostack-api:older-sha" }),
    expectedImage,
  );

  assert.equal(result.status, "pending");
  assert.match(result.reason, /waiting for/);
});

test("fails closed on incomplete Azure state", () => {
  const result = evaluateRevision(
    readySnapshot({ latestReadyRevisionName: undefined }),
    expectedImage,
  );

  assert.equal(result.status, "failed");
  assert.match(result.reason, /latestReadyRevisionName/);
});

test("deployment workflow gates each update before advancing", () => {
  const workflow = readFileSync(
    new URL("../.github/workflows/deploy.yml", import.meta.url),
    "utf8",
  );
  const apiUpdate = workflow.indexOf("- name: Update API container app");
  const apiVerify = workflow.indexOf("- name: Verify API revision readiness");
  const webUpdate = workflow.indexOf("- name: Update frontend container app");
  const webVerify = workflow.indexOf("- name: Verify frontend revision readiness");

  assert.ok(apiUpdate >= 0 && apiUpdate < apiVerify);
  assert.ok(apiVerify < webUpdate && webUpdate < webVerify);
  assert.match(workflow, /biostack-api:\$\{\{ github\.sha \}\}/);
  assert.match(workflow, /biostack-web:\$\{\{ github\.sha \}\}/);
  assert.match(workflow, /--health-path \/health/);
});
