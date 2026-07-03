# BioStack Protocol Operations Export Bundle Verifier CLI

`BioStack.ProtocolOperationsExportBundleVerifierCli` is an offline verification tool for BioStack Protocol Operations export bundles and their deterministic verification receipts.

## Supported Modes

### Verify a bundle and print a human-readable summary

```bash
BioStack.ProtocolOperationsExportBundleVerifierCli <bundle.json>
```

This mode verifies a `ProtocolOperationsExportBundle` JSON file and prints a stable text summary to stdout.

### Verify a bundle and emit deterministic receipt JSON

```bash
BioStack.ProtocolOperationsExportBundleVerifierCli <bundle.json> --receipt-json
```

This mode verifies a `ProtocolOperationsExportBundle` JSON file and emits machine-readable deterministic receipt JSON to stdout.

### Verify an emitted receipt without the original bundle

```bash
BioStack.ProtocolOperationsExportBundleVerifierCli --verify-receipt-json <receipt.json>
```

This mode verifies a previously emitted receipt JSON file and prints a stable receipt-verification summary to stdout.

## Explicit Guarantees

The tool:

- does not generate PDFs
- does not write files unless stdout is redirected by caller
- does not access persistence/database
- does not call export-generation services
- does not replay original bundle verification during receipt verification
- does not expand Protocol Intelligence runtime behavior
- does not provide medical advice, dosing, diagnosis, treatment, prescription, or recommendations
- emits deterministic receipt JSON with no timestamps, hostnames, local paths, or environment-specific values

## Operator Workflow

Use the CLI to verify the full local trust path:

1. Verify a `ProtocolOperationsExportBundle` JSON file.
2. Emit deterministic receipt JSON.
3. Verify the emitted receipt JSON.
4. Confirm the process stays deterministic and boundary-clean.

## `dotnet run` Examples

Verify a bundle and print the human summary:

```bash
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- ./bundle.json
```

Verify a bundle and capture deterministic receipt JSON:

```bash
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- ./bundle.json --receipt-json > receipt.json
```

Verify a receipt file without the original bundle:

```bash
dotnet run --project backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli -- --verify-receipt-json ./receipt.json
```

## Boundary Notes

- Receipt verification is receipt-only validation. It does not require the original bundle file to remain present.
- Receipt output is deterministic by design and excludes machine-specific or environment-specific values.
- The CLI is limited to educational, observational verification of export artifacts.
