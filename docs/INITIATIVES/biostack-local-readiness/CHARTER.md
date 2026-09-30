# Initiative Charter — biostack-local-readiness

## Mission

Prove BioStack is locally runnable and honestly bounded at `main@e5b75e0`: dev stack boots, public evidence/tools read correctly, authenticated surfaces isolate by owner, governance Spine + receipts + guidance gates hold, and every claim maps to a local evidence artifact. No production, staging, or revenue claim is in scope.

## In scope

Local dev-stack boot and health; public knowledge/tools read paths; TierGate + entitlement projection (B3/B4/B5 reduced shapes); magic-link/passkey auth + ownership isolation (local, in-memory inbox); versioned consent gate; Spine hash chain + signed checkpoints + receipt anchoring (stubbed-runtime posture stated, not oversold); Guidance Content Contract Class A–D enforcement incl. backstop tests; product-contract mirror integrity; targeted 150 compounds seeded locally (seed file 57 → 150, draft/needsReview/inactive, SeedJob run + served-count proof); local test/lint/build evidence.

## Out of scope

All deployments and hosted promotion (`docker-compose.yml` prod shape, Azure, OIDC, image push, traffic/smoke); live Stripe products/prices/webhook lifecycle beyond contract shape; live email/SMTP delivery; PostgreSQL backup/restore drills; historical secret rotation / full-history scans; SEO/browser-matrix/a11y sign-off; provider SLA/operations; any new clinical/prescriptive feature; any public claim of revenue, subscribers, clinical validation, regulatory clearance, privacy guarantees (`/privacy`, `/terms` are stubs), local-first architecture, or live governance.

## Success criteria

Every local release gate in `RELEASE-GATES.md` passes with an environment-pinned artifact at `e5b75e0` (or named successor if seed expansion forces a new SHA, with re-verification); all `unverified` gates are either proven or explicitly waived by a named owner; the eleven foreman parcels (BIO-LOCAL-001..011) complete their loops with green chains; the seed file holds exactly 150 draft/needsReview/inactive records and the local API serves 150 compounds; the release owner records LOCAL-GO or HOLD in `FINAL-HANDOFF.md`.

## Stop conditions

Stop on: missing product decision; contract drift (`product-contract.v1.json` or guidance contract needs a version bump); unclear security ownership; any attempt to promote, deploy, charge, send live email, or claim production readiness; evidence that cannot be reproduced locally; frozen foreman-line contract needing modification.
