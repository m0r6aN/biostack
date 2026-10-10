# p0b_confirm — Final Bounded Confirmation Review (read-only)

- **Role:** p0b_confirm (read-only defensive validation reviewer; no git writes; no other reviews read)
- **Reviewed at:** branch tip of `feat/p0b-product-capability-contract`
- **Reviewed SHA:** `922ec87ec8484045068bc27c4799ce448a5c690e` ("fix(governed-delivery): P0-B impl fix 3 — byte-pin all normative text (closes F1/T4i + F2)")
- **Verifier:** `docs/specs/scripts/verify-p0b.ps1` (19 checks), run against `BaseCommit fd4b00b8d4a43cf100e71e58f1cd9bc25c9c0f3f`
- **Method:** full clone of the worktree into `/tmp/p0b-confirm/clone` (in /tmp copies ONLY — no writes to the repo or worktree); each penetration attempt applied as a targeted edit, verifier re-run, clone reset (`git checkout -- .` + evidence dir removed) between attempts
- **Verdict: PASS**

---

## 1. Penetration test results

All penetration attempts were run in the /tmp clone at the reviewed SHA with `pwsh -NoProfile` invoking `verify-p0b.ps1` with `BuilderId p0b_builder` and two reviewer IDs unless noted. "Held" = the verifier failed closed (non-zero exit, assertion thrown). No evasion was observed in any test.

### Test 1 — Edits to pinned formulaic criterion sentences (incl. comma-merged clauses): PASS (control held, 3/3)

| # | Attempt | Result | Control that caught it |
|---|---|---|---|
| 1a | Comma-merged behavior clause **with a citation-laundering `[cite: D-B1]` tag** appended to the pinned formulaic sentence (`ordinary`, JSON `applicabilityCriterion.test`) | **HELD** (rc=1) | check 19 `normative-text-byte-pin` — byte-exact JSON pin of the whole `test` field; the citation tag does not launder the appended clause |
| 1b | Comma-merged clause, **no citation tag**, appended to the pinned `LOCKED.` formulaic sentence (`controlled-or-illegal-sourcing`, JSON) | **HELD** (rc=1) | check 10 `unbounded-operationalization` (defense-in-depth stem scan fired first, before the byte-pin could) |
| 1c | **Narrowed criterion** — "required identity/concentration/manufacturing-source provenance field" → "required concentration provenance field" in the Markdown artifact's own criteria table row (`gray-market-or-identity-uncertain`) | **HELD** (rc=1) | check 19 `normative-text-byte-pin` — per-row squashed-content pin of the .md's own "Per-label applicability criteria" table |

### Test 2 — Edits to D-B rule text / matrix cells / preemptionOrder / compositionNote / D-J text / enablementState literals in EITHER artifact: PASS (control held, 11/11)

| # | Attempt | Artifact | Result | Control that caught it |
|---|---|---|---|---|
| 2a | D-B3 rule text weakened (dropped "evidence applicability, uncertainty, and risk controls … never as bare numbers") | JSON | **HELD** (rc=1) | check 19 `normative-text-byte-pin` (b) — JSON array diffed against §3 D-B3 |
| 2k | D-B3 rule 3 weakened in the .md's own prose ("may render in a neutral-arithmetic frame when convenient") | MD | **HELD** (rc=1) | check 19 `normative-text-byte-pin` (b) — .md prose independently diffed against §3 D-B3 |
| 2b | Matrix cell weakened: `minor-or-age-uncertain.C3` `refused` → `allowed` | JSON | **HELD** (rc=1) | check 11 `matrix-fidelity` — JSON cell diffed against §3 D-B2 ruled table |
| 2c | Matrix cell weakened in the .md's own per-label matrix table (`minor-or-age-uncertain` C3 `**R**` → `**A**`) | MD | **HELD** (rc=1) | check 11 `matrix-fidelity` — the .md's OWN table independently diffed against §3 D-B2 ground truth |
| 2d | preemptionOrder stage demotion: `minor-or-age-uncertain` moved from refusal-cap stage 3 to calibrating stage 4 | JSON | **HELD** (rc=1) | check 19 `normative-text-byte-pin` (c) — stage-label membership/order pin |
| 2e | compositionNote rewritten to a label-erasure license ("the strictest label's outcome may simply supersede and erase the others' obligations") | JSON | **HELD** (rc=1) | check 19 `normative-text-byte-pin` (c) — compositionNote vs §3 D-B2 ruled sentence |
| 2f | Same label-erasure rewrite in the .md's `compositionNote` prose | MD | **HELD** (rc=1) | check 19 `normative-text-byte-pin` (c) — .md compositionNote vs §3 D-B2 ground truth |
| 2g | D-J dose-context definition narrowed (deleted "or an amount-derived quantity") | JSON | **HELD** (rc=1) | check 19 `normative-text-byte-pin` (d) — `doseContextDefinition.text` vs COORDINATOR-DECISIONS-2026-10-07.md ## D-J |
| 2h | Same narrowing in the .md's own Dose-context definition (D-J) section | MD | **HELD** (rc=1) | check 19 `normative-text-byte-pin` (d) — .md D-J blockquote vs ground truth |
| 2i | enablementState literal changed: `currentPosture` → `"all-classes-enabled-today"` | JSON | **HELD** (rc=1) | check 7 `enablement-field-fidelity` — field-by-field literal check |
| 2j | Same literal changed **only** in the .md's embedded enablement JSON snippet | MD | **HELD** (rc=1) | check 19 `normative-text-byte-pin` (f) — .md snippet cross-checked against the JSON artifact |

Both-surface coverage confirmed: every protected surface class in the test charter was attacked in the JSON artifact AND in the Markdown artifact, and every attack was refused.

### Test 3 — Uncited extra sentence in applicabilityCriterion.test (any wording): PASS (control held, 2/2)

| # | Attempt | Artifact | Result | Control that caught it |
|---|---|---|---|---|
| 3a | Stem-free, vocabulary-free extra sentence appended ("Bunnies hop across the meadow at dawn." — contains none of refus/allow/degrad/escalat/block/suppress stems, so it evades any lexical blocklist by construction) | JSON (`injection-or-sterile-preparation`) | **HELD** (rc=1) | check 10 `unbounded-operationalization` — the closed-world sentence-citation rule: a non-pinned sentence with no `[cite: …]` tag fails regardless of wording |
| 3b | Same stem-free uncited sentence appended in the .md's own criteria table row (`minor-or-age-uncertain`) | MD | **HELD** (rc=1) | check 19 `normative-text-byte-pin` (e) — per-row content pin of the .md criteria table |

The wording-independence claim holds: the JSON-side catch is structural (sentence-splitting + citation presence), not vocabulary matching; the deliberately stem-free sentence was still refused.

### Test 4 — Single ReviewerId: PASS (control held)

Run with `-ReviewerIds @('p0b_impl_review_1')` (one ID): **HELD** (rc=1) — check 18 `dual-reviewer-sign-off-required` failed closed ("must carry at least two distinct, non-empty reviewer IDs … found 1"), before any evidence was written. (Also incidentally observed: a comma-joined single-string `p0b_impl_review_1,p0b_impl_review_2` is one distinct ID and likewise fails — the count is on distinct non-empty IDs, not on argument count.)

### Test 5 — Clean baseline: PASS (19/19)

Untouched clone at the reviewed SHA, two reviewer IDs: `P0-B verification PASS (19 checks)`, exit 0, all 19 checks green — matching the worktree's own `artifacts/p0b-verification/verification-summary.json` (headCommit `922ec87…`, pass=true, 19 checks).

---

## 2. Byte-faithfulness to P0-B-DESIGN-GATE.md §3 — CONFIRMED

Confirmed on two independent lines of evidence:

1. **Programmatic (the verifier, at baseline):** check 11 `matrix-fidelity` diffs all 10 labels × 3 guidance-class cells (30 cells) plus `calibrationRequired` text and the `locked` flag for BOTH artifacts against §3's parsed D-B2 ruled table — zero drift; check 19 `normative-text-byte-pin` diffed the D-B2 footnote, every D-B3–D-B6 rule/rung sentence (JSON array and the .md's own prose, each against §3), preemptionOrder stage membership/order, and compositionNote against §3 ground truth — all exact.
2. **Independent re-implementation (this reviewer, /tmp only):** a from-scratch Python re-parsing of §3 (D-B2 matrix table → JSON behavior cells + calibration; D-B3–D-B6 numbered rule sentences → JSON rule arrays, squashed/whitespace-and-emphasis-insensitive comparison, including the one pinned, both-artifact citation addendum on D-B4 rung 3) reported **zero drift** — FAITHFUL.

The D-J `doseContextDefinition.text` is pinned against Coordinator Decision D-J (its own ruled source, per the contract's `ruledBasis`), not §3, and that comparison also passes.

---

## 3. Notes and observations (non-blocking)

- **Honest scoping is preserved.** The verifier's own check descriptions state plainly which controls are structural (closed-world, paraphrase-resistant) and which are lexical defense-in-depth (the six-stem scan), and that citation *content accuracy* is a review-layer control, not an automated one. Pen tests 1b and 3a empirically confirmed the structural controls do what the descriptions claim.
- **Known, pinned addendum:** D-B4 rung 3 carries one shipped citation addendum (`(`docs/guidance/biostack-guidance-content-contract.v1.md`, "Required warning and uncertainty language" table — reused by reference, not re-defined).`), explicitly enumerated in the verifier's `$RuleTailAddenda` and present consistently in both artifacts — not drift; any other addition or edit to that exact addendum fails the pin.
- **Evidence-directory hygiene:** the verifier requires `artifacts/p0b-verification` to be untracked and writes the summary there only after all 19 checks pass; single-reviewer and content-attack runs fail before any summary is written.
- **MD criteria rows:** two of the ten .md table rows use substantively-equivalent-but-not-identical phrasing relative to the JSON field (a shipped authoring fact the verifier discloses and pins per-row against the .md's own ground truth); this is disclosed, pinned, and any edit to either surface still fails — confirmed by tests 1c and 3b.

---

## 4. Verdict

**PASS.** At SHA `922ec87ec8484045068bc27c4799ce448a5c690e`: clean baseline passes 19/19; all 18 penetration attempts (3 pinned-sentence edits, 11 protected-surface edits across both artifacts, 2 stem-free uncited-sentence insertions, 1 single-reviewer run, plus the comma-joined-ID variant) were refused — zero evasions. The contract content is byte-faithful to `P0-B-DESIGN-GATE.md` §3, confirmed both by the verifier's ground-truth diff checks at baseline and by this reviewer's independent re-implementation.
