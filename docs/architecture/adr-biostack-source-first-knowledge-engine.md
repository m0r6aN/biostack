# ADR: BioStack Evidence-Guided Protocol Intelligence Engine

Date: 2026-06-18
Status: Accepted for implementation planning
Source research: `research/protocol-intelligence/biostack-ai-model-data-asset-research-memo.md`

## Decision

BioStack will build an Evidence-Guided Protocol Intelligence Engine. The implementation remains source-first internally, but the product posture is guidance-oriented: BioStack helps users interpret protocol context, risk signals, source quality, biomarker observations, symptom timelines, regulatory status, and uncertainty.

BioStack uses AI to retrieve, classify, extract, cite, organize, interpret, and synthesize evidence into educational protocol intelligence. BioStack rejects autonomous medical authority, prescribing, diagnosis, and treatment planning. BioStack may provide evidence-informed educational guidance, risk-aware recommendations, and decision support when outputs are grounded in cited sources, clear uncertainty, safety boundaries, and user-observable context.

The core architecture is curated source ingestion, deterministic normalization, retrieval, source-aware extraction/classification, evidence-tier scoring, risk classification, protocol interpretation, human review, and canonical promotion. A constrained general LLM may synthesize cited evidence, identify uncertainty, propose review queues, and generate allowed educational guidance only after retrieval and guardrail checks.

## Context

BioStack needs protocol intelligence across biomedical literature, drug labels, supplement labels, regulatory sources, anti-doping lists, adverse-event signals, clinical trials, chemical databases, pathway resources, user observations, symptom timelines, and biomarker signals. The research memo concludes that the durable value is not a broad medical instruction model. The value is evidence-guided protocol intelligence: a governed evidence supply chain plus source-aware interpretation, pattern detection, risk classification, and uncertainty-aware educational guidance.

The product must remain evidence-bounded, uncertainty-aware, and educational. It may provide evidence-context recommendations, risk-aware educational guidance, tracking and baseline recommendations, source-quality warnings, regulatory-status warnings, protocol complexity warnings, side-effect ambiguity analysis, research-gap alerts, clinician-escalation suggestions, safer decision pathways, and statements that a claim is speculative, weakly supported, high risk, or unsupported.

It must not provide diagnosis, prescribing, individualized medical dosing, treatment plans, start/stop/taper/escalation instructions, SARM or SERM cycle design, post-cycle therapy guidance, peptide injection instructions, sourcing guidance, claims that investigational or gray-market substances are safe or effective for human use, individualized medical-device claims, or open medical authority without strict retrieval, citation, and refusal controls.

## Why Standalone Biomedical LLMs Are Not The Core Strategy

Standalone biomedical LLMs are not acceptable as BioStack's primary knowledge engine because they:

- Can hallucinate citations, regulatory status, clinical certainty, and contraindications.
- Optimize fluent answers rather than auditable evidence provenance.
- Do not provide current labels, trial status, WADA status, or supplement source freshness by memory.
- Can drift into prohibited medical authority, dosing, cycles, injection, sourcing, start/stop/taper/escalation instructions, or treatment planning.
- Cannot satisfy license boundaries for sources such as DrugBank, NatMed, UMLS-derived assets, SIDER, SemMedDB, MIMIC-IV, or Global DRO by model output alone.
- Are difficult to regress-test against safety, citation, freshness, and human-review requirements.

Open medical LLMs such as BioMistral, Meditron, BioGPT, and GatorTron may be benchmarked or red-teamed internally. They are not approved as autonomous user-facing medical authorities. They may only support cited, constrained, uncertainty-aware educational synthesis after retrieval and deterministic guardrails.

## Recommended Evidence-Guided Architecture

1. Source registry
   - Track source name, maintainer, URL, license, allowed use, disallowed use, refresh cadence, retrieval method, citation identifiers, redistribution constraints, and human-review requirements.

2. Ingestion and provenance foundation
   - Ingest approved public and licensed sources through permitted APIs, downloads, or manually approved access paths.
   - Preserve retrieval timestamp, source version, stable source IDs, raw source pointers, parsed spans, license status, and transformation history.

3. Retrieval and citation backbone
   - Use MedCPT for PubMed-oriented retrieval.
   - Use stable identifiers where available: PMID, PMCID, DOI, NCT number, NDC, SPL set ID, RxCUI, ChEMBL ID, PubChem CID, Reactome ID, WADA list year, and source URL.

4. Entity normalization
   - Use deterministic terminology and alias resolution before model synthesis.
   - Approved inputs include RxNorm/RxNav/RxClass/MED-RT, PubChem, ChEMBL identifiers, Reactome IDs, PubTator3 annotations, scispaCy, Stanza biomedical models, HunFlair2, and SapBERT where license posture allows.

5. Source-aware extraction and classification
   - Extract candidate claims, warnings, adverse events, contraindications, interactions, trial status, evidence type, study design, mechanism, target, pathway, and regulatory status.
   - Treat extraction output as candidate evidence, not canonical truth.

6. Evidence-tier and source-quality scoring
   - Classify claim support by source type, study design, human relevance, regulatory status, label status, adverse-event limitations, and license constraints.
   - Distinguish label claims, registry entries, adverse-event signals, in vitro findings, animal evidence, observational evidence, randomized human outcomes, and regulatory labeling.

7. High-risk category guardrail engine
   - Run deterministic rules before LLM synthesis for SARMs, SERMs, investigational peptides, prescription-only substances, compounded GLP-1 products, banned-in-sport substances, gray-market substances, dosing/cycle/PCT/injection/sourcing requests, and individualized medical claims.

8. Human review and canonical promotion
   - Human review is required before extracted edges, warning copy, evidence grades, source-quality grades, supplement interpretations, regulatory interpretations, or high-risk category classifications become canonical.

9. Constrained LLM synthesis and guidance
   - A general LLM may summarize only retrieved sources with citations.
   - It may generate BioStack guidance, evidence context, risk signals, what to track, what changed, what is uncertain, and clinician-escalation suggestions when grounded in cited sources and user-observable context.
   - If retrieval or source IDs are missing, the system must say the evidence is unavailable or uncertain rather than infer regulatory, safety, or clinical status from model memory.

## Allowed Guidance Patterns

BioStack may provide:

- Evidence-context recommendations.
- Risk-aware educational guidance.
- Tracking and baseline recommendations.
- Source-quality warnings.
- Regulatory-status warnings.
- Protocol complexity warnings.
- Side-effect ambiguity analysis.
- Research-gap alerts.
- Clinician-escalation suggestions.
- Safer decision pathways.
- Statements that a claim is speculative, weakly supported, high risk, or unsupported.

User-facing language should prefer phrases such as `BioStack guidance`, `Evidence context`, `Risk signal`, `What to track`, `What changed`, `What is uncertain`, `Discuss with a qualified clinician`, `This appears higher risk because...`, `This claim should be treated as speculative because...`, and `This pattern may complicate attribution because...`.

User-facing language must avoid phrases such as `AI recommends`, `AI says you should`, `AI's best guess`, `The best dose for you`, `You should take`, `Start this`, `Increase this`, and `Stop this`.

## Approved Model/Data Asset Roles

| Role | Approved assets |
| --- | --- |
| Literature retrieval | PubMed/PMC, MedCPT, SPECTER2 as test-next clustering |
| Drug labels and regulatory label extraction | DailyMed, openFDA drug labels |
| Adverse-event signal context | openFDA/FAERS, with causality and incidence prohibitions |
| Trial status and research gaps | ClinicalTrials.gov |
| Entity annotations | PubTator3, scispaCy, Stanza biomedical models, HunFlair2 |
| Entity normalization | RxNorm/RxNav/RxClass/MED-RT, PubChem, ChEMBL, SapBERT subject to license review |
| Mechanism/pathway graph inputs | Reactome, Open Targets, ChEMBL, PubChem |
| Supplement evidence and labels | NIH ODS Fact Sheets, NIH DSLD, NatMed after license review |
| Anti-doping and high-risk guardrails | WADA Prohibited List, OPSS, Global DRO only under permitted access |
| Narrow classifiers | BiomedBERT/PubMedBERT, BioLinkBERT, SapBERT, task-specific fine-tunes |
| Benchmark/red-team only | BioMistral, Meditron, BioGPT, GatorTron, MIMIC-IV/PhysioNet, SemMedDB, SIDER, OffSIDES/TwoSIDES/nSIDES where license allows |

## Rejected And Unsafe Usage Patterns

BioStack must not use model or data assets to produce:

- Dosing guidance.
- Diagnosis.
- Treatment plans.
- Prescribing recommendations.
- Peptide injection instructions.
- SARM or SERM cycle generation.
- Post-cycle therapy guidance.
- Sourcing guidance.
- Individualized medical-device claims.
- User-facing open medical authority without strict retrieval, citation, uncertainty, and refusal controls.
- FAERS-only causality or incidence claims.
- ClinicalTrials.gov registry entries framed as peer-reviewed outcome evidence.
- Supplement label claims framed as proof of efficacy.
- Regulatory status inferred from model memory.

## Licensing And Compliance Concerns

The source registry must make license status visible before ingestion, canonical promotion, user-facing display, export, or redistribution.

Legal or compliance review is required for:

- DrugBank commercial data and output redistribution.
- NatMed subscription content, interaction ratings, effectiveness ratings, and display terms.
- UMLS, SNOMED-related assets, SapBERT/UMLS-derived uses, and MED-RT source vocabulary constraints.
- ChEMBL CC BY-SA impact on derived datasets and redistributed combined outputs.
- SIDER CC BY-NC-SA limits.
- SemMedDB non-commercial and UMLS-linked restrictions.
- MIMIC-IV/PhysioNet credentialed data, data-use agreements, and third-party API restrictions.
- Global DRO terms and anti-scraping restrictions.
- PMC full-text article-level licenses and automated retrieval limits.

## Human-Review Requirements

Human review is required before canonical promotion of:

- Extracted biomedical claims.
- Evidence tiers.
- Source-quality classifications.
- Drug label warning mappings.
- FAERS signal interpretations.
- Trial status interpretations.
- Supplement evidence and label-claim separation.
- Regulatory status mappings.
- WADA or banned-in-sport status outputs.
- High-risk category classifications.
- Relationship graph edges.
- User-facing warning or ambiguity copy.

Unreviewed, rejected, draft, or needs-review candidates are admin-only and cannot appear as canonical user-facing knowledge.

## Consequences And Tradeoffs

This decision favors correctness, provenance, reviewability, and safety over broad answer coverage. It will produce more `Unknown` or `not enough cited evidence` outcomes than a freeform chatbot. It also requires source registry operations, license tracking, human review workflows, and regression tests.

The tradeoff is intentional. BioStack becomes an evidence-informed protocol intelligence system with auditable guidance, risk context, and uncertainty-aware decision support rather than a passive research database or a medical authority.

## Retatrutide Handling Example

Retatrutide is treated as investigational and not FDA-approved for public use. Trial exposure data may be cited as research context, not user instructions. Gray-market products trigger source-quality and identity-risk warnings. Influencer claims are classified as market signal only. Any planned exposure beyond cited trial context triggers high-risk uncertainty language, clinician-escalation suggestions, and refusal of dosing, sourcing, injection, or treatment-planning instructions.
