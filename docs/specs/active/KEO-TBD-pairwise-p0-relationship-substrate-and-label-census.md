---
ticket: KEO-TBD
title: Pairwise P0 — relationship substrate inventory and label interaction census
status: draft
owner: clinton.morgan
created: 2026-09-17
updated: 2026-09-17
supersedes: null
superseded_by: null
risk: low
surfaces:
  - docs/specs/active/KEO-TBD-pairwise-p0-relationship-substrate-and-label-census.md
  - research/output/pairwise-lane-20260917/
routing_class: implementation/standard
permission_profile: reviewer-readonly
data_classification: internal
---

# KEO-TBD-pairwise-P0 — Relationship substrate inventory and label interaction census

## Intent

Measure two unknowns that every later parcel in the pairwise lane is currently
guessing at: what the already-built relationship substrate actually does
end-to-end, and how many library compounds have a real product label carrying a
DRUG INTERACTIONS section. The lane was scoped on the belief that a pairwise
contract had to be invented; a full `relationship-packet.schema.json`, taxonomy,
graph entities, store, service, migrations and fixtures already exist with an
empty input directory. P0 replaces belief with a measured record. Its consumers
are the P1 ratification author, the coordinator, and the owner deciding whether
the negative lane is worth funding at its measured size.

## Constraints

- Read-only. No packet authored, no schema edited, no code changed, no database
  touched, no deployment, no promotion, no Refresh.
- No new source acquisition and no network fetch. Classify each packet's
  existing source records from what the repository already contains. Where a
  record is insufficient to determine whether the cited source is a product
  label with an interactions section, report `undetermined` — do not infer from
  the compound's name, class, or regulatory status.
- The census counts sources, not claims. It must not assert that any specific
  interaction exists; establishing that is P4's job under P1's bar.
- Absence of a label is not evidence that no interaction exists, and the report
  must state so explicitly wherever a zero appears.
- Findings are reported with file paths and line numbers. An assertion without a
  citation does not belong in the output.

## Acceptance Criteria

1. A substrate inventory names, with `file:line`, every component of the
   existing relationship path: schema, taxonomy, domain entities, store,
   `GraphIntelligenceService`, the `RunMode.Research` wiring and
   `WorkerOptions` entries, the migrations, and the test fixtures.
2. The inventory states definitively whether a relationship packet placed in
   `research/input/relationships/` reaches (a) the graph store, (b) any
   `KnowledgeEntry` field, (c) any API response, and (d) any rendered surface —
   each answer either demonstrated by a cited code path or recorded as
   `not connected`, with the exact break point named.
3. The inventory states whether `EvidencePacketSubstanceRecordCompiler`'s
   hardcoded empty relationship arrays and the relationship-packet path are two
   competing mechanisms or one supersedes the other, and recommends which is
   authoritative for the lane.
4. A per-packet census table covers all 78 evidence packets: canonical name,
   whether an FDA/DailyMed lane is cited, the classification of each such source
   (`product-label` / `approval-package` / `warning-letter` / `advisory` /
   `other` / `undetermined`), and whether a label with an interactions section
   is present.
5. Counts are reported for each classification, the nine packets with no
   FDA/DailyMed lane are named, and a scope recommendation for P1 states the
   measured size of the negative lane in compounds.
6. No file outside the P0 output directory is created, modified or deleted.

## Out of Scope

- Authoring any relationship packet or pairwise claim (P4).
- Ratifying the publishable subset or the rendering contract (P1).
- Connecting the substrate to any surface (P2).
- Any change to `CompoundInteractionHint` or its rows (P3).
- Any public surface, DTO, or copy change (P5).
- Positive / synergy relationships, which remain deferred for the whole lane.
- Fetching, acquiring, or authorizing any new source.

## Context & References

- `backend/src/BioStack.KnowledgeWorker/Schemas/relationship-packet.schema.json`
- `research/protocol-intelligence/relationship-taxonomy.json`
- `backend/src/BioStack.Domain/Entities/Graph/`
- `backend/src/BioStack.Application/Services/Intelligence/GraphIntelligenceService.cs`
- `backend/src/BioStack.Infrastructure/Knowledge/CompoundGraphStore.cs`
- `backend/src/BioStack.KnowledgeWorker/Pipeline/EvidencePacketSubstanceRecordCompiler.cs`
- `backend/src/BioStack.KnowledgeWorker/Config/WorkerOptions.cs`
- `research/input/evidence/`
- `docs/guidance/biostack-guidance-content-contract.v1.md`

## Allowed Files

- `research/output/pairwise-lane-20260917/p0-substrate-inventory.md`
- `research/output/pairwise-lane-20260917/p0-label-interaction-census.md`

## Verification Plan

Reviewer focus questions:

- Does every "not connected" verdict name the exact break point, or does it
  hand-wave at a layer?
- Does the census distinguish a cited FDA source from a product label with an
  interactions section, or does it silently treat the two as equivalent?
- Does any sentence in the output read as an assertion that a particular
  interaction exists?
- Is the recommendation in criterion 3 supported by the code paths cited, or is
  it a preference dressed as a finding?
