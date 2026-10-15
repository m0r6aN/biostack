# BIO-GOLIVE-005 spec review

- Reviewer: spec-review-1
- Date: 2026-10-14
- Spec: `docs/specs/active/BIO-GOLIVE-005-nuget-high-advisory-pins.md`
- Spec blob: untracked working tree, `git hash-object` `6cbb70b87db2a4f126b3c826e75216f38ecc35d5`
- Tree: HEAD `5b686e69536f2eedf0195ee77466f20b08a490d8`
- Verdict: **PASS**

No blocker or major. The pin version 10.0.13 is the patched 10.0 release for every advisory the spec names. `backend/Directory.Build.props` would apply to every project in `backend/BioStack.sln`, including test projects and the in-solution tool, and every backend project targets `net10.0`, which both 10.0.13 packages support. This review does not authorize a production deploy or go-live.

## Checks

| check | status | evidence |
| --- | --- | --- |
| Direct `System.Security.Cryptography.Xml` 10.0.10 references exist | Pass | `BioStack.Infrastructure.csproj` line 19 and `BioStack.KnowledgeWorker.csproj` line 22. No other `System.Security.Cryptography` package reference in a csproj. |
| `deploy.yml` fails the job on NU1903 | Pass | `.github/workflows/deploy.yml` lines 59–61. No `continue-on-error`. Azure login is later, lines 102–109. |
| `backend/Directory.Build.props` would apply to every project in `backend/BioStack.sln`, including tests and tools | Pass | All 14 solution projects live under `backend/`. No nearer `Directory.Build.props`, and no project sets `ImportDirectoryBuildProps`. |
| No project targets a framework that cannot reference the 10.0.13 packages | Pass | All 15 backend csproj files are `net10.0` only. Both nuspecs have a `net10.0` dependency group. |
| Spec forbids `NoWarn` and disabling the audit | Pass | Forbidden, lines 92–93. Allowed files do not include `deploy.yml`. |
| 10.0.13 is the patched 10.0 version | Pass | GitHub Advisory Database, fetched 2026-10-14. NuGet.org serves both packages. |

## Findings

| id | severity | location | finding |
| --- | --- | --- | --- |
| N1 | nit | Fact 3 (line 46); AC2 (line 63) | AC2's "affected ranges named above" are only 10.0.0 through 10.0.12. [GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx) also marks Pkcs `>= 9.0.0, <= 9.0.20` as high, and Fact 2 says the API project resolved 9.0.0. AC1 still fails closed on that 9.0.0 package, and a direct 10.0.13 reference is outside both ranges. Naming the 9.0 range in AC2 would match the advisory. It is not required for this verdict. |
| N2 | nit | Surfaces (line 52); SDK `CollectPackageReferences` | Leaving the two `Version="10.0.10"` includes in place emits NU1504. `CheckForDuplicateNuGetItemsTask` in SDK 10.0.402 keeps the first item. `Directory.Build.props` is imported before the project body, so command-line restore keeps 10.0.13 and the spec's fallback does not run. NU1504 is not in `WarningsAsErrors`, so AC1 can still exit 0. |

## Verified facts

- Direct references are exactly `System.Security.Cryptography.Xml` `Version="10.0.10"` in `backend/src/BioStack.Infrastructure/BioStack.Infrastructure.csproj` and `backend/src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj`. Pkcs is not a direct reference.
- `deploy.yml` step "Reject high-severity backend dependency advisories" runs, with `working-directory: backend`, `dotnet restore BioStack.sln --force-evaluate -p:NuGetAudit=true -p:NuGetAuditMode=all -p:NuGetAuditLevel=high -p:WarningsAsErrors=NU1903%3BNU1904`. `%3B` is the MSBuild command-line escape for `;`. The step has no `if:` and no `continue-on-error`, so a non-zero restore fails the job before Azure login. The spec's acceptance command is the same restore from the repo root (`dotnet restore backend/BioStack.sln` plus the same `-p:` flags). This review did not re-execute the hosted runs.
- `backend/BioStack.sln` contains 14 SDK projects: 8 under `src/`, 5 under `tests/`, and `tools/BioStack.ProtocolOperationsExportBundleVerifierCli`. `backend/tools/BioStack.StructuralEvaluationReportCli` is the only backend csproj not in the solution. There is no `.fsproj`, `.vbproj`, or other project file under `backend/`.
- No `Directory.Build.props`, `Directory.Build.targets`, or `Directory.Packages.props` exists under `backend/`. No project sets `ImportDirectoryBuildProps`, `ManagePackageVersionsCentrally`, `TargetFrameworks`, or `TreatWarningsAsErrors`. There is no `packages.lock.json`. `backend/NuGet.Config` clears sources and then adds the vendored `packages` folder and `https://api.nuget.org/v3/index.json`. It does not disable NuGet audit.
- MSBuild imports the nearest `Directory.Build.props` while walking upward from the project file. A new `backend/Directory.Build.props` is that file for every project under `backend/`, including every solution project, every test project, the in-solution verifier CLI, and the out-of-solution structural-evaluation CLI. SDK-style projects import it from the SDK props, before the project body. `Microsoft.NET.Sdk`, `Microsoft.NET.Sdk.Web` (Api), and `Microsoft.NET.Sdk.Worker` (KnowledgeWorker) all do this.
- Every one of those 15 projects sets `<TargetFramework>net10.0</TargetFramework>` only. The 10.0.13 nuspec for Pkcs has dependency groups for `net10.0` (empty), `net9.0`, `net8.0`, `.NETStandard2.1`, `.NETStandard2.0`, and `.NETFramework4.6.2`. The 10.0.13 nuspec for Xml has `net10.0` (depends on Pkcs 10.0.13), `net9.0`, `net8.0`, `.NETStandard2.0`, and `.NETFramework4.6.2`. None of the backend projects target a framework outside that support. On `net10.0`, the pin does not pull `Microsoft.Bcl.Cryptography`.
- Central package management is off, so a `PackageReference` `Version="10.0.13"` in `Directory.Build.props` is a direct reference. For Infrastructure and KnowledgeWorker, that item is first and the existing 10.0.10 item is second. Command-line restore deduplicates to the first, which is 10.0.13. See N2.
- Advisory pages and the GitHub Advisory API, read on 2026-10-14:
  - [GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx) (`System.Security.Cryptography.Pkcs`, high): `>= 10.0.0, <= 10.0.12` patched by 10.0.13. Also `>= 9.0.0, <= 9.0.20` patched by 9.0.21, and `>= 8.0.0, <= 8.0.1` patched by 8.0.2. 10.0.13 is outside the vulnerable range.
  - [GHSA-7hqp-7c5m-4vq6](https://github.com/advisories/GHSA-7hqp-7c5m-4vq6), [GHSA-c399-9853-6jmj](https://github.com/advisories/GHSA-c399-9853-6jmj), and [GHSA-r955-8q5j-3h83](https://github.com/advisories/GHSA-r955-8q5j-3h83) (`System.Security.Cryptography.Xml`, high): each lists `>= 10.0.0, <= 10.0.12` patched by 10.0.13, plus the 11.0.0-rc.2, 8.0.5, and 9.0.21 lines.
  - The Xml advisory API page lists no high range that contains 10.0.13. Older high Xml ranges stop at 10.0.5, 10.0.9, or 10.0.12. The other high Pkcs advisory returned with the high filter, GHSA-555c-2p6r-68mm, is 6.x/7.x only.
- Both packages are published. `https://api.nuget.org/v3-flatcontainer/system.security.cryptography.pkcs/10.0.13/system.security.cryptography.pkcs.nuspec` and the Xml nuspec at the same version returned the 10.0.13 manifests.
- The spec forbids adding a `NoWarn` for NU1903 or NU1904, forbids disabling `NuGetAudit`, forbids any other package-version change, and forbids deploying. AC1 turns the audit on and promotes NU1903 and NU1904 to errors. AC2 requires the restore graph to drop the named vulnerable versions, so a suppress-only edit would still fail the acceptance criteria.
- No `.cs` file references `SignedXml`, `SignedCms`, or `EncryptedXml`. The only matches are the two package references above.
- `docs/specs/scripts/validate-spec.ps1` with the repo-relative spec path exited 0: shape `ticket-spec`, result `valid`, reason null. An absolute `-SpecPath` does not match the `docs/specs/active/*.md` pattern and is not a spec defect.

## Commands

- `git rev-parse HEAD` → `5b686e69536f2eedf0195ee77466f20b08a490d8`
- `git hash-object docs/specs/active/BIO-GOLIVE-005-nuget-high-advisory-pins.md` → `6cbb70b87db2a4f126b3c826e75216f38ecc35d5`
- `git status --short` for that spec → `?? docs/specs/active/BIO-GOLIVE-005-nuget-high-advisory-pins.md`
- Backend csproj count: 15, all `net10.0`. No `Directory.Build.props` under `backend/`. `backend/NuGet.Config` adds nuget.org.
- `validate-spec.ps1 -SpecPath docs/specs/active/BIO-GOLIVE-005-nuget-high-advisory-pins.md -RepoRoot D:\Repos\BioStack` → exit 0, `result` `valid`. `rtk` was not on PATH.
- Advisory and nuspec URLs above were fetched read-only. Restore was not run, so this review did not rewrite `obj/`.

## Fix checklist

None. N1 and N2 do not block the spec.
