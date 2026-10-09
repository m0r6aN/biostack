# P0-B Implementation Gate 2 Dispatch Record — Product Capability and Safety Contract

Coordinator-owned record, frozen at dispatch (2026-10-09). This is the build that decides —
in machine-readable, enforceable form — **what the product may say**. Its normative content is
the owner-ruled frozen matrix (D-B1..D-B6; `P0-B-DESIGN-GATE.md`, ruled in ledger **D-I**),
plus definitional amendments **D-J** (dose-context) and **D-K** (precedence directional
constraint, as inherited context). The P0-B spec (PR #526 + amendment #529) is its build
authority. **Dispatch precondition: P0-A canon-precedence freeze — satisfied at PR #530 merge**
(`closures/P0-A.md`). This record takes effect at that moment; the builder's base is the commit
that adds this record to `main`.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "P0-B-IMPL",
  "specPath": "docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md",
  "builderId": "p0b_builder",
  "branch": "feat/p0b-product-capability-contract",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/p0b-impl",
  "baseCommit": "single anchor = the coordinator's seal commit after P0-A closed at 2d24fdd (recorded at dispatch)",
  "permissionEnvelope": "docs-only:p0b-contract-surfaces",
  "builderSurfaces": "exactly the spec's allowed surfaces (the machine-readable Product Capability and Safety Contract artifact(s), its schema/validator, the enablement-state fields, and the spec's named verification inputs — zero product runtime code, zero canon-document edits)",
  "reviewerIds": ["p0b_impl_review_1", "p0b_impl_review_2"],
  "risk": "health-boundary + privacy + legal-policy + knowledge-promotion — dual independent review AND recorded human approval at merge (D-G/D14 fold)",
  "verificationContract": "the spec's Deterministic verification section; exit proof = validator PASS + adversarial matrix-fidelity checks (every cell byte-equal to the frozen matrix) + enablement-state hard-fail test (biostack-recommended origination must FAIL enablement while publiclyEnabled is false)"
}
```

Dispatch notes:

- **The matrix is frozen doctrine.** Every one of the 10×4 cells, the preemption order, D-B3..D-B6
  rules, and `enablementState.publiclyEnabled: false` are encoded byte-faithfully. Any deviation →
  STOP-AND-REPORT (it is an owner-ruling change, not a design choice).
- **D-J's dose-context definition** is the normative scope semantics behind `dose-context-only`.
- **D-K travels as context**: the contract must never present rank-mechanics as license to weaken
  a safety prohibition.
- Verifier lessons (P3-A/P0-A closures): pin the anchor pair (BaseCommit = this record's commit;
  HEAD = builder tip) in any surface-census check; guard empty-content paths; quote verification
  must be structural-block contiguous.
- Delivery: commit → push → PR vs `main` (**merge is the owner's — 4-class human approval**) →
  worktree removed.
