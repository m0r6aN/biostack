# Parcel Spec: P08 Collective Outbound Authorization

Status: **active — Amendment 02 ratified; awaiting clean isolated worktree and confirmed Step 0**

Coordinator lint: **PASSED 2026-09-02**. The eight-file scope covers every verified constructor/DI/test compile site without adding a project reference; scoped adapter/orchestrator/board composition avoids captive dependencies; targeted and aggregate count deltas match the pinned receipts. The local named-client override in the new API integration test remains an implementation tripwire: if AF-P08 cannot provide a non-delegating handler inside that one file, the parcel stops rather than widening scope. One-check-per-`RunAsync`, no authenticated-user-to-intent actor/tenant binding, and no mid-poll consent re-check are explicit residual boundaries.

## Identity

- Goal: `biostack-algorithm-remediation`
- Project: `BioStack`
- Parcel: `P08-collective-outbound-authorization`
- Wave: follow-on critical outbound boundary
- Routing: fresh frontier implementation
- Risk: critical outbound-data authorization and DI-lifetime risk
- Branch: `codex/biostack-remediation-p08`
- Isolated worktree: `C:\Users\clint\.codex\worktrees\biostack-remediation-p08\BioStack`
- Required starting commit: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Required starting tree: `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`

## Goal

Retain Q01's reproduced Collective config-only transmission as a regression and make the smallest fail-closed production change: the live orchestrator must obtain the current authenticated user's current server-side consent before constructing a submit request, borrowing a client, constructing an API client, materializing headers/body, or sending. Denial or gate failure produces the existing degraded envelope and zero local-handler calls; an affirmative current-consent decision permits exactly one initial local-fake submit and preserves existing mapping/poll behavior.

Amendment 02 is explicitly ratified and extends contingent Gate 2 authority to P08. Dispatch still requires the exact isolated base/worktree and confirmed Step 0. Gate 3 is ungranted.

## Authority and Evidence

- Controlling documents: the ratified charter, plan-review findings, loop directive, Gate 1 receipts, and ratified Narrow Gate 1 Amendment 02.
- Q01 evidence source: local evidence-only commit `83d1235b3a964ddf7130f67900f4dcd3b38dcf42`, sole parent `339f259b1a467034db4f57cf9d774c292f11b53a`, changed path exactly `backend/tests/BioStack.Application.Tests/Cognition/CollectiveOutboundBoundaryInvestigationTests.cs`.
- Q01 test blob: `02e966837595b0284029acb280cbc8b79c6780f8`; file SHA-256 `0302578C83C89C78A55E3ED3AAD2F7DEECEAA8C8A15366A0C42D30275AD25CC4`.
- Q01 transcript: `C:\Users\clint\.codex\evidence\biostack-algorithm-remediation\q01-collective-outbound-2026-09-02.transcript.txt`.
- Accepted disposition: `REPRODUCED_CONFIG_ONLY_TRANSMISSION`; one intended assertion failure observed exactly one local-handler `POST /api/collective/live-runs`, with synthetic objective/identity sentinels crossing and no Authorization header/current-user consent input.
- The Q01 branch remains evidence-only, intentionally failing, local, preserved, and unmerged. P08 copies the test file content; it never merges or rewrites Q01 history.

## Production Owners and Dependency Contract

- `CollectiveLiveOrchestrator.RunAsync` owns the check immediately before any outbound-operation object is constructed.
- `CognitionServiceCollectionExtensions` owns compatible lifetimes for `ICognitiveDensityOrchestrator` and `IStackReviewBoardService`.
- New API `CollectiveOutboundAuthorizationGate` owns the narrow bridge from Cognition's authorization interface to existing `IConsentGate.IsConsentGrantedAsync`.
- `Program.cs` owns scoped adapter registration before `AddCollectiveIntegration` composition.
- Existing `ConsentGate` owns current-user/current-version consent semantics and remains unchanged.
- Existing `CollectiveApiClient` remains downstream and unchanged.

The Cognition project may declare a narrow public `ICollectiveOutboundAuthorizationGate` beside the live orchestrator. The API adapter implements it by directly depending on scoped `IConsentGate`. To prevent captive dependencies, register the adapter, live `ICognitiveDensityOrchestrator`, and `IStackReviewBoardService` as scoped. `IStackDeliberationTranslator` may remain singleton. Do not add a Cognition-to-Application project reference, resolve scoped services from the root provider, create an ad hoc service scope, use ambient statics, or add a caller-supplied authorization boolean.

The API integration regression must prove that the request-scoped adapter sees the current authenticated user through the existing `HttpContextCurrentUserAccessor` and real `ConsentGate`, not a request-body/header/config boolean.

## Pinned-Base Facts

These facts were inspected at `339f259b1a467034db4f57cf9d774c292f11b53a`:

- `CollectiveLiveOrchestrator.cs` blob `b3a7f2fb6f7e731ef7a435f599be1716be158f40` constructs `CollectiveSubmitRequest`, borrows `keon-collective`, constructs `CollectiveApiClient`, and submits without a current-user gate.
- `CognitionDependencyInjection.cs` blob `d2a7afb204904ea2f59c875962cc85a230a6c83d` registers the live orchestrator and stack-review board as singletons. Its `BioStack.Cognition` project does not reference `BioStack.Application`.
- `Program.cs` blob `26ad33636037a5d40b440ef7b59330335793746d` registers `IHttpContextAccessor`, scoped `ICurrentUserAccessor`, scoped `IConsentGate`, and then Collective integration.
- `ConsentGate.cs` blob `13ad7d33c5817360d40f89777caaa61d11e9aa9a` obtains the current user through `ICurrentUserAccessor` and grants only the current server consent version. `HttpContextCurrentUserAccessor.cs` blob `f58d343c8f3e37114fb0c412dab5ab22a4a50c22` binds that accessor to the request context. Both remain read-only.
- `CollectiveApiClient.cs` blob `efe37ffc154574be1ce064e2dddcf2af0494dcdf` constructs the request headers/content and sends through the injected client. It remains read-only because the orchestrator can deny before instantiating it.
- `CollectiveLiveOrchestratorTests.cs` blob `5a0df07028b897759f84dbbd7d264cb520c6ccad` has six facts and three-argument constructor call sites. `CollectiveLiveIntegrationTests.cs` blob `1f5981c69df2a213b1fcab8dd54c2d61eb24c7bd` has five opt-in skippable facts and another constructor call site.
- `BioStack.Application.Tests.csproj` blob `d83d0fea607573adec124c8e4417c768ba71aaa0` references Cognition and compiles ordinary `.cs` files by SDK convention. `BioStack.Api.Tests.csproj` blob `536ccab0ecd50607bbc76c2ac55447128e87bbeb` references the API project and needs no edit for a new integration `.cs` file.
- The only production/test constructor call sites found for `CollectiveLiveOrchestrator` are Cognition DI, the six-case unit fixture, the five-case opt-in integration fixture, and Q01's retained evidence test. AF-P08 includes every compile change plus the new real-current-user integration file.

## Exact Allowed Files — AF-P08

Only these eight paths may change:

- `backend/src/BioStack.Cognition/CollectiveApi/CollectiveLiveOrchestrator.cs`
- `backend/src/BioStack.Cognition/CognitionDependencyInjection.cs`
- `backend/src/BioStack.Api/Auth/CollectiveOutboundAuthorizationGate.cs`
- `backend/src/BioStack.Api/Program.cs`
- `backend/tests/BioStack.Application.Tests/Cognition/CollectiveOutboundBoundaryInvestigationTests.cs`
- `backend/tests/BioStack.Application.Tests/Cognition/CollectiveLiveOrchestratorTests.cs`
- `backend/tests/BioStack.Application.Tests/Cognition/CollectiveLiveIntegrationTests.cs`
- `backend/tests/BioStack.Api.Tests/Integration/CollectiveOutboundAuthorizationIntegrationTests.cs`

At the pinned base, the adapter, retained regression file, and new API integration test are absent. All other Allowed Files exist. If compilation, DI validation, or testing needs any other path, stop for a charter/spec amendment; do not edit a project file or substitute a nearby test.

## Forbidden and Out of Scope

- Every path outside AF-P08, including project/package/lock files, `ConsentGate.cs`, `ICurrentUserAccessor.cs`, `CollectiveApiClient.cs`, Collective options/models, endpoints, middleware, configuration files, database/repositories, frontend, sidecar, deployment, and IaC.
- No change to consent copy/version, authentication/session issuance, endpoint authorization, payload schema, tenant/actor/correlation mapping, static provider authentication, base URL/timeouts, submit/poll response mapping, doctrine flags, or live-mode enablement.
- No claim that the Collective endpoint is unauthenticated or that Collective is a first- or third-party processor.
- No authenticated-user-to-intent-actor rebinding and no mid-poll consent re-check. Amendment 02 authorizes one current-consent decision at the start of each `RunAsync` operation only.
- No refactor, package, schema, public endpoint, config, or unrelated cleanup.
- No network, registry, DNS/socket, Collective/provider/cloud/production database, protected/customer data, credential, secret, or payload dump.
- No push, PR, merge, deployment, provider enablement, publication, release, evidence-branch mutation, or diagnostic-branch cleanup.

## Required Invariant — D15

For each live `RunAsync` call:

1. await `ICollectiveOutboundAuthorizationGate.IsAuthorizedAsync(cancellationToken)` before `CollectiveSubmitRequest`, `CreateClient(HttpClientName)`, `CollectiveApiClient`, request URI/headers/JSON content, or any send can occur;
2. false consent returns the existing degraded envelope and the handler observes zero calls;
3. missing/invalid current-user state, cancellation during authorization, repository error, or any gate exception returns the existing degraded envelope and the handler observes zero calls;
4. only `true` may proceed into the unchanged submit/poll path; the local 200 success fake observes exactly one initial `POST /api/collective/live-runs` and existing mapping remains intact;
5. `LiveMode`, `ControlBaseUrl`, static Authorization/Bearer values, `CollectiveIntent` identity fields, and caller data never substitute for the server-side gate; and
6. the gate dependency and its consumers are request-scoped; startup DI validation must not report a scoped-to-singleton capture.

The orchestrator may log a sanitized gate failure but must not log objective/payload contents, credentials, user identifiers, or consent records. Do not construct a request merely to dispose it after denial.

## Smallest Production Fix

- In `CollectiveLiveOrchestrator.cs`, declare the narrow authorization interface, inject it, and add one early fail-closed authorization block before the existing submit-request construction. Leave `CollectiveApiClient` and the authorized submit/poll/mapping path intact.
- In `CognitionDependencyInjection.cs`, change only the lifetimes needed to compose the scoped gate safely: live `ICognitiveDensityOrchestrator` and `IStackReviewBoardService` become scoped; translator remains singleton; stub orchestrator may remain singleton because a scoped board can consume it.
- Add `CollectiveOutboundAuthorizationGate.cs` in the API Auth layer as a scoped, direct adapter over existing scoped `IConsentGate`.
- In `Program.cs`, register the adapter as scoped before `AddCollectiveIntegration` is composed. Do not add a project reference or resolve a scoped gate from a singleton/root provider.

If this design cannot compile or validate within AF-P08, stop rather than adding a dependency, changing a project file, or using a service locator.

## Regression Custody and Exact Test Cases

Copy the Q01 regression content from exact commit `83d1235b3a964ddf7130f67900f4dcd3b38dcf42` into its original path. Preserve:

- class `CollectiveOutboundBoundaryInvestigationTests`;
- exact method `RunAsync_without_current_user_authorization_must_not_emit_collective_payload`;
- all synthetic sentinel classifications;
- the named-client factory and non-delegating local handler; and
- the intended zero-request invariant.

Adapt construction only to inject explicit server-side gate fakes, then add exactly two cases to that class. The final application targeted set is exactly three `[Fact]` cases:

1. retained denied-consent reproduction: `false`, degraded envelope, zero handler calls/body;
2. gate/current-user failure: local exception, degraded envelope, zero handler calls/body; and
3. current-consent authorized: `true`, exactly one local fake `POST`, expected path/fields/sentinels, and successful synthetic response mapping.

Update the existing six `CollectiveLiveOrchestratorTests` only so their helper supplies an explicit authorized gate; all six existing behaviors and assertions remain. Update the five opt-in live integration tests only enough to compile with an explicit test-only authorized gate; never run them in P08 because their real endpoint is unauthorized.

Add exactly one `[Fact]` in `CollectiveOutboundAuthorizationIntegrationTests.cs`. Through a local `WebApplicationFactory` configured for live Collective with a non-delegating handler, it must:

1. authenticate one synthetic local user;
2. call the existing stack-review endpoint before consent and observe zero Collective calls;
3. record current consent through the real consent endpoint;
4. call the same stack-review endpoint again and observe exactly one local Collective submit; and
5. prove the adapter used real request-scoped `ConsentGate`/`HttpContextCurrentUserAccessor` composition and the tree performed no external access.

The integration test must not replace `IConsentGate`, `ICurrentUserAccessor`, or the authorization adapter with a fake. Only Collective HTTP is faked locally.

## Step 0 — Restate and Stop

After Amendment 02 ratification and before any edit/import/build/test, a fresh builder must restate and stop for coordinator confirmation:

1. P08 goal, critical risk, exact branch/worktree/base/tree, and Amendment 02 authority;
2. all eight AF-P08 paths and that every other path is forbidden;
3. Q01 evidence commit/file hash, retained method/sentinels/local-handler scenario, and evidence-branch non-merge custody;
4. D15 order: gate before submit-request/client/header/body/send construction; denial/error/authorized sends `0/0/1`; no caller boolean;
5. scoped adapter/orchestrator/board lifetime contract, no project cycle, no root-provider/service-locator scope;
6. exact targeted, adjacent, integration, aggregate, count-delta, scope, ancestry, and clean-tree commands;
7. SG-OUTBOUND, SG-SCOPE, synthetic/local-only boundary, two adversarial plus separate security reviews; and
8. Gate 3 ungranted and no push/PR/merge/deploy/enable/publish/release/diagnostic mutation.

Silence is not confirmation. Any mismatch or loss of Amendment 02 custody stops P08.

## Deterministic Verification

Run only from `C:\Users\clint\.codex\worktrees\biostack-remediation-p08\BioStack` with coordinator-provisioned, hash-verified local restore metadata. Every .NET command uses `--no-restore`. Clear `KEON_COLLECTIVE_CONTROL_URL`, `KEON_COLLECTIVE_BEARER_TOKEN`, and any equivalent Collective credential/config environment variables before every test command. If local assets are missing, report `ENVIRONMENT_BLOCKED`; do not restore or access a registry.

`rtk` was not resolvable during shaping. Raw commands below use the repository debugging fallback; use equivalent `rtk` forms without changing arguments if it is available in the isolated worktree.

### Base receipt before edits

```powershell
git rev-parse HEAD
git show -s --format=%T HEAD
git status --porcelain=v1 --untracked-files=all
git merge-base --is-ancestor 339f259b1a467034db4f57cf9d774c292f11b53a HEAD
git ls-tree -r --name-only 339f259b1a467034db4f57cf9d774c292f11b53a -- backend/src/BioStack.Api/Auth/CollectiveOutboundAuthorizationGate.cs backend/tests/BioStack.Application.Tests/Cognition/CollectiveOutboundBoundaryInvestigationTests.cs backend/tests/BioStack.Api.Tests/Integration/CollectiveOutboundAuthorizationIntegrationTests.cs
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter 'FullyQualifiedName~BioStack.Application.Tests.Cognition.CollectiveLiveOrchestratorTests' --disable-build-servers --logger 'console;verbosity=minimal'
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --disable-build-servers --logger 'console;verbosity=minimal'
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --disable-build-servers --logger 'console;verbosity=minimal'
```

Expected: exact base/tree, clean status, ancestor exit `0`, no lines for the three absent paths, adjacent Collective `6/0/0`, Application `609 passed / 5 skipped / 614 total`, and API `377 passed` with zero failed. A different base/count is a stop for coordinator adjudication.

### Targeted application boundary

```powershell
$env:KEON_COLLECTIVE_CONTROL_URL = ''
$env:KEON_COLLECTIVE_BEARER_TOKEN = ''
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter 'FullyQualifiedName~BioStack.Application.Tests.Cognition.CollectiveOutboundBoundaryInvestigationTests' --disable-build-servers --logger 'console;verbosity=detailed'
```

Expected: exactly three passed, zero failed/skipped; local send counts exactly `0/0/1`. No default handler may be reachable.

### Current-user/consent integration and adjacent suites

```powershell
$env:KEON_COLLECTIVE_CONTROL_URL = ''
$env:KEON_COLLECTIVE_BEARER_TOKEN = ''
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --filter 'FullyQualifiedName~BioStack.Api.Tests.Integration.CollectiveOutboundAuthorizationIntegrationTests' --disable-build-servers --logger 'console;verbosity=detailed'
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter 'FullyQualifiedName~BioStack.Application.Tests.Cognition.CollectiveLiveOrchestratorTests' --disable-build-servers --logger 'console;verbosity=minimal'
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --filter 'FullyQualifiedName~BioStack.Api.Tests.Integration.ConsentGateIntegrationTests|FullyQualifiedName~BioStack.Api.Tests.Integration.StackReviewEndpointsIntegrationTests' --disable-build-servers --logger 'console;verbosity=minimal'
```

Expected: P08 API integration `1/0/0`; existing Collective adjacent `6/0/0`; existing consent plus stack-review adjacent `26/0/0` (`17 + 9`). The integration fake observes zero sends before consent and exactly one after current consent.

### Aggregate, DI, and scope hygiene

```powershell
$env:KEON_COLLECTIVE_CONTROL_URL = ''
$env:KEON_COLLECTIVE_BEARER_TOKEN = ''
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --disable-build-servers --logger 'console;verbosity=minimal'
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --disable-build-servers --logger 'console;verbosity=minimal'
dotnet test backend/BioStack.sln --no-restore --disable-build-servers --verbosity minimal
git diff --check 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff --name-only 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git merge-base --is-ancestor 339f259b1a467034db4f57cf9d774c292f11b53a HEAD
git status --porcelain=v1 --untracked-files=all
```

Expected isolated P08 project totals: Application `612 passed / 5 skipped / 617 total`; API `378 passed` with failed/skipped counts unchanged from base. Full-solution failed/skipped deltas remain unchanged and passed total increases exactly four. Startup/service-provider validation must be green. Changed paths are exactly AF-P08; diff check, ancestry, and status are clean.

## Acceptance Criteria

- Amendment 02 is ratified before dispatch; Step 0 is exact and confirmed.
- Only AF-P08 differs from the pinned base; no project/package/config/network widening occurs.
- The retained Q01 method and synthetic local-handler scenario remain recognizable and turn green because production denies before request construction.
- Consent denial and gate/current-user/repository failure each produce the existing degraded envelope and zero handler calls; current consent produces exactly one local-fake initial submit.
- No caller/config/static credential/intent field acts as authorization.
- Real request-scoped `ConsentGate` plus current-user integration proves `0 -> 1` behavior across consent recording without replacing the gate/accessor/adapter.
- DI validation is green with scoped adapter, live orchestrator, and stack-review board; no singleton captures a scoped dependency and no project-reference cycle/service locator is introduced.
- Exact target/adjacent/aggregate counts, no-network receipt, ancestry, Allowed Files, `git diff --check`, and clean status are green.
- Two fresh adversarial reviews and a separate defensive security review accept the exact candidate, or every finding is fixed and all verification/reviews rerun against a new hash.
- Candidate stays local and unmerged; Gate 3 remains ungranted.

## Evidence Required

The builder handoff must include:

1. exact base/tree, branch/worktree, final candidate commit/tree, sole-parent/ancestry receipt, and clean-start/final status;
2. exact AF-P08 changed-path list, `git diff --check`, absent/new-file receipts, and hashes;
3. Q01 commit/blob/file-hash provenance and confirmation that its evidence branch was not merged or changed;
4. static proof and test evidence that the gate await precedes submit-request/client/API-client/header/body/send construction;
5. denied/error/authorized gate outcomes and local-handler counts `0/0/1`, plus degraded/success envelope outcomes;
6. real current-user/ConsentGate integration receipt with pre-consent zero and post-consent one local send;
7. exact targeted, adjacent, Application, API, and solution commands, exit codes, passed/failed/skipped totals, and expected `+3/+1/+4` deltas;
8. DI lifetime/startup validation and explicit no project-cycle/root-provider/service-locator finding;
9. explicit no-network/no-provider/no-registry/no-protected-data/no-secret receipt identifying the non-delegating handlers and cleared environment variables;
10. residual risks and rollback; and
11. confirmation of no push, PR, merge, deploy, enable, publish, release, or diagnostic/evidence-branch mutation.

## Reviews and Gate 3

- Two fresh, independent, read-only adversarial reviewers inspect the exact candidate commit. Lenses: regression custody, authorization ordering, fail-closed exceptions/cancellation, DI lifetimes, adapter/current-user composition, existing mapping/poll behavior, count receipts, and scope.
- A third fresh read-only defensive security reviewer applies SG-OUTBOUND and SG-SCOPE and attempts authorization bypass through config/static credentials/intent fields, request construction before gating, alternate DI paths, scoped-to-singleton capture, service-locator/root resolution, fake-handler escape, payload logging, and out-of-scope changes.
- Reviewers never edit, fix, stage, or commit. Any candidate change invalidates all prior acceptances and requires the complete deterministic/review chain again.
- Builder green tests and reviewer acceptance do not grant Gate 3. The human must approve the exact merge/release set after goal-level integration is green.

## Collision, Rollback, and Custody

P08 collides with no existing implementation parcel on the pinned base, but its `Program.cs` and Cognition DI edits are integration-sensitive and must be reconciled with any base movement or prior accepted candidate before goal-level verification. P08 branches directly from the pinned main base, never from Q01 or another parcel.

Logical rollback is the one P08 candidate commit after any future authorized merge. Revert restores prior Collective/DI behavior. Q01 commit `83d1235b3a964ddf7130f67900f4dcd3b38dcf42` remains intentionally failing, local, preserved, and unmerged; all original diagnostic branches remain untouched.

## Stop Rules

Stop immediately if Amendment 02 custody is absent; Step 0 is unconfirmed; base/tree/status differs; `origin/main` moved materially; AF-P08 is insufficient; any project/package/config/endpoint/ConsentGate/CollectiveApiClient edit is needed; the retained test is weakened/deleted/renamed; gate order cannot precede all submit construction; a singleton captures a scoped dependency; a project cycle/service locator is proposed; real network/provider/registry/cloud/production/protected data/credentials are needed; count/scope/ancestry/DI/no-network verification fails; a security finding cannot close in AF-P08; the same tripwire recurs twice; or any push/PR/merge/deploy/enable/publish/release/diagnostic mutation is proposed before exact human Gate 3.

## Shaping Boundary

BioStack lacks the Foreman emitter/linter. This docs-only spec creates no `ShapingResult`. Coordinator lint is authoritative; its active status confers only contingent Gate 2 authority and no authority before the clean isolated worktree and confirmed Step 0.
