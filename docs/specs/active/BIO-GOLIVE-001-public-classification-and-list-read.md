---
ticket: BIO-GOLIVE-001
title: Public classification labels and a bounded compound-list read
status: active
owner: clinton.morgan
created: 2026-10-14
updated: 2026-10-14
delivery_classes: [standard, knowledge-promotion]
guidance_classes: []
substance_function_risk: []
function_review_status: not-applicable
surfaces:
  - backend/src/BioStack.Domain/Enums/CompoundCategory.cs
  - backend/src/BioStack.KnowledgeWorker/Pipeline/SubstanceCanonicalizer.cs
  - backend/src/BioStack.Infrastructure/Knowledge/IKnowledgeSource.cs
  - backend/src/BioStack.Infrastructure/Knowledge/DatabaseKnowledgeSource.cs
  - backend/src/BioStack.Infrastructure/Knowledge/LocalKnowledgeSource.cs
  - backend/src/BioStack.Application/Services/KnowledgeService.cs
  - backend/tests/BioStack.KnowledgeWorker.Tests/SubstanceCanonicalizerClassificationTests.cs
  - backend/tests/BioStack.Application.Tests/Services/KnowledgeListProjectionTests.cs
  - backend/tests/BioStack.Application.Tests/Services/KnowledgeServicePublicBoundaryTests.cs
  - backend/tests/BioStack.Application.Tests/Services/TranscriptCandidatePromotionPreviewServiceTests.cs
  - docs/INITIATIVES/biostack-go-live/evidence/BIO-GOLIVE-001-http-transcript.md
routing_class: implementation/knowledge-promotion
verification_class: component-testable
permission_profile: reviewer-readonly
data_classification: public
---

# BIO-GOLIVE-001 — Public classification labels and a bounded compound-list read

## Objective

The public API currently returns `classification: Unknown` for Amino Acid and Research Compound records whose stored integer is 0. Glycine is the named Amino Acid sample. The full list `GET /api/v1/knowledge/compounds` loads every `KnowledgeEntry` column, including ignored dose and schedule fields, before it projects the public response. This parcel adds the two enum members, maps those schema strings on a future canonicalization, and gives the public list its own column projection.

This parcel does not generate catalog entries, does not run Refresh, does not rewrite stored rows, and does not authorize production deploy. A human deploy does not change labels already stored as `Unknown`. Live `AminoAcid` and `ResearchCompound` labels require a later classification-repair parcel. Local tests prove the mapper and the projection. The coordinator transcript after a human deploy records the revision and the still-`Unknown` live labels. That transcript closes this parcel.

## Initiative (Product Lane)

`biostack-go-live`, next-work item 1 in `docs/INITIATIVES/biostack-go-live/HANDOFF-2026-10-14.md`. This parcel does not change `docs/INITIATIVES/biostack-production-readiness/` and does not authorize go-live.

## Branch and Worktree

Proposed only. The coordinator assigns both at Gate 2. Until then this spec stays a review candidate and no builder may implement it.

- Branch: `feat/bio-golive-001-classification-list`
- Worktree: `D:/Repos/BioStack/.worktrees/bio-golive-001-classification-list`

## Dependencies

- The 2026-10-14 library publish already wrote 158 production rows. This parcel must not run Refresh and must not rewrite production rows.
- `docs/specs/INDEX.md` has no approved active spec for this surface. P4 remains a review candidate. That does not block shaping this ticket spec.
- BIO-GOLIVE-002 and BIO-GOLIVE-003 touch different files and may be reviewed in parallel. They must not share a worktree.

## Verified Facts

1. Dispatch tree for this amendment: `5b686e69536f2eedf0195ee77466f20b08a490d8`, which is contained in `origin/main` `9550964256d507d03b789df8f0eb4c448a6f7ac8`. The 2026-10-14 shaping pin `6c9d8eccfd69f848d69c223e547c4d40dbf6ccee` is history. A builder implements from the Gate 2 tree, not from that pin.
2. `CompoundCategory` ends at `Other = 10`. It has no Amino Acid or Research Compound member. `JsonStringEnumConverter` therefore cannot emit those names.
3. `SubstanceCanonicalizer.MapClassification` maps Peptide, Pharmaceutical, Supplement, Hormone, SARM, and SERM. Amino Acid, Research Compound, and every other schema class fall through to `Unknown`.
4. `KnowledgeEntries.Classification` is an integer column (`INTEGER`, migration `20260422125251_RecoverBillingTierEnforcement`). `MapClassification` runs on canonicalization and upsert. It does not run on read. A live API deploy of a new enum member does not rewrite rows already stored as `Unknown` (0).
5. At the dispatch tree, committed `substances-seed.json` classifies Retatrutide as `Peptide` and includes the alias `triple agonist`. At the shaping pin the same row was `Research Compound` with alias `LY3437943` only. The promotion record `go-live-library-2026-10-14-retatrutide` approves promotion and names no class. The Peptide value is unreviewed seed drift. It is not a reviewed class. This parcel does not edit the seed and does not choose a class. A later knowledge-promotion parcel must name Retatrutide's reviewed class before any repair writes one. Until that record exists, no live Retatrutide label is acceptance for this parcel.
6. At the dispatch tree, Glycine's committed classification is `Amino Acid`. Glycine was absent from the seed at the shaping pin. The live Glycine row is still stored as `Unknown`. This parcel does not change that row.
7. `IKnowledgeSource.GetAllCompoundsAsync` is a full-entity read. `DatabaseKnowledgeSource.GetAllCompoundsAsync` calls `ToListAsync()` on the entity. Callers include `InteractionIntelligenceService`, `CounterfactualCandidateService`, `ProtocolParser`, and the swap pool, which reads pairing fields from that result. `KnowledgeService.GetAllCompoundsAsync` uses the same method and then clears dose and schedule fields in memory. `KnowledgeEntryResponse` already marks dose, schedule, pairing, and optimization fields with `JsonIgnore`. Those attributes stay. This parcel adds a separate list method. It does not narrow `GetAllCompoundsAsync`.
8. Resume 3 probe on 2026-10-14: Glycine and Retatrutide API `classification` were `Unknown`. Production OpenAPI and Scalar returned HTTP 200. The earlier warm list of 158 items in 334 ms is not a cold-start measurement. Cold-start latency is outside this parcel.
9. The public dossier page reads `entry.classification` from the API. A page that mentions Amino Acid in other prose does not satisfy a later repair parcel. It is not acceptance for this parcel.

## Surfaces

- `backend/src/BioStack.Domain/Enums/CompoundCategory.cs` — append `AminoAcid = 11` and `ResearchCompound = 12`. Do not renumber 0 through 10.
- `backend/src/BioStack.KnowledgeWorker/Pipeline/SubstanceCanonicalizer.cs` — map the schema strings `Amino Acid` and `Research Compound` to those members. Leave every other unmapped schema class on `Unknown`.
- `backend/src/BioStack.Infrastructure/Knowledge/IKnowledgeSource.cs` — add `ListPublicCompoundsAsync`. Leave `GetAllCompoundsAsync` as the full-entity read.
- `backend/src/BioStack.Infrastructure/Knowledge/DatabaseKnowledgeSource.cs` — implement `ListPublicCompoundsAsync` as a projection that selects `Id` and the public response columns only. Leave `GetAllCompoundsAsync` unchanged.
- `backend/src/BioStack.Infrastructure/Knowledge/LocalKnowledgeSource.cs` — implement `ListPublicCompoundsAsync` with the same public fields. Leave `GetAllCompoundsAsync` unchanged.
- `backend/src/BioStack.Application/Services/KnowledgeService.cs` — `GetAllCompoundsAsync` calls `ListPublicCompoundsAsync` only. No other service method changes.
- Two new test files named in Tests.
- `docs/INITIATIVES/biostack-go-live/evidence/BIO-GOLIVE-001-http-transcript.md` — coordinator-written after the human deploy. The builder does not invent this transcript.

## Contracts

- Public `classification` remains the existing `CompoundCategory` enum serialized by `JsonStringEnumConverter`. Amino Acid serializes as `AminoAcid`. Research Compound serializes as `ResearchCompound`. The frontend already renders the API string. No new public field is added.
- Existing stored integers 0 through 10 keep their current names. New members use unused integers 11 and 12.
- The public list response stays `KnowledgeEntryResponse`. Dose, schedule, pairing, reconstitution, and optimization fields remain absent from the JSON.
- Single-compound `GetCompoundAsync`, alias lookup, pathway search, and worker upsert stay on the full entity. This parcel does not change their contracts.
- Production rows already stored as `Unknown` stay `Unknown` until a later repair parcel. This parcel's code must make a fresh canonicalization of the schema strings `Amino Acid` and `Research Compound` persist as 11 and 12. It must not itself write production. Live labels on existing rows are not acceptance for this parcel.

## Source/License/Provenance

No new knowledge content is authored. The mapper learns the schema strings `Amino Acid` and `Research Compound`. Provenance for the catalog remains the 2026-10-14 promotion batch `research/review-decisions/review-decision-batch-2026-10-14-go-live-library.json`. This parcel does not edit that file. The committed Retatrutide `Peptide` value is not a reviewed class and is not a source for this parcel.

## Evidence Grade

The class label is an identity field, not a new evidence claim. Existing evidence-tier values are unchanged. This parcel's public evidence is a coordinator HTTP transcript that names the deployed revision and shows Glycine and Retatrutide still returning `Unknown`. A local unit test proves the mapper. It does not prove the deployed revision, and it does not prove a repaired live label.

## Review Lifecycle

1. This review-candidate spec is reviewed by two independent reviewers before Gate 2, because the folded delivery classes require two reviewers.
2. Gate 2 names the spec hash, branch, worktree, builder, and an independent implementation reviewer who did not write the fix.
3. The builder implements only the allowed files in the named worktree.
4. The implementation reviewer checks the diff against this spec.
5. After merge, a human deploys the API. The coordinator records the HTTP transcript. The transcript closes this parcel when it names the revision and shows the stored `Unknown` labels unchanged. The library class-label launch gate stays failed until a later repair parcel changes those rows.
6. A separate approved parcel is required before existing production `Unknown` rows can show `AminoAcid` or `ResearchCompound`. That repair is not this parcel and is not a Refresh rerun. Retatrutide's reviewed class must be named by a promotion record before that repair writes a Retatrutide class.

## Promotion Authority

This parcel does not promote knowledge. Promotion authority for the 158 records already exercised on 2026-10-14 stays with that batch. No builder, reviewer, or coordinator session may approve a new public claim or run production Refresh from this parcel.

## Acceptance Criteria

1. A canonicalized seed record whose schema classification is `Amino Acid` persists and serializes as `AminoAcid`, not `Unknown`.
2. A canonicalized seed record whose schema classification is `Research Compound` persists and serializes as `ResearchCompound`, not `Unknown`.
3. Existing enum names for integers 0 through 10 are unchanged. SARM still serializes as `Sarm`. SERM still serializes as `Serm`.
4. `ListPublicCompoundsAsync` selects `Id` and the public response columns. It does not select `RecommendedDosage`, `Frequency`, `PreferredTimeOfDay`, `StandardDosageRange`, `MaxReportedDose`, `WeeklyDosageSchedule`, `IncrementalEscalationSteps`, `TieredDosing`, `PairsWellWith`, `CompatibleBlends`, `VialCompatibility`, or the optimization strings. `GetAllCompoundsAsync` remains a full-entity read for its existing callers.
5. `KnowledgeService.GetAllCompoundsAsync` calls `ListPublicCompoundsAsync` and still returns one item per distinct canonical name. The public JSON still omits the `JsonIgnore` fields. Duplicate canonical names still break ties by `PublicEvidenceCompleteness` descending, then `Id`. The projection includes `Id` and every column that function reads, plus the other public response columns. It still excludes dose, schedule, pairing, and optimization columns.
6. `GetCompoundAsync` still resolves a canonical name and an alias from the full entity.
7. Local tests prove that a fresh canonicalization of `Amino Acid` persists as `AminoAcid` and `Research Compound` persists as `ResearchCompound`. After a human production deploy, the coordinator transcript names the revision and shows public Glycine and Retatrutide `classification` still `Unknown`. That transcript closes this parcel. Live `AminoAcid` and `ResearchCompound` on those rows are acceptance for the later repair parcel only. Cold-start time is recorded if observed and is not a pass or fail for this parcel.

## Tests

1. `SubstanceCanonicalizerClassificationTests` asserts Amino Acid maps to `CompoundCategory.AminoAcid` and Research Compound maps to `CompoundCategory.ResearchCompound`. It also asserts Small Molecule still maps to `Unknown`, so the parcel does not silently widen the enum.
2. `KnowledgeListProjectionTests` asserts that `KnowledgeService.GetAllCompoundsAsync` calls `ListPublicCompoundsAsync`, that the database projection selects `Id` and the public columns and does not reference the dose, schedule, pairing, or optimization properties, and that a stored integer 11 is returned as `AminoAcid`.
3. Existing `KnowledgeServicePublicBoundaryTests` stay green and are not weakened. Their list setups stub `ListPublicCompoundsAsync` instead of `GetAllCompoundsAsync`.
4. `PreviewFakeKnowledgeSource` implements `ListPublicCompoundsAsync` by returning the same entries as its `GetAllCompoundsAsync`. Preview behavior does not change.
5. Focused command: `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests --filter SubstanceCanonicalizerClassificationTests` and `dotnet test backend/tests/BioStack.Application.Tests --filter "FullyQualifiedName~KnowledgeListProjectionTests|FullyQualifiedName~KnowledgeServicePublicBoundaryTests|FullyQualifiedName~TranscriptCandidatePromotionPreviewServiceTests"`.
6. The post-deploy HTTP transcript is coordinator evidence. The builder does not call production.

## Rollback

Revert the parcel commit. Enum values 11 and 12 disappear from new writes. Rows already stored as 11 or 12, if a later human repair created them, read back as the numeric value until a forward fix returns. This parcel itself writes no production rows, so reverting the code returns the public mapper to today's `Unknown` behavior for those schema strings. No database down-migration is authorized.

## Out of Scope

- Production Refresh, seed edits, trust-gate edits, and any change to the committed Retatrutide row.
- Adding Small Molecule, Biologic, Vitamin, Mineral, or any class other than Amino Acid and Research Compound.
- Removing `JsonIgnore` from dose, schedule, or pairing fields.
- Frontend copy changes. The dossier already prints `entry.classification`.
- A production row repair. That needs its own approved spec, a dry run, and a human operator.
- OpenAPI, rate limits, lead capture, and checkout. Those are BIO-GOLIVE-002 and BIO-GOLIVE-003.

## Allowed Files

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

Coordinator-owned: this spec, `docs/specs/INDEX.md`, the HTTP transcript, and every go-live status file.

## Forbidden

- Any edit to `substances-seed.json`, `TrustGate.cs`, review-decision JSON, or `JsonIgnore` attributes on `KnowledgeEntryResponse`.
- Any production connection string, Refresh invocation, Container App deploy, or live database write.
- Renumbering existing `CompoundCategory` values.
- Treating the committed Retatrutide Peptide value as the reviewed class.
- Marking the library gate passed without the HTTP transcript.
