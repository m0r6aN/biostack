# Protocol Operations Offline Trust Chain Capstone Guard Implementation Plan
**For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
**Goal:** Add a narrow PR #153 capstone guard that freezes offline verification schema, CLI mode, and docs boundary alignment without broadening production behavior.
**Architecture:** Extend the existing offline verification manifest pattern with focused guard tests that compare the golden manifest snapshot, the verifier README, and the repo README safety posture. Keep implementation doc-first and test-owned unless a failing assertion proves a small docs adjustment is required.
**Tech Stack:** .NET 10, xUnit, JSON manifest snapshots, repo-local Markdown docs
---

### Task 1: Freeze capstone snapshot and repo safety posture
**Files:**
- Create: `backend/tests/BioStack.Application.Tests/ProtocolOperationsOfflineVerificationCapstoneGuardTests.cs`
- Test: `backend/tests/Fixtures/ProtocolOperationsExportBundle/ProtocolOperationsOfflineVerificationContractManifest.golden.json`
- Reference: `README.md`

- [x] **Step 1: Write failing test**
```csharp
[Fact]
public void ContractManifestSnapshot_FreezesOfflineVerificationSchemaIdentifiers()
{
    using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestSnapshotPath()));
    var bundle = manifest.RootElement.GetProperty("bundle");
    var receipt = manifest.RootElement.GetProperty("receipt");

    Assert.Equal("biostack.protocol-operations-export-bundle", bundle.GetProperty("schemaId").GetString());
    Assert.Equal(ProtocolOperationsExportBundleService.SchemaVersion, bundle.GetProperty("schemaVersion").GetString());
    Assert.Equal("biostack.protocol-operations-export-bundle.verification-receipt", receipt.GetProperty("receiptSchemaId").GetString());
}
```

- [x] **Step 2: Run test verify fails or proves existing coverage**
Run: `rtk test dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsOfflineVerificationCapstoneGuardTests`
Expected: Either a targeted failure showing schema/posture drift or a passing result proving the capstone is already aligned on the app side.

- [x] **Step 3: Write minimal implementation**
```csharp
[Fact]
public void RepoReadme_PreservesEducationalObservationalSafetyPosture()
{
    var readme = File.ReadAllText(Path.Combine(RepositoryRoot(), "README.md"));

    Assert.Contains("BioStack is for educational and observational use only.", readme, StringComparison.Ordinal);
    Assert.Contains("Mathematical Logic Only", readme, StringComparison.Ordinal);
}
```

- [x] **Step 4: Run test verify passes**
Run: `rtk test dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsOfflineVerificationCapstoneGuardTests`
Expected: `Passed!` with 2/2 tests green.

### Task 2: Freeze CLI README mode and boundary alignment
**Files:**
- Create: `backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/ProtocolOperationsOfflineVerificationReadmeCapstoneGuardTests.cs`
- Modify: `backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/README.md`
- Test: `backend/tests/Fixtures/ProtocolOperationsExportBundle/ProtocolOperationsOfflineVerificationContractManifest.golden.json`

- [x] **Step 1: Write failing test**
```csharp
[Fact]
public void CliReadme_PreservesLockedOfflineBoundaryClaims()
{
    var readme = File.ReadAllText(CliReadmePath());

    Assert.Contains("does not make PDF authenticity claims", readme, StringComparison.Ordinal);
    Assert.Contains("does not verify persistence or database state", readme, StringComparison.Ordinal);
}
```

- [x] **Step 2: Run test verify fails**
Run: `rtk test dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj --filter ProtocolOperationsOfflineVerificationReadmeCapstoneGuardTests`
Expected: `Assert.Contains()` failure for the missing README guarantee text.

- [x] **Step 3: Write minimal implementation**
```md
- does not generate PDFs
- does not make PDF authenticity claims
- does not access persistence/database
- does not verify persistence or database state
```

- [ ] **Step 4: Run test verify passes**
Run: `rtk test dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj --filter ProtocolOperationsOfflineVerificationReadmeCapstoneGuardTests`
Expected: `Passed!` with 2/2 tests green.

### Task 3: Run PR #153 validation set
**Files:**
- Verify only: `backend/BioStack.sln`
- Verify only: `backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj`
- Verify only: `backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj`

- [ ] **Step 1: Run build**
```bash
rtk err dotnet build backend/BioStack.sln
```

- [ ] **Step 2: Run focused application tests**
```bash
rtk test dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundle
rtk test dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundleGoldenFixture
rtk test dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolIntelligenceOfflineBoundaryTests
```

- [ ] **Step 3: Run CLI tests**
```bash
rtk test dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj
```

- [ ] **Step 4: Run dependency and diff hygiene checks**
```bash
rtk summary dotnet list backend/BioStack.sln package --include-transitive --vulnerable
rtk git diff --check origin/main...HEAD
```
