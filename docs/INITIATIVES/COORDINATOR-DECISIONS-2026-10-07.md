# Coordinator Decisions — 2026-10-07

Coordinator-owned record. Owner delegated decision authority for the pairwise lane in session
("I authorize you to make the decision") and ratified amendment R2 ("Ratify R2").

## R2 ratification (owner, 2026-10-07)

Amendment R2 in `biostack-governed-delivery/dispatch/P1-REWORK-2026-10-07.md` is **ratified**:
the worktree-literal equivalence mapping (`d:/repos/biostack-governance-p1` ≡
`/home/cmorgan76/Repos/biostack-wt/governance-p1`) stands as the accepted environment
reconciliation. The deviation remains disclosed in `CLOSURE-P1.md` as accepted, not hidden.
Fallback (full P1 re-dispatch) is not needed.

## Pairwise lane decisions (coordinator, under delegated authority)

### D-A — Schema sufficiency pre-gate (BIO-PAIRWISE-002 constraint)

**Decision: proceed with P0 dispatch; pre-authorize a bounded schema-change parcel if and only if
P0's census proves `relationship-packet.schema.json` insufficient. The lane is NOT de-scoped.**

- Rationale: the pairwise negative-relationship lane has direct product value (interaction
  intelligence and harm-reduction signals); de-scoping on a hypothetical schema gap would forfeit
  it. A schema change is legitimate governance work when evidence demands it — it just needs its
  own ratified parcel.
- If P0 finds the schema sufficient: BIO-PAIRWISE-002 proceeds on the existing frozen schema; no
  schema parcel is created.
- If P0 finds it insufficient: the coordinator shapes `BIO-PAIRWISE-SCHEMA-001` (bounded to the
  proven gaps: relationship types, assertion classes, source references, evidence tiers), and
  BIO-PAIRWISE-002 waits on it per dependency order. Schema change remains a separate ratified
  parcel exactly as the spec demands — this decision pre-authorizes its SHAPING, not its merge.

### D-B — Migration vs. redesign (BIO-PAIRWISE-003 constraint)

**Decision: P2 proceeds code-only in ALL outcomes. No migration is authored in P2 under any
circumstance.**

- If P0 recommends abandoning the non-authoritative input path: P2 makes that path explicitly
  inert (fail-closed, documented), keeping the authoritative path untouched.
- If P0 recommends the hard-coded arrays must stay: same shape — P2 marks the packet path
  non-authoritative and inert rather than migrating anything.
- If P0's finding genuinely cannot be honored code-only: P2 stops and reports; a migration would
  be a separate future parcel requiring owner approval. That door stays closed here.

Both decisions preserve the specs' hard constraints (no in-lane schema authoring, no migration)
while removing the pre-dispatch deadlock. Recorded for Gate 2 reference by BIO-PAIRWISE-001..006.
