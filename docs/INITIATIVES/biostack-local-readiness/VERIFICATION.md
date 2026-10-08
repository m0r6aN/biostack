# Verification — biostack-local-readiness (A-4)

## Assessment-level runs (this session, `e5b75e0`, local workspace, no containers started)

| Run (UTC 2026-09-18) | Environment/build | Procedure | Result |
|---|---|---|---|
| git-state | local worktree `D:\Repos\BioStack` | `git status --short --branch`, `git log --oneline -8`, `git worktree list` | `main` in sync with `origin/main`; HEAD `e5b75e0`; only untracked `Claude outputs/*` + `docs/specs/`; ~100 stale worktrees noted as hygiene debt, no main-tree contamination |
| diff-hygiene | local, `e5b75e0` | `git diff --check` | clean (LS11 assessment-level evidence) |
| static-presence | local source/diff | read `docker-compose.dev.yml`, `contracts/product-contract.v1.json` v1.0.0, `docs/guidance/RATIFICATION.md`, `frontend/src` knowledge/TierGate/overlap wiring, `backend/src/.../KeonRuntimeDependencyInjection.cs`, `Program.cs` health mapping | contracts/compose/wiring PRESENT on disk; behavior UNVERIFIED (no runtime executed per no-surprise local-only rule — containers are started only inside BIO-LOCAL-001's isolated parcel) |
| prior-evidence triage | docs only | read `biostack-production-readiness/*`, `BIOSTACK_FRONTEND_READINESS_AUDIT.md` | HOLD verdict + audit deltas accepted as CONSTRAINTS; zero rows promoted to local passing |
| seed baseline (2026-09-18) | local source/diff | count `Seeds/substances-seed.json` (57 records); read one record shape (claims-bearing, `reviewStatus: draft`, `needsReview: true`, `isActive: false`); read frozen count tests (`CorpusIdentityInventoryBuilderTests` asserts 57/45/12, `StructuralEvaluationReportBuilderTests` asserts 57/45); confirm test `Fixtures/substances-seed.json` is an independent synthetic sample (11 KB vs 614 KB corpus); read candidate universe (70) + pilot candidates + evidence dir (78 packets) | corpus at 57, target 150 (D10); two frozen tests must update explicitly in 011; fixture unaffected; reachability of 150 from existing inputs UNVERIFIED (parcel 007 measures) |

## Required verification record (per parcel run)

**Sealed receipt references (coordinator, 2026-10-08):** every parcel below completed the
governed loop; evidence paths are repo-pinned; review verdicts are independent; merges are owner
Gate 3 decisions.

| Parcel | PR / merge | Evidence | Independent review | Closure |
|---|---|---|---|---|
| BIO-LOCAL-001 | #476 / `28fc37d6` | `evidence/BIO-LOCAL-001-boot-proof.md` + config note | PASS (replayed) | `closures/BIO-LOCAL-001.md` |
| BIO-LOCAL-002 | #479 / `3a1d3f18` | `evidence/BIO-LOCAL-002-public-read-proof.md` | PASS (retro, replayed) | `closures/BIO-LOCAL-002-006.md` |
| BIO-LOCAL-003 | #480 / `8663aa4` | `evidence/BIO-LOCAL-003-auth-isolation-proof.md` | PASS ×2 (retro, dual) | `closures/BIO-LOCAL-003.md` |
| BIO-LOCAL-004 | #484 / `b2b00fb4` | `evidence/BIO-LOCAL-004-spine-receipt-proof.md` | PASS ×2 (retro, dual; R1 → 013) | `closures/BIO-LOCAL-013.md` (incl. 004 chain) |
| BIO-LOCAL-005 | #488 / `5894578f` | `evidence/BIO-LOCAL-005-guidance-enforcement-proof.md` | PASS + CONDITIONAL (C1 → H1) | `CLOSURES-WAVE3-2026-10-07.md` |
| BIO-LOCAL-006 | #478 / `c6fa4d13` | `evidence/BIO-LOCAL-006-contract-mirror-proof.md` | PASS (retro) | `closures/BIO-LOCAL-002-006.md` |
| BIO-LOCAL-007 | #483 / `77ab065f` | `evidence/BIO-LOCAL-007-seed-gap-inventory.md` | PASS (counts re-derived) | `CLOSURES-WAVE3-2026-10-07.md` |
| BIO-LOCAL-008 | #487 / `35529d8b` | `evidence/BIO-LOCAL-008-batch-a-record.md` | PASS (28/28 verbatim citations) | `CLOSURES-WAVE3-2026-10-07.md` |
| BIO-LOCAL-009 | #489 | `evidence/BIO-LOCAL-009-batch-b-record.md` | PASS (retro, combined) | `closures/BIO-LOCAL-009-010.md` |
| BIO-LOCAL-010 | #494 | `evidence/BIO-LOCAL-010-batch-c-record.md` | PASS (retro, combined; final count re-derived) | `closures/BIO-LOCAL-009-010.md` |
| BIO-LOCAL-011 | #499 / `9947ac1` | `evidence/BIO-LOCAL-011-seed-run-proof.md` | PASS (counts re-derived 100=100) | this record |
| BIO-LOCAL-012 (M1) | #493 | Gate 2 contract record | PASS | `closures/BIO-LOCAL-003.md` (M1 entry) |
| BIO-LOCAL-013 (R1) | #497 | Gate 2 contract record | PASS ×2 (dual) | `closures/BIO-LOCAL-013.md` |
| BIO-LOCAL-014 (D-D) | #500 | `evidence/BIO-LOCAL-014-identity-consolidation.md` | in flight (`bio_local_014_review_1`) | pending review return |

Scenario, environment (always `local` + compose/service + SQLite/stub/inbox posture), exact commit, config class (Development; secrets blank/`example`), UTC time, command/procedure, result, durable artifact path/link, limitations, redaction confirmation. Template lives in each BIO-LOCAL spec's `## Verification` + `## Evidence Required`.

## Per-scenario status

See `SCENARIOS.md`. LS1–LS10: `unverified` (no pinned local run at `e5b75e0` exists yet). LS11: `passing` at assessment level, re-proven per parcel. LS12–LS13: `unverified` (parcels 007–011 pending Gate 2 after lint).

## What was deliberately NOT run

No `docker compose up/build`, no `dotnet test`, no `npm` suite, no `uv pytest`, no `--check` mirror run, no live HTTP against `:5000`/`:3043` — those are parcel-001/002/005/006's deterministic passes, executed in isolated worktrees under the charter's green chain, not as ambient assessment side effects. No production-shaped compose, no Azure, no Stripe, no SMTP, no Postgres — out of scope (D1).
