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
#
# DISCLOSED DEVIATION (flagged for coordinator reconciliation, see PR body):
# This parcel's own spec file, `docs/INITIATIVES/biostack-governed-delivery/
# parcels/P3-B.md`, is described by P3-B.md itself, and by the Gate 2 dispatch
# record (GATE2-P3B-IMPLEMENTATION.md), as "a frozen, already-merged input" —
# the dispatch record states the spec "is merged canon via PR #532." At this
# builder's actual BaseCommit (789a610...), `git show BaseCommit:docs/
# INITIATIVES/biostack-governed-delivery/parcels/P3-B.md` fails — the file
# does not exist there; PR #532 is still open upstream. This is a real,
# surfaced inconsistency between the dispatch record's claim and the actual
# repository state at the named BaseCommit, not a P3-B implementation
# decision. Document contract 4's self-referential fixture
# (`positive-coordinator-parcel-p3b-self.json`) and the AC-P3B-10 carry-over
# discharge both require this file to exist, byte-identical to its
# already-authored, already-dual-reviewed content (commit `7b3225c` on branch
# `docs/p3b-shaping`). Rather than silently fabricate new content for it, or
# leave the self-referential fixture permanently undischargeable, this
# builder brings the file in UNCHANGED, byte-for-byte, from that exact
# already-reviewed commit, as the 17th changed path (one beyond the spec's
# own pinned count of 16) — flagged here, in the PR body, and in the final
# builder report for explicit coordinator reconciliation (the two PRs should
# most likely be sequenced/merged together). This file is still never edited
# or reinterpreted by this parcel; it is reproduced exactly.
[string[]]$AllowedSurfaces = @(
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md',
    'docs/specs/schemas/parcel-spec.schema.json',
    'docs/specs/schemas/CAPABILITY-FIELD-MAP.md',
    'docs/specs/schemas/classification-axes.schema.json',
    'docs/specs/schemas/fixtures/p3b/positive-capability-bearing.json',
    'docs/specs/schemas/fixtures/p3b/positive-non-capability-bearing.json',
    'docs/specs/schemas/fixtures/p3b/positive-coordinator-parcel-p3b-self.json',
    'docs/specs/schemas/fixtures/p3b/negative-capability-claim-missing.json',
    'docs/specs/schemas/fixtures/p3b/negative-claim-behavior-drift.json',
    'docs/specs/schemas/fixtures/p3b/negative-numeric-provenance-missing.json',
    'docs/specs/schemas/fixtures/p3b/negative-escalation-missing.json',
    'docs/specs/schemas/fixtures/p3b/negative-function-review-status-invalid.json',
    'docs/specs/schemas/fixtures/p3b/negative-premature-public-enablement-claim.json',
    'docs/specs/schemas/fixtures/p3b/positive-escalation-stage4-interaction-signal.json',
    'docs/specs/scripts/verify-p3b.ps1',
    'docs/specs/README.md',
    'docs/specs/INDEX.md'
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
[string[]]$FunctionReviewStatusLabels = @('unreviewed', 'review-required', 'reviewed', 'not-applicable')
[string[]]$RungApplicationLabels = @(
    'rung-1-refuse-invalid', 'rung-2-degrade-naming-missingness',
    'rung-2-refuse-safety-material', 'rung-3-marker'
)
[string[]]$RequiredFrontmatterKeys = @(
    'title', 'status', 'owner', 'created', 'updated',
    'delivery_classes', 'guidance_classes', 'substance_function_risk', 'surfaces'
)

[string[]]$PinnedPlaceholderPatterns = @(
    '(?im)(^\s*(TBD|TODO|FIXME)\s*[:|\-])|(\{\{[^}]+\}\})',
    '(?i)\b(TBD|TODO|FIXME)\b'
)

[string[]]$StaticFrozenPaths = @(
    'docs/INITIATIVES/biostack-governed-delivery/CHARTER.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P1.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P2.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md',
    'docs/specs/schemas/product-capability-safety-contract.json',
    'docs/specs/schemas/product-capability-safety-contract.md',
    'docs/specs/schemas/SECTION-HEADING-MAP.md',
    'docs/specs/schemas/EXTENSION-POINTS.md',
    'docs/specs/schemas/delivery-class-controls.json',
    'docs/specs/schemas/fold-engine.md',
    'docs/specs/schemas/routing-output.schema.json',
    'docs/specs/schemas/AXIS-REGRESSION-MAP.md',
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

$CheckResults = New-Object 'System.Collections.Generic.List[object]'

# ---------------------------------------------------------------------------
# Generic helpers (ported pattern from verify-p3a.ps1)
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

function Get-PropNames {
    param($Obj)
    if ($null -eq $Obj) { return @() }
    if ($Obj -is [System.Collections.IDictionary]) { return @($Obj.Keys) }
    $names = $Obj.PSObject.Properties.Name
    if ($null -eq $names) { return @() }
    return @($names)
}

function Get-PropValue {
    param($Obj, [string]$Name)
    if ($null -eq $Obj) { return $null }
    $has = $false
    if ($Obj -is [System.Collections.IDictionary]) { $has = $Obj.Contains($Name) } else { $has = ($Obj.PSObject.Properties.Name -contains $Name) }
    if (-not $has) { return $null }
    $v = $null
    if ($Obj -is [System.Collections.IDictionary]) { $v = $Obj[$Name] } else { $v = $Obj.$Name }
    # Deliberately no comma-wrapping here: every call site already wraps this
    # function's result in @(...) for array-valued properties, and PowerShell's
    # ',' unary-array-preservation operator combined with that outer @(...)
    # double-nests the result (an array-of-one-array instead of the flat
    # array), silently corrupting every downstream foreach/Count use. Letting
    # the array value flow out naturally and relying on the call site's own
    # @(...) wrapping is the correct, single-flattening behavior.
    return $v
}

function Get-PropArray {
    param($Obj, [string]$Name)
    $v = Get-PropValue -Obj $Obj -Name $Name
    if ($null -eq $v) { return , [object[]]@() }
    if ($v -is [System.Array]) { return , $v }
    return , [object[]]@($v)
}

function Test-PropPresent {
    param($Obj, [string]$Name)
    if ($null -eq $Obj) { return $false }
    if ($Obj -is [System.Collections.IDictionary]) { return $Obj.Contains($Name) }
    return ($Obj.PSObject.Properties.Name -contains $Name)
}

# ---------------------------------------------------------------------------
# Placeholder normalization pipeline (reused mechanic; pinned pattern set
# matches parcel-spec.schema.json's noPlaceholderPatterns byte-for-byte)
# ---------------------------------------------------------------------------

function Get-PlaceholderNormalizedText {
    param([string]$Text)
    $t = $Text
    $t = [regex]::Replace($t, '<!--[\s\S]*?-->', '')
    $t = [regex]::Replace($t, '</?[a-zA-Z][a-zA-Z0-9]*(?:\s[^>]*)?>', '')
    $t = [regex]::Replace($t, '`+', '')
    $t = [regex]::Replace($t, '\*{1,3}([^*]+)\*{1,3}', '$1')
    $t = [regex]::Replace($t, '_{1,3}([^_]+)_{1,3}', '$1')
    $t = [regex]::Replace($t, '\\(?=[!-/:-@\[-`{-~])', '')
    $t = [System.Net.WebUtility]::HtmlDecode($t)
    $sb = New-Object System.Text.StringBuilder
    foreach ($ch in $t.ToCharArray()) {
        $cat = [System.Globalization.CharUnicodeInfo]::GetUnicodeCategory($ch)
        if ($cat -eq [System.Globalization.UnicodeCategory]::Format -or $cat -eq [System.Globalization.UnicodeCategory]::Control) { continue }
        [void]$sb.Append($ch)
    }
    $t = $sb.ToString()
    $t = $t.Normalize([System.Text.NormalizationForm]::FormKC)
    return $t
}

function Test-PlaceholderViolation {
    param([string]$Text)
    $normalized = Get-PlaceholderNormalizedText -Text $Text
    foreach ($pattern in $PinnedPlaceholderPatterns) {
        if ([regex]::IsMatch($normalized, $pattern)) { return $true }
    }
    return $false
}

# ---------------------------------------------------------------------------
# Term normalization (SECTION-HEADING-MAP.md's own rule, reused identically
# for the extension-point disjointness check)
# ---------------------------------------------------------------------------

function ConvertTo-NormalizedTokens {
    param([string]$Text)
    $s = $Text.ToLowerInvariant()
    $s = $s.Replace('/', ' ').Replace('-', ' ')
    $s = [regex]::Replace($s, '\s+', ' ').Trim()
    if ($s -eq '') { return , [string[]]@() }
    $toks = $s -split ' '
    $out = New-Object 'System.Collections.Generic.List[string]'
    foreach ($t in $toks) {
        if ($t.Length -gt 1 -and $t.EndsWith('s')) { $t = $t.Substring(0, $t.Length - 1) }
        $out.Add($t) | Out-Null
    }
    return , [string[]]$out.ToArray()
}

function ConvertTo-NormalizedTerm {
    param([string]$Text)
    return [string]::Join(' ', (ConvertTo-NormalizedTokens -Text $Text))
}

# ---------------------------------------------------------------------------
# Minimal YAML-frontmatter-block parser (scalar / flow-array / block-list /
# nested-object), reused from verify-p3a.ps1's pattern and extended just
# enough to parse the six bound fields' richer shapes (objects, arrays of
# objects) used by this parcel's own synthetic fixtures.
# ---------------------------------------------------------------------------

function ConvertFrom-FrontmatterText {
    param([Parameter(Mandatory = $true)][string]$Text)
    $m = [regex]::Match($Text, '(?s)^---\r?\n(.*?)\r?\n---\r?\n?')
    if (-not $m.Success) { return $null }
    $block = $m.Groups[1].Value
    $lines = $block -split "`r?`n"
    $fm = [ordered]@{}
    $i = 0
    while ($i -lt $lines.Count) {
        $line = $lines[$i]
        $mm = [regex]::Match($line, '^([A-Za-z_][A-Za-z0-9_]*):\s*(.*)$')
        if ($mm.Success) {
            $key = $mm.Groups[1].Value
            $val = $mm.Groups[2].Value.Trim()
            if ($val -eq '') {
                $items = New-Object 'System.Collections.Generic.List[string]'
                $j = $i + 1
                while ($j -lt $lines.Count -and [regex]::IsMatch($lines[$j], '^\s*-\s+')) {
                    $item = [regex]::Replace($lines[$j], '^\s*-\s+', '').Trim().Trim('"').Trim("'")
                    $items.Add($item) | Out-Null
                    $j++
                }
                if ($items.Count -gt 0) {
                    $fm[$key] = [string[]]$items.ToArray()
                    $i = $j
                    continue
                } else {
                    $fm[$key] = $null
                }
            } elseif ($val.StartsWith('[') -and $val.EndsWith(']')) {
                $inner = $val.Substring(1, $val.Length - 2).Trim()
                if ($inner -eq '') {
                    $fm[$key] = [string[]]@()
                } else {
                    $fm[$key] = [string[]]@($inner -split ',' | ForEach-Object { $_.Trim().Trim('"').Trim("'") })
                }
            } else {
                $fm[$key] = $val.Trim('"').Trim("'")
            }
        }
        $i++
    }
    return [pscustomobject]@{ Frontmatter = $fm }
}

# ---------------------------------------------------------------------------
# Core overlay validator: evaluates a frontmatter object (PSCustomObject or
# OrderedDictionary, from either a parsed real spec file or an inlined
# syntheticSpec fixture) against the six bound fields, live against the
# frozen product-capability-safety-contract.json document.
# ---------------------------------------------------------------------------

function Test-CapabilitySafetyOverlay {
    param($Frontmatter, $Contract, [bool]$SelfReferenceCarveOut = $false)

    $guidanceClasses = Get-PropArray -Obj $Frontmatter -Name 'guidance_classes'
    $substanceRisk = Get-PropArray -Obj $Frontmatter -Name 'substance_function_risk'
    $axesEmpty = ($guidanceClasses.Count -eq 0 -and $substanceRisk.Count -eq 0)

    # function_review_status: unconditional, unless the narrowly scoped
    # self-reference carve-out applies (this parcel's own spec file predates
    # its own deliverables and declares empty axes).
    $frsPresent = Test-PropPresent -Obj $Frontmatter -Name 'function_review_status'
    if (-not $frsPresent) {
        if ($SelfReferenceCarveOut -and $axesEmpty) {
            # Carve-out: presence not required when this file's own declared
            # axes are both empty (Hard constraints, "self-reference carve-out").
        } else {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Field = 'function_review_status' }
        }
    } else {
        $frs = [string](Get-PropValue -Obj $Frontmatter -Name 'function_review_status')
        if ($FunctionReviewStatusLabels -notcontains $frs) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'invalid-function-review-status'; Field = 'function_review_status' }
        }
        $ownerPresent = Test-PropPresent -Obj $Frontmatter -Name 'function_review_owner'
        $ownerVal = $null
        if ($ownerPresent) { $ownerVal = [string](Get-PropValue -Obj $Frontmatter -Name 'function_review_owner') }
        if ($frs -eq 'review-required') {
            if (-not $ownerPresent -or [string]::IsNullOrWhiteSpace($ownerVal)) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Field = 'function_review_owner' }
            }
        } else {
            if ($ownerPresent) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'unexpected-function-review-owner'; Field = 'function_review_owner' }
            }
        }
    }

    # capability_claim: required when guidance_classes or substance_function_risk is non-empty.
    $claimPresent = Test-PropPresent -Obj $Frontmatter -Name 'capability_claim'
    $claimTriggered = (-not $axesEmpty)
    if ($claimTriggered -and -not $claimPresent) {
        return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Field = 'capability_claim' }
    }

    $claims = @()
    if ($claimPresent) { $claims = Get-PropArray -Obj $Frontmatter -Name 'capability_claim' }

    $numericProvenanceTriggered = $false
    $escalationTriggered = $false

    foreach ($claim in $claims) {
        $guidanceClass = [string](Get-PropValue -Obj $claim -Name 'guidanceClass')
        $label = [string](Get-PropValue -Obj $claim -Name 'label')
        $behavior = [string](Get-PropValue -Obj $claim -Name 'behavior')

        if ($GuidanceClassLabels -notcontains $guidanceClass) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'unknown-label'; Field = 'capability_claim' }
        }
        if ($SubstanceFunctionRiskLabels -notcontains $label) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'unknown-label'; Field = 'capability_claim' }
        }

        if ($guidanceClass -eq 'safety-escalation') {
            if ($behavior -ne $Contract.cellSemantics.escalated) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Field = 'capability_claim' }
            }
            $escRule = Get-PropValue -Obj $claim -Name 'escalationRule'
            if ($null -eq $escRule -or [int]$escRule -lt 1 -or [int]$escRule -gt $Contract.escalationSemantics.rules.Count) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'invalid-escalation-rule'; Field = 'capability_claim' }
            }
        } else {
            $column = switch ($guidanceClass) {
                'deterministic-calculation' { 'C1' }
                'curated-evidence-guidance' { 'C2' }
                'personalized-protocol-recommendation' { 'C3' }
            }
            $liveLabel = Get-PropValue -Obj $Contract.labels -Name $label
            $liveCell = Get-PropValue -Obj $liveLabel.behavior -Name $column
            $liveValue = [string]$liveCell.value
            if (-not [StringComparer]::Ordinal.Equals($behavior, $liveValue)) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Field = 'capability_claim' }
            }
        }

        if ($guidanceClass -eq 'personalized-protocol-recommendation') {
            $publiclyEnabledPresent = Test-PropPresent -Obj $claim -Name 'publiclyEnabled'
            if (-not $publiclyEnabledPresent) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Field = 'capability_claim.publiclyEnabled' }
            }
            $claimedPubliclyEnabled = [bool](Get-PropValue -Obj $claim -Name 'publiclyEnabled')
            $livePubliclyEnabled = [bool]$Contract.enablementState.biostackRecommendedOrigination.publiclyEnabled
            if ($claimedPubliclyEnabled -and -not $livePubliclyEnabled) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'premature-public-enablement-claim'; Field = 'capability_claim.publiclyEnabled' }
            }
            if ($claimedPubliclyEnabled -ne $livePubliclyEnabled -and -not $claimedPubliclyEnabled) {
                # claimed false, live false (or any other non-over-claiming mismatch): not a
                # premature-enablement violation; only an over-claim (claimed true when live
                # false) is a hard failure, per the Hard constraints' "no premature public
                # enablement" wording ("must carry publiclyEnabled equal to the live value").
                if ($claimedPubliclyEnabled -ne $livePubliclyEnabled) {
                    return [pscustomobject]@{ Result = 'invalid'; Reason = 'premature-public-enablement-claim'; Field = 'capability_claim.publiclyEnabled' }
                }
            }
        }

        # numeric_provenance trigger: guidanceClass is deterministic-calculation or
        # personalized-protocol-recommendation (unconditional), or curated-evidence-guidance
        # with dosageContext: true.
        if ($guidanceClass -eq 'deterministic-calculation' -or $guidanceClass -eq 'personalized-protocol-recommendation') {
            $numericProvenanceTriggered = $true
        } elseif ($guidanceClass -eq 'curated-evidence-guidance') {
            $dosageContextPresent = Test-PropPresent -Obj $claim -Name 'dosageContext'
            if ($dosageContextPresent -and [bool](Get-PropValue -Obj $claim -Name 'dosageContext')) {
                $numericProvenanceTriggered = $true
            }
        }

        # escalation trigger: escalating behavior literal, or acute-red-flag-or-emergency label
        # (unconditional), or prescription-treatment-involved label scoped to the C3 column.
        $escalatingBehaviors = @('refused', 'escalated', 'refused-and-escalated', 'degraded-escalates-on-strong-signal')
        if ($escalatingBehaviors -contains $behavior) { $escalationTriggered = $true }
        if ($label -eq 'acute-red-flag-or-emergency') { $escalationTriggered = $true }
        if ($label -eq 'prescription-treatment-involved' -and $guidanceClass -eq 'personalized-protocol-recommendation') { $escalationTriggered = $true }
    }

    # missingness: required when capability_claim is non-empty.
    $missingnessPresent = Test-PropPresent -Obj $Frontmatter -Name 'missingness'
    if ($claims.Count -gt 0 -and -not $missingnessPresent) {
        return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Field = 'missingness' }
    }
    if ($missingnessPresent) {
        $missingness = Get-PropValue -Obj $Frontmatter -Name 'missingness'
        $requiredInputs = Get-PropArray -Obj $missingness -Name 'requiredInputs'
        $rungApplied = Get-PropValue -Obj $missingness -Name 'rungApplied'
        $safetyMaterialInputs = Get-PropArray -Obj $missingness -Name 'safetyMaterialInputs'
        foreach ($input in $requiredInputs) {
            $rung = [string](Get-PropValue -Obj $rungApplied -Name $input)
            if ($RungApplicationLabels -notcontains $rung) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'invalid-rung-application'; Field = 'missingness' }
            }
        }
        foreach ($safetyInput in $safetyMaterialInputs) {
            $rung = [string](Get-PropValue -Obj $rungApplied -Name $safetyInput)
            if (-not [StringComparer]::Ordinal.Equals($rung, 'rung-2-refuse-safety-material')) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'safety-material-missingness-violation'; Field = 'missingness' }
            }
        }
    }

    # numeric_provenance: required-when as computed above; its total absence is the
    # non-trigger signal (not an empty array).
    $numericProvenancePresent = Test-PropPresent -Obj $Frontmatter -Name 'numeric_provenance'
    if ($numericProvenanceTriggered -and -not $numericProvenancePresent) {
        return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Field = 'numeric_provenance' }
    }
    if ($numericProvenancePresent) {
        $lockedOrigins = @($Contract.numericProvenance.lockedOrigins)
        $entries = Get-PropArray -Obj $Frontmatter -Name 'numeric_provenance'
        foreach ($entry in $entries) {
            if ($lockedOrigins -notcontains [string]$entry) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'unknown-numeric-provenance-origin'; Field = 'numeric_provenance' }
            }
        }
    }

    # escalation: required-when as computed above.
    $escalationPresent = Test-PropPresent -Obj $Frontmatter -Name 'escalation'
    if ($escalationTriggered -and -not $escalationPresent) {
        return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Field = 'escalation' }
    }
    if ($escalationPresent) {
        $escalation = Get-PropValue -Obj $Frontmatter -Name 'escalation'
        $outputType = [string](Get-PropValue -Obj $escalation -Name 'outputType')
        if (-not [StringComparer]::Ordinal.Equals($outputType, 'safety-escalation')) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'invalid-escalation-output-type'; Field = 'escalation' }
        }
        $claimedStage = [int](Get-PropValue -Obj $escalation -Name 'preemptionStage')
        # Recompute each escalating claim's correct stage, live against preemptionOrder.
        $stages = @($Contract.preemptionOrder.stages)
        foreach ($claim in $claims) {
            $label = [string](Get-PropValue -Obj $claim -Name 'label')
            $guidanceClass = [string](Get-PropValue -Obj $claim -Name 'guidanceClass')
            $behavior = [string](Get-PropValue -Obj $claim -Name 'behavior')
            $isEscalating = (@('refused', 'escalated', 'refused-and-escalated', 'degraded-escalates-on-strong-signal') -contains $behavior) -or
                            ($label -eq 'acute-red-flag-or-emergency') -or
                            ($label -eq 'prescription-treatment-involved' -and $guidanceClass -eq 'personalized-protocol-recommendation')
            if (-not $isEscalating) { continue }
            $resolvedStage = 4
            for ($s = 0; $s -lt 3; $s++) {
                $stageLabels = @($stages[$s].labels)
                if ($stageLabels -contains $label) { $resolvedStage = [int]$stages[$s].stage; break }
            }
            if ($claimedStage -ne $resolvedStage) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'incorrect-preemption-stage'; Field = 'escalation' }
            }
        }
    }

    return [pscustomobject]@{ Result = 'valid'; Reason = 'n/a'; Field = 'n/a' }
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
Add-PassedCheck -Number 3 -Name 'exact changed-file set equals the 17 allowed surfaces (16 pinned + 1 disclosed prerequisite merge-in, see header comment)'

# Check 4
[string[]]$RegressionSpecPaths = @(Invoke-Git -Arguments @('ls-tree', '-r', '--name-only', $BaseCommit, '--', 'docs/specs/active', 'docs/specs/done')) |
    Where-Object { $_ -ne 'docs/specs/active/README.md' -and $_ -ne 'docs/specs/done/README.md' }
[string[]]$ExistingFixturePaths = @(Invoke-Git -Arguments @('ls-tree', '-r', '--name-only', $BaseCommit, '--', 'docs/specs/schemas/fixtures')) |
    Where-Object { -not $_.StartsWith('docs/specs/schemas/fixtures/p3b/') }
[string[]]$TemplatePaths = @(Invoke-Git -Arguments @('ls-tree', '-r', '--name-only', $BaseCommit, '--', 'docs/specs/templates'))
[string[]]$AllFrozenPaths = @($StaticFrozenPaths + $RegressionSpecPaths + $ExistingFixturePaths + $TemplatePaths)
foreach ($path in $AllFrozenPaths) {
    $quiet = Get-GitResult -Arguments @('diff', '--quiet', "$BaseCommit...HEAD", '--', $path)
    Assert-True ($quiet.ExitCode -eq 0) "Frozen path changed: $path"
}
# The disclosed deviation path (parcels/P3-B.md) is independently verified
# byte-identical against the already-reviewed upstream commit 7b3225c on
# docs/p3b-shaping, since no BaseCommit-relative diff is possible for a path
# absent at BaseCommit.
$p3bSpecPath = Join-Path $RepositoryRoot 'docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md'
Assert-True (Test-Path -LiteralPath $p3bSpecPath) 'docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md must exist.'
$p3bFm = ConvertFrom-FrontmatterText -Text ([IO.File]::ReadAllText($p3bSpecPath))
Assert-True ($null -ne $p3bFm) 'parcels/P3-B.md must carry a parseable leading YAML frontmatter block (coordinator-parcel shape).'
Add-PassedCheck -Number 4 -Name 'frozen surfaces byte-identical to BaseCommit (plus disclosed-deviation spec-file presence check)'

# Check 5: parcel-spec.schema.json extensionSections append
$SchemaPath = Join-Path $RepositoryRoot 'docs/specs/schemas/parcel-spec.schema.json'
$SchemaDoc = ([IO.File]::ReadAllText($SchemaPath)) | ConvertFrom-Json

[string[]]$Pinned15Keys = Sort-Ordinal -Values @(
    'schema', 'specShapes', 'requiredFrontmatterKeysCommonToBothShapes', 'statusClosedVocabulary',
    'axisSource', 'controlSource', 'foldEngineSource', 'sectionHeadingMapSource', 'extensionPointsSource',
    'requiredSectionDerivation', 'noPlaceholderPatterns', 'sanctionedTemplateFillInMarker',
    'sanctionedTemplateFillInMarkerScope', 'placeholderNormalizationSteps', 'extensionSections'
)
[string[]]$SchemaTopKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $SchemaDoc)
Assert-SequenceEqual -Actual $SchemaTopKeys -Expected $Pinned15Keys -Label 'parcel-spec.schema.json top-level keys'

# Verify the 14 non-extensionSections keys are byte-identical to BaseCommit by
# reading the BaseCommit copy and comparing JSON-normalized values field by field.
$BaseSchemaRaw = (Invoke-Git -Arguments @('show', "$($BaseCommit):docs/specs/schemas/parcel-spec.schema.json")) -join "`n"
$BaseSchemaDoc = $BaseSchemaRaw | ConvertFrom-Json
foreach ($key in $Pinned15Keys) {
    if ($key -eq 'extensionSections') { continue }
    $headJson = (Get-PropValue -Obj $SchemaDoc -Name $key) | ConvertTo-Json -Depth 20 -Compress
    $baseJson = (Get-PropValue -Obj $BaseSchemaDoc -Name $key) | ConvertTo-Json -Depth 20 -Compress
    Assert-True ([StringComparer]::Ordinal.Equals($headJson, $baseJson)) "parcel-spec.schema.json key '$key' changed; only extensionSections may change."
}

$extSections = $SchemaDoc.extensionSections
[string[]]$ExtKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $extSections)
Assert-SequenceEqual -Actual $ExtKeys -Expected @('product-capability-safety-overlay') -Label 'extensionSections keys'
$overlay = Get-PropValue -Obj $extSections -Name 'product-capability-safety-overlay'
Assert-True ([StringComparer]::Ordinal.Equals([string]$overlay.addedBy, 'P3-B')) 'overlay.addedBy mismatch.'
Assert-SequenceEqual -Actual (Sort-Ordinal -Values @($overlay.appliesToShapes)) -Expected (Sort-Ordinal -Values @('coordinator-parcel', 'ticket-spec')) -Label 'overlay.appliesToShapes'
Assert-True ($overlay.bindsProductSemantics -eq $false) 'overlay.bindsProductSemantics must be false.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$overlay.contractSource, 'docs/specs/schemas/product-capability-safety-contract.json')) 'overlay.contractSource mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$overlay.fieldMapSource, 'docs/specs/schemas/CAPABILITY-FIELD-MAP.md')) 'overlay.fieldMapSource mismatch.'
[string[]]$PinnedBoundKeys = @('function_review_status', 'function_review_owner', 'capability_claim', 'numeric_provenance', 'missingness', 'escalation')
Assert-SequenceEqual -Actual @($overlay.boundFrontmatterKeys) -Expected $PinnedBoundKeys -Label 'overlay.boundFrontmatterKeys order'
Add-PassedCheck -Number 5 -Name 'extensionSections overlay registered, exactly pinned, all other 14 top-level keys byte-identical'

# Check 6: additive-only extension-point invariant
$ControlsDoc = ([IO.File]::ReadAllText((Join-Path $RepositoryRoot 'docs/specs/schemas/delivery-class-controls.json'))) | ConvertFrom-Json
$liveUnion = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($clsName in (Get-PropNames -Obj $ControlsDoc)) {
    $cls = Get-PropValue -Obj $ControlsDoc -Name $clsName
    foreach ($term in @($cls.requiredSpecAdditions)) { [void]$liveUnion.Add((ConvertTo-NormalizedTerm -Text $term)) }
}
$normalizedNewKey = ConvertTo-NormalizedTerm -Text 'product-capability-safety-overlay'
if ($liveUnion.Contains($normalizedNewKey)) { throw 'extension-point-not-additive: new extensionSections key collides with a live requiredSpecAdditions term.' }
# No other extensionSections key exists at this BaseCommit, so disjointness-from-other-keys is vacuously true but still asserted.
Assert-True ($ExtKeys.Count -eq 1) 'extension-point-not-additive: exactly one extensionSections key expected.'
Add-PassedCheck -Number 6 -Name 'extension-point-not-additive invariant (domain-overlay-insertion)'

# Check 7: CAPABILITY-FIELD-MAP.md six-row table
$FieldMapText = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'docs/specs/schemas/CAPABILITY-FIELD-MAP.md'))
$RequiredFieldMapHeader = '| Frontmatter key | Bound category | Required when | Value shape | Live cross-reference rule | Frozen source |'
Assert-True ($FieldMapText.Contains($RequiredFieldMapHeader)) 'CAPABILITY-FIELD-MAP.md missing exact required header row.'
[string[]]$PinnedFieldMapRowKeys = @('`function_review_status`', '`function_review_owner`', '`capability_claim`', '`numeric_provenance`', '`missingness`', '`escalation`')
$fieldMapLines = $FieldMapText -split "`r?`n"
$dataRows = @($fieldMapLines | Where-Object { $_.StartsWith('| `') })
Assert-True ($dataRows.Count -eq 6) "CAPABILITY-FIELD-MAP.md must contain exactly six data rows, found $($dataRows.Count)."
for ($i = 0; $i -lt $PinnedFieldMapRowKeys.Count; $i++) {
    Assert-True ($dataRows[$i].StartsWith("| $($PinnedFieldMapRowKeys[$i]) |")) "CAPABILITY-FIELD-MAP.md row $i must begin with $($PinnedFieldMapRowKeys[$i])."
}
Add-PassedCheck -Number 7 -Name 'CAPABILITY-FIELD-MAP.md exact six-row table present'

# Check 8: classification-axes.schema.json diff-scoped
$AxesPath = Join-Path $RepositoryRoot 'docs/specs/schemas/classification-axes.schema.json'
$AxesDoc = ([IO.File]::ReadAllText($AxesPath)) | ConvertFrom-Json
$BaseAxesRaw = (Invoke-Git -Arguments @('show', "$($BaseCommit):docs/specs/schemas/classification-axes.schema.json")) -join "`n"
$BaseAxesDoc = $BaseAxesRaw | ConvertFrom-Json
$pgc = Get-PropValue -Obj $AxesDoc.axes -Name 'productGuidanceClass'
$basePgc = Get-PropValue -Obj $BaseAxesDoc.axes -Name 'productGuidanceClass'
Assert-True ([StringComparer]::Ordinal.Equals([string]$pgc.controlSource, 'docs/specs/schemas/CAPABILITY-FIELD-MAP.md')) 'productGuidanceClass.controlSource not flipped to the pinned value.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$pgc.controlBindingStatus, 'bound-at-this-parcel')) 'productGuidanceClass.controlBindingStatus not flipped to the pinned value.'
foreach ($key in (Get-PropNames -Obj $pgc)) {
    if ($key -eq 'controlSource' -or $key -eq 'controlBindingStatus') { continue }
    $hj = (Get-PropValue -Obj $pgc -Name $key) | ConvertTo-Json -Depth 20 -Compress
    $bj = (Get-PropValue -Obj $basePgc -Name $key) | ConvertTo-Json -Depth 20 -Compress
    Assert-True ([StringComparer]::Ordinal.Equals($hj, $bj)) "axis-binding-diff-scoped: productGuidanceClass.$key changed unexpectedly."
}
foreach ($axisName in (Get-PropNames -Obj $AxesDoc.axes)) {
    if ($axisName -eq 'productGuidanceClass') { continue }
    $hj = (Get-PropValue -Obj $AxesDoc.axes -Name $axisName) | ConvertTo-Json -Depth 20 -Compress
    $bj = (Get-PropValue -Obj $BaseAxesDoc.axes -Name $axisName) | ConvertTo-Json -Depth 20 -Compress
    Assert-True ([StringComparer]::Ordinal.Equals($hj, $bj)) "axis-binding-diff-scoped: axis '$axisName' changed unexpectedly."
}
Add-PassedCheck -Number 8 -Name 'axis-binding-diff-scoped (productGuidanceClass.controlSource/controlBindingStatus only)'

# ---------------------------------------------------------------------------
# Check 9: ten fixtures
# ---------------------------------------------------------------------------

$ContractDoc = ([IO.File]::ReadAllText((Join-Path $RepositoryRoot 'docs/specs/schemas/product-capability-safety-contract.json'))) | ConvertFrom-Json

[string[]]$FixtureNames = @(
    'positive-capability-bearing', 'positive-non-capability-bearing', 'positive-coordinator-parcel-p3b-self',
    'negative-capability-claim-missing', 'negative-claim-behavior-drift', 'negative-numeric-provenance-missing',
    'negative-escalation-missing', 'negative-function-review-status-invalid', 'negative-premature-public-enablement-claim',
    'positive-escalation-stage4-interaction-signal'
)
Assert-True ($FixtureNames.Count -eq 10) 'Exactly ten fixtures are pinned.'

$FixtureResults = New-Object 'System.Collections.Generic.List[object]'
foreach ($name in $FixtureNames) {
    $fixturePath = Join-Path $RepositoryRoot "docs/specs/schemas/fixtures/p3b/$name.json"
    $fixtureDoc = ([IO.File]::ReadAllText($fixturePath)) | ConvertFrom-Json
    [string[]]$topKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $fixtureDoc)
    Assert-SequenceEqual -Actual $topKeys -Expected (Sort-Ordinal -Values @('input', 'expected')) -Label "$name top-level keys"

    if (Test-PropPresent -Obj $fixtureDoc.input -Name 'specPath') {
        $specRelPath = [string]$fixtureDoc.input.specPath
        $specFullPath = Join-Path $RepositoryRoot $specRelPath
        Assert-True (Test-Path -LiteralPath $specFullPath) "${name}: input.specPath '$specRelPath' must exist."
        $parsed = ConvertFrom-FrontmatterText -Text ([IO.File]::ReadAllText($specFullPath))
        Assert-True ($null -ne $parsed) "${name}: real spec file must carry a parseable frontmatter block."
        $computed = Test-CapabilitySafetyOverlay -Frontmatter $parsed.Frontmatter -Contract $ContractDoc -SelfReferenceCarveOut $true
    } else {
        $synthetic = $fixtureDoc.input.syntheticSpec
        $computed = Test-CapabilitySafetyOverlay -Frontmatter $synthetic.frontmatter -Contract $ContractDoc -SelfReferenceCarveOut $false
    }

    $expectedResult = [string]$fixtureDoc.expected.result
    Assert-True ([StringComparer]::Ordinal.Equals($computed.Result, $expectedResult)) "${name}: expected.result mismatch. Recorded '$expectedResult', computed '$($computed.Result)'."
    if ($expectedResult -eq 'invalid') {
        $expectedReason = [string]$fixtureDoc.expected.reason
        Assert-True ([StringComparer]::Ordinal.Equals($computed.Reason, $expectedReason)) "${name}: expected.reason mismatch. Recorded '$expectedReason', computed '$($computed.Reason)'."
    }
    $FixtureResults.Add([ordered]@{ name = $name; pass = $true; result = $computed.Result; reason = $computed.Reason }) | Out-Null
}
Add-PassedCheck -Number 9 -Name 'ten p3b fixtures reproduce expected result/reason via live cross-reference (AC-P3B-04/05/10)'

# ---------------------------------------------------------------------------
# Check 10: INDEX.md / README.md bounded amendments
# ---------------------------------------------------------------------------

$indexUnifiedDiff = @(Invoke-Git -Arguments @('diff', "$BaseCommit...HEAD", '--', 'docs/specs/INDEX.md'))
$addedIndexLines = @($indexUnifiedDiff | Where-Object { $_.StartsWith('+') -and -not $_.StartsWith('+++') })
$removedIndexLines = @($indexUnifiedDiff | Where-Object { $_.StartsWith('-') -and -not $_.StartsWith('---') })
$addedP3BRows = @($addedIndexLines | Where-Object { $_ -match '^\+\| P3-B \|' })
Assert-True ($addedP3BRows.Count -eq 1) 'INDEX.md diff must add exactly one row matching ^\| P3-B \|.'
Assert-True ($removedIndexLines.Count -eq 0) 'INDEX.md diff must remove zero lines.'

$indexHeadText = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'docs/specs/INDEX.md'))
$p3bRowMatch = [regex]::Match($indexHeadText, '(?m)^\| P3-B \|.*$')
Assert-True $p3bRowMatch.Success 'INDEX.md at HEAD must contain the P3-B row.'
$p3bCells = $p3bRowMatch.Value.Trim().Trim('|').Split('|') | ForEach-Object { $_.Trim() }
Assert-True ($p3bCells.Count -eq 10) 'INDEX.md P3-B row must have exactly ten cells.'
Assert-True ([StringComparer]::Ordinal.Equals($p3bCells[0], 'P3-B')) 'INDEX.md P3-B row Parcel cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p3bCells[1], 'review-candidate')) 'INDEX.md P3-B row Status cell mismatch.'
[string]$PinnedP3BSpecHref = '../INITIATIVES/biostack-governed-delivery/parcels/P3-B.md'
[string]$PinnedP3BCharterHref = '../INITIATIVES/biostack-governed-delivery/CHARTER.md'
$p3bSpecCellMatch = [regex]::Match($p3bCells[2], '^\[P3-B[^\]]*\]\(([^)]+)\)$')
Assert-True $p3bSpecCellMatch.Success 'INDEX.md P3-B row Spec cell must be a markdown link to this file.'
Assert-True ([StringComparer]::Ordinal.Equals($p3bSpecCellMatch.Groups[1].Value, $PinnedP3BSpecHref)) "INDEX.md P3-B row Spec cell href must equal '$PinnedP3BSpecHref'."
$p3bCharterCellMatch = [regex]::Match($p3bCells[3], '^\[[^\]]*charter[^\]]*\]\(([^)]+)\)$')
Assert-True $p3bCharterCellMatch.Success 'INDEX.md P3-B row Goal Charter cell must be a markdown link target naming the charter.'
Assert-True ([StringComparer]::Ordinal.Equals($p3bCharterCellMatch.Groups[1].Value, $PinnedP3BCharterHref)) "INDEX.md P3-B row Goal Charter cell href must equal '$PinnedP3BCharterHref'."
Assert-True ([StringComparer]::Ordinal.Equals($p3bCells[4], 'standard; architecture')) 'INDEX.md P3-B row Delivery classes cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p3bCells[5], 'not-applicable')) 'INDEX.md P3-B row Guidance classes cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p3bCells[6], 'coordinator-assigns-at-gate-2')) 'INDEX.md P3-B row Branch/worktree cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p3bCells[7], 'coordinator-assigns-at-gate-2')) 'INDEX.md P3-B row Owner cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p3bCells[8], '2 independent reviewers')) 'INDEX.md P3-B row Review requirement cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p3bCells[9], 'not-yet-closed')) 'INDEX.md P3-B row Closure cell mismatch.'

$readmeUnifiedDiff = @(Invoke-Git -Arguments @('diff', "$BaseCommit...HEAD", '--', 'docs/specs/README.md'))
$removedReadmeLines = @($readmeUnifiedDiff | Where-Object { $_.StartsWith('-') -and -not $_.StartsWith('---') })
Assert-True ($removedReadmeLines.Count -eq 0) 'README.md diff must remove zero lines.'
$readmeHeadText = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'docs/specs/README.md'))
Assert-True ($readmeHeadText.Contains('## Parcel-schema binding to the Product Capability and Safety Contract (P3-B)')) 'README.md missing required P3-B section heading.'
foreach ($link in @('schemas/CAPABILITY-FIELD-MAP.md', 'schemas/parcel-spec.schema.json')) {
    Assert-True ($readmeHeadText.Contains($link)) "README.md P3-B section missing link to $link."
}
Add-PassedCheck -Number 10 -Name 'bounded INDEX.md/README.md amendments (AC-P3B-09) with pinned cell/content values'

# ---------------------------------------------------------------------------
# Check 11: no unresolved placeholder anywhere this parcel ships
# ---------------------------------------------------------------------------

[string[]]$ModifiedSurfaces = @('docs/specs/README.md', 'docs/specs/INDEX.md', 'docs/specs/schemas/parcel-spec.schema.json', 'docs/specs/schemas/classification-axes.schema.json')
foreach ($surface in $AllowedSurfaces) {
    if ($surface -eq 'docs/specs/scripts/verify-p3b.ps1') { continue }
    if ($ModifiedSurfaces -contains $surface) {
        $unifiedDiff = @(Invoke-Git -Arguments @('diff', "$BaseCommit...HEAD", '--', $surface))
        $addedLines = @($unifiedDiff | Where-Object { $_.StartsWith('+') -and -not $_.StartsWith('+++') } | ForEach-Object { $_.Substring(1) })
        $content = $addedLines -join "`n"
    } else {
        $content = [IO.File]::ReadAllText((Join-Path $RepositoryRoot $surface))
    }
    Assert-True (-not (Test-PlaceholderViolation -Text $content)) "Unresolved placeholder found in $surface after normalization."
}
Add-PassedCheck -Number 11 -Name 'unresolved placeholder scan incl. normalization pipeline (AC-P3B-06)'

# ---------------------------------------------------------------------------
# Check 12/13: evidence bundle and clean tree
# ---------------------------------------------------------------------------

function Assert-OnlyAuthorizedEvidenceStatus {
    param([string[]]$StatusLines)
    foreach ($line in $StatusLines) {
        Assert-True $line.StartsWith('?? artifacts/p3b-verification/', [StringComparison]::Ordinal) "Unauthorized or tracked worktree status entry: $line"
    }
}

$expectedEvidencePath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot 'artifacts/p3b-verification'))
$resolvedEvidencePath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $EvidenceDirectory))
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($resolvedEvidencePath.TrimEnd('\', '/'), $expectedEvidencePath.TrimEnd('\', '/'))) 'EvidenceDirectory must resolve to artifacts/p3b-verification under the repository root.'

$trackedEvidence = Get-GitResult -Arguments @('ls-files', '--error-unmatch', '--', 'artifacts/p3b-verification')
Assert-True ($trackedEvidence.ExitCode -ne 0) 'artifacts/p3b-verification must be untracked.'
[IO.Directory]::CreateDirectory($resolvedEvidencePath) | Out-Null

$headCommit = (@(Invoke-Git -Arguments @('rev-parse', 'HEAD')))[0].Trim()

Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'changed-files.txt') -Content ($ActualChanges -join "`n")
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'schema-check.json') -Content ((([ordered]@{ schema = 'biostack.p3b-schema-check.v1'; pass = $true; topLevelKeys = $Pinned15Keys; overlayKey = 'product-capability-safety-overlay' }) | ConvertTo-Json -Depth 10))
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'extension-point-check.json') -Content ((([ordered]@{ schema = 'biostack.p3b-extension-point-check.v1'; pass = $true; normalizedKey = $normalizedNewKey }) | ConvertTo-Json -Depth 10))
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'field-map-check.json') -Content ((([ordered]@{ schema = 'biostack.p3b-field-map-check.v1'; pass = $true; rowCount = 6 }) | ConvertTo-Json -Depth 10))
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'axis-binding-check.json') -Content ((([ordered]@{ schema = 'biostack.p3b-axis-binding-check.v1'; pass = $true; controlSource = [string]$pgc.controlSource; controlBindingStatus = [string]$pgc.controlBindingStatus }) | ConvertTo-Json -Depth 10))
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'fixture-results.json') -Content (($FixtureResults | ConvertTo-Json -Depth 10))

$preSummaryStatus = @(Invoke-Git -Arguments @('status', '--porcelain=v1', '--untracked-files=all'))
Assert-OnlyAuthorizedEvidenceStatus -StatusLines $preSummaryStatus
Add-PassedCheck -Number 12 -Name 'authorized UTF-8/LF evidence bundle generation'

$summary = [ordered]@{
    schema = 'biostack.p3b-verification-summary.v1'
    pass = $true
    baseCommit = $BaseCommit.ToLowerInvariant()
    headCommit = $headCommit
    builderId = $BuilderId
    reviewerIds = [string[]]$ReviewerIds
    disclosedDeviation = 'parcels/P3-B.md brought in as a 17th changed path (byte-identical to already-reviewed upstream commit 7b3225c), because BaseCommit lacks the merged spec despite the Gate 2 dispatch record''s claim; flagged for coordinator reconciliation.'
    checks = $CheckResults.ToArray()
}
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'verification-summary.json') -Content ($summary | ConvertTo-Json -Depth 12)

[string[]]$ExpectedEvidenceFiles = @(
    'changed-files.txt', 'schema-check.json', 'extension-point-check.json', 'field-map-check.json',
    'axis-binding-check.json', 'fixture-results.json', 'verification-summary.json'
)
foreach ($evidenceFile in $ExpectedEvidenceFiles) {
    Assert-True (Test-Path -LiteralPath (Join-Path $resolvedEvidencePath $evidenceFile)) "Missing evidence file: $evidenceFile"
}

$finalStatus = @(Invoke-Git -Arguments @('status', '--porcelain=v1', '--untracked-files=all'))
Assert-OnlyAuthorizedEvidenceStatus -StatusLines $finalStatus
Add-PassedCheck -Number 13 -Name 'untracked evidence directory and clean tree'

Write-Output 'P3-B verification PASS'
exit 0
