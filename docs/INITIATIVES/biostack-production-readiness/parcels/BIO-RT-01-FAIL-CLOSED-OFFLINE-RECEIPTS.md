# Parcel: BIO-RT-01

Status: implementation and focused verification complete; publish blocked by
integration-test harness reliance on the removed synthetic receipt behavior.

## Goal

Make offline Keon Runtime receipt issuance fail closed so BioStack cannot emit a
synthetic URI or local record that could be mistaken for a Keon-issued,
retrievable Decision Receipt.

## Initiative

`biostack-production-readiness`

## Project Track

BioStack backend governance and Keon Runtime client boundary.

## Wave

Hardening

## Branch

`codex/biort01-fail-closed-receipts-20260726`

## Worktree

`D:\Repos\BioStack-biort01-fail-closed-receipts-20260726`

The worktree is a sparse checkout rooted at
`53ed0df5a0c207e99b6b3582d6c40b64e6b4f11c`.

## Dependencies

- None. The existing `IKeonRuntimeClient.IssueReceiptAsync` contract already
  requires `KeonRuntimeUnavailableException` when Keon cannot issue a receipt.

## Integration Surfaces

- `biostack-api -> keon-runtime-decision-receipt`

## Security Gate

Security review required before merge because this parcel changes the
fail-closed behavior of an audit/receipt authority boundary.

## Allowed Files

- `backend/src/BioStack.Infrastructure/Keon/KeonRuntimeClientStub.cs`
- `backend/tests/BioStack.Api.Tests/KeonRuntimeClientStubTests.cs`
- `backend/tests/BioStack.Api.Tests/Unit/Keon/RuntimeReceiptFactoryTests.cs`
- `docs/INITIATIVES/biostack-production-readiness/parcels/BIO-RT-01-FAIL-CLOSED-OFFLINE-RECEIPTS.md`
- `research/routing-events/bio-rt-01-fail-closed-offline-receipts-20260726.json`

## Forbidden

- No live Keon Runtime, Control, Collective, MCP Gateway, Azure, database, or
  deployment action.
- No endpoint, DTO, persistence schema, production configuration, frontend, or
  lockfile change.
- No local artifact may use a `keon://` URI or claim Keon receipt authority.
- No change to the existing policy-check or evidence-gate fail-closed behavior.
- No files changed by pull request #230.
- No commit, push, pull request, merge, or deployment in this parcel session.

## Out of Scope

- Aligning BioStack's live Runtime API contract with `keon-systems`.
- Introducing an explicitly local receipt format or offline audit ledger.
- Changing callers to continue an effect after authoritative receipt issuance
  fails.
- Production Runtime configuration or cross-repository integration testing.

## Existing Patterns To Follow

- `backend/src/BioStack.Infrastructure/Keon/IKeonRuntimeClient.cs` defines
  receipt unavailability as an exception that requires the caller to halt.
- `backend/src/BioStack.Infrastructure/Keon/KeonRuntimeClient.cs` throws
  `KeonRuntimeUnavailableException` when live receipt issuance fails.
- `backend/src/BioStack.Infrastructure/Keon/RuntimeReceiptFactory.cs` appends to
  the Governed Spine only after Keon returns a receipt.

## Contract

When `KeonRuntime:LiveMode` is false:

```text
IKeonRuntimeClient.IssueReceiptAsync(request)
  -> faulted Task<DecisionReceipt>
  -> KeonRuntimeUnavailableException
  -> no DecisionReceipt value
  -> no keon:// URI
  -> no Governed Spine append
```

`StubAllowAll` remains a development override for policy and evidence checks
only. It cannot grant receipt authority.

## Required Tests

- The offline stub throws `KeonRuntimeUnavailableException` even when
  `StubAllowAll=true`.
- The exception contains no `keon://` authority URI.
- `RuntimeReceiptFactory` appends no Spine entry when the offline client rejects
  issuance.
- Existing offline policy and evidence gate tests remain passing.

## Acceptance Criteria

- No `keon://receipt/stub-*` value can be returned by the offline client.
- No local Spine entry can be persisted after offline issuance fails.
- No returned value or error claims that Keon issued a receipt.
- Focused stub and factory suites pass with zero failures.
- Only Allowed Files appear in the final diff.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --filter FullyQualifiedName~KeonRuntimeClientStubTests --disable-build-servers
rtk proxy dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --filter FullyQualifiedName~KeonRuntimeClientStubTests --no-build --disable-build-servers --verbosity minimal
rtk proxy dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --filter FullyQualifiedName~RuntimeReceiptFactoryTests --no-build --disable-build-servers --verbosity minimal
rtk grep "keon://receipt/stub-" backend
rtk git diff --check
rtk git status --short
```

## Verification Results

- `KeonRuntimeClientStubTests`: 7 passed, 0 failed, 0 skipped.
- `RuntimeReceiptFactoryTests`: 5 passed, 0 failed, 0 skipped.
- The initial focused command compiled the test project successfully with
  existing package advisories/warnings and no errors.
- Full `BioStack.Api.Tests`: 266 passed, 30 failed, 0 skipped. Every observed
  failure is a receipt-producing integration path receiving the intended
  fail-closed HTTP 500 from the offline stub instead of the prior synthetic
  receipt.
- The affected integration harnesses need an explicit test-only
  `IKeonRuntimeClient` authority before the repository-wide suite can pass.
  Four affected files are also part of pull request #230, so this parcel does
  not modify them.

## Security-Focused Self-Audit

- Reviewed the unavailable exception path, `StubAllowAll` bypass behavior,
  factory append ordering, authority strings, final file scope, and pull
  request #230 overlap.
- No security finding remains in the three implementation/test files: receipt
  issuance returns no value, `StubAllowAll` cannot grant receipt authority, and
  the Spine append remains unreachable after failure.
- `rtk grep "keon://receipt/stub-" backend` returned no match.
- Coverage gap: this parcel did not design or authorize a shared test-only Keon
  receipt authority. That is a separate test-harness contract and must not be
  solved by weakening the production stub.

## Evidence Required

- Focused test output recorded above.
- Final diff and status limited to Allowed Files.
- Routing event:
  `research/routing-events/bio-rt-01-fail-closed-offline-receipts-20260726.json`.

## Collision Risk

Low. No implementation or test file in this parcel is changed by pull request
#230. The parcel and routing-event paths are new and unique. Shared parcel
indexes, project files, workflows, and lockfiles are intentionally unchanged.

## PR Notes

- What changed: offline receipt issuance now throws the contract-defined
  unavailable exception and cannot create a synthetic Keon URI.
- Why: a local stub is not a receipt authority and cannot truthfully produce a
  Keon-issued/retrievable audit artifact.
- Risk: receipt-required offline operations now halt instead of continuing with
  synthetic evidence; that is the intended fail-closed contract.
- Verification: run the two focused suites and confirm no synthetic receipt
  string remains.
- Evidence: this parcel record, routing event, and focused test output.

## Session Handoff

- Starting commit: `53ed0df5a0c207e99b6b3582d6c40b64e6b4f11c`
- Ending commit: unchanged; work is intentionally uncommitted.
- Files changed: the five Allowed Files listed above.
- Commands run: focused stub and receipt-factory tests, diff/status checks, and
  scoped source searches.
- Tests passed: 12 focused tests; 266 tests in the broad API regression.
- Tests failed: 30 broad API integration tests that implicitly depended on the
  removed synthetic receipt behavior.
- Decisions needed: authorize a serialized test-harness parcel, after pull
  request #230 ownership clears, to inject an explicit test-only Keon receipt
  client into receipt-producing integration factories.
- Blockers: broad API CI is red until that harness decision is implemented;
  independent security review also remains required before merge.
- Next safe action: coordinator reviews the fail-closed diff and dispatches the
  non-production test-harness follow-up without weakening the offline client.
- Do not touch: live services, other repositories, frontend/lockfiles, PR #230
  files, deployment configuration, or Runtime API contract alignment.

## Stop-and-Report Rule

Stop if a caller must continue after receipt issuance fails, if an offline audit
artifact becomes a product requirement, or if any implementation outside the
Allowed Files is required. Those cases require a separate contract/product
decision and parcel.
