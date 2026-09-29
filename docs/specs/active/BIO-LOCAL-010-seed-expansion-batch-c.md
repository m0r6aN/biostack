---
ticket: BIO-LOCAL-010
title: Seed expansion batch C (remainder to exactly 150)
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

# BIO-LOCAL-010 — Seed expansion batch C

## Goal

Append batch-C records (007's allocation remainder) to land the corpus at EXACTLY 150, same draft-only, claim-traced bar.

## Initiative

`biostack-local-readiness` (D10).

## Project Track

T3 seed-expansion.

## Wave

proof (blocked on 007 verdict + 008 + 009 merges; rebases onto 009).

## Branch

`proof/bio-local-010-seed-batch-c`

## Worktree

`D:\Repos\BioStack.BIO-LOCAL-010`

## Dependencies

BIO-LOCAL-007 (allocation), BIO-LOCAL-008 + 009 (merged).

## Integration Surfaces

T3 corpus (C6).

## Security Gate

SG-L8 (primary). Dual review; same probes as 008/009, plus exact-count scrutiny.

## Intent

Close the gap: after C, the file holds exactly 150 schema-valid records — no more, no fewer. Batch C takes the thinnest-evidenced IDs, so explicit evidence gaps (never padded claims) are the expected norm here, and the batch record must read that way.

## Constraints

- Same discipline as 008/009, plus the exact-count rule: final count MUST equal 150. If 007's allocation arithmetic drifts (A+B sizes differed from plan), batch C absorbs the difference by owner/coordinator-confirmed ID adjustment — recorded, never silent. Any adjustment beyond 007's reserve list → stop + ruling.
- Pure addition over the 009-merge; all prior records untouchable.

## Acceptance Criteria

1. Full-file validation passes; count equals EXACTLY 150.
2. Every batch-C record draft/needsReview/inactive/lastChangeType-seed; zero exceptions; file-wide re-assert (spot-check prior batches untouched).
3. Claim-trace table complete; gaps explicit where thin; zero untraced claims.
4. Canonical IDs unique file-wide (150); collisions recorded, never merged.
5. Diff vs 009-merge is pure addition; `git diff --check` clean.
6. Batch record written, including the exact-count reconciliation (|A| + |B| + |C| = 93) and any allocation adjustment with its ruling reference.

## Out of Scope

- Test-expectation updates (011), seed-run/serving proof (011), review/promotion, acquisition, any edit to the 57 + A + B records.

## Existing Patterns To Follow

- 008/009 merged records — shape precedent; 007 allocation — ID authority (plus reserve list for count reconciliation).

## Contract

C6. Corpus size lands at exactly 150.

## Required Tests

Validator pass (mandatory, full file). Lane run to record final pre-011 impact. No new persistent tests.

## Allowed Files

- `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`
- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-010-batch-c-record.md`

## Forbidden

- Anything outside Allowed Files; claims without packet lines; active/published flips; auto-merges; editing prior records; landing at ≠150 without a recorded ruling.

## Verification

- Validator pass + exact-count 150 + file-wide uniqueness; trace audit; pure-addition proof; reconciliation arithmetic (|A|+|B|+|C|=93); lane run recorded; `git diff --check`.
- Success: AC1–AC6 hold.

## Evidence Required

- Seed-file diff + batch record with reconciliation; PR link + LS13-part row + SG-L8 evidence.

## Collision Risk

High. Final serial writer of the seed file. No parallel seed edits.

## PR Notes

- What changed: +|C| seed records (lands 150); batch record with reconciliation.
- Why: LS13 (part) + SG-L8 + D10.
- Risk: exact-count pressure tempts padding or quiet adjustment — reconciliation + ruling refs are the control.
- Verification: validator + count-150 + trace sample + addition proof.
- Evidence: batch record path + diff link.

## Session Handoff

- Starting commit: / Ending commit: / Files changed: / Commands run: / Tests passed: / Tests failed: / Decisions needed: / Blockers: / Next safe action: / Do not touch:

## Stop-and-Report Rule

Count cannot land at 150 within the allocation (+reserve), any required claim is untraceable, or any pressure to flip active/published → stop, report, request ruling. Never force the number.

## Verification Plan

Reviewer focus questions:

- Does the file hold EXACTLY 150 validated records (independent recount), or 150 ± drift?
- Is the reconciliation arithmetic exact and every adjustment ruling-referenced?
- Are C's thin records honestly gapped, and are prior batches byte-identical?

## Context & References

- `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`
- `backend/src/BioStack.KnowledgeWorker/Schemas/substance-record.schema.json`
- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-007-seed-gap-inventory.md`
- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-008-batch-a-record.md`
- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-009-batch-b-record.md`
- `docs/INITIATIVES/biostack-local-readiness/SCENARIOS.md` (LS13)
