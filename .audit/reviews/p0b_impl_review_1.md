# p0b_impl_review_1 — P0-B implementation review

**Verdict:** PASS-WITH-FIXES
**Subject:** `/home/cmorgan76/Repos/biostack-wt/p0b-impl`, branch `feat/p0b-product-capability-contract`
**Commit reviewed (tip):** `36fa450747f81a2c61b746c70445a32e34f43af0`
**Parent (BaseCommit used for verification):** `fd4b00b8d4a43cf100e71e58f1cd9bc25c9c0f3f` ("P0-A SEALED ... P0-B implementation Gate 2 activated")
**Build authority:** `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md`
**Normative design input:** `docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` §3 (D-B1..D-B6), Coordinator Decision D-I, plus amendments D-J (dose-context definition) and D-K (precedence-directional constraint)
**Date:** 2026-10-09

**File hashes (computed independently, this review):**
- `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md` — SHA-256 `4A49E3D6086670857D0190AD0032E8DE0C50DED14F9830C2ECFABE1A4B414347`
- `docs/specs/schemas/product-capability-safety-contract.json` — SHA-256 `9AB3F01CC9814CC58C29253A5B4E06FB662C39E01E9447338B22E518B96EAC39`
- `docs/specs/schemas/product-capability-safety-contract.md` — SHA-256 `1D5931A7C0843D700A470D3BA2E3523BA3CF46E57FD27FF145202481D552AFFE`
- `docs/specs/scripts/verify-p0b.ps1` — SHA-256 `66633280844D04D1B45C292A237D61E8E63E914B8ED5571182642740B91911D0`

## Summary

The implementation is content-faithful to the owner-ruled D-B1..D-B6 matrix: I independently
transcribed and diffed every cell of the 10-label × 3-class (30-cell) matrix, the calibration
text, the locked flags, the five numeric-provenance origins, the missing-input ladder, the
function-review rules, the escalation rules, the D-J dose-context definition, and the D-K
constraint, against `P0-B-DESIGN-GATE.md` §3 and `COORDINATOR-DECISIONS-2026-10-07.md` directly —
I found **zero drift**. Normative-input hashes embedded in the artifact (`normativeInputs.*.sha256AtBaseCommit`)
match the actual file contents at the pinned BaseCommit, independently recomputed. The
`classification-axes.schema.json` diff is scoped to exactly the fields named in "Exact allowed
surfaces," item 3, and nothing else. `docs/specs/README.md` and `docs/specs/INDEX.md` each carry
exactly one appended block, zero other diffs. No file outside the six allowed surfaces was
touched. No `TBD`/`TODO`/`FIXME`/`{{` marker exists in any file this parcel created or modified.

However, adversarial testing of `verify-p0b.ps1` (run against a throwaway `/tmp` copy of this
repository, never against the reviewed repo) found two real non-vacuousness gaps in the
deterministic verifier that let a **constructed nonconforming contract pass all 17 checks
unchanged**. These are PASS-WITH-FIXES findings against the verifier, not against the delivered
contract content itself (which I independently confirmed is correct).

## Ranked findings

### F1 — BLOCKER (verifier): `matrix-fidelity` does not actually check the Markdown artifact's own per-label table against ground truth

**Claim:** The spec requires (Deliverables, Required document contract 2; Deterministic
verification, `matrix-fidelity`; Acceptance criterion 1) that `product-capability-safety-contract.md`
is an "exact content-mirror" of the JSON artifact and that `matrix-fidelity` "fails closed" on any
divergence, verifying "every per-label table row ... is present, unweakened." In the actual
`verify-p0b.ps1`, the `matrix-fidelity` check (lines ~301-375) parses the ruled table out of
`P0-B-DESIGN-GATE.md` and diffs it against the **JSON** artifact's `labels` object, cell by cell —
this part is genuinely rigorous and adversarial (I confirmed it correctly rejects a JSON cell
tampering attempt). But the check's only engagement with the **Markdown** artifact is four
`.Contains(...)` substring checks for the literal strings `degraded-escalates-on-strong-signal`,
`refused-and-escalated`, `dose-context-only`, and `prescribed-treatment-only` appearing *anywhere*
in the Markdown file — it never parses or diffs the Markdown's own per-label table against the
ruled table or against the JSON.

**Evidence (reproduced by this reviewer, in a throwaway `/tmp` copy, not the audited repo):**
Changing `docs/specs/schemas/product-capability-safety-contract.md` line 214 from
`| \`investigational-or-unapproved\` | A | D | D | ... |` to
`| \`investigational-or-unapproved\` | A | A | A | ... |` (silently weakening the Markdown
artifact's own stated behavior for Class C2/C3 from `degraded` to `allowed`, while leaving the
JSON artifact untouched) and re-running `verify-p0b.ps1 -BaseCommit fd4b00b8d4a43cf100e71e58f1cd9bc25c9c0f3f ...`
produced **`P0-B verification PASS (17 checks)`**, including `PASS: matrix-fidelity`. This is
exactly the "JSON/MD divergence" scenario the spec names `matrix-fidelity` to catch, and it did
not catch it.

**Why this matters:** `product-capability-safety-contract.md` is the document a future human
reviewer (P0-C's fixture builder, a runtime-integration parcel's builder) is explicitly told to
trust as the readable mirror precisely because parsing JSON is error-prone (Deliverables, Required
document contract 2: "so a reviewer can verify fidelity by reading prose and tables rather than
parsing JSON"). A verifier that cannot actually detect Markdown-table drift defeats that stated
purpose and is a materially weaker safety net than the spec requires and than the commit message
claims ("an adversarial matrix-fidelity diff ... programmatically compares every one of 30 cells +
calibration text + locked flags against the JSON artifact" — true only for the JSON side).

**Does this affect the currently-delivered artifact?** No. I independently confirmed the real
`product-capability-safety-contract.md` table matches the ruled table and the JSON exactly, cell
for cell. This finding is about the verifier's future-proofing, not about present drift.

**Smallest amendment:** Extend the `matrix-fidelity` check to parse
`product-capability-safety-contract.md`'s own "Per-label behavior matrix" table with the same
`$tableRowPattern`/`ConvertTo-ExpectedCell` logic already used for the design-gate table, and
assert cell-for-cell equality between the parsed Markdown table and the already-validated JSON
`labels` object (not just four fixed substrings).

**Changes a locked decision?** No — this is a verifier-robustness fix, not a change to any ruled
D-B1..D-B6 clause.

### F2 — MAJOR (verifier): `unbounded-operationalization`'s "smuggled behavior decision" detector is a small fixed phrase blacklist, trivially evaded by paraphrase

**Claim:** The spec's hard constraint and stop-condition require that an `applicabilityCriterion`
"test" confine itself to detection and never smuggle an allowed/refused/degraded/escalated
decision (Hard constraints, `[OPERATIONALIZED — bounded]` traceability; Deterministic
verification, `unbounded-operationalization`: "a criterion ... that introduces a product-behavior
decision beyond detection ... fails this check"). The actual implementation (lines ~289-298)
checks for exactly six literal phrases: `'is refused'`, `'is allowed'`, `'is degraded'`,
`'is escalated'`, `'must be refused'`, `'must be allowed'`.

**Evidence (reproduced in `/tmp`, not the audited repo):** Appending the clause
`", in which case output is blocked outright with no evidence shown"` to the
`gray-market-or-identity-uncertain` applicability test in the JSON artifact — a plain-English
behavior decision ("blocked outright") smuggled into a detection test — and re-running the
verifier produced `PASS: unbounded-operationalization` (and all 17 checks green) because the
inserted text contains none of the six blacklisted phrases.

**Why this matters:** this check is the implementation's only automated defense against exactly
the failure mode the spec calls out by name ("smuggling an allowed/refused rule into an
applicability test"). A six-phrase blacklist stops an obviously careless builder but not a
careless-but-fluent one, and does not meet the bar of a "deterministic, scriptable check" that
reliably catches the named failure class.

**Does this affect the currently-delivered artifact?** No. I read every one of the ten
`applicabilityCriterion.test` strings directly; none smuggles a behavior decision — each is
confined to detection, consistent with the corresponding text already present (verbatim, in most
cases) in `P0-B.md`'s own "Per-label applicability criteria" table.

**Smallest amendment:** Either (a) broaden the blacklist meaningfully (e.g., also flag `refus`,
`allow`, `degrad`, `escalat`, `block`, `suppress`, `must not` as word-stems, with an explicit
allow-list for the few legitimate uses already present, such as `calibrationRequired` text that
is a separate field), or (b) make this check structural rather than lexical — require
`applicabilityCriterion.test` to be validated against a template grammar ("True when ...") that
cannot express an output-state verb at all. Either is a bounded, same-review-tier fix; it does not
reopen D-I.

**Changes a locked decision?** No.

## Verification notes (what I independently checked and found clean)

1. **Byte-faithfulness of the frozen matrix (duty #1).** I read `P0-B-DESIGN-GATE.md` §3 directly
   and independently transcribed all 10 rows × 3 columns (30 cells) plus the "Calibration
   required" column, and diffed them against `product-capability-safety-contract.json`'s `labels`
   object. Zero discrepancies: every `A`/`D`/`R`/`E`/`D→E`/`R+E` cell maps to the correct
   `allowed`/`degraded`/`refused`/`escalated`/`degraded-escalates-on-strong-signal`/
   `refused-and-escalated` literal, every scope qualifier (`pregnancy-or-lactation` C1 =
   `dose-context-only`, `prescription-treatment-involved` C3 = `prescribed-treatment-only`) is
   present and correctly placed and nowhere else, every `calibrationRequired` string is a verbatim
   match, and `locked: true` is set for exactly `acute-red-flag-or-emergency` and
   `controlled-or-illegal-sourcing`. I separately ran the repository's own `verify-p0b.ps1` (via
   `pwsh`) against the real, unmodified repository with `BaseCommit = fd4b00b8d4a43cf100e71e58f1cd9bc25c9c0f3f`;
   all 17 named checks reported `PASS`.
2. **`enablementState.publiclyEnabled = false` is hard; a `biostack-recommended`/C3 output attempt
   is refused (duty #2, fixture run by me).** I read the `enablement-hard-fail-fixture` and
   `enablement-field-fidelity` check logic and ran it: with the real contract
   (`publiclyEnabled = false`, `enablementGatesC3 = true` on every one of the ten labels), a
   simulated C3-output attempt is forced to refuse, and I verified via the script's own built-in
   negative control (hypothetically flipping `publiclyEnabled` to `true` in a throwaway in-memory
   copy) that the refusal is actually caused by the flag, not a vacuous always-true check. I also
   grepped both artifacts for forbidden availability phrases myself; none present. I separately
   confirmed by direct reading that every one of the ten `"behavior"` objects carries
   `"enablementGatesC3": true` and that `publiclyEnabled` is the literal JSON boolean `false` (not
   a string) in the committed file.
3. **Numeric-provenance fail-closed (duty #3, fixture run by me).** `numericProvenance.rules[3]`
   reads, verbatim, "Missing provenance → the numeric output is refused (fail-closed), not
   downgraded," matching `P0-B-DESIGN-GATE.md` §3 "D-B3" rule 4 exactly. I grepped both artifacts
   for a weaker alternate framing (e.g., "missing provenance is degraded"); none found.
4. **D-J dose-context definition as operative scope semantics (duty #4).** `doseContextDefinition`
   in the JSON is a verbatim transcription of D-J's text (I diffed it directly against
   `COORDINATOR-DECISIONS-2026-10-07.md` lines 194-213), and it is the only definition the
   `pregnancy-or-lactation.C1.scope = "dose-context-only"` cell cites (`appliesToCell:
   "pregnancy-or-lactation.C1"`). The `qualified-cell-scope-present` check independently verifies
   exactly that one cell (and `prescription-treatment-involved.C3`) carry non-null scope and every
   other cell carries `null` — I re-ran this check's logic by hand against the full `labels` object
   and it is correct and non-vacuous (confirmed by adversarial test: nulling the scope on either
   qualified cell in a throwaway copy fails the check as expected).
5. **D-K is not contradicted anywhere (duty #4/#5).** `precedenceDirectionalConstraint` carries
   D-K's text verbatim and is explicitly marked `"carried as context — not a D-B1..D-B6 clause"`
   with `"effectOnThisContract": "none of this contract's ruled matrix ... is weakened ...by any
   precedence-rank argument"`. I found no sentence anywhere in either artifact that invokes
   precedence rank to weaken, soften, or make conditional any locked-label behavior or any ruled
   cell.
6. **17 verifier checks read line-by-line; adversarial construction attempted (duty #5).** I read
   `verify-p0b.ps1` in full (540 lines) and attempted to construct a nonconforming contract that
   passes, in `/tmp` only (never touching the audited repository or performing any git write
   there). Findings F1 and F2, above, are the two gaps I found and reproduced. All other 15 checks
   held up under targeted tampering attempts I tried (JSON cell tampering → `matrix-fidelity`
   correctly fails; nulling a required scope → `qualified-cell-scope-present` correctly fails;
   setting `publiclyEnabled: true` → `enablement-field-fidelity` correctly fails; adding a file
   outside allowed surfaces → `canonical-write-fencing-violation` correctly fails; removing a
   label → `vocabulary-closure` correctly fails).
7. **Per-label applicability criteria determinism (duty #6, two-implementer test on paper).** Every
   `applicabilityCriterion.test` string in the JSON is either a verbatim or near-verbatim copy of
   the corresponding row in `P0-B.md`'s own "Per-label applicability criteria" table (which is
   itself the bounded-operationalization source the implementation is required to transcribe).
   Two implementers reading these criteria against a function's own declared capability-contract
   fields would reach the same determination in every case I checked, including the two criteria
   that explicitly defer part of their matching logic to the function's own declaration
   (`interaction-or-contraindication-signal`'s match algorithm, `acute-red-flag-or-emergency`'s
   triggering criterion set) — these deferrals are themselves explicit and bounded, not open
   judgment calls for the criterion's own text.
8. **Zero scope creep into P0-C/P3-B (duty #7).** The only mentions of `P0-C`, `P3-B`, or `P0-D` in
   the delivered artifacts are informational ("a future reader (P0-C's fixture builder...)" in the
   "How to read this contract" section, and D-K's own text naming "P0-D's disposition process").
   None of these assert that P0-C, P3-B, or P0-D is authorized, dispatched, or that this parcel
   does any part of their work. `classification-axes.schema.json`'s `productGuidanceClass`
   object's `"controlBindingStatus": "deferred-to-P3-B"` is untouched (confirmed by direct diff
   and by the verifier's own `axis-binding-diff-scoped` byte-identity check on that object).
9. **No `TBD`, hashes verify (duty #8).** No `TBD`/`TODO`/`FIXME`/`{{` literal in any file this
   commit creates or modifies (grepped independently). Every `sha256AtBaseCommit` value embedded
   in `product-capability-safety-contract.json`'s `normativeInputs` object was independently
   recomputed by me against the live files and matches exactly, including
   `classification-axes.schema.json`'s pre-modification hash (verified against the file's content
   at the parent commit `fd4b00b`, not the post-modification working copy).
10. **Allowed-surfaces fencing.** `git diff fd4b00b 36fa450 --stat` shows exactly the six files
    named in "Exact allowed surfaces": the two new contract artifacts, the new verifier script, the
    scoped `classification-axes.schema.json` field update, and the one-section/one-row appends to
    `docs/specs/README.md`/`docs/specs/INDEX.md`. No frozen-surface file (charter, design-gate
    decision package, coordinator-decisions ledger, guidance-content-contract v1.0.0, P2 substrate
    beyond the named field, P3-A's `parcel-spec.schema.json`, `frontend/`/`backend/`/`contracts/`/
    `.github/`) was touched.

## Missing pieces / collisions / unknowns

- F1 and F2 are the only substantive gaps found. Neither currently manifests as an actual defect
  in the delivered contract content — both are latent weaknesses in the automated safety net that
  a future, less careful (or adversarial) amendment to this contract could exploit without the
  verifier noticing.
- This review did not independently verify the two independent reviewers' identity/freshness
  requirements, the Gate 3 human-approval-condition process, or the three-step dispatch
  precondition (P3-A closed; P0-A closed; dual review passed) — those are coordinator-level
  process facts outside this review's scope (verifying the *implementation artifact*, not the
  *dispatch record*), and I did not look for or rely on any other reviewer's output, per the
  independence rule.

## Recommendation

**PASS-WITH-FIXES.** The delivered Product Capability and Safety Contract is byte-faithful to the
owner-ruled D-B1..D-B6 matrix with zero drift on independent re-verification; enablement is hard;
numeric provenance is fail-closed; D-J's dose-context definition is correctly the operative scope
semantics; D-K is transcribed as context and contradicts nothing; the allowed-surfaces fence held
exactly; there is zero scope creep and zero placeholder content. The two findings above (F1, F2)
are verifier-robustness gaps, not contract-content defects, and should be fixed in the same
`verify-p0b.ps1` artifact before this parcel is treated as a durable, self-enforcing safety net for
future amendments — they do not require reopening D-I or any ruled clause.
