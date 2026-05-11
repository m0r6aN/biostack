# BioStack Compound Research Launchpack

This directory is the local staging area for the offline compound research supply chain.

## Directory contract

| Path | Purpose |
|---|---|
| `input/candidates/` | Compound universe batches matching `compound-candidate.schema.json`. |
| `input/sources/` | Source registries matching `source-registry.schema.json`. |
| `input/evidence/` | One evidence packet per compound, matching `evidence-packet.schema.json`. |
| `research-requests/` | Human/operator research requests for compounds that do not yet have evidence packets. |
| `review-decisions/` | Human/expert review decisions matching `review-decision.schema.json`. |
| `output/` | Generated drafts, review queues, and run reports. Ignored by git except `.gitkeep`. |
| `directives/` | Reusable agent instructions for consistent research passes. |
| `pilot/` | Pilot manifest and category slicing plan. |

## Local run

Use the runner from the repository root:

```powershell
./tools/research/run-knowledge-research.ps1 `
  -CandidateFile research/input/candidates/compound-candidates.json `
  -SourceRegistryFile research/input/sources/source-registry.json `
  -EvidenceDirectory research/input/evidence `
  -OutputDirectory research/output/latest
```

The runner invokes `BioStack.KnowledgeWorker` with `Worker:RunMode=Research`, which does not touch the database.
By default it also loads research-request batches from `research/research-requests` and review decisions from `research/review-decisions` when those paths exist.

## Publication rule

Research output is not customer-facing. Generated draft substance records must still pass review before ingestion or publication.

Review decisions may clear soft review blockers, but they must not clear hard blockers such as source-registry authorization failures or missing required authoritative support.
