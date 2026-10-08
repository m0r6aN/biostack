# BIO-KEON-002 & BIO-PAIRWISE-002 Gate 2 Dispatch Records

Coordinator-owned records, frozen at dispatch (2026-10-08).

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcels": [
    {
      "parcel": "BIO-KEON-002",
      "specPath": "docs/specs/active/BIO-KEON-002-keon-compatible-language-replacement.md",
      "specSha256": "2b249d73156738943a6be914c67bec8ebaa77811a119ba866c3323fac132b288",
      "builderId": "bio_keon_002_builder",
      "branch": "docs/keon-k3-language-replacement",
      "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-KEON-002",
      "permissionEnvelope": "docs-only:keon-k3-target-docs",
      "builderSurfaces": "exactly the spec's Allowed Files section (docs only)",
      "reviewerIds": ["bio_keon_002_review_1"],
      "risk": "standard",
      "dispatchWhen": "immediate (K-1 merged at PR #491; serialization satisfied)"
    },
    {
      "parcel": "BIO-PAIRWISE-002",
      "specPath": "docs/specs/active/BIO-PAIRWISE-002-publishable-relationship-ratification.md",
      "specSha256": "b540f53ffe35a239238ff7a5b7e85c58c1f076bc47c1f9fd57345c2a265d7ec5",
      "builderId": "bio_pairwise_002_builder",
      "branch": "proof/bio-pairwise-002-ratification",
      "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-PAIRWISE-002",
      "permissionEnvelope": "local-only:ratification-outputs-per-spec",
      "builderSurfaces": "exactly the spec's Allowed Files section",
      "reviewerIds": ["bio_pairwise_002_review_1", "bio_pairwise_002_review_2"],
      "risk": "elevated (health-boundary-adjacent publishing decisions — dual review per D14 fold)",
      "dispatchWhen": "immediate (spec review APPROVE-WITH-FIXES → fixes merged at PR #490 → coordinator triage closed)"
    }
  ]
}
```

Dispatch notes:
- BIO-KEON-002: K-1's wording rules apply; use "Keon-compatible offline inspection" per the spec's
  wording table; never imply an existing Keon dependency; preserve BioStack-owned language
  verbatim; K-1's merged changes are your baseline — do not regress them.
- BIO-PAIRWISE-002: consumes P0's census outputs (no re-census); the publishable-vs-quarantine
  decision procedure must run exactly as the spec defines; uncertainty preserved, no uncertain
  evidence presented as established (product doctrine); fixtures for positive + quarantine paths
  per the spec's Required Tests. Verbatim-citation discipline as in the seed chain.
