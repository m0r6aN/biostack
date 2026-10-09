# p0b_impl_review_2 — P0-B implementation review

**Verdict:** PASS-WITH-FIXES
**Subject:** P0-B Product Capability and Safety Contract implementation
**Worktree:** `/home/cmorgan76/Repos/biostack-wt/p0b-impl`
**Branch:** `feat/p0b-product-capability-contract`
**Reviewed commit SHA (HEAD, tip):** `36fa450747f81a2c61b746c70445a32e34f43af0`
**BaseCommit (per `artifacts/p0b-verification/verification-summary.json`):** `fd4b00b8d4a43cf100e71e58f1cd9bc25c9c0f3f`
**Build authority:** `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md`
**Date:** 2026-10-09

## Summary

I read the P0-B parcel spec in full, the owner ruling chain (D-G/D-H/D-I/D-J/D-K in
`COORDINATOR-DECISIONS-2026-10-07.md`), and `P0-B-DESIGN-GATE.md` §3 (D-B1..D-B6). I then read
every byte of the two delivered artifacts
(`docs/specs/schemas/product-capability-safety-contract.{json,md}`), the
`classification-axes.schema.json` diff, the `README.md`/`INDEX.md` diffs, and the full
`verify-p0b.ps1` verifier (540 lines). I ran the verifier against the actual dispatch
(`BaseCommit fd4b00b` → `HEAD 36fa450`): **17/17 checks PASS**. I then adversarially corrupted
copies of the contract **in `/tmp` only** to test whether the verifier's named checks actually
catch violations, specifically targeting the five attack classes in my mandate.

**Content verdict:** the per-label behavior matrix, preemption order, numeric-provenance rules,
missing-input ladder, function-review rules, escalation semantics, and enablement gating are all
byte-for-byte faithful to the owner-ruled D-B1..D-B6 text and to D-J's dose-context definition. I
could not construct an output class that escapes the enablement gate, bypass a refusal cell via
multi-label fold, or smuggle dose semantics past the `R (dose-context)` cell using only the
artifact's own stated content — every attempted violation of the *committed* matrix content was
caught by `matrix-fidelity` or `enablement-field-fidelity` when I corrupted a `/tmp` copy (see
"Adversarial verifier testing," below). No `C3`/Class-D public-enablement path exists in the
artifact as shipped: `enablementState.biostackRecommendedOrigination.publiclyEnabled` is the
literal boolean `false`, every label carries `"enablementGatesC3": true`, and no sentence in
either artifact states or implies public availability today.

**Why not a clean PASS:** two findings below (F1, a mislabeled provenance marker; F2/F3, narrow
keyword-matching verifier checks that are evadable by paraphrase) are real defects that should be
fixed before merge, though neither currently causes the *shipped* artifact to say more than the
owner ruled. I also flag one pre-existing worktree contamination issue (F4) that reviewers after
me should be aware of.

## Ranked findings

### F1 — MAJOR: mislabeled provenance marker on `applicabilityCriterionDeferralRule`

**Claim:** the JSON artifact's top-level `applicabilityCriterionDeferralRule` block is tagged
`"basis": "[RULED — verbatim] P0-B.md, \"Per-label applicability criteria,\" closing paragraph"`,
but the source text it transcribes lives inside a section P0-B.md's own Deliverables text
explicitly marks `[OPERATIONALIZED — bounded]`, not `[RULED — verbatim]`.

**Evidence:**
- `docs/specs/schemas/product-capability-safety-contract.json`, key
  `applicabilityCriterionDeferralRule.basis`: `"[RULED — verbatim] P0-B.md, \"Per-label
  applicability criteria,\" closing paragraph"`.
- `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md`, the section heading immediately
  governing that closing paragraph: `### Per-label applicability criteria (required content —
  authored by this parcel, bounded by the closed vocabulary; **[OPERATIONALIZED — bounded]**, not
  a ruled D-B1..D-B6 clause)` (line ~454), with the deferral paragraph itself at line 478:
  `A criterion that cannot be evaluated deterministically from a function's own declared fields at
  invocation time ... defers to that function's own missing-input ladder behavior
  (`missingInputLadder`, rule 2) — it never causes this contract itself to guess an applicability
  determination.`

The text content is transcribed correctly (verified byte-identical), but its provenance marker is
wrong. P0-B.md's own "Escalation" bullet (Mandatory class sections, Health-boundary) names exactly
this category of error as a stop condition: *"a disputed `[RULED — verbatim]`-vs-`[OPERATIONALIZED
— bounded]` classification ... stops the parcel and returns to the coordinator for owner
escalation."* This is not a capability-widening defect (the rule itself is procedurally
conservative — it only says an undeterminable criterion defers to the missing-input ladder, it
does not grant any new allowed output), but mislabeling an `[OPERATIONALIZED — bounded]` clause as
owner-ruled falsely elevates its immutability status and is exactly the class of drift this
parcel's own traceability discipline exists to prevent.

**Smallest amendment:** change the `basis` string to `"[OPERATIONALIZED — bounded] P0-B.md,
\"Per-label applicability criteria,\" closing paragraph"`. Mirror the same correction in the `.md`
companion if it uses the same marker there (it does not explicitly mark this specific sentence
with either marker in the Markdown prose — recommend adding the marker there too for consistency).

**Does it change a locked decision?** No.

### F2 — MAJOR: `unbounded-operationalization` check is a narrow keyword blocklist, evadable by paraphrase

**Claim:** `verify-p0b.ps1`'s `unbounded-operationalization` check (the control meant to catch "an
applicability criterion that... introduces a product-behavior decision beyond detection, for
example, smuggling an allowed/refused rule into an applicability test" — P0-B.md, Deterministic
verification) only forbids six exact substrings (`'is refused'`, `'is allowed'`, `'is degraded'`,
`'is escalated'`, `'must be refused'`, `'must be allowed'`) inside an `applicabilityCriterion`'s
`test` field. Any semantically equivalent behavior directive phrased differently passes silently.

**Evidence/reproduction (in `/tmp` only, never touching the reviewed worktree's tracked state):**
I copied the worktree to `/tmp/p0b-corrupt-test`, then appended the sentence `"In this case, the
output should proceed without refusal."` to `labels["gray-market-or-identity-uncertain"]
.applicabilityCriterion.test` in the JSON artifact — a smuggled behavior directive inside a
detection-only field, exactly the pattern the check is named to catch. Ran
`verify-p0b.ps1 -BaseCommit fd4b00b8d4a43cf100e71e58f1cd9bc25c9c0f3f ...`: **all 17 checks
PASS**, including `unbounded-operationalization`. The script file itself, lines ~271-279
(`foreach ($forbidden in @('is refused', 'is allowed', 'is degraded', 'is escalated', 'must be
refused', 'must be allowed'))`), confirms the blocklist is exact-substring, not semantic.

**Scope note:** the actual shipped artifact at `36fa450` contains no such smuggled directive — all
ten `applicabilityCriterion.test` fields are clean, detection-only text (verified by direct
reading). This is a finding about the *verifier's* robustness as an automated backstop, not a
defect in the delivered content. Because dual independent human review plus owner Gate-3 approval
remain the primary controls (per "Review/gates"), this does not by itself constitute a fail-open
path for the parcel's actual output — but it means a future amendment to this contract that
smuggles a behavior rule into an applicability test via different wording would not be caught by
the automated check, only by human reviewers.

**Smallest amendment:** broaden the check to flag any applicability-criterion `test` field that
contains behavior-outcome vocabulary regardless of surrounding grammar (e.g., search for the cell
literals themselves — `refus`, `allow`, `degrad`, `escalat` stems — combined with a prescriptive
verb, or simpler: forbid the four cell-literal word-stems entirely inside `applicabilityCriterion
.test`, since a detection-only test should never need to name an output disposition at all).

**Does it change a locked decision?** No.

### F3 — MINOR: `enablement-field-fidelity`'s "forbidden availability phrase" list is similarly a narrow, evadable blocklist

**Claim:** the same script's `$ForbiddenAvailabilityPhrases` list (six exact phrases, e.g.
`'now publicly available'`, `'currently publicly enabled'`) is meant to catch any sentence
"stating or implying public Class D/C3 availability today" but only matches those exact strings.

**Reproduction (in `/tmp` only):** appended `"note": "biostack-recommended origination ships live
to all users today."` to `enablementState.biostackRecommendedOrigination` in the `/tmp` copy. Ran
the verifier: **all 17 checks still PASS**, `enablement-field-fidelity` included.

**Scope note:** same caveat as F2 — the shipped artifact contains no such sentence (confirmed by
direct reading of both artifacts; no availability-claiming language found anywhere). This is a
verifier-robustness gap, not a shipped-content defect, and is downgraded to MINOR relative to F2
because `enablement-field-fidelity`'s *structural* checks (exact-match on every required
`enablementState` field, `publiclyEnabled == false`, `enablementGatesC3 == true` on all ten
labels) are strict and were not evadable in my testing — only a free-text *additional* claim
elsewhere in the document would slip through, and the contract schema as specified does not
actually define a free-text field that would invite such an addition (I had to add a new,
non-schema key to demonstrate the gap).

**Smallest amendment:** either (a) forbid additive/unspecified keys inside `enablementState
.biostackRecommendedOrigination` entirely (closed-object validation), which would have prevented my
test case from being structurally possible, or (b) broaden the phrase list to a regex on
"publicly" + "available"/"enabled"/"live"/"shipped" co-occurrence within N words, case-insensitive.

**Does it change a locked decision?** No.

### F4 — INFORMATIONAL: pre-existing worktree contamination (not attributable to this review)

While testing, I found the worktree's *working tree* (not the committed tip) carries an
uncommitted, untracked modification to `docs/specs/schemas/product-capability-safety-contract.json`
(`gray-market-or-identity-uncertain`'s `applicabilityCriterion.test` field has an appended clause
not present at `HEAD`/`36fa450`). File `Modify` timestamp predates any command I ran in this
session. I did not create, and did not revert, this modification (read-only mandate). I confirmed
via `git show 36fa450:...` that **the committed tip is clean** and matches the content I reviewed
throughout this report — my verdict is based on the committed tip, not the dirty working tree.
Flagging this so the coordinator can confirm worktree hygiene before any further review pass reads
from this same worktree path (a stale or cross-contaminated worktree could mislead a
less-careful reviewer who trusts `cat`/`Read` over `git show <tip>:<path>`).

## Adversarial attack-class results (per my mandate)

- **(a) escape the enablement gate** (biostack-recommended value framed as deterministically
  derived; prefilled value as neutral arithmetic): not found in the shipped artifact.
  `numericProvenance` rule 3 explicitly forbids rendering a prefilled/recommended value in a
  neutral-arithmetic frame, and rule 4 makes missing provenance a hard refusal, not a downgrade —
  both transcribed verbatim and verified present and unweakened
  (`numeric-provenance-fail-closed-fixture` check, and my own direct reading). No numeric value,
  formula, or calculation exists anywhere in this parcel's artifacts (P0-B ships no runtime), so
  there is no concrete surface to mis-frame; the rule as written is the correct fail-closed
  definition for when such a surface is later built.
- **(b) exploit preemption order/multi-label fold to bypass a refusal cell** (e.g.
  pregnancy + ordinary): `preemptionOrder` is encoded as an explicit four-stage array with
  `pregnancy-or-lactation` in stage 3 (refusal-cap labels) and `ordinary` only ever appearing as
  the stage-4 residual/union label, with `compositionNote` stating verbatim "no label erases
  another's obligations." There is no runtime fold engine in this parcel to attack (P0-B defines,
  does not implement enforcement), and the textual definition itself does not admit an `ordinary`
  label silently suppressing a `pregnancy-or-lactation` refusal — union semantics, not override,
  are explicit.
- **(c) exploit dose-context scope to smuggle dose semantics past the R (dose-context) cell**:
  D-J's dose-context definition (transcribed verbatim and correctly, confirmed byte-for-byte
  against the ledger) is broad — any compound amount or amount-derived quantity, including
  syringe-unit renderings — and explicitly excludes only calendar/interval arithmetic over
  non-amount quantities. I could not identify a dose-semantic output class inside this
  definition's own text that would evade it (e.g. "relative multiplier of current dose" without an
  absolute amount is a residual edge case inherent to D-J's own owner-ratified definition, not an
  implementation deviation — the implementation transcribes D-J unmodified and does not narrow or
  reinterpret it).
- **(d) label row allowing more than Classes A-C permit today**: none found.
  `"enablementGatesC3": true` is present and verified on **all ten** labels (including the two
  locked labels and `controlled-or-illegal-sourcing`, whose `C3` value is already `"allowed"` in
  the ruled table but remains structurally gated), `publiclyEnabled` is the literal boolean
  `false`, and the `enablement-hard-fail-fixture` check proves (with a negative control) that the
  refusal is actually caused by the flag, not a vacuous always-true check. I independently
  reproduced this proof logic against the real artifact and it holds.
- **(e) break `matrix-fidelity` with a corrupted contract (`/tmp` only)**: attempted three times —
  weakening `pregnancy-or-lactation.C1` from `refused` to `degraded`, weakening
  `minor-or-age-uncertain.C3` from `refused` to `degraded`, and flipping `publiclyEnabled` to
  `true`. **All three were caught** (`matrix-fidelity` or `enablement-field-fidelity` threw and
  halted the script with an accurate diagnostic). `matrix-fidelity` genuinely re-parses
  `P0-B-DESIGN-GATE.md`'s own markdown table via regex and diffs every cell against the JSON at
  verification time — it does not merely assert a hardcoded expectation, so it is robust against
  drift in either direction. I was not able to defeat `matrix-fidelity` itself; the verifier gaps
  I did find (F2, F3) are in different, narrower checks.
- **(f) missing fail-closed paths**: the two gaps found (F2, F3) are both evadable "detect and
  reject" checks relying on substring blocklists rather than closed-vocabulary/semantic matching.
  Everything else I probed (matrix cells, enablement flag, axis-binding diff scope,
  canonical-write fencing, citation resolvability) is closed-form and held under adversarial
  testing.

## Verification notes

- Independently re-derived and matched all six cited SHA-256 hashes in
  `normativeInputs` against `git show <BaseCommit>:<path> | sha256sum` for
  `P0-B-DESIGN-GATE.md`, `CHARTER.md`, and `COORDINATOR-DECISIONS-2026-10-07.md` — all three
  matched exactly (case-insensitive).
- Re-ran `verify-p0b.ps1` against the actual dispatch (`BaseCommit fd4b00b`, `HEAD 36fa450`):
  17/17 checks PASS, matching the committed `artifacts/p0b-verification/verification-summary.json`
  (untracked evidence bundle, consistent with "Rollback"/evidence-directory discipline).
- Confirmed `git diff fd4b00b...36fa450 --stat` touches exactly the six files "Exact allowed
  surfaces" names, no more, no fewer.
- Confirmed `classification-axes.schema.json`'s diff touches only
  `controlBindingStatus`/`allowedOutputBindingStatus`/`determinationAuthority`/`status` for the
  ten `substanceFunctionRisk` labels, and that `deliveryClass`/`productGuidanceClass` axis objects
  and the label list itself are byte-identical to `BaseCommit` (independent diff read, not solely
  trusting `axis-binding-diff-scoped`'s own claim).
- Confirmed `docs/specs/README.md`/`INDEX.md` carry exactly one appended section/row each, zero
  other diffs.
- Confirmed the dispatch precondition: `docs/specs/INDEX.md` at `BaseCommit` shows both `P3-A` and
  `P0-A` rows as `done`.
- Read every per-label `applicabilityCriterion.test`, `behavior` cell, and `calibrationRequired`
  string in the JSON artifact and cross-checked each against the D-B2 table text in
  `P0-B-DESIGN-GATE.md` by eye, independent of the verifier script — found zero divergence beyond
  F1 (a provenance-marker, not content, defect).
- Confirmed no personal-data token, no placeholder marker (`TBD`/`TODO`/`FIXME`/`{{`), and no
  numeric formula/calculation pattern anywhere in the six allowed-surface files by direct reading
  (not solely trusting the corresponding automated checks).
- Searched for any sentence anywhere in both artifacts that could be read as asserting current
  public Class D/C3 availability or v2.0.0 ratification — found none.

## Recommendation

**PASS-WITH-FIXES.** The delivered contract content is a faithful, byte-for-byte, fail-closed
transcription of the owner-ruled D-B1..D-B6 matrix and D-J's dose-context amendment; it does not
say more than the owner approved, and I could not construct a path that makes it do so. Fix F1
(provenance-marker correction, no content change) and harden F2/F3 (close the keyword-blocklist
gaps in the verifier) before Gate 3 merge; none of the three is a blocker to the *content* as
written, but F2 in particular is exactly the kind of automated-backstop gap this parcel's own
"Deterministic verification" section was designed to close, and should not ship uncorrected into
the baseline future amendments (and P0-C's fixture work) will build on.
