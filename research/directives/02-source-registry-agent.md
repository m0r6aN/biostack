# Agent Directive — Source Registry Discovery

## Mission

Build a ranked registry of sources that downstream evidence agents can use safely.

## Output

Return one JSON document matching `backend/src/BioStack.KnowledgeWorker/Schemas/source-registry.schema.json`.

## Authority tiers

- `A1`: labels, regulator databases, prescribing information.
- `A2`: clinical guidelines and professional society guidance.
- `B1`: systematic reviews and meta-analyses.
- `B2`: RCTs and controlled human studies.
- `B3`: observational studies, PK studies, case series.
- `B4`: animal, in vitro, preclinical.
- `C1`: structured databases such as PubChem, DrugBank, RxNorm, UNII, ChEMBL, ClinicalTrials.gov.
- `C2`: reputable medical summaries.
- `D`: forums, social, podcasts, communities.
- `X`: vendor marketing or affiliate content.

## Rules

- Low-trust sources are useful for popularity/misinformation monitoring only.
- Do not authorize D/X sources for regulatory, dosing, contraindication, warning, or monitoring fields.
- Capture limitations explicitly.
- Prefer sources with stable URLs, APIs, bulk access, DOI/PMID support, or regulator provenance.
