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

Scenario, environment (always `local` + compose/service + SQLite/stub/inbox posture), exact commit, config class (Development; secrets blank/`example`), UTC time, command/procedure, result, durable artifact path/link, limitations, redaction confirmation. Template lives in each BIO-LOCAL spec's `## Verification` + `## Evidence Required`.

## Per-scenario status

See `SCENARIOS.md`. LS1–LS10: `unverified` (no pinned local run at `e5b75e0` exists yet). LS11: `passing` at assessment level, re-proven per parcel. LS12–LS13: `unverified` (parcels 007–011 pending Gate 2 after lint).

## What was deliberately NOT run

No `docker compose up/build`, no `dotnet test`, no `npm` suite, no `uv pytest`, no `--check` mirror run, no live HTTP against `:5000`/`:3043` — those are parcel-001/002/005/006's deterministic passes, executed in isolated worktrees under the charter's green chain, not as ambient assessment side effects. No production-shaped compose, no Azure, no Stripe, no SMTP, no Postgres — out of scope (D1).
