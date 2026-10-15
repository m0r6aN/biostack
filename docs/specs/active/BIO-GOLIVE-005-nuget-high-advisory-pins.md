---
ticket: BIO-GOLIVE-005
title: Pin patched cryptography packages so the deploy audit can restore
status: active
owner: clinton.morgan
created: 2026-10-14
updated: 2026-10-14
delivery_classes: [standard]
guidance_classes: []
substance_function_risk: []
function_review_status: not-applicable
surfaces:
  - backend/Directory.Build.props
routing_class: implementation
verification_class: component-testable
permission_profile: reviewer-readonly
data_classification: public
---

# BIO-GOLIVE-005 — Pin patched cryptography packages

## Objective

Deploy runs for PR #544 and PR #545 failed `dotnet restore` with NU1903 before Azure login. The high advisories are GHSA-846f-f764-76mx on `System.Security.Cryptography.Pkcs` and GHSA-7hqp-7c5m-4vq6, GHSA-c399-9853-6jmj, and GHSA-r955-8q5j-3h83 on `System.Security.Cryptography.Xml`. Published patched versions for the 10.0 line are 10.0.13. This parcel pins those packages so the restore audit can succeed. It does not deploy.

## Initiative (Product Lane)

`biostack-go-live`. A green restore is a precondition for a later human deploy. This parcel does not authorize that deploy and does not change the production-readiness NO-GO.

## Branch and Worktree

Proposed only. Gate 2 assigns both.

- Branch: `fix/bio-golive-005-nuget-advisory-pins`
- Worktree: `D:/Repos/BioStack/.worktrees/bio-golive-005-nuget-advisory-pins`

## Dependencies

- Independent of BIO-GOLIVE-001 and BIO-GOLIVE-004. Different files.
- NuGet.org must serve 10.0.13 of both packages. If either version is absent, the builder stops and reports that fact. The builder does not pick a different version.

## Verified Facts

1. Direct references to `System.Security.Cryptography.Xml` 10.0.10 exist in `BioStack.Infrastructure.csproj` and `BioStack.KnowledgeWorker.csproj`.
2. The deploy log also reported transitive `System.Security.Cryptography.Pkcs` 10.0.10 and, on the API project, 9.0.0.
3. GitHub Advisory Database lists 10.0.13 as the patched 10.0 release for both packages. Affected 10.0 versions are 10.0.0 through 10.0.12.
4. `.github/workflows/deploy.yml` fails the job when restore emits NU1903 or NU1904.

## Surfaces

- Add `backend/Directory.Build.props` with direct package references for `System.Security.Cryptography.Pkcs` and `System.Security.Cryptography.Xml` at version 10.0.13, so every project under `backend/` restores the patched versions.
- Do not edit the two existing direct 10.0.10 references unless the restore still selects 10.0.10 after the props file. If that happens, the builder changes those two version attributes to 10.0.13 and no other package versions.

## Contracts

- No application behavior change is intended.
- The pin is 10.0.13 exactly. No floating version.
- Package versions other than these two are unchanged.

## Acceptance Criteria

1. `dotnet restore backend/BioStack.sln --force-evaluate -p:NuGetAudit=true -p:NuGetAuditMode=all -p:NuGetAuditLevel=high -p:WarningsAsErrors=NU1903%3BNU1904` exits 0.
2. The restore graph no longer contains Pkcs or Xml versions in the affected ranges named above.
3. No other PackageReference version attribute changes.

## Tests

1. The restore command in acceptance criterion 1 is the test. The builder saves the command output under the worktree evidence note in the session handoff, without secrets.
2. No new unit test is required. Cryptography package behavior is covered by the patched upstream packages.

## Rollback

Delete `backend/Directory.Build.props` and restore the two 10.0.10 attributes if they were edited. The deploy audit will fail again. No data migration.

## Out of Scope

- Runtime or SDK installs on GitHub-hosted runners.
- ACR admin, OpenAPI, rate limits, and checkout.
- A production deploy.

## Allowed Files

- `backend/Directory.Build.props`
- `backend/src/BioStack.Infrastructure/BioStack.Infrastructure.csproj`
- `backend/src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj`

The two csproj files are allowed only for the version-attribute fallback in Surfaces.

## Forbidden

- Changing any other package version.
- Adding a `NoWarn` for NU1903 or NU1904.
- Disabling `NuGetAudit`.
- Deploying, printing secrets, or editing application source.
