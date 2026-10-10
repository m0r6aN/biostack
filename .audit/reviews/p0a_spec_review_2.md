# p0a_spec_review_2 — P0-A spec review

**Verdict:** REJECT
**Spec file:** docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md
**Spec SHA-256:** 50c22f74569c2453952b56e372e40bfc159162b0de56c583c1fdc6ca93e0bc10
**Date:** 2026-10-09

## Ranked findings

### F1 (BLOCKER) — Cited ledger hash is wrong, and the live ledger has already materially
overtaken the spec's "load-bearing" authorization narrative, including the flagship seed
contradiction.

**Claim:** The spec quotes `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` D-G "verbatim
because it is this parcel's sole standing-authorization source" and pins it to SHA-256
`EE506C3B54FB57ABD09BBB5180CD704AABCA3EB73875B7E760EF84B538863D47` ("Lineage and dependencies").
That hash does not match the file on disk now
(`908faa5999526de76cc7e21b6753d41bc8bdb9f29c5f1717a504d9103e4ca7ae`, verified by direct
`sha256sum`). The file has grown since this spec's shaping anchor
(`main@32280aa2219100e20520a275d19af2fbdd1f32be`, confirmed by `git merge-base --is-ancestor` to be
an ancestor of current `HEAD` `ca7b11b`) by two further entries: **D-H** ("Gate 3 posture + P0-B
design gate opened") and **D-I** ("P0-B design gate RULED... D-B1: c staged split; D-B2–D-B6 as
recommended — matrix frozen"). D-I explicitly states: *"the §2 canon conflict is resolved in
substance by D-B1(c)"* — and that §2 conflict, quoted in full in the now-committed
`docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` ("2. The central conflict this
gate must resolve... may the product originate personalized numerical guidance?"), is word-for-word
the same conflict P0-A's own mandatory seed finding **CI-001** names (CHARTER.md `may`/D13 versus
guidance-content-contract Class D "Prohibited") and assigns `proposed_disposition: fix,
handoff_target: P0-D1` as if still fully open and unaddressed outside this parcel's process.

This is not a hypothetical drift risk; it is the actual current state of the repository the spec
claims to review. A builder dispatched against this spec text would (a) treat the quoted D-G block
as the full and current standing-authorization picture when it is stale, (b) build
`canon-precedence.md`/`CONTRADICTION-INVENTORY.md` without any required obligation to consult
`P0-B-DESIGN-GATE.md` or D-H/D-I (neither is in the "Required source list," nor in "Frozen
surfaces," nor anywhere named in this spec), and (c) ship a seed-regression-protected CI-001 row
that still reads as an open conflict awaiting P0-D1 reconciliation, duplicating or silently
conflicting with an owner ruling that already exists on record. This is the exact harm the
spec's own Objective section says the precedence manifest exists to prevent ("without
re-litigating authority order each time").

**Evidence:** P0-A.md "Lineage and dependencies" (ledger hash + D-G quote block);
`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` lines 114-169 (D-G, D-H, D-I);
`docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` lines 1-16 and "2. The central
conflict..."; P0-A.md CI-001 row; `git merge-base --is-ancestor
32280aa2219100e20520a275d19af2fbdd1f32be HEAD` → ancestor confirmed.

**Smallest amendment:** Before this spec can be reviewed as review-candidate again: (a) re-verify
and re-cite the ledger's current SHA-256; (b) add `docs/INITIATIVES/biostack-governed-delivery/
P0-B-DESIGN-GATE.md` and the D-H/D-I entries of `COORDINATOR-DECISIONS-2026-10-07.md` to the
Required source list; (c) re-evaluate CI-001 (and CI-002/CI-003/CI-004/CI-005, all downstream of
the same conflict) against D-I's "resolved in substance" ruling — either reclassify them
`accept-as-documented` with a citation to D-I, split them into "resolved by D-I at the design-gate
level, pending v2.0.0 re-ratification for public enablement" rows, or explain in the row why P0-D1
must still act despite D-I. Shipping the current seed rows unchanged, with `seed-regression`
protecting them byte-for-byte, would freeze a now-contradicted framing.

**Does it change a locked decision?** No — it does not ask to change D1-D18 or the ratified
charter doctrine. It asks the spec to accurately reflect an owner ruling (D-I) that already
exists and that the spec's own precedence-rule-3 ("recency of ratification... outranks") would,
if applied honestly to the full current corpus, almost certainly rank above the stale framing the
spec ships today.

### F2 (MAJOR) — "Unreviewable" Source Manifest status has no deterministic
anti-bypass check; a narrowing vector exists.

**Claim:** The task explicitly asks whether Source Manifest handling of deleted/unreadable files
creates a bypass. It does. "Missingness" (health-boundary section) and the stop-condition list
require a builder to record `unreviewable` with "the exact reason" when a required source is
"deleted, unreadable, or outside this worktree's checkout" — but the deterministic verifier
(`verify-p0a.ps1`, per the "Deterministic verification" table and the named checks list) has no
check that cross-validates an `unreviewable` claim against the actual filesystem/`BaseCommit`
state. `source-manifest-completeness` only checks that every required source has *a* valid status
and *a* citation or reason string — it does not check that a claimed "file unreadable" reason is
true. All eleven required sources are, in fact, present and readable on disk today (verified
directly). A builder under schedule or scope pressure could mark an inconvenient, perfectly
readable source `unreviewable` with a plausible-sounding but false reason, and no automated check
would catch it; only a human reviewer's independent re-check would (and the two-reviewer backstop
is not named anywhere as specifically re-verifying `unreviewable` claims against the filesystem —
"Disagreement handling" only covers disputed findings the reviewers both saw, not an un-asserted
omission one reviewer might miss).

**Evidence:** P0-A.md "Mandatory class sections... Health-boundary... Missingness"; "Deterministic
verification" table row for `source-manifest-completeness`; direct filesystem check (all 11
required-source files present and readable at review time).

**Smallest amendment:** Add a named check (e.g. `unreviewable-claim-verified`) requiring the
verifier to independently confirm, for every `unreviewable` row, that the named file truly does
not exist (or truly is unreadable/outside the worktree) at `BaseCommit` — failing closed if the
file is in fact present and readable.

### F3 (MAJOR) — `BaseCommit` is used as a load-bearing defined term (acceptance criteria,
seed-regression, citation-resolution checks) but is never defined in this document.

**Claim:** "Real-corpus grounding" and three deterministic checks
(`canonical-write-fencing-violation`/`unresolvable-citation`, the "Missingness" stop condition, and
the hard-constraint "pinned `BaseCommit`") all depend on a term `BaseCommit` that this spec never
defines, names, or binds to a specific commit SHA inline. The "Lineage and dependencies" section
names a "Reconciled shaping base anchor: `main@32280aa2219100e20520a275d19af2fbdd1f32be`" but never
states that this anchor *is* `BaseCommit`, nor does the spec include a "Gate 2 builder handoff
requirements" section (present in sibling parcels P2 and P3-A) that would bind `BaseCommit` and an
evidence-destination directory convention explicitly at dispatch time. Two honest builders could
reasonably differ on what commit "BaseCommit" means at dispatch (the shaping anchor vs. the actual
Gate 2 dispatch commit, which per this spec's own dispatch precondition must postdate P2/P3-A
closing — i.e., a different, later commit than the shaping anchor).

**Evidence:** P0-A.md, "Real-corpus grounding," `canonical-write-fencing-violation`/
`unresolvable-citation` row, "Missingness" bullet, all use `BaseCommit` without definition;
contrast with P3-A.md "Gate 2 builder handoff requirements" (explicit "single `BaseCommit` dispatch
anchor SHA").

**Smallest amendment:** Add a one-line definition binding `BaseCommit` to the commit the Gate 2
dispatch record names (not the shaping anchor), and add a "Gate 2 builder handoff requirements"
section consistent with P2/P3-A precedent, naming the evidence-destination artifact directory.

### F4 (MINOR) — Exhaustiveness of the contradiction inventory is asserted in prose but not
independently machine-verifiable; the only enforced floor is the ten named seed rows.

**Claim:** The Objective and deliverable 2 both describe the inventory as "exhaustive." The only
deterministic checks that bear on completeness are `source-manifest-completeness` (every required
source has *a* status) and `seed-regression` (the ten named seeds are not weakened). Nothing
prevents a builder from reading a source, marking it `consistent`, and missing a real,
un-seeded contradiction within it — no script can prove a negative like "no contradiction was
missed." This is a known, inherent limit of any analytical-catalogue task and is partially
mitigated by the two-reviewer requirement, but the spec should say so explicitly rather than
implying a verifier-enforced "exhaustive" claim it cannot deliver.

**Evidence:** P0-A.md Objective ("exhaustive, citation-backed catalogue of every inconsistency"),
Deterministic verification table (no exhaustiveness-proving check exists), Acceptance criteria #2
("covers one hundred percent of the required source list with a valid status" — status coverage,
not exhaustive-pairwise-comparison coverage).

**Smallest amendment:** Add a sentence acknowledging that "exhaustive" is a best-effort,
reviewer-checked claim rather than a verifier-provable one, and that the two independent reviewers
are the actual backstop against silent narrowing (not the verifier script alone).

### F5 (MINOR) — Precedence-manifest property classification itself is judgment-dependent,
even though the final `compare()` output is formally total via the `open-tie` fallback.

**Claim:** The three-property ranking rule (ratification formality, recency, scope breadth)
requires subjective classification before the deterministic lookup applies — e.g., whether a
document counts as having "an explicit, dated, named-owner ratification event," or whether its
"stated scope is product-wide" vs. "a single feature." Two honest builders could classify an
ambiguous document (for example, is a Coordinator Decisions ledger entry a "ratification event" at
all, as opposed to a procedural authorization record?) differently, producing different registries
even though each individual registry, once built, would be internally total. The `open-tie`
fallback resolves ties on identical classifications, not disagreements about classification
itself.

**Evidence:** P0-A.md "Precedence manifest... Design," properties 1-3; no worked example or
decision procedure is given for classifying a borderline document (e.g., the Coordinator Decisions
ledger, which this review's F1 shows is directly relevant and currently omitted from the required
corpus).

**Smallest amendment:** Add one or two worked classification examples for borderline document
types (decision ledgers, design-gate decision packages) so two builders converge on the same
registry inputs, not just the same comparison function.

## Missing pieces / collisions / unknowns

- The Required source list (11 items) omits `docs/INITIATIVES/biostack-governed-delivery/
  P0-B-DESIGN-GATE.md` and the Coordinator Decisions ledger's D-H/D-I content, both of which are
  already-committed, already materially relevant to CI-001 through CI-005, and would plausibly
  outrank CHARTER.md's bare D13 text under the spec's own recency rule if included. See F1.
- No "Gate 2 builder handoff requirements" section exists (present in P2.md and P3-A.md
  precedent), leaving `BaseCommit`, the evidence-destination directory, and the exact dispatch
  commit undefined in this document. See F3.
- The spec's "Lineage and dependencies" section self-describes P2 and P3-A as both still
  "review-candidate" at the shaping anchor. At review time, P2 is actually `done` (per
  `docs/specs/INDEX.md`) and P3-A is not registered in `docs/specs/INDEX.md` at all (absent, not
  merely `review-candidate`). This is consistent with the spec's own dispatch-precondition design
  (re-check at Gate 2, don't assume), so it is not itself a defect, but it underscores that this
  spec was shaped against a now-superseded snapshot in more than one respect (see F1 for the
  material instance).
- No check verifies `unreviewable` Source Manifest claims against actual file state. See F2.

## Verification notes

- Computed this spec's own SHA-256 directly; recorded above.
- Verified CHARTER.md SHA-256 (`4cd390d6...`) and PLAN-REVIEW.md SHA-256 (`fbe40053...`) both
  match the spec's citations exactly — clean.
- Verified the Coordinator Decisions ledger SHA-256 citation does **not** match the current file —
  see F1 (the central finding of this review).
- Verified `docs/specs/schemas/delivery-class-controls.json` fold values (reviewer count 2 per
  class, `additionalMergeGate` = `green-dual-review-required` for health-boundary and
  `recorded-human-approval` for legal-policy, none for privacy/knowledge-promotion;
  `requiredSpecAdditions` lists) against the spec's "Mandatory class sections" and "Review/gates" —
  these match correctly; the D14 fieldwise-union math the spec claims is accurate.
- Confirmed all 11 required-source files exist and are readable on disk today; also confirmed
  `docs/specs/schemas/parcel-spec.schema.json` (referenced for a future `no-placeholder` check
  once P3-A merges) does not yet exist, consistent with the spec's own stated dispatch
  precondition — not a defect.
- Confirmed via `git merge-base --is-ancestor` that the spec's cited shaping anchor
  (`32280aa2...`) is an ancestor of current `HEAD` (`ca7b11b`), i.e., genuinely stale relative to
  now-committed D-H/D-I and `P0-B-DESIGN-GATE.md`.
- Did not find any language in the spec that could be read as authorizing P0-B, P0-C, or P0-D
  dispatch or substantive work; the "Authorization boundary," "Hard constraints," and "Stop
  conditions" sections are explicit and repeated on this point. This part of the spec is sound on
  its own terms — the problem (F1) is that its own account of what is/ is not yet decided is
  stale, not that it tries to claim authorization it lacks.
- Confirmed the row schema, allowed-surfaces list, and frozen-surfaces list are internally
  consistent and do not touch any product-facing file, runtime code, or existing canon document —
  no scope-creep into product behavior found.
- Did not independently re-derive every CI-NNN quotation against its cited source span in full
  (time-bounded); spot-checked CI-001's `biostack-protocol-intelligence-canon.md` quote and
  README.md "Safety and compliance boundary" quote structure by cross-reference against
  `P0-B-DESIGN-GATE.md`'s restatement of the same conflict — consistent, no fabrication found.
