---
ticket: KEO-TBD
title: Pairwise P4 — first sourced negative relationship, end to end
status: draft
owner: clinton.morgan
created: 2026-09-17
updated: 2026-09-17
supersedes: null
superseded_by: null
risk: standard
surfaces:
  - docs/specs/active/KEO-TBD-pairwise-p4-first-sourced-negative-pair.md
  - research/input/relationships/
  - research/review-decisions/
routing_class: implementation/standard
permission_profile: builder-standard
data_classification: internal
---

# KEO-TBD-pairwise-P4 — First sourced negative relationship, end to end

## Intent

Author exactly one real negative relationship record against P1's ratified bar,
carry it through the existing review-decision pipeline, and demonstrate it
reaching the knowledge projection P2 wired. One genuine pair proves the whole
chain — contract, extraction, review, promotion — before any effort is spent
authoring at scale. This is the vertical slice the owner chose on 2026-09-17
over building plumbing that might sit empty.

## Constraints

- Exactly one relationship record. Not two, not a batch. The parcel's value is
  proof of the chain, and a second record adds cost without adding proof.
- The pair is selected from P0's census on measured grounds: a compound already
  promoted and live, whose interaction is stated in an authorized source. If
  P0's census yields no such pair, stop and report — inventing one to complete
  the slice is the failure this lane exists to prevent.
- No new source acquisition or authorization. The supporting source must already
  sit in an authorized lane. Source-rights doctrine applies: paraphrase, exact
  page locators, and `quote: null` wherever authorship is unproven.
- Verbatim page-image verification is load-bearing. Fetch-extraction layers in
  this repository have fabricated quotes before; a quote that cannot be verified
  against the page image is not used.
- The author does not review their own record. Independent review is recorded
  through the normal review-decision pipeline with no bypass.
- Promotion and the live Refresh remain owner-only. The parcel may produce a
  dry-run plan; it may not run a live Refresh.
- No other evidence packet, seed record, or review decision is modified.

## Acceptance Criteria

1. One relationship record exists in `research/input/relationships/`, validates
   against `relationship-packet.schema.json`, and satisfies every clause of
   P1's bar.
2. Its `sourceRefs` resolve to an authorized-lane source, and the supporting
   extraction is page-locator accurate and verified against the page image.
3. An independent review is recorded as a review decision through the normal
   pipeline, authored by someone other than the record's author.
4. The record reaches the knowledge projection via P2's path, demonstrated
   without modifying P2.
5. A gated Refresh dry-run plan shows the expected change confined to the single
   affected compound, with the per-record table attached to the receipt.
6. `git status` shows no modification to any other packet, seed, or decision.
7. The receipt states plainly what remains unproven: one pair is not coverage.

## Out of Scope

- Authoring a second pair, or any bulk authoring pass.
- Positive / synergy relationships.
- Running the live Refresh or promoting anything (owner-only).
- Rendering the pair to a user (P5).
- Changing P1's bar, P2's wiring, or P3's quarantine because the chosen pair is
  inconvenient — a pair that does not fit is reported, not accommodated.
- Acquiring, licensing, or authorizing any new source.

## Context & References

- P0 census: `research/output/pairwise-lane-20260917/p0-label-interaction-census.md`
- P1 ratification: `docs/guidance/pairwise-relationship-publication-contract.v1.md`
- `backend/src/BioStack.KnowledgeWorker/Schemas/relationship-packet.schema.json`
- `research/review-decisions/` and the promotion gate in
  `backend/src/BioStack.KnowledgeWorker/Pipeline/PromotionGate.cs`
- `docs/operations/knowledge-worker-refresh.md`
- Source-rights doctrine and the fabricated-quote lesson recorded in
  `research/output/gtm-activation-20260909/` source-authorization receipts.

## Allowed Files

- `research/input/relationships/pairwise-p4-first-negative.relationship.json`
- `research/output/pairwise-lane-20260917/p4-authoring-and-review.md`

## Verification Plan

Reviewer focus questions:

- Is the supporting source genuinely in an authorized lane, or adjacent to one?
- Was every extracted string verified against the page image, or was any of it
  taken from a fetch-extraction layer?
- Does the record satisfy each clause of P1's bar independently, or does it pass
  only on a generous reading of one clause? Name the weakest.
- Would this pair have been selected if the lane needed a result, regardless of
  evidence? Argue the case against its selection.
