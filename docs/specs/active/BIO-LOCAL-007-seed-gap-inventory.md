---
ticket: BIO-LOCAL-007
title: Seed-gap inventory and batch allocation (150 target)
status: active
owner: clinton.morgan
created: 2026-09-18
updated: 2026-09-18
supersedes: null
superseded_by: null
risk: standard
surfaces:
  - backend/src/BioStack.KnowledgeWorker/Pipeline/CorpusIdentityInventoryBuilder.cs
  - backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json
  - research/input/candidates/peptide-serm-sarm-market-interest.v1.json
  - research/input/evidence
routing_class: standard-feature
data_classification: internal
---

# BIO-LOCAL-007 — Seed-gap inventory and batch allocation

## Goal

Prove whether 150 seeded compounds is reachable from existing repo inputs, and allocate the new canonical IDs into batches A/B/C — or prove unreachable and stop the expansion chain.

## Initiative

`biostack-local-readiness` (D10, ratified at Gate 1).

## Project Track

T3 seed-inventory.

## Wave

proof (needs 001 boot only; may run alongside 002–005 once 001 is green).

## Branch

`proof/bio-local-007-seed-gap-inventory`

## Worktree

`D:\Repos\BioStack.BIO-LOCAL-007`

## Dependencies

BIO-LOCAL-001 (boot posture for any local runs). Blocks 008/009/010/011.

## Integration Surfaces

T3 corpus (inventory only).

## Security Gate

SG-L8 (partial: allocation lists contain identities only — no claim content authored here).

## Intent

Replace the 150-target guesswork with a measured record: current seed (57) / candidates (universe 70 + pilot file) / evidence packets (78) / source-registry posture, computed overlap and reachable total from existing repo inputs WITHOUT new acquisition, and a concrete A/B/C batch allocation (~31 IDs each) that 008–010 execute. If the reachable total is below 150, this parcel PROVES unreachable, stops 008–011, and hands the owner the sourcing decision (KEO-73/KEO-74 gates apply).

## Constraints

- Read-only. No seed record authored, no schema edited, no code changed, no database touched, no deployment, no promotion, no Refresh, no source acquisition, no network fetch.
- Metadata only: canonical IDs, counts, overlap sets, per-ID evidence-availability flags (packet present/absent), collision flags. No claim sentences, no dosing, no URLs beyond file paths already in the repo.
- Run the existing `CorpusIdentityInventoryBuilder` (read path) rather than hand-counting; hand counts cross-check, never substitute.
- Identity collisions follow the existing rule: recorded, never auto-merged.

## Acceptance Criteria

1. Inventory table (measured, not carried): seed count, per-candidate-file counts, evidence-packet count, registry counts (rights/operations/acquisition/authorized), seed↔candidate overlap, evidence-without-candidate list, candidate-without-evidence list, identity-token collisions.
2. Reachability verdict with arithmetic: `57 + <new-traceable> = <reachable>`; verdict is either `reachable-150` (with allocation) or `unreachable-<n>` (with shortfall analysis).
3. If reachable: batch allocation A/B/C listing every new canonical ID, each with its evidence-availability flag and packet reference(s); batches ordered so A contains the best-evidenced IDs (early signal), C the thinnest (explicit gaps expected).
4. If unreachable: shortfall report (which IDs are missing inputs, what sourcing would be required, which KEO-73/74 gates apply) + explicit STOP recommendation for 008–011.
5. Evidence file written; `git diff --check` clean; no file outside the evidence path created, modified, or deleted.

## Out of Scope

- Authoring any seed record or claim (008–010).
- Any source acquisition, approval, activation, or promotion decision.
- Changing the seed schema, validator, loader, inventory builder, or any test expectation.
- Deciding the `/compounds` gating or homepage-panel questions.

## Existing Patterns To Follow

- `backend/src/BioStack.KnowledgeWorker/Pipeline/CorpusIdentityInventoryBuilder.cs` — the deterministic builder under proof (run it, don't reimplement it).
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-75-CORPUS-IDENTITY-INVENTORY-001.md` — the prior inventory's metadata-only discipline + collision rule.
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-75-MARKET-INTEREST-CANDIDATE-UNIVERSE-002.md` — universe boundaries (queue targets, not facts).

## Contract

None (C6 re-stated, not changed). Behavior contract: builder output is deterministic, sorted, timestamp-free, model/network-free.

## Required Tests

Run the existing inventory-related tests as READ posture (`CorpusIdentityInventoryBuilderTests`, full KnowledgeWorker lane optional — record what ran + counts). No test edits here (011 owns expectation updates).

## Allowed Files

- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-007-seed-gap-inventory.md`

## Forbidden

- Editing seeds, candidates, packets, schemas, code, tests, contracts, or any file outside Allowed Files.
- Acquiring sources, browsing for new compounds, inventing claims or IDs.
- Pre-declaring reachability before measuring (the verdict follows the arithmetic).

## Verification

- Run `CorpusIdentityInventoryBuilder` (record exact command + output hash/summary).
- Independent cross-check counts (seed file parse, candidate file parse, evidence dir listing) with commands saved.
- Determinism check: two consecutive builds byte-identical.
- `git diff --check` + `git status --short` (prove read-only: zero tracked modifications).
- Success: AC1–AC5 hold; verdict arithmetic reproduces from the saved commands.

## Evidence Required

- Evidence file (inventory tables, verdict arithmetic, batch allocation or shortfall report).
- PR link + row for LS12 + SG-L8 (partial) for coordinator merge into `VERIFICATION.md`.

## Collision Risk

Low. Read-only parcel; no shared-file writes. Its output (allocation) is consumed by 008–010 — sequencing dependency, not a write collision.

## PR Notes

- What changed: evidence file only.
- Why: LS12 + SG-L8 (partial) + D10 inventory-first order.
- Risk: an unreachable verdict stops four downstream parcels — that is the parcel working as designed, not a failure.
- Verification: reviewer replays builder + cross-check counts.
- Evidence: evidence file path.

## Session Handoff

- Starting commit: / Ending commit: / Files changed: / Commands run: / Tests passed: / Tests failed: / Decisions needed: / Blockers: / Next safe action: / Do not touch:

## Stop-and-Report Rule

If the builder, schema, candidate files, or evidence dir contradict the assessment baseline (57/70/16/78/30) in a way that changes the verdict frame: stop, record observed vs expected, request coordinator ruling. If reachable < 150: record the verdict, recommend STOP for 008–011, do not soften the arithmetic.

## Verification Plan

Reviewer focus questions:

- Is every count produced by a saved command (builder + cross-check), or is any number carried from prior docs?
- Does the batch allocation cover EXACTLY the gap IDs with no duplicates across A/B/C and no overlap with existing seed IDs?
- If unreachable: does the shortfall name the missing inputs and the exact gates that own them, or hand-wave at "more research"?

## Context & References

- `backend/src/BioStack.KnowledgeWorker/Pipeline/CorpusIdentityInventoryBuilder.cs`
- `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`
- `research/input/candidates/peptide-serm-sarm-market-interest.v1.json`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-75-CORPUS-IDENTITY-INVENTORY-001.md`
- `docs/INITIATIVES/biostack-local-readiness/SCENARIOS.md` (LS12)
