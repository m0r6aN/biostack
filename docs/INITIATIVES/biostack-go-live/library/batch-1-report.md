# Batch 1 library review report

Date: 2026-10-14  
Reviewer: independent library agent (not a human queue; Clint is not the substance reviewer)  
Source file: `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`

## Summary

| Metric | Count |
|---|---|
| Assigned | 17 |
| Accepted | 8 |
| Narrowed | 9 |
| Could not make valid | 0 |

All 17 assigned objects were edited in place, marked `provenance.reviewStatus=reviewed`, `ops.lastChangeType=agent-review-2026-10-14`, `ops.isActive=true`, `ops.needsReview=false`, `ops.completeness=substantial`, and `provenance.lastReviewedAt=2026-10-14T00:00:00Z`. Launch-seed / draft notes requiring human review were removed.

## Per-id results

| canonicalId | result | evidence tier | regulatory status | primary source relied on |
|---|---|---|---|---|
| ac-262536 | narrowed | Insufficient | not-established | FDA Warns Against Use of SARMs in Body-building Products |
| acarbose | accepted | Strong | FDA-approved prescription product or labeled drug class | ACARBOSE tablet prescribing information (DailyMed search) |
| acp-105 | narrowed | Insufficient | not-established | FDA Warns Against Use of SARMs in Body-building Products |
| afamelanotide | accepted | Strong | FDA-approved prescription product | SCENESSE (afamelanotide) implant label |
| amycretin | narrowed | Limited | not-established | PubMed search: amycretin clinical trial |
| anastrozole | accepted | Strong | FDA-approved prescription product or labeled drug class | ANASTROZOLE tablet prescribing information (DailyMed search) |
| andarine | narrowed | Limited | not-established | FDA Warns Against Use of SARMs in Body-building Products |
| aod-9604 | narrowed | Limited | not-established | FDA Briefing Document, Pharmacy Compounding Advisory Committee (AOD-9604 evaluation) |
| bazedoxifene | accepted | Strong | FDA-approved as combination product component (DUAVEE); standalone US marketing not established | DUAVEE (conjugated estrogens/bazedoxifene) tablet label |
| berberine | narrowed | Moderate | dietary ingredient / not FDA-approved as a drug for disease treatment | PubMed search: berberine diabetes randomized trial review |
| beta-alanine | accepted | Moderate | dietary ingredient / conventional food constituent; not FDA-approved as a drug | PubMed search: beta-alanine supplementation exercise performance review |
| bpc-157 | narrowed | Limited | not-established | Certain Bulk Drug Substances for Use in Compounding that May Present Significant Safety Risks |
| bremelanotide | accepted | Strong | FDA-approved prescription product or labeled drug class | VYLEESI (bremelanotide) injection label (DailyMed search) |
| bupropion | accepted | Strong | FDA-approved prescription product or labeled drug class | BUPROPION hydrochloride prescribing information (DailyMed search) |
| caffeine | accepted | Strong | conventional food constituent and OTC drug ingredient in some products; not a prescription drug for general alertness | Caffeine: Fact Sheet for Health Professionals (NIH ODS) |
| cagrilintide | narrowed | Moderate | not-established | REDEFINE 1 and REDEFINE 2 journal scan (American College of Cardiology) |
| cardarine | narrowed | Insufficient | not-established | WADA issues alert on GW501516 |

## Notes on judgement

- **Accepted** means the substance already had a usable core and was completed/corrected without major claim demotion (labeled drugs, well-supported supplements).
- **Narrowed** means claims, tier, classification, and/or regulatory wording were tightened toward unknown/not-established or class-level risk (unapproved SARMs/peptides, investigational agents, or over-strong seed tiers).
- No doses, schedules, reconstitution steps, sourcing instructions, or personal safety guarantees were added.
- Prescription, investigational, and gray-market status were kept visible where applicable.
- `classification` values of `Other` were replaced with real classes (SARM, Peptide, SERM, Small Molecule, etc.).
- AC-262536 overall tier was demoted from Strong to Insufficient (no human trials).
- Bazedoxifene US approval is scoped to the DUAVEE combination only.
- Cardarine is classified as a PPAR-delta small molecule, not a SARM, with carcinogenicity and WADA/FDA enforcement framing.
- Amycretin and cagrilintide remain investigational (`not-established`); cagrilintide keeps Moderate trial-signal tier without asserting approval.
- JSON array order preserved (99 objects). No assigned object deleted. No object could not be made valid.

## Invalid / blocked

None.
