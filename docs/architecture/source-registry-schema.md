# Source Registry Schema Proposal

Date: 2026-06-18
Source research: `research/protocol-intelligence/biostack-ai-model-data-asset-research-memo.md`

The source registry is the control plane for BioStack knowledge ingestion. It determines whether a source can be retrieved, indexed, displayed, exported, used for model training, or promoted into canonical knowledge.

## Registry Record

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `sourceName` | string | Yes | Human-readable source name. |
| `sourceType` | enum | Yes | `literature`, `full_text`, `drug_label`, `adverse_event`, `trial_registry`, `terminology`, `chemical`, `pathway`, `supplement`, `anti_doping`, `commercial`, `model`, `benchmark`, `internal`. |
| `sourceUrl` | string | Yes | Primary source or documentation URL. |
| `maintainer` | string | Yes | Source owner or maintainer. |
| `license` | string | Yes | License, terms, or review status. |
| `usageStatus` | enum | Yes | `use_now`, `test_next`, `license_review`, `benchmark_only`, `avoid`. |
| `allowedUse` | string[] | Yes | Permitted BioStack uses. |
| `disallowedUse` | string[] | Yes | Explicitly prohibited uses. |
| `refreshCadence` | string | Yes | Expected refresh schedule or review interval. |
| `retrievalMethod` | enum/string | Yes | API, download, manual review, licensed feed, no retrieval, or other approved method. |
| `citationIdentifierType` | string[] | Yes | Stable identifiers, such as PMID, PMCID, DOI, NCT, SPL set ID, RxCUI, ChEMBL ID, Reactome ID, source URL. |
| `provenanceFields` | string[] | Yes | Fields that must be stored with every ingested item. |
| `humanReviewRequirement` | enum | Yes | `always`, `high_risk_only`, `before_canonical_promotion`, `not_applicable`. |
| `redistributionConstraints` | string[] | Yes | Display, export, sharing, derived-data, or source-text restrictions. |
| `notes` | string | No | Operational or compliance notes. |

## Provenance Envelope

Every ingested item should carry:

- Source registry ID.
- Source item stable ID.
- Source URL.
- Source version or publication/update date where available.
- Retrieval timestamp.
- Retrieval method.
- Raw pointer or storage reference.
- Parsed section or source span.
- License status at retrieval time.
- Transformation pipeline version.
- Extractor or classifier version when applicable.
- Human review status and reviewer decision when promoted.

## Example Records

```json
[
  {
    "sourceName": "PubMed",
    "sourceType": "literature",
    "sourceUrl": "https://pubmed.ncbi.nlm.nih.gov/",
    "maintainer": "NCBI/NLM",
    "license": "Public metadata API; abstracts and linked full text require source-specific review",
    "usageStatus": "use_now",
    "allowedUse": ["citation retrieval", "literature metadata", "evidence retrieval", "review queue support"],
    "disallowedUse": ["treat abstract text as blanket full-text license", "generate medical authority", "infer clinical certainty without evidence tier"],
    "refreshCadence": "daily metadata refresh for active topics; full reindex monthly",
    "retrievalMethod": "NCBI E-utilities/API",
    "citationIdentifierType": ["PMID", "DOI", "PMCID when available"],
    "provenanceFields": ["pmid", "doi", "pmcid", "title", "journal", "publicationDate", "retrievedAtUtc", "query", "sourceUrl"],
    "humanReviewRequirement": "before_canonical_promotion",
    "redistributionConstraints": ["store citation metadata separately from article full text", "check full-text license before redistributing text"],
    "notes": "Primary literature backbone."
  },
  {
    "sourceName": "DailyMed",
    "sourceType": "drug_label",
    "sourceUrl": "https://dailymed.nlm.nih.gov/",
    "maintainer": "NLM/FDA",
    "license": "Public federal drug label resource; verify display requirements",
    "usageStatus": "use_now",
    "allowedUse": ["label warning extraction", "adverse reaction section parsing", "contraindication source retrieval"],
    "disallowedUse": ["personalized prescribing", "individualized dosing", "treatment plans", "start/stop/taper/escalation instructions"],
    "refreshCadence": "weekly label refresh; immediate refresh for watched products",
    "retrievalMethod": "DailyMed API/downloads",
    "citationIdentifierType": ["SPL set ID", "NDC", "source URL"],
    "provenanceFields": ["splSetId", "ndc", "labelVersion", "sectionName", "sectionCode", "retrievedAtUtc", "sourceUrl"],
    "humanReviewRequirement": "before_canonical_promotion",
    "redistributionConstraints": ["present as label evidence, not medical instruction"],
    "notes": "Use for source-backed label sections."
  },
  {
    "sourceName": "openFDA drug label",
    "sourceType": "drug_label",
    "sourceUrl": "https://open.fda.gov/apis/drug/label/",
    "maintainer": "FDA",
    "license": "Public API with openFDA disclaimers",
    "usageStatus": "use_now",
    "allowedUse": ["JSON label retrieval", "label section indexing", "source comparison with DailyMed"],
    "disallowedUse": ["medical authority", "prescribing", "diagnosis", "treatment planning", "regulatory inference without current source"],
    "refreshCadence": "weekly or source-driven",
    "retrievalMethod": "openFDA API",
    "citationIdentifierType": ["SPL set ID", "NDC", "openFDA application number", "source URL"],
    "provenanceFields": ["openfda", "set_id", "effective_time", "section", "retrievedAtUtc", "apiUrl"],
    "humanReviewRequirement": "before_canonical_promotion",
    "redistributionConstraints": ["include source and caveat that openFDA data is not medical authority"],
    "notes": "Prefer DailyMed SPL for label canonicalization when possible."
  },
  {
    "sourceName": "FAERS/openFDA drug event",
    "sourceType": "adverse_event",
    "sourceUrl": "https://open.fda.gov/apis/drug/event/",
    "maintainer": "FDA",
    "license": "Public API with adverse-event caveats",
    "usageStatus": "use_now",
    "allowedUse": ["signal detection", "ambiguity overlays", "review prioritization"],
    "disallowedUse": ["causality claims", "incidence estimates", "risk quantification without denominator", "medical authority"],
    "refreshCadence": "quarterly or API release cadence",
    "retrievalMethod": "openFDA API and FAERS releases",
    "citationIdentifierType": ["safety report ID", "source URL", "retrieval query"],
    "provenanceFields": ["safetyReportId", "receivedDate", "drug", "reaction", "query", "retrievedAtUtc"],
    "humanReviewRequirement": "always",
    "redistributionConstraints": ["must display caveat: reports do not establish causality or incidence"],
    "notes": "Never present FAERS as proof of causality."
  },
  {
    "sourceName": "ClinicalTrials.gov",
    "sourceType": "trial_registry",
    "sourceUrl": "https://clinicaltrials.gov/",
    "maintainer": "NLM",
    "license": "Public registry API",
    "usageStatus": "use_now",
    "allowedUse": ["trial status", "intervention metadata", "research gap alerts"],
    "disallowedUse": ["treat registry entry as peer-reviewed outcome evidence", "infer efficacy from registration"],
    "refreshCadence": "weekly for watched topics; monthly baseline",
    "retrievalMethod": "ClinicalTrials.gov API",
    "citationIdentifierType": ["NCT ID", "source URL"],
    "provenanceFields": ["nctId", "status", "phase", "interventions", "conditions", "lastUpdateSubmitDate", "retrievedAtUtc"],
    "humanReviewRequirement": "before_canonical_promotion",
    "redistributionConstraints": ["label as registry data"],
    "notes": "Outcome publications require separate literature sources."
  },
  {
    "sourceName": "NIH ODS Fact Sheets",
    "sourceType": "supplement",
    "sourceUrl": "https://ods.od.nih.gov/factsheets/list-all/",
    "maintainer": "NIH Office of Dietary Supplements",
    "license": "Public federal resource; verify page reuse terms",
    "usageStatus": "use_now",
    "allowedUse": ["supplement evidence baseline", "safety summary retrieval", "review queue support"],
    "disallowedUse": ["individualized dosing", "diagnosis", "treatment planning", "start/stop/taper/escalation instructions"],
    "refreshCadence": "monthly watch; source update detection",
    "retrievalMethod": "approved page/API retrieval or manual source review",
    "citationIdentifierType": ["source URL", "page title", "update date"],
    "provenanceFields": ["pageUrl", "section", "updatedDate", "retrievedAtUtc"],
    "humanReviewRequirement": "before_canonical_promotion",
    "redistributionConstraints": ["cite source; avoid implying BioStack recommendation"],
    "notes": "Separate ODS evidence from supplement label claims."
  },
  {
    "sourceName": "WADA Prohibited List",
    "sourceType": "anti_doping",
    "sourceUrl": "https://www.wada-ama.org/en/prohibited-list",
    "maintainer": "World Anti-Doping Agency",
    "license": "Public standard document; verify reuse/display terms",
    "usageStatus": "use_now",
    "allowedUse": ["banned-in-sport classification", "warning-first regulatory status"],
    "disallowedUse": ["performance-use guidance", "cycle design", "status inference from model memory"],
    "refreshCadence": "annual list refresh plus interim monitoring",
    "retrievalMethod": "manual/legal-approved document ingestion",
    "citationIdentifierType": ["list year", "section", "source URL"],
    "provenanceFields": ["listYear", "section", "substanceClass", "retrievedAtUtc", "sourceUrl"],
    "humanReviewRequirement": "always",
    "redistributionConstraints": ["display list year and source; stale status must be blocked"],
    "notes": "Guardrail source, not advice source."
  },
  {
    "sourceName": "DrugBank",
    "sourceType": "commercial",
    "sourceUrl": "https://go.drugbank.com/",
    "maintainer": "DrugBank",
    "license": "Commercial license required",
    "usageStatus": "license_review",
    "allowedUse": ["licensed DDI pilot", "structured drug target/interaction review support"],
    "disallowedUse": ["unlicensed redistribution", "autonomous prescribing", "diagnosis", "treatment planning", "clinical decision support without review"],
    "refreshCadence": "per contract",
    "retrievalMethod": "licensed feed/API only",
    "citationIdentifierType": ["DrugBank ID", "source URL"],
    "provenanceFields": ["drugbankId", "field", "releaseVersion", "retrievedAtUtc", "licenseScope"],
    "humanReviewRequirement": "always",
    "redistributionConstraints": ["contract-specific display/export limits", "separate licensed fields from open fields"],
    "notes": "Do not ingest until license terms are approved."
  },
  {
    "sourceName": "NatMed",
    "sourceType": "commercial",
    "sourceUrl": "https://naturalmedicines.therapeuticresearch.com/",
    "maintainer": "TRC Healthcare",
    "license": "Commercial subscription/API required",
    "usageStatus": "license_review",
    "allowedUse": ["licensed supplement interaction/effectiveness pilot", "review support"],
    "disallowedUse": ["unlicensed display", "autonomous supplement advice", "dosing recommendations"],
    "refreshCadence": "per contract",
    "retrievalMethod": "licensed API/feed only",
    "citationIdentifierType": ["NatMed monograph ID if provided", "source URL"],
    "provenanceFields": ["monographId", "rating", "releaseVersion", "retrievedAtUtc", "licenseScope"],
    "humanReviewRequirement": "always",
    "redistributionConstraints": ["contract-specific output limits", "no derived redistribution unless allowed"],
    "notes": "High value for supplement gaps; high compliance sensitivity."
  },
  {
    "sourceName": "ChEMBL",
    "sourceType": "chemical",
    "sourceUrl": "https://www.ebi.ac.uk/chembl/",
    "maintainer": "EMBL-EBI",
    "license": "CC BY-SA 3.0",
    "usageStatus": "license_review",
    "allowedUse": ["compound-target mapping", "assay provenance", "mechanism graph enrichment"],
    "disallowedUse": ["treat in vitro or assay evidence as clinical outcome evidence", "ignore share-alike obligations"],
    "refreshCadence": "per ChEMBL release",
    "retrievalMethod": "ChEMBL API/downloads",
    "citationIdentifierType": ["ChEMBL ID", "assay ID", "document ID", "source URL"],
    "provenanceFields": ["chemblId", "assayId", "targetId", "documentId", "activityType", "retrievedAtUtc", "releaseVersion"],
    "humanReviewRequirement": "before_canonical_promotion",
    "redistributionConstraints": ["share-alike obligations require legal review for derived datasets"],
    "notes": "Useful for mechanism; not clinical proof."
  },
  {
    "sourceName": "Reactome",
    "sourceType": "pathway",
    "sourceUrl": "https://reactome.org/",
    "maintainer": "Reactome consortium",
    "license": "CC0",
    "usageStatus": "use_now",
    "allowedUse": ["pathway graph enrichment", "biomarker/pathway context"],
    "disallowedUse": ["clinical efficacy inference", "medical authority"],
    "refreshCadence": "per Reactome release",
    "retrievalMethod": "Reactome API/downloads",
    "citationIdentifierType": ["Reactome ID", "source URL"],
    "provenanceFields": ["reactomeId", "pathwayName", "species", "releaseVersion", "retrievedAtUtc"],
    "humanReviewRequirement": "before_canonical_promotion",
    "redistributionConstraints": ["cite source in documentation"],
    "notes": "Low license friction; still requires evidence context."
  }
]
```
