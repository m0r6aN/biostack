---
ticket: KEO-TBD
title: Pairwise P3 — interaction hint provenance and unsourced quarantine
status: draft
owner: clinton.morgan
created: 2026-09-17
updated: 2026-09-17
supersedes: null
superseded_by: null
risk: elevated
surfaces:
  - docs/specs/active/KEO-TBD-pairwise-p3-interaction-hint-provenance.md
  - backend/src/BioStack.Domain/Entities/
  - backend/src/BioStack.Infrastructure/Persistence/Migrations/
  - backend/src/BioStack.Application/
routing_class: architecture/risk
permission_profile: builder-architecture
data_classification: internal
---

# KEO-TBD-pairwise-P3 — Interaction hint provenance and quarantine

## Intent

Give `CompoundInteractionHint` a provenance surface and quarantine the fourteen
hand-authored rows that currently drive scoring, so they continue to serve the
internal score but can never reach a public surface or be counted as evidence.
These rows carry no citation field of any kind and are today the origin of most
rendered interaction findings. The owner ratified on 2026-09-17 that they are
kept, flagged, never publicly rendered and never called evidence. P3 makes that
ratification structurally true rather than a convention someone must remember.

## Constraints

- No row is deleted. Quarantine means excluded from public projection, not
  removed from the database; deleting them silently degrades the score with no
  replacement.
- The migration is hand-written, copying the established pattern: hand-picked
  timestamp with trailing zeros, `type: "TEXT"` for strings, a partial
  `.Designer.cs` carrying the `[Migration]` attribute, central snapshot left
  untouched. `dotnet ef migrations add` is prohibited — the scaffolder rewrites
  the intentionally minimal snapshot and emits `CreateTable` for the whole
  schema.
- Enforcement lives at the projection boundary already established by the
  2026-09-16 ruling, so an unsourced hint cannot be rendered publicly even if a
  future caller forgets to check. A flag that only advises is not enforcement.
- Both providers round-trip identically.
- Existing scoring behaviour for entitled users is unchanged by this parcel. A
  score that moves is a defect, not an improvement, and must be reported.
- The new provenance fields are additive and nullable; existing rows remain
  valid without backfill, defaulting to unsourced.
- Backend changes are compiled before delivery.

## Acceptance Criteria

1. `CompoundInteractionHint` carries provenance fields sufficient to record a
   source reference and an explicit sourced/unsourced discriminator defaulting
   to unsourced.
2. A hand-written migration applies cleanly on PostgreSQL and SQLite, and the
   central model snapshot is unmodified.
3. All fourteen existing rows persist and are marked unsourced.
4. An integration test proves an unsourced hint appears in no anonymous
   response, no Observer response, and nothing labelled as evidence anywhere.
5. Scores computed for an entitled user are byte-identical before and after,
   demonstrated by a test over a fixed fixture.
6. A sourced hint, once one exists, is not blocked by the quarantine — the
   discriminator, not the table, decides.
7. `dotnet build` and the affected test projects pass, recorded in the receipt.

## Out of Scope

- Authoring sources for the fourteen rows.
- Deleting or editing row content.
- Relationship packets and the graph substrate (P1, P2, P4).
- Public rendering (P5).
- Changing what the score computes or how it weights terms.
- Re-opening the 2026-09-16 reasoning-gating ruling.

## Context & References

- `backend/src/BioStack.Domain/Entities/CompoundInteractionHint.cs`
- `backend/src/BioStack.Infrastructure/Knowledge/CompoundInteractionHintCatalog.cs`
- `backend/src/BioStack.Infrastructure/Repositories/CompoundInteractionHintRepository.cs`
- `backend/src/BioStack.Api/ProductionMigrationBaselineConfiguration.cs`
- Migration pattern reference: `20260626000000_AddReceiptClassToSpine.cs`
- The 2026-09-16 owner ruling and its RATIFICATION entries (#369, #370, #372).
- `research/output/gtm-activation-20260909/owner-feedback-20260915/b1-relationship-audit/relationship-data-audit.md`

## Allowed Files

- `backend/src/BioStack.Domain/Entities/CompoundInteractionHint.cs`
- `backend/src/BioStack.Infrastructure/Persistence/BioStackDbContext.cs`
- `backend/src/BioStack.Infrastructure/Knowledge/CompoundInteractionHintCatalog.cs`
- `backend/tests/BioStack.Api.Tests/Integration/InteractionHintQuarantineIntegrationTests.cs`

## Verification Plan

Reviewer focus questions:

- Is the quarantine enforced structurally, or does it depend on every caller
  remembering a flag? Name the single place it is enforced.
- Does the migration follow the hand-written pattern exactly, and is the central
  snapshot genuinely untouched?
- Can an unsourced hint reach any surface a signed-out visitor sees? Try to find
  a path.
- Did the score move? Show the fixture comparison rather than asserting it.
