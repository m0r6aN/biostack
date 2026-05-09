[CmdletBinding()]
param(
    [string]$PromotionImportPreviewPath = "research/output/latest/promotion-import-preview.json",
    [string]$PromotionImportAggregatePath = "research/output/latest/promotion-export/substances.promotable.json",
    [string]$OutputDirectory = "research/output/latest/import-dry-run",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$project = Join-Path $repoRoot "backend/src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj"
$preview = if ([System.IO.Path]::IsPathRooted($PromotionImportPreviewPath)) { $PromotionImportPreviewPath } else { Join-Path $repoRoot $PromotionImportPreviewPath }
$aggregate = if ([System.IO.Path]::IsPathRooted($PromotionImportAggregatePath)) { $PromotionImportAggregatePath } else { Join-Path $repoRoot $PromotionImportAggregatePath }
$output = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $repoRoot $OutputDirectory }

New-Item -ItemType Directory -Force -Path $output | Out-Null

$dotnetArgs = @("run")
if ($NoBuild) { $dotnetArgs += "--no-build" }
$dotnetArgs += @("--project", $project, "--")
$workerArgs = @(
    "--Worker:RunMode=PromotionImportDryRun",
    "--Worker:PromotionImportPreviewPath=$preview",
    "--Worker:PromotionImportAggregatePath=$aggregate",
    "--Worker:PromotionImportDryRunOutputDirectory=$output"
)

Write-Host "[BioStack PromotionImportDryRun] Preview: $preview"
Write-Host "[BioStack PromotionImportDryRun] Aggregate: $aggregate"
Write-Host "[BioStack PromotionImportDryRun] Output: $output"
& dotnet @dotnetArgs @workerArgs
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "[BioStack PromotionImportDryRun] Complete. Generated files:"
    Get-ChildItem $output -File | ForEach-Object { Write-Host " - $($_.FullName)" }
}

exit $exitCode