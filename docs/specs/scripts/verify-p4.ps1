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
# REMEDIATION-1 (fix/p4-remediation-1): the original implementation (commit
# cbf6e22) disclosed three deviations from P4.md's then-current text (D1/D2/
# D3, below) without a ratified amendment, which p4_impl_review_2 found a
# BLOCKER (F1), two MAJORs (F2/F3), and two MINORs (F4/F5) against. The
# coordinator ratified a three-pin amendment, A-P4-2 (docs/INITIATIVES/
# COORDINATOR-DECISIONS-2026-10-07.md ## D-M), landed in its own commit
# immediately before this one, amending P4.md itself to match the corrected,
# honestly-computed reality and to restore AC-P4-05's full cross-verifier-
# agreement scope. This code commit implements that ratified amendment plus
# the remaining findings (F1 BLOCKER, F3's fixture gap, F5's hash-pinning):
#
# D1 (ratified, A-P4-2a): `positive-real-spec-p3b-cross-check.json`'s
#     `expected.result`/`expected.reason`/`expected.detail` are the honestly
#     and independently computed top-level answer for `parcels/P3-B.md`
#     (`"invalid"` / `"missing-required-section"` / naming `contracts`),
#     because SECTION-HEADING-MAP.md's own byte-pinned heading-length-bound
#     rule objectively disqualifies every one of `parcels/P3-B.md`'s real
#     `### Required document contract N: ...` H3 subheadings for the
#     1-token terms `contracts`/`tests`, and the file carries no bare
#     `## Contracts`/`## Tests` heading. `parcels/P3-B.md` is frozen and
#     cannot be edited. This is now P4.md's own pinned value (amendment
#     commit, A-P4-2a), not a builder-local deviation.
# D2 (ratified, A-P4-2a; full scope restored): check 7's cross-verifier-
#     agreement comparison performs BOTH (a) a full top-level
#     result/reason/detail comparison of validate-spec.ps1's real CLI output
#     for `parcels/P3-B.md` against the now-pinned, honestly-computed
#     expected value (D1), and (b) the narrower capability-field-binding
#     overlay sub-comparison against verify-p3b.ps1's own check 10 logic
#     (the one sub-computation check 10 ever performs) -- restored to the
#     amendment's required full scope, not narrowed to (b) alone. Sub-check
#     (b)'s both sides are still computed by genuinely invoking the real
#     logic: check 10's own side via a byte-ported copy of verify-p3b.ps1's
#     own `Test-CapabilitySafetyOverlay` (read-only prior art, the same
#     ported-reference-implementation pattern verify-p2.ps1's own
#     `Invoke-Fold` already established), and validate-spec.ps1's own side
#     via that same script's actual, real `Test-CapabilityFieldBinding`
#     function (extracted and dot-sourced from the real, shipped
#     docs/specs/scripts/validate-spec.ps1 file -- never re-derived or
#     duplicated).
# D3 (ratified, A-P4-2a): check 10(b)'s own worked example is corrected from
#     the spec's own, now-amended, self-contradictory `pilot-rollback-alias`
#     probe (which already contained the bare term "rollback" as a literal
#     substring and therefore could not mechanically distinguish pre/post
#     mutation behavior) to the coordinator-ratified, non-self-contradictory
#     pair (`## Pilot Wind-Down Plan` / alias `pilot wind down plan`), which
#     genuinely and mechanically distinguishes pre-mutation (missing-
#     required-section) from post-mutation (satisfied) behavior while
#     mutating the exact same field (SECTION-HEADING-MAP.md's `rollback` row
#     canonical-alias cell) the spec names.
#
# Additionally, this commit: (F1, BLOCKER) fixes validate-spec.ps1's stage-5
# placeholder scan to also scan the real file's Markdown body prose in disk
# mode (previously frontmatter values and heading text only); (F3) adds a
# dedicated fixture for the ratified 13th reason literal,
# `missing-required-field`, and a reason-vocabulary closed-world assertion
# to check 6; (F5) hash-pins every fixture's own `expected` block in this
# file, independent of check 6's own live re-derivation.
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
    'docs/specs/schemas/fixtures/p4/negative-missing-required-field-migration-conditional.json',
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
    # parcels/P4.md intentionally excluded here (remediation-1, A-P4-2/D-M):
    # the coordinator ratified a three-pin amendment to this parcel's own
    # spec (see the dedicated amendment-commit check, below, immediately
    # after check 4); it is still fully frozen against the code commit that
    # follows the amendment commit, just not against the pre-amendment
    # BaseCommit this check otherwise pins every other frozen path to.
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
    'negative-invalid-status-value', 'negative-missing-required-field-migration-conditional',
    'negative-placeholder-entity-disguised-multilabel',
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
# F5 remediation: fixture expected-outcome hash-pinning (remediation-1).
# A fixture's own `expected` block is canonicalized (sorted keys, no
# whitespace, invariant-culture scalar formatting) and SHA-256-hashed; the
# resulting hex digest is compared against a table pinned below, hardcoded
# at remediation-authoring time by running this exact function once against
# every fixture's own `expected` block as shipped. This is an independent
# backstop against Deterministic verification check 6's own live
# re-derivation (check 6 compares a fixture's `expected` against
# validate-spec.ps1's own live output -- both could regress together in the
# same weakening direction; this hash does not move unless a fixture file is
# deliberately re-authored and its pinned hash is deliberately re-derived in
# the same remediation commit that touches it).
# ---------------------------------------------------------------------------

function ConvertTo-CanonicalJson {
    param($Value)
    if ($null -eq $Value) { return 'null' }
    if ($Value -is [bool]) { return $(if ($Value) { 'true' } else { 'false' }) }
    if ($Value -is [string]) { return (ConvertTo-Json -InputObject $Value -Compress) }
    if (($Value -is [int]) -or ($Value -is [long])) { return [string]$Value }
    if ($Value -is [double] -or $Value -is [decimal]) { return $Value.ToString([System.Globalization.CultureInfo]::InvariantCulture) }
    $isObj = ($Value -is [System.Collections.IDictionary]) -or ($Value -is [System.Management.Automation.PSCustomObject])
    if ($isObj) {
        $keys = Sort-Ordinal -Values @(Get-PropNames -Obj $Value)
        $parts = foreach ($k in $keys) { ((ConvertTo-Json -InputObject $k -Compress) + ':' + (ConvertTo-CanonicalJson -Value (Get-PropValue -Obj $Value -Name $k))) }
        return '{' + ($parts -join ',') + '}'
    }
    $isArr = ($Value -is [System.Collections.IEnumerable]) -and -not ($Value -is [string])
    if ($isArr) {
        $parts2 = foreach ($item in @($Value)) { ConvertTo-CanonicalJson -Value $item }
        return '[' + ($parts2 -join ',') + ']'
    }
    return (ConvertTo-Json -InputObject ([string]$Value) -Compress)
}

function Get-CanonicalJsonSha256 {
    param($Value)
    $canon = ConvertTo-CanonicalJson -Value $Value
    $bytes = [Text.Encoding]::UTF8.GetBytes($canon)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hashBytes = $sha256.ComputeHash($bytes)
    } finally {
        $sha256.Dispose()
    }
    return -join ($hashBytes | ForEach-Object { $_.ToString('x2') })
}

# Pinned independently of validate-spec.ps1/verify-p4.ps1's own live logic --
# computed once, at remediation-authoring time, from the seventeen fixture
# files' own `expected` blocks exactly as shipped in this commit.
$script:PinnedFixtureExpectedHashes = @{
    'negative-capability-claim-drift-referential'            = 'a86591d6ffd2c497d57eb6661dfe1b7de6d711c79b17d7f6b99263033adf8995'
    'negative-capability-field-missing-on-trigger'            = '33a9f703dd1c3f4200bbc3f8d0f1bef5c9ade3756b70f270437b6066eb192490'
    'negative-incompatible-controls-scalar-conflict'          = '1b8f682fe327c9322140765160cc1b4357423d28826f18a454def5b5fb661744'
    'negative-invalid-status-value'                           = 'ea7faf568e04da3f520388f6b9c3af24b9a00a84ce63a619415a4d578a009a6a'
    'negative-missing-required-field-migration-conditional'   = '15573ddfb84b55ebfbbfad46a5372a89fb1dc03d602737ac4a53da513c8cc4cb'
    'negative-missing-required-frontmatter-field'             = '0bd9147f7ee2f2b97af961068114b6e55b28677a81d8a8940e6f668c9feda246'
    'negative-placeholder-entity-disguised-multilabel'        = '4584cd568af59c4eb85dc6c880ae70b55ba0f29e75630649c712dad4e45c2fcb'
    'negative-placeholder-homoglyph-confusables'              = '76857eef1dd353ea8f3234b4fc36889c0566667f4bd161307b7ab21c0fa90670'
    'negative-synthetic-capability-claim-unsatisfied-twin'    = 'b6ecae8e8c63f5e69923e4ecc4ba5e57f961a2ed0018ce52da83e06bdbf78da8'
    'negative-synthetic-empty-malformed-input'                = 'a6ec2d97c7b5adb2eee0873fe4016351d2ccb063de31ab47881b3b984a5a7ac5'
    'negative-unknown-delivery-label-multilabel'              = '5c668aebad2ba4b7cf88990f971617f171ce4facaca5b8913171b638c5bc3bd7'
    'negative-unknown-extension-point-reference'              = '59893e2ef11bdc649c18c10172027fcf7fd39e40287555689334e50f942b9803'
    'negative-unrecognized-shape-path'                        = 'd4b7bd829c1563f25ea237ed1aa904b48953f208355479ae2c8763d6692dcb56'
    'positive-coordinator-parcel-multilabel-fold'             = '9fedb347573750d5520b211234425c16b31c09712a7056fe6dc8d7a5d98f19c1'
    'positive-real-spec-p3b-cross-check'                      = '606796dedd6c8a3ae05ee588778c13a9464918bdac6d1b5bf0874dc501adf330'
    'positive-synthetic-capability-claim-satisfied'           = '64c71041b9a5d220a5bd4b1ec771d35f47919704dcd62635385488df9f3f7de2'
    'positive-ticket-spec-standard-single'                    = 'a72c0733452251bcf208f1fe6824e1103c299b5fff28effa35cb65b01636fa9c'
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
# Carried-forward allow-list discipline (P3-A's own closure lesson; P4.md's
# own "Carried-forward allow-list discipline" paragraph): when BaseCommit
# pre-dates one or more coordinator-authored, out-of-scope dispatch/decision
# paths that are not part of this parcel's own diff (e.g. a pre-existing
# BaseCommit anchor reused across a later amendment/remediation pass, which
# necessarily also picks up any coordinator path touched on main in the
# interim), those paths are tolerated ONLY if named and pinned here exactly
# -- never by wildcard or unbounded allowance -- plus this parcel's own spec
# file, parcels/P4.md (the amendment-commit carve-out, check 4, above).
[string[]]$KnownOutOfScopeCoordinatorPaths = @(
    'docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md',
    'docs/INITIATIVES/COORDINATOR-DISPATCH-QUEUE.md',
    'docs/INITIATIVES/biostack-governed-delivery/dispatch/GATE2-P4-IMPLEMENTATION.md'
)
[string[]]$ExpectedChanges = Sort-Ordinal -Values ($AllowedSurfaces + $KnownOutOfScopeCoordinatorPaths + @('docs/INITIATIVES/biostack-governed-delivery/parcels/P4.md'))
[string[]]$ActualChanges = Sort-Ordinal -Values (Invoke-Git -Arguments @('diff', '--name-only', "$BaseCommit...HEAD", '--'))
Assert-SequenceEqual -Actual $ActualChanges -Expected $ExpectedChanges -Label 'BaseCommit...HEAD changed files'
Add-PassedCheck -Number 3 -Name 'exact changed-file set equals the 21 allowed surfaces, plus parcels/P4.md (amendment carve-out) and the named, pinned, out-of-scope coordinator paths -- no open-ended tolerance (amended: +1 fixture, A-P4-2b)'

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

# Amendment-commit carve-out (remediation-1, A-P4-2/D-M): parcels/P4.md is
# allowed to differ from BaseCommit (it carries the coordinator-ratified
# three-pin amendment -- A-P4-2a/b/c), but it must be completely untouched
# by the code commit that follows the amendment commit ("spec amendments
# alone before code"; "no governance-contract edits beyond the D-M pins").
# This is checked mechanically, not by reviewer judgment: the amendment
# commit must be HEAD's immediate, sole parent carrying any parcels/P4.md
# change since BaseCommit, and HEAD itself (the code commit) must carry no
# further parcels/P4.md change.
$p4SpecPathForCarveOut = 'docs/INITIATIVES/biostack-governed-delivery/parcels/P4.md'
$headParent = (@(Invoke-Git -Arguments @('rev-parse', 'HEAD~1')))[0].Trim()
$codeCommitP4Diff = Get-GitResult -Arguments @('diff', '--quiet', "$headParent..HEAD", '--', $p4SpecPathForCarveOut)
Assert-True ($codeCommitP4Diff.ExitCode -eq 0) 'parcels/P4.md must be unchanged by the code commit (HEAD); only the dedicated amendment commit (HEAD~1) may carry the ratified A-P4-2 pins.'
$amendmentCommitP4Diff = Get-GitResult -Arguments @('diff', '--quiet', "$BaseCommit..$headParent", '--', $p4SpecPathForCarveOut)
Assert-True ($amendmentCommitP4Diff.ExitCode -ne 0) 'parcels/P4.md must carry the ratified A-P4-2 amendment in the commit immediately preceding the code commit (HEAD~1).'

Add-PassedCheck -Number 4 -Name 'frozen surfaces byte-identical to BaseCommit, plus parcels/P4.md amendment-commit carve-out (amended once, in HEAD~1 only, byte-frozen thereafter -- A-P4-2/D-M)'

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
# Check 6: seventeen fixtures, reproduced by actually invoking
# validate-spec.ps1, hash-pinned independently (F5), and reason-vocabulary
# closed (A-P4-2b / F3 coverage)
# ---------------------------------------------------------------------------

[string[]]$PinnedReasonVocabulary = @(
    'unknown-label', 'empty-required-axis', 'incompatible-controls',
    'missing-required-frontmatter-key', 'missing-required-section',
    'placeholder-violation', 'unknown-extension-point', 'unrecognized-shape',
    'capability-claim-drift', 'invalid-function-review-status',
    'premature-public-enablement-claim', 'missing-required-field', 'invalid-status'
)
Assert-True ($PinnedReasonVocabulary.Count -eq 13) 'Pinned reason vocabulary must carry exactly thirteen literals (A-P4-2b).'

$FixtureResults = New-Object 'System.Collections.Generic.List[object]'
$FixturesDir = Join-Path $RepositoryRoot 'docs/specs/schemas/fixtures/p4'

foreach ($name in $FixtureNames) {
    $fixturePath = Join-Path $FixturesDir "$name.json"
    $fixtureDoc = ([IO.File]::ReadAllText($fixturePath)) | ConvertFrom-Json
    [string[]]$topKeys = Sort-Ordinal -Values @(Get-PropNames -Obj $fixtureDoc)
    Assert-SequenceEqual -Actual $topKeys -Expected (Sort-Ordinal -Values @('input', 'expected')) -Label "$name top-level keys"

    # F5: hash-pin this fixture's own `expected` block, independent of its
    # live re-derivation below.
    Assert-True ($script:PinnedFixtureExpectedHashes.ContainsKey($name)) "$name : no pinned expected-outcome hash recorded (F5)."
    $actualExpectedHash = Get-CanonicalJsonSha256 -Value $fixtureDoc.expected
    Assert-True ([StringComparer]::OrdinalIgnoreCase.Equals($actualExpectedHash, $script:PinnedFixtureExpectedHashes[$name])) "$name : expected-outcome hash mismatch (F5). Pinned $($script:PinnedFixtureExpectedHashes[$name]), computed $actualExpectedHash -- the fixture's own expected block was weakened or altered without re-deriving its pinned hash."

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

    # F3 coverage: validate-spec.ps1's own output `reason` literal, whenever
    # `result` is `invalid`, must be a member of the pinned, now-thirteen-
    # literal closed vocabulary (A-P4-2b) -- never silently outside it.
    if ([StringComparer]::Ordinal.Equals([string]$produced.result, 'invalid')) {
        Assert-True ($PinnedReasonVocabulary -ccontains [string]$producedReason) "$name : produced reason '$producedReason' is outside the pinned thirteen-literal closed vocabulary (A-P4-2b)."
    }

    $FixtureResults.Add([ordered]@{ fixture = $name; pass = $true; result = [string]$produced.result; reason = [string]$producedReason; expectedHash = $script:PinnedFixtureExpectedHashes[$name] }) | Out-Null
}
Add-PassedCheck -Number 6 -Name 'seventeen fixtures reproduced exactly by actually invoking validate-spec.ps1, hash-pinned (F5) and reason-vocabulary-closed (A-P4-2b/F3) (AC-P4-03/04)'

# ---------------------------------------------------------------------------
# Check 7: positive-real-spec-p3b-cross-check.json cross-verifier agreement,
# restored to full scope (A-P4-2a/D-M -- not the builder's narrowed form):
# two independent, additive sub-checks, neither substituting for the other.
# ---------------------------------------------------------------------------

# Sub-check (a): full top-level agreement. validate-spec.ps1's real CLI,
# invoked exactly as every other fixture is invoked (never an internally-
# extracted or ported sub-function), must produce the pinned, honestly-
# computed expected result/reason/detail for parcels/P3-B.md.
$p3bFixtureDoc = ([IO.File]::ReadAllText((Join-Path $FixturesDir 'positive-real-spec-p3b-cross-check.json'))) | ConvertFrom-Json
$p3bCliInvocation = Invoke-ValidateSpecCli -ScriptPath $ValidateSpecPath -RepoRootArg $RepositoryRoot -SpecPathArg ([string]$p3bFixtureDoc.input.specPath)
$p3bCliOutput = $p3bCliInvocation.Output
Assert-True ($null -ne $p3bCliOutput) 'general-linter-parcel-verifier-disagreement: validate-spec.ps1 produced no parseable output for parcels/P3-B.md.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$p3bCliOutput.result, [string]$p3bFixtureDoc.expected.result)) 'general-linter-parcel-verifier-disagreement: validate-spec.ps1''s own full top-level result for parcels/P3-B.md does not match the pinned, honestly-computed expected value (A-P4-2a).'
Assert-True ([StringComparer]::Ordinal.Equals([string]$p3bCliOutput.reason, [string]$p3bFixtureDoc.expected.reason)) 'general-linter-parcel-verifier-disagreement: validate-spec.ps1''s own full top-level reason for parcels/P3-B.md does not match the pinned, honestly-computed expected value (A-P4-2a).'
Assert-True (Test-DeepEqual -Left $p3bCliOutput.detail -Right $p3bFixtureDoc.expected.detail) 'general-linter-parcel-verifier-disagreement: validate-spec.ps1''s own full top-level detail for parcels/P3-B.md does not match the pinned, honestly-computed expected value (A-P4-2a).'

# Sub-check (b): capability-field-binding sub-agreement -- the one narrower
# sub-outcome both tools can actually compute for this file (stage 4's real
# missing-required-section finding, sub-check (a) above, pre-empts
# validate-spec.ps1's own stage 7 in its normal top-level pipeline; this
# sub-check evaluates stage 7's own logic directly, independent of that
# pre-emption, purely to compare it against verify-p3b.ps1's own check 10
# logic -- it never feeds back into or overrides sub-check (a)'s top-level
# result above).
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

Assert-True ([StringComparer]::Ordinal.Equals([string]$p4OverlaySide.Result, [string]$p3bOverlaySide.Result)) 'capability-overlay-sub-agreement-mismatch: Result mismatch between validate-spec.ps1''s own capability-field-binding logic and verify-p3b.ps1''s own check 10 logic, for parcels/P3-B.md.'
Assert-True ([StringComparer]::Ordinal.Equals([string]$p4OverlaySide.Result, 'valid')) 'capability-overlay-sub-agreement-mismatch: expected both sides to resolve valid for parcels/P3-B.md (neither side''s capability-bearing fields are triggered).'

# Independently require validate-spec.ps1's own foldSummary/boundFieldsChecked
# for this real file (computed above via the real P3-B.md fixture invocation
# in check 6) to be internally well-formed: schema-correct foldSummary and a
# boundFieldsChecked entry for each of the six CAPABILITY-FIELD-MAP.md rows
# when stage 7 is reached in isolation.
Assert-True ($p4OverlaySide.Checked.Count -eq 6) 'parcels/P3-B.md overlay-only evaluation must report all six CAPABILITY-FIELD-MAP.md rows checked.'
foreach ($row in $p4OverlaySide.Checked) {
    Assert-True ([bool]$row.pass) "parcels/P3-B.md overlay-only evaluation row '$($row.field)' did not pass."
}

Add-PassedCheck -Number 7 -Name 'cross-verifier agreement with verify-p3b.ps1 on parcels/P3-B.md: (a) full top-level result/reason/detail agreement with the pinned expected value, and (b) capability-field-binding sub-agreement with check 10''s own logic -- full scope restored, not narrowed (AC-P4-05; A-P4-2a/D-M)'

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
                fixture               = 'positive-real-spec-p3b-cross-check (cross-verifier-agreement sub-result, full scope -- A-P4-2a)'
                pass                  = $true
                fullTopLevelResult    = [string]$p3bCliOutput.result
                fullTopLevelReason    = [string]$p3bCliOutput.reason
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
        'R1 (resolved by ratified amendment A-P4-2a/D-M, remediation-1): the original builder commit (cbf6e22) unilaterally set positive-real-spec-p3b-cross-check.json''s expected.result to the honestly-computed "invalid"/"missing-required-section" (naming contracts) for parcels/P3-B.md, diverging from P4.md''s own then-current narrative text ("valid") without a ratified amendment, and narrowed check 7''s cross-verifier-agreement comparison to the capability-field-binding overlay sub-computation only. The coordinator ratified the corrected expected.result and the non-self-contradictory check 10(b) probe pair (A-P4-2a/A-P4-2a note on pilot-rollback-alias) via D-M, amending P4.md itself (remediation-1 commit 1) before this code commit landed. Check 7 now performs BOTH the full top-level comparison (sub-check (a), restored) and the capability-overlay sub-comparison (sub-check (b), retained) -- neither narrows or substitutes for the other.',
        'R2 (resolved by ratified amendment A-P4-2a/D-M, remediation-1): check 10(b)''s probe heading/alias pair is corrected from the spec''s own, now-amended, self-contradictory "pilot-rollback-alias" worked example (which already contained the bare term "rollback" as a literal substring and therefore could not mechanically distinguish pre/post mutation behavior) to the coordinator-ratified pair that does not already contain the bare term ("## Pilot Wind-Down Plan" / alias "pilot wind down plan"), while mutating the exact same SECTION-HEADING-MAP.md field the spec names.',
        'R3 (F3, resolved by ratified amendment A-P4-2b/D-M, remediation-1): missing-required-field is ratified as the thirteenth literal of validate-spec.ps1''s own closed reason vocabulary (a pre-existing routing-output.schema.json/fold-engine.md literal, not an invention); it now has its own dedicated fixture (negative-missing-required-field-migration-conditional.json) and check 6 additionally asserts every produced invalid-result reason is a member of the pinned thirteen-literal vocabulary.',
        'R4 (F1, code fix, remediation-1): validate-spec.ps1''s stage-5 placeholder scan now additionally scans the real file''s full Markdown body prose (disk-mode only; $parsed.Body, already available at that point) rather than only frontmatter values and heading text, per P4.md''s own stage-5 text ("every frontmatter value and heading/body text available to the validator"). Reproduced live against the reviewer''s own ATTACK.md-style proof-of-concept: pre-fix, a real file with a fully clean frontmatter/heading set but a body consisting entirely of repeated unresolved-decision placeholder markers was reported "valid"; post-fix it is correctly reported "invalid"/"placeholder-violation".',
        'R5 (F5, code fix, remediation-1): every fixture''s own expected block is now additionally hash-pinned (SHA-256 over a canonical, sorted-key JSON serialization) in verify-p4.ps1, independent of check 6''s own live re-derivation against validate-spec.ps1''s actual output, as a backstop against a future rework that weakens both together.'
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
