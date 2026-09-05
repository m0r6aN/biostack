# Source Truth Re-review: Evidence Brief

Reviewed 2026-09-05 against base `4d8754c670a7d4553ade857be80aa170ede85653`.
This brief contains evidence, not publication approval. Source excerpts are short;
follow their links and sections for full populations, methods, and limitations.

## Context

BioStack publishes observational, source-graded compound dossiers. Its next proposed
promotion wave includes Creatine, Vitamin D3, and Tamoxifen. All three input packets
have `ops.needsReview=true` and `completeness=partial`. Independent source-family
verification is required; a mirror of a paper is not a second source family or study.
No prescriptions. Protected claim types require A1/A2 support or remain gated.
Only Clint merges, deploys, changes infrastructure/secrets, or writes production DBs.

## Evidence Inventory

### Creatine

Packet: `research/input/evidence/creatine.evidence.json`,
`creatine-safety-common-adverse-effects-001` and its review flags.
The statement calls 13.7% versus 13.2% side-effect rates and describes a 2025
analysis as independently corroborating the tolerability profile.

[Kreider et al. 2025, indexed primary abstract and author metadata](https://www.ebi.ac.uk/europepmc/webservices/rest/search?query=EXT_ID:40198156&format=json&resultType=core),
PMID 40198156, DOI 10.1080/15502783.2025.2488937, Results:

> Side effects were reported in 13.2% of studies in the PLA groups and 13.7% of studies in the Cr-supplemented groups

The abstract separately reports participant side-effect frequency as placebo
4.21% and creatine 4.60% (p=0.828). Its 28.4 million adverse-event reports are a
separate analysis; mention frequency is not a creatine-exposed population risk.
Both this paper and the packet's 2017 ISSN position stand list Richard B. Kreider
as first author. Distinct publication is not author-independent corroboration.

[Hair-loss RCT primary full text](https://pmc.ncbi.nlm.nih.gov/articles/PMC12020143/),
DOI 10.1080/15502783.2025.2495229, Methods and Results:

> Hair growth and loss parameters were evaluated using the Trichogram test and the FotoFinder system

The reviewer found 38 hormone participants but 37 hair-assessment participants;
one participant lacked hair measurements. The abstract and methods describe
recruitment/randomization differently, so the exact randomized count needs
reconciliation. AHLA's [separate commentary](https://www.americanhairloss.org/creatine-and-hair-loss-what-the-latest-study-got-right-and-what-it-missed/)
calls the Trichogram outdated; that is an appraisal, not an additional trial.
The trial itself notes absent family-history assessment and circulating rather
than scalp androgen measurements. Null results at 12 weeks do not establish
absence of an effect in all populations or over longer durations.

[2026 renal meta-analysis](https://link.springer.com/article/10.1007/s11255-026-05287-x)
reports 26 RCTs/1,036 participants overall, not an all-measured-GFR dataset.
Its measured Cr-EDTA GFR and creatinine-estimated GFR results differ. It says
non-dialysis CKD studies are absent from its reviewed evidence base; this is not
an exhaustive claim about every study available on today's date. The packet's
renal-population conflict remains `needs-human-review`.

The FDA-hosted GRN 931 PDF was not recovered as readable text in this pass.
An applicant's FDA-hosted notice is not, by hosting alone, an FDA safety conclusion.
NIH ODS requests returned 403. Broad safety/authority verification remains incomplete.

### Vitamin D3

Packet: `research/input/evidence/vitamin-d3.evidence.json`,
`vitamind3-efficacy-fracture-falls-uspstf-negative-001`:

> documents a small increased kidney-stone risk not mentioned in the 2018 statement

[USPSTF April 17, 2018 final statement](https://www.uspreventiveservicestaskforce.org/uspstf/recommendation/vitamin-d-calcium-or-combined-supplementation-for-the-primary-prevention-of-fractures-in-adults-preventive-medication),
Harms of Preventive Medication:

> The USPSTF found adequate evidence that supplementation with vitamin D and calcium increases the incidence of kidney stones.

It explicitly calls the harm small. Its recommendation table also contains an
I statement for higher-dose vitamin D/calcium fracture prevention in postmenopausal
women, absent from the packet's historical summary. Its historical falls text
specifies community-dwelling adults age 65 or older.

Independent primary-study provenance: Jackson et al., WHI, NEJM 2006,
DOI 10.1056/NEJMoa055218, [indexed primary abstract](https://www.ebi.ac.uk/europepmc/webservices/rest/search?query=EXT_ID:16481635&resultType=core&format=json):

> The risk of renal calculi increased with calcium plus vitamin D (hazard ratio, 1.17; 95 percent confidence interval, 1.02 to 1.34).

This is a combined-intervention result, not an isolated D3 effect.

The [December 17, 2024 consolidated document](https://www.uspreventiveservicestaskforce.org/uspstf/recommendation/vitamin-d-calcium-combined-supplementation-primary-prevention-falls-fractures-communitydwelling-adults)
returned a page headed Draft Recommendation Statement. It says "When final"
it will replace the 2018 recommendation. A separate current falls-guidance URL
returned 404; the reviewer did not certify the blanket assertion that all 2018
grades remain current. Its pooled stone estimate is an update, not first discovery.

[VITAL primary manuscript](https://pmc.ncbi.nlm.nih.gov/articles/PMC6425757/),
DOI 10.1056/NEJMoa1809944: 25,871 adults, median intervention 5.3 years,
invasive cancer HR 0.96 (95% CI 0.88-1.06), major cardiovascular events HR 0.97
(0.85-1.12). Primary outcomes were not significantly reduced. BMI analyses are
hypothesis-generating and unadjusted for multiple comparisons. The reported serum
increase comes from a subset with repeat measurements, not every participant.
PMC is the primary paper's host, not a second independent trial.

[IOM original reference-setting chapter](https://www.ncbi.nlm.nih.gov/books/NBK56058/)
distinguishes lower caution concentrations from those often reported in toxicity
and says the human toxicity threshold is not readily defined. Exact live ODS,
StatPearls, and product-label portions were not fully re-verified in this pass.
No universal diagnostic/safety boundary is established by 125 versus 375 nmol/L.

### Tamoxifen

Packet: `research/input/evidence/tamoxifen.evidence.json`,
`tamoxifen-efficacy-atlas-extended-001` labels 18.6% versus 21.1%, RR 0.87,
as cumulative breast-cancer mortality, and 1.80% versus 0.97% as cumulative
endometrial-cancer incidence.

[ATLAS primary Lancet paper](https://pmc.ncbi.nlm.nih.gov/articles/PMC3596060/),
DOI 10.1016/S0140-6736(12)61963-1, Table 2:

The ER-positive "Any death" row is 639/3,428 versus 722/3,418, RR 0.87
(95% CI 0.78-0.97). These reproduce the packet's 18.6%/21.1% as crude
all-cause proportions. The paper's years 5-14 cumulative outcomes instead are:

| Endpoint | Continue to ten years | Stop at five years |
| --- | ---: | ---: |
| Recurrence, ER-positive cohort | 21.4% | 25.1% |
| Breast-cancer mortality, ER-positive cohort | 12.2% | 15.0% |
| Endometrial cancer, safety analysis | 3.1% | 1.6% |

Efficacy analyses comprise 6,846 ER-positive women. Safety analyses include
12,894 women across ER statuses; uterine-tumour incidence excludes women with
hysterectomy recorded at entry and is censored at recurrence. Entry counts as
year five after approximately five years of prior treatment, not time zero for
the reported years 5-14 window. Benefits and harms must stay together.

[NCI independent editorial synthesis](https://www.cancer.gov/types/breast/research/10-years-tamoxifen)
reproduces the cumulative recurrence and breast-cancer mortality table. This is
a different source family, not independent trial replication. The reviewer also
checked the [2017 correction](https://pmc.ncbi.nlm.nih.gov/articles/PMC8889022/);
it does not turn all-cause death into breast-cancer mortality.

For `tamoxifen-efficacy-nsabp-p1-001`, the [Fisher 1998 primary abstract](https://www.ebi.ac.uk/europepmc/webservices/rest/search?query=EXT_ID:9747868%20AND%20SRC:MED&format=json&resultType=core)
reports 49% reduction, cumulative incidence through 69 months, and endometrial
RR 2.53. The [packet's DailyMed label](https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=8f642753-9e12-433c-a0bc-ab33dac41ddf)
reports 44% reduction after median 4.2 years and endometrial RR 2.48.
A cumulative-incidence horizon is not a median follow-up. The precise analysis
difference is unresolved, so do not average the estimates or attribute the
discrepancy solely to duration. Dedicated structured chemical identifiers were
not resolved by this review.

## Review Provenance

Three fresh-context native general subagents performed read-only primary-source
re-review. They did not author the packets or write decisions. Model selection
was host-controlled/advisory; no distinct-provider identity is claimed for them.

| Scope | Session receipt | Bounded web calls |
| --- | --- | ---: |
| Creatine | `ses_f8ee039f5ffezZb6p9i2xMhr8B` | 10 |
| Vitamin D3 | `ses_f8ee039dfffeKPzNExPmx86SDG` | 9 |
| Tamoxifen | `ses_f8ee039d2ffeSZJwsIUbUPLXcD` | 8 |

This file is the lead's condensed evidence receipt, not verbatim agent output.
The lead separately fetched the ATLAS primary paper, USPSTF 2018 statement, and
Kreider 2025 abstract and verified the three headline discrepancies. Secondary
details above are reviewer-verified, not all separately re-fetched by the lead.
No unreviewed claim receives new approval. Provider access failures are not
evidence that a biomedical claim is false.

## Your Task

Evaluate whether the evidence supports promotion of any of these unchanged
packets, what minimum corrections and independent checks are needed, and where
the evidence itself does not justify a conclusion. Do not fetch new evidence,
approve publication, edit files, or inspect other local data. Judge only this brief.

Output exactly `TOP 3 FINDINGS` followed by three numbered one-line findings,
then `TOP 3 MOVES` followed by three numbered implementation-ready moves.
Maximum 250 words. Distinguish factual contradictions from unresolved checks.
