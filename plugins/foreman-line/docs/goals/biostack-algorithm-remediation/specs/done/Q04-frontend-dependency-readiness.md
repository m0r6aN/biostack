# Parcel Q04: Frontend Dependency Readiness

Status: **COMPLETE — dependency environment ready; P07 and full frontend verification green**

Coordinator re-lint: **PASSED 2026-09-02**. The unchanged parcel boundary remains command-only, requires an offline npm install with no registry fallback, selects only the existing network-faked route test, validates exact Git/blob identities, and bounds recursive cleanup to three pre-absent, resolved paths inside the isolated Q04 frontend directory. The embedded final-attempt PowerShell parses with zero errors; the installed `Tee-Object` exposes `FilePath` and `Append`; the new write-once transcript path is absent. Runtime capture behavior remains subject to the final execution and fresh review.

Procedural amendment 2026-09-02: two bounded runs are rejected and remain preserved as non-controlling evidence. Run 1 proved offline dependency readiness and `2/2` default-worker test success, but npm 11.6.2 consumed the required worker flags instead of forwarding them. Run 2 emitted the corrected direct-node argument vector and displayed `1/1` file and `2/2` tests passing in the live terminal, but `Start-Transcript` omitted the native npm/Vitest stream, the transcript-based count checks were false, and separately submitted `elseif`/`else` clauses failed to bind to the preceding `if`; therefore it did not produce an acceptable outcome token. This spec defines one third and final bounded attempt. It uses a new transcript path, captures every native stdout/stderr stream explicitly through `Tee-Object`, preserves `$LASTEXITCODE` immediately after each native command, and assigns outcome precedence without `elseif`. A further evidence-capture failure terminally blocks Q04; it does not authorize another run. This is a command-level repair only; it does not alter a Gate 1 decision or unblock P07 before fresh acceptance.

## Goal

Determine whether the pinned BioStack frontend dependency graph can be installed entirely from the local npm cache and whether the one existing `main`-based research-suggestion route test passes under the deterministic worker settings required by plan-review finding F1. Q04 may produce only terminal evidence. It makes no repository source change, imports no diagnostic test, creates no candidate commit, and does not remediate P07.

## Initiative / Project / Wave

- Initiative: `biostack-algorithm-remediation`
- Project: `BioStack` / `frontend`
- Parcel: `Q04-frontend-dependency-readiness`
- Wave: Wave Q — adjudicate inconclusive items
- Risk/routing: medium environment-readiness risk; command-only investigator
- Dependency role: Q04 must reach `READY` before P07 may be dispatched. Q04 success does not authorize P07 by itself; P07 remains subject to its ratified decisions, shaped spec, Step 0, review, and gates.

## Branch / Worktree / Base

- Branch: `codex/biostack-remediation-q04`
- Isolated worktree: `C:\Users\clint\.codex\worktrees\biostack-remediation-q04\BioStack`
- Required starting and ending commit: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Required starting and ending tree: `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`
- Pinned `frontend/package.json` blob: `c961913b9c7fd598f4a6c5a6be811edbe35bfe07`
- Pinned `frontend/package-lock.json` blob: `6772a07e5d8cfb575788f78afda23ec388bc5720`
- Pinned main-based test blob: `b1619779e26cdf34d7d47d3e51858c54b43a31fc`

The branch and worktree persist from the two rejected attempts. Before this final attempt, the coordinator must reconfirm their ownership, exact base/head/tree, empty status, and absence of generated roots. Do not recreate them from, merge, or otherwise consume a diagnostic branch. If either is under another owner or has drifted, stop for coordinator reconciliation.

## Dependencies and Preconditions

1. Gate 1 D1-D14 and contingent Gate 2 authority cover Q04. Pending Amendment 01 does not reopen a Q04 decision, but this final-attempt amendment must pass fresh coordinator lint and receive a new Step 0 confirmation before Q04 may run.
2. Before dispatch, the coordinator owns the `origin/main` comparison and must confirm it still equals the pinned base or apply the charter's base-change rule. The investigator must not fetch, pull, or contact a remote Git host.
3. The worktree must begin on the exact branch, commit, and tree above with an empty `git status --porcelain=v1 --untracked-files=all` receipt.
4. The three pinned frontend blobs must match before dependency installation. The lockfile is lockfile version 3 and has 636 package entries on the pinned base. `frontend/package.json` maps `npm test` to `vitest run`; the lock resolves Vitest `4.1.10`.
5. The generated directories named in the cleanup contract must be absent before execution. If any already exists, stop rather than deleting pre-existing ignored state.
6. `rtk` was not resolvable in the shaping PowerShell session. The commands below are therefore written raw under the repository AGENTS.md debugging exception. If `rtk` is available during dispatch, the investigator may use the semantically identical wrapped form, but must preserve every npm and Vitest argument and record which form ran.

## Authority and Allowed Effects — AF-Q04

- No repository file may be created, modified, deleted, renamed, staged, or committed.
- Commands may create only ignored dependency/test artifacts under the isolated Q04 worktree during execution. The known permitted generated roots are exactly:
  - `frontend/node_modules`
  - `frontend/.next`
  - `frontend/coverage`
- Those roots must be absent before the run, must be removed by the exact-path cleanup in this spec, and must be absent afterward.
- The pre-run and post-cleanup outputs of `git status --porcelain=v1 --untracked-files=all` must be byte-for-byte identical. For a correctly initialized Q04 worktree both are exactly empty.
- `HEAD` and the Git tree must remain the pinned base. There is no Q04 candidate commit and no expected changed-file list; `git diff --name-only <base>` must be empty.

## Forbidden and Out of Scope

- Do not edit `frontend/package.json`, `frontend/package-lock.json`, npm configuration, Vitest configuration, the route, any test, any source file, any goal document, or any other repository path in the parcel worktree.
- Do not run `npm install`, `npm update`, `npm audit`, `npx`, an online `npm ci`, or any retry that omits `--offline`. Do not change registries, populate the npm cache, fetch a tarball, or fall back to a network registry. A failed offline install is evidence, not permission to repair the environment.
- Do not import, restore, copy, inspect through execution, or run `frontend/src/__tests__/app/api/research/suggest.outbound-boundary.reproduction.test.ts`. That diagnostic reproduction belongs only to P07 after Q04 succeeds. Do not use diagnostic commit `c3e30a93e64be5a2662bb9040a52105d3dc909c9` as a source or ancestor for Q04.
- Run only `frontend/src/__tests__/app/api/research/suggest.route.test.ts`. Do not widen to the frontend suite, lint, build, coverage, another test file, backend tests, or the goal-level aggregate matrix.
- Preserve the existing test file byte-for-byte. Its provider-call case uses a Vitest `fetch` stub; its missing-key case exits before a provider call. Do not replace those network fakes, inject a real credential, contact OpenAI or any other provider, or use production/protected data.
- Do not use `git clean`, wildcards, a workspace-wide delete, a parent-directory delete, or an unresolved path for cleanup. Never delete an ignored directory that existed before Q04.
- Do not stage or commit. Do not push, open a PR, merge, deploy, enable a provider, publish, release, or mutate/clean a diagnostic branch.
- Do not create a `ShapingResult`. BioStack does not contain the Foreman emitter/linter required to emit one.

## Verified Pinned-Base Test Contract

The sole test target is the existing base file:

`frontend/src/__tests__/app/api/research/suggest.route.test.ts`

It has exactly two cases on the pinned base:

1. `requires the OpenAI API key` removes `OPENAI_API_KEY` and expects the route to return `503` without a provider call.
2. `calls OpenAI and normalizes unsafe promotion suggestions when hard blockers remain` sets synthetic environment values and replaces global `fetch` with a local `vi.fn()` response before invoking the route.

The execution receipt, not source counting, is authoritative. Expected pinned-base result: one test file, two tests passed, zero failed, zero skipped. Because Q04 adds no test or source, the expected base-to-final count delta is `2 -> 2` (`+0`). Any other discovery count is a stop condition.

## Exact Command Procedure

This is the third and final bounded attempt. Run the following as one parsed PowerShell script from the Q04 worktree root only after a new Step 0 confirmation. Do not submit its compound statements one line at a time. The new external transcript path must not already exist; do not overwrite either rejected-run transcript. `Start-Transcript` is forbidden. Every native command sends stdout and stderr through `Tee-Object`, and `$LASTEXITCODE` is copied immediately after that exact native pipeline. Explicit receipt lines use the same append-only `Tee-Object` path. Do not remove or weaken any preflight, `--offline`, worker, capture, cleanup, equality, count, or warning check.

```powershell
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

$q04Base = '339f259b1a467034db4f57cf9d774c292f11b53a'
$q04Tree = '0f6d0b609ce255aad5cca81698eb3ad0917cfda6'
$q04ExpectedRoot = [IO.Path]::GetFullPath('C:\Users\clint\.codex\worktrees\biostack-remediation-q04\BioStack')
$q04TranscriptPath = [IO.Path]::GetFullPath('C:\Users\clint\.codex\evidence\biostack-algorithm-remediation\q04-final-attempt-2026-09-02.transcript.txt')
$q04ExpectedEvidenceRoot = [IO.Path]::GetFullPath('C:\Users\clint\.codex\evidence\biostack-algorithm-remediation')
$q04ActualEvidenceRoot = [IO.Path]::GetFullPath((Split-Path -Parent $q04TranscriptPath))
if (-not $q04ActualEvidenceRoot.Equals($q04ExpectedEvidenceRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Q04 evidence path escaped its exact root: $q04TranscriptPath"
}
if (-not (Test-Path -LiteralPath $q04ExpectedEvidenceRoot -PathType Container)) {
    throw "Q04 evidence root is absent: $q04ExpectedEvidenceRoot"
}
if (Test-Path -LiteralPath $q04TranscriptPath) {
    throw "Q04 final-attempt transcript already exists; do not overwrite or rerun: $q04TranscriptPath"
}
[IO.File]::WriteAllText($q04TranscriptPath, '', [Text.UTF8Encoding]::new($false))

function Write-Q04Line {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Text)
    $Text | Tee-Object -FilePath $q04TranscriptPath -Append
}

Write-Q04Line "Q04_ATTEMPT=3-final"
Write-Q04Line "Q04_TRANSCRIPT_PATH=$q04TranscriptPath"
Write-Q04Line 'Q04_CAPTURE_MODE=explicit native stdout+stderr via Tee-Object; no Start-Transcript'

$q04ActualRoot = [IO.Path]::GetFullPath((Get-Location).Path)
if (-not $q04ActualRoot.Equals($q04ExpectedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Q04 wrong worktree: $q04ActualRoot"
}

$q04BranchLines = @(& git branch --show-current 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append)
$q04BranchExit = $LASTEXITCODE
$q04Branch = ($q04BranchLines | ForEach-Object { $_.ToString() }) -join "`n"
if ($q04BranchExit -ne 0 -or $q04Branch -cne 'codex/biostack-remediation-q04') {
    throw "Q04 wrong branch: $q04Branch"
}
$q04HeadLines = @(& git rev-parse HEAD 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append)
$q04HeadExit = $LASTEXITCODE
$q04Head = ($q04HeadLines | ForEach-Object { $_.ToString() }) -join "`n"
if ($q04HeadExit -ne 0 -or $q04Head -cne $q04Base) {
    throw "Q04 wrong HEAD: $q04Head"
}
$q04HeadTreeLines = @(& git rev-parse 'HEAD^{tree}' 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append)
$q04HeadTreeExit = $LASTEXITCODE
$q04HeadTree = ($q04HeadTreeLines | ForEach-Object { $_.ToString() }) -join "`n"
if ($q04HeadTreeExit -ne 0 -or $q04HeadTree -cne $q04Tree) {
    throw "Q04 wrong tree: $q04HeadTree"
}
Write-Q04Line "Q04_START_BRANCH=$q04Branch"
Write-Q04Line "Q04_START_HEAD=$q04Head"
Write-Q04Line "Q04_START_TREE=$q04HeadTree"

$q04PreStatus = @(& git status --porcelain=v1 --untracked-files=all 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append)
$q04PreStatusExit = $LASTEXITCODE
if ($q04PreStatusExit -ne 0 -or $q04PreStatus.Count -ne 0) {
    throw 'Q04 requires an exactly clean pre-run porcelain receipt.'
}

$q04BlobChecks = @(
    @{ Path = 'frontend/package.json'; Expected = 'c961913b9c7fd598f4a6c5a6be811edbe35bfe07' },
    @{ Path = 'frontend/package-lock.json'; Expected = '6772a07e5d8cfb575788f78afda23ec388bc5720' },
    @{ Path = 'frontend/src/__tests__/app/api/research/suggest.route.test.ts'; Expected = 'b1619779e26cdf34d7d47d3e51858c54b43a31fc' }
)
foreach ($q04BlobCheck in $q04BlobChecks) {
    $q04ActualBlobLines = @(& git hash-object -- $q04BlobCheck.Path 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append)
    $q04ActualBlobExit = $LASTEXITCODE
    $q04ActualBlob = ($q04ActualBlobLines | ForEach-Object { $_.ToString() }) -join "`n"
    if ($q04ActualBlobExit -ne 0 -or $q04ActualBlob -cne $q04BlobCheck.Expected) {
        throw "Q04 blob mismatch: $($q04BlobCheck.Path) => $q04ActualBlob"
    }
    Write-Q04Line "Q04_PINNED_BLOB=$($q04BlobCheck.Path)|$q04ActualBlob"
}

$q04OutboundReproduction = Join-Path $q04ExpectedRoot 'frontend\src\__tests__\app\api\research\suggest.outbound-boundary.reproduction.test.ts'
if (Test-Path -LiteralPath $q04OutboundReproduction) {
    throw 'Q04 outbound reproduction is present; do not import or run it.'
}
Write-Q04Line 'Q04_OUTBOUND_REPRODUCTION_PRESENT=false'

$q04GeneratedPaths = @(
    (Join-Path $q04ExpectedRoot 'frontend\node_modules'),
    (Join-Path $q04ExpectedRoot 'frontend\.next'),
    (Join-Path $q04ExpectedRoot 'frontend\coverage')
)
foreach ($q04GeneratedPath in $q04GeneratedPaths) {
    if (Test-Path -LiteralPath $q04GeneratedPath) {
        throw "Q04 will not delete pre-existing ignored state: $q04GeneratedPath"
    }
}
Write-Q04Line "Q04_GENERATED_PATHS_PREABSENT=$($q04GeneratedPaths -join ' | ')"

$env:npm_config_offline = 'true'
$env:npm_config_audit = 'false'
$env:npm_config_fund = 'false'
$env:npm_config_update_notifier = 'false'
$q04CiExit = $null
$q04TestExit = $null
$q04CleanupErrors = @()
$q04RemovedPaths = @()

Push-Location (Join-Path $q04ExpectedRoot 'frontend')
try {
    & node --version 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append
    $q04NodeVersionExit = $LASTEXITCODE
    & npm --version 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append
    $q04NpmVersionExit = $LASTEXITCODE
    if ($q04NodeVersionExit -ne 0 -or $q04NpmVersionExit -ne 0) {
        throw 'Q04 Node/npm version receipt failed.'
    }

    Write-Q04Line 'Q04_INSTALL_COMMAND=npm ci --offline'
    & npm ci --offline 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append
    $q04CiExit = $LASTEXITCODE

    if ($q04CiExit -eq 0) {
        $q04VitestArgs = @(
            '.\node_modules\vitest\vitest.mjs',
            'run',
            'src/__tests__/app/api/research/suggest.route.test.ts',
            '--pool=threads',
            '--maxWorkers=2',
            '--no-file-parallelism'
        )
        Write-Q04Line 'Q04_VITEST_COMMAND=node .\node_modules\vitest\vitest.mjs run src/__tests__/app/api/research/suggest.route.test.ts --pool=threads --maxWorkers=2 --no-file-parallelism'
        Write-Q04Line 'Q04_VITEST_EXECUTABLE=node'
        Write-Q04Line "Q04_VITEST_ARGV=$($q04VitestArgs | ConvertTo-Json -Compress)"
        & node @q04VitestArgs 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append
        $q04TestExit = $LASTEXITCODE
    }
}
finally {
    Pop-Location
    foreach ($q04GeneratedPath in $q04GeneratedPaths) {
        if (-not (Test-Path -LiteralPath $q04GeneratedPath)) { continue }
        try {
            $q04ExpectedGeneratedPath = [IO.Path]::GetFullPath($q04GeneratedPath)
            $q04GeneratedItem = Get-Item -LiteralPath $q04GeneratedPath -Force
            $q04ResolvedGeneratedPath = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $q04GeneratedPath).Path)
            if ($q04GeneratedItem.LinkType) {
                throw "refusing to recursively remove a link: $q04GeneratedPath"
            }
            if (-not $q04ResolvedGeneratedPath.Equals($q04ExpectedGeneratedPath, [StringComparison]::OrdinalIgnoreCase)) {
                throw "resolved cleanup target mismatch: $q04ResolvedGeneratedPath"
            }
            if (-not $q04ResolvedGeneratedPath.StartsWith((Join-Path $q04ExpectedRoot 'frontend') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
                throw "cleanup target escaped frontend: $q04ResolvedGeneratedPath"
            }
            Remove-Item -LiteralPath $q04ResolvedGeneratedPath -Recurse -Force
            $q04RemovedPaths += $q04ResolvedGeneratedPath
        }
        catch {
            $q04CleanupErrors += $_.Exception.Message
        }
    }
}

$q04TranscriptSoFar = Get-Content -LiteralPath $q04TranscriptPath -Raw
$q04UnknownConfigToken = 'Unknown' + ' cli config'
$q04UnknownOptionToken = 'Unknown' + ' option'
$q04UnknownConfigWarning = $q04TranscriptSoFar.Contains($q04UnknownConfigToken, [StringComparison]::OrdinalIgnoreCase)
$q04UnknownOptionWarning = $q04TranscriptSoFar.Contains($q04UnknownOptionToken, [StringComparison]::OrdinalIgnoreCase)
$q04OneFilePattern = 'Test' + ' Files\s+1 passed \(1\)'
$q04TwoTestsPattern = 'Tests' + '\s+2 passed \(2\)'
$q04OneFileReceipt = [regex]::IsMatch($q04TranscriptSoFar, $q04OneFilePattern)
$q04TwoTestsReceipt = [regex]::IsMatch($q04TranscriptSoFar, $q04TwoTestsPattern)
$q04CountReceipt = $q04OneFileReceipt -and $q04TwoTestsReceipt

$q04PostStatus = @(& git status --porcelain=v1 --untracked-files=all 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append)
$q04PostStatusExit = $LASTEXITCODE
$q04PreStatusText = $q04PreStatus -join "`n"
$q04PostStatusText = $q04PostStatus -join "`n"
$q04FinalHeadLines = @(& git rev-parse HEAD 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append)
$q04FinalHeadExit = $LASTEXITCODE
$q04FinalHead = ($q04FinalHeadLines | ForEach-Object { $_.ToString() }) -join "`n"
$q04FinalTreeLines = @(& git rev-parse 'HEAD^{tree}' 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append)
$q04FinalTreeExit = $LASTEXITCODE
$q04FinalTree = ($q04FinalTreeLines | ForEach-Object { $_.ToString() }) -join "`n"
& git merge-base --is-ancestor $q04Base HEAD 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append
$q04BaseAncestryExit = $LASTEXITCODE
$q04DiffNames = @(& git diff --name-only $q04Base 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append)
$q04DiffNamesExit = $LASTEXITCODE
& git diff --check $q04Base 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append
$q04DiffCheckExit = $LASTEXITCODE
$q04FinalTestBlobLines = @(& git hash-object -- frontend/src/__tests__/app/api/research/suggest.route.test.ts 2>&1 | Tee-Object -FilePath $q04TranscriptPath -Append)
$q04FinalTestBlobExit = $LASTEXITCODE
$q04FinalTestBlob = ($q04FinalTestBlobLines | ForEach-Object { $_.ToString() }) -join "`n"
$q04Residue = @($q04GeneratedPaths | Where-Object { Test-Path -LiteralPath $_ })

Write-Q04Line "Q04_PRE_STATUS=$($(if ($q04PreStatus.Count -eq 0) { '<empty>' } else { $q04PreStatusText }))"
Write-Q04Line "Q04_PRE_STATUS_EXIT=$q04PreStatusExit"
Write-Q04Line "Q04_POST_STATUS=$($(if ($q04PostStatus.Count -eq 0) { '<empty>' } else { $q04PostStatusText }))"
Write-Q04Line "Q04_POST_STATUS_EXIT=$q04PostStatusExit"
Write-Q04Line "Q04_STATUS_EQUAL=$($q04PreStatusText -ceq $q04PostStatusText)"
Write-Q04Line "Q04_NODE_VERSION_EXIT=$q04NodeVersionExit"
Write-Q04Line "Q04_NPM_VERSION_EXIT=$q04NpmVersionExit"
Write-Q04Line "Q04_NPM_CI_EXIT=$q04CiExit"
Write-Q04Line "Q04_TEST_EXIT=$q04TestExit"
Write-Q04Line "Q04_UNKNOWN_CONFIG_WARNING=$q04UnknownConfigWarning"
Write-Q04Line "Q04_UNKNOWN_OPTION_WARNING=$q04UnknownOptionWarning"
Write-Q04Line "Q04_ONE_FILE_RECEIPT=$q04OneFileReceipt"
Write-Q04Line "Q04_TWO_TESTS_RECEIPT=$q04TwoTestsReceipt"
$q04TestCountText = 'unverified'
if ($q04CountReceipt) {
    $q04TestCountText = 'discovered=2;executed=2;passed=2;failed=0;skipped=0;delta=2->2(+0)'
}
Write-Q04Line "Q04_TEST_COUNTS=$q04TestCountText"
Write-Q04Line "Q04_REMOVED_PATHS=$($q04RemovedPaths -join ' | ')"
Write-Q04Line "Q04_CLEANUP_ERRORS=$($q04CleanupErrors -join ' | ')"
Write-Q04Line "Q04_RESIDUE=$($q04Residue -join ' | ')"
Write-Q04Line "Q04_FINAL_HEAD=$q04FinalHead"
Write-Q04Line "Q04_FINAL_TREE=$q04FinalTree"
Write-Q04Line "Q04_BASE_ANCESTRY_EXIT=$q04BaseAncestryExit"
Write-Q04Line "Q04_FINAL_TEST_BLOB=$q04FinalTestBlob"
Write-Q04Line "Q04_DIFF_NAMES=$($q04DiffNames -join ' | ')"
Write-Q04Line "Q04_DIFF_NAMES_EXIT=$q04DiffNamesExit"
Write-Q04Line "Q04_DIFF_CHECK_EXIT=$q04DiffCheckExit"

$q04ScopeFailure = (
    $q04CleanupErrors.Count -ne 0 -or
    $q04Residue.Count -ne 0 -or
    $q04PostStatusExit -ne 0 -or
    $q04PreStatusText -cne $q04PostStatusText -or
    $q04FinalHeadExit -ne 0 -or $q04FinalHead -cne $q04Base -or
    $q04FinalTreeExit -ne 0 -or $q04FinalTree -cne $q04Tree -or
    $q04BaseAncestryExit -ne 0 -or
    $q04DiffNamesExit -ne 0 -or $q04DiffNames.Count -ne 0 -or
    $q04DiffCheckExit -ne 0 -or
    $q04FinalTestBlobExit -ne 0 -or $q04FinalTestBlob -cne 'b1619779e26cdf34d7d47d3e51858c54b43a31fc'
)

$q04FinalCaptureText = ''
$q04CaptureReadFailed = $false
try {
    $q04FinalCaptureText = Get-Content -LiteralPath $q04TranscriptPath -Raw
}
catch {
    $q04CaptureReadFailed = $true
}
$q04CaptureFailure = (
    $q04CaptureReadFailed -or
    -not (Test-Path -LiteralPath $q04TranscriptPath -PathType Leaf) -or
    [string]::IsNullOrWhiteSpace($q04FinalCaptureText) -or
    -not $q04FinalCaptureText.Contains('Q04_INSTALL_COMMAND=npm ci --offline', [StringComparison]::Ordinal) -or
    ($q04CiExit -eq 0 -and (
        -not $q04FinalCaptureText.Contains('Q04_VITEST_EXECUTABLE=node', [StringComparison]::Ordinal) -or
        -not $q04FinalCaptureText.Contains('Q04_VITEST_ARGV=', [StringComparison]::Ordinal) -or
        -not $q04FinalCaptureText.Contains('Test Files', [StringComparison]::Ordinal) -or
        -not $q04FinalCaptureText.Contains('Tests', [StringComparison]::Ordinal)
    ))
)

$q04Outcome = 'READY'
$q04OutcomeExit = 0
if ($q04CiExit -eq 0 -and (
    $q04TestExit -ne 0 -or
    $q04UnknownConfigWarning -or
    $q04UnknownOptionWarning -or
    -not $q04CountReceipt
)) {
    $q04Outcome = 'BASELINE_TEST_FAILED'
    $q04OutcomeExit = 21
}
if ($q04CiExit -ne 0) {
    $q04Outcome = 'ENVIRONMENT_BLOCKED'
    $q04OutcomeExit = 20
}
if ($q04CaptureFailure) {
    $q04Outcome = 'EVIDENCE_CAPTURE_BLOCKED'
    $q04OutcomeExit = 91
}
if ($q04ScopeFailure) {
    $q04Outcome = 'SCOPE_CLEANUP_BLOCKED'
    $q04OutcomeExit = 90
}

Write-Q04Line "Q04_CAPTURE_MARKERS_PRESENT=$(-not $q04CaptureFailure)"
Write-Q04Line "Q04_OUTCOME=$q04Outcome"
Write-Q04Line 'Q04_AUTHORITY_CLOSE=no repository changes; no commit; local evidence only; not pushed; no PR; not merged; not deployed or released; Gate 3 ungranted; diagnostic branches untouched'

try {
    $q04TranscriptInfo = Get-Item -LiteralPath $q04TranscriptPath
    $q04TranscriptHash = Get-FileHash -LiteralPath $q04TranscriptPath -Algorithm SHA256
    if ($q04TranscriptInfo.Length -le 0 -or [string]::IsNullOrWhiteSpace($q04TranscriptHash.Hash)) {
        throw 'Q04 final transcript size/hash receipt is empty.'
    }
    Write-Output "Q04_TRANSCRIPT_BYTES=$($q04TranscriptInfo.Length)"
    Write-Output "Q04_TRANSCRIPT_SHA256=$($q04TranscriptHash.Hash)"
}
catch {
    Write-Error "Q04_OUTCOME=EVIDENCE_CAPTURE_BLOCKED; transcript size/hash failed: $($_.Exception.Message)"
    exit 91
}

exit $q04OutcomeExit
```

The exact test invocation is the F1 repair and must remain:

```powershell
node .\node_modules\vitest\vitest.mjs run src/__tests__/app/api/research/suggest.route.test.ts --pool=threads --maxWorkers=2 --no-file-parallelism
```

It selects only the existing main-based file and bypasses npm's CLI configuration parser. The emitted `Q04_VITEST_ARGV` JSON is the authoritative argument-vector receipt. Any npm-style unknown-config warning, Vitest unknown-option warning, omitted argument, or different echoed vector is `BASELINE_TEST_FAILED`, even if tests otherwise pass. Do not substitute `npm test`, `npx vitest`, a wildcard, a directory, the diagnostic reproduction, or different worker settings.

The final transcript path is exactly `C:\Users\clint\.codex\evidence\biostack-algorithm-remediation\q04-final-attempt-2026-09-02.transcript.txt`. It is distinct from both rejected-run evidence paths and is write-once for this parcel. The post-capture byte count and SHA-256 are external receipts over the final transcript bytes; they are not appended to the hashed file. The coordinator must independently recompute both before review.

## Outcome Classification and Acceptance

### `READY`

All of the following are required:

- `npm ci --offline` exits `0` without a retry or registry fallback;
- the exact direct-node single-file Vitest command emits the required argument vector without unknown-config/unknown-option warnings, exits `0`, and reports one file and exactly `2/2` tests passed, `0` failed, and `0` skipped;
- the existing test blob is unchanged, so its local fetch fake and missing-key no-call behavior remain intact;
- pre/post porcelain receipts are identical and empty;
- generated roots are absent after exact-path cleanup;
- `HEAD`, tree, diff-name, and `git diff --check` receipts prove no repository change.

`READY` establishes dependency/test-environment readiness only. It does not reproduce or fix R-OUT-02, validate the P07 outbound regression, approve a package change, or clear P07 review/security/Gate 3 requirements.

### `ENVIRONMENT_BLOCKED`

If the exact offline `npm ci` command exits nonzero because a cached package, dependency artifact, or otherwise required offline dependency state is unavailable, classify Q04 as `ENVIRONMENT_BLOCKED`, never as a BioStack product defect. Preserve the exact command, exit code, and sanitized npm error. Do not retry online, switch registries, run `npm install`, alter the lockfile, or run the Vitest file. Cleanup and identical-status proof remain mandatory. Any registry access would require separately recorded registry-only human authorization and a new bounded procedure; none is granted here.

### Other stop outcomes

- `BASELINE_TEST_FAILED`: offline install succeeded, but the exact main-based two-test file failed or did not report exactly the expected count. Preserve the output and stop. Do not edit or import the P07 reproduction.
- `SCOPE_CLEANUP_BLOCKED`: cleanup failed, a generated root remains, pre/post status differs, a blob changed, a diff exists, or `HEAD`/tree moved. This overrides any dependency/test result. Stop for coordinator recovery; do not broaden deletion or commit a cleanup.
- `EVIDENCE_CAPTURE_BLOCKED`: the new transcript is absent/empty, lacks required command/native-summary markers after a successful install, cannot be hashed, or lacks a nonzero byte-size receipt. This overrides any otherwise green runtime observation. Because this is the third and final bounded attempt, this outcome terminally blocks Q04 and P07; do not create another transcript, rerun npm/Vitest, change capture mechanisms, or request an automatic retry. Any future attempt requires a new human-authorized procedure outside this parcel.
- Base, branch, worktree, starting-status, blob, or pre-existing-ignored-state preflight failure: stop before `npm ci` and report the exact mismatch. It is not a Q04 readiness result.

## Evidence Package

The investigator hands the coordinator the complete terminal transcript and one concise evidence record containing the items below. The transcript must be created outside every Git worktree at exactly `C:\Users\clint\.codex\evidence\biostack-algorithm-remediation\q04-final-attempt-2026-09-02.transcript.txt`, must include the complete native npm/Vitest stdout and stderr plus explicit receipt lines, and must remain available to the fresh reviewer. The investigator must also report the post-close transcript byte size and SHA-256; the coordinator independently recomputes both before accepting the evidence:

- parcel `Q04`, exact branch/worktree, starting and ending commit/tree, and base ancestry;
- `node --version` and `npm --version` output and exits;
- the package, lockfile, and main-based test blob receipts, plus lockfile version `3`;
- exact pre-run and post-cleanup `git status --porcelain=v1 --untracked-files=all` outputs and exit codes, rendered as `<empty>` when empty, plus an explicit byte-for-byte equality result;
- the exact `npm ci --offline` command, exit code, and sanitized output;
- if install succeeded, the exact direct-node single-file Vitest command, emitted JSON argument vector, absence of unknown-config/unknown-option warnings, exit code, final summary, discovered/executed/passed/failed/skipped counts, and expected delta `2 -> 2 (+0)`;
- explicit confirmation that only the main-based test ran, the outbound reproduction was absent/not imported/not run, the existing local fetch fake was preserved, no credential was supplied, and no registry/provider/cloud/production/protected-data access occurred;
- exact cleanup targets considered, confirmation they were absent before execution, exact targets removed, any cleanup error, and confirmation all are absent afterward;
- final `git diff --check`, empty `git diff --name-only <base>`, unchanged test blob, and empty final status;
- final-attempt marker, exact new transcript path, explicit `Tee-Object` capture mode, post-close byte size, and SHA-256;
- one outcome exactly: `READY`, `ENVIRONMENT_BLOCKED`, `BASELINE_TEST_FAILED`, `SCOPE_CLEANUP_BLOCKED`, or `EVIDENCE_CAPTURE_BLOCKED`, with the controlling evidence;
- residual risks, unexpected observations, and requested decision, or an explicit `none`.

Do not write the evidence record into the repository. The coordinator preserves it in the goal/session handoff and later Stage-F record.

## Review and Custody

After the Q04 evidence record exists, the coordinator obtains a fresh read-only adversarial review of the exact transcript and handoff. The reviewer checks command identity, offline/no-fallback controls, selected test identity and count, preservation of the network fake, outcome classification, cleanup target safety, and exact pre/post Git equality. The reviewer does not edit, stage, commit, rerun with network, or remediate a failure. Any finding is dispositioned before Q04 is accepted.

Q04 custody is outcome-specific under plan-review finding F10:

- `READY` allows the coordinator to proceed to P07 shaping/dispatch only after all independent P07 gates are satisfied.
- `ENVIRONMENT_BLOCKED`, `BASELINE_TEST_FAILED`, `SCOPE_CLEANUP_BLOCKED`, and `EVIDENCE_CAPTURE_BLOCKED` remain evidence-only and do not authorize a repository change or P07 dispatch. `EVIDENCE_CAPTURE_BLOCKED` is terminal for this procedure; no fourth attempt is authorized.
- The Q04 branch and outcome are recorded at Stage F. The branch is not deleted without explicit cleanup authority.

## Step 0 — Restate and Stop

Before running any command that can create an ignored artifact, the investigator must send the coordinator one restatement containing all of the following, then stop for explicit confirmation:

1. goal, Q04 identifier, Wave Q, command-only/no-repository-change authority, and Q04's prerequisite relationship to P07;
2. branch `codex/biostack-remediation-q04`, worktree `C:\Users\clint\.codex\worktrees\biostack-remediation-q04\BioStack`, base `339f259b1a467034db4f57cf9d774c292f11b53a`, tree `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`, and the three pinned blobs;
3. AF-Q04's empty repository change set, exact permitted ignored roots, exact pre/post porcelain equality requirement, and the no-stage/no-commit rule;
4. the exact offline install and deterministic single-file Vitest commands, expected one-file/two-test receipt, expected count delta `2 -> 2 (+0)`, and immediate preservation of each native `$LASTEXITCODE` after its `Tee-Object` pipeline;
5. the prohibition on importing or running `suggest.outbound-boundary.reproduction.test.ts`, the fact that it belongs to P07 after Q04, and preservation of the existing network-faked tests;
6. this is attempt 3 and final; the new write-once transcript path, no `Start-Transcript`, complete native stdout/stderr capture, byte-size/SHA-256 receipts, and the terminal no-rerun effect of `EVIDENCE_CAPTURE_BLOCKED`;
7. outcome rules, especially that an offline cache/dependency failure is `ENVIRONMENT_BLOCKED` with no registry fallback;
8. no network/provider/cloud/production/protected-data/secret authority, exact-path cleanup rules, fresh read-only review, Gate 3 ungranted, and no push/PR/merge/deploy/publish/release/diagnostic-branch mutation.

Silence is not confirmation. Any discrepancy blocks execution.

## Handoff Contract

The investigator sends one concise record with this exact field order:

1. `Parcel / outcome`: `Q04` and one authorized outcome token.
2. `Branch / worktree / base`: exact values, starting and ending `HEAD`/tree, and ancestry result.
3. `Pinned identities`: all three blob hashes and lockfile version.
4. `Environment`: Node/npm versions; no secrets or cache contents.
5. `Install receipt`: exact offline command, exit code, sanitized result, and explicit no-registry-fallback statement.
6. `Test receipt`: exact command or `not run because ENVIRONMENT_BLOCKED`; if run, exact argv, warning scans, file/test counts, and exit code.
7. `Capture receipt`: attempt `3-final`, exact new transcript path, `Tee-Object` native stdout/stderr method, capture-marker result, final byte size, and SHA-256.
8. `Network boundary`: main-based test only, local fetch fake preserved, outbound reproduction absent/not imported/not run, and no external access.
9. `Cleanup / Git equality`: exact cleanup targets, pre/post porcelain receipts, equality result, residual generated paths, diff check, empty changed-path list, unchanged test blob.
10. `Findings / residual risk / decision requested`: exact items or `none`.
11. `Authority close`: `no repository changes; no commit; local evidence only; not pushed; no PR; not merged; not deployed or released; Gate 3 ungranted; diagnostic branches untouched`.

The coordinator verifies the handoff against Git and the transcript. A handoff claim is not acceptance by itself.

## Stop Rules

Stop without widening scope if:

- Step 0 is not explicitly confirmed;
- `origin/main` was reported materially different from the pinned base, or the exact branch/worktree/commit/tree/blob/status preflight fails;
- any permitted generated root already exists before the run;
- `npm ci --offline` fails, a cache/dependency is unavailable, or any command attempts registry/network fallback;
- the selected test is not exactly the main-based `suggest.route.test.ts`, the outbound reproduction is present/imported/selected, or the expected two-test count changes;
- a test needs a real external provider, credential, cloud/production resource, protected data, or non-faked outbound request;
- any repository path changes, a cleanup target resolves outside its exact expected frontend path, cleanup encounters a link, cleanup fails, or pre/post porcelain differs;
- a package, lockfile, config, test, source, receipt file, or nearby path would need modification;
- the new final-attempt transcript already exists, its explicit native capture is incomplete, or its post-close byte size/SHA-256 cannot be produced; this terminally blocks Q04 and no further attempt is authorized;
- the same tripwire or false-closure condition fires twice;
- any staging, commit, push, PR, merge, deployment, provider enablement, publication, release, or diagnostic-branch mutation is proposed.

Gate 3 remains ungranted and human-only. Q04 cannot request or imply Gate 3 clearance, and even `READY` is only an environment-readiness disposition.

BioStack does not contain the Foreman emitter/linter required for a `ShapingResult`. This draft intentionally creates no `ShapingResult` JSON and makes no promotion from `draft` to `active`.
