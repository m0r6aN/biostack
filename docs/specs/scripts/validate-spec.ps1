[CmdletBinding(DefaultParameterSetName = 'BySpecPath')]
param(
    [Parameter(ParameterSetName = 'BySpecPath', Mandatory = $true)]
    [string]$SpecPath,

    [Parameter(ParameterSetName = 'BySyntheticSpecJson', Mandatory = $true)]
    [string]$SyntheticSpecJson,

    [Parameter()]
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

# =============================================================================
# docs/specs/scripts/validate-spec.ps1 (P4)
#
# The dependency-light, general-purpose, invocable spec linter (document
# contract 1 of docs/INITIATIVES/biostack-governed-delivery/parcels/P4.md).
# Exposes one externally callable entrypoint, `Invoke-SpecValidation`, and a
# CLI surface accepting either `-SpecPath` (a real on-disk file) or
# `-SyntheticSpecJson` (a JSON file carrying an inlined `frontmatter`/
# `headings`/`declaredShape` synthetic spec, P3-A's own `syntheticSpec`
# convention), both sharing this one implementation. No network access, no
# non-PowerShell dependency; every input is read live from disk at
# validation time (Hard constraints, "Composed, not hardcoded").
# =============================================================================

# -----------------------------------------------------------------------------
# Generic helpers (ported, byte-identical in spirit, from verify-p3a.ps1 /
# verify-p3b.ps1's own pattern)
# -----------------------------------------------------------------------------

function Get-PropNames {
    param($Obj)
    if ($null -eq $Obj) { return @() }
    if ($Obj -is [System.Collections.IDictionary]) { return @($Obj.Keys) }
    $names = $Obj.PSObject.Properties.Name
    if ($null -eq $names) { return @() }
    return @($names)
}

function Test-PropPresent {
    param($Obj, [string]$Name)
    if ($null -eq $Obj) { return $false }
    if ($Obj -is [System.Collections.IDictionary]) { return $Obj.Contains($Name) }
    return ($Obj.PSObject.Properties.Name -contains $Name)
}

function Get-PropValue {
    param($Obj, [string]$Name)
    if ($null -eq $Obj) { return $null }
    if (-not (Test-PropPresent -Obj $Obj -Name $Name)) { return $null }
    # Deliberately no comma-wrapping here: every call site already wraps this
    # function's result in @(...) for array-valued properties (the same
    # convention verify-p3b.ps1's own Get-PropValue documents) -- wrapping
    # here as well would double-nest the result through PowerShell's
    # pipeline-unrolling semantics. Presence/emptiness of a possibly-empty
    # array property is decided by the dedicated Test-FrontmatterKeyState
    # helper, below, which never routes an array value through a function
    # return boundary.
    if ($Obj -is [System.Collections.IDictionary]) { return $Obj[$Name] }
    return $Obj.$Name
}

function Test-FrontmatterKeyState {
    # Returns @{ Present; IsEmpty } for a frontmatter key, type-agnostic over
    # OrderedDictionary (disk-path mode) and PSCustomObject (synthetic mode),
    # without ever passing a possibly-empty array value through a function
    # return boundary (the flattening hazard Get-PropValue's own comment
    # documents).
    param($Obj, [string]$Name)
    if ($null -eq $Obj) { return [pscustomobject]@{ Present = $false; IsEmpty = $true } }
    $present = Test-PropPresent -Obj $Obj -Name $Name
    if (-not $present) { return [pscustomobject]@{ Present = $false; IsEmpty = $true } }
    $raw = $null
    if ($Obj -is [System.Collections.IDictionary]) { $raw = $Obj[$Name] } else { $raw = $Obj.$Name }
    if ($raw -is [System.Array]) { return [pscustomobject]@{ Present = $true; IsEmpty = $false } }
    $isEmpty = (($null -eq $raw) -or ($raw -is [string] -and [string]::IsNullOrWhiteSpace($raw)))
    return [pscustomobject]@{ Present = $true; IsEmpty = $isEmpty }
}

function Get-PropArray {
    param($Obj, [string]$Name)
    $v = Get-PropValue -Obj $Obj -Name $Name
    if ($null -eq $v) { return , [object[]]@() }
    if ($v -is [System.Array]) { return , $v }
    return , [object[]]@($v)
}

function Sort-Ordinal {
    param([string[]]$Values)
    [string[]]$copy = @($Values)
    [Array]::Sort($copy, [StringComparer]::Ordinal)
    return , $copy
}

function ConvertTo-StrictInt {
    param($Value)
    if ($null -eq $Value) { return $null }
    if ($Value -is [int]) { return $Value }
    if ($Value -is [long] -and $Value -ge [int]::MinValue -and $Value -le [int]::MaxValue) { return [int]$Value }
    if ($Value -is [double] -or $Value -is [decimal]) {
        if ($Value -ne [Math]::Floor($Value)) { return $null }
        return [int]$Value
    }
    if ($Value -is [string]) {
        $parsed = 0
        if ([int]::TryParse($Value, [System.Globalization.NumberStyles]::Integer, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) { return $parsed }
        return $null
    }
    return $null
}

function ConvertTo-StrictBool {
    param($Value)
    if ($Value -is [bool]) { return $Value }
    if ($Value -is [string]) {
        if ([StringComparer]::Ordinal.Equals($Value, 'true')) { return $true }
        if ([StringComparer]::Ordinal.Equals($Value, 'false')) { return $false }
        return $null
    }
    return $null
}

# -----------------------------------------------------------------------------
# Minimal YAML-frontmatter-block parser and heading extractor (disk-path
# mode only), ported from verify-p3a.ps1 / verify-p3b.ps1's own pattern,
# extended to parse nested objects/arrays-of-objects for the six P3-B bound
# fields a real file may carry.
# -----------------------------------------------------------------------------

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

# -----------------------------------------------------------------------------
# Term/heading normalization (SECTION-HEADING-MAP.md's own matching rule)
# -----------------------------------------------------------------------------

function ConvertTo-NormalizedTokens {
    param([string]$Text)
    $s = ([string]$Text).ToLowerInvariant()
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

function Get-HeadingAliasMap {
    # Live-reads SECTION-HEADING-MAP.md's Term map table; returns a hashtable
    # term -> string[] of canonical alias(es) (comma-split), the term itself
    # always included by the caller. This is the one seam check 10(b)'s
    # mutation-overlay probe (rollback row alias append) must flow through.
    param([string]$RepoRoot)
    $path = Join-Path $RepoRoot 'docs/specs/schemas/SECTION-HEADING-MAP.md'
    $text = [IO.File]::ReadAllText($path)
    $lines = $text -split "`r?`n"
    $rows = @($lines | Where-Object { $_.StartsWith('| ') -and -not $_.StartsWith('| Control term') -and -not $_.StartsWith('|---') })
    $map = @{}
    foreach ($row in $rows) {
        $cells = $row.Trim().Trim('|').Split('|') | ForEach-Object { $_.Trim() }
        if ($cells.Count -lt 2) { continue }
        $term = $cells[0]
        [string[]]$aliases = @($cells[1] -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' })
        $map[$term] = $aliases
    }
    return $map
}

function Resolve-RequiredSections {
    # Ported from verify-p3a.ps1's own Resolve-RequiredSections (distinct-
    # heading-per-term bipartite match plus heading-length bound), extended
    # to consult the live alias map.
    param([string[]]$Terms, [string[]]$Headings, $AliasMap)
    $termsSorted = Sort-Ordinal -Values $Terms
    $consumed = New-Object 'System.Collections.Generic.HashSet[int]'
    $unsatisfied = New-Object 'System.Collections.Generic.List[string]'
    $assignment = @{}
    $headingToks = @()
    for ($i = 0; $i -lt $Headings.Count; $i++) { $headingToks += , (ConvertTo-NormalizedTokens -Text $Headings[$i]) }

    foreach ($term in $termsSorted) {
        $phrases = New-Object 'System.Collections.Generic.List[string]'
        $phrases.Add($term) | Out-Null
        if ($null -ne $AliasMap -and $AliasMap.ContainsKey($term)) {
            foreach ($alias in $AliasMap[$term]) {
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

# -----------------------------------------------------------------------------
# Placeholder normalization pipeline (parcel-spec.schema.json's
# placeholderNormalizationSteps, read live for the step order/pattern set;
# the confusables skeleton table itself is a bounded, deterministic, offline
# approximation table -- not a copy of any of the five composed sources --
# ported byte-for-byte from verify-p3a.ps1 / verify-p3b.ps1's own table,
# which both carry the identical scope disclosure).
# -----------------------------------------------------------------------------

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
    param([string]$Text, [string[]]$StepOrder)
    $t = [string]$Text
    foreach ($step in $StepOrder) {
        switch ($step) {
            'strip-html-comments' { $t = [regex]::Replace($t, '<!--[\s\S]*?-->', '') }
            'strip-html-tags' { $t = [regex]::Replace($t, '</?[a-zA-Z][a-zA-Z0-9]*(?:\s[^>]*)?>', '') }
            'strip-inline-code-delimiters' { $t = [regex]::Replace($t, '`+', '') }
            'strip-emphasis-markers' {
                $t = [regex]::Replace($t, '\*{1,3}([^*]+)\*{1,3}', '$1')
                $t = [regex]::Replace($t, '_{1,3}([^_]+)_{1,3}', '$1')
            }
            'strip-markdown-escape-backslashes' { $t = [regex]::Replace($t, '\\(?=[!-/:-@\[-`{-~])', '') }
            'decode-html-entities' { $t = [System.Net.WebUtility]::HtmlDecode($t) }
            'strip-unicode-category-Cf-and-Cc' {
                $sb = New-Object System.Text.StringBuilder
                foreach ($ch in $t.ToCharArray()) {
                    $cat = [System.Globalization.CharUnicodeInfo]::GetUnicodeCategory($ch)
                    if ($cat -eq [System.Globalization.UnicodeCategory]::Format -or $cat -eq [System.Globalization.UnicodeCategory]::Control) { continue }
                    [void]$sb.Append($ch)
                }
                $t = $sb.ToString()
            }
            'nfkc-normalize' { $t = $t.Normalize([System.Text.NormalizationForm]::FormKC) }
            'unicode-confusables-skeleton' {
                $sb2 = New-Object System.Text.StringBuilder
                foreach ($ch in $t.ToCharArray()) {
                    if ($script:ConfusablesMap.ContainsKey($ch)) { [void]$sb2.Append($script:ConfusablesMap[$ch]) } else { [void]$sb2.Append($ch) }
                }
                $t = $sb2.ToString()
            }
            default { }
        }
    }
    return $t
}

function Test-PlaceholderViolation {
    param([string]$Text, [string[]]$StepOrder, [string[]]$Patterns)
    $normalized = Get-PlaceholderNormalizedText -Text $Text -StepOrder $StepOrder
    foreach ($pattern in $Patterns) {
        if ([regex]::IsMatch($normalized, $pattern)) { return $true }
    }
    return $false
}

# -----------------------------------------------------------------------------
# pathPattern matching grammar (Resolution order, step 2; first-definition,
# pinned by this parcel's own spec -- full-string-anchored glob, `*` never
# spans `/`, `|`-split alternatives)
# -----------------------------------------------------------------------------

function ConvertTo-GlobRegex {
    param([string]$Pattern)
    $escaped = [regex]::Escape($Pattern).Replace('\*', '[^/]+')
    return '^' + $escaped + '$'
}

function Test-PathPattern {
    param([string]$CandidatePath, [string]$PatternValue)
    $normalizedCandidate = $CandidatePath.Replace('\', '/')
    $alternatives = @($PatternValue -split '\|')
    foreach ($alt in $alternatives) {
        $regex = ConvertTo-GlobRegex -Pattern $alt
        if ([regex]::IsMatch($normalizedCandidate, $regex)) { return $true }
    }
    return $false
}

function Resolve-SpecShapeForPath {
    param([string]$CandidatePath, $SchemaDoc)
    $shapes = Get-PropValue -Obj $SchemaDoc -Name 'specShapes'
    foreach ($shapeName in (Get-PropNames -Obj $shapes)) {
        $shapeDef = Get-PropValue -Obj $shapes -Name $shapeName
        $pattern = [string](Get-PropValue -Obj $shapeDef -Name 'pathPattern')
        if (Test-PathPattern -CandidatePath $CandidatePath -PatternValue $pattern) {
            return $shapeName
        }
    }
    return 'unrecognized-shape'
}

# -----------------------------------------------------------------------------
# D14 fieldwise fold (fold-engine.md's Algorithm, ported from the P2
# reference implementation in verify-p2.ps1's own `Invoke-Fold`, unmodified
# in mechanics -- reads AxesDoc/ControlsDoc live, every call)
# -----------------------------------------------------------------------------

function Invoke-SpecFold {
    param($DeliveryClasses, $GuidanceClasses, $SubstanceRisk, $ConditionalInputs, $AxesDoc, $ControlsDoc, [bool]$FixtureOnly, $TestOnlyScalarValues)

    $deliveryLabels = @((Get-PropValue -Obj (Get-PropValue -Obj $AxesDoc.axes -Name 'deliveryClass') -Name 'labels'))
    $guidanceLabels = @((Get-PropValue -Obj (Get-PropValue -Obj $AxesDoc.axes -Name 'productGuidanceClass') -Name 'labels'))
    $substanceLabels = @((Get-PropValue -Obj (Get-PropValue -Obj $AxesDoc.axes -Name 'substanceFunctionRisk') -Name 'labels'))

    $deliveryClasses = @($DeliveryClasses)
    $guidanceClasses = @($GuidanceClasses)
    $substanceRisk = @($SubstanceRisk)

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

    if ($deliveryClasses.Count -eq 0) {
        return @{ schema = 'biostack.p2-routing-output.v1'; result = 'stopped'; stopReason = @{ reason = 'empty-required-axis'; axis = 'deliveryClass' } }
    }

    if ($FixtureOnly -and $null -ne $TestOnlyScalarValues) {
        [object[]]$vals = @()
        foreach ($c in $deliveryClasses) {
            $v = Get-PropValue -Obj $TestOnlyScalarValues -Name $c
            if ($null -ne $v) { $vals += $v }
        }
        $distinct = [object[]]($vals | Select-Object -Unique)
        if ($distinct.Count -gt 1) {
            return @{ schema = 'biostack.p2-routing-output.v1'; result = 'stopped'; stopReason = @{ reason = 'incompatible-controls'; field = 'testOnlyScalarField'; conflictingValues = $vals } }
        }
    }

    [int[]]$reviewerValues = @()
    foreach ($c in $deliveryClasses) {
        $ctrl = Get-PropValue -Obj $ControlsDoc -Name $c
        $r = Get-PropValue -Obj $ctrl -Name 'reviewers'
        if ($r -is [int] -or $r -is [long] -or $r -is [double]) {
            $reviewerValues += [int]$r
        } else {
            $condName = [string](Get-PropValue -Obj $r -Name 'condition')
            $hasCond = Test-PropPresent -Obj $ConditionalInputs -Name $condName
            if (-not $hasCond) {
                return @{ schema = 'biostack.p2-routing-output.v1'; result = 'stopped'; stopReason = @{ reason = 'missing-required-field'; deliveryClass = $c; field = $condName } }
            }
            $condVal = Get-PropValue -Obj $ConditionalInputs -Name $condName
            if ($condVal -eq $true) { $reviewerValues += [int](Get-PropValue -Obj $r -Name 'ifTrue') } else { $reviewerValues += [int](Get-PropValue -Obj $r -Name 'default') }
        }
    }
    $reviewerCount = ($reviewerValues | Measure-Object -Maximum).Maximum

    [string[]]$requiredSpecAdditions = @()
    [string[]]$requiredChecks = @()
    [string[]]$mandatoryStopConditions = @()
    [string[]]$requiredClosureEvidence = @()
    [string[]]$mergeGates = @()
    foreach ($c in $deliveryClasses) {
        $ctrl = Get-PropValue -Obj $ControlsDoc -Name $c
        foreach ($v in [string[]]@(Get-PropValue -Obj $ctrl -Name 'requiredSpecAdditions')) { if ($requiredSpecAdditions -notcontains $v) { $requiredSpecAdditions += $v } }
        foreach ($v in [string[]]@(Get-PropValue -Obj $ctrl -Name 'minimumChecks')) { if ($requiredChecks -notcontains $v) { $requiredChecks += $v } }
        foreach ($v in [string[]]@(Get-PropValue -Obj $ctrl -Name 'mandatoryStopConditions')) { if ($mandatoryStopConditions -notcontains $v) { $mandatoryStopConditions += $v } }
        foreach ($v in [string[]]@(Get-PropValue -Obj $ctrl -Name 'requiredClosureEvidence')) { if ($requiredClosureEvidence -notcontains $v) { $requiredClosureEvidence += $v } }
        $amg = Get-PropValue -Obj $ctrl -Name 'additionalMergeGate'
        if ($null -ne $amg) { if ($mergeGates -notcontains $amg) { $mergeGates += [string]$amg } }
    }

    $dispatchAll = $true
    $mergeAll = $true
    foreach ($c in $deliveryClasses) {
        $ctrl = Get-PropValue -Obj $ControlsDoc -Name $c
        if ([string](Get-PropValue -Obj $ctrl -Name 'dispatchEligibility') -ne 'eligible-when-named') { $dispatchAll = $false }
        if ([string](Get-PropValue -Obj $ctrl -Name 'mergeEligibility') -ne 'eligible-when-named') { $mergeAll = $false }
    }
    $standingAuthorizationEligible = $dispatchAll -and $mergeAll

    return @{
        schema                        = 'biostack.p2-routing-output.v1'
        result                        = 'routed'
        requiredSpecAdditions         = @($requiredSpecAdditions)
        requiredChecks                = @($requiredChecks)
        mandatoryStopConditions       = @($mandatoryStopConditions)
        requiredClosureEvidence       = @($requiredClosureEvidence)
        mergeGates                    = @($mergeGates)
        reviewerCount                 = $reviewerCount
        standingAuthorizationEligible = $standingAuthorizationEligible
        recordedGuidanceClasses       = @($guidanceClasses)
        recordedSubstanceFunctionRisk = @($substanceRisk)
    }
}

# -----------------------------------------------------------------------------
# CAPABILITY-FIELD-MAP.md six-row binding (stage 7), collapsed to the four
# reason literals this parcel's own Hard constraints pin for this stage:
# `missing-required-frontmatter-key`, `invalid-function-review-status`,
# `capability-claim-drift`, `premature-public-enablement-claim`. The six
# known rows carry dedicated logic (each reading its own live
# cross-reference cell out of product-capability-safety-contract.json at
# call time -- never a cached/static copy); any further row appended to the
# live CAPABILITY-FIELD-MAP.md table (beyond the six named keys) is picked
# up generically via its own live "Required when"/"Value shape" cell text
# (the "unconditional" / "must equal the pinned literal `<value>`" patterns
# -- check 10(c)'s own pinned mutation shape), proving this stage is driven
# by the live file, not by a hardcoded six-field enumeration.
# -----------------------------------------------------------------------------

[string[]]$script:GuidanceClassLabels = @(
    'deterministic-calculation', 'curated-evidence-guidance',
    'personalized-protocol-recommendation', 'safety-escalation'
)
[string[]]$script:SubstanceFunctionRiskLabels = @(
    'ordinary', 'prescription-treatment-involved', 'investigational-or-unapproved',
    'gray-market-or-identity-uncertain', 'injection-or-sterile-preparation',
    'interaction-or-contraindication-signal', 'minor-or-age-uncertain',
    'pregnancy-or-lactation', 'acute-red-flag-or-emergency',
    'controlled-or-illegal-sourcing'
)
[string[]]$script:FunctionReviewStatusLabels = @('unreviewed', 'review-required', 'reviewed', 'not-applicable')
[string[]]$script:RungApplicationLabels = @(
    'rung-1-refuse-invalid', 'rung-2-degrade-naming-missingness',
    'rung-2-refuse-safety-material', 'rung-3-marker'
)
[string[]]$script:KnownCapabilityFieldMapKeys = @(
    'function_review_status', 'function_review_owner', 'capability_claim',
    'numeric_provenance', 'missingness', 'escalation'
)

function Get-CapabilityFieldMapRows {
    # Live-parses CAPABILITY-FIELD-MAP.md's six-plus-row table, returning an
    # ordered list of @{ Key; RequiredWhen; ValueShape }. Used only to detect
    # any row beyond the six known keys (the generic fallback path).
    param([string]$RepoRoot)
    $path = Join-Path $RepoRoot 'docs/specs/schemas/CAPABILITY-FIELD-MAP.md'
    $text = [IO.File]::ReadAllText($path)
    $lines = $text -split "`r?`n"
    $dataRows = @($lines | Where-Object { $_.StartsWith('| `') })
    $rows = New-Object 'System.Collections.Generic.List[object]'
    foreach ($row in $dataRows) {
        $cells = $row.Trim().Trim('|').Split('|') | ForEach-Object { $_.Trim() }
        if ($cells.Count -lt 4) { continue }
        $key = $cells[0].Trim('`')
        $rows.Add([pscustomobject]@{ Key = $key; RequiredWhen = $cells[2]; ValueShape = $cells[3] }) | Out-Null
    }
    return $rows
}

function Test-CapabilityFieldBinding {
    param($Frontmatter, $Contract, [string]$RepoRoot, [bool]$SelfReferenceCarveOut = $false)

    $guidanceClasses = Get-PropArray -Obj $Frontmatter -Name 'guidance_classes'
    $substanceRisk = Get-PropArray -Obj $Frontmatter -Name 'substance_function_risk'
    $axesEmpty = ($guidanceClasses.Count -eq 0 -and $substanceRisk.Count -eq 0)

    $checked = New-Object 'System.Collections.Generic.List[object]'

    # --- function_review_status (unconditional, except the narrowly scoped,
    #     by-path self-reference carve-out this parcel inherits verbatim
    #     from P3-B's own already-ratified Hard constraints clause, applying
    #     to exactly `parcels/P3-B.md` and no other file -- required for
    #     AC-P4-05 agreement with verify-p3b.ps1's own check 10.) ---
    $frsPresent = Test-PropPresent -Obj $Frontmatter -Name 'function_review_status'
    if (-not $frsPresent) {
        if ($SelfReferenceCarveOut -and $axesEmpty) {
            $checked.Add([ordered]@{ field = 'function_review_status'; pass = $true }) | Out-Null
            $checked.Add([ordered]@{ field = 'function_review_owner'; pass = $true }) | Out-Null
        } else {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Detail = @{ field = 'function_review_status' }; Checked = $checked.ToArray() }
        }
    } else {
        $frs = [string](Get-PropValue -Obj $Frontmatter -Name 'function_review_status')
        if ($script:FunctionReviewStatusLabels -cnotcontains $frs) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'invalid-function-review-status'; Detail = @{ field = 'function_review_status'; value = $frs }; Checked = $checked.ToArray() }
        }
        $ownerPresent = Test-PropPresent -Obj $Frontmatter -Name 'function_review_owner'
        $ownerVal = $null
        if ($ownerPresent) { $ownerVal = [string](Get-PropValue -Obj $Frontmatter -Name 'function_review_owner') }
        if ($frs -eq 'review-required') {
            if (-not $ownerPresent -or [string]::IsNullOrWhiteSpace($ownerVal)) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Detail = @{ field = 'function_review_owner' }; Checked = $checked.ToArray() }
            }
        } elseif ($ownerPresent) {
            # Value-shape violation: owner present when function_review_status is
            # not review-required (CAPABILITY-FIELD-MAP.md: "absent ... for the
            # other three values"). Collapsed into invalid-function-review-status
            # (the one shape-violation reason this field group owns), per Hard
            # constraints' four-literal closed vocabulary for this stage.
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'invalid-function-review-status'; Detail = @{ field = 'function_review_owner' }; Checked = $checked.ToArray() }
        }
        $checked.Add([ordered]@{ field = 'function_review_status'; pass = $true }) | Out-Null
        $checked.Add([ordered]@{ field = 'function_review_owner'; pass = $true }) | Out-Null
    }

    # --- capability_claim ---
    $claimPresent = Test-PropPresent -Obj $Frontmatter -Name 'capability_claim'
    $claimTriggered = (-not $axesEmpty)
    if ($claimTriggered -and -not $claimPresent) {
        return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Detail = @{ field = 'capability_claim' }; Checked = $checked.ToArray() }
    }

    $claims = @()
    if ($claimPresent) { $claims = Get-PropArray -Obj $Frontmatter -Name 'capability_claim' }

    $numericProvenanceTriggered = $false
    $escalationTriggered = $false

    foreach ($claim in $claims) {
        $guidanceClass = [string](Get-PropValue -Obj $claim -Name 'guidanceClass')
        $label = [string](Get-PropValue -Obj $claim -Name 'label')
        $behavior = [string](Get-PropValue -Obj $claim -Name 'behavior')

        if ($script:GuidanceClassLabels -cnotcontains $guidanceClass) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Detail = @{ field = 'capability_claim'; guidanceClass = $guidanceClass }; Checked = $checked.ToArray() }
        }
        if ($script:SubstanceFunctionRiskLabels -cnotcontains $label) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Detail = @{ field = 'capability_claim'; label = $label }; Checked = $checked.ToArray() }
        }

        if ($guidanceClass -eq 'safety-escalation') {
            if (-not [StringComparer]::Ordinal.Equals($behavior, 'escalated')) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Detail = @{ field = 'capability_claim'; guidanceClass = $guidanceClass; label = $label }; Checked = $checked.ToArray() }
            }
            $escRuleInt = ConvertTo-StrictInt -Value (Get-PropValue -Obj $claim -Name 'escalationRule')
            $ruleCount = @(Get-PropValue -Obj $Contract.escalationSemantics -Name 'rules').Count
            if ($null -eq $escRuleInt -or $escRuleInt -lt 1 -or $escRuleInt -gt $ruleCount) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Detail = @{ field = 'capability_claim'; subField = 'escalationRule' }; Checked = $checked.ToArray() }
            }
        } else {
            $column = switch ($guidanceClass) {
                'deterministic-calculation' { 'C1' }
                'curated-evidence-guidance' { 'C2' }
                'personalized-protocol-recommendation' { 'C3' }
            }
            $liveLabel = Get-PropValue -Obj $Contract.labels -Name $label
            $liveCell = Get-PropValue -Obj (Get-PropValue -Obj $liveLabel -Name 'behavior') -Name $column
            $liveValue = [string](Get-PropValue -Obj $liveCell -Name 'value')
            if (-not [StringComparer]::Ordinal.Equals($behavior, $liveValue)) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Detail = @{ field = 'capability_claim'; guidanceClass = $guidanceClass; label = $label; claimed = $behavior; live = $liveValue }; Checked = $checked.ToArray() }
            }
        }

        if ($guidanceClass -eq 'personalized-protocol-recommendation') {
            if (-not (Test-PropPresent -Obj $claim -Name 'publiclyEnabled')) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Detail = @{ field = 'capability_claim.publiclyEnabled' }; Checked = $checked.ToArray() }
            }
            $claimedPubliclyEnabled = ConvertTo-StrictBool -Value (Get-PropValue -Obj $claim -Name 'publiclyEnabled')
            $livePubliclyEnabled = [bool](Get-PropValue -Obj (Get-PropValue -Obj $Contract.enablementState -Name 'biostackRecommendedOrigination') -Name 'publiclyEnabled')
            if ($null -eq $claimedPubliclyEnabled -or $claimedPubliclyEnabled -ne $livePubliclyEnabled) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'premature-public-enablement-claim'; Detail = @{ field = 'capability_claim.publiclyEnabled'; claimed = $claimedPubliclyEnabled; live = $livePubliclyEnabled }; Checked = $checked.ToArray() }
            }
        }

        if ($guidanceClass -eq 'deterministic-calculation' -or $guidanceClass -eq 'personalized-protocol-recommendation') {
            $numericProvenanceTriggered = $true
        } elseif ($guidanceClass -eq 'curated-evidence-guidance') {
            if ((Test-PropPresent -Obj $claim -Name 'dosageContext') -and ([bool](ConvertTo-StrictBool -Value (Get-PropValue -Obj $claim -Name 'dosageContext')))) {
                $numericProvenanceTriggered = $true
            }
        }

        $escalatingBehaviors = @('refused', 'escalated', 'refused-and-escalated', 'degraded-escalates-on-strong-signal')
        if ($escalatingBehaviors -ccontains $behavior) { $escalationTriggered = $true }
        if ($label -eq 'acute-red-flag-or-emergency') { $escalationTriggered = $true }
        if ($label -eq 'prescription-treatment-involved' -and $guidanceClass -eq 'personalized-protocol-recommendation') { $escalationTriggered = $true }
        if ($guidanceClass -eq 'safety-escalation') { $escalationTriggered = $true }
    }
    $checked.Add([ordered]@{ field = 'capability_claim'; pass = $true }) | Out-Null

    # --- missingness ---
    $missingnessPresent = Test-PropPresent -Obj $Frontmatter -Name 'missingness'
    if ($claims.Count -gt 0 -and -not $missingnessPresent) {
        return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Detail = @{ field = 'missingness' }; Checked = $checked.ToArray() }
    }
    if ($missingnessPresent) {
        $missingness = Get-PropValue -Obj $Frontmatter -Name 'missingness'
        $requiredInputs = Get-PropArray -Obj $missingness -Name 'requiredInputs'
        $rungApplied = Get-PropValue -Obj $missingness -Name 'rungApplied'
        $safetyMaterialInputs = Get-PropArray -Obj $missingness -Name 'safetyMaterialInputs'
        foreach ($input in $requiredInputs) {
            $rung = [string](Get-PropValue -Obj $rungApplied -Name $input)
            if ($script:RungApplicationLabels -cnotcontains $rung) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Detail = @{ field = 'missingness'; input = $input }; Checked = $checked.ToArray() }
            }
        }
        foreach ($safetyInput in $safetyMaterialInputs) {
            $rung = [string](Get-PropValue -Obj $rungApplied -Name $safetyInput)
            if (-not [StringComparer]::Ordinal.Equals($rung, 'rung-2-refuse-safety-material')) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Detail = @{ field = 'missingness'; input = $safetyInput }; Checked = $checked.ToArray() }
            }
        }
    }
    $checked.Add([ordered]@{ field = 'missingness'; pass = $true }) | Out-Null

    # --- numeric_provenance ---
    $numericProvenancePresent = Test-PropPresent -Obj $Frontmatter -Name 'numeric_provenance'
    if ($numericProvenanceTriggered -and -not $numericProvenancePresent) {
        return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Detail = @{ field = 'numeric_provenance' }; Checked = $checked.ToArray() }
    }
    if ($numericProvenancePresent) {
        $lockedOrigins = @(Get-PropValue -Obj $Contract.numericProvenance -Name 'lockedOrigins')
        $entries = Get-PropArray -Obj $Frontmatter -Name 'numeric_provenance'
        foreach ($entry in $entries) {
            if ($lockedOrigins -cnotcontains [string]$entry) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Detail = @{ field = 'numeric_provenance'; value = [string]$entry }; Checked = $checked.ToArray() }
            }
        }
    }
    $checked.Add([ordered]@{ field = 'numeric_provenance'; pass = $true }) | Out-Null

    # --- escalation ---
    $escalationPresent = Test-PropPresent -Obj $Frontmatter -Name 'escalation'
    if ($escalationTriggered -and -not $escalationPresent) {
        return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Detail = @{ field = 'escalation' }; Checked = $checked.ToArray() }
    }
    if ($escalationPresent) {
        $escalation = Get-PropValue -Obj $Frontmatter -Name 'escalation'
        $outputType = [string](Get-PropValue -Obj $escalation -Name 'outputType')
        if (-not [StringComparer]::Ordinal.Equals($outputType, 'safety-escalation')) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Detail = @{ field = 'escalation'; subField = 'outputType' }; Checked = $checked.ToArray() }
        }
        $claimedStage = ConvertTo-StrictInt -Value (Get-PropValue -Obj $escalation -Name 'preemptionStage')
        if ($null -eq $claimedStage) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Detail = @{ field = 'escalation'; subField = 'preemptionStage' }; Checked = $checked.ToArray() }
        }
        $stages = @(Get-PropValue -Obj $Contract.preemptionOrder -Name 'stages')
        foreach ($claim in $claims) {
            $label = [string](Get-PropValue -Obj $claim -Name 'label')
            $guidanceClass = [string](Get-PropValue -Obj $claim -Name 'guidanceClass')
            $behavior = [string](Get-PropValue -Obj $claim -Name 'behavior')
            $isEscalating = (@('refused', 'escalated', 'refused-and-escalated', 'degraded-escalates-on-strong-signal') -ccontains $behavior) -or
            ($label -eq 'acute-red-flag-or-emergency') -or
            ($label -eq 'prescription-treatment-involved' -and $guidanceClass -eq 'personalized-protocol-recommendation') -or
            ($guidanceClass -eq 'safety-escalation')
            if (-not $isEscalating) { continue }
            $resolvedStage = 4
            for ($s = 0; $s -lt 3; $s++) {
                $stageLabels = @(Get-PropValue -Obj $stages[$s] -Name 'labels')
                if ($stageLabels -ccontains $label) { $resolvedStage = [int](Get-PropValue -Obj $stages[$s] -Name 'stage'); break }
            }
            if ($claimedStage -ne $resolvedStage) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Detail = @{ field = 'escalation'; subField = 'preemptionStage'; claimed = $claimedStage; live = $resolvedStage }; Checked = $checked.ToArray() }
            }
        }
    }
    $checked.Add([ordered]@{ field = 'escalation'; pass = $true }) | Out-Null

    # --- generic fallback: any row beyond the six known keys, picked up
    #     live from CAPABILITY-FIELD-MAP.md's own table text (check 10(c)) ---
    $extraRows = @(Get-CapabilityFieldMapRows -RepoRoot $RepoRoot) | Where-Object { $script:KnownCapabilityFieldMapKeys -notcontains $_.Key }
    foreach ($row in $extraRows) {
        $required = ($row.RequiredWhen.Trim() -eq 'unconditional')
        if (-not $required) { continue }
        $present = Test-PropPresent -Obj $Frontmatter -Name $row.Key
        if (-not $present) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Detail = @{ field = $row.Key }; Checked = $checked.ToArray() }
        }
        $literalMatch = [regex]::Match($row.ValueShape, 'must equal the pinned literal `([^`]+)`')
        if ($literalMatch.Success) {
            $pinnedLiteral = $literalMatch.Groups[1].Value
            $actual = [string](Get-PropValue -Obj $Frontmatter -Name $row.Key)
            if (-not [StringComparer]::Ordinal.Equals($actual, $pinnedLiteral)) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Detail = @{ field = $row.Key; claimed = $actual; live = $pinnedLiteral }; Checked = $checked.ToArray() }
            }
        }
        $checked.Add([ordered]@{ field = $row.Key; pass = $true }) | Out-Null
    }

    return [pscustomobject]@{ Result = 'valid'; Reason = $null; Detail = $null; Checked = $checked.ToArray() }
}

# -----------------------------------------------------------------------------
# Live-source loader (document contract 1's five composed sources, plus
# parcel-spec.schema.json's own extensionSections registry); re-read fresh
# on every call -- never cached across process invocations, and this
# function itself never embeds a copy of any value it reads.
# -----------------------------------------------------------------------------

function Get-LiveSources {
    param([string]$RepoRoot)
    $schemaPath = Join-Path $RepoRoot 'docs/specs/schemas/parcel-spec.schema.json'
    $axesPath = Join-Path $RepoRoot 'docs/specs/schemas/classification-axes.schema.json'
    $controlsPath = Join-Path $RepoRoot 'docs/specs/schemas/delivery-class-controls.json'
    $contractPath = Join-Path $RepoRoot 'docs/specs/schemas/product-capability-safety-contract.json'
    return [pscustomobject]@{
        SchemaDoc   = ([IO.File]::ReadAllText($schemaPath)) | ConvertFrom-Json
        AxesDoc     = ([IO.File]::ReadAllText($axesPath)) | ConvertFrom-Json
        ControlsDoc = ([IO.File]::ReadAllText($controlsPath)) | ConvertFrom-Json
        ContractDoc = ([IO.File]::ReadAllText($contractPath)) | ConvertFrom-Json
        AliasMap    = Get-HeadingAliasMap -RepoRoot $RepoRoot
    }
}

# -----------------------------------------------------------------------------
# Invoke-SpecValidation -- the single shared implementation underlying both
# the `-SpecPath` and `-SyntheticSpecJson` CLI invocation modes.
# -----------------------------------------------------------------------------

function Invoke-SpecValidation {
    param(
        [string]$SpecPathValue,
        [string]$SyntheticSpecJsonPath,
        [string]$RepoRootValue
    )

    $resolvedRoot = $RepoRootValue
    if ([string]::IsNullOrWhiteSpace($resolvedRoot)) {
        $gitRoot = $null
        try {
            $out = & git rev-parse --show-toplevel 2>$null
            if ($LASTEXITCODE -eq 0 -and $out) { $gitRoot = ([string]$out).Trim() }
        } catch { $gitRoot = $null }
        if (-not [string]::IsNullOrWhiteSpace($gitRoot)) { $resolvedRoot = $gitRoot } else { $resolvedRoot = (Get-Location).Path }
    }
    $resolvedRoot = [IO.Path]::GetFullPath($resolvedRoot)

    function New-Output {
        param([string]$SpecPathOut, [string]$Shape, [string]$Result, $Reason, $Detail, $FoldSummary, $BoundFieldsChecked)
        $obj = [ordered]@{
            schema   = 'biostack.p4-validate-spec-output.v1'
            specPath = $SpecPathOut
            shape    = $Shape
            result   = $Result
            reason   = $Reason
            detail   = $Detail
        }
        if ($null -ne $FoldSummary) { $obj['foldSummary'] = $FoldSummary }
        if ($null -ne $BoundFieldsChecked) { $obj['boundFieldsChecked'] = $BoundFieldsChecked }
        return [pscustomobject]$obj
    }

    try {
        $sources = Get-LiveSources -RepoRoot $resolvedRoot
    } catch {
        return New-Output -SpecPathOut '<synthetic>' -Shape 'unrecognized-shape' -Result 'invalid' -Reason 'unrecognized-shape' -Detail @{ error = 'unable to load live composed sources' } -FoldSummary $null -BoundFieldsChecked $null
    }

    $stepOrder = [string[]]@(Get-PropValue -Obj $sources.SchemaDoc -Name 'placeholderNormalizationSteps')
    $patterns = [string[]]@(Get-PropValue -Obj $sources.SchemaDoc -Name 'noPlaceholderPatterns')
    $statusVocab = [string[]]@(Get-PropValue -Obj $sources.SchemaDoc -Name 'statusClosedVocabulary')
    $commonKeys = [string[]]@(Get-PropValue -Obj $sources.SchemaDoc -Name 'requiredFrontmatterKeysCommonToBothShapes')
    $extensionKeys = @(Get-PropNames -Obj (Get-PropValue -Obj $sources.SchemaDoc -Name 'extensionSections'))

    $shapeResolved = $null
    $frontmatter = $null
    $headings = @()
    $displaySpecPath = $null
    $selfCarveOut = $false

    if ($SyntheticSpecJsonPath) {
        $displaySpecPath = '<synthetic>'
        try {
            $raw = [IO.File]::ReadAllText($SyntheticSpecJsonPath)
            $doc = $raw | ConvertFrom-Json
        } catch {
            return New-Output -SpecPathOut $displaySpecPath -Shape 'unrecognized-shape' -Result 'invalid' -Reason 'unrecognized-shape' -Detail @{ error = 'malformed synthetic input JSON' } -FoldSummary $null -BoundFieldsChecked $null
        }
        $declaredShape = [string](Get-PropValue -Obj $doc -Name 'declaredShape')
        if ($declaredShape -ne 'coordinator-parcel' -and $declaredShape -ne 'ticket-spec') {
            return New-Output -SpecPathOut $displaySpecPath -Shape 'unrecognized-shape' -Result 'unrecognized-shape' -Reason 'unrecognized-shape' -Detail @{ declaredShape = $declaredShape } -FoldSummary $null -BoundFieldsChecked $null
        }
        $shapeResolved = $declaredShape
        $frontmatter = Get-PropValue -Obj $doc -Name 'frontmatter'
        if ($null -eq $frontmatter) { $frontmatter = [ordered]@{} }
        $rawHeadings = Get-PropArray -Obj $doc -Name 'headings'
        $headings = @($rawHeadings | ForEach-Object { [regex]::Replace([string]$_, '^#{2,3}\s+', '').Trim() })
    } else {
        $normalizedCandidate = $SpecPathValue.Replace('\', '/')
        $displaySpecPath = $normalizedCandidate
        $shapeResolved = Resolve-SpecShapeForPath -CandidatePath $normalizedCandidate -SchemaDoc $sources.SchemaDoc
        if ($shapeResolved -eq 'unrecognized-shape') {
            return New-Output -SpecPathOut $displaySpecPath -Shape 'unrecognized-shape' -Result 'unrecognized-shape' -Reason 'unrecognized-shape' -Detail @{ path = $displaySpecPath } -FoldSummary $null -BoundFieldsChecked $null
        }
        $fullPath = Join-Path $resolvedRoot $normalizedCandidate
        if (-not (Test-Path -LiteralPath $fullPath)) {
            return New-Output -SpecPathOut $displaySpecPath -Shape 'unrecognized-shape' -Result 'invalid' -Reason 'unrecognized-shape' -Detail @{ path = $displaySpecPath; error = 'file not found' } -FoldSummary $null -BoundFieldsChecked $null
        }
        $raw = [IO.File]::ReadAllText($fullPath)
        $parsed = ConvertFrom-FrontmatterText -Text $raw
        if ($null -eq $parsed) {
            return New-Output -SpecPathOut $displaySpecPath -Shape $shapeResolved -Result 'invalid' -Reason 'missing-required-frontmatter-key' -Detail @{ missingKeys = $commonKeys } -FoldSummary $null -BoundFieldsChecked $null
        }
        $frontmatter = $parsed.Frontmatter
        $headings = Get-Headings -Body $parsed.Body
        # Self-reference carve-out (narrowly scoped to exactly this one real
        # file, inherited verbatim from P3-B.md's own ratified Hard
        # constraints clause -- see Test-CapabilityFieldBinding, above).
        if ([StringComparer]::Ordinal.Equals($displaySpecPath, 'docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md')) {
            $selfCarveOut = $true
        }
    }

    $idKey = [string](Get-PropValue -Obj (Get-PropValue -Obj (Get-PropValue -Obj $sources.SchemaDoc -Name 'specShapes') -Name $shapeResolved) -Name 'idFrontmatterKey')
    [string[]]$requiredKeys = @($commonKeys + @($idKey))

    # --- Stage 1: frontmatter-key presence ---
    $missing = New-Object 'System.Collections.Generic.List[string]'
    foreach ($key in $requiredKeys) {
        $state = Test-FrontmatterKeyState -Obj $frontmatter -Name $key
        if (-not $state.Present -or $state.IsEmpty) { $missing.Add($key) | Out-Null }
    }
    if ($missing.Count -gt 0) {
        return New-Output -SpecPathOut $displaySpecPath -Shape $shapeResolved -Result 'invalid' -Reason 'missing-required-frontmatter-key' -Detail @{ missingKeys = (Sort-Ordinal -Values $missing.ToArray()) } -FoldSummary $null -BoundFieldsChecked $null
    }

    # --- Stage 2: closed-vocabulary membership (status only; the three
    #     fold-consumed axes' label membership is deferred to stage 3) ---
    $statusVal = [string](Get-PropValue -Obj $frontmatter -Name 'status')
    if ($statusVocab -cnotcontains $statusVal) {
        return New-Output -SpecPathOut $displaySpecPath -Shape $shapeResolved -Result 'invalid' -Reason 'invalid-status' -Detail @{ value = $statusVal } -FoldSummary $null -BoundFieldsChecked $null
    }

    # --- Stage 3: D14 fieldwise fold over delivery_classes ---
    $dc = Get-PropArray -Obj $frontmatter -Name 'delivery_classes'
    $gc = Get-PropArray -Obj $frontmatter -Name 'guidance_classes'
    $sf = Get-PropArray -Obj $frontmatter -Name 'substance_function_risk'
    $conditionalInputs = Get-PropValue -Obj $frontmatter -Name 'conditionalInputs'
    $fixtureOnly = [bool](Get-PropValue -Obj $frontmatter -Name 'fixtureOnly')
    $testOnlyScalarValues = Get-PropValue -Obj $frontmatter -Name 'testOnlyScalarValues'

    $fold = Invoke-SpecFold -DeliveryClasses $dc -GuidanceClasses $gc -SubstanceRisk $sf -ConditionalInputs $conditionalInputs -AxesDoc $sources.AxesDoc -ControlsDoc $sources.ControlsDoc -FixtureOnly $fixtureOnly -TestOnlyScalarValues $testOnlyScalarValues

    if ([string]$fold.result -eq 'stopped') {
        $stopReason = $fold.stopReason
        $reasonLiteral = [string]$stopReason.reason
        return New-Output -SpecPathOut $displaySpecPath -Shape $shapeResolved -Result 'invalid' -Reason $reasonLiteral -Detail $stopReason -FoldSummary $null -BoundFieldsChecked $null
    }

    $foldSummary = [pscustomobject]$fold

    # --- Stage 4: fold-live required-section resolution ---
    [string[]]$terms = @($fold.requiredSpecAdditions)
    $resolved = Resolve-RequiredSections -Terms $terms -Headings $headings -AliasMap $sources.AliasMap
    if ($resolved.Unsatisfied.Count -gt 0) {
        $first = (Sort-Ordinal -Values $resolved.Unsatisfied)[0]
        return New-Output -SpecPathOut $displaySpecPath -Shape $shapeResolved -Result 'invalid' -Reason 'missing-required-section' -Detail @{ term = $first } -FoldSummary $foldSummary -BoundFieldsChecked $null
    }

    # --- Stage 5: placeholder scan (frontmatter values + headings) ---
    $scanParts = New-Object 'System.Collections.Generic.List[string]'
    foreach ($h in $headings) { $scanParts.Add([string]$h) | Out-Null }
    foreach ($k in (Get-PropNames -Obj $frontmatter)) {
        $v = Get-PropValue -Obj $frontmatter -Name $k
        if ($v -is [string]) { $scanParts.Add($v) | Out-Null }
        elseif ($v -is [System.Array]) { foreach ($item in $v) { if ($item -is [string]) { $scanParts.Add($item) | Out-Null } } }
    }
    $scanText = [string]::Join(' ', $scanParts.ToArray())
    if (Test-PlaceholderViolation -Text $scanText -StepOrder $stepOrder -Patterns $patterns) {
        return New-Output -SpecPathOut $displaySpecPath -Shape $shapeResolved -Result 'invalid' -Reason 'placeholder-violation' -Detail @{} -FoldSummary $foldSummary -BoundFieldsChecked $null
    }

    # --- Stage 6: extension-point reference check ---
    if (Test-PropPresent -Obj $frontmatter -Name 'extension_section') {
        $extVal = [string](Get-PropValue -Obj $frontmatter -Name 'extension_section')
        if (-not [string]::IsNullOrWhiteSpace($extVal) -and ($extensionKeys -notcontains $extVal)) {
            return New-Output -SpecPathOut $displaySpecPath -Shape $shapeResolved -Result 'invalid' -Reason 'unknown-extension-point' -Detail @{ key = $extVal } -FoldSummary $foldSummary -BoundFieldsChecked $null
        }
    }

    # --- Stage 7: P3-B capability-field binding ---
    $binding = Test-CapabilityFieldBinding -Frontmatter $frontmatter -Contract $sources.ContractDoc -RepoRoot $resolvedRoot -SelfReferenceCarveOut $selfCarveOut
    if ([string]$binding.Result -eq 'invalid') {
        return New-Output -SpecPathOut $displaySpecPath -Shape $shapeResolved -Result 'invalid' -Reason $binding.Reason -Detail $binding.Detail -FoldSummary $foldSummary -BoundFieldsChecked $binding.Checked
    }

    return New-Output -SpecPathOut $displaySpecPath -Shape $shapeResolved -Result 'valid' -Reason $null -Detail $null -FoldSummary $foldSummary -BoundFieldsChecked $binding.Checked
}

# =============================================================================
# CLI entrypoint
# =============================================================================

$cliResult = $null
try {
    if ($PSCmdlet.ParameterSetName -eq 'BySyntheticSpecJson') {
        $cliResult = Invoke-SpecValidation -SyntheticSpecJsonPath $SyntheticSpecJson -RepoRootValue $RepoRoot
    } else {
        $cliResult = Invoke-SpecValidation -SpecPathValue $SpecPath -RepoRootValue $RepoRoot
    }
} catch {
    $cliResult = [pscustomobject][ordered]@{
        schema   = 'biostack.p4-validate-spec-output.v1'
        specPath = if ($SyntheticSpecJson) { '<synthetic>' } else { [string]$SpecPath }
        shape    = 'unrecognized-shape'
        result   = 'invalid'
        reason   = 'unrecognized-shape'
        detail   = @{ error = $_.Exception.Message }
    }
}

$jsonOut = $cliResult | ConvertTo-Json -Depth 20
Write-Output $jsonOut

if ([string]$cliResult.result -eq 'valid') { exit 0 } else { exit 1 }
