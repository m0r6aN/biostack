[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$BaseCommit,

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

# ---------------------------------------------------------------------------
# Constants: allowed surfaces, closed vocabularies, pinned paths
# ---------------------------------------------------------------------------

[string[]]$AllowedSurfaces = @(
    'docs/specs/schemas/product-capability-safety-contract.json',
    'docs/specs/schemas/product-capability-safety-contract.md',
    'docs/specs/schemas/classification-axes.schema.json',
    'docs/specs/scripts/verify-p0b.ps1',
    'docs/specs/README.md',
    'docs/specs/INDEX.md'
)

[string[]]$RequiredSourceList = @(
    'docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md',
    'docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md',
    'docs/INITIATIVES/biostack-governed-delivery/CHARTER.md',
    'docs/guidance/biostack-guidance-content-contract.v1.md',
    'docs/specs/schemas/classification-axes.schema.json',
    'docs/specs/schemas/delivery-class-controls.json',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md'
)

[string[]]$PlaceholderPatterns = @('TBD', 'TODO', 'FIXME', '{{')

[string[]]$SubstanceFunctionRiskLabels = @(
    'ordinary', 'prescription-treatment-involved', 'investigational-or-unapproved',
    'gray-market-or-identity-uncertain', 'injection-or-sterile-preparation',
    'interaction-or-contraindication-signal', 'minor-or-age-uncertain',
    'pregnancy-or-lactation', 'acute-red-flag-or-emergency',
    'controlled-or-illegal-sourcing'
)
[string[]]$LockedLabels = @('acute-red-flag-or-emergency', 'controlled-or-illegal-sourcing')
[string[]]$NumericProvenanceOrigins = @(
    'user-entered', 'label-or-prescription-transcribed', 'source-studied',
    'biostack-recommended', 'deterministically-derived'
)

$RepositoryRoot = (& git rev-parse --show-toplevel).Trim()
Set-Location $RepositoryRoot

$CheckResults = New-Object 'System.Collections.Generic.List[object]'

# ---------------------------------------------------------------------------
# Generic helpers
# ---------------------------------------------------------------------------

function Assert-True {
    param([Parameter(Mandatory = $true)][bool]$Condition, [Parameter(Mandatory = $true)][string]$Message)
    if (-not $Condition) { throw $Message }
}

function Get-GitResult {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    $output = @(& git @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
    return [pscustomobject]@{ ExitCode = $exitCode; Output = [string[]]@($output | ForEach-Object { [string]$_ }) }
}

function Invoke-Git {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    $result = Get-GitResult -Arguments $Arguments
    if ($result.ExitCode -ne 0) {
        $detail = ($result.Output -join "`n")
        throw "git $($Arguments -join ' ') failed (exit $($result.ExitCode)): $detail"
    }
    return $result.Output
}

function Read-RepoFile {
    param([Parameter(Mandatory = $true)][string]$Path)
    return [IO.File]::ReadAllText((Join-Path $RepositoryRoot $Path))
}

function Write-Utf8Lf {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Content)
    $normalized = $Content -replace "`r`n", "`n"
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [IO.File]::WriteAllText($Path, $normalized, $utf8NoBom)
}

function Add-PassedCheck {
    param([Parameter(Mandatory = $true)][string]$Name, [string]$Detail = '')
    $CheckResults.Add([ordered]@{ name = $Name; pass = $true; detail = $Detail }) | Out-Null
    Write-Output "PASS: $Name"
}

# ---------------------------------------------------------------------------
# Check 1: source-availability-verified -- required sources readable at BaseCommit
# ---------------------------------------------------------------------------

Invoke-Git -Arguments @('cat-file', '-e', "$BaseCommit^{commit}") | Out-Null
foreach ($src in $RequiredSourceList) {
    $probe = Get-GitResult -Arguments @('cat-file', '-e', "${BaseCommit}:$src")
    Assert-True ($probe.ExitCode -eq 0) "source-availability-verified: required source missing at BaseCommit: $src"
    Assert-True (Test-Path -LiteralPath (Join-Path $RepositoryRoot $src)) "source-availability-verified: required source not present in this worktree checkout: $src"
}
Add-PassedCheck -Name 'source-availability-verified' -Detail "$($RequiredSourceList.Count) required-source files checked at BaseCommit"

# ---------------------------------------------------------------------------
# Check 2: canonical-write-fencing-violation -- diff BaseCommit...HEAD stays inside AllowedSurfaces
# ---------------------------------------------------------------------------

$headCommit = (@(Invoke-Git -Arguments @('rev-parse', 'HEAD')))[0].Trim()
$fencingBaseCommit = $BaseCommit

$diffNames = @(Invoke-Git -Arguments @('diff', '--name-only', "$fencingBaseCommit...HEAD"))
$untracked = @(Invoke-Git -Arguments @('ls-files', '--others', '--exclude-standard'))
$workingTreeDirty = @(Invoke-Git -Arguments @('diff', '--name-only', 'HEAD'))
$allChanged = @($diffNames + $untracked + $workingTreeDirty | Where-Object { $_ -and ($_ -notlike 'artifacts/p0b-verification/*') } | Select-Object -Unique)

foreach ($path in $allChanged) {
    Assert-True ($AllowedSurfaces -contains $path) "canonical-write-fencing-violation: $path is outside the allowed-surfaces list."
}
Add-PassedCheck -Name 'canonical-write-fencing-violation (absence)' -Detail "$($allChanged.Count) changed path(s), all inside allowed surfaces"

# ---------------------------------------------------------------------------
# Check 3: no-placeholder -- TBD/TODO/FIXME/{{ absent from every file this parcel creates/modifies
# ---------------------------------------------------------------------------

[string[]]$ModifiedNotNewSurfaces = @('docs/specs/README.md', 'docs/specs/INDEX.md')
foreach ($surface in $AllowedSurfaces) {
    if ($surface -eq 'docs/specs/scripts/verify-p0b.ps1') { continue }
    if ($ModifiedNotNewSurfaces -contains $surface) {
        $unifiedDiff = @(Invoke-Git -Arguments @('diff', "$BaseCommit...HEAD", '--', $surface))
        $addedLines = @($unifiedDiff | Where-Object { $_.StartsWith('+') -and -not $_.StartsWith('+++') } | ForEach-Object { $_.Substring(1) })
        $content = $addedLines -join "`n"
    } else {
        $content = Read-RepoFile -Path $surface
    }
    foreach ($pattern in $PlaceholderPatterns) {
        Assert-True ((-not $content.Contains($pattern))) "no-placeholder: literal marker '$pattern' found in $surface"
    }
}
Add-PassedCheck -Name 'no-placeholder' -Detail "TBD/TODO/FIXME/{{ scanned across $($AllowedSurfaces.Count) allowed-surface files (this script's own source excluded as pinned scan apparatus)"

# ---------------------------------------------------------------------------
# Check 4: unexpected-numeric-surface -- no formula/rounding-rule/calculation pattern
# ---------------------------------------------------------------------------

[string[]]$NumericSurfaceScanTargets = @(
    'docs/specs/schemas/product-capability-safety-contract.md'
)
$formulaPattern = '(?m)^\s*[A-Za-z_][A-Za-z0-9_]*\s*[:=]\s*[-+*/0-9().\s]+$'
foreach ($target in $NumericSurfaceScanTargets) {
    $content = Read-RepoFile -Path $target
    Assert-True ((-not [regex]::IsMatch($content, $formulaPattern))) "unexpected-numeric-surface: formula-like pattern found in $target"
}
Add-PassedCheck -Name 'unexpected-numeric-surface (absence)' -Detail "$($NumericSurfaceScanTargets.Count) prose content file(s) scanned; the JSON artifact and verify-p0b.ps1 itself are structural/scripting surfaces, not product prose, out of this check's scope"

# ---------------------------------------------------------------------------
# Check 5: personal-data-token-found -- grep-based scan, zero matches
# ---------------------------------------------------------------------------

[string]$EmailPattern = '[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}'
foreach ($surface in $AllowedSurfaces) {
    $content = Read-RepoFile -Path $surface
    Assert-True ((-not [regex]::IsMatch($content, $EmailPattern))) "personal-data-token-found: email-shaped token found in $surface"
}
Add-PassedCheck -Name 'personal-data-token-found (absence)' -Detail "email-pattern scan across $($AllowedSurfaces.Count) allowed-surface files; zero personal data expected (this parcel processes none)"

# ---------------------------------------------------------------------------
# Load the JSON artifact and the design-gate ruled table
# ---------------------------------------------------------------------------

$contractJsonText = Read-RepoFile -Path 'docs/specs/schemas/product-capability-safety-contract.json'
$contract = $contractJsonText | ConvertFrom-Json -Depth 20
$contractMdContent = Read-RepoFile -Path 'docs/specs/schemas/product-capability-safety-contract.md'
$designGateContent = Read-RepoFile -Path 'docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md'

# ---------------------------------------------------------------------------
# Check 6: vocabulary-closure -- labels object = exactly the 10 closed labels
# ---------------------------------------------------------------------------

$labelsObj = $contract.labels
$jsonLabelNames = @($labelsObj.PSObject.Properties.Name)
Assert-True ($jsonLabelNames.Count -eq 10) "vocabulary-closure: expected exactly 10 labels, found $($jsonLabelNames.Count)."
foreach ($lbl in $SubstanceFunctionRiskLabels) {
    Assert-True ($jsonLabelNames -contains $lbl) "vocabulary-closure: closed-vocabulary label '$lbl' is missing from the contract's labels object."
}
foreach ($lbl in $jsonLabelNames) {
    Assert-True ($SubstanceFunctionRiskLabels -contains $lbl) "vocabulary-closure: labels object carries an unrecognized label '$lbl' not in the closed vocabulary."
}
$axesContent = Read-RepoFile -Path 'docs/specs/schemas/classification-axes.schema.json'
$axesObj = $axesContent | ConvertFrom-Json -Depth 20
$axesLabels = @($axesObj.axes.substanceFunctionRisk.labels)
Assert-True ($axesLabels.Count -eq 10) "vocabulary-closure: classification-axes.schema.json substanceFunctionRisk.labels does not carry exactly 10 labels."
foreach ($lbl in $axesLabels) {
    Assert-True ($jsonLabelNames -contains $lbl) "vocabulary-closure: classification-axes.schema.json label '$lbl' is not present in the contract's labels object."
}
Add-PassedCheck -Name 'vocabulary-closure' -Detail "labels object carries exactly the 10 classification-axes.schema.json substanceFunctionRisk labels, no more, no fewer"

# ---------------------------------------------------------------------------
# Check 7: enablement-field-fidelity
# ---------------------------------------------------------------------------

$enablement = $contract.enablementState.biostackRecommendedOrigination
Assert-True ($enablement.publiclyEnabled -eq $false) 'enablement-field-fidelity: enablementState.biostackRecommendedOrigination.publiclyEnabled is not the literal boolean false.'
Assert-True ($enablement.definedInContract -eq $true) 'enablement-field-fidelity: definedInContract must be true.'
Assert-True ($enablement.currentPosture -eq 'deterministic-math-on-user-entered-values-plus-guidance-contract-v1.0.0-class-A-B-C') 'enablement-field-fidelity: currentPosture mismatch.'
Assert-True ($enablement.governingGuidanceContractVersion -eq '1.0.0') 'enablement-field-fidelity: governingGuidanceContractVersion mismatch.'
Assert-True ($enablement.requiredGuidanceContractVersionForPublicEnablement -eq '2.0.0') 'enablement-field-fidelity: requiredGuidanceContractVersionForPublicEnablement mismatch.'
Assert-True ($enablement.requiredApprovalLevelForPublicEnablement -eq 'legal_product_ratification') 'enablement-field-fidelity: requiredApprovalLevelForPublicEnablement mismatch.'
Assert-True ($enablement.publicEnablementEvent -eq 'not-yet-occurred-separate-future-owner-event') 'enablement-field-fidelity: publicEnablementEvent mismatch.'
Assert-True ($enablement.rulingReference -eq 'COORDINATOR-DECISIONS-2026-10-07.md#D-I') 'enablement-field-fidelity: rulingReference mismatch.'

# Provenance-trace / allow-list model (R2-F3; replaces a bare forbidden-phrase blocklist, which is
# evadable by any additive free-text key/sentence that never uses one of the listed phrases).
# `enablementState.biostackRecommendedOrigination` is a CLOSED object: exactly the eight fields
# this contract defines, and no other key, may exist on it. Any additive key -- regardless of its
# wording -- is uncited, unauthorized content and fails this check outright; nothing needs to
# match a specific forbidden phrase for the check to catch it.
[string[]]$AllowedEnablementStateKeys = @(
    'definedInContract', 'publiclyEnabled', 'currentPosture', 'governingGuidanceContractVersion',
    'requiredGuidanceContractVersionForPublicEnablement', 'requiredApprovalLevelForPublicEnablement',
    'publicEnablementEvent', 'rulingReference'
)
$enablementActualKeys = @($enablement.PSObject.Properties.Name)
foreach ($k in $enablementActualKeys) {
    Assert-True ($AllowedEnablementStateKeys -contains $k) "enablement-field-fidelity: enablementState.biostackRecommendedOrigination carries an additive key '$k' not on the closed allow-list -- uncited behavior-affecting content (e.g. an availability claim smuggled in via a new field) is not permitted, regardless of its wording."
}
Assert-True ($enablementActualKeys.Count -eq $AllowedEnablementStateKeys.Count) "enablement-field-fidelity: enablementState.biostackRecommendedOrigination expected exactly $($AllowedEnablementStateKeys.Count) fields, found $($enablementActualKeys.Count)."

# Defense-in-depth: no sentence anywhere in the two artifacts may assert public Class D/C3
# availability today (retained as a secondary net; the closed-object check above is the primary,
# non-lexical control that actually closes the demonstrated evasion).
[string[]]$ForbiddenAvailabilityPhrases = @(
    'publiclyEnabled": true',
    'publiclyEnabled is true',
    'now publicly available',
    'currently publicly enabled',
    'v2.0.0 has been ratified',
    'v2.0.0 is ratified'
)
foreach ($phrase in $ForbiddenAvailabilityPhrases) {
    Assert-True ((-not $contractJsonText.Contains($phrase))) "enablement-field-fidelity: forbidden availability phrase '$phrase' found in the JSON artifact."
    Assert-True ((-not $contractMdContent.Contains($phrase))) "enablement-field-fidelity: forbidden availability phrase '$phrase' found in the Markdown artifact."
}
# Every label's C3 cell is gated.
foreach ($lblName in $jsonLabelNames) {
    $lblObj = $labelsObj.$lblName
    Assert-True ($lblObj.enablementGatesC3 -eq $true) "enablement-field-fidelity: label '$lblName' does not carry enablementGatesC3: true."
}
Add-PassedCheck -Name 'enablement-field-fidelity' -Detail "enablementState literal fragment verified field-by-field; publiclyEnabled=false; all 10 labels carry enablementGatesC3=true; no forbidden availability phrase found"

# ---------------------------------------------------------------------------
# Check 8: cell-semantics-fidelity
# ---------------------------------------------------------------------------

$cellSemantics = $contract.cellSemantics
Assert-True ($cellSemantics.allowed -eq 'A — allowed') 'cell-semantics-fidelity: allowed literal mismatch.'
Assert-True ($cellSemantics.degraded -eq 'D — degraded (allowed with mandatory evidence/explanation/validation additions)') 'cell-semantics-fidelity: degraded literal mismatch (parenthetical dropped/shortened/paraphrased).'
Assert-True ($cellSemantics.refused -eq 'R — refused') 'cell-semantics-fidelity: refused literal mismatch.'
Assert-True ($cellSemantics.escalated -eq 'E — escalate (stop ordinary output, surface escalation)') 'cell-semantics-fidelity: escalated literal mismatch (parenthetical dropped/shortened/paraphrased).'
Assert-True ($contractMdContent.Contains('D — degraded (allowed with mandatory evidence/explanation/validation additions)')) 'cell-semantics-fidelity: degraded literal missing from Markdown artifact.'
Assert-True ($contractMdContent.Contains('E — escalate (stop ordinary output, surface escalation)')) 'cell-semantics-fidelity: escalated literal missing from Markdown artifact.'
Add-PassedCheck -Name 'cell-semantics-fidelity' -Detail 'all four cellSemantics literals (including operative parentheticals) verified byte-for-byte in JSON and present in Markdown'

# ---------------------------------------------------------------------------
# Check 9: qualified-cell-scope-present
# ---------------------------------------------------------------------------

$scopeExpectations = @{
    'pregnancy-or-lactation'             = @{ cell = 'C1'; scope = 'dose-context-only' }
    'prescription-treatment-involved'    = @{ cell = 'C3'; scope = 'prescribed-treatment-only' }
}
foreach ($lblName in $jsonLabelNames) {
    $lblObj = $labelsObj.$lblName
    foreach ($cellName in @('C1', 'C2', 'C3')) {
        $cellObj = $lblObj.behavior.$cellName
        $expected = $null
        if ($scopeExpectations.ContainsKey($lblName) -and $scopeExpectations[$lblName].cell -eq $cellName) {
            $expected = $scopeExpectations[$lblName].scope
        }
        if ($null -eq $expected) {
            Assert-True ($null -eq $cellObj.scope) "qualified-cell-scope-present: $lblName.$cellName must have a null scope; found '$($cellObj.scope)'."
        } else {
            Assert-True ($cellObj.scope -eq $expected) "qualified-cell-scope-present: $lblName.$cellName must carry scope '$expected'; found '$($cellObj.scope)'."
        }
    }
}
Add-PassedCheck -Name 'qualified-cell-scope-present' -Detail 'pregnancy-or-lactation.C1=dose-context-only and prescription-treatment-involved.C3=prescribed-treatment-only carry non-null scope; every other cell is null'

# ---------------------------------------------------------------------------
# Check 10: unbounded-operationalization -- structural provenance-trace model (R1-F2/R2-F2).
#
# A bare forbidden-phrase blocklist (the prior implementation) is trivially evaded by paraphrase:
# any behavior-affecting clause that avoids the exact listed substrings passes silently. This
# check instead runs an ALLOW-LIST / provenance-trace model: any clause inside an
# `applicabilityCriterion.test` field that uses behavior-outcome vocabulary (naming a disposition
# this contract's matrix already governs -- refuse/allow/degrade/escalate/block/suppress, in any
# inflection) is only permitted if it carries, nearby, an explicit `[cite: ...]` provenance tag
# that resolves to either a ruled-rule ID (`D-B1`..`D-B6`, optionally `.ruleN`) or a matrix cell ID
# (`D-B2.<label>.<C1|C2|C3>`). Behavior-affecting vocabulary with no nearby citation fails closed,
# regardless of its exact wording -- this is what makes the control paraphrase-proof: the test
# does not look for specific forbidden words, it demands every behavior-affecting mention justify
# itself against the ruled matrix/rules it is citing, or be absent.
# ---------------------------------------------------------------------------

$citationTagPattern = '\[cite:\s*(D-B[1-6](?:\.rule[1-9][0-9]*)?|D-B2\.[a-z0-9-]+\.C[1-3])\]'
$behaviorStemPattern = '(?i)(refus\w*|allow\w*|degrad\w*|escalat\w*|\bblock\w*|suppress\w*)'

foreach ($lblName in $jsonLabelNames) {
    $lblObj = $labelsObj.$lblName
    $basis = [string]$lblObj.applicabilityCriterion.basis
    Assert-True (-not [string]::IsNullOrWhiteSpace($basis)) "unbounded-operationalization: $lblName applicabilityCriterion has no basis."
    Assert-True (($basis.Contains('[OPERATIONALIZED') -or $basis.Contains('[RULED')) -and $basis.Contains($lblName)) "unbounded-operationalization: $lblName applicabilityCriterion basis does not cite a traceable provenance marker and the label name."
    $test = [string]$lblObj.applicabilityCriterion.test
    Assert-True (-not [string]::IsNullOrWhiteSpace($test)) "unbounded-operationalization: $lblName applicabilityCriterion has no test."

    $citationHits = @([regex]::Matches($test, $citationTagPattern))
    foreach ($stemMatch in [regex]::Matches($test, $behaviorStemPattern)) {
        $windowStart = [Math]::Max(0, $stemMatch.Index - 150)
        $windowEnd = [Math]::Min($test.Length, $stemMatch.Index + $stemMatch.Length + 150)
        $window = $test.Substring($windowStart, $windowEnd - $windowStart)
        $hasNearbyCitation = [regex]::IsMatch($window, $citationTagPattern)
        Assert-True ($hasNearbyCitation) "unbounded-operationalization: $lblName applicabilityCriterion test contains behavior-affecting vocabulary ('$($stemMatch.Value)') with no nearby [cite: D-Bn | D-B2.<label>.<Cn>] provenance citation -- uncited behavior-affecting content fails this check regardless of wording (allow-list provenance model, not a forbidden-phrase blocklist)."
    }
    # Every citation tag present must actually resolve to a real matrix cell (this label's own
    # behavior object) or a real ruled-rule ID -- a fabricated/dangling citation does not launder
    # an uncited behavior decision.
    foreach ($cm in $citationHits) {
        $cited = $cm.Groups[1].Value
        if ($cited -match '^D-B2\.([a-z0-9-]+)\.(C[1-3])$') {
            $citedLabel = $Matches[1]
            $citedCell = $Matches[2]
            Assert-True ($jsonLabelNames -contains $citedLabel) "unbounded-operationalization: $lblName applicabilityCriterion test cites a matrix cell for unrecognized label '$citedLabel'."
            $citedLblObj = $labelsObj.$citedLabel
            Assert-True ($null -ne $citedLblObj.behavior.$citedCell) "unbounded-operationalization: $lblName applicabilityCriterion test cites matrix cell '$cited' which does not resolve to a real behavior cell."
        } elseif ($cited -notmatch '^D-B[1-6](\.rule[1-9][0-9]*)?$') {
            Assert-True ($false) "unbounded-operationalization: $lblName applicabilityCriterion test cites an unrecognized provenance tag '[cite: $cited]'."
        }
    }
}
Add-PassedCheck -Name 'unbounded-operationalization' -Detail 'every applicabilityCriterion cites a traceable basis (provenance marker + label name); every behavior-affecting clause inside a test field carries a resolvable [cite: D-Bn | D-B2.<label>.<Cn>] provenance tag (allow-list provenance-trace model, paraphrase-proof)'

# ---------------------------------------------------------------------------
# Check 11: matrix-fidelity -- parse D-B2 table from P0-B-DESIGN-GATE.md, diff every cell
# against the JSON artifact (adversarial, byte-for-byte, zero tolerated drift)
# ---------------------------------------------------------------------------

# Confined to a single physical line and to non-pipe content only ([ \t] not \s, [^|\r\n] not .): a
# prior version of this pattern used \s*/.+? , which can cross a newline inside the \s* separators
# between cells and spuriously match into a *different*, differently-shaped pipe table immediately
# below (e.g. the two-column "Per-label applicability criteria" table) when the current row runs
# out of its own pipe-delimited cells. Tightened so each match is provably one single table row.
$tableRowPattern = '(?m)^\|[ \t]*`([a-z0-9-]+)`[ \t]*\|[ \t]*([^|\r\n]+?)[ \t]*\|[ \t]*([^|\r\n]+?)[ \t]*\|[ \t]*([^|\r\n]+?)[ \t]*\|[ \t]*([^|\r\n]+?)[ \t]*\|[ \t]*$'
$tableMatches = [regex]::Matches($designGateContent, $tableRowPattern)
Assert-True ($tableMatches.Count -ge 10) "matrix-fidelity: expected at least 10 D-B2 table rows parsed from P0-B-DESIGN-GATE.md, found $($tableMatches.Count)."

function ConvertTo-ExpectedCell {
    param([Parameter(Mandatory = $true)][string]$RawCell)
    $stripped = ($RawCell -replace '\*\*', '').Trim()
    $scope = $null
    if ($stripped -match '\(dose-context\)') { $scope = 'dose-context-only' }
    if ($stripped -match '\(for the prescribed treatment\)') { $scope = 'prescribed-treatment-only' }
    if ($stripped -match '^R\s*\+\s*E$') {
        return [pscustomobject]@{ Value = 'refused-and-escalated'; Scope = $scope }
    }
    if ($stripped -match '^D\s*→\s*E$') {
        return [pscustomobject]@{ Value = 'degraded-escalates-on-strong-signal'; Scope = $scope }
    }
    $leadingLetter = ($stripped -replace '[^ADRE].*$', '')
    $value = switch ($leadingLetter) {
        'A' { 'allowed' }
        'D' { 'degraded' }
        'R' { 'refused' }
        'E' { 'escalated' }
        default { throw "matrix-fidelity: unrecognized cell literal '$RawCell' (stripped '$stripped')." }
    }
    return [pscustomobject]@{ Value = $value; Scope = $scope }
}

$matrixLabelsSeen = New-Object 'System.Collections.Generic.HashSet[string]'
$statesSeen = New-Object 'System.Collections.Generic.HashSet[string]'
$expectedByLabel = @{}
foreach ($m in $tableMatches) {
    $label = $m.Groups[1].Value
    if (-not ($SubstanceFunctionRiskLabels -contains $label)) { continue }
    $matrixLabelsSeen.Add($label) | Out-Null
    $rawC1 = $m.Groups[2].Value
    $rawC2 = $m.Groups[3].Value
    $rawC3 = $m.Groups[4].Value
    $rawCalibration = $m.Groups[5].Value.Trim()

    $expC1 = ConvertTo-ExpectedCell -RawCell $rawC1
    $expC2 = ConvertTo-ExpectedCell -RawCell $rawC2
    $expC3 = ConvertTo-ExpectedCell -RawCell $rawC3

    $lblObj = $labelsObj.$label
    Assert-True ($null -ne $lblObj) "matrix-fidelity: label '$label' from the ruled table is missing from the JSON artifact."

    foreach ($pair in @(
            @{ Name = 'C1'; Exp = $expC1 },
            @{ Name = 'C2'; Exp = $expC2 },
            @{ Name = 'C3'; Exp = $expC3 }
        )) {
        $actual = $lblObj.behavior.($pair.Name)
        Assert-True ($actual.value -eq $pair.Exp.Value) "matrix-fidelity: $label.$($pair.Name) expected value '$($pair.Exp.Value)' (from ruled cell), found '$($actual.value)'."
        if ($null -ne $pair.Exp.Scope) {
            Assert-True ($actual.scope -eq $pair.Exp.Scope) "matrix-fidelity: $label.$($pair.Name) expected scope '$($pair.Exp.Scope)', found '$($actual.scope)'."
        }
        $statesSeen.Add($pair.Exp.Value) | Out-Null
    }

    $actualCalibration = [string]$lblObj.calibrationRequired
    Assert-True ($actualCalibration.Trim() -eq $rawCalibration.Trim()) "matrix-fidelity: $label calibrationRequired text diverges from the ruled table's Calibration required column.`nExpected: $rawCalibration`nFound:    $actualCalibration"

    $expectedLocked = ($LockedLabels -contains $label)
    Assert-True ($lblObj.locked -eq $expectedLocked) "matrix-fidelity: $label.locked expected $expectedLocked, found $($lblObj.locked)."

    $expectedByLabel[$label] = [pscustomobject]@{ C1 = $expC1; C2 = $expC2; C3 = $expC3; Calibration = $rawCalibration.Trim() }
}
Assert-True ($matrixLabelsSeen.Count -eq 10) "matrix-fidelity: expected all 10 labels parsed from the ruled table, found $($matrixLabelsSeen.Count)."

# JSON and MD must never diverge: every labelled behavior literal text appearing in the JSON
# artifact's non-null `value` set must also appear in the Markdown artifact's reproduced ruled
# table / composite-cell explanation prose.
Assert-True ($contractMdContent.Contains('degraded-escalates-on-strong-signal')) 'matrix-fidelity: Markdown artifact missing the degraded-escalates-on-strong-signal composite-cell encoding note (JSON/MD divergence).'
Assert-True ($contractMdContent.Contains('refused-and-escalated')) 'matrix-fidelity: Markdown artifact missing the refused-and-escalated composite-cell encoding note (JSON/MD divergence).'
Assert-True ($contractMdContent.Contains('dose-context-only')) 'matrix-fidelity: Markdown artifact missing the dose-context-only scope value (JSON/MD divergence).'
Assert-True ($contractMdContent.Contains('prescribed-treatment-only')) 'matrix-fidelity: Markdown artifact missing the prescribed-treatment-only scope value (JSON/MD divergence).'

# R1-F1 (BLOCKER remediation): the prior implementation only probed the Markdown artifact with
# four fixed substring .Contains(...) checks, above -- it never parsed the Markdown artifact's OWN
# per-label behavior-matrix table and diffed it against ground truth, so a silently-corrupted
# Markdown table cell (e.g. weakening a C2/C3 literal while leaving the JSON artifact untouched)
# passed unnoticed. This block closes that gap: it parses
# `product-capability-safety-contract.md`'s own "Per-label behavior matrix" table with the exact
# same `$tableRowPattern` / `ConvertTo-ExpectedCell` logic already used above for the design-gate
# table, and asserts cell-for-cell equality against the frozen ground truth parsed from
# `P0-B-DESIGN-GATE.md` §3 directly (`$expectedByLabel`, built above) -- not against the JSON
# artifact's own claims, so a JSON/MD co-corruption (or a corruption of the Markdown table alone)
# cannot pass by the two artifacts merely agreeing with each other.
$mdTableMatches = [regex]::Matches($contractMdContent, $tableRowPattern)
$mdMatrixLabelsSeen = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($m in $mdTableMatches) {
    $label = $m.Groups[1].Value
    if (-not ($SubstanceFunctionRiskLabels -contains $label)) { continue }
    if (-not $expectedByLabel.ContainsKey($label)) { continue }
    $mdMatrixLabelsSeen.Add($label) | Out-Null

    $mdRawC1 = $m.Groups[2].Value
    $mdRawC2 = $m.Groups[3].Value
    $mdRawC3 = $m.Groups[4].Value
    $mdRawCalibration = $m.Groups[5].Value.Trim()

    $mdC1 = ConvertTo-ExpectedCell -RawCell $mdRawC1
    $mdC2 = ConvertTo-ExpectedCell -RawCell $mdRawC2
    $mdC3 = ConvertTo-ExpectedCell -RawCell $mdRawC3
    $groundTruth = $expectedByLabel[$label]

    foreach ($pair in @(
            @{ Name = 'C1'; Actual = $mdC1; Exp = $groundTruth.C1 },
            @{ Name = 'C2'; Actual = $mdC2; Exp = $groundTruth.C2 },
            @{ Name = 'C3'; Actual = $mdC3; Exp = $groundTruth.C3 }
        )) {
        Assert-True ($pair.Actual.Value -eq $pair.Exp.Value) "matrix-fidelity: the Markdown artifact's OWN per-label behavior matrix table cell $label.$($pair.Name) expected value '$($pair.Exp.Value)' (from P0-B-DESIGN-GATE.md's ruled table, ground truth), found '$($pair.Actual.Value)' in product-capability-safety-contract.md -- the Markdown artifact itself has drifted from ground truth."
        if ($null -ne $pair.Exp.Scope) {
            Assert-True ($pair.Actual.Scope -eq $pair.Exp.Scope) "matrix-fidelity: the Markdown artifact's OWN per-label behavior matrix table cell $label.$($pair.Name) expected scope '$($pair.Exp.Scope)' (ground truth), found '$($pair.Actual.Scope)' in product-capability-safety-contract.md."
        }
    }
    Assert-True ($mdRawCalibration -eq $groundTruth.Calibration) "matrix-fidelity: the Markdown artifact's OWN per-label behavior matrix table Calibration-required text for $label diverges from ground truth (P0-B-DESIGN-GATE.md's ruled table).`nExpected: $($groundTruth.Calibration)`nFound in .md: $mdRawCalibration"
}
Assert-True ($mdMatrixLabelsSeen.Count -eq 10) "matrix-fidelity: expected all 10 labels parsed from the Markdown artifact's own per-label behavior matrix table, found $($mdMatrixLabelsSeen.Count) -- the Markdown artifact's own table is a required, independently-verified content surface, not merely a JSON-mirroring claim."

Add-PassedCheck -Name 'matrix-fidelity' -Detail "all 10 labels x 3 guidance-class cells (30 cells) + calibrationRequired text + locked flag programmatically diffed against P0-B-DESIGN-GATE.md's parsed D-B2 table for BOTH artifacts independently (JSON and the Markdown artifact's own table); zero drift"

# ---------------------------------------------------------------------------
# Check 12: missing-behavior-state-coverage -- all four states present at least once
# ---------------------------------------------------------------------------

Assert-True ($statesSeen.Contains('allowed')) 'missing-behavior-state-coverage: no allowed cell found.'
Assert-True ($statesSeen.Contains('degraded')) 'missing-behavior-state-coverage: no degraded cell found.'
Assert-True ($statesSeen.Contains('refused')) 'missing-behavior-state-coverage: no refused cell found.'
$escalatedPresent = $statesSeen.Contains('escalated') -or $statesSeen.Contains('degraded-escalates-on-strong-signal') -or $statesSeen.Contains('refused-and-escalated')
Assert-True ($escalatedPresent) 'missing-behavior-state-coverage: no escalated (or escalation-composite) cell found.'
Add-PassedCheck -Name 'missing-behavior-state-coverage (absence)' -Detail 'allowed, degraded, refused, and escalated/escalation-composite states all present across the matrix'

# ---------------------------------------------------------------------------
# Check 13: axis-binding-diff-scoped -- classification-axes.schema.json diff touches only
# the exact fields named in P0-B.md "Exact allowed surfaces," item 3
# ---------------------------------------------------------------------------

$axesDiff = @(Invoke-Git -Arguments @('diff', "$BaseCommit...HEAD", '--', 'docs/specs/schemas/classification-axes.schema.json'))
$axesDiffChangedLines = @($axesDiff | Where-Object { ($_.StartsWith('+') -and -not $_.StartsWith('+++')) -or ($_.StartsWith('-') -and -not $_.StartsWith('---')) })
foreach ($line in $axesDiffChangedLines) {
    $bareLine = $line.Substring(1).Trim()
    $isAllowed = (
        $bareLine -eq '"controlBindingStatus": "deferred-to-P0-B",' -or
        $bareLine -eq '"controlBindingStatus": "bound-at-this-parcel",' -or
        $bareLine -eq '"allowedOutputBindingStatus": "out-of-scope-for-P2",' -or
        $bareLine -eq '"allowedOutputBindingStatus": "bound-at-P0-B",' -or
        $bareLine -eq '"determinationAuthority": "deferred-to-P0-B-deterministic-criteria",' -or
        $bareLine -eq '"determinationAuthority": "defined-by-product-capability-safety-contract-v1.0.0",' -or
        $bareLine -eq '"status": "deferred"' -or
        $bareLine -eq '"status": "defined"'
    )
    Assert-True ($isAllowed) "axis-binding-diff-scoped: unexpected diff line in classification-axes.schema.json: $line"
}
# Confirm the productGuidanceClass and deliveryClass axis objects are byte-identical (untouched).
$axesAtBase = (Invoke-Git -Arguments @('show', "${BaseCommit}:docs/specs/schemas/classification-axes.schema.json")) -join "`n"
$axesAtBaseObj = $axesAtBase | ConvertFrom-Json -Depth 20
Assert-True (($axesObj.axes.deliveryClass | ConvertTo-Json -Depth 20 -Compress) -eq ($axesAtBaseObj.axes.deliveryClass | ConvertTo-Json -Depth 20 -Compress)) 'axis-binding-diff-scoped: deliveryClass axis object was modified; it must be untouched.'
Assert-True (($axesObj.axes.productGuidanceClass | ConvertTo-Json -Depth 20 -Compress) -eq ($axesAtBaseObj.axes.productGuidanceClass | ConvertTo-Json -Depth 20 -Compress)) 'axis-binding-diff-scoped: productGuidanceClass axis object was modified; it must be untouched.'
Assert-True ($axesObj.axes.substanceFunctionRisk.labels.Count -eq $axesAtBaseObj.axes.substanceFunctionRisk.labels.Count) 'axis-binding-diff-scoped: substanceFunctionRisk label count changed.'
for ($i = 0; $i -lt $axesObj.axes.substanceFunctionRisk.labels.Count; $i++) {
    Assert-True ($axesObj.axes.substanceFunctionRisk.labels[$i] -eq $axesAtBaseObj.axes.substanceFunctionRisk.labels[$i]) "axis-binding-diff-scoped: substanceFunctionRisk label at index $i changed (no label may be added, removed, renamed)."
}
Add-PassedCheck -Name 'axis-binding-diff-scoped' -Detail 'classification-axes.schema.json diff touches only controlBindingStatus/allowedOutputBindingStatus/determinationAuthority/status; deliveryClass and productGuidanceClass axis objects and the label list are byte-identical to BaseCommit'

# ---------------------------------------------------------------------------
# Check 14: no-unattributed-claim -- every ruled block carries a ruledBasis citation
# ---------------------------------------------------------------------------

# R2-F1 remediation: applicabilityCriterionDeferralRule transcribes the closing paragraph of
# P0-B.md's "Per-label applicability criteria" section, which P0-B.md's own section heading marks
# `[OPERATIONALIZED — bounded]` ("required content -- authored by this parcel ... not a ruled
# D-B1..D-B6 clause"), never `[RULED — verbatim]`. Checked here, ground-truth / non-lexically:
# this contract's citation of that section may never claim `[RULED` while the named source section
# in P0-B.md itself is marked operationalized-bounded.
$p0bMdContent = Read-RepoFile -Path 'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md'
Assert-True ($p0bMdContent.Contains('### Per-label applicability criteria (required content') -and $p0bMdContent.Contains(', not a ruled D-B1..D-B6 clause)')) 'no-unattributed-claim: could not re-confirm, in P0-B.md itself, that the "Per-label applicability criteria" section is marked [OPERATIONALIZED -- bounded] (source-text drift -- cannot validate the deferral-rule marker against ground truth).'
Assert-True ((-not $contract.applicabilityCriterionDeferralRule.basis.Contains('[RULED')) -and $contract.applicabilityCriterionDeferralRule.basis.Contains('[OPERATIONALIZED')) 'no-unattributed-claim: applicabilityCriterionDeferralRule.basis claims [RULED -- verbatim], but its named source (P0-B.md, "Per-label applicability criteria" closing paragraph) lives inside a section P0-B.md itself marks [OPERATIONALIZED -- bounded], not a ruled D-B1..D-B6 clause -- mislabeling an operationalized clause as owner-ruled falsely elevates its immutability status.'

[string[]]$RuledBlockKeys = @('preemptionOrder', 'numericProvenance', 'missingInputLadder', 'functionReviewStatus', 'escalationSemantics', 'cellSemantics', 'doseContextDefinition', 'applicabilityCriterionDeferralRule')
foreach ($key in $RuledBlockKeys) {
    $block = $contract.$key
    Assert-True ($null -ne $block) "no-unattributed-claim: top-level key '$key' is missing from the contract."
    $basis = [string]$block.ruledBasis
    if ([string]::IsNullOrWhiteSpace($basis)) { $basis = [string]$block.basis }
    Assert-True (-not [string]::IsNullOrWhiteSpace($basis)) "no-unattributed-claim: top-level key '$key' has no ruledBasis/basis provenance citation."
    Assert-True ($basis.Contains('[RULED') -or $basis.Contains('[OPERATIONALIZED') -or $basis.Contains('D-J') -or $basis.Contains('D-K')) "no-unattributed-claim: top-level key '$key' provenance citation '$basis' does not carry a recognizable [RULED — verbatim]/[OPERATIONALIZED — bounded] marker."
}
Add-PassedCheck -Name 'no-unattributed-claim' -Detail "all $($RuledBlockKeys.Count) top-level ruled blocks plus every per-label applicabilityCriterion carry an explicit [RULED — verbatim]/[OPERATIONALIZED — bounded] provenance citation"

# ---------------------------------------------------------------------------
# Check 15: unresolvable-citation -- every docs/... path token in the two artifacts
# resolves at BaseCommit
# ---------------------------------------------------------------------------

$citedPathPattern = '`(docs/[A-Za-z0-9_\-./]+\.(?:md|json))`'
$citedPaths = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($content in @($contractJsonText, $contractMdContent)) {
    foreach ($m in [regex]::Matches($content, $citedPathPattern)) {
        $citedPaths.Add($m.Groups[1].Value) | Out-Null
    }
}
$unresolvable = New-Object 'System.Collections.Generic.List[string]'
foreach ($p in $citedPaths) {
    $probe = Get-GitResult -Arguments @('cat-file', '-e', "${BaseCommit}:$p")
    if ($probe.ExitCode -ne 0) { $unresolvable.Add($p) | Out-Null }
}
Assert-True ($unresolvable.Count -eq 0) "unresolvable-citation: $($unresolvable.Count) cited path(s) do not resolve at BaseCommit: $($unresolvable -join ', ')"
Add-PassedCheck -Name 'unresolvable-citation (absence)' -Detail "$($citedPaths.Count) distinct cited file path(s) resolved at BaseCommit"

# ---------------------------------------------------------------------------
# Check 16: enablement-hard-fail-fixture -- adapted structural proof. P0-B ships no runtime; this
# proves the contract's own structure would force a hard refusal for any surface attempting a
# biostack-recommended (C3) output while publiclyEnabled is false, rather than merely stating the
# flag's value.
# ---------------------------------------------------------------------------

function Test-FixtureRefused {
    param([Parameter(Mandatory = $true)][string]$LabelName, [Parameter(Mandatory = $true)][object]$Contract)
    # Mirrors the ruled rule the fixture exercises: enablementState.publiclyEnabled gates every
    # C3 cell (per "enablementGatesC3": true) regardless of that label's own table-designed value.
    $publiclyEnabled = [bool]$Contract.enablementState.biostackRecommendedOrigination.publiclyEnabled
    $lbl = $Contract.labels.$LabelName
    $gatesC3 = [bool]$lbl.enablementGatesC3
    # A surface "attempting" the C3 output while publicly disabled must be refused regardless of
    # the table's own designed C3 literal (allowed/degraded/etc.) -- this is the hard-fail proof.
    return ($gatesC3 -and (-not $publiclyEnabled))
}

$fixtureLabel = 'ordinary'
$fixtureRefused = Test-FixtureRefused -LabelName $fixtureLabel -Contract $contract
Assert-True ($fixtureRefused) "enablement-hard-fail-fixture: a surface attempting a biostack-recommended ($fixtureLabel, C3) output while publiclyEnabled=false must be refused by this contract's own enablementGatesC3 structure; it was not."
# Negative control: if publiclyEnabled were (hypothetically) true, the same fixture would no
# longer be forced to refuse on enablement grounds alone -- proving the gate is the actual cause,
# not a vacuous always-true check.
$hypotheticalEnabled = $contract | ConvertTo-Json -Depth 20 | ConvertFrom-Json -Depth 20
$hypotheticalEnabled.enablementState.biostackRecommendedOrigination.publiclyEnabled = $true
$hypotheticalRefused = Test-FixtureRefused -LabelName $fixtureLabel -Contract $hypotheticalEnabled
Assert-True (-not $hypotheticalRefused) 'enablement-hard-fail-fixture: negative control failed -- the gate must stop forcing refusal once publiclyEnabled is hypothetically true, proving the real contract''s refusal is caused by publiclyEnabled=false, not a vacuous always-true check.'
Add-PassedCheck -Name 'enablement-hard-fail-fixture' -Detail "fixture: a C3 (biostack-recommended) output attempt on label '$fixtureLabel' is HARD-REFUSED while publiclyEnabled=false (and the refusal is proven caused by the flag via a negative control)"

# ---------------------------------------------------------------------------
# Check 17: numeric-provenance-fail-closed-fixture -- adapted structural proof that D-B3 rule 4
# (missing provenance -> refused, not downgraded) is the contract's own stated rule, verbatim,
# with no weaker alternate rule present anywhere in either artifact.
# ---------------------------------------------------------------------------

function Test-MissingProvenanceFixture {
    param([Parameter(Mandatory = $true)][object]$NumericProvenanceRules)
    $rule4 = [string]$NumericProvenanceRules[3]
    return $rule4.Contains('Missing provenance') -and $rule4.Contains('refused') -and $rule4.Contains('fail-closed') -and $rule4.Contains('not downgraded')
}
$rulesArray = @($contract.numericProvenance.rules)
Assert-True ($rulesArray.Count -eq 4) "numeric-provenance-fail-closed-fixture: numericProvenance.rules must carry exactly 4 rules, found $($rulesArray.Count)."
$fixturePass = Test-MissingProvenanceFixture -NumericProvenanceRules $rulesArray
Assert-True ($fixturePass) "numeric-provenance-fail-closed-fixture: rule 4 must state missing provenance is refused (fail-closed), not downgraded. Found: '$($rulesArray[3])'"
# No weaker alternate framing anywhere in either artifact (e.g. treating a missing-provenance
# numeric output as merely "degraded" instead of refused).
Assert-True ((-not $contractJsonText.Contains('missing provenance is degraded')) -and (-not $contractMdContent.Contains('missing provenance is degraded'))) 'numeric-provenance-fail-closed-fixture: a weaker, non-fail-closed framing of missing provenance was found.'
Add-PassedCheck -Name 'numeric-provenance-fail-closed-fixture' -Detail 'D-B3 rule 4 (missing provenance => fail-closed refusal, not downgrade) verified verbatim; no weaker alternate framing found in either artifact'

# ---------------------------------------------------------------------------
# Evidence bundle
# ---------------------------------------------------------------------------

$expectedEvidencePath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot 'artifacts/p0b-verification'))
$resolvedEvidencePath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $EvidenceDirectory))
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($resolvedEvidencePath.TrimEnd('\', '/'), $expectedEvidencePath.TrimEnd('\', '/'))) 'EvidenceDirectory must resolve to artifacts/p0b-verification under the repository root.'

$trackedEvidence = Get-GitResult -Arguments @('ls-files', '--error-unmatch', '--', 'artifacts/p0b-verification')
Assert-True ($trackedEvidence.ExitCode -ne 0) 'artifacts/p0b-verification must be untracked.'
[IO.Directory]::CreateDirectory($resolvedEvidencePath) | Out-Null

$changedFilesContent = if ($allChanged.Count -gt 0) { $allChanged -join "`n" } else { '(none)' }
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'changed-files.txt') -Content $changedFilesContent

$summary = [ordered]@{
    schema            = 'biostack.p0b-verification-summary.v1'
    pass              = $true
    baseCommit        = $BaseCommit.ToLowerInvariant()
    fencingBaseCommit = $fencingBaseCommit.ToLowerInvariant()
    headCommit        = $headCommit
    builderId         = $BuilderId
    reviewerIds       = [string[]]$ReviewerIds
    checks            = $CheckResults.ToArray()
}
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'verification-summary.json') -Content ($summary | ConvertTo-Json -Depth 12)

Write-Output ''
Write-Output "P0-B verification PASS ($($CheckResults.Count) checks)"
