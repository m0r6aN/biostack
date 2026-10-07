# Plan-Level Adversarial Review Findings

## Review record

- Goal: `biostack-algorithm-remediation`
- Ratified charter review commit: `6720edb7b37162757f14809e1eded130dc8f3cae`
- Execution base: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Reviewer: fresh read-only frontier subagent `/root/plan_adversarial_review`
- Context boundary: ratified charter plus repository and diagnostic canon only
- Reviewer verdict: **not ready for unrestricted parcel dispatch**
- Coordinator action: reproduced the cited conditions from Git objects and the pinned base before dispositioning them below

## Triage

| ID | Severity | Disposition | Coordinator ruling | Locked decision impact | Blocked work |
|---|---|---|---|---|---|
| F1 | Critical | Fix | Q04 must run the existing main-based `suggest.route.test.ts` as a dependency-readiness probe. It does not import or run the diagnostic outbound regression; P07 imports that file after Q04 succeeds. | None; this clarifies the Q04 command without changing D1 or D12. | Q04 until its shaped command is corrected; P07 already paused under F7. |
| F2 | Critical | Fix | P05 must replace the bug-observation wait with a deterministic worker-completion signal, retain the same synchronized race and test name, and assert the terminal snapshot is unchanged. | None; diagnostic-harness repair preserves D8. | P05. |
| F3 | High | Fix plus narrow re-ratification | D8 must enumerate active and terminal states, define first-terminal-writer custody, specify immutable terminal fields and idempotent behavior, and serialize final P06 integration after P05. | **D8 amendment required.** | P05 and P06. |
| F4 | Critical | Fix plus narrow re-ratification | `AvoidWith` must outrank both graph and stored hints. P03 must add positive-hint, inactive/unreviewed-artifact, unreviewed-edge, and `NeedsReview` negatives. | **D6 amendment required.** | P03. |
| F5 | High | Fix | P04 must directly prove that each ineligible artifact status causes no staging upsert, independently prove internal-only attribution is denied, and retain positive controls for an eligible candidate and external locator. | None; this makes the tests bind to D7. | P04 until its shaped spec includes the repaired harness. |
| F6 | High | Fix | P06 must assert the source cap over `tools_invoked`, artifact tool lists, provenance `tool_results`, claims, and claim `source_ids`, using a second result with unique evidence. | None; this makes the test bind to D8. | P06, also paused under F3. |
| F7 | Critical | Fix plus narrow re-ratification | D9 must pin trusted-origin, named-cookie, no-store, no-redirect, bounded-time, exact-response, and fail-closed behavior before the frontend provider call. | **D9 amendment required.** | P07. |
| F8 | High | Accept as documented with strengthened paired verification | P07 remains the single frontend/backend boundary parcel. Its local fake must assert the exact backend path, method, named cookie, request policy, and response contract; aggregate verification must run the real backend `ConsentGateIntegrationTests`. Because malformed/drifted responses deny outbound, residual contract drift is availability-only. A separate SG-OUTBOUND reviewer must reject this ruling if the final candidate can fail open. | None; no eighth implementation parcel or new Allowed File is required. | Gate 3 if paired verification or SG-OUTBOUND review is absent. |
| F9 | High | Fix | Q03 may not make a deterministic exploitability claim from wall-clock timing. It must record `inconclusive — no deterministic seam` unless static/runtime evidence already on the pinned base proves a bounded regex timeout. It creates no test merely to restate P02 cancellation. | None; D4 already permits an evidence-backed inconclusive disposition with no production write. | Q03 only. |
| F10 | Medium | Fix | Q custody is outcome-specific: reproduced branches/tests remain preserved and unmerged pending a separately ratified remediation; not-reproduced, inconclusive, and environment-blocked outcomes remain evidence-only; every Q branch and outcome is recorded at Stage F and no Q branch is deleted without explicit cleanup authority. | None; this installs the branch-custody detail already required by D14. | Q01-Q04 Stage F until receipts exist. |
| F11 | Medium | Fix | Shaped specs and the loop directive must carry exact targeted, adjacent, and aggregate commands, base test-count receipts, offline/network controls, sequencing, and expected count deltas before their Gate 2 contingency is satisfied. | None. | Gate 2 for any spec missing the matrix; Gate 3 globally until aggregate verification is green. |
| F12 | Critical | Fix plus narrow re-ratification | Coordinator lint found that D7's “otherwise non-candidate” phrase does not classify `Completed` or `Partial`, and its locator forms lack executable syntax. Current executor evidence supports `PendingReview`/`Partial` as candidate outputs, but a builder cannot silently choose that product contract. | **D7 amendment required.** | P04. |

## Coordinator reproduction evidence

- F1: `git cat-file -e` failed for the outbound reproduction test on base `339f259...` and succeeded at diagnostic commit `c3e30a9...`; the existing `suggest.route.test.ts` is present on base.
- F2: diagnostic sidecar lines 103-109 wait for `status != FAILED`, then lines 111-117 require `FAILED`; a correct monotonicity fix would time out before the assertion.
- F3: `ResearchJobStatusCode` has four active states and six terminal candidates; `InMemoryJobStore.update` currently mutates arbitrary fields under its lock; the executor later writes its terminal snapshot through the same store boundary.
- F4: production order is graph, hint, then `AvoidWith`; the retained diagnostic covers only the graph case and only a `NeedsReview` edge.
- F5: the failed artifact is staged successfully and the retained assertion checks only the later EvidenceGate result.
- F6: the retained source-cap reproduction asserts only `updated.tools_invoked`; executor materializes provenance and claims in separate collections.
- F7/F8: the frontend currently calls the provider directly; the authenticated backend contract is `GET /api/v1/consent`; the existing middleware demonstrates a server-controlled origin, named cookie, and `cache: no-store`; `ConsentGateIntegrationTests` exercises the real endpoint.
- F9: PDF extraction uses synchronous `Regex.Matches` without a timeout or deterministic instrumentation seam.
- F11: repository commands exist for .NET, Vitest/lint/build, and sidecar pytest/ruff, but the charter did not pin an aggregate command matrix or count receipts.
- F12: the current sidecar executor emits `PendingReview` for full candidate success and `Partial` for partial candidate output; it does not emit `Completed`. The .NET enum nevertheless exposes all ten states, and D7 did not turn “otherwise non-candidate” into an exact allowlist or locator grammar.

## Repairs that do not reopen Gate 1

1. Q04 dependency probe:
   - from `frontend/`, run `npm ci --offline`;
   - run `npm test -- src/__tests__/app/api/research/suggest.route.test.ts --pool=threads --maxWorkers=2 --no-file-parallelism`;
   - verify tracked/untracked state matches the pre-run receipt after ignored-artifact cleanup;
   - a cache miss is `ENVIRONMENT_BLOCKED`, never a product failure.
2. P04, P05, and P06 shaped specs must include the harness repairs from F2, F5, and F6 without deleting or weakening the original scenarios.
3. Q03 is a bounded evidence adjudication, not a timing benchmark. Without a deterministic seam already present on base, its durable answer is `INCONCLUSIVE_NO_DETERMINISTIC_SEAM`.
4. Every shaped spec records base test counts and exact expected deltas. The goal-level aggregate matrix must include:
   - `dotnet test backend/BioStack.sln --verbosity minimal`;
   - from `backend/research-sidecar`, offline `uv run pytest` and `uv run ruff check .`;
   - from `frontend`, dependency readiness, `npm run lint`, deterministic Vitest, and `npm run build`;
   - exact Allowed Files and `git diff --check` validation;
   - local-fake outbound call counts and an external-network prohibition receipt.
5. P05 merges before final P06 integration verification. P07 verification pairs its exact fail-closed local contract tests with the existing real-backend `ConsentGateIntegrationTests`.

## Narrow Gate 1 reopening

Only D6, D7, D8, and D9 are reopened. The proposed replacement text is recorded in `gate-1-amendment-01.md`.

- P03 is paused on D6.
- P04 is paused on D7.
- P05 and P06 are paused on D8.
- P07 is paused on D9.
- D9(a), the OCR half implemented by P02, is unchanged and orthogonal to the reopened frontend clauses.
- P01, P02, and Q01-Q04 remain eligible for shaping under the original contingent Gate 2 authorization, provided their specs incorporate every applicable non-decision repair above.
- Gate 3 remains ungranted.
