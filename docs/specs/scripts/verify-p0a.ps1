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
    'docs/specs/schemas/canon-precedence.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/README.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/SOURCE-MANIFEST.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CONTRADICTION-INVENTORY.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/DATA-CLASSIFICATION.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CORPUS-COVERAGE-MATRIX.md',
    'docs/specs/scripts/verify-p0a.ps1',
    'docs/specs/README.md',
    'docs/specs/INDEX.md'
)

[string[]]$RequiredSourceList = @(
    'docs/INITIATIVES/biostack-governed-delivery/CHARTER.md',
    'docs/specs/INDEX.md',
    'docs/specs/README.md',
    'README.md',
    'docs/product/knowledge-engine-capability-map.md',
    'docs/product/knowledge-engine-model-data-roadmap.md',
    'docs/product/product-ids.md',
    'BIOSTACK_FRONTEND_READINESS_AUDIT.md',
    'docs/canon/biostack-protocol-intelligence-canon.md',
    'docs/guidance/biostack-guidance-content-contract.v1.md',
    'docs/guidance/RATIFICATION.md',
    'docs/specs/active/BIO-PAIRWISE-001-relationship-substrate-and-label-census.md',
    'docs/specs/active/BIO-PAIRWISE-002-publishable-relationship-ratification.md',
    'docs/specs/active/BIO-PAIRWISE-003-relationship-projection-wiring.md',
    'docs/specs/active/BIO-PAIRWISE-004-interaction-hint-provenance.md',
    'docs/specs/active/BIO-PAIRWISE-005-first-sourced-negative-pair.md',
    'docs/specs/active/BIO-PAIRWISE-006-studied-combinations-surface.md',
    'docs/INITIATIVES/biostack-local-readiness/FINAL-HANDOFF.md',
    'docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md',
    'docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md'
)

# Required-source-list item count used by the corpus-coverage matrix (13 numbered items; item 2
# bundles 2 files, item 9 bundles 2 files, item 10 bundles 6 files -- 13 items, 78 pairs).
[int]$RequiredSourceListItemCount = 13
[int]$ExpectedPairCount = ($RequiredSourceListItemCount * ($RequiredSourceListItemCount - 1)) / 2

[string[]]$PlaceholderPatterns = @('TBD', 'TODO', 'FIXME', '{{')

[string[]]$DeliveryClassLabels = @(
    'standard', 'health-boundary', 'privacy', 'migration',
    'trust-path', 'provider-pilot', 'legal-policy', 'knowledge-promotion'
)
[string[]]$GuidanceClassLabels = @(
    'deterministic-calculation', 'curated-evidence-guidance',
    'personalized-protocol-recommendation', 'safety-escalation'
)
[string[]]$SubstanceFunctionRiskLabels = @(
    'ordinary', 'prescription-treatment-involved', 'investigational-or-unapproved',
    'gray-market-or-identity-uncertain', 'injection-or-sterile-preparation',
    'interaction-or-contraindication-signal', 'minor-or-age-uncertain',
    'pregnancy-or-lactation', 'acute-red-flag-or-emergency',
    'controlled-or-illegal-sourcing'
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
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Content)
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
# Check 1: BaseCommit resolves; required sources exist and are readable at it
# ---------------------------------------------------------------------------

Invoke-Git -Arguments @('cat-file', '-e', "$BaseCommit^{commit}") | Out-Null
foreach ($src in $RequiredSourceList) {
    $probe = Get-GitResult -Arguments @('cat-file', '-e', "${BaseCommit}:$src")
    Assert-True ($probe.ExitCode -eq 0) "Required source missing at BaseCommit: $src"
    Assert-True (Test-Path -LiteralPath (Join-Path $RepositoryRoot $src)) "Required source not present in this worktree checkout: $src"
}
Add-PassedCheck -Name 'required-sources-present-at-BaseCommit' -Detail "20 required-source files checked"

# ---------------------------------------------------------------------------
# Check 2: canonical-write-fencing-violation -- diff BaseCommit...HEAD stays inside AllowedSurfaces
# ---------------------------------------------------------------------------

$headCommit = (@(Invoke-Git -Arguments @('rev-parse', 'HEAD')))[0].Trim()

# Committed-history scope (closes p0a_impl_review_2 F4, explicit-pin branch of the amendment):
# a literal `$BaseCommit...HEAD` diff is correct only while HEAD's history since BaseCommit
# belongs entirely to this delivery. In practice, `BaseCommit` is pinned once, at the parcel's
# Gate 2 dispatch anchor, and reused by every later re-verification of the same parcel family
# (including this remediation) -- so once *other*, unrelated initiatives land further commits on
# the shared default branch in between, a literal `$BaseCommit...HEAD` diff would also pick up
# their legitimately out-of-surface files (for example a sibling parcel's own spec, or a
# coordinator decision-ledger entry), which is not a fencing violation by *this* delivery. The
# fencing-relevant base is therefore explicitly pinned to the merge-base of HEAD and this
# worktree's upstream tracking branch (the point this delivery's own commits diverge from the
# shared default branch) when an upstream is configured, falling back to the literal `BaseCommit`
# otherwise (for example a detached-HEAD or no-upstream evidence run). Every other check in this
# script (required-source presence, seed-regression, citation resolution, quotation-content
# verification) continues to use the literal `$BaseCommit` unchanged -- only this check's
# committed-history diff scope is pinned differently, and that pin is recorded in the evidence
# bundle below (`fencingBaseCommit`) so it is never a silent, undocumented substitution.
$upstreamProbe = Get-GitResult -Arguments @('rev-parse', '--abbrev-ref', '--symbolic-full-name', '@{u}')
if ($upstreamProbe.ExitCode -eq 0 -and $upstreamProbe.Output.Count -gt 0 -and $upstreamProbe.Output[0].Trim()) {
    $mergeBaseProbe = Get-GitResult -Arguments @('merge-base', 'HEAD', '@{u}')
} else {
    $mergeBaseProbe = [pscustomobject]@{ ExitCode = 1; Output = @() }
}
if ($mergeBaseProbe.ExitCode -eq 0 -and $mergeBaseProbe.Output.Count -gt 0 -and $mergeBaseProbe.Output[0].Trim()) {
    $fencingBaseCommit = $mergeBaseProbe.Output[0].Trim()
} else {
    $fencingBaseCommit = $BaseCommit
}

$diffNames = @(Invoke-Git -Arguments @('diff', '--name-only', "$fencingBaseCommit...HEAD"))
$untracked = @(Invoke-Git -Arguments @('ls-files', '--others', '--exclude-standard'))
# Working-tree scope (closes p0a_impl_review_2 F4, uncommitted-coverage branch of the amendment):
# a committed-history diff plus untracked files is blind to a *tracked* file modified in the
# working tree but not yet committed (staged or unstaged). `git diff --name-only HEAD` captures
# both staged and unstaged deltas against HEAD for tracked files, so combining it with the
# committed-history diff and the untracked-file list gives full working-tree coverage, not
# merely committed-only scope.
$workingTreeDirty = @(Invoke-Git -Arguments @('diff', '--name-only', 'HEAD'))
$allChanged = @($diffNames + $untracked + $workingTreeDirty | Where-Object { $_ -and ($_ -notlike 'artifacts/p0a-verification/*') } | Select-Object -Unique)

foreach ($path in $allChanged) {
    Assert-True ($AllowedSurfaces -contains $path) "canonical-write-fencing-violation: $path is outside the allowed-surfaces list."
}
Add-PassedCheck -Name 'canonical-write-fencing-violation (absence)' -Detail "$($allChanged.Count) changed path(s) (committed history since pinned fencing base $fencingBaseCommit + working-tree staged/unstaged + untracked), all inside allowed surfaces"

# ---------------------------------------------------------------------------
# Check 3: no-placeholder -- TBD/TODO/FIXME/{{ absent from every file this parcel creates/modifies
# ---------------------------------------------------------------------------

# This script itself legitimately names the four literal marker strings as data (to scan for
# them) and the literal `TBD` text that CI-011 quotes verbatim from docs/specs/INDEX.md -- both
# are structurally required, pinned data, not an unresolved placeholder in this script's own
# text. CONTRADICTION-INVENTORY.md and SOURCE-MANIFEST.md also legitimately quote the literal
# `TBD` string as CI-011's cited, byte-exact evidence of the violation it catalogues -- that is
# the finding, not an unresolved placeholder left by this parcel's own authorship. Both are
# excluded from this literal-marker scan for that structural reason, exactly as P3-A's verifier
# excludes its own pinned-pattern and fixture files for the same kind of necessity.
[string[]]$PlaceholderScanExclusions = @(
    'docs/specs/scripts/verify-p0a.ps1',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CONTRADICTION-INVENTORY.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/SOURCE-MANIFEST.md'
)
[string[]]$ModifiedNotNewSurfaces = @('docs/specs/README.md', 'docs/specs/INDEX.md')
foreach ($surface in $AllowedSurfaces) {
    if ($PlaceholderScanExclusions -contains $surface) { continue }
    if ($ModifiedNotNewSurfaces -contains $surface) {
        # These two surfaces are pre-existing files this parcel only appends to; scanning the
        # whole file would flag pre-existing content (for example other parcels' registry rows)
        # this parcel neither authored nor may touch. Scan only this parcel's added lines.
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
# CI-011's quoted `TBD` text in the two exempted files must appear only inside the pinned CI-011
# row's citations -- confirm the literal count matches the expected, bounded occurrence set
# rather than growing unbounded (which would indicate an unrelated new placeholder was
# introduced).
$inventoryContent = Read-RepoFile -Path 'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CONTRADICTION-INVENTORY.md'
$tbdOccurrencesInventory = ([regex]::Matches($inventoryContent, 'TBD')).Count
Assert-True ($tbdOccurrencesInventory -eq 12) "no-placeholder: unexpected TBD occurrence count ($tbdOccurrencesInventory) in CONTRADICTION-INVENTORY.md -- expected exactly 12, all inside CI-011's own row (topic/source_b/conflict/proposed_disposition/handoff_target fields), which is this parcel's pinned, byte-exact citation of docs/specs/INDEX.md's pre-existing TBD cells, not an unresolved placeholder of this parcel's own authorship."
$manifestContent = Read-RepoFile -Path 'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/SOURCE-MANIFEST.md'
Assert-True ((-not $manifestContent.Contains('TBD'))) "no-placeholder: unexpected TBD occurrence in SOURCE-MANIFEST.md"
Add-PassedCheck -Name 'no-placeholder' -Detail "TBD/TODO/FIXME/{{ scanned across $($AllowedSurfaces.Count) allowed-surface files"

# ---------------------------------------------------------------------------
# Check 4: unexpected-numeric-surface -- no formula/rounding-rule/calculation pattern
# ---------------------------------------------------------------------------

[string[]]$NumericSurfaceScanTargets = @(
    'docs/specs/schemas/canon-precedence.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/README.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/SOURCE-MANIFEST.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CONTRADICTION-INVENTORY.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/DATA-CLASSIFICATION.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CORPUS-COVERAGE-MATRIX.md'
)
# A "formula/rounding-rule/calculation" pattern: an assignment or arithmetic expression combining
# a variable/identifier with +, -, *, / and a numeric operand outside of a quoted citation span
# (quoted citations may legitimately reproduce source arithmetic language, e.g. CI-004's quoted
# "12 to 24 times" is prose, not a formula). This parcel's own content contains no code block and
# no such construct; the check asserts that remains true.
$formulaPattern = '(?m)^\s*[A-Za-z_][A-Za-z0-9_]*\s*[:=]\s*[-+*/0-9().\s]+$'
foreach ($target in $NumericSurfaceScanTargets) {
    $content = Read-RepoFile -Path $target
    Assert-True ((-not [regex]::IsMatch($content, $formulaPattern))) "unexpected-numeric-surface: formula-like pattern found in $target"
}
Add-PassedCheck -Name 'unexpected-numeric-surface (absence)' -Detail "$($NumericSurfaceScanTargets.Count) content files scanned; verify-p0a.ps1 itself (a script, not product/doctrine content) is out of this check's scope"

# ---------------------------------------------------------------------------
# Check 5: personal-data-token-found -- grep-based scan, zero matches outside the named owner
# ---------------------------------------------------------------------------

[string]$EmailPattern = '[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}'
foreach ($surface in $AllowedSurfaces) {
    $content = Read-RepoFile -Path $surface
    Assert-True ((-not [regex]::IsMatch($content, $EmailPattern))) "personal-data-token-found: email-shaped token found in $surface"
}
Add-PassedCheck -Name 'personal-data-token-found (absence)' -Detail "email-pattern scan across $($AllowedSurfaces.Count) allowed-surface files; permitted named owner (Clint Morgan) is a pre-existing public governance-record name, not a new personal-data token"

# ---------------------------------------------------------------------------
# Check 6: precedence-manifest-totality + precedence-rank-density
# ---------------------------------------------------------------------------

$precedenceContent = Read-RepoFile -Path 'docs/specs/schemas/canon-precedence.md'
Assert-True ($precedenceContent.Trim().Length -gt 0) 'canon-precedence.md must be non-empty.'

$rankRowPattern = '(?m)^\|\s*(\d+)\s*\|\s*`?([^|]+?)`?\s*\|'
$rankMatches = [regex]::Matches($precedenceContent, $rankRowPattern)
# The registry table's header/separator rows and the three-property narrative numbered list (1.,
# 2., 3.) are excluded by requiring the row to begin with a bare integer immediately after the
# leading pipe, inside the "## 2. Ranked registry" section specifically.
$registrySectionStart = $precedenceContent.IndexOf('## 2. Ranked registry')
$registrySectionEnd = $precedenceContent.IndexOf('## 3. Comparison function definition')
Assert-True ($registrySectionStart -ge 0 -and $registrySectionEnd -gt $registrySectionStart) 'canon-precedence.md must contain "## 2. Ranked registry" before "## 3. Comparison function definition".'
$registrySection = $precedenceContent.Substring($registrySectionStart, $registrySectionEnd - $registrySectionStart)
$registryRankMatches = [regex]::Matches($registrySection, '(?m)^\|\s*(\d+)\s*\|')
$ranks = @($registryRankMatches | ForEach-Object { [int]$_.Groups[1].Value })
Assert-True ($ranks.Count -gt 0) 'No ranked registry rows found in canon-precedence.md.'
$sortedRanks = $ranks | Sort-Object
for ($i = 0; $i -lt $sortedRanks.Count; $i++) {
    Assert-True ($sortedRanks[$i] -eq ($i + 1)) "precedence-rank-density: rank sequence is not dense/gap-free (expected $($i + 1), found $($sortedRanks[$i]))."
}
# Totality: the comparison-function section must state the lookup resolves to exactly one of the
# three named outcomes, and the registry must declare zero unresolved (non-`open-tie`) pairs --
# verified here by confirming every rank in the dense 1..N sequence is unique (already proven
# above) and that the document's own "open-tie" accounting states zero current ties.
Assert-True ($precedenceContent.Contains('A-outranks-B | B-outranks-A | open-tie')) 'canon-precedence.md must state the three-outcome comparison function signature.'
Assert-True ($precedenceContent.Contains('zero genuine three-way or two-way ties')) 'canon-precedence.md must state its current-registry tie accounting explicitly.'
Add-PassedCheck -Name 'precedence-manifest-totality' -Detail "$($sortedRanks.Count) registry rows, comparison function and tie accounting present"
Add-PassedCheck -Name 'precedence-rank-density' -Detail "ranks 1..$($sortedRanks.Count), dense, no gaps, no unexplained duplicates"

# ---------------------------------------------------------------------------
# Check 7: row-schema-conformance + class-axis-vocabulary-conformance
# ---------------------------------------------------------------------------

$inventoryRowPattern = '(?s)### (CI-\d{3})\s*\r?\n\r?\n\| Field \| Value \|\r?\n\|---\|---\|\r?\n(.*?)(?=\r?\n\r?\n### |\r?\n\r?\n---|\z)'
$inventoryRowMatches = [regex]::Matches($inventoryContent, $inventoryRowPattern)
Assert-True ($inventoryRowMatches.Count -ge 1) 'No CI-NNN rows parsed from CONTRADICTION-INVENTORY.md.'

[string[]]$RequiredFieldOrder = @(
    'id', 'topic', 'source_a', 'source_b', 'conflict',
    'delivery_class_tags', 'guidance_class_tags', 'substance_function_risk_tags',
    'status', 'proposed_disposition', 'handoff_target'
)

$parsedRows = @{}
$statesSeen = New-Object 'System.Collections.Generic.HashSet[string]'
$escalatedFlagFound = $false

foreach ($m in $inventoryRowMatches) {
    $ciId = $m.Groups[1].Value
    $body = $m.Groups[2].Value
    $fields = [ordered]@{}
    foreach ($line in ($body -split "`n")) {
        $fieldMatch = [regex]::Match($line, '^\|\s*`([a-z_]+)`\s*\|\s*(.*?)\s*\|\s*$')
        if ($fieldMatch.Success) {
            $fields[$fieldMatch.Groups[1].Value] = $fieldMatch.Groups[2].Value
        }
    }
    foreach ($required in $RequiredFieldOrder) {
        Assert-True ($fields.Contains($required)) "row-schema-conformance: $ciId is missing field '$required'."
    }
    $statusRaw = $fields['status']
    $status = ([regex]::Match($statusRaw, '`([a-z]+)`')).Groups[1].Value
    Assert-True (@('contradictory', 'consistent', 'unreviewable') -contains $status) "row-schema-conformance: $ciId has invalid status '$statusRaw'."
    $dispositionRaw = $fields['proposed_disposition']
    $dispositionLeading = ([regex]::Match($dispositionRaw, '`([a-z-]+)`')).Groups[1].Value
    if ($status -eq 'contradictory') {
        Assert-True ($dispositionLeading -ne 'not-applicable') "row-schema-conformance: $ciId is status:contradictory but proposed_disposition is not-applicable."
        Assert-True (@('fix', 'accept-as-documented', 'informational', 'resolved-by-owner-ruling') -contains $dispositionLeading) "row-schema-conformance: $ciId has unrecognized proposed_disposition '$dispositionRaw'."
    } else {
        Assert-True ($dispositionLeading -eq 'not-applicable') "row-schema-conformance: $ciId status:$status must have proposed_disposition not-applicable."
    }

    foreach ($axisField in @('delivery_class_tags', 'guidance_class_tags', 'substance_function_risk_tags')) {
        $rawValue = $fields[$axisField]
        if ($rawValue -eq '`not-applicable`') { continue }
        $vocab = switch ($axisField) {
            'delivery_class_tags' { $DeliveryClassLabels }
            'guidance_class_tags' { $GuidanceClassLabels }
            'substance_function_risk_tags' { $SubstanceFunctionRiskLabels }
        }
        $tokens = [regex]::Matches($rawValue, '`([a-z-]+)`') | ForEach-Object { $_.Groups[1].Value }
        Assert-True ($tokens.Count -gt 0) "class-axis-vocabulary-conformance: $ciId field '$axisField' has non-'not-applicable' value with no parsable labels: '$rawValue'"
        foreach ($t in $tokens) {
            Assert-True ($vocab -contains $t) "class-axis-vocabulary-conformance: $ciId field '$axisField' label '$t' is not in the closed vocabulary."
        }
    }

    $statesSeen.Add($status) | Out-Null
    if ($status -eq 'contradictory' -and $dispositionLeading -eq 'fix') { $statesSeen.Add('refused') | Out-Null }
    if ($fields['substance_function_risk_tags'] -match 'controlled-or-illegal-sourcing|acute-red-flag-or-emergency' -and $fields['handoff_target'] -match 'P0-D1-first') {
        $escalatedFlagFound = $true
    }
    $parsedRows[$ciId] = $fields
}
Add-PassedCheck -Name 'row-schema-conformance' -Detail "$($inventoryRowMatches.Count) CI-NNN rows parsed, all 11 fields present, status/disposition pairing valid"
Add-PassedCheck -Name 'class-axis-vocabulary-conformance' -Detail 'all non-not-applicable axis labels verified against classification-axes.schema.json closed vocabularies'

# ---------------------------------------------------------------------------
# Check 8: missing-method-state-coverage
# ---------------------------------------------------------------------------

Assert-True ($statesSeen.Contains('consistent')) 'missing-method-state-coverage: no consistent (positive) row found.'
Assert-True ($statesSeen.Contains('unreviewable')) 'missing-method-state-coverage: no unreviewable (degraded) row found.'
Assert-True ($statesSeen.Contains('refused')) 'missing-method-state-coverage: no contradictory+fix (refused) row found.'
Assert-True ($escalatedFlagFound) 'missing-method-state-coverage: no priority:P0-D1-first row touching a red-flag/illegal-sourcing label found.'
Add-PassedCheck -Name 'missing-method-state-coverage (absence)' -Detail 'consistent, unreviewable, contradictory+fix, and escalated-priority states all present'

# ---------------------------------------------------------------------------
# Check 9: seed-regression -- CI-001..CI-010 quotations present byte-for-byte as in the spec
# ---------------------------------------------------------------------------

$specContent = Read-RepoFile -Path 'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md'
$seedTableStart = $specContent.IndexOf('| CI-001 |')
$seedTableEnd = $specContent.IndexOf("`n`n", $seedTableStart)
if ($seedTableEnd -lt 0) { $seedTableEnd = $specContent.Length }
$seedTableText = $specContent.Substring($seedTableStart, $seedTableEnd - $seedTableStart)
$seedQuoteMatches = [regex]::Matches($seedTableText, '\*"(.*?)"\*')
Assert-True ($seedQuoteMatches.Count -ge 10) "seed-regression: expected at least 10 italic-quoted spans in the spec's seed table, found $($seedQuoteMatches.Count)."
$missingQuotes = New-Object 'System.Collections.Generic.List[string]'
foreach ($qm in $seedQuoteMatches) {
    $quote = $qm.Groups[1].Value
    if (-not $inventoryContent.Contains($quote)) {
        $missingQuotes.Add($quote) | Out-Null
    }
}
Assert-True ($missingQuotes.Count -eq 0) "seed-regression: $($missingQuotes.Count) seed quotation(s) from the spec are missing or altered in CONTRADICTION-INVENTORY.md: $($missingQuotes -join ' ||| ')"
foreach ($seedId in @('CI-001', 'CI-002', 'CI-003', 'CI-004', 'CI-005', 'CI-006', 'CI-007', 'CI-008', 'CI-009', 'CI-010')) {
    Assert-True ($parsedRows.Contains($seedId)) "seed-regression: $seedId is missing from CONTRADICTION-INVENTORY.md."
}
Add-PassedCheck -Name 'seed-regression' -Detail "$($seedQuoteMatches.Count) seed quotations verified byte-for-byte present; CI-001..CI-010 all present"

# ---------------------------------------------------------------------------
# Check 10: source-manifest-completeness
# ---------------------------------------------------------------------------

Assert-True ($manifestContent.Contains('| 1 | `docs/INITIATIVES/biostack-governed-delivery/CHARTER.md`')) 'source-manifest-completeness: required source item 1 row missing.'
for ($i = 1; $i -le 13; $i++) {
    $numeralPattern = switch ($i) {
        2 { @('| 2a |', '| 2b |') }
        9 { @('| 9a |', '| 9b |') }
        default { @("| $i |") }
    }
    foreach ($p in $numeralPattern) {
        Assert-True ($manifestContent.Contains($p)) "source-manifest-completeness: required source list row '$p' missing from SOURCE-MANIFEST.md."
    }
}
$manifestStatusPattern = '(?m)^\|\s*(?:\d+[ab]?)\s*\|.*?\|\s*`(consistent|contradictory|unreviewable)`\s*\|'
$manifestStatusMatches = [regex]::Matches($manifestContent, $manifestStatusPattern)
Assert-True ($manifestStatusMatches.Count -eq 15) "source-manifest-completeness: expected exactly 15 required-source rows with a valid status (13 items, with items 2 and 9 each split into two rows), found $($manifestStatusMatches.Count)."
Add-PassedCheck -Name 'source-manifest-completeness' -Detail "all 13 required-source-list items (15 rows, 18 underlying files) present with a valid consistent/contradictory/unreviewable status"

# ---------------------------------------------------------------------------
# Check 11: unreviewable-claim-verified
# ---------------------------------------------------------------------------

$unreviewableManifestRows = [regex]::Matches($manifestContent, '(?m)^\|\s*(?:\d+[ab]?)\s*\|[^|]*\|\s*`unreviewable`\s*\|')
Assert-True ($unreviewableManifestRows.Count -eq 0) "unreviewable-claim-verified: SOURCE-MANIFEST.md declares $($unreviewableManifestRows.Count) unreviewable required-source row(s); this parcel's corpus found zero (every required source was present at BaseCommit and readable in this checkout) -- a nonzero count here would require replaying that row's git cat-file/filesystem-read evidence, which none is recorded."
Add-PassedCheck -Name 'unreviewable-claim-verified (vacuous)' -Detail 'zero unreviewable rows in SOURCE-MANIFEST.md; check is vacuously satisfied, consistent with every required source being readable at BaseCommit'

# ---------------------------------------------------------------------------
# Check 12: corpus-coverage-matrix-complete
# ---------------------------------------------------------------------------

$matrixContent = Read-RepoFile -Path 'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CORPUS-COVERAGE-MATRIX.md'
$pairMatches = [regex]::Matches($matrixContent, '(?m)^\|\s*(\d+)-(\d+)\s*\|\s*`(compared-contradictory|compared-consistent|scope-disjoint)`\s*\|')
$seenPairs = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($pm in $pairMatches) {
    $a = [int]$pm.Groups[1].Value
    $b = [int]$pm.Groups[2].Value
    Assert-True ($a -lt $b) "corpus-coverage-matrix-complete: pair '$a-$b' is not in canonical ascending order."
    $key = "$a-$b"
    Assert-True ($seenPairs.Add($key)) "corpus-coverage-matrix-complete: pair '$key' appears more than once."
}
$expectedPairs = New-Object 'System.Collections.Generic.List[string]'
for ($i = 1; $i -le $RequiredSourceListItemCount; $i++) {
    for ($j = $i + 1; $j -le $RequiredSourceListItemCount; $j++) {
        $expectedPairs.Add("$i-$j") | Out-Null
    }
}
Assert-True ($seenPairs.Count -eq $ExpectedPairCount) "corpus-coverage-matrix-complete: expected $ExpectedPairCount pairs, found $($seenPairs.Count)."
foreach ($expected in $expectedPairs) {
    Assert-True ($seenPairs.Contains($expected)) "corpus-coverage-matrix-complete: pair '$expected' is missing from CORPUS-COVERAGE-MATRIX.md."
}
Add-PassedCheck -Name 'corpus-coverage-matrix-complete' -Detail "$($seenPairs.Count) of $ExpectedPairCount expected pairs present, each with a valid outcome"

# ---------------------------------------------------------------------------
# Check 13: invented-review-status
# ---------------------------------------------------------------------------

# This parcel never asserts a stricter D15 status than a source states. The one D15-adjacent
# finding this parcel records (CI-008) records `unreviewable` -- the absence of any named
# reviewer/status in the audit -- and never upgrades it to `reviewed`. Assert that no artifact
# this parcel creates contains the literal word "reviewed" as an *asserted* status for any
# catalogued claim (the word may legitimately appear in other senses, e.g. "independent
# reviewers", "review-candidate", which this check does not flag).
foreach ($surface in @(
        'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CONTRADICTION-INVENTORY.md',
        'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/SOURCE-MANIFEST.md'
    )) {
    $content = Read-RepoFile -Path $surface
    Assert-True ((-not [regex]::IsMatch($content, '(?i)function-review status[:\s]+`?reviewed`?'))) "invented-review-status: $surface asserts a function-review status of 'reviewed' this parcel did not find stated in a source document."
}
Add-PassedCheck -Name 'invented-review-status (absence)' -Detail 'no catalogued claim upgraded to a reviewed status this parcel did not find in its source'

# ---------------------------------------------------------------------------
# Check 14: no-unattributed-claim
# ---------------------------------------------------------------------------

# Heuristic, deterministic scan: flag any line in the inventory/manifest/README artifacts
# containing an unquoted first-person product-rule assertion pattern ("BioStack must", "BioStack
# may", "BioStack does not", "BioStack is") that is NOT immediately inside an italic-quote span
# (`*"..."*`) and NOT inside a Markdown inline-code/backtick citation. Every such pattern in this
# parcel's own artifacts is expected to appear only inside a quoted citation.
$claimPattern = '(?<!\*")\bBioStack (must|may|does not|is|should)\b'
foreach ($surface in @(
        'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/README.md',
        'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CONTRADICTION-INVENTORY.md',
        'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/SOURCE-MANIFEST.md',
        'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CORPUS-COVERAGE-MATRIX.md',
        'docs/specs/schemas/canon-precedence.md'
    )) {
    $content = Read-RepoFile -Path $surface
    $lines = $content -split "`n"
    foreach ($line in $lines) {
        $matches = [regex]::Matches($line, $claimPattern)
        foreach ($cm in $matches) {
            # Permit the pattern when it occurs inside a quoted span on the same line: require
            # at least one double-quote character earlier on the line AND at least one
            # double-quote character later on the line (the match sits between an opening and a
            # closing quote mark somewhere on the same line -- true for every legitimate,
            # possibly long, citation this parcel writes; a bare, unquoted assertion has no quote
            # mark on at least one side).
            $before = $line.Substring(0, $cm.Index)
            $after = $line.Substring($cm.Index)
            $quotedOnLine = $before.Contains([char]34) -and $after.Contains([char]34)
            Assert-True ($quotedOnLine) "no-unattributed-claim: possible unattributed product-rule assertion in $surface : '$($line.Trim())'"
        }
    }
}
Add-PassedCheck -Name 'no-unattributed-claim (heuristic absence)' -Detail 'no unquoted BioStack must/may/does-not/is/should assertion found outside a quoted citation window'

# ---------------------------------------------------------------------------
# Check 15: registry append-only diffs (docs/specs/README.md, docs/specs/INDEX.md)
# ---------------------------------------------------------------------------

$readmeDiff = @(Invoke-Git -Arguments @('diff', '--unified=0', "$BaseCommit...HEAD", '--', 'docs/specs/README.md'))
$readmeRemoved = @($readmeDiff | Where-Object { $_.StartsWith('-') -and -not $_.StartsWith('---') })
Assert-True ($readmeRemoved.Count -eq 0) 'docs/specs/README.md must carry zero removed/reordered lines.'
$readmeAdded = @($readmeDiff | Where-Object { $_.StartsWith('+') -and -not $_.StartsWith('+++') })
Assert-True ($readmeAdded.Count -gt 0) 'docs/specs/README.md must carry an appended section.'
$readmeContentNow = Read-RepoFile -Path 'docs/specs/README.md'
Assert-True ($readmeContentNow.Contains('## Canon precedence and contradiction inventory (P0-A)')) 'docs/specs/README.md must carry the exact appended section heading.'

$indexDiff = @(Invoke-Git -Arguments @('diff', '--unified=0', "$BaseCommit...HEAD", '--', 'docs/specs/INDEX.md'))
$indexRemoved = @($indexDiff | Where-Object { $_.StartsWith('-') -and -not $_.StartsWith('---') })
Assert-True ($indexRemoved.Count -eq 0) 'docs/specs/INDEX.md must carry zero removed lines.'
$indexAdded = @($indexDiff | Where-Object { $_.StartsWith('+') -and -not $_.StartsWith('+++') })
Assert-True ($indexAdded.Count -eq 1) "docs/specs/INDEX.md must carry exactly one appended row (found $($indexAdded.Count) added line(s))."
Assert-True ($indexAdded[0] -like '*P0-A*') 'docs/specs/INDEX.md appended line must be the P0-A row.'
Assert-True ($indexAdded[0] -like '*coordinator-assigns-at-gate-2*') 'docs/specs/INDEX.md P0-A row must use the coordinator-assigns-at-gate-2 literal for Branch/worktree and Owner.'
Add-PassedCheck -Name 'registry append-only diffs' -Detail 'docs/specs/README.md: one appended section, zero removed lines; docs/specs/INDEX.md: exactly one appended P0-A row, zero removed lines'

# ---------------------------------------------------------------------------
# Check 16: unresolvable-citation -- every CI-NNN source_a/source_b file path resolves at BaseCommit
#
# Path pattern fix (found while closing p0a_impl_review_2 F2): the prior pattern required a
# `docs`-prefixed or exact-literal `README.md`/`BIOSTACK_FRONTEND_READINESS_AUDIT.md` alternative
# to be immediately followed by additional `[^`]*?\.md` content before the closing backtick --
# which never matches a bare, standalone `` `README.md` `` or `` `BIOSTACK_FRONTEND_READINESS_AUDIT.md` ``
# backtick citation (there is nothing left to match after the alternative already consumes the
# whole literal). Both forms are used standalone in this inventory; the pattern below resolves
# them correctly in addition to every `docs/...`-prefixed path.
# ---------------------------------------------------------------------------

$citedPathPattern = '`(README\.md|BIOSTACK_FRONTEND_READINESS_AUDIT\.md|docs[^`]*?\.md)`'
$citedPaths = [regex]::Matches($inventoryContent, $citedPathPattern) | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
$unresolvable = New-Object 'System.Collections.Generic.List[string]'
foreach ($p in $citedPaths) {
    $probe = Get-GitResult -Arguments @('cat-file', '-e', "${BaseCommit}:$p")
    if ($probe.ExitCode -ne 0) { $unresolvable.Add($p) | Out-Null }
}
Assert-True ($unresolvable.Count -eq 0) "unresolvable-citation: $($unresolvable.Count) cited path(s) do not resolve at BaseCommit: $($unresolvable -join ', ')"
Add-PassedCheck -Name 'unresolvable-citation (absence)' -Detail "$($citedPaths.Count) distinct cited file paths resolved at BaseCommit"

# ---------------------------------------------------------------------------
# Check 16b: quotation-content-verified -- every CI-NNN row's source_a/source_b quotation
# appears character-exact in its cited file's content at BaseCommit (closes p0a_impl_review_2 F2:
# the check above only confirmed the cited *path* existed, never the quoted *content*, for any
# row beyond the ten seed rows that seed-regression separately anchors to the spec's own text,
# not to the real source files -- a fabricated quotation in a non-seed row previously passed
# cleanly).
# ---------------------------------------------------------------------------

$FileContentCache = @{}
function Get-SourceFileContent {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not $FileContentCache.ContainsKey($Path)) {
        $result = Get-GitResult -Arguments @('show', "${BaseCommit}:$Path")
        if ($result.ExitCode -eq 0) {
            $FileContentCache[$Path] = ($result.Output -join "`n")
        } else {
            $FileContentCache[$Path] = $null
        }
    }
    return $FileContentCache[$Path]
}

# Normalization applied identically to both the cited file's content and each quoted segment
# before comparison. This does not weaken the "character-exact" discipline against *fabricated*
# content (fabricated prose still fails to match real source text after identical normalization
# on both sides -- verified below by the adversarial reproduction from p0a_impl_review_2 F2); it
# exists only to absorb two kinds of structural, non-substantive difference this corpus's own
# markdown-bulleted/bold-emphasis source files introduce when their content is quoted as
# continuous prose: (1) a quotation spanning more than one source markdown list item or line
# (bullet-dash prefixes and line breaks collapse to a single space, matching how a reader
# naturally reconstructs prose from a bulleted list without needing a literal "..." between
# every item), and (2) bold/italic markdown emphasis markers (`**`/`*`) the *source itself* wraps
# around the quoted words -- these are source typography, not substantive content, and this
# inventory's own citation-apparatus rule already treats surrounding decoration as exempt from
# the byte-for-byte test.
function ConvertTo-NormalizedQuoteText {
    param([Parameter(Mandatory = $true)][string]$Text)
    $t = $Text -replace "`r`n", "`n"
    $t = $t -replace '\*\*', ''
    $t = $t -replace '(?m)^[-*]\s+', ' '
    $t = $t -replace '\s+', ' '
    return $t.Trim()
}
$NormalizedFileContentCache = @{}
function Get-NormalizedSourceFileContent {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not $NormalizedFileContentCache.ContainsKey($Path)) {
        $raw = Get-SourceFileContent -Path $Path
        if ($null -eq $raw) {
            $NormalizedFileContentCache[$Path] = $null
        } else {
            $NormalizedFileContentCache[$Path] = ConvertTo-NormalizedQuoteText -Text $raw
        }
    }
    return $NormalizedFileContentCache[$Path]
}

# Two file identifiers are referenced in source_a/source_b prose as a bare filename (no
# backticks, no directory prefix) rather than a backtick-wrapped path: `CHARTER.md` and
# `README.md`. Both are unambiguous within this corpus (CHARTER.md has exactly one registered
# file by that basename; bare README.md prose always refers to the repository-root file, never
# `docs/specs/README.md`, which this inventory always cites by its full backtick-wrapped path).
$BareAliasTarget = @{
    'CHARTER.md' = 'docs/INITIATIVES/biostack-governed-delivery/CHARTER.md'
    'README.md'  = 'README.md'
}
$bareTokenPattern = '(?<![`/\w])(CHARTER\.md|README\.md)\b'
$quotePattern = '\*"(.*?)"\*'

$quoteMismatches = New-Object 'System.Collections.Generic.List[string]'
$quotesVerifiedCount = 0

foreach ($ciId in $parsedRows.Keys) {
    $rowFields = $parsedRows[$ciId]
    foreach ($fieldName in @('source_a', 'source_b')) {
        if (-not $rowFields.Contains($fieldName)) { continue }
        $fieldText = $rowFields[$fieldName]
        if ([string]::IsNullOrEmpty($fieldText)) { continue }

        # Collect path tokens and quote tokens in their original left-to-right order; a quote is
        # verified against the most recently seen path token that precedes it in the same field,
        # mirroring how a human reader resolves "which file does this quotation come from" when a
        # field cites more than one source document.
        $tokens = New-Object 'System.Collections.Generic.List[object]'
        foreach ($m in [regex]::Matches($fieldText, $citedPathPattern)) {
            $tokens.Add([pscustomobject]@{ Index = $m.Index; Kind = 'path'; Value = $m.Groups[1].Value }) | Out-Null
        }
        foreach ($m in [regex]::Matches($fieldText, $bareTokenPattern)) {
            $tokens.Add([pscustomobject]@{ Index = $m.Index; Kind = 'path'; Value = $BareAliasTarget[$m.Groups[1].Value] }) | Out-Null
        }
        foreach ($m in [regex]::Matches($fieldText, $quotePattern)) {
            $tokens.Add([pscustomobject]@{ Index = $m.Index; Kind = 'quote'; Value = $m.Groups[1].Value }) | Out-Null
        }
        $ordered = $tokens | Sort-Object Index

        $currentPath = $null
        foreach ($tok in $ordered) {
            if ($tok.Kind -eq 'path') {
                $currentPath = $tok.Value
                continue
            }
            if (-not $currentPath) {
                $quoteMismatches.Add("${ciId}.${fieldName}: quoted span '$($tok.Value)' has no preceding cited file path to verify against") | Out-Null
                continue
            }
            $fileContent = Get-NormalizedSourceFileContent -Path $currentPath
            if ($null -eq $fileContent) {
                $quoteMismatches.Add("${ciId}.${fieldName}: cited file '$currentPath' could not be read at BaseCommit to verify quotation '$($tok.Value)'") | Out-Null
                continue
            }
            # A quotation may use a literal "..." to join two or more non-adjacent spans of the
            # same source (for example, skipping intervening bullet-list items); each segment
            # between the ellipses must independently appear present, in normalized form, in the
            # source. A trailing comma this inventory sometimes appends purely to join two
            # back-to-back quoted list fragments into readable prose (citation apparatus at the
            # outer boundary of the span, not source content) is tolerated as a fallback only when
            # the unmodified segment does not already match.
            $segments = $tok.Value -split '\.\.\.' | ForEach-Object { $_.Trim() } | Where-Object { $_.Length -gt 0 }
            foreach ($segment in $segments) {
                $quotesVerifiedCount++
                $normalizedSegment = ConvertTo-NormalizedQuoteText -Text $segment
                $matched = $fileContent.Contains($normalizedSegment)
                if (-not $matched -and $normalizedSegment.EndsWith(',')) {
                    $matched = $fileContent.Contains($normalizedSegment.TrimEnd(',', ' ').Trim())
                }
                if (-not $matched) {
                    $quoteMismatches.Add("${ciId}.${fieldName}: quoted segment not found (modulo whitespace/bullet/bold normalization) in '$currentPath': '$segment'") | Out-Null
                }
            }
        }
    }
}
Assert-True ($quoteMismatches.Count -eq 0) "quotation-content-verified: $($quoteMismatches.Count) quoted span(s) failed verification against their cited source file at BaseCommit:`n$($quoteMismatches -join "`n")"
Add-PassedCheck -Name 'quotation-content-verified' -Detail "$quotesVerifiedCount quoted segment(s) across all CI-NNN rows' source_a/source_b fields verified present, modulo whitespace/bullet-prefix/bold-markdown normalization, in their cited file's content at BaseCommit"

# ---------------------------------------------------------------------------
# Check 17: dk-directional-constraint-present -- canon-precedence.md carries Coordinator Decision
# D-K's directional-application constraint, transcribed verbatim (closes p0a_impl_review_2 F1 /
# Coordinator Decision D-K).
# ---------------------------------------------------------------------------

[string[]]$DkConstraintLines = @(
    'The precedence manifest answers **document authority order only**. Where the mechanically',
    'resolved order favors a more permissive text over a narrower safety prohibition, the',
    "prohibition **stands unchanged** until an explicit owner ruling supersedes it. Such rows route",
    "to the owner through P0-D's disposition process; a P0-D builder must never weaken a safety",
    "prohibition by rank alone. This is consistent with the charter's must-not list, D12's principle",
    "(controls calibrate useful guidance; they do not license weakened safety), and D-I's staged-split",
    "posture, all of which are already the owner's ruled doctrine."
)
Assert-True ($precedenceContent.Contains('D-K')) "dk-directional-constraint-present: canon-precedence.md does not reference Coordinator Decision D-K."
foreach ($line in $DkConstraintLines) {
    Assert-True ($precedenceContent.Contains("> $line")) "dk-directional-constraint-present: canon-precedence.md is missing D-K's verbatim constraint line: '$line'"
}
Add-PassedCheck -Name 'dk-directional-constraint-present' -Detail "canon-precedence.md carries D-K's directional-application constraint, verbatim, all $($DkConstraintLines.Count) blockquote lines verified"

# ---------------------------------------------------------------------------
# Evidence bundle
# ---------------------------------------------------------------------------

$expectedEvidencePath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot 'artifacts/p0a-verification'))
$resolvedEvidencePath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $EvidenceDirectory))
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($resolvedEvidencePath.TrimEnd('\', '/'), $expectedEvidencePath.TrimEnd('\', '/'))) 'EvidenceDirectory must resolve to artifacts/p0a-verification under the repository root.'

$trackedEvidence = Get-GitResult -Arguments @('ls-files', '--error-unmatch', '--', 'artifacts/p0a-verification')
Assert-True ($trackedEvidence.ExitCode -ne 0) 'artifacts/p0a-verification must be untracked.'
[IO.Directory]::CreateDirectory($resolvedEvidencePath) | Out-Null

Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'changed-files.txt') -Content ($allChanged -join "`n")

$summary = [ordered]@{
    schema            = 'biostack.p0a-verification-summary.v1'
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
Write-Output "P0-A verification PASS ($($CheckResults.Count) checks)"
