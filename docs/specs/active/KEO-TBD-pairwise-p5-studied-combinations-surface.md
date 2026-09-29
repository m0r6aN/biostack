---
ticket: KEO-TBD
title: Pairwise P5 — public studied-combinations surface
status: draft
owner: clinton.morgan
created: 2026-09-17
updated: 2026-09-17
supersedes: null
superseded_by: null
risk: standard
surfaces:
  - docs/specs/active/KEO-TBD-pairwise-p5-studied-combinations-surface.md
  - frontend/src/components/knowledge/
  - backend/src/BioStack.Contracts/
  - docs/guidance/RATIFICATION.md
routing_class: standard-feature
permission_profile: builder-standard
data_classification: internal
---

# KEO-TBD-pairwise-P5 — Public studied-combinations surface

## Intent

Render sourced negative relationships publicly on the compound dossier and the
compounds side panel, each showing its source and evidence tier, as Class A
observation under P1's ratified contract. This is the surface the owner asked
for on 2026-09-16 — interactions visible in the free public library — delivered
only for pairs that clear the bar. It closes the vertical slice by making P4's
single pair visible to a signed-out visitor.

## Constraints

- Only records satisfying P1's bar render. The projection boundary decides;
  the component does not filter, and never renders a record the backend sent
  in error.
- Observation phrasing only, per P1's permitted table. Forbidden phrasing does
  not appear even in aria labels, tooltips, empty states or test fixtures.
- Every rendered pair displays its source and evidence tier. A pair without
  both does not render.
- Absence never reassures. A compound with no qualifying records renders no
  section at all rather than an empty state implying nothing interacts, and no
  copy anywhere may suggest that absence of a record means absence of an
  interaction.
- This surface is public and distinct from the per-pair *reasoning* gated to
  `reviewed_relationship_graph` by the 2026-09-16 ruling. Sourced records are
  public evidence; unsourced inference stays gated. The implementation must not
  blur the two, and must not route sourced records through the reasoning gate.
- All new user-visible strings are registered in the conversion-copy allowlist
  and pass the banned-word guards, including negated uses.
- The calm, technical dark surface and reduced-motion handling are preserved. No
  new dependency. No new `react-hooks/set-state-in-effect` violation.
- Honest coverage: the surface states how many of the library's compounds carry
  any sourced record, rather than implying the set is complete.

## Acceptance Criteria

1. P4's pair renders on the dossier and in the compounds side panel, with source
   and evidence tier visible, to a signed-out visitor.
2. A compound with no qualifying record renders no section and no reassurance
   copy; a test asserts the absence of both.
3. A record missing a source or tier does not render; a test covers it.
4. No rendered string, in any state, matches the banned-word or prescriptive
   guards; all new copy is registered.
5. The public surface does not consult the reasoning gate, and a test asserts a
   signed-out visitor sees the sourced record while still seeing no unsourced
   reasoning.
6. Rendering is correct at 375px and 1280px, and the section degrades to a
   readable block rather than chips when a statement is long.
7. A RATIFICATION entry records the surface going live under P1's contract.
8. Frontend suite, lint and typecheck show no regression against baseline.

## Out of Scope

- The Operator relationship graph and protocol-console callouts, already gated.
- Positive / synergy rendering.
- Bulk authoring or coverage expansion (a later research lane).
- Any change to P1's bar or P3's quarantine to make more content renderable.
- Marketing or landing-page copy.

## Context & References

- P1 ratification: `docs/guidance/pairwise-relationship-publication-contract.v1.md`
- P4 record and review receipt.
- `frontend/src/components/knowledge/CompoundIntelligenceCard.tsx`
- `frontend/src/__tests__/conversion/conversionCopy.test.ts`
- `frontend/src/__tests__/conversion/launchSafetyCopy.test.ts`
- The 2026-09-16 ruling and RATIFICATION entries from PRs #369, #370, #372.
- Positioning constraints: no safety promise, no expertise gating.

## Allowed Files

- `frontend/src/components/knowledge/CompoundIntelligenceCard.tsx`
- `frontend/src/components/knowledge/StudiedCombinations.tsx`
- `frontend/src/__tests__/components/StudiedCombinations.test.tsx`
- `frontend/src/__tests__/conversion/conversionCopy.test.ts`
- `docs/guidance/RATIFICATION.md`

## Verification Plan

Reviewer focus questions:

- Read every string as a recommendation. Which one survives that reading?
- Can a signed-out visitor reach any unsourced reasoning through this surface?
- Does the no-records case imply safety anywhere, including in what it omits?
- Is the coverage statement honest about how few compounds carry records, or
  does it imply completeness?
- Does the component filter, rather than trusting the boundary?
