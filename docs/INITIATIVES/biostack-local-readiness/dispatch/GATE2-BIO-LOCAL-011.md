# BIO-LOCAL-011 Gate 2 Dispatch Record (seed run + serving proof — corpus finale)

Coordinator-owned record, frozen at dispatch (2026-10-08). The FINAL local-readiness corpus
parcel: it runs the SeedJob over the 100-record corpus (57 + 43 from batches A/B/C), proves
serving, updates the frozen count-asserting tests EXPLICITLY to the actual counts (D10's in-parcel
rule), and records the 50-record gap (decision D-C) for the record.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "BIO-LOCAL-011",
  "specPath": "docs/specs/active/BIO-LOCAL-011-seed-run-and-serving-proof.md",
  "specSha256": "1d47a399456f9c37a58595421f54e8d6870ab9fd93f626cc41dfd16c5f23e868",
  "builderId": "bio_local_011_builder",
  "branch": "proof/bio-local-011-seed-run-proof",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-011",
  "permissionEnvelope": "local-only:two-test-files-plus-evidence",
  "allowedBuilderSurfaces": [
    "backend/tests/BioStack.KnowledgeWorker.Tests/CorpusIdentityInventoryBuilderTests.cs",
    "backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationReportBuilderTests.cs",
    "docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-011-seed-run-proof.md"
  ],
  "reviewerIds": ["bio_local_011_review_1"],
  "risk": "standard"
}
```

Dispatch notes: A1 pattern (this record governs stale spec pins; exact tested SHA in evidence).
Docker via `newgrp docker`. Count-test updates must be EXPLICIT (asserted counts changed to the
real values with the arithmetic in the evidence — never silent). The 50-record gap and its
KEO-73/74 sourcing path get a dedicated section in the evidence. Unknown-honest doctrine
preserved in every record untouched here. Delivery: commit → push → PR vs `main` (Gate 3 owner's)
→ worktree removed.
