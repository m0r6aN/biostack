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
# Constants: allowed surfaces, frozen surfaces, pinned contract values
# ---------------------------------------------------------------------------

[string[]]$AllowedSurfaces = @(
    'docs/specs/schemas/classification-axes.schema.json',
    'docs/specs/schemas/delivery-class-controls.json',
    'docs/specs/schemas/fold-engine.md',
    'docs/specs/schemas/routing-output.schema.json',
    'docs/specs/schemas/fixtures/positive-standard-single.json',
    'docs/specs/schemas/fixtures/positive-multilabel-health-privacy.json',
    'docs/specs/schemas/fixtures/positive-migration-affected.json',
    'docs/specs/schemas/fixtures/positive-migration-unaffected.json',
    'docs/specs/schemas/fixtures/positive-guidance-substance-passthrough.json',
    'docs/specs/schemas/fixtures/negative-missing-conditional-input.json',
    'docs/specs/schemas/fixtures/negative-unknown-delivery-label.json',
    'docs/specs/schemas/fixtures/negative-empty-delivery-axis.json',
    'docs/specs/schemas/fixtures/negative-synthetic-scalar-conflict.json',
    'docs/specs/schemas/AXIS-REGRESSION-MAP.md',
    'docs/specs/scripts/verify-p2.ps1',
    'docs/specs/README.md',
    'docs/specs/INDEX.md'
)

[string[]]$FixtureFiles = @(
    'positive-standard-single',
    'positive-multilabel-health-privacy',
    'positive-migration-affected',
    'positive-migration-unaffected',
    'positive-guidance-substance-passthrough',
    'negative-missing-conditional-input',
    'negative-unknown-delivery-label',
    'negative-empty-delivery-axis',
    'negative-synthetic-scalar-conflict'
)

[string[]]$StaticFrozenPaths = @(
    'docs/INITIATIVES/biostack-governed-delivery/CHARTER.md',
    'docs/INITIATIVES/biostack-governed-delivery/PLAN-REVIEW.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P1.md',
    'docs/INITIATIVES/biostack-governed-delivery/dispatch/P1-GATE2.md',
    'docs/INITIATIVES/biostack-governed-delivery/closures/P1.md',
    'docs/specs/CORE-CONTEXT.md',
    'docs/specs/active/README.md',
    'docs/specs/done/README.md',
    'AGENTS.md',
    'docs/INITIATIVES/biostack-production-readiness',
    'frontend',
    'backend',
    'contracts',
    '.github'
)

$DeliveryClassLabels = @(
    'standard', 'health-boundary', 'privacy', 'migration',
    'trust-path', 'provider-pilot', 'legal-policy', 'knowledge-promotion'
)
$GuidanceClassLabels = @(
    'deterministic-calculation', 'curated-evidence-guidance',
    'personalized-protocol-recommendation', 'safety-escalation'
)
$SubstanceFunctionRiskLabels = @(
    'ordinary', 'prescription-treatment-involved', 'investigational-or-unapproved',
    'gray-market-or-identity-uncertain', 'injection-or-sterile-preparation',
    'interaction-or-contraindication-signal', 'minor-or-age-uncertain',
    'pregnancy-or-lactation', 'acute-red-flag-or-emergency',
    'controlled-or-illegal-sourcing'
)

$PinnedReviewers = @{
    'standard'             = 1
    'health-boundary'      = 2
    'privacy'              = 2
    'trust-path'           = 2
    'provider-pilot'       = 2
    'legal-policy'         = 2
    'knowledge-promotion'  = 2
}
$PinnedMigrationReviewers = @{ default = 1; condition = 'userDataOrProductionSchemaAffected'; ifTrue = 2 }
$PinnedAdditionalMergeGate = @{
    'standard'            = $null
    'health-boundary'     = 'green-dual-review-required'
    'privacy'             = $null
    'migration'           = 'explicit-base-and-environment-scope'
    'trust-path'          = $null
    'provider-pilot'      = 'explicit-human-owner-acknowledgment'
    'legal-policy'        = 'recorded-human-approval'
    'knowledge-promotion' = $null
}

$CheckResults = New-Object 'System.Collections.Generic.List[object]'

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
        throw "git $($Arguments -join ' ') failed with exit $($result.ExitCode): $detail"
    }
    return $result.Output
}

function Sort-Ordinal {
    param([string[]]$Values)
    [string[]]$copy = @($Values)
    [Array]::Sort($copy, [StringComparer]::Ordinal)
    return $copy
}

function Assert-SequenceEqual {
    param([string[]]$Actual, [string[]]$Expected, [string]$Label)
    Assert-True ($Actual.Count -eq $Expected.Count) "$Label count mismatch. Expected $($Expected.Count), got $($Actual.Count). Actual: $($Actual -join ', ')"
    for ($index = 0; $index -lt $Expected.Count; $index++) {
        Assert-True ([StringComparer]::Ordinal.Equals($Actual[$index], $Expected[$index])) "$Label mismatch at index $index. Expected '$($Expected[$index])', got '$($Actual[$index])'."
    }
}

function Write-Utf8Lf {
    param([Parameter(Mandatory = $true)][string]$Path, [AllowEmptyString()][string]$Content)
    $normalized = $Content.Replace("`r`n", "`n").Replace("`r", "`n").TrimEnd("`n") + "`n"
    $encoding = New-Object Text.UTF8Encoding($false)
    [IO.File]::WriteAllText($Path, $normalized, $encoding)
}

function Add-PassedCheck {
    param([int]$Number, [string]$Name)
    $CheckResults.Add([pscustomobject]@{ number = $Number; name = $Name; pass = $true }) | Out-Null
}

function Assert-OnlyAuthorizedEvidenceStatus {
    param([string[]]$StatusLines)
    foreach ($line in $StatusLines) {
        Assert-True $line.StartsWith('?? artifacts/p2-verification/', [StringComparison]::Ordinal) "Unauthorized or tracked worktree status entry: $line"
    }
}

function Get-PropNames {
    param($Obj)
    if ($null -eq $Obj) { return @() }
    if ($Obj -is [System.Collections.IDictionary]) { return @($Obj.Keys) }
    return @($Obj.PSObject.Properties.Name)
}

function Get-PropValue {
    param($Obj, [string]$Name)
    if ($Obj -is [System.Collections.IDictionary]) { return $Obj[$Name] }
    return $Obj.$Name
}

function Test-DeepEqual {
    param($Left, $Right)

    if ($null -eq $Left -and $null -eq $Right) { return $true }
    if ($null -eq $Left -or $null -eq $Right) { return $false }

    $leftIsObj = ($Left -is [System.Collections.IDictionary]) -or ($Left -is [System.Management.Automation.PSCustomObject])
    $rightIsObj = ($Right -is [System.Collections.IDictionary]) -or ($Right -is [System.Management.Automation.PSCustomObject])
    if ($leftIsObj -and $rightIsObj) {
        $lk = Sort-Ordinal -Values @(Get-PropNames -Obj $Left)
        $rk = Sort-Ordinal -Values @(Get-PropNames -Obj $Right)
        if ($lk.Count -ne $rk.Count) { return $false }
        for ($i = 0; $i -lt $lk.Count; $i++) { if (-not [StringComparer]::Ordinal.Equals($lk[$i], $rk[$i])) { return $false } }
        foreach ($k in $lk) {
            if (-not (Test-DeepEqual -Left (Get-PropValue -Obj $Left -Name $k) -Right (Get-PropValue -Obj $Right -Name $k))) { return $false }
        }
        return $true
    }

    $leftIsArr = ($Left -is [System.Collections.IEnumerable]) -and -not ($Left -is [string])
    $rightIsArr = ($Right -is [System.Collections.IEnumerable]) -and -not ($Right -is [string])
    if ($leftIsArr -and $rightIsArr) {
        $la = @($Left)
        $ra = @($Right)
        if ($la.Count -ne $ra.Count) { return $false }
        for ($i = 0; $i -lt $la.Count; $i++) { if (-not (Test-DeepEqual -Left $la[$i] -Right $ra[$i])) { return $false } }
        return $true
    }

    if ($Left -is [bool] -or $Right -is [bool]) { return ([bool]$Left) -eq ([bool]$Right) }
    if (($Left -is [int]) -or ($Left -is [long]) -or ($Left -is [double]) -or ($Right -is [int]) -or ($Right -is [long]) -or ($Right -is [double])) {
        return ([double]$Left) -eq ([double]$Right)
    }
    return [string]$Left -eq [string]$Right
}

# ---------------------------------------------------------------------------
# Reference fold implementation (Algorithm section of fold-engine.md)
# ---------------------------------------------------------------------------

function Invoke-Fold {
    param($FixtureInput, $AxesDoc, $ControlsDoc)

    $deliveryLabels = @($AxesDoc.axes.deliveryClass.labels)
    $guidanceLabels = @($AxesDoc.axes.productGuidanceClass.labels)
    $substanceLabels = @($AxesDoc.axes.substanceFunctionRisk.labels)

    $deliveryClasses = @($FixtureInput.deliveryClasses)
    $guidanceClasses = @($FixtureInput.guidanceClasses)
    $substanceRisk = @($FixtureInput.substanceFunctionRisk)
    $conditionalInputs = $FixtureInput.conditionalInputs

    # (1) validate every declared label against its axis's closed vocabulary
    foreach ($l in $deliveryClasses) {
        if ($deliveryLabels -notcontains $l) {
            return @{ schema = 'biostack.p2-routing-output.v1'; result = 'stopped'; stopReason = @{ reason = 'unknown-label'; axis = 'deliveryClass'; label = $l } }
        }
    }
    foreach ($l in $guidanceClasses) {
        if ($guidanceLabels -notcontains $l) {
            return @{ schema = 'biostack.p2-routing-output.v1'; result = 'stopped'; stopReason = @{ reason = 'unknown-label'; axis = 'productGuidanceClass'; label = $l } }
        }
    }
    foreach ($l in $substanceRisk) {
        if ($substanceLabels -notcontains $l) {
            return @{ schema = 'biostack.p2-routing-output.v1'; result = 'stopped'; stopReason = @{ reason = 'unknown-label'; axis = 'substanceFunctionRisk'; label = $l } }
        }
    }

    # (2) reject with empty-required-axis if deliveryClass has zero declared labels
    if ($deliveryClasses.Count -eq 0) {
        return @{ schema = 'biostack.p2-routing-output.v1'; result = 'stopped'; stopReason = @{ reason = 'empty-required-axis'; axis = 'deliveryClass' } }
    }

    # (3) scalar-exact-match-required: only the fixture-only synthetic field exercises this
    if ($FixtureInput.fixtureOnly -eq $true -and (Get-PropNames -Obj $FixtureInput) -contains 'testOnlyScalarValues') {
        [object[]]$vals = @()
        foreach ($c in $deliveryClasses) {
            $v = Get-PropValue -Obj $FixtureInput.testOnlyScalarValues -Name $c
            if ($null -ne $v) { $vals += $v }
        }
        $distinct = [object[]]($vals | Select-Object -Unique)
        if ($distinct.Count -gt 1) {
            return @{ schema = 'biostack.p2-routing-output.v1'; result = 'stopped'; stopReason = @{ reason = 'incompatible-controls'; field = 'testOnlyScalarField'; conflictingValues = $vals } }
        }
    }

    # (4) resolve max-scalar fields (reviewers), stopping on missing conditional input
    [int[]]$reviewerValues = @()
    foreach ($c in $deliveryClasses) {
        $ctrl = Get-PropValue -Obj $ControlsDoc -Name $c
        $r = $ctrl.reviewers
        if ($r -is [int] -or $r -is [long] -or $r -is [double]) {
            $reviewerValues += [int]$r
        }
        else {
            $condName = [string]$r.condition
            $hasCond = (Get-PropNames -Obj $conditionalInputs) -contains $condName
            if (-not $hasCond) {
                return @{ schema = 'biostack.p2-routing-output.v1'; result = 'stopped'; stopReason = @{ reason = 'missing-required-field'; deliveryClass = $c; field = $condName } }
            }
            $condVal = Get-PropValue -Obj $conditionalInputs -Name $condName
            if ($condVal -eq $true) { $reviewerValues += [int]$r.ifTrue } else { $reviewerValues += [int]$r.default }
        }
    }
    $reviewerCount = ($reviewerValues | Measure-Object -Maximum).Maximum

    # (5) union the union-set fields
    [string[]]$requiredSpecAdditions = @()
    [string[]]$requiredChecks = @()
    [string[]]$mandatoryStopConditions = @()
    [string[]]$requiredClosureEvidence = @()
    [string[]]$mergeGates = @()
    foreach ($c in $deliveryClasses) {
        $ctrl = Get-PropValue -Obj $ControlsDoc -Name $c
        foreach ($v in [string[]]@($ctrl.requiredSpecAdditions)) { if ($requiredSpecAdditions -notcontains $v) { $requiredSpecAdditions += $v } }
        foreach ($v in [string[]]@($ctrl.minimumChecks)) { if ($requiredChecks -notcontains $v) { $requiredChecks += $v } }
        foreach ($v in [string[]]@($ctrl.mandatoryStopConditions)) { if ($mandatoryStopConditions -notcontains $v) { $mandatoryStopConditions += $v } }
        foreach ($v in [string[]]@($ctrl.requiredClosureEvidence)) { if ($requiredClosureEvidence -notcontains $v) { $requiredClosureEvidence += $v } }
        if ($null -ne $ctrl.additionalMergeGate) {
            if ($mergeGates -notcontains $ctrl.additionalMergeGate) { $mergeGates += [string]$ctrl.additionalMergeGate }
        }
    }

    # (6) intersect intersection-boolean fields
    $dispatchAll = $true
    $mergeAll = $true
    foreach ($c in $deliveryClasses) {
        $ctrl = Get-PropValue -Obj $ControlsDoc -Name $c
        if ([string]$ctrl.dispatchEligibility -ne 'eligible-when-named') { $dispatchAll = $false }
        if ([string]$ctrl.mergeEligibility -ne 'eligible-when-named') { $mergeAll = $false }
    }
    $standingAuthorizationEligible = $dispatchAll -and $mergeAll

    # (7)/(8) pass-through and emit
    return @{
        schema                         = 'biostack.p2-routing-output.v1'
        result                         = 'routed'
        requiredSpecAdditions          = @($requiredSpecAdditions)
        requiredChecks                 = @($requiredChecks)
        mandatoryStopConditions        = @($mandatoryStopConditions)
        requiredClosureEvidence        = @($requiredClosureEvidence)
        mergeGates                     = @($mergeGates)
        reviewerCount                  = $reviewerCount
        standingAuthorizationEligible  = $standingAuthorizationEligible
        recordedGuidanceClasses        = @($guidanceClasses)
        recordedSubstanceFunctionRisk  = @($substanceRisk)
    }
}

# ---------------------------------------------------------------------------
# Setup
# ---------------------------------------------------------------------------

if ($ReviewerIds.Count -eq 1 -and $ReviewerIds[0].Contains(',')) {
    $ReviewerIds = $ReviewerIds[0].Split(',') | ForEach-Object { $_.Trim() }
}
Assert-True ($ReviewerIds.Count -eq 2) 'ReviewerIds must contain exactly two identities.'
Assert-True (-not [string]::IsNullOrWhiteSpace($ReviewerIds[0])) 'ReviewerIds[0] is empty.'
Assert-True (-not [string]::IsNullOrWhiteSpace($ReviewerIds[1])) 'ReviewerIds[1] is empty.'
Assert-True (-not [StringComparer]::Ordinal.Equals($ReviewerIds[0], $ReviewerIds[1])) 'ReviewerIds must be distinct.'

$rootResult = @(Invoke-Git -Arguments @('rev-parse', '--show-toplevel'))
Assert-True ($rootResult.Count -eq 1) 'Unable to resolve one repository root.'
$RepositoryRoot = [IO.Path]::GetFullPath($rootResult[0].Trim())
Set-Location -LiteralPath $RepositoryRoot

# Check 1
$ancestor = Get-GitResult -Arguments @('merge-base', '--is-ancestor', $BaseCommit, 'HEAD')
Assert-True ($ancestor.ExitCode -eq 0) 'HEAD does not descend from BaseCommit.'
Add-PassedCheck -Number 1 -Name 'HEAD descends from BaseCommit'

# Check 2
$diffCheck = Get-GitResult -Arguments @('diff', '--check', "$BaseCommit...HEAD")
Assert-True ($diffCheck.ExitCode -eq 0) "git diff --check failed: $($diffCheck.Output -join "`n")"
Add-PassedCheck -Number 2 -Name 'base-to-head diff check'

# Check 3
[string[]]$ExpectedChanges = Sort-Ordinal -Values $AllowedSurfaces
[string[]]$ActualChanges = Sort-Ordinal -Values (Invoke-Git -Arguments @('diff', '--name-only', "$BaseCommit...HEAD", '--'))
Assert-SequenceEqual -Actual $ActualChanges -Expected $ExpectedChanges -Label 'BaseCommit...HEAD changed files'
Add-PassedCheck -Number 3 -Name 'exact changed-file set equals the 17 allowed surfaces'

# Check 4
[string[]]$RegressionSpecPaths = @(Invoke-Git -Arguments @('ls-files', 'docs/specs/active', 'docs/specs/done')) |
    Where-Object { $_ -ne 'docs/specs/active/README.md' -and $_ -ne 'docs/specs/done/README.md' }
[string[]]$AllFrozenPaths = @($StaticFrozenPaths + $RegressionSpecPaths)
foreach ($path in $AllFrozenPaths) {
    $quiet = Get-GitResult -Arguments @('diff', '--quiet', "$BaseCommit...HEAD", '--', $path)
    Assert-True ($quiet.ExitCode -eq 0) "Frozen path changed: $path"
}
Add-PassedCheck -Number 4 -Name 'frozen surfaces byte-identical to BaseCommit'

# Check 5: classification-axes.schema.json
$AxesPath = Join-Path $RepositoryRoot 'docs/specs/schemas/classification-axes.schema.json'
$AxesRaw = [IO.File]::ReadAllText($AxesPath)
$AxesDoc = $AxesRaw | ConvertFrom-Json

[string[]]$axesTopKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $AxesDoc)
Assert-SequenceEqual -Actual $axesTopKeys -Expected (Sort-Ordinal -Values @('schema', 'axes')) -Label 'classification-axes.schema.json top-level keys'
Assert-True ([StringComparer]::Ordinal.Equals([string]$AxesDoc.schema, 'biostack.risk-taxonomy.v1')) 'classification-axes.schema.json schema literal mismatch.'

[string[]]$axesKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $AxesDoc.axes)
Assert-SequenceEqual -Actual $axesKeys -Expected (Sort-Ordinal -Values @('deliveryClass', 'productGuidanceClass', 'substanceFunctionRisk')) -Label 'axes keys'

Assert-SequenceEqual -Actual @($AxesDoc.axes.deliveryClass.labels) -Expected $DeliveryClassLabels -Label 'deliveryClass.labels order'
Assert-SequenceEqual -Actual @($AxesDoc.axes.productGuidanceClass.labels) -Expected $GuidanceClassLabels -Label 'productGuidanceClass.labels order'
Assert-SequenceEqual -Actual @($AxesDoc.axes.substanceFunctionRisk.labels) -Expected $SubstanceFunctionRiskLabels -Label 'substanceFunctionRisk.labels order'

Assert-True ($AxesDoc.axes.deliveryClass.multiLabel -eq $true) 'deliveryClass.multiLabel must be true.'
Assert-True ($AxesDoc.axes.productGuidanceClass.multiLabel -eq $true) 'productGuidanceClass.multiLabel must be true.'
Assert-True ($AxesDoc.axes.substanceFunctionRisk.multiLabel -eq $true) 'substanceFunctionRisk.multiLabel must be true.'

Assert-True ([StringComparer]::Ordinal.Equals([string]$AxesDoc.axes.productGuidanceClass.controlBindingStatus, 'deferred-to-P3-B')) 'productGuidanceClass.controlBindingStatus mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$AxesDoc.axes.substanceFunctionRisk.controlBindingStatus, 'deferred-to-P0-B')) 'substanceFunctionRisk.controlBindingStatus mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$AxesDoc.axes.substanceFunctionRisk.allowedOutputBindingStatus, 'out-of-scope-for-P2')) 'substanceFunctionRisk.allowedOutputBindingStatus mismatch.'

Assert-True ([StringComparer]::Ordinal.Equals([string]$AxesDoc.axes.deliveryClass.controlSource, 'delivery-class-controls.json')) 'deliveryClass.controlSource mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$AxesDoc.axes.productGuidanceClass.controlSource, 'none')) 'productGuidanceClass.controlSource mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$AxesDoc.axes.substanceFunctionRisk.controlSource, 'none')) 'substanceFunctionRisk.controlSource mismatch.'

Assert-True ([StringComparer]::Ordinal.Equals([string]$AxesDoc.axes.deliveryClass.definitionOwner, 'charter-governance-overlay-table')) 'deliveryClass.definitionOwner mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$AxesDoc.axes.productGuidanceClass.definitionOwner, 'charter-product-guidance-classes')) 'productGuidanceClass.definitionOwner mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$AxesDoc.axes.substanceFunctionRisk.definitionOwner, 'charter-substance-function-vocabulary')) 'substanceFunctionRisk.definitionOwner mismatch.'

$inlinedControlKeyPattern = '"(requiredSpecAdditions|minimumChecks|mandatoryStopConditions|requiredClosureEvidence)"\s*:'
Assert-True (-not [regex]::IsMatch($AxesRaw, $inlinedControlKeyPattern)) 'classification-axes.schema.json inlines delivery-class control content (AC-P2-06).'

function Assert-ApplicabilityFields {
    param($AxisObj, [string[]]$Labels, [string]$ExpectedFieldName, [string]$ExpectedAuthority, [string]$ExpectedStatus, [string]$AxisName)
    $afKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $AxisObj.applicabilityField)
    Assert-SequenceEqual -Actual $afKeys -Expected (Sort-Ordinal -Values $Labels) -Label "$AxisName applicabilityField label set"
    foreach ($label in $Labels) {
        $entry = Get-PropValue -Obj $AxisObj.applicabilityField -Name $label
        $entryKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $entry)
        Assert-SequenceEqual -Actual $entryKeys -Expected (Sort-Ordinal -Values @('fieldName', 'determinationAuthority', 'status')) -Label "$AxisName.$label applicabilityField keys"
        Assert-True ([StringComparer]::Ordinal.Equals([string]$entry.fieldName, $ExpectedFieldName)) "$AxisName.$label fieldName mismatch."
        Assert-True ([StringComparer]::Ordinal.Equals([string]$entry.determinationAuthority, $ExpectedAuthority)) "$AxisName.$label determinationAuthority mismatch."
        Assert-True ([StringComparer]::Ordinal.Equals([string]$entry.status, $ExpectedStatus)) "$AxisName.$label status mismatch."
    }
}
Assert-ApplicabilityFields -AxisObj $AxesDoc.axes.deliveryClass -Labels $DeliveryClassLabels -ExpectedFieldName 'deliveryClasses' -ExpectedAuthority 'spec-author-at-shaping-time' -ExpectedStatus 'defined' -AxisName 'deliveryClass'
Assert-ApplicabilityFields -AxisObj $AxesDoc.axes.productGuidanceClass -Labels $GuidanceClassLabels -ExpectedFieldName 'guidanceClasses' -ExpectedAuthority 'spec-author-at-shaping-time' -ExpectedStatus 'defined' -AxisName 'productGuidanceClass'
Assert-ApplicabilityFields -AxisObj $AxesDoc.axes.substanceFunctionRisk -Labels $SubstanceFunctionRiskLabels -ExpectedFieldName 'substanceFunctionRisk' -ExpectedAuthority 'deferred-to-P0-B-deterministic-criteria' -ExpectedStatus 'deferred' -AxisName 'substanceFunctionRisk'

$axisSchemaCheck = [ordered]@{
    schema = 'biostack.p2-axis-schema-check.v1'
    pass = $true
    threeAxes = $true
    closedVocabulariesExact = $true
    multiLabelAllTrue = $true
    noInlinedControlKeys = $true
    allowedOutputBindingStatus = [string]$AxesDoc.axes.substanceFunctionRisk.allowedOutputBindingStatus
    controlBindingStatuses = @{
        productGuidanceClass = [string]$AxesDoc.axes.productGuidanceClass.controlBindingStatus
        substanceFunctionRisk = [string]$AxesDoc.axes.substanceFunctionRisk.controlBindingStatus
    }
}
Add-PassedCheck -Number 5 -Name 'classification-axes.schema.json correctness (AC-P2-01/05/06)'

# Check 6: delivery-class-controls.json
$ControlsPath = Join-Path $RepositoryRoot 'docs/specs/schemas/delivery-class-controls.json'
$ControlsDoc = [IO.File]::ReadAllText($ControlsPath) | ConvertFrom-Json

[string[]]$controlsTopKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $ControlsDoc)
Assert-SequenceEqual -Actual $controlsTopKeys -Expected (Sort-Ordinal -Values $DeliveryClassLabels) -Label 'delivery-class-controls.json top-level keys'

[string[]]$RequiredControlKeys = Sort-Ordinal -Values @(
    'requiredSpecAdditions', 'minimumChecks', 'reviewers', 'dispatchEligibility',
    'mergeEligibility', 'additionalMergeGate', 'mandatoryStopConditions', 'requiredClosureEvidence'
)
foreach ($class in $DeliveryClassLabels) {
    $entry = Get-PropValue -Obj $ControlsDoc -Name $class
    $entryKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $entry)
    Assert-SequenceEqual -Actual $entryKeys -Expected $RequiredControlKeys -Label "$class control keys"

    Assert-True (@($entry.requiredSpecAdditions).Count -gt 0) "$class requiredSpecAdditions must be non-empty."
    Assert-True (@($entry.minimumChecks).Count -gt 0) "$class minimumChecks must be non-empty."
    Assert-True (@($entry.mandatoryStopConditions).Count -gt 0) "$class mandatoryStopConditions must be non-empty."
    Assert-True (@($entry.requiredClosureEvidence).Count -gt 0) "$class requiredClosureEvidence must be non-empty."

    Assert-True ([StringComparer]::Ordinal.Equals([string]$entry.dispatchEligibility, 'eligible-when-named')) "$class dispatchEligibility mismatch."
    Assert-True ([StringComparer]::Ordinal.Equals([string]$entry.mergeEligibility, 'eligible-when-named')) "$class mergeEligibility mismatch."

    if ($class -eq 'migration') {
        $r = $entry.reviewers
        $rKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $r)
        Assert-SequenceEqual -Actual $rKeys -Expected (Sort-Ordinal -Values @('default', 'condition', 'ifTrue')) -Label 'migration.reviewers keys'
        Assert-True ([int]$r.default -eq 1) 'migration.reviewers.default must be 1.'
        Assert-True ([StringComparer]::Ordinal.Equals([string]$r.condition, 'userDataOrProductionSchemaAffected')) 'migration.reviewers.condition literal mismatch.'
        Assert-True ([int]$r.ifTrue -eq 2) 'migration.reviewers.ifTrue must be 2.'
    } else {
        Assert-True ([int]$entry.reviewers -eq [int]$PinnedReviewers[$class]) "$class reviewers mismatch."
    }

    $expectedGate = $PinnedAdditionalMergeGate[$class]
    if ($null -eq $expectedGate) {
        Assert-True ($null -eq $entry.additionalMergeGate) "$class additionalMergeGate must be null."
    } else {
        Assert-True ([StringComparer]::Ordinal.Equals([string]$entry.additionalMergeGate, $expectedGate)) "$class additionalMergeGate mismatch."
    }
}
Add-PassedCheck -Number 6 -Name 'delivery-class-controls.json fidelity (AC-P2-02)'

# Check 7: fold-engine.md
$FoldEnginePath = Join-Path $RepositoryRoot 'docs/specs/schemas/fold-engine.md'
$FoldEngineText = [IO.File]::ReadAllText($FoldEnginePath)
[string[]]$RequiredHeadings = @(
    '## Field-type taxonomy', '## Algorithm', '## Stop on incompatible controls',
    '## Guidance-class and substance/function-risk pass-through'
)
foreach ($heading in $RequiredHeadings) {
    Assert-True ($FoldEngineText.Contains($heading)) "fold-engine.md missing required heading: $heading"
}
[string[]]$RequiredSentences = @(
    'Recording a product-guidance-class or substance/function-risk label adds no required sections, checks, reviewer weight, standing-authorization change, or stop condition under P2.',
    "A spec receives only the controls folded from the delivery-class labels it actually declares, never every delivery-class label's content by default."
)
foreach ($sentence in $RequiredSentences) {
    Assert-True ($FoldEngineText.Contains($sentence)) "fold-engine.md missing required verbatim sentence: $sentence"
}
Add-PassedCheck -Number 7 -Name 'fold-engine.md headings and pinned sentences (AC-P2-03)'

# Check 8: routing-output.schema.json
$RoutingSchemaPath = Join-Path $RepositoryRoot 'docs/specs/schemas/routing-output.schema.json'
$RoutingSchemaDoc = [IO.File]::ReadAllText($RoutingSchemaPath) | ConvertFrom-Json
[string[]]$RoutingOutputKeys = Sort-Ordinal -Values @(
    'schema', 'result', 'requiredSpecAdditions', 'requiredChecks', 'mandatoryStopConditions',
    'requiredClosureEvidence', 'mergeGates', 'reviewerCount', 'standingAuthorizationEligible',
    'recordedGuidanceClasses', 'recordedSubstanceFunctionRisk', 'stopReason'
)
[string[]]$ActualRoutingOutputKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $RoutingSchemaDoc.properties)
Assert-SequenceEqual -Actual $ActualRoutingOutputKeys -Expected $RoutingOutputKeys -Label 'routing-output.schema.json top-level keys'
Add-PassedCheck -Number 8 -Name 'routing-output.schema.json exact key set'

# Checks 9 and 10: fixtures
$FixtureResults = New-Object 'System.Collections.Generic.List[object]'
$FixturesDir = Join-Path $RepositoryRoot 'docs/specs/schemas/fixtures'
foreach ($fixtureName in $FixtureFiles) {
    $fixturePath = Join-Path $FixturesDir "$fixtureName.json"
    $fixtureDoc = [IO.File]::ReadAllText($fixturePath) | ConvertFrom-Json
    $fixtureKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $fixtureDoc)
    Assert-SequenceEqual -Actual $fixtureKeys -Expected (Sort-Ordinal -Values @('input', 'expected')) -Label "$fixtureName top-level keys"

    try {
        $actual = Invoke-Fold -FixtureInput $fixtureDoc.input -AxesDoc $AxesDoc -ControlsDoc $ControlsDoc
    } catch {
        throw "Invoke-Fold threw for fixture $fixtureName : $($_.Exception.Message)"
    }
    $deepEqual = Test-DeepEqual -Left $actual -Right $fixtureDoc.expected
    Assert-True $deepEqual "Fixture mismatch: $fixtureName. Actual: $($actual | ConvertTo-Json -Depth 10 -Compress) Expected: $($fixtureDoc.expected | ConvertTo-Json -Depth 10 -Compress)"

    # Check 10: per-fixture specific assertions from the fixtures table
    switch ($fixtureName) {
        'positive-standard-single' {
            Assert-True ([string]$fixtureDoc.expected.result -eq 'routed') "$fixtureName must be routed."
            Assert-True ([int]$fixtureDoc.expected.reviewerCount -eq 1) "$fixtureName reviewerCount must be 1."
            Assert-True (@($fixtureDoc.expected.mergeGates).Count -eq 0) "$fixtureName mergeGates must be empty."
        }
        'positive-multilabel-health-privacy' {
            Assert-True ([string]$fixtureDoc.expected.result -eq 'routed') "$fixtureName must be routed."
            Assert-True ([int]$fixtureDoc.expected.reviewerCount -eq 2) "$fixtureName reviewerCount must be 2."
            Assert-True ((@($fixtureDoc.expected.mergeGates) -join ',') -eq 'green-dual-review-required') "$fixtureName mergeGates mismatch."
        }
        'positive-migration-affected' {
            Assert-True ([int]$fixtureDoc.expected.reviewerCount -eq 2) "$fixtureName reviewerCount must be 2."
        }
        'positive-migration-unaffected' {
            Assert-True ([int]$fixtureDoc.expected.reviewerCount -eq 1) "$fixtureName reviewerCount must be 1."
        }
        'positive-guidance-substance-passthrough' {
            Assert-True ([int]$fixtureDoc.expected.reviewerCount -eq 1) "$fixtureName reviewerCount must be 1 (unaffected by guidance/substance labels)."
            Assert-True (Test-DeepEqual -Left @($fixtureDoc.expected.recordedGuidanceClasses) -Right @($fixtureDoc.input.guidanceClasses)) "$fixtureName recordedGuidanceClasses must equal declared input verbatim."
            Assert-True (Test-DeepEqual -Left @($fixtureDoc.expected.recordedSubstanceFunctionRisk) -Right @($fixtureDoc.input.substanceFunctionRisk)) "$fixtureName recordedSubstanceFunctionRisk must equal declared input verbatim."
        }
        'negative-missing-conditional-input' {
            Assert-True ([string]$fixtureDoc.expected.result -eq 'stopped') "$fixtureName must be stopped."
            Assert-True ([string]$fixtureDoc.expected.stopReason.reason -eq 'missing-required-field') "$fixtureName stopReason.reason mismatch."
            Assert-True ([string]$fixtureDoc.expected.stopReason.field -eq 'userDataOrProductionSchemaAffected') "$fixtureName stopReason detail must name userDataOrProductionSchemaAffected."
        }
        'negative-unknown-delivery-label' {
            Assert-True ([string]$fixtureDoc.expected.result -eq 'stopped') "$fixtureName must be stopped."
            Assert-True ([string]$fixtureDoc.expected.stopReason.reason -eq 'unknown-label') "$fixtureName stopReason.reason mismatch."
            Assert-True ([string]$fixtureDoc.expected.stopReason.label -eq 'elevated') "$fixtureName stopReason detail must name 'elevated'."
            Assert-True ([string]$fixtureDoc.expected.stopReason.axis -eq 'deliveryClass') "$fixtureName stopReason detail must name axis 'deliveryClass'."
        }
        'negative-empty-delivery-axis' {
            Assert-True ([string]$fixtureDoc.expected.result -eq 'stopped') "$fixtureName must be stopped."
            Assert-True ([string]$fixtureDoc.expected.stopReason.reason -eq 'empty-required-axis') "$fixtureName stopReason.reason mismatch."
        }
        'negative-synthetic-scalar-conflict' {
            Assert-True ([string]$fixtureDoc.expected.result -eq 'stopped') "$fixtureName must be stopped."
            Assert-True ([string]$fixtureDoc.expected.stopReason.reason -eq 'incompatible-controls') "$fixtureName stopReason.reason mismatch."
            Assert-True ([string]$fixtureDoc.expected.stopReason.field -eq 'testOnlyScalarField') "$fixtureName stopReason detail must name testOnlyScalarField."
        }
    }

    # Independent recomputation of standingAuthorizationEligible for routed fixtures (check 10)
    if ([string]$fixtureDoc.expected.result -eq 'routed') {
        $dispatchAllIndep = $true
        $mergeAllIndep = $true
        foreach ($c in @($fixtureDoc.input.deliveryClasses)) {
            $ctrl = Get-PropValue -Obj $ControlsDoc -Name $c
            if ([string]$ctrl.dispatchEligibility -ne 'eligible-when-named') { $dispatchAllIndep = $false }
            if ([string]$ctrl.mergeEligibility -ne 'eligible-when-named') { $mergeAllIndep = $false }
        }
        $expectedStandingIndep = $dispatchAllIndep -and $mergeAllIndep
        Assert-True (([bool]$fixtureDoc.expected.standingAuthorizationEligible) -eq $expectedStandingIndep) "$fixtureName standingAuthorizationEligible independent recomputation mismatch."
    }

    $FixtureResults.Add([ordered]@{ fixture = $fixtureName; pass = $true }) | Out-Null
}
Add-PassedCheck -Number 9 -Name 'all nine fixtures reproduced exactly by the embedded reference fold (AC-P2-04)'
Add-PassedCheck -Number 10 -Name 'per-fixture table assertions and independent standingAuthorizationEligible recomputation'

# Check 11: AXIS-REGRESSION-MAP.md
$RegressionMapPath = Join-Path $RepositoryRoot 'docs/specs/schemas/AXIS-REGRESSION-MAP.md'
$RegressionMapText = [IO.File]::ReadAllText($RegressionMapPath)
$RegressionMapLines = $RegressionMapText -split "`r?`n"
$RequiredHeader = '| Spec file | Existing ad hoc fields found | Mapped delivery class(es) | Mapped guidance class(es) | Mapped substance/function risk | Disposition |'
$headerLine = $RegressionMapLines | Where-Object { $_.Trim() -eq $RequiredHeader } | Select-Object -First 1
Assert-True ($null -ne $headerLine) 'AXIS-REGRESSION-MAP.md missing exact required header row.'

$dataRows = @($RegressionMapLines | Where-Object { $_.StartsWith('| docs/specs/') })
$expectedRowCount = $RegressionSpecPaths.Count
Assert-True ($dataRows.Count -eq $expectedRowCount) "AXIS-REGRESSION-MAP.md row count mismatch. Expected $expectedRowCount, got $($dataRows.Count)."

[string[]]$AllowedDispositions = @('conforms', 'needs-reconciliation', 'no-axis-fields-present')
$localCiteFound = $false
foreach ($row in $dataRows) {
    $cells = $row.Trim().Trim('|').Split('|')
    $disposition = $cells[-1].Trim()
    Assert-True ($AllowedDispositions -contains $disposition) "AXIS-REGRESSION-MAP.md row has non-closed Disposition: $disposition"
    if (($row -match 'BIO-LOCAL-003' -or $row -match 'BIO-LOCAL-005') -and $disposition -eq 'needs-reconciliation') {
        $localCiteFound = $true
    }
}
Assert-True $localCiteFound 'AXIS-REGRESSION-MAP.md must contain at least one needs-reconciliation row citing BIO-LOCAL-003 or BIO-LOCAL-005.'
Add-PassedCheck -Number 11 -Name 'AXIS-REGRESSION-MAP.md completeness (AC-P2-07)'

# Check 12: INDEX.md / README.md bounded amendments
$indexDiffNameStatus = @(Invoke-Git -Arguments @('diff', '--name-status', "$BaseCommit...HEAD", '--', 'docs/specs/INDEX.md'))
$indexUnifiedDiff = @(Invoke-Git -Arguments @('diff', "$BaseCommit...HEAD", '--', 'docs/specs/INDEX.md'))
$addedIndexLines = @($indexUnifiedDiff | Where-Object { $_.StartsWith('+') -and -not $_.StartsWith('+++') })
$removedIndexLines = @($indexUnifiedDiff | Where-Object { $_.StartsWith('-') -and -not $_.StartsWith('---') })
$addedP2Rows = @($addedIndexLines | Where-Object { $_ -match '^\+\| P2 \|' })
Assert-True ($addedP2Rows.Count -eq 1) 'INDEX.md diff must add exactly one row matching ^\| P2 \|.'
Assert-True ($removedIndexLines.Count -eq 0) 'INDEX.md diff must remove zero lines.'

$indexHeadText = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'docs/specs/INDEX.md'))
$p2RowMatch = [regex]::Match($indexHeadText, '(?m)^\| P2 \|.*$')
Assert-True $p2RowMatch.Success 'INDEX.md at HEAD must contain the P2 row.'
$p2Cells = $p2RowMatch.Value.Trim().Trim('|').Split('|') | ForEach-Object { $_.Trim() }
Assert-True ($p2Cells.Count -eq 10) 'INDEX.md P2 row must have exactly ten cells.'
Assert-True ([StringComparer]::Ordinal.Equals($p2Cells[0], 'P2')) 'INDEX.md P2 row Parcel cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p2Cells[1], 'review-candidate')) 'INDEX.md P2 row Status cell mismatch.'
Assert-True ($p2Cells[2] -match '^\[P2[^\]]*\]\(.+\)$') 'INDEX.md P2 row Spec cell must be a link to this file.'
Assert-True ($p2Cells[3] -match '^\[[^\]]*charter[^\]]*\]\(.+\)$') 'INDEX.md P2 row Goal Charter cell must be a link target naming the charter.'
Assert-True ([StringComparer]::Ordinal.Equals($p2Cells[4], 'architecture; risk-sensitive')) 'INDEX.md P2 row Delivery classes cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p2Cells[5], 'not-applicable')) 'INDEX.md P2 row Guidance classes cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p2Cells[6], 'coordinator-assigns-at-gate-2')) 'INDEX.md P2 row Branch/worktree cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p2Cells[7], 'coordinator-assigns-at-gate-2')) 'INDEX.md P2 row Owner cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p2Cells[8], '2 independent reviewers')) 'INDEX.md P2 row Review requirement cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p2Cells[9], 'not-yet-closed')) 'INDEX.md P2 row Closure cell mismatch.'

$readmeUnifiedDiff = @(Invoke-Git -Arguments @('diff', "$BaseCommit...HEAD", '--', 'docs/specs/README.md'))
$removedReadmeLines = @($readmeUnifiedDiff | Where-Object { $_.StartsWith('-') -and -not $_.StartsWith('---') })
Assert-True ($removedReadmeLines.Count -eq 0) 'README.md diff must remove zero lines.'
$readmeHeadText = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'docs/specs/README.md'))
Assert-True ($readmeHeadText.Contains('## Classification-axis schema and routing (P2)')) 'README.md missing required P2 section heading.'
foreach ($link in @('classification-axes.schema.json', 'delivery-class-controls.json', 'fold-engine.md', 'routing-output.schema.json')) {
    Assert-True ($readmeHeadText.Contains($link)) "README.md P2 section missing link to $link."
}
Assert-True ($readmeHeadText.Contains('P2 defines composition mechanics for all three D14 axes without binding substance/function labels to product allowed outputs.')) 'README.md missing required P2 scope sentence.'
Add-PassedCheck -Number 12 -Name 'bounded INDEX.md/README.md amendments (AC-P2-09) with pinned cell/content values'

# Check 13: placeholder scan.
# Built from fragments so this scanner's own source text never contains the
# literal forbidden words it searches for (the alternatives are assembled at
# runtime, not embedded as contiguous literals in this file).
$placeholderWordTbd = -join @('T', 'B', 'D')
$placeholderWordTodo = -join @('T', 'O', 'D', 'O')
$placeholderWordFixme = -join @('F', 'I', 'X', 'M', 'E')
$placeholderAlternation = @($placeholderWordTbd, $placeholderWordTodo, $placeholderWordFixme) -join '|'
$lineLeadingPattern = "(?im)(^\s*($placeholderAlternation)\s*[:|\-])|(\{\{[^}]+\}\})"
$wordBoundaryPattern = "(?i)\b($placeholderAlternation)\b"

# README.md and INDEX.md are modified (not new) and carry pre-existing,
# out-of-scope placeholder cells on unrelated rows (document contract 8);
# only lines P2 actually adds are scanned for those two files. Every other
# allowed surface is newly created by P2, so its full content is scanned.
[string[]]$ModifiedSurfaces = @('docs/specs/README.md', 'docs/specs/INDEX.md')
foreach ($surface in $AllowedSurfaces) {
    if ($ModifiedSurfaces -contains $surface) {
        $unifiedDiff = @(Invoke-Git -Arguments @('diff', "$BaseCommit...HEAD", '--', $surface))
        $addedLines = @($unifiedDiff | Where-Object { $_.StartsWith('+') -and -not $_.StartsWith('+++') } | ForEach-Object { $_.Substring(1) })
        $content = $addedLines -join "`n"
    } else {
        $content = [IO.File]::ReadAllText((Join-Path $RepositoryRoot $surface))
    }
    Assert-True (-not [regex]::IsMatch($content, $lineLeadingPattern)) "Unresolved placeholder (line-leading) found in $surface."
    Assert-True (-not [regex]::IsMatch($content, $wordBoundaryPattern)) "Unresolved placeholder (word-boundary) found in $surface."
}
Add-PassedCheck -Number 13 -Name 'unresolved placeholder scan (AC-P2-10)'

# Check 14/15: evidence bundle and clean tree
$expectedEvidencePath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot 'artifacts/p2-verification'))
$resolvedEvidencePath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $EvidenceDirectory))
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($resolvedEvidencePath.TrimEnd('\', '/'), $expectedEvidencePath.TrimEnd('\', '/'))) 'EvidenceDirectory must resolve to artifacts/p2-verification under the repository root.'

$trackedEvidence = Get-GitResult -Arguments @('ls-files', '--error-unmatch', '--', 'artifacts/p2-verification')
Assert-True ($trackedEvidence.ExitCode -ne 0) 'artifacts/p2-verification must be untracked.'
[IO.Directory]::CreateDirectory($resolvedEvidencePath) | Out-Null

$headCommit = (@(Invoke-Git -Arguments @('rev-parse', 'HEAD')))[0].Trim()

Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'changed-files.txt') -Content ($ActualChanges -join "`n")
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'axis-schema-check.json') -Content ($axisSchemaCheck | ConvertTo-Json -Depth 10)
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'delivery-controls-check.json') -Content (([ordered]@{ schema = 'biostack.p2-delivery-controls-check.v1'; pass = $true; classesChecked = $DeliveryClassLabels }) | ConvertTo-Json -Depth 10)
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'fold-engine-check.json') -Content (([ordered]@{ schema = 'biostack.p2-fold-engine-check.v1'; pass = $true; headingsFound = $RequiredHeadings; sentencesFound = $RequiredSentences }) | ConvertTo-Json -Depth 10)
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'fixture-results.json') -Content (($FixtureResults) | ConvertTo-Json -Depth 10)
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'regression-map-check.json') -Content (([ordered]@{ schema = 'biostack.p2-regression-map-check.v1'; pass = $true; rowCount = $dataRows.Count; expectedRowCount = $expectedRowCount }) | ConvertTo-Json -Depth 10)

$preSummaryStatus = @(Invoke-Git -Arguments @('status', '--porcelain=v1', '--untracked-files=all'))
Assert-OnlyAuthorizedEvidenceStatus -StatusLines $preSummaryStatus
Add-PassedCheck -Number 14 -Name 'authorized UTF-8/LF evidence bundle generation'

$summary = [ordered]@{
    schema = 'biostack.p2-verification-summary.v1'
    pass = $true
    baseCommit = $BaseCommit.ToLowerInvariant()
    headCommit = $headCommit
    builderId = $BuilderId
    reviewerIds = [string[]]$ReviewerIds
    checks = $CheckResults.ToArray()
}
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'verification-summary.json') -Content ($summary | ConvertTo-Json -Depth 12)

[string[]]$ExpectedEvidenceFiles = @(
    'changed-files.txt', 'axis-schema-check.json', 'delivery-controls-check.json',
    'fold-engine-check.json', 'fixture-results.json', 'regression-map-check.json',
    'verification-summary.json'
)
foreach ($evidenceFile in $ExpectedEvidenceFiles) {
    Assert-True (Test-Path -LiteralPath (Join-Path $resolvedEvidencePath $evidenceFile)) "Missing evidence file: $evidenceFile"
}

$finalStatus = @(Invoke-Git -Arguments @('status', '--porcelain=v1', '--untracked-files=all'))
Assert-OnlyAuthorizedEvidenceStatus -StatusLines $finalStatus
Add-PassedCheck -Number 15 -Name 'untracked evidence directory and clean tree'

Write-Output 'P2 verification PASS'
exit 0
