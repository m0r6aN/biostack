# Wave 006 Review Disposition Brief

## Context

BioStack publishes source-graded educational compound dossiers, not prescriptions. This branch is review-only. Clint alone merges, deploys, writes production data, and handles secrets/infra, Stripe production, legal and money. All nine safety-critical claimTypes retain A1/A2 authority or explicit gating. Independent review must distinguish source fidelity, independent appraisal and independent experimental replication.

## Inventory And Verified Observations

- Base: origin/main 4d8754c670a7d4553ade857be80aa170ede85653. PR #264 merged at 339f259; main deployment run 33315286760 succeeded. Latest main deployment run 33743439471 succeeded for 4d8754c.
- Anonymous production passkey status GET returned enabled:true. Magic-link verification and enrollment write production records and were not executed.
- Production GETs for Toremifene, Lasofoxifene, LL-37, AC-262536, LGD-3303 returned 404 via frontend proxy AND direct API. Raloxifene returned 200 with one source reference.
- Creatine packet says safety side-effect rates were 13.7% versus 13.2%. PMID 40198156 abstract labels these proportions of studies and separately gives participant frequencies 4.60% versus 4.21%.
- Tamoxifen packet labels 18.6% versus 21.1%, RR 0.87 as cumulative breast-cancer mortality. Primary ATLAS text labels 639 versus 722 overall deaths; its years 5-14 breast-cancer mortality is 12.2% versus 15.0%.
- Vitamin D3 packet says kidney-stone risk was not mentioned in the 2018 USPSTF statement. The April 17, 2018 FINAL explicitly states supplementation with vitamin D and calcium increases kidney-stone incidence.
- Semax contraindication claim has fieldAuthorityRequired:true and only B1 insert support. No packet edits are proposed.
- PCAC article source has three live:false links but still renders summaries. It says no agency reviewed efficacy and that list consideration says nothing about effectiveness. FDA July agenda says the chart identifies uses FDA reviewed; 21 CFR 216.23(c)(3) includes available effectiveness evidence among evaluation criteria.
- Four independent fresh-context native-agent receipts cover 23 claims fully within bounded retrieval budgets, plus blocker-focused review of three PCAC packets. Some sources returned 403/404/challenges. No full-corpus review, authenticated production flow, completed refresh, or medical/legal clearance is claimed.

## Your Task

Independently judge the proposed review-only disposition: six request-changes decisions, no promotion, no packet/seed/route/authority edits, and a remediation/operator handoff. Identify any unsafe inference, overclaim, missing acceptance criterion or unnecessary blocker. Legal wording corrections are referred to Clint, not silently published. Do not perform additional web research or modify files. Use only this brief; do not read other reviewers or their verdicts.

## Output Contract

Exactly TOP 3 FINDINGS (numbered one-line findings), then TOP 3 MOVES (numbered implementation-ready actions). Maximum 300 words. Distinguish verified facts from uncertainty; model agreement is not biomedical evidence.
