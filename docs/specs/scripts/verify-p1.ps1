[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$BaseCommit,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$DispatchCommit,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$BuilderId,

    [Parameter(Mandatory = $true)]
    [string[]]$ReviewerIds,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$EvidenceDirectory
)

$ErrorActionPreference = 'Stop'

$GatePath = 'docs/INITIATIVES/biostack-governed-delivery/dispatch/P1-GATE2.md'
$SpecPath = 'docs/INITIATIVES/biostack-governed-delivery/parcels/P1.md'
$ApprovedSpecSha256 = 'A55235E81FF251B7A67620917522B9173F697D9776336C98D03B441F366C22BA'
$AgentsBaselineSha256 = 'D321EF8092AEC6EA449A0C78362BB5C6057DD45C3CE541624A8423EA52EDA9FD'
$AgentsFinalSha256 = 'AF175D88CD872D8ADD203294614BED3B164C16D2F14197BB03D239321BEC1276'
$RequiredProductionStatus = '**Status:** active, release blocked. **Recommendation:** **NO-GO / HOLD**.'

[string[]]$AllowedBuilderSurfaces = @(
    'AGENTS.md',
    'docs/INITIATIVES/biostack-governed-delivery/README.md',
    'docs/specs/CORE-CONTEXT.md',
    'docs/specs/INDEX.md',
    'docs/specs/README.md',
    'docs/specs/active/README.md',
    'docs/specs/done/README.md',
    'docs/specs/scripts/verify-p1.ps1'
)

[string[]]$FrozenSurfaces = @(
    'docs/INITIATIVES/biostack-governed-delivery/CHARTER.md',
    'docs/INITIATIVES/biostack-governed-delivery/PLAN-REVIEW.md',
    'docs/INITIATIVES/biostack-governed-delivery/dispatch/P1-GATE2.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P1.md'
)

[string[]]$CoreContextTargets = @(
    'docs/INITIATIVES/biostack-governed-delivery/CHARTER.md',
    'docs/architecture/adr-biostack-source-first-knowledge-engine.md',
    'docs/canon/biostack-protocol-intelligence-canon.md',
    'docs/biostack/evidence-grading-methodology-v1.md',
    'docs/knowledge-engine/protocol-intelligence-safety-guardrails.md',
    'contracts/product-contract.v1.json',
    'docs/INITIATIVES/biostack-production-readiness/README.md'
)

$CheckResults = New-Object 'System.Collections.Generic.List[object]'

function Assert-True {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$Condition,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-GitResult {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $output = @(& git @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
    return [pscustomobject]@{
        ExitCode = $exitCode
        Output = [string[]]@($output | ForEach-Object { [string]$_ })
    }
}

function Invoke-Git {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $result = Get-GitResult -Arguments $Arguments
    if ($result.ExitCode -ne 0) {
        $detail = ($result.Output -join "`n")
        throw "git $($Arguments -join ' ') failed with exit $($result.ExitCode): $detail"
    }
    return [string[]]$result.Output
}

function Sort-Ordinal {
    param([string[]]$Values)

    [string[]]$copy = @($Values)
    [Array]::Sort($copy, [StringComparer]::Ordinal)
    return $copy
}

function Assert-SequenceEqual {
    param(
        [string[]]$Actual,
        [string[]]$Expected,
        [string]$Label
    )

    Assert-True ($Actual.Count -eq $Expected.Count) "$Label count mismatch. Expected $($Expected.Count), got $($Actual.Count)."
    for ($index = 0; $index -lt $Expected.Count; $index++) {
        Assert-True ([StringComparer]::Ordinal.Equals($Actual[$index], $Expected[$index])) "$Label mismatch at index $index. Expected '$($Expected[$index])', got '$($Actual[$index])'."
    }
}

function Get-NormalizedText {
    param([Parameter(Mandatory = $true)][string]$Path)

    return [IO.File]::ReadAllText($Path).Replace("`r`n", "`n").Replace("`r", "`n")
}

function Get-Sha256Hex {
    param([Parameter(Mandatory = $true)][string]$Text)

    $encoding = New-Object Text.UTF8Encoding($false)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash($encoding.GetBytes($Text))
        return (($hash | ForEach-Object { $_.ToString('X2') }) -join '')
    }
    finally {
        $sha.Dispose()
    }
}

function Write-Utf8Lf {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [AllowEmptyString()][string]$Content
    )

    $normalized = $Content.Replace("`r`n", "`n").Replace("`r", "`n").TrimEnd("`n") + "`n"
    $encoding = New-Object Text.UTF8Encoding($false)
    [IO.File]::WriteAllText($Path, $normalized, $encoding)
}

function Add-PassedCheck {
    param([int]$Number, [string]$Name)

    $CheckResults.Add([pscustomobject]@{
        number = $Number
        name = $Name
        pass = $true
    }) | Out-Null
}

function Assert-OnlyAuthorizedEvidenceStatus {
    param([string[]]$StatusLines)

    foreach ($line in $StatusLines) {
        Assert-True $line.StartsWith('?? artifacts/p1-verification/', [StringComparison]::Ordinal) "Unauthorized or tracked worktree status entry: $line"
    }
}

if ($ReviewerIds.Count -eq 1 -and $ReviewerIds[0].Contains(',')) {
    $ReviewerIds = [string[]]@($ReviewerIds[0].Split(','))
}

Assert-True ($ReviewerIds.Count -eq 2) 'ReviewerIds must contain exactly two identities.'
Assert-True (-not [string]::IsNullOrWhiteSpace($ReviewerIds[0])) 'ReviewerIds[0] is empty.'
Assert-True (-not [string]::IsNullOrWhiteSpace($ReviewerIds[1])) 'ReviewerIds[1] is empty.'
Assert-True (-not [StringComparer]::Ordinal.Equals($ReviewerIds[0], $ReviewerIds[1])) 'ReviewerIds must be distinct.'

$rootResult = Invoke-Git -Arguments @('rev-parse', '--show-toplevel')
Assert-True ($rootResult.Count -eq 1) 'Unable to resolve one repository root.'
$RepositoryRoot = [IO.Path]::GetFullPath($rootResult[0].Trim())
Set-Location -LiteralPath $RepositoryRoot

$resolvedBase = (Invoke-Git -Arguments @('rev-parse', '--verify', "$BaseCommit^{commit}"))[0].Trim()
$resolvedDispatch = (Invoke-Git -Arguments @('rev-parse', '--verify', "$DispatchCommit^{commit}"))[0].Trim()
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($resolvedBase, $BaseCommit)) 'BaseCommit did not resolve to the supplied commit.'
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($resolvedDispatch, $DispatchCommit)) 'DispatchCommit did not resolve to the supplied commit.'

$ancestor = Get-GitResult -Arguments @('merge-base', '--is-ancestor', $DispatchCommit, 'HEAD')
Assert-True ($ancestor.ExitCode -eq 0) 'HEAD does not descend from DispatchCommit.'
$dispatchParent = (Invoke-Git -Arguments @('rev-parse', "$DispatchCommit^"))[0].Trim()
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($dispatchParent, $BaseCommit)) 'DispatchCommit sole parent is not BaseCommit.'

$gateAtBase = Get-GitResult -Arguments @('cat-file', '-e', "${BaseCommit}:$GatePath")
Assert-True ($gateAtBase.ExitCode -ne 0) 'Gate 2 record unexpectedly exists at BaseCommit.'
$dispatchChange = Invoke-Git -Arguments @('diff', '--name-status', $BaseCommit, $DispatchCommit, '--')
Assert-SequenceEqual -Actual $dispatchChange -Expected @("A`t$GatePath") -Label 'Dispatch-anchor sole change'
Add-PassedCheck -Number 1 -Name 'two-anchor topology and Gate 2 addition'

$diffCheck = Get-GitResult -Arguments @('diff', '--check', "$BaseCommit...HEAD")
Assert-True ($diffCheck.ExitCode -eq 0) "git diff --check failed: $($diffCheck.Output -join "`n")"
Add-PassedCheck -Number 2 -Name 'base-to-head diff check'

[string[]]$ExpectedBaseChanges = Sort-Ordinal -Values @($AllowedBuilderSurfaces + $GatePath)
[string[]]$ExpectedDispatchChanges = Sort-Ordinal -Values $AllowedBuilderSurfaces
[string[]]$BaseChanges = Sort-Ordinal -Values (Invoke-Git -Arguments @('diff', '--name-only', "$BaseCommit...HEAD", '--'))
[string[]]$DispatchChanges = Sort-Ordinal -Values (Invoke-Git -Arguments @('diff', '--name-only', "$DispatchCommit...HEAD", '--'))
Assert-SequenceEqual -Actual $BaseChanges -Expected $ExpectedBaseChanges -Label 'BaseCommit...HEAD changed files'
Assert-SequenceEqual -Actual $DispatchChanges -Expected $ExpectedDispatchChanges -Label 'DispatchCommit...HEAD changed files'
$gateQuiet = Get-GitResult -Arguments @('diff', '--quiet', "$DispatchCommit...HEAD", '--', $GatePath)
Assert-True ($gateQuiet.ExitCode -eq 0) 'Gate 2 record changed after the dispatch anchor.'
Add-PassedCheck -Number 3 -Name 'exact changed-file sets and frozen Gate 2 diff'

$gateRaw = [IO.File]::ReadAllText((Join-Path $RepositoryRoot $GatePath))
$jsonFenceStarts = [regex]::Matches($gateRaw, '(?im)^```json\b')
$jsonBlocks = [regex]::Matches($gateRaw, '(?ms)^```json[ \t]*\r?\n(?<json>.*?)\r?\n```[ \t]*$')
Assert-True ($jsonFenceStarts.Count -eq 1) 'Gate 2 record must contain exactly one fenced json block.'
Assert-True ($jsonBlocks.Count -eq 1) 'Gate 2 fenced json block is missing or malformed.'
$GateContract = $jsonBlocks[0].Groups['json'].Value | ConvertFrom-Json

[string[]]$ExpectedGateFields = Sort-Ordinal -Values @(
    'allowedBuilderSurfaces', 'branch', 'builderId', 'comparisonBase', 'evidenceDirectory',
    'frozenSurfaces', 'parcel', 'permissionEnvelope', 'reviewerIds', 'schema', 'specPath',
    'specSha256', 'status', 'verificationContract', 'worktree'
)
[string[]]$ActualGateFields = Sort-Ordinal -Values @($GateContract.PSObject.Properties.Name)
Assert-SequenceEqual -Actual $ActualGateFields -Expected $ExpectedGateFields -Label 'Gate 2 fields'

Assert-True ([StringComparer]::Ordinal.Equals([string]$GateContract.schema, 'biostack.p1-gate2.v1')) 'Gate 2 schema mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$GateContract.status, 'approved')) 'Gate 2 status mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$GateContract.parcel, 'P1')) 'Gate 2 parcel mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$GateContract.specPath, $SpecPath)) 'Gate 2 specPath mismatch.'
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals([string]$GateContract.specSha256, $ApprovedSpecSha256)) 'Gate 2 specSha256 mismatch.'
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals([string]$GateContract.comparisonBase, $BaseCommit)) 'Gate 2 comparisonBase mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$GateContract.builderId, $BuilderId)) 'Gate 2 builderId mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$GateContract.permissionEnvelope, 'docs-only:p1-eight-surfaces')) 'Gate 2 permissionEnvelope mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$GateContract.evidenceDirectory, 'artifacts/p1-verification')) 'Gate 2 evidenceDirectory mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$GateContract.verificationContract, 'p1-two-anchor-verifier-v1')) 'Gate 2 verificationContract mismatch.'

$currentBranch = (Invoke-Git -Arguments @('branch', '--show-current'))[0].Trim()
Assert-True ([StringComparer]::Ordinal.Equals([string]$GateContract.branch, $currentBranch)) 'Gate 2 branch does not match the current branch.'
$normalizedRoot = $RepositoryRoot.Replace('\', '/').TrimEnd('/')
$normalizedGateWorktree = ([string]$GateContract.worktree).Replace('\', '/').TrimEnd('/')
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($normalizedGateWorktree, $normalizedRoot)) 'Gate 2 worktree does not match the repository root.'

Assert-SequenceEqual -Actual ([string[]]@($GateContract.reviewerIds)) -Expected $ReviewerIds -Label 'Gate 2 ordered reviewerIds'
Assert-SequenceEqual -Actual ([string[]]@($GateContract.allowedBuilderSurfaces)) -Expected $AllowedBuilderSurfaces -Label 'Gate 2 allowedBuilderSurfaces'
Assert-SequenceEqual -Actual ([string[]]@($GateContract.frozenSurfaces)) -Expected $FrozenSurfaces -Label 'Gate 2 frozenSurfaces'

$actualSpecSha = (Get-FileHash -LiteralPath (Join-Path $RepositoryRoot $SpecPath) -Algorithm SHA256).Hash
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($actualSpecSha, $ApprovedSpecSha256)) 'Approved P1 spec hash mismatch.'
$dispatchGateBlob = (Invoke-Git -Arguments @('rev-parse', "${DispatchCommit}:$GatePath"))[0].Trim()
$headGateBlob = (Invoke-Git -Arguments @('rev-parse', "HEAD:$GatePath"))[0].Trim()
Assert-True ([StringComparer]::Ordinal.Equals($dispatchGateBlob, $headGateBlob)) 'DispatchCommit and HEAD Gate 2 blobs differ.'
Add-PassedCheck -Number 4 -Name 'Gate 2 machine-readable authorization contract'

foreach ($path in @($AllowedBuilderSurfaces + $CoreContextTargets)) {
    Assert-True (Test-Path -LiteralPath (Join-Path $RepositoryRoot $path)) "Required path is missing: $path"
}
Add-PassedCheck -Number 5 -Name 'deliverable and core-context target existence'

$agentsTracked = Get-GitResult -Arguments @('ls-files', '--error-unmatch', '--', 'AGENTS.md')
Assert-True ($agentsTracked.ExitCode -eq 0) 'AGENTS.md is not tracked.'
$agentsNormalized = Get-NormalizedText -Path (Join-Path $RepositoryRoot 'AGENTS.md')
$agentsHash = Get-Sha256Hex -Text $agentsNormalized
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($agentsHash, $AgentsFinalSha256)) 'AGENTS.md normalized final SHA-256 mismatch.'
$pointerDelimiter = "`n`n<!-- biostack:governed-delivery-pointer -->"
$pointerOffset = $agentsNormalized.IndexOf($pointerDelimiter, [StringComparison]::Ordinal)
Assert-True ($pointerOffset -ge 0) 'AGENTS.md does not contain the required single blank line before the pointer block.'
$baselineText = $agentsNormalized.Substring(0, $pointerOffset) + "`n"
$baselineHash = Get-Sha256Hex -Text $baselineText
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($baselineHash, $AgentsBaselineSha256)) 'AGENTS.md pinned RTK baseline changed.'
Assert-True ([regex]::Matches($agentsNormalized, '<!-- biostack:governed-delivery-pointer -->').Count -eq 1) 'AGENTS.md pointer opening marker must appear exactly once.'
Assert-True ([regex]::Matches($agentsNormalized, '<!-- /biostack:governed-delivery-pointer -->').Count -eq 1) 'AGENTS.md pointer closing marker must appear exactly once.'
Add-PassedCheck -Number 6 -Name 'tracked AGENTS.md baseline, pointer, and final hash'

$indexNormalized = Get-NormalizedText -Path (Join-Path $RepositoryRoot 'docs/specs/INDEX.md')
$firstNonblank = @($indexNormalized -split "`n" | Where-Object { $_.Trim().Length -gt 0 })[0]
$requiredHeader = '| Parcel | Status | Spec | Goal Charter | Delivery classes | Guidance classes | Branch/worktree | Owner | Review requirement | Closure |'
Assert-True ([StringComparer]::Ordinal.Equals($firstNonblank, $requiredHeader)) 'INDEX.md first nonblank line is not the exact required header.'
$p1Rows = [regex]::Matches($indexNormalized, '(?m)^\| P1 \|.*$')
Assert-True ($p1Rows.Count -eq 1) 'INDEX.md must contain exactly one row beginning | P1 |.'
$indexRow = $p1Rows[0].Value
$indexCells = $indexRow.Trim().Trim('|').Split('|')
Assert-True ($indexCells.Count -eq 10) 'INDEX.md P1 row must contain exactly ten cells.'
Add-PassedCheck -Number 7 -Name 'exact registry header and P1 row shape'

$LinkResults = New-Object 'System.Collections.Generic.List[object]'
[string[]]$MarkdownSurfaces = @($AllowedBuilderSurfaces | Where-Object { $_.EndsWith('.md', [StringComparison]::OrdinalIgnoreCase) })
foreach ($sourcePath in $MarkdownSurfaces) {
    $sourceFullPath = Join-Path $RepositoryRoot $sourcePath
    $sourceText = [IO.File]::ReadAllText($sourceFullPath)
    foreach ($match in [regex]::Matches($sourceText, '\[[^\]]+\]\(([^)]+)\)')) {
        $target = $match.Groups[1].Value
        if ($target.StartsWith('http://', [StringComparison]::OrdinalIgnoreCase) -or
            $target.StartsWith('https://', [StringComparison]::OrdinalIgnoreCase) -or
            $target.StartsWith('mailto:', [StringComparison]::OrdinalIgnoreCase) -or
            $target.StartsWith('#', [StringComparison]::Ordinal)) {
            continue
        }

        $pathPart = ($target -split '#', 2)[0]
        Assert-True (-not [string]::IsNullOrWhiteSpace($pathPart)) "Empty local link target in $sourcePath."
        $decodedPath = [Uri]::UnescapeDataString($pathPart)
        $resolvedPath = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $sourceFullPath) $decodedPath))
        $exists = Test-Path -LiteralPath $resolvedPath
        Assert-True $exists "Broken local link in ${sourcePath}: $target"
        $LinkResults.Add([pscustomobject]@{
            source = $sourcePath
            target = $target
            resolved = $resolvedPath.Replace('\', '/')
            exists = $true
        }) | Out-Null
    }
}
Add-PassedCheck -Number 8 -Name 'local Markdown link resolution'

$unresolvedPattern = '(?im)(^\s*(TBD|TODO|FIXME)\s*[:|\-])|(\{\{[^}]+\}\})|(<(BASE|BRANCH|WORKTREE|OWNER|REVIEWER)[^>]*>)'
foreach ($path in $AllowedBuilderSurfaces) {
    $content = [IO.File]::ReadAllText((Join-Path $RepositoryRoot $path))
    Assert-True (-not [regex]::IsMatch($content, $unresolvedPattern)) "Unresolved marker found in $path."
}
Add-PassedCheck -Number 9 -Name 'unresolved placeholder scan'

$productionCodeQuiet = Get-GitResult -Arguments @('diff', '--quiet', "$BaseCommit...HEAD", '--', 'frontend', 'backend', 'contracts', '.github')
Assert-True ($productionCodeQuiet.ExitCode -eq 0) 'Production code, contract, or workflow paths changed.'
Add-PassedCheck -Number 10 -Name 'production code and workflow independence'

$frozenQuiet = Get-GitResult -Arguments @(
    'diff', '--quiet', "$BaseCommit...HEAD", '--',
    'docs/INITIATIVES/biostack-production-readiness',
    'docs/INITIATIVES/biostack-governed-delivery/CHARTER.md',
    'docs/INITIATIVES/biostack-governed-delivery/PLAN-REVIEW.md'
)
Assert-True ($frozenQuiet.ExitCode -eq 0) 'Production-readiness, charter, or plan-review content changed.'
Add-PassedCheck -Number 11 -Name 'frozen governance and production-readiness diff'

$productionReadinessText = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'docs/INITIATIVES/biostack-production-readiness/README.md'))
$statusMatches = [regex]::Matches($productionReadinessText, [regex]::Escape($RequiredProductionStatus))
Assert-True ($statusMatches.Count -eq 1) 'Production-readiness NO-GO / HOLD status sentence must appear exactly once.'
Add-PassedCheck -Number 12 -Name 'production-readiness NO-GO / HOLD preservation'

$expectedEvidencePath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot 'artifacts/p1-verification'))
$resolvedEvidencePath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $EvidenceDirectory))
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($resolvedEvidencePath.TrimEnd('\', '/'), $expectedEvidencePath.TrimEnd('\', '/'))) 'EvidenceDirectory must resolve to artifacts/p1-verification under the repository root.'

$trackedEvidence = Get-GitResult -Arguments @('ls-files', '--error-unmatch', '--', 'artifacts/p1-verification')
Assert-True ($trackedEvidence.ExitCode -ne 0) 'artifacts/p1-verification must be untracked.'
[IO.Directory]::CreateDirectory($resolvedEvidencePath) | Out-Null

$headCommit = (Invoke-Git -Arguments @('rev-parse', 'HEAD'))[0].Trim()
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'changed-files.txt') -Content ($BaseChanges -join "`n")
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'link-results.json') -Content ($LinkResults | ConvertTo-Json -Depth 8)
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'index-row.txt') -Content $indexRow
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'agents-sha256.txt') -Content $agentsHash
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'gate2-contract.json') -Content ($GateContract | ConvertTo-Json -Depth 12)
Add-PassedCheck -Number 13 -Name 'authorized UTF-8/LF evidence bundle generation'

$preSummaryStatus = Invoke-Git -Arguments @('status', '--porcelain=v1', '--untracked-files=all')
Assert-OnlyAuthorizedEvidenceStatus -StatusLines $preSummaryStatus
Add-PassedCheck -Number 14 -Name 'committed HEAD and authorized untracked evidence only'

$summary = [ordered]@{
    schema = 'biostack.p1-verification-summary.v1'
    verificationContract = 'p1-two-anchor-verifier-v1'
    pass = $true
    warningState = $false
    comparisonBase = $BaseCommit.ToLowerInvariant()
    dispatchAnchor = $DispatchCommit.ToLowerInvariant()
    headCommit = $headCommit
    builderId = $BuilderId
    reviewerIds = [string[]]$ReviewerIds
    checks = @($CheckResults)
}
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'verification-summary.json') -Content ($summary | ConvertTo-Json -Depth 12)

[string[]]$ExpectedEvidenceFiles = @(
    'changed-files.txt',
    'link-results.json',
    'index-row.txt',
    'agents-sha256.txt',
    'gate2-contract.json',
    'verification-summary.json'
)
foreach ($evidenceFile in $ExpectedEvidenceFiles) {
    Assert-True (Test-Path -LiteralPath (Join-Path $resolvedEvidencePath $evidenceFile)) "Missing evidence file: $evidenceFile"
}

$finalStatus = Invoke-Git -Arguments @('status', '--porcelain=v1', '--untracked-files=all')
Assert-OnlyAuthorizedEvidenceStatus -StatusLines $finalStatus

Write-Output 'P1 verification PASS'
exit 0
