# Parcel P03: Interaction Safety

Status: **review-complete — candidate held unmerged; Gate 3 ungranted**

Coordinator lint: **PASSED 2026-09-02 after Narrow Gate 1 Amendment 05.** Replacement three-file AF-P03, the one-value API fixture restriction, base/tree, production and diagnostic blobs, all seven amended D6 clauses, the 4 retained plus 7 hostile-case census, the independent 20-case adjacent filter, the 609-passed/5-skipped Application baseline, API 377 tripwire, replacement count deltas, offline commands, and single-review depth were checked against the pinned repository and ratified amendments. Resumption remains contingent on an exact fresh Step 0 restatement.

Import-receipt correction: **PASSED 2026-09-02 after a pre-edit builder stop.** Because the reproduction path is absent from the pinned index, restoring it into the worktree creates an untracked file; pre-commit `git diff --name-only` correctly emits nothing. The import tripwire therefore uses `git status --short --untracked-files=all` plus `git ls-files --others --exclude-standard` and requires exactly the one AF-P03 test path. This changes no invariant, Allowed File, case/count expectation, or Gate authority.

Amendment 04 disposition: **COMPLETED 2026-09-02.** The initial scoped state passed target `10/10`, adjacent `20/20`, and Application `619 passed / 5 skipped`, but the full solution exposed one API graph-positive helper publishing a `provisional` artifact. Amendment 04 added only `CompoundGraphIntelligenceTests.cs` and authorized only that stale fixture state to change from `provisional` to exactly `reviewed`. Candidate `7e8087a00722e38c2a090d49826fa6289c880356` contains that exact correction and passed the amended full verification chain before adversarial review.

Adversarial disposition: **ACCEPT 2026-09-02.** Candidate `a69d945a3cd8a3655911707031386441fc55079f`, tree `7205d5ea11148eac865534cf5ae545634eb84be5`, passed the complete amended shaped verification chain and a fresh independent read-only adversarial review with no actionable findings. Target `11/11`, adjacent `20/20`, Application `620 passed / 5 skipped`, focused API `8/8`, and full solution counts 7 / 620+5 / 158 / 377 / 866 were independently reproduced offline. Rejected parent `7e8087a00722e38c2a090d49826fa6289c880356` and diagnostic history remain preserved and unmerged. Gate 3 remains ungranted.

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
- Required rework starting candidate: `7e8087a00722e38c2a090d49826fa6289c880356`
- Required rework starting tree: `403cd7b89017e35e587afc9252ad25fdd9154d49`

The parcel branch was created from the required starting commit, never from a diagnostic branch or another remediation candidate. Amendment-05 rework begins from the exact rejected candidate and tree above so that rejected history remains preserved and the new candidate is its direct child. The pinned base must remain an ancestor, the diagnostic commit must remain a non-ancestor, and no merge commit is allowed. The diagnostic commit is an evidence source only. Do not merge, cherry-pick, rebase, or otherwise incorporate its history.

## Dependencies and Preconditions

1. Original Gate 1 D1-D14 and Narrow Gate 1 Amendments 01-05 are ratified. The replacement D6 in `gate-1-amendment-01.md`, supplemented by the artifact-identity rule in `gate-1-amendment-05.md`, is the controlling interaction contract. Amendment 04 replaces AF-P03 and authorizes only the exact stale API fixture correction described below; Amendment 05 changes no Allowed File or API-fixture authority.
2. Contingent Gate 2 covers this P03 rework only after the coordinator lints this spec, verifies the isolated branch/worktree is clean at the exact required rework candidate/tree with the pinned base still ancestral and diagnostic history absent, confirms AF-P03, and approves the builder's fresh Step 0 restatement.
3. The coordinator owns the required `origin/main` comparison. The builder must not fetch. If the coordinator reports material movement from the pinned base, stop under the charter's base-change rule.
4. P03 has no implementation dependency on P01, P02, P04-P08, or Q01-Q04, and it must consume no candidate branch. No other live builder may edit any AF-P03 path.
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

Exactly these three repository paths may be created or modified:

1. `backend/src/BioStack.Application/Services/InteractionIntelligenceService.cs`
2. `backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs`
3. `backend/tests/BioStack.Api.Tests/CompoundGraphIntelligenceTests.cs`

The third path may change only in its stale graph-positive fixture helper, from `provisional` to exactly `reviewed`; no other edit in that file is authorized. If a path is missing, insufficient, or cannot carry the complete ratified D6 contract, stop for a charter/spec amendment. Do not substitute a nearby file.

## Forbidden and Out of Scope

- Every repository path outside AF-P03 is forbidden, including project/solution files, graph entities, graph-store interfaces/implementations, repositories, migrations, contracts/responses, endpoint code, package/lock files, build configuration, other fixtures, generated files, and handoff records.
- Within `CompoundGraphIntelligenceTests.cs`, every change except the one ratified `provisional`-to-`reviewed` fixture-state correction is forbidden.
- Do not merge or cherry-pick diagnostic commit `56b7ad229a34a6f151f5bf7b96a8bfdcbce8132f`; restore only the one AF-P03 test file's contents.
- Do not mutate, rebase, force-update, delete, or clean the diagnostic coordination branch or any diagnostic parcel branch.
- Do not rename, delete, skip, weaken, or replace the four diagnostic scenarios or their assertions. The original failing conditions must remain observable regressions.
- Do not add packages, dependencies, schemas, migrations, configuration, feature flags, endpoints, response fields, telemetry, provider calls, network fixtures, or test-only branches in production code.
- Do not modify graph persistence semantics, publish/activate graph artifacts, redefine graph relationship types, change stored hint data, or expand the public interaction response contract.
- Do not infer that `PairsWellWith`, `CompatibleBlends`, shared pathways, drug-interaction free text, or unknown relationships establish safety. This parcel changes only ratified precedence/eligibility/deduplication/confidence behavior.
- Do not redesign scoring, counterfactuals, swaps, complementary-domain detection, top-finding ordering, reason strings, or unrelated pair inference.
- Do not create a `ShapingResult` JSON. BioStack does not vendor the Foreman permission-profile emitter/linter required to author one.
- Do not stage or commit during shaping. The rework builder may make exactly one new local candidate commit atop the preserved rejected candidate only after explicit coordinator Step 0 confirmation and a green required verification chain.

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
- The clean ambient checkout at the identical pinned commit/tree produced 609 passed, 5 skipped, 0 failed, total 614 for `BioStack.Application.Tests`. These counts are the preserved clean-base tripwire and were independently reproduced in the isolated parcel worktree before the original import.
- The pinned-base solution receipt is: `BioStack.Domain.Tests` 7 passed; `BioStack.Application.Tests` 609 passed plus 5 skipped; `BioStack.ProtocolOperationsExportBundleVerifierCli.Tests` 158 passed; `BioStack.Api.Tests` 377 passed; `BioStack.KnowledgeWorker.Tests` 866 passed. The builder must re-establish this baseline in the isolated local-assets-only environment rather than treating this document as execution evidence.

## Exact Ratified D6 Contract

The following clauses are exhaustive for P03 and must be satisfied together.

1. **Safety precedence — R-INT-01.** A named match in either compound's explicit `AvoidWith` metadata is evaluated before graph lookup and before stored pair-hint lookup. It returns the existing interfering safety result and cannot be weakened by a positive graph edge or a positive stored hint. The graph and hint repositories must not be consulted for that pair once the explicit safety result is established.
2. **Reviewed active graph eligibility — R-INT-02 and Amendment 01.** Graph intelligence is eligible only when the current artifact exists, `IsActive` is true, the artifact `ReviewState` is exactly ordinal `reviewed`, the relationship `ReviewState` is exactly ordinal `reviewed`, and `NeedsReview` is false. Missing, inactive, non-reviewed, or case-variant artifact/edge review states are ineligible. An ineligible graph path falls through to the next eligible source; it may not emit `Source == Graph` or cite the ineligible artifact hash.
3. **Independent `NeedsReview` denial.** `NeedsReview == true` makes an edge ineligible even when both artifact and edge review states are exactly `reviewed`. The retained reproduction where edge state and the flag are both ineligible must remain, and the new hostile case must isolate the flag.
4. **Canonical-identity deduplication — R-INT-03.** After name/alias resolution and before pair generation, entries are unique by canonical compound identity using the service's canonical name with ordinal case-insensitive comparison. Canonical and alias inputs resolving to the same compound yield no self-pair. Input-string deduplication alone is insufficient.
5. **Finite bounded graph confidence — R-INT-04.** Every graph-backed confidence entering a response is finite and in `[0,1]`. Numeric values below/above the range remain clamped by the existing behavior. Non-finite numeric inputs such as `NaN` must be rejected or normalized to a finite bounded fallback; they may never propagate into interaction scores, ordering, findings, counterfactuals, or swaps.
6. **Next-source semantics.** After an ineligible graph, evaluation continues in the existing precedence order: an eligible stored hint, then existing metadata/rule fallbacks. This does not elevate hints over explicit `AvoidWith` and does not turn unknown/legacy pairing metadata into a safety claim.
7. **Artifact identity and provenance — Amendment 05.** A graph relationship is eligible only when its `GraphArtifactId` equals the `Id` of the same present, active, exactly reviewed artifact whose hash is cited. A mismatch caused by rotation or any other condition is ineligible, may not emit graph intelligence or cite that artifact hash, and follows the existing next-source semantics.

Preserve behavior unrelated to these clauses, including public-boundary omission of action scenarios, legacy pairing metadata remaining `Unknown`, shared/complementary pathway logic, drug-interaction fallback, summaries, composite scoring, top-finding ordering, counterfactuals, swaps, cancellation propagation, and existing reason/response shapes.

## Diagnostic Import and Required Regression Shape

The following import was performed only during the original build, after its original Step 0 and while `HEAD` was still the pinned base. It is retained as provenance and must **not** be rerun during Amendment-05 rework:

```powershell
git restore --source=56b7ad229a34a6f151f5bf7b96a8bfdcbce8132f --worktree -- backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs
git hash-object backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs
```

The original imported hash was required to be `a234e398b4d8f226ee43baf0ecb69f352c584307`. Amendment-05 rework must start from the exact rejected candidate, where the retained four cases and six Amendment-01 hostile cases already exist. Retain the class name, four diagnostic `[Fact]` scenarios, method names, synthetic inputs, assertions, and six Amendment-01 hostile cases. Extend it only with the one Amendment-05 artifact-identity case below and the smallest helper changes needed to express it; the final test-file blob is expected to differ from the diagnostic blob, so preservation is proved by assertion/method inspection and aggregate diff review, not a final blob-equality check.

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

9. `EvaluateAsync_GraphEdgeFromDifferentArtifact_IsNotAttributedToReviewedArtifact` — one case with a present active exactly reviewed artifact A and an otherwise reviewed, `NeedsReview == false` relationship whose `GraphArtifactId` belongs to distinct artifact B; assert no graph-backed result and no artifact hash, while preserving the existing next-source fallthrough.

The class therefore has exactly 11 discovered cases: 4 retained facts plus 7 hostile cases (two facts, one three-row theory, and two further facts). If the builder needs a different test name, row count, or scenario to implement D6, stop for coordinator review before editing.

## Implementation Constraint

Make the smallest coherent change to `InteractionIntelligenceService.cs` that enforces the exact D6 contract, including comparing the returned relationship's artifact ID with the validated reviewed artifact ID before graph intelligence is emitted. Prefer narrow private helpers and existing control flow. Do not add a public API, alter dependency injection, or change an interface/entity. Do not special-case synthetic names or test inputs. If the contract cannot be implemented within the one production file while all 11 cases and adjacent behavior remain green, stop for amendment.

## Required Tests and Acceptance Criteria

- All four retained diagnostic methods compile, are discovered, and pass without weakened assertions.
- All seven additional hostile cases compile, are independently discovered, and pass.
- Targeted class: 11 passed, 0 failed, 0 skipped, total 11.
- Safety precedence is observable against both a positive graph edge and a positive stored hint; explicit `AvoidWith` cannot be replaced by either.
- Graph output is impossible for missing, inactive, non-reviewed, `NeedsReview`, or cross-artifact graph state, and ineligible graph state falls through without exposing graph provenance.
- Canonical plus alias resolution does not generate a self-pair.
- Graph confidence in every governed case is finite and within `[0,1]`.
- Adjacent interaction/public-boundary/swap suite remains exactly 20 passed, 0 failed, 0 skipped.
- Full `BioStack.Application.Tests` is exactly 620 passed, 5 skipped, 0 failed, total 625: the pinned 614 total plus exactly 11 P03 cases.
- Backend solution aggregate is green offline. Only `BioStack.Application.Tests` gains 11 passed/discovered cases; Domain remains 7, verifier remains 158, API remains 377, and KnowledgeWorker remains 866, with no new skips.
- The focused `CompoundGraphIntelligenceTests` API class passes after the exact fixture-state correction; the complete API suite remains exactly 377 passed.
- Exactly the three AF-P03 paths differ from base; no staged, modified, or untracked path exists outside AF-P03.
- Candidate descends from the pinned base, contains no merge commit from diagnostic history, and diagnostic commit `56b7ad2...` is not its ancestor.
- `git diff --check` is clean, and no command contacted an external service.

## Exact Verification Commands

`rtk` was not resolvable in the shaping shell. The raw commands below are the repository AGENTS.md debugging fallback. If `rtk` is available to the builder, each command may use its equivalent `rtk` prefix. Do not remove `--no-restore` from test commands.

Run from the P03 worktree root.

### Historical clean-base receipt (already satisfied; do not rerun as rework Step 0)

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

Historical receipts: HEAD/base and tree equaled the pinned values; status was empty; production blob equaled `3ce1f4075818b1e3eb2ec205dc059aba68905fa6`; `git cat-file -e` exited 128 because the reproduction file was absent. Adjacent was exactly 20/0/0/20. Application was 609 passed, 5 skipped, 0 failed, total 614. Solution project counts matched the pinned receipt above. These receipts remain the count baseline; Amendment-05 rework does not reset the branch or rerun import.

### Historical diagnostic import identity (already satisfied; do not rerun)

```powershell
git restore --source=56b7ad229a34a6f151f5bf7b96a8bfdcbce8132f --worktree -- backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs
git hash-object backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs
git status --short --untracked-files=all
git ls-files --others --exclude-standard
```

Historical receipt before the original extension: hash `a234e398b4d8f226ee43baf0ecb69f352c584307`; status was exactly one untracked AF-P03 reproduction test and the untracked-file listing contained exactly that path. Empty `git diff --name-only` output at that stage was normal and was not scope evidence because the path was not yet indexed.

### Fresh Amendment-05 rework receipt after Step 0 confirmation, before edits

```powershell
git rev-parse HEAD
git rev-parse 'HEAD^{tree}'
git status --short
git merge-base --is-ancestor 339f259b1a467034db4f57cf9d774c292f11b53a HEAD
git merge-base --is-ancestor 56b7ad229a34a6f151f5bf7b96a8bfdcbce8132f HEAD
git rev-list --merges 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff --check 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff --name-only 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD -- backend/tests/BioStack.Api.Tests/CompoundGraphIntelligenceTests.cs
```

Expected rework-start receipts: HEAD `7e8087a00722e38c2a090d49826fa6289c880356`; tree `403cd7b89017e35e587afc9252ad25fdd9154d49`; empty status; base-ancestor exit 0; diagnostic-ancestor exit 1; no merges; clean diff check; exactly the three AF-P03 paths against the pinned base; and the API file diff contains only Amendment 04's one `provisional`-to-`reviewed` value. Any mismatch stops the parcel. These read-only checks do not replace the full post-rework verification chain.

### Targeted regression

```powershell
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.Services.InteractionIntelligenceReproductionTests" --disable-build-servers --logger "console;verbosity=minimal"
```

Expected candidate receipt: passed 11, failed 0, skipped 0, total 11. Before the production repair, the retained/new cases must expose the governed behavior; record assertion-level failures, but do not commit a red state. A harness/build/environment failure is not diagnostic evidence and stops the parcel.

### Adjacent interaction, public-boundary, and swap suites

```powershell
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.Services.InteractionIntelligenceServiceTests|FullyQualifiedName~BioStack.Application.Tests.Services.InteractionIntelligencePublicBoundaryTests|FullyQualifiedName~BioStack.Application.Tests.Services.SwapRecommendationTests" --disable-build-servers --logger "console;verbosity=minimal"
```

Expected candidate receipt: passed 20, failed 0, skipped 0, total 20. The filter names the three existing classes individually, so this adjacent count remains independent of the 11 targeted reproduction cases.

### Application and backend-solution aggregate

```powershell
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --disable-build-servers --logger "console;verbosity=minimal"
dotnet test backend/BioStack.sln --no-restore --verbosity minimal --disable-build-servers
```

Expected Application receipt: 620 passed, 5 skipped, 0 failed, total 625. Before the aggregate, run the focused API class:

```powershell
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Api.Tests.CompoundGraphIntelligenceTests" --disable-build-servers --logger "console;verbosity=minimal"
```

Expected focused API receipt: passed 8, failed 0, skipped 0, total 8. Expected solution delta: only Application gains 11; Domain stays 7, verifier 158, API stays exactly 377, and KnowledgeWorker stays 866. Record every assembly summary and exit code. The later goal-level aggregate may consume its separately mandated command only in an already dependency-ready, network-prohibited environment; P03 grants no implicit restore/network authority.

### Diff, ancestry, scope, and cleanliness receipts after candidate commit

```powershell
git merge-base --is-ancestor 339f259b1a467034db4f57cf9d774c292f11b53a HEAD
git merge-base --is-ancestor 56b7ad229a34a6f151f5bf7b96a8bfdcbce8132f HEAD
git rev-list --merges 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff --check 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff --name-only 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff -- backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs
git diff -- backend/tests/BioStack.Api.Tests/CompoundGraphIntelligenceTests.cs
git status --short
```

Expected results: base-ancestor exit 0; diagnostic-ancestor exit 1; merge list empty; diff-check exit 0; name-only output exactly the three AF-P03 paths; reproduction diff visibly retains all four original methods/assertions and adds only the seven specified cases/helper support; API diff contains only the one `provisional`-to-`reviewed` fixture-state correction; final status empty after exactly one new local rework commit atop the rejected candidate. Capture native exit codes explicitly because empty output alone is not proof.

## Count and Scope Tripwire Method

1. Preserve the already-reproduced exact clean-base counts from before the original import: adjacent 20; Application 609 passed plus 5 skipped; solution counts 7/609(+5 skipped)/158/377/866 by the named assemblies. Do not reset or re-import during Amendment-05 rework.
2. Preserve the original diagnostic import receipt: exactly four `[Fact]` cases at blob `a234e398...` before extension; confirm all four methods/assertions remain in the aggregate candidate diff.
3. Extend the file to exactly 11 discovered cases using the specified 4 retained plus 7 hostile row layout.
4. Candidate targeted must be 11/0/0/11; adjacent stays 20/0/0/20; Application becomes 620 passed plus 5 skipped, total 625; only the Application assembly gains 11 in the solution run.
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

1. goal, initiative, parcel, Wave 1/frontier routing, branch, isolated worktree, pinned base commit/tree, exact rejected rework-start candidate/tree, current HEAD, clean status, base ancestry, diagnostic non-ancestry, and no-merge state;
2. the three exact AF-P03 paths, the third-file one-value restriction, and the statement that every other repository path is forbidden;
3. all seven exact D6 clauses, including safety-before-graph-and-hint, active/exactly-reviewed artifact and edge eligibility, independent `NeedsReview` denial, canonical identity deduplication, finite/clamped confidence, next-source fallthrough, and relationship-to-reviewed-artifact ID equality;
4. diagnostic commit, historical one-file restore command and diagnostic blob, explicit prohibition on rerunning the import, all four retained method names, seven hostile-case shape/row counts including the exact Amendment-05 method, and the prohibition on importing diagnostic history;
5. dependencies, ratification status, and confirmation that P03 consumes no other parcel branch;
6. baseline, targeted, adjacent, Application, focused API, full API/solution, count-delta, diff, ancestry, and cleanliness commands and exact expected receipts;
7. SG-SCOPE, synthetic-only/offline/no-network/no-restore/no-provider/no-production-data constraints, local-assets-only rule, one fresh read-only adversarial review, and ungranted Gate 3;
8. every stop condition below and any mismatch or ambiguity found.

The builder then stops. Only explicit coordinator confirmation of this exact restatement permits import or implementation. Silence, the human's standing Gate 2 authorization, a clean base, or this spec alone is not confirmation.

## PR Notes for a Future Authorized PR

No PR may be created now. If separately authorized after Gate 3, notes must state:

- **What changed:** the exact D6 interaction-precedence/provenance/deduplication/confidence/artifact-identity repair and the retained-plus-hostile 11-case regression class.
- **Why:** explicit avoid metadata must never be weakened by graph/hint ordering, only relationships belonging to the same active exactly reviewed graph artifact whose hash is cited may drive intelligence, canonical aliases must not self-pair, and non-finite confidence must not enter scoring.
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

- the branch/worktree is missing, dirty before work, not exactly at rejected candidate `7e8087a00722e38c2a090d49826fa6289c880356` and tree `403cd7b89017e35e587afc9252ad25fdd9154d49`, does not retain the pinned base as ancestor, contains diagnostic ancestry or a merge, or has any extra candidate ancestry;
- the coordinator has not explicitly confirmed Step 0, reports material `origin/main` movement, or identifies another live owner for AF-P03;
- an AF-P03 path is missing/insufficient, the production blob differs, the initial diagnostic blob differs, or a retained method/assertion is changed or removed;
- implementation needs any path, dependency, package, interface, entity, schema, config, fixture change other than the exact ratified API correction, endpoint, or product decision outside this spec;
- D6 can pass only by weakening `AvoidWith`, treating unreviewed graph state as eligible, suppressing fallback, using input-string/reference deduplication instead of canonical identity, or allowing non-finite/out-of-range confidence;
- a required case must be renamed, skipped, weakened, deleted, or have its expected safety/provenance result changed merely to pass;
- targeted, adjacent, Application, or solution counts differ from the exact tripwires; a new skip/failure appears; or a harness/build problem cannot be separated from the assertion;
- a command would require restore, network, provider, cloud, production database, protected data, secret, or external payload access;
- an adversarial finding cannot close inside AF-P03, or rework changes the reviewed candidate without fresh review;
- a forbidden path appears, diagnostic history becomes an ancestor, a merge commit appears, `git diff --check` fails, or final status is dirty;
- the returned relationship's artifact ID differs from the validated reviewed artifact ID, the mismatch can emit graph intelligence or cite the reviewed artifact hash, or the same tripwire/false-closure condition repeats twice;
- any push, PR, merge, deployment, publication, release, diagnostic-branch mutation, or cleanup is proposed before explicit human authority and exact Gate 3.
