# Inquiry Parcel Q01: Collective Outbound Behavior

Status: **active — coordinator lint passed; dispatch remains contingent on exact worktree/base verification and Step 0 confirmation**

Coordinator lint: **PASSED 2026-09-02**. The pinned project/blobs, six-case adjacent count, internal-access seam, local-handler request path, and AF-Q01 absence were independently rechecked. Any executable outcome other than an environment/scope stop must preserve the single test in one local evidence-only commit for exact review; no production remediation is authorized.

## Identity

- Goal: `biostack-algorithm-remediation`
- Project: `BioStack` / `BioStack.Cognition`
- Parcel: `Q01-collective-outbound-behavior`
- Wave: Wave Q — inconclusive-claim adjudication
- Routing: fresh frontier evidence investigator
- Risk: high outbound-data boundary risk; test/evidence authority only
- Branch: `codex/biostack-remediation-q01`
- Isolated worktree: `C:\Users\clint\.codex\worktrees\biostack-remediation-q01\BioStack`
- Required starting commit: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Required starting tree: `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`

## Goal and Decision Boundary

Adjudicate only the currently inconclusive claim that the live Keon Collective adapter can transmit compound/goal data when live configuration is enabled without a caller-specific authorization or consent decision reaching the outbound boundary. Exercise the production `CollectiveLiveOrchestrator.RunAsync` path with one synthetic intent and one local intercepting `HttpMessageHandler`, capture and classify the exact first outbound request, and preserve the result as evidence.

Q01 does not remediate the behavior. A reproduced config-only transmission stops this parcel and requires a new production owner, invariant, exact Allowed Files, security gate, and narrowly reopened Gate 1 before any fix. Absence of a request is not by itself proof of a consent control: a `NOT_REPRODUCED` result must identify the concrete production gate that denied the call. Gate 3 is ungranted.

## Authority and Preconditions

1. D4 authorizes Q01 as a test/evidence-only inquiry. The original diagnostic parcel could not use its named `BioStack.Cognition.Tests` target because that project does not exist; no behavior conclusion may be carried forward from that missing target.
2. D10 and SG-OUTBOUND require synthetic fixtures and a local interceptor. No DNS lookup, socket, Collective host, Keon Control instance, cloud resource, production database, protected data, user data, credential, or secret is authorized.
3. Gate 2 is contingent on coordinator lint, an isolated branch/worktree at the exact base/tree, exact Allowed Files, a clean start, and an explicitly confirmed Step 0 restatement.
4. Pending Gate 1 Amendment 01 does not change D4 or Q01. It does not authorize a Collective production fix.
5. The coordinator owns any `origin/main` comparison and any ignored .NET restore-metadata seed. The investigator must not fetch, pull, restore, or contact a package registry. Missing local assets are `ENVIRONMENT_BLOCKED`.

## Production Ownership

No production file is owned or writable by Q01. The observed boundary is owned by `CollectiveLiveOrchestrator` and its `CollectiveApiClient` in `BioStack.Cognition`, but identifying that owner is evidence, not write authority.

## Exact Allowed Files and Effects — AF-Q01

Exactly one repository path may be created or modified:

- `backend/tests/BioStack.Application.Tests/Cognition/CollectiveOutboundBoundaryInvestigationTests.cs`

Permitted non-repository effects are limited to ordinary ignored .NET build artifacts and a sanitized transcript outside every Git worktree at:

- `C:\Users\clint\.codex\evidence\biostack-algorithm-remediation\q01-collective-outbound-2026-09-02.transcript.txt`

No production, configuration, project, package, lock, fixture, snapshot, goal, handoff, or other test file may change. If the one test file cannot compile against the existing project and internal-access contract, stop; do not edit a project file or substitute a nearby test project.

After the required receipts, an executable inquiry outcome must create exactly one local test-only commit containing only AF-Q01. Environment- or scope-blocked outcomes create no commit. The evidence commit is never pushed or merged and cannot be treated as a remediation candidate.

## Pinned-Base Facts

These facts were inspected at the execution base and must be reverified before dispatch:

- `BioStack.Application.Tests.csproj` references `BioStack.Cognition`, includes ordinary `.cs` files by SDK convention, includes xUnit and Moq, and has blob `d83d0fea607573adec124c8e4417c768ba71aaa0`.
- `BioStack.Cognition.csproj` grants `InternalsVisibleTo` to `BioStack.Application.Tests`; no project edit is required.
- `CollectiveLiveOrchestrator.cs` has blob `b3a7f2fb6f7e731ef7a435f599be1716be158f40`. `RunAsync` creates a submit request from the intent, obtains named client `keon-collective`, and submits before mapping or degrading.
- `CollectiveApiClient.cs` has blob `efe37ffc154574be1ce064e2dddcf2af0494dcdf`. Its first request is `POST /api/collective/live-runs`; it adds tenant, actor, and correlation headers and sends JSON through the injected client.
- `CollectiveApiModels.cs` has blob `adc4b5f6afbad8166fc30ce4e3ff9edcf7220478`. The submit body names `objective`, `tenantId`, `actorId`, `actorType`, `correlationId`, `context`, and `intentId`.
- `CollectiveApiOptions.cs` has blob `7706fdaa31fca1b334747771a48635397adcae4f`; the pinned options expose live/configuration and static authorization-header values, not a current-user consent decision.
- `StackDeliberationTranslator.cs` has blob `39ef0462454c89e2c42bbb4a98f48ad159c5579f`; it is read-only context for why `intent.Goal` may contain compound/goal material. Q01 uses only synthetic strings and does not exercise an API endpoint.
- Existing `CollectiveLiveOrchestratorTests.cs` has blob `5a0df07028b897759f84dbbd7d264cb520c6ccad` and exactly six `[Fact]` cases. It demonstrates the local-handler pattern but does not adjudicate caller-specific authorization or consent.
- The AF-Q01 test path is absent at the pinned base. Therefore its exact base target count is `0`; the final target count is exactly `1` (`+1`).

## Exact Inquiry Contract

Create one xUnit `[Fact]` named:

`RunAsync_without_current_user_authorization_must_not_emit_collective_payload`

The single test must:

1. construct a `CollectiveApiOptions` instance with `LiveMode = true`, a synthetic `.invalid` base URL, zero poll delay, and both `AuthorizationHeader` and `BearerToken` absent;
2. construct a fully synthetic `CollectiveIntent` whose objective contains unmistakable sentinel compound and user-goal strings, plus synthetic tenant, actor, correlation, and intent identifiers;
3. use a test-local `HttpMessageHandler` that overrides `SendAsync`, records the method, resolved URI/path, headers, and UTF-8 JSON body before returning a complete synthetic HTTP 200 Collective response; the fake must never delegate to a default handler;
4. use a fake or mock `IHttpClientFactory` that returns only that handler-backed client for `CollectiveLiveOrchestrator.HttpClientName`;
5. invoke the real `CollectiveLiveOrchestrator.RunAsync` exactly once;
6. if a request is observed, parse its JSON with `JsonDocument` and record only the synthetic classification: request count, method, path, sorted field names, whether the synthetic objective/tenant/actor/correlation/intent sentinels crossed the boundary, whether `context` is null, whether an Authorization header exists, and the three identity-header values;
7. assert the intended invariant last: without a current-user authorization/consent decision, the local handler must observe zero requests and no body; and
8. never assert that the API endpoint is unauthenticated. This test proves only what authority reaches the Collective outbound adapter. API authentication and first-party/third-party processor status remain separate questions.

The test must not add or imagine a production option such as `UserDataTransmissionAuthorized`; it adjudicates the pinned behavior. Do not add a second theory row, endpoint test, source-text search, reflection-only claim, real integration test, or credential-bearing case.

## Outcome Taxonomy

Exactly one disposition is recorded:

- `REPRODUCED_CONFIG_ONLY_TRANSMISSION`: the local handler observes exactly one `POST /api/collective/live-runs` with the expected synthetic body/identity fields while no current-user authorization or consent decision was supplied. The final invariant assertion fails for that reason. Stop and request a narrow Gate 1 amendment; do not fix.
- `NOT_REPRODUCED_FAIL_CLOSED`: the local handler observes zero requests, the test passes, and the evidence identifies the concrete production gate that denied before request construction. A thrown exception or degraded response without an identified gate is not sufficient.
- `INCONCLUSIVE_UNEXPECTED_RUNTIME_BEHAVIOR`: the local fake is reached with a different method/path/count/body, or execution terminates in a production path that cannot be separated from the claim. Preserve exact sanitized evidence; do not infer a defect.
- `ENVIRONMENT_BLOCKED`: compilation, collection, SDK, or local package/restore metadata prevents execution before the production path is reached. Do not restore or go online.
- `SCOPE_OR_NETWORK_BLOCKED`: any out-of-scope diff, real network attempt, secret/protected-data need, or unsafe transcript condition occurs. Stop immediately.

No other label is permitted. In particular, configuration being dormant by default is not `NOT_REPRODUCED`, and an absent authorization header alone does not prove a missing consent control.

## Deterministic Verification

Run from the Q01 worktree root with already-provisioned local restore metadata. `--no-restore` is mandatory.

`rtk` was not resolvable in the shaping environment. The raw commands below therefore use the repository AGENTS.md debugging fallback; if `rtk` is available in the investigator worktree, the corresponding `rtk` form may be used without changing arguments or evidence.

### Baseline before creating the test

```powershell
$env:KEON_COLLECTIVE_CONTROL_URL = ''
$env:KEON_COLLECTIVE_BEARER_TOKEN = ''
git rev-parse HEAD
git show -s --format=%T HEAD
git status --porcelain=v1 --untracked-files=all
git cat-file -e 339f259b1a467034db4f57cf9d774c292f11b53a:backend/tests/BioStack.Application.Tests/Cognition/CollectiveOutboundBoundaryInvestigationTests.cs
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.Cognition.CollectiveLiveOrchestratorTests" --disable-build-servers --logger "console;verbosity=minimal"
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --disable-build-servers --logger "console;verbosity=minimal"
```

Expected static receipt: exact base commit/tree, empty status, `git cat-file -e` exit `128`, and six adjacent tests passed with zero failed/skipped. Record the full-project baseline summary as `B_app`. Any different adjacent count or failed baseline is a stop, not Q01 evidence.

### Targeted inquiry

```powershell
$env:KEON_COLLECTIVE_CONTROL_URL = ''
$env:KEON_COLLECTIVE_BEARER_TOKEN = ''
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.Cognition.CollectiveOutboundBoundaryInvestigationTests" --disable-build-servers --logger "console;verbosity=detailed"
```

Expected discovery is exactly one test. A reproduced result is exit nonzero with exactly one intended assertion failure after the handler prints the sanitized request classification. A not-reproduced result is one passed, zero failed/skipped and requires the concrete denying gate described above. Compilation, collection, fake-handler, or response-deserialization failure is inconclusive/environmental, not reproduced.

### Adjacent and aggregate

```powershell
$env:KEON_COLLECTIVE_CONTROL_URL = ''
$env:KEON_COLLECTIVE_BEARER_TOKEN = ''
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.Cognition.CollectiveLiveOrchestratorTests" --disable-build-servers --logger "console;verbosity=minimal"
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --disable-build-servers --logger "console;verbosity=minimal"
git diff --check 339f259b1a467034db4f57cf9d774c292f11b53a
git diff --name-only 339f259b1a467034db4f57cf9d774c292f11b53a
git status --porcelain=v1 --untracked-files=all
```

Adjacent remains exactly six passed. Relative to `B_app`, the project discovers exactly one additional test. If Q01 is reproduced, the aggregate has exactly that one intended failure and no other failure; if not reproduced, totals/passed increase by one and failure/skip counts remain unchanged. The changed-path list contains exactly AF-Q01 and `git diff --check` is clean.

The coordinator later reruns the goal-level offline matrix. Q01 does not make a reproduced test green and is never merged as remediation evidence.

## Evidence and Handoff

The investigator sends a concise handoff in this exact order:

1. `Parcel / outcome`: Q01 and one authorized outcome token.
2. `Branch / worktree / base`: exact branch, path, starting/ending commit and tree, ancestry, clean-start receipt.
3. `Allowed Files`: base absence, final changed-path list, test-file hash, `git diff --check`, final status.
4. `Runtime path`: named client, method, path, request count, synthetic response result.
5. `Payload classification`: sorted JSON field names and sentinel-crossing booleans; no real payload or secret.
6. `Authority classification`: static auth-header presence, current-user authorization/consent input observed at the adapter, and explicit non-conclusion about endpoint authentication/processor status.
7. `Test receipts`: baseline six-case adjacent result, `B_app`, targeted result, final adjacent and aggregate deltas.
8. `Network boundary`: handler never delegated; no DNS/socket/provider/cloud/production/protected-data access.
9. `Finding / residual uncertainty / decision requested`: exact items; a reproduced result requests narrow Gate 1 reopening and no implementation.
10. `Authority close`: test/evidence only; no production change; not pushed, merged, deployed, enabled, published, or released; Gate 3 ungranted; diagnostic branches untouched.

For an executable inquiry outcome, the handoff also names the exact local evidence commit and proves it has the pinned base as its sole parent, contains only AF-Q01, and leaves the worktree clean. A blocked outcome records commit `none`.

## Review and Custody

A fresh read-only adversarial reviewer examines the exact Q01 branch/diff, transcript, synthetic-data boundary, handler implementation, captured classification, test failure/pass reason, and outcome token. The reviewer does not edit, rerun with network, fix, stage, or commit. Any reproduced outcome remains an unmerged diagnostic branch pending a separately ratified remediation. Not-reproduced, inconclusive, or blocked outcomes remain evidence-only. The Q01 branch and outcome are recorded at Stage F and are not deleted without explicit cleanup authority.

## Step 0 — Restate and Stop

Before creating the test, the investigator must restate and then stop for coordinator confirmation:

1. Q01's test/evidence-only goal, risk, exact branch/worktree/base/tree, and AF-Q01;
2. the one exact test name and local-handler production path;
3. the synthetic options/intent, expected method/path/body-field classification, and zero-request intended invariant;
4. exact baseline/target/adjacent/aggregate commands and count contract (`0 -> 1`, adjacent `6 -> 6`, project `+1`);
5. outcome taxonomy and the rule that reproduction stops for a narrow Gate 1 amendment without a fix;
6. no network/provider/cloud/production/protected-data/secret/payload-dump authority and no inference from absence;
7. fresh read-only review, outcome-specific branch custody, Gate 3 ungranted, and no push/PR/merge/deploy/enable/publish/release/diagnostic mutation.

Silence is not confirmation. Any mismatch stops Q01.

## Stop Rules

Stop without widening scope if Step 0 is unconfirmed; base/tree/status or a pinned blob differs; the Allowed File is insufficient; another repository path changes; a second test or production edit is proposed; the fake could delegate to real HTTP; local assets are missing; a real credential/data/provider/cloud resource is requested; the result cannot be separated from a harness failure; `origin/main` was reported materially moved; or any push, PR, merge, deployment, provider enablement, publication, release, or diagnostic-branch mutation is proposed.

BioStack lacks the native Foreman shaping emitter/linter, so this draft intentionally creates no `ShapingResult`. Coordinator lint remains authoritative.
