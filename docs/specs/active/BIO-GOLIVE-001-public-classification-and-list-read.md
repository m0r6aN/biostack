---
ticket: BIO-GOLIVE-001
title: Public classification labels and a bounded compound-list read
status: review-candidate
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
  - backend/src/BioStack.Infrastructure/Knowledge/DatabaseKnowledgeSource.cs
  - backend/src/BioStack.Application/Services/KnowledgeService.cs
  - backend/tests/BioStack.KnowledgeWorker.Tests/SubstanceCanonicalizerClassificationTests.cs
  - backend/tests/BioStack.Application.Tests/Services/KnowledgeListProjectionTests.cs
  - docs/INITIATIVES/biostack-go-live/evidence/BIO-GOLIVE-001-http-transcript.md
routing_class: implementation/knowledge-promotion
verification_class: component-testable
permission_profile: reviewer-readonly
data_classification: public
---

# BIO-GOLIVE-001 — Public classification labels and a bounded compound-list read

## Objective

The public API currently returns `classification: Unknown` for reviewed Amino Acid and Research Compound records. Glycine and Retatrutide are the named acceptance samples. The full list `GET /api/v1/knowledge/compounds` also loads every `KnowledgeEntry` column, including ignored dose and schedule fields, before it projects the public response. This parcel makes the reviewed class visible on the public JSON and makes the list read project only the public columns.

This parcel does not generate catalog entries, does not run Refresh, and does not authorize production deploy. Deploy and any classification rewrite of existing production rows remain human production actions. Local tests are necessary. The launch acceptance evidence is an HTTP transcript after the human deploy.

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

1. Repository pin at shaping: `dev@6c9d8eccfd69f848d69c223e547c4d40dbf6ccee`. Uncommitted seed, trust-gate, and go-live files already exist. This spec does not authorize editing them.
2. `CompoundCategory` ends at `Other = 10`. It has no Amino Acid or Research Compound member. `JsonStringEnumConverter` therefore cannot emit those names.
3. `SubstanceCanonicalizer.MapClassification` maps Peptide, Pharmaceutical, Supplement, Hormone, SARM, and SERM. Amino Acid, Research Compound, and every other schema class fall through to `Unknown`.
4. `KnowledgeEntries.Classification` is an integer column (`INTEGER`, migration `20260422125251_RecoverBillingTierEnforcement`). A live API deploy of a new enum member does not rewrite rows already stored as `Unknown` (0).
5. Committed seed `HEAD:backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json` classifies Retatrutide as Research Compound. The uncommitted working tree classifies Retatrutide as Peptide and adds the alias `triple agonist`. The reviewed promotion record `go-live-library-2026-10-14-retatrutide` does not name a class. The public acceptance class for Retatrutide is Research Compound until a later approved knowledge-promotion parcel records a different reviewed class. The working-tree Peptide value is not the acceptance class.
6. Glycine is Amino Acid in both the committed seed and the working tree.
7. `DatabaseKnowledgeSource.GetAllCompoundsAsync` calls `ToListAsync()` on the full entity. `KnowledgeService.GetAllCompoundsAsync` then groups by canonical name and maps the public response, clearing dose and schedule fields in memory. `KnowledgeEntryResponse` already marks dose, schedule, pairing, and optimization fields with `JsonIgnore`. Those attributes stay.
8. Resume probe `SESSION-2026-10-14-resume.md`: warm list returned HTTP 200, 158 items, 185,797 bytes, in 334 ms. Glycine and Retatrutide `classification` were `Unknown`. `https://biostack.cc/knowledge/glycine` contained the words Amino Acid. `https://biostack.cc/knowledge/retatrutide` contained Unknown. The earlier 40-second list timeout is a cold-start observation, not a closed latency proof.
9. The public dossier page reads `entry.classification` from the API. A page that mentions Amino Acid in other prose does not satisfy this parcel.

## Surfaces

- `backend/src/BioStack.Domain/Enums/CompoundCategory.cs` — append `AminoAcid = 11` and `ResearchCompound = 12`. Do not renumber 0 through 10.
- `backend/src/BioStack.KnowledgeWorker/Pipeline/SubstanceCanonicalizer.cs` — map the schema strings `Amino Acid` and `Research Compound` to those members. Leave every other unmapped schema class on `Unknown`.
- `backend/src/BioStack.Infrastructure/Knowledge/DatabaseKnowledgeSource.cs` — add a list projection that selects only public response columns.
- `backend/src/BioStack.Application/Services/KnowledgeService.cs` — use that projection for `GetAllCompoundsAsync` only.
- Two new test files named in Tests.
- `docs/INITIATIVES/biostack-go-live/evidence/BIO-GOLIVE-001-http-transcript.md` — coordinator-written after the human deploy. The builder does not invent this transcript.

## Contracts

- Public `classification` remains the existing `CompoundCategory` enum serialized by `JsonStringEnumConverter`. Amino Acid serializes as `AminoAcid`. Research Compound serializes as `ResearchCompound`. The frontend already renders the API string. No new public field is added.
- Existing stored integers 0 through 10 keep their current names. New members use unused integers 11 and 12.
- The public list response stays `KnowledgeEntryResponse`. Dose, schedule, pairing, reconstitution, and optimization fields remain absent from the JSON.
- Single-compound `GetCompoundAsync`, alias lookup, pathway search, and worker upsert stay on the full entity. This parcel does not change their contracts.
- Production rows already stored as `Unknown` stay `Unknown` until a human runs a reviewed classification repair. This parcel's code must make a fresh canonicalization of Amino Acid and Research Compound persist as 11 and 12. It must not itself write production.

## Source/License/Provenance

No new knowledge content is authored. The class strings come from the already published seed schema values `Amino Acid` and `Research Compound`. Provenance for the catalog remains the 2026-10-14 promotion batch `research/review-decisions/review-decision-batch-2026-10-14-go-live-library.json`. This parcel does not edit that file and does not treat the working-tree seed diff as a new source.

## Evidence Grade

The class label is an identity field, not a new evidence claim. Existing evidence-tier values are unchanged. Acceptance of the public label requires an HTTP transcript, not an agent assurance. A local unit test proves the mapper. It does not prove the deployed revision.

## Review Lifecycle

1. This review-candidate spec is reviewed by two independent reviewers before Gate 2, because the folded delivery classes require two reviewers.
2. Gate 2 names the spec hash, branch, worktree, builder, and an independent implementation reviewer who did not write the fix.
3. The builder implements only the allowed files in the named worktree.
4. The implementation reviewer checks the diff against this spec.
5. After merge, a human deploys the API. The coordinator records the HTTP transcript. Until that transcript exists, the library class-label gate stays failed.
6. A separate human action is required before existing production `Unknown` rows can show the new labels: a reviewed classification repair of those rows. That repair is not this parcel and is not a Refresh rerun.

## Promotion Authority

This parcel does not promote knowledge. Promotion authority for the 158 records already exercised on 2026-10-14 stays with that batch. No builder, reviewer, or coordinator session may approve a new public claim or run production Refresh from this parcel.

## Acceptance Criteria

1. A canonicalized seed record whose schema classification is `Amino Acid` persists and serializes as `AminoAcid`, not `Unknown`.
2. A canonicalized seed record whose schema classification is `Research Compound` persists and serializes as `ResearchCompound`, not `Unknown`.
3. Existing enum names for integers 0 through 10 are unchanged. SARM still serializes as `Sarm`. SERM still serializes as `Serm`.
4. `GetAllCompoundsAsync` does not materialize `RecommendedDosage`, `Frequency`, `PreferredTimeOfDay`, `StandardDosageRange`, `MaxReportedDose`, `WeeklyDosageSchedule`, `IncrementalEscalationSteps`, `TieredDosing`, `PairsWellWith`, `CompatibleBlends`, `VialCompatibility`, or the optimization strings.
5. The public list still returns one item per distinct canonical name and still omits the `JsonIgnore` fields.
6. `GetCompoundAsync` still resolves a canonical name and an alias.
7. After a human production deploy, public Glycine JSON `classification` is `AminoAcid` and public Retatrutide JSON `classification` is `ResearchCompound`, unless a later approved promotion record has changed Retatrutide's reviewed class before that deploy. A cold `GET /api/v1/knowledge/compounds` returns 158 items in under 3 seconds. Evidence is the HTTP transcript, with the deployed revision named and no secret printed.
8. If the human deploy happens before the classification repair, the transcript must say the code mapping passed in tests and the live rows are still `Unknown`. That result does not close acceptance criterion 7.

## Tests

1. `SubstanceCanonicalizerClassificationTests` asserts Amino Acid maps to `CompoundCategory.AminoAcid` and Research Compound maps to `CompoundCategory.ResearchCompound`. It also asserts Small Molecule still maps to `Unknown`, so the parcel does not silently widen the enum.
2. `KnowledgeListProjectionTests` asserts the list query selects only public columns and that a stored Amino Acid row is returned as `AminoAcid`.
3. Existing `KnowledgeServicePublicBoundaryTests` stay green and are not weakened.
4. Focused command: `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests --filter SubstanceCanonicalizerClassificationTests` and `dotnet test backend/tests/BioStack.Application.Tests --filter KnowledgeListProjectionTests`.
5. The post-deploy HTTP transcript is coordinator evidence. The builder does not call production.

## Rollback

Revert the parcel commit. Enum values 11 and 12 disappear from new writes. Rows already stored as 11 or 12, if a later human repair created them, read back as the numeric value until a forward fix returns. This parcel itself writes no production rows, so reverting the code returns the public mapper to today's `Unknown` behavior for those schema strings. No database down-migration is authorized.

## Out of Scope

- Production Refresh, seed edits, trust-gate edits, and the uncommitted go-live files.
- Adding Small Molecule, Biologic, Vitamin, Mineral, or any class other than Amino Acid and Research Compound.
- Removing `JsonIgnore` from dose, schedule, or pairing fields.
- Frontend copy changes. The dossier already prints `entry.classification`.
- A production row repair. That needs its own approved spec, a dry run, and a human operator.
- OpenAPI, rate limits, lead capture, and checkout. Those are BIO-GOLIVE-002 and BIO-GOLIVE-003.

## Allowed Files

- `backend/src/BioStack.Domain/Enums/CompoundCategory.cs`
- `backend/src/BioStack.KnowledgeWorker/Pipeline/SubstanceCanonicalizer.cs`
- `backend/src/BioStack.Infrastructure/Knowledge/DatabaseKnowledgeSource.cs`
- `backend/src/BioStack.Application/Services/KnowledgeService.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/SubstanceCanonicalizerClassificationTests.cs`
- `backend/tests/BioStack.Application.Tests/Services/KnowledgeListProjectionTests.cs`

Coordinator-owned: this spec, `docs/specs/INDEX.md`, the HTTP transcript, and every go-live status file.

## Forbidden

- Any edit to `substances-seed.json`, `TrustGate.cs`, review-decision JSON, or `JsonIgnore` attributes on `KnowledgeEntryResponse`.
- Any production connection string, Refresh invocation, Container App deploy, or live database write.
- Renumbering existing `CompoundCategory` values.
- Treating the working-tree Retatrutide Peptide class as the reviewed class.
- Marking the library gate passed without the HTTP transcript.
