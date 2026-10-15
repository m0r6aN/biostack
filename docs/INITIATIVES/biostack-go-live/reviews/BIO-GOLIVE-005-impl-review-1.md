# BIO-GOLIVE-005 implementation review

- Reviewer: impl-review-1
- Date: 2026-10-14
- Spec: `docs/specs/active/BIO-GOLIVE-005-nuget-high-advisory-pins.md`
- Commit: `d285e4fac26cccad8f1ff74e9b954ecf337d1869`
- Parent / Gate 2 base: `5b686e69536f2eedf0195ee77466f20b08a490d8`
- Branch: `fix/bio-golive-005-nuget-advisory-pins`
- Worktree: `D:\Repos\BioStack\.worktrees\bio-golive-005-nuget-advisory-pins`
- Author: m0r6aN `<morganclint76@gmail.com>`. This reviewer did not write the commit.
- Verdict: **PASS**

The only product change is new `backend/Directory.Build.props`, which pins `System.Security.Cryptography.Pkcs` and `System.Security.Cryptography.Xml` to exact `10.0.13`. There is no `NoWarn` and no `NuGetAudit` disable. A fresh worktree restore of the spec command exited 0, and every solution `project.assets.json` resolves both packages only at 10.0.13. This review does not authorize a production deploy.

## Definition of Done

| item | status | evidence |
| --- | --- | --- |
| AC1. Spec restore exits 0 with audit on and NU1903/NU1904 as errors | Pass | Fresh `dotnet restore` in the 005 worktree, exit 0. No NU1903 or NU1904. |
| AC2. Restore graph has no Pkcs or Xml package in 10.0.0–10.0.12 | Pass | All 14 solution assets files contain only `System.Security.Cryptography.Pkcs/10.0.13` and `System.Security.Cryptography.Xml/10.0.13` as package identities. |
| Gate 2 note: resolved Pkcs is not 9.0.0–9.0.20 | Pass | No `Pkcs/9.*` library identity. Api's previous resolved Pkcs 9.0.0 is now 10.0.13. |
| AC3. No other PackageReference version attribute changes | Pass | `git diff --name-status` against the parent is only `A backend/Directory.Build.props`. The two `Xml` `Version="10.0.10"` attributes are unchanged. |
| No `NoWarn` for NU1903 or NU1904 | Pass | The new file is six lines and has no `NoWarn`. No other file changed. |
| `NuGetAudit` not disabled | Pass | Props file does not set `NuGetAudit`. `backend/NuGet.Config` is unchanged and does not disable audit. The acceptance command passed `-p:NuGetAudit=true`. |
| Allowed files only; no application source; no deploy | Pass | Csproj fallback was not used. No `.cs` edit. `deploy.yml` was not edited. |

## Findings

| id | severity | location | finding |
| --- | --- | --- | --- |
| N1 | nit | `BioStack.Infrastructure.csproj` line 19; `BioStack.KnowledgeWorker.csproj` line 22 | Restore warns NU1504 because those projects still include `System.Security.Cryptography.Xml` 10.0.10 beside the props pin at 10.0.13. SDK 10.0.402 kept 10.0.13. The spec says to edit those attributes only if restore still selects 10.0.10, so leaving them matches AC3. NU1504 is not in `WarningsAsErrors`, and the deploy step has the same error list. |
| N2 | nit | `BioStack.Api.csproj` during restore | One NU1510 warning says the Xml `PackageReference` will not be pruned. The Api assets file still resolves Xml and Pkcs at 10.0.13. Do not remove the pin to silence NU1510. |

No blocker or major.

Optional cleanup for N1, not required for this verdict. It would also match the Gate 2 nit, but it is outside the spec's fallback condition because 10.0.10 was not selected:

```xml
<PackageReference Include="System.Security.Cryptography.Xml" Version="10.0.13" />
```

Apply that only on the two existing direct includes. Do not change any other package version.

## Verified facts

- Commit `d285e4fac26cccad8f1ff74e9b954ecf337d1869` adds only:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="System.Security.Cryptography.Pkcs" Version="10.0.13" />
    <PackageReference Include="System.Security.Cryptography.Xml" Version="10.0.13" />
  </ItemGroup>
</Project>
```

- Versions are exact. There is no floating version, no `NoWarn`, and no audit property.
- `git diff --check` against the parent produced no output. Worktree `git status --short` was empty before and after the worktree restore. `obj/` assets are ignored.
- The only `Directory.Build.props` in the worktree is `backend/Directory.Build.props`. No project file under `backend/` sets `ImportDirectoryBuildProps`, `ManagePackageVersionsCentrally`, `NoWarn`, `NuGetAudit`, or `WarningsAsErrors`.
- Direct `Xml` 10.0.10 references remain at Infrastructure line 19 and KnowledgeWorker line 22. Pkcs is not otherwise a direct csproj reference.
- NU1504 lists the duplicate as Xml 10.0.13, then Xml 10.0.10, which matches props-before-project-body order. Resolved library keys are 10.0.13 only.
- `"System.Security.Cryptography.Pkcs": "9.0.0"` on Api is the minimum requested by `Microsoft.Build.Tasks.Core` 17.14.28. `"7.0.2"` is the minimum requested by `Microsoft.CodeAnalysis.Workspaces.MSBuild` 4.14.0. Those are constraints, not restored packages. The project dependency section requests `10.0.13`, and the library path is `system.security.cryptography.pkcs/10.0.13`.
- Infrastructure likewise still records older Xml minima from other packages and resolves `System.Security.Cryptography.Xml/10.0.13`.
- A restore started in `D:\Repos\BioStack` before the working directory was set to the worktree. That tree has no pin, so it failed NU1903 on Pkcs 10.0.10, Xml 10.0.10, and Api Pkcs 9.0.0. `git diff --stat -- backend` there was empty. That run is not the acceptance result.

## Commands

`rtk` was not on PATH. Commands ran without it.

- `git show --stat --format=fuller d285e4fac26cccad8f1ff74e9b954ecf337d1869` → 1 file, 6 insertions. Author m0r6aN, 2026-10-14 19:08:48 -0400.
- `git diff --name-status 5b686e69536f2eedf0195ee77466f20b08a490d8 d285e4fac26cccad8f1ff74e9b954ecf337d1869` → `A backend/Directory.Build.props`.
- `git diff --check` for that range → no output.
- Discarded wrong-tree restore from `D:\Repos\BioStack`: exit 1, NU1903 on unpinned 10.0.10 and Api Pkcs 9.0.0. Not a result for this commit.
- Fresh acceptance command, cwd `D:\Repos\BioStack\.worktrees\bio-golive-005-nuget-advisory-pins`:

```text
dotnet restore backend/BioStack.sln --force-evaluate -p:NuGetAudit=true -p:NuGetAuditMode=all -p:NuGetAuditLevel=high -p:WarningsAsErrors=NU1903%3BNU1904
```

Trimmed output:

```text
warning NU1504: Duplicate 'PackageReference' items found. ... System.Security.Cryptography.Xml 10.0.13, System.Security.Cryptography.Xml 10.0.10.
  (BioStack.Infrastructure.csproj and BioStack.KnowledgeWorker.csproj)
warning NU1510: PackageReference System.Security.Cryptography.Xml will not be pruned. ... BioStack.Api.csproj
Restored all 14 solution projects.
EXIT:0
```

- Package identity tally across those 14 `project.assets.json` files: `System.Security.Cryptography.Pkcs/10.0.13` 42, `System.Security.Cryptography.Xml/10.0.13` 42. No other slash-form version. `dotnet --version` → `10.0.402`.

## Fix checklist

None. N1 and N2 do not block the commit.
