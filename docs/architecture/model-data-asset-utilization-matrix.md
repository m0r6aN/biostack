# Model And Data Asset Utilization Matrix

Date: 2026-06-18
Source research: `research/protocol-intelligence/biostack-ai-model-data-asset-research-memo.md`

This matrix defines how model and data assets support BioStack's Evidence-Guided Protocol Intelligence Engine.

Status values:

- Use now: approved for evidence-guided protocol intelligence planning, subject to source terms and normal engineering validation.
- Test next: evaluate in benchmarks or offline prototypes before production commitment.
- License review: useful, but legal/compliance review is required before production use or redistribution.
- Benchmark only: internal evaluation, red-team, or comparison only.
- Avoid: do not integrate into the production knowledge engine without a new architecture decision.

User-facing means whether outputs derived from the asset can appear to users after retrieval, citation, guardrails, uncertainty labeling, and review. It permits evidence-informed educational guidance and risk-aware decision support where allowed. It never permits medical authority, diagnosis, prescribing, individualized dosing, treatment planning, start/stop/taper/escalation instructions, cycles, PCT, injection instructions, or sourcing guidance.

| Asset | Role in BioStack | Recommended usage | Status | Safety risk | Licensing risk | Data freshness concern | User-facing output allowed | Human review before canonical promotion |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PubMed/PMC | Literature corpus and citation backbone | Retrieve papers, abstracts, metadata, and licensed full text; store PMID/PMCID/DOI provenance | Use now | Medium: papers vary in quality and human relevance | Medium: PMC full text has article-level licenses | Medium: ingest cadence and retractions matter | Yes, as cited evidence summaries | Yes |
| MedCPT | Biomedical dense retrieval and reranking | PubMed retrieval, query/article embeddings, reranking | Use now | Low: relevance is not truth | Low-medium: attribution/disclaimer review | Low: model is static but retrieval corpus refreshes | Indirectly, as retrieval support | Yes for promoted claims |
| DailyMed/openFDA drug labels | Drug label warnings, adverse reactions, contraindications | Parse label sections with SPL/openFDA IDs and label versions | Use now | Medium: label text is not personalized guidance | Low-medium: public data, openFDA caveats | Medium: label updates must refresh | Yes, label-context only | Yes |
| openFDA/FAERS | Adverse-event signal source | Detect ambiguity and post-market signals; never causality or incidence | Use now | High: reporting bias, duplicates, no denominator | Low-medium: public API with disclaimers | Medium: quarterly/periodic refresh | Yes, with signal caveats only | Yes |
| ClinicalTrials.gov | Trial registry and status source | Trial status, intervention metadata, research-gap detection | Use now | Medium: registry is not peer-reviewed outcome evidence | Low | Medium: status changes | Yes, as registry status only | Yes |
| PubTator3 | Literature entity and relation annotations | Bootstrap entity/relation candidates tied to PMIDs/PMCIDs | Use now | Medium: automated annotations can be wrong | Low-medium: respect source text restrictions | Medium: depends on NCBI updates | Candidate output only after review | Yes |
| RxNorm/RxNav/RxClass/MED-RT | Drug naming and class normalization | Resolve ingredients, products, classes, and guardrail categories | Use now | Medium: not a full DDI or prescribing system | Medium: UMLS/source vocabulary constraints may apply | Medium: drug terminology updates | Yes, normalized names/classes | Yes |
| ChEMBL | Compound, target, assay, and bioactivity data | Mechanism and in vitro/assay evidence with source distinction | License review | Medium: in vitro data can be overstated | High: CC BY-SA obligations | Medium: release-based updates | Yes, if clinical distinction is explicit | Yes |
| Reactome | Curated pathway graph | Pathway context and mechanism graph enrichment | Use now | Low-medium: pathway association is not clinical proof | Low: CC0 data | Low-medium: release-based updates | Yes, pathway context only | Yes |
| Open Targets | Target-disease-drug evidence graph | Target and disease evidence features, graph enrichment | Use now | Medium: drug-discovery evidence is not protocol advice | Low: CC0 platform data | Medium: release-based updates | Yes, evidence graph context | Yes |
| PubChem | Chemical identifiers, synonyms, structures, assays | Identifier resolution and chemical enrichment | Use now | Medium: submitted-source provenance varies | Low-medium: source-level licenses vary | Medium: frequent updates | Yes, identifiers and sourced facts | Yes |
| NIH ODS Fact Sheets | Supplement evidence summaries | Supplement evidence baseline and safety summary source | Use now | Medium: summaries can be misread as advice | Low | Medium: fact sheet updates vary | Yes, cited evidence summaries | Yes |
| NIH DSLD | Dietary supplement label database | Label ingredient and claim inventory; separate labels from evidence | Use now | High: labels are not validated evidence | Low-medium: public API terms | Medium: labels can be stale/incomplete | Yes, label context only | Yes |
| WADA Prohibited List | Anti-doping regulatory status | Deterministic banned-in-sport classification by list year | Use now | High if stale or inferred | Low-medium: document terms | High: annual and interim updates | Yes, with list year/source | Yes |
| OPSS | Supplement and high-risk substance safety resource | SARM/supplement warning references and guardrail copy | Use now | Medium: web content is not structured evidence | Low-medium: reuse terms review | Medium | Yes, cited warning context | Yes |
| DrugBank | Drug data, targets, interactions | Licensed pilot for DDI and structured drug intelligence | License review | High: can become clinical decision support | High: commercial license and redistribution limits | Medium: subscription updates | Limited, license-compliant risk flags only | Yes |
| NatMed | Supplement evidence and interactions | Licensed pilot for supplement interactions/effectiveness | License review | High: clinical-advice boundary | High: subscription/API/display terms | Medium: subscription updates | Limited, license-compliant summaries only | Yes |
| SapBERT | Biomedical entity linking embeddings | Alias/entity normalization benchmark and potential resolver | Test next | Medium: wrong linking creates bad guardrails | High: UMLS-derived use needs review | Low: model static; vocabulary refresh separate | Indirectly, normalized IDs only | Yes |
| BiomedBERT/PubMedBERT | Biomedical encoder | Narrow classifiers and extraction models | Test next | Medium: classifier errors can overstate claims | Low-medium: model license review | Low: model static | Indirectly through reviewed classifications | Yes |
| BioLinkBERT | Citation/link-aware biomedical encoder | Relation extraction and evidence graph candidate scoring | Test next | Medium | Low-medium: model license review | Low | Indirectly through reviewed candidates | Yes |
| SPECTER2 | Scientific document embeddings | Related-paper clustering and research-gap discovery | Test next | Low-medium: similarity is not evidence quality | Low-medium: model license review | Low | Indirectly, not as claim authority | Yes |
| scispaCy | Biomedical NLP pipeline | Local NER, abbreviation detection, entity candidates | Use now | Medium: entity linking can be wrong | Medium if UMLS linker used | Low-medium: model package updates | Candidate output only after review | Yes |
| Stanza biomedical models | Biomedical NER and syntax | NER/syntax fallback and extraction benchmark | Test next | Medium | Low-medium: per-model license review | Low-medium | Candidate output only after review | Yes |
| HunFlair2 | Biomedical NER/normalization | NER benchmark for chemicals, genes, diseases, species | Test next | Medium | Low-medium: model artifact review | Low-medium | Candidate output only after review | Yes |
| BioMistral | Biomedical LLM | Internal benchmark and red-team only | Benchmark only | High: hallucination and medical-advice drift | Medium: model/source license review | High: static model | No | Not promotable without reviewed sources |
| Meditron | Medical LLM | Internal benchmark and red-team only | Benchmark only | High: access-gated medical model, static cutoff | High: Llama 2 and source terms | High | No | Not promotable without reviewed sources |
| BioGPT | Biomedical generative model | Offline extraction comparison only | Benchmark only | High: generation hallucination | Low-medium: model license review | High | No | Not promotable without reviewed sources |
| GatorTron | Clinical transformer | Clinical NLP benchmark in controlled research only | Benchmark only | High: clinical note domain and PHI risk | High: access/provenance review | High | No | Not promotable without reviewed sources |
| MIMIC-IV/PhysioNet | Credentialed clinical data | Avoid product training and third-party API workflows unless a research sandbox is approved | Avoid | Critical: PHI/clinical-care boundary | High: DUA and third-party sharing limits | Medium | No | Not for canonical product knowledge |
| SemMedDB | Extracted PubMed predications | Research benchmark only unless UMLS/non-commercial issues are resolved | Benchmark only | High: noisy extracted triples | High: non-commercial/UMLS constraints | Medium | No | Yes if ever licensed |
| SIDER | Side-effect resource | Internal side-effect benchmark only unless license posture fits | Benchmark only | Medium-high: old/non-commercial side-effect associations | High: CC BY-NC-SA | High: older dataset | No unless license permits | Yes |
| OffSIDES/TwoSIDES/nSIDES | FAERS-derived signal databases | Pharmacovigilance benchmark and signal comparison | Test next | High: signals are not causality | Medium-high: release-specific license review | Medium-high | Limited signal caveats only | Yes |
| Global DRO | Anti-doping medication lookup | Manual reference or permitted/contracted lookup only; do not scrape | License review | High if stale or overgeneralized | High: terms restrict reuse/scraping | High: country/product status changes | Limited, terms-compliant status only | Yes |

## Output Rule

No asset in this matrix authorizes BioStack to act as a medical authority, prescriber, diagnosis engine, or treatment planner. User-facing outputs may include BioStack guidance, evidence context, risk signals, source-quality warnings, what to track, what changed, what is uncertain, and clinician-escalation suggestions when grounded in cited sources, guardrail checks, clear uncertainty, and human-reviewed canonical promotion where the output is a knowledge claim.
