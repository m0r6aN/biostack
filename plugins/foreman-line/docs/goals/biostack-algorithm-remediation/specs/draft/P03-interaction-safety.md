# Parcel P03: Interaction Safety

Status: **blocked — target/adjacent/Application green; stale outside-AF API fixture requires Narrow Gate 1 Amendment 04**

Coordinator lint: **PASSED 2026-09-02.** AF-P03, base/tree, production and diagnostic blobs, all six amended D6 clauses, the 4 retained plus 6 hostile-case census, the independent 20-case adjacent filter, the 609-passed/5-skipped Application baseline, count deltas, offline commands, and single-review depth were checked against the pinned repository and ratified amendment. Dispatch authority remains contingent on a clean isolated worktree and exact Step 0 restatement.

Import-receipt correction: **PASSED 2026-09-02 after a pre-edit builder stop.** Because the reproduction path is absent from the pinned index, restoring it into the worktree creates an untracked file; pre-commit `git diff --name-only` correctly emits nothing. The import tripwire therefore uses `git status --short --untracked-files=all` plus `git ls-files --others --exclude-standard` and requires exactly the one AF-P03 test path. This changes no invariant, Allowed File, case/count expectation, or Gate authority.

Aggregate disposition: **BLOCKED 2026-09-02.** The scoped uncommitted candidate passed target `10/10`, adjacent `20/20`, and Application `619 passed / 5 skipped`, but the full solution failed one API case whose graph-positive helper publishes a `provisional` artifact. D6 requires exactly `reviewed`; production must not be weakened. Amendment 04 proposes adding only `CompoundGraphIntelligenceTests.cs` to AF-P03 and changing only that stale fixture state. Preserve the current two-file uncommitted state; no further edit, test, stage, or commit is authorized before ratification and an amended Step 0 restatement.

## Goal

Remediate only the four reproduced interaction-intelligence defects and the hostile coverage required by ratified D6 Amendment 01. Preserve the diagnostic scenarios as regressions, add the smallest production repair inside `InteractionIntelligenceService.cs`, and prove that safety metadata, graph provenance, canonical identity, and confidence bounds remain fail-closed under offline deterministic verification. This parcel does not authorize a push, pull request, merge, deployment, release, publication, or mutation of any diagnostic branch.

## Initiative / Project / Wave

- Initiative: `biostack-algorithm-remediation`
- Project track: `BioStack` / backend `BioStack.Application`
- Parcel: `P03-interaction-safety`
- Wave: Wave 1 — independent fixes
- Risk/routing: critical safety semantics and high provenance/scoring integrity; frontier implementation routing
- Production owner: `InteractionIntelligenceService` (module/class ownership only; the pinned base has no repository `CODEOWNERS` file)

## Branch / Worktree / Base

- Branch: `codex/biostack-remediation-p03`
- Isolated worktree: `C:\Users\clint\.codex\worktrees\biostack-remediation-p03\BioStack`
- Required starting commit: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Required starting tree: `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`
- Diagnostic interaction commit: `56b7ad229a34a6f151f5bf7b96a8bfdcbce8132f`
- Diagnostic reproduction-test blob: `a234e398b4d8f226ee43baf0ecb69f352c584307`
- Pinned-base production blob: `3ce1f4075818b1e3eb2ec205dc059aba68905fa6`

The parcel branch must be created from the required starting commit, never from a diagnostic branch or another remediation candidate. The diagnostic commit is an evidence source only. Do not merge, cherry-pick, rebase, or otherwise incorporate its history.

## Dependencies and Preconditions

1. Original Gate 1 D1-D14 and Narrow Gate 1 Amendment 01 are ratified. The replacement D6 in `gate-1-amendment-01.md` is the controlling interaction contract. Amendment 02 does not alter P03.
2. Contingent Gate 2 covers P03 only after the coordinator lints this spec, verifies an isolated branch/worktree at the exact base/tree, confirms AF-P03, and approves the builder's Step 0 restatement.
3. The coordinator owns the required `origin/main` comparison. The builder must not fetch. If the coordinator reports material movement from the pinned base, stop under the charter's base-change rule.
4. P03 has no implementation dependency on P01, P02, P04-P08, or Q01-Q04, and it must consume no candidate branch. No other live builder may edit either AF-P03 path.
5. The diagnostic reproduction file exists at the diagnostic commit and is absent at the pinned base. `backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj` is an SDK-style `net10.0` xUnit test project that includes ordinary `.cs` files by default. No project-file change is needed or allowed.
6. Required .NET SDK and NuGet packages must already be available locally. If the fresh worktree lacks ignored restore metadata, only the coordinator may seed `project.assets.json`, `project.nuget.cache`, and generated NuGet props/targets from a clean ambient worktree proven to have the identical base/tree, with a relative-path and SHA-256 manifest. This is environment provisioning, not source authority. No restore or registry/network access is authorized.

## Integration Surfaces

- `InteractionIntelligenceService` pair evaluation: explicit `KnowledgeEntry.AvoidWith` safety metadata, reviewed graph relationships/artifacts, stored `CompoundInteractionHint`, and fallback rules.
- `EvaluateByNamesAsync` resolution: caller-supplied names/aliases resolve through `IKnowledgeSource` and must collapse to one canonical compound identity before pair generation.
- `InteractionResultResponse.Confidence`: graph-provided confidence enters scoring, sorting, summaries, findings, counterfactuals, and swap evaluation only after finite bounded mapping.

This is a repo-local service parcel. It changes no endpoint, persistence schema, public response shape, or provider boundary.

## Security and Release Gates

- **SG-SCOPE applies:** exactly AF-P03 may change; all fixtures are synthetic/local; no secrets, protected data, payload dumps, production/cloud access, external provider calls, or unapproved network activity.
- D10 applies: run only offline tests with Moq/local in-memory objects and `--no-restore`. Do not run `dotnet restore`, `git fetch`, `git pull`, or any command that can contact a package registry, provider, cloud resource, production database, or remote Git host.
- D11 as amended applies: P03 requires one fresh independent read-only adversarial review of the exact final candidate commit. P03 is not listed for dual adversarial or separate defensive security review. A security-relevant finding still blocks Gate 3 until dispositioned.
- Gate 3 remains ungranted and human-only. No push, PR, merge, deployment, provider enablement, publication, release, or diagnostic-branch cleanup is authorized.
- Passing builder tests alone does not clear independent review, Gate 3, merge, deployment, or release.

## Allowed Files — AF-P03

Exactly these two repository paths may be created or modified:

1. `backend/src/BioStack.Application/Services/InteractionIntelligenceService.cs`
2. `backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs`

If either path is missing, insufficient, or cannot carry the complete ratified D6 contract, stop for a charter/spec amendment. Do not substitute a nearby file.

## Forbidden and Out of Scope

- Every repository path outside AF-P03 is forbidden, including project/solution files, graph entities, graph-store interfaces/implementations, repositories, migrations, contracts/responses, endpoint code, package/lock files, build configuration, goal documents, fixtures, generated files, and handoff records.
- Do not merge or cherry-pick diagnostic commit `56b7ad229a34a6f151f5bf7b96a8bfdcbce8132f`; restore only the one AF-P03 test file's contents.
- Do not mutate, rebase, force-update, delete, or clean the diagnostic coordination branch or any diagnostic parcel branch.
- Do not rename, delete, skip, weaken, or replace the four diagnostic scenarios or their assertions. The original failing conditions must remain observable regressions.
- Do not add packages, dependencies, schemas, migrations, configuration, feature flags, endpoints, response fields, telemetry, provider calls, network fixtures, or test-only branches in production code.
- Do not modify graph persistence semantics, publish/activate graph artifacts, redefine graph relationship types, change stored hint data, or expand the public interaction response contract.
- Do not infer that `PairsWellWith`, `CompatibleBlends`, shared pathways, drug-interaction free text, or unknown relationships establish safety. This parcel changes only ratified precedence/eligibility/deduplication/confidence behavior.
- Do not redesign scoring, counterfactuals, swaps, complementary-domain detection, top-finding ordering, reason strings, or unrelated pair inference.
- Do not create a `ShapingResult` JSON. BioStack does not vendor the Foreman permission-profile emitter/linter required to author one.
- Do not stage or commit during shaping. A future builder may make one local candidate commit only after explicit coordinator Step 0 confirmation and a green required verification chain.

## Verified Existing Patterns on the Pinned Base

All line references are to commit `339f259b1a467034db4f57cf9d774c292f11b53a`.

- `backend/src/BioStack.Application/Services/InteractionIntelligenceService.cs:67-81` removes duplicate input strings before lookup but appends every resolved `KnowledgeEntry`; canonical and alias inputs that resolve to the same canonical compound therefore remain duplicated.
- `InteractionIntelligenceService.cs:155-203` computes pair data in this order: graph, stored hint, then `AvoidWith`. This directly permits positive graph or hint intelligence to preempt explicit safety metadata.
- `InteractionIntelligenceService.cs:272-304` accepts any relationship returned by `ICompoundGraphStore.FindRelationshipAsync`, then reads only an active artifact hash. It does not require a present active artifact, `IsActive == true`, artifact `ReviewState == "reviewed"`, relationship `ReviewState == "reviewed"`, or `NeedsReview == false` before emitting `IntelligenceSource.Graph`.
- `InteractionIntelligenceService.cs:306-321` caches only the active artifact hash, including the no-artifact case. Any repair must keep provenance aligned with the eligible reviewed active artifact and must not serve an ineligible edge merely because a hash is cached.
- `InteractionIntelligenceService.cs:338-360` invariant-parses numeric graph confidence and applies `Math.Clamp`, but `double.TryParse("NaN", ...)` succeeds and `Math.Clamp(double.NaN, ...)` remains non-finite.
- `backend/src/BioStack.Infrastructure/Knowledge/ICompoundGraphStore.cs` is read-only context: its documented runtime surface exposes `GetActiveArtifactAsync` plus an active-artifact relationship lookup. It is outside AF-P03 and must not change.
- `backend/src/BioStack.Domain/Entities/Graph/CompoundGraphArtifact.cs` exposes `ArtifactHash`, `ReviewState`, and `IsActive`; `CompoundGraphRelationship.cs` exposes `ReviewState` and `NeedsReview`. Both are outside AF-P03.
- Existing adjacent coverage consists of 2 `InteractionIntelligenceServiceTests`, 2 `InteractionIntelligencePublicBoundaryTests`, and 16 `SwapRecommendationTests`, for 20 discovered/passing cases at the pinned base.
- The clean ambient checkout at the identical pinned commit/tree produced 609 passed, 5 skipped, 0 failed, total 614 for `BioStack.Application.Tests`. These counts are a tripwire and must be independently reproduced in the isolated parcel worktree before import.
- The pinned-base solution receipt is: `BioStack.Domain.Tests` 7 passed; `BioStack.Application.Tests` 609 passed plus 5 skipped; `BioStack.ProtocolOperationsExportBundleVerifierCli.Tests` 158 passed; `BioStack.Api.Tests` 377 passed; `BioStack.KnowledgeWorker.Tests` 866 passed. The builder must re-establish this baseline in the isolated local-assets-only environment rather than treating this document as execution evidence.

## Exact Ratified D6 Contract

The following clauses are exhaustive for P03 and must be satisfied together.

1. **Safety precedence — R-INT-01.** A named match in either compound's explicit `AvoidWith` metadata is evaluated before graph lookup and before stored pair-hint lookup. It returns the existing interfering safety result and cannot be weakened by a positive graph edge or a positive stored hint. The graph and hint repositories must not be consulted for that pair once the explicit safety result is established.
2. **Reviewed active graph eligibility — R-INT-02 and Amendment 01.** Graph intelligence is eligible only when the current artifact exists, `IsActive` is true, the artifact `ReviewState` is exactly ordinal `reviewed`, the relationship `ReviewState` is exactly ordinal `reviewed`, and `NeedsReview` is false. Missing, inactive, non-reviewed, or case-variant artifact/edge review states are ineligible. An ineligible graph path falls through to the next eligible source; it may not emit `Source == Graph` or cite the ineligible artifact hash.
3. **Independent `NeedsReview` denial.** `NeedsReview == true` makes an edge ineligible even when both artifact and edge review states are exactly `reviewed`. The retained reproduction where edge state and the flag are both ineligible must remain, and the new hostile case must isolate the flag.
4. **Canonical-identity deduplication — R-INT-03.** After name/alias resolution and before pair generation, entries are unique by canonical compound identity using the service's canonical name with ordinal case-insensitive comparison. Canonical and alias inputs resolving to the same compound yield no self-pair. Input-string deduplication alone is insufficient.
5. **Finite bounded graph confidence — R-INT-04.** Every graph-backed confidence entering a response is finite and in `[0,1]`. Numeric values below/above the range remain clamped by the existing behavior. Non-finite numeric inputs such as `NaN` must be rejected or normalized to a finite bounded fallback; they may never propagate into interaction scores, ordering, findings, counterfactuals, or swaps.
6. **Next-source semantics.** After an ineligible graph, evaluation continues in the existing precedence order: an eligible stored hint, then existing metadata/rule fallbacks. This does not elevate hints over explicit `AvoidWith` and does not turn unknown/legacy pairing metadata into a safety claim.

Preserve behavior unrelated to these clauses, including public-boundary omission of action scenarios, legacy pairing metadata remaining `Unknown`, shared/complementary pathway logic, drug-interaction fallback, summaries, composite scoring, top-finding ordering, counterfactuals, swaps, cancellation propagation, and existing reason/response shapes.

## Diagnostic Import and Required Regression Shape

After Step 0 is confirmed and while `HEAD` is still the pinned base, import exactly one worktree file without merging diagnostic history:

```powershell
git restore --source=56b7ad229a34a6f151f5bf7b96a8bfdcbce8132f --worktree -- backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs
git hash-object backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs
```

The initial hash must be `a234e398b4d8f226ee43baf0ecb69f352c584307`. If not, stop. Retain the class name, four `[Fact]` scenarios, method names, synthetic inputs, and assertions. The test file may then be extended only with the six Amendment-01 hostile cases below and the smallest helper changes needed to express them; the final test-file blob is expected to differ from the diagnostic blob, so preservation is proved by assertion/method inspection and diff review, not a final blob-equality check.

Retained reproduced cases, one discovered case each:

1. `EvaluateAsync_AvoidWithSafetySignal_OutranksPositiveGraphEdge`
2. `EvaluateAsync_NeedsReviewGraphEdge_IsNotServedAsReviewedIntelligence`
3. `EvaluateByNamesAsync_CanonicalNameAndAlias_DoNotCreateSelfPair`
4. `EvaluateAsync_NonFiniteGraphConfidence_IsRejectedOrNormalized`

Add exactly these hostile regressions:

5. `EvaluateAsync_AvoidWithSafetySignal_OutranksPositiveStoredHint` — one case; both a positive graph edge and positive stored hint are configured, explicit `AvoidWith` returns interfering, and both repository mocks are verified with `Times.Never` so neither source can authorize or replace the safety result.
6. `EvaluateAsync_IneligibleGraphArtifact_DoesNotAuthorizeGraphIntelligence` — three independently discovered data rows: missing artifact; `IsActive == false` with exact reviewed state; active artifact with `ReviewState == "provisional"`. Each uses an otherwise reviewed, `NeedsReview == false`, positive edge and proves no graph-backed result. At least one row supplies an eligible stored hint and proves next-source fallthrough.
7. `EvaluateAsync_NonReviewedGraphEdge_DoesNotAuthorizeGraphIntelligence` — one case with a present active exactly reviewed artifact, a relationship whose `ReviewState == "Reviewed"`, and `NeedsReview == false`; no graph-backed result. This case proves that case-insensitive acceptance is forbidden by the word “exactly.”
8. `EvaluateAsync_NeedsReviewFlag_IsIneligible_WhenArtifactAndEdgeAreReviewed` — one case with a present active exactly reviewed artifact and exactly reviewed relationship but `NeedsReview == true`; no graph-backed result.

The class therefore has exactly 10 discovered cases: 4 retained facts plus 6 new hostile cases (one fact, one three-row theory, and two facts). If the builder needs a different test name, row count, or scenario to implement D6, stop for coordinator review before editing.

## Implementation Constraint

Make the smallest coherent change to `InteractionIntelligenceService.cs` that enforces the exact D6 contract. Prefer narrow private helpers and existing control flow. Do not add a public API, alter dependency injection, or change an interface/entity. Do not special-case synthetic names or test inputs. If the contract cannot be implemented within the one production file while all 10 cases and adjacent behavior remain green, stop for amendment.

## Required Tests and Acceptance Criteria

- All four retained diagnostic methods compile, are discovered, and pass without weakened assertions.
- All six additional hostile cases compile, are independently discovered, and pass.
- Targeted class: 10 passed, 0 failed, 0 skipped, total 10.
- Safety precedence is observable against both a positive graph edge and a positive stored hint; explicit `AvoidWith` cannot be replaced by either.
- Graph output is impossible for missing, inactive, non-reviewed, or `NeedsReview` graph state, and ineligible graph state falls through without exposing graph provenance.
- Canonical plus alias resolution does not generate a self-pair.
- Graph confidence in every governed case is finite and within `[0,1]`.
- Adjacent interaction/public-boundary/swap suite remains exactly 20 passed, 0 failed, 0 skipped.
- Full `BioStack.Application.Tests` is exactly 619 passed, 5 skipped, 0 failed, total 624: the pinned 614 total plus exactly 10 P03 cases.
- Backend solution aggregate is green offline. Only `BioStack.Application.Tests` gains 10 passed/discovered cases; Domain remains 7, verifier remains 158, API remains 377, and KnowledgeWorker remains 866, with no new skips.
- Exactly the two AF-P03 paths differ from base; no staged, modified, or untracked path exists outside AF-P03.
- Candidate descends from the pinned base, contains no merge commit from diagnostic history, and diagnostic commit `56b7ad2...` is not its ancestor.
- `git diff --check` is clean, and no command contacted an external service.

## Exact Verification Commands

`rtk` was not resolvable in the shaping shell. The raw commands below are the repository AGENTS.md debugging fallback. If `rtk` is available to the builder, each command may use its equivalent `rtk` prefix. Do not remove `--no-restore` from test commands.

Run from the P03 worktree root.

### Baseline receipt after Step 0 confirmation, before import or edits

```powershell
git rev-parse HEAD
git rev-parse 'HEAD^{tree}'
git status --short
git rev-parse 339f259b1a467034db4f57cf9d774c292f11b53a:backend/src/BioStack.Application/Services/InteractionIntelligenceService.cs
git cat-file -e 339f259b1a467034db4f57cf9d774c292f11b53a:backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.Services.InteractionIntelligenceServiceTests|FullyQualifiedName~BioStack.Application.Tests.Services.InteractionIntelligencePublicBoundaryTests|FullyQualifiedName~BioStack.Application.Tests.Services.SwapRecommendationTests" --disable-build-servers --logger "console;verbosity=minimal"
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --disable-build-servers --logger "console;verbosity=minimal"
dotnet test backend/BioStack.sln --no-restore --verbosity minimal --disable-build-servers
```

Expected receipts: HEAD/base and tree equal the pinned values; status is empty; production blob equals `3ce1f4075818b1e3eb2ec205dc059aba68905fa6`; `git cat-file -e` exits 128 because the reproduction file is absent. Adjacent is exactly 20/0/0/20. Application is 609 passed, 5 skipped, 0 failed, total 614. Solution project counts match the pinned receipt above. Any mismatch, missing local asset, restore attempt, network attempt, or baseline failure stops the parcel.

### Diagnostic import identity

```powershell
git restore --source=56b7ad229a34a6f151f5bf7b96a8bfdcbce8132f --worktree -- backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs
git hash-object backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs
git status --short --untracked-files=all
git ls-files --others --exclude-standard
```

Expected before extension: hash `a234e398b4d8f226ee43baf0ecb69f352c584307`; status is exactly one untracked AF-P03 reproduction test and the untracked-file listing contains exactly that path. Empty `git diff --name-only` output at this stage is normal and is not scope evidence because the path is not yet indexed.

### Targeted regression

```powershell
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.Services.InteractionIntelligenceReproductionTests" --disable-build-servers --logger "console;verbosity=minimal"
```

Expected candidate receipt: passed 10, failed 0, skipped 0, total 10. Before the production repair, the retained/new cases must expose the governed behavior; record assertion-level failures, but do not commit a red state. A harness/build/environment failure is not diagnostic evidence and stops the parcel.

### Adjacent interaction, public-boundary, and swap suites

```powershell
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.Services.InteractionIntelligenceServiceTests|FullyQualifiedName~BioStack.Application.Tests.Services.InteractionIntelligencePublicBoundaryTests|FullyQualifiedName~BioStack.Application.Tests.Services.SwapRecommendationTests" --disable-build-servers --logger "console;verbosity=minimal"
```

Expected candidate receipt: passed 20, failed 0, skipped 0, total 20. The filter names the three existing classes individually, so this adjacent count remains independent of the 10 targeted reproduction cases.

### Application and backend-solution aggregate

```powershell
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --disable-build-servers --logger "console;verbosity=minimal"
dotnet test backend/BioStack.sln --no-restore --verbosity minimal --disable-build-servers
```

Expected Application receipt: 619 passed, 5 skipped, 0 failed, total 624. Expected solution delta: only Application gains 10; Domain stays 7, verifier 158, API 377, KnowledgeWorker 866. Record every assembly summary and exit code. The later goal-level aggregate may consume its separately mandated command only in an already dependency-ready, network-prohibited environment; P03 grants no implicit restore/network authority.

### Diff, ancestry, scope, and cleanliness receipts after candidate commit

```powershell
git merge-base --is-ancestor 339f259b1a467034db4f57cf9d774c292f11b53a HEAD
git merge-base --is-ancestor 56b7ad229a34a6f151f5bf7b96a8bfdcbce8132f HEAD
git rev-list --merges 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff --check 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff --name-only 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff -- backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs
git status --short
```

Expected results: base-ancestor exit 0; diagnostic-ancestor exit 1; merge list empty; diff-check exit 0; name-only output exactly the two AF-P03 paths; reproduction diff visibly retains all four original methods/assertions and adds only the six specified cases/helper support; final status empty after one local candidate commit. Capture native exit codes explicitly because empty output alone is not proof.

## Count and Scope Tripwire Method

1. Reproduce the exact clean-base counts in the isolated worktree before importing the diagnostic file: adjacent 20; Application 609 passed plus 5 skipped; solution counts 7/609(+5 skipped)/158/377/866 by the named assemblies.
2. Confirm the diagnostic import initially contains exactly four `[Fact]` cases and matches blob `a234e398...`.
3. Extend the file to exactly 10 discovered cases using the specified 4 retained plus 6 hostile row layout.
4. Candidate targeted must be 10/0/0/10; adjacent stays 20/0/0/20; Application becomes 619 passed plus 5 skipped, total 624; only the Application assembly gains 10 in the solution run.
5. Any missing case, extra row, changed skip, unexpected failure, other-assembly count drift, or status/diff path outside AF-P03 stops the parcel for reconciliation. Do not change filters or assertions to force the counts.

Do not add repository receipt files. Preserve command output and explicit exit codes in the builder's handoff/evidence channel for coordinator consumption.

## Evidence Required

- Starting branch/worktree, commit/tree, clean status, base production blob, absent base test path, and restore-metadata manifest if seeding occurred.
- Diagnostic import command, commit, initial imported blob hash, and assertion-level proof that all four original scenarios remain in the final diff.
- Candidate commit, `git show --stat --oneline`, and complete diff limited to AF-P03.
- Baseline/candidate table for targeted, adjacent, Application, and solution runs: command, passed, failed, skipped, total, and exit code.
- D6 clause-by-clause result, including zero preemption of explicit `AvoidWith`, graph eligibility denials/fallthrough, canonical deduplication, and finite bounded confidence.
- Exact changed-path, `git diff --check`, ancestry, no-merge, diagnostic-not-ancestor, and clean-status receipts.
- Offline/no-network receipt naming `--no-restore`, synthetic Moq/in-memory fixtures, and any local network isolation used. Include no secret, protected data, or payload dump.
- One fresh independent read-only adversarial review on the exact candidate commit, with every finding and coordinator disposition. Any rework changes the candidate and invalidates the prior acceptance.
- Residual risk, blockers/decisions, and next safe action.

## Collision Risk

Inter-parcel file collision is low: no other authorized parcel owns `InteractionIntelligenceService.cs` or `InteractionIntelligenceReproductionTests.cs`. Behavioral collision is medium because analyzer, public interaction, counterfactual, and swap paths consume this service. No other live builder may edit AF-P03. If another branch changes the service, the base moves, or an interface/entity change is needed, stop and return to the coordinator; do not broaden scope or silently rebase.

## Step 0 — Restate and Stop Gate

Before any import, edit, test build, dependency action, stage, or commit, the builder must inspect the named local objects and send one restatement containing:

1. goal, initiative, parcel, Wave 1/frontier routing, branch, isolated worktree, pinned base commit/tree, current HEAD, and clean status;
2. the two exact AF-P03 paths and the statement that every other repository path is forbidden;
3. all six exact D6 clauses, including safety-before-graph-and-hint, active/exactly-reviewed artifact and edge eligibility, independent `NeedsReview` denial, canonical identity deduplication, finite/clamped confidence, and next-source fallthrough;
4. diagnostic commit, one-file restore command, diagnostic blob, all four retained method names, six hostile-case shape/row counts, and the prohibition on importing diagnostic history;
5. dependencies, ratification status, and confirmation that P03 consumes no other parcel branch;
6. baseline, targeted, adjacent, Application, solution, count-delta, diff, ancestry, and cleanliness commands and exact expected receipts;
7. SG-SCOPE, synthetic-only/offline/no-network/no-restore/no-provider/no-production-data constraints, local-assets-only rule, one fresh read-only adversarial review, and ungranted Gate 3;
8. every stop condition below and any mismatch or ambiguity found.

The builder then stops. Only explicit coordinator confirmation of this exact restatement permits import or implementation. Silence, the human's standing Gate 2 authorization, a clean base, or this spec alone is not confirmation.

## PR Notes for a Future Authorized PR

No PR may be created now. If separately authorized after Gate 3, notes must state:

- **What changed:** the exact D6 interaction-precedence/provenance/deduplication/confidence repair and the retained-plus-hostile 10-case regression class.
- **Why:** explicit avoid metadata must never be weakened by graph/hint ordering, only active exactly reviewed graph material may drive intelligence, canonical aliases must not self-pair, and non-finite confidence must not enter scoring.
- **Scope:** exactly AF-P03; no graph schema/store, package, config, endpoint, provider, or response-contract change.
- **Risk:** central interaction evaluation affects summaries, findings, counterfactuals, and swaps; mitigated by exact hostile regressions, unchanged 20-case adjacent suite, full Application/solution verification, scope checks, and fresh adversarial review.
- **Verification:** exact commands, exit codes, base/candidate counts, changed paths, and review disposition.
- **Rollback:** revert only the exact approved P03 candidate and rerun the same targeted/adjacent/aggregate matrix; preserve diagnostic branches/history.
- **Release boundary:** merge, deployment, and release remain separate human-authorized actions.

## Session Handoff Template

- Status: `BLOCKED`, `READY_FOR_REVIEW`, or `REWORK_REQUIRED`; never claim merged, deployed, or released without separate evidence
- Goal / parcel: `biostack-algorithm-remediation` / `P03-interaction-safety`
- Branch / worktree:
- Starting commit / tree:
- Candidate commit:
- Base production blob / diagnostic initial blob:
- Allowed Files changed:
- Forbidden files changed: `none` or stop
- Diagnostic import command and four-scenario preservation result:
- D6 clause-by-clause disposition:
- Baseline and candidate count table:
- Targeted / adjacent / Application / solution commands, summaries, and exit codes:
- Diff-check / changed-path / ancestry / no-merge / clean-status receipts:
- Offline/no-network/no-external-service receipt:
- Adversarial reviewer identity, exact reviewed commit, verdict, findings, and coordinator dispositions:
- Tests failed or skipped:
- Residual risk:
- Decisions needed / blockers:
- Next safe action: coordinator inspection and fresh review, or explicit rework direction
- Do not touch: diagnostic branches/history, paths outside AF-P03, remote Git, PRs, merges, deployments, releases

## Stop Rules

Stop immediately and report if:

- the branch/worktree is missing, dirty before work, not exactly at the pinned base/tree, or derived from a diagnostic/candidate branch;
- the coordinator has not explicitly confirmed Step 0, reports material `origin/main` movement, or identifies another live owner for AF-P03;
- an AF-P03 path is missing/insufficient, the production blob differs, the initial diagnostic blob differs, or a retained method/assertion is changed or removed;
- implementation needs any path, dependency, package, interface, entity, schema, config, fixture, endpoint, or product decision outside this spec;
- D6 can pass only by weakening `AvoidWith`, treating unreviewed graph state as eligible, suppressing fallback, using input-string/reference deduplication instead of canonical identity, or allowing non-finite/out-of-range confidence;
- a required case must be renamed, skipped, weakened, deleted, or have its expected safety/provenance result changed merely to pass;
- targeted, adjacent, Application, or solution counts differ from the exact tripwires; a new skip/failure appears; or a harness/build problem cannot be separated from the assertion;
- a command would require restore, network, provider, cloud, production database, protected data, secret, or external payload access;
- an adversarial finding cannot close inside AF-P03, or rework changes the reviewed candidate without fresh review;
- a forbidden path appears, diagnostic history becomes an ancestor, a merge commit appears, `git diff --check` fails, or final status is dirty;
- the same tripwire or false-closure condition repeats twice;
- any push, PR, merge, deployment, publication, release, diagnostic-branch mutation, or cleanup is proposed before explicit human authority and exact Gate 3.
