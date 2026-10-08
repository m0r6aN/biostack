# BIO-LOCAL-014 Gate 2 Dispatch Record (seed identity consolidation — owner rules D-D)

Coordinator-owned record, frozen at dispatch (2026-10-08). Executes decision D-D
(COORDINATOR-DECISIONS-2026-10-07.md): the owner's human rules for the two open identity
collisions.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "BIO-LOCAL-014",
  "contract": "bounded-consolidation:owner-rules-D-D",
  "builderId": "bio_local_014_builder",
  "branch": "fix/bio-local-014-identity-consolidation",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-014",
  "permissionEnvelope": "local-only:seed-file-plus-count-tests-plus-evidence",
  "allowedBuilderSurfaces": [
    "backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json",
    "backend/tests/BioStack.KnowledgeWorker.Tests/CorpusIdentityInventoryBuilderTests.cs",
    "backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationReportBuilderTests.cs",
    "docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-014-identity-consolidation.md"
  ],
  "reviewerIds": ["bio_local_014_review_1"],
  "risk": "standard",
  "acceptanceCriteria": [
    "AC1 (D-D rule 1): creatine + creatine-monohydrate remain DISTINCT records; cross-references added in schema-supported fields only, else documented in the evidence record; no invented schema fields",
    "AC2 (D-D rule 2): chorionic-gonadotropin + human-chorionic-gonadotropin consolidated into ONE record, canonicalName = human-chorionic-gonadotropin, the shorthand recorded as alias per schema capability (else documented); no claim strings lost (deduplicate, don't delete content)",
    "AC3: corpus total = 99; the two count-asserting test files updated EXPLICITLY with the arithmetic in the evidence",
    "AC4: evidence record quotes the owner's rulings verbatim, lists before/after per affected record, and records any knock-on expectation shifts in recorded-failure lists (record, never fix)",
    "AC5: schema/validator green on the full file; git diff --check clean; scope confined to the allowed surfaces or STOP-AND-REPORT"
  ]
}
```

Dispatch notes: unknown-honest doctrine preserved (tracking fields unchanged per record unless the
doctrine says otherwise); no record content is invented — consolidation merges existing content
only. Delivery: commit → push → PR vs `main` (Gate 3 owner's) → worktree removed.
