# REAL-SPEC-COMPATIBILITY-SET — P3-A real-corpus read-only compatibility pass

Read-only compatibility pass of `parcel-spec.schema.json` against the **frozen P2 census**: every
`Spec file` row P2's own `docs/specs/schemas/AXIS-REGRESSION-MAP.md` already enumerates (26 files,
fixed by that file's own shipped content at its own pinned `BaseCommit` lineage), not a live
`git ls-files docs/specs/active docs/specs/done` query at this parcel's own `BaseCommit`. Corpus
growth after P2's `BaseCommit` (for example `docs/specs/active/BIO-FE-002-onboarding-route-
consolidation.md`, added after P2 closed) adds no row to this file and does not change the row
count; a file removed from the live corpus after P2's `BaseCommit` does not remove its row either.
This file does not edit, reconcile, or "fix" any inspected spec — a non-conforming result is a
correctly named, evidenced finding, not a validator defect.

## Carry-over items this file discharges

1. **Pre-existing ad hoc unresolved-placeholder cells in `docs/specs/INDEX.md`.** Several existing
   `INDEX.md` rows (`BIO-PAIRWISE-001` through `BIO-PAIRWISE-006`) carry, in their Branch/worktree
   and Owner columns, ad hoc cell text reading `(coordinator to assign Gate 2)` and a bracketed
   single-word marker — the exact literal this parcel's own `noPlaceholderPatterns` would flag if
   it appeared in a file this parcel validates — predating any enforced no-placeholder rule. This
   parcel does not edit `INDEX.md`'s existing rows
   (frozen surface, out of scope, the same restraint P2 applied to active/done specs); it instead
   names this exact, still-open gap here as a visible, traceable finding for a future
   reconciliation parcel or P4's general linter to enforce, satisfying the amendment by making it
   visible rather than by performing an out-of-scope edit.
2. **Structural (body-section) compatibility, not just frontmatter-field compatibility.** P2's
   `AXIS-REGRESSION-MAP.md` census checked only whether each existing spec's ad hoc classification
   *frontmatter* fields map onto the new closed-vocabulary axes; it never checked whether each
   spec's *body* contains the sections a fully composed schema would require. The
   `parcel-spec.schema.json result`/`Reason`/`Agreement` columns below are precisely this second,
   independent, structural-conformance layer, cross-referenced against P2's frontmatter-only
   dispositions — `docs/specs/done/BIO-FE-001-homepage-live-proof-panel.md` is the concrete proof
   point: P2 recorded `conforms` at the frontmatter layer, but this file's independent structural
   pass finds `invalid` / `missing-required-section (contracts)` (its body has no heading
   satisfying the `standard` class's `contracts` term), so `Agreement` for that row is
   `flagged-for-human-review`, not `consistent` — a disagreement P2's frontmatter-only census could
   never have surfaced, exactly the gap this carry-over item names.

## Compatibility table

| Spec file | Shape | AXIS-REGRESSION-MAP disposition | parcel-spec.schema.json result | Reason | Agreement |
|---|---|---|---|---|---|
| docs/specs/active/BIO-KEON-001-keon-ownership-docs-alignment.md | ticket-spec | needs-reconciliation | invalid | unknown-label (productGuidanceClass: not-applicable) | consistent |
| docs/specs/active/BIO-KEON-002-keon-compatible-language-replacement.md | ticket-spec | needs-reconciliation | invalid | unknown-label (productGuidanceClass: not-applicable) | consistent |
| docs/specs/active/BIO-PAIRWISE-001-relationship-substrate-and-label-census.md | ticket-spec | needs-reconciliation | invalid | unknown-label (productGuidanceClass: not-applicable) | consistent |
| docs/specs/active/BIO-PAIRWISE-002-publishable-relationship-ratification.md | ticket-spec | needs-reconciliation | invalid | unknown-label (productGuidanceClass: not-applicable) | consistent |
| docs/specs/active/BIO-PAIRWISE-003-relationship-projection-wiring.md | ticket-spec | needs-reconciliation | invalid | unknown-label (productGuidanceClass: not-applicable) | consistent |
| docs/specs/active/BIO-PAIRWISE-004-interaction-hint-provenance.md | ticket-spec | needs-reconciliation | invalid | unknown-label (productGuidanceClass: not-applicable) | consistent |
| docs/specs/active/BIO-PAIRWISE-005-first-sourced-negative-pair.md | ticket-spec | needs-reconciliation | invalid | unknown-label (productGuidanceClass: not-applicable) | consistent |
| docs/specs/active/BIO-PAIRWISE-006-studied-combinations-surface.md | ticket-spec | needs-reconciliation | invalid | unknown-label (productGuidanceClass: not-applicable) | consistent |
| docs/specs/active/biostack-pairwise-negative-relationship-lane-20260917.shaping-result.json | n/a (non-spec artifact) | no-axis-fields-present | n/a | n/a | flagged-for-human-review |
| docs/specs/active/protocol-upload-known-gaps-residual-a-b-c-2026-10-04.shaping-result.json | n/a (non-spec artifact) | no-axis-fields-present | n/a | n/a | flagged-for-human-review |
| docs/specs/done/BIO-ANALYZER-001-upload-table-label-leak.md | ticket-spec | needs-reconciliation | invalid | unknown-label (productGuidanceClass: not-applicable) | consistent |
| docs/specs/done/BIO-ANALYZER-002-xlsx-package-robustness.md | ticket-spec | needs-reconciliation | invalid | unknown-label (productGuidanceClass: not-applicable) | consistent |
| docs/specs/done/BIO-ANALYZER-003-single-word-frequency-prose-gate.md | ticket-spec | needs-reconciliation | invalid | unknown-label (productGuidanceClass: not-applicable) | consistent |
| docs/specs/done/BIO-ANALYZER-004-spreadsheet-row-reconstruction.md | ticket-spec | needs-reconciliation | invalid | unknown-label (productGuidanceClass: not-applicable) | consistent |
| docs/specs/done/BIO-FE-001-homepage-live-proof-panel.md | ticket-spec | conforms | invalid | missing-required-section (contracts) | flagged-for-human-review |
| docs/specs/done/BIO-LOCAL-001-local-dev-boot-proof.md | ticket-spec | needs-reconciliation | invalid | missing-required-frontmatter-key (delivery_classes) | consistent |
| docs/specs/done/BIO-LOCAL-002-knowledge-public-read-proof.md | ticket-spec | needs-reconciliation | invalid | missing-required-frontmatter-key (delivery_classes) | consistent |
| docs/specs/done/BIO-LOCAL-003-auth-tenancy-isolation-proof.md | ticket-spec | needs-reconciliation | invalid | missing-required-frontmatter-key (delivery_classes) | consistent |
| docs/specs/done/BIO-LOCAL-004-governance-spine-receipt-proof.md | ticket-spec | needs-reconciliation | invalid | missing-required-frontmatter-key (delivery_classes) | consistent |
| docs/specs/done/BIO-LOCAL-005-guidance-contract-enforcement-proof.md | ticket-spec | needs-reconciliation | invalid | missing-required-frontmatter-key (delivery_classes) | consistent |
| docs/specs/done/BIO-LOCAL-006-product-contract-mirror-proof.md | ticket-spec | needs-reconciliation | invalid | missing-required-frontmatter-key (delivery_classes) | consistent |
| docs/specs/done/BIO-LOCAL-007-seed-gap-inventory.md | ticket-spec | needs-reconciliation | invalid | missing-required-frontmatter-key (delivery_classes) | consistent |
| docs/specs/done/BIO-LOCAL-008-seed-expansion-batch-a.md | ticket-spec | needs-reconciliation | invalid | missing-required-frontmatter-key (delivery_classes) | consistent |
| docs/specs/done/BIO-LOCAL-009-seed-expansion-batch-b.md | ticket-spec | needs-reconciliation | invalid | missing-required-frontmatter-key (delivery_classes) | consistent |
| docs/specs/done/BIO-LOCAL-010-seed-expansion-batch-c.md | ticket-spec | needs-reconciliation | invalid | missing-required-frontmatter-key (delivery_classes) | consistent |
| docs/specs/done/BIO-LOCAL-011-seed-run-and-serving-proof.md | ticket-spec | needs-reconciliation | invalid | missing-required-frontmatter-key (delivery_classes) | consistent |
Reconciling any flagged spec's frontmatter or body is out of scope for P3-A and is left as an
explicit, visible finding for a future reconciliation parcel or P4's general linter to enforce.
