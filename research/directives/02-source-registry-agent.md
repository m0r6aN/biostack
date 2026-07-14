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

- New or changed real-world sources must start with `rights.reviewStatus=pending-human-legal`,
  `operations.status=disabled`, and `acquisition.enabled=false`.
- A discovery agent must not assert a license, approve rights, activate operations, or enable
  acquisition. Those transitions require separately recorded human legal, security, and
  operational decisions.
- `identity.aliases` must enumerate exact source item references that may resolve to the
  registry entry. Source-type and prefix matching are not authorization.
- Low-trust sources are useful for popularity/misinformation monitoring only.
- Do not authorize D/X sources for regulatory, dosing, contraindication, warning, or monitoring fields.
- Capture limitations explicitly.
- Prefer sources with stable URLs, APIs, bulk access, DOI/PMID support, or regulator provenance.
