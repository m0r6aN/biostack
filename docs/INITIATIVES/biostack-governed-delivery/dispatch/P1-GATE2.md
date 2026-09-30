# P1 Gate 2 Dispatch Record

This coordinator-owned record is frozen at dispatch. Its containing commit is named separately in the dispatch envelope as the dispatch anchor.

```json
{
  "schema": "biostack.p1-gate2.v1",
  "status": "approved",
  "parcel": "P1",
  "specPath": "docs/INITIATIVES/biostack-governed-delivery/parcels/P1.md",
  "specSha256": "A55235E81FF251B7A67620917522B9173F697D9776336C98D03B441F366C22BA",
  "comparisonBase": "8206dd6501912ac41ca003e3dbab208477211837",
  "builderId": "p1_builder",
  "branch": "codex/biostack-governance-p1",
  "worktree": "D:/Repos/BioStack-governance-p1",
  "permissionEnvelope": "docs-only:p1-eight-surfaces",
  "evidenceDirectory": "artifacts/p1-verification",
  "reviewerIds": [
    "p1_impl_review_1",
    "p1_impl_review_2"
  ],
  "verificationContract": "p1-two-anchor-verifier-v1",
  "allowedBuilderSurfaces": [
    "AGENTS.md",
    "docs/INITIATIVES/biostack-governed-delivery/README.md",
    "docs/specs/CORE-CONTEXT.md",
    "docs/specs/INDEX.md",
    "docs/specs/README.md",
    "docs/specs/active/README.md",
    "docs/specs/done/README.md",
    "docs/specs/scripts/verify-p1.ps1"
  ],
  "frozenSurfaces": [
    "docs/INITIATIVES/biostack-governed-delivery/CHARTER.md",
    "docs/INITIATIVES/biostack-governed-delivery/PLAN-REVIEW.md",
    "docs/INITIATIVES/biostack-governed-delivery/dispatch/P1-GATE2.md",
    "docs/INITIATIVES/biostack-governed-delivery/parcels/P1.md"
  ]
}
```
