# Coordinator Dispatch Queue — all BioStack goals

Coordinator-owned registry artifact. Started 2026-10-07 on owner directive: the coordinator
session is coordinator and owner of bringing every incomplete goal to completion, delegating all
build/verify/documentation work to worker agents. Merges to `main` remain Gate 3 human decisions
(owner). Production release remains governed by `biostack-production-readiness` only.

## Ordering principles

1. Governance foundations first (they govern every later parcel).
2. Proof substrate next (local-readiness chain unblocks honest product verification).
3. Product-facing frontend/analyzer value next (the premium-site goal).
4. Blocked/human-gated lanes scheduled only when their dependency clears.

## Rules of engagement (all worker agents)

- Max **8 concurrent agent processes**. Launch in waves; never oversubscribe.
- **Right-sized models**: `claude-haiku-4-5` for mechanical verification and well-specified docs
  work; `claude-sonnet-4-5` for builders, shapers, and reviewers; frontier models only for
  dual-review of architecture/risk-sensitive parcels where sonnet review quality is insufficient.
- Every worker: work only in the **named branch and worktree** from its Gate 2 / dispatch record.
  No ambient branch or worktree selection. No scope expansion. Stop-and-report on any conflict.
- **Commit → push → open PR against `main`.** Never merge. Gate 3 merge is the owner's decision.
- After push + PR: remove your worktree. Keep the branch (it backs the PR). Post-merge cleanup
  deletes merged branches separately.
- No secrets, credentials, PII, or health payloads in any commit, artifact, or PR body.
- `rtk` from AGENTS.md is not installed in this environment; use plain commands.
- Verification run in the wrong environment, stale base, or with any red check voids the
  dispatch for that parcel (spec lifecycle rule 6) — report, do not patch over it.

## Wave plan

### Wave 1 — dispatched 2026-10-07 (6 agents)

| # | Parcel / task | Agent role | Model | Branch / worktree | Status |
|---|---|---|---|---|---|
| 1 | P1 closure: deterministic verifier run at reconstructed builder tip | p1_verifier | haiku-4-5 | `biostack-wt/p1-closure` (detached `456fed2`) | dispatched |
| 2 | P1 closure: independent review A | p1_review_1 | sonnet-4-5 | read-only at `456fed2` | dispatched |
| 3 | P1 closure: independent review B | p1_review_2 | sonnet-4-5 | read-only at `456fed2` | dispatched |
| 4 | BIO-LOCAL-001 local dev-boot proof | bio_local_001_builder | sonnet-4-5 | `proof/bio-local-001-local-dev-boot` @ `biostack-wt/BIO-LOCAL-001` | dispatched |
| 5 | PR10 TODO: focused frontend build/type check (+ TODO tick if green) | todo_verifier | haiku-4-5 | `chore/todo-pr10-verify` (only if green) | dispatched |
| 6 | Keon K-1/K-3 docs lanes: shape review-candidate specs | keon_k1k3_shaper | haiku-4-5 | `docs/keon-k1-k3-shaping` @ `biostack-wt/keon-k1k3-shaping` | dispatched |

### Wave 2 — queued (parallel where surfaces are disjoint)

- BIO-LOCAL-002 (knowledge/tools read proof), BIO-LOCAL-003 (auth/tenancy isolation),
  BIO-LOCAL-006 (product contract mirror): independent surfaces, parallel after 001 evidence lands.
- BIO-LOCAL-004 (governance spine), BIO-LOCAL-005 (guidance contract enforcement): elevated risk,
  dual review; parallel with each other after 001.
- P2 shaping+build (risk taxonomy) — after P1 closure record lands (dependency spine).
- Frontend readiness shaping: homepage live proof panel (audit top item), `/compounds` public-read
  decision input, onboarding route consolidation (`/start`, `/map`, `/onboarding`).
- Analyzer residual parcels: DOCX/PDF table dose binding, multi-token prose (A2), `Check-in daily`
  (A3) — shape as BIO-ANALYZER-005+ review candidates.
- Pairwise lane remediation: replace `KEO-TBD-*` placeholders with real ticket IDs and close the
  no-TBD rule violations (lifecycle requirement before any dispatch).

### Wave 3 — queued

- BIO-LOCAL-007 (seed gap inventory) → BIO-LOCAL-008/009/010 (seed batches) → BIO-LOCAL-011
  (seed run + serving proof): serialized chain, 007 gates reachability verdict first.
- P3-A → P4 → P6 → P7 (governed-delivery spine after P2).
- Keon K-2 (adapter seam) — only when the versioned Keon adapter/compatibility contract exists.
- Sidecar-deployment recovery: diagnose blocked P01 acceptance (`blocked-handoff-2026-09-11`),
  report-only; P04b / Gate 3A / 3B remain ungranted (human gates).

### Wave 4 — queued behind human gates / external dependencies

- Production-readiness parcels: PR-PROV-001, PR-DATA-001, PR-BILL-001 (human billing decisions),
  SEC-RECEIPT-001 (blocked-on-KEO-64), PR-REL-001 (release owner), BIO-RT-01 live-hold.
- P0-A..P0-D4 (product doctrine recovery — human-gated), P5, P8 (after P7 + pinned Keon contract),
  P9 capstone (the north-star product initiative; P9-0 inventory first).

## Human gates (owner-only; coordinator stops and returns)

- Every Gate 3 merge to `main`.
- Local-readiness OQ1–OQ3 rulings (parcels proceed on stated default assumptions, stop on conflict).
- Any production deployment, release verdict change, billing/Stripe decision, legal-policy
  effectiveness, provider-pilot expansion, data deletion.
- Keon contract publication (unblocks K-2/K-4, P8); KEO-64 (unblocks SEC-RECEIPT-001).
- Sidecar P04b / Gate 3A / 3B authorization.

## Status log

| Date | Event |
|---|---|
| 2026-10-07 | Queue created; Wave 1 dispatched (6 agents); P1 closure plan + BIO-LOCAL-001 Gate 2 recorded |
| 2026-10-07 | Wave 1 returned: PR #472 (PR10 verification green, TODO ticked); PR #473 (BIO-KEON-001/002 review-candidate specs); P1 reviews 1+2 PASS (2 minor findings); P1 verifier STOPPED on `verify-p1.ps1` runtime bug → rework authorized (`P1-REWORK-2026-10-07.md`); pairwise survey complete, remediation checklist ready |
| 2026-10-07 | Wave 1b dispatched (4 agents): p1_repair_builder, bio_local_001_builder (docker access granted via `newgrp docker`), keon_spec_reviewer (PR #473), pairwise_remediation_shaper (BIO-PAIRWISE-001..006 naming ratified) |
