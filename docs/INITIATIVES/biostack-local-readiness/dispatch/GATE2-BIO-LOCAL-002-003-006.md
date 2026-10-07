# BIO-LOCAL-002 / -003 / -006 Gate 2 Dispatch Records

Coordinator-owned records, frozen at dispatch (2026-10-07). All three follow the BIO-LOCAL-001
pattern (GATE2-BIO-LOCAL-001.md) including its amendment A1 clause: the specs' `@e5b75e0` pin and
`D:\Repos\...` worktree literals predate the Windows→Linux move and are 725+ commits stale; the
named successor is the dispatch anchor below (the `origin/main` tip each builder branch forks),
with full re-verification by each parcel's own run at the exact SHA it records in evidence.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcels": [
    {
      "parcel": "BIO-LOCAL-002",
      "specPath": "docs/specs/active/BIO-LOCAL-002-knowledge-public-read-proof.md",
      "specSha256": "835dfcfc46c49c034c12230136ba36ed95c46e203eb89de8360a9d7515741bb5",
      "builderId": "bio_local_002_builder",
      "branch": "proof/bio-local-002-public-read",
      "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-002",
      "permissionEnvelope": "local-only:dev-compose-boot:one-evidence-file",
      "allowedBuilderSurfaces": ["docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-002-public-read-proof.md"],
      "reviewerIds": ["bio_local_002_review_1"],
      "risk": "standard"
    },
    {
      "parcel": "BIO-LOCAL-003",
      "specPath": "docs/specs/active/BIO-LOCAL-003-auth-tenancy-isolation-proof.md",
      "specSha256": "22eadecb4e4977ecdf5f047754842bb4a4c8058b284f36eb3d01660f773662eb",
      "builderId": "bio_local_003_builder",
      "branch": "proof/bio-local-003-auth-isolation",
      "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-003",
      "permissionEnvelope": "local-only:dev-compose-boot:one-evidence-file",
      "allowedBuilderSurfaces": ["docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-003-auth-isolation-proof.md"],
      "reviewerIds": ["bio_local_003_review_1", "bio_local_003_review_2"],
      "risk": "elevated",
      "reviewNote": "elevated class — dual independent review (D8), one reviewer leads on hostile-input/denial-path reproduction"
    },
    {
      "parcel": "BIO-LOCAL-006",
      "specPath": "docs/specs/active/BIO-LOCAL-006-product-contract-mirror-proof.md",
      "specSha256": "8b453416b73fcc33d157f074c7bd8f35d137c1aecdd1fa334fd19d41f903031b",
      "builderId": "bio_local_006_builder",
      "branch": "proof/bio-local-006-contract-mirrors",
      "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-006",
      "permissionEnvelope": "local-only:dev-compose-boot:one-evidence-file",
      "allowedBuilderSurfaces": ["docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-006-contract-mirror-proof.md"],
      "reviewerIds": ["bio_local_006_review_1"],
      "risk": "standard"
    }
  ]
}
```

## Dispatch notes (apply to all three)

- Spec text wins on substance; where a spec's Windows worktree literal or `e5b75e0` pin conflicts
  with this record, this record governs (A1 pattern) and the builder records the exact tested SHA
  in its evidence (lesson from BIO-LOCAL-001 review F1: pin the SHA actually under test).
- Docker via `newgrp docker`; `.env` placeholders only; product code READ-ONLY; stop-and-report on
  any product defect (bounded remediation parcels follow, never in-parcel fixes).
- Delivery: evidence commit on the named branch → push → PR vs `main` (Gate 3 is the owner's) →
  worktree removed → branch kept. BIO-LOCAL-003 is elevated: two independent reviewers replay and
  probe before Gate 3.
