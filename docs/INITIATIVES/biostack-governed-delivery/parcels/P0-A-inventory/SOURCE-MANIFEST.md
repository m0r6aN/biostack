# P0-A Source Manifest

Schema: `biostack.p0a-source-manifest.v1`

`BaseCommit` for every row below: `b78e7de8463a3db410c45cf223cd722713fec816`. Every source was
read in full against that pinned commit (verified by `git cat-file -p
b78e7de8463a3db410c45cf223cd722713fec816:<path>` resolving and matching the worktree read).

## Status interpretation (stated explicitly so a reviewer does not have to infer it)

- **`consistent`** — fully read; either cited by zero `CI-NNN` rows, or cited only by `CI-NNN`
  rows whose own `status` field is `consistent` (never `contradictory`); no contradiction against
  any other catalogued source was found.
- **`contradictory`** — fully read; cited by at least one `CI-NNN` row whose own `status` field is
  `contradictory`.
- **`unreviewable`** — reserved strictly for the three deterministic, independently verifiable
  Missingness conditions (deleted at `BaseCommit`, unreadable at dispatch time, or tree-present-
  but-outside-this-checkout). No required source in this corpus meets any of those three
  conditions; every required source was readable and was read. `BIOSTACK_FRONTEND_READINESS_AUDIT.md`
  is cited by `CI-008`, whose own row `status` is `unreviewable` — that is a distinct,
  content-level finding (insufficient information inside the audit to determine consistency or
  contradiction), not a file-read failure, so this manifest marks that source `consistent` per the
  rule above (fully read, zero `contradictory`-status rows cite it) rather than `unreviewable`,
  which would misrepresent a successful, complete read as a Missingness condition it does not meet.

## Required sources (per P0-A parcel spec, "Required source list")

| # | Source | Status | Citation / reason | Read at |
|---|---|---|---|---|
| 1 | `docs/INITIATIVES/biostack-governed-delivery/CHARTER.md` | `contradictory` | Cited by `CI-001`, `CI-002`, `CI-003`, `CI-004`, `CI-005` (all `status: contradictory`); also compared-consistent in `CORPUS-COVERAGE-MATRIX.md` pairs 1-5, 1-7, 1-10, 1-11, 1-12, 1-13 | `b78e7de8463a3db410c45cf223cd722713fec816` |
| 2a | `docs/specs/INDEX.md` | `contradictory` | Cited by `CI-011` (`status: contradictory`) | `b78e7de8463a3db410c45cf223cd722713fec816` |
| 2b | `docs/specs/README.md` | `contradictory` | Cited by `CI-011` (`status: contradictory`) | `b78e7de8463a3db410c45cf223cd722713fec816` |
| 3 | `README.md` (repository root) | `contradictory` | Cited by `CI-001` (`status: contradictory`, `source_b` third citation); also compared-consistent in pairs 3-4, 3-7, 3-8, 3-9, 3-11 | `b78e7de8463a3db410c45cf223cd722713fec816` |
| 4 | `docs/product/knowledge-engine-capability-map.md` | `contradictory` | Cited by `CI-005` (`status: contradictory`) | `b78e7de8463a3db410c45cf223cd722713fec816` |
| 5 | `docs/product/knowledge-engine-model-data-roadmap.md` | `consistent` | Fully read; cited by zero `CI-NNN` rows; compared-consistent/scope-disjoint against all other sources (`CORPUS-COVERAGE-MATRIX.md` pairs 1-5, 2-5, 3-5, 4-5, 5-6 … 5-13); no claim in this document was found to conflict with any other catalogued source's claim | `b78e7de8463a3db410c45cf223cd722713fec816` |
| 6 | `docs/product/product-ids.md` | `consistent` | Fully read; the file is effectively empty (a single blank line, 2 bytes) and contains no claim to conflict with any other source | `b78e7de8463a3db410c45cf223cd722713fec816` |
| 7 | `BIOSTACK_FRONTEND_READINESS_AUDIT.md` | `consistent` | Fully read; cited by `CI-008` (`status: unreviewable` — a content-level finding, not a Missingness condition; see "Status interpretation," above); no `contradictory`-status `CI-NNN` row cites this source | `b78e7de8463a3db410c45cf223cd722713fec816` |
| 8 | `docs/canon/biostack-protocol-intelligence-canon.md` | `contradictory` | Cited by `CI-001`, `CI-002`, `CI-003` (all `status: contradictory`) | `b78e7de8463a3db410c45cf223cd722713fec816` |
| 9a | `docs/guidance/biostack-guidance-content-contract.v1.md` | `contradictory` | Cited by `CI-001`, `CI-004` (both `status: contradictory`) | `b78e7de8463a3db410c45cf223cd722713fec816` |
| 9b | `docs/guidance/RATIFICATION.md` | `consistent` | Fully read; cited by zero `CI-NNN` rows as a formal `source_a`/`source_b`; its "Pairwise relationship publication contract v1 ratified" entry is compared-consistent against the pairwise lane (`CORPUS-COVERAGE-MATRIX.md` pair 9-10) | `b78e7de8463a3db410c45cf223cd722713fec816` |
| 10 | `docs/specs/active/BIO-PAIRWISE-001-relationship-substrate-and-label-census.md` through `BIO-PAIRWISE-006-studied-combinations-surface.md` (six files) | `consistent` | Cited by `CI-009` (`status: consistent`); fully read, all six files | `b78e7de8463a3db410c45cf223cd722713fec816` |
| 11 | `docs/INITIATIVES/biostack-local-readiness/FINAL-HANDOFF.md` | `consistent` | Fully read; cited by zero `CI-NNN` rows as a formal `source_a`/`source_b`; compared-consistent against `CHARTER.md`, `README.md`, and `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` (`CORPUS-COVERAGE-MATRIX.md` pairs 1-11, 3-11, 11-12) | `b78e7de8463a3db410c45cf223cd722713fec816` |
| 12 | `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` | `consistent` | Fully read; excluded from the precedence registry per `canon-precedence.md`'s Classification guidance; cross-referenced in prose by `CI-001`'s disposition paragraph (`resolved-by-owner-ruling`, citing D-I) but not a formal `source_a`/`source_b` of any row; no contradiction found against any catalogued source's own text | `b78e7de8463a3db410c45cf223cd722713fec816` |
| 13 | `docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` | `consistent` | Fully read; excluded from the precedence registry per `canon-precedence.md`'s Classification guidance; cross-referenced in prose by `CI-001`'s disposition paragraph but not a formal `source_a`/`source_b` of any row; no contradiction found against any catalogued source's own text | `b78e7de8463a3db410c45cf223cd722713fec816` |

## Additional source discovered during corpus read (not in the Required source list)

| Source | Status | Citation / reason | Read at |
|---|---|---|---|
| `docs/guidance/GOVERNANCE-ENFORCEMENT-FINDINGS.md` | `consistent` | Fully read (named as an example of a document contract 2 should cover, "etc."); cited by zero `CI-NNN` rows; its enforcement-gap findings (F1-F2, et al.) concern runtime code behavior, which this parcel does not review (Hard constraints: "no runtime code" is explicitly out of scope), and its own text does not contradict any catalogued canon document's claims — see `canon-precedence.md` rank 11 for its registry placement and precedence note | `b78e7de8463a3db410c45cf223cd722713fec816` |

Three further discovered documents (`docs/guidance/BUILDER-HANDOFF.md`,
`docs/guidance/OPS-SPINE-AND-SIDECAR.md`, `docs/guidance/pairwise-relationship-publication-contract.v1.md`)
and `docs/legal/*` (two `.docx` drafts plus a redline changelog) were read for context but are
**not** required sources and are not entered as additional manifest rows; `canon-precedence.md`'s
registry section records the explicit rationale for treating them as out of this parcel's
registry/manifest scope (binary, pre-approval-draft, or narrow-scope-already-covered-by-a-
registered-parent-document, respectively).

## Coverage statement

All 13 Required source list items (18 individual files, counting item 2's two files, item 9's two
files, and item 10's six files) are recorded above with a valid status. Zero `unreviewable` rows
exist in this manifest, because every required source was present at `BaseCommit`, present in this
worktree's checkout, and readable — none of the three Missingness conditions
(`docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md`, Health-boundary "Missingness")
applied to any required source.
