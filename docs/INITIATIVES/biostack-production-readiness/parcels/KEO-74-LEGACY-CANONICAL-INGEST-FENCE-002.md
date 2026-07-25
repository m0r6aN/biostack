# Parcel: KEO-74-LEGACY-CANONICAL-INGEST-FENCE-002

## Status

Backend and frontend fence implemented and locally verified on 2026-07-25. The route is no longer mapped, the stale caller/control is removed, and the admin page now exposes read-only statistics plus the fail-closed governance posture.

## Goal

Remove the legacy canonical knowledge-ingest route so configuration and a static override header cannot bypass source-registry and provenance controls.

## Initiative

BioStack Production Readiness & Monetization

## Project Track

M3 - Data & Intelligence Coverage / API governance hardening

## Wave

Hardening

## Branch

`codex/keo-74-ingest-fence-20260725`

## Worktree

`D:\Repos\BioStack-keo74-ingest-fence-20260725`

## Dependencies

- KEO-74 source-registry v2 gate analysis

## Integration Surfaces

- Admin API -> canonical knowledge store
- Source registry -> canonical promotion

## Security Gate

`source-registry-canonical-write-bypass`

## Allowed Files

- `backend/src/BioStack.Api/Endpoints/AdminEndpoints.cs`
- `backend/tests/BioStack.Api.Tests/Integration/AdminSourceLaneGovernanceIntegrationTests.cs`
- `frontend/src/app/admin/page.tsx`
- `frontend/src/__tests__/components/AdminPage.test.tsx`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-74-LEGACY-CANONICAL-INGEST-FENCE-002.md`

If a required file is not listed, stop and request a spec amendment before editing or creating it.

## Forbidden

- Do not add a replacement ingest route.
- Do not weaken admin authentication or any transcript source-lane gate.
- Do not change registry, source acquisition, review, promotion, database schema, receipts, or deployment configuration.
- Do not treat the video/channel intake DTO as an official-source intake contract.
- Do not perform live endpoint testing or deployment.

## Out of Scope

Registry-bound official-source intake, provenance migrations, source activation, acquisition clients, and canonical promotion redesign.

## Existing Patterns To Follow

- `docs/INITIATIVES/biostack-production-readiness/KEO-74-SOURCE-REGISTRY-V2-GATES.md` - states that the current bulk endpoint is not registry/provenance-bound.
- `backend/tests/BioStack.Api.Tests/Integration/AdminSourceLaneGovernanceIntegrationTests.cs` - integration coverage for no-write and no-receipt behavior.

## Contract

`POST /api/v1/admin/knowledge/ingest` must not be mapped. It must return `404 Not Found` for an authenticated administrator regardless of:

- `Admin:KnowledgeIngest:Enabled`;
- the presence or value of `X-BioStack-Admin-Override`; or
- a syntactically valid bulk `KnowledgeEntry` payload.

The request must create no `KnowledgeEntry` and no `admin.override.performed` receipt.

The admin page must not render the removed bulk-ingest control or call the removed route. It may continue to render read-only system statistics and accurate governance posture.

## Required Tests

- Replace positive override ingestion coverage with a negative test proving enabled configuration plus the former header still returns 404.
- Preserve or consolidate default-disabled and malformed/missing header cases only where they add independent evidence.
- Assert both no canonical write and no override receipt.
- Run the focused API integration class.
- Replace the stale frontend ingest test with coverage that the admin page fetches statistics but exposes no bulk-ingest control and never calls `/api/v1/admin/knowledge/ingest`.
- Run the focused frontend admin-page test.

## Acceptance Criteria

- The route mapping and its static header/config gate are removed.
- Former override inputs cannot cause a write.
- The frontend contains no caller or UI promise for the removed endpoint.
- Other admin source-lane tests remain unchanged and passing.
- Only the five allowed files change.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --filter FullyQualifiedName~AdminSourceLaneGovernanceIntegrationTests
rtk pnpm --dir frontend test -- frontend/src/__tests__/components/AdminPage.test.tsx
rtk git diff --check
```

Success means the focused integration class passes with a deterministic 404/no-write/no-receipt result, and the focused frontend test proves no removed-route caller or bulk-ingest control remains.

## Evidence Required

- Focused integration-test output.
- Focused frontend-test output.
- Diff showing the route is absent.
- `git diff --check`.

## Collision Risk

High. `AdminEndpoints.cs` is an API serialization point.

## PR Notes

- What changed: permanently removes the unbound canonical bulk-ingest route.
- Why: config plus a static header could bypass source registry, provenance, and review contracts.
- Risk: callers using the legacy route receive 404 and must wait for the future registry-bound path; the admin UI no longer offers that operation.
- Verification: focused integration coverage proves no write and no override receipt; focused frontend coverage proves statistics remain read-only with no ingest control or request.
- Evidence: parcel, diff, and test output.

## Session Handoff

- Starting commit: `9a74df2279383b3ea8f61094b5ef164c0c6a3950`
- Ending commit: uncommitted working tree; `HEAD` remains `9a74df2279383b3ea8f61094b5ef164c0c6a3950`.
- Files changed:
  - `backend/src/BioStack.Api/Endpoints/AdminEndpoints.cs`
  - `backend/tests/BioStack.Api.Tests/Integration/AdminSourceLaneGovernanceIntegrationTests.cs`
  - `frontend/src/app/admin/page.tsx`
  - `frontend/src/__tests__/components/AdminPage.test.tsx`
  - `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-74-LEGACY-CANONICAL-INGEST-FENCE-002.md`
- Commands run:
  - `rtk test dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --filter FullyQualifiedName~AdminSourceLaneGovernanceIntegrationTests`
  - `rtk proxy dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~AdminSourceLaneGovernanceIntegrationTests" --logger "console;verbosity=minimal"`
  - `rtk git diff --check`
  - `rtk git diff -- backend/src/BioStack.Api/Endpoints/AdminEndpoints.cs backend/tests/BioStack.Api.Tests/Integration/AdminSourceLaneGovernanceIntegrationTests.cs`
  - `rtk git status --short`
  - `rtk proxy rg -n -F "/knowledge/ingest" backend/src/BioStack.Api/Endpoints/AdminEndpoints.cs` (no matches)
  - `rtk pnpm --dir frontend test -- frontend/src/__tests__/components/AdminPage.test.tsx`
  - `rtk pnpm --dir frontend exec vitest run src/__tests__/components/AdminPage.test.tsx`
  - `rtk git diff --exit-code -- frontend/package-lock.json`
- Tests passed: 5 focused backend integration tests and 1 focused frontend test; 0 skipped. The parcel-prescribed frontend command also completed the full frontend suite with 927 tests passing.
- Tests failed: 0 after lockfile-pinned frontend dependencies were available. The first frontend attempt did not start tests because the fresh worktree lacked `node_modules`; dependency bootstrap briefly changed `frontend/package-lock.json`, and that generated delta was reverted with `apply_patch`.
- Decisions needed: none.
- Blockers: none for this parcel. Test restore/build continues to report the pre-existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903` high-severity advisory warnings.
- Next safe action: merge after security review; design registry-bound intake as a separate contract parcel.
- Do not touch: source registry, acquisition, promotion, migrations, deployment.

## Stop-and-Report Rule

If implementation requires a product decision not present in this spec, a file outside Allowed Files, a contract amendment, or an unclear security boundary, stop and report before continuing.
