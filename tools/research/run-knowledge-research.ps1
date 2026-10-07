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
    [switch]$AllowMissingSourceRegistry,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$project = Join-Path $repoRoot "backend/src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj"
$output = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $repoRoot $OutputDirectory }

# Snapshot of what the caller actually passed. Used to tell an explicitly supplied
# path (a typo the caller must see) apart from an unchanged default.
$boundParameters = $PSBoundParameters
$inputSummary = New-Object System.Collections.Generic.List[object]

function Resolve-ResearchInput {
    <#
        Resolves one path parameter against the repo root and classifies it as
        present / missing-explicit / missing-default / not-requested.
        Nothing here silently drops an input: every parameter that was requested
        is recorded in $inputSummary and surfaced in the run output.
    #>
    param(
        [Parameter(Mandatory = $true)][string]$ParameterName,
        [AllowEmptyString()][string]$Value,
        [ValidateSet("Leaf", "Container")][string]$Kind = "Leaf"
    )

    $explicit = $boundParameters.ContainsKey($ParameterName)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        $entry = [pscustomobject]@{
            Parameter = $ParameterName
            Kind      = $Kind
            Explicit  = $explicit
            Requested = ""
            Resolved  = ""
            Status    = "not-requested"
        }
        $inputSummary.Add($entry) | Out-Null
        return $entry
    }

    $resolved = if ([System.IO.Path]::IsPathRooted($Value)) { $Value } else { Join-Path $repoRoot $Value }
    $exists = Test-Path -LiteralPath $resolved -PathType $Kind
    if ($exists) { $resolved = (Resolve-Path -LiteralPath $resolved).Path }
    $status = if ($exists) { "present" } elseif ($explicit) { "missing-explicit" } else { "missing-default" }

    $entry = [pscustomobject]@{
        Parameter = $ParameterName
        Kind      = $Kind
        Explicit  = $explicit
        Requested = $Value
        Resolved  = $resolved
        Status    = $status
    }
    $inputSummary.Add($entry) | Out-Null
    return $entry
}

function Stop-WithInputError {
    <#
        Writes a multi-line, unmangled failure block to stderr and exits non-zero.
        Used instead of `throw` so the actionable detail is not reflowed by the
        PowerShell error formatter.
    #>
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string[]]$Lines)

    [Console]::Error.WriteLine("")
    foreach ($line in $Lines) { [Console]::Error.WriteLine($line) }
    [Console]::Error.WriteLine("")
    exit 1
}

function Assert-NoMissingExplicitInput {
    <#
        An explicitly supplied path that does not exist is always fatal. Previously
        the runner dropped the argument and the run "succeeded" against nothing.
    #>
    param([Parameter(Mandatory = $true)][object]$InputEntry)

    if ($InputEntry.Status -ne "missing-explicit") { return }

    $kindWord = if ($InputEntry.Kind -eq "Container") { "directory" } else { "file" }
    Stop-WithInputError -Lines @(
        "ERROR [BioStack Research] FATAL: -$($InputEntry.Parameter) was passed explicitly but the $kindWord does not exist."
        "  Requested: $($InputEntry.Requested)"
        "  Resolved:  $($InputEntry.Resolved)"
        "Refusing to run: an explicitly requested input must never be silently omitted."
    )
}

function Write-MissingDefaultWarning {
    param([Parameter(Mandatory = $true)][object]$InputEntry)

    if ($InputEntry.Status -ne "missing-default") { return }
    $kindWord = if ($InputEntry.Kind -eq "Container") { "directory" } else { "file" }
    Write-Warning ("[BioStack Research] Default -{0} {1} does not exist and will NOT be loaded: {2}" -f $InputEntry.Parameter, $kindWord, $InputEntry.Resolved)
}

# ---------------------------------------------------------------------------
# Resolve and classify every path input before anything is built or executed.
# ---------------------------------------------------------------------------

$candidateInput          = Resolve-ResearchInput -ParameterName "CandidateFile"            -Value $CandidateFile            -Kind Leaf
$sourceRegistryInput     = Resolve-ResearchInput -ParameterName "SourceRegistryFile"       -Value $SourceRegistryFile       -Kind Leaf
$evidencePacketInput     = Resolve-ResearchInput -ParameterName "EvidencePacketPath"       -Value $EvidencePacketPath       -Kind Leaf
$evidenceDirectoryInput  = Resolve-ResearchInput -ParameterName "EvidenceDirectory"        -Value $EvidenceDirectory        -Kind Container
$reviewDecisionInput     = Resolve-ResearchInput -ParameterName "ReviewDecisionPath"       -Value $ReviewDecisionPath       -Kind Leaf
$reviewDecisionDirInput  = Resolve-ResearchInput -ParameterName "ReviewDecisionDirectory"  -Value $ReviewDecisionDirectory  -Kind Container
$researchRequestInput    = Resolve-ResearchInput -ParameterName "ResearchRequestPath"      -Value $ResearchRequestPath      -Kind Leaf
$researchRequestDirInput = Resolve-ResearchInput -ParameterName "ResearchRequestDirectory" -Value $ResearchRequestDirectory -Kind Container
$relationshipInput       = Resolve-ResearchInput -ParameterName "RelationshipPacketPath"   -Value $RelationshipPacketPath   -Kind Leaf
$relationshipDirInput    = Resolve-ResearchInput -ParameterName "RelationshipDirectory"    -Value $RelationshipDirectory    -Kind Container

foreach ($entry in $inputSummary) { Assert-NoMissingExplicitInput -InputEntry $entry }

# ---------------------------------------------------------------------------
# Source registry: the promotion-authorization input. A run that loaded no
# registry must never be mistakable for an authorized one.
# ---------------------------------------------------------------------------

$sourceRegistryLoaded = ($sourceRegistryInput.Status -eq "present")

if (-not $sourceRegistryLoaded) {
    $sourcesDirectory = Join-Path $repoRoot "research/input/sources"
    $availableRegistries = @()
    if (Test-Path -LiteralPath $sourcesDirectory -PathType Container) {
        $availableRegistries = @(
            Get-ChildItem -LiteralPath $sourcesDirectory -File -Filter "*.json" |
                ForEach-Object { "research/input/sources/$($_.Name)" }
        )
    }
    $availableText = if ($availableRegistries.Count -gt 0) { $availableRegistries -join ", " } else { "(none found)" }

    $requestedText = if ([string]::IsNullOrWhiteSpace($sourceRegistryInput.Requested)) { "(empty)" } else { $sourceRegistryInput.Requested }
    $resolvedText = if ([string]::IsNullOrWhiteSpace($sourceRegistryInput.Resolved)) { "(none)" } else { $sourceRegistryInput.Resolved }

    if (-not $AllowMissingSourceRegistry) {
        Stop-WithInputError -Lines @(
            "ERROR [BioStack Research] FATAL: no source registry is available; refusing to run."
            "  Requested (default): $requestedText"
            "  Resolved:            $resolvedText"
            "  Registries present:  $availableText"
            ""
            "A successful compile with no registry loaded must not be mistaken for"
            "authorized promotion readiness."
            ""
            "Fix by passing an existing registry, e.g.:"
            ("  -SourceRegistryFile " + $(if ($availableRegistries.Count -gt 0) { $availableRegistries[0] } else { "<path to a source registry>" }))
            "Or, to run deliberately unregistered (the run output is marked as such):"
            "  -AllowMissingSourceRegistry"
        )
    }

    Write-Warning "[BioStack Research] ============================================================"
    Write-Warning "[BioStack Research] RUNNING WITH NO SOURCE REGISTRY (-AllowMissingSourceRegistry)."
    Write-Warning "[BioStack Research] Source-registry authorization is NOT evaluated in this run."
    Write-Warning "[BioStack Research] Results are NOT evidence of promotion readiness."
    Write-Warning ("[BioStack Research] Registries present: {0}" -f $availableText)
    Write-Warning "[BioStack Research] ============================================================"
}

foreach ($entry in $inputSummary) {
    if ($entry.Parameter -eq "SourceRegistryFile") { continue }
    Write-MissingDefaultWarning -InputEntry $entry
}

# ---------------------------------------------------------------------------
# Build the worker argument list from the classified inputs.
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $output | Out-Null

$dotnetArgs = @("run")
if ($NoBuild) { $dotnetArgs += "--no-build" }
$dotnetArgs += @("--project", $project, "--")
$workerArgs = @(
    "--Worker:RunMode=Research",
    "--Worker:ResearchOutputDirectory=$output"
)

if ($candidateInput.Status -eq "present") {
    $workerArgs += "--Worker:ResearchCandidateFilePath=$($candidateInput.Resolved)"
}

if ($sourceRegistryLoaded) {
    $workerArgs += "--Worker:ResearchSourceRegistryFilePath=$($sourceRegistryInput.Resolved)"
}

if ($evidencePacketInput.Status -eq "present") {
    $workerArgs += "--Worker:ResearchEvidencePacketPath=$($evidencePacketInput.Resolved)"
} elseif (-not [string]::IsNullOrWhiteSpace($evidenceDirectoryInput.Resolved)) {
    $workerArgs += "--Worker:ResearchEvidencePacketDirectory=$($evidenceDirectoryInput.Resolved)"
}

if ($reviewDecisionInput.Status -eq "present") {
    $workerArgs += "--Worker:ResearchReviewDecisionPath=$($reviewDecisionInput.Resolved)"
} elseif ($reviewDecisionDirInput.Status -eq "present") {
    $workerArgs += "--Worker:ResearchReviewDecisionDirectory=$($reviewDecisionDirInput.Resolved)"
}

if ($researchRequestInput.Status -eq "present") {
    $workerArgs += "--Worker:ResearchRequestPath=$($researchRequestInput.Resolved)"
} elseif ($researchRequestDirInput.Status -eq "present") {
    $workerArgs += "--Worker:ResearchRequestDirectory=$($researchRequestDirInput.Resolved)"
}

if ($relationshipInput.Status -eq "present") {
    $workerArgs += "--Worker:ResearchRelationshipPacketPath=$($relationshipInput.Resolved)"
} elseif ($relationshipDirInput.Status -eq "present") {
    $workerArgs += "--Worker:ResearchRelationshipPacketDirectory=$($relationshipDirInput.Resolved)"
}

# ---------------------------------------------------------------------------
# Record the resolved-input state in the run output so a completed run can be
# audited for whether a registry was actually loaded.
# ---------------------------------------------------------------------------

$sourceRegistryPathForSummary = if ($sourceRegistryLoaded) { $sourceRegistryInput.Resolved } else { $null }
# NOTE: use .ToArray(); @($genericList) inside a hashtable literal throws
# "Argument types do not match" in PowerShell.
$inputSummaryArray = $inputSummary.ToArray()
$runnerInputSummary = [ordered]@{
    generatedAtUtc             = (Get-Date).ToUniversalTime().ToString("o")
    runner                     = "tools/research/run-knowledge-research.ps1"
    outputDirectory            = "$output"
    sourceRegistryLoaded       = $sourceRegistryLoaded
    allowMissingSourceRegistry = [bool]$AllowMissingSourceRegistry
    sourceRegistryPath         = $sourceRegistryPathForSummary
    inputs                     = $inputSummaryArray
}
$runnerInputSummary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $output "runner-input-summary.json") -Encoding utf8

$noRegistryMarker = Join-Path $output "SOURCE-REGISTRY-MISSING.WARNING.txt"
if ($sourceRegistryLoaded) {
    if (Test-Path -LiteralPath $noRegistryMarker) { Remove-Item -LiteralPath $noRegistryMarker -Force }
} else {
    $requestedRegistryText = if ([string]::IsNullOrWhiteSpace($sourceRegistryInput.Requested)) { "(empty)" } else { $sourceRegistryInput.Requested }
    @(
        "NO SOURCE REGISTRY WAS LOADED FOR THIS RUN."
        ""
        "The runner was invoked with -AllowMissingSourceRegistry, so source-registry"
        "authorization was not evaluated. Any 'promotable' or 'candidate' result in"
        "this directory is UNREGISTERED and is NOT evidence of promotion readiness."
        ""
        ("Requested registry path: " + $requestedRegistryText)
        ("Generated at (UTC):      " + (Get-Date).ToUniversalTime().ToString("o"))
    ) | Set-Content -LiteralPath $noRegistryMarker -Encoding utf8
}

Write-Host "[BioStack Research] Output: $output"
if ($sourceRegistryLoaded) {
    Write-Host "[BioStack Research] Source registry: $($sourceRegistryInput.Resolved)"
} else {
    Write-Host "[BioStack Research] Source registry: NONE (unregistered run)"
}
& dotnet @dotnetArgs @workerArgs
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    if (-not $sourceRegistryLoaded) {
        Write-Warning "[BioStack Research] Run completed WITHOUT a source registry. See SOURCE-REGISTRY-MISSING.WARNING.txt in the output directory."
    }
    Write-Host "[BioStack Research] Complete. Generated files:"
    Get-ChildItem $output -File | ForEach-Object { Write-Host " - $($_.FullName)" }
}

exit $exitCode
