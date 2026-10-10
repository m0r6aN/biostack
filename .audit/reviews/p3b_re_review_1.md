# p3b_re_review_1 — P3-B spec review

**Verdict:** APPROVE
**Spec file:** /home/cmorgan76/Repos/biostack-wt/p3b-shaping/docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md
**Spec SHA-256:** B4535C95215EB57C0F18DD27865A2A0BE0839746BC382A5F0943C0311A685DFC
**Date:** 2026-10-09

## Ranked findings

No BLOCKER or MAJOR findings. No new defects found in the delta between the reviewed spec
(`0eeb94e`, SHA `7C1B3350...C62266`, reviewed by `p3b_spec_review_1`/`_2`) and the current tip
(`7b3225c`, SHA `B4535C95...685DFC`, PR #532). All six checklist items (R1-F1, R1-F2, R2-F1,
R2-F2, R2-F3, R2-F4) are genuinely closed. See "Verification notes" for the evidence behind each.

### MINOR-N1 — residual-stage-4 rule is an interpretive extension, correctly flagged and cleanly resolved, not a remaining defect

**Claim (informational, not a defect):** The frozen contract's `preemptionOrder.stages[3].labels`
is the literal empty array `[]`, not a populated list of "calibrating labels." The spec's
resolution — "any escalating label absent from stages 1-3's explicit lists resolves to stage 4" —
is an inference from the ruled prose ("calibrating labels union their obligations") rather than a
literal enumeration in the JSON. I independently re-derived this by elimination: of the ten closed
`substanceFunctionRisk` labels, five are named explicitly across stages 1-3
(`acute-red-flag-or-emergency`, `controlled-or-illegal-sourcing`, `minor-or-age-uncertain`,
`pregnancy-or-lactation`, `prescription-treatment-involved`); the remaining five
(`ordinary`, `investigational-or-unapproved`, `gray-market-or-identity-uncertain`,
`injection-or-sterile-preparation`, `interaction-or-contraindication-signal`) are necessarily the
"calibrating labels" the stage-4 role text names, since D14's fieldwise fold over this axis is
exhaustive and the four-stage encoding is presented as a complete ordering, not a partial one. This
is the same elimination-by-exhaustion the spec itself performs, and it is deterministic (two
honest builders re-deriving stage-4 membership from the same ten-label closed vocabulary and the
same three explicit stage lists will agree). This is not a product-semantic invention — it assigns
no new allowed/degraded/refused/escalated behavior, only a `preemptionStage` integer, which is
explicitly a schema-mechanics field (document contract 2's own "Bound category: escalation," not a
behavior cell). Recorded as MINOR-informational only because a stricter contract-literalist reading
could object that "the contract itself doesn't say so" — the spec's own fixture
(`positive-escalation-stage4-interaction-signal.json`) and explicit line-by-line justification in
the `escalation` row's "Live cross-reference rule" column pre-empt that objection by naming the
reasoning and the sourcing (`preemptionOrder.stages[3]`'s role text) explicitly, which is exactly
what P3-B's own Stop Conditions require when a fixture's expected result needs to be derived rather
than read off verbatim.
**Does it change a locked decision?** No.

## Missing pieces / collisions / unknowns

None beyond MINOR-N1 above. No scope creep, no frozen-surface touch beyond `parcels/P3-B.md`
itself (confirmed by diffing `0eeb94e..7b3225c`: only `parcels/P3-B.md` changed, 42
insertions/22 deletions, `git status --porcelain` clean in the worktree).

## Verification notes

**(a) Check 5's `extensionSections` field count — now factually correct.** Independently counted
the top-level keys in document contract 1's pinned `product-capability-safety-overlay` JSON block:
`addedBy`, `appliesToShapes`, `bindsProductSemantics`, `contractSource`, `fieldMapSource`,
`boundFrontmatterKeys` — **exactly six**. Check 5 (Deterministic verification) now reads "the six
fields pinned in document contract 1," AC-P3B-01/02 are consistent with six, and the delta diff
confirms both occurrences of "nine" were changed to "six." Closed — R2-F1 (BLOCKER) genuinely
fixed, no off-by-N remains.

**(b) `interaction-or-contraindication-signal` D→E `preemptionStage` cross-reference — now
DETERMINATE and fixture-backed.** Read `product-capability-safety-contract.json` live:
`preemptionOrder.stages` = stage 1 `[acute-red-flag-or-emergency]`, stage 2
`[controlled-or-illegal-sourcing]`, stage 3 `[minor-or-age-uncertain, pregnancy-or-lactation,
prescription-treatment-involved]`, stage 4 `labels: []`, role `"calibrating labels union their
obligations"`. `interaction-or-contraindication-signal` is absent from stages 1-3. The reworked
`escalation` row's "Live cross-reference rule" column now states explicitly: "for any escalating
label absent from `preemptionOrder.stages[0].labels` through `stages[2].labels`... `preemptionStage`
is determinately `4`... this is the deterministic resolution for `interaction-or-contraindication-
signal`'s own D→E escalating case... exercised by fixture
`positive-escalation-stage4-interaction-signal.json`." Independently re-derived: confirmed
`labels.interaction-or-contraindication-signal.behavior.C3.value` is
`"degraded-escalates-on-strong-signal"` live in the contract (matches the fixture's claimed
`behavior`), and confirmed via elimination over the ten closed `substanceFunctionRisk` labels that
this label is one of exactly five not named in stages 1-3, consistent with the stage-4 "calibrating
labels" bucket. The required fixture `positive-escalation-stage4-interaction-signal.json` is
present in document contract 4, the frontmatter `surfaces` list, and "Exact allowed surfaces" item
13; the fixture's `input`/`expected` content is fully specified (not a stub) with
`escalation: {preemptionStage: 4, outputType: "safety-escalation"}`. Check 9 was updated from "nine
fixtures" / "other two positive" to "ten fixtures" / "other three positive," correctly. Closed —
R2-F2 (BLOCKER) genuinely fixed with the exact fixture the finding demanded (its option (a)).

**(c) `numeric_provenance`'s trigger — now covers `curated-evidence-guidance` dose-context outputs,
no vacuous binding.** The reworked `numeric_provenance` row's "Required when" column now reads:
"...**or** at least one `capability_claim` entry's `guidanceClass` is `curated-evidence-guidance`
and that entry carries `dosageContext: true`." The companion `capability_claim` row was extended in
the same edit to add the `dosageContext: true` fourth key for exactly this case, sourced to
`doseContextDefinition` (confirmed live in the contract: a generically-phrased compound-amount test,
"An output is in dose context if and only if any value it presents or derives is a compound amount
or an amount-derived quantity..."). I checked whether this is scope creep: `doseContextDefinition`'s
`appliesToCell` key names `pregnancy-or-lactation.C1` as its one currently-invoked matrix
application, but its own test text is written in general, value-shape terms (not label- or
cell-scoped), and `numericProvenance.rules[0]` independently states "every displayed number
carries a machine-readable origin" with no guidance-class qualifier — so applying the same
already-frozen test to determine `numeric_provenance` applicability for a different
(`curated-evidence-guidance`) guidance class is a legitimate re-use of an existing frozen test
against the already-general `numericProvenance` rule, not an invented rule. This also matches the
exact remediation option (b) the original finding proposed ("broaden
`CAPABILITY-FIELD-MAP.md`'s trigger to also cover any `curated-evidence-guidance` entry whose
function declares a dose-context output"). Closed — R2-F3 (MAJOR) genuinely fixed, not vacuous.

**(d) Every changed binding still only REFERENCES frozen contract IDs — byte-checked a sample.**
Re-derived live and compared against the spec's citations:
- `functionReviewStatus.rules[2]` = `"review-required` names the human owner and blocks public
  enablement until `reviewed`."` — byte-matches the `function_review_owner` row's quoted citation.
- `escalationSemantics.rules[0]` = `"acute-red-flag-or-emergency: output stops ordinary guidance
  immediately..."` — matches the `escalation` row's "unscoped 'stops ordinary guidance
  immediately'" citation (now correctly `rules[0]`, i.e. the unconditional acute-red-flag clause).
- `escalationSemantics.rules[1]` = `"prescription-treatment-involved: alteration-of-treatment
  surfaces refuse..."` — matches the row's "alteration-of-treatment surfaces refuse" citation (now
  correctly `rules[1]`, replacing the prior review's flagged off-by-one `rules[3]`).
- `escalationSemantics.rules[2]` = `"Escalation is a distinct output type (class
  `safety-escalation`), never a footnote..."` — matches the `outputType` requirement's citation
  (now correctly `rules[2]`, fixing R1-F1's off-by-one).
- `cellSemantics.escalated` = `"E — escalate (stop ordinary output, surface escalation)"` — now
  explicitly cited in both the capability_claim row's prose ("the literal defined once, live, at
  `cellSemantics.escalated`") and its "Frozen source" cell, closing R1-F2's under-citation.
- `labels.prescription-treatment-involved.behavior.C3` = `{value: "refused", scope:
  "prescribed-treatment-only"}` — matches the row's new narrowed-trigger citation
  (`labels.prescription-treatment-involved.behavior.C3.scope`) exactly, confirming the C1/C2
  `degraded` cells (scope `null`) are correctly excluded from the narrowed trigger.
- `missingInputLadder.rungs` (3 rungs) and `numericProvenance.lockedOrigins` (5 values) —
  unaffected by this delta, re-confirmed unchanged and matching.
No citation in the sampled set restates, paraphrases, or copies a ruled value as a static literal
inside the P3-B file; every "Live cross-reference rule" cell describes a lookup procedure against
the live contract, consistent with the "composed, not hardcoded" hard constraint. Nothing sampled
diverges from the live contract.

**(e) No new defects in the delta.** Diffed `0eeb94e` (reviewed version) against `7b3225c` (tip)
directly: the only file touched in the whole commit range is `parcels/P3-B.md` itself (42
insertions / 22 deletions); the frozen contract (`product-capability-safety-contract.json`/`.md`)
is untouched (empty diff); the worktree is clean (`git status --porcelain` empty). Every edit in
the diff maps 1:1 to one of the five findings being closed: surfaces-list/allowed-surfaces/
deterministic-verification counts (15→16, nine→six, nine→ten fixtures — all internally
consistent after the edit, re-counted by hand), the `capability_claim`/`numeric_provenance` row
rewrite (R2-F3), the `escalation` row rewrite (R1-F1, R2-F2, R2-F4), and the new fixture's full
addition across all five places it needed to appear (frontmatter `surfaces`, document contract 4,
"Exact allowed surfaces," AC-P3B-04, acceptance-to-evidence map). No stray, unrelated, or
half-applied edit found. Re-ran the full set of numbered deterministic checks (1-13) by hand
against the current text for internal self-consistency (fixture count, surface count, key count)
— all match.

**Hash integrity (re-verified independently).** Recomputed `sha256sum` for the spec file itself
(`B4535C95215EB57C0F18DD27865A2A0BE0839746BC382A5F0943C0311A685DFC`, matching the file at tip
`7b3225c`) and spot-checked the contract/`classification-axes.schema.json` live values the spec
cites as its "before" state (`productGuidanceClass.controlSource: "none"`,
`controlBindingStatus: "deferred-to-P3-B"`) — both match the spec's claims exactly.

**No `TBD`/placeholder.** Grepped the full reworked spec body for `TBD`, `TODO`, `FIXME`, `{{` —
zero matches (unchanged from the prior reviews' finding).

**Determinism.** The reworked `escalation` and `numeric_provenance` rows, and the new fixture, are
specified with concrete, byte-exact expected values; two independent honest builders reading the
current text and the frozen contract would produce byte-identical `CAPABILITY-FIELD-MAP.md`
content and the same ten fixtures. No material ambiguity remains in the delta.
