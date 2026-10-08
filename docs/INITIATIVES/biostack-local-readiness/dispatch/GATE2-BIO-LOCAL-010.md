# BIO-LOCAL-010 Gate 2 Dispatch Record (seed batch C — final serial writer)

Coordinator-owned record, frozen at dispatch (2026-10-08). A1+A2 pattern per
GATE2-BIO-LOCAL-008-010.md (this dispatch activates the BIO-LOCAL-010 entry there). Batch B
merged at PR #489; you are the FINAL serial writer of the seed file.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "BIO-LOCAL-010",
  "specPath": "docs/specs/active/BIO-LOCAL-010-seed-expansion-batch-c.md",
  "specSha256": "b2b5ea86345aa1a911253d208d71d0ed84a6b7240cba78f1c5cb643c86e42cec",
  "builderId": "bio_local_010_builder",
  "branch": "proof/bio-local-010-seed-batch-c",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-010",
  "permissionEnvelope": "local-only:seed-batch-c:seed-file-plus-one-record",
  "allowedBuilderSurfaces": [
    "backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json",
    "docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-010-batch-c-record.md"
  ],
  "reviewerIds": ["bio_local_010_review_1"],
  "risk": "standard"
}
```

Dispatch notes: identical doctrine to batches A/B (unknown-honest tracking fields, verbatim-cited
claim strings, batch-C identifiers ONLY from the 007 inventory partition, collisions to human
rule, expected count-test failures recorded not fixed). Post-merge the corpus stands at 100 with
the 50-record gap documented pending KEO-73/74 sourcing (decision D-C).
