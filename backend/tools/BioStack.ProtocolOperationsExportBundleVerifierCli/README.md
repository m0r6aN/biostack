# BioStack Protocol Operations Export Bundle Verifier CLI

`BioStack.ProtocolOperationsExportBundleVerifierCli` is an offline verification
tool for BioStack Protocol Operations export bundles and deterministic
verification receipts.

This tool is intentionally narrow: it reads local JSON files, recomputes local
hashes, and prints either a stable human-readable summary or deterministic
receipt JSON to stdout.

## What The Bundle Is

A Protocol Operations export bundle is a local JSON artifact shaped as a
`ProtocolOperationsExportBundle`. It contains:

- bundle metadata, including the bundle schema version
- bundle integrity metadata, including the supplied bundle content hash
- an embedded Protocol Operations report export JSON payload
- report-export integrity metadata, including the supplied report-export content
  hash
- artifact descriptors that identify the embedded report-export JSON artifact

The verifier treats the bundle as an already-created local artifact. It does not
create the bundle, fetch the bundle, call export services, or inspect database
state.

## What The Verification Receipt Is

A verification receipt is deterministic JSON emitted by this CLI after bundle
verification. It records the verifier schema, bundle schema binding, recomputed
hashes, supplied hashes, ordered checks, errors, explicit boundary flags, and two
receipt-level hashes.

Receipt JSON is designed for offline handoff. It excludes timestamps, hostnames,
local paths, usernames, process details, environment values, and stack traces.

## Supported Modes

### Verify Bundle And Print Human Summary

```bash
BioStack.ProtocolOperationsExportBundleVerifierCli <bundle.json>
```

This mode verifies a local `ProtocolOperationsExportBundle` JSON file and prints
a stable text summary to stdout.

### Verify Bundle And Emit Deterministic Receipt JSON

```bash
BioStack.ProtocolOperationsExportBundleVerifierCli <bundle.json> --receipt-json
```

This mode verifies a local `ProtocolOperationsExportBundle` JSON file and emits
machine-readable deterministic receipt JSON to stdout.

### Verify An Emitted Receipt Without The Original Bundle

```bash
BioStack.ProtocolOperationsExportBundleVerifierCli --verify-receipt-json <receipt.json>
```

This mode verifies a previously emitted receipt JSON file and prints a stable
receipt-verification summary to stdout. Receipt verification validates only the
receipt file; it does not replay original bundle verification and does not
require the original bundle file to remain present.

## Copy-Paste Command Examples

Run these examples from the repository root. Replace the placeholder paths with
local files on the machine performing offline verification.

Verify a bundle and print a human summary:

```bash
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- ./local/offline/protocol-operations-export-bundle.json
```

Verify a bundle and capture deterministic receipt JSON:

```bash
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- ./local/offline/protocol-operations-export-bundle.json --receipt-json > ./local/offline/protocol-operations-export-bundle.receipt.json
```

Verify a receipt file without the original bundle:

```bash
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- --verify-receipt-json ./local/offline/protocol-operations-export-bundle.receipt.json
```

Minimal placeholder examples:

```bash
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- ./bundle.json
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- ./bundle.json --receipt-json > receipt.json
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- --verify-receipt-json ./receipt.json
```

The shorter invocation form is also supported when the CLI executable is already
available on `PATH`:

```bash
BioStack.ProtocolOperationsExportBundleVerifierCli <bundle.json>
BioStack.ProtocolOperationsExportBundleVerifierCli <bundle.json> --receipt-json
BioStack.ProtocolOperationsExportBundleVerifierCli --verify-receipt-json <receipt.json>
```

## What Hashes Are Recomputed Locally

Bundle verification recomputes these hashes from the local bundle content:

- `actual-bundle-sha256`: recomputed bundle content hash
- `actual-report-export-sha256`: recomputed embedded report-export content hash

It compares them with these supplied values from the bundle:

- `expected-bundle-sha256`: supplied bundle content hash
- `expected-report-export-sha256`: supplied report-export content hash

Receipt generation also computes receipt-local hashes:

- `verificationResultContentHash`: hash over the verifier result material,
  including verifier schema, supplied hashes, recomputed hashes, checks, and
  errors
- `receiptContentHash`: hash over the deterministic receipt payload excluding
  the receipt content hash itself

Receipt verification recomputes the receipt-local hashes and compares them with
the receipt fields. It does not recompute the original bundle hashes because the
receipt verifier intentionally reads only the receipt JSON file.

## What Success Means

For bundle verification, success means the local bundle passed the verifier's
offline structural checks and the locally recomputed bundle/report-export hashes
matched the supplied hashes.

For receipt verification, success means the receipt has the expected schema and
boundary fields, contains allowed status values, has the required hash bindings
for its status, avoids forbidden host/path/PDF/persistence/runtime/medical
language, and its receipt-local hashes recompute to the values in the receipt.

## What Failure Means

Failure means the CLI found a local verification problem and returned a non-zero
exit code. The stable summary or receipt `errors` field names the reason.
Common reasons include invalid JSON, a missing input file, schema mismatch,
required field drift, supplied hash mismatch, receipt hash mismatch, forbidden
boundary language, or conflicting CLI arguments.

Failure is not a clinical judgment. It only describes whether the local artifact
or receipt satisfied this verifier's deterministic offline contract.

## What This Chain Does Not Prove

The verification chain does not prove:

- medical validity
- clinical endorsement
- dosing guidance
- PDF authenticity
- persistence/database state
- Protocol Intelligence runtime behavior
- that any API, frontend, background job, telemetry path, or network service ran
- that the bundle came from a particular machine, user, database row, or hosted
  environment

## Troubleshooting

- Invalid JSON: confirm the input is strict JSON with no comments or trailing
  commas, then rerun the same command.
- Missing file: confirm the placeholder path was replaced with a local file path
  that exists on the verifier machine.
- Hash mismatch: treat the bundle as changed or inconsistent with its supplied
  integrity fields; reacquire the intended local artifact before relying on the
  receipt.
- Schema drift: check whether the bundle, verifier, or receipt schema version is
  newer than this CLI supports, then update the offline kit rather than editing
  the artifact by hand.
- Receipt verification failure: regenerate the receipt from the intended bundle
  if the bundle is available; if only the receipt is available, treat the receipt
  as invalid when its schema, boundary fields, required hashes, or
  `receiptContentHash` do not validate.

## Explicit Guarantees

The tool:

- does not generate PDFs
- does not make PDF authenticity claims
- does not write files unless stdout is redirected by caller
- does not access persistence/database
- does not verify persistence or database state
- does not call export-generation services
- does not replay original bundle verification during receipt verification
- does not expand Protocol Intelligence runtime behavior
- does not provide medical advice, dosing, diagnosis, treatment, prescription, or recommendations
- emits deterministic receipt JSON with no timestamps, hostnames, local paths, or environment-specific values

## Operator Workflow

Use the CLI to verify the full local trust path:

1. Verify the `ProtocolOperationsExportBundle` JSON file.
2. Emit deterministic receipt JSON.
3. Verify the emitted receipt JSON.
4. Confirm the process stays deterministic and boundary-clean.

## Offline Kit Guard

Run the focused offline verification guard locally from the repository root:

```bash
pwsh ./backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/verify-offline-kit.ps1
```

The minimum verifier-kit validation set is:

- `dotnet build backend/BioStack.sln`
- `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundle`
- `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundleGoldenFixture`
- `dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolIntelligenceOfflineBoundaryTests`
- `dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj`
- `dotnet list backend/BioStack.sln package --include-transitive --vulnerable`
- `git diff --check`

## Boundary Notes

- Receipt verification is receipt-only validation. It does not require the original bundle file to remain present.
- Receipt output is deterministic by design and excludes machine-specific or
  environment-specific values.
- The CLI is limited to educational, observational verification of local export
  artifacts.
