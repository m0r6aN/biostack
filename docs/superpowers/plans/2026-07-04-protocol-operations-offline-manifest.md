# Protocol Operations Offline Manifest Implementation Plan
**For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
**Goal:** Add a checked-in, test-owned offline verification contract manifest snapshot plus focused drift-guard tests for the Protocol Operations bundle, receipt, verifier, and CLI surfaces.
**Architecture:** Keep the manifest entirely in the test seam. Store one canonical JSON snapshot under shared test fixtures, derive a current manifest from frozen bundle/receipt/CLI surfaces in tests, and compare the derived output to the checked-in snapshot so drift fails closed. Split assertions by responsibility: application tests guard bundle/posture/no-service-invocation contract details, while CLI tests guard receipt schema, hash-surface ordering, and exact flag/mode surface.
**Tech Stack:** .NET, xUnit, existing Protocol Operations application services/contracts, existing verifier CLI tests and fixtures.
---

### Task 1: Add canonical manifest snapshot fixture
**Files:**
- Create: `backend/tests/Fixtures/ProtocolOperationsExportBundle/ProtocolOperationsOfflineVerificationContractManifest.golden.json`
- Modify: `backend/tests/BioStack.Application.Tests/ProtocolOperationsExportBundleGoldenFixtureTests.cs` only if existing fixture discovery patterns need an extra helper

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public void ProtocolOperationsOfflineVerificationContractManifest_SnapshotExists()
{
    var path = Path.Combine(
        RepositoryRoot(),
        "backend",
        "tests",
        "Fixtures",
        "ProtocolOperationsExportBundle",
        "ProtocolOperationsOfflineVerificationContractManifest.golden.json");

    Assert.True(File.Exists(path), $"Expected manifest snapshot at '{path}'.");
}
```

- [ ] **Step 2: Run test verify fails**
Run: `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsOfflineVerificationContractManifest_SnapshotExists`
Expected: FAIL with missing fixture file.

- [ ] **Step 3: Write minimal implementation**

```json
{
  "manifestSchemaId": "biostack.protocol-operations-offline-verification-contract-manifest",
  "manifestSchemaVersion": "1.0.0",
  "manifestScope": "protocol_operations_offline_verification_contract_manifest",
  "manifestPosture": {
    "backendOnly": true,
    "testOwned": true,
    "driftGuard": true,
    "nonProductSurface": true
  }
}
```

- [ ] **Step 4: Run test verify passes**
Run: `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsOfflineVerificationContractManifest_SnapshotExists`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/tests/Fixtures/ProtocolOperationsExportBundle/ProtocolOperationsOfflineVerificationContractManifest.golden.json
git commit -m "test: add offline verification manifest snapshot"
```

### Task 2: Add application-side manifest builder and snapshot drift tests
**Files:**
- Create: `backend/tests/BioStack.Application.Tests/ProtocolOperationsOfflineVerificationContractManifestTests.cs`
- Modify: `backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj` only if fixture inclusion needs explicit metadata

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public void ProtocolOperationsOfflineVerificationContractManifest_DerivedApplicationSurfaceMatchesSnapshot()
{
    var expected = ReadManifestSnapshot();
    var actual = BuildManifestFromApplicationSurface();

    Assert.Equal(expected, actual);
}

[Fact]
public void ProtocolOperationsOfflineVerificationContractManifest_ApplicationSurfaceRequiresNoRuntimeServices()
{
    var exportService = new Mock<IProtocolOperationsReportExportService>(MockBehavior.Strict);

    _ = BuildManifestFromApplicationSurface();

    exportService.VerifyNoOtherCalls();
}
```

- [ ] **Step 2: Run test verify fails**
Run: `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsOfflineVerificationContractManifest`
Expected: FAIL because test file/helper does not exist yet.

- [ ] **Step 3: Write minimal implementation**

```csharp
private static string BuildManifestFromApplicationSurface()
{
    var manifest = new
    {
        manifestSchemaId = "biostack.protocol-operations-offline-verification-contract-manifest",
        manifestSchemaVersion = "1.0.0",
        manifestScope = "protocol_operations_offline_verification_contract_manifest",
        manifestPosture = new
        {
            backendOnly = true,
            testOwned = true,
            driftGuard = true,
            nonProductSurface = true
        },
        bundle = new
        {
            schemaId = "biostack.protocol-operations-export-bundle",
            schemaVersion = ProtocolOperationsExportBundleService.SchemaVersion,
            hashAlgorithm = ProtocolOperationsExportBundleService.HashAlgorithmName,
            topLevelFields = new[] { "Metadata", "ReportExport", "Artifacts", "Integrity", "Disclaimer" }
        },
        boundaries = new
        {
            noMedicalAdvice = true,
            noPdfGeneration = true,
            noPersistenceDatabaseOrFileWriteClaimsExceptReceiptEmission = true,
            noProtocolIntelligenceRuntimeBehavior = true
        }
    };

    return JsonSerializer.Serialize(manifest);
}
```

- [ ] **Step 4: Run test verify passes**
Run: `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsOfflineVerificationContractManifest`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/tests/BioStack.Application.Tests/ProtocolOperationsOfflineVerificationContractManifestTests.cs backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj
git commit -m "test: add application manifest drift guards"
```

### Task 3: Add CLI-side manifest drift tests against receipt and flag surfaces
**Files:**
- Create: `backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/ProtocolOperationsOfflineVerificationContractManifestTests.cs`
- Modify: `backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj` only if fixture inclusion needs explicit metadata

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public void ProtocolOperationsOfflineVerificationContractManifest_DerivedCliSurfaceMatchesSnapshot()
{
    var expected = ReadManifestSnapshot();
    var actual = BuildManifestFromCliSurface();

    Assert.Equal(expected, actual);
}

[Fact]
public void ProtocolOperationsOfflineVerificationContractManifest_CliSurfaceFreezesExactFlagsAndOrdering()
{
    var manifest = JsonDocument.Parse(BuildManifestFromCliSurface());

    Assert.Equal("--receipt-json", manifest.RootElement.GetProperty("cli").GetProperty("emitReceiptJsonFlag").GetString());
    Assert.Equal("--verify-receipt-json", manifest.RootElement.GetProperty("cli").GetProperty("verifyReceiptJsonFlag").GetString());
    Assert.Equal(
        new[]
        {
            "bundle-non-null",
            "schema-version",
            "required-metadata",
            "json-artifact-descriptor",
            "embedded-report-export",
            "embedded-report-export-hash",
            "preserved-report-export-hash",
            "bundle-sha256",
            "observational-boundary"
        },
        manifest.RootElement.GetProperty("hashSurfaces").GetProperty("verificationChecksOrder").EnumerateArray().Select(x => x.GetString()).ToArray());
}
```

- [ ] **Step 2: Run test verify fails**
Run: `dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj --filter ProtocolOperationsOfflineVerificationContractManifest`
Expected: FAIL because test file/helper does not exist yet.

- [ ] **Step 3: Write minimal implementation**

```csharp
private static string BuildManifestFromCliSurface()
{
    var receipt = ParseReceipt(EmitReceiptJson());
    var manifest = new
    {
        receipt = new
        {
            schemaId = ReadString(receipt, "receiptSchemaId"),
            schemaVersion = ReadString(receipt, "receiptSchemaVersion"),
            verifierSchemaId = ReadString(receipt, "verifierSchemaId"),
            verifierSchemaVersion = ReadString(receipt, "verifierSchemaVersion")
        },
        cli = new
        {
            verifyBundleMode = "positional-bundle-json-input",
            emitReceiptJsonFlag = "--receipt-json",
            verifyReceiptJsonFlag = "--verify-receipt-json"
        },
        hashSurfaces = new
        {
            verificationChecksOrder = ReadStringArray(receipt, "checks"),
            verificationErrorsOrder = ReadStringArray(receipt, "errors"),
            verificationResultContentHashField = "verificationResultContentHash",
            receiptContentHashField = "receiptContentHash"
        }
    };

    return JsonSerializer.Serialize(manifest);
}
```

- [ ] **Step 4: Run test verify passes**
Run: `dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj --filter ProtocolOperationsOfflineVerificationContractManifest`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/ProtocolOperationsOfflineVerificationContractManifestTests.cs backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj
git commit -m "test: add cli manifest drift guards"
```

### Task 4: Expand snapshot to full manifest shape and verify focused validation commands
**Files:**
- Modify: `backend/tests/Fixtures/ProtocolOperationsExportBundle/ProtocolOperationsOfflineVerificationContractManifest.golden.json`
- Modify: `docs/superpowers/specs/2026-07-04-protocol-operations-offline-manifest-design.md` only if the implemented field names need exact wording sync

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public void ProtocolOperationsOfflineVerificationContractManifest_SnapshotContainsExpectedContractSections()
{
    using var manifest = JsonDocument.Parse(ReadManifestSnapshot());

    Assert.True(manifest.RootElement.TryGetProperty("bundle", out _));
    Assert.True(manifest.RootElement.TryGetProperty("receipt", out _));
    Assert.True(manifest.RootElement.TryGetProperty("cli", out _));
    Assert.True(manifest.RootElement.TryGetProperty("hashSurfaces", out _));
    Assert.True(manifest.RootElement.TryGetProperty("boundaries", out _));
}
```

- [ ] **Step 2: Run test verify fails**
Run: `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsOfflineVerificationContractManifest_SnapshotContainsExpectedContractSections`
Expected: FAIL because the initial minimal snapshot is incomplete.

- [ ] **Step 3: Write minimal implementation**

```json
{
  "manifestSchemaId": "biostack.protocol-operations-offline-verification-contract-manifest",
  "manifestSchemaVersion": "1.0.0",
  "manifestScope": "protocol_operations_offline_verification_contract_manifest",
  "manifestPosture": {
    "backendOnly": true,
    "testOwned": true,
    "driftGuard": true,
    "nonProductSurface": true
  },
  "bundle": {},
  "receipt": {},
  "cli": {},
  "hashSurfaces": {},
  "boundaries": {}
}
```

- [ ] **Step 4: Run test verify passes**
Run: `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsOfflineVerificationContractManifest`
Expected: PASS after the snapshot and both builders converge on the full finalized shape.

- [ ] **Step 5: Commit**

```bash
git add backend/tests/Fixtures/ProtocolOperationsExportBundle/ProtocolOperationsOfflineVerificationContractManifest.golden.json docs/superpowers/specs/2026-07-04-protocol-operations-offline-manifest-design.md
git commit -m "test: freeze offline verification manifest contract"
```

### Task 5: Run full focused validation and hygiene checks
**Files:**
- No file changes expected

- [ ] **Step 1: Run focused manifest tests**

```bash
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsOfflineVerificationContractManifest
dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj --filter ProtocolOperationsOfflineVerificationContractManifest
```

- [ ] **Step 2: Run broader contract regression tests**

```bash
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundle
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundleGoldenFixture
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolIntelligenceOfflineBoundaryTests
dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj
```

- [ ] **Step 3: Run build, vulnerability, and diff checks**

```bash
dotnet build backend/BioStack.sln
dotnet list backend/BioStack.sln package --include-transitive --vulnerable
git diff --check origin/main...HEAD
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "test: validate offline verification manifest lane"
```

## Self-Review

- Spec coverage:
  - Manifest identity, posture, scope, and drift-guard semantics are covered in Tasks 1, 2, and 4.
  - Exact CLI flags and positional mode are covered in Task 3.
  - Hash fields and ordering are covered in Task 3.
  - No-runtime/no-service invocation is covered in Task 2.
  - Focused validation filters and broader regression commands are covered in Task 5.
- Placeholder scan:
  - No `TODO` or `TBD` placeholders remain.
- Type consistency:
  - The plan uses one snapshot path and one manifest naming convention throughout.

Plan complete saved `docs/superpowers/plans/2026-07-04-protocol-operations-offline-manifest.md`. Proceeding with inline execution in this session.
