# BioStack AI Model and Data Asset Research Memo

Date: 2026-06-18

## Executive Position

BioStack should not bet on a medical chatbot or a standalone biomedical LLM as the core knowledge engine. The safer and more durable path is a source-first architecture: curated biomedical/regulatory/supplement corpora, domain retrieval models, deterministic entity linking, extraction/classification pipelines, and a constrained general LLM used only to synthesize cited evidence and generate human-review queues.

The highest-value assets are not generative clinical models. They are retrieval and evidence substrates: PubMed/PMC, ClinicalTrials.gov, DailyMed/openFDA, FAERS/openFDA, PubTator3, MedCPT, RxNorm/RxNav, ChEMBL, Reactome, Open Targets, NIH ODS/DSLD, and licensed interaction/supplement data where public coverage is weak.

## Safety Boundary

Recommended uses are limited to evidence retrieval, source citation, claim classification, evidence-tier grading, risk/warning detection, side-effect ambiguity analysis, substance-to-pathway mapping, regulatory status awareness, source-quality classification, research-gap detection, and human-review assistance.

Do not use these assets to generate dosing, diagnosis, prescribing, treatment plans, SARM/SERM cycles, post-cycle therapy, peptide injection instructions, sourcing, or individualized medical-device claims.

## Model and Data Asset Inventory

| Name | Maintainer | Type | Source basis | Status / license | Strengths | Weaknesses and safety concerns | BioStack score | Recommended usage | Official sources |
|---|---|---|---|---|---|---|---|---|---|
| BiomedBERT / PubMedBERT | Microsoft Research | Biomedical BERT encoder | PubMed abstracts and PMC full text | Open weights; MIT on Hugging Face | Strong biomedical text classification, NER, relation extraction base | Not a citation engine or clinical reasoning model | High | Fine-tune classifiers; extraction; benchmark | [HF](https://huggingface.co/microsoft/BiomedNLP-BiomedBERT-base-uncased-abstract-fulltext), [BLURB](https://microsoft.github.io/BLURB/models.html) |
| BioBERT | DMIS Lab | Biomedical BERT encoder | PubMed and PMC biomedical text | Open research code/weights; verify downstream model license per checkpoint | Mature baseline for NER, relation extraction, QA | Older than PubMedBERT/BioLinkBERT; no inherent provenance/citation control | Medium | Benchmark; extraction baseline | [GitHub](https://github.com/dmis-lab/biobert) |
| SciBERT | AllenAI | Scientific BERT encoder | Semantic Scholar scientific papers; large biomedical share | Open model; Apache-style ecosystem, verify model card | Good general scientific document understanding | Less biomedical-specific than PubMedBERT | Medium | Source-quality and methods-section classification | [GitHub](https://github.com/allenai/scibert) |
| BioLinkBERT | Michihiro Yasunaga et al. | Biomedical/scientific BERT encoder | Linked documents/citations | Open weights; Apache-2.0 on common deployments | Uses document-link signal; good for citation-aware extraction | Still bounded by 512-token style encoder limits | High | Relationship extraction; evidence graph edge candidates | [GitHub](https://github.com/michiyasunaga/LinkBERT), [HF](https://huggingface.co/michiyasunaga/BioLinkBERT-base) |
| SapBERT | Cambridge LTL | Biomedical entity embedding/linking | UMLS synonym self-alignment over PubMedBERT variants | MIT code; HF models include Apache-2.0 variants; UMLS-derived use needs legal review | Excellent synonym/entity normalization | UMLS/SNOMED source licensing can constrain redistribution | High | Alias resolution for substances, biomarkers, diseases | [GitHub](https://github.com/cambridgeltl/sapbert), [HF](https://huggingface.co/cambridgeltl/SapBERT-from-PubMedBERT-fulltext) |
| MedCPT | NCBI | Biomedical dense retriever and reranker | PubMed search logs and article/query contrastive training | Public NCBI model; license requires attribution/disclaimer review | Purpose-built for PubMed-style retrieval | Retrieval only; does not grade truth | High | Primary PubMed retrieval stack; query/article embeddings; reranking | [GitHub](https://github.com/ncbi/MedCPT), [HF query encoder](https://huggingface.co/ncbi/MedCPT-Query-Encoder) |
| SPECTER2 | AllenAI / Semantic Scholar | Scientific document embeddings | Scientific papers and task adapters | Open model; check HF/model license | Good for scientific document similarity and clustering | General scientific, not supplement/regulatory-specific | Medium | Research-gap clustering; related-paper discovery | [AllenAI blog](https://allenai.org/blog/specter2-adapting-scientific-document-embeddings-to-multiple-fields-and-task-formats-c95686c06567), [GitHub](https://github.com/allenai/SPECTER2) |
| BioGPT | Microsoft | Biomedical generative model | PubMed biomedical literature | MIT; model weights included | Useful benchmark for biomedical generation and relation extraction | High hallucination risk if used as answer generator | Medium | Offline benchmark; extraction experiments only | [GitHub](https://github.com/microsoft/biogpt), [HF](https://huggingface.co/microsoft/BioGPT-Large) |
| BioMistral-7B | BioMistral team | Biomedical LLM | Mistral continued pretraining on PMC OA | Open; license and PMC OA source mix require review | Useful biomedical reasoning benchmark | Model card warns against production medical use; citation hallucination risk | Low-medium | Internal eval and red-team comparison, not user-facing | [HF](https://huggingface.co/BioMistral/BioMistral-7B) |
| Meditron | EPFL LLM team | Medical LLM | Llama 2 continued pretraining on PubMed, guidelines, RedPajama | Llama 2 community license for model; Apache-2.0 code | Medical-domain LLM baseline | Access-gated; static 2023 cutoff; model card warns against direct production impact | Low-medium | Benchmark and safety red-team only | [HF](https://huggingface.co/epfl-llm/meditron-7b), [GitHub](https://github.com/epfllm/meditron) |
| GatorTron | UF/NVIDIA | Clinical transformer encoder | De-identified clinical notes, MIMIC-III, PubMed, WikiText | Model access varies; clinical data provenance requires review | Strong clinical NLP for notes | Clinical-note domain may push BioStack toward care delivery; PHI risk if fine-tuned on user data | Medium | Internal classifier benchmark for symptom/lab text only | [HF](https://huggingface.co/UFNLP/gatortron-base), [Paper](https://pmc.ncbi.nlm.nih.gov/articles/PMC9792464/) |
| scispaCy | AllenAI | Biomedical NLP pipeline | Scientific/biomedical parsing and entity models | Apache-2.0 | Practical local NER, abbreviation, UMLS linker options | Entity linking can involve UMLS constraints | High | Deterministic preprocessor; entity candidates | [GitHub](https://github.com/allenai/scispacy), [Docs](https://allenai.github.io/scispacy/) |
| Stanza biomedical models | Stanford NLP | Biomedical and clinical NLP models | Biomedical literature and clinical NER corpora | Open-source library; model licenses need per-package review | Reliable NER/syntax pipeline | Clinical models may include sensitive-domain assumptions | Medium | NER fallback; syntax extraction | [Docs](https://stanfordnlp.github.io/stanza/available_biomed_models.html) |
| HunFlair2 | Humboldt / Flair | Biomedical NER and normalization | Biomedical NER corpora and LinkBERT-style models | Open-source Flair ecosystem; verify model artifacts | Strong NER for genes, chemicals, diseases, species, cell lines | Still extraction, not claim truth | Medium | Entity extraction benchmark | [Docs](https://flairnlp.github.io/flair/master/tutorial/tutorial-hunflair2/overview.html), [Paper](https://academic.oup.com/bioinformatics/article/40/10/btae564/7762634) |
| PubTator3 | NCBI | AI-annotated literature resource | PubMed and PMC OA entity/relation annotations | Public NCBI service; respect PMC license restrictions | Fast entity/relation annotations for genes, diseases, chemicals, variants, species, cell lines | Automated annotations require confidence and human review | High | Entity graph bootstrap; relation candidates; citation linking | [PubTator3](https://www.ncbi.nlm.nih.gov/research/pubtator3/), [NAR](https://academic.oup.com/nar/article/52/W1/W540/7640526) |
| PubMed / NCBI E-utilities | NCBI/NLM | Literature API | PubMed citations/metadata | Public API; abstracts/citations are not a blanket full-text license | Canonical biomedical literature search | Full-text reuse depends on article/PMC license | High | Retrieval corpus, citation backbone | [NCBI APIs](https://www.ncbi.nlm.nih.gov/home/develop/api/) |
| PMC OA / PMC APIs | NCBI/NLM | Full-text corpus/API | PMC open-access subset and PMC content | License varies by article; automated retrieval restricted to approved services | Full-text methods/results extraction | License heterogeneity; bulk retrieval restrictions | High | Full-text extraction where license permits | [PMC developers](https://pmc.ncbi.nlm.nih.gov/tools/developers/) |
| ClinicalTrials.gov API | NLM | Trial registry/API | Registered clinical studies | Public API | Trial status, interventions, outcomes, sponsor metadata | Registry data is not peer-reviewed outcome evidence by itself | High | GLP-1, peptide, metabolic trial observability and research gaps | [API](https://clinicaltrials.gov/data-api/api) |
| DailyMed SPL | NLM/FDA | Drug labels/API/downloads | Structured Product Labeling | Public API/downloads | Current label sections: warnings, adverse reactions, contraindications | Label content can lag distributed products; not clinical advice | High | FDA label parser; boxed warning/risk taxonomy | [DailyMed API](https://dailymed.nlm.nih.gov/dailymed/app-support-web-services.cfm), [downloads](https://dailymed.nlm.nih.gov/dailymed/spl-resources-all-drug-labels.cfm) |
| openFDA drug labels | FDA | JSON drug-label API | FDA SPL transformed to JSON | Public API; not validated for clinical production | Easy JSON access; weekly updates | FDA warns not to rely on openFDA for medical care decisions | High | Label evidence ingestion with disclaimer | [Label API](https://open.fda.gov/apis/drug/label/), [API overview](https://open.fda.gov/apis/) |
| openFDA / FAERS | FDA | Adverse-event API | FDA adverse event reports | Public API; reports are signals, not causality | Useful for side-effect ambiguity and warning detection | Reporting bias, duplicate reports, no denominator, no causality | High | Pharmacovigilance signal overlay, not risk quantification alone | [Drug event API](https://open.fda.gov/apis/drug/event/), [FAERS](https://www.fda.gov/drugs/drug-approvals-and-databases/fda-adverse-event-reporting-system-faers-database) |
| RxNorm / RxNav / RxClass / MED-RT | NLM | Drug terminology/API | Standardized clinical drug concepts and classes | Public APIs; UMLS account may apply for full files | Ingredient/product normalization; drug class mapping | Not complete clinical DDI by itself; upstream sources vary | High | Substance normalization and class guardrails | [RxNorm API](https://lhncbc.nlm.nih.gov/RxNav/APIs/RxNormAPIs.html), [RxNav](https://lhncbc.nlm.nih.gov/RxNav/) |
| DrugBank | DrugBank | Commercial drug knowledge/API | Curated drug, target, interaction data | Commercial license required for use/redistribution/API | Strong DDI, mechanisms, structured drug data | Licensing cost; clinical decision support risk | High if licensed | Buy for DDI and structured drug intelligence; keep output non-prescriptive | [DrugBank](https://go.drugbank.com/), [license note](https://go.drugbank.com/about) |
| ChEMBL | EMBL-EBI | Bioactivity/drug discovery database | Curated molecule, assay, target, mechanism data | CC BY-SA 3.0 | Mechanism and target mapping for compounds | ShareAlike can contaminate combined derived datasets | High | MoA/pathway graph; potency/assay evidence with legal review | [ChEMBL](https://www.ebi.ac.uk/chembl/), [license](https://chembl.gitbook.io/chembl-interface-documentation/about) |
| PubChem | NCBI | Chemical database/API | Substance, compound, BioAssay submissions | Public; source-level licenses can vary | Chemical identifiers, synonyms, structures, bioassays | Submitted-source provenance varies; not evidence grading | High | Identifier/synonym resolver; chemical enrichment | [PUG REST](https://pubchem.ncbi.nlm.nih.gov/docs/pug-rest), [downloads](https://pubchem.ncbi.nlm.nih.gov/docs/downloads) |
| Open Targets Platform | Open Targets | Target-disease-drug evidence KG | Integrated public target validation data | CC0 1.0 | Strong target/disease/drug evidence integration | Drug-discovery framing, not consumer protocol advice | High | Pathway/target evidence graph and confidence features | [Platform](https://platform.opentargets.org/), [license](https://platform-docs.opentargets.org/licence) |
| Reactome | Reactome consortium | Curated pathway database | Manually curated peer-reviewed pathways | Data CC0 | High-quality pathway hierarchy | Human pathway scope; not supplement-specific | High | Substance-to-pathway and biomarker context | [Reactome](https://reactome.org/), [license](https://reactome.org/license) |
| PrimeKG | Harvard/Zitnik Lab | Precision medicine KG | 20 biomedical resources | MIT repo; upstream data constraints still need review | Broad disease/drug/pathway graph | Derived from multiple sources; not always current | Medium-high | Internal KG bootstrap and graph ML benchmark | [GitHub](https://github.com/mims-harvard/PrimeKG), [Nature](https://www.nature.com/articles/s41597-023-01960-3) |
| Hetionet | Het.io | Biomedical heterogeneous KG | 29 public biomedical databases | CC BY 4.0 / upstream restrictions | Simple graph substrate for drug repurposing style relations | Older and upstream-restricted in places | Medium | Benchmark graph schema; not canonical source | [Het.io](https://het.io/), [about](https://het.io/about/) |
| SemMedDB | NLM/LHNCBC | Extracted PubMed predications | SemRep over PubMed with UMLS concepts | Non-commercial use; UMLS license required | Massive literature-derived triples | Noisy extraction; license-constrained | Low-medium | Research benchmark only unless licensed | [LHNCBC](https://lhncbc.nlm.nih.gov/LHC-publications/pubs/SemMedDBaPubMedscalerepositoryofbiomedicalsemanticpredications.html) |
| UMLS / MeSH / SNOMED-related assets | NLM and source vocabularies | Biomedical terminology | Many vocabularies | Free access with license; individual not org license; source restrictions | Critical normalization backbone | Redistribution and source vocabulary restrictions | High with legal review | Internal normalization; do not redistribute controlled vocab content | [UMLS access](https://www.nlm.nih.gov/databases/umls.html), [Metathesaurus](https://www.nlm.nih.gov/research/umls/knowledge_sources/metathesaurus/index.html) |
| NIH ODS Fact Sheets | NIH ODS | Supplement evidence summaries | Federal supplement evidence reviews | Public federal resource | Conservative supplement evidence and safety summaries | Not complete interaction database | High | Supplement evidence baseline and source-quality gold set | [ODS fact sheets](https://ods.od.nih.gov/factsheets/list-all/) |
| NIH DSLD | NIH ODS | Dietary supplement label DB/API | U.S. supplement product labels | Public API; label claims are not validated evidence | Product ingredient/label claim inventory | Labels can be incomplete or misleading | High | Label-claim extraction and high-risk ingredient detection | [DSLD](https://ods.od.nih.gov/Research/Dietary_Supplement_Label_Database.aspx), [API](https://dsld.od.nih.gov/api-guide) |
| Natural Medicines / NatMed | TRC Healthcare | Commercial supplement evidence/interactions | Curated natural product monographs and interaction ratings | Commercial subscription/API | Strong supplement-drug interaction and effectiveness ratings | Paid/licensed; clinical-advice guardrails required | High if licensed | Buy for interaction/effectiveness reference, not autonomous advice | [NatMed](https://naturalmedicines.therapeuticresearch.com/), [TRC](https://trchealthcare.com/product/natmed-pro/) |
| USDA FoodData Central | USDA ARS | Food/nutrient API | Food composition and branded foods | Public domain / CC0 | Nutrition and nutrient lookup | Food nutrients are not biomarker interpretation | Medium-high | Nutrition context and biomarker prompt enrichment | [API](https://fdc.nal.usda.gov/api-guide), [license](https://fdc.nal.usda.gov/) |
| NHANES | CDC/NCHS | Public health, lab, diet survey | Exam, lab, dietary, questionnaire data | Public-use datasets | Population-level biomarker/nutrition distributions | Observational, not individualized interpretation | Medium | Population context; biomarker reference features | [NHANES](https://www.cdc.gov/nchs/nhanes/index.html), [datasets](https://wwwn.cdc.gov/nchs/nhanes/) |
| MIMIC-IV / PhysioNet | MIT/PhysioNet | Clinical EHR and waveform datasets | De-identified ICU data | Credentialed access and DUA | Useful for clinical NLP/time-series research | DUA prohibits sharing restricted data with third-party APIs; clinical/PHI risk | Low-medium | Avoid for product training unless strict research sandbox | [MIMIC-IV](https://physionet.org/content/mimiciv/), [PhysioNet LLM policy](https://physionet.org/) |
| SIDER | EMBL | Side-effect resource | Public documents/package inserts | CC BY-NC-SA 4.0 for SIDER data | Marketed drug side-effect associations | Non-commercial license; older | Medium | Internal benchmark only unless license posture fits | [SIDER](https://sideeffects.embl.de/), [about/license](https://sideeffects.embl.de/about/) |
| OffSIDES / TwoSIDES / nSIDES | Tatonetti Lab | AE signal databases | FAERS-derived statistical signals | Public research resource; verify license per release | Off-label side effects and DDI signals | Signal mining, not causality; licensing varies | Medium | Pharmacovigilance benchmarking; signal comparison | [nSIDES](https://nsides.io/), [Tatonetti Lab](https://tatonettilab.org/resources/tatonetti-stm.html) |
| WADA Prohibited List | WADA | Anti-doping regulatory list | Annual prohibited substances/methods | Public standard documents | Authoritative sports-ban classification | PDF/list ingestion needs yearly refresh | High | Sports-ban/regulatory guardrails | [WADA List](https://www.wada-ama.org/en/resources/world-anti-doping-code-and-international-standards/prohibited-list) |
| Global DRO | Global DRO partners | Medication anti-doping lookup | WADA list plus country medication brands | Web service; terms restrict reuse, no broad supplement coverage | Practical athlete-facing medication status | Does not cover most supplements/pre-workouts/herbals | Medium | Manual reference or licensed/terms-approved lookup | [Global DRO](https://www.globaldro.com/) |
| OPSS | U.S. DoD | Supplement and high-risk substance resource | DoD supplement safety program | Public web resource; check reuse terms | Strong high-risk supplement/SARM context | Not a structured commercial API | High | High-risk category guardrails and copy references | [OPSS](https://www.opss.org/), [SARM warning](https://www.opss.org/article/sarms-whats-harm) |
| FDA SARM warnings | FDA | Regulatory safety warnings | FDA consumer updates and warning letters | Public federal resource | Clear regulatory stance on SARMs | Not a full structured SARM database | High | SARM/SERM/peptide guardrails and enforcement signals | [FDA SARM consumer update](https://www.fda.gov/consumers/consumer-updates/fda-warns-use-selective-androgen-receptor-modulators-sarms-among-teens-young-adults), [bodybuilding warning](https://www.fda.gov/drugs/fraudulent-products/certain-bodybuilding-products-put-consumers-risk-heart-attack-stroke-serious-liver-damage-and-more) |

## Architecture Patterns

| BioStack capability | Recommended pattern | Best-fit assets | Avoid |
|---|---|---|---|
| Evidence Confidence Overlay | Retrieval + structured evidence scoring + cited LLM summary | PubMed/PMC, MedCPT, BioASQ/PubMedQA benchmarks, ClinicalTrials.gov | Freeform LLM confidence without citations |
| Phase-Aware Protocol Graph | Internal KG with phase nodes, evidence edges, risk overlays | Reactome, ChEMBL, Open Targets, RxNorm, PubTator3, internal protocol taxonomy | External KG as canonical truth without provenance |
| Side-Effect Ambiguity Detector | Label + FAERS signal + literature contrast | DailyMed/openFDA labels, FAERS, SIDER/OffSIDES benchmark, DrugBank if licensed | Causality claims from FAERS alone |
| Source Quality Tracker | Classifier over source metadata and study design | PubMed metadata, ClinicalTrials.gov, SciBERT/BiomedBERT classifiers, EBM-NLP | Treating all papers or preprints equally |
| GLP-1 Observability Pack | Label/trial/literature dashboard with adverse-event and biomarker fields | DailyMed/openFDA, ClinicalTrials.gov, PubMed, FAERS, RxNorm | Dosing or personalized treatment recommendations |
| High-Risk Category Guardrails | Deterministic category/rule engine plus regulatory source links | WADA, OPSS, FDA SARM warnings, RxNorm, DrugBank/NatMed if licensed | LLM-only moderation of SARM/peptide content |
| Research Gap Alerts | Query expansion + embedding clustering + evidence-count thresholds | MedCPT, SPECTER2, PubMed, ClinicalTrials.gov, PubTator3 | “No evidence” conclusions without corpus coverage reporting |
| Biomarker Prompt Generator | Non-diagnostic prompt templates grounded in evidence categories | NHANES, NIH ODS, PubMed, internal clinician-reviewed rules | Individual lab interpretation or diagnosis |
| Protocol Complexity Score | Rule-based graph features and risk signals | Internal graph, RxNorm classes, interaction data, regulatory categories | Hidden model score without explainable factors |
| Regulatory Status Awareness | Deterministic status lookups and yearly refresh jobs | FDA labels/warnings, WADA, OPSS, RxNorm, ClinicalTrials.gov | Using LLM memory for status |

## Top 10 Recommended Assets

1. PubMed/PMC plus NCBI E-utilities and PMC APIs
   - Matters because all evidence features need source-backed retrieval.
   - Supports Evidence Confidence Overlay, Research Gap Alerts, Source Quality Tracker.
   - Integrate with MedCPT retrieval, PMID/PMCID provenance, license-aware full-text ingestion.
   - Guardrail: every generated claim must carry source IDs and retrieval timestamp.
   - Risk: PMC full-text licenses vary; automated retrieval rules are strict.

2. MedCPT
   - Matters because it is designed for biomedical retrieval, not generic semantic search.
   - Supports PubMed search, evidence overlays, gap detection.
   - Integrate as query/article encoders and cross-encoder reranker.
   - Guardrail: retrieval scores are relevance, not truth.
   - Risk: NCBI license/disclaimer and attribution review.

3. DailyMed/openFDA Drug Labels
   - Matters because FDA label sections are the best public structured source for warnings, adverse reactions, contraindications, and indications.
   - Supports GLP-1 Observability Pack, High-Risk Guardrails, Side-Effect Ambiguity Detector.
   - Integrate with SPL section parser and label-version tracking.
   - Guardrail: display as label evidence, not medical instruction.
   - Risk: openFDA warns its reformatted content is not verified for care decisions.

4. openFDA / FAERS
   - Matters because post-market reports reveal ambiguity and emerging safety signals.
   - Supports side-effect ambiguity, warning detection, risk watchlists.
   - Integrate with disproportionality/signal heuristics and label/literature comparison.
   - Guardrail: never infer incidence or causality from FAERS alone.
   - Risk: reporting bias, duplicates, missing denominators.

5. PubTator3
   - Matters because it gives scalable entity and relation annotations over PubMed/PMC.
   - Supports protocol graph edge candidates, entity linking, relationship extraction.
   - Integrate as an annotation layer feeding review queues.
   - Guardrail: all extracted relations need confidence, sentence span, and human promotion.
   - Risk: automated annotations are not curated truth.

6. RxNorm/RxNav/RxClass/MED-RT
   - Matters because BioStack needs normalized drug/substance/class identity before any guardrail is reliable.
   - Supports regulatory status, class risk, ingredient matching.
   - Integrate as a canonical drug normalization service.
   - Guardrail: use for naming/classes, not full clinical DDI decisions.
   - Risk: source-vocabulary and UMLS terms require review for redistribution.

7. ChEMBL
   - Matters because mechanism, target, assay, and bioactivity data are needed for substance-to-pathway relationships.
   - Supports Phase-Aware Protocol Graph and Protocol Complexity Score.
   - Integrate with compound identifiers and assay provenance, not as direct claims.
   - Guardrail: distinguish in vitro/bioactivity from clinical evidence.
   - Risk: CC BY-SA creates share-alike obligations for derived redistributed data.

8. Reactome
   - Matters because it is curated, peer-reviewed pathway knowledge with CC0 data.
   - Supports pathway mapping and biomarker context.
   - Integrate as a pathway hierarchy and graph enrichment layer.
   - Guardrail: pathway association is not proof of clinical effect.
   - Risk: coverage gaps for supplements and investigational compounds.

9. NIH ODS Fact Sheets and DSLD
   - Matters because supplements are a core BioStack domain and public data is otherwise noisy.
   - Supports supplement evidence, label-claim detection, high-risk ingredient monitoring.
   - Integrate ODS as evidence baseline and DSLD as label inventory.
   - Guardrail: label claims are not evidence; keep evidence and marketing claims separate.
   - Risk: DSLD labels can be incomplete or inaccurate.

10. Licensed DrugBank and/or NatMed
   - Matters because public datasets are weak for drug-drug, drug-supplement, and supplement effectiveness interactions.
   - Supports Side-Effect Ambiguity Detector, High-Risk Guardrails, Source Quality Tracker.
   - Integrate behind a compliance-reviewed data boundary with citation and license metadata.
   - Guardrail: use as risk flags and clinician-reviewed references, not automated advice.
   - Risk: commercial license cost, redistribution restrictions, medical-decision-support exposure.

## Build Versus Buy

| Layer | Recommendation |
|---|---|
| General LLM | Use a strong general LLM with strict RAG, citation requirements, refusal policies, and no freeform medical advice. Do not rely on model memory for regulatory status. |
| Biomedical embeddings | Use MedCPT for PubMed retrieval. Test SPECTER2 for related-paper clustering and source similarity. Keep generic embeddings as a fallback only. |
| Biomedical extraction | Build a local extraction pipeline using PubTator3, scispaCy/Stanza/HunFlair2, and BiomedBERT/BioLinkBERT fine-tuned classifiers. |
| Knowledge graphs | Use Reactome, Open Targets, ChEMBL, PubChem, RxNorm, and PubTator3-derived edges as inputs. Build BioStack’s protocol-intelligence graph internally with provenance on every edge. |
| Drug/supplement interactions | Buy or license where public data is insufficient. DrugBank and NatMed are the practical candidates. |
| Evidence grading | Build BioStack-specific classifiers and rules. Use BioASQ, PubMedQA, EBM-NLP, and clinical-trial metadata as benchmarks/training aids, not as canonical truth. |
| Fine-tuning | Fine-tune narrow classifiers: claim type, source quality, risk category, study design, adverse-event ambiguity, regulatory status, relation confidence. Avoid broad medical instruction tuning. |
| Distillation | Distill only classification/extraction behavior into smaller models. Do not distill medical advice generation. |
| Human review | Required before any extracted edge, warning, claim grade, or supplement/regulatory interpretation becomes canonical knowledge. |

## Avoid Direct Use

Avoid user-facing direct generation from BioMistral, Meditron, BioGPT, GatorTron, or any open medical LLM. They may be useful for benchmarking or internal red-team comparison, but they are unsafe as autonomous BioStack advisors because they can hallucinate citations, overstate clinical certainty, and generate actionable medical guidance. Model cards for BioMistral and Meditron explicitly warn against direct production medical use without extensive alignment and real-world testing.

Avoid using MIMIC-IV/PhysioNet restricted data in product training or API-based LLM workflows unless BioStack creates a controlled research program that satisfies the DUA. PhysioNet’s credentialed-data terms prohibit sharing restricted data with third-party services.

Avoid building from SemMedDB as a commercial canonical graph unless the UMLS and non-commercial restrictions are resolved.

Avoid Global DRO scraping. Treat it as a reference or pursue permitted access; it is not a broad supplement status database.

## Safety and Compliance Review

| Risk | Practical control |
|---|---|
| Medical advice | Product copy and model policy must refuse diagnosis, treatment, dosing, prescribing, cycle/PCT/injection instructions, and sourcing. |
| Hallucinated citations | Every answer must be retrieval-grounded with PMID/PMCID/NCT/label IDs. If no source is retrieved, the system says so. |
| Unsupported supplement claims | Separate label claims from evidence. Use ODS/NatMed/public literature grades before promotion. |
| Investigational peptides | Classify as investigational/regulatory-risk content; provide evidence/status summaries only. |
| SARMs/SERMs misuse | Deterministic high-risk category guardrails using FDA, OPSS, WADA, and label/trial sources. |
| Dosing/cycle generation | Hard refusal plus retrieval suppression for protocol instructions. |
| Regulated medical-device claims | Avoid claims that BioStack diagnoses, treats, monitors disease, or makes therapeutic decisions. Keep observability and evidence organization language. |
| PHI handling | Do not ingest PHI into third-party LLM APIs without HIPAA/security review. Use de-identification and data minimization. |
| Licensing/redistribution | Track license per source, per field. Separate CC0/public-domain data from CC BY-SA, noncommercial, UMLS-restricted, and commercial data. |
| Canonical promotion | Human review required for all canonical edges, evidence grades, and warning copy. Preserve source span and reviewer decision. |

## Implementation Path

### Use Now

- PubMed/PMC via NCBI-approved APIs, with MedCPT retrieval and PMID/PMCID provenance.
- DailyMed/openFDA labels for drug warning/adverse-reaction/contraindication extraction.
- ClinicalTrials.gov for trial status and research-gap detection.
- PubTator3, scispaCy, and BiomedBERT/BioLinkBERT for extraction candidates.
- RxNorm/RxNav for drug normalization.
- Reactome, Open Targets, PubChem, and carefully license-reviewed ChEMBL for graph enrichment.
- NIH ODS and DSLD for supplement evidence and label-claim separation.
- WADA, OPSS, and FDA warnings for high-risk category guardrails.

### Test Next

- SPECTER2 versus MedCPT for clustering and related-paper discovery.
- SapBERT for alias resolution across substances, biomarkers, diseases, and pathways.
- Fine-tuned narrow classifiers for claim type, evidence tier, adverse-event ambiguity, and source quality.
- Licensed DrugBank and NatMed pilots for interaction and supplement risk coverage.

### Avoid

- User-facing open medical LLM answers.
- Any dosing/cycle/PCT/injection/sourcing generation.
- FAERS-only risk claims.
- Unlicensed UMLS/SNOMED/DrugBank/NatMed/SIDER-derived redistribution.
- MIMIC/PhysioNet data in external LLM APIs.

### Build Internally

- BioStack canonical protocol-intelligence graph with provenance, source IDs, license metadata, review status, and confidence.
- Evidence-tier classifier and source-quality tracker.
- High-risk category guardrail engine.
- Phase-aware protocol graph schema and complexity score.
- Human review console for promotion into canonical knowledge.

### Legal / Licensing Review Required

- DrugBank, NatMed, UMLS/SNOMED-derived use, ChEMBL share-alike impact, SIDER non-commercial/share-alike restrictions, SemMedDB non-commercial/UMLS constraints, Global DRO terms, and PMC full-text reuse by article license.

## Final Recommendation

BioStack should build an evidence-bounded protocol intelligence layer, not a medical-advice model. The core system should combine MedCPT retrieval, PubMed/PMC provenance, DailyMed/openFDA labels, FAERS signals, ClinicalTrials.gov, PubTator3 extraction, RxNorm normalization, Reactome/Open Targets/ChEMBL pathway and mechanism context, and NIH ODS/DSLD supplement evidence. A general LLM can summarize and classify only after retrieval, with hard refusal boundaries and source-required outputs.

The first production milestone should be a read-only evidence graph with cited claims, label warnings, adverse-event ambiguity flags, regulatory/high-risk tags, and human-reviewed promotion. Fine-tuning should focus on narrow classifiers and extraction confidence, not broad biomedical generation.
