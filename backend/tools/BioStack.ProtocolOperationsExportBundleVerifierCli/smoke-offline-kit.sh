#!/usr/bin/env bash
# Offline verification kit smoke run. Exercises the full local chain against the
# repo-local golden bundle. Fails closed on any step failure.
#
# Usage:
#   ./smoke-offline-kit.sh                 # fully offline smoke run
#   ./smoke-offline-kit.sh --vulnerable-scan   # also run the network vuln scan
set -euo pipefail

# Repo-relative anchors only. No absolute machine-specific paths are emitted.
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../../.." && pwd)"
cli_project='backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli'
golden_bundle='backend/tests/BioStack.Application.Tests/Fixtures/ProtocolOperationsExportBundle/ProtocolOperationsExportBundle.golden.json'

include_vuln_scan=0
if [ "${1:-}" = "--vulnerable-scan" ]; then
    include_vuln_scan=1
fi

# Scratch output lives in an OS temp dir and is always cleaned up on exit, so the
# working tree is never touched.
work_dir="$(mktemp -d "${TMPDIR:-/tmp}/biostack-offline-kit-smoke-XXXXXX")"
receipt_path="${work_dir}/protocol-operations-export-bundle.receipt.json"
cleanup() { rm -rf "${work_dir}"; }
trap cleanup EXIT

cd "${repo_root}"

echo '==> [1/8] build offline verifier and tests'
dotnet build backend/BioStack.sln

echo '==> [2/8] focused application tests'
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundle

echo '==> [3/8] CLI tests'
dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj

echo '==> [4/8] verify golden bundle'
dotnet run --project "${cli_project}" --no-build -- "${golden_bundle}"

echo '==> [5/8] emit receipt json'
dotnet run --project "${cli_project}" --no-build -- "${golden_bundle}" --receipt-json > "${receipt_path}"

echo '==> [6/8] verify receipt json'
dotnet run --project "${cli_project}" --no-build -- --verify-receipt-json "${receipt_path}"

echo '==> [7/8] emit bundle result json'
dotnet run --project "${cli_project}" --no-build -- "${golden_bundle}" --result-json
echo

echo '==> [8/8] emit receipt result json'
dotnet run --project "${cli_project}" --no-build -- --result-json --verify-receipt-json "${receipt_path}"
echo

if [ "${include_vuln_scan}" -eq 1 ]; then
    echo '==> optional: vulnerable package scan (network)'
    dotnet list backend/BioStack.sln package --include-transitive --vulnerable
fi

echo 'offline-kit smoke: OK'
