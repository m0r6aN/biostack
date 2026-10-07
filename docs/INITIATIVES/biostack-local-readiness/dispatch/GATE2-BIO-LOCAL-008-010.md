# BIO-LOCAL-008 / -009 / -010 Gate 2 Dispatch Records (SERIAL chain)

Coordinator-owned records, frozen at dispatch (2026-10-07). **Serialization is mandatory**: all
three write `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`. Dispatch order is
strictly 008 → 009 → 010; each next parcel starts only after the previous PR is MERGED and its
branch rebased onto the new main. No parallel seed edits under any circumstances.

Amendment A2 (see COORDINATOR-DECISIONS-2026-10-07.md, decision D-C): the specs' `reachable-150`
precondition is satisfied by BIO-LOCAL-007's `VERDICT: REACHABLE-100` plus the owner's option-(a)
ruling. Batch ID lists are exactly the three partitions recorded in
`evidence/BIO-LOCAL-007-seed-gap-inventory.md`. Target = the reachable 43 records (→ 100 total);
the 50-record gap is held pending KEO-73/74 sourcing.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcels": [
    {
      "parcel": "BIO-LOCAL-008",
      "specPath": "docs/specs/active/BIO-LOCAL-008-seed-expansion-batch-a.md",
      "specSha256": "f697a3b1ee0993b44bb38327ddf74359e0db42b1d59a1d392a4f05c0523d72ba",
      "builderId": "bio_local_008_builder",
      "branch": "proof/bio-local-008-seed-batch-a",
      "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-008",
      "permissionEnvelope": "local-only:seed-batch-a:seed-file-plus-one-record",
      "allowedBuilderSurfaces": [
        "backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json",
        "docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-008-batch-a-record.md"
      ],
      "reviewerIds": ["bio_local_008_review_1"],
      "risk": "standard",
      "dispatchWhen": "immediate"
    },
    {
      "parcel": "BIO-LOCAL-009",
      "specPath": "docs/specs/active/BIO-LOCAL-009-seed-expansion-batch-b.md",
      "specSha256": "4dd56ceb6fb8aa13bd3bdf0bf476c4f544a4bdbfc517c87097043c414da728c8",
      "builderId": "bio_local_009_builder",
      "branch": "proof/bio-local-009-seed-batch-b",
      "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-009",
      "permissionEnvelope": "local-only:seed-batch-b:seed-file-plus-one-record",
      "allowedBuilderSurfaces": [
        "backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json",
        "docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-009-batch-b-record.md"
      ],
      "reviewerIds": ["bio_local_009_review_1"],
      "risk": "standard",
      "dispatchWhen": "after BIO-LOCAL-008 merge"
    },
    {
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
      "risk": "standard",
      "dispatchWhen": "after BIO-LOCAL-009 merge"
    }
  ]
}
```

Dispatch notes (all three):
- A1 pattern applies (this record governs stale spec pins; exact tested SHA recorded in the
  batch record).
- Unknown-honest doctrine in full: `reviewStatus: draft`, `needsReview: true`, `isActive: false`,
  `completeness: partial` unless fully evidenced, `ops.lastChangeType: seed`; unknown fields stay
  unknown; **every claim sentence copied verbatim from its cited evidence-packet source line**
  (file:line in the batch record); zero invented content; identity collisions listed for human
  rule, never auto-merged.
- Required tests: schema/validator pass on the full seed file (command + output in the record);
  the full KnowledgeWorker lane run is recorded (count-test impacts noted as EXPECTED FAILURES to
  be updated by BIO-LOCAL-011 — never fixed in the batch parcels).
- Delivery: seed-file + record commit → push → PR vs `main` (Gate 3 is the owner's) → worktree
  removed → branch kept until merge.
