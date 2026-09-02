# Inquiry Parcel Q02: Endpoint Gate Coverage

Status: **active — teardown-only continuation passed coordinator re-lint; dispatch remains contingent on fresh Step 0 confirmation**

Coordinator lint: **PASSED 2026-09-02**. The eleven exact endpoint identities/patterns, project and production blobs, 21-case adjacent count, AF-Q02 absence, and runtime metadata approach were independently rechecked. The parcel is intentionally bounded and may not claim endpoint-wide completeness. Any executable inventory outcome must preserve its single test in one local evidence-only commit for exact review.

Teardown-continuation re-lint: **PASSED 2026-09-02**. The existing AF-Q02 identity and failed-run transcript were rechecked; local `Microsoft.Data.Sqlite.Core` 10.0.0 exposes `ClearAllPools`; no package/project change is needed. Its process-wide pool effect is accepted only inside this synthetic test process and must be checked by the mandatory API/solution aggregate. A repeated cleanup or aggregate failure is terminal for Q02.

## Identity

- Goal: `biostack-algorithm-remediation`
- Project: `BioStack` / `BioStack.Api`
- Parcel: `Q02-endpoint-gate-coverage`
- Wave: Wave Q — inconclusive-claim adjudication
- Routing: fresh frontier architecture-evidence investigator
- Risk: high safety-boundary classification risk; test/evidence authority only
- Branch: `codex/biostack-remediation-q02`
- Isolated worktree: `C:\Users\clint\.codex\worktrees\biostack-remediation-q02\BioStack`
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

- `C:\Users\clint\.codex\evidence\biostack-algorithm-remediation\q02-endpoint-gate-inventory-2026-09-02.transcript.txt`

The test must dispose its factory/client resources and delete only the exact temporary database file it created. No project, production, configuration, fixture, package/lock, snapshot, goal, handoff, or other test file may change. If the test cannot compile or access runtime endpoint metadata in this file alone, stop rather than adding a seam.

After all required receipts and exact temporary-file cleanup, an executable inventory outcome must create exactly one local test-only commit containing only AF-Q02. Environment-, scope-, cleanup-, or inventory-drift stops create no commit. The evidence commit is never pushed or merged and is not a remediation candidate.

## One Authorized Same-AF Teardown Repair

The first targeted run failed to compile because AF-Q02 lacked the already-existing integration-extension namespace; the investigator corrected only that import inside AF-Q02. The second targeted run completed all eleven rows and emitted `INCONCLUSIVE_NO_COMPLETE_RECOMMENDATION_SURFACE_MARKER`, but the final test result was `0` passed / `1` failed because the exact SQLite file remained locked during teardown. The omitted exception text must not be invented or reconstructed. The continuation remains bounded by the original outcome taxonomy and the no-absence-inference rule.

The locked file was `C:\Users\clint\AppData\Local\Temp\biostack-q02-endpoint-inventory-42c215732fd442d2b42f19f9e5f3bc87.db`, size `565248` bytes, with no `-wal` or `-shm` sibling. Only after the test process exited, the investigator validated the resolved OS-temporary parent and exact GUID filename grammar, deleted that one file, and verified it absent. That post-process recovery establishes clean custody but does not satisfy the test's deterministic in-process cleanup invariant. The transcript is `7403` bytes with SHA-256 `4E664A6144506C988FA5C6F87DD0FA30CD608DDA36CD086AFDAEEB8C673AC45A`.

After coordinator re-lint and a new Step 0 confirmation, exactly one teardown-only repair attempt is permitted in AF-Q02. At the continuation preflight, AF-Q02 must be the sole Git-visible path, remain untracked, and match both of these identities:

- SHA-256: `CDB62C0F6FE7C5203CF788FA8E298E762DBB7B05F5E38369404BC1C46FAFD9C5`
- Git blob identity: `98ad70ed8681b18e03f4eae3b4d4692e0ab29f1d`

The repair may make only these two code changes:

1. add `using Microsoft.Data.Sqlite;`; and
2. in the existing `finally` block, after the `WebApplicationFactory` disposal attempt has completed and before the existing exact-path `File.Exists` / `File.Delete` block begins, call `SqliteConnection.ClearAllPools()` in its own `try` / `catch`, preserving the first cleanup exception with the existing `cleanupFailure ??= exception` rule.

This ordering is mandatory: application/factory disposal, SQLite pool release, exact-path existence/deletion validation. The repair must not change the connection string, inventory catalog, assertions, outcome tokens, metadata interpretation, test name, configuration, network posture, or any other behavior. Do not add sleeps, retries, garbage collection, wildcard deletion, directory deletion, process termination, or a second file. The locally resolved `Microsoft.Data.Sqlite.Core` 10.0.0 reference exposes `SqliteConnection.ClearAllPools()`, and the existing test assets already contain that compile reference; no package, project, restore, or network change is authorized.

If pool release throws, exact-path deletion throws, the exact file still exists after deletion, or any cleanup uncertainty remains, emit `SCOPE_OR_CLEANUP_BLOCKED`, create no commit, preserve AF-Q02 and the evidence, and terminally stop Q02. A second teardown repair or retry is not authorized.

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
8. emit `INCONCLUSIVE_NO_COMPLETE_RECOMMENDATION_SURFACE_MARKER` after a clean eleven-row inventory when usable handler metadata is available; if it is unavailable for any catalog row, record the affected rows and emit `INCONCLUSIVE_NO_HANDLER_METADATA_SEAM`. Either inconclusive disposition passes the test; and
9. delete only the exact temporary database file in teardown. A cleanup failure is `SCOPE_OR_CLEANUP_BLOCKED`, even if inventory assertions pass.

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

Run from the Q02 worktree root with already-provisioned local restore metadata. `--no-restore` is mandatory.

`rtk` was not resolvable in the shaping environment. The raw commands below therefore use the repository AGENTS.md debugging fallback; if `rtk` is available in the investigator worktree, the corresponding `rtk` form may be used without changing arguments or evidence.

### Baseline before creating the test

```powershell
$env:KEON_COLLECTIVE_CONTROL_URL = ''
$env:KEON_COLLECTIVE_BEARER_TOKEN = ''
git rev-parse HEAD
git show -s --format=%T HEAD
git status --porcelain=v1 --untracked-files=all
git cat-file -e 339f259b1a467034db4f57cf9d774c292f11b53a:backend/tests/BioStack.Api.Tests/Architecture/UserFacingGateCoverageInvestigationTests.cs
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Api.Tests.Integration.AuthorizationEnforcementMatrixIntegrationTests|FullyQualifiedName~BioStack.Api.Tests.Integration.IntelligenceSafetyGateIntegrationTests|FullyQualifiedName~BioStack.Api.Tests.Integration.StackReviewEndpointsIntegrationTests" --disable-build-servers --logger "console;verbosity=minimal"
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --disable-build-servers --logger "console;verbosity=minimal"
```

Expected static receipt: exact base commit/tree, empty status, target absence (`git cat-file -e` exit `128`), and exactly 21 adjacent tests passed with zero failed/skipped. Record the full API-project baseline as `B_api`. Any different adjacent count or baseline failure stops Q02 before the file is created.

### Fresh continuation preflight after the cleanup-blocked run

The original baseline remains the comparison receipt; do not recreate the test or relabel the uncommitted continuation as a clean-start baseline. Before the single teardown repair, rerun only this read-only identity preflight and stop for coordinator confirmation:

```powershell
$q02Worktree = 'C:\Users\clint\.codex\worktrees\biostack-remediation-q02\BioStack'
$q02Test = Join-Path $q02Worktree 'backend\tests\BioStack.Api.Tests\Architecture\UserFacingGateCoverageInvestigationTests.cs'
git -C $q02Worktree rev-parse HEAD
git -C $q02Worktree show -s --format=%T HEAD
git -C $q02Worktree status --porcelain=v1 --untracked-files=all
(Get-FileHash -Algorithm SHA256 -LiteralPath $q02Test).Hash
git -C $q02Worktree hash-object -- $q02Test
git -C $q02Worktree diff --check
git -C $q02Worktree diff --name-only
```

Expected continuation receipt: HEAD `339f259b1a467034db4f57cf9d774c292f11b53a`; tree `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`; status exactly `?? backend/tests/BioStack.Api.Tests/Architecture/UserFacingGateCoverageInvestigationTests.cs`; the two AF-Q02 identities above; empty tracked diff, changed-path list, and diff-check output. Any mismatch terminally stops this continuation without editing.

### Targeted inventory

```powershell
$env:KEON_COLLECTIVE_CONTROL_URL = ''
$env:KEON_COLLECTIVE_BEARER_TOKEN = ''
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Api.Tests.Architecture.UserFacingGateCoverageInvestigationTests" --disable-build-servers --logger "console;verbosity=detailed"
```

Expected clean inventory receipt: exactly one test discovered/passed, zero failed/skipped, eleven sorted catalog rows, and one authorized inconclusive token. If route inventory or harness checks fail, classify from the taxonomy; do not infer a product defect.

### Adjacent and aggregate

```powershell
$env:KEON_COLLECTIVE_CONTROL_URL = ''
$env:KEON_COLLECTIVE_BEARER_TOKEN = ''
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Api.Tests.Integration.AuthorizationEnforcementMatrixIntegrationTests|FullyQualifiedName~BioStack.Api.Tests.Integration.IntelligenceSafetyGateIntegrationTests|FullyQualifiedName~BioStack.Api.Tests.Integration.StackReviewEndpointsIntegrationTests" --disable-build-servers --logger "console;verbosity=minimal"
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --disable-build-servers --logger "console;verbosity=minimal"
dotnet test backend/BioStack.sln --no-restore --verbosity minimal --disable-build-servers
git diff --check 339f259b1a467034db4f57cf9d774c292f11b53a
git diff --name-only 339f259b1a467034db4f57cf9d774c292f11b53a
git status --porcelain=v1 --untracked-files=all
```

Expected candidate receipt: adjacent remains exactly 21 passed; relative to `B_api`, the API project total and passed count increase by exactly one, with failed zero and skipped unchanged; only the API test assembly gains one test in the solution run. The changed-path list contains exactly AF-Q02, temporary state is gone, and `git diff --check` is clean.

For the teardown continuation, the full post-repair chain is mandatory and cannot be shortened based on the earlier attempts: targeted inventory exactly `1` passed / `0` failed / `0` skipped; adjacent exactly `21` passed / `0` failed / `0` skipped; full API totals exactly `B_api + 1` passed with `0` failed and skipped unchanged from `B_api`; solution totals changed only by the API assembly's one added test; exact temporary SQLite path absent; and all scope/diff receipts green. Do not stage or commit between commands. Only after this entire chain is green may the investigator create the one AF-Q02 evidence commit and then recheck that the commit has the pinned base as sole parent, contains only AF-Q02, and leaves the worktree clean.

## Evidence and Handoff

The investigator sends one record in this exact order:

1. `Parcel / outcome`: Q02 and one authorized outcome token.
2. `Branch / worktree / base`: exact branch, path, starting/ending commit/tree, ancestry, clean-start receipt.
3. `Allowed Files / effects`: changed-path list, test-file hash, temp-database path and deletion result, ignored artifacts, final status, diff check.
4. `Inventory method`: runtime `EndpointDataSource` and framework metadata types; explicit no-source-text/no-IL-scan statement.
5. `Bounded catalog`: eleven sorted rows with name/method/pattern/tags/auth/policies/handler-metadata/direct-gate-parameter fields.
6. `Completeness limit`: explicit absence of a first-class recommendation-surface marker and explicit non-inference from tags/names/routes/negative parameters.
7. `Test receipts`: baseline 21-case adjacent result, `B_api`, targeted one-case result, final adjacent/API/solution deltas.
8. `Network/data boundary`: local startup only, no request issued, synthetic temp database/config, no external provider/cloud/production/protected-data/secret access.
9. `Finding / residual uncertainty / decision requested`: exact items, including surfaces outside the bounded catalog and indirect-gate uncertainty.
10. `Authority close`: test/evidence only; no production change; not pushed, merged, deployed, published, or released; Gate 3 ungranted; diagnostic branches untouched.

For an executable inventory outcome, the handoff also names the exact local evidence commit and proves it has the pinned base as its sole parent, contains only AF-Q02, and leaves the worktree clean. A blocked or drift outcome records commit `none`.

## Review and Custody

After the evidence record exists, a fresh read-only adversarial reviewer checks the exact branch/diff, runtime inventory construction, eleven-row catalog, auth inheritance, handler-metadata handling, count receipts, no-source-text rule, outcome classification, no-network controls, and cleanup. Because Q02 expressly does not claim completeness, one independent review is required. If any revision claims complete endpoint-wide coverage, two independent adversarial reviews become mandatory under the charter before acceptance. Reviewers do not edit, stage, commit, or convert the inventory into a remediation.

The Q02 branch and inconclusive/blocked outcome remain evidence-only, are recorded at Stage F, and are not deleted without explicit cleanup authority.

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

### Fresh Step 0 for the teardown continuation

The prior Step 0 does not carry forward. Before changing AF-Q02, the investigator must restate and stop again for coordinator confirmation:

1. the exact continuation branch/worktree/base/tree, sole untracked AF-Q02 path, SHA-256, Git blob identity, and empty tracked diff;
2. that this is one teardown-only repair: add the SQLite namespace import and call `SqliteConnection.ClearAllPools()` after factory disposal and before exact-path deletion, preserving first cleanup failure;
3. that no inventory, catalog, assertion, outcome, configuration, request, network, source/IL heuristic, or absence-inference behavior may change;
4. the mandatory rerun counts: targeted `1/0/0`, adjacent `21/0/0`, API `B_api + 1` with failed `0` and skipped unchanged, and solution delta only in the API assembly;
5. that there is no stage or commit until targeted, adjacent, API, solution, exact-path cleanup, diff, and status receipts are all green;
6. that any repeated cleanup failure is terminal `SCOPE_OR_CLEANUP_BLOCKED`: no second repair, no retry, no commit, and preserved AF-Q02/evidence for coordinator adjudication; and
7. the unchanged bounded-inconclusive/no-HTTP/no-network/no-production/no-Gate-3/no-push/PR/merge/deploy/publish/release authority boundary.

Silence is not confirmation. Any discrepancy terminally stops the continuation.

## Stop Rules

Stop without widening scope if Step 0 is unconfirmed; base/tree/status or a pinned blob differs; AF-Q02 is insufficient; another repository path changes; runtime endpoint data is unavailable; a source-text/IL scan, route-keyword classifier, private-handler invocation, or HTTP request is proposed; an external resource or real data is needed; the temporary database cannot be safely deleted by its exact path; a result cannot be separated from harness failure; `origin/main` was reported materially moved; or any push, PR, merge, deployment, publication, release, or diagnostic-branch mutation is proposed. For the one teardown continuation, any second cleanup or pool-release failure is terminal: do not retry, widen the repair, stage, or commit.

BioStack lacks the native Foreman shaping emitter/linter, so this draft intentionally creates no `ShapingResult`. Coordinator lint remains authoritative.
