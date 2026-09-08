# Codex / Claude GTM collaboration — 2026-09-08

Status: active. This is a coordination and verification handoff, not a source approval or launch clearance. It supplements the existing `docs/INITIATIVES/biostack-production-readiness/` initiative; its older gate observations must not be mistaken for current deployment evidence.

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
