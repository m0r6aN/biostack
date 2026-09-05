# Wave006 Creatine Independent Evidence Review

Date: 2026-09-05

Reviewer: OpenCode, independent evidence-review agent, not packet author or final delegated approver.

Worktree: `D:\Repos\BioStack\.worktrees\gtm-wave006-20260905`

Packet: `research/input/evidence/creatine.evidence.json`, packetId `creatine-evidence-expansion-001`.

## Decision

**Entire packet promotable: NO.** All eight claims were assessed, but full independent verification was not achieved. Decisions: **0 approve, 2 unresolved, 6 request-changes**. Retain `ops.needsReview = true`, partial completeness, unpublished status, and the renal population conflict. This receipt is not a schema-valid approval batch, a publication instruction, or a claim that every citation was accessible.

Principal blockers are mislabeled safety denominators, omitted renal endpoint distinctions, incomplete hair-method reporting and hair-outcome denominator, insufficient demonstrated A1/A2 field-level support, and remaining independent-family/source-fidelity gaps. Prior packet assertions that remediation is resolved are not accepted as verification.

## Instructions And Scope

- Read repository `AGENTS.md` in `D:\Repos\BioStack`, worktree `research/README.md`, `research/directives/04-preprocess-compile-review.md`, and `research/review-decisions/OPERATOR-DELEGATION-2026-08-28.md`.
- Delegation transfers who approves, not the cross-source standard. It names the lead as final judge; this agent supplies independent findings only.
- Used 12 public web fetches, including blocked requests and one multi-record primary-literature retrieval. No authenticated access, secrets, production operations, git mutations, author edits, code changes, or other manual file writes.
- Government hosting of a journal abstract does not make it government-authored authority. A mirror of the same article is source-fidelity evidence, not independent corroboration.
- Independence is evaluated against each claim's current `sourceRefs`, not just the packet's earliest version. A different systematic review remains the same family. Direct examination of a trial included in a cited synthesis provides a different evidence level/family but not a statistically independent replication of that synthesis.
- Only primary human studies or government material are credited toward the requested new-source check. ISSN and other reviews can expose contradictions or verify attribution, but are not silently recast as primary studies. No model consensus is used as evidence.
- Quote excerpts below normalize whitespace and Unicode typography to ASCII. Numerical findings remain attributed to the inspected source; this review did not reanalyze participant data.

## Access Ledger

These are the exact URLs requested in this pass. URLs appearing in returned reference lists were not automatically visited. The U9 record identifiers below distinguish articles obtained in that single response.

| ID | Exact URL | Access And Provenance |
|---|---|---|
| U1 | https://ods.od.nih.gov/factsheets/ExerciseAndAthleticPerformance-HealthProfessional/ | HTTP 403. No NIH quote or current field-level authority verified. |
| U2 | https://www.fda.gov/media/143525/download | Returned PDF bytes as unreadable text, not a usable document extraction. Filing, authorship, response disposition, and quotations remain unverified. FDA hosting alone supplies no A1 credit for a submitter's assertions. |
| U3 | https://link.springer.com/article/10.1186/s12970-017-0173-z | ISSN 2017 position stand accessible, including abstract, metabolic role, supplementation protocols, and position statements. Professional review, packet B1, not primary or A1/A2. |
| U4 | https://pubmed.ncbi.nlm.nih.gov/40198156/ | Cookie challenge, not article content. Same article's abstract recovered through U9. |
| U5 | https://www.fda.gov/food/information-industry-dietary-supplements/notifications-structurefunction-and-related-claims-dietary-supplement-labeling | Accessible FDA-authored regulatory explanation. Relevant to labeling, not a creatine drug-approval database search or renal safety assessment. |
| U6 | https://link.springer.com/article/10.1186/s12882-025-04558-6 | Accessible BMC Nephrology systematic review. Published November 6, 2025, not packet January 1, 2025. |
| U7 | https://www.ebi.ac.uk/europepmc/webservices/rest/search?query=DOI:10.1080/15502783.2025.2495229&format=json&resultType=core | Accessible original packet endpoint, with trial abstract and provenance, PMID 40265319, PMCID PMC12020143. First publication April 23, 2025, not packet January 1. |
| U8 | https://www.americanhairloss.org/creatine-and-hair-loss-what-the-latest-study-got-right-and-what-it-missed/ | Accessible AHLA article, by AHLA, April 29, 2025. Commentary, not primary research or demonstrated formal clinical guideline. |
| U9 | https://www.ebi.ac.uk/europepmc/webservices/rest/search?query=SRC:MED%20AND%20(EXT_ID:8828669%20OR%20EXT_ID:10449017%20OR%20EXT_ID:40198156%20OR%20DOI:10.1186/1550-2783-10-26%20OR%20EXT_ID:19741313%20OR%20TITLE:%22creatine%20supplementation%20and%20endurance%22)&format=json&resultType=core&pageSize=10 | Accessible batch of article records/abstracts. Used primary records 8828669, 10449017, 23680457, 9662683, 19741313, plus original safety-analysis record 40198156. Other returned reviews/animal evidence were not used to clear human claims. |
| U10 | https://www.ebi.ac.uk/europepmc/webservices/rest/PMC12020143/fullTextXML | Accessible full text of the same hair trial as U7. Source fidelity only for the hair claim. |
| U11 | https://link.springer.com/article/10.1007/s11255-026-05287-x | Accessible International Urology and Nephrology systematic review. Published July 27, 2026, not packet January 1, 2026. |
| U12 | https://www.efsa.europa.eu/en/efsajournal/pub/2303 | HTTP 403. Attempted government scientific assessment expansion for exercise scope; no excerpt or corroboration credited. Not evidence of U.S. regulatory status. |

Unvisited claim-cited URLs, listed explicitly to avoid an implied fidelity pass:

- `https://pubchem.ncbi.nlm.nih.gov/compound/Creatine`: PubChem mechanism quotation and upstream attribution not checked.
- `https://go.drugbank.com/drugs/DB00148`: description quotation not checked.
- `https://www.mdpi.com/2072-6643/17/17/2748`: all three extracted quotations, numeric effects, and subgroup statements not checked.

The GSRS registry entry is not in any of the eight claims' `sourceRefs`; identifier/alias certification was not performed. Additional source expansion stopped at the user-specified fetch bound, not because a configured worker expansion limit was measured or exhausted.

## Per-Claim Decisions

### 1. creatine-regulatory-status-001

**Decision: request-changes.** Packet lines 191-228. `fieldAuthorityRequired: true` must remain.

**Original families:** regulator/label (FDA notice and labeling explanation), government fact sheet (NIH).

**Fidelity and excerpts:** U5 reproduces both the packet's quoted non-drug-solely-because-of-claim sentence and standard disclaimer. It also requires that "the entity making the claim has substantiation that the statement is truthful and not misleading" and notification "no later than 30 days after first marketing." It says an FDA response or absence of one "should not be read as a statement about the product's compliance with other legal requirements."

**Unsupported or incorrect:** "only subject to post-market notification" is materially incomplete and misleading: substantiation, non-misleading content, permissible claim scope, and disclaimer requirements also apply. U5 does not prove the categorical current claim that creatine is not an FDA-approved drug. A GRAS notice is not a drug approval and does not establish unrestricted ingredient/formulation/use safety. U2 could not establish the named filing's contents, notifier, intended-use scope, or any FDA-authored response. Do not describe its renal safety text as an FDA conclusion or carry its current regulator/A1 classification forward without document-level authorship verification.

**Independent check:** No materially different primary/government family confirming U.S. approval and GRAS disposition was obtained. Re-reading U5 is fidelity, not independence; U12 was blocked and in any event cannot establish U.S. law. No current Drugs@FDA, NDI, or warning-letter sweep was performed. This is explicitly incomplete, not a finding that subsequent FDA action does or does not exist.

**Blockers:** Correct the notification-only wording; distinguish submitter assertions from agency findings and restricted GRAS use from drug approval; obtain current authoritative regulatory evidence and a qualifying cross-family check. Retain A1/A2 field gate and legal-review escalation.

### 2. creatine-mechanism-atp-phosphocreatine-001

**Decision: unresolved.** Packet lines 231-267. Existing `fieldAuthorityRequired: false` is not changed.

**Original families:** structured databases (PubChem, DrugBank), government fact sheet (NIH).

**New primary check:** U9, Hultman et al., PMID 8828669, *Muscle creatine loading in men*, examines 31 males: "Muscle total creatine concentration increased by approximately 20% after 6 days" at 20 g/day. This is primary human experimental evidence materially different from database/fact-sheet families, but measures muscle creatine accumulation rather than directly proving each step of the reversible kinase reaction.

**Supporting context, not a primary clearance:** U3 states, "The primary metabolic role of creatine is to combine with a phosphoryl group (Pi) to form PCr through the enzymatic reaction of creatine kinase (CK)" and that PCr energy buffers ATP resynthesis during maximal-effort anaerobic exercise. That professional-review family differs from the claim's originals, but does not meet the user's primary/government new-source restriction by itself.

**Fidelity:** Original PubChem/DrugBank quotes were not accessed; U1 was blocked. The packet's PubChem quotation also contains an ellipsis and needs verification against the actual upstream attributed record, rather than assuming PubChem originated the text.

**Unsupported parts/blockers:** No substantive biochemical contradiction found, but exact original quotation provenance and direct primary confirmation of the complete reversible phosphate-transfer account are incomplete. Obtain those before approval. Confidence in a familiar mechanism is not a substitute for the required receipt.

### 3. creatine-efficacy-highintensity-strength-lbm-001

**Decision: unresolved.** Packet lines 269-323. Existing `fieldAuthorityRequired: false` is not changed.

**Original families:** professional position stand (ISSN), government fact sheet (NIH), systematic review/meta-analysis (MDPI).

**New primary checks:** U9, Volek et al., PMID 10449017, *Performance and muscle fiber adaptations to creatine supplementation and heavy resistance training*, reports 19 healthy resistance-trained men randomized to creatine (10) or placebo (9), 12 weeks. Excerpts: "increases in body mass and fat-free mass were greater in creatine (6.3% and 6.3%, respectively) than placebo (3.6% and 3.1%, respectively)"; bench press/squat increases were 24%/32% versus 16%/24%. U9, PMID 9662683, reports increased interval power but no endurance performance improvement. These are controlled/experimental human study evidence rather than the claim's synthesis families. Their possible inclusion in those syntheses means they are not new independent trial replications.

**Fidelity:** U3 contains the "most effective ergogenic nutritional supplement" wording in its body/position statements, not the abstract location supplied by the packet. Preserve it as an attributed 2017 ISSN characterization, not a newly established 2026 head-to-head ranking. U1 is blocked; the MDPI source and its three quoted passages were not accessed. The packet's own cautionary review flag does not verify those quotations.

**Unsupported parts/blockers:** The primary checks support a narrow strength/training and lean-tissue benefit, not superiority over every supplement, all demographics, every strength endpoint, or every endurance-related task. Fat-free mass is not interchangeable with dry contractile muscle. Correct the quote locator and verify NIH/MDPI text and the comparative/current-superlative scope before approving the composite claim. U12 government expansion was blocked.

### 4. creatine-dose-context-loading-maintenance-001

**Decision: request-changes.** Packet lines 325-362. Retain `fieldAuthorityRequired: true` and trial-context-only presentation.

**Original families:** NIH government fact sheet and ISSN professional position stand.

**New primary checks:** U9, Hultman PMID 8828669: "20 g of creatine for 6 days," maintenance "2 g/day thereafter," and "3 g/day" for 28 days. U9, Lugaresi PMID 23680457: "20 g/d for 5 d followed by 5 g/d throughout the trial" for 12 weeks. These materially different human-study sources substantiate particular protocols, not every endpoint of the packet's blended ranges. Hultman's 2 g/day maintenance is not a contradiction of other studies' 3-5 g/day protocols.

**Fidelity:** U3 contains both quoted ISSN dosing phrases in "Supplementation protocols," not "Abstract." Its source sentence literally places "or approximately 0.3 g/kg body weight" before "four times daily," an ambiguous construction which must not be converted into 0.3 g/kg per dose. The packet statement's 0.3 g/kg/day is the intended daily-total framing, but needs an unambiguous authoritative supporting quotation. U3 discusses 5-10 g/day for larger athletes and 3 g/day for 28 days. It does not alone confirm the packet's entire alternative 3-6 g/day for 3-4 weeks. NIH quotation fidelity could not be checked because U1 is blocked.

**Unsupported parts/blockers:** Fix locators and dose-unit ambiguity; separately substantiate all protocol ranges and populations, especially 5-10 g/day and the 3-6 g/day alternative. The accessed primary protocols are not A1/A2 dosing authority, and the packet's A2 listing is not a verified field-level match for every subclaim. Keep unavailable portions explicitly unknown/review-gated. "Not a recommendation" does not waive the dose-context gate.

### 5. creatine-safety-common-adverse-effects-001

**Decision: request-changes.** Packet lines 364-418. Retain the existing `fieldAuthorityRequired: true` despite the `adverse-effect` type.

**Original families:** NIH government fact sheet, ISSN position stand, paper-level pooled trial/adverse-report analysis.

**Fidelity and denominator finding:** U4 was blocked, but U9 recovered the same PMID 40198156 abstract. Its methods evaluated 685 human trials; placebo participants numbered 13,452 in 652 studies, and creatine participants 12,839 in 685 studies. It explicitly says "Side effects were reported in 13.2% of studies in the PLA groups and 13.7% of studies in the Cr-supplemented groups" (p=0.776). Those are proportions of studies reporting effects, NOT participant adverse-event rates. The participant-level aggregate quoted in the abstract is PLA 4.21%, creatine 4.60% (p=0.828).

The abstract confirms the rare mention proportion "0.00072%" among 28.4 million adverse-event reports and the conclusion quotation in substance. That reporting-database denominator is not 28.4 million creatine users, person-years, or an exposed cohort. The analysis also reports significant study-level GI and cramping/pain comparisons, but not significant participant-level comparisons for those categories. The packet must not collapse all these endpoints into one undifferentiated safety rate. Its extracted study-level quote is largely faithful, while the statement and supposedly resolved review flag lose the denominator.

**New primary check:** U10, the 2025 controlled hair trial, reports "There were no reported side effects in either group over the duration of the study." U9, Volek PMID 10449017, similarly says "No negative side effects to the supplementation were reported." Controlled human studies differ from the general-safety claim's synthesis/position/fact-sheet families. They supply narrow short-term observations only, not an independent validation of the exact pooled percentages, each adverse effect, or five-year exposure safety. U9 PMID 40198156 is original-source fidelity, not another independent family; its Kreider authorship also overlaps the ISSN source.

**Other fidelity:** U3 abstract does contain the attributed "up to 30 g/day for 5 years" safety language, extending to healthy and patient populations. Fidelity to a B1 review sentence is not A1/A2 support for a joint maximum-dose/maximum-duration assurance in healthy adults. U1's NIH short-term statement, water-retention discussion, and anecdotal symptom list remain inaccessible.

**Blockers:** Correct study versus participant denominators and trial versus spontaneous-report analyses; restore uncertainty around rate interpretation and longer/high-dose exposure; obtain field-matched A1/A2 backing. Do not retain the prior flag's assertion that the safety gap is resolved or automatically retain its upgraded Strong confidence on that basis.

### 6. creatine-monitoring-renal-function-001

**Decision: request-changes.** Packet lines 420-475 and conflict lines 572-580. Retain `fieldAuthorityRequired: true` and `creatine-renal-safety-population-scope-001` as `needs-human-review`.

**Original families:** two systematic reviews and an FDA-hosted GRAS filing currently mislabeled as regulator/A1 without verified passage authorship.

**Fidelity:** U6 abstract contains both supplied BMC conclusion quotations. Its GFR analysis is five studies, 69 creatine and 74 control participants, not its entire serum-creatinine dataset. Its body reports significant serum-creatinine increases again beyond 12 weeks. Thus "transient" is a faithful abstract word, not evidence that every user's elevation resolves during continued supplementation. The article itself has inconsistent study/outcome counts and requires caution rather than stronger inference.

U11 confirms 26 studies and 1,036 participants overall. It reports unchanged Cr-EDTA measured GFR (MD 5.89 mL/min, CI -0.30 to 12.08, abstract p=.06; body p=.062), but ALSO "a reduction in GFR assessed by creatinine-based methods" (MD -10.75 mL/min, CI -17.48 to -4.02, p=.002). These are distinct endpoints. The packet's added review flag frames the whole 26-trial/1,036-person set as gold-standard Cr-EDTA evidence and omits the creatinine-based reduction. The overall study count cannot be used as the measured-GFR denominator without endpoint-specific extraction.

U11's discussion supports the two hemodialysis RCTs and absence of non-dialysis CKD studies within its reviewed evidence base. The packet's "Only two RCTs..." quote is an edited version of a sentence beginning "To date, only two RCTs" with intervening reference links, and the location is Discussion, not Results. The Cr-EDTA quote combines/rounds wording from abstract/body rather than reproducing one body sentence verbatim. The non-dialysis CKD quote is present in Discussion. These scope limitations do not establish that no trials exist anywhere as of September 2026: the review's search ended March 2025.

**New primary checks:** U9, Lugaresi PMID 23680457, measured 51Cr-EDTA clearance in a 12-week placebo-controlled resistance-training/high-protein-diet study: "No significant differences were observed for 51Cr-EDTA clearance" (group-by-time p=.64); other kidney assessments were largely unchanged. This controlled-study family differs from systematic reviews, but Lugaresi is included in the syntheses and is not an independent replication of their pooled dataset. U10 adds a later 12-week primary trial reporting no creatinine/eGFR changes, but does not establish measured GFR or clinical CKD safety.

**Authority/blockers:** U2's renal quote remains unreadable and cannot be credited as FDA-authored A1. B1 meta-analyses and primary trials are not A1/A2 monitoring guidance. Correct measured versus estimated GFR, endpoint-specific denominators, quote locations/format, and time-bounded evidence-gap wording. Keep renal-impaired populations and monitoring advice gated pending appropriate authority/expert review. A nonsignificant GFR difference is not an equivalence or definitive no-harm demonstration.

### 7. creatine-misinformation-hairloss-dht-001

**Decision: request-changes.** Packet lines 477-540.

**Original families:** controlled human study (2025 trial) and AHLA commentary, labeled professional-position-stand in the packet.

**Source fidelity:** U7/U10 confirm the three trial abstract excerpts, 5 g/day, 12 weeks, and no statistically significant group-by-time interactions. U8 contains all six AHLA quoted passages in substance, with typography differences. However, it is an AHLA article linking to a Substack analysis, not demonstrated primary research or a formally developed clinical guideline. It also incorrectly calls the trial an April 2024 publication while its own page date is April 2025; U7/U10 establish April 23, 2025.

**Denominators:** U10 sections 2.3-2.4 say 45 recruited, six withdrawals before intervention, 20 creatine and 19 placebo allocated at study start, one subsequent creatine withdrawal, and 19 per group for blood analyses. Crucially, "one participant from the placebo group did not complete the final hair assessment." Results reiterate exclusion of that participant from hair analyses. Thus 38 completed the study/blood outcomes, but **37 contributed hair outcomes (19 creatine, 18 placebo)**. The abstract's "45 ... randomly assigned" and body allocation counts are not fully reconciled; do not silently resolve them as "45 randomized." State the recruitment/allocation discrepancy and endpoint denominators.

**Hair methods:** U10 section 2.7.2 explicitly used "the Trichogram test and the FotoFinder system," board-certified dermatologists, standardized imaging, and vertex assessment. Figure 5 describes "Raw TrichoScale trichogram results," including hair count/density, anagen/telogen fractions, terminal/vellus rates, follicular units, and cumulative thickness. The packet elevates AHLA's "outdated" characterization without accounting for the actual imaging system. A quoted criticism can be accurately transcribed yet still be an inadequately supported methodological judgment. Do not describe the study as merely an obsolete conventional trichogram or claim it lacked direct hair assessment.

Serum rather than scalp DHT was measured; no genetic screening is reported in the inspected methods. Those limitations remain relevant, but do not establish a positive causal effect in genetically susceptible users. Industry-related commentary also cannot by itself establish funding of this trial: U10 states no funding associated with the work. Author relationships, journal support, and trial funding are different provenance questions.

**Additional primary challenge, NOT qualifying family independence:** U9, van der Merwe et al., PMID 19741313, reports a double-blind placebo-controlled crossover study in 20 rugby players, 25 g/day loading then 5 g/day maintenance. Excerpt: "levels of DHT increased by 56% after 7 days of creatine loading and remained 40% above baseline" after maintenance. Its measured outcomes were serum hormones/body composition, not hair loss. It is a different experiment from the 2025 trial but still the controlled-human-study family already present. It cannot clear the materially-different-family requirement and cannot turn a hormonal surrogate into observed alopecia. U10 is a mirror of U7, not independent evidence.

**Blockers:** Correct the hair-outcome denominator and recruitment ambiguity; report actual methods; qualify or remove the unverified "outdated" judgment; distinguish serum DHT, scalp activity, and hair outcomes. Obtain a materially different primary/government family for the composite claim. The 12-week null result is neither proof of universal no-risk nor proof of harm in an unstudied subgroup. Existing `fieldAuthorityRequired: false` is not used to clear embedded dosing or safety reassurance; any promoted warning/dose/safety field must retain the mandated authority gate.

### 8. creatine-evidence-gap-endurance-broad-marketing-001

**Decision: request-changes.** Packet lines 542-569. Existing `fieldAuthorityRequired: false` is not changed.

**Original families:** NIH government fact sheet and ISSN professional position stand.

**New primary check:** U9, PMID 9662683, *Creatine supplementation in endurance sports*, tested triathletes after 6 g/day for five days. Excerpt: "Although interval power performance was significantly increased by 18%, endurance performance was not influenced." Its conclusion confines benefit to "short-term exercise included into aerobic endurance exercise." This is a materially different human experimental family. It distinguishes sustained endurance performance from anaerobic surges within an endurance event; it does not justify a blanket athlete-category exclusion. Its abstract alone does not supply full design/risk-of-bias detail.

U9, Volek PMID 10449017, provides direct resistance-training evidence as described above. U3 itself discusses possible benefit in high-intensity intermittent and endurance events; therefore a categorical extrapolation to all endurance-sport tasks is not supported even by the original position stand. U12 government expansion was blocked.

**Fidelity:** The sole extracted NIH quotation remains unchecked because U1 was blocked. No primary evidence of the prevalence/content of "online marketing and popular use" was collected. No dedicated cognitive/wellness evidence appraisal was performed, consistent with the packet's admission that those claims were not sourced.

**Unsupported parts/blockers:** Narrow the statement to evaluated exercise outcomes and distinguish sustained endurance from sprint finishes/surges. Separately substantiate or remove the broad marketing-prevalence assertion and cognitive/wellness comparison. An unevaluated indication is not demonstrated ineffective, nor is an exercise trial base sufficient to rank cognitive evidence. Keep the composite claim gated until those parts are supported or explicitly excluded.

## Promotion And Follow-Up Gates

1. Repair the general-safety study/participant/report denominators, renal measured-versus-estimated endpoints and endpoint sample sizes, and hair-outcome sample size/methods. Preserve attribution and uncertainty rather than replacing one categorical safety assurance with another.
2. Verify FDA filing authorship, precise use conditions, and any agency-authored disposition. Do not assign A1 to notifier safety conclusions merely because FDA hosts the submission. Correct publication dates and exact quotation locators; label paraphrases as paraphrases.
3. Obtain missing readable NIH, PubChem, DrugBank, and MDPI passages, and complete the per-claim independent-family gaps identified above. An article already listed in a claim cannot be renamed "independent" by accessing another host.
4. For `regulatory`, `approved-indication`, `dose-context`, `formulation`, `storage-reconstitution`, `contraindication`, `warning`, `monitoring`, and `interaction`, retain `fieldAuthorityRequired` and demonstrated A1/A2 support, or explicit unknown/review-gated status. Also retain every existing true flag, including this packet's adverse-effect claim. No safety clearance is inferred from an efficacy or misinformation classification.
5. Retain renal impairment escalation and the conflict ID. An expert/lead must resolve medical/legal concerns and issue any eventual schema-valid decision under the operator delegation. This review does not authorize runtime ingestion, publication, or promotion of the packet or any of its claims.

Only this Markdown receipt was manually written. The evidence packet and all existing review decisions were left unchanged.
