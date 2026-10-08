# BIO-LOCAL-013 Gate 2 Dispatch Record (bounded remediation — rollback/truncation gap)

Coordinator-owned record, frozen at dispatch (2026-10-08). Remediation for the blocker found by
`bio_local_004_retro_reviewer_2` in the BIO-LOCAL-004 chain:

**Finding R1 (BLOCKER, tamper-resistance):** deleting trailing spine rows rolls the chain back to
a stale-but-genuine checkpoint and verification reports `IsFullyValid = true` — truncation is
silently accepted and the limitation is disclosed nowhere (evidence, docstrings, README).

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "BIO-LOCAL-013",
  "contract": "bounded-remediation:R1-from-BIO-LOCAL-004-retro-review-2",
  "builderId": "bio_local_013_builder",
  "branch": "fix/bio-local-013-spine-truncation-detection",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-013",
  "permissionEnvelope": "local-only:spine-module-plus-tests-plus-doc-strings",
  "builderSurfaces": "the governance spine module + its tests + the module's docstrings/README claims ONLY (backend/src/BioStack.Domain/Governance, backend/src/BioStack.Infrastructure/Governance, their test files; nothing else)",
  "reviewerIds": ["bio_local_013_review_1", "bio_local_013_review_2"],
  "risk": "elevated (trust-adjacent — dual review)",
  "acceptanceCriteria": [
    "AC1: truncation/rollback detection — verification FAILS when trailing rows are deleted (head/sequence anchoring: e.g. expected head hash + monotonic sequence enforced across verification and the fail-closed boot check)",
    "AC2: the previously-accepted rollback probe is a named regression test and now fails closed",
    "AC3: all prior spine/checkpoint/receipt tests remain green; no behavior change to legitimate round-trips",
    "AC4: docstrings/README evidence language states precisely what truncation resistance IS and IS NOT proven locally (no overclaim)",
    "AC5: git diff --check clean; scope confined to the named surfaces or STOP-AND-REPORT"
  ],
  "stopCondition": "If AC1 cannot be achieved inside the named surfaces (e.g. it needs an external anchoring design), STOP-AND-REPORT: the coordinator will shape a trust-path parcel instead"
}
```

Dispatch notes: standard SHA-in-evidence discipline; product code changes limited to the named
surfaces; dual independent review before Gate 3; rollback = revert commit. This remediation lifts
the BIO-LOCAL-004 chain's open blocker when merged + reviewed.
