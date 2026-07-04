[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path

function Invoke-OfflineKitCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Command
    )

    Write-Host "==> $Command"
    & {
        Invoke-Expression $Command
    }
}

Push-Location $repoRoot
try {
    Invoke-OfflineKitCommand 'dotnet build backend/BioStack.sln'
    Invoke-OfflineKitCommand 'dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundle'
    Invoke-OfflineKitCommand 'dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundleGoldenFixture'
    Invoke-OfflineKitCommand 'dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolIntelligenceOfflineBoundaryTests'
    Invoke-OfflineKitCommand 'dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj'
    Invoke-OfflineKitCommand 'dotnet list backend/BioStack.sln package --include-transitive --vulnerable'
    Invoke-OfflineKitCommand 'git diff --check'
}
finally {
    Pop-Location
}
