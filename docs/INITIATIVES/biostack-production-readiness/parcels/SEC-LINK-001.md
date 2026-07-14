# Parcel: SEC-LINK-001

## Goal
Close the authenticated link analyzer's SSRF, redirect, timeout, and unbounded-response boundary.

## Initiative
`biostack-production-readiness`

## Project Track
Security / analyzer egress

## Wave
Release hardening

## Branch
`codex/sec-link-analyzer`

## Worktree
`D:/Repos/BioStack-sec-link-analyzer`

## Dependencies
- Current release baseline `2bdb7ba`.
- KEO-65 finding SR-02.

## Integration Surfaces
- Named `protocol-link-extractor` HTTP client.
- Link protocol extraction and nested document parsing.

## Security Gate
Every initial and redirected destination must be HTTPS on port 443, contain no user-info, and resolve exclusively to public unicast addresses. Connection establishment re-resolves and pins a validated public address. Automatic redirects are disabled. Responses are streamed under a 12 MiB ceiling and a bounded timeout.

## Allowed Files
- `docs/INITIATIVES/biostack-production-readiness/PARCELS.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/SEC-LINK-001.md`
- `backend/src/BioStack.Api/Program.cs`
- `backend/src/BioStack.Application/Services/ProtocolIngestionService.cs`
- `backend/tests/BioStack.Application.Tests/Services/LinkProtocolExtractorSecurityTests.cs`

## Forbidden
- Analyzer scoring, normalization, UI, auth, billing, secrets, dependencies, deployment, or production mutation.

## Out of Scope
- General-purpose outbound proxy infrastructure.
- Support for authenticated document providers.

## Contract
The link analyzer may fetch only bounded public HTTPS documents. It must not reach local, private, link-local, multicast, reserved, documentation, or metadata-service addresses, including through redirects or DNS changes between validation and connection.

## Required Tests
- Reject loopback, RFC1918, link-local/metadata, IPv6 local, non-443, and user-info destinations before sending.
- Reject public-to-private redirects.
- Reject oversized declared and streamed bodies.
- Reject excessive redirects.
- Preserve valid public plain-text extraction.
- Focused tests, serial build, and diff hygiene pass.

## Acceptance Criteria
- Named client disables automatic redirects and has a bounded timeout.
- Initial and redirect destinations are validated.
- Actual connections pin a newly validated public address.
- At most three redirects are followed.
- At most 12 MiB is buffered.
- Only Allowed Files change.

## Evidence Required
- Focused test output, build result, changed-file list, and `git diff --check`.

## Current Verification
- Link extractor security tests: **14 passed**, 0 warnings.
- Link extractor plus existing protocol-ingestion tests: **19 passed**, 0 warnings.
- Serial solution build: **15 projects, 0 errors**, 4 pre-existing analyzer warnings.
- Diff hygiene: required before handoff.

## Collision Risk
Medium: shared analyzer service and API composition root. Keep isolated until integration review.

## Stop-and-Report Rule
Stop on required files outside Allowed Files, a requirement for private-network document access, or inability to preserve TLS hostname validation while pinning a public address.
