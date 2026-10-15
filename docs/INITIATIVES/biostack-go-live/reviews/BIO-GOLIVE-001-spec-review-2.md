# BIO-GOLIVE-001 spec review 2

- Reviewer: spec-review-2
- Date: 2026-10-14
- Spec: `docs/specs/active/BIO-GOLIVE-001-public-classification-and-list-read.md`
- Tree: `HEAD` `5b686e69536f2eedf0195ee77466f20b08a490d8` (spec shaping pin `6c9d8eccfd69f848d69c223e547c4d40dbf6ccee` is not current `HEAD`)
- Verdict: **FAIL**

No go-live recommendation. This review does not authorize implementation, deploy, Refresh, or a production write.

## Focus results

| # | Check | Result |
| --- | --- | --- |
| 1 | Enum numbering: append `AminoAcid = 11`, `ResearchCompound = 12`, do not renumber 0–10 | **Pass** |
| 2 | `JsonStringEnumConverter` emits `AminoAcid` and `ResearchCompound` on the public classification field | **Pass** |
| 3 | List projection limited to the public list, other reads stay full, no dose/schedule/pairing/reconstitution/optimization leak | **Fail** (Major) |
| 4 | Stored `Unknown` rows stay `Unknown` until a separate repair, and acceptance does not also require live labels to change on deploy alone | **Fail** (Blocker) |
| 5 | Committed Retatrutide class vs acceptance class | **Fail** (Blocker) |
| 6 | Cold-list acceptance is measurable, or the production transcript is left to the coordinator without an unmeasurable gate | **Fail** (Major) |

Enum and serializer work are implementable. Acceptance of this parcel cannot be proven as written.

## Findings

| ID | Severity | Finding |
| --- | --- | --- |
| F-1 | Blocker | Acceptance criterion 7 requires live Glycine `AminoAcid` and Retatrutide `ResearchCompound` after deploy alone. The same spec says this parcel must not write production and that stored `Unknown` (0) stays `Unknown` until a repair that is not this parcel. Criterion 8 says a pre-repair transcript does not close criterion 7. |
| F-2 | Blocker | Current committed seed classifies Retatrutide as `Peptide`. Acceptance requires `ResearchCompound`. The spec's resolution cites a `HEAD` and an uncommitted working tree that are not the current tree, and it forbids the seed edit that would make them match. The builder is not free to choose, and the named sample still cannot be proven from allowed work. |
| F-3 | Major | The only `GetAllCompoundsAsync` on the production knowledge source is shared. Narrowing it to public columns strips pairing and dose fields that swap evaluation and the protocol analyzer read from that same method. `IKnowledgeSource` is not an allowed file, and the spec does not name a separate query. |
| F-4 | Major | "Cold" and "inside a page load" are not defined. "Under 3 seconds" is an unmeasurable pass/fail line. Assigning the transcript to the coordinator does not say which clock, which cold start, or which response boundary counts. |

No Minor or Nit findings are waived. None were opened. Reconstitution is not a `KnowledgeEntry` column; it is not a separate leak. `Id` is used as a duplicate tie-break and is not a public response field; that is absorbed into F-3's projection note below rather than scored on its own.

### F-1 — Blocker: deploy-alone labels vs no production write

Criterion 7 is a success condition of this parcel:

> After a human production deploy, public Glycine JSON `classification` is `AminoAcid` and public Retatrutide JSON `classification` is `ResearchCompound`, unless a later approved promotion record has changed Retatrutide's reviewed class before that deploy.

The only exception is a later promotion record. A classification repair is not that exception.

These lines say the opposite outcome, and they say this parcel cannot perform the write that would change it:

- Verified fact 4: `KnowledgeEntries.Classification` is an integer. A deploy of a new enum member does not rewrite rows stored as `Unknown` (0).
- Contract: rows stored as `Unknown` stay `Unknown` until a human runs a reviewed classification repair. This parcel must not itself write production.
- Review lifecycle step 6: the repair is not this parcel and is not a Refresh rerun.
- Out of scope: a production row repair.
- Acceptance criterion 8: if deploy happens before that repair, live rows are still `Unknown`, and that transcript does not close criterion 7.

`MapClassification` runs when a seed record is canonicalized and upserted. It does not run on read. `GET /api/v1/knowledge/compounds` and `GET /api/v1/knowledge/compounds/{name}` return the stored enum. After this parcel's code is deployed and before a repair, both named rows still serialize as `Unknown`.

Criterion 7 therefore demands a live label change on deploy alone, and the rest of the spec forbids that write and assigns it to another parcel. Criterion 8 keeps 7 open until that other parcel exists. This parcel's acceptance cannot be closed by the work it allows.

**Spec patch:**

```text
7. This parcel does not require live labels to change on deploy.
   After the human deploy, and before the separate classification repair,
   the coordinator transcript must show Glycine and Retatrutide still
   serializing as Unknown, name the deployed revision, and record that
   the local mapper tests passed. That transcript closes this parcel.
   AminoAcid and ResearchCompound on those live rows are acceptance for
   the repair parcel only.
8. Delete. It currently contradicts 7.
```

### F-2 — Blocker: Retatrutide acceptance does not match the committed seed

The spec does **not** leave the builder free to choose. Criterion 7, verified fact 5, and the forbidden list lock the acceptance class to `ResearchCompound` and say the Peptide value is not the reviewed class.

That lock does not resolve the disagreement on this tree. Verified fact 5 says committed `HEAD:.../substances-seed.json` classifies Retatrutide as Research Compound, and that an uncommitted working tree classifies it as Peptide and adds the alias `triple agonist`. On the tree under review:

- Current `HEAD` is `5b686e69536f2eedf0195ee77466f20b08a490d8`, not the shaping pin `6c9d8eccfd69f848d69c223e547c4d40dbf6ccee`.
- At the pin, Retatrutide's classification is `Research Compound` and the aliases are only `LY3437943`.
- At current `HEAD`, and in the clean working tree, Retatrutide's classification is `Peptide` and the aliases include `triple agonist` (`substances-seed.json` lines 2026–2032). `git status --short` does not show the seed as modified.
- The change is in commit `5b686e69`.
- `go-live-library-2026-10-14-retatrutide` approves promotion and does not name a class (`review-decision-batch-2026-10-14-go-live-library.json` lines 141–164). It does not break the tie.
- Glycine is `Amino Acid` at current `HEAD` (`substances-seed.json` line 37036). That part of fact 6 holds. The Retatrutide half does not.

`MapClassification` already maps the schema string `Peptide` to `CompoundCategory.Peptide` (1). It does not special-case the compound name. This parcel forbids editing `substances-seed.json`. A fresh canonicalization of the committed Retatrutide row therefore persists `Peptide`, not `ResearchCompound`. The allowed tests can still pass by feeding the schema string `Research Compound` as a fixture. They do not prove the named Retatrutide sample.

The builder is forbidden both ways: do not treat Peptide as the reviewed class, and do not edit the seed to Research Compound. Acceptance criterion 7 still requires the live JSON to be `ResearchCompound`. That sample cannot be proven from the committed source this parcel is allowed to read.

**Spec patch:** Replace fact 5 with the current `HEAD` seed text, and pick one normative sentence:

```text
Committed HEAD 5b686e69 classifies Retatrutide as Peptide and includes
the alias "triple agonist". The shaping pin 6c9d8ecc classified it as
Research Compound. The promotion record go-live-library-2026-10-14-retatrutide
names no class. This parcel does not edit the seed and does not special-case
the compound. Its mapper tests use the schema strings "Amino Acid" and
"Research Compound" only. Retatrutide's live class is not acceptance of
this parcel until a later approved promotion record names the class and a
separate repair writes it.
```

Do not keep both "acceptance class is ResearchCompound" and "the class string comes from the committed seed" while the committed seed says Peptide.

### F-3 — Major: the public-list projection is the shared full-catalog read

The spec says add the projection on `DatabaseKnowledgeSource` and use it for `KnowledgeService.GetAllCompoundsAsync` only. It says `GetCompoundAsync`, alias lookup, pathway search, and worker upsert stay on the full entity. Those four paths are separate methods today, and the spec does not tell the builder to change them:

- Single compound and alias lookup load full rows (`DatabaseKnowledgeSource.cs` lines 16–41). The alias miss loads every column via `ToListAsync`.
- Pathway search loads full rows (lines 51–66).
- Upsert writes the full entity (lines 69–91).

The public list uses the same source method those other features use. Production registers one implementation (`Program.cs` line 466):

- `IKnowledgeSource.GetAllCompoundsAsync` returns `List<KnowledgeEntry>` (`IKnowledgeSource.cs` lines 6–8).
- `DatabaseKnowledgeSource.GetAllCompoundsAsync` is `ToListAsync()` on the full entity (lines 44–48).
- `KnowledgeService.GetAllCompoundsAsync` calls that method (lines 22–28).
- `ProtocolParser` caches that same call for 24 hours (`ProtocolParser.cs` lines 80–84). `ProtocolAnalyzerService` then uses `parseResult.KnowledgeByCompound` for evidence comparison (`ProtocolAnalyzerService.cs` lines 99–103 and 127–128). `KnowledgeEntryExposureProfileBuilder` reads `TieredDosing`, `MaxReportedDose`, `Frequency`, `StandardDosageRange`, and `RecommendedDosage` (lines 25–81).
- `InteractionIntelligenceService` loads the swap candidate pool from `GetAllCompoundsAsync` (lines 504–505 and 528–529). Pair metadata uses `PairsWellWith` and `CompatibleBlends` (lines 210–212). `SwapRecommendationEngine.HasExplicitPeerSignal` reads the same fields (lines 418–423).

Acceptance criterion 4 says `GetAllCompoundsAsync` must not materialize dose, schedule, pairing, or optimization columns. Applied to the shared method, that drops the fields the analyzer and swap pool read. `IKnowledgeSource.cs` is not an allowed file, so the builder cannot add a typed interface method. `KnowledgeServicePublicBoundaryTests` mock `IKnowledgeSource` and must stay green without being edited. A cast to `DatabaseKnowledgeSource` with no fallback fails those tests. Changing the shared method keeps those tests green and breaks the analyzer and swaps. No in-scope test calls either caller.

Public JSON leakage is a separate, already-closed path. `KnowledgeEntryResponse` marks dose, schedule, pairing, and optimization members with `JsonIgnore` (lines 18–50). `MapToResponse` also replaces them with empty values (`KnowledgeService.cs` lines 50–69). `Results.Ok` therefore omits them even when the entity was fully loaded. Reconstitution is not a `KnowledgeEntry` property; the canonicalizer does not copy `SubstanceRecord.Reconstitution` onto the row. The projection's job is to stop the list query from reading those columns, not to invent a new JSON omit.

`MapPublicGroupToResponse` orders ties by `entry.Id` (`KnowledgeService.cs` lines 98–101). `Id` is not a `KnowledgeEntryResponse` field. A projection of "public response columns" only would make that tie-break `Guid.Empty`.

**Spec patch:**

```text
Add DatabaseKnowledgeSource.GetPublicCompoundListAsync. It is not
IKnowledgeSource.GetAllCompoundsAsync. KnowledgeService.GetAllCompoundsAsync
calls it only when the injected source is DatabaseKnowledgeSource, and
otherwise keeps the existing interface call so the current mocks stay green.
Do not change DatabaseKnowledgeSource.GetAllCompoundsAsync.
ProtocolParser, InteractionIntelligenceService, and
CounterfactualCandidateService stay on the full-entity method.
The projection selects the public response columns plus Id.
KnowledgeListProjectionTests must assert the command text does not
reference RecommendedDosage, Frequency, PreferredTimeOfDay,
StandardDosageRange, MaxReportedDose, WeeklyDosageSchedule,
IncrementalEscalationSteps, TieredDosing, PairsWellWith, CompatibleBlends,
VialCompatibility, OptimizationProtein, OptimizationCarbs,
OptimizationSupplements, OptimizationSleep, or OptimizationExercise.
```

### F-4 — Major: the cold-list gate cannot be scored

Handoff item 1 says a cold list finishes "inside a normal page load" and points at a 40-second timeout versus a later warm probe of 334 ms (`HANDOFF-2026-10-14.md` lines 50–52). The spec replaces that phrase with acceptance criterion 7:

> A cold `GET /api/v1/knowledge/compounds` returns 158 items in under 3 seconds. Evidence is the HTTP transcript, with the deployed revision named and no secret printed.

The spec does leave the transcript file to the coordinator after a human deploy (objective paragraph, surfaces list, evidence grade, criterion 7, tests item 5). It does not say what that transcript must measure. Missing decisions:

- What is cold: scale-from-zero, a restarted process, the first request after deploy, or any request with an empty client cache? The warm 334 ms probe was explicitly not a cold proof.
- Which clock: client total time, time to first byte, server duration, or the browser page that calls the API?
- Does the budget include DNS, TLS, and the full body (the warm probe was 185,797 bytes)?
- One request or the slower of several?
- The dossier page's 5-second `AbortSignal.timeout` (`frontend/src/app/knowledge/[slug]/page.tsx` lines 23–28) is a single-compound fetch, not this list, and the spec does not adopt it.

"158 items" is checkable. "Cold" and "under 3 seconds" are not, until the clock and the cold condition are written down. A coordinator can record a number and still not know if criterion 7 passed.

**Spec patch:** Either delete the numeric gate and say the coordinator records the observed client total time, item count, and whether the app had scaled to zero, with no pass/fail latency in this parcel, or define the measurement in the criterion itself:

```text
Cold means the first GET after the new revision is deployed and the API
container has scaled to zero. Measure curl --max-time 30 time_total from
outside the container, full body read, one attempt, no retry. Pass only
if time_total is below 3.000 seconds, the JSON array length is the
production row count recorded in the same transcript, and the transcript
names the revision. Do not use the warm 334 ms probe. "Inside a page load"
is not a criterion.
```

## What checked clean

### Enum numbering

`CompoundCategory` is exactly these members (`CompoundCategory.cs` lines 3–16):

| Name | Value |
| --- | --- |
| Unknown | 0 |
| Peptide | 1 |
| Coenzyme | 2 |
| Pharmaceutical | 3 |
| Supplement | 4 |
| Compound | 5 |
| Sarm | 6 |
| Serm | 7 |
| Hormone | 8 |
| Nutraceutical | 9 |
| Other | 10 |

There is no `AminoAcid`, no `ResearchCompound`, no duplicate value, and no gap from 0 through 10. Appending `AminoAcid = 11` and `ResearchCompound = 12` does not collide and does not create a hole the serializer would label as a different existing name. Existing stored integers 0 through 10 keep today's names. This part of the spec matches the tree.

### Serialization

The public field is `KnowledgeEntryResponse.Classification` (`KnowledgeEntryResponse.cs` line 10). It has no property converter and no string column. The store is an `INTEGER` (`20260422125251_RecoverBillingTierEnforcement.cs` line 76; snapshot `Property<int>("Classification")`). `BioStackDbContext` does not apply `HasConversion<string>()` to it, so EF persists the enum as its integer.

The public list and single-compound routes return `Results.Ok` (`KnowledgeEndpoints.cs` lines 26–35). Minimal-API JSON uses `ConfigureHttpJsonOptions`, which adds `new JsonStringEnumConverter()` with no naming policy (`Program.cs` lines 94–100). That converter is what writes the classification field. It is not a second, field-specific converter.

On `net10.0`, `JsonSerializerDefaults.Web` uses camel case for property names. A parameterless `JsonStringEnumConverter` still writes the member name. Local check: `AminoAcid` stays `"AminoAcid"`, `Sarm` stays `"Sarm"`, and a converter constructed with `JsonNamingPolicy.CamelCase` is what produces `"researchCompound"`. The shipped registration is the parameterless converter, so the spec's required tokens `AminoAcid`, `ResearchCompound`, `Sarm`, and `Serm` are what this API emits. The frontend dossier prints `entry.classification` as returned (`frontend/src/app/knowledge/[slug]/page.tsx` lines 64–65). No frontend rename is required for that display.

### Glycine seed

Committed Glycine is schema class `Amino Acid` (`substances-seed.json` line 37036). That agrees with acceptance token `AminoAcid` once a row is actually stored as 11. It does not agree with a live row that is still integer 0. See F-1.

## Spec fix checklist

Do not dispatch a builder until the spec text is changed. This is not an implementation checklist.

1. Rewrite criterion 7 so a deploy without the repair is a successful transcript for this parcel, with live rows still `Unknown`. Remove the demand that Glycine and Retatrutide labels change on deploy alone.
2. Delete or rewrite criterion 8 so it does not both describe that outcome and say it fails criterion 7.
3. Replace verified fact 5 with current `HEAD` `5b686e69`: Retatrutide is committed `Peptide` with alias `triple agonist`. State that the promotion record names no class. Keep the named live class out of this parcel, or name one class and the source that owns it. Do not leave the builder to pick.
4. Name `GetPublicCompoundListAsync` (or equivalent) on `DatabaseKnowledgeSource` only. State that `IKnowledgeSource.GetAllCompoundsAsync` stays a full entity read for the parser, swap pool, and counterfactual pool. Include `Id` in the projection. Require a command-text assertion, not only an empty DTO field.
5. Define the cold-list clock and scale-to-zero condition, or drop the 3-second pass/fail and leave an observational transcript. Do not use "inside a page load" as a silent synonym.

## Commands

`git rev-parse HEAD`

```text
5b686e69536f2eedf0195ee77466f20b08a490d8
```

`git status --short` (seed path produced no line; seed is clean at `HEAD`)

```text
 M docs/INITIATIVES/biostack-go-live/STATUS.md
?? --nologo
?? .memory/
?? docs/INITIATIVES/biostack-go-live/SESSION-2026-10-14-resume-3.md
```

`git show` of `identity.classification` for canonical name Retatrutide:

```text
pin 6c9d8ecc: aliases ["LY3437943"], classification "Research Compound"
HEAD 5b686e69: aliases ["LY3437943", "triple agonist"], classification "Peptide"
HEAD Glycine: classification "Amino Acid"
```

`dotnet run` on a temporary `net10.0` console outside the repo, using `JsonSerializerDefaults.Web` plus `new JsonStringEnumConverter()`:

```text
web-default-converter="AminoAcid"
bare-converter="AminoAcid"
web-camel-converter="researchCompound"
web-sarm="Sarm"
naming=JsonCamelCaseNamingPolicy
```

The temporary project was not written under `D:\Repos\BioStack`.

## Source lines

Spec: `docs/specs/active/BIO-GOLIVE-001-public-classification-and-list-read.md` lines 30–32, 47, 52–59, 64–78, 82, 86, 95–96, 103–118, 122–123, 127–131, 145–149.

Enum and serializer: `backend/src/BioStack.Domain/Enums/CompoundCategory.cs` lines 3–16; `backend/src/BioStack.Contracts/Responses/KnowledgeEntryResponse.cs` lines 7–50; `backend/src/BioStack.Api/Program.cs` lines 94–100 and 466; `backend/src/BioStack.Api/Endpoints/KnowledgeEndpoints.cs` lines 13–35; `backend/src/BioStack.Infrastructure/Persistence/Migrations/20260422125251_RecoverBillingTierEnforcement.cs` lines 69–101; `backend/src/BioStack.Infrastructure/Persistence/BioStackDbContext.cs` lines 402–442 (no string conversion on `Classification`).

Mapper and seed: `backend/src/BioStack.KnowledgeWorker/Pipeline/SubstanceCanonicalizer.cs` lines 35–39 and 97–111; `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json` lines 2022–2032 and 37027–37036; `research/review-decisions/review-decision-batch-2026-10-14-go-live-library.json` lines 141–164.

List and other readers: `backend/src/BioStack.Infrastructure/Knowledge/IKnowledgeSource.cs` lines 5–19; `backend/src/BioStack.Infrastructure/Knowledge/DatabaseKnowledgeSource.cs` lines 16–91; `backend/src/BioStack.Application/Services/KnowledgeService.cs` lines 16–34 and 94–113; `backend/src/BioStack.Application/Services/ProtocolParser.cs` lines 80–84; `backend/src/BioStack.Application/Services/ProtocolAnalyzerService.cs` lines 99–128; `backend/src/BioStack.Application/Evidence/KnowledgeEntryExposureProfileBuilder.cs` lines 25–81; `backend/src/BioStack.Application/Services/InteractionIntelligenceService.cs` lines 210–212, 504–505, and 528–529; `backend/src/BioStack.Application/Services/SwapRecommendationEngine.cs` lines 418–423; `backend/src/BioStack.Application/Services/CounterfactualCandidateService.cs` lines 22 and 49.

Cold-list context: `docs/INITIATIVES/biostack-go-live/HANDOFF-2026-10-14.md` lines 46–52; `frontend/src/app/knowledge/[slug]/page.tsx` lines 21–35 and 64–65; `frontend/src/components/knowledge/CompoundIntelligenceCard.tsx` line 131.
