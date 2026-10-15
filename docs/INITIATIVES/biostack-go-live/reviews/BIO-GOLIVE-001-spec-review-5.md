# BIO-GOLIVE-001 spec review 5

- Reviewer: spec-review-5
- Date: 2026-10-14
- Spec: `docs/specs/active/BIO-GOLIVE-001-public-classification-and-list-read.md`
- Spec blob reviewed: `cb401509df53c00f261128743ce2816e78f48e6d` (working copy; `git status` shows this file modified)
- HEAD blob of the same path: `65178cbdea1f02f877b5a4806090af0398d16202` (not the text reviewed)
- Tree: HEAD `5b686e69536f2eedf0195ee77466f20b08a490d8`, contained in `origin/main` `9550964256d507d03b789df8f0eb4c448a6f7ac8`
- Verdict: **PASS**

No Blocker or Major findings. Nits do not change the verdict. No fix checklist.

This review does not recommend or authorize go-live, a production deploy, Refresh, a live database write, or implementation. Scope is the current spec text against current source.

## Prior-failure re-check

| Item | Result |
| --- | --- |
| Review 4 F1: `KnowledgeServicePublicBoundaryTests` not allowed, so the list stubs could not move | **Closed.** Allowed Files line 149 and frontmatter surfaces line 21 name that file. Tests item 3 (line 121) says the tests stay green and are not weakened, and that their list setups stub `ListPublicCompoundsAsync` instead of `GetAllCompoundsAsync`. |
| Review 4 F2: `PreviewFakeKnowledgeSource` not allowed and no default interface body | **Closed.** Allowed Files line 150 and frontmatter surfaces line 22 name `TranscriptCandidatePromotionPreviewServiceTests.cs`. Tests item 4 (line 122) requires `PreviewFakeKnowledgeSource` to implement `ListPublicCompoundsAsync` and says preview behavior does not change. |
| Live `AminoAcid` / `ResearchCompound` as a success condition | **Pass.** Not acceptance. See check 6. |
| Retatrutide `Peptide` and the seed | **Pass.** Unreviewed. Seed edit forbidden. See check 7. |
| Enum 11 and 12 | **Pass.** Free. See check 8. |
| Cold-start gate | **Pass.** No 3-second gate. See check 9. |
| Refresh | **Pass.** Prohibition only. See check 10. |

## Definition of done

| # | Check | Result |
| --- | --- | --- |
| 1 | `KnowledgeServicePublicBoundaryTests` is an allowed file and the required edit is specified | **Pass** |
| 2 | `PreviewFakeKnowledgeSource` is an allowed file and the required edit is specified | **Pass** |
| 3 | Projection includes every `PublicEvidenceCompleteness` column plus `Id`, and still excludes dose, schedule, pairing, and optimization | **Pass** |
| 4 | No concrete `IKnowledgeSource` implementor besides `DatabaseKnowledgeSource`, `LocalKnowledgeSource`, and `PreviewFakeKnowledgeSource` | **Pass** |
| 5 | Moq mocks need no source edit to compile | **Pass** |
| 6 | No live-label success condition | **Pass** |
| 7 | Retatrutide `Peptide` is unreviewed and the seed is forbidden | **Pass** |
| 8 | Enum integers 11 and 12 are unused | **Pass** |
| 9 | No 3-second cold-start gate | **Pass** |
| 10 | Refresh is not a step in this parcel | **Pass** |
| 11 | Spec validator accepts the file | **Pass** |

## Check notes

### 1 and 2. Allowed files and required edits

The two list tests that stub the source today are `GetAllCompoundsAsync_UsesTheSamePublicProjection` and `GetAllCompoundsAsync_DeduplicatesCanonicalNamesWithoutDeletingEvidence` (`KnowledgeServicePublicBoundaryTests.cs` lines 47–59 and 63–86). The third test stubs `GetCompoundAsync` only (lines 15–18) and is not a list setup. Tests item 3 matches that split: move the list setups, do not weaken assertions.

`PreviewFakeKnowledgeSource` is the only hand-written fake (`TranscriptCandidatePromotionPreviewServiceTests.cs` lines 59–90). Its `GetAllCompoundsAsync` throws `NotSupportedException` (lines 68–70). Tests item 4 tells the builder to add `ListPublicCompoundsAsync` on that fake. Delegating to `GetAllCompoundsAsync` keeps current preview behavior. Nothing in that file calls `GetAllCompoundsAsync`.

Both production sources are already allowed (lines 144–145) and are told to implement the new method while leaving `GetAllCompoundsAsync` unchanged (lines 72–73).

### 3. Projection columns

`PublicEvidenceCompleteness` (`KnowledgeService.cs` lines 73–91) reads only:

- `SourceReferences` (line 75)
- `Pathways` (line 76)
- `Benefits` (line 77)
- `AvoidWith` (line 78)
- `DrugInteractions` (line 79)
- `MechanismSummary` (line 81)
- `RegulatoryStatus` (line 86)

The duplicate winner is then `entry.Id` (lines 98–100). `Id` is not on `KnowledgeEntryResponse`.

Acceptance criterion 5 (line 114) requires the projection to include `Id` and every column that function reads, plus the other public response columns, and to exclude dose, schedule, pairing, and optimization columns. Those seven members are all public response fields: none of them carries `[property: JsonIgnore]` (`KnowledgeEntryResponse.cs` lines 7–50). The other non-ignored response fields are `CanonicalName`, `Aliases`, `Classification`, `EvidenceTier`, and `Notes`. `MapPublicGroupToResponse` also merges `Aliases`, `SourceReferences`, `Pathways`, `Benefits`, `AvoidWith`, and `DrugInteractions` (lines 107–112). Those names are inside the same included set. The frozen dedup assertions (boundary test lines 89–97) still require that merge, and Tests item 3 says not to weaken them.

Excluded members named in acceptance criterion 4 (line 113) are `RecommendedDosage`, `Frequency`, `PreferredTimeOfDay`, `StandardDosageRange`, `MaxReportedDose`, `WeeklyDosageSchedule`, `IncrementalEscalationSteps`, `TieredDosing`, `PairsWellWith`, `CompatibleBlends`, and `VialCompatibility`, plus optimization. That matches the dose, schedule, and pairing properties on `KnowledgeEntry` (lines 20–41) and the `JsonIgnore` set. Tests item 2 (line 120) requires the database projection test to select `Id` and the public columns and not to reference dose, schedule, pairing, or optimization properties. A response-object assertion cannot prove that, because `MapToResponse` already blanks those fields in memory (`KnowledgeService.cs` lines 52–69). The spec assigns the proof to the database projection, not to the cleared response.

`KnowledgeEntries.Classification` is an unconstrained `INTEGER` (`20260422125251_RecoverBillingTierEnforcement.cs` line 76). A local SQLite row can store 11 and read it back as `AminoAcid` once that member exists. Tests item 2 requires that.

### 4 and 5. Implementors and Moq

A workspace search for `IKnowledgeSource` in C# finds exactly three concrete implementors:

- `DatabaseKnowledgeSource` (`DatabaseKnowledgeSource.cs` line 7)
- `LocalKnowledgeSource` (`LocalKnowledgeSource.cs` line 6)
- `PreviewFakeKnowledgeSource` (`TranscriptCandidatePromotionPreviewServiceTests.cs` line 59)

`KnowledgeSourceIntakeService` implements `IKnowledgeSourceIntakeService`, not `IKnowledgeSource`. No other `.cs` type declares `: IKnowledgeSource`. Current interface members have no default bodies (`IKnowledgeSource.cs` lines 5–19).

Every other test site uses `Mock<IKnowledgeSource>` or resolves the interface from DI. Moq builds the proxy. Adding a member does not require edits at those call sites for compilation. Loose mocks still return the default for an unsetup new method at runtime. The only `KnowledgeService.GetAllCompoundsAsync` stubs that would then return null are the two list tests, and those are the setups Tests item 3 moves. Callers that must keep the full-entity read still call `GetAllCompoundsAsync`: `InteractionIntelligenceService` (lines 504 and 528), `CounterfactualCandidateService` (lines 22 and 49), and `ProtocolParser` (line 83). `SwapRecommendationEngine.HasExplicitPeerSignal` (lines 418–423) reads `PairsWellWith` and `CompatibleBlends` from that pool. The spec leaves that method unchanged (lines 63, 71–73, 83, 113).

`BioStack.Infrastructure` references Domain only, not Contracts. The return shape that fits the allowed files, carries `Id`, and matches the existing `ReturnsAsync(new List<KnowledgeEntry>…)` setups is `Task<List<KnowledgeEntry>>`. The spec does not print that signature. See N-2. It is not a second legal target that drops `Id`.

### 6. Live labels are not acceptance

Objective (line 36), fact 6 (line 62), contracts (line 84), evidence grade (line 92), review lifecycle steps 5 and 6 (lines 100–101), and acceptance criterion 7 (line 115) close this parcel on local mapper and projection tests plus a coordinator transcript. The transcript must name the deployed revision and show public Glycine and Retatrutide `classification` still `Unknown`. Live `AminoAcid` and `ResearchCompound` belong to a later repair parcel. The library class-label launch gate stays failed after that transcript (line 100). This parcel does not authorize go-live (lines 36, 40, 95).

### 7. Retatrutide Peptide is unreviewed; the seed stays forbidden

Committed `substances-seed.json` lines 2024–2032: canonical name `Retatrutide`, aliases `LY3437943` and `triple agonist`, `"classification": "Peptide"`. Glycine at lines 37029–37036 is `"Amino Acid"`. `go-live-library-2026-10-14-retatrutide` (`review-decision-batch-2026-10-14-go-live-library.json` lines 141–163) approves promotion and names no class. Fact 5 (line 61), provenance (line 88), out of scope (line 132), and Forbidden (lines 156 and 159) call Peptide unreviewed seed drift, forbid a seed edit, and forbid treating Peptide as the reviewed class. No acceptance criterion tells the builder to edit `substances-seed.json`. `LocalKnowledgeSource` still hardcodes Retatrutide as `Pharmaceutical` (lines 228–230). The spec does not tell the builder to change that row or to choose a class.

### 8. Integers 11 and 12 are free

`CompoundCategory` is `Unknown = 0` through `Other = 10` (`CompoundCategory.cs` lines 5–15). No member uses 11 or 12. No `AminoAcid` or `ResearchCompound` member exists. The spec appends `AminoAcid = 11` and `ResearchCompound = 12` and forbids renumbering 0 through 10 (lines 69, 81, 112, 158). `IconFor` and `AccentFor` already end in `_` (`ProtocolPortalService.cs` lines 656–666), so those switches do not need an edit. `MapClassification` still sends Amino Acid, Research Compound, and Small Molecule to `Unknown` through the discard arm (`SubstanceCanonicalizer.cs` lines 97–111). The spec maps only the first two strings and leaves Small Molecule on `Unknown` (lines 70 and 119).

`Program.cs` line 100 registers a parameterless `JsonStringEnumConverter`. `Sarm` and `Serm` stay the wire names. The new members serialize as `AminoAcid` and `ResearchCompound` without an allowed-file change. Spec lines 80 and 112 match that.

### 9 and 10. No cold gate and no Refresh

The spec file has no `TODO`, `TBD`, `FIXME`, or placeholder token. It has no "under 3 seconds" budget and does not use the word "second". Fact 8 (line 64) says the 334 ms list is not a cold-start measurement and that cold-start latency is outside this parcel. Acceptance criterion 7 (line 115) says cold-start time is not pass or fail.

`Refresh` appears only as something this parcel must not run (lines 36, 51, 101, 105, 132, 157).

## Findings

| ID | Severity | Finding |
| --- | --- | --- |
| N-1 | Nit | Acceptance criterion 4 ends the exclusion list with "the optimization strings." `OptimizationSupplements` is a `List<string>` (`KnowledgeEntry.cs` line 39) and is `[property: JsonIgnore]` (`KnowledgeEntryResponse.cs` line 44). Acceptance criterion 5 says "optimization columns," and Tests item 2 says "optimization properties." Those two sentences already exclude the list. The column is not optional. |
| N-2 | Nit | `ListPublicCompoundsAsync` has no written signature. Sibling methods return `Task<List<KnowledgeEntry>>`, `Id` is on `KnowledgeEntry`, and Infrastructure cannot reference `KnowledgeEntryResponse` without a project file that is not allowed. The boundary-test setups already pass `List<KnowledgeEntry>`. Naming `Task<List<KnowledgeEntry>> ListPublicCompoundsAsync(CancellationToken cancellationToken = default)` would remove the last reading step. It is not a second contract. |
| N-3 | Nit | Tests item 4 says the fake returns "the same entries as" `GetAllCompoundsAsync`. That method throws `NotSupportedException` and does not return a list (`TranscriptCandidatePromotionPreviewServiceTests.cs` lines 68–70). Delegating to it satisfies "preview behavior does not change." Do not replace the throw with a new returned catalog. |

## Validator

`rtk` is not installed in this shell (`rtk` is not recognized). The validator was run directly.

Command:

`pwsh -NoProfile -File docs/specs/scripts/validate-spec.ps1 -SpecPath docs/specs/active/BIO-GOLIVE-001-public-classification-and-list-read.md -RepoRoot D:\Repos\BioStack`

Exit code 0. Trimmed result:

- `result`: `valid`
- `shape`: `ticket-spec`
- `reason`: null
- `reviewerCount`: 2
- `requiredChecks`: focused tests, lint/build as affected, provenance/license/freshness, fail-closed promotion, canonical-write fencing tests
- Bound fields `function_review_status`, `function_review_owner`, `capability_claim`, `missingness`, `numeric_provenance`, and `escalation` passed
- Required headings for `standard` and `knowledge-promotion` are the headings the validator listed, and they are present

`standingAuthorizationEligible: true` is a router field on this document. It is not go-live authorization, not deploy authority, and not permission to run Refresh.

No product tests were run. The new projection test does not exist yet. This review did not modify the spec or product source.
