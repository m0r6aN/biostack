# Parcel: KEO-65-NUGET-HIGH-ADVISORY-GATE-030

## Goal
Remove the known high-severity `System.Security.Cryptography.Xml` advisories and make pull-request CI reject future high-or-critical NuGet advisories.

## Initiative
biostack-production-readiness

## Project Track
Security / backend dependencies

## Wave
Release hardening

## Branch
codex/nuget-high-advisory-gate-20260727

## Worktree
D:/Repos/BioStack-nuget-high-advisory-gate-20260727

## Dependencies
- Current production-readiness baseline `2807b95f77b9ae8670458a95da62bc486e0f2cf0`.
- KEO-65 defensive security review and the historical `dependency-hygiene/nuget-vulnerabilities` remediation already present on `main`.

## Integration Surfaces
- Backend solution restore -> NuGet advisory database.
- Pull-request validation -> `.github/workflows/deploy.yml`.

## Security Gate
Security review required before merge. Pull-request CI must fail closed when restore reports `NU1903` or `NU1904` for direct or transitive backend dependencies.

## Allowed Files
- `.github/workflows/deploy.yml`
- `backend/src/BioStack.Infrastructure/BioStack.Infrastructure.csproj`
- `backend/src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj`
- `docs/INITIATIVES/biostack-production-readiness/PARCELS.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-65-NUGET-HIGH-ADVISORY-GATE-030.md`
- `docs/INITIATIVES/biostack-production-readiness/routing-events/KEO-65-NUGET-HIGH-ADVISORY-GATE-030.json`

If a required file is not listed, stop and amend this spec before editing or creating it.

## Forbidden
- Do not update unrelated packages or generate lockfiles.
- Do not suppress NuGet advisory warnings.
- Do not weaken the existing frontend dependency audit.
- Do not merge, deploy, or mutate production.

## Out of Scope
Moderate-or-lower dependency advisories, frontend packages, source changes, and production release actions.

## Existing Patterns To Follow
- `.github/workflows/deploy.yml` - existing pull-request build, test, and frontend production-audit gate.
- `docs/INITIATIVES/biostack-production-readiness/parcels/SEC-DEPS-001.md` - narrow dependency remediation and evidence format.

## Contract
- Both direct references to `System.Security.Cryptography.Xml` resolve to stable version `10.0.10`.
- CI audits direct and transitive backend dependencies at restore time.
- `NU1903` (high) and `NU1904` (critical) are errors; lower advisory levels remain visible without widening this parcel's gate.

## Required Tests
- Prove the configured restore gate fails against the `10.0.9` baseline.
- Prove the same restore gate passes after the `10.0.10` upgrade.
- Run `dotnet list backend/BioStack.sln package --include-transitive --vulnerable`.
- Build the backend solution.
- Run the affected Infrastructure and KnowledgeWorker test projects.
- Validate workflow YAML, routing-event JSON Schema, changed-file scope, and diff hygiene.

## Acceptance Criteria
- No vulnerable backend package is reported after restore.
- The backend solution builds and the affected tests pass.
- Pull-request CI contains an explicit high-or-critical NuGet advisory gate.
- Only Allowed Files change.

## Verification
- `dotnet restore backend/BioStack.sln --force-evaluate -p:NuGetAudit=true -p:NuGetAuditMode=all -p:NuGetAuditLevel=high -p:WarningsAsErrors=NU1903%3BNU1904`
- `dotnet list backend/BioStack.sln package --include-transitive --vulnerable`
- `dotnet build backend/BioStack.sln --no-restore`
- `dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore`
- `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore`

## Evidence Required
- Before/after advisory-gate output.
- Vulnerable-package scan, build, and test output.
- Workflow and JSON Schema validation.
- Independent bounded diff review.
- Changed-file list and `git diff --check`.

## Collision Risk
High: `.github/workflows/deploy.yml` is a shared serialization point. Rebase onto current `origin/main` before publishing.

## PR Notes
- What changed: upgrade the vulnerable cryptography XML dependency and add a fail-closed backend advisory restore gate.
- Why: `10.0.9` currently emits multiple high-severity `NU1903` findings while CI allows them as warnings.
- Risk: restore behavior in shared CI; limited to high and critical NuGet advisories.
- Verification: reproduce the baseline failure, rerun after upgrade, scan, build, test, schema-check, and independently review.
- Evidence: parcel handoff and routing event.

## Session Handoff
- Starting commit: `2807b95f77b9ae8670458a95da62bc486e0f2cf0` after the required rebase from the original dispatch baseline `fa86b99626fa1e3d6939e62882444334f624c720`
- Ending commit: branch HEAD (see local handoff)
- Files changed: the six files listed in Allowed Files
- Commands run: fail-closed restore negative control, post-upgrade audited restore, transitive vulnerable-package scan, solution build, API tests, KnowledgeWorker tests, YAML/JSON validation, diff checks, and bounded local Qwen review
- Tests passed: audited restore; zero vulnerable packages across all solution projects; solution build with zero errors; API 299/299; KnowledgeWorker 866/866; workflow and routing-event validation; Qwen review `PASS`
- Tests failed: no final validation failures; the intentional `10.0.9` negative control exited 1 with `NU1903` as required
- Decisions needed: none
- Blockers: none
- Next safe action: coordinator publishes the local commit as a draft pull request and waits for hosted CI.
- Do not touch: unrelated packages, application source, production, or the historical dependency-hygiene worktree.

## Stop-and-Report Rule
If remediation requires a broad dependency update, advisory suppression, files outside Allowed Files, or a release mutation, stop and report it.
