[CmdletBinding()]
param(
    # Off by default so the smoke run stays fully offline. The vulnerable-package
    # scan is the only step that reaches the network.
    [switch] $IncludeVulnerableScan
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Repo-relative anchors only. No absolute machine-specific paths are emitted.
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$cliProject = 'backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli'
$goldenBundle = 'backend/tests/BioStack.Application.Tests/Fixtures/ProtocolOperationsExportBundle/ProtocolOperationsExportBundle.golden.json'

# Scratch output lives in an OS temp dir and is always cleaned up, so the working
# tree is never touched.
$workDir = Join-Path ([System.IO.Path]::GetTempPath()) ("biostack-offline-kit-smoke-" + [System.Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $workDir -Force | Out-Null
$receiptPath = Join-Path $workDir 'protocol-operations-export-bundle.receipt.json'

function Assert-LastExit {
    param([Parameter(Mandatory = $true)][string] $Step)
    if ($LASTEXITCODE -ne 0) {
        throw "offline-kit smoke step failed (exit $LASTEXITCODE): $Step"
    }
}

Push-Location $repoRoot
try {
    Write-Host '==> [1/8] build offline verifier and tests'
    dotnet build backend/BioStack.sln
    Assert-LastExit 'build'

    Write-Host '==> [2/8] focused application tests'
    dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundle
    Assert-LastExit 'application tests'

    Write-Host '==> [3/8] CLI tests'
    dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj
    Assert-LastExit 'cli tests'

    Write-Host '==> [4/8] verify golden bundle'
    dotnet run --project $cliProject --no-build -- $goldenBundle
    Assert-LastExit 'verify golden bundle'

    Write-Host '==> [5/8] emit receipt json'
    dotnet run --project $cliProject --no-build -- $goldenBundle --receipt-json > $receiptPath
    Assert-LastExit 'emit receipt json'

    Write-Host '==> [6/8] verify receipt json'
    dotnet run --project $cliProject --no-build -- --verify-receipt-json $receiptPath
    Assert-LastExit 'verify receipt json'

    Write-Host '==> [7/8] emit bundle result json'
    dotnet run --project $cliProject --no-build -- $goldenBundle --result-json
    Assert-LastExit 'emit bundle result json'
    Write-Host ''

    Write-Host '==> [8/8] emit receipt result json'
    dotnet run --project $cliProject --no-build -- --result-json --verify-receipt-json $receiptPath
    Assert-LastExit 'emit receipt result json'
    Write-Host ''

    if ($IncludeVulnerableScan) {
        Write-Host '==> optional: vulnerable package scan (network)'
        dotnet list backend/BioStack.sln package --include-transitive --vulnerable
        Assert-LastExit 'vulnerable package scan'
    }

    Write-Host 'offline-kit smoke: OK'
}
finally {
    Pop-Location
    if (Test-Path $workDir) {
        Remove-Item -Recurse -Force $workDir
    }
}
