# Protocol Operations Offline Auditor Packet Index

This index is the docs-only entry point for a third-party reviewer inspecting the
Protocol Operations offline verification kit. It does not create a new product
surface, generate artifacts, add runtime behavior, or replace the verifier CLI,
runbook, release checklist, smoke scripts, result-code catalog, or capstone
guard.

## Review Order

Inspect these artifacts in order:

1. [Verifier CLI reference](backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/README.md)
   - Establishes the supported local-file verification modes, receipt-only mode,
     deterministic stdout behavior, hash terminology, and explicit guarantees.
2. [Clean-checkout offline runbook](backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/OFFLINE_VERIFICATION_RUNBOOK.md)
   - Shows how a reviewer reproduces the offline verification chain from a clean
     checkout using local commands and supplied local JSON files.
3. [Offline verification release checklist](backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/OFFLINE_VERIFICATION_RELEASE_CHECKLIST.md)
   - Lists required build, test, vulnerability-scan, boundary-language, and diff
     hygiene gates before sharing verifier-kit changes.
4. [PowerShell smoke script](backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/smoke-offline-kit.ps1)
   - Exercises the local build, focused application tests, CLI tests, bundle
     verification, receipt generation, receipt verification, and result JSON
     paths on Windows.
5. [Bash smoke script](backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/smoke-offline-kit.sh)
   - Exercises the same local smoke path on Unix-like shells.
6. [Stable result-code catalog](backend/tests/Fixtures/ProtocolOperationsExportBundle/ProtocolOperationsOfflineVerificationResultCodeCatalog.golden.json)
   - Freezes machine-readable result and error code identifiers so reviewers can
     key automation off stable codes instead of human text.
7. [Offline verification capstone guard](backend/tests/BioStack.Application.Tests/ProtocolOperationsOfflineVerificationCapstoneGuardTests.cs)
   - Proves integrated docs and contract posture do not drift after verifier-kit
     pieces land together.

## What this proves

- The auditor packet has a deterministic inspection order.
- The verifier kit remains air-gapped for its verification path.
- The CLI verifies supplied local bundle and receipt artifacts without creating
  new export data.
- The receipt verifier can inspect a receipt without requiring the original
  bundle file.
- The clean-checkout runbook and smoke scripts point reviewers at reproducible
  local commands.
- The stable result-code catalog gives automation a durable code surface.
- The capstone guard keeps the integrated offline verification posture aligned.

## What this does not prove

- It does not provide medical advice, clinical endorsement, diagnosis, dosing
  guidance, treatment guidance, prescription language, or recommendations.
- It does not prove medical validity or protocol effectiveness.
- It does not generate PDFs or make PDF authenticity claims.
- It does not add persistence, inspect database state, or verify stored runtime
  records.
- It does not add frontend, website, marketing, API endpoint, or product UI
  surface.
- It does not expand Protocol Intelligence runtime behavior.
- It does not authorize a bundle or receipt beyond deterministic local
  inspection of supplied artifacts.

## Boundary

This index is non-authoritative documentation. The authoritative checks remain
the verifier CLI, checked-in snapshots, focused tests, smoke scripts, release
checklist, and capstone guard. If this index disagrees with those artifacts,
treat the index as stale and update it to match the deterministic verifier-kit
surface.
