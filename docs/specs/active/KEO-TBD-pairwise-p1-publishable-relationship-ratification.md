---
ticket: KEO-TBD
title: Pairwise P1 — publishable relationship subset and public rendering ratification
status: draft
owner: clinton.morgan
created: 2026-09-17
updated: 2026-09-17
supersedes: null
superseded_by: null
risk: critical
surfaces:
  - docs/specs/active/KEO-TBD-pairwise-p1-publishable-relationship-ratification.md
  - docs/guidance/RATIFICATION.md
  - docs/guidance/
routing_class: architecture/risk
permission_profile: builder-architecture
data_classification: internal
---

# KEO-TBD-pairwise-P1 — Publishable relationship subset and rendering ratification

## Intent

Ratify which records under the existing, already-frozen
`relationship-packet.schema.json` may reach a public surface, what bar each must
clear, and exactly how it may be worded. The schema is not the gap; the gap is
that nothing states which of its fourteen `relationshipType` values and seven
`assertionClass` values constitute publishable evidence. Without that
enumeration, wiring "relationships" to the dossier publishes vendor claims and
curator hypotheses as evidence — the precise failure BioStack exists to oppose.
P1 produces a ratification record and changes no code. Its consumers are P2, P4,
P5, and every later reviewer.

## Constraints

- Zero implementation. No schema edit, no code, no migration, no fixture, no
  surface change. The existing schema is authoritative and stays byte-unchanged.
- The publishable set is expressed as an **allowlist**, never a blocklist. A
  `relationshipType` or `assertionClass` not named publishable is unpublishable
  by default, including any value added to the enum later.
- Guidance Content Contract v1 governs. Class D is prohibited outright. The
  rendering contract must land in Class A: sourced observation, never direction.
  Permitted phrasing describes what a source reports; forbidden phrasing tells a
  reader what to do, combine, avoid, take, or adjust.
- Every publishable record requires at least one `sourceRefs` entry drawn from an
  authorized source lane, an `evidenceTier` other than `Unknown`, and a
  `relationshipReviewStatus` the ratification names as sufficient. A record
  failing any of these has no permitted public class at any tier — it is not
  demoted to a lesser public rendering.
- v1 ratifies negative relationships only. Positive and synergy types are named
  as deferred with the reason recorded; deferral is not denial.
- Absence of a relationship record must never render as reassurance. The
  ratification must state how absence is presented, or that it is not presented.
- The 2026-09-16 ruling gating per-pair *reasoning* to `reviewed_relationship_graph`
  governs unsourced inference from `CompoundInteractionHint`. It does not govern
  sourced relationship records, which are public evidence. The ratification must
  state this distinction explicitly so the two are never conflated.
- No credential, customer datum, internal hostname, or environment endpoint
  appears in the record.

## Acceptance Criteria

1. A ratification record enumerates the publishable `relationshipType` values
   for v1 and, separately, every value withheld, each with a one-line reason.
2. The record enumerates publishable `assertionClass` values and explicitly
   names `vendor-claim`, `community-signal`, and `curator-hypothesis` as never
   publishable as evidence.
3. The sourcing bar is stated as a checkable conjunction (authorized lane,
   `evidenceTier != Unknown`, named `relationshipReviewStatus` values, at least
   one `sourceRefs` entry) such that a validator could implement it without
   further interpretation.
4. A permitted/forbidden phrasing table gives at least three permitted
   renderings and at least six forbidden ones, each forbidden entry annotated
   with the contract class it would breach.
5. The record states how a rendered relationship displays its source and
   evidence tier, and how absence is handled.
6. The record states the reasoning-gate distinction required by the constraints
   above in terms a reader cannot misread as reversing the 2026-09-16 ruling.
7. An entry is appended to `docs/guidance/RATIFICATION.md` in the existing
   format, and the two surfaces agree.
8. No file outside `docs/guidance/` and this spec is created or modified, and
   `relationship-packet.schema.json` is byte-unchanged.

## Out of Scope

- Editing `relationship-packet.schema.json` or any schema. If v1 genuinely
  cannot be expressed in the existing fields, stop and report — a schema change
  is a separate ratified parcel, not a P1 decision.
- Positive / synergy relationship types (deferred, named in the record).
- Any code, DTO, projection, migration, test or UI change (P2, P3, P5).
- Authoring relationship records (P4).
- Auto-fetching substances absent from the library and processing them, which
  the owner named as a future direction on 2026-09-17. It is recorded as a
  named future parcel and is not shaped, authorized, or designed here.
- Re-opening the 2026-09-16 reasoning-gating ruling.

## Context & References

- `backend/src/BioStack.KnowledgeWorker/Schemas/relationship-packet.schema.json`
- `research/protocol-intelligence/relationship-taxonomy.json`
- `docs/guidance/biostack-guidance-content-contract.v1.md`
- `docs/guidance/RATIFICATION.md`
- P0 output: `research/output/pairwise-lane-20260917/`
- The 2026-09-16 owner ruling on per-pair reasoning gating and its RATIFICATION
  entries from PRs #369, #370 and #372.

## Allowed Files

- `docs/guidance/pairwise-relationship-publication-contract.v1.md`
- `docs/guidance/RATIFICATION.md`

## Verification Plan

Reviewer focus questions:

- Is the publishable set genuinely an allowlist, such that a new enum value
  added tomorrow is unpublishable without a further ratification?
- Could a careful implementer read the sourcing bar and build a validator
  without asking a question? Name the first ambiguity you find.
- Does any permitted phrasing survive a hostile reading as direction rather
  than observation? Try to read each one as advice and report what happens.
- Does the reasoning-gate distinction hold, or could a reader conclude the
  2026-09-16 ruling has been loosened?
- Does the record anywhere let absence of a relationship read as safety?
