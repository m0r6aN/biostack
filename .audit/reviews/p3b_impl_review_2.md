# p3b_impl_review_2 — P3-B implementation review

**Verdict:** PASS-WITH-FIXES
**Commit reviewed:** `dd92fa577fb1bee0b40053cb34a6ea81c780b11d` (HEAD of `feat/p3b-schema-binding`,
PR #533), worktree `/home/cmorgan76/Repos/biostack-wt/p3b-impl`. Parent/dispatch commit
`789a6106381fffe1dde99f94c702ccb7b3761a15` ("Gate 2 — P3-B implementation dispatch").
**Spec (sole build authority):** `docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md`,
SHA-256 `B4535C95215EB57C0F18DD27865A2A0BE0839746BC382A5F0943C0311A685DFC` (as present at HEAD;
byte-identical to the pre-reviewed upstream commit `7b3225c`, independently re-hashed).
**Dispatch record:** `docs/INITIATIVES/biostack-governed-delivery/dispatch/GATE2-P3B-IMPLEMENTATION.md`
**Date:** 2026-10-09

## Summary

I independently ran `verify-p3b.ps1` (pwsh 7.6.6, no network) against the real worktree with
`-BaseCommit 789a6106381fffe1dde99f94c702ccb7b3761a15 -BuilderId p3b_builder -ReviewerIds
p3b_impl_review_1,p3b_impl_review_2 -EvidenceDirectory artifacts/p3b-verification`. It printed
`P3-B verification PASS` and exited `0`; all 13 checks recorded `pass: true`. I then
independently re-derived every claimed hash (charter, P3-A spec/closure, P0-B spec/closure, both
`product-capability-safety-contract.{json,md}` files) against the spec's pinned values — all
matched exactly, no stale hash. I confirmed the changed-file set is scoped to the 17 paths
(16 spec-pinned + the disclosed `parcels/P3-B.md` carry-in), that `frontend/`, `backend/`,
`contracts/`, `.github/`, templates, and every other frozen path are byte-identical, and that
every fixture's claimed live cross-reference (behavior values, `preemptionOrder` stage placement,
`numericProvenance.lockedOrigins`, `enablementState.biostackRecommendedOrigination.publiclyEnabled`)
matches the real contract file's live content exactly — no drift, no invented product decision.
I extracted the verifier's core validator function into an isolated `/tmp`-only harness and
exercised it against hand-crafted adversarial frontmatter objects to probe for vacuous bindings,
ID mis-reference/semantic drift, extension-point mutation, and crash/robustness failures. This
surfaced one genuine functional defect (F1) and confirmed the extension-point and preemption-stage
anti-drift checks hold under adversarial pressure. No product allowed-output decision is smuggled
into the binding; one process/scope-authority concern (F2) and several lower-severity robustness
gaps are recorded below.

## Ranked findings

### F1 — MAJOR — the `guidanceClass: safety-escalation` binding is broken and completely untested; this guidanceClass can never validate as intended

**Claim:** `CAPABILITY-FIELD-MAP.md`'s `capability_claim` row states: *"for `guidanceClass:
safety-escalation`, `behavior` must equal the literal `escalated` (the literal defined once, live,
at `cellSemantics.escalated`)"*. `verify-p3b.ps1` implements this literally: `if ($behavior -ne
$Contract.cellSemantics.escalated) { ... capability-claim-drift }`. But the live value of
`product-capability-safety-contract.json`'s `cellSemantics.escalated` is **not** the bare literal
`"escalated"` — it is the prose string `"E — escalate (stop ordinary output, surface escalation)"`.
I independently read the live contract (`cellSemantics` object, four keys `allowed`/`degraded`/
`refused`/`escalated`, each holding a descriptive prose sentence, not a short code) and confirmed
no label's `behavior.*.value` anywhere in the contract is ever the bare word `"escalated"` either
(the only "escalat"-containing behavior values in the whole contract are the compound
`degraded-escalates-on-strong-signal` and `refused-and-escalated`, both handled by the *other*
branch of the validator for the `C1`/`C2`/`C3` guidance classes, not the `safety-escalation`
branch). Consequently, no spec author could ever write a `behavior` value that both (a) matches
this parenthetical's promised bare-literal `escalated` and (b) equals the live
`cellSemantics.escalated` string — the comparison as coded will reject every real
`guidanceClass: safety-escalation` submission as `capability-claim-drift`, permanently, making the
fourth closed `productGuidanceClass` label (`safety-escalation`) non-functional under this overlay.

**Compounding evidence this is a real, unexercised gap, not reviewer misreading:** none of the
required ten fixtures (document contract 4) uses `guidanceClass: safety-escalation` — every
positive and negative fixture uses `curated-evidence-guidance`, `personalized-protocol-
recommendation`, or `deterministic-calculation`. AC-P3B-02 ("field map complete") and AC-P3B-05
("live agreement, no drift... in every positive fixture") both certify this row as fully
implemented and drift-free, but the one branch of the six-field binding that would prove it is
never run by any required fixture, so this defect produced zero red checks anywhere in the
evidence bundle.

**Evidence:** `docs/specs/schemas/CAPABILITY-FIELD-MAP.md` `capability_claim` row, "Live
cross-reference rule" column; `docs/specs/scripts/verify-p3b.ps1`, the `if ($guidanceClass -eq
'safety-escalation')` branch (~line 404 of `Test-CapabilitySafetyOverlay`); live read of
`docs/specs/schemas/product-capability-safety-contract.json`'s `cellSemantics` object
(`{"allowed":"A — allowed","degraded":"D — degraded (...)","refused":"R — refused","escalated":"E
— escalate (stop ordinary output, surface escalation)"}`) and a full scan of `labels.*.behavior.*
.value` confirming no cell is ever the bare word `escalated`.

**Smallest amendment:** either (a) correct the field map's live cross-reference rule to assert
`behavior` is the literal string `escalated` as a closed vocabulary member (a static, pinned
4th-column literal analogous to how `escalationRule`'s integer range is already pinned), dropping
the mistaken claim that this literal is "defined live" at `cellSemantics.escalated` (since the
contract never stores it there as a bare code), and add one fixture exercising
`guidanceClass: safety-escalation` so the branch is actually proven; or (b) if P0-B's contract
intends `cellSemantics.escalated`'s prose value itself to be matched, document that explicitly and
add the corresponding fixture. Either path requires a fixture addition — the branch is currently
dead code.

**Does it change a locked decision?** No. This is a binding-mechanics bug in how P3-B's own field
map/verifier cross-reference an existing frozen P0-B cell; it neither invents nor alters any P0-B
behavior, and it is in the fail-closed (over-restrictive) direction, not the smuggling direction —
but it is a genuine, demonstrable, fully-unexercised defect in required document contracts 2 and 5.

### F2 — MAJOR — the delivered changed-file set is 17 paths, not the spec's pinned "exactly 16 allowed surfaces" (AC-P3B-08), and the builder resolved this unilaterally rather than stopping per the spec's own Stop Conditions

**Claim:** The spec's "Exact allowed surfaces" section enumerates exactly 16 paths and AC-P3B-08
states: *"the changed-file set equals exactly the 16 allowed surfaces."* The actual `BaseCommit`
used (`789a610`, the Gate 2 dispatch commit) does not contain
`docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md` at all, despite the Gate 2 dispatch
record's explicit claim that "the spec is merged canon via PR #532." I independently confirmed:
`git show 789a610:docs/.../parcels/P3-B.md` fails (file absent at `BaseCommit`); the actual HEAD
diff touches 17 paths, not 16; and the builder's own verifier (`verify-p3b.ps1` header comment,
check 3's label, and `verification-summary.json`'s `disclosedDeviation` field) openly documents
this as "a 17th changed path... flagged for coordinator reconciliation." The builder brought the
file in byte-identical from its already-dual-reviewed upstream commit `7b3225c` — I independently
re-hashed it and confirmed exact byte match — so no content was fabricated or altered. However,
the spec's own Stop Conditions explicitly require: *"Stop and return to the coordinator if: Any
required target path is absent or materially different from the contract assumed here."* A
`BaseCommit` that is missing the very spec file the dispatch record swears is merged canon is
exactly such a case. Instead of stopping, the builder silently redefined check 3's own pass
criterion from "16" to "17" allowed surfaces and proceeded to a green build. This is the identical
failure mode P3-A's own review flagged (a check's label over-claiming or quietly renegotiating its
own acceptance boundary) — here the renegotiation is transparent and well-evidenced, which
substantially mitigates it, but it does not change the fact that AC-P3B-08 as literally written in
the approved spec is not satisfied by this HEAD, and the authorization boundary (built strictly to
the named spec's 16-surface contract, no more) was crossed without coordinator sign-off before the
build proceeded.

**Evidence:** `docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md`, "Exact allowed
surfaces" (16 numbered items) and AC-P3B-08; `GATE2-P3B-IMPLEMENTATION.md`'s
`"baseCommit": "single anchor = the commit that adds THIS file to main"` claim vs. the actual
`git show 789a610:...parcels/P3-B.md` failure I reproduced directly; `verify-p3b.ps1` lines ~24-42
(header comment) and check 3's renamed label ("...equals the 17 allowed surfaces (16 pinned + 1
disclosed prerequisite merge-in...)"); `verification-summary.json`'s `disclosedDeviation` field.

**Smallest amendment:** coordinator reconciles PR #532 (merge or rebase) before this PR's Gate 3
merge, so the actual `BaseCommit` genuinely contains the spec file and the diff set returns to
exactly 16 paths as the spec requires; alternatively, the coordinator issues an explicit amendment
to AC-P3B-08 accepting the 17-path carry-in before closure, rather than leaving the discrepancy
resolved only inside the builder's own verifier.

**Does it change a locked decision?** No — the carried-in file's content is unchanged and already
dual-reviewed; this is a process/sequencing defect in how Gate 2 was dispatched, not a semantic or
product decision change. It should still block an unconditional PASS until the coordinator
closes the loop it was explicitly flagged for.

### F3 — MINOR — a capability-bearing spec can declare `function_review_status: not-applicable`, producing a structurally valid but vacuous function-review binding

**Claim:** `function_review_status`'s "Required when" is "unconditional," independent of whether
`guidance_classes`/`substance_function_risk` are non-empty, and its value shape permits any of the
four closed literals regardless of axis content. I built an adversarial frontmatter object
(`guidance_classes: [curated-evidence-guidance]`, a fully correct `capability_claim`/`missingness`
pair, `function_review_status: not-applicable`) and ran it through the extracted validator
function directly: it returns `Result = valid`. Nothing in `CAPABILITY-FIELD-MAP.md`'s
`function_review_status` row or the verifier cross-links this field to `capability_claim`'s
non-emptiness, so a genuinely capability-bearing (guidance-class-declaring) function can carry a
"not applicable" review status — a textbook vacuous binding (field present, closed-vocabulary
member, but asserting no real review commitment for a function that, by declaring a
`productGuidanceClass`, is exactly the kind of function D-B5's rules describe reviewing).

**Evidence:** `CAPABILITY-FIELD-MAP.md`, `function_review_status` row ("Required when:
unconditional... one of the closed values... no other literal... is accepted" — no axis-linkage
clause); `verify-p3b.ps1`'s `function_review_status` branch (no check cross-references
`guidance_classes`/`substance_function_risk`); reproduced via isolated `/tmp`-only harness
(destroyed after use, no repository file touched).

**Smallest amendment:** since this gap originates in the spec/field-map text itself (verbatim
transcription, not a builder deviation), the smallest fix is a field-map amendment (not a P3-B
implementation change): add a cross-link row or footnote requiring `function_review_status` to be
a member of `{unreviewed, review-required, reviewed}` (excluding `not-applicable`) whenever
`capability_claim` is non-empty, with a corresponding verifier check and negative fixture. This is
flagged for the coordinator/next-parcel disposition, not a defect the current builder introduced.

**Does it change a locked decision?** No — it is a field-map/spec gap, faithfully implemented; it
does not touch P0-B's frozen contract.

### F4 — MINOR — two verifier robustness gaps found via adversarial `/tmp` input (not reachable by the delivered fixtures)

- A non-numeric `escalation.preemptionStage` (e.g. the string `"one"`) causes an unhandled .NET
  cast exception (`Cannot convert value "one" to type "System.Int32"`) rather than a labeled
  `invalid`/`incorrect-preemption-stage` result. The script still exits non-zero under
  `$ErrorActionPreference = 'Stop'` (fail-closed, not an exploitable bypass), but the failure mode
  is an unlabeled stack trace rather than a deterministic reason code, which weakens auditability
  if this path is ever reached by a real future fixture.
- `[bool]`-casting non-boolean JSON values for `publiclyEnabled` (e.g. a JSON string `"false"` or
  `"0"`) coerces to PowerShell `$true` (PowerShell treats any non-empty string as truthy),
  producing false-positive `premature-public-enablement-claim` rejections for honestly-`false`
  claims expressed as non-boolean JSON. Not exploitable toward passing an actual premature-
  enablement claim (the only reachable direction is over-strict, not permissive); not triggered by
  any delivered fixture (all use real JSON booleans).

Both reproduced only in an isolated `/tmp` harness (function extraction + synthetic frontmatter
objects), never executed against or written into the repository/worktree.

**Smallest amendment:** wrap the `[int]`/`[bool]` casts in a type-check that returns a labeled
`invalid` result on a non-conforming shape, rather than letting a .NET exception propagate.

**Does it change a locked decision?** No.

### F5 — MINOR/Informational — `rungApplied`'s four-literal vocabulary is P3-B-invented shorthand, never live-counted against the contract's three rungs

`CAPABILITY-FIELD-MAP.md`'s `missingness` row maps `rungApplied` values to four closed literals
(`rung-1-refuse-invalid`, `rung-2-degrade-naming-missingness`, `rung-2-refuse-safety-material`,
`rung-3-marker`) that do not appear verbatim anywhere in `product-capability-safety-contract.json`
(whose `missingInputLadder.rungs` is three prose paragraphs). The verifier hardcodes this
four-literal set as `$RungApplicationLabels` and never asserts that the live contract still has
exactly three rungs or that rung 2's text still distinguishes a safety-material sub-case — i.e.
there is no live tripwire if a future P0-B revision merges/splits these rungs. This is narrower
than "hardcoding a copy of a contract value" (the contract never used these literal strings to
begin with), but it is the same category of risk the Hard Constraints' "Composed, not hardcoded"
rule is meant to catch. Not a defect requiring action in this parcel; flagged for awareness only.

## Missing pieces / collisions / unknowns

- F1 and F2 should both be resolved (or explicitly re-dispositioned by the coordinator) before
  this PR is treated as closeable; neither is a BLOCKER on its own (no product allowed-output is
  smuggled, no frozen artifact is altered, no `C3`/public-enablement overclaim exists anywhere in
  the delivered fixtures), but together they mean two of this parcel's own acceptance criteria
  (AC-P3B-02/04/05 for the `safety-escalation` branch, and AC-P3B-08 literally) are not actually
  satisfied by the evidence bundle as it stands, despite `verification-summary.json` reporting
  `pass: true` for every numbered check.
- I did not find any fixture, schema diff, or verifier branch that weakens or strengthens P0-B's
  frozen contract, alters its cells, or implies any `C3`/`personalized-protocol-recommendation`
  capability is publicly available — every `publiclyEnabled` fixture value and check correctly
  reads `false` live and the one negative fixture testing an over-claim (`true`) correctly fails.
- I did not find any extensionSections-key mutation path; the "exactly one key" assertion (check
  5/6) would reject any pre-existing-plus-new-key combination, verified via an isolated harness.
- I did not find any ID mis-reference (wrong label cited against a correct cell, or vice versa)
  that the validator fails to catch; cross-label and wrong-preemption-stage adversarial inputs
  were both correctly rejected.

## Verification notes

- Ran `verify-p3b.ps1` live against the real worktree at the real `BaseCommit`/`HEAD` pair: PASS,
  all 13 checks green, evidence bundle written and inspected, then deleted (untracked, not part of
  my review-output commit).
- Re-derived every hash the spec cites (charter, P3-A spec/closure, P0-B spec/closure, both
  contract-file mirrors) independently via `sha256sum`: all match the spec's pinned values exactly.
- Confirmed the 17-file changed set contains zero touches to `frontend/`, `backend/`,
  `contracts/`, `.github/`, `docs/specs/templates/`, `docs/specs/active|done/`, or any other
  frozen path named in the spec.
- Confirmed every fixture's live cross-reference claim (behavior values for `curated-evidence-
  guidance`/`personalized-protocol-recommendation`/`deterministic-calculation`, `preemptionOrder`
  stage placement including the stage-4 residual case, `numericProvenance.lockedOrigins`
  membership, `enablementState.biostackRecommendedOrigination.publiclyEnabled`) against a direct,
  independent Python read of the live contract file: all matched exactly.
- Built and ran an isolated `/tmp`-only adversarial harness (function-extraction + synthetic
  frontmatter objects, never touching the repository) to probe vacuous bindings, semantic drift,
  extension-point mutation, and crash robustness; all artifacts and the harness directory were
  deleted after use (`rm -rf /tmp/p3b_adv`).
- Left the worktree clean: reverted my own `artifacts/p3b-verification/` evidence-run output
  (untracked by design) and made no other change to any repository file.
