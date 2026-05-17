[CmdletBinding()]
param(
    [string]$CandidateFile = "research/input/candidates/compound-candidates.json",
    [string]$SourceRegistryFile = "research/input/sources/source-registry.json",
    [string]$EvidenceDirectory = "research/input/evidence",
    [string]$EvidencePacketPath = "",
    [string]$ReviewDecisionDirectory = "research/review-decisions",
    [string]$ReviewDecisionPath = "",
    [string]$ResearchRequestDirectory = "research/research-requests",
    [string]$ResearchRequestPath = "",
    [string]$RelationshipPacketPath = "",
    [string]$RelationshipDirectory = "research/input/relationships",
    [string]$OutputDirectory = "research/output/latest",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$project = Join-Path $repoRoot "backend/src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj"
$output = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $repoRoot $OutputDirectory }

New-Item -ItemType Directory -Force -Path $output | Out-Null

$dotnetArgs = @("run")
if ($NoBuild) { $dotnetArgs += "--no-build" }
$dotnetArgs += @("--project", $project, "--")
$workerArgs = @(
    "--Worker:RunMode=Research",
    "--Worker:ResearchOutputDirectory=$output"
)

if (-not [string]::IsNullOrWhiteSpace($CandidateFile)) {
    $candidate = if ([System.IO.Path]::IsPathRooted($CandidateFile)) { $CandidateFile } else { Join-Path $repoRoot $CandidateFile }
    if (Test-Path $candidate) { $workerArgs += "--Worker:ResearchCandidateFilePath=$candidate" }
}

if (-not [string]::IsNullOrWhiteSpace($SourceRegistryFile)) {
    $sourceRegistry = if ([System.IO.Path]::IsPathRooted($SourceRegistryFile)) { $SourceRegistryFile } else { Join-Path $repoRoot $SourceRegistryFile }
    if (Test-Path $sourceRegistry) { $workerArgs += "--Worker:ResearchSourceRegistryFilePath=$sourceRegistry" }
}

if (-not [string]::IsNullOrWhiteSpace($EvidencePacketPath)) {
    $packet = if ([System.IO.Path]::IsPathRooted($EvidencePacketPath)) { $EvidencePacketPath } else { Join-Path $repoRoot $EvidencePacketPath }
    $workerArgs += "--Worker:ResearchEvidencePacketPath=$packet"
} else {
    $evidence = if ([System.IO.Path]::IsPathRooted($EvidenceDirectory)) { $EvidenceDirectory } else { Join-Path $repoRoot $EvidenceDirectory }
    $workerArgs += "--Worker:ResearchEvidencePacketDirectory=$evidence"
}

if (-not [string]::IsNullOrWhiteSpace($ReviewDecisionPath)) {
    $reviewDecision = if ([System.IO.Path]::IsPathRooted($ReviewDecisionPath)) { $ReviewDecisionPath } else { Join-Path $repoRoot $ReviewDecisionPath }
    if (Test-Path $reviewDecision) { $workerArgs += "--Worker:ResearchReviewDecisionPath=$reviewDecision" }
} elseif (-not [string]::IsNullOrWhiteSpace($ReviewDecisionDirectory)) {
    $reviewDecisions = if ([System.IO.Path]::IsPathRooted($ReviewDecisionDirectory)) { $ReviewDecisionDirectory } else { Join-Path $repoRoot $ReviewDecisionDirectory }
    if (Test-Path $reviewDecisions) { $workerArgs += "--Worker:ResearchReviewDecisionDirectory=$reviewDecisions" }
}

if (-not [string]::IsNullOrWhiteSpace($ResearchRequestPath)) {
    $researchRequest = if ([System.IO.Path]::IsPathRooted($ResearchRequestPath)) { $ResearchRequestPath } else { Join-Path $repoRoot $ResearchRequestPath }
    if (Test-Path $researchRequest) { $workerArgs += "--Worker:ResearchRequestPath=$researchRequest" }
} elseif (-not [string]::IsNullOrWhiteSpace($ResearchRequestDirectory)) {
    $researchRequests = if ([System.IO.Path]::IsPathRooted($ResearchRequestDirectory)) { $ResearchRequestDirectory } else { Join-Path $repoRoot $ResearchRequestDirectory }
    if (Test-Path $researchRequests) { $workerArgs += "--Worker:ResearchRequestDirectory=$researchRequests" }
}

if (-not [string]::IsNullOrWhiteSpace($RelationshipPacketPath)) {
    $relPacket = if ([System.IO.Path]::IsPathRooted($RelationshipPacketPath)) { $RelationshipPacketPath } else { Join-Path $repoRoot $RelationshipPacketPath }
    if (Test-Path $relPacket) {
        $relPacketResolved = (Resolve-Path -Path $relPacket -ErrorAction SilentlyContinue).Path
        if ($relPacketResolved) { $workerArgs += "--Worker:ResearchRelationshipPacketPath=$relPacketResolved" }
    }
} elseif (-not [string]::IsNullOrWhiteSpace($RelationshipDirectory)) {
    $relDir = if ([System.IO.Path]::IsPathRooted($RelationshipDirectory)) { $RelationshipDirectory } else { Join-Path $repoRoot $RelationshipDirectory }
    $relDirResolved = (Resolve-Path -Path $relDir -ErrorAction SilentlyContinue).Path
    if ($relDirResolved -and (Test-Path $relDirResolved -PathType Container)) {
        $workerArgs += "--Worker:ResearchRelationshipPacketDirectory=$relDirResolved"
    }
}

Write-Host "[BioStack Research] Output: $output"
& dotnet @dotnetArgs @workerArgs
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "[BioStack Research] Complete. Generated files:"
    Get-ChildItem $output -File | ForEach-Object { Write-Host " - $($_.FullName)" }
}

exit $exitCode