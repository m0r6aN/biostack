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
    'docs/specs/schemas/parcel-spec.schema.json',
    'docs/specs/schemas/SECTION-HEADING-MAP.md',
    'docs/specs/schemas/EXTENSION-POINTS.md',
    'docs/specs/templates/parcel-template.standard.md',
    'docs/specs/templates/parcel-template.health-boundary.md',
    'docs/specs/templates/parcel-template.privacy.md',
    'docs/specs/templates/parcel-template.migration.md',
    'docs/specs/templates/parcel-template.trust-path.md',
    'docs/specs/templates/parcel-template.provider-pilot.md',
    'docs/specs/templates/parcel-template.legal-policy.md',
    'docs/specs/templates/parcel-template.knowledge-promotion.md',
    'docs/specs/templates/README.md',
    'docs/specs/schemas/fixtures/p3a/positive-template-standard.json',
    'docs/specs/schemas/fixtures/p3a/positive-template-health-boundary.json',
    'docs/specs/schemas/fixtures/p3a/positive-template-privacy.json',
    'docs/specs/schemas/fixtures/p3a/positive-template-migration.json',
    'docs/specs/schemas/fixtures/p3a/positive-template-trust-path.json',
    'docs/specs/schemas/fixtures/p3a/positive-template-provider-pilot.json',
    'docs/specs/schemas/fixtures/p3a/positive-template-legal-policy.json',
    'docs/specs/schemas/fixtures/p3a/positive-template-knowledge-promotion.json',
    'docs/specs/schemas/fixtures/p3a/negative-tbd-violation-literal.json',
    'docs/specs/schemas/fixtures/p3a/negative-tbd-violation-entity-disguised.json',
    'docs/specs/schemas/fixtures/p3a/negative-missing-required-field.json',
    'docs/specs/schemas/fixtures/p3a/negative-unknown-extension-point.json',
    'docs/specs/schemas/fixtures/p3a/REAL-SPEC-COMPATIBILITY-SET.md',
    'docs/specs/scripts/verify-p3a.ps1',
    'docs/specs/README.md',
    'docs/specs/INDEX.md'
)

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
[string[]]$RequiredFrontmatterKeys = @(
    'title', 'status', 'owner', 'created', 'updated',
    'delivery_classes', 'guidance_classes', 'substance_function_risk', 'surfaces'
)
[string[]]$StatusClosedVocabulary = @('review-candidate', 'active', 'done', 'superseded')

[string[]]$PinnedPlaceholderPatterns = @(
    '(?im)(^\s*(TBD|TODO|FIXME)\s*[:|\-])|(\{\{[^}]+\}\})',
    '(?i)\b(TBD|TODO|FIXME)\b'
)
[string]$SanctionedMarker = '\[REPLACE:[^\]]+\]'
[string]$SanctionedMarkerScope = 'docs/specs/templates/** only'
[string[]]$PinnedNormalizationSteps = @(
    'strip-html-comments', 'strip-html-tags', 'strip-inline-code-delimiters',
    'strip-emphasis-markers', 'strip-markdown-escape-backslashes', 'decode-html-entities',
    'strip-unicode-category-Cf-and-Cc', 'nfkc-normalize', 'unicode-confusables-skeleton'
)

[string[]]$StaticFrozenPaths = @(
    'docs/INITIATIVES/biostack-governed-delivery/CHARTER.md',
    'docs/INITIATIVES/biostack-governed-delivery/PLAN-REVIEW.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P1.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P2.md',
    'docs/INITIATIVES/biostack-governed-delivery/dispatch/P1-GATE2.md',
    'docs/INITIATIVES/biostack-governed-delivery/closures/P1.md',
    'docs/INITIATIVES/biostack-governed-delivery/closures/P2.md',
    'docs/specs/schemas/classification-axes.schema.json',
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

[string[]]$TemplateClasses = @(
    'standard', 'health-boundary', 'privacy', 'migration',
    'trust-path', 'provider-pilot', 'legal-policy', 'knowledge-promotion'
)

[string[]]$PositiveFixtureNames = @(
    'positive-template-standard', 'positive-template-health-boundary', 'positive-template-privacy',
    'positive-template-migration', 'positive-template-trust-path', 'positive-template-provider-pilot',
    'positive-template-legal-policy', 'positive-template-knowledge-promotion'
)
[string[]]$NegativeFixtureNames = @(
    'negative-tbd-violation-literal', 'negative-tbd-violation-entity-disguised',
    'negative-missing-required-field', 'negative-unknown-extension-point'
)

$CheckResults = New-Object 'System.Collections.Generic.List[object]'

# ---------------------------------------------------------------------------
# Generic helpers (ported pattern from verify-p2.ps1)
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
    $v = $null
    if ($Obj -is [System.Collections.IDictionary]) { $v = $Obj[$Name] } else { $v = $Obj.$Name }
    if ($v -is [System.Array]) { return , $v }
    return $v
}

# ---------------------------------------------------------------------------
# Minimal YAML-frontmatter-block parser (scalar / flow-array / block-list)
# ---------------------------------------------------------------------------

function ConvertFrom-FrontmatterText {
    param([Parameter(Mandatory = $true)][string]$Text)
    $m = [regex]::Match($Text, '(?s)^---\r?\n(.*?)\r?\n---\r?\n?')
    if (-not $m.Success) { return $null }
    $block = $m.Groups[1].Value
    $rest = $Text.Substring($m.Length)
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
    return [pscustomobject]@{ Frontmatter = $fm; Body = $rest }
}

function Get-Headings {
    param([string]$Body)
    $heads = New-Object 'System.Collections.Generic.List[string]'
    foreach ($m in [regex]::Matches($Body, '(?m)^#{2,3}\s+(.+)$')) {
        $heads.Add($m.Groups[1].Value.Trim()) | Out-Null
    }
    return , [string[]]$heads.ToArray()
}

function Get-HeadingSections {
    # Returns ordered list of @{ Text = heading text; Body = text until next heading }
    param([string]$Body)
    $matches = [regex]::Matches($Body, '(?m)^(#{2,3})\s+(.+)$')
    $sections = New-Object 'System.Collections.Generic.List[object]'
    for ($i = 0; $i -lt $matches.Count; $i++) {
        $start = $matches[$i].Index + $matches[$i].Length
        $end = if ($i + 1 -lt $matches.Count) { $matches[$i + 1].Index } else { $Body.Length }
        $sectionBody = $Body.Substring($start, $end - $start)
        $sections.Add([pscustomobject]@{ Text = $matches[$i].Groups[2].Value.Trim(); Body = $sectionBody }) | Out-Null
    }
    return , $sections.ToArray()
}

# ---------------------------------------------------------------------------
# Term/heading normalization (document contract 2)
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
    # Comma-return: a function emitting a single-element array otherwise
    # unrolls through the output pipeline and the caller receives the bare
    # scalar element instead of a 1-element array (PowerShell pipeline
    # enumeration gotcha), which would silently corrupt downstream
    # token-count and token-indexing logic.
    return , [string[]]$out.ToArray()
}

function Get-RequiredUnion {
    param([string[]]$DeliveryClasses, $ControlsDoc)
    $seen = New-Object 'System.Collections.Generic.List[string]'
    foreach ($c in $DeliveryClasses) {
        $ctrl = Get-PropValue -Obj $ControlsDoc -Name $c
        if ($null -eq $ctrl) { continue }
        foreach ($t in [string[]]@($ctrl.requiredSpecAdditions)) {
            if (-not $seen.Contains($t)) { $seen.Add($t) | Out-Null }
        }
    }
    return , [string[]]$seen.ToArray()
}

function Resolve-RequiredSections {
    # Returns @{ Unsatisfied = string[]; Assignment = hashtable term->headingIndex }
    # Per document contract 2's alias rule (R2-F3), a term is satisfied by any
    # heading matching either the term's own normalized tokens OR any
    # normalized `Canonical alias(es)` phrase for that term, as populated into
    # $script:HeadingMapAliasMap from SECTION-HEADING-MAP.md (check 6). If no
    # alias map has been populated yet (or a term has no row), the term is its
    # own sole candidate phrase, preserving prior behavior.
    param([string[]]$Terms, [string[]]$Headings)
    $termsSorted = Sort-Ordinal -Values $Terms
    $consumed = New-Object 'System.Collections.Generic.HashSet[int]'
    $unsatisfied = New-Object 'System.Collections.Generic.List[string]'
    $assignment = @{}
    $headingToks = @()
    for ($i = 0; $i -lt $Headings.Count; $i++) { $headingToks += ,(ConvertTo-NormalizedTokens -Text $Headings[$i]) }

    foreach ($term in $termsSorted) {
        $phrases = New-Object 'System.Collections.Generic.List[string]'
        $phrases.Add($term) | Out-Null
        if ($null -ne $script:HeadingMapAliasMap -and $script:HeadingMapAliasMap.ContainsKey($term)) {
            foreach ($alias in $script:HeadingMapAliasMap[$term]) {
                if (-not [string]::IsNullOrWhiteSpace($alias) -and -not $phrases.Contains($alias)) {
                    $phrases.Add($alias) | Out-Null
                }
            }
        }
        $candidatePhraseToks = New-Object 'System.Collections.Generic.List[object]'
        foreach ($p in $phrases) {
            $pt = ConvertTo-NormalizedTokens -Text $p
            if ($pt.Count -gt 0) { $candidatePhraseToks.Add($pt) | Out-Null }
        }
        $bestIdx = -1
        $bestKey = $null
        for ($idx = 0; $idx -lt $Headings.Count; $idx++) {
            if ($consumed.Contains($idx)) { continue }
            $hToks = $headingToks[$idx]
            if ($hToks.Count -gt 10) { continue }
            $found = $false
            foreach ($phraseToks in $candidatePhraseToks) {
                $phraseLen = $phraseToks.Count
                if ($phraseLen -eq 0) { continue }
                if ($hToks.Count -gt ($phraseLen + 4)) { continue }
                for ($start = 0; $start -le ($hToks.Count - $phraseLen); $start++) {
                    $match = $true
                    for ($k = 0; $k -lt $phraseLen; $k++) {
                        if ($hToks[$start + $k] -ne $phraseToks[$k]) { $match = $false; break }
                    }
                    if ($match) { $found = $true; break }
                }
                if ($found) { break }
            }
            if (-not $found) { continue }
            $normalizedHeadingText = [string]::Join(' ', $hToks)
            if ($null -eq $bestKey -or
                ([string]::CompareOrdinal($normalizedHeadingText, $bestKey.Text) -lt 0) -or
                ([string]::CompareOrdinal($normalizedHeadingText, $bestKey.Text) -eq 0 -and $idx -lt $bestKey.Idx)) {
                $bestKey = [pscustomobject]@{ Text = $normalizedHeadingText; Idx = $idx }
                $bestIdx = $idx
            }
        }
        if ($bestIdx -ge 0) {
            $consumed.Add($bestIdx) | Out-Null
            $assignment[$term] = $bestIdx
        } else {
            $unsatisfied.Add($term) | Out-Null
        }
    }
    return [pscustomobject]@{ Unsatisfied = [string[]]$unsatisfied.ToArray(); Assignment = $assignment }
}

# ---------------------------------------------------------------------------
# Placeholder normalization pipeline (document contract 1, check 12)
# ---------------------------------------------------------------------------

# Minimal, deterministic, offline confusables-skeleton table (UTS #39-style)
# covering common Latin-lookalike Cyrillic/Greek codepoints. Applied after
# NFKC normalization as the final pipeline step.
#
# Scope disclosure (R2-F5, accept-as-documented): this table is intentionally
# bounded to the common Latin-lookalike Cyrillic/Greek codepoints enumerated
# below, not a full UTS #39 confusables database; it is a deterministic,
# offline approximation sufficient for the placeholder-evasion cases this
# parcel's own fixtures exercise, not a general-purpose confusables defense.
$script:ConfusablesMap = @{
    [char]0x0410 = 'A'; [char]0x0430 = 'a'; [char]0x0412 = 'B'; [char]0x0415 = 'E'; [char]0x0435 = 'e'
    [char]0x041A = 'K'; [char]0x043A = 'k'; [char]0x041C = 'M'; [char]0x041D = 'H'; [char]0x041E = 'O'
    [char]0x043E = 'o'; [char]0x0420 = 'P'; [char]0x0440 = 'p'; [char]0x0421 = 'C'; [char]0x0441 = 'c'
    [char]0x0422 = 'T'; [char]0x0442 = 't'; [char]0x0425 = 'X'; [char]0x0445 = 'x'; [char]0x0423 = 'Y'
    [char]0x0443 = 'y'; [char]0x0406 = 'I'; [char]0x0456 = 'i'; [char]0x0391 = 'A'; [char]0x0392 = 'B'
    [char]0x0395 = 'E'; [char]0x0396 = 'Z'; [char]0x0397 = 'H'; [char]0x0399 = 'I'; [char]0x039A = 'K'
    [char]0x039C = 'M'; [char]0x039D = 'N'; [char]0x039F = 'O'; [char]0x03A1 = 'P'; [char]0x03A4 = 'T'
    [char]0x03A5 = 'Y'; [char]0x03A7 = 'X'
}

function Get-PlaceholderNormalizedText {
    param([string]$Text)
    $t = $Text
    # 1. strip-html-comments
    $t = [regex]::Replace($t, '<!--[\s\S]*?-->', '')
    # 2. strip-html-tags
    $t = [regex]::Replace($t, '</?[a-zA-Z][a-zA-Z0-9]*(?:\s[^>]*)?>', '')
    # 3. strip-inline-code-delimiters
    $t = [regex]::Replace($t, '`+', '')
    # 4. strip-emphasis-markers (paired * and _ runs)
    $t = [regex]::Replace($t, '\*{1,3}([^*]+)\*{1,3}', '$1')
    $t = [regex]::Replace($t, '_{1,3}([^_]+)_{1,3}', '$1')
    # 5. strip-markdown-escape-backslashes (backslash immediately before ASCII punctuation)
    $t = [regex]::Replace($t, '\\(?=[!-/:-@\[-`{-~])', '')
    # 6. decode-html-entities
    $t = [System.Net.WebUtility]::HtmlDecode($t)
    # 7. strip-unicode-category-Cf-and-Cc
    $sb = New-Object System.Text.StringBuilder
    foreach ($ch in $t.ToCharArray()) {
        $cat = [System.Globalization.CharUnicodeInfo]::GetUnicodeCategory($ch)
        if ($cat -eq [System.Globalization.UnicodeCategory]::Format -or $cat -eq [System.Globalization.UnicodeCategory]::Control) { continue }
        [void]$sb.Append($ch)
    }
    $t = $sb.ToString()
    # 8. nfkc-normalize
    $t = $t.Normalize([System.Text.NormalizationForm]::FormKC)
    # 9. unicode-confusables-skeleton
    $sb2 = New-Object System.Text.StringBuilder
    foreach ($ch in $t.ToCharArray()) {
        if ($script:ConfusablesMap.ContainsKey($ch)) {
            [void]$sb2.Append($script:ConfusablesMap[$ch])
        } else {
            [void]$sb2.Append($ch)
        }
    }
    return $sb2.ToString()
}

function Test-PlaceholderViolation {
    param([string]$Text)
    $normalized = Get-PlaceholderNormalizedText -Text $Text
    foreach ($pattern in $PinnedPlaceholderPatterns) {
        if ([regex]::IsMatch($normalized, $pattern)) { return $true }
    }
    return $false
}

function Get-PlaceholderMatchCount {
    param([string]$Text)
    $normalized = Get-PlaceholderNormalizedText -Text $Text
    $count = 0
    foreach ($pattern in $PinnedPlaceholderPatterns) {
        $count += [regex]::Matches($normalized, $pattern).Count
    }
    return $count
}

# ---------------------------------------------------------------------------
# Core parcel-spec validator (frontmatter + headings + body + raw text)
# ---------------------------------------------------------------------------

function Test-ParcelSpec {
    param(
        [System.Collections.Specialized.OrderedDictionary]$Frontmatter,
        [string[]]$Headings,
        [string]$RawScanText,
        $ControlsDoc,
        [string[]]$ExtensionKeys
    )
    foreach ($key in $RequiredFrontmatterKeys) {
        $present = $Frontmatter.Contains($key)
        $val = $null
        if ($present) { $val = $Frontmatter[$key] }
        $isArrayVal = ($val -is [System.Array])
        $isEmpty = (-not $isArrayVal) -and (($null -eq $val) -or ($val -is [string] -and [string]::IsNullOrWhiteSpace($val)))
        if (-not $present -or $isEmpty) {
            return [pscustomobject]@{ Result = 'invalid'; ReasonCode = 'missing-required-frontmatter-key'; ReasonDetail = "missing-required-frontmatter-key ($key)" }
        }
    }
    $dc = @($Frontmatter['delivery_classes'])
    $gc = @($Frontmatter['guidance_classes'])
    $sf = @($Frontmatter['substance_function_risk'])

    foreach ($l in $dc) {
        if ($DeliveryClassLabels -notcontains $l) {
            return [pscustomobject]@{ Result = 'invalid'; ReasonCode = 'unknown-label'; ReasonDetail = "unknown-label (deliveryClass: $l)" }
        }
    }
    foreach ($l in $gc) {
        if ($GuidanceClassLabels -notcontains $l) {
            return [pscustomobject]@{ Result = 'invalid'; ReasonCode = 'unknown-label'; ReasonDetail = "unknown-label (productGuidanceClass: $l)" }
        }
    }
    foreach ($l in $sf) {
        if ($SubstanceFunctionRiskLabels -notcontains $l) {
            return [pscustomobject]@{ Result = 'invalid'; ReasonCode = 'unknown-label'; ReasonDetail = "unknown-label (substanceFunctionRisk: $l)" }
        }
    }
    if ($dc.Count -eq 0) {
        return [pscustomobject]@{ Result = 'invalid'; ReasonCode = 'empty-required-axis'; ReasonDetail = 'empty-required-axis' }
    }

    $terms = Get-RequiredUnion -DeliveryClasses $dc -ControlsDoc $ControlsDoc
    $resolved = Resolve-RequiredSections -Terms $terms -Headings $Headings
    if ($resolved.Unsatisfied.Count -gt 0) {
        $first = (Sort-Ordinal -Values $resolved.Unsatisfied)[0]
        return [pscustomobject]@{ Result = 'invalid'; ReasonCode = 'missing-required-section'; ReasonDetail = "missing-required-section ($first)" }
    }

    if (Test-PlaceholderViolation -Text $RawScanText) {
        return [pscustomobject]@{ Result = 'invalid'; ReasonCode = 'placeholder-violation'; ReasonDetail = 'placeholder-violation' }
    }

    if ($Frontmatter.Contains('extension_section') -and $Frontmatter['extension_section']) {
        $ext = [string]$Frontmatter['extension_section']
        if ($ExtensionKeys -notcontains $ext) {
            return [pscustomobject]@{ Result = 'invalid'; ReasonCode = 'unknown-extension-point'; ReasonDetail = "unknown-extension-point ($ext)" }
        }
    }

    return [pscustomobject]@{ Result = 'valid'; ReasonCode = 'n/a'; ReasonDetail = 'n/a' }
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
Add-PassedCheck -Number 3 -Name 'exact changed-file set equals the 28 allowed surfaces'

# Check 4
# Census is computed frozen at BaseCommit (git ls-tree against BaseCommit), not
# from the current working tree/HEAD (git ls-files), so that a hypothetical
# deletion of a frozen path between BaseCommit and HEAD is still enumerated
# here and therefore still caught by the per-path `git diff --quiet` loop
# below (R2-F4).
[string[]]$RegressionSpecPaths = @(Invoke-Git -Arguments @('ls-tree', '-r', '--name-only', $BaseCommit, '--', 'docs/specs/active', 'docs/specs/done')) |
    Where-Object { $_ -ne 'docs/specs/active/README.md' -and $_ -ne 'docs/specs/done/README.md' }
[string[]]$P2FixturePaths = @(Invoke-Git -Arguments @('ls-tree', '-r', '--name-only', $BaseCommit, '--', 'docs/specs/schemas/fixtures')) |
    Where-Object { -not $_.StartsWith('docs/specs/schemas/fixtures/p3a/') }
[string[]]$AllFrozenPaths = @($StaticFrozenPaths + $RegressionSpecPaths + $P2FixturePaths)
foreach ($path in $AllFrozenPaths) {
    $quiet = Get-GitResult -Arguments @('diff', '--quiet', "$BaseCommit...HEAD", '--', $path)
    Assert-True ($quiet.ExitCode -eq 0) "Frozen path changed: $path"
}
Add-PassedCheck -Number 4 -Name 'frozen surfaces byte-identical to BaseCommit'

# Check 5: parcel-spec.schema.json
$SchemaPath = Join-Path $RepositoryRoot 'docs/specs/schemas/parcel-spec.schema.json'
$SchemaRaw = [IO.File]::ReadAllText($SchemaPath)
$SchemaDoc = $SchemaRaw | ConvertFrom-Json

[string[]]$Pinned15Keys = Sort-Ordinal -Values @(
    'schema', 'specShapes', 'requiredFrontmatterKeysCommonToBothShapes', 'statusClosedVocabulary',
    'axisSource', 'controlSource', 'foldEngineSource', 'sectionHeadingMapSource', 'extensionPointsSource',
    'requiredSectionDerivation', 'noPlaceholderPatterns', 'sanctionedTemplateFillInMarker',
    'sanctionedTemplateFillInMarkerScope', 'placeholderNormalizationSteps', 'extensionSections'
)
[string[]]$SchemaTopKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $SchemaDoc)
Assert-SequenceEqual -Actual $SchemaTopKeys -Expected $Pinned15Keys -Label 'parcel-spec.schema.json top-level keys'
Assert-True ([StringComparer]::Ordinal.Equals([string]$SchemaDoc.schema, 'biostack.parcel-spec.v1')) 'schema literal mismatch.'

[string[]]$ShapeKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $SchemaDoc.specShapes)
Assert-SequenceEqual -Actual $ShapeKeys -Expected (Sort-Ordinal -Values @('coordinator-parcel', 'ticket-spec')) -Label 'specShapes keys'
Assert-True ([StringComparer]::Ordinal.Equals([string]$SchemaDoc.specShapes.'coordinator-parcel'.pathPattern, 'docs/INITIATIVES/*/parcels/*.md')) 'coordinator-parcel pathPattern mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$SchemaDoc.specShapes.'coordinator-parcel'.idFrontmatterKey, 'parcel_id')) 'coordinator-parcel idFrontmatterKey mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$SchemaDoc.specShapes.'ticket-spec'.pathPattern, 'docs/specs/active/*.md|docs/specs/done/*.md')) 'ticket-spec pathPattern mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$SchemaDoc.specShapes.'ticket-spec'.idFrontmatterKey, 'ticket')) 'ticket-spec idFrontmatterKey mismatch.'

Assert-SequenceEqual -Actual @($SchemaDoc.requiredFrontmatterKeysCommonToBothShapes) -Expected $RequiredFrontmatterKeys -Label 'requiredFrontmatterKeysCommonToBothShapes order'
Assert-SequenceEqual -Actual @($SchemaDoc.statusClosedVocabulary) -Expected $StatusClosedVocabulary -Label 'statusClosedVocabulary order'

Assert-True ([StringComparer]::Ordinal.Equals([string]$SchemaDoc.axisSource, 'docs/specs/schemas/classification-axes.schema.json')) 'axisSource mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$SchemaDoc.controlSource, 'docs/specs/schemas/delivery-class-controls.json')) 'controlSource mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$SchemaDoc.foldEngineSource, 'docs/specs/schemas/fold-engine.md')) 'foldEngineSource mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$SchemaDoc.sectionHeadingMapSource, 'docs/specs/schemas/SECTION-HEADING-MAP.md')) 'sectionHeadingMapSource mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$SchemaDoc.extensionPointsSource, 'docs/specs/schemas/EXTENSION-POINTS.md')) 'extensionPointsSource mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$SchemaDoc.requiredSectionDerivation, 'fold-live')) 'requiredSectionDerivation mismatch.'

Assert-SequenceEqual -Actual @($SchemaDoc.noPlaceholderPatterns) -Expected $PinnedPlaceholderPatterns -Label 'noPlaceholderPatterns'
Assert-True ([StringComparer]::Ordinal.Equals([string]$SchemaDoc.sanctionedTemplateFillInMarker, $SanctionedMarker)) 'sanctionedTemplateFillInMarker mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$SchemaDoc.sanctionedTemplateFillInMarkerScope, $SanctionedMarkerScope)) 'sanctionedTemplateFillInMarkerScope mismatch.'
Assert-SequenceEqual -Actual @($SchemaDoc.placeholderNormalizationSteps) -Expected $PinnedNormalizationSteps -Label 'placeholderNormalizationSteps order'

[string[]]$ExtensionSectionsKeys = @(Get-PropNames -Obj $SchemaDoc.extensionSections)
Assert-True ($ExtensionSectionsKeys.Count -eq 0) 'extensionSections must be an empty object {} at this parcel''s shipped hash.'
Add-PassedCheck -Number 5 -Name 'parcel-spec.schema.json shape correctness (AC-P3A-01)'

# Check 6: SECTION-HEADING-MAP.md live-composition cross-check
$ControlsPath = Join-Path $RepositoryRoot 'docs/specs/schemas/delivery-class-controls.json'
$ControlsDoc = [IO.File]::ReadAllText($ControlsPath) | ConvertFrom-Json
$LiveUnion = New-Object 'System.Collections.Generic.List[string]'
foreach ($cls in $DeliveryClassLabels) {
    $ctrl = Get-PropValue -Obj $ControlsDoc -Name $cls
    foreach ($t in [string[]]@($ctrl.requiredSpecAdditions)) {
        if (-not $LiveUnion.Contains($t)) { $LiveUnion.Add($t) | Out-Null }
    }
}
[string[]]$LiveUnionSorted = Sort-Ordinal -Values $LiveUnion.ToArray()

$HeadingMapPath = Join-Path $RepositoryRoot 'docs/specs/schemas/SECTION-HEADING-MAP.md'
$HeadingMapText = [IO.File]::ReadAllText($HeadingMapPath)
$HeadingMapLines = $HeadingMapText -split "`r?`n"
$mapRows = @($HeadingMapLines | Where-Object { $_.StartsWith('| ') -and -not $_.StartsWith('| Control term') -and -not $_.StartsWith('|---') })
$mapTerms = New-Object 'System.Collections.Generic.List[string]'
# R2-F3: populate the term -> canonical-alias(es) map consulted by
# Resolve-RequiredSections (document contract 2's alias rule), not just the
# term column used for the live-union cross-check below.
$script:HeadingMapAliasMap = @{}
foreach ($row in $mapRows) {
    $cells = $row.Trim().Trim('|').Split('|') | ForEach-Object { $_.Trim() }
    Assert-True ($cells.Count -eq 3) "SECTION-HEADING-MAP.md malformed row: $row"
    Assert-True (-not [string]::IsNullOrWhiteSpace($cells[1])) "SECTION-HEADING-MAP.md row has empty Canonical alias(es): $row"
    Assert-True ([StringComparer]::Ordinal.Equals($cells[2], 'delivery-class-controls.json')) "SECTION-HEADING-MAP.md row has wrong Source: $row"
    $mapTerms.Add($cells[0]) | Out-Null
    [string[]]$aliases = @($cells[1] -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' })
    $script:HeadingMapAliasMap[$cells[0]] = $aliases
}
[string[]]$MapTermsSorted = Sort-Ordinal -Values $mapTerms.ToArray()
Assert-SequenceEqual -Actual $MapTermsSorted -Expected $LiveUnionSorted -Label 'SECTION-HEADING-MAP.md term column vs live requiredSpecAdditions union'
Add-PassedCheck -Number 6 -Name 'SECTION-HEADING-MAP.md live composition fidelity (AC-P3A-02)'

# Check 7: EXTENSION-POINTS.md
$ExtPointsPath = Join-Path $RepositoryRoot 'docs/specs/schemas/EXTENSION-POINTS.md'
$ExtPointsText = [IO.File]::ReadAllText($ExtPointsPath)
[string[]]$RequiredExtHeadings = @('## `delivery-class-extension`', '## `domain-overlay-insertion`', '## `template-set-extension`')
foreach ($h in $RequiredExtHeadings) {
    Assert-True ($ExtPointsText.Contains($h)) "EXTENSION-POINTS.md missing required heading: $h"
}
[string]$NonBindingSentence = 'This extension point adds no required section, check, reviewer weight, standing-authorization change, or stop condition by existing; it only becomes active when a future parcel''s own approved spec uses it.'
[string]$AppendOnlySentence = 'A newly appended `SECTION-HEADING-MAP.md` row''s normalized term (same normalization as document contract 2, including the anti-heading-soup constraints) must not duplicate any term already present in the live `requiredSpecAdditions` union, and no existing row may be removed, renamed, value-mutated, or reordered by this mechanic; the amending parcel''s own deterministic verifier must assert this append-only, non-duplicating invariant as a named check, failing `extension-point-not-additive` on violation.'
[string]$DisjointnessSentence = 'A newly appended `extensionSections` key''s normalized form (same normalization as `SECTION-HEADING-MAP.md`) must not equal any term already present in the live `requiredSpecAdditions` union at append time, nor equal any other `extensionSections` key; no existing `extensionSections` key may be removed, renamed, or value-mutated by any future append; and the appending parcel''s own deterministic verifier must assert this disjointness-and-non-removal invariant as a named check, failing `extension-point-not-additive` on violation.'

# R2-F2: pinned-sentence assertions are scoped per subsection (not a
# whole-file `.Contains`), so a sentence present anywhere in the file but
# outside its own named extension point's subsection does not satisfy this
# check (the adversarial "sentence anywhere" exploit must fail).
$ExtPointsSections = Get-HeadingSections -Body $ExtPointsText
function Get-ExtensionPointSection {
    param([string]$NameNeedle)
    return ($ExtPointsSections | Where-Object { $_.Text.Contains($NameNeedle) } | Select-Object -First 1)
}
$DeliveryClassExtSection = Get-ExtensionPointSection -NameNeedle 'delivery-class-extension'
$DomainOverlaySection = Get-ExtensionPointSection -NameNeedle 'domain-overlay-insertion'
$TemplateSetExtSection = Get-ExtensionPointSection -NameNeedle 'template-set-extension'
Assert-True ($null -ne $DeliveryClassExtSection) 'EXTENSION-POINTS.md missing the delivery-class-extension subsection.'
Assert-True ($null -ne $DomainOverlaySection) 'EXTENSION-POINTS.md missing the domain-overlay-insertion subsection.'
Assert-True ($null -ne $TemplateSetExtSection) 'EXTENSION-POINTS.md missing the template-set-extension subsection.'

foreach ($extSection in @($DeliveryClassExtSection, $DomainOverlaySection, $TemplateSetExtSection)) {
    Assert-True ($extSection.Body.Contains($NonBindingSentence)) "EXTENSION-POINTS.md subsection '$($extSection.Text)' missing the pinned non-binding sentence within its own subsection."
}
Assert-True ($DeliveryClassExtSection.Body.Contains($AppendOnlySentence)) 'EXTENSION-POINTS.md missing the pinned delivery-class-extension append-only invariant sentence within its own subsection.'
Assert-True ($DomainOverlaySection.Body.Contains($DisjointnessSentence)) 'EXTENSION-POINTS.md missing the pinned domain-overlay-insertion disjointness invariant sentence within its own subsection.'
Add-PassedCheck -Number 7 -Name 'EXTENSION-POINTS.md exact mechanics and pinned sentences, scoped per subsection (AC-P3A-03)'

# ---------------------------------------------------------------------------
# Check 8: eight per-delivery-class templates
# ---------------------------------------------------------------------------

[string[]]$ExtensionKeysRegistry = @()  # extensionSections is {} at this parcel's shipped hash

$TemplateCheckResults = New-Object 'System.Collections.Generic.List[object]'

function Test-Template {
    param([string]$Class)
    $path = Join-Path $RepositoryRoot "docs/specs/templates/parcel-template.$Class.md"
    $raw = [IO.File]::ReadAllText($path)
    $parsedPre = ConvertFrom-FrontmatterText -Text $raw
    Assert-True ($null -ne $parsedPre) "Template $Class missing YAML frontmatter."
    $fmPre = $parsedPre.Frontmatter
    foreach ($key in @($RequiredFrontmatterKeys + @('parcel_id'))) {
        Assert-True ($fmPre.Contains($key)) "Template $Class missing frontmatter key: $key"
    }
    $dcPre = @($fmPre['delivery_classes'])
    Assert-SequenceEqual -Actual $dcPre -Expected @($Class) -Label "Template $Class delivery_classes"
    Assert-True ((@($fmPre['guidance_classes'])).Count -eq 0) "Template $Class guidance_classes must be []."
    Assert-True ((@($fmPre['substance_function_risk'])).Count -eq 0) "Template $Class substance_function_risk must be []."

    $headingsPre = Get-Headings -Body $parsedPre.Body
    $terms = Get-RequiredUnion -DeliveryClasses $dcPre -ControlsDoc $ControlsDoc
    $resolvedPre = Resolve-RequiredSections -Terms $terms -Headings $headingsPre
    Assert-True ($resolvedPre.Unsatisfied.Count -eq 0) "Template $Class has unsatisfied required sections: $($resolvedPre.Unsatisfied -join ', ')"

    # Pre-substitution content-quality floor check
    $sections = Get-HeadingSections -Body $parsedPre.Body
    foreach ($term in $terms) {
        $headingIdx = $resolvedPre.Assignment[$term]
        $section = $sections[$headingIdx]
        $strippedBody = [regex]::Replace($section.Body, $SanctionedMarker, '')
        $wordTokens = @($strippedBody -split '\s+' | Where-Object { $_ -ne '' })
        Assert-True ($wordTokens.Count -ge 8) "Template $Class heading '$($section.Text)' fails the 8-word content floor (template-content-floor-not-met)."
    }

    # Extension points used heading
    $extSection = $sections | Where-Object { $_.Text -eq 'Extension points used' } | Select-Object -First 1
    Assert-True ($null -ne $extSection) "Template $Class missing '## Extension points used' heading (extension-points-used-section-missing-or-malformed)."
    Assert-True ($extSection.Body.Trim() -eq 'None.') "Template $Class 'Extension points used' body must equal exactly 'None.' (extension-points-used-section-missing-or-malformed)."

    # Mechanical substitution and re-validation
    $substituted = [regex]::Replace($raw, $SanctionedMarker, 'filled')
    $parsedPost = ConvertFrom-FrontmatterText -Text $substituted
    $fmPost = $parsedPost.Frontmatter
    $headingsPost = Get-Headings -Body $parsedPost.Body
    $resolvedPost = Resolve-RequiredSections -Terms $terms -Headings $headingsPost
    Assert-True ($resolvedPost.Unsatisfied.Count -eq 0) "Substituted template $Class has unsatisfied required sections: $($resolvedPost.Unsatisfied -join ', ')"

    $normalizedSubstituted = Get-PlaceholderNormalizedText -Text $substituted
    foreach ($word in @('TBD', 'TODO', 'FIXME')) {
        Assert-True (-not [regex]::IsMatch($normalizedSubstituted, "(?i)\b$word\b")) "Substituted template $Class still contains $word."
    }
    Assert-True (-not $normalizedSubstituted.Contains('{{')) "Substituted template $Class still contains '{{'."
    Assert-True (-not [regex]::IsMatch($normalizedSubstituted, '\[REPLACE:[^\]]+\]')) "Substituted template $Class still contains a [REPLACE: ...] marker."

    $TemplateCheckResults.Add([ordered]@{ template = $Class; pass = $true }) | Out-Null
    return [pscustomobject]@{ FmPre = $fmPre; BodyPre = $parsedPre.Body; HeadingsPre = $headingsPre; Terms = $terms }
}

$TemplateInfo = @{}
foreach ($cls in $TemplateClasses) {
    $TemplateInfo[$cls] = Test-Template -Class $cls
}
Add-PassedCheck -Number 8 -Name 'eight per-delivery-class templates (AC-P3A-04)'

# ---------------------------------------------------------------------------
# Check 9: twelve fixtures/p3a/*.json (eight positive, four negative)
# ---------------------------------------------------------------------------

$FixtureResults = New-Object 'System.Collections.Generic.List[object]'
$FixturesDir = Join-Path $RepositoryRoot 'docs/specs/schemas/fixtures/p3a'

foreach ($fixtureName in $PositiveFixtureNames) {
    $fixturePath = Join-Path $FixturesDir "$fixtureName.json"
    $fixtureDoc = [IO.File]::ReadAllText($fixturePath) | ConvertFrom-Json
    $fixtureKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $fixtureDoc)
    Assert-SequenceEqual -Actual $fixtureKeys -Expected (Sort-Ordinal -Values @('input', 'expected')) -Label "$fixtureName top-level keys"

    $specPath = [string]$fixtureDoc.input.specPath
    $fullPath = Join-Path $RepositoryRoot $specPath
    Assert-True (Test-Path -LiteralPath $fullPath) "$fixtureName specPath does not exist: $specPath"
    $raw = [IO.File]::ReadAllText($fullPath)
    $substituted = [regex]::Replace($raw, $SanctionedMarker, 'filled')
    $parsed = ConvertFrom-FrontmatterText -Text $substituted
    $headings = Get-Headings -Body $parsed.Body
    $result = Test-ParcelSpec -Frontmatter $parsed.Frontmatter -Headings $headings -RawScanText $substituted -ControlsDoc $ControlsDoc -ExtensionKeys $ExtensionKeysRegistry
    Assert-True ([StringComparer]::Ordinal.Equals($result.Result, [string]$fixtureDoc.expected.result)) "$fixtureName expected.result mismatch. Expected $($fixtureDoc.expected.result), got $($result.Result) ($($result.ReasonDetail))."
    Assert-True ([StringComparer]::Ordinal.Equals($result.Result, 'valid')) "$fixtureName must resolve valid."
    $FixtureResults.Add([ordered]@{ fixture = $fixtureName; pass = $true }) | Out-Null
}

foreach ($fixtureName in $NegativeFixtureNames) {
    $fixturePath = Join-Path $FixturesDir "$fixtureName.json"
    $fixtureDoc = [IO.File]::ReadAllText($fixturePath) | ConvertFrom-Json
    $fixtureKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $fixtureDoc)
    Assert-SequenceEqual -Actual $fixtureKeys -Expected (Sort-Ordinal -Values @('input', 'expected')) -Label "$fixtureName top-level keys"

    $synth = $fixtureDoc.input.syntheticSpec
    $fmOrdered = New-Object System.Collections.Specialized.OrderedDictionary
    foreach ($name in (Get-PropNames -Obj $synth.frontmatter)) { $fmOrdered[$name] = Get-PropValue -Obj $synth.frontmatter -Name $name }
    $fm = @{}
    foreach ($k in $fmOrdered.Keys) { $fm[$k] = $fmOrdered[$k] }
    $headings = @($synth.headings | ForEach-Object { [regex]::Replace([string]$_, '^#{2,3}\s+', '').Trim() })
    $bodyText = [string]$synth.body
    # Join with explicit spaces (not bare newlines): the pinned normalization
    # pipeline (step 7) strips Cc/Cf codepoints, including \n, with no
    # substitution, so a bare newline separator would fuse adjacent words.
    $scanText = ($bodyText + ' ' + (($headings) -join ' ') + ' ' + (($fm.Values | Where-Object { $_ -is [string] }) -join ' '))

    if ($fixtureName -in @('negative-tbd-violation-literal', 'negative-tbd-violation-entity-disguised')) {
        $matchCount = Get-PlaceholderMatchCount -Text $scanText
        Assert-True ($matchCount -eq 1) "fixture-not-independently-dispositive: $fixtureName body must contain exactly one placeholder-pattern occurrence after normalization, found $matchCount."
    }

    $result = Test-ParcelSpec -Frontmatter $fmOrdered -Headings $headings -RawScanText $scanText -ControlsDoc $ControlsDoc -ExtensionKeys $ExtensionKeysRegistry
    Assert-True ([StringComparer]::Ordinal.Equals($result.Result, [string]$fixtureDoc.expected.result)) "$fixtureName expected.result mismatch. Expected $($fixtureDoc.expected.result), got $($result.Result)."
    Assert-True ([StringComparer]::Ordinal.Equals($result.ReasonCode, [string]$fixtureDoc.expected.reason)) "$fixtureName expected.reason mismatch. Expected $($fixtureDoc.expected.reason), got $($result.ReasonCode)."
    $FixtureResults.Add([ordered]@{ fixture = $fixtureName; pass = $true }) | Out-Null
}
Add-PassedCheck -Number 9 -Name 'twelve fixtures/p3a/*.json reproduced exactly (AC-P3A-05)'

# ---------------------------------------------------------------------------
# Check 10: REAL-SPEC-COMPATIBILITY-SET.md vs frozen P2 census
# ---------------------------------------------------------------------------

$RegressionMapPath = Join-Path $RepositoryRoot 'docs/specs/schemas/AXIS-REGRESSION-MAP.md'
$RegressionMapText = [IO.File]::ReadAllText($RegressionMapPath)
$RegressionMapLines = $RegressionMapText -split "`r?`n"
$regressionRows = @($RegressionMapLines | Where-Object { $_.StartsWith('| docs/specs/') })
$FrozenCensus = New-Object 'System.Collections.Generic.List[object]'
foreach ($row in $regressionRows) {
    $cells = $row.Trim().Trim('|').Split('|') | ForEach-Object { $_.Trim() }
    $FrozenCensus.Add([pscustomobject]@{ Path = $cells[0]; Disposition = $cells[-1] }) | Out-Null
}

$CompatSetPath = Join-Path $RepositoryRoot 'docs/specs/schemas/fixtures/p3a/REAL-SPEC-COMPATIBILITY-SET.md'
$CompatSetText = [IO.File]::ReadAllText($CompatSetPath)
$CompatSetLines = $CompatSetText -split "`r?`n"
$RequiredCompatHeader = '| Spec file | Shape | AXIS-REGRESSION-MAP disposition | parcel-spec.schema.json result | Reason | Agreement |'
$compatHeaderLine = $CompatSetLines | Where-Object { $_.Trim() -eq $RequiredCompatHeader } | Select-Object -First 1
Assert-True ($null -ne $compatHeaderLine) 'REAL-SPEC-COMPATIBILITY-SET.md missing exact required header row.'

$compatDataRows = @($CompatSetLines | Where-Object { $_.StartsWith('| docs/specs/') })
Assert-True ($compatDataRows.Count -eq $FrozenCensus.Count) "REAL-SPEC-COMPATIBILITY-SET.md row count mismatch. Expected $($FrozenCensus.Count), got $($compatDataRows.Count)."

$compatByPath = @{}
foreach ($row in $compatDataRows) {
    $cells = $row.Trim().Trim('|').Split('|') | ForEach-Object { $_.Trim() }
    Assert-True ($cells.Count -eq 6) "REAL-SPEC-COMPATIBILITY-SET.md malformed row: $row"
    $compatByPath[$cells[0]] = [pscustomobject]@{ Shape = $cells[1]; Disposition = $cells[2]; Result = $cells[3]; Reason = $cells[4]; Agreement = $cells[5] }
}

[string[]]$CensusPathSet = Sort-Ordinal -Values @($FrozenCensus | ForEach-Object { $_.Path })
[string[]]$CompatPathSet = Sort-Ordinal -Values @($compatByPath.Keys)
Assert-SequenceEqual -Actual $CompatPathSet -Expected $CensusPathSet -Label 'REAL-SPEC-COMPATIBILITY-SET.md Spec file set vs frozen AXIS-REGRESSION-MAP.md census'

$CompatCheckResults = New-Object 'System.Collections.Generic.List[object]'
foreach ($entry in $FrozenCensus) {
    $path = $entry.Path
    $recorded = $compatByPath[$path]
    if ($path.EndsWith('.shaping-result.json')) {
        Assert-True ([StringComparer]::Ordinal.Equals($recorded.Shape, 'n/a (non-spec artifact)')) "$path Shape must be 'n/a (non-spec artifact)'."
        Assert-True ([StringComparer]::Ordinal.Equals($recorded.Result, 'n/a')) "$path result must be n/a."
        Assert-True ([StringComparer]::Ordinal.Equals($recorded.Reason, 'n/a')) "$path Reason must be n/a."
        $expectedAgreement = if ($entry.Disposition -eq 'needs-reconciliation' -or $entry.Disposition -eq 'conforms') { 'consistent' } else { 'flagged-for-human-review' }
        Assert-True ([StringComparer]::Ordinal.Equals($recorded.Agreement, $expectedAgreement)) "$path Agreement mismatch. Expected $expectedAgreement, got $($recorded.Agreement)."
        $CompatCheckResults.Add([ordered]@{ path = $path; pass = $true }) | Out-Null
        continue
    }
    Assert-True ([StringComparer]::Ordinal.Equals($recorded.Shape, 'ticket-spec')) "$path Shape must be ticket-spec."
    $fullPath = Join-Path $RepositoryRoot $path
    $raw = [IO.File]::ReadAllText($fullPath)
    $parsed = ConvertFrom-FrontmatterText -Text $raw
    if ($null -eq $parsed) {
        $computedResult = 'invalid'
        $computedReasonDetail = 'missing-required-frontmatter-key (no YAML frontmatter block found)'
    } else {
        $headings = Get-Headings -Body $parsed.Body
        $r = Test-ParcelSpec -Frontmatter $parsed.Frontmatter -Headings $headings -RawScanText $raw -ControlsDoc $ControlsDoc -ExtensionKeys $ExtensionKeysRegistry
        $computedResult = $r.Result
        $computedReasonDetail = $r.ReasonDetail
    }
    Assert-True ([StringComparer]::Ordinal.Equals($recorded.Result, $computedResult)) "$path parcel-spec.schema.json result mismatch. Recorded $($recorded.Result), independently computed $computedResult."
    Assert-True ([StringComparer]::Ordinal.Equals($recorded.Reason, $computedReasonDetail)) "$path Reason mismatch. Recorded '$($recorded.Reason)', independently computed '$computedReasonDetail'."
    $expectedAgreement = if (($entry.Disposition -eq 'needs-reconciliation' -and $computedResult -eq 'invalid') -or ($entry.Disposition -eq 'conforms' -and $computedResult -eq 'valid')) { 'consistent' } else { 'flagged-for-human-review' }
    Assert-True ([StringComparer]::Ordinal.Equals($recorded.Agreement, $expectedAgreement)) "$path Agreement mismatch. Expected $expectedAgreement, got $($recorded.Agreement)."
    $CompatCheckResults.Add([ordered]@{ path = $path; pass = $true }) | Out-Null
}

# R1-F1: carry-over-item-3 disposition. No `coordinator-parcel`-shape fixture
# or compatibility-set row exists in this parcel (per document contract 1's
# `appliesFrom`, that shape binds P3-B.md-forward); this is not silently
# omitted but must be named, in the chosen allowed file, as an explicit,
# bounded carry-forward obligation that a future dispatch discharges or
# re-affirms.
[string]$CarryOverItem3Sentence = 'This gap is not discharged by this remediation parcel: **P3-B''s own dispatch must either (a) include a fixture or compatibility-set row exercising the `coordinator-parcel` branch against its own conforming spec file, or (b) explicitly re-affirm this gap''s continuation with a named reason.**'
Assert-True ($CompatSetText.Contains($CarryOverItem3Sentence)) 'REAL-SPEC-COMPATIBILITY-SET.md missing the pinned carry-over-item-3 (coordinator-parcel shape) disposition sentence.'
Add-PassedCheck -Number 10 -Name 'REAL-SPEC-COMPATIBILITY-SET.md frozen-P2-census compatibility pass incl. carry-over-item-3 disposition (AC-P3A-07/11)'

# ---------------------------------------------------------------------------
# Check 11: INDEX.md / README.md bounded amendments
# ---------------------------------------------------------------------------

$indexUnifiedDiff = @(Invoke-Git -Arguments @('diff', "$BaseCommit...HEAD", '--', 'docs/specs/INDEX.md'))
$addedIndexLines = @($indexUnifiedDiff | Where-Object { $_.StartsWith('+') -and -not $_.StartsWith('+++') })
$removedIndexLines = @($indexUnifiedDiff | Where-Object { $_.StartsWith('-') -and -not $_.StartsWith('---') })
$addedP3ARows = @($addedIndexLines | Where-Object { $_ -match '^\+\| P3-A \|' })
Assert-True ($addedP3ARows.Count -eq 1) 'INDEX.md diff must add exactly one row matching ^\| P3-A \|.'
Assert-True ($removedIndexLines.Count -eq 0) 'INDEX.md diff must remove zero lines.'

$indexHeadText = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'docs/specs/INDEX.md'))
$p3aRowMatch = [regex]::Match($indexHeadText, '(?m)^\| P3-A \|.*$')
Assert-True $p3aRowMatch.Success 'INDEX.md at HEAD must contain the P3-A row.'
$p3aCells = $p3aRowMatch.Value.Trim().Trim('|').Split('|') | ForEach-Object { $_.Trim() }
Assert-True ($p3aCells.Count -eq 10) 'INDEX.md P3-A row must have exactly ten cells.'
Assert-True ([StringComparer]::Ordinal.Equals($p3aCells[0], 'P3-A')) 'INDEX.md P3-A row Parcel cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p3aCells[1], 'review-candidate')) 'INDEX.md P3-A row Status cell mismatch.'
# R2-F1: resolve the Spec/Goal Charter cells' hrefs to the pinned literal
# paths via captured regex groups, not merely assert shape (the adversarial
# bogus-href exploit, e.g. a syntactically valid markdown link pointing
# somewhere else, must fail).
[string]$PinnedP3ASpecHref = '../INITIATIVES/biostack-governed-delivery/parcels/P3-A.md'
[string]$PinnedP3ACharterHref = '../INITIATIVES/biostack-governed-delivery/CHARTER.md'
$p3aSpecCellMatch = [regex]::Match($p3aCells[2], '^\[P3-A[^\]]*\]\(([^)]+)\)$')
Assert-True $p3aSpecCellMatch.Success 'INDEX.md P3-A row Spec cell must be a markdown link to this file.'
Assert-True ([StringComparer]::Ordinal.Equals($p3aSpecCellMatch.Groups[1].Value, $PinnedP3ASpecHref)) "INDEX.md P3-A row Spec cell href must equal '$PinnedP3ASpecHref' exactly, got '$($p3aSpecCellMatch.Groups[1].Value)'."
$p3aCharterCellMatch = [regex]::Match($p3aCells[3], '^\[[^\]]*charter[^\]]*\]\(([^)]+)\)$')
Assert-True $p3aCharterCellMatch.Success 'INDEX.md P3-A row Goal Charter cell must be a markdown link target naming the charter.'
Assert-True ([StringComparer]::Ordinal.Equals($p3aCharterCellMatch.Groups[1].Value, $PinnedP3ACharterHref)) "INDEX.md P3-A row Goal Charter cell href must equal '$PinnedP3ACharterHref' exactly, got '$($p3aCharterCellMatch.Groups[1].Value)'."
Assert-True ([StringComparer]::Ordinal.Equals($p3aCells[4], 'standard; architecture')) 'INDEX.md P3-A row Delivery classes cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p3aCells[5], 'not-applicable')) 'INDEX.md P3-A row Guidance classes cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p3aCells[6], 'coordinator-assigns-at-gate-2')) 'INDEX.md P3-A row Branch/worktree cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p3aCells[7], 'coordinator-assigns-at-gate-2')) 'INDEX.md P3-A row Owner cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p3aCells[8], '2 independent reviewers')) 'INDEX.md P3-A row Review requirement cell mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals($p3aCells[9], 'not-yet-closed')) 'INDEX.md P3-A row Closure cell mismatch.'

$readmeUnifiedDiff = @(Invoke-Git -Arguments @('diff', "$BaseCommit...HEAD", '--', 'docs/specs/README.md'))
$removedReadmeLines = @($readmeUnifiedDiff | Where-Object { $_.StartsWith('-') -and -not $_.StartsWith('---') })
Assert-True ($removedReadmeLines.Count -eq 0) 'README.md diff must remove zero lines.'
$readmeHeadText = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'docs/specs/README.md'))
Assert-True ($readmeHeadText.Contains('## Parcel-spec schema, templates, and extension points (P3-A)')) 'README.md missing required P3-A section heading.'
foreach ($link in @('schemas/parcel-spec.schema.json', 'schemas/SECTION-HEADING-MAP.md', 'schemas/EXTENSION-POINTS.md', 'templates/README.md')) {
    Assert-True ($readmeHeadText.Contains($link)) "README.md P3-A section missing link to $link."
}
Assert-True ($readmeHeadText.Contains('P3-A composes P2''s axis/fold substrate into a generic spec contract and invents no product capability semantics.')) 'README.md missing required P3-A scope sentence.'
Add-PassedCheck -Number 11 -Name 'bounded INDEX.md/README.md amendments (AC-P3A-10) with pinned cell/content values'

# ---------------------------------------------------------------------------
# Check 12: placeholder scan across every file this parcel ships
# ---------------------------------------------------------------------------

# parcel-spec.schema.json is excluded from this prose scan: document contract 1
# pins its noPlaceholderPatterns/sanctionedTemplateFillInMarker field VALUES to
# literally contain the regex pattern text "(TBD|TODO|FIXME)" as DATA (the
# banned-literal detector's own pattern definition), not as unresolved
# placeholder prose; check 5 already asserts that file's exact pinned content
# byte-for-byte, which is the authoritative correctness check for those
# fields. Scanning it here would make the schema's own, required, pinned
# pattern text fail a check designed to catch incomplete authoring prose.
[string[]]$ModifiedSurfaces = @('docs/specs/README.md', 'docs/specs/INDEX.md')
# The two no-TBD negative fixtures deliberately carry a single literal or
# HTML-entity-disguised TBD occurrence as their one proof-of-detection
# violation (document contract 6); check 9 already independently re-derives
# and asserts their expected invalid/placeholder-violation result against the
# embedded resolver, which is the authoritative proof for those two files.
# verify-p3a.ps1 itself is structurally required to contain the exact pinned
# noPlaceholderPatterns literal strings (to assert their byte-for-byte
# equality against parcel-spec.schema.json in check 5) and the two pinned
# fixture filenames; both necessarily contain the contiguous substring
# "TBD" as pinned data, the same structural necessity as the schema file.
[string[]]$PlaceholderScanExclusions = @(
    'docs/specs/schemas/parcel-spec.schema.json',
    'docs/specs/schemas/fixtures/p3a/negative-tbd-violation-literal.json',
    'docs/specs/schemas/fixtures/p3a/negative-tbd-violation-entity-disguised.json',
    'docs/specs/scripts/verify-p3a.ps1'
)
foreach ($surface in $AllowedSurfaces) {
    if ($PlaceholderScanExclusions -contains $surface) { continue }
    if ($ModifiedSurfaces -contains $surface) {
        $unifiedDiff = @(Invoke-Git -Arguments @('diff', "$BaseCommit...HEAD", '--', $surface))
        $addedLines = @($unifiedDiff | Where-Object { $_.StartsWith('+') -and -not $_.StartsWith('+++') } | ForEach-Object { $_.Substring(1) })
        $content = $addedLines -join "`n"
    } else {
        $content = [IO.File]::ReadAllText((Join-Path $RepositoryRoot $surface))
    }
    Assert-True (-not (Test-PlaceholderViolation -Text $content)) "Unresolved placeholder found in $surface after normalization."

    if ($surface -notmatch '^docs/specs/templates/') {
        $normalized = Get-PlaceholderNormalizedText -Text $content
        Assert-True (-not [regex]::IsMatch($normalized, $SanctionedMarker)) "Sanctioned [REPLACE: ...] marker found outside docs/specs/templates/** in $surface."
    }
}
foreach ($cls in $TemplateClasses) {
    $surface = "docs/specs/templates/parcel-template.$cls.md"
    $content = [IO.File]::ReadAllText((Join-Path $RepositoryRoot $surface))
    $normalized = Get-PlaceholderNormalizedText -Text $content
    Assert-True ([regex]::IsMatch($normalized, $SanctionedMarker)) "Template $surface contains no [REPLACE: ...] occurrence."
}
Add-PassedCheck -Number 12 -Name 'unresolved placeholder scan incl. normalization pipeline (AC-P3A-06)'

# ---------------------------------------------------------------------------
# Check 13/14: evidence bundle and clean tree
# ---------------------------------------------------------------------------

function Assert-OnlyAuthorizedEvidenceStatus {
    param([string[]]$StatusLines)
    foreach ($line in $StatusLines) {
        Assert-True $line.StartsWith('?? artifacts/p3a-verification/', [StringComparison]::Ordinal) "Unauthorized or tracked worktree status entry: $line"
    }
}

$expectedEvidencePath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot 'artifacts/p3a-verification'))
$resolvedEvidencePath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $EvidenceDirectory))
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($resolvedEvidencePath.TrimEnd('\', '/'), $expectedEvidencePath.TrimEnd('\', '/'))) 'EvidenceDirectory must resolve to artifacts/p3a-verification under the repository root.'

$trackedEvidence = Get-GitResult -Arguments @('ls-files', '--error-unmatch', '--', 'artifacts/p3a-verification')
Assert-True ($trackedEvidence.ExitCode -ne 0) 'artifacts/p3a-verification must be untracked.'
[IO.Directory]::CreateDirectory($resolvedEvidencePath) | Out-Null

$headCommit = (@(Invoke-Git -Arguments @('rev-parse', 'HEAD')))[0].Trim()

Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'changed-files.txt') -Content ($ActualChanges -join "`n")
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'schema-check.json') -Content ((([ordered]@{ schema = 'biostack.p3a-schema-check.v1'; pass = $true; topLevelKeys = $Pinned15Keys }) | ConvertTo-Json -Depth 10))
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'heading-map-check.json') -Content ((([ordered]@{ schema = 'biostack.p3a-heading-map-check.v1'; pass = $true; termCount = $LiveUnionSorted.Count }) | ConvertTo-Json -Depth 10))
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'extension-points-check.json') -Content ((([ordered]@{ schema = 'biostack.p3a-extension-points-check.v1'; pass = $true; extensionPoints = @('delivery-class-extension', 'domain-overlay-insertion', 'template-set-extension') }) | ConvertTo-Json -Depth 10))
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'template-check.json') -Content (($TemplateCheckResults | ConvertTo-Json -Depth 10))
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'fixture-results.json') -Content (($FixtureResults | ConvertTo-Json -Depth 10))
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'compatibility-set-check.json') -Content (($CompatCheckResults | ConvertTo-Json -Depth 10))

$preSummaryStatus = @(Invoke-Git -Arguments @('status', '--porcelain=v1', '--untracked-files=all'))
Assert-OnlyAuthorizedEvidenceStatus -StatusLines $preSummaryStatus
Add-PassedCheck -Number 13 -Name 'authorized UTF-8/LF evidence bundle generation'

$summary = [ordered]@{
    schema = 'biostack.p3a-verification-summary.v1'
    pass = $true
    baseCommit = $BaseCommit.ToLowerInvariant()
    headCommit = $headCommit
    builderId = $BuilderId
    reviewerIds = [string[]]$ReviewerIds
    checks = $CheckResults.ToArray()
}
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'verification-summary.json') -Content ($summary | ConvertTo-Json -Depth 12)

[string[]]$ExpectedEvidenceFiles = @(
    'changed-files.txt', 'schema-check.json', 'heading-map-check.json', 'extension-points-check.json',
    'template-check.json', 'fixture-results.json', 'compatibility-set-check.json', 'verification-summary.json'
)
foreach ($evidenceFile in $ExpectedEvidenceFiles) {
    Assert-True (Test-Path -LiteralPath (Join-Path $resolvedEvidencePath $evidenceFile)) "Missing evidence file: $evidenceFile"
}

$finalStatus = @(Invoke-Git -Arguments @('status', '--porcelain=v1', '--untracked-files=all'))
Assert-OnlyAuthorizedEvidenceStatus -StatusLines $finalStatus
Add-PassedCheck -Number 14 -Name 'untracked evidence directory and clean tree'

Write-Output 'P3-A verification PASS'
exit 0
