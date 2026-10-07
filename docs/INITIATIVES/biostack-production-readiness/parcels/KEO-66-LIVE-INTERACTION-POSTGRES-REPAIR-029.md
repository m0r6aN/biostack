# Parcel: KEO-66-LIVE-INTERACTION-POSTGRES-REPAIR-029

## Goal
Restore the production interaction-check path for known compounds and remove duplicate names from the public compound projection without deleting stored evidence.

## Initiative
biostack-production-readiness

## Project Track
BioStack API / public knowledge surface

## Wave
Hardening

## Branch
codex/keo66-live-interaction-hotfix-20260727

## Worktree
D:/Repos/BioStack-go-live-audit-021-20260726

## Dependencies
- Production deployment at `fa86b99626fa1e3d6939e62882444334f624c720`

## Integration Surfaces
- Public web/API -> `POST /api/v1/knowledge/interaction-check`
- Public web/API -> `GET /api/v1/knowledge/compounds`
- API startup -> production PostgreSQL migrations

## Security Gate
Security review required before merge.

## Allowed Files
- `backend/src/BioStack.Infrastructure/Persistence/Migrations/20260727090000_RepairCompoundGraphPostgresTypes.cs`
- `backend/src/BioStack.Application/Services/KnowledgeService.cs`
- `backend/tests/BioStack.Api.Tests/CompoundGraphPostgresTypeRepairMigrationTests.cs`
- `backend/tests/BioStack.Application.Tests/Services/KnowledgeServicePublicBoundaryTests.cs`
- `docs/INITIATIVES/biostack-production-readiness/PARCELS.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-66-LIVE-INTERACTION-POSTGRES-REPAIR-029.md`
- `docs/INITIATIVES/biostack-production-readiness/routing-events/KEO-66-LIVE-INTERACTION-POSTGRES-REPAIR-029.json`

If a required file is not listed, stop and amend this spec before editing or creating it.

## Forbidden
- Do not delete or merge production knowledge rows.
- Do not rewrite or remove an already-applied migration.
- Do not change the public response contract or expose prescriptive fields.
- Do not merge or deploy from this parcel.

## Out of Scope
Corpus curation, graph publication, interaction inference changes, authenticated protocol recommendations, and production data cleanup.

## Existing Patterns To Follow
- `backend/src/BioStack.Infrastructure/Persistence/Migrations/20260713230000_AddVersionedConsentDecline.cs` - provider-discoverable migration.
- `backend/tests/BioStack.Application.Tests/Services/KnowledgeServicePublicBoundaryTests.cs` - public projection boundary tests.

## Contract
- Existing API request and response shapes remain unchanged.
- PostgreSQL compound-graph UUID, boolean, and timestamp columns must match the EF model after migration.
- Public compound listing returns at most one projection per normalized canonical name.

## Required Tests
- PostgreSQL repair migration emits provider-scoped, forward-only type repair SQL.
- Non-PostgreSQL providers receive no repair SQL.
- Public projection collapses case/whitespace-equivalent canonical names and deterministically retains the evidence-richer entry.

## Acceptance Criteria
- Two resolved compounds no longer fail because the graph schema uses SQLite-specific PostgreSQL column types.
- Public compound names are unique case-insensitively.
- Stored duplicate evidence rows are preserved.
- Focused backend tests and the full affected test projects pass.

## Verification
- `dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj`
- `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj`
- Generate and inspect the Npgsql migration script.
- After merge/deploy: verify known-compound interaction checks return 200 and the compound list has equal total/unique canonical-name counts.

## Evidence Required
- Local test output.
- Migration SQL inspection.
- Independent security/diff review.
- Hosted CI URL.
- Post-deployment API evidence (after an authorized merge).

## Collision Risk
High: production migration chain. Serialize with other migration work.

## PR Notes
- What changed: forward-repair graph schema types and deduplicate the public list projection.
- Why: production returns 500 for any pair of resolved compounds and emits duplicate public names.
- Risk: production schema repair; guarded to PostgreSQL and type-inspected before conversion.
- Verification: focused test projects plus generated migration SQL.
- Evidence: parcel handoff and CI checks.

## Session Handoff
- Starting commit: `fa86b99626fa1e3d6939e62882444334f624c720`
- Ending commit: branch HEAD (see draft PR)
- Files changed: the seven files listed in Allowed Files
- Commands run: focused/full .NET tests, Npgsql migration generation, PostgreSQL 16 positive and negative executions, diff checks, local Qwen review
- Tests passed: API 299/299; Application 536/536 with 5 explicit live-Collective skips; PostgreSQL type/row/UTC/offset assertions; invalid booleans rejected
- Tests failed: none in final runs
- Decisions needed: none
- Blockers: none
- Next safe action: hosted CI and post-deployment verification after authorized merge.
- Do not touch: production data rows, applied migration history, authenticated recommendation behavior.

## Stop-and-Report Rule
If implementation requires deletion or mutation of existing knowledge evidence, a public contract change, or a new product decision, stop and report it.
