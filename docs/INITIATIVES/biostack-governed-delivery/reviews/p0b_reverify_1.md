# p0b_reverify_1 — P0-B implementation fix (95252e6) targeted re-verification

**Verdict:** FAIL

**Commit:** 95252e6 ("fix(governed-delivery): P0-B impl fix 1 — verifier provenance-trace hardening")
**Worktree:** /home/cmorgan76/Repos/biostack-wt/p0b-impl
**Branch:** feat/p0b-product-capability-contract (PR #531)
**BaseCommit used for verifier runs:** fd4b00b8d4a43cf100e71e58f1cd9bc25c9c0f3f (dispatch-record commit, per `dispatch/GATE2-P0B-IMPLEMENTATION.md`)
**Date:** 2026-10-09 (re-verification date; environment clock)

**File SHA-256 (at HEAD, informational):**
- `docs/specs/scripts/verify-p0b.ps1`: `daa8393b3d350e13d7289881dd303d8f6f98285aa639be6fd2d3705c8e4550fb`
- `docs/specs/schemas/product-capability-safety-contract.json`: `d1a00046f214152cae92f838ed2cec262efef79f7d5a3ccaa8377857c896c342`
- `docs/specs/schemas/product-capability-safety-contract.md`: `870b11e8d2c6559688d3f92b7686cf128d3649d4f5674e6a5fbbe1f1ff5b29cb`

## Scope of this re-verification

Targeted re-check of the five claims in the dispatch for this reverify pass. Not a full
independent spec review; findings below are limited to what was tested.

## Result summary

| # | Claim | Result |
|---|---|---|
| 1 | matrix-fidelity now covers MD's own per-label table (corrupt → must FAIL) | **CONFIRMED** |
| 2a | provenance-trace is structural; uncited behavior field → must FAIL | **CONFIRMED** |
| 2b | paraphrased smuggle → must FAIL | **FAILED TO FAIL — the paraphrase passed all 17 checks** |
| 3 | `applicabilityCriterionDeferralRule` marker now correct | **CONFIRMED** |
| 4 | matrix/rule/enablement content unchanged vs. `P0-B-DESIGN-GATE.md` §3 | **CONFIRMED for the ruled D-B2 table/rules/enablement block, but one undisclosed non-ruled content edit found (see F2)** |
| 5 | 17/17 clean on real content | **CONFIRMED** |

## Ranked findings

### F1 (BLOCKER) — "Paraphrase-proof" provenance-trace claim is false; a paraphrased behavior smuggle passes all 17 checks

**Claim in commit message:** "Replaced with a structural, paraphrase-proof provenance-trace
(allow-list) model: any behavior-outcome vocabulary ... inside an `applicabilityCriterion.test`
field must carry a nearby `[cite: ...]` tag ... uncited behavior-affecting content fails
regardless of wording."

**Evidence:** `docs/specs/scripts/verify-p0b.ps1`, check 10 ("unbounded-operationalization"),
the actual detector is:

```
$behaviorStemPattern = '(?i)(refus\w*|allow\w*|degrad\w*|escalat\w*|\bblock\w*|suppress\w*)'
```

This is still a closed keyword list (six stems), not a structural/semantic detector. It is
marketed as "paraphrase-proof" but is a lexical blocklist with a slightly larger vocabulary than
the prior six-phrase version it replaced — the same class of control the commit claims to have
eliminated.

**Reproduction (in `/tmp/p0b-corrupt`, a clean copy of the worktree at HEAD):**

Edited `docs/specs/schemas/product-capability-safety-contract.json`,
`labels.ordinary.applicabilityCriterion.test`, appending an uncited behavior-affecting sentence
that avoids every listed stem:

- Attempt 1: `"...for this invocation. If the user requests an unusual dosage form the function must decline to produce the output and must not respond further."`
- Attempt 2: `"...for this invocation. For an unusual dosage form the function will stay silent and not produce an answer, deferring the request to a human reviewer."`

Ran: `pwsh -NoProfile -File docs/specs/scripts/verify-p0b.ps1 -BaseCommit fd4b00b8d4a43cf100e71e58f1cd9bc25c9c0f3f -BuilderId p0b_builder -ReviewerIds 'p0b_impl_review_1','p0b_impl_review_2' -EvidenceDirectory artifacts/p0b-verification`

Both attempts produced: `P0-B verification PASS (17 checks)` — the uncited, newly-introduced
refusal/non-response behavior clause (which the matrix never authorizes for `ordinary` at any
cell: `ordinary` is A/A/A, no refusal state at all) is silently accepted.

By contrast, the exact-word version (`"...the function should refuse the output."`, using the
listed stem `refus\w*`) correctly throws:
`unbounded-operationalization: ordinary applicabilityCriterion test contains behavior-affecting
vocabulary ('refuse') with no nearby [cite: ...] provenance citation ...` — confirming the gap is
specifically the paraphrase-evasion path the commit message claims is now closed, not a general
detector failure.

**Smallest fix:** Either (a) honestly rename/re-scope the claim — this is a six-word-stem
blocklist extension, explicitly label it as defense-in-depth against the *specific* known
evasions, not as "paraphrase-proof" or "structural, regardless of wording"; or (b) implement an
actual structural control, e.g. require every `applicabilityCriterion.test` sentence that
introduces a new disposition-shaping clause to carry *some* `[cite: ...]` tag by default (closed
allow-list of *uncited* sentence patterns, rather than open free text with a few forbidden
stems), or run an LLM-based semantic check as a second-pass reviewer gate. As shipped, this is a
re-skinned blocklist, and the commit message materially overstates what it does. This is the
exact class of finding (R1-F2/R2-F2) the fix commit claims to have closed; it has not been
closed, only narrowed.

**Does this change a locked decision?** No — this is a verifier-tooling gap, not a change to the
ruled D-B1..D-B6 matrix content itself. But it directly undermines the delivered control the
P0-B contract depends on (every behavior-affecting clause must be traceable to a ruled source),
and the PR's own stated remediation claim for R1-F2/R2-F2 is false as shipped.

### F2 (MINOR) — Undisclosed content edit in `gray-market-or-identity-uncertain`'s `applicabilityCriterion.test`, not mentioned in the commit message

**Claim in commit message:** "Verifier-hardening + one provenance-marker correction only — no
matrix cell, rule wording, or enablement semantics changed" and, specifically on item 2, "Added
the one needed citation tag to `interaction-or-contraindication-signal`'s test field (content
unchanged, traceability annotation only)."

**Evidence:** `git diff 4f155e7 95252e6 -- docs/specs/schemas/product-capability-safety-contract.json`
shows a second, undisclosed edit to `labels.gray-market-or-identity-uncertain.applicabilityCriterion.test`:

```
- "...not resolvable to one of the five locked numeric origins, in which case output is blocked outright with no evidence shown."
+ "...not resolvable to one of the five locked numeric origins."
```

The deleted clause used the word "blocked," which the hardened check 10 would have flagged as
uncited behavior-affecting vocabulary; rather than cite it, the builder silently deleted it. The
deletion does not corrupt the ruled matrix cells (`gray-market-or-identity-uncertain` remains
D/D/D, unaffected) and does not appear to contradict `numericProvenance` rule 4 ("missing
provenance → refused, fail-closed"), so this is likely benign in substance — but it is an
undisclosed content change to a per-label criterion's test prose that the commit message
explicitly claims did not happen ("content unchanged" is asserted for the *other* label's edit;
this edit is not mentioned at all). A matching equivalent deletion was made in
`product-capability-safety-contract.md`'s mirrored table row (confirmed via the same diff) so
JSON/MD stayed consistent with each other on this point.

**Smallest fix:** Amend the commit message to disclose this second content edit explicitly (what
changed, why, and that it is non-matrix, non-rule content), per the same transparency standard
already applied to the `interaction-or-contraindication-signal` edit.

**Does this change a locked decision?** No.

## Verification notes (what was checked and found clean)

- **Claim 1 (MD-table matrix-fidelity).** Read `verify-p0b.ps1` check 11 in full. Confirmed it
  now parses `product-capability-safety-contract.md`'s own per-label table with the same
  `$tableRowPattern`/`ConvertTo-ExpectedCell` logic used for the design-gate table, and diffs
  cell-for-cell against `$expectedByLabel` (ground truth parsed directly from
  `P0-B-DESIGN-GATE.md`, not against the JSON artifact's claims). **Reproduced the fail**: in
  `/tmp/p0b-corrupt`, changed `ordinary`'s `C3` cell in the Markdown table only (JSON left
  untouched) from `A` to `D`. Verifier threw: `matrix-fidelity: the Markdown artifact's OWN
  per-label behavior matrix table cell ordinary.C3 expected value 'allowed' ..., found 'degraded'
  in product-capability-safety-contract.md`. Confirmed.
- **Claim 2a (uncited behavior field fails).** Reproduced: appending `"...the function should
  refuse the output."` (no citation) to `ordinary`'s test field correctly throws
  `unbounded-operationalization: ... contains behavior-affecting vocabulary ('refuse') with no
  nearby [cite: ...] provenance citation`. Confirmed.
- **Claim 2b (paraphrased smuggle fails).** **Did not confirm — see F1.** Both paraphrase
  attempts passed all 17 checks.
- **Claim 3 (deferral-rule marker).** Confirmed `applicabilityCriterionDeferralRule.basis` in the
  JSON artifact now reads `[OPERATIONALIZED — bounded] P0-B.md, "Per-label applicability
  criteria," closing paragraph (section header: "required content — authored by this parcel,
  bounded by the closed vocabulary; [OPERATIONALIZED — bounded], not a ruled D-B1..D-B6
  clause")`. Confirmed against `P0-B.md` line 457: `### Per-label applicability criteria
  (required content — authored by this parcel, bounded by the closed vocabulary;
  [OPERATIONALIZED — bounded], not a ruled D-B1..D-B6 clause)` — the cited section header text
  matches verbatim. Confirmed check 14 (`no-unattributed-claim`) re-derives this from `P0-B.md`
  itself at runtime (not a static string), so a future drift of `P0-B.md`'s own marker would be
  caught.
- **Claim 4 (matrix/rule/enablement unchanged).** Manually compared `P0-B-DESIGN-GATE.md` §3
  D-B2 table (lines 100-109) and D-B3 rules (lines 118-131) against the JSON artifact's `labels`
  object and `numericProvenance` block — byte-identical in content/semantics. Ran the verifier's
  own `matrix-fidelity` check against real content (17/17 PASS, see below) and separately
  corrupted a cell to confirm the check is live, not vacuous (see Claim 1 reproduction).
  `git diff fd4b00b 95252e6 --name-only` confirms only the six `AllowedSurfaces` files changed;
  no canon document (`P0-B-DESIGN-GATE.md`, `P0-B.md`, `CHARTER.md`, `COORDINATOR-DECISIONS-*.md`)
  was touched by this commit. One undisclosed non-matrix content edit found — see F2.
- **Claim 5 (17/17 clean).** Ran `verify-p0b.ps1` against the actual worktree HEAD (95252e6) with
  `BaseCommit=fd4b00b8d4a43cf100e71e58f1cd9bc25c9c0f3f`, `BuilderId=p0b_builder`,
  `ReviewerIds=p0b_impl_review_1,p0b_impl_review_2`, `EvidenceDirectory=artifacts/p0b-verification`.
  Output: all 17 named checks printed `PASS:`, final line `P0-B verification PASS (17 checks)`.
  Confirmed.
- Spot-checked `enablementState.biostackRecommendedOrigination.publiclyEnabled == false` and the
  closed-key allow-list logic (check 7) — correct and unchanged in substance from prior review
  cycle; not itself part of this commit's claimed changes.
- Did not re-review checks 1-9, 12-17 line-by-line for new defects beyond what's described in the
  commit message; those were not part of this commit's claimed delta and were only spot-checked
  for pass/fail behavior via the full clean run.

## Disposition

Findings F1 and F2 were found via direct adversarial reproduction against a disposable `/tmp`
copy of the worktree (`/tmp/p0b-corrupt`); no repository file was modified. No git write
commands were run. This reviewer's output file is the only file written.

**Overall verdict: FAIL.** Claim 2b (paraphrased smuggle must fail) does not hold — a paraphrased,
uncited behavior-affecting clause passes the hardened verifier cleanly, contradicting the commit's
central "structural, paraphrase-proof" claim for the R1-F2/R2-F2 remediation. This is a BLOCKER:
the delivered control does not do what the PR claims it does, for the exact attack class it was
built to close. F2 is a MINOR transparency gap (undisclosed non-matrix content edit) that does not
independently block, but should be corrected alongside F1.
