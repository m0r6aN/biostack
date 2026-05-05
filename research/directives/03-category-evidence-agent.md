# Agent Directive — Category Evidence Extraction

## Mission

For one assigned category, extract claim-level evidence for each compound.

## Input

- Category name.
- Candidate substances for that category.
- Relevant source registry entries.
- Evidence packet schema.

## Output

Return one JSON document per compound matching `backend/src/BioStack.KnowledgeWorker/Schemas/evidence-packet.schema.json`.

## Claim extraction rules

- Every nontrivial claim must cite `sourceRefs`.
- Keep mechanism, outcome, safety, regulatory, dosing, and misinformation claims separate.
- Use exact source excerpts where possible in `extractedEvidence`.
- Mark `fieldAuthorityRequired = true` for regulatory, safety-critical, monitoring, product-specific dosing, formulation, and storage/reconstitution claims.
- If evidence is weak or conflicting, state that in claims, `conflicts`, `reviewFlags`, and `ops.reviewReasons`.
- Do not infer human efficacy from animal/in vitro evidence.
- Do not convert common online use into proven benefit.
- Do not write medical advice.

## Required misinformation handling

If forums/social/vendor pages contain popular claims, capture them as `misinformation-claim`, `controversy`, or `evidence-gap` unless supported by stronger sources.
