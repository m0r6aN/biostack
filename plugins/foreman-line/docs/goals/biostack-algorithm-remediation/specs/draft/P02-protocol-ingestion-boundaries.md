# Parcel Spec: P02 Protocol Ingestion Boundaries

Status: **active — coordinator lint passed 2026-09-02; dispatch remains contingent on exact worktree/base verification and Step 0 confirmation**

## Identity

- Goal: `biostack-algorithm-remediation`
- Initiative: `biostack-algorithm-remediation`
- Project: `BioStack`
- Wave: Wave 2 — outbound and shared-file boundaries; P02 may run alongside Wave 1 because no other ratified parcel owns `ProtocolIngestionService.cs`
- Routing: frontier implementation
- Risk: high collision risk and critical outbound-data security risk
- Branch: `codex/biostack-remediation-p02`
- Isolated worktree: `C:\Users\clint\.codex\worktrees\biostack-remediation-p02\BioStack`
- Execution base: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Base tree: `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`

## Goal

Close the two reproduced protocol-ingestion defects without broadening behavior: observe PDF cancellation before decoding and at bounded synchronous extraction checkpoints, and make configured OCR deny by default unless the current authenticated user's server-side consent gate grants current consent before any provider request is constructed or sent. Retain both diagnostic reproductions as local, deterministic regressions and make the smallest production change inside AF-P02.

## Authority and Gate State

- The ratified goal charter, plan-review findings, and coordinator loop directive control this parcel.
- Gate 1 for D1-D14 was granted on 2026-09-02. Contingent Gate 2 covers P02 only after this draft is coordinator-linted, its exact branch/worktree is verified, and Step 0 is confirmed.
- Gate 3 is ungranted and human-only. This parcel authorizes no push, pull request, merge, deployment, provider enablement, publication, or release.
- Narrow Gate 1 Amendment 01 remains pending for D6, D7, D8, and D9. Its D9(a) wording restates the already-ratified OCR boundary: OCR consults the current authenticated user's server-side consent gate before constructing or sending a provider request, and denial or gate failure is fail-closed. The pending D9(b) frontend amendment is orthogonal to P02 and must not be implemented here.
- SG-SCOPE and SG-OUTBOUND both apply. A failure of either gate blocks Gate 3.

## Dependencies

- Start only from exact base `339f259b1a467034db4f57cf9d774c292f11b53a`; never branch from, merge, rebase, or rewrite a diagnostic branch.
- Import the PDF regression content only from parser diagnostic commit `817f6f3331c2b7c3410da63289c02fb27a98ed74`.
- Import the backend OCR regression content only from outbound diagnostic commit `c3e30a93e64be5a2662bb9040a52105d3dc909c9`. Do not import that commit's frontend test.
- The pinned base already provides `IConsentGate.IsConsentGrantedAsync(CancellationToken)` in `ConsentGate.cs`, registers `IConsentGate` and `IProtocolOcrService` as scoped services in `BioStack.Api/Program.cs`, and includes `Moq` plus xUnit in the application test project. These are read-only dependencies, not Allowed Files.
- P02 has no implementation dependency on Q04, P07, or ratification of the frontend-only D9(b) clauses. It has no collision with another ratified remediation parcel, but the large shared ingestion source remains a high-risk integration surface.
- A material movement of `origin/main` from the pinned base invokes the charter's base-change rule and stops dispatch pending coordinator adjudication.

## Verified Existing Patterns

The following facts were verified against the pinned base and diagnostic Git objects:

- `PdfProtocolExtractor.ExtractAsync` is synchronous inside its returned `Task`; it currently decodes the entire byte array and runs page/direct-text/array-text regular expressions without consulting its cancellation token.
- `AzureVisionProtocolOcrService.ExtractAsync` currently checks OCR configuration, creates the named `protocol-ocr` client, constructs a POST with raw image bytes, and sends it without consulting `IConsentGate`.
- `ConsentGate.IsConsentGrantedAsync` resolves the current authenticated user through server-side state and returns true only for the current server consent version. No caller-supplied boolean is needed or authoritative.
- Existing scoped DI can satisfy an added `IConsentGate` constructor dependency on `AzureVisionProtocolOcrService`; no edit to `Program.cs` is required.
- `ProtocolIngestionServiceTests` contains five local ingestion cases. `LinkProtocolExtractorSecurityTests` contains fourteen local-handler boundary cases. Together they form the existing nineteen-case adjacent receipt on the pinned base.
- AF-P02's two regression files do not exist on the execution base. Their verified source blobs are:
  - PDF regression blob `a7c59d99f2a22460d7d471dcbdf9ba1618f283f1` at parser commit `817f6f3331c2b7c3410da63289c02fb27a98ed74`;
  - OCR regression blob `bb3ea3fcec68b78ecdd2dbd2c065e372a1d8874e` at outbound commit `c3e30a93e64be5a2662bb9040a52105d3dc909c9`.
- The pinned production-file blob is `0a1b40d563e1eab591427802d7caf68a46a721d6`.

## Exact Allowed Files — AF-P02

Only these three files may change on the P02 implementation branch:

- `backend/src/BioStack.Application/Services/ProtocolIngestionService.cs`
- `backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionPdfReproductionTests.cs`
- `backend/tests/BioStack.Application.Tests/Services/ProtocolOcrOutboundBoundaryReproductionTests.cs`

If any required change or test needs another file, stop for a charter/spec amendment. A nearby or substitute file is not authorized.

## Forbidden and Out of Scope

- Every repository path outside AF-P02 is forbidden, including `backend/src/BioStack.Application/Services/ConsentGate.cs`, `backend/src/BioStack.Api/Program.cs`, project files, package/lock files, configuration, deployment/IaC, and all frontend files.
- D9(b), P07, the suggestion BFF, session-cookie forwarding, backend-origin selection, redirect/cache/timeout frontend policy, and frontend consent-response parsing are pending and orthogonal. Do not implement or test them here.
- Do not alter consent copy, consent version, user/session/authentication semantics, database schema, endpoint filters, OCR endpoint/key configuration, public API contracts, or link-ingestion behavior.
- No refactor, package change, schema change, config enablement, unrelated cleanup, new service abstraction, provider proxy, or public behavior expansion.
- No real OCR/OpenAI/Collective/provider request, DNS/socket access, registry access, cloud/production resource, production database, protected or customer data, credential, secret, or payload dump.
- No diagnostic-branch merge, branch cleanup, history rewrite, push, PR, merge, deployment, publication, or release.

## Required Contracts

### R-PDF-01 — cancellation boundary

Reproduced current behavior: an already-cancelled PDF request enters synchronous extraction.

Required invariant: `PdfProtocolExtractor.ExtractAsync` observes cancellation before decoding and at bounded extraction checkpoints.

The production fix must:

1. preserve the existing empty-PDF validation and successful extraction behavior;
2. call `CancellationToken.ThrowIfCancellationRequested()` before `Encoding.GetEncoding("ISO-8859-1").GetString(...)` can decode source bytes;
3. consult the token between bounded synchronous extraction phases, including page counting, direct-text matching, array-text matching, normalization/materialization, and result return, without using wall-clock timing as an oracle;
4. propagate cancellation as `OperationCanceledException` rather than converting it to `ProtocolIngestionException`; and
5. preserve `ExtractAsync_AlreadyCancelledRequest_DoesNotEnterRegexExtraction` with its original synthetic PDF scenario and method name.

The parcel does not claim to solve Q03 regex exploitability. Q03 remains a separate evidence lane and must not add timeout or regex-engine policy here.

### R-OUT-01 / D9(a) — OCR current-user outbound boundary

Reproduced current behavior: configured OCR transmits raw document bytes without a consent check at the outbound adapter.

Required invariant: OCR consults the current authenticated user's server-side consent gate before constructing or sending a provider request; denial or gate failure is fail-closed.

The production fix must:

1. inject and use the existing `IConsentGate`; do not accept a caller-supplied authorization or consent boolean;
2. await `IsConsentGrantedAsync(cancellationToken)` before `CreateClient("protocol-ocr")`, `HttpRequestMessage`, provider URI, subscription-key header, or `ByteArrayContent` construction;
3. on false consent, missing/invalid current-user state, or consent-gate failure, perform zero provider sends and return/propagate a local failure without falling back to another provider;
4. preserve cancellation semantics and never turn cancellation into authorization;
5. after configuration is valid and current server-side consent is granted, construct and send exactly one provider request through the named client and preserve the existing successful OCR response mapping; and
6. keep OCR endpoint/key handling and application DI registration unchanged.

Endpoint-level `.RequireConsent()` is not sufficient for this invariant. The outbound adapter itself must enforce the server-side gate before request construction so alternate internal call paths cannot bypass it.

## Smallest Production Fix

Limit production changes to two surgical edits in `ProtocolIngestionService.cs`:

- add explicit cancellation checks around the existing bounded PDF decoding/extraction phases, passing the token into the private PDF extraction helper only as needed for those checks; and
- add `IConsentGate` to `AzureVisionProtocolOcrService`, perform the fail-closed current-user consent check before client/request construction, then leave the existing one-request OCR send and response parsing intact.

Do not move classes, split files, rename public interfaces, or change unrelated extractors. If existing DI cannot compose this constructor without another production-file edit, stop rather than widening AF-P02.

## Regression Custody and Test Design

### Retained imports

- Copy `ProtocolIngestionPdfReproductionTests.cs` content from `817f6f3331c2b7c3410da63289c02fb27a98ed74` into its exact AF-P02 path. Preserve the class, test name, pre-cancelled token, and synthetic PDF bytes.
- Copy `ProtocolOcrOutboundBoundaryReproductionTests.cs` content from `c3e30a93e64be5a2662bb9040a52105d3dc909c9` into its exact AF-P02 path. Preserve the class, original denial test name, synthetic payload, local intercepting handler, and raw-byte boundary assertion; adapt only constructor/test doubles required by the production gate.
- Do not cherry-pick either diagnostic commit and do not import `ProtocolParserReproductionTests.cs` or the frontend outbound reproduction.

### Required P02 cases

The final targeted set contains exactly four discovered xUnit cases:

1. retained PDF regression `ExtractAsync_AlreadyCancelledRequest_DoesNotEnterRegexExtraction` throws `OperationCanceledException` before extraction;
2. retained OCR regression `ConfiguredOcr_DoesNotTransmitRawBytesWithoutExplicitOutboundAuthorization` uses a server-side consent fake returning false and records exactly zero local-fake provider sends; and
3. one new consent-gate-failure OCR case uses a consent fake that throws a local exception and records exactly zero local-fake provider sends; and
4. one new authorized-current-consent OCR case uses a consent fake returning true, a synthetic configured endpoint/key, and the local intercepting handler; it records exactly one provider send, confirms the synthetic body crosses only that fake boundary, and preserves successful response mapping.

The expected targeted delta is exactly `+4` from the pinned base. Any additional separately discovered case requires a spec amendment before implementation.

All fixtures are synthetic. `synthetic-ocr.invalid` may appear only behind the injected `HttpMessageHandler`; no default network handler, DNS lookup, or socket is permitted. Denial must be asserted from the fake handler's `RequestCount == 0`; the authorized-consented path must assert `RequestCount == 1`.

## Step 0 — Restate and Stop

Before editing, the fresh builder must report all of the following and then stop for coordinator confirmation:

1. goal, parcel P02, branch, worktree, and exact base commit;
2. both invariants: R-PDF-01 and R-OUT-01/D9(a);
3. the three exact AF-P02 Allowed Files and that every other path is forbidden;
4. diagnostic source commits and retained test names;
5. SG-SCOPE, SG-OUTBOUND, synthetic-only/no-network limits, zero-send denial, and exactly-one-send authorized-consented expectation;
6. smallest-fix boundary, targeted/adjacent commands, expected count delta, evidence, review requirements, and stop rules; and
7. confirmation that D9(b)/P07 is pending, orthogonal, and excluded.

No edit, import, build, or test begins until the coordinator confirms Step 0.

## Deterministic Verification Commands

Run from `C:\Users\clint\.codex\worktrees\biostack-remediation-p02\BioStack` in PowerShell with already-restored local dependencies. `--no-restore` is mandatory for parcel commands; if required packages are unavailable, report `ENVIRONMENT_BLOCKED` rather than accessing a registry.

### Base receipt, before edits

```powershell
git rev-parse HEAD
git status --porcelain
git merge-base --is-ancestor 339f259b1a467034db4f57cf9d774c292f11b53a HEAD
git ls-tree -r --name-only 339f259b1a467034db4f57cf9d774c292f11b53a -- backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionPdfReproductionTests.cs backend/tests/BioStack.Application.Tests/Services/ProtocolOcrOutboundBoundaryReproductionTests.cs
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter 'FullyQualifiedName~BioStack.Application.Tests.Services.ProtocolIngestionServiceTests|FullyQualifiedName~BioStack.Application.Tests.Services.LinkProtocolExtractorSecurityTests' --verbosity minimal
```

Expected base receipt: exact HEAD `339f259b1a467034db4f57cf9d774c292f11b53a`, clean status, ancestor exit `0`, no lines for the two absent regression paths, and nineteen adjacent cases passed.

### Targeted P02 regressions

```powershell
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter 'FullyQualifiedName~BioStack.Application.Tests.Services.ProtocolIngestionPdfReproductionTests|FullyQualifiedName~BioStack.Application.Tests.Services.ProtocolOcrOutboundBoundaryReproductionTests' --verbosity minimal
```

Expected final receipt: four passed, zero failed, zero skipped; denied and gate-failure OCR each record zero local-fake sends, and authorized-consented OCR records exactly one.

### Adjacent ingestion and boundary suites

```powershell
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter 'FullyQualifiedName~BioStack.Application.Tests.Services.ProtocolIngestionServiceTests|FullyQualifiedName~BioStack.Application.Tests.Services.LinkProtocolExtractorSecurityTests' --verbosity minimal
```

Expected final receipt: the existing nineteen adjacent cases remain passed, with zero failed and zero skipped.

### Aggregate and diff hygiene

The plan-review matrix names the goal-level aggregate command exactly as:

```powershell
dotnet test backend/BioStack.sln --verbosity minimal
```

For P02's no-network parcel execution, use the already-restored equivalent and do not permit restore traffic:

```powershell
dotnet test backend/BioStack.sln --no-restore --verbosity minimal
git diff --check 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff --name-only 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git merge-base --is-ancestor 339f259b1a467034db4f57cf9d774c292f11b53a HEAD
git status --porcelain
```

The coordinator later consumes the exact goal-level aggregate receipt before Gate 3. No passing targeted run substitutes for aggregate verification.

## Base/New Count Receipt Method

- Capture the base adjacent console summary before edits: nineteen passed.
- Establish the targeted base count as zero by the empty `git ls-tree` receipt for both exact test paths and by confirming no duplicate fully qualified class names exist on the base.
- After import/fix, capture the targeted console summary. The required result is four passed: one retained PDF case, one retained denied-OCR case, one new gate-failure case, and one new authorized-consented OCR case.
- Rerun the adjacent filter and require nineteen passed unchanged.
- Capture the base aggregate passed/failed/skipped totals before edits and the candidate aggregate totals after edits. Expected candidate passed count is base `B + 4`, while failed and skipped totals are unchanged. Any extra case requires a spec amendment.
- Record command text, exit code, and console count summary directly in the handoff. Do not create tracked receipts or leave untracked `TestResults`, coverage, or build residue.

## Acceptance Criteria

- Only AF-P02 files differ from the pinned base.
- Both diagnostic test files are retained at their exact paths with their original failing scenarios recognizable and method names preserved.
- Already-cancelled PDF input throws before byte decoding or regex extraction; bounded synchronous phases consult the token.
- OCR uses current authenticated server state through `IConsentGate` before client or request construction. Caller claims and configuration alone never authorize outbound transmission.
- Consent denial and gate failure fail closed with zero local-fake sends. Valid configuration plus current consent produces exactly one local-fake send and successful synthetic response mapping.
- Targeted P02, adjacent ingestion/boundary, and required aggregate suites are green with the recorded count delta.
- All tests are synthetic and local; no real network/provider/data/secret/cloud/production access occurs.
- Diff hygiene, base ancestry, Allowed Files, and clean-tree checks pass.
- Two fresh independent adversarial reviews and a separate defensive SG-OUTBOUND security review accept the exact final candidate commit, or all findings are closed/reviewed again after rework.
- Gate 3 remains ungranted; the candidate remains local and unmerged.

## Evidence Required

The builder handoff must contain:

- exact starting base and final candidate commit;
- `git diff --stat`, `git diff --name-only`, and confirmation that only AF-P02 changed;
- diagnostic commit/blob provenance for both imported regressions;
- exact targeted, adjacent, and aggregate commands, exit codes, passed/failed/skipped totals, base/new count delta, and any warnings;
- denied, gate-failure, and authorized-consented fake consent outcomes plus local handler request counts (`0`, `0`, and `1` respectively when gate failure has a separate case);
- an explicit no-network/no-provider/no-protected-data receipt describing the injected local handler and synthetic fixtures;
- `git diff --check`, base-ancestry, and clean `git status --porcelain` receipts;
- residual risk, rollback note, and confirmation that D9(b), P07, external enablement, push/PR/merge/deploy/release, and diagnostic branch mutation did not occur; and
- exact candidate hash supplied to every reviewer.

## Review and Gate 3 Requirements

- Dispatch two fresh, independent, read-only adversarial reviewers against the exact final candidate commit. Review lenses must include cancellation coverage/checkpoint sufficiency, regression custody, DI compatibility, alternate OCR call paths, and scope/collision risk.
- Dispatch a third, separate, read-only defensive security reviewer for SG-SCOPE and SG-OUTBOUND. The security review must attempt to find fail-open paths, request construction before consent, caller/config-as-authority, multiple sends, real-network escape, data leakage, and scope drift.
- Reviewers never fix or commit. Every finding is dispositioned as fix, accept-as-documented, or informational with evidence.
- Any rework changes the candidate commit and invalidates all prior acceptance; rerun deterministic verification and all required reviews against the new exact hash.
- Builder tests or reviewer approval alone do not grant Gate 3. The human must later approve the exact merge/release set.

## Collision and Rollback

- Collision risk is high because `ProtocolIngestionService.cs` contains all protocol extractors and is an active security-sensitive integration surface, even though no other ratified remediation parcel owns it.
- Keep P02 isolated until final integration. If the base moves, compare this source and both adjacent suites before rebasing or dispatching.
- Logical rollback is the single P02 candidate commit after Gate 3; reverting it restores prior PDF/OCR behavior while the diagnostic branches remain untouched. No runtime/provider/config rollback is part of this parcel.

## Session Handoff

The builder stops after one local candidate commit and sends the coordinator a session handoff containing the Evidence Required section, all test/count receipts, exact changed files, review readiness, residual risks, and any stop condition encountered. Do not push or open a PR. The coordinator independently inspects the candidate diff and reruns/consumes verification before dispatching reviews.

## Stop Rules

Stop immediately and report if:

- Step 0 is not explicitly confirmed;
- the worktree is not clean at exact base `339f259b1a467034db4f57cf9d774c292f11b53a`, or `origin/main` moved materially;
- any required change falls outside AF-P02, including a need to edit `Program.cs`, `ConsentGate.cs`, project/package/config/frontend files, or a new abstraction;
- either diagnostic file or source commit is unavailable, or the retained scenario can pass only by deletion or weakening;
- cancellation cannot be honored before decoding and at bounded extraction checkpoints inside the allowed production file;
- OCR cannot consult the current authenticated user's server-side gate before request construction, or any denial/gate-failure path can send;
- a real network/provider/registry/cloud/production resource, protected data, secret, credential, or payload dump would be required;
- targeted, adjacent, aggregate, count-delta, ancestry, Allowed Files, diff-hygiene, or clean-tree evidence fails;
- a security finding cannot close inside AF-P02, or the same tripwire/rework failure repeats twice;
- pending D9(b)/P07 work is proposed inside this parcel; or
- any push, PR, merge, deployment, provider enablement, publication, release, diagnostic-branch mutation, or cleanup is proposed before explicit human Gate 3.

## Shaping Boundary

This repository lacks the Foreman emitter/linter. This draft is the only shaping artifact for P02; no `ShapingResult` JSON is created. Coordinator lint remains authoritative, and `status: draft` is not dispatch authority.
