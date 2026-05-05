# Compound Research Supply Chain

> Status: Phase 0 implementation guide  
> Scope: Agent-assisted compound discovery, evidence capture, preprocessing, reconciliation, and canonical BioStack substance record compilation.

## 1. Principle

BioStack compound research is an evidence supply chain, not a content-writing task.

The canonical `substance-record.schema.json` remains the publish target. Research agents should first produce validated intermediate artifacts:

1. `compound-candidate.schema.json` — broad substance universe and prioritization signals.
2. `source-registry.schema.json` — ranked source catalog and authorized field use.
3. `evidence-packet.schema.json` — claim-level evidence with citations, conflicts, and review flags.

Only reconciled evidence packets should be compiled into final substance records.

## 2. Non-negotiable rules

- No prescription, diagnosis, or personalized medical advice.
- Every nontrivial claim must cite source IDs.
- Low-trust sources may identify popularity or misinformation; they do not establish truth.
- Regulatory, safety-critical, monitoring, product-specific dosing, and storage/reconstitution fields require authoritative support.
- AI proposes; validators constrain; humans approve safety-critical or conflicted outputs.
- Runtime should consume published artifacts, never invent new compound claims.

## 3. Source authority model

| Tier | Examples | Authorized use |
|---|---|---|
| A1 | FDA/EMA labels, DailyMed, regulator bulletins | Regulatory status, approved indications, warnings, contraindications, product dosing |
| A2 | Clinical guidelines, professional societies | Standard-of-care context, monitoring, safety framing |
| B1 | Systematic reviews, meta-analyses | Evidence synthesis and benefit/risk summaries |
| B2 | RCTs and controlled human studies | Claim-level efficacy/safety evidence |
| B3 | Observational studies, PK studies, case series | Signals and context, not definitive truth |
| B4 | Animal/in vitro/preclinical papers | Mechanism hypotheses only |
| C1 | PubChem, DrugBank, RxNorm, UNII, ChEMBL, ClinicalTrials.gov | Identity, identifiers, target/trial metadata |
| C2 | Reputable medical summaries | Consumer-readable cross-checks |
| D | Forums, Reddit, social, podcasts | Popularity, aliases, misinformation monitoring |
| X | Vendor marketing, affiliate pages | Discovery only; never canonical authority |

## 4. Agent topology

### Agent 1 — Compound universe

Input: BioStack target domains and existing seed list.  
Output: `compound-candidate` batch grouped by category.

Directive:

> Return a broad candidate list of substances relevant to health optimization, peptides, supplements, hormones, nootropics, performance, longevity, metabolic health, recovery, and emerging research. Group by category. Include canonical-name candidate, aliases, brand names, classification, compound family, popularity, emerging interest, clinical relevance, safety risk, misinformation risk, source availability, inclusion rationale, discovery sources, and review flags. Do not give user-specific advice.

### Agent 2 — Source registry

Input: target categories from Agent 1.  
Output: `source-registry` file.

Directive:

> Discover reliable and unreliable-but-informative sources for compound research. Rank each source by authority tier, reliability score, source type, jurisdiction, category coverage, structured access, authorized field use, and limitations. Distinguish authoritative scientific/regulatory sources from discovery/misinformation-monitoring sources.

### Agents 3-n — Category evidence extraction

Input: one category, candidate substances, and relevant source registry entries.  
Output: one `evidence-packet` per compound plus optional draft substance record.

Directive:

> For each assigned compound, collect sourced claims about identity, mechanism, regulatory status, approved indications, studied uses, common off-label claims, safety, contraindications, warnings, adverse effects, monitoring, interactions, formulations, route, storage/reconstitution when applicable, evidence gaps, controversies, and misinformation claims. Every claim must cite source IDs. Do not infer beyond the source. Mark whether authoritative field support is required. If evidence is weak or conflicting, flag it.

## 5. Preprocessing before merge

Every agent output must pass these gates before entering the merged evidence corpus:

1. JSON parse.
2. Schema validation.
3. Compound-name normalization and alias clustering.
4. External identifier resolution where available.
5. Source ID resolution against the source registry.
6. Source authority scoring.
7. Claim deduplication.
8. Conflict detection.
9. Field-authority check.
10. Review flag assignment.

Raw agent output should be archived unchanged. Only preprocessed artifacts should be merged.

## 6. Merge and compile

The merge stage builds a claim/evidence graph keyed by canonical compound identity, claim type, context, and source refs.

Compilation into `substance-record` should obey field authority:

- Class A / authoritative sources may populate regulatory, safety-critical, product-specific dosing, and product storage fields.
- Class B sources may enrich mechanisms, pathways, aliases, evidence gaps, controversies, and supportive context.
- Low-trust sources may populate popularity and misinformation-watch claims only.
- Conflicted or Class B-only safety-critical fields must set `ops.needsReview = true` and add review reasons.

## 7. Recommended batch sequence

Start with a pilot, not the full universe:

1. 3 common supplements.
2. 3 peptides.
3. 2 GLP-1/incretin compounds.
4. 2 hormone/SERM/SARM compounds.
5. 2 nootropics.
6. 2 emerging/research compounds.

Use the pilot to tune schemas, source scoring, conflict rules, and review workflow before scaling to hundreds of substances.

## 8. Category slices for full expansion

- Peptides — core and emerging.
- GLP-1 / incretin / metabolic pharmaceuticals.
- Hormones and thyroid compounds.
- SARMs and SERMs.
- Nootropics and cognitive agents.
- Sleep and circadian compounds.
- Mitochondrial and metabolic supplements.
- Longevity and senolytic compounds.
- Vitamins, minerals, and electrolytes.
- Amino acids and protein-derived compounds.
- Botanicals and adaptogens.
- Anti-inflammatory and immune-modulating agents.
- Research compounds and discontinued compounds still discussed online.

## 9. Publication readiness

A compound is publishable only when:

- It has a valid canonical substance record.
- Required provenance fields are present.
- Safety-critical claims are authoritative or marked unknown/review-required.
- Contradictions are resolved or explicitly surfaced as controversies.
- `ops.completeness` is at least `partial` for discovery pages and `substantial` for high-demand pages.
- Human review is complete for high-risk, regulatory, dosing, monitoring, or contraindication fields.

## 10. Next engineering steps

1. Add validators for the three intermediate schemas.
2. Add a preprocessor that emits normalized evidence packets and conflict reports.
3. Add a compiler from evidence packets to `substance-record` draft JSON.
4. Add tests for field-authority enforcement at claim level.
5. Add a review queue for conflicted or high-risk records.
