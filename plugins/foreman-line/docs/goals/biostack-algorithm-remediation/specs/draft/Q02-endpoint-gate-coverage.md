# Inquiry Parcel Q02: Endpoint Gate Coverage

Status: **active v2 rework — rejected v1 evidence is preserved; dispatch remains contingent on coordinator re-lint and a fresh v2 Step 0 confirmation**

Coordinator lint: **PASSED 2026-09-02**. The eleven exact endpoint identities/patterns, project and production blobs, 21-case adjacent count, AF-Q02 absence, and runtime metadata approach were independently rechecked. The parcel is intentionally bounded and may not claim endpoint-wide completeness. Any executable inventory outcome must preserve its single test in one local evidence-only commit for exact review.

V1 review disposition: **REJECTED 2026-09-02**. Candidate `dea721288521a5789350e2398d0d7292b01ad10f` is preserved on `codex/biostack-remediation-q02` with its original transcript. It is not an accepted evidence result and must not be amended, reset, rebased, cherry-picked, or reviewed as the v2 candidate. The rejection identified two evidence-integrity defects: the test could emit a pending inconclusive outcome before teardown later emitted `SCOPE_OR_CLEANUP_BLOCKED`, and the transcript did not preserve the full native output needed to verify rows, the sole outcome, counts, and Git receipts.

V2 shaping disposition: start once from the pinned base on a new branch/worktree and create a new write-once transcript. V2 may copy and rework only AF-Q02 from the rejected commit. Any further outcome-cardinality, teardown-ordering, transcript-completeness, count, scope, or taxonomy defect terminally blocks Q02.

V2 coordinator re-lint: **PASSED 2026-09-02**. The embedded native-output helper parses with zero PowerShell errors; `git diff --check` is clean; the new branch, worktree, and transcript are absent; and the v1 branch/commit/transcript custody is preserved. Fresh v2 Step 0 remains mandatory before any v2 write.

## Identity

- Goal: `biostack-algorithm-remediation`
- Project: `BioStack` / `BioStack.Api`
- Parcel: `Q02-endpoint-gate-coverage`
- Wave: Wave Q — inconclusive-claim adjudication
- Routing: fresh frontier architecture-evidence investigator
- Risk: high safety-boundary classification risk; test/evidence authority only
- Branch: `codex/biostack-remediation-q02-v2`
- Isolated worktree: `C:\Users\clint\.codex\worktrees\biostack-remediation-q02-v2\BioStack`
- Required starting commit: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Required starting tree: `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`

## Goal and Deliberate Limitation

Create a deterministic runtime inventory for the bounded endpoint catalog named by the audit, recording route identity, HTTP method, endpoint name, tags, inherited authorization metadata, handler metadata availability, and any direct `IUserFacingIntelligenceGate` handler parameter. Do not scan source text, comments, IL strings, filenames, response prose, or route-name keywords to infer safety semantics.

The pinned application has no first-class runtime metadata marker that declares an endpoint “recommendation-shaped.” Therefore this parcel does not claim endpoint-wide completeness and does not classify absence of a direct gate parameter as a bypass. Its expected durable disposition is `INCONCLUSIVE_NO_COMPLETE_RECOMMENDATION_SURFACE_MARKER` after a clean bounded inventory. A concrete new defect may be declared only by a separately authorized local runtime probe that proves a named route is reachable, emits the relevant user-facing response, and observes zero calls to an injected gate spy. Q02 as shaped here does not authorize that probe or a production fix.

## Authority and Preconditions

1. D4 authorizes Q02 to add test/evidence only. Any reproduced bypass requires a new production owner, invariant, exact Allowed Files, security gate, and narrow Gate 1 amendment before remediation.
2. The plan review prohibits brittle source-text proof and requires no inference from absence. Runtime endpoint metadata is evidence about registered routes and declared metadata only; indirect gates, service-internal gates, and response semantics are not disproved by a missing handler parameter.
3. Gate 2 is contingent on coordinator lint, exact isolated base/tree/status, exact Allowed Files, and confirmed Step 0.
4. D10 and SG-SCOPE require a local `WebApplicationFactory`, synthetic configuration, a temporary local SQLite file, and no external network/provider/cloud/production/protected-data/secret access.
5. The coordinator owns `origin/main` comparison and any ignored .NET restore-metadata seed. The investigator must not fetch, pull, restore, or contact a registry. Missing assets are `ENVIRONMENT_BLOCKED`.
6. Pending Gate 1 Amendment 01 does not alter Q02 or authorize endpoint remediation.

## Production Ownership

Q02 owns no production file. The bounded catalog spans endpoint registrations in `BioStack.Api.Endpoints`; these modules are observed owners only. No person/team ownership is inferred because the pinned repository has no `CODEOWNERS` file.

## Exact Allowed Files and Effects — AF-Q02

Exactly one repository path may be created or modified:

- `backend/tests/BioStack.Api.Tests/Architecture/UserFacingGateCoverageInvestigationTests.cs`

Permitted non-repository effects are limited to one uniquely named synthetic SQLite file under the OS temporary directory, ordinary ignored .NET build artifacts, and a sanitized transcript outside every Git worktree at:

- `C:\Users\clint\.codex\evidence\biostack-algorithm-remediation\q02-endpoint-gate-inventory-v2-2026-09-02.transcript.txt`

The v2 transcript path must not exist at Step 0. The verification procedure creates it once, appends only during that single v2 execution, and seals it by recording its final byte length and SHA-256 after all evidence is present. It must never truncate, overwrite, reuse, or append to the preserved v1 transcript. A partial v2 transcript produced by a stop is retained and may not be reused; the stop is terminal.

The test must dispose its factory/client resources, call `SqliteConnection.ClearAllPools()`, and delete only the exact temporary database file it created. No project, production, configuration, fixture, package/lock, snapshot, goal, handoff, or other test file may change. If the test cannot compile or access runtime endpoint metadata in this file alone, stop rather than adding a seam.

After all required receipts and exact temporary-file cleanup, an executable inventory outcome must create exactly one local test-only commit containing only AF-Q02. Environment-, scope-, cleanup-, or inventory-drift stops create no commit. The evidence commit is never pushed or merged and is not a remediation candidate.

## Rejected V1 Custody and Authorized V2 Rework

The rejected evidence-only commit is `dea721288521a5789350e2398d0d7292b01ad10f`, with sole parent `339f259b1a467034db4f57cf9d774c292f11b53a`, tree `61fbbe865249bc82a5ef748e74227350e9fb35d0`, and sole path AF-Q02. Its preserved transcript is `C:\Users\clint\.codex\evidence\biostack-algorithm-remediation\q02-endpoint-gate-inventory-2026-09-02.transcript.txt`; the observed final file is `21064` bytes with SHA-256 `767AA4D74D3F7E71CB8CED33BF411B91B4E193EC6BE0DE4B90D487B8C1EC9375`. These identities are custody receipts, not acceptance evidence.

V2 starts clean at the pinned base, not at the rejected commit. After the baseline absence check passes, the investigator may read that one path from `dea721288521a5789350e2398d0d7292b01ad10f` and materialize/rework AF-Q02 through `apply_patch` only. No commit, tree, transcript content, or other path from v1 may be copied. The resulting test must retain the same eleven-entry catalog, runtime-only metadata method, exact test name, synthetic local configuration, `ClearAllPools` teardown, and no-HTTP/no-absence-inference posture.

The v2 code must compute a pending classification without emitting any `OUTCOME ` line inside the inventory `try`. It may emit sanitized `ROW`, `DRIFT`, `HANDLER_METADATA_UNAVAILABLE`, and recommendation-marker evidence while computing that pending token. Teardown then runs in this exact order: dispose the application/factory; call `SqliteConnection.ClearAllPools()`; delete and verify absence of the exact database path. Only after all teardown steps succeed may the test emit exactly one `OUTCOME <pending-token>` line. If any teardown step fails, it must discard/suppress the pending token, emit exactly one `OUTCOME SCOPE_OR_CLEANUP_BLOCKED` line, fail, and emit no other outcome token. An inventory/harness exception after successful teardown similarly emits exactly one `OUTCOME ENVIRONMENT_BLOCKED` line. Assertions run only after the single post-teardown token is emitted.

Do not add sleeps, retries, garbage collection, wildcard deletion, directory deletion, process termination, a second file, package/project changes, or a network-capable seam. Local `Microsoft.Data.Sqlite.Core` 10.0.0 already exposes `SqliteConnection.ClearAllPools()`. Any further cleanup, output-cardinality, evidence, taxonomy, aggregate, or scope failure is terminal: preserve the rejected v1 and partial v2 evidence, create no v2 commit, and stop Q02 without a third attempt.

## Pinned-Base Identities and Existing Receipts

- `backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj` has blob `536ccab0ecd50607bbc76c2ac55447128e87bbeb`; it already references the API project plus xUnit, Moq, and `Microsoft.AspNetCore.Mvc.Testing`.
- `Program.cs` has blob `26ad33636037a5d40b440ef7b59330335793746d` and maps the endpoint modules into one runtime graph.
- Endpoint blobs are: `AnalyzeEndpoints.cs` `3279f3385cf6221c4745689ba486bb2d7d4dd424`; `ProtocolEndpoints.cs` `30403e681f32e65418ba91fdb5b03e78f93dfa98`; `KnowledgeEndpoints.cs` `e2a4472c4de764e560f13fcca8e9628077355cf2`; `IntelligenceEndpoints.cs` `60442fb3eb0014ba9081d8ffe33d63309e97bd9a`; `StackReviewEndpoints.cs` `5987d524e8c80281054eae346ecf38c086b2f5d9`.
- The AF-Q02 test path is absent at the base, so its target count is exactly `0 -> 1` (`+1`).
- The read-only adjacent receipt is exactly 21 cases: `AuthorizationEnforcementMatrixIntegrationTests` has nine discovered cases (seven theory rows plus two facts), `IntelligenceSafetyGateIntegrationTests` has three facts, and `StackReviewEndpointsIntegrationTests` has nine facts. Their pinned blobs are respectively `0c7386494020bd8b83387008fc3c8505638be72b`, `df3bc9d52a99a56628033eb52a08e192d5fda5eb`, and `fdfd3f7ed68174cedaad13fe6093c1e229091085`.

## Bounded Audit Catalog

The test contains this exact, explicitly bounded catalog. It is not an endpoint-wide classifier.

| Endpoint name | Method | Runtime route pattern | Audit role |
|---|---|---|---|
| `AnalyzeProtocol` | POST | `/api/analyze/protocol` | alleged ungated candidate |
| `GetProtocolReview` | GET | `/api/v1/protocols/{id}/review` | alleged ungated candidate |
| `GetProtocolPatterns` | GET | `/api/v1/protocols/{id}/patterns` | alleged ungated candidate |
| `GetProtocolDrift` | GET | `/api/v1/protocols/{id}/drift` | alleged ungated candidate |
| `GetAllCompounds` | GET | `/api/v1/knowledge/compounds` | alleged ungated candidate |
| `GetCompound` | GET | `/api/v1/knowledge/compounds/{name}` | alleged ungated candidate |
| `CheckOverlap` | POST | `/api/v1/knowledge/overlap-check` | alleged ungated candidate |
| `CheckInteractions` | POST | `/api/v1/knowledge/interaction-check` | alleged ungated candidate |
| `GetCompoundRelationships` | GET | `/api/v1/intelligence/compounds/{compound}/relationships` | known direct-gate positive control |
| `GetCompoundCompatibility` | GET | `/api/v1/intelligence/compatibility` | known direct-gate positive control |
| `GenerateStackDeliberationEnvelope` | POST | `/api/v1/stack-review/envelope` | known direct-gate positive control |

Do not add `current-stack-intelligence`, `mission-control`, `sequence-expectation`, admin, billing, policy, health, frontend, or other surfaces by semantic guess. Their omission is a recorded coverage limitation, not evidence that they are safe, unsafe, recommendation-shaped, or irrelevant.

## Exact Test Design

Create one xUnit `[Fact]` named:

`Runtime_inventory_records_bounded_gate_and_auth_metadata_without_claiming_endpoint_wide_coverage`

The test must:

1. create a `WebApplicationFactory<Program>` in Development with a uniquely named temporary SQLite database, synthetic JWT settings, Collective live mode disabled, and `UseTestKeonRuntimeClient`; it must not issue an HTTP request;
2. resolve the runtime `EndpointDataSource` only after the application is started locally, enumerate `RouteEndpoint` instances, and extract structured metadata through framework types such as `IHttpMethodMetadata`, `IEndpointNameMetadata`, `ITagsMetadata`, `IAuthorizeData`, and any `MethodInfo` metadata already attached by the runtime;
3. normalize only framework representation details: methods upper-case ordinal, tags sorted ordinal, policy/auth records sorted ordinal, and a single leading slash on route patterns. Do not normalize away route parameters or infer from words in a route/name;
4. match the eleven entries by exact endpoint name, then require exactly one runtime endpoint for each expected name and exact method/pattern equality. Duplicates, missing names, or route/method drift are an inventory failure, not a gate defect;
5. record for each row: name, method, pattern, tags, whether any `IAuthorizeData` metadata exists, authorization policy names, whether handler `MethodInfo` metadata exists, handler declaring type/name if available, and whether that method directly declares an `IUserFacingIntelligenceGate` parameter;
6. require the three positive controls to expose authorization metadata and, when all eleven handler `MethodInfo` values are available, require the three positive controls to declare the direct gate parameter. This validates the inventory seam without treating a negative parameter result as proof;
7. record whether any runtime metadata type or value explicitly marks recommendation-shaped output. On the pinned base, no such first-class marker has been identified. Do not treat tags, endpoint names, route words, response types, or the audit catalog itself as that marker;
8. compute pending `INCONCLUSIVE_INVENTORY_DRIFT` when catalog identity/method/pattern or positive-control checks drift; otherwise compute pending `INCONCLUSIVE_NO_COMPLETE_RECOMMENDATION_SURFACE_MARKER` after a clean eleven-row inventory when usable handler metadata is available, or record affected rows and compute pending `INCONCLUSIVE_NO_HANDLER_METADATA_SEAM` when it is unavailable. Do not emit any pending token yet; either clean inconclusive disposition can pass only after teardown succeeds, while inventory drift emits after clean teardown and then fails;
9. dispose the factory, clear all SQLite pools, and delete/verify absence of only the exact temporary database file in teardown; and
10. emit exactly one outcome token after teardown: `SCOPE_OR_CLEANUP_BLOCKED` alone on any cleanup failure, `ENVIRONMENT_BLOCKED` alone on an inventory/harness exception after successful cleanup, otherwise the one pending classification. Never emit the pending token before teardown. Assertions may run only after this sole token is emitted.

The test may use `ITestOutputHelper` for the sanitized rows and outcome. It must not read endpoint source files, use regex over source/IL, invoke private handlers by reflection, construct route requests, replace the gate with a spy, or claim a bypass/non-bypass. Reflection is permitted only to inspect a `MethodInfo` object supplied directly in runtime endpoint metadata.

## Outcome Taxonomy

Exactly one outcome is recorded:

- `INCONCLUSIVE_NO_COMPLETE_RECOMMENDATION_SURFACE_MARKER`: the exact bounded catalog is present and deterministically inventoried, but runtime metadata cannot identify the universe of recommendation-shaped surfaces. This is the expected pinned-base disposition and is not a product defect.
- `INCONCLUSIVE_NO_HANDLER_METADATA_SEAM`: runtime routes are available but the framework exposes no usable handler `MethodInfo` metadata for the bounded catalog. Record route/auth metadata only; do not fall back to source text.
- `INCONCLUSIVE_INVENTORY_DRIFT`: a catalog name is missing/duplicated or its method/pattern differs. Preserve the exact runtime rows and stop for coordinator adjudication; do not relabel as a bypass.
- `ENVIRONMENT_BLOCKED`: the application/test cannot build, start locally, or resolve endpoint data because of SDK, dependency, restore metadata, or local harness failure.
- `SCOPE_OR_CLEANUP_BLOCKED`: an out-of-scope diff, external network attempt, non-synthetic resource need, or exact temporary-file cleanup failure occurs.
- `REPRODUCED_REACHABLE_GATE_BYPASS` is reserved and cannot be emitted by this shaped parcel. It would require a separately authorized local request with a gate spy, a relevant 2xx response, and zero gate calls. A later amendment must name that endpoint, fixture, expected response field, owner, invariant, Allowed Files, and security review.

`NOT_REPRODUCED_ENDPOINT_WIDE` is forbidden because the parcel has no complete surface classifier. No absence-based conclusion is allowed.

## Deterministic Verification

Run from the fresh Q02-v2 worktree with already-provisioned local restore metadata. `--no-restore` is mandatory. `rtk` was not resolvable during shaping, so the raw commands use the repository AGENTS.md debugging fallback; if `rtk` resolves in v2, its corresponding form may be used without changing arguments or evidence.

### Fresh v2 preflight and source custody

Before the transcript is created or AF-Q02 is materialized, verify and restate: branch `codex/biostack-remediation-q02-v2`; HEAD `339f259b1a467034db4f57cf9d774c292f11b53a`; tree `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`; empty Git status; AF-Q02 absent at HEAD and on disk; new v2 transcript absent; rejected branch still at `dea721288521a5789350e2398d0d7292b01ad10f`; and preserved v1 transcript length/hash exactly as stated above. A mismatch stops v2 before any write.

After coordinator confirms Step 0, AF-Q02 source content may be read only with `git show dea721288521a5789350e2398d0d7292b01ad10f:backend/tests/BioStack.Api.Tests/Architecture/UserFacingGateCoverageInvestigationTests.cs`; create and rework the one Allowed File through `apply_patch`. This is content custody, not a cherry-pick. Recheck status is exactly one untracked AF-Q02 path before testing.

### One syntax-checked, write-once evidence procedure

Use one PowerShell procedure for the complete baseline, targeted, adjacent, API, solution, scope, commit, and final-custody chain. The procedure has exactly two authorized phases, `Baseline` and `Candidate`, because AF-Q02 must remain absent during the clean baseline and must be edited with `apply_patch` between phases. Define the complete procedure source in a here-string, parse it before each invocation with `System.Management.Automation.Language.Parser::ParseInput`, and require zero parse errors. `Baseline` must fail if the new transcript exists, create it with `New-Item -ItemType File -ErrorAction Stop`, capture the clean-base receipts/tests, emit `PHASE_END baseline=GREEN`, and return without creating AF-Q02. After AF-Q02 is materialized and reworked through `apply_patch`, `Candidate` must require that exact transcript, the baseline sentinel, and the sole untracked AF-Q02 status before appending the remaining evidence. No third phase or invocation is authorized. Every evidence line uses `Tee-Object -LiteralPath $transcript -Append`; do not use `Start-Transcript`, replacement redirection, or any writer other than these two invocations by the same investigator.

The procedure's native-command helper must follow this exact control pattern so full native stdout/stderr is captured and the exit is preserved before PowerShell formatting can overwrite it:

```powershell
function Invoke-NativeLogged {
    param(
        [Parameter(Mandatory)][string] $Label,
        [Parameter(Mandatory)][string] $FilePath,
        [Parameter(Mandatory)][string[]] $ArgumentList,
        [int[]] $AllowedExitCodes = @(0)
    )

    "COMMAND_BEGIN label=$Label file=$FilePath arguments=$($ArgumentList -join ' ')" |
        Tee-Object -LiteralPath $transcript -Append | Out-Host
    $nativeOutput = @(& $FilePath @ArgumentList 2>&1)
    $nativeExit = $LASTEXITCODE
    $textLines = @($nativeOutput | ForEach-Object { $_.ToString() })
    $textLines | Tee-Object -LiteralPath $transcript -Append | Out-Host
    "COMMAND_END label=$Label exit=$nativeExit lines=$($textLines.Count)" |
        Tee-Object -LiteralPath $transcript -Append | Out-Host

    if ($AllowedExitCodes -notcontains $nativeExit) {
        throw "Native command failed: $Label exit=$nativeExit"
    }

    [pscustomobject]@{ Label = $Label; ExitCode = $nativeExit; Lines = $textLines }
}
```

Every `git` and `dotnet` receipt below must go through that helper. Set both Collective environment variables to the empty string inside the parsed procedure. For each test result, select all native summary lines matching the .NET test `Failed/Passed/Skipped/Total` summary shape, emit them again as `COUNT label=<label> <native-summary>` through `Tee-Object`, and assert the required counts. For the targeted result, trim the captured native lines and assert exactly eleven distinct `ROW ` lines and exactly one `OUTCOME ` line. The only green targeted outcome is one of the two authorized inconclusive tokens. A duplicate outcome, missing row, unparseable/ambiguous count, or missing Git command-boundary/exit receipt terminally stops v2; a deliberately empty native Git result remains valid only when its captured exit and expected-empty assertion are present.

Execute and capture in this order:

1. `Baseline`: Git receipts `rev-parse HEAD`, `show -s --format=%T HEAD`, `branch --show-current`, `status --porcelain=v1 --untracked-files=all`, base target-absence check (exit `128` is the sole allowed result), rejected commit/parent/tree/path receipts, preserved-branch identity, and preserved-v1-transcript length/hash.
2. `Baseline`: adjacent command, requiring exactly `21` passed / `0` failed / `0` skipped.
3. `Baseline`: full API and full solution commands; record the unique API test summary as `B_api` and every solution assembly summary as `B_solution`, then emit the baseline phase sentinel and return.
4. Between phases, materialize AF-Q02 from the exact rejected commit path and use `apply_patch` for the v2 rework. `Candidate` then records sole status path, file SHA-256, Git blob identity, and `git diff --check`; the procedure never creates or edits AF-Q02.
5. Targeted inventory command with detailed console logging, requiring `1` passed / `0` failed / `0` skipped, eleven distinct sorted rows, exactly one post-teardown outcome, and zero other `OUTCOME ` lines.
6. Adjacent command, requiring `21` passed / `0` failed / `0` skipped.
7. Full API command, requiring exactly `B_api + 1` total/passed, zero failed, and skipped unchanged from `B_api`.
8. Full `backend/BioStack.sln` command; capture every native assembly summary and require only the API assembly to gain the one test relative to `B_solution`.
9. Pre-commit scope receipts: exact Q02 temporary-file glob count `0`, status exactly sole untracked AF-Q02, `git diff --check`, AF-Q02 SHA-256/blob, and no other Git-visible path.
10. Only after steps 1-9 are green, stage AF-Q02 by its exact path, create one local evidence-only commit, and capture the native commit output.
11. Final custody receipts: branch; HEAD/tree; `rev-list --parents -n 1 HEAD`; `diff-tree --no-commit-id --name-status -r HEAD`; candidate blob/SHA-256; `diff --check <base> HEAD`; `diff --name-status <base> HEAD`; empty final status; exact Q02 temp-file glob count `0`; rejected branch still at the rejected commit; v1 transcript unchanged; and `TRANSCRIPT_END status=GREEN`.

The four test command shapes retain the exact arguments from v1 and are repeated where the phase list requires: targeted uses the exact Q02 fully-qualified-name filter and detailed logger; adjacent uses the exact three-suite filter; full API runs `backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj`; solution runs `backend/BioStack.sln`; all use `--no-restore --disable-build-servers`, and API/adjacent use console loggers. No HTTP request, restore, package registry, external network, source/IL scan, or real data is authorized.

After the procedure exits successfully and no further transcript write is possible, compute the new transcript's byte length and SHA-256 outside the procedure and place both in the handoff. The transcript itself, its hash receipt, the preserved v1 transcript, and the rejected branch are not committed. Any procedure exception, nonzero unallowed native exit, count/output mismatch, partial transcript, cleanup failure, or scope drift produces no commit and terminally blocks Q02.

## Evidence and Handoff

The investigator sends one record in this exact order:

1. `Parcel / outcome`: Q02-v2 and the one authorized post-teardown outcome token; explicit statement that v1 was rejected.
2. `Branch / worktree / base`: exact v2 branch, path, starting/ending commit/tree, ancestry, clean-start receipt, plus preserved v1 branch/commit identity.
3. `Allowed Files / effects`: changed-path list, test-file hash, temp-database path and deletion result, ignored artifacts, final status, diff check.
4. `Inventory method`: runtime `EndpointDataSource` and framework metadata types; explicit no-source-text/no-IL-scan statement.
5. `Bounded catalog`: eleven sorted rows with name/method/pattern/tags/auth/policies/handler-metadata/direct-gate-parameter fields.
6. `Completeness limit`: explicit absence of a first-class recommendation-surface marker and explicit non-inference from tags/names/routes/negative parameters.
7. `Test receipts`: baseline 21-case adjacent result, `B_api`, `B_solution`, targeted one-case result, final adjacent/API/solution counts and deltas.
8. `Network/data boundary`: local startup only, no request issued, synthetic temp database/config, no external provider/cloud/production/protected-data/secret access.
9. `Finding / residual uncertainty / decision requested`: exact items, including surfaces outside the bounded catalog and indirect-gate uncertainty.
10. `Transcript custody`: new v2 transcript path, final byte length/SHA-256, full-output/count/row/outcome/Git-receipt assertions, write-once/two-phase procedure receipt, and unchanged v1 transcript length/SHA-256.
11. `Authority close`: test/evidence only; no production change; not pushed, merged, deployed, published, or released; Gate 3 ungranted; diagnostic and rejected v1 branches untouched.

For an executable inventory outcome, the handoff also names the exact local evidence commit and proves it has the pinned base as its sole parent, contains only AF-Q02, and leaves the worktree clean. A blocked or drift outcome records commit `none`.

## Review and Custody

After the v2 evidence record exists, a fresh read-only adversarial reviewer who did not perform the v1 review checks the exact v2 commit/diff, rejected-v1 preservation, runtime inventory construction, eleven-row catalog, auth inheritance, handler-metadata handling, post-teardown single-outcome control flow, full native transcript output, count/row/outcome/Git receipts, write-once transcript custody, no-source-text rule, outcome classification, no-network controls, and cleanup. The reviewer must inspect the actual new transcript, not only the investigator's summary. Because Q02 expressly does not claim completeness, one independent review is required. If any revision claims complete endpoint-wide coverage, two independent adversarial reviews become mandatory under the charter before acceptance. Reviewers do not edit, stage, commit, or convert the inventory into a remediation. Any new evidence or taxonomy rejection is terminal for Q02 rather than authorization for v3.

Both Q02 branches and both transcripts remain evidence-only, are recorded at Stage F, and are not deleted without explicit cleanup authority. Acceptance of v2 does not rehabilitate or rewrite v1.

## Step 0 — Restate and Stop

Before creating the test, the investigator must restate and then stop for coordinator confirmation:

1. Q02's test/evidence-only goal, bounded-not-complete posture, exact branch/worktree/base/tree, and AF-Q02;
2. the eleven exact catalog rows and the runtime `EndpointDataSource` metadata fields;
3. the one exact test name, prohibition on source/IL text heuristics and HTTP requests, and no inference from a missing direct parameter;
4. exact baseline/target/adjacent/aggregate commands and count contract (`0 -> 1`, adjacent `21 -> 21`, API project `+1`);
5. outcome taxonomy, especially the forbidden `NOT_REPRODUCED_ENDPOINT_WIDE` and reserved reproduced-bypass outcome;
6. synthetic local SQLite lifecycle, no network/provider/cloud/production/protected-data/secret authority, and cleanup stop rule;
7. fresh read-only review, outcome-specific custody, Gate 3 ungranted, and no push/PR/merge/deploy/publish/release/diagnostic mutation.

Silence is not confirmation. Any discrepancy stops Q02.

### Fresh Step 0 for v2

No prior Q02 Step 0 carries forward. Before creating the v2 worktree, transcript, or AF-Q02, the fresh investigator must restate and stop for coordinator confirmation:

1. the exact new v2 branch/worktree/base/tree, clean target absence, and new transcript absence;
2. rejected v1 commit/tree/sole path, preserved branch, preserved transcript path/length/hash, and the prohibition on changing or reusing them;
3. that v2 may copy/rework only AF-Q02 content from the rejected commit, using no cherry-pick and no other path;
4. the pending-classification rule: no `OUTCOME ` before teardown, exact disposal/pool-clear/exact-delete ordering, exactly one post-teardown token, and cleanup failure suppressing every pending token in favor of sole `SCOPE_OR_CLEANUP_BLOCKED`;
5. the two-phase, syntax-checked `Tee-Object` procedure; full native stdout/stderr, immediate exit capture, exact row/outcome/count/Git evidence, new transcript custody, and terminal failure rule;
6. mandatory counts: baseline and final adjacent `21/0/0`; targeted `1/0/0`; API `B_api + 1` with failed zero/skipped unchanged; and solution delta only in the API assembly relative to `B_solution`;
7. no stage or commit until targeted, adjacent, API, solution, exact-path cleanup, transcript, diff, and status receipts are green; then exactly one local AF-Q02 evidence commit and final custody receipts;
8. fresh review of the new exact commit and transcript, with any further evidence/taxonomy rejection terminally blocking Q02; and
9. the unchanged bounded-inconclusive/no-HTTP/no-network/no-production/no-Gate-3/no-push/PR/merge/deploy/publish/release authority boundary.

Silence is not confirmation. Any discrepancy terminally stops v2.

## Stop Rules

Stop without widening scope if Step 0 is unconfirmed; base/tree/status or a pinned blob differs; the v2 branch/worktree/transcript already exists unexpectedly; rejected v1 custody changes; AF-Q02 is insufficient; another repository path changes; runtime endpoint data is unavailable; a source-text/IL scan, route-keyword classifier, private-handler invocation, or HTTP request is proposed; an external resource or real data is needed; the temporary database cannot be safely deleted by its exact path; the pending outcome is emitted before teardown; outcome count is not exactly one; native output/exit/count/Git evidence is incomplete; a result cannot be separated from harness failure; `origin/main` was reported materially moved; or any push, PR, merge, deployment, publication, release, or diagnostic-branch mutation is proposed. Any v2 cleanup, procedure, evidence, taxonomy, or fresh-review failure is terminal: do not retry, create v3, widen, stage, or commit.

BioStack lacks the native Foreman shaping emitter/linter, so this draft intentionally creates no `ShapingResult`. Coordinator lint remains authoritative.
