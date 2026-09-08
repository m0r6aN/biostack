# Codex / Claude GTM collaboration — 2026-09-08

Status: active. This is a coordination and verification handoff, not a source approval or launch clearance. It supplements the existing `docs/INITIATIVES/biostack-production-readiness/` initiative; its older gate observations must not be mistaken for current deployment evidence.

**Current checkpoint:** four collaboration rounds are complete. The inventory fix and earlier factual corrections reached main through [PR #289](https://github.com/m0r6aN/biostack/pull/289), merged at `2026-09-08T17:34:15Z`. The later [owner-decision sheet](OWNER-DECISION-SHEET-2026-09-08.md) has now been corrected by Codex and independently reviewed by Claude; it remains unsigned. Codex owns publishing this documentation follow-up and has not merged or deployed anything in this collaboration. The sections below preserve earlier snapshots; the post-merge evidence supersedes their current-state counts. Scheduled technical follow-up remains active.

## Scope and ownership

Clint explicitly requested monitoring and collaboration with the existing Claude desktop conversation, **BioStack go-to-market status**, to resolve GTM blockers. Codex verified that conversation and its visible **Opus 5 / High** label, read the completed response, and submitted a bounded follow-up. Claude owns preparing the existing six-file source-review set as separate reviewable commits and a draft PR, plus `CLAUDE-GTM-BLOCKER-HANDOFF-2026-09-08.md`. Codex owns independent verification and this handoff. Neither agent is recording owner/legal approval, changing authorization bindings, enabling sources, deleting evidence, merging or deploying.

Starting checkout: `lead/source-authorization-v2-proposal-20260908` at `defdf97`, with the six pre-existing source metadata, inventory and rights-document changes. This review does not reinterpret earlier agent-authored documents as instructions from Clint. The pending source decisions remain pending; the user's commercial/no-paid-licences constraint remains authoritative.

## Independent validation

The full KnowledgeWorker suite was rebuilt into an isolated output path, avoiding Claude's default build directories, and run against the current checkout:

```powershell
dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --artifacts-path backend/tests/BioStack.KnowledgeWorker.Tests/bin/codex-gtm-20260908 --logger 'trx;LogFileName=worker-tests.trx' --results-directory research/output/codex-gtm-collaboration-20260908 --nologo
```

**875 passed, 8 failed, 0 skipped, 883 total; test duration 2m 11s.** This independently reproduces Claude's reported baseline. Evidence: `research/output/codex-gtm-collaboration-20260908/worker-tests.trx` (ignored local output). SDK: 10.0.303. `git diff --check` passed before this handoff was added. No deployed worker or authenticated customer journey was exercised.

| Failure | Observed assertion | Meaning / next action |
|---|---|---|
| `ResearchArtifactValidatorTests.Validator_Accepts_Governed_Pilot_Source_Registry` | 13 vs 30 registry records | Historical corpus expectation; inspect all following assertions before changing a count. |
| `CorpusIdentityInventoryBuilderTests.Build_CurrentRepository_RecordsIdentityCoverageWithoutPromotionAuthority` | 13 vs 30 registry records | Historical inventory expectation; does not itself verify approval. |
| `StructuralEvaluationReportBuilderTests.Build_CurrentArtifacts_RecordsPartialPolicyWithoutVerdict` | 13 vs 30 registry records | Same; subsequent rights/operations/packet-count assertions also need explicit reconciliation. |
| `StructuralEvaluationReportBuilderTests.BuildJson_CurrentArtifacts_IsStableAndOmitsRunTimeClaims` | 7 vs 26 recorded approved-rights flags | Counts registry declarations, not 26 verified approvals. Do not relabel these as authorized sources. |
| `SourceAcquisitionPlanningTests.PlanBuilder_Real_Registry_Raw_Byte_Hash_Matches_Decision_Binding` | Registry and v1 hashes differ | Genuine authorization-binding blocker. Preserve the issued record. |
| `ResearchArtifactValidatorTests.Validator_Accepts_Recommended_Seven_Source_Decision_Batch_With_Exact_Registry_Binding` | Same hash mismatch | Same blocker, inverse assertion. |
| `SourceAcquisitionPlanningTests.PlanBuilder_CurrentArtifacts_Produce_Ready_Intents` | 490 expected, 0 actual | Current registry is not authorized by the bound batch; denial is expected. |
| `SourceAcquisitionRuntimeTests.Job_validates_current_exact_inputs_and_preflight_without_transport` | Preflight differs from 70/490/490/0/7 | Downstream manifestation of the authorization denial. |

The eight failures should not all be described as the same mechanism. Four concern historical inventory declarations; four concern authorization binding and its consequences. Repairing a test's data model does not grant rights, and green tests alone must not be used to bypass the explicit authorization/deployment gate. A historical positive fixture plus a current-registry denial case is a possible design, pending bounded review; it has not been implemented here.

## Inventory finding to reconcile with Claude

Independent enumeration found **634 source occurrences, 621 source IDs, and 10 IDs whose packet metadata differs between occurrences**. Current `collect_corpus` retains only the first occurrence of each ID. It therefore omits later retrieval dates and title/publisher declarations from the inventory, despite the packet originals remaining intact. `pubchem-cid-44200882` also has differing recorded URLs. This is a real limitation for item/version-level rights review, not a reason to delete or rewrite packet evidence.

Recommended bounded fix: retain every source occurrence with its packet and original metadata, report differing fields, and keep current aggregate usage counts explicit. Preserve alias, query, path-case, section and date identity. Add behavioral verification for two packets sharing an ID with different dates/URLs, with no corpus or authorization mutation. Until that exists, consult the original packets for item/version decisions.

Independent report: `research/output/codex-gtm-collaboration-20260908/inventory-independent-review.json`. Shared-URL groups remain **10 groups / 50 members**. No duplicate claim IDs were found across the current packets.

Inspected hashes (before any subsequent edits):

- Script: `085c7c3cf923947f9ecdb42d0dfcbadcb5dc5b760ee24312e235f811a65b4536`.
- Generated inventory: `515ffd0d33fddcced818043ad970eee54df64ca819f1e8f46072b243f8871c94`.
- Registry: `79aa44c5e8139de87a220da86b65e95d135a6598e4f0b41b3e066eb064e7c0e2`.
- Issued v1 registry binding: `3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28`.

The script's `coveredByDecisionBatch` records membership only. It is not an evaluation of stage approvals, identity, binding, conditions or item rights. Its current disclaimer acknowledges that limitation; downstream uses must preserve it.

## Current hosted evidence

Read-only GitHub inspection found these completed main runs at `44ec029f177a3d39f0be569c4d7bdb79f6312499`, created September 8 at 11:28 UTC:

- [Deploy to Azure Container Apps](https://github.com/m0r6aN/biostack/actions/runs/34220861253): failed at **Run backend tests**; Azure login, image pushes and container updates were skipped.
- [Structural Evaluation Report Artifact](https://github.com/m0r6aN/biostack/actions/runs/34220861193): failed at **Verify structural report contract**; generation/upload were skipped.
- [Secret scan](https://github.com/m0r6aN/biostack/actions/runs/34220861134) and [offline verification kit](https://github.com/m0r6aN/biostack/actions/runs/34220861113): succeeded. Those results do not clear the failed gates.

These observations supersede reliance on older successful runs for the current main candidate. They do not imply the existing production site is down. The September 5 GTM handoff's authentication and database-refresh acceptance gaps remain unverified by this session.

## Next safe actions

1. Receive Claude's completed response and verify the PR, commits, tests and exact scope. Do not interrupt an active response or overwrite a user draft.
2. Reconcile the inventory occurrence finding and test categories; implement only bounded technical fixes agreed between the agents, preserving denial behavior and issued evidence.
3. Consolidate concrete owner decisions from the rights assessment/addendum: role succession, scoped permitted sources and access methods, retained-content handling, and exact authorization issuance. Do not ask for another blanket approval of all sources.
4. Recheck the resulting candidate in hosted CI, then identify remaining environment and owner gates. Keep overall launch status **HOLD / not established** until the applicable release evidence exists.

Automation `biostack-claude-blocker-follow-up` continues this task every 15 minutes, quiet while unchanged. It should pause on completion or a concrete user-action dependency and report that dependency once. The desktop must remain available and the intended conversation must be re-verified on each run.

## Round 2 — independent verification of the remediation

Claude opened [draft PR #288](https://github.com/m0r6aN/biostack/pull/288), initially at `f01895d`. Codex reviewed the PR and returned concrete corrections in a second message after Claude's response completed. The first round's [collaboration receipt](receipts/codex-claude-gtm-2026-09-08-round1.json) records partial acceptance and preserved dissent; it passed the interactive-session receipt validator.

The subsequent inventory implementation at script SHA-256 `ab86a43ce71165629e34e38420afbb52b91146c623e1fd53650c118800be94ae` was independently checked against every original source occurrence and every nonempty quote locator in the corpus. **All 634 occurrence records match their source metadata, and all 1,329 quote locators match their original packet, claim ID, section and character count.** The report adds no copied quote text. There remain 621 source IDs, 10 IDs with metadata variants, 1,014 claim citations and 160,067 stored quote characters. The newly preserved second URL for `pubchem-cid-44200882` increases the grouping to **582 normalized URL groups / 542 cited groups**. These counts supersede the earlier first-occurrence projection for this new format; the historical snapshots above remain evidence of what they counted.

Codex ran the new behavioral suite: **11 passed** with pytest 9.1.1. Tests cover metadata variants, query/path identity, aggregate usage, excerpt locators and absence of authorization derived from grouping. Independent full-corpus comparison evidence is in `research/output/codex-gtm-collaboration-20260908/inventory-v2-verification.json` and the separate `source-inventory-v2-verified.json`. The earlier 883-test worker baseline remains applicable to the unchanged worker/registry bytes; the new Python tests do not clear its eight failures.

The historical-registry correction is independently proved: `git show cbaed8e:research/input/sources/pilot-source-registry.json` hashes to exactly the issued v1 binding and contains 13 lanes with 7 approved/enabled. The assertion that reverting that historical file would reactivate 17 newer lanes was incorrect. This observation is not an instruction to revert it. The current 26 approved flags comprise 7 original plus 19 additional declarations; the two rejected placeholders are separate changes.

All seven GSRS identities were also verified in the rendered public NCATS interface. [The identity-verification note](GSRS-PUBLIC-IDENTITY-VERIFICATION-2026-09-08.md) records their exact names, UNIIs, versions and public-definition status. This narrows S1's remaining work to the actual field/excerpt and acquisition scope; it does not issue permission or approve a provenance migration.

Additional read-only live observations: the API health endpoint returned 200 at `2026-09-08T17:15:09Z`, and the sign-in page returned 200 at `17:15:29Z`. An unauthenticated HTTP request to the session route also returned 200, but this check does not establish its authentication semantics or an authenticated user journey. No existing production outage is inferred from the failing main deployment workflow.

Next checkpoint: verify Claude's completed correction commit and PR body, preserve any remaining material dissent, and prepare only concrete owner decisions after the independent technical work is complete. Overall GTM readiness remains unestablished.

## Post-merge verification and Round 3

Claude's completed second response delivered `0a9ad8c` (occurrence retention and tests), `b0b6148` (factual corrections) and `bbcd3a8` (post-merge baseline). Codex read the completed response and independently verified the four-file diff in draft PR #289. The [second receipt](receipts/codex-claude-gtm-2026-09-08-round2.json) accepts this bounded remediation, with the remaining test-design disagreement explicit.

Main advanced concurrently, adding a registry alias and new packet content. Codex re-ran the full worker suite against the resulting checkout: **881 passed, 8 failed, 0 skipped, 889 total; 2m 38s.** Independent comparison of the two TRX files confirms exactly the same eight failure names. The six additional passing tests arrived with the registry-backed field-authority work. Evidence: `research/output/codex-gtm-collaboration-20260908/worker-tests-post-merge.trx`.

A fresh inventory generated at `2026-09-08T17:34:39Z` contains **636 source occurrences, 623 source IDs, 584 normalized URL groups, 1,018 claim citations and 1,343 stored quotes (160,804 characters)**. There are 10 IDs with metadata variants and 285 unregistered IDs. Codex independently compared every occurrence's original metadata and every nonempty quote locator to the current packets; all match. These additions explain the change from the pre-merge snapshot and were not authored by this inventory fix.

- Script SHA-256 remains `ab86a43ce71165629e34e38420afbb52b91146c623e1fd53650c118800be94ae`.
- New registry SHA-256: `248d8f02d9c812f524e7fa48eba3971de8de17b936537d74df15a82898445d8f`.
- Independently generated inventory SHA-256: `326fd1742f2727679d764353276dccc7b4e08bd58ef1d23e669f515d69741010`.
- Issued v1 binding remains `3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28`; mismatch and denial are preserved.
- Independent comparison report: `research/output/codex-gtm-collaboration-20260908/inventory-post-merge-verification.json`.

After the second response completed, Codex sent a third bounded request: identify actual distinct-person requirements, compare the seven S1 excerpts against verified GSRS identity scope, and produce an unsigned owner-decision sheet with specific recommendations and choices. No role assignment, source approval, activation, provenance migration, worker-test change or binding replacement is authorized by that request. Claude owns his new decision sheet and handoff changes; Codex owns this document, the GSRS verification note and the receipts. Files must be staged explicitly after each author reads the diff.

## Decision-sheet review completed; next scheduled work

Claude produced `7cf7f1d` after PR #289 had already merged. Codex rejected that first version: it missed the explicit NCCIH distinct-reviewer guard, inferred a CAS restriction without finding an applicable notice, and reopened an afamelanotide finding without reading its existing co-citations. The [third receipt](receipts/codex-claude-gtm-2026-09-08-round3.json) records partial acceptance, not approval. The problematic new assertions were branch-only; they had not reached main through PR #289.

Codex took ownership of the correction edits and Git work, then requested a read-only verification from Claude. Claude confirmed all five factual corrections and reviewed the revised document. His useful wording concern was incorporated: broader GSRS fields are **outside the proposed field set**, not declared prohibited. The [fourth receipt](receipts/codex-claude-gtm-2026-09-08-round4.json) records acceptance of that bounded review. All four receipts pass the session-receipt validator. No owner decision was inferred from the conversation or the suggested text in Claude's empty composer.

The decision sheet now distinguishes the source-rights successor from the independent NCCIH capture reviewer, preserves earlier decisions as history, and presents scoped proposals for GSRS, DrugBank handling, ChEMBL excerpts and placeholder retirement. Additional GSRS field verification is agent work. The remaining test-design disagreement is explicit; this documentation changes no worker test, binding, registry, evidence packet or production state.

The initial PR #289 checks at `bbcd3a8` still failed Build & Deploy and the structural-report job, while secret scan and the offline verification kit passed. A merge is not evidence of a passing release gate. The 881/8 worker baseline above is the latest independently executed suite in this collaboration.

**Next heartbeat priorities:** first verify the intended Claude conversation and fresh Git state. Then continue the narrow public-GSRS field/notice comparison for the two cited CAS/formula excerpts, preserving the original precisionFDA provenance and unresolved IGF-1 LR3 identifier discrepancy. If that lane is waiting on an external source, a read-only URL-group review map can be prepared from the existing inventory, retaining every alias, packet occurrence, date and citation locator. Do not turn the 285 unregistered IDs into a blanket registration backlog. Return only concrete additional scope decisions after the evidence is ready. These tasks need no fabricated source approval and can progress while the owner considers the unsigned sheet.

Keep the 15-minute heartbeat active and quiet while unchanged. Pause it only when the bounded collaboration is complete or meaningful further progress actually depends on user action; record that action once. The desktop conversation may auto-archive after completion, so re-verify or locate the same session rather than opening an unrelated one. The overall go-to-market release status remains **HOLD / not established**.
