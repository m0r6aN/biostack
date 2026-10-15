# BIO-GOLIVE-001 implementation review

- Label: impl-review-1
- Date: 2026-10-14
- Spec: `docs/specs/active/BIO-GOLIVE-001-public-classification-and-list-read.md`
- Gate 2: `docs/INITIATIVES/biostack-go-live/dispatch/GATE2-BIO-GOLIVE-001.md`
- Commit: `6eb3710148d94e90f68cc2bbc5ddd433204dd746`
- Parent / Gate 2 base: `5b686e69536f2eedf0195ee77466f20b08a490d8`
- Branch: `feat/bio-golive-001-classification-list`
- Worktree: `D:\Repos\BioStack\.worktrees\bio-golive-001-classification-list`
- Author: m0r6aN `<morganclint76@gmail.com>`. This reviewer did not write the commit.
- Verdict: **PASS**
- Production readiness: **NO-GO / HOLD**. This pass is the implementation review only. It does not authorize deploy, Refresh, or go-live.

`rtk` is not on PATH in this environment. Commands used plain `git` and `dotnet` (`dotnet` 10.0.402).

## Definition of Done

| # | item | status | evidence |
| --- | --- | --- | --- |
| 1 | Diff is only the ten allowed files. No seed, TrustGate, review JSON, `Program.cs`, or `JsonIgnore` edit. | Pass | `git diff --name-only` against the parent is exactly those ten paths. `KnowledgeEntryResponse.cs` is not in the diff. |
| 2 | Enum appends `AminoAcid = 11` and `ResearchCompound = 12`. Values 0 through 10 are not renumbered. SARM/SERM names unchanged. | Pass | `CompoundCategory.cs` still has `Unknown = 0` through `Other = 10`, then the two new members. Member names remain `Sarm` and `Serm`. |
| 3 | `MapClassification` maps only `"Amino Acid"` and `"Research Compound"` as the new members. `"Small Molecule"` and every other previously unmapped class stay `Unknown`. Peptide, Pharmaceutical, Supplement, Hormone, SARM, and SERM mappings are unchanged. | Pass | Switch arms for those six strings are unchanged except alignment whitespace. The two new arms are exact schema strings. The discard arm is still `Unknown`. |
| 4 | `GetAllCompoundsAsync` stays a full-entity read on `DatabaseKnowledgeSource` and `LocalKnowledgeSource`. Only `KnowledgeService.GetAllCompoundsAsync` switches to `ListPublicCompoundsAsync`. No other service method changes its source call. | Pass | Database method is still `ToListAsync()` on the entity. Local method still returns the backing entries. The service diff is that one call. Other services are not in the commit and still call `GetAllCompoundsAsync`. |
| 5 | Public list projection includes `Id` and every column `PublicEvidenceCompleteness` reads, plus the other public response columns. It does not select dose, schedule, pairing, or optimization columns. Tie-break stays completeness descending, then `KnowledgeEntry.Id`. | Pass | Both projections set `Id`, `CanonicalName`, `Aliases`, `Classification`, `RegulatoryStatus`, `MechanismSummary`, `EvidenceTier`, `SourceReferences`, `Notes`, `Pathways`, `Benefits`, `AvoidWith`, and `DrugInteractions`. They do not assign `RecommendedDosage`, `Frequency`, `PreferredTimeOfDay`, `StandardDosageRange`, `MaxReportedDose`, `WeeklyDosageSchedule`, `IncrementalEscalationSteps`, `TieredDosing`, `PairsWellWith`, `CompatibleBlends`, `VialCompatibility`, `OptimizationProtein`, `OptimizationCarbs`, `OptimizationSupplements`, `OptimizationSleep`, or `OptimizationExercise`. `MapPublicGroupToResponse` is unchanged: `OrderByDescending(PublicEvidenceCompleteness).ThenBy(entry => entry.Id)`. |
| 6 | `GetCompoundAsync` still uses the full entity for canonical name and alias. | Pass | `KnowledgeService.GetCompoundAsync` still calls `IKnowledgeSource.GetCompoundAsync`. `DatabaseKnowledgeSource.GetCompoundAsync` is not in the diff and still loads the entity, then scans `Aliases`. |
| 7 | Every concrete `IKnowledgeSource` implementor compiles. `PreviewFakeKnowledgeSource.GetAllCompoundsAsync` throws. `ListPublicCompoundsAsync` throws the same way. Preview behavior does not change. Public-boundary list setups stub `ListPublicCompoundsAsync`. Dose-field assertions are not weakened. | Pass | The three implementors are `DatabaseKnowledgeSource`, `LocalKnowledgeSource`, and `PreviewFakeKnowledgeSource`. The fake delegates `ListPublicCompoundsAsync` to the throwing `GetAllCompoundsAsync`. Preview still calls only `GetCompoundAsync`. The public-boundary diff changes the two list setups and nothing else. |
| 8 | Tests assert Amino Acid → `AminoAcid`, Research Compound → `ResearchCompound`, Small Molecule → `Unknown`, the service call to `ListPublicCompoundsAsync`, that the database projection omits dose/schedule/pairing/optimization, and that stored integer 11 reads as `AminoAcid`. | Pass | See the test files cited below. Both focused commands passed. |
| 9 | Re-run both focused test commands and record pass counts. | Pass | Knowledge worker filter: 16 passed, 0 failed, exit 0. Application filter: 18 passed, 0 failed, exit 0. |
| 10 | Post-deploy HTTP transcript is not this reviewer's job. Do not fail the commit for its absence. | Pass | `docs/INITIATIVES/biostack-go-live/evidence/BIO-GOLIVE-001-http-transcript.md` is absent. The parcel stays not-yet-closed until that coordinator transcript exists. No production result is claimed. |

## Findings

No blocker, major, or minor.

### Nit

`LocalKnowledgeSource.CopyPublicCompound` assigns the same list instances (`Aliases`, `SourceReferences`, `Pathways`, `Benefits`, `AvoidWith`, `DrugInteractions`) rather than copying them. A caller that mutates those lists would change the in-memory catalog. Dose and optimization fields are not copied, and the database projection materializes new lists. The spec does not require a defensive copy, and the local test still checks public values and default dose fields. No change is required for this pass.

## Implementors

Search for `: IKnowledgeSource` in `*.cs` found three concrete implementors and no `, IKnowledgeSource` implementor:

- `DatabaseKnowledgeSource` implements `ListPublicCompoundsAsync`.
- `LocalKnowledgeSource` implements `ListPublicCompoundsAsync`.
- `PreviewFakeKnowledgeSource` implements `ListPublicCompoundsAsync` by calling `GetAllCompoundsAsync`, which throws `NotSupportedException`.

`KnowledgeSourceIntakeService` implements `IKnowledgeSourceIntakeService`, not `IKnowledgeSource`. Mocks are not concrete implementors. `BioStack.Infrastructure` and `BioStack.Application.Tests` both compiled in the application test run, so all three classes were compiled by the focused tests. None is missing the new method.

## What the commit does

`git diff --stat` against the parent: 10 files, 481 insertions, 9 deletions.

Allowed files, all present and no others:

- `backend/src/BioStack.Domain/Enums/CompoundCategory.cs`
- `backend/src/BioStack.KnowledgeWorker/Pipeline/SubstanceCanonicalizer.cs`
- `backend/src/BioStack.Infrastructure/Knowledge/IKnowledgeSource.cs`
- `backend/src/BioStack.Infrastructure/Knowledge/DatabaseKnowledgeSource.cs`
- `backend/src/BioStack.Infrastructure/Knowledge/LocalKnowledgeSource.cs`
- `backend/src/BioStack.Application/Services/KnowledgeService.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/SubstanceCanonicalizerClassificationTests.cs`
- `backend/tests/BioStack.Application.Tests/Services/KnowledgeListProjectionTests.cs`
- `backend/tests/BioStack.Application.Tests/Services/KnowledgeServicePublicBoundaryTests.cs`
- `backend/tests/BioStack.Application.Tests/Services/TranscriptCandidatePromotionPreviewServiceTests.cs`

`KnowledgeService` changes one line: `GetAllCompoundsAsync` calls `ListPublicCompoundsAsync`. Grouping, `PublicEvidenceCompleteness`, and the `Id` tie-break are untouched. `GetCompoundAsync` and `SearchByPathwayAsync` are untouched.

The database list query selects the public columns into an anonymous type and maps them onto a new `KnowledgeEntry`. `GetAllCompoundsAsync` remains `AsNoTracking().ToListAsync()`. The SQLite test reads stored `Classification` as integer 11, then `ListPublicCompoundsAsync` returns `CompoundCategory.AminoAcid`, and the captured `SELECT` contains the public column names and not the excluded dose, schedule, pairing, or optimization names. The same context's full read still returns those excluded values.

Public response columns checked against `KnowledgeEntryResponse` (fields without `JsonIgnore`) are `CanonicalName`, `Aliases`, `Classification`, `RegulatoryStatus`, `MechanismSummary`, `EvidenceTier`, `SourceReferences`, `Notes`, `Pathways`, `Benefits`, `AvoidWith`, and `DrugInteractions`. `PublicEvidenceCompleteness` reads `SourceReferences`, `Pathways`, `Benefits`, `AvoidWith`, `DrugInteractions`, `MechanismSummary`, and `RegulatoryStatus`. `Id` is included for the tie-break. Those are the projected columns.

`MapClassification` still maps `Peptide`, `Pharmaceutical`, `Supplement`, `Hormone`, `SARM`, and `SERM` to the existing members. `"Amino Acid"` and `"Research Compound"` map to 11 and 12. `"Small Molecule"`, `"Biologic"`, `"Vitamin"`, `"Mineral"`, `"Other"`, empty, and the lowercase variants asserted in the test stay `Unknown`.

`KnowledgeServicePublicBoundaryTests` only retargets the two list setups from `GetAllCompoundsAsync` to `ListPublicCompoundsAsync`. Dose, pairing, and optimization assertions on `GetCompoundAsync` are unchanged. `GetAllCompoundsAsync_UsesTheSamePublicProjection` still expects an empty `RecommendedDosage`, an empty `PairsWellWith`, and the caution list. The dedup test still prefers the evidence-richer row and merges the caution lists.

`TranscriptCandidatePromotionPreviewService` still uses `GetCompoundAsync` only. The twelve preview tests passed, so preview behavior did not change. The spec sentence that the fake returns the same entries as `GetAllCompoundsAsync` is satisfied by delegation: that method throws, and the review rule accepts that delegation.

## Commands

Worktree was clean (`git status --short` empty). HEAD was the reviewed commit. Parent was exactly `5b686e69536f2eedf0195ee77466f20b08a490d8`.

```text
git rev-parse HEAD
6eb3710148d94e90f68cc2bbc5ddd433204dd746

git log -1 --format="%H%n%P%n%s"
6eb3710148d94e90f68cc2bbc5ddd433204dd746
5b686e69536f2eedf0195ee77466f20b08a490d8
feat: map amino acid and research compound without narrowing the full catalog read
```

`git diff --stat 5b686e69536f2eedf0195ee77466f20b08a490d8 6eb3710148d94e90f68cc2bbc5ddd433204dd746` → 10 files, +481 / −9, names listed above.

Transcript check: `Test-Path` on `D:\Repos\BioStack\docs\INITIATIVES\biostack-go-live\evidence\BIO-GOLIVE-001-http-transcript.md` printed `TRANSCRIPT_ABSENT`.

Focused tests, cwd `D:\Repos\BioStack\.worktrees\bio-golive-001-classification-list`:

```text
dotnet test backend/tests/BioStack.KnowledgeWorker.Tests --filter SubstanceCanonicalizerClassificationTests
```

```text
Passed!  - Failed:     0, Passed:    16, Skipped:     0, Total:    16, Duration: 40 ms - BioStack.KnowledgeWorker.Tests.dll (net10.0)
EXIT_KNOWLEDGE=0
```

The 16 results are 4 approved-schema cases, 9 unmapped-schema cases including `Small Molecule`, 1 integer and serialization fact, and 2 existing `SARM`/`SERM` cases.

```text
dotnet test backend/tests/BioStack.Application.Tests --filter "FullyQualifiedName~KnowledgeListProjectionTests|FullyQualifiedName~KnowledgeServicePublicBoundaryTests|FullyQualifiedName~TranscriptCandidatePromotionPreviewServiceTests"
```

```text
Passed!  - Failed:     0, Passed:    18, Skipped:     0, Total:    18, Duration: 1 s - BioStack.Application.Tests.dll (net10.0)
EXIT_APPLICATION=0
```

`--list-tests --no-build` for that filter listed 3 `KnowledgeListProjectionTests`, 3 `KnowledgeServicePublicBoundaryTests`, and 12 `TranscriptCandidatePromotionPreviewServiceTests` (9 facts plus 3 theory cases).

Both builds emitted pre-existing `NU1902` / `NU1903` advisories for `System.Security.Cryptography.Pkcs` 10.0.10 and `System.Security.Cryptography.Xml` 10.0.10. This commit does not change package references. Those warnings are outside this parcel and do not change the verdict. The Knowledge worker build also emitted existing nullable and xUnit warnings in files this commit does not touch.

## Parcel status

The implementation review passes. The parcel stays **not-yet-closed** until a coordinator writes `docs/INITIATIVES/biostack-go-live/evidence/BIO-GOLIVE-001-http-transcript.md` after a human deploy. That transcript must name the deployed revision and show the stored Glycine and Retatrutide labels still `Unknown`. This review did not call production, did not run Refresh, and does not invent that result. Production readiness stays **NO-GO / HOLD**.

## Fix checklist

None.
