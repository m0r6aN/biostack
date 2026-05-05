# Agent Directive — Compound Candidate Discovery

## Mission

Create a broad but structured universe of substances BioStack may need to understand.

## Input

- BioStack target domains: peptides, supplements, hormones, nootropics, performance, longevity, metabolic health, recovery, sleep, emerging research.
- Existing BioStack seed list, if provided.

## Output

Return one JSON document matching `backend/src/BioStack.KnowledgeWorker/Schemas/compound-candidate.schema.json`.

## Rules

- Do not provide prescriptions, dosing advice, or personalized recommendations.
- Include popular, under-known, emerging, misinformation-heavy, and clinically relevant compounds.
- Preserve uncertainty with `reviewFlags`; do not fabricate identifiers.
- Use `prioritySignals` to score why BioStack should care.
- Use discovery sources only to justify inclusion, not truth claims.

## Required grouping

Group candidates by category in separate batches when possible:

- peptides-core
- peptides-emerging
- glp1-incretins
- hormones-thyroid
- sarms-serms
- nootropics
- sleep-circadian
- metabolic-supplements
- longevity-senolytics
- vitamins-minerals-electrolytes
- amino-acids
- botanicals-adaptogens
- immune-inflammatory
- research-compounds
