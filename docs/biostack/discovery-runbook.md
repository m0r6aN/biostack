# Open discovery runbook

Added 2026-10-24 after Clinton reported that BioStack had nothing on **ER-100**. The research process could not find new substances and therapies. It only looked up names someone had already typed into a hand-written list.

## Why discovery was missing

| Cause | Where |
|---|---|
| Source acquisition only looks up names it is given: the search terms are the target name plus its aliases. | `Pipeline/RecommendedOfficialSourcePlanningAdapters.cs` L28-37 |
| The ClinicalTrials.gov adapter queries `AREA[InterventionName]` for those exact terms. | `Pipeline/ClinicalTrialsGovV2AcquisitionAdapter.cs` L144-145, L257-274 |
| The candidate universe is 70 hand-written names. The only other input is an 8-name watchlist. | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json`; `Seeds/discovery-watchlist.json` |
| Acquisition is a manual job and needs human source and legal approvals: 21 of 30 registry sources are `pending-human-legal`. | `infra/azure/source-acquisition-jobs.bicep` L220; `research/input/sources/pilot-source-registry.json`; `Pipeline/SourceAcquisitionPlanning.cs` L262-357 |
| There was no classification for gene, epigenetic or cell therapies. Unknown values map to `Unknown`. | `Schemas/substance-record.schema.json` classification enum; `Pipeline/SubstanceCanonicalizer.cs` `MapClassification` |
| Matching works on names and aliases only. Enobosarm appeared in the Ostarine record's text but not as an alias. | `Seeds/substances-seed.json` |

## What runs now

`RunMode=Discovery` is database-free and scheduled daily at 05:30 UTC by `infra/azure/knowledge-schedules.bicep`. Each run:

1. **Queries four public APIs** over a look-back window (`Discovery:LookbackDays`, default 30; preprints use `Discovery:PreprintLookbackDays`, default 7):
   - **ClinicalTrials.gov v2.** Each scope area's terms (obesity, aging, myostatin, gene therapy, epigenetic reprogramming and so on) for studies first posted in the window. Phase 1–3 drug, biologic, genetic and supplement interventions only.
   - **PubMed E-utilities.** `esearch` plus `esummary` per area, limited to clinical-trial, phase and first-in-human papers. Code names and INN-stem names are taken from titles.
   - **openFDA Drugs@FDA.** Original NDA (type 1, new molecular entity) and BLA approvals in the window. Biosimilars are dropped.
   - **bioRxiv/medRxiv.** The `details` API for the window, filtered by scope keywords.
2. **Extracts names:**
   - development code names (`ER-100`, `LY3437943`, `SLU-PP-332`, `UBT251`)
   - INN stems (`-glutide`, `-lintide`, `-glipron`, `-mab`, `-vec`, `-cel`, `-siran`, `-rsen`, ...)
   - raw names of genetic, biologic and supplement interventions

   Out-of-scope titles and conditions (oncology, infectious disease) are skipped.
3. **Merges and matches.** Hits that share any normalized name or alias are merged (`ER-100` = `ER100` = `er 100`), so `CT-388` and `enicepatide` become one candidate. Each candidate is then checked against the seed plus discovered records:
   - `InCorpus`: the name matches an existing record.
   - `PossibleAlias`: the name is mentioned next to a record's own name, which suggests a missing alias.
   - `New`: neither.
4. **Scores** each candidate by independent sources: ClinicalTrials.gov 3, FDA 3, PubMed 2, preprint 1, plus 1 when it has 3 or more citations. New candidates at or above `Discovery:MinScore` (default 2) get a **draft record**, up to `Discovery:MaxCandidatesPerRun` (default 50).
5. **Writes:**
   - `discovery/discovery-report-<stamp>.json`: every candidate, its citations, its corpus status, and the queries sent (keys redacted).
   - `research-requests/research-request-discovery-<stamp>.json`: a schema-valid research-request batch.
   - `discovered-records/discovered-<stamp>.json`: schema-valid draft records. A draft holds identity, registry or approval status and citations. Everything else is unknown or empty, and the record is `Not graded`.

**No human gate.** Drafts go through the same pipeline as seed records:
- CouncilReview reviews discovered records first, so the 158 seed records do not crowd them out.
- Refresh publishes them through the promotion gate, labelled pending review until the council decides.
- ResearchPlanning gives them update checks.

The seed always wins on a name collision.

**Curated records outside the seed** live in `Seeds/discovered-records/`. ER-100 is the first one.

## Setup

| Setting | Where | Notes |
|---|---|---|
| `ncbiEmail` | Bicep param → `Discovery__NcbiEmail` | **Required for PubMed** (NCBI usage policy). If it is empty, PubMed is skipped and the report says why. |
| `ncbiApiKeySecretUri` | Key Vault → `Discovery__NcbiApiKey` | Optional. Raises the limit from 3 to 10 requests/s. Get a key at https://account.ncbi.nlm.nih.gov/settings/. |
| `openFdaApiKeySecretUri` | Key Vault → `Discovery__OpenFdaApiKey` | Optional. Raises the quota from 1,000 to 120,000 requests/day. |
| `discoveryCron`, `discoveryLookbackDays` | Bicep params | Defaults: `30 5 * * *` (01:30 EDT / 00:30 EST) and 30 days. |

Keys are only ever secret references. A test (`DiscoveryTests.Discovery_Is_Scheduled_In_Bicep_With_Keys_Only_As_Secret_Refs`) enforces it.

Run locally (no database):

```
cd backend/src/BioStack.KnowledgeWorker/bin/Debug/net10.0
DOTNET_ENVIRONMENT=Staging Worker__RunMode=Discovery Discovery__NcbiEmail=<you@example.com> \
  Discovery__OutputDirectory=/tmp/disc Worker__DiscoveredRecordDirectory=/tmp/disc/drafts \
  Worker__ResearchPlanningOutputDirectory=/tmp/disc/requests dotnet BioStack.KnowledgeWorker.dll
```

## Source terms and known risks

Only metadata from APIs whose terms allow automated access is used. No HTML is scraped, and no full text or abstracts are stored.

| Source | Terms | Risk logged |
|---|---|---|
| ClinicalTrials.gov API v2 | https://clinicaltrials.gov/about-site/terms-conditions: public registry data, API use permitted. | No published rate limit; requests are spaced 400 ms apart. Registry entries are sponsor-submitted and not verified. |
| PubMed E-utilities | https://www.ncbi.nlm.nih.gov/books/NBK25497/: `tool` and `email` are required; 3 req/s without a key, 10 with one. Large jobs should run off-peak (weekends or 21:00–05:00 ET). | The daily 05:30 UTC run is inside the off-peak window. Abstracts can carry publisher copyright, so only titles and IDs are kept. |
| openFDA | https://open.fda.gov/terms/: public data. 240 req/min and 1,000/day per IP without a key. | Drugs@FDA covers CDER only. CBER products (most gene and cell therapies) and IND clearances are not there; they come from ClinicalTrials.gov instead. |
| bioRxiv/medRxiv API | https://api.biorxiv.org/: open metadata, 30 records per call. | Each preprint has its own license (`cc_by`, `cc_no` and others), so only title, DOI and date are kept. Preprints are not peer reviewed: drafts label them that way and they score lowest. |

Other risks:
- **Noise.** Code names that are really genes or targets (for example `GDF15`), and off-topic trials, can become drafts. Prefix deny-lists, exclusion keywords, phase and type filters, and the council are the controls. Tune `Seeds/discovery-scope.json`.
- **Gray-market names** (for example `5-Amino-1MQ`, `Dihexa`, `SLU-PP-332`) rarely appear in registries. Keep them in the watchlist.
- **Combination brands** (`CagriSema`) are often registered under their components (cagrilintide plus semaglutide), so the brand never appears as a name. Keep combinations in the watchlist.

## Backtest (2026-10-24, 330-day window, ClinicalTrials.gov + openFDA only)

Of the 17 benchmark items missing from the 158-record corpus, open discovery found 9:
- ER-100 (NCT07290244)
- orforglipron
- ecnoglutide
- petrelintide
- eloralintide
- CT-388 (as enicepatide)
- apitegromab
- NMN
- follistatin gene therapy

The other 8 came out as follows:
- **Not found:** bimagrumab, efinopegdutide, SLU-PP-332, 5-Amino-1MQ, Dihexa, rapamycin and LOY-002. PubMed (once `ncbiEmail` is set), preprints and the watchlist cover these.
- **CagriSema:** only its component cagrilintide appeared, and that is already in the corpus. The combination name itself was never seen.
