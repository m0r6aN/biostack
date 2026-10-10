# p3b_reverify_1 — P3-B remediation re-verification

**Verdict:** PASS-WITH-FIXES
**Subject:** `/home/cmorgan76/Repos/biostack-wt/p3b-remediation-1`, branch `fix/p3b-remediation-1`,
tip `e43aa40e0a7f951dea25b9262c64536bc8863fd4`, PR #534 (base `997b9b3`, parent dispatch `789a610`).
**Findings re-verified:** `p3b_impl_review_1` (R1-F2 homoglyph TBD, F1/F3 process items not in
remediation scope) and `p3b_impl_review_2` (R2-F1 safety-escalation binding, R2-F3 vacuous
function-review, R2-F4 two robustness gaps, R2-F5 rungApplied live-count).
**Date:** 2026-10-09 (session date; repo commit timestamps read 2026-10-09)

## Summary

All six targeted findings (R2-F1, R1-F2, R2-F3, R2-F4×2, R2-F5) are genuinely closed by this
remediation, confirmed by independently re-extracting the verifier's core validator function
(`Test-CapabilitySafetyOverlay` plus its helpers) into an isolated `/tmp`-only PowerShell harness
and running both the required positive fixture and a battery of hand-built adversarial inputs
against it — none bypassed the fixed logic. I also ran the real `verify-p3b.ps1` end-to-end against
the real worktree at the named `BaseCommit`: **PASS**, 13 checks recorded `pass:true` in
`verification-summary.json` plus Check 14 (clean tree) executing without throwing after the summary
write (confirmed by inspecting the script: Check 14 necessarily runs after evidence-bundle
generation and only emits `P3-B verification PASS`/`exit 0` if `Add-PassedCheck -Number 14`
succeeds) — so **14/14 checks and 12/12 fixtures** are confirmed clean. Frozen P0-B artifacts
(`product-capability-safety-contract.json`/`.md`, `SECTION-HEADING-MAP.md`, `EXTENSION-POINTS.md`,
`delivery-class-controls.json`, `fold-engine.md`, `routing-output.schema.json`,
`AXIS-REGRESSION-MAP.md`, `CHARTER.md`) and `parcels/P3-B.md` are independently re-hashed
bit-identical between `789a610`/`7b3225c` and `e43aa40`.

One residual, non-blocking documentation-fidelity gap remains (see Finding N1): the remediation
correctly fixed `verify-p3b.ps1`'s logic but left `CAPABILITY-FIELD-MAP.md` (the frozen,
spec-transcribed field map) with stale prose that no longer accurately describes how the fixed
verifier sources the `safety-escalation` binding's `behavior` literal. This mirrors the same
category of gap impl_review_1's F4 recorded (a spec-prose inaccuracy requiring a future spec
amendment, not a code change) and does not indicate any actual defect in the delivered, tested
behavior — hence PASS-WITH-FIXES rather than FAIL.

## (1) `guidanceClass: safety-escalation` binding — validates end-to-end; could not break it

Confirmed root cause and fix: the old code compared `behavior` against
`$Contract.cellSemantics.escalated`, which live-reads as the long prose sentence `"E — escalate
(stop ordinary output, surface escalation)"`, not the short token `escalated` any fixture could
plausibly supply — making the branch permanently unsatisfiable and, separately, the subsequent
`$escalatingBehaviors` membership test unreachable. The fix (`verify-p3b.ps1` ~line 514) now
compares `behavior` against the literal `'escalated'` via `[StringComparer]::Ordinal.Equals`, and
adds an explicit `if ($guidanceClass -eq 'safety-escalation') { $escalationTriggered = $true }`
belt-and-suspenders trigger (~line 580-585).

**Fixture run:** `fixtures/p3b/positive-safety-escalation-binding.json` (new) — ran the live
validator against its exact frontmatter in my isolated harness: `Result: valid`. Independently
re-derived its `escalationRule: 2` and `preemptionStage: 2` are consistent with the live contract
(`controlled-or-illegal-sourcing` is `preemptionOrder.stages[1]`).

**Break attempts (all correctly rejected, `/tmp`-isolated harness, never touching the repo):**
- `behavior: "allowed"` → `invalid` / `capability-claim-drift`
- `behavior` set to the long `cellSemantics.escalated` prose string (the *old*, broken comparand) →
  `invalid` / `capability-claim-drift` (confirms the old vacuous-acceptance path is closed, not
  just relocated)
- `escalation` block omitted entirely → `invalid` / `missing-required-frontmatter-key`
  (`Field: escalation`) — this is the PR's own claimed before/after delta; I reproduced it
  independently and it holds
- `escalationRule: 99` (out of the live `escalationSemantics.rules` 1-4 range) → `invalid` /
  `invalid-escalation-rule`
- `escalation.outputType: "ordinary"` → `invalid` / `invalid-escalation-output-type`
- `behavior: "ESCALATED"` (case-smuggled) → `invalid` / `capability-claim-drift` (confirms the new
  `-ccontains`/`Ordinal.Equals` case-sensitivity fix, R2-F4 part 1, applies here too)

No combination I constructed produced a false `valid`, and the one documented true-positive case
(correct claim + correct escalation block) validates. Closed.

## (2) Homoglyph TBD evasion — dead; reproduced reviewer's exact evasion and two novel variants, all FAIL

Confirmed the pinned `unicode-confusables-skeleton` step (step 9) is now present in
`Get-PlaceholderNormalizedText` (`verify-p3b.ps1` ~lines 230-253), with a 37-entry confusables
table ported byte-for-byte from `verify-p3a.ps1`'s own table (diffed both tables directly —
identical).

**Reviewer's exact evasion, reproduced:** `"Owner: T" + [char]0x0412 + "D pending decision"`
(Cyrillic В U+0412 in place of Latin B) → `Test-PlaceholderViolation` now returns **`True`**
(previously `False` per impl_review_1's F2). Matches the PR body's own before/after transcript.

**Novel variant 1 (not in either prior review):** full two-character Cyrillic substitution of
`TODO`'s both `O`s (`"T" + 0x041E + "D" + 0x041E`, i.e. `TОDО`, `T`/`D` left ASCII) → **`True`**.

**Novel variant 2:** `FIXME` with all four non-`F` letters replaced by Cyrillic/Greek confusables
(`F` + 0x0406 `І` + 0x0425 `Х` + 0x041C `М` + 0x0415 `Е`) → **`True`**.

**Novel variant 3:** fullwidth-bracket `｛｛name｝｝` (NFKC-foldable, tests step 8 interaction, not
step 9 specifically) → **`True`** (NFKC correctly folds fullwidth braces to ASCII `{{`/`}}` before
the confusables pass even runs).

**One honest negative-control gap, not exploitable:** a fully-Cyrillic spelling of `TODO` using
Cyrillic Де (`Д`, U+0414) for the `D` → **`False`** (not detected), because U+0414 is absent from
the ported table. This is consistent with the table's own disclosed scope ("intentionally bounded,
not a full UTS #39 database") and is not a meaningful evasion in practice: Cyrillic `Д` is a poor
visual match for Latin `D` in virtually every font (it resembles a Greek delta more than a Latin
D), unlike the classic phishing-grade confusable set (`А,В,Е,К,М,Н,О,Р,С,Т,Х,У` / lowercase
equivalents) the table does cover, which includes every letter actually needed to spell `TBD`,
`TODO`, and `FIXME` convincingly except `D`/`F`/`X`-as-`X`(covered)/`M`(covered). I do not treat
this as a live finding since (a) it was already disclosed scope in the ported, already-reviewed
P3-A table, and (b) my variant 1 above shows the actually-plausible `TODO` attack (substituting the
two `O`s, which *are* classic confusables) is caught. Recorded for completeness only.

Closed — the reviewer's exact disclosed evasion and realistic novel variants both fail as required.

## (3) Vacuous function-review path — constrained per the chosen option

Confirmed the chosen option matches R2-F3's "fail outright" disposition (explicitly stated in the
PR body as deliberately avoiding inventing a new justification-field rule, since
`CAPABILITY-FIELD-MAP.md` disclaims authority to define new function-review rules). New code
(`verify-p3b.ps1` ~lines 446-460): `function_review_status: 'not-applicable'` combined with
non-empty `guidance_classes`/`substance_function_risk` now returns `invalid` /
`not-applicable-with-capability-bearing-axes`.

**Independently re-derived in isolated harness:**
- Capability-bearing (`guidance_classes: [curated-evidence-guidance]`, populated
  `capability_claim`) + `function_review_status: not-applicable` → `invalid` /
  `not-applicable-with-capability-bearing-axes`. Matches new negative fixture
  `negative-function-review-status-not-applicable-capability-bearing.json` exactly (byte-compared
  the fixture's frontmatter against my harness input — same shape, same expected reason).
- Non-capability-bearing (empty axes, empty `capability_claim`) + `not-applicable` → `valid`
  (control case correctly still passes; the carve-out is scoped correctly to capability-bearing
  specs only, not a blanket ban on `not-applicable`).

Closed.

## (4) Two R2-F4 verifier robustness gaps — closed

**Gap A (non-numeric `preemptionStage`):** old code did a bare `[int]$claimedStage` cast, which
threw an unhandled `.NET` cast exception (non-script-terminating label, just a raw stack trace) on
a non-numeric value. New `ConvertTo-StrictInt` helper (lines ~388-404) returns `$null` on anything
not a clean integer form; every call site now checks `$null -eq ...` and returns a labeled
`invalid` result instead. **Reproduced:** `escalation.preemptionStage: "one"` on an otherwise
well-formed `safety-escalation` claim → `invalid` / `invalid-escalation-preemption-stage` (no
exception, no stack trace, clean fail-closed result).

**Gap B (`[bool]`-cast truthy-string coercion):** old code did `[bool](Get-PropValue ...)` on
`publiclyEnabled`, and PowerShell casts any non-empty string (including the string `"false"`) to
`$true`. New `ConvertTo-StrictBool` helper returns `$true`/`$false` only for the literal strings
`'true'`/`'false'` (or native booleans), `$null` otherwise, with call sites failing closed on
`$null`. **Reproduced:** `capability_claim[0].publiclyEnabled: "false"` (a JSON string, not a
native boolean) now correctly evaluates as boolean `false` (`Result: valid`, i.e. not
false-positive-flagged as a premature-enablement claim) rather than being silently coerced to
`$true`.

**Bonus, disclosed in the PR body and independently reproduced by me (not originally itemized as a
separate item but part of "R2-F4"):** case-insensitive closed-vocabulary matching
(`-notcontains`/`-contains` default to case-insensitive in PowerShell) was also fixed via
`-cnotcontains`/`-ccontains` + `Ordinal.Equals` throughout. Reproduced `function_review_status:
"Reviewed"` (cased) → now correctly `invalid` / `invalid-function-review-status` (previously would
have passed).

Closed.

## (5) `rungApplied` vocabulary live-counted against `missingInputLadder.rungs`

Confirmed new Check 9 (`verify-p3b.ps1` ~lines 811-841): live-reads
`$ContractDoc.missingInputLadder.rungs.Count` (independently re-confirmed via direct Python read of
the contract: **3** rungs), regex-extracts the distinct `rung-<N>-` prefixes from the static
`$RungApplicationLabels` four-literal set (`rung-1-*`, two `rung-2-*`, `rung-3-*` → 3 distinct
numbers), and asserts equality, failing loudly on any future drift. This check passed in the live
run (`verification-summary.json` check 9: `pass: true`, name confirms "live-counted against frozen
missingInputLadder.rungs"). Previously this four-vs-three correspondence was prose-only. Closed.

## (6) Frozen P0-B artifacts + `parcels/P3-B.md` bit-identity

Independently re-hashed (SHA-256) nine named frozen paths at `789a610` vs. tip `e43aa40`:
`product-capability-safety-contract.json`, `.md`, `SECTION-HEADING-MAP.md`, `EXTENSION-POINTS.md`,
`delivery-class-controls.json`, `fold-engine.md`, `routing-output.schema.json`,
`AXIS-REGRESSION-MAP.md`, `CHARTER.md` — **all nine MATCH exactly.** Separately re-hashed
`parcels/P3-B.md` at `7b3225c` vs. `e43aa40`: **identical** (`b4535c95...685dfc`, zero-line diff).
`git diff --name-only 789a610 e43aa40` shows exactly 18 changed paths (the 16 originally pinned +
the disclosed `parcels/P3-B.md` carry-in + the 2 new remediation fixtures); `verify-p3b.ps1` itself
was already one of the 16 pinned paths so is not separately counted — consistent with the PR
header comment's "19 allowed surfaces" accounting (16 + 1 + 2, where `verify-p3b.ps1` is inside the
original 16). Closed.

## (7) Clean run: 14/14 checks, 12/12 fixtures

Ran `verify-p3b.ps1` live against the real worktree: `pwsh -NoProfile -ExecutionPolicy Bypass -File
docs/specs/scripts/verify-p3b.ps1 -BaseCommit 789a6106381fffe1dde99f94c702ccb7b3761a15 -BuilderId
p3b_remediation_builder -ReviewerIds p3b_impl_review_1,p3b_impl_review_2 -EvidenceDirectory
artifacts/p3b-verification` → **`P3-B verification PASS`**, exit 0.
`verification-summary.json` records checks 1-13 all `pass: true` (check numbering: 1 HEAD-descent,
2 base-diff, 3 changed-file set, 4 frozen-surface byte-identity, 5 extensionSections, 6
extension-point-not-additive, 7 CAPABILITY-FIELD-MAP table, 8 axis-binding scope, 9 rungApplied
live-count [new], 10 twelve-fixture reproduction, 11 INDEX/README amendments, 12 placeholder scan,
13 evidence-bundle generation). Check 14 (untracked evidence dir / clean tree) necessarily runs
*after* `verification-summary.json` is written (by script construction — confirmed by reading
`verify-p3b.ps1`'s tail) and only reaches `Write-Output 'P3-B verification PASS'; exit 0` if its own
`Add-PassedCheck -Number 14` call does not throw; since the script did print `PASS` and exit 0,
Check 14 is confirmed passed even though (by design, since it checks tree state *after* evidence
generation) it is not itself serialized into the summary JSON. `fixture-results.json` lists all 12
pinned fixture names with no error; the fixture loop (`Assert-True` per-fixture inside the check)
would have thrown and aborted the whole run on any mismatch, and it did not. Confirmed 14/14 + 12/12.
Evidence directory deleted afterward (untracked, not left behind), worktree left clean
(`git status --porcelain=v1 --untracked-files=all` empty after cleanup).

## Ranked findings

### N1 — MINOR (documentation fidelity, not a functional defect) — `CAPABILITY-FIELD-MAP.md`'s `safety-escalation` citation text is now stale relative to the fixed verifier

**Claim:** `CAPABILITY-FIELD-MAP.md`'s `capability_claim` row still reads: "for `guidanceClass:
safety-escalation`, `behavior` must equal the literal `escalated` (**the literal defined once,
live, at `cellSemantics.escalated`**)." This was the exact wrong premise R2-F1 diagnosed as the
root cause of the broken binding (`cellSemantics.escalated`'s live value is the long prose sentence
`"E — escalate (stop ordinary output, surface escalation)"`, never the bare token `escalated`). The
remediation correctly fixed the verifier to compare against the pinned literal `'escalated'`
directly (not read live from that path) — but the field-map's own "Live cross-reference rule" text
was not updated and still claims this value is "defined... live" at that contract path, which is
no longer true of the implementation. `git diff 997b9b3 e43aa40 -- docs/specs/schemas/CAPABILITY-FIELD-MAP.md`
is empty — this file was not touched by the remediation.

**Mitigating facts:** `CAPABILITY-FIELD-MAP.md` is a byte-for-byte transcription of the approved,
merged spec's (`parcels/P3-B.md`) own document contract 2 text (confirmed in a prior review,
`p3b_re_review_1.md` verification note (c), and independently spot-checked here); changing it
without a spec amendment would itself be an unauthorized edit to frozen/merged canon, exactly the
kind of overreach this initiative's prior reviews have flagged elsewhere. The PR body explicitly
addresses the analogous tension for R2-F3 (choosing not to invent a field-map-level rule the
document "disclaims authority to define") but does not explicitly flag this specific stale citation
for R2-F1. This is the same category of gap as impl_review_1's F4 (a spec-prose inaccuracy that
needs a future spec amendment, not a code fix) and does not indicate any actual defect in the
delivered, independently-tested verifier behavior — the fixed comparison is correct and
non-bypassable (see item 1 above).

**Smallest amendment:** in a future P3-B spec amendment, correct the `capability_claim` row's
`safety-escalation` citation to read "...`behavior` must equal the literal `escalated` (a closed,
pinned 4th-column literal — not itself stored verbatim in `cellSemantics`, whose `escalated` key
holds the long descriptive sentence, not the short token)" and update the "Frozen source" column
accordingly. No implementation file needs to change.

**Does it change a locked decision?** No.

## Missing pieces / collisions / unknowns

- PR #534's body discloses that `p3b_impl_review_1.md`/`p3b_impl_review_2.md` were not found
  anywhere in the remediation branch's own history at build time and that this remediation proceeded
  directly from dispatch-stated findings plus the builder's own independent adversarial reading for
  R2-F4. Both review files do exist in `main`'s `.audit/reviews/` (read directly for this
  re-verification) — this appears to be a worktree/branch-provenance discrepancy at build time, not
  a finding against the remediation's content; flagging for coordinator awareness only, as the PR
  itself already does.
- No `GATE2-P3B-REMEDIATION-1.md` dispatch record exists in this worktree (unlike P3-A's analogous
  `GATE2-P3A-REMEDIATION-1.md`); the remediation PR's own "Authorization" section cites the
  charter's standing authorization instead. I take no position on whether this is sufficient — it
  is outside this verification's scope, which is the technical closure of the six named findings.
- I did not find any case where the remediation's `-ccontains`/`Ordinal.Equals` case-sensitivity
  hardening or the strict-cast helpers introduced a new false-fail on any of the 12 pinned fixtures
  (all 12 reproduce their pinned `expected.result`/`expected.reason` against the live, fixed
  validator).
- I found no edit to any frozen P0-B/P1/P2/P3-A artifact, and no edit to `parcels/P3-B.md`, in this
  remediation's diff.

## Verification notes

What I checked and found clean (independently, read-only, against the actual repository and an
isolated `/tmp`-only harness that never wrote to the repository):

1. Diffed `verify-p3b.ps1` line-by-line between `997b9b3` (pre-remediation tip) and `e43aa40`
   (this PR's tip) — every hunk maps 1:1 to one of the six named findings; no unrelated or
   half-applied edit found.
2. Extracted `Test-CapabilitySafetyOverlay` and its full helper chain (`Get-PropValue`,
   `Get-PropArray`, `Test-PropPresent`, `ConvertTo-StrictInt`, `ConvertTo-StrictBool`,
   `Get-PlaceholderNormalizedText`, `Test-PlaceholderViolation`) verbatim into an isolated,
   standalone `/tmp` script (dummy `param()` block, live contract read from the real file path,
   read-only) and exercised it directly against hand-built adversarial frontmatter objects — see
   items (1)-(4) above for the full list of adversarial probes and results.
3. Ran the real `verify-p3b.ps1` against the real worktree, with the exact `BaseCommit` the dispatch
   record names: `P3-B verification PASS`, 13/13 JSON-recorded checks `pass: true`, Check 14
   confirmed passed by construction (script only reaches its final `PASS`/`exit 0` after Check 14's
   `Add-PassedCheck` call succeeds without throwing).
4. Re-hashed all nine named frozen P0-B/P1/P2/P3-A artifacts and `parcels/P3-B.md` independently
   (SHA-256) between `789a610`/`7b3225c` and `e43aa40` — all match exactly.
5. Confirmed the confusables table in `verify-p3b.ps1` is byte-identical to `verify-p3a.ps1`'s own
   (direct text diff of both 37-entry hashtable literals).
6. Confirmed the live contract's `missingInputLadder.rungs` has exactly 3 entries (direct Python
   JSON read) matching the new Check 9's live-count assertion.
7. Left the worktree exactly as found: deleted the evidence directory I generated
   (`artifacts/p3b-verification/`, untracked by design) and all `/tmp` harness/diff artifacts;
   `git status --porcelain=v1 --untracked-files=all` is empty. No git write command was run
   (no add/commit/checkout/merge/rebase/stash/reset) — only read commands (`git show`, `git diff`,
   `git log`, `git status`) and the verifier itself, which only writes to the untracked
   `EvidenceDirectory`.
