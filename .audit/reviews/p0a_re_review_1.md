# p0a_re_review_1 — P0-A spec review (targeted re-review)

**Verdict:** APPROVE
**Spec file:** docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md (worktree
`/home/cmorgan76/Repos/biostack-wt/p0a-rework-1`, branch `docs/p0a-rework-1`, PR #522)
**Spec SHA-256:** `2ffa54311c93876eff52578b4f0afbe2e41f76e261221276db4525fc0922e6e9` (computed directly
via `sha256sum` against the file on disk)
**Date:** 2026-10-09

## Ranked findings

No BLOCKER or MAJOR findings. Two MINOR observational notes only (neither blocks dispatch
readiness of this spec-review step; neither changes a locked decision).

### N1 — MINOR — `seed-regression`'s CI-001 byte-floor update is self-declared, not independently re-verifiable by a future verifier run alone
**Claim:** The `seed-regression` check definition states CI-001's `source_a` quotation "was
extended, with a named reason, during this spec's dual-review rework... that extension is the new
byte-for-byte floor, not the prior spec hash's shorter quotation." This correctly resolves R1-F5
(restore elided qualifiers) without conflicting with the seed-protection discipline, but the
*mechanism* by which `verify-p0a.ps1` knows "this is the new floor, not drift" is prose inside the
spec, not a hash or pinned artifact the script reads. A future script author re-implementing
`verify-p0a.ps1` from this spec text alone would need to hardcode the full extended quotation
directly (which the spec's own CI-001 row gives them verbatim) — functionally fine, but worth
flagging as the kind of "spec text is the only source of truth for a floor update" pattern that
should not recur casually across further reworks without an explicit changelog convention.
**Evidence:** P0-A.md, "Deterministic verification," `seed-regression` bullet.
**Smallest amendment:** None required for this review; optionally, future spec reworks that alter
a seed-regression floor should add a one-line "seed floor changelog" entry (version, reason, who)
rather than relying on inline prose explanation each time.
**Changes a locked decision?** No.

### N2 — MINOR — `resolved-by-owner-ruling` is a new disposition literal not present in any external closed vocabulary
**Claim:** The row schema's `proposed_disposition` field gains a fourth value,
`resolved-by-owner-ruling`, beyond the original three (`fix`, `accept-as-documented`,
`informational`). This is a self-contained addition to a vocabulary P0-A itself owns (it is not
drawn from `classification-axes.schema.json`'s closed vocabularies, which only govern the three
axis-tag fields), so it does not violate any class-control closed-vocabulary contract. It is
flagged only because it is a genuinely new concept introduced in the rework and worth a reviewer's
explicit notice: it is well-defined (document contract 1's disposition-vocabulary paragraph,
quoted in full) and consistently applied (CI-001 uses it; CI-002/CI-004/CI-005 deliberately do
not, with a stated reason each time).
**Evidence:** P0-A.md, row schema table (`proposed_disposition` row); "Required document
contracts," item 1 (disposition vocabulary paragraph); CI-001 row; paragraph following the seed
table (CI-002/CI-004/CI-005 reasoning).
**Smallest amendment:** None required; this is a clean, intentional, well-scoped addition.
**Changes a locked decision?** No.

## Triage fix-row verification checklist (R1-F1..R2-F5)

All items below were independently re-derived against the repository (hashes computed directly,
line ranges opened and read, cross-file claims checked), not taken on the spec's word.

| Finding | Disposition required | Verdict | Evidence |
|---|---|---|---|
| R1-F1 / R2-F1 — ledger hash fragile/stale | `fix`: pin D-G entry-text span hash + file-hash-at-anchor; record D-H/D-I in lineage | **CLOSED — verified correct.** Span hash of `COORDINATOR-DECISIONS-2026-10-07.md` lines 114-123 (`sed -n '114,123p' ... \| sha256sum`) is `3d46ac19aef9a0f553239b6ec984e90216286c72918ffb2aa04efa81b90bc4f9` at **both** the original shaping anchor (`32280aa2...`) and current `HEAD` — independently reproduced, matches the spec's cited value exactly (case-insensitive). The shaping-time whole-file hash (`ee506c3b...`) and current whole-file hash (`908faa59...`) both verified against their respective commits. D-H (lines 125-150) and D-I (lines 152-169) existence and content both reproduced and match the spec's quotes/paraphrases verbatim. This is a genuinely more robust anchor than a whole-file hash: it survives ledger growth by construction. | See "Verification notes" below for raw commands. |
| R1-F2 — required source list omits decisions ledger / design gate | `fix`: add `COORDINATOR-DECISIONS-2026-10-07.md` and `P0-B-DESIGN-GATE.md` to the source list | **CLOSED — verified correct.** Required source list items 12-13 add exactly these two files, with correct descriptions of D-G/D-H/D-I and the design-gate's §2 conflict. Both files are read-and-cross-referenced by the inventory (CI-001's disposition paragraph cites `P0-B-DESIGN-GATE.md` §2, lines 56-76 — reproduced exactly, word-for-word match against the live file) but explicitly excluded from the precedence *registry* itself, with a stated classification rule (see R2-F5 row below) rather than silently folded in as a ratified canon document. | Confirmed items 1-13 present; confirmed P0-B-DESIGN-GATE.md lines 56-76 match spec's quote. |
| R1-F3 — precedence worked example unverifiable/conflicting | `fix`: re-derive from live corpus or remove | **CLOSED — verified correct.** The prior spec's self-judging "charter provisionally ranks first" sentence is gone. The reworked rule 2 paragraph states the charter carries no internal dated ratification event (confirmed: `grep` for date patterns in `CHARTER.md` returns nothing; only the undated "100% Ratified" lineage phrase, confirmed at line 12) and the guidance-content-contract does (`2026-08-02`, confirmed), then explicitly declines to pre-judge the resulting rank ("This spec does not assert which of the two therefore ranks first under rule 2 — the builder applies rule 2 to the registry's actual dated evidence"). This removes the determinism-breaking self-contradiction the original BLOCKER identified, without inventing a new outcome-determinative assertion. | Reproduced `grep -n "2026-1\|2026-0" CHARTER.md` (no match) and `git log -1 --format=%ad -- CHARTER.md` (`2026-07-16`, a file-touch date, correctly distinguished in spec text from a ratification-event date). |
| R1-F4 — stale P2/P3-A registry-status claim | `fix`: refresh (P2 `done`; P3-A review-candidate) | **CLOSED — verified correct.** Lineage section now states P2 is `done`/closed with its closure record cited, and correctly describes P3-A as `REVIEW CANDIDATE` per its own file header with **no row at all** in `docs/specs/INDEX.md` (not "review-candidate there," the original stale framing). Reproduced directly: `docs/specs/INDEX.md` P2 row reads `done`; `grep -n "P3-A" docs/specs/INDEX.md` returns no match. | Live `INDEX.md` checked directly. |
| R1-F5 — seed quotes elide material qualifiers | `fix`: restore full quotations | **CLOSED — verified correct.** CI-001's `source_a` quotation now includes the charter's "when the applicable capability contract permits it" conditional clause and D13's closing sentence ("This does not authorize diagnosis, prescribing, clinician impersonation, or unsupervised alteration of prescribed treatment") verbatim. Reproduced directly against `CHARTER.md` lines 34 and 101 — exact match, no remaining elision of the scope-limiting language. | `grep -n` against CHARTER.md lines 34/101, byte-for-byte match. |
| R1-F6 — misattributed `coordinator-assigns-at-gate-2` precedent | `fix`: correct attribution to P2 | **CLOSED — verified correct.** "Exact allowed surfaces," item 9 now attributes the literal to "P2's realized precedent," citing P2.md's document contract 8/acceptance criterion 12 and `verify-p2.ps1` lines 642-643, and separately notes P3-A only describes it as a planned future usage with no live registry row. Reproduced: P2.md and `verify-p2.ps1` both use the literal live; P3-A.md has no `INDEX.md` row. | Grepped both files directly. |
| R1-F7 — `mandatoryStopConditions` fold not per-class | `fix`: make per-class explicit like `minimumChecks` | **CLOSED — verified correct.** New "`mandatoryStopConditions` fold (per class...)" table maps all twelve class-specific stop conditions (4 health-boundary + 3 privacy + 2 legal-policy + 3 knowledge-promotion) from `delivery-class-controls.json`, reproduced directly, 1:1 against the spec's table — every entry present and correctly mapped to a named P0-A tripwire or explicit `not-applicable` with reasoning. | Parsed `delivery-class-controls.json` directly; all 12 entries matched. |
| R2-F2 — `unreviewable` status nondeterministic (bypass) | `fix`: define deterministic criteria + required evidence | **CLOSED — verified correct.** Health-boundary "Missingness" now enumerates exactly three deterministic, independently-checkable conditions (deleted at `BaseCommit`, unreadable at dispatch time, outside worktree checkout) with required evidence per claim, and a new `unreviewable-claim-verified` deterministic check independently re-validates every `unreviewable` claim against the filesystem at `BaseCommit`, failing closed (`false-unreviewable-claim`) if the file is actually present and readable — directly closing the bypass vector R2-F2 identified. | Read in full; logically sound, fail-closed design. |
| R2-F3 — `BaseCommit` used but undefined | `fix`: define the term and its pinning rule | **CLOSED — verified correct.** New "`BaseCommit` (defined term)" section explicitly binds it to the Gate 2 dispatch record's commit (not either shaping anchor), states every clause that depends on it, and explains why neither shaping anchor qualifies (both predate P3-A's close). This mirrors the P2/P3-A "Gate 2 builder handoff requirements" precedent as the amendment requested. | Read in full; consistent with P2/P3-A's own `BaseCommit` handling pattern. |
| R2-F4 — exhaustiveness asserted, not checked | `fix`: add a checkable exhaustiveness criterion (corpus-coverage matrix) | **CLOSED — verified correct.** New deliverable `CORPUS-COVERAGE-MATRIX.md` (allowed surface 6) requires an outcome for every combinatorial pair of the 13 required sources, with a new `corpus-coverage-matrix-complete` check failing on any missing pair; the Objective section now explicitly distinguishes checkable *coverage* from best-effort, reviewer-backstopped *exhaustiveness of findings*, matching R2-F4's own suggested framing almost exactly. | Read in full; check is well-specified and matches acceptance criterion 2. |
| R2-F5 — property classification judgment-dependent | `fix`: document the classification judgment rule explicitly | **CLOSED — verified correct.** New "Classification guidance for borderline document types" paragraph in "Precedence manifest / Design" gives an explicit test (does the document's own text state a dated, named-owner ratification/sign-off event for *itself*, vs. only recording a ruling *about* other documents) and applies it by name to the two newly-added borderline sources (`COORDINATOR-DECISIONS-2026-10-07.md`, `P0-B-DESIGN-GATE.md`), both classified `no ratification event` / excluded from the registry, consistent with document contract 2's explicit exclusion of items 12-13. | Read in full; internally consistent with document contract 2's registry-table instruction. |

**All eleven triage `fix` rows for P0-A are genuinely closed.** No row was found to be
cosmetically addressed (reworded without substance) or to introduce a new determinism gap while
closing the cited one.

## D-I / D-B1(c) seed-row fidelity check (task item 3)

Compared CI-001's disposition and its explanatory paragraph directly against
`COORDINATOR-DECISIONS-2026-10-07.md` D-I (lines 152-169) and `P0-B-DESIGN-GATE.md` §2 (lines
56-76) and D-B1 (lines ~96-101):

- CI-001's `proposed_disposition: resolved-by-owner-ruling` with `handoff_target: P0-D1` is
  faithful to D-I's own instruction: *"the §2 canon conflict is resolved in substance by
  D-B1(c), and P0-A's contradiction inventory records its disposition by this ruling"*
  (reproduced verbatim, matches spec's quotation exactly).
- The spec correctly preserves `status: contradictory` for CI-001 (the underlying canon texts
  still textually disagree; D-I rules which posture governs the product *now*, it does not amend
  either document's text) — this is the correct analytical distinction and avoids the spec
  silently declaring the contradiction "resolved" in the sense of textually reconciled, which it
  is not.
- The spec accurately summarizes D-B1(c)'s substance (deterministic math + Class A/B/C now;
  `biostack-recommended` origination gated behind guidance-content-contract v2.0.0
  re-ratification) without restating or re-deriving the full D-B2–D-B6 behavior matrix, without
  asserting any applicability criterion or allowed/degraded/refused/escalated behavior of its own,
  and without describing P0-B-DESIGN-GATE.md's matrix as though P0-A were adopting or ratifying
  it. It attributes every substantive claim to D-I/D-B1(c) by name and citation. This stays inside
  P0-A's analytical lane — it reports that an owner ruling exists and what it says, it does not
  itself decide a product allowed-output.
- CI-002, CI-004, and CI-005 (narrower-scope siblings of the same headline conflict) are
  deliberately **not** given `resolved-by-owner-ruling` — they keep `proposed_disposition: fix`
  with a cross-reference to D-I and an explicit, reasoned explanation that D-I's text addresses
  only the headline dose/reconstitution/schedule-origination question (D-B1) and does not, by its
  own text, resolve the narrower protocol-builder/profile-aware-language/capability-map
  prohibitions. This is a careful, non-overreaching application of D-I — it would have been easy
  (and wrong) to blanket-apply "resolved-by-owner-ruling" to every row touching the same general
  topic; the spec does not do this.

**Verdict on this sub-task: faithful.** The seed row records D-I's/D-B1(c)'s disposition
accurately and narrowly, cites its exact source, and does not itself decide, imply, or pre-judge
any product allowed-output — the task's explicit concern.

## Missing pieces / collisions / unknowns

- None found that rise above the two MINOR notes above.
- Confirmed via `git log --oneline` on the worktree branch that `docs(governed-delivery): P0-A
  rework 1` is a single commit changing exactly one file
  (`docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md`, +274/-57 lines) relative to its
  parent — no scope creep into any other file, script, or canon document.
- Confirmed the dispatch precondition (P3-A must close before P0-A dispatch; P2 already closed)
  is unchanged in substance from the prior spec version and remains correctly stated against live
  registry state.
- Confirmed the charter's dependency spine string (`... -> P1 -> P2 -> P3-A -> P0-A -> P0-B ->
  ...`) and D14 fold mechanics (`minimumChecks`, `mandatoryStopConditions`, reviewer counts,
  `additionalMergeGate` values) cited in the spec match `CHARTER.md` and
  `docs/specs/schemas/delivery-class-controls.json` exactly, field for field.

## Verification notes

Checked directly against the repository (not taken on the spec's word):

- `sha256sum docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md` →
  `2ffa54311c93876eff52578b4f0afbe2e41f76e261221276db4525fc0922e6e9` (the hash reported above).
- `sha256sum docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` (current) →
  `908faa5999526de76cc7e21b6753d41bc8bdb9f29c5f1717a504d9103e4ca7ae` — matches spec's cited
  "current whole-file hash" exactly.
- `git show 32280aa2219100e20520a275d19af2fbdd1f32be:...COORDINATOR-DECISIONS-2026-10-07.md \|
  sha256sum` → `ee506c3b54fb57abd09bbb5180cd704aabca3eb73875b7e760ef84b538863d47` — matches spec's
  cited "shaping-time whole-file hash" exactly.
- `sed -n '114,123p' docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md | sha256sum`, run both
  against current `HEAD` and against the `32280aa2...` anchor via `git show` — **identical**
  result both times, `3d46ac19aef9a0f553239b6ec984e90216286c72918ffb2aa04efa81b90bc4f9`, matching
  the spec's cited D-G span hash exactly. This independently proves the spec's central new claim
  (a span hash is a robust, append-growth-immune anchor) rather than merely accepting it.
- Located and read D-G (lines 114-123), D-H (lines 125-150), D-I (lines 152-169) in full against
  the live ledger file — all three headings and line ranges match the spec's citations exactly;
  total file length 170 lines, consistent with D-I ending at the file's end.
- Read `P0-B-DESIGN-GATE.md` in full, including §2 (lines 56-76, reproduced exactly against the
  spec's citation) and the D-B1 decision table — confirmed the spec's paraphrase of D-B1(c) is
  accurate and does not overstate, understate, or alter its substance.
- Confirmed `docs/specs/INDEX.md`'s current P2 row reads `done` with real (non-placeholder)
  links, and that `P3-A` has no row at all — both match the spec's lineage claims.
- Confirmed `docs/specs/schemas/delivery-class-controls.json`'s `mandatoryStopConditions` (12
  entries across the 4 declared classes) and `minimumChecks`/`additionalMergeGate` fold values
  match the spec's fold tables field-for-field, via direct `python3 -c "json.load(...)"`
  inspection, not spec-text trust.
- Confirmed `CHARTER.md` has no internal dated ratification event (`grep` for date patterns
  returns nothing) and that `docs/guidance/biostack-guidance-content-contract.v1.md` carries an
  explicit `2026-08-02` dated, named-owner ratification record — both facts the spec's rule-2
  paragraph depends on, reproduced independently.
- Confirmed the row schema's eleven named fields are exactly the fields used in all ten seed
  rows (`CI-001` through `CI-010`), and that `status: contradictory` pairs with a non-`not-
  applicable` `proposed_disposition` in every contradictory row, with no violation.
- Confirmed no `TBD`/`TODO`/`FIXME`/`{{...}}` placeholder literal appears anywhere in the spec
  except as quoted, self-referential rule text describing what is forbidden in future
  deliverables (not an actual unresolved placeholder in this document).
- Confirmed the diff against this spec's parent commit touches exactly one file
  (`parcels/P0-A.md`), consistent with "nothing else may change in those diffs" (chain-state rule
  2 in `TRIAGE-2026-10-08.md`).
- Did not find any sentence in the spec, in its own voice, that states, implies, or could
  reasonably be read as deciding a product allowed-output, a substance/function-risk
  applicability criterion, or any allowed/degraded/refused/escalated behavior — including in the
  reworked CI-001 row and its explanatory paragraph, which was the highest-risk location for such
  drift given the new D-I cross-reference. The hard-constraint/stop-condition tripwire language is
  unchanged in substance from the (previously twice-REJECTed-on-other-grounds, never on this
  ground) prior version, and both prior reviews independently found this boundary intact; this
  re-review reconfirms it directly against the new CI-001 text specifically.
- No locked decision (D1-D18, the four product guidance classes, the ten substance/function-risk
  labels, the charter dependency spine, or P0-B-DESIGN-GATE.md's D-B1..D-B6 matrix) is restated
  incorrectly, altered, or pre-empted anywhere in the reworked spec.
