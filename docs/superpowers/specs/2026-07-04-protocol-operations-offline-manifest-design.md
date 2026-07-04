# Protocol Operations Offline Verification Contract Manifest Design

## Goal

Add a deterministic, backend-only manifest contract for the Protocol Operations offline verification chain. The manifest must describe the frozen offline trust surfaces without adding new product/runtime behavior.

## Scope

This design covers PR #149 only.

In scope:
- A machine-readable manifest for the offline verification contract.
- Focused tests that fail on drift between the manifest and current bundle, receipt, and CLI surfaces.
- Boundary assertions for offline-only, observational, non-medical behavior.

Out of scope:
- API endpoints
- Frontend work
- PDF generation
- Persistence or database access
- Export-generation service changes
- Protocol Intelligence runtime behavior

## Recommended Approach

Use a checked-in, test-owned manifest JSON snapshot plus a test-owned manifest builder that derives current values from frozen bundle, receipt, verifier, and CLI surfaces.

Rationale:
- Keeps the new seam narrow and additive.
- Avoids widening production responsibilities unless a minimal shared helper is clearly justified.
- Fits the directive to prefer test-owned manifest generation.
- Reduces collision risk with nearby CLI and documentation lanes.
- Prevents a self-validating generator from drifting silently with underlying constants.

Tests must compare the derived manifest to the checked-in snapshot so schema, CLI, hash-surface, and boundary drift fails closed unless the snapshot update is intentional.

The manifest is a contract drift guard only. It must not introduce API endpoints, runtime behavior, persistence, export generation, PDF generation, frontend UI, or medical-advice behavior.

## Manifest Contract

The manifest should describe:

- Manifest schema id and version
- Manifest scope/name, for example `protocol_operations_offline_verification_contract_manifest`
- Manifest posture: backend-only, test-owned, drift guard, non-product surface
- Bundle schema id and version
- Receipt schema id and version
- Supported CLI operations:
  - verify bundle JSON using the existing positional bundle JSON input mode
  - emit receipt JSON using `--receipt-json`
  - verify receipt JSON using `--verify-receipt-json`
- Deterministic hash surfaces:
  - bundle content hash
  - embedded report export content hash
  - verification result content hash
  - receipt content hash
  - verification checks ordering
  - verification errors ordering
- Required boundary categories:
  - no medical advice
  - no PDF generation
  - no persistence, database access, or file-write claims except explicit CLI `--receipt-json` receipt-emission output behavior
  - no Protocol Intelligence runtime behavior

## Source Of Truth

The manifest should derive from current frozen surfaces where possible:

- Bundle schema/version, hash algorithm, and serialized field names from existing bundle contracts, golden fixtures, and contract tests
- Receipt verification shape from the receipt JSON writer/verifier and current contract snapshot tests
- CLI operation surface from the verifier CLI flags and round-trip/offline-contract tests
- Boundary language from existing bundle/verifier disclaimers and offline boundary tests

The manifest must not become an independent product contract that can drift from these source surfaces silently.

## Test Strategy

Add focused tests that:

- Assert the manifest shape and serialized contents are deterministic
- Assert manifest schema/version entries match bundle and receipt constants
- Assert manifest CLI operation declarations match the actual supported CLI flags and modes
- Assert manifest hash-surface declarations match the current bundle/receipt verification fields
- Assert manifest boundary declarations match current offline-only guardrails
- Assert manifest generation does not call export-generation services, persistence services, API handlers, or Protocol Intelligence runtime services
- Fail closed on drift without requiring endpoints, persistence, or runtime service calls

Preferred placement:

- Application tests for bundle/verification contract alignment
- CLI tests for receipt/flag/mode alignment

## Implementation Notes

- Prefer creating the manifest payload inside tests unless a tiny shared helper materially reduces duplication.
- If a shared helper is introduced, keep it pure, deterministic, and contract-only.
- Keep string assertions exact where the boundary wording is intentionally frozen.
- Reuse the existing golden fixture and receipt emission helpers where practical.

## Validation

Run:

```powershell
dotnet build backend/BioStack.sln
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsOfflineVerificationContractManifest
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundle
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundleGoldenFixture
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolIntelligenceOfflineBoundaryTests
dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj --filter ProtocolOperationsOfflineVerificationContractManifest
dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj
dotnet list backend/BioStack.sln package --include-transitive --vulnerable
git diff --check origin/main...HEAD
```

## Self-Review

- No new endpoint, UI, PDF, persistence, or runtime Protocol Intelligence scope was added.
- The design stays inside the frozen offline verification seam.
- The manifest is positioned as a drift guard, not a new public-facing capability.
