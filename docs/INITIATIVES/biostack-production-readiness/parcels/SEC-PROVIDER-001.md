# Parcel: SEC-PROVIDER-001

## Goal
Remove provider-request enumeration and unauthorized public reopen/overwrite behavior while preserving the low-friction public intake form.

## Initiative
`biostack-production-readiness`

## Project Track
Security / provider intake

## Wave
Release hardening

## Branch
`codex/sec-provider-access`

## Worktree
`D:/Repos/BioStack-sec-provider-access`

## Dependencies
- Current release baseline `2bdb7ba`.
- KEO-65 finding SR-03.

## Integration Surfaces
- Anonymous provider-access request submission.
- Authenticated administrative provider queue.

## Security Gate
Every valid public submission receives the same opaque pending acknowledgement. The acknowledgement ID is not the persisted queue ID. A repeated email neither reveals queue state nor changes an existing request. Reopen and owner/status changes remain administrative operations.

## Allowed Files
- `docs/INITIATIVES/biostack-production-readiness/PARCELS.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/SEC-PROVIDER-001.md`
- `backend/src/BioStack.Api/Endpoints/ProviderAccessEndpoints.cs`
- `backend/tests/BioStack.Api.Tests/Integration/ProviderAccessEndpointsIntegrationTests.cs`

## Forbidden
- Provider form copy/UI, database schema, admin authorization policy, auth/session behavior, secrets, dependencies, deployment, or production mutation.

## Out of Scope
- Email verification workflow and CRM integration.
- Changing the existing public response JSON shape.

## Contract
The public endpoint is write-only intake, not a request-status lookup. Repeated submissions are idempotent and non-mutating. Existing request IDs, statuses, owners, consent times, and queue state are never returned to public callers.

## Required Tests
- New submission persists normalized contact/consent and returns an opaque acknowledgement.
- Duplicate open submission does not create or mutate a record.
- Duplicate closed submission cannot reopen, overwrite fields, or clear owner.
- Public response does not expose persisted queue ID or status.
- Admin queue can still update status/owner using the persisted ID.
- Focused integration tests, serial build, and diff hygiene pass.

## Acceptance Criteria
- Existing public records are never mutated by submission.
- Every accepted submission uses `pending` plus a fresh opaque ID/time.
- Unique-index races return the same opaque response only when the competing record exists.
- Administrative queue behavior remains intact.
- Only Allowed Files change.

## Evidence Required
- Focused test output, build result, changed-file list, and `git diff --check`.

## Current Verification
- Provider-access integration tests: **4 passed**, 4 pre-existing build warnings.
- Serial solution build: **15 projects, 0 errors, 0 warnings**.
- Diff hygiene: `git diff --check` passed.

## Collision Risk
Low-to-medium: provider endpoint/tests only. Keep isolated until integration review.

## Stop-and-Report Rule
Stop on required files outside Allowed Files, a requirement for public status tracking, or a need to weaken the administrative boundary.
