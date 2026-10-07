# BIO-LOCAL-005 Gate 2 Dispatch Record

Coordinator-owned record, frozen at dispatch (2026-10-07). A1 pattern applies (see
GATE2-BIO-LOCAL-002-003-006.md).

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "BIO-LOCAL-005",
  "specPath": "docs/specs/active/BIO-LOCAL-005-guidance-contract-enforcement-proof.md",
  "specSha256": "0c301f9d1ecb5e1404ffec1b7ef29e3d90b597a51234f771df99684abf81006f",
  "builderId": "bio_local_005_builder",
  "branch": "proof/bio-local-005-guidance-enforcement",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-005",
  "permissionEnvelope": "local-only:dev-compose-boot:one-evidence-file",
  "allowedBuilderSurfaces": ["docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-005-guidance-enforcement-proof.md"],
  "reviewerIds": ["bio_local_005_review_1", "bio_local_005_review_2"],
  "risk": "elevated (guidance-contract enforcement — dual independent review per D8)"
}
```

Dispatch notes: as GATE2-BIO-LOCAL-002-003-006.md (exact SHA in evidence, docker via `newgrp
docker`, placeholders-only `.env`, product code READ-ONLY, stop-and-report, evidence-only PR,
worktree cleanup, branch kept).
