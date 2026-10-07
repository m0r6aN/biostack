// Verifies the Stripe billing reconciliation surface on a deployed environment
// before promoting a build:
//   1. quarantined webhook receipts are visible with their failure codes,
//   2. the admin subscriptions resync endpoint answers with its summary contract,
//   3. optionally, a user's subscription read model reflects a paid tier after
//      the resync (the user-facing proof).
//
// The resync is idempotent (rows are reconciled from Stripe's current state),
// so this script is safe to run repeatedly.
//
// Usage:
//   BIOSTACK_BASE_URL=https://biostack.cc \
//   BIOSTACK_ADMIN_COOKIE='biostack_session=...' \
//   [BIOSTACK_CUSTOMER_ID=cus_...] \
//   [BIOSTACK_USER_COOKIE='biostack_session=...'] \
//   node scripts/verify-billing-resync.mjs
//
// Exit codes:
//   0  passed
//   1  failed (HTTP or response-contract failure)
//   2  passed with findings (unmapped customers / unresolved prices need config fixes)

import { pathToFileURL } from "node:url";

const REQUIRED_SUMMARY_KEYS = ["reconciled", "unmappedCustomerIds", "unresolvedPriceIds"];

export function evaluateResync(summary) {
  for (const key of REQUIRED_SUMMARY_KEYS) {
    if (!(key in (summary ?? {}))) {
      return { status: "failed", reason: `Resync response is missing ${key}.` };
    }
  }

  if (typeof summary.reconciled !== "number" || summary.reconciled < 0) {
    return { status: "failed", reason: "Resync response has an invalid reconciled count." };
  }

  if (!Array.isArray(summary.unmappedCustomerIds) || !Array.isArray(summary.unresolvedPriceIds)) {
    return { status: "failed", reason: "Resync response findings must be arrays." };
  }

  if (summary.unmappedCustomerIds.length > 0) {
    return {
      status: "findings",
      reason: `Unmapped Stripe customers: ${summary.unmappedCustomerIds.join(", ")} — subscriptions for these cannot reconcile until their metadata or app mapping is fixed.`,
    };
  }

  if (summary.unresolvedPriceIds.length > 0) {
    return {
      status: "findings",
      reason: `Unresolved price ids: ${summary.unresolvedPriceIds.join(", ")} — add them to the approved product contract configuration (e.g. Stripe:OperatorPriceId) and re-run.`,
    };
  }

  return { status: "passed", reason: `Reconciled ${summary.reconciled} subscription(s).` };
}

export function evaluateQuarantine(receipts) {
  if (!Array.isArray(receipts)) {
    return { status: "failed", reason: "Quarantine response must be an array." };
  }

  const byFailureCode = new Map();
  for (const receipt of receipts) {
    const code = receipt?.failureCode ?? "<missing>";
    byFailureCode.set(code, (byFailureCode.get(code) ?? 0) + 1);
  }

  if (receipts.length === 0) {
    return { status: "passed", reason: "No quarantined Stripe events." };
  }

  const breakdown = [...byFailureCode.entries()].map(([code, count]) => `${code}×${count}`).join(", ");
  return { status: "findings", reason: `${receipts.length} quarantined Stripe event(s): ${breakdown}.` };
}

export function evaluateSubscription(subscription) {
  if (subscription === null || typeof subscription !== "object") {
    return { status: "failed", reason: "Subscription response is missing." };
  }

  const { tier, status, currentPeriodEndUtc } = subscription;
  if (typeof tier !== "string" || tier.length === 0) {
    return { status: "failed", reason: "Subscription response is missing tier." };
  }

  if (tier === "Observer") {
    return {
      status: "failed",
      reason: `Subscription still reads as Observer (status ${status ?? "<missing>"}) after resync — the row is stale, unmapped, or the subscription is genuinely lapsed.`,
    };
  }

  return {
    status: "passed",
    reason: `Subscription reads as ${tier} (status ${status ?? "<missing>"}, paid through ${currentPeriodEndUtc ?? "<missing>"}).`,
  };
}

async function request(baseUrl, path, { method = "GET", cookie } = {}) {
  const response = await fetch(`${baseUrl}${path}`, {
    method,
    headers: {
      accept: "application/json",
      ...(cookie ? { cookie } : {}),
    },
  });

  const text = await response.text();
  let body = null;
  try {
    body = text.length > 0 ? JSON.parse(text) : null;
  } catch {
    // Non-JSON bodies are reported through the caller's status check.
  }

  return { status: response.status, body };
}

function report(label, evaluation) {
  const marker = evaluation.status === "passed" ? "PASS" : evaluation.status === "findings" ? "FINDING" : "FAIL";
  console.log(`[${marker}] ${label}: ${evaluation.reason}`);
}

function worstExit(evaluations) {
  if (evaluations.some((evaluation) => evaluation.status === "failed")) return 1;
  if (evaluations.some((evaluation) => evaluation.status === "findings")) return 2;
  return 0;
}

export async function main(env = process.env, fetchImpl = request) {
  const baseUrl = (env.BIOSTACK_BASE_URL ?? "").replace(/\/$/, "");
  const adminCookie = env.BIOSTACK_ADMIN_COOKIE ?? "";
  const userCookie = env.BIOSTACK_USER_COOKIE ?? "";
  const customerId = env.BIOSTACK_CUSTOMER_ID ?? "";

  if (baseUrl.length === 0 || adminCookie.length === 0) {
    console.error("BIOSTACK_BASE_URL and BIOSTACK_ADMIN_COOKIE are required.");
    return 1;
  }

  const evaluations = [];

  const quarantine = await fetchImpl(baseUrl, "/api/v1/admin/billing/stripe/events/quarantined", { cookie: adminCookie });
  if (quarantine.status !== 200) {
    const evaluation = { status: "failed", reason: `Quarantine listing returned HTTP ${quarantine.status}.` };
    report("quarantined events", evaluation);
    return 1;
  }
  const quarantineEvaluation = evaluateQuarantine(quarantine.body);
  report("quarantined events", quarantineEvaluation);
  evaluations.push(quarantineEvaluation);

  const resyncPath = customerId.length > 0
    ? `/api/v1/admin/billing/subscriptions/resync?customerId=${encodeURIComponent(customerId)}`
    : "/api/v1/admin/billing/subscriptions/resync";
  const resync = await fetchImpl(baseUrl, resyncPath, { method: "POST", cookie: adminCookie });
  if (resync.status !== 200) {
    const evaluation = { status: "failed", reason: `Resync returned HTTP ${resync.status}.` };
    report("subscriptions resync", evaluation);
    return 1;
  }
  const resyncEvaluation = evaluateResync(resync.body);
  report("subscriptions resync", resyncEvaluation);
  evaluations.push(resyncEvaluation);

  if (userCookie.length > 0) {
    const subscription = await fetchImpl(baseUrl, "/api/v1/billing/subscription", { cookie: userCookie });
    if (subscription.status !== 200) {
      const evaluation = { status: "failed", reason: `Subscription read model returned HTTP ${subscription.status}.` };
      report("subscription read model", evaluation);
      return 1;
    }
    const subscriptionEvaluation = evaluateSubscription(subscription.body);
    report("subscription read model", subscriptionEvaluation);
    evaluations.push(subscriptionEvaluation);
  }

  return worstExit(evaluations);
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? "").href) {
  process.exit(await main());
}
