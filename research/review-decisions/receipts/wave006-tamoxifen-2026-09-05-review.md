# Wave006 Tamoxifen Independent Evidence Review

Date: 2026-09-05. Reviewer: OpenCode, independent evidence reviewer, not packet author or delegated final approver.

Worktree: `D:\Repos\BioStack\.worktrees\gtm-wave006-20260905`.
Input: `research/input/evidence/tamoxifen.evidence.json`, packet `tamoxifen-evidence-expansion-001`, all eight claims.

## Disposition

**Do not promote this packet.** Two claims are substantively supported, two remain partially verified/unresolved, and four need correction. These are evidence-review verdicts, not schema-valid approval decisions. Keep `ops.needsReview = true`. The delegated lead must adjudicate corrected evidence and unresolved gaps before any approval; expert review remains appropriate for the safety language.

| Claim ID | Evidence verdict | Immediate promotion blocker |
| --- | --- | --- |
| tamoxifen-approved-indication-001 | Needs correction | Missing age/sex/menopausal and treatment-setting limits; tablet evidence does not establish every Soltamox-specific indication |
| tamoxifen-warning-boxed-001 | Supported, with provenance qualification | Preserve DCIS/high-risk scope and distinct incidence-data windows; lead safety adjudication still required |
| tamoxifen-contraindication-001 | Partially verified; unresolved | Explicit DCIS scope and complete independent corroboration of the indication-specific coumarin restriction |
| tamoxifen-mechanism-serm-001 | Supported | No substantive blocker for this narrow mechanism statement; not evidence of human efficacy or universal estrogen antagonism |
| tamoxifen-dose-context-001 | Needs correction | Not all label trials used 20 mg/day for five years; historical adjuvant context must not imply a current universal duration |
| tamoxifen-efficacy-nsabp-p1-001 | Partially verified; unresolved | Primary abstract supports numbers, but primary full-text/table verification and exact label/publication discrepancy reconciliation remain open |
| tamoxifen-efficacy-atlas-extended-001 | Needs correction | All-cause mortality mislabeled; cumulative versus crude measures and safety denominators mixed |
| tamoxifen-evidence-gap-bodybuilding-pct-001 | Needs correction | Product-testing paper mislabeled as human survey; adverse events are cited prior forum research, not its clinical results |

## Controls and Method

Read `D:\Repos\BioStack\AGENTS.md`; no worktree-local or nested `AGENTS.md`/`CLAUDE.md` was returned by the instruction-file searches. Read `research/directives/04-preprocess-compile-review.md`, `research/directives/03-category-evidence-agent.md`, and `research/review-decisions/OPERATOR-DELEGATION-2026-08-28.md`.

- The delegation names Claude as lead/final judge; it does not transfer that role to this reviewer or waive author/reviewer separation and cross-source verification.
- Claim types, field-authority requirements, evidence tiers and confidence values were not changed. Safety-critical content retains the A1/A2-or-explicit-unknown/review gate. A trial attribution, an `efficacy`/`evidence-gap` container, or the packet's previous ATLAS "recalibrated" flag is not permission to bypass that gate.
- A1 label backing and materially different-family confirmation are separate requirements. A different URL, publisher wrapper, or republication of the same label is not independent evidence. PMC hosting does not turn a paper into government guidance. Europe PMC hosting does not turn the original abstract into an independent study.
- ASCO guidelines provide professional-society synthesis beyond a label or trial-summary page, but their descriptions of P-1 and ATLAS still derive from those same trials. They are independent review families, not independent patient cohorts or replications.
- Twelve external fetch attempts were used, including two blocked/unusable attempts. Expansion proceeded from label/primary sources to professional synthesis, a clinical review, and alternate abstract retrieval. The runtime `Worker:ResearchReviewSourceExpansionLimit` was not inspected or claimed exhausted; the user-set approximately 12-fetch budget bounds this review. Unclosed items remain unresolved.
- Fetched HTML/text and returned tables were inspected, with targeted reads/searches of tool-captured text where long responses were truncated. No primary PDF was verified. No other agents' review receipts were read. No additional council was launched within this single independent-review assignment.
- Only this receipt was manually written, using `apply_patch`. No packet edits, shell commands, tests, git mutations, secret reads, or production writes were performed. This is a source-evidence review, not a compiler/runtime validation or current-treatment recommendation.

## Fetch Ledger

All attempts below were made on 2026-09-05. Source keys in each claim refer to these exact fetched URLs, not merely suggested links.

| Key | URL actually requested | Access and family assessment |
| --- | --- | --- |
| S1 | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=8f642753-9e12-433c-a0bc-ab33dac41ddf | Label text retrieved. Andrx tablet label, updated April 20, 2007; inactivated NDC notice. Original packet source, A1; not a fresh independent family or proof of current Soltamox labeling. |
| S2 | https://pmc.ncbi.nlm.nih.gov/articles/PMC3596060/ | ATLAS primary Lancet full-text HTML and tables retrieved. Materially different from Wiki Journal Club summary. Page lists 2013 and 2017 corrections; separate correction notices were not fetched. |
| S3 | https://pubmed.ncbi.nlm.nih.gov/9747868/ | Unusable cookie challenge: "Cookies must be enabled". No paper evidence obtained here. |
| S4 | https://pmc.ncbi.nlm.nih.gov/articles/PMC2716943/ | ASCO 2009 breast-cancer risk-reduction guideline, full-text HTML/tables. Professional-society guideline/synthesis, A2-type backing, not a label republication. Historical guidance, not represented as the latest guideline. |
| S5 | https://www.ncbi.nlm.nih.gov/books/NBK532905/ | StatPearls Tamoxifen, updated March 28, 2025; professional clinical reference with literature citations. Distinct from label/database sources but not automatically A1/A2 because NCBI hosts it. |
| S6 | https://academic.oup.com/jnci/article/90/18/1371/897928 | HTTP 403. Primary P-1 full text/PDF unresolved. |
| S7 | https://pmc.ncbi.nlm.nih.gov/articles/PMC6517163/ | Anawalt 2019 JCEM clinical review, full-text HTML. Independent clinical-review family versus the packet's product-testing paper; not an Endocrine Society practice guideline merely because published in its journal. |
| S8 | https://www.frontiersin.org/journals/chemistry/articles/10.3389/fchem.2025.1536858/full | Original product-testing full text retrieved, published March 19, 2025. Original packet source; rereading is not independent confirmation. |
| S9 | https://www.ebi.ac.uk/europepmc/webservices/rest/search?query=EXT_ID:9747868%20AND%20SRC:MED&format=json&resultType=core | Fisher 1998 primary abstract/metadata retrieved. Same primary-paper lineage as S6, not another family; API reports no full text/PDF in Europe PMC. |
| S10 | https://pmc.ncbi.nlm.nih.gov/articles/PMC4876310/ | ASCO 2014 focused adjuvant endocrine guideline, full-text HTML/tables. Independent professional synthesis/A2-type backing versus Wiki Journal Club; same ATLAS data where it cites ATLAS. |
| S11 | https://www.cancer.gov/about-cancer/treatment/drugs/tamoxifencitrate | NCI educational drug entry, updated January 2, 2025. Corroborates general approved-use/mechanism scope; links to DailyMed, not independently dispositive for exact regulatory wording. |
| S12 | https://www.ebi.ac.uk/europepmc/webservices/rest/search?query=DOI:10.1016/j.therap.2022.03.004&format=json&resultType=core | Rochoy 2022 forum-analysis primary abstract retrieved. Underlying adverse-event source already cited by S8, not independent replication of S8's quoted observations. |

Excerpts below are short verbatim text fragments from the returned content, with whitespace normalized. Numerical reconciliations outside quotation marks are reviewer analysis, not invented source quotations.

## Claim Reviews

### 1. tamoxifen-approved-indication-001

**Verdict: needs correction.** Original family: regulator/label S1. Independent checks: professional reference S5 and ASCO guidance S4/S10; NCI S11 is supplemental educational corroboration.

Exact excerpts:

- S1, Indications and Usage: "node-positive breast cancer in postmenopausal women".
- S1, high-risk definition: "women at least 35 years of age".
- S11, Use in Cancer: "as adjuvant therapy in women whose cancer was treated with surgery and radiation therapy".
- S11, DCIS: "to decrease the chance of invasive breast cancer".

The metastatic indication includes women and men, but that must not make all subsequent indications gender-neutral. The cited tablet label specifies postmenopausal women for node-positive adjuvant treatment; node-negative adjuvant use is in women, with stated surgery/axillary-dissection/irradiation context. DCIS use after surgery and radiation is risk reduction for subsequent invasive breast cancer, not an unqualified assertion of treating existing DCIS. High risk in this label requires age at least 35 AND five-year Gail risk at least 1.67%; the packet omits age.

S4 supports prevention scope but uses 1.66% in its guidance/trial discussion. Do not silently replace the label's 1.67% threshold with that different source's convention. S5 and S11 support the broad categories but do not establish every missing product-specific qualifier. S1 is a tablet label; the context also naming oral solution/Soltamox needs its own exact product-label check.

**Promotion blockers:** restore exact population and indication purpose/setting; separate product formulations as needed; replace the packet's compressed, nonverbatim "quote" with an exact anchor. A1 backing exists but does not support the statement at its present breadth. No authority recalibration is allowed.

### 2. tamoxifen-warning-boxed-001

**Verdict: substantively supported, with provenance qualification.** Original family: S1 regulator/label. Independent family: S4 professional risk-reduction guideline; S5 independently authored clinical reference. S9 additionally supports the adverse-event direction from the primary prevention trial, although its original report has a different data window.

Exact excerpts:

- S1, Boxed Warning: "Some of the strokes, pulmonary emboli, and uterine malignancies were fatal."
- S1: "2.20 for tamoxifen vs. 0.71 for placebo".
- S1: "0.75 for tamoxifen versus 0.25 for placebo".
- S5, Box Warnings: "Tamoxifen carries a boxed warning for uterine malignancies, pulmonary embolism, and stroke in patients at high risk for cancer or those with DCIS."

The packet's endometrial adenocarcinoma and pulmonary embolism rates exactly match S1, both per 1,000 women-years. The warning encompasses BOTH high-risk women and women with DCIS in the risk-reduction setting. S1 also gives stroke rates 1.43 versus 1.00 and uterine sarcoma rates 0.17 versus 0.04 in that box. These are not percentages or risks per 1,000 randomized women.

Important provenance: the box footnotes identify endometrial/sarcoma figures as updated P-1 follow-up, median 6.9 years; the stroke/PE figures refer to Table 3's earlier data. For the uterine rates, S1's detailed warning identifies 8,306 women with an intact uterus at randomization and 53 versus 17 endometrial adenocarcinomas during total follow-up, not all randomized women. S4 independently supports the harm pattern and clinical restrictions, but is not independent measurement of precisely these label rates. No significance claim is justified merely because stroke incidence is numerically higher; the label's stroke RR CI crosses 1.

**Promotion blockers:** retain explicit DCIS/high-risk context, rate denominators, and distinct data windows in the eventual evidence anchors. The numerical source-check is resolved; do not describe the same-label fetch as independent verification or this receipt as final safety approval.

### 3. tamoxifen-contraindication-001

**Verdict: partially verified; unresolved for full independent clearance.** Original family: S1 label. Independent checks: S4 guideline and S5 professional reference.

Exact excerpts:

- S1, Contraindications heading: "Reduction in Breast Cancer Incidence in High Risk Women and Women with DCIS".
- S1: "require concomitant coumarin-type anticoagulant therapy".
- S4, Table 1: "Is not recommended for women with a prior history of deep vein thrombosis, pulmonary embolus, stroke, or transient ischemic attack."
- S5: "Tamoxifen should not be used in patients with a known allergy to the drug or any of its components".

The packet accurately reflects the label's hypersensitivity prohibition and the risk-reduction restriction on coumarin anticoagulants or prior DVT/PE. However, its separate listing of DCIS and high-risk incidence reduction in claim 1 makes "risk-reduction indication specifically" potentially ambiguous: the contraindication expressly includes women with DCIS. It must not be read as only primary prevention in otherwise cancer-free women.

S4 independently corroborates the DVT/PE clinical exclusion, with additional stroke/TIA guidance that should not be misrepresented as the exact S1 contraindication list. S5 supports hypersensitivity and DCIS-related DVT/PE avoidance, but its unqualified statement against coadministration with warfarin is broader than S1's indication-specific contraindication. S1 separately allows for careful prothrombin-time monitoring where coumarin coadministration occurs outside that restriction. Thus S5 does not independently settle the exact indication-specific coumarin-class boundary.

**Promotion blockers:** explicitly include DCIS, preserve the treatment-versus-risk-reduction distinction, and obtain an independently authored, appropriately authoritative clinical source that resolves the coumarin scope. Do not broaden the label contraindication to all oncology treatment or downgrade the authority gate.

### 4. tamoxifen-mechanism-serm-001

**Verdict: supported for its narrow mechanism scope.** Original families: S1 label and PubChem structured database. Independent family: S5 professional clinical reference; S11 educational corroboration. PubChem was not refetched or counted as independent.

Exact excerpts:

- S5, Mechanism of Action: "In breast tissue, tamoxifen competes with estrogen for binding sites, exerting antiestrogenic and antitumor effects."
- S11, tissue-specific mechanism: "while mimicking its effects in others."
- S1, Clinical Pharmacology: "In cytosols derived from human breast adenocarcinomas, tamoxifen competes with estradiol for estrogen receptor protein."

The competitive antiestrogenic mechanism in breast is confirmed beyond the packet's label/database families. S5's tissue-dependent agonist/antagonist explanation matters: this is not universal estrogen blockade. S1 includes a human-tumor-cytosol observation as well as animal findings, but neither is a patient-outcome trial. Keep the packet's warning against deriving clinical efficacy from mechanism alone.

**Promotion blockers:** none substantive for this narrow statement, subject to the delegated lead's formal per-claim decision and packet-level review gate. No evidence-tier or confidence change is proposed.

### 5. tamoxifen-dose-context-001

**Verdict: needs correction.** Original family: S1 label. Independent families: S9 primary prevention trial, S4 risk-reduction guideline, S10 adjuvant guideline; S5 supports the difference between metastatic and prevention dosing.

Exact excerpts:

- S1, Dosage and Administration: "For patients with breast cancer, the recommended daily dose is 20-40 mg."
- S1: "Dosages greater than 20 mg per day should be given in divided doses (morning and evening)."
- S4, Table 1: "20 mg/d for 5 years".
- S10, Key Changes: "Tamoxifen is now recommended for a duration of up to 10 years rather than 5 years."

The narrow historical B-14 and P-1 regimen of 20 mg/day for five years is supported. S1's dedicated dosage section additionally describes 10 mg twice OR three times daily in different adjuvant trials for two years, and its overview includes 20-40 mg/day regimens. It specifies 20 mg daily for five years for DCIS and high-risk incidence reduction. Therefore "protocols underlying tamoxifen's approved uses used 20 mg/day" is overbroad as an umbrella summary.

S10 demonstrates that a five-year historical statement must not be presented as an exclusive contemporary adjuvant duration. It does not extend the primary-prevention regimen to ten years. S1 is from 2007 and its B-14 statement predates ATLAS. Also, tablet strengths are tamoxifen-equivalent doses: 20 mg tamoxifen corresponds to 30.4 mg tamoxifen citrate in S1, not 20 mg of citrate salt mass.

**Promotion blockers:** restrict wording to named protocols, date the historical label context, preserve treatment/prevention distinctions and tamoxifen-equivalent units. Do not turn the receipt's label comparison into dosing advice. A1/A2 field authority remains required.

### 6. tamoxifen-efficacy-nsabp-p1-001

**Verdict: partially verified; unresolved discrepancy.** Original families: Fisher controlled-human study and S1 label. New independent family: S4 professional guideline/synthesis. S9 recovers the original abstract but is not an additional independent family.

Exact excerpts:

- S9, Results: "cumulative incidence through 69 months of follow-up of 43.4 versus 22.0 per 1000 women".
- S9: "Tamoxifen reduced the occurrence of estrogen receptor-positive tumors by 69%".
- S9: "risk ratio = 2.53; 95% confidence interval = 1.35-4.97".
- S4, NSABP-P1 discussion: "The initial results were based on a median of 4.6 years (54.6 months) of follow-up."
- S1: "86 cases- tamoxifen, 156 cases-placebo".

| Measure | 1998 publication evidence (S9; S4 synthesis where noted) | S1 label evidence |
| --- | --- | --- |
| Randomized population | 13,388; tamoxifen 6,681, placebo 6,707; planned 20 mg/day for five years | Same randomized totals; median treatment 3.5 years |
| Analysis/follow-up population | S4: 6,576 tamoxifen, 6,599 placebo | Follow-up available January 31, 1998 for 6,544 tamoxifen, 6,570 placebo, total 13,114 |
| Follow-up descriptor | Initial median 54.6 months (S4); cumulative estimate THROUGH 69 months (S9) | Median 4.2 years for initial incidence results |
| Invasive cancers | S4: 89 versus 175; RR 0.51, CI 0.39-0.66; 49% reduction | 86 versus 156; RR 0.56, CI 0.43-0.72; 44% reduction |
| Absolute invasive estimate | 22.0 versus 43.4 per 1,000 women through 69 months | 3.58 versus 6.49 per 1,000 women-years in Table 3 |
| ER-positive tumors | 69% reduction; S4: 41 versus 130, RR 0.31 | Table 4: 38 versus 115 |
| Initial endometrial estimate | RR 2.53, CI 1.35-4.97 (S9) | 33 versus 14; RR 2.48, CI 1.27-4.92 |

The packet's primary-paper numerical statement is supported by the recovered abstract, including direction of stroke/PE/DVT events and concentration of adverse events in older women. S4 corroborates initial efficacy and the 54.6-month median independently as professional synthesis. However, the review flag's explanation comparing "69-month follow-up" with a 4.2-year median is statistically misleading: a cumulative-estimate horizon is not a median follow-up. The analysis populations and event counts also differ. Do not average, merge, or casually choose these estimates as interchangeable versions of one denominator/window.

The evidence is consistent with different analysis snapshots, but the exact publication cutoff, inclusion/exclusion/adjudication rules, and endometrial-risk-set reconciliation were not verified against the primary full text or a regulator analysis. S6 was blocked; S3 was unusable; S9 retrieved only the abstract. A claim that the discrepancy has been completely explained by follow-up would exceed this evidence. Also, the label's later 6.9-year uterine update and boxed-warning rates are a third dataset, not substitutes for either initial RR.

**Promotion blockers:** obtain the primary tables/methods and resolve the analysis-set/cutoff/endometrial-denominator differences explicitly; fix the misleading follow-up explanation. Keep prevention findings distinct from treatment efficacy and statistical significance distinct from numerical elevation. A1 backing for the general safety pattern is not exact backing for every 1998 point estimate.

### 7. tamoxifen-efficacy-atlas-extended-001

**Verdict: needs correction; high-priority endpoint and denominator error.** Original family: Wiki Journal Club secondary trial summary. Independently fetched families: S2 primary controlled trial and S10 professional guideline/synthesis.

Exact excerpts:

- S2, Findings: "reduced overall mortality".
- S2, Table 2 row labels: "Any death"; "Death with recurrence".
- S2, statistical methods: "Kaplan-Meier graphs show absolute risks".
- S2, Table 2 footnote: "analyses of uterine tumour incidence exclude women with hysterectomy recorded at trial entry."
- S10, Table 2 footnote: "Percentages calculated when not reported in original study."

Exact outcome reconciliation, continued treatment versus stopping:

| Endpoint | Primary evidence and analysis | Error in packet |
| --- | --- | --- |
| Recurrence, ER-positive | 617/3,428 versus 711/3,418, approximately 18.0% versus 20.8% crude proportions; overall log-rank event-rate ratio 0.84 (0.76-0.94), p=0.002 | Crude proportions need their denominator and observation period; they are not the years 5-14 cumulative risks |
| Cumulative recurrence | 21.4% versus 25.1% during years 5-14 after diagnosis; absolute reduction 3.7 percentage points | Missing from packet's endpoint/time framing |
| All-cause mortality, ER-positive | 639/3,428 versus 722/3,418, approximately 18.6% versus 21.1%; Table 2 "Any death" RR 0.87 (0.78-0.97), p=0.01 | Packet falsely calls this breast-cancer mortality and "cumulative" |
| Breast-cancer mortality | Years 5-14 cumulative mortality 12.2% versus 15.0%, absolute reduction 2.8 percentage points; summary counts 331 versus 397 | These are not 18.6% versus 21.1%. Table 2 separately reports death-with-recurrence RR 0.83 (0.72-0.96); do not substitute it uncritically for the methods-defined breast-cancer mortality analysis |
| Endometrial cancer | 116 versus 63 events; event-rate ratio 1.74 (1.30-2.34), p=0.0002; cumulative risk years 5-14: 3.1% versus 1.6%; mortality 0.4% versus 0.2% | 1.80% versus 0.97% are not cumulative estimates. 116/6,454 is about 1.80%, while 63/6,440 is about 0.98%, and these use all-randomized denominators including hysterectomies |

Population/time controls: 12,894 women completed a median five years of prior tamoxifen; randomized groups 6,454 versus 6,440. Breast-cancer main analyses use 6,846 ER-positive women (3,428 versus 3,418). Side-effect analyses use any ER status, censor at recurrence, and uterine-incidence analyses exclude known hysterectomy at entry. Table 1 lists 1,066 versus 1,160 hysterectomies: subtracting those gives 5,388 versus 5,280 potentially eligible for uterine-incidence analysis, including unknown hysterectomy status, not the ER-positive denominator. This subtraction is reviewer arithmetic, not a claim of a fully observed fixed follow-up risk set.

The paper uses the August 31, 2012 dataset and reports mean further follow-up of 7.6 woman-years after entry at about year 5. Overall recurrence counts include events after year 15; they must not be labeled solely years 5-14. Recurrence RRs are 0.90 during years 5-9 and 0.75 in later years; breast-cancer mortality RRs are 0.97 and 0.71, respectively. The mortality analysis subtracts death-without-recurrence log-rank statistics from overall mortality, rather than simply assigning all deaths after recurrence to breast cancer. Preserve these distinctions.

S10 independently confirms the direction of extended-adjuvant benefit and the endometrial/VTE tradeoff; its tables expose the packet's mortality-column confusion. It provides an A2-type evidence route, contrary to treating the absence of ATLAS in the 2007 label as grounds to disable authority requirements. Neither S2 nor S10 establishes the packet's erroneous exact statement. S1's older B-14 finding is different trial evidence, not a regulator refutation of ATLAS.

**Promotion blockers:** correct outcome labels, cumulative windows and populations; supply primary and professional anchors with validated source authority; resolve `tamoxifen-extended-duration-benefit-risk-001` only after corrected evidence is independently reviewed. The prior authority-flag recalibration is not accepted as a gate waiver. Preserve both benefit and harm, and do not infer a ten-year prevention regimen.

### 8. tamoxifen-evidence-gap-bodybuilding-pct-001

**Verdict: needs correction; limited independent confirmation.** Original family: S8, described in the packet as a human survey. Actual design is forensic product-composition testing plus contextual literature discussion. New family: S7 clinical review; S12 traces the underlying forum-analysis source but is not independent replication of adverse-event reports already cited in S8. S1/S11 independently bound the approved-use context, not illicit-product outcomes.

Exact excerpts:

- S8, Methods: "were tested at the Polish Official Medicines Control Laboratory."
- S8, tamoxifen Results: "Seven samples with declared tamoxifen were tested; five contained the declared API, while two were incorrectly labeled, and the declared API was absent."
- S12, Results: "157 ADRs were identified: 95 for SERMs and 62 for AI."
- S7, Abstract: "The evidence for effective, safe management of AAS cessation and withdrawal is weak."

S8 examined 601 seized PED samples from January 2020 through August 2024, including 63 declaring PCT drugs and seven declaring tamoxifen. Two of those seven lacked tamoxifen: one contained EDTA disodium salt dihydrate and the other had no API. This is a selected seized-product sample, not a representative estimate of all illicit tamoxifen or an efficacy/safety trial. The claim's reference to mislabeled/inactive products is directionally supported, but its quoted sentence about "Among the 63 samples" is a synthesized sentence rather than the exact retrieved tamoxifen paragraph.

The 24%, 21%, 19%, 13%, 5%, 4%, 4%, and 2% adverse-event figures occur in S8's INTRODUCTION, attributed to Rochoy et al. 2022, not S8's Results. S12 confirms that the source analyzed forum posts from 2013-2019, including 845 SERM-related posts and 571 AI-related posts among 1,792 posts; its abstract reports 95 SERM ADRs. These are self-reported, selected forum observations, not tamoxifen-specific incidence rates in a clinical cohort, and not patient outcomes measured in the 2025 laboratory study. S12's abstract does not independently verify every percentage in the packet; its full text remains unverified.

S7 independently supports a historical evidence gap for AAS withdrawal management, not a comprehensive negative search proving that no relevant guideline or controlled trial exists as of September 2026. Nor does it establish tamoxifen-specific PCT efficacy. S5 describes other off-label gynecomastia uses and literature; those must not be conflated with unsupervised AAS PCT or dismissed by a blanket statement that only approved uses have any evidence. FDA nonapproval and absence of efficacy evidence are different propositions.

**Promotion blockers:** correct design/source metadata and adverse-event attribution; bound claims to the seized sample and historical evidence searches; do not claim clinical causality or population frequency from forum reports. Preserve `tamoxifen-gray-market-vs-approved-use-001` as unresolved pending corrected wording and review. A C1 survey/product-testing reference and an `evidence-gap` claim type do not authorize unqualified safety assertions or relax A1/A2 gates.

## Lead Handoff

The highest-priority repairs are ATLAS endpoint/denominator labeling, PCT study attribution, indication qualifiers, and dose-scope/date boundaries. P-1 needs a documented methods/table reconciliation, not averaging or a follow-up-only explanation. The contraindication's complete independent scope check remains open. Source metadata and excerpts should be corrected by the author in a separately authorized step; this reviewer has made no packet changes.

No claim is approved for publication by this receipt. Formal decisions, validated authority links and any expert escalation remain with the delegated lead. Failed access and source-independence gaps must remain visible, and unpublished drafts must not become runtime content.
