# P4 Implementation Gate 2 Dispatch Record — Deterministic Validation (Spec Linter)

Coordinator-owned record, frozen at dispatch (2026-10-10). P4's spec completed its review chain
(dual review AWF/AWF → fix 1 PR #536 → targeted re-review AWF → micro-fix `c97109f`/PR #537,
amendments A-P4-1). Covered by the charter's **P1–P7 standing authorization** (standard +
architecture dual review).

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "P4-IMPL",
  "specPath": "docs/INITIATIVES/biostack-governed-delivery/parcels/P4.md",
  "builderId": "p4_builder",
  "branch": "feat/p4-spec-linter",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/p4-impl",
  "baseCommit": "RE-ANCHORED: 152ec94 (PR #537 merge — final reviewed spec state incl. A-P4-1); the record's original anchor fdb83ee is superseded — see ANCHOR NOTE in the queue log",
  "permissionEnvelope": "docs-only:p4-linter-surfaces",
  "builderSurfaces": "exactly the spec's allowed surfaces (verify-p4.ps1, the 16 fixtures, spec-linter inputs — zero governance-contract edits, zero product code)",
  "reviewerIds": ["p4_impl_review_1", "p4_impl_review_2"],
  "risk": "standard; architecture — dual independent review",
  "verificationContract": "the spec's Deterministic verification section (12 checks); exit proof = full fixture suite PASS + every named evasion class from the closures rejected + the RR3 build-time tightening applied"
}
```

Dispatch notes:

- **RR3 build-time tightening (required, not optional):** once the two exclusion fixtures are
  authored, `verify-p4.ps1`'s check 8 must hardcode the one actual JSON-pointer path used —
  no two live candidate branches left in the verifier.
- Required evasion-class rejections (the initiative's own scar tissue): homoglyph TBD,
  empty/malformed-input crashes, spliced quotes, merged-clause smuggles, vacuous P3-B bindings,
  semantic drift via ID mis-reference, hardcoded-linter AC-P4-02 adversary.
- Composition-verification mutations must be the pinned per-source mutations from amendment
  A-P4-1 (RR2), not illustrative examples.
- Standard verifier lessons: pin anchor pairs (BaseCommit = this record's commit; HEAD = builder
  tip); guard empty-content paths; byte-pin normative text; closed-world over blocklists; honest
  scope claims; STOP-AND-REPORT on any deviation (D-L enforcement).
- Delivery: commit → push → PR vs `main` (Gate 3: owner) → worktree removed.
