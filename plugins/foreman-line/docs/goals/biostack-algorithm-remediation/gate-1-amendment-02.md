# Narrow Gate 1 Amendment 02 — Collective Outbound Authorization

**Status:** RATIFIED 2026-09-02 — coordinator lint passed

**Scope:** D3, D4, D11, D12, new D15, AF-P08, and contingent Gate 2 authority for P08 only

**Trigger:** accepted Q01 evidence commit `83d1235b3a964ddf7130f67900f4dcd3b38dcf42` and its preserved transcript at `C:\Users\clint\.codex\evidence\biostack-algorithm-remediation\q01-collective-outbound-2026-09-02.transcript.txt`

Q01 deterministically reproduced `REPRODUCED_CONFIG_ONLY_TRANSMISSION`: with live mode enabled, no current-user authorization/consent input, and no static Authorization header, the production `CollectiveLiveOrchestrator.RunAsync` path emitted exactly one local-handler `POST /api/collective/live-runs`. The synthetic objective, tenant, actor, correlation, and intent sentinels crossed the adapter boundary. The handler never delegated to the network. The evidence does not determine endpoint authentication or first-party/third-party processor status.

Gate 1 Amendment 01 was ratified concurrently. All other ratified decisions and the existing contingent Gate 2 authority for Q01-Q04 and P01-P07 remain unchanged. Gate 3 remains ungranted.

## D3 replacement — eight implementation parcels

Replace D3 with:

> **D3 — Eight implementation parcels.** The 17 originally reproduced failures plus the independently reproduced Q01 Collective outbound defect are owned by P01-P08. Parcels sharing a production file are combined or serialized; no two live builders may edit the same file.

## D4 replacement — Q01 adjudicated

Replace D4 with:

> **D4 — Inconclusive claims stay diagnostic until adjudicated.** Q01 reproduced a new Collective outbound defect and is preserved as unmerged evidence at commit `83d1235b3a964ddf7130f67900f4dcd3b38dcf42`; only separately authorized P08 may remediate it. Q02 endpoint-wide gate coverage and Q03 regex exploitability remain diagnostic lanes with test/evidence-only authority and no production writes. Q04 is adjudicated `READY` for dependency/test-environment purposes only and does not itself authorize P07. Any further reproduced new defect requires its own charter amendment and narrowly reopened Gate 1 before remediation.

## D11 replacement — P08 review depth

Replace D11 with:

> **D11 — Review depth.** Every parcel receives a fresh independent adversarial review. P02, P04, P05, P06, P07, and P08 are high-risk boundary parcels and each receives two independent adversarial reviews plus a separate defensive security review. Reviewers are read-only and never fix or commit.

## D12 replacement — P08 dispatch authority

Replace D12 with:

> **D12 — Gate 2 scope.** Standing Gate 2 authorization covers Q01-Q04 and P01-P07 under the original ratification, and P08 only after Amendment 02 is explicitly ratified. Each parcel may dispatch only after its shaped spec exactly matches the governing charter and ratified amendments, names an isolated worktree/branch, passes Step 0 restatement, and preserves its exact Allowed Files. Any amendment, missing file, new dependency, lifetime mismatch, or scope widening stops the affected parcel.

## D15 addition — Collective current-user outbound authorization

Add D15:

> **D15 — Collective current-user outbound authorization.** Every live `CollectiveLiveOrchestrator.RunAsync` invocation must obtain the current authenticated user's current server-side consent through the existing `IConsentGate` before constructing a `CollectiveSubmitRequest`, borrowing the named `HttpClient`, constructing a `CollectiveApiClient`, materializing request headers or JSON, or sending to Collective. Consent denial, missing/invalid current-user state, cancellation during the gate, or any gate/repository failure is fail-closed: return the existing degraded envelope and perform zero outbound calls. Only an affirmative current-consent result may enter the existing submit/poll operation, and the authorized local-fake path emits exactly one initial submit. Live configuration, static provider credentials, intent tenant/actor fields, and caller-supplied booleans are not user authorization. The Cognition project owns the authorization interface and live-orchestrator check; the API owns a scoped adapter to `IConsentGate` and composition. `ICognitiveDensityOrchestrator`, `IStackReviewBoardService`, the adapter, and their consumers must have compatible lifetimes; no singleton may capture a scoped consent/current-user/repository dependency, and no project-reference cycle or service-locator scope may be introduced.

The authorization decision is taken once at the start of each `RunAsync` operation. P08 does not change Collective payload shape, static provider authentication, tenant/actor mapping, polling behavior after an authorized submit, endpoint authentication, consent version/copy, or the status of Collective as a processor.

## Production owner

- Primary boundary owner: `BioStack.Cognition.CollectiveApi.CollectiveLiveOrchestrator.RunAsync`.
- Composition/lifetime owner: `BioStack.Cognition.CognitionServiceCollectionExtensions`.
- Current-user consent bridge owner: a new API `CollectiveOutboundAuthorizationGate` that delegates to the existing scoped `IConsentGate`.
- Composition root owner: `BioStack.Api/Program.cs`.
- `CollectiveApiClient` remains the unchanged downstream request constructor/sender; it is not an authorization owner and is outside AF-P08.

## Exact Allowed Files — AF-P08

Only these eight repository paths may change on P08:

- `backend/src/BioStack.Cognition/CollectiveApi/CollectiveLiveOrchestrator.cs`
- `backend/src/BioStack.Cognition/CognitionDependencyInjection.cs`
- `backend/src/BioStack.Api/Auth/CollectiveOutboundAuthorizationGate.cs`
- `backend/src/BioStack.Api/Program.cs`
- `backend/tests/BioStack.Application.Tests/Cognition/CollectiveOutboundBoundaryInvestigationTests.cs`
- `backend/tests/BioStack.Application.Tests/Cognition/CollectiveLiveOrchestratorTests.cs`
- `backend/tests/BioStack.Application.Tests/Cognition/CollectiveLiveIntegrationTests.cs`
- `backend/tests/BioStack.Api.Tests/Integration/CollectiveOutboundAuthorizationIntegrationTests.cs`

The two new paths are the API adapter and the API integration test. Project files, package/lock files, `ConsentGate.cs`, `CollectiveApiClient.cs`, options/models, endpoints, configuration files, and all other paths are forbidden. If these files are insufficient, P08 stops for another narrow amendment.

## Required regression and adjacent verification

P08 must import the Q01 evidence test from exact commit `83d1235b3a964ddf7130f67900f4dcd3b38dcf42`, preserve the class, synthetic sentinels, local non-delegating handler, and exact retained method `RunAsync_without_current_user_authorization_must_not_emit_collective_payload`, and turn that intended failure green through the production gate. The application targeted set contains exactly three cases:

1. current-user consent denied: retained reproduction, zero handler calls;
2. consent-gate/current-user failure: zero handler calls; and
3. current authenticated user with current consent: exactly one local-fake submit and unchanged successful response mapping.

One new API integration case must exercise the real scoped `ConsentGate` and `HttpContextCurrentUserAccessor` through an authenticated local `WebApplicationFactory` session: zero Collective handler calls before consent, then exactly one local-fake submit after recording current consent. It must use a synthetic `.invalid` base URI with a non-delegating local handler and must never contact a provider.

Adjacent verification retains:

- six `CollectiveLiveOrchestratorTests` cases, updated only with explicit authorized test gates so their pre-existing mapping/degradation assertions remain intact;
- five opt-in `CollectiveLiveIntegrationTests` compiling with an explicit test-only authorized gate but remaining skipped because Collective environment variables are cleared;
- seventeen `ConsentGateIntegrationTests` cases;
- nine `StackReviewEndpointsIntegrationTests` cases; and
- full offline Application, API, and solution suites.

On the pinned base, the application project receipt is 609 passed, 5 skipped, 614 total; P08 adds exactly three passing application cases, so the isolated P08 candidate expects 612 passed, 5 skipped, 617 total. The API suite receipt is 377 passed on the pinned base; P08 adds exactly one API integration case, so the isolated candidate expects 378 passed with failed/skipped counts unchanged. A changed base or different discovered count stops for coordinator adjudication rather than silently changing this contract.

## Security gates and verification controls

- **SG-OUTBOUND:** the gate is awaited before submit request/client/header/body/send construction; false/error paths observe `0/0` sends and the affirmative current-consent path observes exactly `1` local-fake submit. No configuration flag, static credential, intent field, or caller boolean can bypass the gate.
- **SG-SCOPE:** only AF-P08 changes; synthetic fixtures only; no credentials, protected/customer data, payload dumps, registry, DNS/socket, provider, cloud, production database, or other network access.
- DI validation must prove no singleton captures `IConsentGate`, `ICurrentUserAccessor`, `IAppUserRepository`, or `BioStackDbContext`. The scoped authorization adapter delegates directly to scoped `IConsentGate`; the live orchestrator and `IStackReviewBoardService` are scoped, while the translator may remain singleton. No Cognition-to-Application project reference is added.
- Verification runs with `--no-restore`, cleared Collective environment variables, local fakes, exact count deltas, base ancestry, `git diff --check`, exact Allowed Files, and a clean final worktree.
- The exact candidate receives two fresh independent adversarial reviews plus a separate defensive SG-OUTBOUND/SG-SCOPE security review. Any rework invalidates prior reviews.

## Risk and rollback

Primary risk is fail-open transmission of objective and identity material. Secondary risks are an availability-only fail-closed result when no authenticated request context exists, DI startup failure from incompatible lifetimes, and accidental weakening of existing Collective mapping/degraded behavior. P08 does not claim to bind outbound tenant/actor values to the authenticated user or to re-check consent during polling after an authorized submit; those are explicit residual boundaries, not authority to widen this parcel.

Rollback is the single isolated P08 candidate commit after any later Gate 3 merge. Reverting it restores the prior live-orchestrator/DI behavior while the Q01 evidence branch and diagnostic branches remain preserved and unmerged. No provider/config rollout or rollback is authorized.

## Gate 2 request and authority boundary

Ratification of this amendment grants only contingent Gate 2 dispatch authority for P08 after coordinator lint, an isolated worktree/branch at the pinned base, exact AF-P08 verification, and confirmed Step 0. It does not authorize a push, pull request, merge, deployment, provider enablement, publication, release, external access, or diagnostic-branch mutation.

Gate 3 remains human-only and ungranted. P08 may be proposed for Gate 3 only after its full deterministic chain, two fresh adversarial reviews, separate security review, and goal-level aggregate integration are green.

## Exact ratification form

> Narrow Gate 1 Amendment 02: I ratify the D3, D4, D11, and D12 replacements, add D15, approve AF-P08 and P08 as written, and grant contingent Gate 2 dispatch authorization for P08. Amendment 01 remains separately pending; all other decisions and existing authorizations remain unchanged. Gate 3 remains ungranted.

If Amendment 01 is ratified in the same human message, use the Amendment 01 exact form followed by this non-conflicting concurrent form:

> Narrow Gate 1 Amendment 02: I ratify the D3, D4, D11, and D12 replacements, add D15, approve AF-P08 and P08 as written, and grant contingent Gate 2 dispatch authorization for P08. Amendment 01 is concurrently ratified by the preceding statement; all other decisions and existing authorizations remain unchanged. Gate 3 remains ungranted.

## Human receipt

The developer supplied the concurrent ratification form above on 2026-09-02. D3, D4, D11, and D12 are replaced; D15, AF-P08, and P08 are approved; and contingent Gate 2 dispatch authority now includes P08. Gate 3 remains ungranted.
