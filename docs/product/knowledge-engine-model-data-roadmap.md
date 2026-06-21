# Knowledge Engine Model/Data Roadmap

Date: 2026-06-18
Source research: `research/protocol-intelligence/biostack-ai-model-data-asset-research-memo.md`

This roadmap converts the model and data asset research memo into implementation phases for BioStack's Evidence-Guided Protocol Intelligence Engine. The implementation remains source-first internally, but the product posture is guidance-oriented: cited evidence processing, pattern detection, risk classification, protocol interpretation, educational guidance, and uncertainty-aware decision support. It does not authorize live integrations, scraping restricted sources, new credentials, or production behavior changes.

## Phase 1: Source Registry And Provenance Foundation

Goals:

- Create a governed registry for all biomedical, supplement, regulatory, anti-doping, and model assets.
- Track license, allowed use, disallowed use, refresh cadence, retrieval method, citation identifiers, and redistribution constraints before ingestion.

Inputs:

- Source registry schema.
- PubMed, DailyMed/openFDA, ClinicalTrials.gov, NIH ODS, WADA, OPSS, PubChem, Reactome, Open Targets, ChEMBL candidate entries.

Outputs:

- Versioned source registry records.
- Provenance envelope required for every retrieved source item.
- License status visible to ingestion, review, and export paths.

Acceptance criteria:

- Every source has license, allowed use, disallowed use, refresh cadence, citation identifier type, and human-review requirement.
- No ingestion job can promote data from an unknown or unapproved source.
- Redistribution constraints are queryable before user-facing display or export.

Required tests:

- Registry schema validation.
- Required-field tests for source examples.
- License-boundary tests that fail closed on missing license or allowed-use fields.

Safety constraints:

- Do not ingest PHI or credentialed clinical data.
- Do not scrape Global DRO, NatMed, DrugBank, or other restricted sources.
- Missing source approval blocks canonical promotion.

Open licensing questions:

- DrugBank, NatMed, UMLS/SNOMED, ChEMBL share-alike, SIDER non-commercial/share-alike, SemMedDB, Global DRO, and PMC full-text article licenses.

## Phase 2: Retrieval And Citation Backbone

Goals:

- Build retrieval over approved literature and label sources with stable citations.
- Require source IDs for every evidence claim.

Inputs:

- PubMed/PMC metadata and permitted full text.
- MedCPT retrieval models.
- DailyMed/openFDA labels.
- ClinicalTrials.gov records.

Outputs:

- Retrieval index with PMID, PMCID, DOI, SPL set ID, NDC, NCT ID, source URL, retrieval timestamp, and source version.
- Citation resolver for user-facing and review-queue outputs.

Acceptance criteria:

- Retrieved evidence can be traced to source IDs and retrieval timestamp.
- If no citation is available, no evidence claim is generated.
- ClinicalTrials.gov records are labeled as registry data, not peer-reviewed outcomes.

Required tests:

- Retrieval quality golden set.
- Citation correctness tests.
- Missing-citation refusal tests.
- ClinicalTrials.gov status-vs-outcome distinction tests.

Safety constraints:

- No freeform medical-authority answer from model memory.
- No regulatory status inferred without retrieved source.
- Label content is presented as label evidence and risk context, not instructions.

Open licensing questions:

- PMC full-text reuse by article license.
- MedCPT attribution/disclaimer requirements.

## Phase 3: Entity Normalization

Goals:

- Normalize substances, drugs, biomarkers, diseases, targets, pathways, trials, labels, and chemicals before synthesis.
- Reduce alias drift in guardrail and graph logic.

Inputs:

- RxNorm/RxNav/RxClass/MED-RT.
- PubChem, ChEMBL, Reactome, PubTator3.
- scispaCy, Stanza, HunFlair2, SapBERT test candidates.

Outputs:

- Deterministic entity registry with canonical IDs, aliases, source IDs, match confidence, and review status.
- Entity-linking candidates for human review.

Acceptance criteria:

- Exact and alias matches produce stable canonical IDs.
- Ambiguous matches route to review instead of promotion.
- UMLS-derived or restricted vocabulary fields are marked with redistribution constraints.

Required tests:

- Entity normalization accuracy tests.
- Ambiguous alias review-routing tests.
- License-constrained field export tests.

Safety constraints:

- Entity linking cannot imply safety, efficacy, indication, or treatment suitability.
- High-risk category matches fail closed into warning/review paths.

Open licensing questions:

- UMLS/SNOMED-derived redistribution.
- SapBERT downstream use where UMLS synonyms are involved.

## Phase 4: Extraction Candidate Pipeline

Goals:

- Extract candidate claims, warnings, adverse events, mechanisms, targets, pathways, interactions, trial statuses, and supplement label claims.
- Keep candidates separate from canonical knowledge.

Inputs:

- PubTator3 annotations.
- BiomedBERT/PubMedBERT, BioLinkBERT, scispaCy, Stanza, HunFlair2.
- DailyMed/openFDA labels, PubMed, ClinicalTrials.gov, NIH DSLD.

Outputs:

- Candidate claim records with source span, citation, extractor, confidence, license tag, and review status.
- Review queues for evidence, safety, regulatory, and supplement claims.

Acceptance criteria:

- Every candidate has source span and citation metadata.
- Supplement label claims are explicitly labeled as claims, not evidence.
- FAERS-derived candidates are flagged as adverse-event signals only.

Required tests:

- Claim extraction quality tests.
- Source span and citation presence tests.
- Supplement label/evidence separation tests.
- FAERS caveat tests.

Safety constraints:

- Candidate extraction can support evidence-context recommendations, risk-aware educational guidance, tracking prompts, and uncertainty labels. It cannot generate dosing, diagnosis, prescribing, injection, cycle, PCT, sourcing, treatment instructions, or start/stop/taper/escalation guidance.
- Candidates remain admin-only until reviewed.

Open licensing questions:

- Label text redistribution terms for source displays.
- NatMed/DrugBank integration boundaries if used in extraction.

## Phase 5: Evidence-Tier And Source-Quality Classifiers

Goals:

- Classify source quality, study design, evidence tier, human relevance, and claim strength.
- Provide transparent confidence overlays.

Inputs:

- PubMed metadata, PMC full text where licensed, ClinicalTrials.gov, DailyMed, NIH ODS, ChEMBL, FAERS.
- Narrow classifier candidates based on PubMedBERT/BioLinkBERT/SciBERT-style models.

Outputs:

- Evidence-tier labels.
- Source-quality labels.
- Reason codes and source-type distinctions.

Acceptance criteria:

- In vitro, animal, registry, adverse-event signal, label, observational, randomized, and review evidence are separated.
- ChEMBL in vitro/bioactivity data is not promoted as clinical outcome evidence.
- Trial registry status is not promoted as outcome evidence.

Required tests:

- Evidence-tier classification tests.
- Source-quality classification tests.
- ChEMBL in vitro versus clinical distinction tests.
- ClinicalTrials.gov status versus outcome-evidence tests.

Safety constraints:

- Evidence scores can support source-quality warnings and risk-aware educational guidance. They cannot imply medical authority, diagnosis, prescribing, individualized dosing, or treatment planning.
- Low or conflicting evidence must not be upgraded by LLM synthesis.

Open licensing questions:

- Training/evaluation use of licensed or non-commercial datasets.

## Phase 6: High-Risk Category Guardrail Engine

Goals:

- Use deterministic rules before LLM synthesis for high-risk categories.
- Enforce hard refusals and warning-first behavior.

Inputs:

- WADA Prohibited List.
- OPSS warnings.
- FDA SARM/bodybuilding warnings.
- RxNorm/RxClass/MED-RT.
- DailyMed/openFDA labels.
- ClinicalTrials.gov investigational status.

Outputs:

- High-risk category tags.
- Refusal reasons.
- Warning-first evidence summaries.
- Suppression rules for prohibited outputs.

Acceptance criteria:

- SARM, SERM, investigational peptide, PCT, injection, dosing, sourcing, and prescription-only prompts are detected.
- Regulatory status comes from retrieved deterministic sources, never LLM memory.
- Guardrails run before any LLM synthesis and may only downgrade or suppress.

Required tests:

- High-risk category detection tests.
- Refusal behavior tests.
- Unsafe content suppression tests.
- Regulatory status classification tests.
- WADA banned-in-sport classification tests.

Safety constraints:

- No diagnosis, prescribing, individualized dosing, treatment plans, start/stop/taper/escalation instructions, cycles, PCT, injection instructions, sourcing, or claims that investigational or gray-market substances are safe or effective for human use.
- No high-risk protocol-builder flows.

Open licensing questions:

- WADA and Global DRO reuse/display terms.

## Phase 7: Human Review And Canonical Promotion

Goals:

- Promote only reviewed claims, warnings, classifications, and graph edges into canonical knowledge.
- Preserve reviewer decisions and audit trails.

Inputs:

- Extraction candidates.
- Evidence-tier outputs.
- Source-quality outputs.
- Guardrail outputs.
- License status.

Outputs:

- Review queue items.
- Approved/rejected canonical artifacts.
- Reviewer notes, decision timestamp, source spans, and artifact versions.

Acceptance criteria:

- No unreviewed candidate appears as canonical user-facing knowledge.
- Rejected candidates remain auditable but not visible.
- Canonical artifacts include citations, license status, reviewer, and version.

Required tests:

- Review-gate tests.
- Promotion/rejection state transition tests.
- Canonical output provenance tests.

Safety constraints:

- High-risk, regulatory, safety, supplement, label, adverse-event, and evidence-grade claims always require review.

Open licensing questions:

- Whether licensed-source-derived reviewer notes can be displayed, exported, or redistributed.

## Phase 8: Evaluation Harness And Regression Tests

Goals:

- Build golden tests for retrieval, citation, normalization, extraction, classification, guardrails, refusal behavior, and license boundaries.
- Prevent safety and evidence regressions.

Inputs:

- Golden examples listed in `docs/testing/knowledge-engine-evaluation-harness.md`.
- Reviewed source fixtures.

Outputs:

- Automated eval suite.
- Regression reports.
- Failure triage categories.

Acceptance criteria:

- Unsafe generations fail CI.
- Citationless claims fail CI.
- License-boundary violations fail CI.
- Golden examples cover GLP-1 labels, FAERS caveats, PubMed retrieval, supplement claim separation, SARMs, SERMs, investigational peptides, WADA, ChEMBL, and ClinicalTrials.gov.

Required tests:

- All evaluation harness categories.
- Forbidden-output scanner.
- License boundary enforcement.

Safety constraints:

- Test prompts must include allowed guidance requests and adversarial requests for dosing, diagnosis, treatment plans, start/stop/taper/escalation instructions, cycles, PCT, injection, and sourcing.

Open licensing questions:

- Whether licensed datasets can be used in CI fixtures.

## Phase 9: Licensed Data Pilots

Goals:

- Evaluate commercial or restricted assets where public coverage is weak.
- Keep pilots isolated behind compliance boundaries.

Inputs:

- DrugBank.
- NatMed.
- Potentially Global DRO permitted access.
- UMLS/SNOMED/MED-RT expanded access.

Outputs:

- Pilot evaluation report.
- License-compatible display/export rules.
- Integration decision record.

Acceptance criteria:

- No licensed content is redistributed outside permitted terms.
- User-facing output rules are documented before implementation.
- Licensed-source fields are separable from open-source fields.

Required tests:

- License boundary tests.
- Export suppression tests.
- Source attribution/display tests.

Safety constraints:

- Licensed interactions or effectiveness ratings are risk flags and review inputs, not automated medical authority.

Open licensing questions:

- Commercial pricing, API terms, display requirements, derived-data ownership, retention, and audit rights.

## Phase 10: Production Hardening

Goals:

- Operationalize ingestion, refresh, review, audit, monitoring, and rollback.
- Keep runtime deterministic and evidence bounded.

Inputs:

- Approved registry.
- Retrieval indexes.
- Entity registry.
- Canonical artifacts.
- Evaluation harness.

Outputs:

- Scheduled refresh jobs.
- Stale-source alerts.
- License expiry alerts.
- Review SLA dashboards.
- Artifact versioning and rollback.
- Production refusal and safety telemetry.

Acceptance criteria:

- Stale regulatory or label sources are detected before user-facing status display.
- Runtime responses can be traced to artifact version and citations.
- Failed refreshes do not silently update canonical knowledge.
- Safety guardrail failures block release.

Required tests:

- Refresh and stale-source tests.
- Artifact rollback tests.
- Runtime provenance tests.
- Release-gate evaluation suite.

Safety constraints:

- Runtime consumes reviewed artifacts and may produce constrained BioStack guidance from them; it does not invent claims or medical authority.
- LLM calls cannot create canonical knowledge at request time.

Open licensing questions:

- Long-term retention and audit obligations for licensed-source-derived artifacts.
