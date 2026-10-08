# AXIS-REGRESSION-MAP — P2 classification-axis regression census

Read-only, one-time census of every file under `docs/specs/active/` and `docs/specs/done/`
(excluding `active/README.md` and `done/README.md`) at `BaseCommit`
(`f812dc3bd51c9e94fdaac5f22d92a73f9f41d982`), mapping each file's existing ad hoc classification
fields onto the P2 closed-vocabulary axis schema (`classification-axes.schema.json`). This file
does not edit any inspected spec; it is a read-only finding surface for a future reconciliation
parcel. `not-applicable` is not a closed-vocabulary label on any axis — every spec that currently
uses the literal string `"not-applicable"` inside `guidance_classes` or `substance_function_risk`
is recorded as `needs-reconciliation` (the correct closed-vocabulary representation is the empty
array `[]`), not as a pre-existing valid label.

| Spec file | Existing ad hoc fields found | Mapped delivery class(es) | Mapped guidance class(es) | Mapped substance/function risk | Disposition |
|---|---|---|---|---|---|
| docs/specs/active/BIO-KEON-001-keon-ownership-docs-alignment.md | `risk: standard`; `delivery_classes: [standard]`; `guidance_classes: [not-applicable]`; `substance_function_risk: [not-applicable]` | standard | [] | [] | needs-reconciliation |
| docs/specs/active/BIO-KEON-002-keon-compatible-language-replacement.md | `risk: standard`; `delivery_classes: [standard]`; `guidance_classes: [not-applicable]`; `substance_function_risk: [not-applicable]` | standard | [] | [] | needs-reconciliation |
| docs/specs/active/BIO-PAIRWISE-001-relationship-substrate-and-label-census.md | `risk: low`; `delivery_classes: [standard]`; `guidance_classes: [not-applicable]`; `substance_function_risk: [not-applicable]` | standard | [] | [] | needs-reconciliation |
| docs/specs/active/BIO-PAIRWISE-002-publishable-relationship-ratification.md | `risk: critical`; `delivery_classes: [standard]`; `guidance_classes: [not-applicable]`; `substance_function_risk: [not-applicable]`; `guidance_classes_rationale` (free text) | standard | [] | [] | needs-reconciliation |
| docs/specs/active/BIO-PAIRWISE-003-relationship-projection-wiring.md | `risk: standard`; `delivery_classes: [standard]`; `guidance_classes: [not-applicable]`; `substance_function_risk: [not-applicable]` | standard | [] | [] | needs-reconciliation |
| docs/specs/active/BIO-PAIRWISE-004-interaction-hint-provenance.md | `risk: elevated`; `delivery_classes: [standard]`; `guidance_classes: [not-applicable]`; `substance_function_risk: [not-applicable]` | standard | [] | [] | needs-reconciliation |
| docs/specs/active/BIO-PAIRWISE-005-first-sourced-negative-pair.md | `risk: standard`; `delivery_classes: [standard]`; `guidance_classes: [not-applicable]`; `substance_function_risk: [not-applicable]` | standard | [] | [] | needs-reconciliation |
| docs/specs/active/BIO-PAIRWISE-006-studied-combinations-surface.md | `risk: standard`; `delivery_classes: [standard]`; `guidance_classes: [not-applicable]`; `substance_function_risk: [not-applicable]` | standard | [] | [] | needs-reconciliation |
| docs/specs/active/biostack-pairwise-negative-relationship-lane-20260917.shaping-result.json | n/a (non-spec artifact, no frontmatter) | n/a | n/a | n/a | no-axis-fields-present |
| docs/specs/active/protocol-upload-known-gaps-residual-a-b-c-2026-10-04.shaping-result.json | n/a (non-spec artifact, no frontmatter) | n/a | n/a | n/a | no-axis-fields-present |
| docs/specs/done/BIO-ANALYZER-001-upload-table-label-leak.md | `risk: standard`; `delivery_classes: [standard]`; `guidance_classes: [not-applicable]`; `substance_function_risk: [not-applicable]` | standard | [] | [] | needs-reconciliation |
| docs/specs/done/BIO-ANALYZER-002-xlsx-package-robustness.md | `risk: low`; `delivery_classes: [standard]`; `guidance_classes: [not-applicable]`; `substance_function_risk: [not-applicable]` | standard | [] | [] | needs-reconciliation |
| docs/specs/done/BIO-ANALYZER-003-single-word-frequency-prose-gate.md | `risk: standard`; `delivery_classes: [standard]`; `guidance_classes: [not-applicable]`; `substance_function_risk: [not-applicable]` | standard | [] | [] | needs-reconciliation |
| docs/specs/done/BIO-ANALYZER-004-spreadsheet-row-reconstruction.md | `risk: elevated`; `delivery_classes: [standard]`; `guidance_classes: [not-applicable]`; `substance_function_risk: [not-applicable]` | standard | [] | [] | needs-reconciliation |
| docs/specs/done/BIO-FE-001-homepage-live-proof-panel.md | `risk: standard`; `delivery_classes: [standard]`; `guidance_classes: [curated-evidence-guidance]`; `substance_function_risk: [investigational-or-unapproved]` | standard | curated-evidence-guidance | investigational-or-unapproved | conforms |
| docs/specs/done/BIO-LOCAL-001-local-dev-boot-proof.md | `risk: standard`; `routing_class: standard-feature` (no `delivery_classes`/`guidance_classes`/`substance_function_risk` fields declared) | standard (recommended) | n/a | n/a | needs-reconciliation |
| docs/specs/done/BIO-LOCAL-002-knowledge-public-read-proof.md | `risk: standard`; `routing_class: standard-feature` (no `delivery_classes`/`guidance_classes`/`substance_function_risk` fields declared) | standard (recommended) | n/a | n/a | needs-reconciliation |
| docs/specs/done/BIO-LOCAL-003-auth-tenancy-isolation-proof.md | `risk: elevated`; `routing_class: architecture/risk` (no `delivery_classes`/`guidance_classes`/`substance_function_risk` fields declared; `elevated` is not a closed-vocabulary delivery-class label) | privacy (recommended) | n/a | n/a | needs-reconciliation |
| docs/specs/done/BIO-LOCAL-004-governance-spine-receipt-proof.md | `risk: elevated`; `routing_class: architecture/risk` (no `delivery_classes`/`guidance_classes`/`substance_function_risk` fields declared) | trust-path (recommended) | n/a | n/a | needs-reconciliation |
| docs/specs/done/BIO-LOCAL-005-guidance-contract-enforcement-proof.md | `risk: elevated`; `routing_class: architecture/risk` (no `delivery_classes`/`guidance_classes`/`substance_function_risk` fields declared; `elevated` is not a closed-vocabulary delivery-class label) | health-boundary (recommended) | n/a | n/a | needs-reconciliation |
| docs/specs/done/BIO-LOCAL-006-product-contract-mirror-proof.md | `risk: standard`; `routing_class: standard-feature` (no `delivery_classes`/`guidance_classes`/`substance_function_risk` fields declared) | standard (recommended) | n/a | n/a | needs-reconciliation |
| docs/specs/done/BIO-LOCAL-007-seed-gap-inventory.md | `risk: standard`; `routing_class: standard-feature` (no `delivery_classes`/`guidance_classes`/`substance_function_risk` fields declared) | knowledge-promotion (recommended) | n/a | n/a | needs-reconciliation |
| docs/specs/done/BIO-LOCAL-008-seed-expansion-batch-a.md | `risk: elevated`; `routing_class: architecture/risk` (no `delivery_classes`/`guidance_classes`/`substance_function_risk` fields declared) | knowledge-promotion (recommended) | n/a | n/a | needs-reconciliation |
| docs/specs/done/BIO-LOCAL-009-seed-expansion-batch-b.md | `risk: elevated`; `routing_class: architecture/risk` (no `delivery_classes`/`guidance_classes`/`substance_function_risk` fields declared) | knowledge-promotion (recommended) | n/a | n/a | needs-reconciliation |
| docs/specs/done/BIO-LOCAL-010-seed-expansion-batch-c.md | `risk: elevated`; `routing_class: architecture/risk` (no `delivery_classes`/`guidance_classes`/`substance_function_risk` fields declared) | knowledge-promotion (recommended) | n/a | n/a | needs-reconciliation |
| docs/specs/done/BIO-LOCAL-011-seed-run-and-serving-proof.md | `risk: elevated`; `routing_class: architecture/risk` (no `delivery_classes`/`guidance_classes`/`substance_function_risk` fields declared) | knowledge-promotion (recommended) | n/a | n/a | needs-reconciliation |

Reconciling the flagged specs' frontmatter is out of scope for P2 and is left as an explicit,
visible finding for a future reconciliation parcel or P4's general linter to enforce.
