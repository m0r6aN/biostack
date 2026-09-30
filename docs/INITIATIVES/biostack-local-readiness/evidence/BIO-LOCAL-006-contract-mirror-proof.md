# BIO-LOCAL-006 — Product contract mirror proof

- Ticket: BIO-LOCAL-006 (parcel: product contract mirror proof)
- Branch: `proof/bio-local-006-contract-mirrors`
- Pinned base: `e5b75e0` (Merge pull request #375)
- Date (UTC): 2026-09-19
- Scope: READ-ONLY against contract, mirrors, `FeatureGate`, TierGate. No contract/mirror/gate edits. No Stripe contact.
- OQ assumptions: OQ1 = focused existing contract/entitlement/tier tests + `--check` (no full backend suite); OQ3 = no INDEX work.

## AC1 — contract shape v1.0.0 effective 2026-07-13 (PASS)

Source: `contracts/product-contract.v1.json` (read-only; never edited).
Reference: `docs/INITIATIVES/biostack-production-readiness/CONTRACTS.md:5` (C1) + `:13-20` (authoritative decisions).

Shape dump (via `node -e` JSON projection, exit 0):

```json
{
  "contractVersion": "1.0.0",
  "effectiveDate": "2026-07-13",
  "interval": "month",
  "currency": "usd",
  "pastDueGraceDays": 0,
  "paidAccessStatuses": ["Active", "Trialing"],
  "plans": [
    { "code": "observer", "tier": "Observer", "monthlyPriceCents": 0, "cta": "/start" },
    { "code": "operator", "tier": "Operator", "monthlyPriceCents": 1200, "cta": "/billing?plan=operator" },
    { "code": "commander", "tier": "Commander", "monthlyPriceCents": 2900, "cta": "/billing?plan=commander" }
  ],
  "features": {
    "active_compounds": "Observer",
    "paid_intelligence": "Operator",
    "commander_intelligence": "Commander",
    "reviewed_relationship_graph": "Operator",
    "source_quality_tracker": "Operator",
    "glp1_observability_pack": "Operator",
    "side_effect_ambiguity_detector": "Commander",
    "high_risk_warning_first_guardrails": "Observer"
  },
  "limits": { "Observer": 8 },
  "canonical": { "onboarding": "/start", "analyzer": "/tools/analyzer", "postSignInDefault": "/protocol-console" },
  "aliases": { "/onboarding": "/start", "/map": "/tools/analyzer" },
  "health": { "livenessPath": "/health", "keonDependencyPath": "/health/keon" }
}
```

C1 match: monthly-only USD 0/1200/2900c; paid access Active/Trialing only; grace 0; canonical `/start` + `/tools/analyzer`; aliases `/onboarding`->`/start`, `/map`->`/tools/analyzer`; health `/health`, `/health/keon`. Zero drift vs C1.

## AC2 — mirrors in sync, `--check` green (PASS)

Command: `node scripts/sync-product-contract.mjs --check` — exit `0`.
Output verbatim: `Product contract mirrors are current.`

Mirror mechanism: `scripts/sync-product-contract.mjs:6-11` (canonical + 2 target paths), `:17-23` (v1.0.0 + monthly-only + grace-0 guards), `:25-31` (`--check` byte-compare, throws on drift).
Mirror paths recorded:
- `backend/src/BioStack.Application/ProductContract/product-contract.v1.json`
- `frontend/src/contracts/product-contract.v1.json`

Byte-identity (SHA256, via `node -e` crypto check, exit 0): all three files `a101d98ac02c659117d739616c7b94f300c1cc2f87d83e0b5a8854f19f88b70f`, 2651 bytes each. No drift; contract NOT edited to make `--check` pass.

## AC3 — FeatureGate entitlement mapping asserted (PASS)

Contract features (minimumTier) mapped to enforcement, observed in tests:

| feature | minimumTier | enforcement (file:line) | observed in |
|---|---|---|---|
| active_compounds | Observer, limit Observer=8 | `backend/src/BioStack.Application/Services/FeatureGate.cs:54-58` GetLimitAsync; `backend/src/BioStack.Application/Services/CompoundService.cs:139-152` EnsureActiveCompoundLimitAsync (402-class `observer_active_compound_limit`, HTTP 402) | `BillingTierIntegrationTests.Observer_IsBlockedBeyondActiveCompoundLimit_AndPaidStatesCanExceedUntilExpired` (PASS 1/1); `BillingAndFeatureGateTests.FeatureGate_ReturnsExpectedLimitsAndPaidFeaturesByTier` (PASS) |
| paid_intelligence | Operator | `FeatureGate.cs:48-52` IsEnabledAsync via `ProductContract.Current.IsFeatureEnabled`; `BillingService.cs:57-61` subscription feature map | `FeatureGate_ReturnsExpectedLimitsAndPaidFeaturesByTier` (Observer false, Commander true) |
| commander_intelligence | Commander | `FeatureGate.cs:60-73` EnsureEnabledAsync ("Commander is required..."); `ProductContract.cs:40-42` tier>=minimumTier | `FeatureGate_ReturnsExpectedLimitsAndPaidFeaturesByTier` (Commander true) |
| reviewed_relationship_graph | Operator | same IsEnabledAsync path | integration asserts false for Observer (`BillingTierIntegrationTests.cs:114`) |
| source_quality_tracker | Operator | same | integration asserts false for Observer (`:115`) |
| glp1_observability_pack | Operator | same | integration asserts false for Observer (`:116`) |
| side_effect_ambiguity_detector | Commander | same + Commander message branch | integration asserts false for Observer (`:117`) |
| high_risk_warning_first_guardrails | Observer | same (Observer-enabled) | integration asserts true for Observer (`:119`) |

Tier math: `backend/src/BioStack.Application/Services/ProductContract.cs:33-38` HasPaidAccess (paidThrough && status in PaidAccessStatuses), `:40-42` IsFeatureEnabled (tier >= ParseTier(minimumTier)), `:44-52` GetLimit, `:76-91` Validate (monthly-only + grace-0 guards), `:101-104` health-path guard.

Backend test runs (verbatim):
- `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter "FullyQualifiedName~BillingAndFeatureGateTests" --nologo` — `Passed! - Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 4 s` (exit 0).
- `dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --filter "FullyQualifiedName~BillingTierIntegrationTests.Observer_IsBlockedBeyondActiveCompoundLimit_AndPaidStatesCanExceedUntilExpired" --nologo` — `Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 1 s` (exit 0; xUnit2012 warnings pre-existing in AdminTranscriptIntakeResolutionIntegrationTests.cs only).

## AC4 — TierGate honest locked-tier rendering + monthly-only/grace-zero (PASS)

Observed logic (no new tests; no TierGate-dedicated test file exists — grep `TierGate` in `frontend/src/**/*.test.*` returns zero files):
- `frontend/src/components/protocol-portal/TierGate.tsx:10-14` TIER_RANK observer 0/operator 1/commander 2; `:22-25` renders children only when `TIER_RANK[currentTier] >= TIER_RANK[requiredTier]`; `:27-42` locked branch renders upgrade card ("Upgrade to unlock this protocol view", "Your current plan stays active everywhere else.", "Compare plans" -> `/pricing`) with zero children rendered — no fake content behind the gate.
- Live usage: `frontend/src/app/my-protocol/page.tsx:308` (calendar operator), `:322` (diet operator), `:330` (monitoring commander), `:336` (progress operator).

Monthly-only + grace-zero asserted by test AND observed logic:
- Test: `BillingAndFeatureGateTests.ProductContract_DefinesCanonicalPlansRoutesAndHealth (:134-146)` asserts version/month/plans/routes/health; `FeatureGate_PastDueDowngradesImmediatelyUnderContract (:95-131)` asserts `PastDueGraceDays == 0`, PastDue -> Observer, paid feature false; `frontend src/__tests__/lib/productContract.test.ts:14-22` asserts 1.0.0/month/0 grace/Active+Trialing/observer-operator-commander/$12/$29.
- Logic: `ProductContract.cs:83-91` throws unless interval==month and grace==0; `sync-product-contract.mjs:21-23` same guard; `FeatureGate.cs:43-45` downgrades to Observer unless HasPaidAccess.

## AC5 — aliases + health paths in code; runtime cross-cite deferred (PASS, code-cited)

| value | code citation (file:line) | test |
|---|---|---|
| `/onboarding` -> `/start` | `frontend/src/app/onboarding/page.tsx:5` `redirect(canonicalRoutes.onboarding)`; `frontend/src/lib/productContract.ts:19-20` canonicalRoutes/routeAliases from contract | `productContract.test.ts:25-28`; `start/page.test.tsx:76-103` (/onboarding -> /start); `middleware.public-routes.test.ts:17` public `/onboarding` |
| `/map` -> `/tools/analyzer` | `frontend/src/app/map/page.tsx:5` `redirect(canonicalRoutes.analyzer)` | `start/page.test.tsx:63-71` (/map -> /tools/analyzer); `middleware.public-routes.test.ts:17` public `/map` |
| canonical `/start`, `/tools/analyzer` | `frontend/src/app/start/page.tsx:9` path /start; `frontend/src/app/tools/analyzer/page.tsx:9` path /tools/analyzer | `SeoMetadata` + marketing/nav tests (out of focused scope, noted) |
| `/health` liveness | `backend/src/BioStack.Api/Program.cs:518` `MapHealthChecks(ProductContract.Current.Health.LivenessPath)` | contract-shape asserted (`BillingAndFeatureGateTests:144`, `productContract.test.ts:29-32`); runtime probe deferred |
| `/health/keon` dependency | `Program.cs:520-530` `MapGet(ProductContract.Current.Health.KeonDependencyPath, ...)` 503-when-unhealthy | same as above |

Runtime cross-cite DEFERRED to BIO-LOCAL-002 per directive: 002 runs after 006, so its transcripts cannot exist at 006 close. Follow-up: coordinator to record 002 PR link (no 002 PR exists at this commit). No live runtime claims made here.

## AC6 — evidence file + clean diff gate (PASS)

- This file is the single Allowed File: `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-006-contract-mirror-proof.md`.
- `git diff --check` — exit 0, clean.
- `git status --short` — only this evidence file (new) + pre-existing untracked coordinator directive copy `docs/specs/` (left untouched, unstaged, per ruling).

## Verification log (commands + exit codes + verbatim counts)

1. `node -e "<contract shape projection>"` — exit 0 — JSON above (v1.0.0/2026-07-13/month/usd/0/Active,Trialing/0,1200,2900/aliases/health).
2. `node scripts/sync-product-contract.mjs --check` — exit 0 — `Product contract mirrors are current.`
3. `node -e "<sha256 of 3 contract files>"` — exit 0 — all `a101d98a...` 2651 bytes.
4. `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter "FullyQualifiedName~BillingAndFeatureGateTests" --nologo` — exit 0 — `Passed! - Failed: 0, Passed: 8, Skipped: 0, Total: 8`.
5. `dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --filter "FullyQualifiedName~BillingTierIntegrationTests.Observer_IsBlockedBeyondActiveCompoundLimit_AndPaidStatesCanExceedUntilExpired" --nologo` — exit 0 — `Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1`.
6. `npm ci --no-audit --no-fund` (frontend, lockfile-present; node_modules was absent) — exit 0 — `added 525 packages in 7m`. No tracked files touched.
7. `npx vitest run src/__tests__/lib/productContract.test.ts src/__tests__/app/start/page.test.tsx src/__tests__/middleware.public-routes.test.ts` (default forks pool) — FAILED to start workers: `3 errors`, `no tests` (`[vitest-pool-runner]: Timeout waiting for worker to respond` x3). Recorded as environment issue, not a product failure.
8. `npx vitest run --pool=threads src/__tests__/lib/productContract.test.ts` — exit 0 — `Test Files 1 passed (1)`, `Tests 3 passed (3)`.
9. `npx vitest run --pool=threads src/__tests__/app/start/page.test.tsx src/__tests__/middleware.public-routes.test.ts` — exit 0 — `Test Files 2 passed (2)`, `Tests 38 passed (38)` (start 10 + middleware 28).
10. `git diff --check` — exit 0 — clean. `git status --short` — `?? docs/specs/` (pre-existing) + `?? docs/INITIATIVES/biostack-local-readiness/` (this evidence).

Frontend focused total: 41 passed, 0 failed. Backend focused total: 9 passed, 0 failed. No new persistent tests added. No full suite run (OQ1).

## LS10 + SG-L5 (partial) row for coordinator merge

- Scenario: LS10 (entitlement/paywall honesty) — evidence: TierGate upgrade-card logic + FeatureGate mapping + Observer-limit integration test above.
- Security gate: SG-L5 partial (entitlement/paywall honesty) — no hostile PII/billing probes beyond contract-shape assertions; no live Stripe contact; no real price IDs; shape + gating only.
- PR link: (coordinator owns Gate 3 — no PR opened by builder; reviewer replays `--check` + test filters above).

## Decisions / blockers

- None blocking. Drift/mismatch/bypass: NONE found — zero stop-and-report triggers.
- Note: default vitest forks pool times out in this worktree environment; `--pool=threads` used and recorded verbatim above.
- 002 follow-up open: runtime alias/health transcript cross-cite belongs to BIO-LOCAL-002 (pending).

## Files changed

- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-006-contract-mirror-proof.md` (new; only file).
