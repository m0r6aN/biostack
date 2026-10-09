# P3-A Remediation — Gate 2 Dispatch Record (round-4 review fixes)

Coordinator-owned record, frozen at dispatch (2026-10-09). The P3-A implementation (PR #525,
merged `4fe5825` by the owner at Gate 3) received retrospective dual adversarial review:
both reviewers **PASS-WITH-FIXES**; reviewer 2 empirically demonstrated two BLOCKER-class
verifier weaknesses (adversarial inputs passed `verify-p3a.ps1`). Coordinator reproduced both
at code level (lines 842–843 shape-only regex; line 625 whole-file `.Contains`). Triage in
`TRIAGE-2026-10-08.md` §Round 4. Spec amendment **A1** (document contract 9 link strings) already
committed alone (`6e497d1`).

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "P3-A-REMEDIATION-1",
  "builderId": "p3a_remediation_builder",
  "branch": "fix/p3a-remediation-1",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/p3a-remediation-1",
  "baseCommit": "6e497d1 (spec amendment A1 on main)",
  "permissionEnvelope": "docs-only:p3a-remediation-surfaces",
  "builderSurfaces": "exactly three files: docs/specs/scripts/verify-p3a.ps1, docs/specs/schemas/fixtures/p3a/REAL-SPEC-COMPATIBILITY-SET.md (or templates/README.md if the fixer judges the F1 paragraph fits there better — one location only), docs/specs/README.md (the P3-A appended section's four link strings ONLY)",
  "reviewerIds": ["p3a_impl_review_1", "p3a_impl_review_2"],
  "risk": "standard; architecture — targeted dual re-verification by the same independent reviewers (fresh runs)",
  "verificationContract": "verify-p3a.ps1 checks 1–14 pass at the remediated hash AND both reviewers' adversarial exploits now FAIL against it"
}
```

Required fixes (triage §Round 4 `fix` rows):

1. **R2-F1 (BLOCKER):** check 11 must resolve the INDEX row's `Spec`/`Goal Charter` hrefs to the
   pinned literal paths (compare captured groups against exact paths), not assert regex shape.
2. **R2-F2 (BLOCKER):** check 7's pinned-sentence assertions must be scoped per subsection
   (each pinned sentence verified inside its own subsection), not whole-file `.Contains`.
3. **R2-F3 (MAJOR):** the fold-live required-section resolver must consult
   `SECTION-HEADING-MAP.md`'s "Canonical alias(es)" column per document contract 2's alias rule.
4. **R2-F4 (MINOR):** check 4's census must be computed as frozen at `BaseCommit`, not from the
   current working tree.
5. **R1-F1 (MAJOR):** add the carry-over-item-3 disposition paragraph (no `coordinator-parcel`-shape
   fixture exists; P3-B's dispatch must discharge or re-affirm it) to the chosen allowed file and
   assert its presence in check 10 (or a new check).
6. **R1-F2 (MINOR):** correct the four README link strings in the appended section to match
   amended document contract 9 (drop `../` prefixes). The README correction is a coordinator-
   authorized in-place correction of text this parcel itself appended; check 11 evaluates the
   cumulative diff from `BaseCommit`, so the append constraint is not violated.

Accept-as-documented (no code change required): R2-F5 (bounded confusables map — add a scope
disclosure sentence where the pipeline is described) and R2-F6 (8-word content floor, already
self-disclaimed).

Exit proof: verifier transcript at the remediated hash in the PR body + both reviewers'
demonstrated exploits fail. Delivery: commit → push → PR vs `main` → NEVER merge.
