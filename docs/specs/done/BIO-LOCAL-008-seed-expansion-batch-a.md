---
ticket: BIO-LOCAL-008
title: Seed expansion batch A (best-evidenced IDs)
status: active
owner: clinton.morgan
created: 2026-09-18
updated: 2026-09-18
supersedes: null
superseded_by: null
risk: elevated
surfaces:
  - backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json
  - backend/src/BioStack.KnowledgeWorker/Schemas/substance-record.schema.json
routing_class: architecture/risk
data_classification: internal
---

# BIO-LOCAL-008 — Seed expansion batch A

## Goal

Append batch-A seed records (007's allocation, ~31 best-evidenced IDs) to the corpus, schema-valid, draft-only, claim-traced.

## Initiative

`biostack-local-readiness` (D10).

## Project Track

T3 seed-expansion.

## Wave

proof (blocked on 007 `reachable-150` verdict; strictly first writer of the seed file).

## Branch

`proof/bio-local-008-seed-batch-a`

## Worktree

`D:\Repos\BioStack.BIO-LOCAL-008`

## Dependencies

BIO-LOCAL-007 (allocation + verdict). If 007 verdict is `unreachable`, this parcel DOES NOT START (owner ruling required).

## Integration Surfaces

T3 corpus (C6).

## Security Gate

SG-L8 (primary). Dual adversarial review (claim-content). Hostile probes: claim-without-source, inferred mechanism/safety, active/published flip, auto-merged collision, duplicate canonical ID across batches.

## Intent

Grow the corpus by batch A with zero invented knowledge: each new record compiles ONLY what existing repo evidence packets already contain for that canonical ID, marks everything else unknown-honest, and ships `reviewStatus: draft` + `needsReview: true` + `isActive: false` + `completeness: partial` + `ops.lastChangeType: seed`. Batch A takes the best-evidenced IDs so the pipeline's first signal is the strongest.

## Constraints

- Author EXACTLY the batch-A IDs from 007's allocation — no substitutions, no additions, no removals. Allocation mismatch → stop.
- Every claim sentence in `evidence.claimSpecificEvidence` traces to an existing packet source line (packet file + line recorded in the batch record). No packet line → no claim → field stays unknown-honest.
- Unknown-honest patterns (mirror the semaglutide-record shape): mechanism "has not been reviewed", regulatory "not authoritatively established", empty arrays where unevidenced. Never infer from name, class, or regulatory status.
- `identity.canonicalId` unique across the whole file (existing 57 + batch A); slugs/aliases follow existing conventions; external identifiers recorded only where the packet states them, else null.
- Preserve file determinism: append in 007's allocation order (documented); no reformatting of existing 57 records (diff must show pure addition).
- No source acquisition, no browsing, no network, no model-generated claims beyond deterministic compilation from cited packet lines (record ModelInvoked-equivalent posture in the batch record: compiled-from-packet-lines only).

## Acceptance Criteria

1. Seed file validates: `SubstanceRecordValidator` (or schema) passes on the full file (57 + A); record count equals 57 + |A|.
2. Every batch-A record carries `reviewStatus: draft`, `needsReview: true` with reasons, `isActive: false`, `lastChangeType: seed`; zero exceptions.
3. Claim-trace table complete: each claim → packet file:line; reviewer spot-checks pass; zero untraced claims.
4. No duplicates: canonical IDs unique; no collision with existing seed IDs; any identity-token collision recorded (never merged).
5. Diff is pure addition (no existing-record edits); `git diff --check` clean.
6. Batch record + evidence file written; KnowledgeWorker full test lane run recorded (expected: count-test failures vs frozen 57 — RECORDED as known-pending-011, not fixed here).

## Out of Scope

- Batches B/C (009/010) — this parcel touches only batch-A IDs.
- Updating frozen count-test expectations (011 owns it; 008 records the expected failures).
- Any review/promotion decision, publication, API/UX change, source acquisition.
- Reformatting, reordering, or "improving" existing records.

## Existing Patterns To Follow

- `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json` (semaglutide record) — the draft/unknown-honest shape to mirror.
- `backend/src/BioStack.KnowledgeWorker/Schemas/substance-record.schema.json` — the validation bar.
- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-007-seed-gap-inventory.md` — the allocation (authoritative ID list).

## Contract

C6 (stated, not changed). Record shape conforms; corpus size moves 57 → 57+|A|.

## Required Tests

Schema/validator pass on the full seed file (mandatory, command + output saved). Full KnowledgeWorker lane run to RECORD (not fix) count-test impact. No new persistent tests here.

## Allowed Files

- `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`
- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-008-batch-a-record.md`

## Forbidden

- Touching any file outside Allowed Files (especially tests, schemas, loaders, API, frontend).
- Authoring claims without packet lines; flipping any record active/published; auto-merging collisions.
- Editing, reordering, or reformatting existing seed records.
- Starting if 007 verdict is not `reachable-150`.

## Verification

- Validator/schema pass on full file (exact command + output).
- Count assertion (57 + |A|) + uniqueness check (command + output).
- Claim-trace audit (reviewer replays a sample of packet file:line refs).
- Diff review: `git diff --stat` + proof of pure addition.
- KnowledgeWorker lane run (record failures verbatim as known-pending-011).
- `git diff --check`.
- Success: AC1–AC6 hold.

## Evidence Required

- Seed-file diff + batch record (per-record source trace, unknown-field statement, collision notes).
- PR link + LS13-part row + SG-L8 evidence for coordinator merge.

## Collision Risk

High. Seed file is a serialization point — 008 is the first writer; 009/010 rebase onto its merge. No parallel seed edits.

## PR Notes

- What changed: +|A| seed records; batch record.
- Why: LS13 (part) + SG-L8 + D10.
- Risk: claim-content — dual review mandatory; any untraced claim fails the parcel.
- Verification: reviewer replays validator + trace sample + diff-addition proof.
- Evidence: batch record path + diff link.

## Session Handoff

- Starting commit: / Ending commit: / Files changed: / Commands run: / Tests passed: / Tests failed: / Decisions needed: / Blockers: / Next safe action: / Do not touch:

## Stop-and-Report Rule

Allocation mismatch, untraceable claim required for completeness, forced active/published state, collision demanding a merge decision, or 007-verdict absence → stop, report, request coordinator/owner ruling. Never pad a thin record with inference.

## Verification Plan

Reviewer focus questions (dual review; coordinator reproduces disputes):

- Is EVERY claim sentence backed by a cited packet line, or do some claims ride on the compound's name/class?
- Is the diff purely additive at the byte level for existing records (hash-compare), or did formatting drift?
- Does any record smuggle `isActive: true`, a missing `needsReview`, or a confident field where the packet is silent?

## Context & References

- `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`
- `backend/src/BioStack.KnowledgeWorker/Schemas/substance-record.schema.json`
- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-007-seed-gap-inventory.md`
- `docs/INITIATIVES/biostack-local-readiness/SCENARIOS.md` (LS13)
