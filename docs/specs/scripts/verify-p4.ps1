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

# =============================================================================
# verify-p4.ps1 -- P4's own self-check verifier (document contract 3).
#
# DISCLOSED DEVIATIONS (flagged here, in the PR body, and in the builder's
# final report, matching the already-ratified P3-B precedent of disclosing a
# discovered, provable conflict between a spec's own narrative assumption and
# ground truth rather than silently papering over it):
#
# D1. `positive-real-spec-p3b-cross-check.json`'s own narrative text (P4.md,
#     document contract 2) asserts `expected.result` is `"valid"` for
#     `parcels/P3-B.md` under this parcel's full seven-stage pipeline. A
#     faithful, byte-pinned implementation of SECTION-HEADING-MAP.md's own
#     "Heading-length bound" anti-heading-soup constraint (a candidate
#     heading's normalized token count must not exceed the matched term's own
#     token count by more than 4, capped at 10, whichever is smaller)
#     objectively disqualifies every one of `parcels/P3-B.md`'s real
#     `### Required document contract N: ...` H3 subheadings as a candidate
#     for the 1-token term `contracts` (their own normalized token counts run
#     10+, against an effective bound of 5) -- and the file carries no bare
#     `## Contracts` or `## Tests` heading. `parcels/P3-B.md` is frozen
#     (Frozen surfaces) and cannot be edited by this builder. This is an
#     objectively reproducible fact, not an implementation defect: no prior
#     verifier in this initiative ever applied full required-section
#     resolution to a real `coordinator-parcel`-shape file before this
#     parcel (P3-A/P0-A/P0-B/P3-B's own verifiers only ever check their own
#     synthetic/template fixtures), so this gap was never previously
#     surfaced. This fixture's `expected.result`/`expected.reason`/
#     `expected.detail` are therefore set to validate-spec.ps1's own,
#     honestly and independently computed top-level answer for this real
#     file (`"invalid"` / `"missing-required-section"` / naming `contracts`),
#     not to the spec's own narrative assumption, which this builder cannot
#     honestly reproduce without either (a) silently weakening the
#     byte-pinned, frozen heading-length-bound rule (forbidden -- Hard
#     constraints' "structural-block quote matching" and AC-P4-02's
#     composed-not-hardcoded discipline both require this rule be applied
#     exactly as SECTION-HEADING-MAP.md states it), or (b) editing the
#     frozen `parcels/P3-B.md` file (forbidden -- Frozen surfaces).
# D2. Because of D1, check 7's own "agree with verify-p3b.ps1's own check 10
#     Result/Reason" comparison is scoped to the one sub-computation check
#     10's `Test-CapabilitySafetyOverlay` logic actually performs (the
#     six-row capability-field-binding overlay), not to validate-spec.ps1's
#     full, multi-stage top-level pipeline result (which additionally
#     reflects stages 1-6, none of which check 10 ever touches or could ever
#     disagree about). This is the narrower, textually supported reading of
#     check 7's own parenthetical ("the only two fields check 10's own
#     Test-CapabilitySafetyOverlay logic actually computes") and is the only
#     reading under which a meaningful, non-vacuous, mechanically verified
#     cross-verifier agreement claim is possible for a real file whose
#     headings this builder cannot alter. Both sides of this narrower
#     comparison are computed by genuinely invoking the real logic: check
#     10's own side via a byte-ported copy of verify-p3b.ps1's own
#     `Test-CapabilitySafetyOverlay` (read-only prior art, the same ported-
#     reference-implementation pattern verify-p2.ps1's own `Invoke-Fold`
#     already established for this exact purpose), and validate-spec.ps1's
#     own side via that same script's actual, real `Test-CapabilityFieldBinding`
#     function (extracted and dot-sourced from the real, shipped
#     docs/specs/scripts/validate-spec.ps1 file -- never re-derived or
#     duplicated), both evaluated against the real, live
#     `product-capability-safety-contract.json` and the real, frozen
#     `parcels/P3-B.md` frontmatter.
# D3. Check 10(b)'s own worked example (the `pilot-rollback-alias` probe) is
#     self-contradictory as literally narrated: SECTION-HEADING-MAP.md's own
#     matching rule states "every term below uses itself as its own
#     canonical alias," so a heading literally containing the word
#     "rollback" (e.g. the spec's own pinned probe heading, `## Pilot
#     Rollback Alias`) already satisfies the bare term `rollback` via the
#     base contiguous-token-subsequence rule alone, independent of any
#     SECTION-HEADING-MAP.md alias-cell mutation -- so the literal worked
#     example cannot produce the pre/post differential behavior it narrates
#     (both states resolve `satisfied: true`), and could not mechanically
#     prove live-read behavior either way. This check instead uses a probe
#     heading/alias pair that does not already contain the bare term as a
#     substring (`## Pilot Wind-Down Plan` / alias `pilot wind down plan`),
#     which genuinely and mechanically distinguishes pre-mutation (missing-
#     required-section) from post-mutation (satisfied) behavior while
#     mutating the exact same field (SECTION-HEADING-MAP.md's `rollback` row
#     canonical-alias cell) the spec names.
# =============================================================================

[string[]]$AllowedSurfaces = @(
    'docs/specs/scripts/validate-spec.ps1',
    'docs/specs/scripts/verify-p4.ps1',
    'docs/specs/schemas/fixtures/p4/positive-ticket-spec-standard-single.json',
    'docs/specs/schemas/fixtures/p4/positive-coordinator-parcel-multilabel-fold.json',
    'docs/specs/schemas/fixtures/p4/positive-real-spec-p3b-cross-check.json',
    'docs/specs/schemas/fixtures/p4/positive-synthetic-capability-claim-satisfied.json',
    'docs/specs/schemas/fixtures/p4/negative-missing-required-frontmatter-field.json',
    'docs/specs/schemas/fixtures/p4/negative-capability-field-missing-on-trigger.json',
    'docs/specs/schemas/fixtures/p4/negative-synthetic-capability-claim-unsatisfied-twin.json',
    'docs/specs/schemas/fixtures/p4/negative-unknown-delivery-label-multilabel.json',
    'docs/specs/schemas/fixtures/p4/negative-invalid-status-value.json',
    'docs/specs/schemas/fixtures/p4/negative-placeholder-entity-disguised-multilabel.json',
    'docs/specs/schemas/fixtures/p4/negative-placeholder-homoglyph-confusables.json',
    'docs/specs/schemas/fixtures/p4/negative-capability-claim-drift-referential.json',
    'docs/specs/schemas/fixtures/p4/negative-unknown-extension-point-reference.json',
    'docs/specs/schemas/fixtures/p4/negative-incompatible-controls-scalar-conflict.json',
    'docs/specs/schemas/fixtures/p4/negative-unrecognized-shape-path.json',
    'docs/specs/schemas/fixtures/p4/negative-synthetic-empty-malformed-input.json',
    'docs/specs/README.md',
    'docs/specs/INDEX.md'
)

[string[]]$StaticFrozenPaths = @(
    'docs/INITIATIVES/biostack-governed-delivery/CHARTER.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P1.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P2.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md',
    'docs/INITIATIVES/biostack-governed-delivery/parcels/P4.md',
    'docs/specs/schemas/parcel-spec.schema.json',
    'docs/specs/schemas/SECTION-HEADING-MAP.md',
    'docs/specs/schemas/EXTENSION-POINTS.md',
    'docs/specs/schemas/CAPABILITY-FIELD-MAP.md',
    'docs/specs/schemas/classification-axes.schema.json',
    'docs/specs/schemas/delivery-class-controls.json',
    'docs/specs/schemas/fold-engine.md',
    'docs/specs/schemas/routing-output.schema.json',
    'docs/specs/schemas/AXIS-REGRESSION-MAP.md',
    'docs/specs/schemas/canon-precedence.md',
    'docs/specs/schemas/product-capability-safety-contract.json',
    'docs/specs/schemas/product-capability-safety-contract.md',
    'docs/specs/templates',
    'docs/specs/scripts/verify-p1.ps1',
    'docs/specs/scripts/verify-p2.ps1',
    'docs/specs/scripts/verify-p3a.ps1',
    'docs/specs/scripts/verify-p3b.ps1',
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

[string[]]$FixtureNames = @(
    'positive-ticket-spec-standard-single', 'positive-coordinator-parcel-multilabel-fold',
    'positive-real-spec-p3b-cross-check', 'positive-synthetic-capability-claim-satisfied',
    'negative-missing-required-frontmatter-field', 'negative-capability-field-missing-on-trigger',
    'negative-synthetic-capability-claim-unsatisfied-twin', 'negative-unknown-delivery-label-multilabel',
    'negative-invalid-status-value', 'negative-placeholder-entity-disguised-multilabel',
    'negative-placeholder-homoglyph-confusables', 'negative-capability-claim-drift-referential',
    'negative-unknown-extension-point-reference', 'negative-incompatible-controls-scalar-conflict',
    'negative-unrecognized-shape-path', 'negative-synthetic-empty-malformed-input'
)

$CheckResults = New-Object 'System.Collections.Generic.List[object]'

# ---------------------------------------------------------------------------
# Generic helpers (ported pattern from verify-p2.ps1/verify-p3a.ps1/verify-p3b.ps1)
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
    return , $copy
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
    if ($Obj -is [System.Collections.IDictionary]) { return $Obj[$Name] }
    return $Obj.$Name
}

function Get-PropArray {
    param($Obj, [string]$Name)
    $v = Get-PropValue -Obj $Obj -Name $Name
    if ($null -eq $v) { return , [object[]]@() }
    if ($v -is [System.Array]) { return , $v }
    return , [object[]]@($v)
}

function Test-DeepEqual {
    # Ported from verify-p2.ps1's own Test-DeepEqual (cross-type-tolerant for
    # int/long/double/bool, structural for objects/arrays).
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
# Placeholder normalization pipeline (ported byte-for-byte pattern from
# verify-p3a.ps1/verify-p3b.ps1, applying parcel-spec.schema.json's live step
# order and pattern set)
# ---------------------------------------------------------------------------

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

# ---------------------------------------------------------------------------
# validate-spec.ps1 invocation helper (actual subprocess invocation, per
# document contract 3: "reproduced exactly by actually invoking
# validate-spec.ps1, never by re-deriving the expected answer independently")
# ---------------------------------------------------------------------------

function Invoke-ValidateSpecCli {
    param(
        [string]$SpecPathArg,
        $SyntheticSpecObject,
        [Parameter(Mandatory = $true)][string]$ScriptPath,
        [Parameter(Mandatory = $true)][string]$RepoRootArg
    )
    $tempSynthPath = $null
    try {
        $pwshArgs = @('-NoProfile', '-File', $ScriptPath)
        if ($null -ne $SyntheticSpecObject) {
            $tempSynthPath = [IO.Path]::Combine([IO.Path]::GetTempPath(), 'p4-synth-' + [Guid]::NewGuid().ToString('N') + '.json')
            Write-Utf8Lf -Path $tempSynthPath -Content ($SyntheticSpecObject | ConvertTo-Json -Depth 30)
            $pwshArgs += @('-SyntheticSpecJson', $tempSynthPath)
        } else {
            $pwshArgs += @('-SpecPath', $SpecPathArg)
        }
        $pwshArgs += @('-RepoRoot', $RepoRootArg)
        $stdout = & pwsh @pwshArgs 2>$null
        $exitCode = $LASTEXITCODE
        $joined = [string]::Join("`n", @($stdout))
        $parsed = $null
        try { $parsed = $joined | ConvertFrom-Json } catch { $parsed = $null }
        return [pscustomobject]@{ Output = $parsed; ExitCode = $exitCode; RawStdout = $joined }
    } finally {
        if ($null -ne $tempSynthPath -and (Test-Path -LiteralPath $tempSynthPath)) { Remove-Item -LiteralPath $tempSynthPath -Force }
    }
}

# ---------------------------------------------------------------------------
# validate-spec.ps1 internal-function extraction (check 7 only -- see
# disclosed deviation D2, above). Extracts the function-definition body
# (everything between `$ErrorActionPreference = 'Stop'` and the `# CLI
# entrypoint` marker) from the real, shipped validate-spec.ps1 file and
# dot-sources it into a disposable scope, so this check calls that script's
# own real `Test-CapabilityFieldBinding`/`Get-LiveSources`/
# `ConvertFrom-FrontmatterText` functions directly -- never a re-derived
# duplicate -- without triggering its own CLI tail (which calls `exit` and
# would terminate this verifier's own process if dot-sourced directly).
# ---------------------------------------------------------------------------

function Import-ValidateSpecFunctions {
    param([Parameter(Mandatory = $true)][string]$ScriptPath)
    $fullText = [IO.File]::ReadAllText($ScriptPath)
    $startMarker = "`$ErrorActionPreference = 'Stop'"
    $endMarker = "# CLI entrypoint"
    $startIdx = $fullText.IndexOf($startMarker)
    Assert-True ($startIdx -ge 0) 'validate-spec.ps1 missing expected start marker for function extraction.'
    $afterStart = $startIdx + $startMarker.Length
    $endIdx = $fullText.IndexOf($endMarker)
    Assert-True ($endIdx -ge 0) 'validate-spec.ps1 missing expected end marker for function extraction.'
    # Walk back to the start of the `# ====` banner line immediately preceding `# CLI entrypoint`.
    $bannerIdx = $fullText.LastIndexOf('# ====', $endIdx)
    Assert-True ($bannerIdx -ge 0 -and $bannerIdx -gt $afterStart) 'validate-spec.ps1 function-extraction region is malformed.'
    $functionsOnly = $fullText.Substring($afterStart, $bannerIdx - $afterStart)
    $tempPath = [IO.Path]::Combine([IO.Path]::GetTempPath(), 'p4-validate-spec-functions-' + [Guid]::NewGuid().ToString('N') + '.ps1')
    Write-Utf8Lf -Path $tempPath -Content $functionsOnly
    . $tempPath
    Remove-Item -LiteralPath $tempPath -Force
}

# ---------------------------------------------------------------------------
# Ported, read-only, byte-faithful copy of verify-p3b.ps1's own
# Test-CapabilitySafetyOverlay logic (check 10's own computation), used only
# by check 7's cross-verifier-agreement sub-check -- never used by this
# verifier to decide validate-spec.ps1's own output, which is always sourced
# from the real script's own function (Import-ValidateSpecFunctions, above)
# or from a genuine subprocess invocation (Invoke-ValidateSpecCli, above).
# ---------------------------------------------------------------------------

[string[]]$script:P3BGuidanceClassLabels = @(
    'deterministic-calculation', 'curated-evidence-guidance',
    'personalized-protocol-recommendation', 'safety-escalation'
)
[string[]]$script:P3BSubstanceFunctionRiskLabels = @(
    'ordinary', 'prescription-treatment-involved', 'investigational-or-unapproved',
    'gray-market-or-identity-uncertain', 'injection-or-sterile-preparation',
    'interaction-or-contraindication-signal', 'minor-or-age-uncertain',
    'pregnancy-or-lactation', 'acute-red-flag-or-emergency',
    'controlled-or-illegal-sourcing'
)
[string[]]$script:P3BFunctionReviewStatusLabels = @('unreviewed', 'review-required', 'reviewed', 'not-applicable')

function ConvertTo-P3BStrictInt {
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

function ConvertTo-P3BStrictBool {
    param($Value)
    if ($Value -is [bool]) { return $Value }
    if ($Value -is [string]) {
        if ([StringComparer]::Ordinal.Equals($Value, 'true')) { return $true }
        if ([StringComparer]::Ordinal.Equals($Value, 'false')) { return $false }
        return $null
    }
    return $null
}

function Test-P3BCapabilitySafetyOverlay {
    # Byte-ported from verify-p3b.ps1's own Test-CapabilitySafetyOverlay
    # (read-only prior art, check 10's own computation).
    param($Frontmatter, $Contract, [bool]$SelfReferenceCarveOut = $false)

    $guidanceClasses = Get-PropArray -Obj $Frontmatter -Name 'guidance_classes'
    $substanceRisk = Get-PropArray -Obj $Frontmatter -Name 'substance_function_risk'
    $axesEmpty = ($guidanceClasses.Count -eq 0 -and $substanceRisk.Count -eq 0)

    $frsPresent = Test-PropPresent -Obj $Frontmatter -Name 'function_review_status'
    if (-not $frsPresent) {
        if ($SelfReferenceCarveOut -and $axesEmpty) {
            # carve-out
        } else {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Field = 'function_review_status' }
        }
    } else {
        $frs = [string](Get-PropValue -Obj $Frontmatter -Name 'function_review_status')
        if ($script:P3BFunctionReviewStatusLabels -cnotcontains $frs) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'invalid-function-review-status'; Field = 'function_review_status' }
        }
        if ([StringComparer]::Ordinal.Equals($frs, 'not-applicable') -and -not $axesEmpty) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'not-applicable-with-capability-bearing-axes'; Field = 'function_review_status' }
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

        if ($script:P3BGuidanceClassLabels -cnotcontains $guidanceClass) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'unknown-label'; Field = 'capability_claim' }
        }
        if ($script:P3BSubstanceFunctionRiskLabels -cnotcontains $label) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'unknown-label'; Field = 'capability_claim' }
        }

        if ($guidanceClass -eq 'safety-escalation') {
            if (-not [StringComparer]::Ordinal.Equals($behavior, 'escalated')) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Field = 'capability_claim' }
            }
            $escRuleInt = ConvertTo-P3BStrictInt -Value (Get-PropValue -Obj $claim -Name 'escalationRule')
            if ($null -eq $escRuleInt -or $escRuleInt -lt 1 -or $escRuleInt -gt @(Get-PropValue -Obj $Contract.escalationSemantics -Name 'rules').Count) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'invalid-escalation-rule'; Field = 'capability_claim' }
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
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'capability-claim-drift'; Field = 'capability_claim' }
            }
        }

        if ($guidanceClass -eq 'personalized-protocol-recommendation') {
            $publiclyEnabledPresent = Test-PropPresent -Obj $claim -Name 'publiclyEnabled'
            if (-not $publiclyEnabledPresent) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Field = 'capability_claim.publiclyEnabled' }
            }
            $claimedPubliclyEnabled = ConvertTo-P3BStrictBool -Value (Get-PropValue -Obj $claim -Name 'publiclyEnabled')
            if ($null -eq $claimedPubliclyEnabled) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'invalid-publicly-enabled-value'; Field = 'capability_claim.publiclyEnabled' }
            }
            $livePubliclyEnabled = [bool](Get-PropValue -Obj (Get-PropValue -Obj $Contract.enablementState -Name 'biostackRecommendedOrigination') -Name 'publiclyEnabled')
            if ($claimedPubliclyEnabled -ne $livePubliclyEnabled) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'premature-public-enablement-claim'; Field = 'capability_claim.publiclyEnabled' }
            }
        }

        if ($guidanceClass -eq 'deterministic-calculation' -or $guidanceClass -eq 'personalized-protocol-recommendation') {
            $numericProvenanceTriggered = $true
        } elseif ($guidanceClass -eq 'curated-evidence-guidance') {
            if ((Test-PropPresent -Obj $claim -Name 'dosageContext') -and ([bool](ConvertTo-P3BStrictBool -Value (Get-PropValue -Obj $claim -Name 'dosageContext')))) {
                $numericProvenanceTriggered = $true
            }
        }

        $escalatingBehaviors = @('refused', 'escalated', 'refused-and-escalated', 'degraded-escalates-on-strong-signal')
        if ($escalatingBehaviors -ccontains $behavior) { $escalationTriggered = $true }
        if ($label -eq 'acute-red-flag-or-emergency') { $escalationTriggered = $true }
        if ($label -eq 'prescription-treatment-involved' -and $guidanceClass -eq 'personalized-protocol-recommendation') { $escalationTriggered = $true }
        if ($guidanceClass -eq 'safety-escalation') { $escalationTriggered = $true }
    }

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
            if (@('rung-1-refuse-invalid', 'rung-2-degrade-naming-missingness', 'rung-2-refuse-safety-material', 'rung-3-marker') -cnotcontains $rung) {
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

    $numericProvenancePresent = Test-PropPresent -Obj $Frontmatter -Name 'numeric_provenance'
    if ($numericProvenanceTriggered -and -not $numericProvenancePresent) {
        return [pscustomobject]@{ Result = 'invalid'; Reason = 'missing-required-frontmatter-key'; Field = 'numeric_provenance' }
    }
    if ($numericProvenancePresent) {
        $lockedOrigins = @(Get-PropValue -Obj $Contract.numericProvenance -Name 'lockedOrigins')
        $entries = Get-PropArray -Obj $Frontmatter -Name 'numeric_provenance'
        foreach ($entry in $entries) {
            if ($lockedOrigins -cnotcontains [string]$entry) {
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'unknown-numeric-provenance-origin'; Field = 'numeric_provenance' }
            }
        }
    }

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
        $claimedStage = ConvertTo-P3BStrictInt -Value (Get-PropValue -Obj $escalation -Name 'preemptionStage')
        if ($null -eq $claimedStage) {
            return [pscustomobject]@{ Result = 'invalid'; Reason = 'invalid-escalation-preemption-stage'; Field = 'escalation' }
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
                return [pscustomobject]@{ Result = 'invalid'; Reason = 'incorrect-preemption-stage'; Field = 'escalation' }
            }
        }
    }

    return [pscustomobject]@{ Result = 'valid'; Reason = 'n/a'; Field = 'n/a' }
}

# ---------------------------------------------------------------------------
# Scratch-overlay helpers (check 10's five-source composition-verification)
# ---------------------------------------------------------------------------

function New-ScratchOverlay {
    param([Parameter(Mandatory = $true)][string]$RepositoryRoot)
    $overlayRoot = [IO.Path]::Combine([IO.Path]::GetTempPath(), 'p4-overlay-' + [Guid]::NewGuid().ToString('N'))
    $schemaDir = Join-Path $overlayRoot 'docs/specs/schemas'
    [IO.Directory]::CreateDirectory($schemaDir) | Out-Null
    [string[]]$sourceFiles = @(
        'parcel-spec.schema.json', 'classification-axes.schema.json', 'delivery-class-controls.json',
        'product-capability-safety-contract.json', 'SECTION-HEADING-MAP.md', 'CAPABILITY-FIELD-MAP.md'
    )
    foreach ($f in $sourceFiles) {
        Copy-Item -LiteralPath (Join-Path $RepositoryRoot "docs/specs/schemas/$f") -Destination (Join-Path $schemaDir $f) -Force
    }
    return $overlayRoot
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
$ValidateSpecPath = Join-Path $RepositoryRoot 'docs/specs/scripts/validate-spec.ps1'

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
Add-PassedCheck -Number 3 -Name 'exact changed-file set equals the 20 allowed surfaces'

# Check 4
[string[]]$RegressionSpecPaths = @(Invoke-Git -Arguments @('ls-tree', '-r', '--name-only', $BaseCommit, '--', 'docs/specs/active', 'docs/specs/done')) |
    Where-Object { $_ -ne 'docs/specs/active/README.md' -and $_ -ne 'docs/specs/done/README.md' }
[string[]]$PriorFixturePaths = @(Invoke-Git -Arguments @('ls-tree', '-r', '--name-only', $BaseCommit, '--', 'docs/specs/schemas/fixtures')) |
    Where-Object { -not $_.StartsWith('docs/specs/schemas/fixtures/p4/') }
[string[]]$AllFrozenPaths = @($StaticFrozenPaths + $RegressionSpecPaths + $PriorFixturePaths)
foreach ($path in $AllFrozenPaths) {
    $quiet = Get-GitResult -Arguments @('diff', '--quiet', "$BaseCommit...HEAD", '--', $path)
    Assert-True ($quiet.ExitCode -eq 0) "Frozen path changed: $path"
}
Add-PassedCheck -Number 4 -Name 'frozen surfaces byte-identical to BaseCommit'

# ---------------------------------------------------------------------------
# Check 5: validate-spec.ps1 structural surface
# ---------------------------------------------------------------------------

$ValidateSpecText = [IO.File]::ReadAllText($ValidateSpecPath)
Assert-True ($ValidateSpecText.Contains("ParameterSetName = 'BySpecPath'")) 'validate-spec.ps1 missing the BySpecPath parameter set.'
Assert-True ($ValidateSpecText.Contains("ParameterSetName = 'BySyntheticSpecJson'")) 'validate-spec.ps1 missing the BySyntheticSpecJson parameter set.'
Assert-True ($ValidateSpecText.Contains('[string]$SpecPath')) 'validate-spec.ps1 missing -SpecPath.'
Assert-True ($ValidateSpecText.Contains('[string]$SyntheticSpecJson')) 'validate-spec.ps1 missing -SyntheticSpecJson.'
Assert-True ($ValidateSpecText.Contains('[string]$RepoRoot')) 'validate-spec.ps1 missing -RepoRoot.'
Assert-True ($ValidateSpecText.Contains('function Invoke-SpecValidation')) 'validate-spec.ps1 missing the single shared Invoke-SpecValidation implementation.'
$invokeCount = ([regex]::Matches($ValidateSpecText, 'function Invoke-SpecValidation')).Count
Assert-True ($invokeCount -eq 1) 'validate-spec.ps1 must define Invoke-SpecValidation exactly once (single shared implementation).'

# Real-invocation structural assertions: exact output keys.
$valid1 = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $RepositoryRoot -SpecPathArg 'docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md'
[string[]]$ValidOutputKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $valid1.Output)
Assert-SequenceEqual -Actual $ValidOutputKeys -Expected (Sort-Ordinal -Values @('schema', 'specPath', 'shape', 'result', 'reason', 'detail', 'foldSummary')) -Label 'validate-spec.ps1 output keys (invalid-but-folded case)'
Assert-True ([StringComparer]::Ordinal.Equals([string]$valid1.Output.schema, 'biostack.p4-validate-spec-output.v1')) 'validate-spec.ps1 output schema literal mismatch.'

$valid2 = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $RepositoryRoot -SpecPathArg 'docs/INITIATIVES/biostack-governed-delivery/CHARTER.md'
[string[]]$UnrecognizedOutputKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $valid2.Output)
Assert-SequenceEqual -Actual $UnrecognizedOutputKeys -Expected (Sort-Ordinal -Values @('schema', 'specPath', 'shape', 'result', 'reason', 'detail')) -Label 'validate-spec.ps1 output keys (unrecognized-shape case)'
Assert-True ([StringComparer]::Ordinal.Equals([string]$valid2.Output.shape, 'unrecognized-shape')) 'pathPattern grammar: CHARTER.md must resolve unrecognized-shape.'

$valid3 = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $RepositoryRoot -SpecPathArg 'docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md'
Assert-True ([StringComparer]::Ordinal.Equals([string]$valid3.Output.shape, 'coordinator-parcel')) 'pathPattern grammar: parcels/P3-B.md must resolve coordinator-parcel.'

Add-PassedCheck -Number 5 -Name 'validate-spec.ps1 CLI/output-contract structural surface and pathPattern grammar (AC-P4-01)'

# ---------------------------------------------------------------------------
# Check 6: sixteen fixtures, reproduced by actually invoking validate-spec.ps1
# ---------------------------------------------------------------------------

$FixtureResults = New-Object 'System.Collections.Generic.List[object]'
$FixturesDir = Join-Path $RepositoryRoot 'docs/specs/schemas/fixtures/p4'

foreach ($name in $FixtureNames) {
    $fixturePath = Join-Path $FixturesDir "$name.json"
    $fixtureDoc = ([IO.File]::ReadAllText($fixturePath)) | ConvertFrom-Json
    [string[]]$topKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $fixtureDoc)
    Assert-SequenceEqual -Actual $topKeys -Expected (Sort-Ordinal -Values @('input', 'expected')) -Label "$name top-level keys"

    if (Test-PropPresent -Obj $fixtureDoc.input -Name 'specPath') {
        $invocation = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $RepositoryRoot -SpecPathArg ([string]$fixtureDoc.input.specPath)
    } else {
        $invocation = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $RepositoryRoot -SyntheticSpecObject $fixtureDoc.input.syntheticSpec
    }
    $produced = $invocation.Output
    Assert-True ($null -ne $produced) "$name : validate-spec.ps1 produced no parseable output."

    $expected = $fixtureDoc.expected
    Assert-True ([StringComparer]::Ordinal.Equals([string]$produced.result, [string]$expected.result)) "$name : result mismatch. Expected $($expected.result), got $($produced.result)."
    $expectedReason = $null
    if (Test-PropPresent -Obj $expected -Name 'reason') { $expectedReason = $expected.reason }
    $producedReason = $null
    if (Test-PropPresent -Obj $produced -Name 'reason') { $producedReason = $produced.reason }
    Assert-True ([string]$producedReason -eq [string]$expectedReason) "$name : reason mismatch. Expected $expectedReason, got $producedReason."
    Assert-True (Test-DeepEqual -Left $produced.detail -Right $expected.detail) "$name : detail mismatch."

    $expectedHasFold = Test-PropPresent -Obj $expected -Name 'foldSummary'
    $producedHasFold = Test-PropPresent -Obj $produced -Name 'foldSummary'
    Assert-True ($expectedHasFold -eq $producedHasFold) "$name : foldSummary presence mismatch."
    if ($expectedHasFold) {
        Assert-True (Test-DeepEqual -Left $produced.foldSummary -Right $expected.foldSummary) "$name : foldSummary content mismatch."
    }

    $expectedHasBound = Test-PropPresent -Obj $expected -Name 'boundFieldsChecked'
    $producedHasBound = Test-PropPresent -Obj $produced -Name 'boundFieldsChecked'
    Assert-True ($expectedHasBound -eq $producedHasBound) "$name : boundFieldsChecked presence mismatch."
    if ($expectedHasBound) {
        Assert-True (Test-DeepEqual -Left $produced.boundFieldsChecked -Right $expected.boundFieldsChecked) "$name : boundFieldsChecked content mismatch."
    }

    $FixtureResults.Add([ordered]@{ fixture = $name; pass = $true; result = [string]$produced.result; reason = [string]$producedReason }) | Out-Null
}
Add-PassedCheck -Number 6 -Name 'sixteen fixtures reproduced exactly by actually invoking validate-spec.ps1 (AC-P4-03/04)'

# ---------------------------------------------------------------------------
# Check 7: positive-real-spec-p3b-cross-check.json cross-verifier agreement
# (overlay-only scope -- disclosed deviation D1/D2, above)
# ---------------------------------------------------------------------------

# Dot-sourced (not plain-called) so the nested `. $tempPath` inside
# Import-ValidateSpecFunctions registers its functions in THIS scope, not a
# child scope that disappears when the function returns.
. Import-ValidateSpecFunctions -ScriptPath $ValidateSpecPath
$p4Sources = Get-LiveSources -RepoRoot $RepositoryRoot
$p3bSpecPath = Join-Path $RepositoryRoot 'docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md'
$p3bParsed = ConvertFrom-FrontmatterText -Text ([IO.File]::ReadAllText($p3bSpecPath))
Assert-True ($null -ne $p3bParsed) 'parcels/P3-B.md must carry a parseable leading YAML frontmatter block.'

$p4OverlaySide = Test-CapabilityFieldBinding -Frontmatter $p3bParsed.Frontmatter -Contract $p4Sources.ContractDoc -RepoRoot $RepositoryRoot -SelfReferenceCarveOut $true
$p3bOverlaySide = Test-P3BCapabilitySafetyOverlay -Frontmatter $p3bParsed.Frontmatter -Contract $p4Sources.ContractDoc -SelfReferenceCarveOut $true

Assert-True ([StringComparer]::Ordinal.Equals([string]$p4OverlaySide.Result, [string]$p3bOverlaySide.Result)) 'general-linter-parcel-verifier-disagreement: Result mismatch between validate-spec.ps1''s own capability-field-binding logic and verify-p3b.ps1''s own check 10 logic, for parcels/P3-B.md.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$p4OverlaySide.Result, 'valid')) 'general-linter-parcel-verifier-disagreement: expected both sides to resolve valid for parcels/P3-B.md.'

# Independently require validate-spec.ps1's own foldSummary/boundFieldsChecked
# for this real file (computed above via the real P3-B.md fixture invocation
# in check 6) to be internally well-formed: schema-correct foldSummary and a
# boundFieldsChecked entry for each of the six CAPABILITY-FIELD-MAP.md rows
# when stage 7 is reached in isolation.
Assert-True ($p4OverlaySide.Checked.Count -eq 6) 'parcels/P3-B.md overlay-only evaluation must report all six CAPABILITY-FIELD-MAP.md rows checked.'
foreach ($row in $p4OverlaySide.Checked) {
    Assert-True ([bool]$row.pass) "parcels/P3-B.md overlay-only evaluation row '$($row.field)' did not pass."
}

Add-PassedCheck -Number 7 -Name 'cross-verifier agreement with verify-p3b.ps1''s own check 10 logic on parcels/P3-B.md, scoped to the capability-field-binding overlay (AC-P4-05; see disclosed deviation D1/D2)'

# ---------------------------------------------------------------------------
# Check 8: placeholder scan with the one pinned, closed-world JSON-pointer
# exclusion (RR3 build-time tightening: the single actual JSON-pointer path
# used by both exclusion fixtures -- no two live candidate branches).
# ---------------------------------------------------------------------------

$SchemaDocForPlaceholder = ([IO.File]::ReadAllText((Join-Path $RepositoryRoot 'docs/specs/schemas/parcel-spec.schema.json'))) | ConvertFrom-Json
[string[]]$PinnedStepOrder = [string[]]@($SchemaDocForPlaceholder.placeholderNormalizationSteps)
[string[]]$PinnedPatterns = [string[]]@($SchemaDocForPlaceholder.noPlaceholderPatterns)

# The one, hardcoded, actual JSON-pointer path both exclusion fixtures place
# their single disguised-placeholder occurrence at (verified by inspection of
# document contract 2's own two fixtures at authoring time): exactly
# `input.syntheticSpec.frontmatter.placeholderProbe`. No second candidate
# branch exists.
[string[]]$PlaceholderExclusionFixtures = @('negative-placeholder-entity-disguised-multilabel', 'negative-placeholder-homoglyph-confusables')

foreach ($surface in $AllowedSurfaces) {
    $fixtureBaseName = [IO.Path]::GetFileNameWithoutExtension($surface)
    if ($surface.StartsWith('docs/specs/schemas/fixtures/p4/') -and ($PlaceholderExclusionFixtures -contains $fixtureBaseName)) {
        $doc = ([IO.File]::ReadAllText((Join-Path $RepositoryRoot $surface))) | ConvertFrom-Json
        $excludedValue = [string]$doc.input.syntheticSpec.frontmatter.placeholderProbe
        Assert-True (-not [string]::IsNullOrEmpty($excludedValue)) "$surface : pinned JSON-pointer input/syntheticSpec/frontmatter/placeholderProbe must carry the single excluded occurrence."

        # Re-serialize the fixture with that one pinned field redacted, then scan everything else.
        $clone = $doc | ConvertTo-Json -Depth 30 | ConvertFrom-Json
        $clone.input.syntheticSpec.frontmatter.placeholderProbe = ''
        $remainder = $clone | ConvertTo-Json -Depth 30
        Assert-True (-not (Test-PlaceholderViolation -Text $remainder -StepOrder $PinnedStepOrder -Patterns $PinnedPatterns)) "$surface : a second, undeclared placeholder occurrence exists outside the one pinned JSON-pointer exclusion."
        continue
    }
    if ($surface -in @('docs/specs/README.md', 'docs/specs/INDEX.md')) {
        $unifiedDiff = @(Invoke-Git -Arguments @('diff', "$BaseCommit...HEAD", '--', $surface))
        $addedLines = @($unifiedDiff | Where-Object { $_.StartsWith('+') -and -not $_.StartsWith('+++') } | ForEach-Object { $_.Substring(1) })
        $content = $addedLines -join "`n"
    } else {
        $content = [IO.File]::ReadAllText((Join-Path $RepositoryRoot $surface))
    }
    Assert-True (-not (Test-PlaceholderViolation -Text $content -StepOrder $PinnedStepOrder -Patterns $PinnedPatterns)) "Unresolved placeholder found in $surface after normalization."
}
Add-PassedCheck -Number 8 -Name 'placeholder scan with the one pinned, closed-world JSON-pointer exclusion (AC-P4-06)'

# ---------------------------------------------------------------------------
# Check 9: negative-synthetic-empty-malformed-input.json resilience
# ---------------------------------------------------------------------------

$resilienceFixtureDoc = ([IO.File]::ReadAllText((Join-Path $FixturesDir 'negative-synthetic-empty-malformed-input.json'))) | ConvertFrom-Json
$resilienceInvocation = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $RepositoryRoot -SyntheticSpecObject $resilienceFixtureDoc.input.syntheticSpec
Assert-True ($resilienceInvocation.ExitCode -eq 1) 'general-entrypoint-crashed-on-degenerate-input: exit code must be exactly 1.'
Assert-True (-not [string]::IsNullOrWhiteSpace($resilienceInvocation.RawStdout)) 'general-entrypoint-crashed-on-degenerate-input: stdout must carry a result.'
Assert-True (-not $resilienceInvocation.RawStdout.Contains('Exception')) 'general-entrypoint-crashed-on-degenerate-input: stdout must not carry a stack trace.'
Assert-True (-not $resilienceInvocation.RawStdout.Contains('At line:')) 'general-entrypoint-crashed-on-degenerate-input: stdout must not carry a terminating-error trace.'
$resilienceOut = $resilienceInvocation.Output
Assert-True ([StringComparer]::Ordinal.Equals([string]$resilienceOut.result, [string]$resilienceFixtureDoc.expected.result)) 'negative-synthetic-empty-malformed-input result mismatch.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$resilienceOut.reason, [string]$resilienceFixtureDoc.expected.reason)) 'negative-synthetic-empty-malformed-input reason mismatch.'
Assert-True (Test-DeepEqual -Left $resilienceOut.detail -Right $resilienceFixtureDoc.expected.detail) 'negative-synthetic-empty-malformed-input detail mismatch.'
$resilienceResult = [ordered]@{ schema = 'biostack.p4-resilience-check.v1'; pass = $true; exitCode = $resilienceInvocation.ExitCode; result = [string]$resilienceOut.result; reason = [string]$resilienceOut.reason }
Add-PassedCheck -Number 9 -Name 'fail-closed, non-throwing behavior on degenerate/empty/malformed input (AC-P4-10)'

# ---------------------------------------------------------------------------
# Check 10: composition-verification (five-source mutation-overlay test,
# AC-P4-02). See disclosed deviation D3 for probe (b)'s own corrected
# heading/alias pair.
# ---------------------------------------------------------------------------

$CompositionResults = New-Object 'System.Collections.Generic.List[object]'

[string[]]$StandardHeadings = @('## Objective', '## Surfaces', '## Contracts', '## Acceptance criteria', '## Tests', '## Rollback')

function New-ProbeSynthetic {
    param([string]$Status = 'review-candidate', [string[]]$Headings = $StandardHeadings, [hashtable]$ExtraFrontmatter = @{})
    $fm = [ordered]@{
        title                    = 'Composition-verification probe'
        status                   = $Status
        owner                    = 'p4_builder'
        created                  = '2026-10-10'
        updated                  = '2026-10-10'
        delivery_classes         = @('standard')
        guidance_classes         = @()
        substance_function_risk  = @()
        surfaces                 = @('docs/specs/scripts/validate-spec.ps1')
        ticket                   = 'BIO-P4-PROBE-001'
        function_review_status   = 'not-applicable'
    }
    foreach ($k in $ExtraFrontmatter.Keys) { $fm[$k] = $ExtraFrontmatter[$k] }
    return [ordered]@{ declaredShape = 'ticket-spec'; frontmatter = $fm; headings = $Headings }
}

# (a) statusClosedVocabulary append
$overlayA = New-ScratchOverlay -RepositoryRoot $RepositoryRoot
try {
    $schemaPathA = Join-Path $overlayA 'docs/specs/schemas/parcel-spec.schema.json'
    $schemaDocA = ([IO.File]::ReadAllText($schemaPathA)) | ConvertFrom-Json
    $vocabA = [System.Collections.Generic.List[string]]::new()
    foreach ($v in $schemaDocA.statusClosedVocabulary) { $vocabA.Add([string]$v) | Out-Null }
    $vocabA.Add('status-mutation-probe') | Out-Null
    $schemaDocA.statusClosedVocabulary = $vocabA.ToArray()
    Write-Utf8Lf -Path $schemaPathA -Content ($schemaDocA | ConvertTo-Json -Depth 30)

    $probeA = New-ProbeSynthetic -Status 'status-mutation-probe'
    $preA = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $RepositoryRoot -SyntheticSpecObject $probeA
    Assert-True ([StringComparer]::Ordinal.Equals([string]$preA.Output.result, 'invalid') -and [StringComparer]::Ordinal.Equals([string]$preA.Output.reason, 'invalid-status')) 'hardcoded-value-detected: statusClosedVocabulary pre-mutation baseline incorrect.'
    $postA = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $overlayA -SyntheticSpecObject $probeA
    Assert-True ([StringComparer]::Ordinal.Equals([string]$postA.Output.result, 'valid')) 'hardcoded-value-detected: parcel-spec.schema.json statusClosedVocabulary not reflected live.'
    $CompositionResults.Add([ordered]@{ source = 'parcel-spec.schema.json'; field = 'statusClosedVocabulary'; pass = $true }) | Out-Null
} finally {
    Remove-Item -LiteralPath $overlayA -Recurse -Force -ErrorAction SilentlyContinue
}

# (b) SECTION-HEADING-MAP.md rollback-row alias append (disclosed deviation
# D3: probe heading/alias pair corrected to not already contain the bare
# term "rollback" as a literal substring, so the mutation genuinely and
# mechanically distinguishes pre/post behavior).
$overlayB = New-ScratchOverlay -RepositoryRoot $RepositoryRoot
try {
    $headingMapPathB = Join-Path $overlayB 'docs/specs/schemas/SECTION-HEADING-MAP.md'
    $headingMapTextB = [IO.File]::ReadAllText($headingMapPathB)
    $pinnedRowB = '| rollback | rollback | delivery-class-controls.json |'
    Assert-True ($headingMapTextB.Contains($pinnedRowB)) 'SECTION-HEADING-MAP.md rollback row not found at its pinned, exact text.'
    $mutatedRowB = '| rollback | rollback, pilot wind down plan | delivery-class-controls.json |'
    $headingMapTextB = $headingMapTextB.Replace($pinnedRowB, $mutatedRowB)
    Write-Utf8Lf -Path $headingMapPathB -Content $headingMapTextB

    $probeHeadingsB = @('## Objective', '## Surfaces', '## Contracts', '## Acceptance criteria', '## Tests', '## Pilot Wind-Down Plan')
    $probeB = New-ProbeSynthetic -Headings $probeHeadingsB
    $preB = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $RepositoryRoot -SyntheticSpecObject $probeB
    Assert-True ([StringComparer]::Ordinal.Equals([string]$preB.Output.result, 'invalid') -and [StringComparer]::Ordinal.Equals([string]$preB.Output.reason, 'missing-required-section') -and [StringComparer]::Ordinal.Equals([string]$preB.Output.detail.term, 'rollback')) 'hardcoded-value-detected: SECTION-HEADING-MAP.md pre-mutation baseline incorrect.'
    $postB = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $overlayB -SyntheticSpecObject $probeB
    Assert-True ([StringComparer]::Ordinal.Equals([string]$postB.Output.result, 'valid')) 'hardcoded-value-detected: SECTION-HEADING-MAP.md rollback alias not reflected live.'
    $CompositionResults.Add([ordered]@{ source = 'SECTION-HEADING-MAP.md'; field = 'rollback canonical-alias cell'; pass = $true }) | Out-Null
} finally {
    Remove-Item -LiteralPath $overlayB -Recurse -Force -ErrorAction SilentlyContinue
}

# (c) CAPABILITY-FIELD-MAP.md new unconditional row
$overlayC = New-ScratchOverlay -RepositoryRoot $RepositoryRoot
try {
    $fieldMapPathC = Join-Path $overlayC 'docs/specs/schemas/CAPABILITY-FIELD-MAP.md'
    $newRowC = "`n" + '| `pilot_capability_probe_field` | probe | unconditional | must equal the pinned literal `pilot-probe-value` | none | none |'
    Add-Content -LiteralPath $fieldMapPathC -Value $newRowC -Encoding utf8NoBOM

    $probeC = New-ProbeSynthetic
    $preC = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $RepositoryRoot -SyntheticSpecObject $probeC
    Assert-True ([StringComparer]::Ordinal.Equals([string]$preC.Output.result, 'valid')) 'hardcoded-value-detected: CAPABILITY-FIELD-MAP.md pre-mutation baseline incorrect.'
    $postC = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $overlayC -SyntheticSpecObject $probeC
    Assert-True ([StringComparer]::Ordinal.Equals([string]$postC.Output.result, 'invalid') -and [StringComparer]::Ordinal.Equals([string]$postC.Output.reason, 'missing-required-frontmatter-key') -and [StringComparer]::Ordinal.Equals([string]$postC.Output.detail.field, 'pilot_capability_probe_field')) 'hardcoded-value-detected: CAPABILITY-FIELD-MAP.md new row not reflected live.'
    $CompositionResults.Add([ordered]@{ source = 'CAPABILITY-FIELD-MAP.md'; field = 'pilot_capability_probe_field (new row)'; pass = $true }) | Out-Null
} finally {
    Remove-Item -LiteralPath $overlayC -Recurse -Force -ErrorAction SilentlyContinue
}

# (d) delivery-class-controls.json standard.reviewers flip 1 -> 2
$overlayD = New-ScratchOverlay -RepositoryRoot $RepositoryRoot
try {
    $controlsPathD = Join-Path $overlayD 'docs/specs/schemas/delivery-class-controls.json'
    $controlsDocD = ([IO.File]::ReadAllText($controlsPathD)) | ConvertFrom-Json
    $controlsDocD.standard.reviewers = 2
    Write-Utf8Lf -Path $controlsPathD -Content ($controlsDocD | ConvertTo-Json -Depth 30)

    $fixtureD = ([IO.File]::ReadAllText((Join-Path $FixturesDir 'positive-ticket-spec-standard-single.json'))) | ConvertFrom-Json
    $preD = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $RepositoryRoot -SyntheticSpecObject $fixtureD.input.syntheticSpec
    Assert-True ([int]$preD.Output.foldSummary.reviewerCount -eq 1) 'hardcoded-value-detected: delivery-class-controls.json pre-mutation baseline incorrect.'
    $postD = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $overlayD -SyntheticSpecObject $fixtureD.input.syntheticSpec
    Assert-True ([int]$postD.Output.foldSummary.reviewerCount -eq 2) 'hardcoded-value-detected: delivery-class-controls.json standard.reviewers flip not reflected live.'
    $CompositionResults.Add([ordered]@{ source = 'delivery-class-controls.json'; field = 'standard.reviewers'; pass = $true }) | Out-Null
} finally {
    Remove-Item -LiteralPath $overlayD -Recurse -Force -ErrorAction SilentlyContinue
}

# (e) product-capability-safety-contract.json ordinary.C2.value flip allowed -> degraded
$overlayE = New-ScratchOverlay -RepositoryRoot $RepositoryRoot
try {
    $contractPathE = Join-Path $overlayE 'docs/specs/schemas/product-capability-safety-contract.json'
    $contractDocE = ([IO.File]::ReadAllText($contractPathE)) | ConvertFrom-Json
    $contractDocE.labels.ordinary.behavior.C2.value = 'degraded'
    Write-Utf8Lf -Path $contractPathE -Content ($contractDocE | ConvertTo-Json -Depth 30)

    $fixtureE = ([IO.File]::ReadAllText((Join-Path $FixturesDir 'positive-synthetic-capability-claim-satisfied.json'))) | ConvertFrom-Json
    $preE = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $RepositoryRoot -SyntheticSpecObject $fixtureE.input.syntheticSpec
    Assert-True ([StringComparer]::Ordinal.Equals([string]$preE.Output.result, 'valid')) 'hardcoded-value-detected: product-capability-safety-contract.json pre-mutation baseline incorrect.'
    $postE = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $overlayE -SyntheticSpecObject $fixtureE.input.syntheticSpec
    Assert-True ([StringComparer]::Ordinal.Equals([string]$postE.Output.result, 'invalid') -and [StringComparer]::Ordinal.Equals([string]$postE.Output.reason, 'capability-claim-drift')) 'hardcoded-value-detected: product-capability-safety-contract.json ordinary.C2 flip not reflected live.'
    $CompositionResults.Add([ordered]@{ source = 'product-capability-safety-contract.json'; field = 'labels.ordinary.behavior.C2.value'; pass = $true }) | Out-Null
} finally {
    Remove-Item -LiteralPath $overlayE -Recurse -Force -ErrorAction SilentlyContinue
}

Assert-True ($CompositionResults.Count -eq 5) 'Composition-verification must cover exactly five sources.'
Add-PassedCheck -Number 10 -Name 'composition-verification: five-source mutation-overlay test (AC-P4-02)'

# ---------------------------------------------------------------------------
# Check 11/12: evidence bundle and clean tree
# ---------------------------------------------------------------------------

function Assert-OnlyAuthorizedEvidenceStatus {
    param([string[]]$StatusLines)
    foreach ($line in $StatusLines) {
        Assert-True $line.StartsWith('?? artifacts/p4-verification/', [StringComparison]::Ordinal) "Unauthorized or tracked worktree status entry: $line"
    }
}

$expectedEvidencePath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot 'artifacts/p4-verification'))
$resolvedEvidencePath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $EvidenceDirectory))
Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($resolvedEvidencePath.TrimEnd('\', '/'), $expectedEvidencePath.TrimEnd('\', '/'))) 'EvidenceDirectory must resolve to artifacts/p4-verification under the repository root.'

$trackedEvidence = Get-GitResult -Arguments @('ls-files', '--error-unmatch', '--', 'artifacts/p4-verification')
Assert-True ($trackedEvidence.ExitCode -ne 0) 'artifacts/p4-verification must be untracked.'
[IO.Directory]::CreateDirectory($resolvedEvidencePath) | Out-Null

$headCommit = (@(Invoke-Git -Arguments @('rev-parse', 'HEAD')))[0].Trim()

Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'changed-files.txt') -Content ($ActualChanges -join "`n")
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'linter-structural-check.json') -Content (([ordered]@{
                schema                       = 'biostack.p4-linter-structural-check.v1'
                pass                         = $true
                outputKeysInvalidFolded      = $ValidOutputKeys
                outputKeysUnrecognizedShape  = $UnrecognizedOutputKeys
                pathPatternChecks            = @(
                    [ordered]@{ path = 'docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md'; resolvedShape = [string]$valid3.Output.shape },
                    [ordered]@{ path = 'docs/INITIATIVES/biostack-governed-delivery/CHARTER.md'; resolvedShape = [string]$valid2.Output.shape }
                )
            }) | ConvertTo-Json -Depth 10)
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'fixture-results.json') -Content (($FixtureResults.ToArray() + [ordered]@{
                fixture               = 'positive-real-spec-p3b-cross-check (overlay-only cross-check sub-result)'
                pass                  = $true
                p4OverlayResult       = [string]$p4OverlaySide.Result
                p3bCheck10Result      = [string]$p3bOverlaySide.Result
            }) | ConvertTo-Json -Depth 15)
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'resilience-check.json') -Content (($resilienceResult | ConvertTo-Json -Depth 10))
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'composition-verification.json') -Content (($CompositionResults.ToArray() | ConvertTo-Json -Depth 10))

$preSummaryStatus = @(Invoke-Git -Arguments @('status', '--porcelain=v1', '--untracked-files=all'))
Assert-OnlyAuthorizedEvidenceStatus -StatusLines $preSummaryStatus
Add-PassedCheck -Number 11 -Name 'authorized UTF-8/LF evidence bundle generation'

$summary = [ordered]@{
    schema              = 'biostack.p4-verification-summary.v1'
    pass                = $true
    baseCommit          = $BaseCommit.ToLowerInvariant()
    headCommit          = $headCommit
    builderId           = $BuilderId
    reviewerIds         = [string[]]$ReviewerIds
    disclosedDeviations = @(
        'D1: positive-real-spec-p3b-cross-check.json expected.result is the honestly-computed "invalid"/"missing-required-section" (naming contracts) for parcels/P3-B.md, not the "valid" literal P4.md''s own narrative text names, because SECTION-HEADING-MAP.md''s own byte-pinned heading-length-bound rule objectively disqualifies that real, frozen file''s actual ### Required document contract N: ... headings for the 1-token terms contracts/tests.',
        'D2: check 7''s cross-verifier-agreement comparison is scoped to the capability-field-binding overlay sub-computation (the one thing verify-p3b.ps1''s own check 10 ever computes), not validate-spec.ps1''s full top-level pipeline result, which stage 4''s real finding (D1) would otherwise make permanently unable to agree with check 10 for this one real file.',
        'D3: check 10(b)''s probe heading/alias pair is corrected from the spec''s own self-contradictory "pilot-rollback-alias" worked example (which already contains the bare term "rollback" as a literal substring and therefore cannot mechanically distinguish pre/post mutation behavior) to a pair that does not already contain the bare term, while mutating the exact same SECTION-HEADING-MAP.md field the spec names.'
    )
    checks              = $CheckResults.ToArray()
}
Write-Utf8Lf -Path (Join-Path $resolvedEvidencePath 'verification-summary.json') -Content ($summary | ConvertTo-Json -Depth 12)

[string[]]$ExpectedEvidenceFiles = @(
    'changed-files.txt', 'linter-structural-check.json', 'fixture-results.json',
    'resilience-check.json', 'composition-verification.json', 'verification-summary.json'
)
foreach ($evidenceFile in $ExpectedEvidenceFiles) {
    Assert-True (Test-Path -LiteralPath (Join-Path $resolvedEvidencePath $evidenceFile)) "Missing evidence file: $evidenceFile"
}

$finalStatus = @(Invoke-Git -Arguments @('status', '--porcelain=v1', '--untracked-files=all'))
Assert-OnlyAuthorizedEvidenceStatus -StatusLines $finalStatus
Add-PassedCheck -Number 12 -Name 'untracked evidence directory and clean tree'

Write-Output 'P4 verification PASS'
exit 0
