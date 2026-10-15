# BIO-GOLIVE-001 spec review 4

- Reviewer: spec-review-4
- Date: 2026-10-14
- Spec: `docs/specs/active/BIO-GOLIVE-001-public-classification-and-list-read.md`
- Verdict: **FAIL**
- Scope: read-only check of that spec against current source. No spec edit, no product edit, no go-live recommendation.

The acceptance criteria cannot be proven by the allowed work. One blocker remains.

## Findings

| ID | Severity | Finding | Spec lines | Source lines |
| --- | --- | --- | --- | --- |
| F1 | Blocker | `KnowledgeServicePublicBoundaryTests` cannot stay green inside the allow-list. The spec requires those tests to stay green and not be weakened, and it also requires `KnowledgeService.GetAllCompoundsAsync` to call `ListPublicCompoundsAsync` only. The two existing tests stub `IKnowledgeSource.GetAllCompoundsAsync` and then call the service. After the switch, Moq returns null for the new method and both tests throw. That test file is not allowed, so the builder cannot retarget the setups. | 71, 119, 136–145 | `KnowledgeService.cs` 22–28; `KnowledgeServicePublicBoundaryTests.cs` 47–59 and 63–86 |
| F2 | Minor | The only concrete `IKnowledgeSource` outside the two production sources is `PreviewFakeKnowledgeSource`. A non-default interface method, matching every current member, makes `dotnet test backend/tests/BioStack.Application.Tests --filter KnowledgeListProjectionTests` fail to compile. A default method body on the allowed interface file would compile, but the spec does not say that. | 69, 120, 136–145 | `IKnowledgeSource.cs` 5–19; `TranscriptCandidatePromotionPreviewServiceTests.cs` 59–90 |
| F3 | Minor | `ListPublicCompoundsAsync` has no return type. The duplicate tie-break uses `KnowledgeEntry.Id`. `KnowledgeEntryResponse` has no `Id`, and the spec forbids a new public field. The method must return `KnowledgeEntry` (or a non-public type that still carries `Id`). | 69–71, 78, 111 | `KnowledgeService.cs` 94–101; `KnowledgeEntryResponse.cs` 7–17; `KnowledgeEntry.cs` 7 |
| F4 | Nit | A check of the public response object cannot see omitted columns. `MapToResponse` already blanks dose, schedule, pairing, and optimization in memory. Column exclusion is observable only from SQL command text. | 70, 111, 118 | `KnowledgeService.cs` 37–70; `DatabaseKnowledgeSource.cs` 44–48 |

No production caller of full-entity `IKnowledgeSource.GetAllCompoundsAsync` was forgotten. See check 3.

## Check results

### 1. CompoundCategory 0 through 10; 11 and 12 unused — PASS

`backend/src/BioStack.Domain/Enums/CompoundCategory.cs` 5–15:

- `Unknown = 0`, `Peptide = 1`, `Coenzyme = 2`, `Pharmaceutical = 3`, `Supplement = 4`, `Compound = 5`, `Sarm = 6`, `Serm = 7`, `Hormone = 8`, `Nutraceutical = 9`, `Other = 10`.
- No member uses 11 or 12. No `AminoAcid` or `ResearchCompound` member exists.
- The migration stores the column as an unconstrained integer: `20260422125251_RecoverBillingTierEnforcement.cs` 76 (`Classification` `INTEGER`). Appending 11 and 12 does not renumber 0–10. Spec 67 and 79 match this.

### 2. JsonStringEnumConverter on the public classification field — PASS

The field is `KnowledgeEntryResponse.Classification` at `backend/src/BioStack.Contracts/Responses/KnowledgeEntryResponse.cs` line 10. That property has no converter attribute.

The converter that serializes it is the parameterless registration at `backend/src/BioStack.Api/Program.cs` line 100:

`options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());`

`KnowledgeEndpoints.GetAllCompounds` returns `Results.Ok(compounds)` at `KnowledgeEndpoints.cs` 28–29, so it uses those HTTP JSON options. There is no second `AddJsonOptions` registration under `BioStack.Api`. The parameterless converter emits the member name, so `Sarm` and `Serm` stay `Sarm` and `Serm`, and the new members would emit `AminoAcid` and `ResearchCompound`. Spec 78 and 109 match. No allowed-file change is required for the converter.

### 3. New method, full-entity read, forgotten callers — PASS on callers

The spec names `ListPublicCompoundsAsync` on `IKnowledgeSource` (spec 69), `DatabaseKnowledgeSource` (spec 70), and `LocalKnowledgeSource` (spec 71). It says `KnowledgeService.GetAllCompoundsAsync` calls that method only (spec 71) and leaves source `GetAllCompoundsAsync` as the full-entity read (spec 61, 69–71). Current source still has the full read at `DatabaseKnowledgeSource.cs` 44–48 (`ToListAsync`) and `LocalKnowledgeSource.cs` 24–26.

Production callers of `IKnowledgeSource.GetAllCompoundsAsync`:

| Caller | Lines | In the spec? |
| --- | --- | --- |
| `KnowledgeService.GetAllCompoundsAsync` | `KnowledgeService.cs` 24 | Yes. This is the only caller that must switch. |
| `InteractionIntelligenceService` swap pool | 504 (`EvaluateSwapsWithTraceAsync`) and 528 (`BuildSwapsAsync`) | Yes. `SwapRecommendationEngine` does not call the source. It reads pairing off that pool at `SwapRecommendationEngine.cs` 418–423. |
| `CounterfactualCandidateService` | 22 and 49 | Yes. |
| `ProtocolParser` | 83 | Yes. |

No forgotten production caller. `KnowledgeEndpoints.cs` 28 calls the service method, which is the public list this parcel is changing, not another full-entity caller. `AnalyzerGoalVocabularyTests.cs` 73–74 calls `LocalKnowledgeSource.GetAllCompoundsAsync` directly and stays valid because that method stays full-entity.

F2 is the unnamed implementer, not a forgotten caller.

### 4. Projection, Id tie-break, and what the named test can observe — PASS

`MapPublicGroupToResponse` orders by evidence completeness and then `entry.Id` at `KnowledgeService.cs` 98–100. The response type has no `Id` (`KnowledgeEntryResponse.cs` 7–17). The projection must therefore still carry `Id` into the service. Spec 111 says that.

Excluded members in spec 111 match the ignored or non-public columns on `KnowledgeEntry.cs` 20–41 and the `[property: JsonIgnore]` members on `KnowledgeEntryResponse.cs` 18–50. Public columns that must remain are the response members without `JsonIgnore`, including `AvoidWith` and `DrugInteractions`.

`KnowledgeListProjectionTests` can observe this without a production database. `BioStack.Application.Tests` already builds a local SQLite `BioStackDbContext` and calls `EnsureCreated` (`BillingAndFeatureGateTests.cs` 421–428; `KnowledgeSourceIntakeServiceTests.cs` 18–24). `DatabaseKnowledgeSource` is reachable through the Application project reference. A `DbCommandInterceptor` on that local context can assert the `SELECT` contains `Id` and the public columns and does not contain `RecommendedDosage`, `Frequency`, `PreferredTimeOfDay`, `StandardDosageRange`, `MaxReportedDose`, `WeeklyDosageSchedule`, `IncrementalEscalationSteps`, `TieredDosing`, `PairsWellWith`, `CompatibleBlends`, `VialCompatibility`, or the five optimization columns. EF Core translates `Select(e => new KnowledgeEntry { ... })` to those assigned columns only.

The same local SQLite file can store integer 11 (`Classification` is `INTEGER`) and read it back as `AminoAcid` after the enum member exists. The Id tie-break itself needs no database: two in-memory rows with equal completeness and different ids are enough, if the new method returns a type that still has `Id`.

What is not observable from the response object alone: column exclusion. `MapToResponse` already replaces dose, schedule, pairing, and optimization with empty values (`KnowledgeService.cs` 52–69). A green response assertion would pass even if the query still loaded those columns. Live Glycine and Retatrutide labels are also outside this test. The spec already assigns those to the coordinator transcript (spec 90, 113, 121) and does not claim the local test proves them.

### 5. Close condition does not require live labels — PASS

Spec 34, 82, 90, 95, and 113 close this parcel on a coordinator transcript that names the deployed revision and shows Glycine and Retatrutide still `Unknown`. Live `AminoAcid` and `ResearchCompound` are reserved for a later repair parcel. Cold-start time is explicitly not pass/fail (spec 113). This parcel does not authorize go-live (spec 38, 95).

### 6. Retatrutide seed drift, seed edit forbidden, no conflict — PASS

Current `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json` 2024–2032: canonical name `Retatrutide`, aliases `LY3437943` and `triple agonist`, `"classification": "Peptide"`. Glycine at 37029–37036 is `"Amino Acid"`. Spec 59 calls the Peptide value unreviewed drift and says this parcel does not edit the seed and does not choose a class. The same prohibition is repeated at spec 86, 129, 151, and 154. No acceptance criterion, allowed-file rule, or test instruction tells the builder to edit `substances-seed.json` or to treat Peptide as the reviewed class. `LocalKnowledgeSource.cs` 228–230 still has its own hardcoded Retatrutide `Pharmaceutical` row; the spec does not tell the builder to change it.

### 7. Allowed files sufficient, forbidden files still forbidden — FAIL

Forbidden files stay forbidden. The allow-list does not include `substances-seed.json`, `TrustGate.cs`, review-decision JSON, or `KnowledgeEntryResponse` `JsonIgnore` attributes (spec 149–154). Enum renumbering, Refresh, production connection strings, and deploy are forbidden and are not in the allow-list.

The allow-list is not sufficient because of F1. The code edits for the enum, mapper, interface, both sources, and `KnowledgeService` are in the allow-list (spec 136–145) and match the surfaces that must change. The HTTP transcript is correctly coordinator-owned (spec 73, 121, 147). F2 is survivable only by a default interface method the spec never states.

### 8. No placeholder, no "under 3 seconds" gate, no Refresh — PASS

The spec file has no `TODO`, `TBD`, `FIXME`, or placeholder token. It has no "under 3 seconds" or other latency gate. `Refresh` appears only as a prohibition (spec 34, 49, 99, 103, 129, 152).

## Fix checklist

1. Add `backend/tests/BioStack.Application.Tests/Services/KnowledgeServicePublicBoundaryTests.cs` to Allowed Files. Say that `GetAllCompoundsAsync_UsesTheSamePublicProjection` and `GetAllCompoundsAsync_DeduplicatesCanonicalNamesWithoutDeletingEvidence` must stub `ListPublicCompoundsAsync` instead of `GetAllCompoundsAsync`, and that their assertions must not be weakened. This is the blocker.
2. Either allow `TranscriptCandidatePromotionPreviewServiceTests.cs` for one throwing `ListPublicCompoundsAsync` stub, or require a default interface method in `IKnowledgeSource.cs` so the focused Application.Tests command compiles. Do not leave both unspecified.
3. Declare `ListPublicCompoundsAsync` as `Task<List<KnowledgeEntry>>` (same cancellation token as `GetAllCompoundsAsync`) so `Id` still reaches `KnowledgeService.cs` 100. Do not put `Id` on `KnowledgeEntryResponse`.
4. Tell `KnowledgeListProjectionTests` to capture local SQLite command text. Do not accept an assertion that only inspects the cleared response object.

## Evidence

`rtk` is not installed in this shell. Evidence is from direct reads of the spec and the source lines cited above. No product tests were run. The new tests do not exist yet, and this review did not modify source.
