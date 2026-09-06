# Wave006 Vitamin D3 Independent Evidence Review

Review date: 2026-09-05. Reviewer: OpenCode, independent evidence reviewer, not packet author or delegated final approver.

## Decision

**NOT PROMOTABLE. Keep `ops.needsReview = true` and completeness partial. No claim approval is issued.**

All **seven expected claims** are present and were covered by this review. The coordinator corrected the task prompt's count from eight to seven; there is no missing-input or claim-count promotion blocker. The seven verdicts are six partial/unresolved and one contradicted/unresolved. These verdicts distinguish scientific plausibility and direct-source fidelity from clearance under the independent-family and authority contract.

Principal blockers:

- **High, factual contradiction:** the fracture/falls claim says kidney-stone risk was not mentioned in the 2018 USPSTF statement. The fetched 2018 FINAL explicitly describes the harm as small and supplies effect estimates.
- **High, approval provenance:** a manufacturer-submitted dietary-supplement SPL is not an FDA-approved drug label. Its presence on DailyMed, directions, and absence of an Indications section cannot establish drug approval status across cholecalciferol products or a predominant U.S. market classification.
- **High, independent-review failure:** NCI and TCTMD accounts of VITAL, and the primary paper fetched here, trace to the same trial/publication. Hosting or publisher changes do not supply independent clinical corroboration.
- **High, safety gate:** the RDA/UL/serum-threshold and toxicity statements lack complete, independently verified, claim-specific A1/A2 support in this review. ODS returned 403. A journal narrative review cannot be relabeled A1/A2 to clear them.
- **Coverage/currentness:** the consolidated USPSTF page fetched here is still explicitly a December 2024 DRAFT, while the fracture page is explicitly the April 2018 FINAL. Attempts to retrieve the separate current falls recommendation failed. This does not establish that no newer final exists anywhere, or that all 2018 falls wording remains the current final position.

## Scope And Contract

Worktree: `D:\Repos\BioStack\.worktrees\gtm-wave006-20260905`.

Packet: `research/input/evidence/vitamin-d3.evidence.json`, packetId `vitamin-d3-evidence-expansion-001`, generatedAt `2026-08-29T04:46:26Z`, 463 lines on inspection.

Instructions inspected: parent repository `D:\Repos\BioStack\AGENTS.md`; `research/README.md`, especially lines 33-45; `research/directives/04-preprocess-compile-review.md`, especially lines 19-34; and `research/review-decisions/OPERATOR-DELEGATION-2026-08-28.md`, especially lines 11-16. No additional AGENTS.md or CLAUDE.md was found inside this worktree by the instruction-file search.

The delegation changes WHO approves, not HOW: an independent reviewer provides evidence to the lead, cannot self-authorize publication, and cannot waive hard authority or source-authorization blockers. This Markdown receipt is not a schema-valid review-decision batch and does not implement approval.

Public fetch budget: **13 attempts**, approximately the requested 12, including a bounded failed expansion for current falls guidance. Eight yielded substantive content; five yielded access failures or a cookie challenge. Retrieved excerpts are identified below by URL and section. No inference of source access is made from a successful transport response containing only a challenge. No worker was launched; the runtime-configured `Worker:ResearchReviewSourceExpansionLimit` was not verified or asserted exhausted. Further review remains gated at this task's fetch boundary.

Only this receipt was manually written, using apply_patch. No packet edits, source-registry edits, production operations, secret reads, or git commands were performed. No compiler, tests, or schema-validation commands were run; this is an evidence review, not a compiler/runtime or complete source-authorization audit. No additional model council or author-chain delegation was launched, and no multi-reviewer consensus is claimed.

## Fetched Evidence Ledger

All fetch attempts below occurred during this review on 2026-09-05. Quoted excerpts are short literal passages from the returned content, not quotations reconstructed from the packet, except where normalization is explicitly identified. Numeric summaries outside quotations are reviewer transcriptions. F-number order groups sources by use rather than request completion order.

### F1: Product SPL, Accessible

URL: https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=ef102aff-a775-4f99-b90d-53507e24a558

Identity/status fields: "Packager: Nationwide Pharmaceutical LLC"; "Category: DIETARY SUPPLEMENT"; "Marketing Status: Dietary Supplement"; "Updated December 5, 2025". The marketing table has a dietary-supplement category, no displayed application number, and a 12/01/2025 marketing start date. Archive shows one current version published Dec 5, 2025.

Directions: "Adults 18 years or older: 25 mcg (1 tablet) daily preferably with a meal or as directed by your doctor." And: "Persons under 18 years of age: Consult your doctor."

Warning: "Do not exceed recommended dosage. Consult your physician before taking this product if you have vascular disease (such as heart disease or history of stroke) or diabetes, are pregnant or nursing, taking medication, facing surgery, have bleeding problems, or undergoing any treatment which may affect the ability of blood to clot."

Family/provenance: product-specific label supplied by the labeler, hosted in a structured-label database. This is the packet's original source; direct fidelity only, not independent corroboration or an approval receipt.

### F2: FDA Supplement Regulation, Accessible

URL: https://www.fda.gov/consumers/consumer-updates/fda-101-dietary-supplements

Section "How Are Dietary Supplements Regulated?": "The FDA does NOT have the authority to approve dietary supplements for safety and effectiveness, or to approve their labeling, before the supplements are sold to the public."

Section "What Are Dietary Supplements?": "Generally, to the extent a product is intended to treat, diagnose, cure, or prevent diseases, it is a drug, even if it is labeled as a dietary supplement."

Page lists "Content current as of: 06/02/2022". It explicitly lists vitamin D among common supplements. This is a directly fetched regulator explanation, not a 2026 approval search or evidence of a 2026 rulemaking outcome.

Family/provenance: regulator policy explanation, independently authored from the labeler, but still within the broad regulator/label family. It corroborates the legal distinction, not this SKU's exhaustive approval history or market prevalence. It does not satisfy the materially different-family requirement for the entire regulatory claim.

### F3: USPSTF 2018 FINAL, Accessible

URL: https://www.uspreventiveservicestaskforce.org/uspstf/recommendation/vitamin-d-calcium-or-combined-supplementation-for-the-primary-prevention-of-fractures-in-adults-preventive-medication

Heading: "Final Recommendation Statement"; date "April 17, 2018" (date whitespace collapsed). Banner: "This topic is being updated. Please use the link(s) below to see the latest documents available."

Recommendation summary: "The USPSTF recommends against daily supplementation with 400 IU or less of vitamin D and 1000 mg or less of calcium for the primary prevention of fractures in community-dwelling, postmenopausal women."

Harms of Preventive Medication: "The USPSTF found adequate evidence that supplementation with vitamin D and calcium increases the incidence of kidney stones. The USPSTF assessed the magnitude of this harm as small."

Potential Harms: "For every 273 women who received supplementation over a 7-year follow-up period, 1 woman was diagnosed with a urinary tract stone."

The detailed harms analysis reports combined vitamin D/calcium pooled RR 1.18 (95% CI 1.04 to 1.35), ARD 0.33% (95% CI 0.06% to 0.60%). The summary also includes an I statement for postmenopausal women at doses greater than 400 IU vitamin D and greater than 1000 mg calcium, omitted from the packet's summary of 2018 grades.

Biological understanding: "Vitamin D controls calcium absorption in the small intestines, interacts with parathyroid hormone to help maintain calcium homeostasis between the blood and bones, and is essential for bone growth and maintaining bone density."

Family/provenance: guideline, identical to a per-claim sourceRef for the fracture/falls claim; it can directly falsify a misquotation without being independent corroboration. For mechanism, it is a different document but the same broad guidance family as ODS.

### F4: USPSTF Consolidated DRAFT, Accessible

Requested URL: https://www.uspreventiveservicestaskforce.org/uspstf/recommendation/vitamin-d-calcium-combined-supplementation-primary-prevention-falls-fractures-communitydwelling-adults

Returned page identifies itself as "Draft Recommendation Statement", "in progress", and "December 17, 2024". Its share/permalink identifies the draft path:

https://www.uspreventiveservicestaskforce.org/uspstf/draft-recommendation/vitamin-d-calcium-combined-supplementation-primary-prevention-falls-fractures-communitydwelling-adults

Update section: "When final, this draft recommendation will replace the 2018 USPSTF recommendation on vitamin D, calcium, or combined supplementation for the primary prevention of fractures in community-dwelling adults."

Recommendation summary: "The USPSTF recommends against supplementation with vitamin D with or without calcium for the primary prevention of fractures in community-dwelling postmenopausal women and men age 60 years or older."

Also: "The USPSTF recommends against supplementation with vitamin D for the prevention of falls in community-dwelling postmenopausal women and men age 60 years or older."

Harms excerpt: "2 more participants with kidney stones per 1,000 persons treated". Its surrounding pooled estimate is RR 1.11 (95% CI 1.03 to 1.21), 10 RCTs, 99,036 participants, 2.5 to 7 years. This differs from the 2018 synthesis; it is not the first mention of stones.

The references also identify a separate 2024 USPSTF falls-intervention recommendation in JAMA 332(1):51-57. That reference is a discovery lead, not a fetched current final or proof of its vitamin D wording.

Family/provenance: original guideline sourceRef, same organization/topic as F3. No independent-family credit and no elevation from draft to final merely because the requested URL contains `/recommendation/`.

### F5: Endocrine Society 2024 Guideline, Accessible

URL: https://www.endocrine.org/clinical-practice-guidelines/vitamin-d-for-prevention-of-disease

Resource date: June 3, 2024. Scope: "Updates and replaces the 2011 Evaluation, Treatment, and Prevention of Vitamin D Deficiency guideline" and focuses on people without established indications for vitamin D treatment or testing.

Recommendation 4 technical remark: "Adults in this age group should follow the Recommended Daily Allowance established by the IOM". Its parenthetical values are 600 IU (15 mcg) daily ages 50-70 and 800 IU (20 mcg) daily older than 70. Recommendation 2 likewise gives 600 IU (15 mcg) daily for adults younger than 50. These values are numeric transcriptions; the source uses microgram symbols.

Recommendation 12: "In healthy adults, we suggest against routine screening for 25(OH)D levels."

Technical remark: "In healthy adults, 25(OH)D levels that provide outcome-specific benefits have not been established in clinical trials."

Recommendation 1: "In children and adolescents ages 1-18 years, we suggest empiric vitamin D supplementation to prevent nutritional rickets and potentially lower the risk of respiratory tract infections."

Recommendation 6: "In the general population ages 75 years and older, we suggest empiric vitamin D supplementation because of the potential to lower the risk of mortality."

Family/provenance: independent professional-society authorship, still within the guidance family, and the RDA explicitly derives from IOM, as does ODS. F5 provides independently authored appraisal and confirmation of the stated adult RDA values and age boundary. Shared IOM ancestry does not disqualify that appraisal, and no independent re-derivation of the official RDA is required or claimed. This is not replication or confirmation of the full dose claim's UL and serum thresholds. The age/outcome-specific recommendations challenge unqualified general-wellness/immune extrapolations; they do not prove COVID efficacy or depression treatment efficacy.

### F6: VITAL Primary Publication, Accessible Through PMC

URL: https://pmc.ncbi.nlm.nih.gov/articles/PMC6425757/

Title: "Vitamin D Supplements and Prevention of Cancer and Cardiovascular Disease". DOI 10.1056/NEJMoa1809944; PMID 30415629; issue date 2019 Jan 3; online publication shown as 2018 Nov 10. This is the publication already identified in the packet's TCTMD source metadata.

Methods: "There were 25,871 U.S. men aged" followed by the displayed greater-than-or-equal age criteria, men 50 and women 55. Literal protocol excerpt: "cholecalciferol, 2000 IU/day".

Primary endpoint definition: "Primary endpoints were total invasive cancer and major cardiovascular events (composite of myocardial infarction, stroke, and cardiovascular mortality)."

Eligibility: "Eligible participants had no history of cancer (except non-melanoma skin cancer) or cardiovascular disease at study entry". Baseline table nevertheless includes diabetes and treated hypertension, so "initially healthy" must not mean free of all disease.

Results: "Vitamin D supplementation did not reduce either of the primary endpoints." Median intervention was 5.3 years. Invasive cancers: 793 versus 824, HR 0.96 (95% CI 0.88-1.06). Major cardiovascular events: 396 versus 409, HR 0.97 (95% CI 0.85-1.12). Table 2 confirms nonsignificant full-follow-up MI, stroke, cardiovascular mortality, and all-cause mortality results.

Discussion: "However, these analyses should be considered hypothesis generating, in the context of the negative findings for the primary outcome measures and given that they are not adjusted for multiple comparisons."

Cancer-mortality sensitivity analysis: "In an analysis excluding 1 and 2 years of follow-up, and that was not specified in the protocol, cancer mortality was significantly reduced in both". Table 2 gives HR 0.75 (95% CI 0.59-0.96) after excluding the first two years. This is not a positive primary endpoint or confirmatory efficacy result, but forbids describing every secondary/exploratory analysis as null.

The 40% serum rise is reported in a repeat-measurement subset of 1,644 participants at one year, not demonstrated for every randomized participant over the entire trial. Cancer deaths were 154/12,927 versus 187/12,944; the first is approximately 1.19%, conventionally 1.2% to one decimal, not the packet's 1.1%. Direct fidelity of the NCI quotation itself was not checked because NCI was not fetched.

Family/provenance: controlled human trial primary report, a stronger direct-fidelity anchor than media. **Not independent corroboration:** PMC is a mirror of the same NEJM publication, and both existing summaries derive from that publication. PubMed, PMC, NEJM, TCTMD, and NCI must not be counted as five independent evidence units for this result.

### F7: Toxicity Clinical Review, Accessible

URL: https://www.frontiersin.org/journals/endocrinology/articles/10.3389/fendo.2018.00550/full

Title: Vitamin D Toxicity-A Clinical Perspective (title punctuation normalized); Marcinowska-Suchowierska et al.; September 20, 2018; DOI 10.3389/fendo.2018.00550. The page labels this a REVIEW article, not a systematic review or guideline.

Abstract excerpt: "concentrations higher than 150 ng/ml (375 nmol/l) are the hallmark of VDT due to vitamin D overdosing."

Definition section: "In healthy individuals, exogenous VDT is usually caused by prolonged use (months) of vitamin D mega doses, but not by the abnormally high exposure of skin to the sun or by eating a diversified diet."

Chronic-intake section: "The IOM cited several association studies that suggest possible deleterious effects of serum 25(OH)D concentrations above 50 ng/ml." Also: "the IOM-recommended UL of 4,000 IU/day."

Introduction: "Vitamin D is an important prohormone that plays a vital role in maintaining healthy bones and calcium levels. Vitamin D deficiency leads to hypocalcemia and defects in bone mineralization."

Diagnosis section: "Endogenous active metabolite intoxication due to coexisting granulomatous diseases or lymphoma may be characterized by suppressed PTH (intact), decreased or normal 25(OH)D concentration, and elevated 1,25(OH)2D."

Family/provenance: distinct authored journal narrative synthesis, not a StatPearls mirror, but not paper-level primary research, systematic review, or A1/A2 guidance. Its UL and lower caution discussion explicitly share IOM ancestry with ODS. It supplies a useful independent-document clinical challenge and partial support, not automatic clearance of a materially different qualifying family or safety authority. It also discusses falls/fractures with bolus dosing; that does not independently reproduce USPSTF recommendation grades.

### F8: CIDRAP COVID Report, Accessible

URL: https://www.cidrap.umn.edu/covid-19/vitamin-d-supplements-don-t-cut-covid-health-care-use-symptom-severity-trial-shows

Population/design excerpt: "A total of 1,747 index patients with a new COVID-19 diagnosis were cluster-randomized with 277 household contacts". Adults were in the United States and Mongolia, median index-patient age 38. Trial regimen: 9,600 IU/day for two days, then 3,200 IU/day for a month, not a general supplement-use recommendation.

Primary outcome: "The primary outcome was at least one health care visit (including hospitalization) or death within a month among index patients."

Results excerpt: "Per-protocol analyses, however, suggested a nonsignificant trend toward a lower prevalence of long COVID at two months (OR, 0.78)."

Senior-author quotation: "While we didn't find that high-dose vitamin D reduced COVID severity or hospitalizations, we observed a promising signal for long COVID that merits additional research." The fetched page uses typographic apostrophes; this sentence is a punctuation-normalized transcription, not an exact-byte quote.

The reported 863 versus 884 group sizes and nonsignificant health-care-use result agree with the packet. The page links the underlying trial to https://www.sciencedirect.com/science/article/abs/pii/S0022316626000477 . That linked paper was **not fetched**, so neither it nor the linked news release counts as verified evidence. The sourceId's `2025` suffix is not proof of publication year; the linked identifier contains 2026, warranting bibliographic reconciliation rather than an invented exact publication date.

Family/provenance: identical per-claim media sourceRef; direct text fidelity, no independent clinical confirmation.

### F9-F13: Access Failures

| ID | Exact attempted URL | Observed result and consequence |
|---|---|---|
| F9 | https://ods.od.nih.gov/factsheets/VitaminD-HealthProfessional/ | HTTP 403. No ODS text or current tables verified. Affects mechanism, reference values, depression, and toxicity attribution. |
| F10 | https://pubmed.ncbi.nlm.nih.gov/30415629/ | Only "Cookies must be enabled" and "Enable cookies for pubmed.ncbi.nlm.nih.gov and reload this page to continue." No abstract obtained here. F6 later restores primary-publication access, not independent corroboration. |
| F11 | https://www.uspreventiveservicestaskforce.org/uspstf/recommendation/falls-prevention-in-older-adults-interventions | HTTP 404. No current final falls text verified. |
| F12 | https://www.uspreventiveservicestaskforce.org/uspstf/recommendation/falls-prevention-in-older-adults-interventions-june-2024 | HTTP 404. No current final falls text verified. |
| F13 | https://www.uspreventiveservicestaskforce.org/uspstf/recommendation/falls-prevention-in-community-dwelling-older-adults-interventions | HTTP 404. No current final falls text verified. |

The three guessed falls routes returning 404 are failed discovery attempts, **not evidence that the recommendation does not exist**. The live F4 reference to the 2024 publication remains an unresolved lead. No CAPTCHA bypass, authentication, or paid source was used.

## Per-Claim Verdicts

### 1. vitamind3-regulatory-supplement-status-001

Packet lines 142-175; regulatory; `fieldAuthorityRequired: true`.

**PARTIAL / UNRESOLVED.** F1 confirms this SKU's supplement coding, strength, directions, and warning text. F2 confirms that dietary-supplement labels are not premarket FDA-approved. The statement's "predominantly" claim is not established by one SKU; "under DailyMed structured product labeling" incorrectly risks presenting a hosting system as the legal marketing pathway. Adults have explicit label directions, not physician-only dosing. Lack of an Indications section is not an exhaustive approval-status search.

Original family: product/regulator-label. Additional check: independently authored FDA legal explanation, still the same broad family. No materially different-family, SKU-specific verification or approved-product database reconciliation was obtained. The packet's DSLD cross-check and March 2026 FDA meeting story have no inspectable fetched receipt here and do not establish prevalence or approval.

Blockers: narrow classification to the identified product; obtain product-specific approval provenance and any cross-product scope evidence; retain A1/A2 and expert/legal review gate. Do not convert manufacturer warning text into an FDA-approved warning or a class-wide contraindication.

### 2. vitamind3-mechanism-calcium-bone-001

Packet lines 178-203; mechanism; `fieldAuthorityRequired: false`.

**PARTIAL / UNRESOLVED.** F3 directly supports intestinal calcium absorption and calcium/bone homeostasis. F7 independently discusses the prohormone, calcium, and mineralization. Nothing fetched contradicts the mechanism, but this review did not independently verify the full serum-phosphate/growth/remodeling formulation or the exact ODS extraction (F9 failed).

Original families: professional guidance (ODS) and structured database (PubChem). F3 is another guidance document; F7 is a distinct narrative clinical review, not an independently inspected mechanistic primary study or a systematic synthesis. Do not promote partial document-level agreement to full qualifying-family confirmation.

Blocker: obtain a distinct mechanistic primary publication or qualifying independent synthesis covering calcium AND phosphate homeostasis, and verify original extraction. No need to turn physiological function into a claim of clinical fracture-prevention efficacy.

### 3. vitamind3-dose-context-rda-ul-001

Packet lines 205-233; dose-context; `fieldAuthorityRequired: true`.

**PARTIAL / UNRESOLVED.** F5 confirms the stated adult RDA values and age boundary. F7 repeats the 4,000 IU UL and a lower caution discussion above 50 ng/mL, separately from overdose toxicity. Both trace reference values to IOM. No fetched source verifies the complete ODS table, especially deficiency below 12 ng/mL; ODS itself failed.

The packet's only extractedEvidence sentence explains mcg/IU conversion, not its RDA, UL, or serum cutoffs. Thus the complete numerical statement is not supported by its quoted extraction. The reference threshold must remain an attributed population reference, not an individualized diagnostic threshold, routine-testing instruction, treatment target, or dose recommendation. F5 explicitly cautions that outcome-specific beneficial serum thresholds have not been established in healthy adults.

Original family: professional guidance. F5 is an independently authored professional-society appraisal within the same broad family, with shared IOM ancestry; F7 is a distinct review that also cites IOM for these values. An independently authored appraisal may share underlying study or authority ancestry. The review must preserve that ancestry and distinguish appraisal from a mirror or mere recap, but does not require a new RDA derivation or claim independent replication. Harvard was not fetched here and receives no verification credit.

Blockers: verify complete authoritative tables and unit/age context, especially the UL and serum thresholds, and repair the original extraction's incomplete numerical support; obtain qualifying independent appraisal for the unresolved portions or retain explicit unknown/review status; maintain the safety authority gate. The RDA values and age boundary are corroborated by F5; a separate derivation is not a promotion condition. A claim of no revisions through August 2026 was not independently established by these dated pages.

### 4. vitamind3-efficacy-vital-cancer-cvd-negative-001

Packet lines 235-285; efficacy; `fieldAuthorityRequired: true`.

**PARTIAL / UNRESOLVED; primary-study substance substantially confirmed.** F6 verifies the negative co-primary endpoints, the listed full-follow-up secondary cardiovascular and all-cause mortality results, and the hypothesis-generating BMI framing. Specify men >=50, women >=55, 25,871 participants, 2,000 IU/day D3, median 5.3 years, invasive cancer, and the defined major-CV composite. Do not extrapolate to all adults, deficiency treatment, all cancers/clinical endpoints, or all later VITAL ancillary publications.

F6 also identifies the significant but post hoc latency-excluded cancer-mortality analysis. The packet's broad null language should remain confined to overall prespecified/full-follow-up analyses, not all secondary/exploratory analyses. The 40% serum-rise context and 1.1% cancer-death percentage need reconciliation as described in F6.

Original sources: TCTMD media summary and NCI government science communication, both VITAL-derived. F6 is an upgraded direct source, not a new underlying publication. **Reject the packet's `RESOLVED` narrative that the NCI retelling supplies independent corroboration merely because it is governmental.** F5 offers independent guideline context, not confirmation of VITAL's exact endpoints/BMI estimates. TCTMD and NCI were not fetched here, so their exact quoted text remains unchecked.

Blockers: reconcile extraction scope, quote numerics, and primary-paper provenance; obtain genuinely different-family independent appraisal with transparent shared-study ancestry; retain the existing field-authority gate rather than automatically upgrading a media source to A2 because it is on a government site.

### 5. vitamind3-efficacy-fracture-falls-uspstf-negative-001

Packet lines 287-346; efficacy with guideline/dose/harms content; `fieldAuthorityRequired: true`.

**CONTRADICTED / UNRESOLVED.** F3 unequivocally contradicts "kidney-stone risk not mentioned in the 2018 statement." It was explicit in the FINAL recommendation's rationale, clinical considerations, and evidence discussion. The newer draft has a different pooled estimate/population, not a newly discovered category of harm.

F3 and F4 confirm the particular pages' final-versus-draft status on retrieval. Do not claim the consolidated draft is final. Conversely, do not use its draft status to assert without qualification that the separate 2018 falls recommendation is still the complete current final guidance. F4 itself references a 2024 falls recommendation, and F11-F13 failed to obtain it. Add the omitted 2018 higher-dose I statement if representing the full historical recommendation. Preserve asymptomatic, community-dwelling population restrictions and exclusions for osteoporosis, diagnosed deficiency, and related conditions; do not generalize to treatment or institutional settings.

Original family: USPSTF guidelines. F3/F4 are original-source checks, not independent corroboration. F5 is separately authored professional guidance but the same broad family and has different age/outcome recommendations. F7 supplies a distinct narrative review of dosing-related harms, not independent proof of USPSTF grades or a qualifying new primary-study check.

Blockers: correct the demonstrably false 2018-harms comparison and stale `RESOLVED` metadata; retrieve the separate current FINAL falls statement and compare its scope; obtain qualifying independent clinical evidence for the efficacy synthesis. No authority waiver based on a draft carrying packet tier A1.

### 6. vitamind3-evidence-gap-immune-covid-marketing-001

Packet lines 348-387; evidence-gap; `fieldAuthorityRequired: false`.

**PARTIAL / UNRESOLVED.** F8 supports the particular trial report's negative health-care-use/severity framing and nonconfirmatory long-COVID signal. It reports a newly infected adult trial and household-contact assessment, not proof about every OTC dose, all immune outcomes, depression treatment, or every general-wellness claim. F9 failed, so the ODS depression extraction was not verified. No independent depression study or COVID primary publication was fetched within this budget.

Original families: ODS guidance and CIDRAP science-media summary. F8 is the same source. F5 is independently authored guidance but shares ODS's broad family; its pediatric respiratory-infection and older-adult mortality recommendations demonstrate why age/outcome-specific uncertainty must not become a universal no-benefit claim. They are not evidence of established adult "immune boosting" or mood lifting. The packet's assertions about prevalence of marketing/use were not separately substantiated.

Blockers: obtain separate controlled-study/systematic-review evidence for depression and COVID, narrow populations/endpoints, and separate marketing prevalence from clinical evidence. Do not count the CIDRAP-linked trial or press release as independently fetched. Keep the long-COVID signal exploratory, not established benefit.

### 7. vitamind3-adverse-effect-hypercalcemia-toxicity-001

Packet lines 389-438; adverse-effect containing dose-threshold and warning content; `fieldAuthorityRequired: true`.

**PARTIAL / UNRESOLVED.** F7 supports distinguishing the lower potential-adverse-effect context near 50 ng/mL (125 nmol/L) from levels above 150 ng/mL (375 nmol/L) characteristic of exogenous overdose toxicity. F1 verifies the precise product warning. These are serum concentrations, not intake doses; 4,000 IU/day is an intake UL, not a toxicity diagnosis or a daily requirement. Conversion consistency: 50 x 2.5 = 125 and 150 x 2.5 = 375.

F7 also prevents treating 150 ng/mL as a universal diagnostic boundary or a guarantee of safety below it: endogenous active-metabolite disorders and hypersensitivity can produce hypercalcemia with normal/lower 25(OH)D. Its lower-threshold discussion is based on associations and explicitly discusses uncertainty; do not turn "potential" into established harm in every person. The packet's `context.doseText` label "frank-toxicity threshold" needs the same clinical qualification as its better-hedged statement.

Original families: ODS guidance, product label, and StatPearls clinical reference. F7 is a different authored journal review, not an independent primary toxicity dataset or A1/A2 guideline; it shares IOM ancestry for the UL/caution values. StatPearls was not fetched, and ODS failed, so their literal extractions and current authority remain unverified. The product's bleeding/medication consultation text does not independently establish a D3-anticoagulant interaction or class-wide contraindication.

Blockers: verify authoritative threshold context and original quotes; supply independent qualifying evidence and A1/A2 support for safety-bearing subclaims or keep them unknown/review-gated. Never promote the quoted StatPearls "maximum suggested daily requirement" wording as an RDA or prescription. Keep label statements product-specific and retain authority sign-off.

## Safety And Promotion Conditions

For `regulatory`, `approved-indication`, `dose-context`, `formulation`, `storage-reconstitution`, `contraindication`, `warning`, `monitoring`, and `interaction`, **authorityRequired remains true in review policy: claim-specific A1/A2 support plus independent confirmation, or explicit unknown/review gating**. The packet calls the flag `fieldAuthorityRequired`; this receipt does not rename or mutate it. These obligations also apply when safety language is embedded in efficacy/adverse-effect claims or context fields. No relabeling to a softer claimType, source-host prestige, draft tier, or compound-wide authority inheritance clears them.

Five present claims already set `fieldAuthorityRequired: true`; mechanism and evidence-gap set false. This receipt changes none of those flags. No new approved-indication, storage, reconstitution, interaction, contraindication, or monitoring claim is approved by implication from the label or clinical review.

Before a later promotion decision, the lead needs: correction of the 2018-harms contradiction; current-final falls provenance; narrow regulatory/SKU wording and approval evidence; source-fidelity repairs and accessible authority for dose/toxicity; and genuine independent-family review without double-counting mirrored or summarized publications as independent replication. Independently authored appraisal may share underlying ancestry, which must remain explicit; independent re-derivation of official RDA values is not required. Source authorization, schema validation, and any runtime publication gates remain separately required and unassessed here.

**Final reviewer disposition: carry all unresolved items forward. This packet is not cleared for promotion or runtime ingestion.**
