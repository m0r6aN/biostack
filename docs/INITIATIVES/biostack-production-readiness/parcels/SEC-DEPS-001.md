# Parcel: SEC-DEPS-001

## Goal
Remove the known moderate PostCSS advisory from the production frontend dependency graph without changing the Next.js major/minor release.

## Initiative
`biostack-production-readiness`

## Project Track
Security / frontend dependencies

## Wave
Release hardening

## Branch
`codex/sec-frontend-postcss`

## Worktree
`D:/Repos/BioStack-sec-frontend-deps`

## Dependencies
- Current release baseline `2bdb7ba`.
- KEO-65 finding SR-06.
- KEO-64 controls final production-build acceptance.

## Integration Surfaces
- Frontend npm dependency resolution and production build.

## Security Gate
The nested PostCSS version used by Next.js must resolve to `>=8.5.10`, the first patched release for GHSA-qx2v-qp2m-jg93. No forced Next.js downgrade or unrelated dependency update is permitted.

## Allowed Files
- `docs/INITIATIVES/biostack-production-readiness/PARCELS.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/SEC-DEPS-001.md`
- `frontend/package.json`
- `frontend/package-lock.json`

## Forbidden
- Application source, broad dependency upgrades, forced audit fix, workflows, secrets, deployment, or production mutation.

## Out of Scope
- Development-only advisories below the configured moderate production threshold.
- KEO-64 application/CI repair.

## Contract
Use a narrow npm override for Next.js's PostCSS dependency. Regenerate the lockfile deterministically. Production dependency audit must report zero moderate-or-higher vulnerabilities.

## Required Tests
- Clean `npm ci` succeeds.
- `npm audit --omit=dev --audit-level=moderate` passes.
- Focused frontend tests pass.
- Production build is attempted and any unrelated KEO-64 blocker is recorded honestly.
- Diff hygiene passes.

## Acceptance Criteria
- Lockfile contains patched nested PostCSS.
- No unrelated package versions change beyond resolver consequences of the override.
- Audit passes at the configured threshold.
- Only Allowed Files change.

## Evidence Required
- Resolved PostCSS version, audit output, focused test/build output, changed-file list, and `git diff --check`.

## Collision Risk
Medium: shared package manifest/lockfile. Integrate after KEO-64 review if that lane changes dependency files.

## Stop-and-Report Rule
Stop if remediation requires a Next.js downgrade, forced audit fix, broad lockfile churn, or files outside Allowed Files.

## Completion Evidence
- Clean install: `npm ci --prefer-offline --ignore-scripts --no-audit --no-fund` completed with 523 packages installed.
- Resolved production dependency: `next@16.2.6 -> postcss@8.5.10 overridden`.
- Production audit: `npm audit --omit=dev --audit-level=moderate` reported zero vulnerabilities.
- Focused validation: `pr2PublicCopyPolish.test.ts` passed 5 of 5 tests.
- Production build: compilation completed, then TypeScript stopped at the pre-existing KEO-64 `ToolsDecisionSurface.tsx` references to removed `reconstitutionInstructions` and `storageInstructions`. The dependency parcel did not modify application source; KEO-64 owns that repair.
- Lockfile review: only Next.js's nested PostCSS resolution, integrity, and its declared dependency ranges changed.
- Changed files remained within the parcel allowlist and `git diff --check` passed.

## Closeout
The security parcel is complete. After integrating KEO-64 repair `44eac22`, candidate code state `fb0ed84` passed a clean install, the full 900-test frontend suite, the production build, and the zero-vulnerability production audit with PostCSS 8.5.10.
