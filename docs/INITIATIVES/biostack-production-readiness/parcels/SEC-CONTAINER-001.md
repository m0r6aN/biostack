# Parcel: SEC-CONTAINER-001

## Goal
Run the production backend container as the .NET runtime image's unprivileged application user while preserving the writable Kompress data path and health check.

## Initiative
`biostack-production-readiness`

## Project Track
Security / container hardening

## Wave
Release hardening

## Branch
`codex/sec-backend-container`

## Worktree
`D:/Repos/BioStack-sec-container`

## Dependencies
- Current release baseline `2bdb7ba`.
- KEO-65 finding SR-09.

## Integration Surfaces
- Backend runtime image, `/app` ownership, `/app/data`, and health check.

## Security Gate
Package installation occurs as root during image construction; runtime executes as the built-in `app` user. Application files and the default data directory are owned by that user.

## Allowed Files
- `docs/INITIATIVES/biostack-production-readiness/PARCELS.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/SEC-CONTAINER-001.md`
- `backend/Dockerfile`

## Forbidden
- Application code/config, dependencies, workflows, registry/push/deploy, secrets, or production mutation.

## Out of Scope
- Read-only root filesystem enforcement in Azure Container Apps.
- Replacing the curl health check.

## Contract
The final image config declares a non-root user. `/app/data` remains writable for the default Kompress path. The API assembly and curl health probe remain executable.

## Required Tests
- Build the backend image locally.
- Inspect the final image user and assert it is non-root.
- Run a one-shot container command that confirms the effective UID is non-zero and `/app/data` is writable.
- Diff hygiene passes.

## Acceptance Criteria
- Final stage declares `USER app`.
- `/app` and `/app/data` ownership allow application runtime writes without root.
- Local image build and non-root/writability checks pass.
- Only Allowed Files change.

## Evidence Required
- Image tag/ID, configured/effective user, writable-path check, changed-file list, and `git diff --check`.

## Current Verification
- Local image build: **passed** as `biostack-api:sec-container-001`.
- Image ID: `sha256:445265b64c760b2394ab6018f7ca13514be8da2fe69e7ccde1c8100d546548a1`.
- Configured user: `app`.
- Effective UID: `1654` (non-root).
- `/app/data` one-shot write/remove check: **passed**.
- Diff hygiene: `git diff --check` passed.

## Collision Risk
Low: backend Dockerfile only. Keep isolated until integration review.

## Stop-and-Report Rule
Stop on a missing built-in runtime user, a required privileged runtime operation, or any need to push/deploy the test image.
