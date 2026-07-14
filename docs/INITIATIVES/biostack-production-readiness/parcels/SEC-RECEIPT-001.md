# Parcel: SEC-RECEIPT-001

## Goal
Close the authenticated receipt-authorization gap without changing the public-safe knowledge evidence surface.

## Initiative
`biostack-production-readiness`

## Project Track
Security / receipt authorization

## Wave
Release hardening

## Branch
`codex/sec-receipt-authorization`

## Worktree
`D:/Repos/BioStack-sec-receipts`

## Dependencies
- Current release baseline `2bdb7ba`.
- KEO-65 defensive security review finding: receipt API BOLA/privacy exposure.

## Integration Surfaces
- Authenticated receipt API reads.
- Authenticated server-rendered receipt detail.
- Authenticated audit receipt feed.

## Security Gate
Non-admin users may read only receipts whose actor is `user:{current-user-id}`. Admins retain cross-user and system receipt access. Direct cross-user receipt lookup returns `404` to avoid existence disclosure; an explicit cross-user actor query returns `403`.

## Allowed Files
- `docs/INITIATIVES/biostack-production-readiness/PARCELS.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/SEC-RECEIPT-001.md`
- `backend/src/BioStack.Api/Endpoints/ReceiptEndpoints.cs`
- `backend/tests/BioStack.Api.Tests/Integration/ReceiptEndpointsIntegrationTests.cs`
- `frontend/src/app/receipts/[uri]/page.tsx`
- `frontend/src/app/governance/receipts/page.tsx`
- `frontend/src/__tests__/app/governance/receipts/AuditReceiptFeedPage.test.tsx`

## Forbidden
- Public knowledge trust-ledger behavior.
- Receipt creation or repository schema changes.
- Auth/session semantics outside receipt reads.
- Dependencies, secrets, deployment, merge, or production mutation.

## Out of Scope
- Receipt redaction policy changes.
- Organization-level tenancy, which does not yet exist.
- Historical secret rotation and other KEO-65 findings.

## Existing Patterns To Follow
- `ReceiptActor.User(Guid)` for stable consumer actor identity.
- `AdminOnly` role claim value `1` for administrative access.
- Cookie-backed authenticated API calls from the frontend.

## Contract
Receipt evidence remains inspectable by the actor who produced it and by administrators, but is not a public or cross-user evidence surface. Subject queries are filtered to the current actor. Public-safe evidence continues through the knowledge trust ledger.

## Required Tests
- Unauthenticated receipt requests return `401`.
- A user can read their own receipt by URI, subject, and actor.
- A user cannot enumerate or directly retrieve another actor's receipt.
- An admin can read a system receipt.
- The audit feed requests the signed-in user's actor ID.
- Focused backend and frontend tests, production frontend build, and diff hygiene pass.

## Acceptance Criteria
- Receipt endpoints require authentication.
- Direct cross-user receipt lookup returns `404`.
- Subject results contain only the current user's receipts for non-admins.
- Non-admin cross-user actor queries return `403`.
- Admin access preserves system/cross-user investigation workflows.
- Server-rendered receipt detail forwards the authenticated request cookie.
- Only Allowed Files change.

## Evidence Required
- Focused test output, production build result, changed-file list, and `git diff --check`.

## Current Verification
- Backend receipt integration tests: **7 passed**, 4 existing build warnings.
- Frontend audit-feed test: **2 passed**.
- Targeted frontend formatting: **passed**.
- Production frontend build: application compilation passed, then repository-wide type checking stopped on pre-existing KEO-64 failure `frontend/src/components/tools/ToolsDecisionSurface.tsx:283` (`reconstitutionInstructions` is undefined).
- Diff hygiene: `git diff --check` passed.
- Integrated candidate verification: KEO-64 repair `44eac22` removed the repository-wide type blocker; combined frontend production build passed at `fb0ed84`.
- Integration status: **integrated and locally verified**; hosted workflow and live authorization checks remain release-gate evidence.

## Collision Risk
Medium: shared receipt endpoint and frontend audit surfaces. Keep isolated until integration review.

## PR Notes
- What changed: authenticated, actor-scoped receipt reads with admin override.
- Why: prevent receipt metadata and evidence-reference disclosure across users.
- Risk: existing unauthenticated receipt deep links now require sign-in.
- Verification: focused integration/UI tests plus production build.

## Stop-and-Report Rule
Stop on required files outside Allowed Files, a need to expose receipts publicly, auth contract changes, or test evidence that system receipts are required for ordinary non-admin users.
