# P3-A Implementation Gate 2 Dispatch Record

Coordinator-owned record, frozen at dispatch (2026-10-08/09). P3-A's SPEC completed its review
chain: round-1 dual adversarial review REJECT/REJECT → rework 3 (#521) → round-2 targeted dual
REJECT/AWF → rework 3b (`7df5ef4`) → round-3 targeted AWF → rework 3c (`be81530`, reviewer's
exact smallest amendment). Chain COMPLETE at `be81530`, merged at Gate 3 (#523, `aba0eb9`).
All findings dispositioned in `TRIAGE-2026-10-08.md`; no locked decision touched. This record
dispatches the P3-A **implementation** (the spec's deliverables).

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "P3-A-IMPL",
  "specPath": "docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md",
  "specSha256Note": "exact shipped hash = this record's BaseCommit tree; the builder records the exact tested SHA",
  "builderId": "p3a_builder",
  "branch": "feat/biostack-governance-p3a",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/governance-p3a",
  "baseCommit": "single anchor = the commit that adds THIS file to main (P3-A single-anchor dispatch model)",
  "permissionEnvelope": "docs-only:p3a-exact-surfaces",
  "builderSurfaces": "exactly the spec's allowed-surfaces list (28 items: parcel-spec.schema.json, template set, EXTENSION-POINTS.md, fixtures/p3a/** incl. manifest, REAL-SPEC-COMPATIBILITY-SET.md, README/INDEX appends, and the spec's named verifier inputs)",
  "reviewerIds": ["p3a_impl_review_1", "p3a_impl_review_2"],
  "risk": "standard; architecture — dual independent review (charter D8)",
  "verificationContract": "the spec's 'Deterministic verification' section, checks 1–10, run via the spec's pwsh verifier at the built hash"
}
```

Dispatch notes (P3-A spec is the sole build authority):

- Build EXACTLY what `parcels/P3-A.md` specifies: the generic extensible parcel-spec schema
  (document contract 1, 15 top-level keys incl. `extensionSections: {}`), template set
  (contract 2), `EXTENSION-POINTS.md` (contract 3), the extension registry (contract 4), the
  no-placeholder machinery (contracts 5–6: normalization incl. `decode-html-entities`, ordinal
  term-processing order, independently-dispositive negative fixtures), the compatibility-set
  materials (contract 7, bound to the **frozen P2 census**), README/INDEX appends (contracts
  8/10 + registry row), and the verifier (contract 9) that executes checks 1–10.
- The verifier's transcript goes in the PR body; the builder records the exact spec SHA tested.
- Boundary tripwire: P3-A adds **no** product capability, claim, evidence-missingness,
  function-review, or escalation semantics (P3-B/P0-B territory). Any deliverable that would
  decide a product allowed-output → STOP-AND-REPORT.
- Frozen surfaces: charter, plan review, P1/P2 records, all canon docs. No other file may change.
- Delivery: commit → push → PR vs `main` (Gate 3: green-chain merge per D-H after dual review) →
  worktree removed.
