# Protocol Operations Offline Verification Release Checklist

Use this checklist before shipping changes to the Protocol Operations offline verification kit. The release gate is intentionally boring, deterministic, and boundary-first.

## Required Validation Commands

Run from repository root:

```bash
dotnet build backend/BioStack.sln
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundle
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundleGoldenFixture
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolIntelligenceOfflineBoundaryTests
dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj
dotnet list backend/BioStack.sln package --include-transitive --vulnerable
git diff --check origin/main...HEAD
```

## Required Snapshot And Contract Checks

- Confirm the offline verification contract manifest snapshot still matches the bundle, receipt, and CLI surfaces.
- Confirm bundle schema id/version and receipt schema id/version stay aligned with the checked-in contract snapshot.
- Confirm the ordered verification checks and ordered error behavior remain deterministic.

## Required CLI Checks

- Verify bundle JSON in human-readable mode.
- Verify bundle JSON and emit receipt JSON with `--receipt-json`.
- Verify receipt JSON with `--verify-receipt-json`.
- Confirm receipt verification stays supplied-artifact-only and does not require the original bundle file.
- Confirm no extra file writes occur unless a caller explicitly redirects stdout or uses an already-supported output path.

## Required Boundary-Language Review

Perform a boundary-language review before release. Confirm the offline verifier docs, help text, and result surfaces do not introduce:

- medical advice
- dosing guidance
- diagnosis
- treatment
- prescription language
- PDF authenticity claims
- persistence/database-state verification claims
- Protocol Intelligence runtime/user-facing claims

## Reviewer Checklist

- Reviewer re-ran the required validation commands or inspected fresh CI evidence for the same commands.
- Reviewer confirmed the docs still describe exactly the supported offline operations.
- Reviewer confirmed the CLI remains local-file-only and receipt verification remains supplied-artifact-only.
- Reviewer confirmed contract and snapshot changes are intentional, minimal, and explained in the PR.
- Reviewer confirmed no sample personal health data was added to docs, fixtures, or examples.

## Allowed Changes

- deterministic verifier bug fixes
- contract snapshot updates that match intentional bundle, receipt, or CLI changes
- offline-kit documentation improvements
- test-only guard expansions
- machine-readable automation output that stays local, deterministic, and boundary-clean

## Forbidden Changes

- medical advice
- dosing guidance
- diagnosis, treatment, or prescription language
- PDF generation
- PDF authenticity claims
- persistence/database-state verification claims
- Protocol Intelligence runtime/user-facing claims
- network calls from offline verification paths
- sample personal health data
- frontend, website, or marketing surface expansion for this kit

## Release Blockers

Block the release if any of the following appear in the diff, docs, tests, or CLI behavior:

- medical advice
- dosing guidance
- diagnosis
- treatment
- prescription language
- PDF generation
- PDF authenticity claims
- persistence/database-state verification claims
- Protocol Intelligence runtime/user-facing claims
- network calls from offline verification paths
- sample personal health data
- failing required validation commands
- vulnerable package findings that affect the offline verification kit
- unreviewed contract or snapshot drift
