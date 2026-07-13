# Parcel: SEC-CONSENT-001

## Goal
Bind consent evidence and mutation gates to the server-required consent version.

## Initiative
`biostack-production-readiness`

## Project Track
Security / consent evidence

## Wave
Release hardening

## Branch
`codex/sec-consent-version`

## Worktree
`D:/Repos/BioStack-sec-consent`

## Dependencies
- Current release baseline `2bdb7ba`.
- KEO-65 finding SR-08.

## Integration Surfaces
- Consent status, recording, and mutation gate.

## Security Gate
The server selects `ConsentGate.CurrentConsentVersion`; a client-supplied value cannot become evidence. Consent is granted only when a timestamp exists and the stored version exactly matches the current required version.

## Allowed Files
- `docs/INITIATIVES/biostack-production-readiness/PARCELS.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/SEC-CONSENT-001.md`
- `backend/src/BioStack.Application/Services/ConsentGate.cs`
- `backend/tests/BioStack.Api.Tests/Integration/ConsentGateIntegrationTests.cs`

## Forbidden
- Consent copy/UI, database schema, provider-access consent, auth/session behavior, dependencies, secrets, deployment, or production mutation.

## Out of Scope
- Adding a consent-document hash column.
- Changing the current consent text/version.

## Contract
Recording consent always persists the current server version. A blank, invented, stale, or future client version cannot alter that evidence. When the server version advances, older records become not accepted until re-acceptance.

## Required Tests
- Existing anonymous, recording, idempotency, and mutation-gate tests remain green.
- An invented client version records the current server version.
- A timestamp paired with a stale stored version reports not accepted and blocks mutation.
- Focused integration tests, serial build, and diff hygiene pass.

## Acceptance Criteria
- `GetStatusAsync` compares the stored version to `CurrentConsentVersion`.
- `RecordAsync` ignores the client-selected version for persistence.
- `IsConsentGrantedAsync` fails for stale/invented versions.
- Only Allowed Files change.

## Evidence Required
- Focused test output, build result, changed-file list, and `git diff --check`.

## Current Verification
- Consent gate integration tests: **16 passed**, 4 pre-existing build warnings.
- Invented-version and stale-version cases pass.
- Serial solution build: **15 projects, 0 errors, 0 warnings**.
- Diff hygiene: `git diff --check` passed.

## Collision Risk
Low-to-medium: consent service and integration tests. Keep isolated until integration review.

## Stop-and-Report Rule
Stop on required files outside Allowed Files, a need to alter consent copy, or a requirement to accept multiple simultaneous server versions.
