# Parcel: SEC-AUTH-001

## Goal
Make one-time magic-link consumption atomic so concurrent verification requests cannot issue multiple sessions.

## Initiative
`biostack-production-readiness`

## Project Track
Security / authentication

## Wave
Release hardening

## Branch
`codex/sec-magic-link-atomic`

## Worktree
`D:/Repos/BioStack-sec-magic-link`

## Dependencies
- Current release baseline `2bdb7ba`.
- KEO-65 finding SR-05.

## Integration Surfaces
- Passwordless magic-link verification.
- Session issuance and challenge attempt accounting.

## Security Gate
Challenge claim is a single conditional database update on token, channel, type, unconsumed state, and expiry. Exactly one affected row is required before session issuance. Reuse attempts increment the attempt counter without issuing a session.

## Allowed Files
- `docs/INITIATIVES/biostack-production-readiness/PARCELS.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/SEC-AUTH-001.md`
- `backend/src/BioStack.Api/Endpoints/AuthEndpoints.cs`
- `backend/tests/BioStack.Api.Tests/Integration/AuthEndpointsIntegrationTests.cs`

## Forbidden
- Token format/lifetime, cookie policy, redirect allowlist, auth UI, database schema, dependencies, secrets, deployment, or production mutation.

## Out of Scope
- Switching the verification endpoint away from GET.
- Email scanner behavior beyond the existing frontend interstitial.

## Contract
For one valid challenge token, at most one request may transition `ConsumedAtUtc` from null and at most one session may be persisted. All concurrent or later requests receive the existing invalid-link redirect.

## Required Tests
- Existing valid, expired, and sequential-reuse tests remain green.
- Two concurrent consumers produce exactly one success redirect, one invalid-link redirect, and one persisted session.
- Focused integration tests, serial build, and diff hygiene pass.

## Acceptance Criteria
- No read-then-write consumed-state decision remains.
- Conditional claim includes unconsumed and unexpired predicates.
- Session creation occurs only after exactly one row is claimed.
- Failed/reuse attempts do not create sessions.
- Only Allowed Files change.

## Evidence Required
- Focused test output, build result, changed-file list, and `git diff --check`.

## Current Verification
- Auth endpoint integration tests: **11 passed**, 4 pre-existing build warnings.
- Concurrent-consumer case proves one success redirect, one invalid-link redirect, and one persisted session.
- Serial solution build: **15 projects, 0 errors, 0 warnings**.
- Diff hygiene: `git diff --check` passed.

## Collision Risk
Medium: central auth endpoint. Keep isolated until integration review.

## Stop-and-Report Rule
Stop on required files outside Allowed Files, provider-specific database behavior that cannot preserve atomicity, or any need to weaken session/cookie validation.
