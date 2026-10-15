# BIO-GOLIVE-001 spec review 3

Verdict: **FAIL**

Reviewed text: `docs/specs/active/BIO-GOLIVE-001-public-classification-and-list-read.md` on tree `5b686e69536f2eedf0195ee77466f20b08a490d8` (`docs/go-live-library-record`). That commit is contained in `origin/main` `9550964256d507d03b789df8f0eb4c448a6f7ac8`.

A builder cannot implement the written contract. `ListPublicCompoundsAsync` is the right split, and `GetAllCompoundsAsync` is no longer narrowed, but the new interface member does not compile inside Allowed Files, and the frozen public-boundary tests still stub the method this parcel tells `KnowledgeService` to stop calling. Those are unresolved contract decisions. This review does not authorize implementation, Refresh, a production write, or go-live.

## Prior-failure re-check

| Item | Status | Result |
| --- | --- | --- |
| 1. Retatrutide | Pass | HEAD `substances-seed.json` classifies Retatrutide as `Peptide` and aliases `LY3437943` and `triple agonist` (identity block at lines 2023–2032). Shaping pin `6c9d8eccfd69f848d69c223e547c4d40dbf6ccee` had `Research Compound` and alias `LY3437943` only. `go-live-library-2026-10-14-retatrutide` approves promotion and names no class. The spec calls the committed Peptide value unreviewed, forbids a seed edit, and does not make Peptide or any live Retatrutide label acceptance. It does not say Peptide is uncommitted. |
| 2. Live labels | Pass | A human deploy must not change rows stored as `Unknown` (0). Close is local mapper tests plus a coordinator transcript that names the revision and shows public Glycine and Retatrutide still `Unknown`. Live `AminoAcid` or `ResearchCompound` is deferred. |
| 3. List query | Fail | The spec adds `ListPublicCompoundsAsync` and leaves `GetAllCompoundsAsync` as the full-entity read. Allowed Files omit `PreviewFakeKnowledgeSource`, and Tests item 3 freezes setups that still call `GetAllCompoundsAsync`. See F1 and F2. |
| 4. Cold start | Pass | Fact 8 and acceptance criterion 7 say cold-start time is not pass/fail. The old 3-second budget is gone. The 334 ms figure is labeled as not a cold-start measurement. |
| 5. Enum | Pass | `CompoundCategory` is `Unknown = 0` through `Other = 10`. Integers 11 and 12 are unused. The spec appends `AminoAcid = 11` and `ResearchCompound = 12` and forbids renumbering 0–10. `IconFor` and `AccentFor` already have `_` defaults, so those switches do not need an edit. |
| 6. Boundaries | Pass | Dose, schedule, pairing, and optimization members on `KnowledgeEntryResponse` stay `[property: JsonIgnore]`. Forbidden and out-of-scope text block Refresh, seed edits, production connection strings, live database writes, and go-live authorization. `Program.cs` registers a parameterless `JsonStringEnumConverter`, so `Sarm` and `Serm` stay the wire names. |

## Findings

| id | severity | location | finding |
| --- | --- | --- | --- |
| F1 | blocker | Spec Allowed Files (lines 136–145) and Surfaces (line 69). Source: `TranscriptCandidatePromotionPreviewServiceTests.cs` lines 59–90 | `IKnowledgeSource` has three concrete implementors: `DatabaseKnowledgeSource`, `LocalKnowledgeSource`, and `PreviewFakeKnowledgeSource`. The first two are allowed. The fake is not. Current interface methods have no default bodies. Adding abstract `ListPublicCompoundsAsync` is CS0535 in `BioStack.Application.Tests`, which is the project the required `KnowledgeListProjectionTests` filter compiles. The spec does not say to give the new member a default body. A default body is not specified and would not fix F2. |
| F2 | blocker | Spec Surfaces (line 72), acceptance criterion 5 (line 111), and Tests item 3 (line 119). Source: `KnowledgeServicePublicBoundaryTests.cs` lines 47–86 and `KnowledgeService.cs` lines 22–28 | `KnowledgeService.GetAllCompoundsAsync` must call `ListPublicCompoundsAsync` only. The two existing list tests stub `IKnowledgeSource.GetAllCompoundsAsync` and are required to stay green and not be weakened. That file is not an allowed file. A loose Moq does not pass the old setup through to the new method, so those tests throw when the service stops calling `GetAllCompoundsAsync`. The builder cannot both switch the call and keep the frozen tests green. |
| F3 | minor | Acceptance criterion 5 (line 111). Source: `KnowledgeService.cs` lines 94–101 | Current duplicate selection is `PublicEvidenceCompleteness` descending, then `Id` ascending, and public lists are merged across the group. `GetAllCompoundsAsync_DeduplicatesCanonicalNamesWithoutDeletingEvidence` requires the evidence-richer row to win. Criterion 5 says only that ties break by `Id`. That phrase matches the secondary key, but it does not name the primary key the frozen test enforces. |
| F4 | minor | Acceptance criterion 4 (line 110). Source: `KnowledgeEntryResponse.cs` line 44 | The exclusion list ends with "the optimization strings." `OptimizationSupplements` is a `List<string>` and is also `[property: JsonIgnore]`. Tests item 2 says "optimization properties," which covers it. Criterion 4 should name `OptimizationSupplements` so the column list and the test sentence are the same. |

## What is already determined

- Return type does not need a new file. `IKnowledgeSource` already returns `List<KnowledgeEntry>`, Infrastructure does not reference Contracts, and `Id` must survive until `KnowledgeService` groups. The signature that fits the allowed files is `Task<List<KnowledgeEntry>> ListPublicCompoundsAsync(CancellationToken cancellationToken = default)`.
- Public columns are the `KnowledgeEntryResponse` members without `JsonIgnore`, plus `Id`: `CanonicalName`, `Aliases`, `Classification`, `RegulatoryStatus`, `MechanismSummary`, `EvidenceTier`, `SourceReferences`, `Notes`, `Pathways`, `Benefits`, `AvoidWith`, and `DrugInteractions`. Excluded members are the `JsonIgnore` set, including `OptimizationSupplements`.
- Callers that must keep the full entity are `InteractionIntelligenceService` (lines 504 and 528), `CounterfactualCandidateService` (lines 22 and 49), `ProtocolParser` (line 83), and `SwapRecommendationEngine.HasExplicitPeerSignal` (lines 420–423), which reads `PairsWellWith` and `CompatibleBlends`. The spec correctly leaves `GetAllCompoundsAsync` unchanged for them.
- `MapClassification` is private. `SubstanceCanonicalizer.ToKnowledgeEntry` is public, and `SubstanceRecordFactory.Minimal` is already visible to `BioStack.KnowledgeWorker.Tests`, so the mapper test can set `Identity.Classification` and read `KnowledgeEntry.Classification`.
- Glycine's committed classification at HEAD is `Amino Acid` (seed lines 37029–37036). The shaping pin has no `canonicalId` of `glycine`.

## Validator

Mandated command:

`pwsh -NoProfile -File D:\Repos\BioStack\docs\specs\scripts\validate-spec.ps1 -SpecPath D:\Repos\BioStack\docs\specs\active\BIO-GOLIVE-001-public-classification-and-list-read.md -RepoRoot D:\Repos\BioStack`

Result: `unrecognized-shape`. `Resolve-SpecShapeForPath` matches only a repo-relative path against `docs/specs/active/*.md`. The absolute path never reaches frontmatter checks. That result is not a spec defect.

Re-run that the script can actually classify:

`pwsh -NoProfile -File D:\Repos\BioStack\docs\specs\scripts\validate-spec.ps1 -SpecPath docs/specs/active/BIO-GOLIVE-001-public-classification-and-list-read.md -RepoRoot D:\Repos\BioStack`

Result: `valid`, shape `ticket-spec`, `reviewerCount` 2. Folded required checks include focused tests, provenance/license/freshness, fail-closed promotion, and canonical-write fencing tests. Bound capability fields passed. Required headings for `standard` and `knowledge-promotion` are present. This document-contract pass does not close F1 or F2.

## Fix checklist

Spec text only. Do not treat this as implementation authority.

1. Keep `GetAllCompoundsAsync` unchanged on the interface and on both production sources. State the signature above on `IKnowledgeSource` with no default body.
2. Add `backend/tests/BioStack.Application.Tests/Services/TranscriptCandidatePromotionPreviewServiceTests.cs` to Allowed Files, limited to `ListPublicCompoundsAsync` on `PreviewFakeKnowledgeSource`, throwing `NotSupportedException` as `GetAllCompoundsAsync` already does.
3. Add `backend/tests/BioStack.Application.Tests/Services/KnowledgeServicePublicBoundaryTests.cs` to Allowed Files. Retarget the two list setups from `GetAllCompoundsAsync` to `ListPublicCompoundsAsync`. Do not remove or weaken assertions.
4. In acceptance criterion 5, name the preserved order: highest public-evidence completeness, then lowest `Id`, with the existing public-list merge. Do not switch the primary key to `Id`.
5. Name `OptimizationSupplements` in the projection exclusion list with the other optimization members.
6. Leave the library class-label launch gate failed after the transcript. The transcript closes only this parcel. It is not acceptance for a repaired live label and it is not go-live authorization.
