# Batch 6 library review report

Date: 2026-10-14
Reviewer: independent library review agent (not Clint)
Seed file: `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`

## Summary

| Metric | Count |
|---|---|
| Assigned | 14 |
| Accepted | 7 |
| Narrowed | 7 |
| Could not make valid | 0 |

All 14 canonical IDs were edited in place, marked `provenance.reviewStatus = reviewed`, `ops.lastChangeType = agent-review-2026-10-14`, `ops.isActive = true`, `ops.needsReview = false`, and `ops.completeness = substantial`. Launch-seed / human-review-required curation notes were removed from assigned objects.

## Per-object results

| canonicalId | disposition | evidence tier | regulatory status | primary source relied on |
|---|---|---|---|---|
| tadalafil | accepted | Strong | FDA-approved prescription product | DailyMed tadalafil prescribing information |
| tamoxifen | accepted | Strong | FDA-approved prescription product | DailyMed tamoxifen citrate label |
| tb-500 | narrowed | Limited | not-established | WADA Prohibited List (thymosin-β4 / TB-500) |
| tesamorelin | accepted | Strong | FDA-approved prescription product | DailyMed EGRIFTA SV tesamorelin label |
| testolone | narrowed | Limited | not-established | FDA Warrior Labz SARM warning letter (RAD-140) |
| testosterone-cypionate | accepted | Strong | FDA-approved prescription product (CIII controlled substance) | DailyMed DEPO-TESTOSTERONE label |
| thymosin-alpha-1 | narrowed | Limited | not-established | FDA Thymosin Alpha-1 bulk drug substance evaluation |
| tirzepatide | accepted | Strong | FDA-approved prescription product | DailyMed Mounjaro/Zepbound tirzepatide labels |
| toremifene | accepted | Strong | FDA-approved prescription product | DailyMed FARESTON (toremifene) label |
| urolithin-a | narrowed | Moderate | not-established | JAMA Network Open urolithin A older-adults RCT |
| vitamin-d3 | accepted | Strong | dietary supplement / OTC vitamin; some high-strength products dispensed as prescription | NIH ODS Vitamin D Fact Sheet for Health Professionals |
| vk2735 | narrowed | Moderate | not-established | Obesity journal / PubMed VENTURE Phase 2 (VK2735) |
| vosilasarm | narrowed | Limited | not-established | FDA Warrior Labz warning letter + PubChem vosilasarm |
| yk-11 | narrowed | Insufficient | not-established | FDA SARM harm page + NIH LiverTox SARMs |

## Notable judgement calls

- **toremifene**: classification corrected from `Other` to `SERM`; PCT/bodybuilding claims left as insufficient/not-indicated; QT boxed warning emphasized.
- **vk2735**: classification corrected from `Other` to `Peptide`; investigational dual GIP/GLP-1 agonist; gray-market sales treated as unverified/fraud risk.
- **vosilasarm**: classification corrected from `Other` to `SARM`; aliases linked to RAD-140/testolone; consumer anabolic claims narrowed. Same molecule also retained under `testolone` for common-name search.
- **testolone / yk-11 / tb-500**: consumer performance claims narrowed; regulator harm and uncertain-identity risks made explicit.
- **thymosin-alpha-1**: U.S. not-approved status preserved; TESTS negative sepsis mortality result and ETASS discordance retained as uncertainty.
- **urolithin-a**: Moderate tier kept for RCT biomarker endpoints; lifespan/anti-aging marketing narrowed.
- **vitamin-d3**: Strong for bone/calcium physiology via NIH ODS; broad CV/cancer prevention hype not upgraded.

## Invalid objects

None. All 14 assigned objects meet the qualifying standard after edit.