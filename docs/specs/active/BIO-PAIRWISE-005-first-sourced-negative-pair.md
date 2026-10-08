---
ticket: BIO-PAIRWISE-005
title: Pairwise P4 — first sourced negative relationship, end to end
status: review-candidate
revision: 1
owner: clinton.morgan
created: 2026-09-17
updated: 2026-10-08
supersedes: null
superseded_by: null
risk: standard
delivery_classes: [knowledge-promotion]
guidance_classes: [not-applicable]
substance_function_risk: [not-applicable]
function_review_status: not-applicable
reviewers_required: 2
surfaces:
  - docs/specs/active/BIO-PAIRWISE-005-first-sourced-negative-pair.md
  - research/input/relationships/pairwise-p4-first-negative.relationship.json
  - research/review-decisions/
  - research/output/pairwise-lane-20260917/p4-authoring-and-review.md
routing_class: implementation/knowledge-promotion
---

# BIO-PAIRWISE-005 — First sourced negative relationship, end to end

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
  P0's census yields no such pair under the sourcing split below, stop and
  report — inventing one to complete the slice is the failure this lane exists
  to prevent.
- **Sourcing split (D-E(1), ratified 2026-10-08):**
  - **Permitted — derivation from already-authorized, already-cited lane
    sources.** Extracting a new claim from a source already sitting in an
    authorized lane and already cited in `research/input/evidence/` packets
    (for example, a DailyMed product label already present as a
    `product-label`-classified `sourceId`) is **derivation, not acquisition**.
    This is the same verbatim-citation derivation doctrine D-C applies to the
    corpus: the claim text must be copied verbatim from the cited source, the
    page/section locator must be exact, and page-image verification (below)
    is part of this derivation step, not a separate sourcing event.
  - **Stops the parcel — new external source acquisition.** If no interaction
    meeting P1's bar can be derived from a source already in an authorized
    lane, acquiring, fetching, or authorizing a *new* external source (one not
    already cited in `research/input/evidence/`) is out of scope. That gap
    invokes the KEO-73/74 (new-sourcing) gates and is not resolved inside this
    parcel. The parcel stops and reports the gap to the coordinator rather
    than acquiring a new source to complete the slice.
  - Source-rights doctrine applies throughout: paraphrase, exact page
    locators, and `quote: null` wherever authorship is unproven.
- Verbatim page-image verification is load-bearing. Fetch-extraction layers in
  this repository have fabricated quotes before; a quote that cannot be
  verified against the page image is not used. Verified extraction (source
  file, page/section locator, verification date, and the verified text) is
  documented in `p4-authoring-and-review.md`.
- The author does not review their own record. Two independent reviews are
  recorded through the normal review-decision pipeline with no bypass —
  reviewer 2 is a different person from reviewer 1, per the knowledge-promotion
  delivery class's dual-review requirement.
- Promotion and the live Refresh remain owner-only. The parcel may produce a
  dry-run plan; it may not run a live Refresh. Owner sign-off on promotion is
  recorded in the closure evidence before the record is treated as eligible
  for the knowledge projection.
- No other evidence packet, seed record, or review decision is modified.

## Knowledge-Promotion Controls (delivery class, D-E(2))

This parcel is reclassified `knowledge-promotion`: a sourced negative-relationship
record reaching the public knowledge projection is a promotion surface under
CHARTER D14. The knowledge-promotion row's required sections, minimum checks,
reviewer count, and stop conditions apply in full and are bound here.

**Source / license / provenance:** The record's `sourceRefs` resolve to a
source already present in an authorized lane (an existing, already-cited
`research/input/evidence/` packet) under the sourcing split above. Each cited
source's license/authorization status is the one already recorded for that
source in its evidence packet — this parcel does not re-license or re-authorize
anything. Provenance (source file, page/section locator, verification date) is
recorded in `p4-authoring-and-review.md` for every extracted claim.

**Evidence grade:** The record's `evidenceTier` is not `Unknown` and the
supporting `sourceRefs` carry an `authorityTier` of `A1` or `A2`, per
`FieldAuthorityPolicy.cs` and P1's ratified bar. The evidence grade and its
basis are stated in `p4-authoring-and-review.md`.

**Review lifecycle:** Two independent reviews are recorded as review decisions
through the normal `research/review-decisions/` pipeline. Reviewer 1 and
reviewer 2 are different people, neither is the record's author, and both
decisions are `accepted-as-evidence-backed` before the record is treated as
promotion-eligible.

**Promotion authority:** Promotion to the live knowledge projection and the
live Refresh remain owner-only. This parcel produces a dry-run plan only
(AC5); the owner's sign-off on promotion, when it happens, is a separate,
later act recorded outside this parcel's closure evidence, not performed by
this parcel.

**Rollback:** `p4-authoring-and-review.md` documents the rollback path: if the
record is rejected in review, fails schema/sourcing verification, or the
dry-run plan surfaces an unexpected blast radius, the record is removed from
`research/input/relationships/` and no output is written under
`research/output/pairwise-lane-20260917/` beyond the stop-and-report note.
Because the parcel never runs a live Refresh, rollback never touches the
knowledge projection itself.

**Mandatory stop conditions (apply in addition to the stops above):**

- Missing source, license, or review state for the chosen pair — stop and
  report rather than proceeding on a partial record.
- Any attempt to bypass promotion (e.g., running a live Refresh, or treating
  the dry-run plan as equivalent to promotion) — stop; promotion stays
  owner-only per the Constraints.
- An unreviewed public claim — the record must not be represented as
  projection-ready until both independent reviews are recorded as
  `accepted-as-evidence-backed`.

## Acceptance Criteria

1. One relationship record exists in `research/input/relationships/`, validates
   against `relationship-packet.schema.json`, and satisfies every clause of
   P1's bar.
2. Its `sourceRefs` resolve to an authorized-lane source, and the supporting
   extraction is page-locator accurate and verified against the page image.
3. Two independent reviews are recorded as review decisions through the normal
   pipeline, each authored by someone other than the record's author and by a
   different person from the other reviewer (dual review, per the
   knowledge-promotion delivery class).
4. The record reaches the knowledge projection via P2's path, demonstrated
   without modifying P2.
5. A gated Refresh dry-run plan shows the expected change confined to the single
   affected compound, with the per-record table attached to the receipt.
6. `git status` shows no modification to any other packet, seed, or decision.
7. The receipt states plainly what remains unproven: one pair is not coverage.

## Required Tests

1. **T1 — Relationship schema validation.** The record in
   `research/input/relationships/pairwise-p4-first-negative.relationship.json`
   validates against `relationship-packet.schema.json` with zero schema
   errors. Fixture: the record itself, run through the repository's existing
   schema validator.
2. **T2 — Source authority verification.** The record's `sourceRefs[0].sourceId`
   resolves to an existing evidence-packet source whose `authorityTier` is
   `A1` or `A2` (per `FieldAuthorityPolicy.cs`). Assertion: `authorityTier` in
   `{A1, A2}`.
3. **T3 — Evidence tier non-unknown.** The record's `evidenceTier` is not
   `Unknown`. Assertion: one of `{Strong, Moderate, Limited, Anecdotal,
   Insufficient}`.
4. **T4 — Sourced-pair provenance (derivation vs. acquisition).** The cited
   source was already present in `research/input/evidence/` before this
   parcel began (derivation, not acquisition), demonstrated by a file:line
   citation into the pre-existing evidence packet recorded in
   `p4-authoring-and-review.md`.
5. **T5 — Page-image verification.** For every extracted quote or paraphrase,
   `p4-authoring-and-review.md` records the source file, page/section locator,
   verification date, and the verified text, demonstrating the claim was
   checked against the page image and not taken from a fetch-extraction layer.
6. **T6 — Review-status gate.** The record's `relationshipReviewStatus` equals
   `accepted-as-evidence-backed` only after both independent review decisions
   (T8) are recorded; a record with zero or one review decision does not carry
   this status. Demonstrated via the review-decision files and the record's
   final state.
7. **T7 — Dry-run projection without P2 modification.** The record is run
   through the knowledge-worker pipeline in dry-run / research mode without
   modifying P2 code, producing the AC5 receipt with the per-record table.
   `git status` and/or a diff against P2's source files shows zero changes to
   P2.
8. **T8 — Dual review recorded.** Two review-decision entries exist in
   `research/review-decisions/`, authored by two different people, neither of
   whom is the record's author, both with `decision: accepted-as-evidence-backed`.

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
- Is the cited source genuinely already in an authorized lane and already
  cited before this parcel began (derivation), or was it newly fetched or
  authorized by this parcel (acquisition, which the sourcing split forbids)?
- Do both independent reviews exist, come from two different people other
  than the author, and both land on `accepted-as-evidence-backed` before the
  record is treated as promotion-eligible?
