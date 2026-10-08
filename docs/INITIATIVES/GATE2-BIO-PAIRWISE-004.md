# BIO-PAIRWISE-004 Gate 2 Dispatch Record (interaction hint provenance & quarantine)

Coordinator-owned record, frozen at dispatch (2026-10-08). Spec review: **APPROVE** (one minor
non-blocking clarification — the builder honors the reviewer's clarification note inline; it does
not change scope).

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "BIO-PAIRWISE-004",
  "specPath": "docs/specs/active/BIO-PAIRWISE-004-interaction-hint-provenance.md",
  "builderId": "bio_pairwise_004_builder",
  "branch": "feat/bio-pairwise-004-hint-provenance",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-PAIRWISE-004",
  "permissionEnvelope": "local-only:hint-provenance-per-spec",
  "builderSurfaces": "exactly the spec's Allowed Files section",
  "reviewerIds": ["bio_pairwise_004_review_1", "bio_pairwise_004_review_2"],
  "risk": "elevated (health-boundary-adjacent hints — dual review per D14 fold)",
  "dispatchWhen": "immediate"
}
```

Dispatch notes: provenance is MANDATORY on every interaction hint — provenance-less hints are
quarantined (fail-closed), never published. Consumes upstream outputs (P0 census, P2 ratification,
P3 wiring as merged); no re-census, no schema change. Uncertain evidence is never presented as
established (product doctrine). Required Tests per the spec (provenance-present +
provenance-absent→quarantine fixtures minimum). If the spec's surfaces prove insufficient (the
PW-003 lesson), STOP-AND-REPORT rather than expanding scope. Delivery: commit → push → PR vs
`main` (Gate 3 owner's) → worktree removed.
