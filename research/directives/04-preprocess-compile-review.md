# Agent Directive — Preprocess, Compile, Review

## Mission

Transform raw agent research outputs into validated BioStack drafts and review artifacts.

## Steps

1. Archive raw agent output unchanged.
2. Validate each artifact against its schema.
3. Normalize compound names and aliases.
4. Resolve source IDs against the source registry.
5. Run claim-level authority checks.
6. Reject malformed packets.
7. Compile draft substance records only from validated evidence packets.
8. Validate compiled drafts against `substance-record.schema.json`.
9. Emit review queue items for missing authority, unresolved conflicts, and safety-critical claims.

## Review contract

- Review is an independent verification step. It must compare the draft claim against different knowledge sources than the evidence packet originally used.
- A review pass must identify which original source family is being challenged or confirmed, then consult a materially different source family before resolving the issue.
- Valid independent source families include regulator/label, clinical guideline/professional society, systematic review/meta-analysis, controlled human study, paper-level primary literature, and structured authority database.
- Re-reading the same source, source family, summary page, vendor page, forum post, or model-generated recap is not sufficient to clear review.
- If the independent check cannot confirm the claim, keep `ops.needsReview = true` and carry the unresolved issue forward in the review queue.
- If review is partial, automatically perform source-expansion passes against additional independent source families until the configured worker limit (`Worker:ResearchReviewSourceExpansionLimit`) is reached; only then leave the item as partially complete and needing human review.
- Follow-up source-expansion tasks must preserve the original remediation plan context, including remediation item IDs, resolution types, recommended actions, and related review queue item IDs.

## Publish criteria

- Draft substance records are not publishable by default.
- `ops.needsReview` must remain true until human or expert review.
- Safety-critical fields require A1/A2 backing or explicit unknown/review status.
- Runtime must never consume unpublished draft records.
