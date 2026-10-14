# Batch 4 library review report

Date: 2026-10-14  
Reviewer: independent BioStack library agent (not a human queue; Clint is not the substance reviewer)  
Seed file: `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`

## Summary

| Metric | Count |
|---|---|
| Assigned canonical IDs | 17 |
| Accepted | 9 |
| Narrowed | 8 |
| Could not make valid | 0 |

All 17 objects were edited in place (no reorder, no extra objects). `provenance.reviewStatus` is `reviewed`, `ops.isActive` is true, `ops.needsReview` is false, `ops.completeness` is `substantial`, `ops.lastChangeType` is `agent-review-2026-10-14`, and `provenance.lastReviewedAt` is `2026-10-14T00:00:00Z`.

## Per-ID results

| canonicalId | result | evidence tier | regulatory status | primary source relied on |
|---|---|---|---|---|
| ll-37 | narrowed | Limited | not-established | FDA bulk compounding safety risks page (Cathelicidin LL-37) |
| magnesium | accepted | Moderate | dietary-supplement-ingredient | NIH ODS Magnesium Health Professional Fact Sheet |
| maridebart-cafraglutide | narrowed | Moderate | investigational | NEJM/PubMed Phase 2 trial PMID 40549887 |
| mazdutide | narrowed | Moderate | not-established | PubMed Mazdutide First Approval + PMC phase 2 T2D trial |
| mecasermin | accepted | Strong | FDA-approved | DailyMed INCRELEX (mecasermin) label |
| melanotan-ii | narrowed | Limited | not-established | FDA unapproved-drug context + existing seed safety framing |
| melatonin | accepted | Moderate | dietary-supplement | NIH NCCIH Melatonin: What You Need To Know |
| metformin | accepted | Strong | FDA-approved | DailyMed Metformin prescribing information |
| modafinil | accepted | Strong | FDA-approved | DailyMed Provigil/modafinil label search |
| mots-c | narrowed | Insufficient | not-established | FDA PCAC briefing / withdrawn bulk nomination for MOTS-c |
| nad | narrowed | Limited | not-established | PubChem NAD + FDA dietary supplement consumer information |
| nandrolone-decanoate | narrowed | Limited | not-established | DEA scheduling class reference + PubChem nandrolone decanoate |
| omega-3-ethyl-esters | accepted | Strong | FDA-approved | DailyMed Lovaza/omega-3-acid ethyl esters label search |
| ospemifene | accepted | Strong | FDA-approved | DailyMed OSPHENA (ospemifene) label |
| ostarine | narrowed | Limited | not-established | FDA consumer update on SARM dangers |
| oxandrolone | accepted | Strong | FDA-approved | DailyMed oxandrolone label search |
| oxytocin | accepted | Strong | FDA-approved | DailyMed PITOCIN (oxytocin) label |

## Narrowing notes

- **ll-37**: overallTier Strong → Limited; filled purpose, risks, interactions; FDA compounding withdrawal and thin human evidence kept visible.
- **maridebart-cafraglutide**: filled from Phase 2 packet; regulatory status set to investigational (no US approval asserted).
- **mazdutide**: China NMPA disclosures noted only as foreign-jurisdiction facts; US status remains not-established; gray-market access flagged.
- **melanotan-ii**: unapproved/research status and injection/identity risks made explicit; evidence remains Limited.
- **mots-c**: overallTier Strong → Insufficient; no human exposure data for administered product per FDA materials.
- **nad**: longevity/IV clinic claims kept Limited; product-quality and lack of anti-aging approval stated.
- **nandrolone-decanoate**: controlled-substance/androgen class risks filled; modern US product approval not asserted.
- **ostarine**: unapproved SARM status and FDA hepatic-risk warning retained; not treated as a legal supplement.

## Invalid objects

None. All assigned objects remain valid JSON objects inside the seed array.
