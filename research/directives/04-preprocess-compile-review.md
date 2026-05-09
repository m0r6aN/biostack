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

## Publish criteria

- Draft substance records are not publishable by default.
- `ops.needsReview` must remain true until human or expert review.
- Safety-critical fields require A1/A2 backing or explicit unknown/review status.
- Runtime must never consume unpublished draft records.
