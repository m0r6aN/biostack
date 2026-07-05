# Offline Verification Runbook (Clean-Checkout Reproduction)

This runbook is an **internal reviewer runbook**. It explains how someone with a
clean checkout reproduces the Protocol Operations offline verification chain on
their own machine, using only local files and deterministic commands.

It is a companion to, not a replacement for, the reference documentation:

- Tool reference: `backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/README.md`
- Release gate checklist: `backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/OFFLINE_VERIFICATION_RELEASE_CHECKLIST.md`
- Result code catalog (stable identifiers): `backend/tests/Fixtures/ProtocolOperationsExportBundle/ProtocolOperationsOfflineVerificationResultCodeCatalog.golden.json`

The README explains *what* the chain does. This runbook explains *how a reviewer
reproduces it from a clean checkout*.

## Posture

BioStack is for educational and observational use only. This verifier applies
mathematical logic only: it reads local JSON files, recomputes local hashes, and
prints deterministic output. It is an educational reference tool. Nothing in this
runbook is medical advice, dosing guidance, diagnosis, treatment, prescription,
clinical approval, PDF authenticity, persistence/database-state verification,
runtime execution verification, or provider approval.

## Prerequisites

- .NET SDK capable of building the solution (the repository targets .NET 10).
- Git.
- A shell. PowerShell (`pwsh`) and Bash are both fine; commands below are shown
  in a portable form.
- No network is required to run the verification chain itself. The only step
  that reaches the network is the optional vulnerable-package scan.

## Clean-Clone Assumptions

- You are working from a fresh clone of the repository with no local build
  output and no uncommitted edits.
- All commands are run from the repository root.
- The bundle and receipt files you verify are **local files you supply**. This
  runbook uses placeholder paths only. Replace every placeholder such as
  `./local/protocol-operations-export-bundle.json` with a real local path on the
  reviewing machine. Do not commit those local artifacts.

## Build

```bash
dotnet build backend/BioStack.sln
```

## Focused Tests

Run the offline verification test surfaces:

```bash
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundle
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundleGoldenFixture
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolIntelligenceOfflineBoundaryTests
dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj
```

## Run The CLI

Verify a bundle and print the stable human-readable summary:

```bash
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- ./local/protocol-operations-export-bundle.json
```

Print CLI usage:

```bash
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- --help
```

## Emit Bundle Result JSON

Machine-readable bundle verification result (does not change the default
human-readable output; only `--result-json` selects JSON):

```bash
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- ./local/protocol-operations-export-bundle.json --result-json
```

## Emit A Receipt

Emit a deterministic verification receipt for a bundle:

```bash
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- ./local/protocol-operations-export-bundle.json --receipt-json > ./local/protocol-operations-export-bundle.receipt.json
```

## Verify A Receipt

Verify a previously emitted receipt without the original bundle:

```bash
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- --verify-receipt-json ./local/protocol-operations-export-bundle.receipt.json
```

## Emit Receipt Result JSON

Machine-readable receipt verification result:

```bash
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- --result-json --verify-receipt-json ./local/protocol-operations-export-bundle.receipt.json
```

## Vulnerable Package Scan

This is the only step that uses the network. It is part of the standard
validation set:

```bash
dotnet list backend/BioStack.sln package --include-transitive --vulnerable
```

## Diff Hygiene

Confirm there are no whitespace errors or leftover conflict markers before
sharing a branch:

```bash
git diff --check
```

## Expected Failure Behavior

The chain is designed to **fail closed** and deterministically:

- A missing input file, an input path that is a directory, invalid JSON, schema
  drift, required-field drift, a supplied-hash mismatch, a receipt hash mismatch,
  forbidden boundary language, or conflicting CLI arguments each produce a
  non-zero exit code and a stable dash-cased error token in the summary or in the
  result/receipt JSON `errors` field.
- Output never echoes absolute local paths, usernames, machine names,
  environment values, stack traces, or raw exception dumps. A failure names the
  deterministic reason only.
- Re-running the same command against the same local input produces byte-stable
  machine-readable output.

A failure is not a clinical judgment. It only reports whether the local artifact
satisfied this verifier's deterministic offline contract.

## Interpreting Result Codes

The offline verification chain emits stable dash-cased tokens (for example
`bundle-sha256-mismatch`) in its `errors` output. Those tokens are mapped to
stable upper-snake result codes in the result code catalog:

`backend/tests/Fixtures/ProtocolOperationsExportBundle/ProtocolOperationsOfflineVerificationResultCodeCatalog.golden.json`

Each catalog entry records the stable `code`, its `severity`, the
`artifactSurface` it applies to (`bundle`, `receipt`, `cli`, or `docs-manifest`),
a short deterministic `meaning`, whether the code is expected on the
`valid-path` or `failure-path`, an optional `boundaryPostureCategory`, and the
`mappedTokens` it covers. Auditors and automation should key off the stable
`code` families rather than the human-readable text.

The code families are:

- `POBUNDLE_*` — bundle verification outcomes (schema, hashes, artifact
  descriptor, boundary language drift).
- `PORECEIPT_*` — receipt verification outcomes (schema binding, receipt id,
  receipt hashes, captured-result status/hash).
- `POCLI_*` — CLI-level outcomes (argument validity, missing input file, invalid
  input JSON, result-JSON contract drift).
- `POBOUNDARY_*` — boundary-posture outcomes (medical-language, PDF-claim,
  persistence-claim, runtime-claim), each tagged with a
  `boundaryPostureCategory`.

The catalog is a stable identifier layer validated by tests. It does not change
the CLI's human-readable output and does not add codes to the runtime result
JSON.

## Known Non-Goals

This kit is intentionally narrow. It is:

- **not** a medical validation tool.
- **not** a PDF generator or PDF authenticator.
- **not** a database-state verifier.
- **not** a runtime execution verifier.
- **not** a clinical review workflow.
- **not** a provider approval workflow.

The verifier inspects a supplied local artifact only. It does not create,
fetch, or persist artifacts, does not call export-generation services, does not
access persistence or database state, does not expand Protocol Intelligence
runtime behavior, and does not run any API, frontend, background job, telemetry
path, or network service as part of verification.
