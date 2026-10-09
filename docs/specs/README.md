# Governed Spec Lifecycle

This directory is BioStack's repository-native registry and lifecycle surface for governed delivery. The [authoritative index](INDEX.md) records every active or completed parcel, while [active](active/README.md) and [done](done/README.md) define placement and transition rules.

## Goal Charter lifecycle

1. A Goal Charter records the objective, ratified product doctrine, locked decisions, delivery controls, authorization boundaries, stop conditions, dependency order, and measurable exit criteria.
2. Charter ratification and plan-review closure precede parcel shaping.
3. A shaped parcel spec is a review candidate until all required fresh, independent reviews pass and coordinator triage closes every finding.
4. Gate 2 records the approved spec hash, builder, branch, worktree, comparison base, permissions, deterministic checks, evidence destination, and required reviewers.
5. Only an approved active spec may be dispatched, and only in the branch and worktree named by the coordinator.
6. Failed, incomplete, disputed, stale, or wrong-environment verification voids standing authorization for the affected parcel.
7. A parcel becomes `done` only after merge and complete closure evidence. Its closure record and index transition are made in the same governed change.

## Status and ownership

- `review-candidate`: the coordinator owns shaping and review triage; implementation is blocked.
- `active`: Gate 2 is complete and the named builder may implement only the approved surfaces.
- `done`: the implementation is merged and the coordinator has linked complete closure evidence.

Builders never approve or close their own parcels. Reviewers are independent and read-only; they report findings and do not modify implementation. The coordinator owns dispatch, finding reproduction, triage, merge sequencing, registry state, and closure.

Active specs must contain no unresolved placeholders, ambient branch or worktree, or unresolved decision. Required product or contract decisions stop the parcel and return to the coordinator.

## P1 bootstrap exception

P1 predates this registry. Its canonical approved spec remains at [the governed-delivery P1 parcel](../INITIATIVES/biostack-governed-delivery/parcels/P1.md) and is not copied or moved into `active/`. Its one-time approved active state requires the recorded spec hash, two independent PASS reviews of that hash, closed coordinator triage, and the [P1 Gate 2 record](../INITIATIVES/biostack-governed-delivery/dispatch/P1-GATE2.md). The active/done placement rule begins with P2.

The initial P1 registry row remains `active` and `not-yet-closed` through merge. After merge, the coordinator alone creates the P1 closure record and changes only that row to `done` with its closure link.

## Classification-axis schema and routing (P2)

P2 defines the deterministic, machine-checkable risk-taxonomy and routing substrate for the three D14 classification axes: [`classification-axes.schema.json`](schemas/classification-axes.schema.json) (the closed label vocabularies, multi-label permission, and applicability metadata), [`delivery-class-controls.json`](schemas/delivery-class-controls.json) (the charter's eight-row governance overlay table as computable data), [`fold-engine.md`](schemas/fold-engine.md) (the D14 fieldwise-fold algorithm), and [`routing-output.schema.json`](schemas/routing-output.schema.json) (the fold's output contract). P2 defines composition mechanics for all three D14 axes without binding substance/function labels to product allowed outputs.

## Parcel-spec schema, templates, and extension points (P3-A)

P3-A defines the generic, extensible, machine-checkable parcel-spec contract every future parcel spec and ticket-level spec is defined to validate against: [`parcel-spec.schema.json`](schemas/parcel-spec.schema.json) (the recognized spec shapes, required frontmatter keys, closed `status` vocabulary, live fold-based required-section derivation, and the no-placeholder rule), [`SECTION-HEADING-MAP.md`](schemas/SECTION-HEADING-MAP.md) (the deterministic term-to-heading bridge, derived live from `delivery-class-controls.json`), [`EXTENSION-POINTS.md`](schemas/EXTENSION-POINTS.md) (the three named, closed-vocabulary extension points), and [`templates/README.md`](templates/README.md) (the eight per-delivery-class starting templates). P3-A composes P2's axis/fold substrate into a generic spec contract and invents no product capability semantics.
