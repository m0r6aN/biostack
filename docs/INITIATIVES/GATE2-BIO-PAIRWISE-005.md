# BIO-PAIRWISE-005 Gate 2 Dispatch Record (first sourced negative pair)

Coordinator-owned record, frozen at dispatch (2026-10-08). Spec review chain COMPLETE: original
REJECT → D-E rulings → amendment → double APPROVE-WITH-FIXES → fix-2 residue applied (PR #514,
incl. anti-self-attestation cross-check). Class: **knowledge-promotion** (D-E(2)) — D14 controls
bind: dual review + promotion-authority separation + rollback story.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "BIO-PAIRWISE-005",
  "specPath": "docs/specs/active/BIO-PAIRWISE-005-first-sourced-negative-pair.md",
  "builderId": "bio_pairwise_005_builder",
  "branch": "feat/bio-pairwise-005-sourced-negative-pair",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-PAIRWISE-005",
  "permissionEnvelope": "local-only:sourced-pair-per-spec",
  "builderSurfaces": "exactly the spec's Allowed Files section (frontmatter surfaces govern per PW-003/PW-004 precedent if they differ)",
  "reviewerIds": ["bio_pairwise_005_review_1", "bio_pairwise_005_review_2"],
  "risk": "elevated (knowledge-promotion — dual review; promoter ≠ approver enforced)",
  "dispatchWhen": "immediate"
}
```

Dispatch notes: D-E(1) sourcing split BINDS — verbatim-derivation from already-authorized cited
lane sources ONLY; if the builder encounters evidence requiring NEW external acquisition, it
STOPs and reports (that triggers the KEO-73/74 gate review per D-F(3) — never acquire silently).
Negative-pair harm framing: evidence-graded with population/context qualifiers; never universal.
Promotion lifecycle per the spec's knowledge-promotion sections (promotion authority ≠ builder).
If spec surfaces prove insufficient: STOP-AND-REPORT (PW-003/PW-004 pattern). Delivery: commit →
push → PR vs `main` (Gate 3 owner's) → worktree removed.
