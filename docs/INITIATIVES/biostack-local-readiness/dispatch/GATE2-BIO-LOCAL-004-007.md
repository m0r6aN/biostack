# BIO-LOCAL-004 / -007 Gate 2 Dispatch Records

Coordinator-owned records, frozen at dispatch (2026-10-07). A1 pattern applies (see
GATE2-BIO-LOCAL-002-003-006.md): this record governs over stale `@e5b75e0` pins and Windows
worktree literals; builders record the exact tested SHA in evidence.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcels": [
    {
      "parcel": "BIO-LOCAL-004",
      "specPath": "docs/specs/active/BIO-LOCAL-004-governance-spine-receipt-proof.md",
      "specSha256": "4d7e7381ce6ba434610b4c8484b0979778d7a2dfe272f0b36ad00cadf2005d08",
      "builderId": "bio_local_004_builder",
      "branch": "proof/bio-local-004-spine-receipts",
      "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-004",
      "permissionEnvelope": "local-only:dev-compose-boot:one-evidence-file",
      "allowedBuilderSurfaces": ["docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-004-spine-receipt-proof.md"],
      "reviewerIds": ["bio_local_004_review_1", "bio_local_004_review_2"],
      "risk": "elevated (architecture/risk — dual independent review per D8)"
    },
    {
      "parcel": "BIO-LOCAL-007",
      "specPath": "docs/specs/active/BIO-LOCAL-007-seed-gap-inventory.md",
      "specSha256": "6eb7c0786af0cf5a2cc59d4bf6c6edbb62b6d9d691af666a006c4a989d3891fb",
      "builderId": "bio_local_007_builder",
      "branch": "proof/bio-local-007-seed-gap-inventory",
      "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-007",
      "permissionEnvelope": "local-only:inventory-analysis:one-evidence-file",
      "allowedBuilderSurfaces": ["docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-007-seed-gap-inventory.md"],
      "reviewerIds": ["bio_local_007_review_1"],
      "risk": "standard",
      "gateNote": "D10 reachability verdict gates BIO-LOCAL-008/009/010 (seed batches) and -011; an unreachable 150 stops the corpus chain and returns to the owner"
    }
  ]
}
```

Dispatch notes: as GATE2-BIO-LOCAL-002-003-006.md (exact SHA in evidence, docker via `newgrp
docker`, placeholders-only `.env`, product code READ-ONLY, stop-and-report, evidence-only PR,
worktree cleanup, branch kept). BIO-LOCAL-004 additionally must not oversell stubbed receipt
anchoring posture.
