---
ticket: BIO-LOCAL-011
title: Seed-run and serving proof (150 ingested + served)
status: active
owner: clinton.morgan
created: 2026-09-18
updated: 2026-09-18
supersedes: null
superseded_by: null
risk: elevated
surfaces:
  - backend/tests/BioStack.KnowledgeWorker.Tests/CorpusIdentityInventoryBuilderTests.cs
  - backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationReportBuilderTests.cs
  - backend/src/BioStack.KnowledgeWorker/Jobs/SeedJob.cs
routing_class: architecture/risk
data_classification: internal
---

# BIO-LOCAL-011 — Seed-run and serving proof

## Goal

Prove 150 seeded compounds end to end locally: SeedJob ingests 150, the knowledge API serves 150, and the frozen count tests assert the new values explicitly.

## Initiative

`biostack-local-readiness` (D10).

## Project Track

T3/T2 seed-run-proof.

## Wave

proof (blocked on 007–010; after 010 merges).

## Branch

`proof/bio-local-011-seed-run-proof`

## Worktree

`D:\Repos\BioStack.BIO-LOCAL-011`

## Dependencies

BIO-LOCAL-007 (projection cross-check), 008/009/010 (merged corpus at 150), 001 (boot), 002/003 (serving baseline honesty: draft records must not read as published).

## Integration Surfaces

T3 corpus + L2 serving (C6).

## Security Gate

SG-L8 (partial: serving layer exposes draft status honestly — unreviewed records must never present as reviewed/published). Dual review (count-expectation changes are high-trust edits).

## Intent

Close D10 with a live local chain: run the inventory builder (observed values), run SeedJob locally (ingest 150 with `lastChangeType=seed`), confirm the knowledge API serves 150 compounds, and update the two frozen count-asserting tests to the OBSERVED values (cross-checked against 007's projection) — explicitly, in one parcel, with nothing else changed. Also confirm the test `Fixtures/substances-seed.json` remains an independent synthetic sample (untouched, still passing).

## Constraints

- Test-expectation updates are set EXACTLY to observed builder/report values; any observed-vs-007-projection mismatch → stop + reconcile (do not force either side).
- Only the two count-test files change, and only their count literals (+ any directly derived overlap lists the tests pin, e.g. seed-only IDs — updated to observed, itemized in the parcel record). No logic edits to tests, builder, schema, loader, or SeedJob.
- Seed run is local-only (SQLite dev volume or parcel-local DB — record which); no Postgres, no promotion, no Refresh, no staging/prod data.
- Serving proof must show draft status honesty: note how unreviewed records present (or are withheld) on public vs authenticated surfaces; any published-reading of a draft record is an SG-L8 finding, not a pass.
- If the seed run or serving count is not 150 → stop (reopen 008–010 chain via coordinator), do not adjust tests to a wrong number.

## Acceptance Criteria

1. `CorpusIdentityInventoryBuilder().Build()` observed on the merged 150-corpus: SeedRecordCount == 150; all derived counts recorded; 007 projection cross-check attached (match, or mismatch reconciled with ruling).
2. Local SeedJob run ingests 150 records (`lastChangeType=seed` stamped; DryRun posture recorded; command + log saved).
3. Local knowledge API serves 150 compounds (`GET /api/v1/knowledge/compounds` count == 150; transcript saved) with draft-status honesty noted per SG-L8.
4. `CorpusIdentityInventoryBuilderTests` + `StructuralEvaluationReportBuilderTests` updated to observed values ONLY; full KnowledgeWorker lane green (record counts); `IngestionPipelineTests` + `SubstanceRecordValidatorTests` still green on the untouched synthetic fixture.
5. Parcel record + evidence file written; `git diff --check` clean; diff shows exactly: two test files (count literals only) + record/evidence files.

## Out of Scope

- Authoring or editing any seed record (008–010 owned the corpus; 011 that is closed).
- Any builder/schema/loader/SeedJob logic change; any promotion/review decision; any publish-surface change.
- Postgres, Refresh, deployment, billing, email.

## Existing Patterns To Follow

- `backend/tests/BioStack.KnowledgeWorker.Tests/CorpusIdentityInventoryBuilderTests.cs` — the frozen expectations being moved (read the full pin list before editing).
- `backend/src/BioStack.KnowledgeWorker/Jobs/SeedJob.cs` + `Config/WorkerOptions.cs` (`SeedFilePath`, `RunMode.Seed`) — the run mechanism under proof.
- 007's inventory evidence — the projection cross-check.

## Contract

C6 (observed, then pinned in tests). Corpus size proven at 150 in file, ingestion, serving, and test expectations together.

## Required Tests

Full `BioStack.KnowledgeWorker.Tests` lane green (mandatory, counts recorded) + focused inventory/report/ingestion/validator filters (commands + outputs saved).

## Allowed Files

- `backend/tests/BioStack.KnowledgeWorker.Tests/CorpusIdentityInventoryBuilderTests.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationReportBuilderTests.cs`
- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-011-seed-run-proof.md`

## Forbidden

- Editing any seed, schema, loader, builder, SeedJob, fixture, API, frontend, or contract file.
- Setting test expectations to anything but observed values; editing test logic; "fixing" a count mismatch by adjusting inputs.
- Running ingestion against shared, staging, or production state.

## Verification

- Builder observed output + 007 cross-check (commands + values saved).
- SeedJob local run (exact command + log excerpt + ingested-count proof).
- Served-count transcript (`compounds` endpoint count == 150) + draft-honesty note.
- Full lane green (counts) + fixture-based tests green (untouched fixture proof via `git diff --name-only`).
- `git diff --check` + diff-scope proof (exactly the Allowed Files).
- Success: AC1–AC5 hold.

## Evidence Required

- Evidence file (observed-vs-projected table, run log, serving transcript, test outputs, diff scope).
- PR link + LS13 row (CLOSES LS13) + SG-L8 (partial) for coordinator merge; coordinator flips `corpus-seeded` gate on this parcel's merge.

## Collision Risk

High. Count-test expectations are serialization points — 011 is the sole writer; no other parcel touches tests.

## PR Notes

- What changed: two test files (count literals → observed) + evidence.
- Why: LS13 close + `corpus-seeded` gate + D10.
- Risk: expectation edits are high-trust — observed-only rule + cross-check are the controls.
- Verification: reviewer replays builder + seed run + full lane from the evidence file.
- Evidence: evidence file path.

## Session Handoff

- Starting commit: / Ending commit: / Files changed: / Commands run: / Tests passed: / Tests failed: / Decisions needed: / Blockers: / Next safe action: / Do not touch:

## Stop-and-Report Rule

Observed ≠ 150 anywhere (file, ingestion, serving), observed ≠ 007 projection without reconcilable cause, serving exposes drafts as published, or any file outside Allowed Files needs changing → stop, report, request ruling. Never tune inputs to fit expectations or vice versa.

## Verification Plan

Reviewer focus questions (dual review; coordinator reproduces disputes):

- Are the new test literals transcribed from the SAVED observed output (character-exact), or hand-typed?
- Does the served count come from a live local run against the 150-corpus DB (not the fixture, not staging)?
- Is the draft-honesty note a real surface check (drafts withheld-or-labeled), or an assertion?

## Context & References

- `backend/tests/BioStack.KnowledgeWorker.Tests/CorpusIdentityInventoryBuilderTests.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationReportBuilderTests.cs`
- `backend/src/BioStack.KnowledgeWorker/Jobs/SeedJob.cs`
- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-007-seed-gap-inventory.md`
- `docs/INITIATIVES/biostack-local-readiness/SCENARIOS.md` (LS13)
