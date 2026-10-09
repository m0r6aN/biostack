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
| 2026-10-07 | Wave 1b returned: PR #476 (BIO-LOCAL-001 evidence, all ACs met); PR #474 (pairwise remediation, mechanical); PR #475 (P1 repair commit `588c456` — stopped on branch-name conflict in the envelope; coordinator fault → amendment R1); Keon specs reviewed: APPROVE-WITH-FIXES (F1–F3 major) |
| 2026-10-07 | Coordinator-checkout incident: worker git state changes in `/home/cmorgan76/Repos/biostack` detached the coordinator checkout and misdirected commit `4ea2cde`; fully recovered (`a73afa4` on `main`); standing rule added: workers must never change git state in the coordinator checkout |
| 2026-10-07 | Wave 1c dispatched (4 agents): p1_branch_reconciler (R1 execution), bio_local_001_reviewer (replay + evidence review, PR #476), keon_spec_fixer (F1–F8, PR #473), frontend_proof_panel_shaper (audit top item) |
| 2026-10-07 | Wave 1c partial return: keon_spec_fixer DONE (F1–F5 applied on PR #473, rules maintained); frontend_proof_panel_shaper DONE (PR #477, BIO-FE-001 review-candidate); p1_branch_reconciler STOPPED on frozen worktree literal (`D:/Repos/...` vs Linux root) → amendment R2 (env equivalence mapping, deviation disclosed for owner ratification) |
| 2026-10-07 | Tooling: real RTK v0.51.0 (rtk-ai/rtk) installed at `~/.local/bin/rtk` + `rtk init -g`; headroom CLI 0.40.0 (headroomlabs-ai/headroom) via uv; pi extension `@r3b1s/pi-token-killer` installed (auto-routes bash through rtk). Impostor npm packages `rtk`/`headroom` removed from the repo |
| 2026-10-07 | Wave 1d dispatched: p1_env_reconciler (R2 execution + PR reconciliation) |
| 2026-10-07 | Wave 1d returned: bio_local_001_reviewer **PASS** (F1 administrative SHA drift → accept-as-documented; PR #476 ready for Gate 3); p1_env_reconciler cleared check 4 via R2 mapping then STOPPED on latent check-7 bug (INDEX.md first-line vs table header — frozen-spec self-conflict) → **R3 ruling**: fix the builder surface (INDEX.md content reorder), zero new verifier deviation |
| 2026-10-07 | Wave 2a dispatched (5 agents): p1_finalizer (R3 + PR reconciliation + delta review prep), bio_fe_001_spec_reviewer (PR #477), bio_local_002_builder, bio_local_003_builder (elevated — dual review post-build), bio_local_006_builder (Gate 2 records: GATE2-BIO-LOCAL-002-003-006.md). Pairwise lane blocked on 2 owner decisions: schema-sufficiency pre-gate + migration-vs-redesign ruling |
| 2026-10-07 | Owner: ALL PRs #472–#477 merged at Gate 3; R2 ratified; pairwise decisions delegated to coordinator → decided (D-A: shape BIO-PAIRWISE-SCHEMA-001 only if P0 proves schema insufficiency, never de-scope; D-B: P2 code-only in all outcomes, no migration ever). BIO-LOCAL-001 closed (closure record + spec→done + INDEX row). Wave 2b dispatched (3 agents): bio_local_004_builder (elevated), bio_local_007_builder (D10 reachability gate), bio_pairwise_001_spec_reviewer |
| 2026-10-07 | Wave 2c returned + owner merged #482 (P1 rework) + #483 (BIO-LOCAL-007): 007 verdict **REACHABLE-100** (57+43=100, gap 50 to target — D10 owner ruling requested: seed-100-now / KEO-73·74 sourcing / ratify-100-target); pairwise-001 spec review **PASS** → P0 Gate 2 recorded; retros 002 **PASS** (F1/F2 admin → accepted) + 006 **PASS** → both closed; BIO-LOCAL-004 refused again → retry on sonnet-5; P1 closure after delta retro review |
| 2026-10-07 | Wave 3a dispatched (6 agents): bio_local_004_builder_v2 (sonnet-5), bio_local_003_retro_reviewer_1 + _2 (elevated, dual), bio_local_005_builder, p1_delta_retro_reviewer, bio_pairwise_001_builder (P0 census, data-framing) |
| 2026-10-07 | Returns: BIO-LOCAL-004 v2 clean → owner merged #484; BIO-LOCAL-005 refused (2nd) → sonnet-5 retry; P1 delta review FAIL → coordinator D8 reproduction: blocker NOT-APPLICABLE (rows are main-side provenance) but exposed real broken-INDEX defect from #482 merge → repaired; **P1 CLOSED** (closures/P1.md, row → done); 003 retros both PASS → closed (M1 MEDIUM → bounded remediation parcel required; L1 LOW → deferred to prod-hardening); P0 census **SUBSTRATE VERDICT: SCHEMA-SUFFICIENT** (D-A resolved: no schema parcel needed) → PR #485 pending review; seed batch 008 refused ×3 (haiku, sonnet-4-5, sonnet-5) → provider switch to GLM-5P3 |
| 2026-10-07 | Wave 3b dispatched: bio_local_008_builder (GLM-5P3), bio_pairwise_001_reviewer, bio_fe_001_builder, bio_local_005_builder_v2 (sonnet-5) |
| 2026-10-07 | Owner merged #485 (P0 census), #486 (BIO-FE-001 live proof panel), #487 (seed batch A — **GLM-5P3 broke the seed refusal streak**), #488 (BIO-LOCAL-005). P0 review **PASS** (citations exact, SCHEMA-SUFFICIENT ratified) → BIO-PAIRWISE-001 CLOSED, D-A resolved with no schema parcel. Wave 3c dispatched (8 agents, full cap): bio_local_009_builder (GLM-5P3, batch B), bio_pairwise_002_spec_reviewer, bio_local_005_retro_1/2, bio_fe_001_retro_reviewer, bio_local_004_retro_1/2, bio_local_007_008_reviewer (combined standard-class retros) |
| 2026-10-07 | Wave 3c returned: batch B built (PR #489 awaiting owner); pairwise-002 **APPROVE-WITH-FIXES** (2 minor); retros — 005 R1 PASS / R2 CONDITIONAL (C1: positive-control not reproducible → production enablement held), FE-001 **PASS** (zero new claims/numerics), 004 R1 PASS, 007+008 **PASS** (batch A: 28/28 verbatim citations); 004 R2 refused → retry sonnet-5. Closed: 005/007/008/FE-001 (closures + registry + spec→done). Wave 3d dispatched: 004-retro-2 retry, pairwise-002 fixer, BIO-LOCAL-012 (M1 remediation), BIO-KEON-001 builder (K-1 serial first), P2 shaper |
| 2026-10-08 | Owner merged #489–#493 (batch B, pairwise-002 fixes, **K-1 docs lane shipped**, P2 spec candidate, **M1 security fix**). 004-retro-2 retry: **FAIL — real blocker R1** (spine accepts truncation/rollback: deleted trailing rows → stale checkpoint passes `IsFullyValid`) → BIO-LOCAL-013 bounded remediation (elevated, dual review). Wave 4a dispatched (6): bio_local_010_builder (batch C, final serial writer), bio_local_013_builder (R1 fix), bio_keon_002_builder (K-3), bio_pairwise_002_builder (elevated, dual), p2_review_1 + p2_review_2 (P2 dual review per charter) |
| 2026-10-08 | Owner merged #494 (batch C — **corpus 85→100**), #495 (K-3 shipped), #496 (pairwise-002 ratification). P2 dual review: **both FAIL, narrow** (R1: D14 intersection rule underdetermined; R2: verifier gaps could pass nonconforming output) → P2 rework via amendment, no locked decisions reopened. 013 still building. Wave 4b dispatched (7): bio_local_011_builder (**corpus finale**), p2_fixer, pairwise-002 retro ×2 (elevated), bio_local_012_reviewer, bio_keon_001_002_reviewer (combined), bio_local_009_010_reviewer (combined) |
| 2026-10-08 | Owner merged #497 (R1 truncation fix), #498 (P2 rework), #499 (**BIO-LOCAL-011 corpus finale**), #500 (D-D consolidation, corpus 100→99). Owner ruled D-D collisions (creatine pair distinct+cross-ref; CG/HCG pair merged). Verdicts: P2 re-review R1 PASS / R2 FAIL-narrow (one new vacuous content-fidelity check → P2 fixer 2); 013 R1+R2 PASS (F1 MEDIUM default-config + F2 substitution gap → hardening H2; R1 blocker resolved); pairwise-003 **APPROVE**; 011 reviewer REFUSED (counting) → GLM retry. Closed: 012, K-1/K-3, 009/010, pairwise-002. Wave 5b dispatched: 011-reviewer-v2 (GLM), 014 reviewer, p2_fixer_2, pairwise-003 builder |
| 2026-10-08 | **LOCAL-GO** — biostack-local-readiness FINAL-HANDOFF.md verdict recorded (exit criteria 1–5 satisfied; corpus gate waived-as-amended by owner rulings D-C/D-D; H1–H3 hardening register; VERIFICATION.md sealed receipt table; RELEASE-GATES.md all resolved; specs → done/; INDEX regenerated). 011 review PASS (GLM re-derived all counts). Open in flight: 014 review, p2_fixer_2, pairwise-003 builder |
| 2026-10-08 | 014 review **PASS** (D-D rules exact, content preserved) — corpus chain FULLY sealed; p2_fixer_2 → PR #501 (targeted re-review next); SG-L8 residual scoped as low-priority H3 (claim-trace evidence already strong via 007–011). Combined wave dispatched (4): p2_re_review_3 (targeted), h1_builder (positive control), h2_builder (truncation default posture, elevated), bio_pairwise_004_spec_reviewer |
| 2026-10-08 | **D-H (owner):** "gate 3 is cleared and open" (green-chain merges proceed; class-triggered merge approvals + production gates unchanged) + **P0-B design gate OPENED** — design presented (`biostack-governed-delivery/P0-B-DESIGN-GATE.md`, D-B1..D-B6 incl. guidance-contract-v1 vs charter-D13 conflict). Zero PRs open at Gate 3. P2 registry drift fixed (closed per `closures/P2.md`). Next: owner rulings D-B1..D-B6 → P3-A review/dispatch (unblocked by P2 closure) → P0-A chain → P0-B spec |
| 2026-10-08 | **D-I (owner): P0-B design gate RULED** — "D-B1: c · D-B2–D-B6: as recommended" → staged split frozen (Class A/B/C + deterministic math now; `biostack-recommended` origination behind guidance-contract v2.0.0 re-ratification); full matrix frozen as P0-B's normative input. Chain next: P3-A → P0-A (canon freeze) → P0-B spec encodes D-B1..D-B6 → P0-C fixtures |
| 2026-10-08/09 | **Spec-review wave (4 blind dual reviews, claude-sonnet-5): P3-A + P0-A both REJECT/REJECT** → triage (all `fix`, no locked decisions) → fixers → round-2 targeted dual (P3-A REJECT/AWF, P0-A APROVE/AWF) → D8 reproductions confirmed (fixture non-dispositiveness, 26-file cardinality, CI-004 quote drift) → fixer-2s → owner merged #521/#522 at Gate 3 mid-wave (D-G's P0-A merge approval evidenced by the owner's own merge) → round-3 targeted (P3-A AWF single MINOR; P0-A APROVE) → exact amendments landed (`be81530`, `63e4cec`) → **#523 merged per D-H green-chain scope**; **#524 open — merge is the owner's (class-triggered human approval)**. Chains COMPLETE: P3-A spec @ `be81530`, P0-A spec @ `63e4cec` |
| 2026-10-09 | **Gate 2: P3-A implementation dispatched** — `p3a_builder` @ `feat/biostack-governance-p3a`, single anchor `756c9d1` (`dispatch/GATE2-P3A-IMPLEMENTATION.md`), dual review after build. Next: #524 owner merge → P3-A build/review/close → P0-A dispatch (D-G chain; needs P3-A closed) → P0-A close freezes canon precedence → P0-B spec encodes D-B1..D-B6 |
| 2026-10-09 | Owner merged #524 (P0-A spec final) + #525 (P3-A impl) mid-wave; retrospective dual impl review PASS-WITH-FIXES ×2 (2 demonstrated verifier blockers) → remediation #527 (merged) → re-verify PASS-WITH-FIXES ×2 (all six fixes closed, new exploit variants rejected) → **P3-A CLOSED** (`closures/P3-A.md`; check-3 anchor constraint dispositioned). **P0-B spec PR #526** (shaped from frozen D-B1..D-B6): dual review AWF/AWF — **matrix fidelity byte-identical** (primary duty clean); 8 fix rows → `p0b_spec_fixer` in flight. **Gate 2: P0-A implementation dispatched** (`dispatch/GATE2-P0A-IMPLEMENTATION.md`, `p0a_builder` @ `feat/p0a-canon-precedence`) — merge is the owner's (4-class human approval) |
| 2026-10-09 | Owner merged #526 (P0-B spec canon) + #528 (P0-A impl) + #529 (P0-B amendment 1, D-J dose-context). P0-B re-review: APPROVE / AWF → **D-J** amendment (spec-gap, owner-override-able). P0-A retro dual review PWF ×2 → **D-K** (precedence directional constraint — F1 BLOCKER-candidate: rank-mechanics vs CI-002/CI-005 safety prohibitions; fails safe, owner-override-able) → remediation #530 (D-K transcription + quoted-content verification + fixes) → re-verify PWF ×2 → micro-fix `19ce733` (verifier empty-diff crash + quote-splice hardening; both adversarial reproductions now fail). Coordinator run: **P0-A verification PASS (20 checks)** + evidence written. **#530 open — owner merge seals P0-A** → then **P0-B implementation dispatch fires** (record staged: `dispatch/GATE2-P0B-IMPLEMENTATION.md`) |
