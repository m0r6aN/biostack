# Batch 5 report

Date: 2026-10-14
Reviewer: independent library review agent (not Clint)
Seed: `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`

## Summary

- Assigned: 17
- Accepted: 6
- Narrowed: 11
- Could not make valid: 0

All 17 assigned `canonicalId` values were edited in place. No objects outside the batch-5 index were intentionally changed. Array order was preserved. JSON remains a single valid array.

## Per-id results

| canonicalId | judgement | evidenceTier | regulatoryStatus | primary source title |
|---|---|---|---|---|
| peg-mgf | narrowed | Insufficient | not-established | Certain Bulk Drug Substances for Use in Compounding that May Present Significant Safety Risks (PEG-MGF) |
| pemvidutide | narrowed | Moderate | not-established | Effect of pemvidutide, a GLP-1/glucagon dual receptor agonist, on MASLD: A randomized, double-blind, placebo-controlled study |
| pramlintide | accepted | Strong | FDA-approved (historical Symlin labeling; check current marketing status) | Label: Symlinpen - pramlintide acetate injection |
| rad-150 | narrowed | Insufficient | not-established | Bodybuilding Products: SARMs Cause Harm |
| raloxifene | accepted | Strong | FDA-approved prescription product (Evista) | EVISTA (raloxifene hydrochloride) tablet - Label |
| retatrutide | narrowed | Moderate | not-established | Triple-Hormone-Receptor Agonist Retatrutide for Obesity - A Phase 2 Trial |
| s-23 | narrowed | Insufficient | not-established | FDA warns about the use of selective androgen receptor modulators (SARMs) |
| selank | narrowed | Limited | not-established | Certain Bulk Drug Substances for Use in Compounding that May Present Significant Safety Risks |
| semaglutide | accepted | Strong | FDA-approved prescription product (Ozempic/Wegovy/Rybelsus product-specific labels) | DailyMed OZEMPIC (semaglutide) injection, solution |
| semax | narrowed | Limited | not-established | Certain Bulk Drug Substances for Use in Compounding that May Present Significant Safety Risks (Semax nomination withdrawn) |
| sermorelin | narrowed | Limited | not-established | Determination That GEREF (Sermorelin Acetate) Injection, 0.5 Milligrams Base/Vial and 1.0 Milligrams Base/Vial, and GEREF (Sermorelin Acetate) Injection, 0.05 Milligrams Base/Amp, Were Not Withdrawn From Sale for Reasons of Safety or Effectiveness |
| setmelanotide | accepted | Strong | FDA-approved prescription product (Imcivree) | Label: IMCIVREE - setmelanotide solution |
| sildenafil | accepted | Strong | FDA-approved prescription product | DailyMed search and product labels for sildenafil (Viagra/Revatio and generics) |
| somatropin | accepted | Strong | FDA-approved prescription biologic/drug (product-specific somatropin labels) | Label: GENOTROPIN- somatropin kit |
| spermidine | narrowed | Limited | not-established | PubChem Compound Summary for Spermidine |
| stenabolic | narrowed | Insufficient | not-established | Regulation of circadian behaviour and metabolism by synthetic REV-ERB agonists |
| survodutide | narrowed | Moderate | not-established | A Phase 2 Randomized Trial of Survodutide in MASH and Fibrosis |

## Notes by judgement

### Accepted (FDA-labeled products with strong primary-label evidence)
- **pramlintide** (Peptide): tier Strong; FDA-approved (historical Symlin labeling; check current marketing status).
- **raloxifene** (SERM): tier Strong; FDA-approved prescription product (Evista).
- **semaglutide** (Peptide): tier Strong; FDA-approved prescription product (Ozempic/Wegovy/Rybelsus product-specific labels).
- **setmelanotide** (Peptide): tier Strong; FDA-approved prescription product (Imcivree).
- **sildenafil** (Pharmaceutical): tier Strong; FDA-approved prescription product.
- **somatropin** (Hormone): tier Strong; FDA-approved prescription biologic/drug (product-specific somatropin labels).

### Narrowed (claims limited to verified scope; unknown/not-established retained where appropriate)
- **peg-mgf** (Peptide): tier Insufficient; not-established.
- **pemvidutide** (Peptide): tier Moderate; not-established.
- **rad-150** (SARM): tier Insufficient; not-established.
- **retatrutide** (Peptide): tier Moderate; not-established.
- **s-23** (SARM): tier Insufficient; not-established.
- **selank** (Peptide): tier Limited; not-established.
- **semax** (Peptide): tier Limited; not-established.
- **sermorelin** (Peptide): tier Limited; not-established.
- **spermidine** (Small Molecule): tier Limited; not-established.
- **stenabolic** (Research Compound): tier Insufficient; not-established.
- **survodutide** (Peptide): tier Moderate; not-established.

## Standard compliance applied to every row

- Classification set to a real class (not `Other`).
- At least one indication (studied/labeled/commonly discussed; not a personal recommendation).
- `evidence.overallTier` in Strong | Moderate | Limited | Insufficient | Unknown.
- At least one contraindication/warning/adverse risk statement.
- At least one interaction or explicit class-risk interaction statement.
- At least one evidence gap or controversy retained.
- At least one public `provenance.sourceRecords` item with title, publisher, and URL (FDA/DailyMed/PubMed/PubChem/DrugBank/NCBI preferred).
- `provenance.lastReviewedAt` = `2026-10-14T00:00:00Z`; `reviewStatus` = `reviewed`.
- Regulatory status and jurisdiction filled; `not-established` used when approval could not be verified.
- `ops.isActive=true`, `needsReview=false`, `completeness=substantial`, `lastChangeType=agent-review-2026-10-14`.
- Removed launch-seed / human-review-required curation notes.
- No doses, schedules, reconstitution steps, sourcing instructions, or safety guarantees added as recommendations.

## Incomplete items

None. Every assigned object was made qualifying-standard valid JSON.
