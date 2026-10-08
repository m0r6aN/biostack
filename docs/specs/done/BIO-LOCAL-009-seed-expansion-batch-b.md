---
ticket: BIO-LOCAL-009
title: Seed expansion batch B (mid-evidence IDs)
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

# BIO-LOCAL-009 — Seed expansion batch B

## Goal

Append batch-B seed records (007's allocation, ~31 mid-evidence IDs) onto 008's merge, same draft-only, claim-traced bar.

## Initiative

`biostack-local-readiness` (D10).

## Project Track

T3 seed-expansion.

## Wave

proof (blocked on 007 verdict + 008 merge; rebases onto 008).

## Branch

`proof/bio-local-009-seed-batch-b`

## Worktree

`D:\Repos\BioStack.BIO-LOCAL-009`

## Dependencies

BIO-LOCAL-007 (allocation), BIO-LOCAL-008 (merged; rebase onto it first).

## Integration Surfaces

T3 corpus (C6).

## Security Gate

SG-L8 (primary). Dual review; same hostile probes as 008.

## Intent

Continue the corpus growth with identical discipline: batch-B IDs only, claims traced to existing packet lines, everything else unknown-honest, all records draft/needsReview/inactive. Mid-evidence IDs will carry thinner claims and explicit evidence gaps — that honesty is the point.

## Constraints

- Same as 008, plus: fork from 008's merge commit (rebase immediately before PR); batch-B IDs exactly per 007 allocation; cross-check uniqueness against 57 + A + B combined.
- Thin records ship with explicit `evidence.evidenceGaps` entries — never padded.
- Pure addition again: 008's records are now existing records (untouchable).

## Acceptance Criteria

1. Full-file validation passes; count equals 57 + |A| + |B|.
2. Every batch-B record draft/needsReview/inactive/lastChangeType-seed; zero exceptions.
3. Claim-trace table complete; spot-checks pass; zero untraced claims; gaps explicit where thin.
4. No duplicates across 57 + A + B; collisions recorded, never merged.
5. Diff vs 008-merge is pure addition; `git diff --check` clean.
6. Batch record written; KnowledgeWorker lane impact recorded (still known-pending-011).

## Out of Scope

- Batches A/C, test-expectation updates (011), review/promotion, acquisition, reformatting existing records (now including A's).

## Existing Patterns To Follow

- 008's merged batch-A records — the in-corpus precedent for shape discipline.
- Seed schema + 007 allocation (authoritative ID list for B).

## Contract

C6. Corpus size moves 57+|A| → 57+|A|+|B|.

## Required Tests

Validator pass (mandatory). Lane run to record impact. No new persistent tests.

## Allowed Files

- `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`
- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-009-batch-b-record.md`

## Forbidden

- Anything outside Allowed Files; claims without packet lines; active/published flips; auto-merges; editing 57 + A records; starting before 008 merges.

## Verification

- Validator pass + count (57+|A|+|B|) + uniqueness check; trace audit sample; pure-addition diff proof; lane run recorded; `git diff --check`.
- Success: AC1–AC6 hold.

## Evidence Required

- Seed-file diff + batch record; PR link + LS13-part row + SG-L8 evidence.

## Collision Risk

High. Seed file serialized; rebases onto 008. No parallel seed edits.

## PR Notes

- What changed: +|B| seed records; batch record.
- Why: LS13 (part) + SG-L8 + D10.
- Risk: thinner claims than A — gaps must read as gaps.
- Verification: validator + trace sample + addition proof.
- Evidence: batch record path + diff link.

## Session Handoff

- Starting commit: / Ending commit: / Files changed: / Commands run: / Tests passed: / Tests failed: / Decisions needed: / Blockers: / Next safe action: / Do not touch:

## Stop-and-Report Rule

Same as 008. Never pad thin records.

## Verification Plan

Reviewer focus questions:

- Are thin records honestly thin (explicit gaps), or padded with low-confidence claims?
- Does uniqueness hold across the COMBINED file, not just within B?
- Is 008's content byte-identical post-rebase (no accidental edits)?

## Context & References

- `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`
- `backend/src/BioStack.KnowledgeWorker/Schemas/substance-record.schema.json`
- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-007-seed-gap-inventory.md`
- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-008-batch-a-record.md`
- `docs/INITIATIVES/biostack-local-readiness/SCENARIOS.md` (LS13)
