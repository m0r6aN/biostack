---
parcel: P06
title: Sidecar accepted-source cap
status: complete-merged-pr-265
goal: biostack-algorithm-remediation
initiative: BioStack Algorithm Remediation
project: BioStack research sidecar
wave: Wave 1 - independent fixes
risk: high resource and provenance integrity
routing: frontier implementation plus SG-SIDECAR
branch: codex/biostack-remediation-p06
worktree: 'C:\Users\clint\.codex\worktrees\biostack-remediation-p06\BioStack'
base: 339f259b1a467034db4f57cf9d774c292f11b53a
base_tree: 0f6d0b609ce255aad5cca81698eb3ad0917cfda6
diagnostic_commit: 82295c3f36b412b9917eaf047a70b10ee2a67cdc
p05_candidate: 2f4a092fb7f0ec534996e6ad1116e8b50d612b77
---

# P06 - Sidecar accepted-source cap

Shaper lint: **PASSED 2026-09-02.** The source owner, diagnostic fixture, P05 custody, exact Allowed Files, offline commands, test counts, security gates, and Gate 3 boundary were reconciled against Git and the local accepted P05 worktree. Coordinator lint, clean worktree creation, and a confirmed fresh Step 0 remain mandatory before implementation.

Coordinator lint: **PASSED 2026-09-02.** Local `main` and `origin/main`, pinned base/tree, executor blob, diagnostic test blob, accepted P05 hash/tree/reviews, exact two-file AF-P06, five-case census, standalone and combined counts, offline commands, lint baselines, security/review depth, and Gate 3 boundary were independently reconciled. Dispatch remains contingent on a clean isolated base-rooted worktree and exact Step 0 restatement.

Final disposition: **REVIEW COMPLETE 2026-09-02.** Candidate `c435caf3cf0cf95fb1bfbc51d2924ea960183957` passes target `5/5`, adjacent `38 passed / 1 expected skip`, aggregate `43 passed / 1 expected skip`, exact five-finding lint baseline, scope, ancestry, and offline boundaries. Coordinator-controlled combined verification with accepted P05 `2f4a092fb7f0ec534996e6ad1116e8b50d612b77` and P06 patch SHA-256 `B2882552D186668F800F1E943EEF831C73DF939955208C8625ADD122C34E57CC` passes targets `15/15`, adjacent `38 passed / 1 expected skip`, aggregate `53 passed / 1 expected skip`, and exact thirteen-finding lint baseline. Two fresh adversarial reviewers and a fresh separate SG-SIDECAR/SG-SCOPE reviewer ACCEPT this exact candidate/custody. Local only and held unmerged; Gate 3 remains ungranted.

## Goal and outcome

Remediate R-SIDE-02 from the pinned `main` base. After the allowlisted workflow returns, the executor must apply `maximum_source_count` before it accepts or materializes ordered result occurrences, tool lists, result-derived warnings, provenance `tool_results`, normalized claims, or claim `source_ids`. Preserve the exact diagnostic method as a regression, extend that file with the smallest hostile and positive controls needed to bind the cap, make the smallest production change inside AF-P06, and return one local candidate commit plus deterministic evidence.

P06 does not alter terminal-state custody. Final D8 evidence must combine the exact accepted P05 candidate `2f4a092fb7f0ec534996e6ad1116e8b50d612b77` with the exact P06 candidate and rerun both retained target files plus the complete sidecar suite.

This parcel does not authorize a push, pull request, merge, deployment, provider enablement, publication, release, or diagnostic-branch mutation. Gate 3 is human-only and remains ungranted.

## Initiative, track, wave, and integration surface

- Initiative: `biostack-algorithm-remediation`.
- Project track: `backend/research-sidecar`.
- Wave: Wave 1 independent fix, serialized downstream of accepted P05 custody.
- Integration surface: D8 sidecar lifecycle/source-boundary aggregate. P05 owns terminal-state monotonicity; P06 owns accepted-source materialization. The production files are disjoint, but the final sidecar verification is shared.
- Production owner: `biostack_research_sidecar.workflows.executor._execute_with_tooluniverse` in `workflows/executor.py`.
- Regression owner: `tests/test_request_constraint_reproduction.py`.

## Authority, source pins, dependency, and gates

- Controlling decisions: ratified D1-D14 plus the ratified D8 replacement in Narrow Gate 1 Amendment 01 and the ratified D8 timeout supplement in Amendment 03. Amendments 02 and 04 do not change P06. Amendment 05 is awaiting ratification for P03 only and does not block P06.
- Remediation execution base: `main` / local `origin/main` at `339f259b1a467034db4f57cf9d774c292f11b53a`; tree `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`.
- Diagnostic evidence source: `82295c3f36b412b9917eaf047a70b10ee2a67cdc`. The retained P06 test blob is `2eae1d80d1e14334a6b670d3cba2747d921dca80`.
- Accepted P05 dependency: candidate `2f4a092fb7f0ec534996e6ad1116e8b50d612b77`, tree `1c0f8406cde32eefeba4411ecde6df3bb680fcf4`. Two fresh adversarial reviewers and the separate SG-SIDECAR/SG-SCOPE reviewer accepted that exact hash. It is local, unmerged, and unpushed.
- P05 changes only `jobs/store.py`, `jobs/runner.py`, and `test_runner_terminal_state_reproduction.py`; its executor blob is identical to the pinned base (`611a091a6697d6a006cc6590f09ff2401f933778`). P06 therefore branches independently from the pinned base and consumes P05 only in final combined verification.
- Plan-review finding F6 requires direct cap assertions across `tools_invoked`, artifact tool lists, provenance `tool_results`, claims, and claim `source_ids`, using discarded results with unique evidence. F11 requires exact offline commands and count deltas.
- Gate 2 remains contingent on coordinator lint, unchanged local base refs, one clean isolated base-rooted branch/worktree, and explicit confirmation of the builder's fresh Step 0 before any edit.
- SG-SCOPE and SG-SIDECAR are mandatory. The exact P06 candidate requires two fresh independent adversarial reviews plus one separate defensive SG-SIDECAR/SG-SCOPE review. Any rework invalidates every prior P06 receipt and review.
- Gate 3 remains ungranted. No integration into `main` or release action is authorized.

## Exact branch and worktree

- Branch: `codex/biostack-remediation-p06`.
- Isolated worktree: `C:\Users\clint\.codex\worktrees\biostack-remediation-p06\BioStack`.
- Starting commit: `339f259b1a467034db4f57cf9d774c292f11b53a`.
- Starting tree: `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`.
- At shaping time, the named branch and worktree do not exist. If either appears before coordinator dispatch, stop for ownership reconciliation.
- The P06 implementation worktree must not branch from P05 or a diagnostic branch. The diagnostic file is reconstructed by content; no diagnostic commit is merged or cherry-picked.

## Exact Allowed Files - AF-P06

The builder may edit exactly these two repository paths:

1. `backend/research-sidecar/src/biostack_research_sidecar/workflows/executor.py`
2. `backend/research-sidecar/tests/test_request_constraint_reproduction.py`

The second path is absent on the pinned base and must be reconstructed from diagnostic commit `82295c3f36b412b9917eaf047a70b10ee2a67cdc`, then strengthened in place. If either file is absent, insufficient, or would require another path, stop for a narrow charter/spec amendment.

## Forbidden

- Every path outside AF-P06 is forbidden, including `contracts/models.py`, `workflows/sequences.py`, the ToolUniverse adapter/allowlist, job store/runner, app/routes/configuration, package/project/lock files, existing adjacent tests, P05's regression, .NET/frontend code, schemas, and goal documents.
- Do not change request validation, `maximum_source_count`'s public type/default, workflow sequence order, allowlist contents, adapter execution, status enums, terminal-state behavior, provider configuration, or public API/serialization contracts.
- Do not claim that P06 prevents ToolUniverse calls. The ratified seam is post-sequence acceptance/materialization in the executor; provider invocation count is outside AF-P06 and outside this parcel.
- Do not refactor the executor, extract a new module, introduce dependencies, clean ambient lint debt, or change unrelated warning/provenance/status behavior.
- Do not weaken, delete, skip, or rename `test_maximum_source_count_bounds_accepted_results`.
- No secrets, protected/customer/production data, payload dumps, cloud resources, production databases, external providers, registry access, DNS/socket access, Docker, HTTP listeners, or other network activity.
- The builder may create only the requested local candidate commit after the complete verification chain. It may not push, open a PR, merge, deploy, publish, release, or mutate/clean diagnostic branches.

## Out of scope

- Preventing tools from executing before `run_workflow_sequence` returns.
- Adding request-model lower/upper bounds or returning a new validation error for zero/negative limits.
- Deduplicating sources globally, changing source identity, adding source-manifest rows, parsing provider payloads, or changing evidence quality/ranking.
- Changing sequence skips: `sequence_skips` remain operational records for steps not invoked and are not accepted ToolUniverse results.
- Changing terminal-state custody established by P05.
- Any provider enablement, deployment, release, or externally exercised integration.

## Verified existing patterns on the pinned base

- `ScientificResearchRequest.maximum_source_count` is an unconstrained integer with default `50` in `contracts/models.py`; that model is outside AF-P06.
- `_execute_with_tooluniverse` receives ordered `(results, claim_rows, skips)` from `run_workflow_sequence`, then currently materializes every result into job/artifact tool lists, result-derived warnings, and provenance `tool_results`.
- The executor currently materializes every claim row into one `NormalizedClaim` with `source_ids=[row.tool_name]` and calculates terminal status from all returned results.
- `run_workflow_sequence` appends one claim row for each successful sequence-step result, preserves step/result order, and may emit repeated tool names for distinct result occurrences (for example, the published-regimen sequence uses two PubMed steps).
- The diagnostic cap test supplies two successful fake results with `maximum_source_count=1` but currently asserts only `updated.tools_invoked`; on the pinned base it fails because both results are accepted.
- The target test path is absent on the pinned base. The five adjacent files contain 39 collected cases: 38 pass and `test_legacy_config_copy_has_not_drifted` is the one expected skip.
- The accepted P05 candidate contains 49 collected cases: its target contributes 10 passing cases and the unchanged adjacent set remains 38 passed plus that one expected skip.
- On the accepted P05 tree, `uv run --offline ruff check .` reports exactly 37 pre-existing findings. `executor.py` accounts for exactly five existing `UP017` findings; the diagnostic P06 test blob has zero ruff findings.

## Exact D8 accepted-source contract

The governing decision is:

> `maximum_source_count` is enforced in the executor before accepted results, claims, provenance, and tool counts are materialized.

P06 binds that decision as follows:

1. After `run_workflow_sequence` returns and before any result-derived list, warning, claim, provenance row, or terminal-success calculation is created, compute an effective limit of `max(0, request.maximum_source_count)`.
2. Accepted results are the first `effective_limit` ordered result occurrences. The cap counts occurrences, not unique tool-name strings. A larger limit preserves every returned occurrence in order; zero or a negative request accepts none.
3. `JobRecord.tools_invoked`, `ScientificResearchArtifact.tools_invoked`, and `artifact.provenance["tool_results"]` contain exactly the accepted result occurrences in the same order. Their lengths cannot exceed the effective limit.
4. Only a successful accepted result occurrence can authorize a normalized claim. Claim rows must be matched in order and by tool identity to successful accepted occurrences, consuming occurrence cardinality. A failed, discarded, unmatched, blank-source, or excess claim row is not materialized. Repeated tool names cannot authorize more claims than the number of accepted successful occurrences bearing that name.
5. Every materialized claim has only its matched accepted tool identity in `source_ids`. No discarded/unmatched tool identity or claim text may enter normalized claims or claim source identifiers.
6. Error warnings, `any_success` / `all_success`, partiality, terminal status, and the no-result failure decision are calculated from accepted results, not discarded results. Discarded result error text and arguments do not enter the artifact.
7. Existing behavior remains unchanged for policy rejection, cancellation-before-execution, ToolUniverse-disabled scaffolding, workflow skips, artifact IDs/timestamps, allowlist/skill warnings, and all request fields other than accepted-source materialization.
8. `source_manifest`, raw hashes, and other currently empty artifact lists remain unchanged and cannot be populated as a side effect. No new artifact/model field is introduced.

If correct behavior requires changing sequence execution, request validation, models, the adapter, the store, or a public contract, stop for a narrow decision. Do not invent a broader resource-control claim.

## Required regression harness and exact census

Preserve the module/class style and exact retained method name `test_maximum_source_count_bounds_accepted_results`. The final target file contains exactly five discovered cases:

1. **Retained cap-one reproduction - 1 case.** Use `maximum_source_count=1` and at least two successful results with distinct tool names, arguments, claim types, claim text, and source identifiers; add a discarded failing result with a unique error sentinel. Assert the record and artifact tool lists contain exactly the first result, provenance `tool_results` contains exactly the first result's identity/arguments, normalized claims contain exactly the first claim and source ID, the discarded sentinels appear in none of the governed collections or warnings, and the accepted success maps to `pending_review` rather than partial/failure.
2. **Ordered positive control - 1 case.** Use a cap of two with three uniquely identifiable successful result/claim pairs. Assert exactly the first two occurrences and claims survive across every governed collection in order. This prevents an overbroad implementation that always accepts only one or none.
3. **Repeated-tool occurrence control - 1 case.** Return two successful result occurrences with the same tool name but unique arguments and two claim rows; use a cap of one. Assert one result/provenance row/tool entry and only one claim/source-ID row survive. A set-membership-only claim filter is a failure.
4. **Non-positive fail-closed controls - 2 parameter rows.** For exact values `0` and `-1`, return uniquely identifiable successful results and claims. Assert zero accepted result/provenance/tool/claim/source-ID material, unchanged empty source-manifest/raw-hash lists, and the existing no-accepted-result failed terminal mapping.

All fakes remain in-process. Monkeypatch the three late-imported seams (`load_allowlist`, `create_adapter`, and `run_workflow_sequence`) with local non-network functions or mocks. Use synthetic `.invalid`-free identifiers and no real provider configuration. The final target count is exactly `5 passed`, zero skipped/failed/errors. A sixth case, additional theory row, or altered census stops for coordinator reconciliation.

## Smallest authorized production change

Make one narrow executor-local repair inside `_execute_with_tooluniverse`:

1. derive the non-negative effective cap and an ordered accepted-results prefix immediately after the sequence returns;
2. use only that prefix for tool lists, result-derived warnings, provenance `tool_results`, success/partial/failure calculation, and artifact creation; and
3. materialize claims only when an ordered successful accepted occurrence authorizes them, consuming repeated tool-name cardinality.

Retain existing signatures and return types. Prefer a local bounded-data transformation over a new helper/module unless a tiny private helper inside `executor.py` is necessary for clarity. No adjacent file change or behavior expansion is authorized.

## Required tests and acceptance criteria

- The five-case P06 target is green with the retained method name present.
- Every governed collection is bounded exactly as specified, discarded unique sentinels are absent, repeated identities cannot inflate claims, non-positive limits accept zero, and a cap of two preserves two ordered occurrences.
- The five existing adjacent files remain `38 passed / 1 expected skip`, with zero failure/error.
- The standalone P06 suite is exactly `44` collected/executed: `43 passed / 1 expected skip`, zero failed/errors. This is exactly +5 passed/collected relative to the pinned 39-case base.
- AF-P06 ruff output contains only the same five pre-existing `UP017` findings in `executor.py`; the new test adds zero findings.
- The P06 candidate is a direct child of the pinned base, its diff names exactly AF-P06, the diagnostic commit is not an ancestor, `git diff --check` passes, and the final candidate worktree is clean.
- Final combined P05+P06 verification uses accepted P05 hash `2f4a092fb7f0ec534996e6ad1116e8b50d612b77` plus the exact P06 patch. The two target files are `15/15`; the adjacent set remains `38 passed / 1 skip`; the aggregate is exactly `54` collected/executed, `53 passed / 1 expected skip`, zero failed/errors.
- Every test and verification command uses offline dependency resolution and local synthetic fakes; no provider/network/cloud/production-data path is touched.

## Deterministic standalone verification

Run from the P06 worktree's `backend/research-sidecar` directory in PowerShell. `rtk` is preferred if available; if unavailable, record that once and run the exact underlying commands. Do not restore online. If the pinned cache cannot create the environment under `UV_OFFLINE=1`, stop as `ENVIRONMENT_BLOCKED`.

Keep environment and receipts outside the repository:

```powershell
$p06ReceiptDir = Join-Path ([System.IO.Path]::GetTempPath()) 'biostack-p06-receipts'
$p06UvEnv = Join-Path ([System.IO.Path]::GetTempPath()) 'biostack-p06-uv-env'
New-Item -ItemType Directory -Force -Path $p06ReceiptDir | Out-Null
$env:UV_PROJECT_ENVIRONMENT = $p06UvEnv
$env:UV_OFFLINE = '1'
```

Before importing the diagnostic test, record the base census, adjacent suite, and AF lint baseline:

```powershell
Test-Path -LiteralPath 'tests/test_request_constraint_reproduction.py'
uv run --offline pytest --collect-only -q
uv run --offline pytest tests/test_executor_status.py tests/test_health_and_jobs.py tests/test_inference_policy.py tests/test_tooluniverse_allowlist.py tests/test_workflow_sequences.py -q -rs --junitxml "$p06ReceiptDir/p06-base-adjacent.xml"
uv run --offline ruff check src/biostack_research_sidecar/workflows/executor.py --output-format concise
```

Expected base facts: target path absent; 39 collected; adjacent `38 passed / 1 expected skip`; exactly five `UP017` findings in `executor.py`. Any different count, path state, or finding census stops edits for coordinator reconciliation.

After implementation, run:

```powershell
uv run --offline pytest tests/test_request_constraint_reproduction.py -q -rs --junitxml "$p06ReceiptDir/p06-new-target.xml"
uv run --offline pytest tests/test_executor_status.py tests/test_health_and_jobs.py tests/test_inference_policy.py tests/test_tooluniverse_allowlist.py tests/test_workflow_sequences.py -q -rs --junitxml "$p06ReceiptDir/p06-new-adjacent.xml"
uv run --offline pytest -q -rs --junitxml "$p06ReceiptDir/p06-new-sidecar.xml"
uv run --offline ruff check src/biostack_research_sidecar/workflows/executor.py tests/test_request_constraint_reproduction.py --output-format concise
git merge-base --is-ancestor 339f259b1a467034db4f57cf9d774c292f11b53a HEAD
git merge-base --is-ancestor 82295c3f36b412b9917eaf047a70b10ee2a67cdc HEAD
git rev-parse '339f259b1a467034db4f57cf9d774c292f11b53a^{tree}'
git diff --check 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff --name-only 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git status --short --branch --untracked-files=all
```

Expected final results: target `5/5`; adjacent `38 passed / 1 expected skip`; aggregate `43 passed / 1 expected skip` from 44; first ancestry command exit `0`; diagnostic-ancestry command nonzero; base tree exactly `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`; name-only output exactly AF-P06; diff check green; final status clean after the candidate commit. JUnit files, not progress lines or source counting, are the authoritative count receipts.

## Final combined P05+P06 verification

After the exact P06 candidate is committed and its standalone chain is green, the coordinator creates a separate temporary detached verification worktree at accepted P05 candidate `2f4a092fb7f0ec534996e6ad1116e8b50d612b77`. It applies only the exact two-file P06 diff from the pinned base to the P06 candidate without committing, merging, rebasing, or changing either candidate branch. The coordinator records the P06 candidate hash and patch SHA-256 before application.

From that combined worktree's `backend/research-sidecar` directory, with a new external receipt directory/environment and `UV_OFFLINE=1`, run exactly:

```powershell
uv run --offline pytest tests/test_runner_terminal_state_reproduction.py tests/test_request_constraint_reproduction.py -q -rs --junitxml "$combinedReceiptDir/p05-p06-targets.xml"
uv run --offline pytest tests/test_executor_status.py tests/test_health_and_jobs.py tests/test_inference_policy.py tests/test_tooluniverse_allowlist.py tests/test_workflow_sequences.py -q -rs --junitxml "$combinedReceiptDir/p05-p06-adjacent.xml"
uv run --offline pytest -q -rs --junitxml "$combinedReceiptDir/p05-p06-sidecar.xml"
uv run --offline ruff check src/biostack_research_sidecar/jobs/store.py src/biostack_research_sidecar/jobs/runner.py src/biostack_research_sidecar/workflows/executor.py tests/test_runner_terminal_state_reproduction.py tests/test_request_constraint_reproduction.py --output-format concise
```

Required results are targets `15 passed`; adjacent `38 passed / 1 expected skip`; aggregate `53 passed / 1 expected skip` from 54; and exactly the unchanged combined AF lint baseline of thirteen findings: P05's eight plus executor's five, with zero test-file findings. The worktree receipt must prove `HEAD` is exact accepted P05, its committed diff from base is exactly AF-P05, the applied index/worktree diff is exactly AF-P06 from the exact P06 candidate, and no third path exists. No candidate or diagnostic branch is mutated. Preserve the verification worktree until the coordinator records its evidence; cleanup is not implicit.

If P05 changes, either P06 file changes, the patch does not apply cleanly, a count changes, or any review triggers rework, discard the combined receipt and rerun it against the new exact accepted hashes. Gate 3 cannot be requested on a stale combined receipt.

## Security gates

### SG-SCOPE

- Exact AF-P06 only; synthetic, local, in-process fixtures only.
- No credential values, protected/customer/production data, payload dumps, provider calls, network/registry/DNS/socket access, cloud resources, databases, containers, or service startup.
- Verify base/tree, direct-parent custody, diagnostic non-ancestry, exact diff scope, lint delta, `git diff --check`, and clean final candidate status.
- Any model, sequence, adapter, allowlist, store, runner, package/lock, adjacent-test, config, or third-file edit stops the parcel.

### SG-SIDECAR

- Review that the cap is applied immediately after the sequence return and before every accepted-result-derived materialization or status calculation.
- Review ordered occurrence semantics, non-positive limits, duplicate tool-name cardinality, claim-to-successful-result attribution, and absence of discarded sentinels in tools, artifact lists, result warnings, provenance results, claims, and source IDs.
- Reject set-membership claim filters, negative Python slicing, post-materialization truncation, inconsistent JobRecord/artifact lists, discarded-result status influence, or any path where claims/provenance exceed the cap.
- Confirm active P05 first-terminal-writer custody remains present in the final combined suite and that P06 does not bypass or rewrite it.
- Confirm the implementation does not claim or attempt to cap actual ToolUniverse invocation at this seam and does not introduce external execution in tests.

## Evidence required

The builder handoff must include:

- candidate and parent hashes, pinned base/tree, direct-parent and diagnostic-non-ancestry receipts;
- exact AF-P06 name-only diff, `git diff --stat`, `git diff --check`, and clean status;
- diagnostic commit/blob, preserved method name, and confirmation that all diagnostic branches remain untouched/unmerged;
- base and final target/adjacent/aggregate JUnit paths, commands, exit codes, timestamps, exact counts, and expected skip;
- exact assertions/results for cap one, cap two, repeated identity, zero, and negative rows across every governed collection;
- AF and full-sidecar ruff baseline/final deltas;
- explicit synthetic/offline/no-network/no-provider/no-production-data receipt;
- smallest-fix rationale, residual risk, and a complete session handoff.

The coordinator must add the final combined-candidate evidence: exact P05 and P06 hashes, P06 patch hash, combined worktree custody, target/adjacent/aggregate JUnits, lint census, path scopes, and no-network receipt. Builder tests alone are not acceptance.

## Review plan

After the exact P06 candidate passes standalone and combined verification, dispatch three fresh read-only reviewers against that exact commit and the combined receipt:

1. Adversarial reviewer A: D8 ordering, occurrence-count semantics, claim attribution/cardinality, status/warning derivation, positive/non-positive boundaries, smallest-change discipline, and adjacent regression risk.
2. Adversarial reviewer B: preservation of the diagnostic scenario/name, unique discarded sentinels, exact five-case census/count receipts, false-green opportunities, AF-P06 scope, P05 hash custody, and combined verification integrity.
3. Separate defensive security reviewer: SG-SIDECAR and SG-SCOPE; negative-slice/set-membership/post-truncation bypasses; provenance/claim/source-ID inflation; discarded-data leakage; mutable/shared-list hazards; package/network/provider boundaries; and any weakening of accepted P05 behavior.

Reviewers do not edit, fix, stage, or commit. Findings are dispositioned as fix, accept-as-documented, or informational. Any code/test rework produces a new P06 candidate hash, invalidates all P06 reviews and the combined receipt, and requires the complete verification/review chain again. Security-review failure blocks Gate 3 unless the human explicitly names and accepts the residual risk.

## Collision risk and sequencing

Collision risk: **Medium**. P05 and P06 have disjoint Allowed Files but share D8 and the same aggregate sidecar suite. No other live parcel may edit `executor.py` or `test_request_constraint_reproduction.py`. P05 must precede P06 in any future Gate 3 merge order. The P06 candidate remains a direct child of the pinned base; only coordinator-controlled combined verification applies both patches before Gate 3.

## Rollback and residual risk

- If later approved and merged under Gate 3, rollback is the single P06 production/test commit after reversing downstream dependencies in the approved order.
- The accepted-source cap does not prevent provider/tool execution before sequence return. It limits only what the current executor accepts and materializes; this is the exact ratified boundary and remains an explicit residual resource risk.
- `maximum_source_count` remains model-valid when zero or negative; P06 safely accepts zero sources rather than adding a new request-validation contract.
- Source quality, deduplication, source-manifest population, and provider payload cardinality are unchanged.

## PR notes

- What changed: executor-local ordered accepted-source cap plus retained/hostile local regressions.
- Why: R-SIDE-02, ratified D8, and plan-review finding F6.
- Risk: provenance/claim mismatch or source-count bypass; controlled by the exact target, combined P05/P06 suite, and separate security review.
- Verification: offline target/adjacent/aggregate receipts, exact lint delta, scope/ancestry checks, and combined-candidate verification.
- Authority: informational notes only. No PR may be opened before exact human Gate 3.

## Step 0 - restate and stop

The builder must send one fresh restatement containing all of the following, then stop for explicit coordinator confirmation:

1. goal, initiative, P06 identifier, Wave 1 risk, and dependency on accepted P05 hash `2f4a092fb7f0ec534996e6ad1116e8b50d612b77` for final combined verification;
2. branch, isolated worktree, pinned base commit/tree, diagnostic commit/test blob, and the rule that the P06 branch starts independently from base;
3. the exact two AF-P06 paths and the rule that every other file is forbidden;
4. the non-negative ordered-occurrence cap; exact bounded tool/artifact/provenance/claim/source-ID/status behavior; duplicate-name cardinality; non-positive fail-closed behavior; and the explicit non-claim about preventing tool execution;
5. the retained method and exact five-case census; standalone target 5, adjacent 39 with one expected skip, aggregate 44; combined targets 15, adjacent 39, aggregate 54; exact offline commands and ruff baselines;
6. SG-SCOPE, SG-SIDECAR, synthetic/no-network/no-provider limits, two adversarial plus separate security review, Gate 3 prohibition, and every stop rule.

Silence is not confirmation. Any discrepancy blocks edits.

## Session handoff contract

The builder returns one concise record containing:

- `P06`, branch, worktree, base/tree, diagnostic source/blob, accepted P05 hash, candidate hash, and parent hash;
- exact changed files and confirmation they equal AF-P06;
- concise production/test changes and preservation of `test_maximum_source_count_bounds_accepted_results`;
- command/exit/count/JUnit table for base, target, adjacent, aggregate, and lint;
- exact cap-row outcomes and discarded-sentinel assertions;
- scope/ancestry/status outputs and offline/no-network receipt;
- open findings/residual risks or exact `none`;
- reviewer-ready diff/evidence pointers;
- explicit statements: local candidate only; not pushed; no PR; not merged; not deployed/released; Gate 3 ungranted; diagnostic branches untouched.

The coordinator verifies every handoff claim against Git and disk. A handoff is not acceptance by itself.

## Stop-and-report rules

Stop without widening scope if:

- local `main` or `origin/main` differs materially from the pinned base/tree, base ancestry fails, or another owner has created the named branch/worktree;
- Step 0 is not explicitly confirmed;
- either AF-P06 file is absent/insufficient or any third file is required;
- the cap requires pre-sequence/provider changes, model validation, a public-contract decision, source deduplication, or behavior outside the exact D8 materialization boundary;
- a correct fix would weaken/delete/skip the retained regression, accept a discarded sentinel, count unique names instead of occurrences, or leave negative slicing/set-membership bypasses;
- target/adjacent/aggregate counts, expected skip, or lint delta differ without coordinator reconciliation;
- the offline environment is unavailable or any provider/network/registry/cloud/production/protected-data path is required or attempted;
- P05 hash/reviews no longer match, the combined patch does not apply cleanly, or combined verification is not exact;
- a security finding cannot close inside AF-P06, or the same tripwire/false-closure condition fires twice;
- any push, PR, merge, deployment, publication, release, provider enablement, diagnostic-branch mutation, or cleanup is proposed before exact human Gate 3;
- a builder or reviewer is asked to operate outside its authorized role.

BioStack does not contain the Foreman emitter/linter required for a `ShapingResult`; this spec intentionally creates no `ShapingResult` JSON. Dispatch remains contingent on coordinator lint, clean isolated base-rooted worktree creation, unchanged base/tree, and a confirmed fresh Step 0.
