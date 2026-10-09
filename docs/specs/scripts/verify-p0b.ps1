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
# Check 10: unbounded-operationalization -- closed-world sentence-citation rule
#           (R1-F2/R2-F2, hardened per p0b_reverify_1 F1) + honestly-scoped stem defense-in-depth.
#
# WHAT THIS CONTROL ACTUALLY DOES (stated plainly, per p0b_reverify_1 F1 -- a prior version of
# this check was marketed as "paraphrase-proof" while actually being a six-word lexical
# blocklist; that overstatement is corrected here, and the control itself is replaced):
#
#   This is a CLOSED-WORLD STRUCTURAL rule over sentence boundaries, not a semantic or
#   vocabulary-based detector. Every `applicabilityCriterion.test` field is split into
#   sentences. The leading formulaic sentence(s) -- an optional literal `LOCKED.` sentence,
#   followed by the one sentence that opens with `True when` / `True whenever` (the label's
#   own criterion-defining clause, authored once) -- are PINNED and exempt from citation.
#   EVERY SENTENCE AFTER the pinned sentence(s) must carry, somewhere within that same
#   sentence, a `[cite: ...]` tag resolving to a ruled rule ID (`D-B1`..`D-B6`, optionally
#   `.ruleN`) or a matrix cell ID (`D-B2.<label>.<C1|C2|C3>`). This rule does not read the
#   sentence's words or meaning at all: an extra sentence with zero citation fails whether or
#   not it contains any particular vocabulary. That is what makes it closed-world -- every
#   non-pinned sentence is presumed uncited-and-failing unless a citation tag is structurally
#   present, rather than open-world (presumed fine unless a forbidden word is matched).
#
# WHAT THIS CONTROL DOES NOT DO: it does not check that a cited sentence's CONTENT actually
#   matches what the cited rule/cell says. A sentence could carry a syntactically valid but
#   substantively wrong or irrelevant citation and this check would still pass it -- verifying
#   citation *accuracy* is a semantic judgment, not a structural one, and is out of scope for
#   this script. That class of error is caught (if at all) by this parcel's review-layer
#   control: the required dual reviewer sign-off (two distinct reviewer IDs, checked
#   elsewhere in this script) plus owner merge approval -- not by any automated check here.
#
#   The six-stem lexical scan below (`$behaviorStemPattern`) is kept as a SEPARATE, honestly
#   labeled defense-in-depth layer only. It is exactly what it looks like: a closed six-word
#   keyword blocklist, nothing more. It is not relied on as the primary or paraphrase-
#   resistant control (that claim was the defect this hardening fixes); it is retained because
#   it is cheap and additionally covers the pinned "True when/whenever" sentence itself, which
#   the sentence-citation rule above deliberately does not require citations for (it is the
#   label's own authored criterion, not an extra clause).
# ---------------------------------------------------------------------------

$citationTagPattern = '\[cite:\s*(D-B[1-6](?:\.rule[1-9][0-9]*)?|D-B2\.[a-z0-9-]+\.C[1-3])\]'
$behaviorStemPattern = '(?i)(refus\w*|allow\w*|degrad\w*|escalat\w*|\bblock\w*|suppress\w*)'
$sentenceSplitPattern = '(?<=[.])\s+(?=[A-Z])'

foreach ($lblName in $jsonLabelNames) {
    $lblObj = $labelsObj.$lblName
    $basis = [string]$lblObj.applicabilityCriterion.basis
    Assert-True (-not [string]::IsNullOrWhiteSpace($basis)) "unbounded-operationalization: $lblName applicabilityCriterion has no basis."
    Assert-True (($basis.Contains('[OPERATIONALIZED') -or $basis.Contains('[RULED')) -and $basis.Contains($lblName)) "unbounded-operationalization: $lblName applicabilityCriterion basis does not cite a traceable provenance marker and the label name."
    $test = [string]$lblObj.applicabilityCriterion.test
    Assert-True (-not [string]::IsNullOrWhiteSpace($test)) "unbounded-operationalization: $lblName applicabilityCriterion has no test."

    # -- Closed-world sentence-citation rule (primary control) --
    $sentences = [regex]::Split($test.Trim(), $sentenceSplitPattern)
    $pinnedCount = 0
    if ($sentences.Count -gt 0 -and $sentences[0].Trim() -eq 'LOCKED.') {
        $pinnedCount = 1
    }
    Assert-True ($sentences.Count -gt $pinnedCount -and $sentences[$pinnedCount] -match '^True (when|whenever)\b') "unbounded-operationalization: $lblName applicabilityCriterion test's first non-LOCKED sentence must be the pinned formulaic 'True when...'/'True whenever...' criterion-defining sentence; found: '$($sentences[$pinnedCount])'."
    $pinnedCount++
    for ($si = $pinnedCount; $si -lt $sentences.Count; $si++) {
        $sentence = $sentences[$si]
        $hasCitation = [regex]::IsMatch($sentence, $citationTagPattern)
        Assert-True ($hasCitation) "unbounded-operationalization: $lblName applicabilityCriterion test has a sentence beyond the pinned formulaic sentence with no [cite: D-Bn | D-B2.<label>.<Cn>] provenance tag -- closed-world rule: every non-pinned sentence must carry its own citation regardless of wording (no vocabulary matching performed for this rule). Uncited sentence: '$($sentence.Trim())'"
    }

    # -- Defense-in-depth: closed six-stem lexical scan (secondary, honestly labeled, not paraphrase-resistant) --
    $citationHits = @([regex]::Matches($test, $citationTagPattern))
    foreach ($stemMatch in [regex]::Matches($test, $behaviorStemPattern)) {
        $windowStart = [Math]::Max(0, $stemMatch.Index - 150)
        $windowEnd = [Math]::Min($test.Length, $stemMatch.Index + $stemMatch.Length + 150)
        $window = $test.Substring($windowStart, $windowEnd - $windowStart)
        $hasNearbyCitation = [regex]::IsMatch($window, $citationTagPattern)
        Assert-True ($hasNearbyCitation) "unbounded-operationalization (defense-in-depth stem scan): $lblName applicabilityCriterion test contains listed behavior-stem vocabulary ('$($stemMatch.Value)') with no nearby [cite: D-Bn | D-B2.<label>.<Cn>] provenance citation -- note: this sub-check is a closed six-word lexical blocklist only, not a semantic or paraphrase-resistant detector; it catches exactly this vocabulary list and nothing else."
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
Add-PassedCheck -Name 'unbounded-operationalization' -Detail 'closed-world sentence-citation rule: every applicabilityCriterion.test sentence beyond the pinned formulaic (optional "LOCKED." +) "True when/whenever" sentence must carry a resolvable [cite: D-Bn | D-B2.<label>.<Cn>] tag -- checked by sentence structure, not vocabulary, so an uncited extra sentence fails regardless of wording; plus a separate, honestly-labeled closed six-stem lexical scan (defense-in-depth only, not itself paraphrase-resistant) covering the pinned sentence. Citation CONTENT accuracy (does the cited rule/cell actually say what the sentence claims) is a review-layer control -- this parcel''s required dual reviewer sign-off plus owner merge approval -- not enforced by this script.'

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
# Check 18: dual-reviewer-sign-off-required (F4 remediation) -- check 10's own text names
# "the required dual reviewer sign-off (two distinct reviewer IDs ... checked elsewhere in this
# script)" as the compensating control for citation-content accuracy. Prior to this check, that
# control did not exist: -ReviewerIds was only echoed into the evidence summary. This makes the
# claim true: at least two distinct, non-empty reviewer IDs are mechanically required, or the
# script fails closed before any evidence is written.
# ---------------------------------------------------------------------------

$distinctReviewerIds = @($ReviewerIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() } | Select-Object -Unique)
Assert-True ($distinctReviewerIds.Count -ge 2) "dual-reviewer-sign-off-required: -ReviewerIds must carry at least two distinct, non-empty reviewer IDs (required dual independent review per P0-B.md / D14 fold); found $($distinctReviewerIds.Count) distinct non-empty ID(s)."
Add-PassedCheck -Name 'dual-reviewer-sign-off-required' -Detail "$($distinctReviewerIds.Count) distinct non-empty reviewer ID(s) supplied; a single-reviewer run now fails closed instead of passing 17/17"

# ---------------------------------------------------------------------------
# Check 19: normative-text-byte-pin (F1/F2/F3 remediation) -- every normative text block in BOTH
# artifacts is compared, independently, against its ground-truth source, with no vocabulary
# matching: whitespace/markdown-emphasis-insensitive but content-exact ("squashed" comparison),
# the same ground-truth-diff discipline check 11 already applies to the D-B2 matrix, extended to
# every other ruled/pinned text block named in p0b_reverify_2:
#   (a) the D-B2 footnote text (matrix cells themselves are check 11's job already)
#   (b) D-B3..D-B6 rule/rung sentences, each array item, JSON and the .md's own prose, both
#       independently diffed against P0-B-DESIGN-GATE.md section 3 (not against each other)
#   (c) preemptionOrder stage-label membership/order + compositionNote text
#   (d) doseContextDefinition.text (D-J) against COORDINATOR-DECISIONS-2026-10-07.md ## D-J
#   (e) every applicabilityCriterion.test pinned formulaic sentence, byte-exact in the JSON and
#       content-exact in the .md's own "Per-label applicability criteria" table row -- this is
#       the control that closes F1/T4i/T4c/T4d/T4g/T4f directly: ANY edit to a pinned sentence
#       (an appended clause, a comma-merged behavior clause, a narrowed criterion, a lowercase-
#       initial follow-on sentence) changes the squashed/literal content and fails this check,
#       regardless of wording, citation tags, or sentence-boundary tricks.
#   (f) enablementState literals, including the .md's own embedded JSON snippet (not just the
#       JSON artifact, already covered field-by-field by check 7)
# This check performs NO content editing of any matrix cell, rule wording, or enablement
# semantics -- it only pins what is already shipped against its ruled/ruling source.
# ---------------------------------------------------------------------------

function Get-Squashed {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text)
    $t = $Text -replace '[*>`¹]', ''
    $t = $t -replace '\s+', ''
    return $t
}

function Get-BoundedSection {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$StartMarker,
        [string]$EndMarker
    )
    $startIdx = $Content.IndexOf($StartMarker)
    Assert-True ($startIdx -ge 0) "normative-text-byte-pin: start marker not found: '$StartMarker'."
    if ($EndMarker) {
        $endIdx = $Content.IndexOf($EndMarker, $startIdx + $StartMarker.Length)
        Assert-True ($endIdx -gt $startIdx) "normative-text-byte-pin: end marker not found after start marker '$StartMarker': '$EndMarker'."
        return $Content.Substring($startIdx, $endIdx - $startIdx)
    }
    return $Content.Substring($startIdx)
}

function Get-NumberedItems {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$SectionText)
    $lines = $SectionText -split "`n"
    $items = New-Object 'System.Collections.Generic.List[string]'
    $current = $null
    foreach ($line in $lines) {
        if ($line -match '^\s*\d+\.\s+(.*)$') {
            if ($null -ne $current) { $items.Add($current.Trim()) }
            $current = $Matches[1]
        } elseif ($null -ne $current) {
            if ($line.Trim() -eq '') {
                $items.Add($current.Trim())
                $current = $null
            } else {
                $current = $current + ' ' + $line.Trim()
            }
        }
    }
    if ($null -ne $current) { $items.Add($current.Trim()) }
    return $items.ToArray()
}

# -- (a) D-B2 footnote --
$designGateB2Section = Get-BoundedSection -Content $designGateContent -StartMarker '### D-B2' -EndMarker '### D-B3'
$footnoteMatch = [regex]::Match($designGateB2Section, '(?m)^¹\s+(.+)$')
Assert-True ($footnoteMatch.Success) 'normative-text-byte-pin: D-B2 footnote (leading ¹) not found in P0-B-DESIGN-GATE.md.'
$expectedFootnoteSquashed = Get-Squashed -Text $footnoteMatch.Groups[1].Value
$sourcingLabelObj = $labelsObj.'controlled-or-illegal-sourcing'.behavior
foreach ($cellName in @('C1', 'C2', 'C3')) {
    $actualFootnote = [string]$sourcingLabelObj.$cellName.footnote
    Assert-True ((Get-Squashed -Text $actualFootnote) -eq $expectedFootnoteSquashed) "normative-text-byte-pin: controlled-or-illegal-sourcing.$cellName.footnote diverges from the D-B2 footnote in P0-B-DESIGN-GATE.md.`nExpected (squashed): $expectedFootnoteSquashed`nFound (squashed):    $(Get-Squashed -Text $actualFootnote)"
}
Assert-True ((Get-Squashed -Text $contractMdContent) -match [regex]::Escape($expectedFootnoteSquashed)) 'normative-text-byte-pin: the Markdown artifact does not reproduce the D-B2 footnote text content.'

# -- (b) D-B3..D-B6 rule/rung sentences: JSON array and the .md's own prose, each independently
#        diffed against P0-B-DESIGN-GATE.md section 3 --
# Known, already-shipped OPERATIONALIZED citation addenda: a handful of ruled sentences are
# legitimately extended (in BOTH artifacts, consistently) with a trailing citation/reference
# parenthetical that replaces the ruled sentence's own closing period. These are pinned here
# exactly as shipped -- this is not an open door for new unpinned text: any OTHER addition, or
# any change to this exact addendum text, still fails the comparison below.
$RuleTailAddenda = @{
    'D-B4 (missingInputLadder.rungs):2' = ' (`docs/guidance/biostack-guidance-content-contract.v1.md`, "Required warning and uncertainty language" table — reused by reference, not re-defined).'
}

$ruledListSpecs = @(
    @{ Name = 'D-B3 (numericProvenance.rules)'; DgStart = '### D-B3'; DgEnd = '### D-B4'; JsonArray = @($contract.numericProvenance.rules); MdStart = '## Numeric provenance (D-B3)'; MdEnd = '## Missing-input ladder (D-B4)' },
    @{ Name = 'D-B4 (missingInputLadder.rungs)'; DgStart = '### D-B4'; DgEnd = '### D-B5'; JsonArray = @($contract.missingInputLadder.rungs); MdStart = '## Missing-input ladder (D-B4)'; MdEnd = '## Function-review status (D-B5)' },
    @{ Name = 'D-B5 (functionReviewStatus.rules)'; DgStart = '### D-B5'; DgEnd = '### D-B6'; JsonArray = @($contract.functionReviewStatus.rules); MdStart = '## Function-review status (D-B5)'; MdEnd = '## Escalation semantics (D-B6)' },
    @{ Name = 'D-B6 (escalationSemantics.rules)'; DgStart = '### D-B6'; DgEnd = '## 4. Ruling format'; JsonArray = @($contract.escalationSemantics.rules); MdStart = '## Escalation semantics (D-B6)'; MdEnd = '## Cell semantics' }
)
foreach ($spec in $ruledListSpecs) {
    $dgSection = Get-BoundedSection -Content $designGateContent -StartMarker $spec.DgStart -EndMarker $spec.DgEnd
    $groundTruthItems = Get-NumberedItems -SectionText $dgSection
    Assert-True ($groundTruthItems.Count -gt 0) "normative-text-byte-pin: no numbered ground-truth items parsed for $($spec.Name) from P0-B-DESIGN-GATE.md."
    Assert-True ($spec.JsonArray.Count -eq $groundTruthItems.Count) "normative-text-byte-pin: $($spec.Name) expected $($groundTruthItems.Count) rule(s) (ground truth), found $($spec.JsonArray.Count) in the JSON artifact."
    for ($i = 0; $i -lt $groundTruthItems.Count; $i++) {
        $itemKey = "$($spec.Name):$i"
        $expectedRaw = $groundTruthItems[$i]
        if ($RuleTailAddenda.ContainsKey($itemKey)) { $expectedRaw = $expectedRaw.TrimEnd('.') + $RuleTailAddenda[$itemKey] }
        $expectedSquashed = Get-Squashed -Text $expectedRaw
        $actualSquashed = Get-Squashed -Text ([string]$spec.JsonArray[$i])
        Assert-True ($actualSquashed -eq $expectedSquashed) "normative-text-byte-pin: $($spec.Name) rule $($i + 1) diverges from P0-B-DESIGN-GATE.md (content-exact, whitespace/markdown-emphasis-insensitive comparison).`nExpected (squashed): $expectedSquashed`nFound (squashed):    $actualSquashed"
    }
    $mdSection = Get-BoundedSection -Content $contractMdContent -StartMarker $spec.MdStart -EndMarker $spec.MdEnd
    $mdItems = Get-NumberedItems -SectionText $mdSection
    Assert-True ($mdItems.Count -eq $groundTruthItems.Count) "normative-text-byte-pin: $($spec.Name) -- the Markdown artifact's own prose carries $($mdItems.Count) numbered item(s), expected $($groundTruthItems.Count) (ground truth) -- the .md's own transcription has drifted structurally."
    for ($i = 0; $i -lt $groundTruthItems.Count; $i++) {
        $itemKey = "$($spec.Name):$i"
        $expectedRaw = $groundTruthItems[$i]
        if ($RuleTailAddenda.ContainsKey($itemKey)) { $expectedRaw = $expectedRaw.TrimEnd('.') + $RuleTailAddenda[$itemKey] }
        $expectedSquashed = Get-Squashed -Text $expectedRaw
        $mdSquashed = Get-Squashed -Text $mdItems[$i]
        Assert-True ($mdSquashed -eq $expectedSquashed) "normative-text-byte-pin: $($spec.Name) rule $($i + 1) -- the Markdown artifact's OWN prose diverges from P0-B-DESIGN-GATE.md (ground truth), independent of the JSON artifact's own value.`nExpected (squashed): $expectedSquashed`nFound in .md (squashed): $mdSquashed"
    }
}

# -- (c) preemptionOrder stage-label membership/order + compositionNote --
[hashtable]$expectedStageLabels = @{
    1 = @('acute-red-flag-or-emergency')
    2 = @('controlled-or-illegal-sourcing')
    3 = @('minor-or-age-uncertain', 'pregnancy-or-lactation', 'prescription-treatment-involved')
    4 = @()
}
$actualStages = @($contract.preemptionOrder.stages)
Assert-True ($actualStages.Count -eq 4) "normative-text-byte-pin: preemptionOrder.stages expected exactly 4 stages, found $($actualStages.Count)."
foreach ($stageObj in $actualStages) {
    $stageNum = [int]$stageObj.stage
    Assert-True ($expectedStageLabels.ContainsKey($stageNum)) "normative-text-byte-pin: preemptionOrder.stages carries an unrecognized stage number '$stageNum'."
    $actualLabels = @($stageObj.labels)
    $expLabels = @($expectedStageLabels[$stageNum])
    Assert-True ($actualLabels.Count -eq $expLabels.Count) "normative-text-byte-pin: preemptionOrder stage $stageNum expected $($expLabels.Count) label(s) (ground truth: P0-B-DESIGN-GATE.md §3 D-B2 preemption paragraph), found $($actualLabels.Count)."
    for ($i = 0; $i -lt $expLabels.Count; $i++) {
        Assert-True ($actualLabels[$i] -eq $expLabels[$i]) "normative-text-byte-pin: preemptionOrder stage $stageNum label at index $i expected '$($expLabels[$i])' (ground truth), found '$($actualLabels[$i])' -- a label may not be demoted, promoted, added, or removed from its ruled stage."
    }
}
$compositionNoteMatch = [regex]::Match($designGateB2Section, '(?s)"Most restrictive wins".*?no label erases another''s obligations\.')
Assert-True ($compositionNoteMatch.Success) 'normative-text-byte-pin: could not locate the compositionNote ground-truth sentence in P0-B-DESIGN-GATE.md §3 D-B2.'
$expectedCompositionSquashed = Get-Squashed -Text $compositionNoteMatch.Value
Assert-True ((Get-Squashed -Text ([string]$contract.preemptionOrder.compositionNote)) -eq $expectedCompositionSquashed) "normative-text-byte-pin: preemptionOrder.compositionNote diverges from P0-B-DESIGN-GATE.md §3 D-B2's ruled sentence (content-exact comparison) -- this is the exact control that closes the label-erasure-license rewrite (T3e).`nExpected (squashed): $expectedCompositionSquashed`nFound (squashed):    $(Get-Squashed -Text ([string]$contract.preemptionOrder.compositionNote))"
$mdCompositionMatch = [regex]::Match($contractMdContent, '(?s)`compositionNote`:.*?no\s+label\s+erases\s+another''s\s+obligations\.')
Assert-True ($mdCompositionMatch.Success) 'normative-text-byte-pin: could not locate the `compositionNote` sentence in the Markdown artifact.'
Assert-True ((Get-Squashed -Text $mdCompositionMatch.Value) -match [regex]::Escape($expectedCompositionSquashed)) "normative-text-byte-pin: the Markdown artifact's own compositionNote prose diverges from P0-B-DESIGN-GATE.md §3 D-B2's ruled sentence.`nExpected to contain (squashed): $expectedCompositionSquashed`nFound (squashed):               $(Get-Squashed -Text $mdCompositionMatch.Value)"

# -- (d) doseContextDefinition.text (D-J) --
$coordinatorDecisionsContent = Read-RepoFile -Path 'docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md'
$dJSection = Get-BoundedSection -Content $coordinatorDecisionsContent -StartMarker '## D-J' -EndMarker '## D-K'
$dJBlockquoteLines = @(($dJSection -split "`n") | Where-Object { $_ -match '^>' } | ForEach-Object { $_ -replace '^>\s?', '' })
Assert-True ($dJBlockquoteLines.Count -gt 0) 'normative-text-byte-pin: no blockquote lines found under ## D-J in COORDINATOR-DECISIONS-2026-10-07.md.'
$dJBlockquoteText = ($dJBlockquoteLines -join ' ') -replace '^\s*\*\*Dose-context output\.\*\*\s*', ''
$expectedDJSquashed = Get-Squashed -Text $dJBlockquoteText
Assert-True ((Get-Squashed -Text ([string]$contract.doseContextDefinition.text)) -eq $expectedDJSquashed) "normative-text-byte-pin: doseContextDefinition.text diverges from Coordinator Decision D-J (content-exact comparison) -- this is the exact control that closes the narrowed dose-context-scope rewrite (T3f).`nExpected (squashed): $expectedDJSquashed`nFound (squashed):    $(Get-Squashed -Text ([string]$contract.doseContextDefinition.text))"
$mdDJSection = Get-BoundedSection -Content $contractMdContent -StartMarker '## Dose-context definition (D-J)' -EndMarker '## Precedence directional constraint'
$mdDJBlockquoteLines = @(($mdDJSection -split "`n") | Where-Object { $_ -match '^>' } | ForEach-Object { $_ -replace '^>\s?', '' })
Assert-True ($mdDJBlockquoteLines.Count -gt 0) 'normative-text-byte-pin: no blockquote lines found under the Markdown artifact''s Dose-context definition (D-J) section.'
$mdDJBlockquoteText = ($mdDJBlockquoteLines -join ' ') -replace '^\s*\*\*Dose-context output\.\*\*\s*', ''
Assert-True ((Get-Squashed -Text $mdDJBlockquoteText) -eq $expectedDJSquashed) "normative-text-byte-pin: the Markdown artifact's own Dose-context definition (D-J) prose diverges from Coordinator Decision D-J (ground truth), independent of the JSON artifact's own value.`nExpected (squashed): $expectedDJSquashed`nFound in .md (squashed): $(Get-Squashed -Text $mdDJBlockquoteText)"

# -- (e) every applicabilityCriterion.test pinned formulaic sentence --
# Ground-truth strings pinned at this parcel's authoring commit (the OPERATIONALIZED criteria this
# parcel itself authors; see the [OPERATIONALIZED -- bounded] marker on each). Pinning these means
# ANY edit -- an appended clause, a comma-merged behavior clause smuggled into the pinned sentence
# (the exact T4i/T4c evasion form), a lowercase-initial follow-on sentence (T4d), or a narrowed
# criterion (T4f) -- changes this exact content and fails this check, regardless of wording,
# citation tags, or sentence-boundary tricks. This is deliberately NOT a sentence-structure or
# vocabulary rule like check 10 -- it is a direct byte-exact pin of the whole field.
([ordered]@{
    'ordinary'                                = 'True when none of the other nine labels'' tests below are true for this invocation.'
    'prescription-treatment-involved'        = 'True when a declared input or output names a substance/treatment that the function''s own capability contract, or a `label-or-prescription-transcribed` input, marks prescription-status, or the user has declared it as a currently prescribed/clinician-directed treatment.'
    'investigational-or-unapproved'          = 'True when the cited evidence source''s own regulatory-status field states investigational, unapproved, or off-label for the declared use, or no approved-use record exists in the evidence source for the declared use. Deterministic threshold: an output with zero cited evidence sources for the declared use is out-of-scope for this criterion (not vacuously true) and instead defers to that function''s own missing-input ladder behavior (missingInputLadder, rule 2) — this criterion only attaches once at least one evidence source is actually cited and its regulatory-status field is read. [cite: D-B4.rule2]'
    'gray-market-or-identity-uncertain'       = 'True when a required identity/concentration/manufacturing-source provenance field (per numericProvenance and the function''s declared inputs) is absent, unverified, or not resolvable to one of the five locked numeric origins.'
    'injection-or-sterile-preparation'        = 'True when the function''s declared output type or route is injection, reconstitution, or any sterile-preparation step.'
    'interaction-or-contraindication-signal'  = 'True when a matching interaction/contraindication record exists in the cited evidence source for the user''s declared concurrent substances, medications, or conditions. Deterministic criterion: the base-applicability matching algorithm itself (exact substance-name match vs. drug-class/mechanism match vs. any broader match) is function-declared, not fixed by this contract — the same deferral-to-function pattern already used for `acute-red-flag-or-emergency`''s triggering criterion set [cite: D-B2.interaction-or-contraindication-signal.C2]. That same function-declared algorithm also carries the function''s own declared strength grading — exactly the quantity the C3 cell''s D→E strong-signal escalation [cite: D-B2.interaction-or-contraindication-signal.C3] names as "the function''s own declared evidence-source match threshold": base applicability (does a record match at all) and the D→E upgrade (is the matched record''s signal strong) are the matching-vs-grading components of one function-declared matching specification, not two independently defined specifications.'
    'minor-or-age-uncertain'                  = 'True when the user''s declared age is below the function''s declared minimum-age threshold, or age is a function-declared required input and is missing or unverified.'
    'pregnancy-or-lactation'                  = 'True when the user has declared current pregnancy or lactation status, or that status is a function-declared required input and is missing/unverified for a function whose substance or output carries a source-labeled pregnancy/lactation signal.'
    'acute-red-flag-or-emergency'             = 'LOCKED. True when declared symptoms, vitals, or context match any function-declared red-flag/emergency criterion; the triggering criterion set is itself function-declared (per D15''s function-specific-review model), not invented by this contract.'
    'controlled-or-illegal-sourcing'          = 'LOCKED. True whenever the declared request or context seeks sourcing, acquisition, legal/regulatory evasion, or concealment of a controlled-or-illegal substance or its acquisition — evaluated against the request/context, never against the mere mention of a controlled substance''s evidence content.'
}).GetEnumerator() | ForEach-Object {
    $lblName = $_.Key
    $expectedTest = $_.Value
    $actualTest = [string]$labelsObj.$lblName.applicabilityCriterion.test
    Assert-True ($actualTest -eq $expectedTest) "normative-text-byte-pin: $lblName applicabilityCriterion.test diverges byte-for-byte from its pinned formulaic sentence -- this is the control that closes F1/F3 (T4i/T4c/T4d/T4g/T4f): any edit, including a comma-merged behavior clause or a narrowed criterion, fails here regardless of citation tags or wording.`nExpected: $expectedTest`nFound:    $actualTest"
}
$mdCriteriaRows = @{}
foreach ($m in [regex]::Matches($contractMdContent, '(?m)^\|\s*`([a-z0-9-]+)`\s*\|\s*([^|\r\n]+?)\s*\|\s*$')) {
    $lbl = $m.Groups[1].Value
    if (($SubstanceFunctionRiskLabels -contains $lbl) -and (-not $mdCriteriaRows.ContainsKey($lbl))) {
        $mdCriteriaRows[$lbl] = $m.Groups[2].Value
    }
}
# The Markdown table's own prose is independently authored (not required to be byte-identical
# to the JSON field's wording for every label -- two of the ten rows carry pre-existing,
# substantively-equivalent-but-not-identical phrasing from this parcel's original authoring),
# so each row is pinned against ITS OWN already-shipped ground truth, not against the JSON
# field. This still closes T4g: any edit to the .md table row content (appended clause,
# comma-merged behavior clause, narrowed criterion) changes the squashed content and fails.
$ExpectedMdCriteriaRows = [ordered]@{
    'ordinary'                               = 'True when none of the other nine labels'' tests below are true for this invocation.'
    'prescription-treatment-involved'        = 'True when a declared input or output names a substance/treatment that the function''s own capability contract, or a `label-or-prescription-transcribed` input, marks prescription-status, or the user has declared it as a currently prescribed/clinician-directed treatment.'
    'investigational-or-unapproved'          = 'True when the cited evidence source''s own regulatory-status field states investigational, unapproved, or off-label for the declared use, or no approved-use record exists in the evidence source for the declared use. Deterministic threshold: an output with **zero cited evidence sources** for the declared use is **out-of-scope** for this criterion (not vacuously true) and instead defers to that function''s own missing-input ladder behavior (`missingInputLadder`, rule 2) — this criterion only attaches once at least one evidence source is actually cited and its regulatory-status field is read. `[cite: D-B4.rule2]`'
    'gray-market-or-identity-uncertain'      = 'True when a required identity/concentration/manufacturing-source provenance field (per `numericProvenance` and the function''s declared inputs) is absent, unverified, or not resolvable to one of the five locked numeric origins.'
    'injection-or-sterile-preparation'       = 'True when the function''s declared output type or route is injection, reconstitution, or any sterile-preparation step.'
    'interaction-or-contraindication-signal' = 'True when a matching interaction/contraindication record exists in the cited evidence source for the user''s declared concurrent substances, medications, or conditions. Deterministic criterion: the base-applicability matching algorithm itself (exact substance-name match vs. drug-class/mechanism match vs. any broader match) is **function-declared**, not fixed by this contract — the same deferral-to-function pattern already used for `acute-red-flag-or-emergency`''s "triggering criterion set is itself function-declared," below `[cite: D-B2.interaction-or-contraindication-signal.C2]`. That same function-declared algorithm also carries the function''s own declared strength grading — exactly the quantity this table''s `C3` cell''s `D→E` strong-signal escalation `[cite: D-B2.interaction-or-contraindication-signal.C3]` names as "the function''s own declared evidence-source match threshold": base applicability (does a record match at all) and the `D→E` upgrade (is the matched record''s signal strong) are therefore the matching-vs-grading components of one function-declared matching specification, not two independently defined, unrelated specifications.'
    'minor-or-age-uncertain'                 = 'True when the user''s declared age is below the function''s declared minimum-age threshold, or age is a function-declared required input and is missing or unverified.'
    'pregnancy-or-lactation'                 = 'True when the user has declared current pregnancy or lactation status, or that status is a function-declared required input and is missing/unverified for a function whose substance or output carries a source-labeled pregnancy/lactation signal.'
    'acute-red-flag-or-emergency'            = '**LOCKED.** True when declared symptoms, vitals, or context match any function-declared red-flag/emergency criterion; the triggering criterion set is itself function-declared (per D15''s function-specific-review model), not invented by this contract.'
    'controlled-or-illegal-sourcing'         = '**LOCKED.** True whenever the declared request or context seeks sourcing, acquisition, legal/regulatory evasion, or concealment of a controlled-or-illegal substance or its acquisition — evaluated against the request/context, never against the mere mention of a controlled substance''s evidence content.'
}
foreach ($lblName in $jsonLabelNames) {
    Assert-True ($mdCriteriaRows.ContainsKey($lblName)) "normative-text-byte-pin: the Markdown artifact's `"Per-label applicability criteria`" table has no row for '$lblName'."
    $expectedSquashed = Get-Squashed -Text ([string]$ExpectedMdCriteriaRows[$lblName])
    $mdSquashed = Get-Squashed -Text $mdCriteriaRows[$lblName]
    Assert-True ($mdSquashed -eq $expectedSquashed) "normative-text-byte-pin: the Markdown artifact's OWN `"Per-label applicability criteria`" table row for '$lblName' diverges from its pinned ground truth (content-exact comparison) -- closes the .md-only clause-smuggling evasion (T4g).`nExpected (squashed): $expectedSquashed`nFound in .md (squashed): $mdSquashed"
}

# -- (f) enablementState literals, including the .md's own embedded JSON snippet --
$mdEnablementSnippetMatch = [regex]::Match($contractMdContent, '(?s)```json\s*\{.*?"biostackRecommendedOrigination":\s*(\{.*?\})\s*\}\s*```')
Assert-True ($mdEnablementSnippetMatch.Success) 'normative-text-byte-pin: could not locate the embedded enablementState JSON snippet in the Markdown artifact.'
$mdEnablementObj = $mdEnablementSnippetMatch.Groups[1].Value | ConvertFrom-Json -Depth 10
[string[]]$EnablementLiteralFields = @(
    'definedInContract', 'publiclyEnabled', 'currentPosture', 'governingGuidanceContractVersion',
    'requiredGuidanceContractVersionForPublicEnablement', 'requiredApprovalLevelForPublicEnablement',
    'publicEnablementEvent', 'rulingReference'
)
foreach ($field in $EnablementLiteralFields) {
    $jsonVal = $enablement.$field
    $mdVal = $mdEnablementObj.$field
    Assert-True ([string]$jsonVal -eq [string]$mdVal) "normative-text-byte-pin: enablementState.biostackRecommendedOrigination.$field diverges between the JSON artifact ('$jsonVal') and the Markdown artifact's own embedded JSON snippet ('$mdVal')."
}
Assert-True ($mdEnablementObj.publiclyEnabled -eq $false) "normative-text-byte-pin: the Markdown artifact's own embedded JSON snippet does not carry the literal boolean publiclyEnabled: false."

Add-PassedCheck -Name 'normative-text-byte-pin' -Detail 'byte/content-exact (whitespace- and markdown-emphasis-insensitive) comparison of every normative text block in BOTH artifacts against ground truth, independently: D-B2 footnote; D-B3..D-B6 rule/rung sentences (JSON array and the .md''s own prose, each diffed against P0-B-DESIGN-GATE.md, not against each other); preemptionOrder stage-label membership/order; compositionNote (JSON and .md); doseContextDefinition.text / D-J (JSON and .md); every applicabilityCriterion.test pinned formulaic sentence byte-exact in JSON plus content-exact in the .md''s own criteria table; enablementState literals cross-checked against the .md''s own embedded JSON snippet. No matrix cell, rule wording, or enablement semantic was changed by this check -- it only pins what already ships.'

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
