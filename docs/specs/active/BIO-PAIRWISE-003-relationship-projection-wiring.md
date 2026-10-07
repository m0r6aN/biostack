---
ticket: BIO-PAIRWISE-003
title: Pairwise P2 — connect ratified relationship records to the knowledge projection
status: review-candidate
revision: 1
owner: clinton.morgan
created: 2026-09-17
updated: 2026-10-07
supersedes: null
superseded_by: null
risk: standard
delivery_classes: [standard]
guidance_classes: [not-applicable]
substance_function_risk: [not-applicable]
function_review_status: not-applicable
surfaces:
  - docs/specs/active/BIO-PAIRWISE-003-relationship-projection-wiring.md
  - backend/src/BioStack.KnowledgeWorker/Pipeline/EvidencePacketSubstanceRecordCompiler.cs
  - backend/src/BioStack.Application/Services/Intelligence/GraphIntelligenceService.cs
  - backend/tests/BioStack.KnowledgeWorker.Tests/CompoundGraphTests.cs
  - backend/tests/BioStack.KnowledgeWorker.Tests/RelationshipProjectionTests.cs
routing_class: standard-feature
verification_class: equivalence-provable
permission_profile: builder-standard
data_classification: internal
---

# BIO-PAIRWISE-003 — Relationship projection wiring

## Intent

Close the break point P0 identifies so that a relationship record satisfying
P1's bar travels from `research/input/relationships/` through the existing graph
substrate into the knowledge projection a dossier can read. Today the substrate
terminates before any consumer-visible field, and
`EvidencePacketSubstanceRecordCompiler` hardcodes its relationship arrays to
empty. P2 makes exactly one of those paths authoritative — the one P0
recommends — and leaves the other inert rather than half-wired. Its consumers
are P4, which needs a real record to travel, and P5, which renders the result.

## Constraints

- Projection only. The pipeline may surface records that exist; it must never
  synthesize, infer, merge or complete a relationship. A pair not authored is a
  pair not rendered.
- P1's bar is enforced in code at the projection boundary, not at the call site.
  A record failing the bar is excluded from the public projection; it is not
  downgraded, blanked, or emitted with fields nulled.
- Fail loudly on malformed input: a record that parses but violates the bar
  fails validation naming its `relationshipId`. Silent skipping is prohibited —
  a quietly dropped safety record is worse than a stopped pipeline.
- Both database providers must round-trip identically. `DateTimeKind` does not
  survive SQLite and PostgreSQL truncates .NET ticks to microseconds; anything
  hashed or ordered must be provider-stable.
- No migration. If P2 cannot be delivered without a schema change, stop and
  report rather than authoring one — migrations in this repository are
  hand-written and `dotnet ef migrations add` is prohibited.
- Positive / synergy types stay inert: their arrays remain empty regardless of
  input until a later ratification admits them.
- Seed regeneration remains a reviewed, explicit edit; P2 does not regenerate
  seeds as a side effect.
- Backend changes are compiled before delivery. Static review alone is not
  sufficient evidence for this parcel.

## Acceptance Criteria

1. A relationship record satisfying P1's bar, placed in the input directory,
   reaches the knowledge projection and is retrievable by the compound it
   concerns, demonstrated by an integration test.
2. A record failing each individual clause of P1's bar is excluded, with one
   test per clause.
3. A malformed record fails validation with an error naming its
   `relationshipId`; a test asserts the failure rather than a skip.
4. A compound with no relationship records projects empty collections and no
   error.
5. Whichever path P0 deems non-authoritative is documented as inert in code
   comments and does not silently contribute records.
6. Provider round-trip equivalence is covered for both PostgreSQL and SQLite.
7. `dotnet build` and the affected test projects pass, with output recorded in
   the parcel receipt.
8. No public DTO or rendered surface changes in this parcel.

## Out of Scope

- Rendering anything to a user (P5).
- `CompoundInteractionHint` and its rows (P3).
- Authoring relationship records (P4).
- Any migration, contract file, or `relationship-packet.schema.json` edit.
- Positive / synergy admission.
- Running the worker against production or performing a Refresh.

## Context & References

- P0 output: `research/output/pairwise-lane-20260917/p0-substrate-inventory.md`
- P1 ratification: `docs/guidance/pairwise-relationship-publication-contract.v1.md`
- `backend/src/BioStack.KnowledgeWorker/Pipeline/EvidencePacketSubstanceRecordCompiler.cs`
- `backend/src/BioStack.Infrastructure/Knowledge/CompoundGraphStore.cs`
- `backend/src/BioStack.Application/Services/Intelligence/GraphIntelligenceService.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/CompoundGraphTests.cs`
- Repo conventions: hand-written migrations, dual-provider round-trip.

## Allowed Files

- `backend/src/BioStack.KnowledgeWorker/Pipeline/EvidencePacketSubstanceRecordCompiler.cs`
- `backend/src/BioStack.Application/Services/Intelligence/GraphIntelligenceService.cs`
- `backend/src/BioStack.Infrastructure/Knowledge/CompoundGraphStore.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/CompoundGraphTests.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/RelationshipProjectionTests.cs`

## Verification Plan

Reviewer focus questions:

- Can any code path produce a rendered pair that no author wrote? Trace it.
- Is the bar enforced once at the boundary, or re-implemented per call site
  where a future caller could forget it?
- Does any failure mode drop a record silently? Look specifically at exception
  handling around parse and validation.
- Is the inert path genuinely inert, or merely unused today?
