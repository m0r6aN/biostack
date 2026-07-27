# Parcel: BIO-RT-01

Status: accepted locally after independent security re-review. Live Keon
remains HOLD pending an idempotent governed-operation contract for distributed
commit reconciliation.

## Goal

Make disabled/offline Keon Runtime receipt issuance fail closed while preserving
successful receipt-producing integration coverage through an explicit
test-assembly-only client.

## Initiative

`biostack-production-readiness`

## Project Track

BioStack backend governance and Keon Runtime client boundary.

## Wave

Hardening

## Branch

`codex/biort01-fail-closed-complete-20260727`

## Worktree

`D:/Repos/BioStack-biort01-complete-20260727`

Starting commit: `2807b95f77b9ae8670458a95da62bc486e0f2cf0`

## Dependencies

- KEO-195 / BIO-RT-01 audited test-runtime contract.

## Integration Surfaces

- `biostack-api -> keon-runtime-decision-receipt`
- `biostack-api-tests -> test-only-receipt-client`

## Security Gate

Independent security re-review passed after both caller-level findings were
remediated: explicit shared-context EF transactions prevent unreceipted local
effects, while transcript provider-failure lifecycle evidence is intentionally
committed. Distributed receipt-success/database-commit-failure reconciliation
remains release-blocking for live Keon.

## Allowed Files

- `backend/src/BioStack.Infrastructure/Keon/KeonRuntimeClientStub.cs`
- `backend/src/BioStack.Api/Endpoints/AdminEndpoints.cs`
- `backend/src/BioStack.Api/Endpoints/ProtocolEndpoints.cs`
- `backend/tests/BioStack.Api.Tests/TestKeonRuntimeClient.cs`
- `backend/tests/BioStack.Api.Tests/KeonRuntimeClientStubTests.cs`
- `backend/tests/BioStack.Api.Tests/Unit/Keon/RuntimeReceiptFactoryTests.cs`
- `backend/tests/BioStack.Api.Tests/Unit/Governance/UserFacingIntelligenceGateTests.cs`
- `backend/tests/BioStack.Api.Tests/Integration/AdminSourceLaneGovernanceIntegrationTests.cs`
- `backend/tests/BioStack.Api.Tests/Integration/AdminTranscriptIntakeResolutionIntegrationTests.cs`
- `backend/tests/BioStack.Api.Tests/Integration/IntelligenceSafetyGateIntegrationTests.cs`
- `backend/tests/BioStack.Api.Tests/Integration/StackReviewEndpointsIntegrationTests.cs`
- `backend/tests/BioStack.Api.Tests/Integration/IntelligenceEndpointsIntegrationTests.cs`
- `backend/tests/BioStack.Api.Tests/Integration/AdminKnowledgeSourceIntakeIntegrationTests.cs`
- `backend/tests/BioStack.Api.Tests/Integration/AdminStagedTranscriptCandidateReviewIntegrationTests.cs`
- `backend/tests/BioStack.Api.Tests/Integration/AuthorizationEnforcementMatrixIntegrationTests.cs`
- `docs/INITIATIVES/biostack-production-readiness/PARCELS.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/BIO-RT-01-FAIL-CLOSED-OFFLINE-RECEIPTS.md`
- `research/routing-events/bio-rt-01-fail-closed-offline-receipts-20260727.json`

## Forbidden

- No live Keon Runtime, Control, Collective, MCP Gateway, Azure, database, or
  deployment action.
- No endpoint change outside `AdminEndpoints.cs` and `ProtocolEndpoints.cs`;
  no DTO, persistence schema, production configuration, frontend, or lockfile
  change.
- No test fake may emit a `keon://` authority URI.
- No change to policy-check, health, evidence-gate, or append-after-success
  behavior.
- No push, pull request, merge, or deployment.

## Out of Scope

- Live Runtime API alignment with `keon-systems`.
- An offline/local production receipt format.
- Continuing a production effect after receipt issuance fails.
- Production Runtime configuration or cross-repository integration tests.
- Distributed atomicity after live Runtime succeeds but the local database
  commit fails; this requires a stable operation/idempotency contract.

## Existing Patterns To Follow

- `IKeonRuntimeClient` defines receipt unavailability as an exception requiring
  the caller to halt.
- `KeonRuntimeClient` throws `KeonRuntimeUnavailableException` when live receipt
  issuance fails.
- `RuntimeReceiptFactory` appends to the Governed Spine only after issuance
  succeeds.

## Contract

When `KeonRuntime:LiveMode` is false, production resolves
`KeonRuntimeClientStub`, and `IssueReceiptAsync` always faults with
`KeonRuntimeUnavailableException`, including when `StubAllowAll=true`.

`TestKeonRuntimeClient` exists only in `BioStack.Api.Tests`, emits
`urn:biostack:test-receipt:*`, and is injected only into the eight audited
integration factories whose successful flows require receipt issuance.

Every receipted business mutation runs inside one explicit EF transaction on
the request-scoped `BioStackDbContext`. Services may call `SaveChangesAsync`
inside that transaction, but the endpoint commits only after Runtime issuance
and the Spine append both succeed. Any exception disposes the transaction
without commit and rolls back all local business and Spine writes.

## Receipt-Call Inventory

| Caller | Direct receipt calls | Pre-receipt behavior | Atomicity action |
|---|---:|---|---|
| `AdminEndpoints` | 5 | Intake create; transcript status and stage; review transition; canonical promotion | Shared-context transaction around each full sequence |
| `ProtocolEndpoints` | 1 | Protocol-review completion event | Shared-context transaction |
| `IntelligenceEndpoints` | 1 | Graph/intelligence reads only | Proven non-mutating; receipt/Spine is the only write |
| `StackReviewEndpoints` | 1 | In-memory commentary/deliberation only | Proven non-mutating; receipt/Spine is the only write |
| `UserFacingIntelligenceGate` | 1 | Policy/safety evaluation only | Proven non-mutating; optional receipt/Spine is the only write |

`RuntimeReceiptFactory` is the sole direct production caller of
`IKeonRuntimeClient.IssueReceiptAsync`. Its existing ordering remains Runtime
issuance first, Spine append second.

## Required Tests

- Offline receipt issuance fails closed with no `keon://` claim.
- Disabled production registration resolves `KeonRuntimeClientStub`, never the
  test fake.
- Failed issuance produces no Governed Spine append.
- Receipt failure rolls back intake, transcript resolution/staging, staged
  review, canonical promotion, and protocol-review business state.
- One successful Runtime issuance followed by a test Spine append failure rolls
  back both business state and the local Spine.
- Successful counterparts persist both the business mutation and the expected
  Spine receipt.
- Test receipt issuance preserves actor, tenant, policy, evidence, class, and
  effect-status fields.
- All eight receipt-producing integration factories pass.
- Complete `BioStack.Api.Tests` passes.

## Acceptance Criteria

- The production stub cannot return a receipt.
- `StubAllowAll` grants policy/evidence test behavior only, never receipt
  authority.
- No test-only client exists under a production source path.
- Test-issued identifiers use `urn:biostack:test-receipt:*`.
- All mutating receipt paths commit only after receipt issuance and Spine append.
- Fresh verification scopes observe no business or Spine mutation after the
  injected failure cases.
- The complete API suite is green.
- Final diff is limited to Allowed Files.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --filter FullyQualifiedName~KeonRuntimeClientStubTests --disable-build-servers
rtk proxy dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --filter FullyQualifiedName~RuntimeReceiptFactoryTests --no-build --disable-build-servers --verbosity minimal
rtk test dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --disable-build-servers
rtk proxy rg -n "TestKeonRuntimeClient" backend/src
rtk proxy rg -n "keon://receipt/stub-" backend
rtk git diff --check
rtk git status --short
```

## Verification Results

- `KeonRuntimeClientStubTests`: passed, including disabled-registration
  production-type regression.
- `RuntimeReceiptFactoryTests`: 5 passed, 0 failed, 0 skipped.
- Atomicity-focused integration suites: 78 passed, 0 failed, 0 skipped.
- Complete `BioStack.Api.Tests`: 307 passed, 0 failed, 0 skipped.
- The eight audited integration factories are the only factories that register
  `TestKeonRuntimeClient`.
- No live Keon capability was enabled or called.
- Fresh verification scopes prove zero business mutation and zero Spine entry
  for receipt failures, including failure on the second transcript receipt.
- A representative Spine append failure after successful test receipt issuance
  also rolls back the intake mutation and Spine.

## Security-Focused Review

- The production offline path returns no receipt value and no authority URI.
- `StubAllowAll=true` cannot bypass receipt unavailability.
- Test-only successful issuance uses a non-authoritative URN namespace.
- `RuntimeReceiptFactory` still appends only after issuance succeeds.
- Immediate finding remediated: the five mutating receipt sequences share the
  endpoint's explicit EF transaction, so inner `SaveChangesAsync` calls do not
  become durable until receipt issuance and Spine append succeed.
- Provider-failure lifecycle evidence commits the intake's `failed` status and
  failure reason without emitting a resolution receipt. The separate successful
  resolution/staging path remains receipt-gated and atomic.
- Coverage gap: a live Runtime receipt can still succeed before the subsequent
  database commit fails. Correct reconciliation requires stable operation IDs,
  idempotent issuance/retrieval, and authorization/completion semantics.
- Final local `qwen2.5-coder:14b` review: PASS on the corrected security-relevant
  staged diff.
- Final independent security re-review: PASS. It reconfirmed five transactions,
  six commits (including the intentional provider-failure evidence commit), all
  nine receipt callers, rollback/success coverage, and test-runtime isolation.

## Evidence Required

- Complete test output above.
- Final scoped diff/status and no-production-artifact searches.
- Routing event:
  `research/routing-events/bio-rt-01-fail-closed-offline-receipts-20260727.json`.
- Local commit; no remote publication.

## Collision Risk

High for the shared parcel index and the eight integration harnesses. This
parcel was implemented in an isolated worktree from current `origin/main`.

## Session Handoff

- Starting commit: `2807b95f77b9ae8670458a95da62bc486e0f2cf0`
- Ending commit: this parcel's local commit; exact hash is recorded in the
  coordinator handoff because a commit cannot embed its own hash.
- Tests passed: 307 complete API tests.
- Tests failed: none in the final run.
- Decisions needed: ratify the later distributed operation/idempotency contract
  before enabling live Keon.
- Blockers: live Keon remains HOLD.
- Next safe action: coordinator verification of the local commit before any
  remote publication.
- Do not touch: live Keon, deployment configuration, other repositories, or
  production data.

## Stop-and-Report Rule

Stop if a caller must continue after receipt issuance fails, if a production
offline audit artifact becomes required, or if the live Runtime contract must
change.
