# HARDENING H2-R2 Gate 2 Dispatch Record (urgent — Finding C regression)

Coordinator-owned record, frozen at dispatch (2026-10-08). PR #502 merged with a CRITICAL review
finding (h2_review_2, FAIL). The finding is precise and reproduced with the real Npgsql driver:

**Finding C (CRITICAL/blocker):** `SpineHeadWatermarkStore.ResolveDefaultPath` assumes
path-shaped `DataSource` (SQLite). For Npgsql, `DataSource` = `tcp://host:port` → (1) distinct
databases on one host:port derive the IDENTICAL watermark path (cross-tenant anchor clobbering),
(2) the derived path is CWD-relative and evaporates on container redeploys, silently reopening R1
every deploy cycle. 428/428 tests are `UseSqlite`; zero Postgres coverage. The "on by default,
fresh install protected" claim is false for the only provider Production may run.

**Finding D (MEDIUM):** the watermark file is predictably named (`{dbfile}.spine-watermark`) and
colocated with the DB — deleting rows + that file accepts the rollback. The "raises the bar"
claim overstates reality.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "H2-R2",
  "contract": "bounded-remediation:findings-C-D-from-h2-review-2",
  "builderId": "h2_r2_builder",
  "branch": "fix/h2r2-watermark-provider-safety",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/H2-R2",
  "permissionEnvelope": "local-only:spine-module-plus-tests",
  "builderSurfaces": "the governance spine module + its tests ONLY (backend/src/BioStack.Domain/Governance, backend/src/BioStack.Infrastructure/Governance, their test files)",
  "reviewerIds": ["h2r2_review_1", "h2r2_review_2"],
  "risk": "elevated (trust-adjacent — dual review)",
  "acceptanceCriteria": [
    "AC1 (C): provider-safe default watermark derivation — unique per database (database/catalog identity in the key, not just host:port), and ROOTED (absolute, deployment-stable location — never CWD-relative). Postgres + SQLite semantics both covered by tests exercising realistic provider DataSource shapes",
    "AC2 (C): docstrings/README state the provider-specific semantics accurately (no universal claim over provider-specific behavior)",
    "AC3 (D): either mutual binding (the chain head references the watermark digest so deleting the file fails closed) OR the claims are corrected to state exactly what the watermark does and does not raise — prefer mutual binding if achievable in-module; stale-snapshot-restore may remain a disclosed limit",
    "AC4: full regression suite green incl. R1/R2 probes and prior BIO-LOCAL-013/H2 tests; git diff --check clean; scope confined or STOP-AND-REPORT"
  ]
}
```

Dispatch notes: the review's probe reproductions are in
/logs/h2-review-2.log (coordinator scratch) — the builder re-derives them from the finding text.
Gate 3 = owner. Dual review must include Postgres-semantics verification (the gap that shipped).
