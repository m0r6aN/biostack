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
