# BIO-GOLIVE-001 spec review

- Reviewer: spec-review-6
- Date: 2026-10-14
- Spec: `docs/specs/active/BIO-GOLIVE-001-public-classification-and-list-read.md`
- Spec git blob: `cb401509df53c00f261128743ce2816e78f48e6d` (working tree; `git status` shows this file modified)
- Tree: HEAD `5b686e69536f2eedf0195ee77466f20b08a490d8`, which is an ancestor of `9550964256d507d03b789df8f0eb4c448a6f7ac8`. The cited source files below are clean at that HEAD.
- Verdict: **PASS**

A builder can add `ListPublicCompoundsAsync` and point `KnowledgeService.GetAllCompoundsAsync` at it without leaving the allowed file list. The solution still compiles. Every concrete `IKnowledgeSource` class is in that list. Acceptance is a fresh local mapping plus a coordinator transcript that still shows `Unknown`. It does not require live `AminoAcid` or `ResearchCompound`. The committed Retatrutide `Peptide` row is not the reviewed class. This review does not recommend or authorize go-live, a production deploy, a Refresh, or a library-gate pass.

## Decisions

| Question | Result |
| --- | --- |
| Can the builder add `ListPublicCompoundsAsync` and update `KnowledgeService.GetAllCompoundsAsync` inside Allowed Files and still compile? | Yes. |
| Is every concrete `IKnowledgeSource` class in Allowed Files? | Yes. Three classes. None missing. |
| Are `KnowledgeServicePublicBoundaryTests` allowed, given they stub the old method? | Yes. The file is allowed, and the spec tells the builder to retarget the list setups. |
| Does acceptance require live `AminoAcid` or `ResearchCompound`? | No. Those labels belong to a later repair parcel. |
| Is the committed Retatrutide `Peptide` row treated as the reviewed class? | No. |

## Definition of Done

| Item | Status |
| --- | --- |
| Add `ListPublicCompoundsAsync` on the interface and both production sources; leave source `GetAllCompoundsAsync` a full-entity read | **Pass** — all three files are allowed. No other production class implements the interface. |
| `KnowledgeService.GetAllCompoundsAsync` calls only the new method; no other method on that service changes | **Pass** — `KnowledgeService.cs` is allowed. The public endpoint already calls the service method. |
| Solution still compiles after that change | **Pass** — the only concrete implementers are allowed, and the only `CompoundCategory` switches already have a discard arm. Not compiled here; the new method does not exist yet. |
| `KnowledgeServicePublicBoundaryTests` stay green without weakening | **Pass** — they stub `GetAllCompoundsAsync` today, the file is allowed, and Tests item 3 requires those setups to stub `ListPublicCompoundsAsync` instead. |
| `PreviewFakeKnowledgeSource` implements the new method | **Pass** — its test file is allowed. |
| Fresh `Amino Acid` and `Research Compound` map to enum members 11 and 12; integers 0–10 stay put | **Pass** — `CompoundCategory` ends at `Other = 10`. `MapClassification` is private but reachable through public `ToKnowledgeEntry` in the allowed canonicalizer file. |
| Live Glycine and Retatrutide stay `Unknown`; that transcript closes the parcel | **Pass** — Acceptance criterion 7. Live `AminoAcid` and `ResearchCompound` are not acceptance. |
| Committed Retatrutide `Peptide` is not a reviewed class and is not edited | **Pass** — Fact 5, Source/License/Provenance, Out of Scope, and Forbidden. The promotion record approves promotion and names no class. |
| No production Refresh, seed edit, or row rewrite | **Pass** — Forbidden and Promotion Authority. |
| Focused test commands are named | **Pass** — Tests item 5. They are the acceptance commands. Not run; the new tests do not exist yet. |
| This review does not recommend go-live | **Pass** |

## Findings

| id | severity | location | finding |
| --- | --- | --- | --- |
| N1 | nit | Surfaces (lines 71–73); `IKnowledgeSource.cs` lines 5–19 | The new method's signature is not written out. The only signature that matches the existing members and stays inside the allowed files is `Task<List<KnowledgeEntry>> ListPublicCompoundsAsync(CancellationToken cancellationToken = default)`. No spec change is required. |
| N2 | nit | Tests item 4 (line 122); `TranscriptCandidatePromotionPreviewServiceTests.cs` lines 68–70 | "Returning the same entries as its `GetAllCompoundsAsync`" describes a method that throws `NotSupportedException` and returns no entries. Delegating to that method keeps preview behavior unchanged. No spec change is required. |

No blocker or major findings.

### N1 — signature is fixed by the files already allowed

`IKnowledgeSource` returns `Task<List<KnowledgeEntry>>` and takes `CancellationToken cancellationToken = default` on every current member. `KnowledgeService.GetAllCompoundsAsync` has to group those entries by canonical name, score `PublicEvidenceCompleteness`, and break ties on `Id`. A new response type would need a file that is not allowed. `KnowledgeEntryResponse` is forbidden because its `JsonIgnore` attributes stay. The builder should copy the existing member shape.

### N2 — preview fake should delegate, not start returning rows

`PreviewFakeKnowledgeSource.GetAllCompoundsAsync` throws. Preview tests call `GetCompoundAsync` only. `ListPublicCompoundsAsync` can return the task from `GetAllCompoundsAsync`. That satisfies "preview behavior does not change" without turning the throw into a row result.

## Concrete `IKnowledgeSource` classes

| Class | File | Allowed |
| --- | --- | --- |
| `DatabaseKnowledgeSource` | `backend/src/BioStack.Infrastructure/Knowledge/DatabaseKnowledgeSource.cs` line 7 | Yes |
| `LocalKnowledgeSource` | `backend/src/BioStack.Infrastructure/Knowledge/LocalKnowledgeSource.cs` line 6 | Yes |
| `PreviewFakeKnowledgeSource` | `backend/tests/BioStack.Application.Tests/Services/TranscriptCandidatePromotionPreviewServiceTests.cs` line 59 | Yes |

No other `class` or `record` in the tree implements `IKnowledgeSource`. `Mock<IKnowledgeSource>` is not a concrete class. Tests that stub `GetAllCompoundsAsync` for the parser, swap pool, counterfactual pool, or interaction service stay correct because those callers keep the full-entity method.

`KnowledgeServicePublicBoundaryTests` stubs the old method at lines 52 and 83, then calls `KnowledgeService.GetAllCompoundsAsync`. That file is allowed (spec line 149). After the service switches, an unstubbed Moq method returns null and those two tests throw. Tests item 3 already requires the list setups to stub `ListPublicCompoundsAsync` instead. The assertions stay. The single-compound test still stubs `GetCompoundAsync`.

## Verified facts

- HEAD is the dispatch commit named in Fact 1. That commit is an ancestor of the named `origin/main` commit. The shaping pin named in Fact 1 exists. This review did not re-check historical seed contents at that pin.
- `CompoundCategory` is `Unknown = 0` through `Other = 10`. There is no member 11 or 12. `Sarm` and `Serm` are the member names, so `JsonStringEnumConverter` in `backend/src/BioStack.Api/Program.cs` line 100 emits `Sarm` and `Serm`.
- `MapClassification` maps Peptide, Pharmaceutical, Supplement, Hormone, SARM, and SERM. Every other string, including Amino Acid, Research Compound, and Small Molecule, hits the discard and becomes `Unknown`. The seed contains `Small Molecule` rows, so the negative assertion is real.
- The only `CompoundCategory` switch expressions are `IconFor` and `AccentFor` in `ProtocolPortalService.cs` lines 656–666. Both end in `_`. Adding two members does not make them non-exhaustive.
- `KnowledgeEntries.Classification` is `table.Column<int>(type: "INTEGER")` in migration `20260422125251_RecoverBillingTierEnforcement.cs` line 76. The current `KnowledgeEntry` configuration does not convert that property to a string. A new enum member does not need a migration or a file outside the allow-list.
- `DatabaseKnowledgeSource.GetAllCompoundsAsync` is `ToListAsync()` on the entity (lines 44–48). `LocalKnowledgeSource.GetAllCompoundsAsync` returns the full in-memory list (lines 24–26).
- Production callers of `IKnowledgeSource.GetAllCompoundsAsync` are `InteractionIntelligenceService` (lines 504 and 528), `CounterfactualCandidateService` (lines 22 and 49), and `ProtocolParser` (line 83). `SwapRecommendationEngine.HasExplicitPeerSignal` (lines 420–423) reads `PairsWellWith` and `CompatibleBlends` from the pool those calls return. The spec leaves that method unchanged and adds a separate list method. `KnowledgeEndpoints` line 28 calls `KnowledgeService.GetAllCompoundsAsync`, which is the only service method the spec switches.
- `KnowledgeService.GetAllCompoundsAsync` (lines 22–28) reads canonical name, evidence fields, and `Id` through `MapPublicGroupToResponse` and `PublicEvidenceCompleteness` (lines 73–113). `MapToResponse` already substitutes empty dose, schedule, pairing, and optimization values. `KnowledgeEntryResponse` still marks those members `JsonIgnore`. The public columns the projection must keep are `Id`, `CanonicalName`, `Aliases`, `Classification`, `RegulatoryStatus`, `MechanismSummary`, `EvidenceTier`, `SourceReferences`, `Notes`, `Pathways`, `Benefits`, `AvoidWith`, and `DrugInteractions`.
- List columns use string value converters in `BioStackDbContext.cs` lines 406–441. Selecting the public ones and omitting the excluded ones can be done in `DatabaseKnowledgeSource.cs` alone.
- Committed `substances-seed.json` classifies Retatrutide as `Peptide` (line 2032) and aliases it as `LY3437943` and `triple agonist` (lines 2026–2029). Glycine is `Amino Acid` (line 37036). `go-live-library-2026-10-14-retatrutide` is `approve-for-promotion` and its notes name no class (`review-decision-batch-2026-10-14-go-live-library.json` lines 141–163).
- Acceptance criterion 7 says the coordinator transcript closes this parcel when Glycine and Retatrutide still return `Unknown`. Live `AminoAcid` and `ResearchCompound` on those rows are acceptance for the later repair parcel only. Objective, Contracts, and Review Lifecycle say the same. This parcel must not write production.
- `validate-spec.ps1` returned `valid` for shape `ticket-spec`. `reviewerCount` is 2. `standingAuthorizationEligible: true` is a fold flag. It is not dispatch authority and not a go-live approval.

## Commands

`rtk` is not on PATH. Git and the spec linter ran directly.

- `git rev-parse HEAD` → `5b686e69536f2eedf0195ee77466f20b08a490d8`
- `git cat-file -t` for `5b686e69536f2eedf0195ee77466f20b08a490d8`, `9550964256d507d03b789df8f0eb4c448a6f7ac8`, and `6c9d8eccfd69f848d69c223e547c4d40dbf6ccee` → `commit` for each
- `git merge-base --is-ancestor 5b686e69536f2eedf0195ee77466f20b08a490d8 HEAD` → exit 0
- `git merge-base --is-ancestor 5b686e69536f2eedf0195ee77466f20b08a490d8 9550964256d507d03b789df8f0eb4c448a6f7ac8` → exit 0
- `git hash-object docs/specs/active/BIO-GOLIVE-001-public-classification-and-list-read.md` → `cb401509df53c00f261128743ce2816e78f48e6d`
- `git status --short` for that spec → ` M docs/specs/active/BIO-GOLIVE-001-public-classification-and-list-read.md`
- `git status --short` for `IKnowledgeSource.cs`, `CompoundCategory.cs`, `KnowledgeService.cs`, `KnowledgeServicePublicBoundaryTests.cs`, `TranscriptCandidatePromotionPreviewServiceTests.cs`, and `substances-seed.json` → no output
- `pwsh -NoProfile -File docs/specs/scripts/validate-spec.ps1 -SpecPath docs/specs/active/BIO-GOLIVE-001-public-classification-and-list-read.md -RepoRoot D:\Repos\BioStack` exited 0. Result: `shape` `ticket-spec`, `result` `valid`, `reason` null, `reviewerCount` 2. `dotnet build` and `dotnet test` were not run.

## Fix checklist

None. No blocker or major finding.
