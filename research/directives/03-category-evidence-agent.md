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

## Independent review rule

- Treat review as cross-source verification, not a second pass over the same references.
- When resolving `ops.reviewReasons`, `conflicts`, safety-critical claims, regulatory claims, dosing/formulation claims, or requested changes, check at least one materially different source family than the sources already used for the claim.
- Prefer higher-authority independent sources: regulator/label data, professional guidance, systematic reviews, controlled human studies, structured databases, or paper-level primary sources as appropriate to the claim.
- If no independent source is available, keep the claim review-gated and record the gap in `conflicts`, `reviewFlags`, and `ops.reviewReasons` instead of re-affirming the original source.
- Do not mark a review issue resolved merely because the original source was re-read.
- For partially complete review, keep searching across additional independent source families until the worker's configured limit (`Worker:ResearchReviewSourceExpansionLimit`) is reached; after that, leave the item human-review gated with the remaining gap documented.
- During follow-up review, treat the attached remediation plan item IDs, resolution types, original recommended actions, and related review queue item IDs as controlling context. Resolve those items directly or carry them forward unchanged if independent sources do not close them.

## Required misinformation handling

If forums/social/vendor pages contain popular claims, capture them as `misinformation-claim`, `controversy`, or `evidence-gap` unless supported by stronger sources.
