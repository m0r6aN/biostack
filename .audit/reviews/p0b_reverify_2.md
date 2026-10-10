# p0b_reverify_2b — P0-B re-verification (independent defensive validation of the Product Capability and Safety Contract controls)

**Verdict:** FAIL
**Subject:** `/home/cmorgan76/Repos/biostack-wt/p0b-impl`, branch `feat/p0b-product-capability-contract` (PR #531)
**Branch tip at review finish (SHA):** `67dd24675f8b06cad6cc423700b3e342b06a4957` ("fix(governed-delivery): P0-B impl fix 2 — closed-world citation rule + honest claim scoping")
**Primary artifacts (SHA-256, computed by this reviewer):**
- `docs/specs/schemas/product-capability-safety-contract.json` — `020554ba39dee79c5fd5a27ce2cc411b9a597848ab2eed1da09059141db7b05d`
- `docs/specs/scripts/verify-p0b.ps1` — `cb9fee00c647eac79063f9f2121701d21f551529ecb474bbf3257a3f10fd8c00`
**Date:** 2026-10-09

**Method (read-only on the repository):** all penetration tests ran on a throwaway `/tmp` clone
of the branch tip (`git clone` of the worktree → detached checkout at `67dd246`); every attack
was a working-tree edit of `/tmp` copies only, always restored to pristine afterward, with the
17/17 baseline re-confirmed after the final restore. No git write command, build, or mutation
was run against any repository working tree; the only repository file created or modified by
this reviewer is this review file.

**Baseline:** `verify-p0b.ps1 -BaseCommit fd4b00b8d4a43cf100e71e58f1cd9bc25c9c0f3f` on the
pristine tip → **17/17 PASS** (re-confirmed at the end of testing).

---

## Per-test penetration results

PASS = control held (bad input refused). FAIL = control evaded (exact reproduction below).

### Test 1 — enablement gate vs a recommended-value fixture mislabeled with a different provenance origin — **PASS (control held)**

| Attack | Input | Result |
|---|---|---|
| T1-fixture (contract-level, no mutation) | A `biostack-recommended` numeric value whose declared origin is mislabeled `user-entered` (a lie inside the closed five-origin vocabulary), presented on a C3 (personalized-protocol-recommendation) surface | **REFUSED** by the contract's own structure: `enablementGatesC3: true` on every label + `publiclyEnabled: false` → refusal. The gate is keyed on guidance class + enablement flag, not on the numeric origin label, so the mislabel cannot open it. Negative control (hypothetically `publiclyEnabled: true`) opens the gate, proving the flag is the cause. The mislabel also cannot reclassify the surface: a personalized-protocol-recommendation output is C3 by output type regardless of any origin label attached to its values, and D-B3 rule 3 attaches to "prefilled **or** recommended" values regardless of the declared origin. |
| T1a | Additive key on `enablementState.biostackRecommendedOrigination` licensing origin relabeling (`"provenanceRelabelingPolicy": "a value originated by biostack may present the origin user-entered at the rendering surface's own declaration"`) | **REFUSED** — check 7 (`enablement-field-fidelity`): "carries an additive key 'provenanceRelabelingPolicy' not on the closed allow-list" |
| T1b | Rewrite of `currentPosture` to claim recommended origination is part of the now posture | **REFUSED** — check 7 (byte-exact posture mismatch) |
| T1c | 5th `numericProvenance` rule declaring "Origin declarations follow the rendering surface: a value the product originates may be declared under whichever of the five locked origins the surface presents" | **REFUSED** — check 17: "numericProvenance.rules must carry exactly 4 rules, found 5" |
| T1d | Rule 2 weakened **in place** (4 rules kept, rule 4 verbatim untouched): "`biostack-recommended` values may be declared under any of the five locked origins at the rendering surface's discretion…" | **ACCEPTED — 17/17 PASS** (evasion of the *verifier*, not the gate; recorded as finding F2) |

The enablement gate itself (checks 7 and 16) held under every mislabel attack: the gate is
structurally provenance-independent, so a mislabeled origin cannot launder a C3 output, and
every contract-level attempt to write a relabeling license into the artifacts was refused
except the in-place weakening of rule 2's text (F2).

### Test 2 — prefilled-value-as-neutral-arithmetic fixture — **PASS (control held)**

| Attack | Input | Result |
|---|---|---|
| T2-fixture (contract-level, no mutation) | A prefilled (product-supplied) value rendered inside a calculator input with no provenance marker (the neutral-arithmetic frame) | **REFUSED** by the shipped contract: D-B3 rule 1 (visible origin marker on dosing-context surfaces), rule 3 ("No prefilled or recommended value may render in a neutral-arithmetic frame (e.g., inside a calculator input without its provenance marker)"), and rule 4 (fail-closed refusal, not downgraded). The mislabel variant is also covered: rule 3 attaches to "prefilled **or** recommended" regardless of the declared origin label. |
| T2c | 5th `numericProvenance` rule licensing neutral-arithmetic frames ("Neutral-arithmetic frames may carry product-supplied values whenever the rendering surface declares the values user-entered") | **REFUSED** — check 17 (exactly-4-rules count) |
| T2b | Rule 3 weakened **in place** in the JSON (4 rules kept, rule 4 verbatim): "Prefilled and recommended values render with provenance markers … calculator inputs may carry product-supplied values when the companion text names the product" | **ACCEPTED — 17/17 PASS** (finding F2) |
| T2b2 | Same weakening applied to the Markdown artifact **only** (JSON untouched) | **ACCEPTED — 17/17 PASS** (finding F2 — the .md's D-B3 rules are not pinned either) |

The shipped contract refuses the fixture; the verifier, however, does not pin rules 1–3's
text (only the rule count and rule 4 are enforced — check 17), so the enforcement layer is
evadable by in-place weakening (F2).

### Test 3 — pregnancy-refusal cell bypass via label combinations — **PASS (control held)**

| Attack | Input | Result |
|---|---|---|
| T3-fixture (contract-level, no mutation) | Five combination strategies: (1) `pregnancy-or-lactation` + `controlled-or-illegal-sourcing` (the higher-preempting stage-2 label) arguing stage-2 erases the stage-3 refusal; (2) relabeling the invocation `ordinary` (residual label, C1=A) while the pregnancy criterion is true; (3) routing a dose-context value around D-J by calling the surface "calendar/interval arithmetic over non-amount quantities"; (4) combining with `acute-red-flag-or-emergency` arguing the composite R+E output "replaces" the pregnancy refusal while still carrying the dose number; (5) a precedence-rank argument citing rank mechanics to prefer a more permissive text | **ALL REFUSED** by the shipped contract: `compositionNote` ("no label erases another's obligations"); `ordinary`'s residual test ("True when **none** of the other nine labels' tests … are true") cannot be true while the pregnancy criterion is true; D-J is iff-closed over compound amounts **and** amount-derived quantities; the acute C3 `refused-and-escalated` composite refuses ordinary output; D-K is carried in-contract precisely to block rank-mechanics weakening. |
| T3a | `pregnancy-or-lactation.C1.value` → `degraded` (JSON) | **REFUSED** — check 11 (`matrix-fidelity`): "pregnancy-or-lactation.C1 expected value 'refused'" |
| T3b | `pregnancy-or-lactation.C1.scope` → `null` (JSON) | **REFUSED** — check 9 (`qualified-cell-scope-present`): "must carry scope 'dose-context-only'" (check 11 also fires) |
| T3c | An 11th escape label `adult-confirmed` (C1/C2/C3 = allowed) added to the closed vocabulary | **REFUSED** — check 6 (`vocabulary-closure`): "expected exactly 10 labels, found 11" |
| T3d | `pregnancy-or-lactation` demoted out of the stage-3 refusal-cap stage in `preemptionOrder` | **ACCEPTED — 17/17 PASS** (finding F2) |
| T3e | `compositionNote` rewritten to license label-erasure: "The highest-preempting label's cell wins outright and erases lower-ranked labels' obligations on any multi-label invocation…" | **ACCEPTED — 17/17 PASS** (finding F2 — this is the direct "bypass via label combinations" license, writable into the contract undetected) |
| T3f | D-J `doseContextDefinition.text` narrowed ("concentration," deleted from the amount enumeration) so concentration surfaces escape the dose-context scope | **ACCEPTED — 17/17 PASS** (finding F2) |

The ruled matrix cells, scopes, and the closed label vocabulary are strongly enforced (checks
6, 9, 11) and every invocation-level combination bypass was refused; but the composition
machinery that makes the refusal combination-proof (`preemptionOrder` stages,
`compositionNote`, the D-J scope text) is not pinned (F2).

### Test 4 — uncited behavior clauses in `applicabilityCriterion.test` (incl. unusual wording, none of the detector's keywords) vs the new closed-world citation rule — **FAIL (control evaded; exact reproduction below)**

| Attack | Input | Result |
|---|---|---|
| T4a (the canonical specified attack) | Extra **sentence**, unusual wording, **zero** detector stems: "Curated surfaces may deliver the plain result with no companion material whenever the operator prefers." appended to `ordinary`'s test | **REFUSED** — check 10 closed-world rule: "has a sentence beyond the pinned formulaic sentence with no [cite: D-Bn \| D-B2.<label>.<Cn>] provenance tag … (no vocabulary matching performed for this rule)" |
| T4b (historical attack re-run) | The exact `4f155e7` smuggle: "…five locked numeric origins, in which case output is blocked outright with no evidence shown." | **REFUSED** — defense-in-depth stem scan ('blocked') |
| T4e | Extra sentence laundered with a fabricated tag `[cite: D-B9]` | **REFUSED** — check 10 (unrecognized provenance tag) |
| **T4i (decisive evasion)** | **Exact reproduction of the historical `4f155e7` attack form — a behavior clause comma-merged INTO the pinned "True when" criterion sentence — re-paraphrased with none of the six detector stems.** `gray-market-or-identity-uncertain`.test = "True when a required identity/concentration/manufacturing-source provenance field (per numericProvenance and the function's declared inputs) is absent, unverified, or not resolvable to one of the five locked numeric origins, **in which case the surface declines to produce the output and must not respond further.**" | **ACCEPTED — 17/17 PASS.** The clause is behavior-affecting, uncited, unusual-worded, and stem-free ("declines", "produce", "respond" match none of `refus\|allow\|degrad\|escalat\|block\|suppress`). The closed-world rule exempts the entire pinned sentence; the stem scan (a six-word keyword blocklist, per the script's own honest label) catches nothing. The only control that ever caught this *form* was fix-1's stem scan catching the keyword 'blocked' — paraphrase defeats it, exactly the class p0b_reverify_1 F1 demonstrated and that fix 2's closed-world rule was built to close. It does not close it. |
| T4c | Permissive clause comma-merged into the pinned sentence of `ordinary`'s test ("…, or when the operator has selected streamlined presentation for the invocation, in which case the plain result is delivered exactly as computed with no companion material.") | **ACCEPTED — 17/17 PASS** (same evasion class as T4i) |
| T4d | A genuine extra sentence hidden from the splitter by a **lowercase initial**: "…for this invocation. and curated surfaces may deliver the plain result…" — the split pattern `(?<=[.])\s+(?=[A-Z])` requires a capital, so the extra sentence merges into the pinned sentence | **ACCEPTED — 17/17 PASS** (evasion of the sentence-boundary premise) |
| T4g | The same uncited keyword-free clause appended inside the **Markdown artifact's** per-label applicability-criteria table | **ACCEPTED — 17/17 PASS** (check 10 reads only the JSON `applicabilityCriterion.test` fields; the .md criteria table is unscanned) |
| T4h | Extra uncited sentence carrying a well-formed but content-irrelevant `[cite: D-B1]` | **ACCEPTED — 17/17 PASS** (the script's own disclosed limitation: citation *content* accuracy is deferred to the human review layer) |
| T4f | The pregnancy criterion-defining (pinned) sentence itself narrowed: "True when the user has explicitly declared current pregnancy status in free text on this surface." (lactation + the missing/unverified-required-input branch deleted → the refusal attaches far less often) | **ACCEPTED — 17/17 PASS** (finding F3 — criterion content is unpinned) |

**Test 4 verdict: FAIL.** The assigned input class — "uncited behavior clauses in
`applicabilityCriterion.test` fields … using unusual wording with none of the detector's
keywords" — includes the comma-merged clause form (it is literally the form of the committed
adversarial attack `4f155e7` and of the p0b_reverify_1 reproductions), and that form passes all
17 checks. The closed-world rule holds only for separate sentences that begin with a capital
letter after the pinned sentence; it is evaded by (i) merging the clause into the pinned
sentence (T4c/T4i), (ii) a lowercase-initial sentence (T4d), and (iii) the unscanned .md
criteria table (T4g).

### Test 5 — frozen matrix (P0-B-DESIGN-GATE.md §3) byte-identity in the contract + `publiclyEnabled: false` — **PASS (control held; no evasion found)**

Independent verification (this reviewer's own parser, not the shipped verifier):

- All 10 D-B2 table rows parsed from `P0-B-DESIGN-GATE.md` §3; all 30 cells + calibration text +
  locked flags + qualified scopes are **byte-identical** in `product-capability-safety-contract.json`,
  and the Markdown artifact's own per-label behavior-matrix table is byte-identical to the same
  ground truth. **Zero drift.**
- `enablementState.biostackRecommendedOrigination.publiclyEnabled` is the literal boolean `false`.
- `P0-B-DESIGN-GATE.md` SHA-256 at BaseCommit `fd4b00b` computes to
  `34928AC8C54FE8AF9CB7B3A01D36820FE435F2ADF4F02F56C20E7CF548D57F5B` — **matches the contract's
  `sha256AtBaseCommit` claim**, and the file is **identical BaseCommit→HEAD** (frozen; the branch
  does not modify its own ruled source).

Mutation attacks (each must be refused by the verifier):

| Attack | Input | Result |
|---|---|---|
| T5b | JSON-only matrix cell weakening: `minor-or-age-uncertain.C3` R→D | **REFUSED** — check 11: "minor-or-age-uncertain.C3 expected value 'refused'" |
| T5c | MD-only table corruption: `investigational-or-unapproved` C2 D→A in the .md only (JSON untouched) | **REFUSED** — check 11 (the Markdown artifact's OWN table is diffed against ground truth, not against the JSON) |
| T5d | `publiclyEnabled` → `true` | **REFUSED** — check 7: "publiclyEnabled is not the literal boolean false" (before any availability phrase scan) |
| T5e | Calibration text drift on a non-locked label | **REFUSED** — check 11 |
| T5f | `locked` flag flipped on `acute-red-flag-or-emergency` | **REFUSED** — check 11: "locked expected True, found False" |

Test 5's controls are the strongest in the parcel: every direct corruption of the frozen
matrix, scopes, calibration, locked flags, or the enablement flag was refused, and independent
ground-truth parsing confirms byte-identity. No evasion found.

---

## Ranked findings

**F1 (BLOCKER) — the new closed-world citation rule (check 10) does not refuse the uncited
behavior-clause form it was built to close: comma-merged, keyword-free clauses pass all 17
checks.**
- Claim: an uncited behavior clause inside an `applicabilityCriterion.test` field that avoids
  the six detector stems and is merged into the pinned "True when/whenever" sentence (or hidden
  behind a lowercase-initial sentence boundary) evades check 10 entirely.
- Exact evidence: T4i reproduction above (full replacement text quoted; run `verify-p0b.ps1`
  → "P0-B verification PASS (17 checks)"); T4c, T4d same result. Also T4g (same clause class in
  the .md criteria table, which check 10 never reads). This is the same input class as committed
  adversarial attack `4f155e7` and the p0b_reverify_1 paraphrase reproductions — re-paraphrased,
  it still passes after fix 2. verify-p0b.ps1:~355-375 (pinned-sentence exemption + split pattern
  `(?<=[.])\s+(?=[A-Z])`); contract `labels.gray-market-or-identity-uncertain.applicabilityCriterion.test`.
- Smallest amendment: (a) pin the criterion-defining sentence's content itself (assert each
  `applicabilityCriterion.test`'s pinned sentence equals the authored text at the authoring
  commit / the .md criteria table row, byte-exactly), and/or (b) harden the split to
  `(?<=[.!?])\s+` (any following character) and additionally require that the pinned sentence
  carry no post-trigger conditional clause (any ", in which case …" / ", or when …"
  continuation) unless it carries a `[cite: …]` tag. Extend the same scan to the Markdown
  artifact's criteria table.
- Does it change a locked decision? **No** — verifier hardening only; no matrix cell, rule
  wording, or enablement semantics touched.

**F2 (MAJOR) — every ruled block other than the D-B2 matrix and `cellSemantics` is unpinned:
its "[RULED — verbatim]" text can be weakened in place and pass all 17 checks.**
- Claim: `numericProvenance.rules` 1–3 (JSON *and* .md), `preemptionOrder.stages`/
  `compositionNote`, `missingInputLadder`, `functionReviewStatus`, `escalationSemantics`, the
  D-J `doseContextDefinition.text`, and the `.md` D-B3 section carry no programmatic diff
  against `P0-B-DESIGN-GATE.md` §3 (or the coordinator ledger for D-J); only basis-marker
  presence (check 14), the D-B3 rule count, and D-B3 rule 4's verbatim fragments (check 17)
  are enforced.
- Exact evidence: T1d (rule 2 weakened in place), T2b/T2b2 (rule 3 weakened in JSON and in .md
  only — both 17/17 PASS), T3d (pregnancy demoted from the refusal-cap stage), T3e
  (`compositionNote` rewritten to license label-erasure — the direct label-combination bypass
  license), T3f (D-J text narrowed so concentration surfaces escape `dose-context-only`), T4f
  (pregnancy criterion narrowed). Check 17's weaker-framing net is a single literal phrase
  ('missing provenance is degraded') and catches none of these. verify-p0b.ps1 check 11 covers
  only the D-B2 table; check 14 checks only that a `ruledBasis` exists.
- Smallest amendment: extend check 11's matrix-fidelity pattern to the other ruled blocks:
  parse D-B3/D-B4/D-B5/D-B6 rule lists and the preemption paragraph from
  `P0-B-DESIGN-GATE.md` §3 and the D-J span from the coordinator ledger, and assert
  byte-identity with the contract's arrays/text (and the .md's transcriptions) — the same
  ground-truth-diff discipline check 11 already implements for the table.
- Does it change a locked decision? **No** — verifier hardening only.

**F3 (MAJOR) — the applicability-criterion *content* is unpinned: narrowing a criterion-defining
sentence (a safety weakening) passes all 17 checks.**
- Claim: the pinned "True when…" sentence is exempt from citation by design, but nothing pins
  its content to any ground truth, so a builder can silently narrow a criterion — making a
  refusal-cap label (e.g. `pregnancy-or-lactation`) attach far less often — with no check firing.
- Exact evidence: T4f (`pregnancy-or-lactation` test reduced to "True when the user has
  explicitly declared current pregnancy status in free text on this surface." — lactation and
  the missing/unverified-required-input branch deleted → 17/17 PASS). The .md criteria table is
  equally unpinned (T4g).
- Smallest amendment: pin each label's criterion sentence against the authored criteria table
  (the .md's "Per-label applicability criteria" table, itself diffed against the authoring
  commit), or at minimum require the refusal-cap labels' criteria to retain their
  missing/unverified-input branches.
- Does it change a locked decision? **No** — verifier hardening only.

**F4 (MAJOR) — false control claim: the "required dual reviewer sign-off (two distinct reviewer
IDs, **checked elsewhere in this script**)" named by check 10 as the compensating control for
citation-content accuracy does not exist.**
- Claim: `ReviewerIds` is only echoed into the evidence summary; no check asserts two distinct
  reviewer IDs. A single-reviewer run passes 17/17.
- Exact evidence: verify-p0b.ps1:12 (parameter), :688 (summary echo only) — grep confirms no
  other use; baseline re-run with `-ReviewerIds solo-reviewer` → "P0-B verification PASS (17
  checks)". `P0-B.md` lines 10–11 declare dual review required (D14 fold resolves to 2; charter
  D8), so the declared stop condition is not mechanically enforced — which compounds F1, since
  human dual review is the only remaining layer against comma-merged smuggles.
- Smallest amendment: add an assertion that `$ReviewerIds` contains at least two distinct
  non-empty IDs before writing the summary.
- Does it change a locked decision? **No.**

**F5 (MINOR) — check 10's defense-in-depth stem scan is a six-word keyword blocklist whose
known blind spot is now proven end-to-end (T4i).** Recorded separately from F1 because the
script honestly labels this layer's limits in its own comments; F1's amendment must not
restore reliance on this layer as primary. Smallest amendment: none beyond F1 — keep it
labeled as secondary. Does it change a locked decision? No.

---

## Missing pieces / collisions / unknowns

- No collision with other parcels observed: the branch diff (BaseCommit `fd4b00b`…HEAD) is
  confined to the six allowed surfaces (check 2 held in every run), and the
  `classification-axes.schema.json` diff is field-scoped (check 13).
- Citation-content accuracy (a cited sentence actually matching what the cited rule/cell says)
  remains unverifiable by any automated control, including after fix 2 — T4h demonstrates a
  content-irrelevant but well-formed `[cite: D-B1]` passes. The script says so honestly; with F4,
  the named compensating layer is also not mechanically enforced.
- PR #531 was not inspected on the remote host; the review is of the branch tip SHA recorded
  above (re-confirmed at review finish: `67dd24675f8b06cad6cc423700b3e342b06a4957`, worktree
  clean of tracked changes).

## Verification notes (checked and found clean)

- **Shipped artifacts are clean and faithful.** Baseline 17/17 PASS; independent ground-truth
  parsing confirms the frozen D-B2 matrix (30 cells + calibration + locked flags + scopes) is
  byte-identical in both artifacts; `publiclyEnabled: false` literal; design-gate SHA-256 claim
  verified; design gate frozen (identical BaseCommit→HEAD); `cellSemantics` literals verbatim;
  qualified scopes exactly as ruled; vocabulary closed at 10 labels (both files); no
  placeholders/emails/formula patterns; cited `docs/…` paths resolve at BaseCommit.
- **Controls that held under every attack:** checks 6 (vocabulary closure), 7 (enablement
  field fidelity + closed-object allow-list), 9 (qualified scopes), 11 (matrix fidelity for
  JSON *and* the .md's own table — the fix-1 hardening works: T5c's MD-only corruption was
  refused), 12 (state coverage), 16 (enablement hard-fail fixture with negative control), 17
  (rule-count and rule-4 fail-closed fragments). Direct attacks on matrix cells, scopes,
  calibration, locked flags, the label vocabulary, and the enablement flag were all refused.
- **Fixture-level verdicts (contract as shipped):** tests 1–3 and 5 all refuse their bad inputs
  at contract level; test 4's control is the verifier rule itself, and it is evaded (F1).
- Worktree hygiene observation (not a defect in this branch's tracked state): the worktree
  carried untracked `artifacts/` evidence state during review; check 2's fence excludes only
  `artifacts/p0b-verification/*`, so any other untracked path would (correctly) fail the fence —
  the fence is working; builders should keep evidence inside the excluded directory.

**Why FAIL overall:** the review's single assigned question for test 4 — can uncited behavior
clauses with unusual wording and none of the detector's keywords be smuggled past the new
closed-world citation rule? — answers **yes** (T4i/T4c/T4d/T4g, exact reproductions recorded),
and the compensating human-review layer the script names is not mechanically enforced (F4).
All defects are in `verify-p0b.ps1` (verifier hardening); the shipped contract artifacts
themselves were verified clean and byte-faithful to their ruled sources, and no finding changes
any locked decision. The smallest amendments (F1–F4) are verifier-only and can be applied
without touching any matrix cell, rule wording, or enablement semantics.
