# P0-A Contradiction Inventory — Method

Schema: `biostack.p0a-inventory-method.v1`

This directory is the Contradiction Inventory deliverable (P0-A parcel spec, "Contradiction
inventory (deliverable 2)"). It is **analytical only**: it decides no product allowed-output, and
every quoted claim belongs to the document it quotes, never to P0-A in its own voice.

`BaseCommit` for every citation in this directory: `b78e7de8463a3db410c45cf223cd722713fec816`.

## Files in this directory

- `SOURCE-MANIFEST.md` — one row per required source (plus any additional source this parcel's
  read discovered), marked `consistent`, `contradictory`, or `unreviewable`, with the date/commit
  it was read at.
- `CONTRADICTION-INVENTORY.md` — every `CI-NNN` row, using the row schema below exactly.
- `CORPUS-COVERAGE-MATRIX.md` — one row per combinatorial pair of required sources, proving
  coverage is checkable, not merely asserted.
- `DATA-CLASSIFICATION.md` — this parcel's own privacy data-classification statement and the
  grep method used to prove it.

## Row schema

Every `CI-NNN` row in `CONTRADICTION-INVENTORY.md` carries exactly these eleven fields:

| Field | Content rule |
|---|---|
| `id` | `CI-NNN`, dense, zero gaps, assigned in discovery order |
| `topic` | one-line human-readable subject |
| `source_a` | file path + section heading or line range + exact quoted text |
| `source_b` | file path + section heading or line range + exact quoted text |
| `conflict` | one paragraph naming exactly what `source_a` permits/asserts that `source_b` forbids/denies, or vice versa — no paraphrase substitutes for the quotations |
| `delivery_class_tags` | zero or more labels from `classification-axes.schema.json`'s `deliveryClass.labels`, or `not-applicable` |
| `guidance_class_tags` | zero or more labels from `productGuidanceClass.labels`, or `not-applicable` |
| `substance_function_risk_tags` | zero or more labels from `substanceFunctionRisk.labels`, or `not-applicable` |
| `status` | exactly one of `contradictory`, `consistent`, `unreviewable` |
| `proposed_disposition` | exactly one of `fix`, `accept-as-documented`, `informational`, `resolved-by-owner-ruling` — required for every `contradictory` row; `not-applicable` for `consistent`/`unreviewable` rows |
| `handoff_target` | `P0-D1` (core doctrine/ADR), `P0-D2` (evidence methodology/guardrails), `P0-D3` (product contract/README/marketing/provider copy), `P0-D4` (enforcement/regression tests), or `not-applicable` |

`status: contradictory` always pairs with `proposed_disposition != not-applicable`.

## Disposition vocabulary — exact meaning

- **`fix`** — a later P0-D subparcel should change a canon document's text. P0-A names the
  conflict and the responsible subparcel only; it never pre-authors the fix text.
- **`accept-as-documented`** — the apparent tension is intentional or acceptable and should be
  cross-referenced rather than removed.
- **`informational`** — not a true conflict; recorded for completeness (for example, a
  `consistent` finding that is still worth a reviewer's attention, or a procedural observation).
- **`resolved-by-owner-ruling`** — an owner ruling already on record (a Coordinator Decisions
  ledger entry, or a ruled design-gate decision package) resolves the conflict in substance. The
  row cites the ruling and does not re-propose a disposition a later P0-D subparcel would decide.

P0-A **proposes** dispositions. It never executes one. A `fix`/`accept-as-documented`/
`informational`/`resolved-by-owner-ruling` label is a recommendation for a future P0-D
reconciliation subparcel, not a reconciliation performed by this parcel.

## Corpus-coverage method — how every required source was proven read, not sampled

1. Every file named in the P0-A parcel spec's "Required source list" (items 1-13) was opened and
   read in full by this parcel's builder, at `BaseCommit`.
2. `SOURCE-MANIFEST.md` records, for every required source, a `consistent`/`contradictory`/
   `unreviewable` status with a citation or the exact `unreviewable` reason (file path + line
   range + the evidence the "Missingness" rule requires).
3. `CORPUS-COVERAGE-MATRIX.md` additionally records, for **every combinatorial pair** of required
   sources (`n choose 2`, `n = 13`, `78` pairs; items 12-13 included even though excluded from the
   precedence registry), an explicit comparison outcome: `compared-contradictory` (cites the
   `CI-NNN` row the pair produced), `compared-consistent` (cites the `CI-NNN` row or
   `SOURCE-MANIFEST.md` entry the comparison was recorded under), or `scope-disjoint` (a
   one-sentence reason the two sources could not plausibly conflict). No pair is silently absent.
4. This two-layer method (`SOURCE-MANIFEST.md` for "was this source read," `CORPUS-COVERAGE-MATRIX.md`
   for "was this pair explicitly compared") makes *coverage* checkable by a script
   (`corpus-coverage-matrix-complete`, `source-manifest-completeness`), distinct from
   "exhaustiveness of findings," which remains a best-effort, reviewer-checked claim — no script
   can prove the negative "no contradiction was missed." The two independent reviewers are the
   actual backstop against silent narrowing of *findings*; the matrix is the backstop against
   silent narrowing of *coverage*.

## What this parcel does not do

- It never asserts a product rule in BioStack's own voice. Every claim in `CONTRADICTION-INVENTORY.md`
  is a quotation attributed to its source document.
- It never edits, "fixes," annotates inline, or reconciles any canon document it reads.
- It never decides, implies, or can be read as deciding a product allowed-output, an
  applicability criterion for any substance/function-risk label, or any
  allowed/degraded/refused/escalated product behavior. Those decisions belong to P0-B, under
  P0-B's own, separate, not-yet-granted owner gate.
- A proposed disposition is not an executed reconciliation. P0-D subparcels, under their own
  future gates, execute (or, for `resolved-by-owner-ruling` rows, encode) the dispositions this
  parcel proposes.
