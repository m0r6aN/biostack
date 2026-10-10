# p4_impl_review_2 — P4 implementation review

**Verdict:** FAIL
**Subject:** P4 implementation (`docs/specs/scripts/validate-spec.ps1`, `docs/specs/scripts/verify-p4.ps1`,
`docs/specs/schemas/fixtures/p4/*`, `docs/specs/README.md`, `docs/specs/INDEX.md`)
**Build authority:** `docs/INITIATIVES/biostack-governed-delivery/parcels/P4.md`
**P4.md SHA-256 (as currently on `main`):** `947de2167b8528c75eede43699ee7454b5104115e70ebd89e0ae0201cfa74b43`
**Builder commit (exact):** `cbf6e229e9f35d9f4b5035d7ea6e7755ef0d78f3` ("feat(governed-delivery): P4 spec linter implementation"), parent `152ec941fe7645ffc6a196bff3e4c67896476974`
**Repo HEAD reviewed (`main`):** `01d73fe1d2fb837301e13a997a70d4d8b20d62f7` (builder commit is an ancestor of `main`; the P4.md content is unchanged since the builder commit)
**Worktree used for live execution:** `/home/cmorgan76/Repos/biostack-wt/p4-impl` (pre-existing, pinned at `cbf6e22`)
**Date:** 2026-10-10

## Summary

`verify-p4.ps1` does execute cleanly end-to-end against the builder commit and its pinned
`BaseCommit` — I ran it for real (`pwsh -File docs/specs/scripts/verify-p4.ps1 -BaseCommit
152ec941fe7645ffc6a196bff3e4c67896476974 -BuilderId p4_builder -ReviewerIds r1,r2 -EvidenceDirectory
artifacts/p4-verification`) and it printed `P4 verification PASS`, all 12 checks green, evidence
bundle written and untracked as required. The mechanical verification suite is real, not
theater — all 16 fixtures are genuinely re-derived by subprocess invocation of
`validate-spec.ps1`, not by independent re-computation.

However, the **deliverable itself** — the general-purpose linter `validate-spec.ps1` — has a
reproducible, PoC-confirmed defect that defeats its own stated purpose for the dominant,
real-world invocation mode (`-SpecPath` against a real file on disk), and the implementation
emits at least one `reason` literal outside the Hard-constraints-pinned closed vocabulary on an
ordinary, realistic input that none of the 16 shipped fixtures exercise. Separately, the builder
unilaterally overrode an explicit `expected.result` the spec itself pins (`"valid"`) for
`positive-real-spec-p3b-cross-check.json`, in direct tension with the Gate 2 dispatch record's own
"STOP-AND-REPORT on any deviation" instruction, without a ratified amendment record. All three are
independently reproducible against the real, shipped code; none require speculation about builder
intent.

## Ranked findings

### F1 — BLOCKER — Real on-disk spec validation never scans body prose for placeholders; `TBD` littered throughout a real file's Objective/Acceptance Criteria/Tests sections is reported `"valid"`.

**Claim:** `validate-spec.ps1`'s stage 5 placeholder scan, in `-SpecPath` (disk) mode, only scans
heading text and frontmatter string values. It never scans the Markdown body prose under those
headings — which is where the overwhelming majority of a real spec's actual content, and every
realistic placeholder occurrence, lives.

**Evidence:** `docs/specs/scripts/validate-spec.ps1`, stage 5 ("`--- Stage 5: placeholder scan
(frontmatter values + headings) ---`"):
```
$scanParts = New-Object 'System.Collections.Generic.List[string]'
foreach ($h in $headings) { $scanParts.Add([string]$h) | Out-Null }
foreach ($k in (Get-PropNames -Obj $frontmatter)) { ... }
$scanText = [string]::Join(' ', $scanParts.ToArray())
```
`$parsed.Body` (the real file's full Markdown body, already parsed and available at this point in
the disk-mode branch — see `$parsed = ConvertFrom-FrontmatterText -Text $raw` earlier in the same
function) is never added to `$scanParts` and never reaches `Test-PlaceholderViolation`.

This directly contradicts the spec's own stage-5 text (P4.md, Required document contract 1,
"Validation stages", item 5): *"apply `placeholderNormalizationSteps`'s nine-step pipeline, in its
exact pinned order, to every frontmatter value **and heading/body text available to the
validator**"* (emphasis mine — "body text" is explicit and the body is in fact available: it is
read from disk a few lines earlier in the same function). It also contradicts the "Structural
validation only" disclaimer's own claim about what a `"valid"` result asserts: *"asserts
frontmatter-key presence, closed-vocabulary membership, fold-live required-section resolution,
**placeholder absence**, and live bound-field agreement only"* — for a real file, placeholder
absence is not actually checked outside frontmatter/headings.

**Proof-of-concept (reproduced live against the actual shipped binary, no edits to the repo):**
I built a scratch `RepoRoot` at `/tmp/p4attack` containing unmodified copies of the five live
schema/contract sources this parcel composes, plus a crafted `coordinator-parcel`-shape file at
`docs/INITIATIVES/biostack-governed-delivery/parcels/ATTACK.md` with a fully clean frontmatter and
heading set (satisfying `standard`'s six required-section terms) but a body whose every section is
nothing but repeated literal `TBD` text (no frontmatter TBDs, no heading TBDs):
```
## Objective
TBD TBD TBD. This entire section is an unresolved decision: TBD, TBD, TBD. ...
## Contracts
TBD. No contract has actually been defined. TBD TBD TBD.
## Acceptance criteria
- AC-1: TBD
- AC-2: TBD
## Tests
TBD -- no tests defined, left entirely as TBD.
## Rollback
TBD.
```
Invoking the real, shipped script from the actual builder-commit worktree:
```
pwsh -NoProfile -ExecutionPolicy Bypass -File docs/specs/scripts/validate-spec.ps1 \
  -SpecPath 'docs/INITIATIVES/biostack-governed-delivery/parcels/ATTACK.md' -RepoRoot /tmp/p4attack
```
produced:
```json
{ "result": "valid", "reason": null, "detail": null, ... }
```
A sanity-check control (same file, but with `title: TBD` in frontmatter instead) correctly
produces `"result": "invalid", "reason": "placeholder-violation"` — confirming the scan logic
itself works, and that the gap is specifically "body prose is never fed into it," not a broader
pipeline bug.

**Why this matters / why it is not covered by the fixture suite:** of the 16 shipped fixtures,
only one (`positive-real-spec-p3b-cross-check.json`) exercises `-SpecPath` disk mode at all; the
other 15 are all `-SyntheticSpecJson` (which, by P3-A's own convention this parcel inherits, never
carries real body prose — only `frontmatter`/`headings`), so they cannot exercise this gap even in
principle. And that one real-file fixture's `expected.result` is `"invalid"` for an *earlier*
stage (`missing-required-section`, stage 4 — see F3 below), so execution never even reaches stage
5 for that fixture. **The body-placeholder-scan gap in disk mode is therefore completely
unexercised by all 16 fixtures and all 12 `verify-p4.ps1` checks.** This is exactly the class of
defect AC-P4-06 ("No unresolved placeholder anywhere") and the charter's own placeholder-laundering
scar tissue (P3-B's closure: "unicode-confusables-skeleton step added (homoglyph-TBD evasion
dead)") exist to prevent — and it defeats the parcel's own stated Carry-over purpose: *"the
reusable `validate <path>` entrypoint P3-A's own spec explicitly deferred to this parcel."* Any
future parcel spec authored under this governance chain and validated for real via
`validate-spec.ps1 -SpecPath` can carry unresolved `TBD`/`TODO`/`FIXME`/`{{...}}` placeholders
throughout its entire substantive prose and still be reported structurally `"valid"`.

**Smallest amendment:** add `$parsed.Body` (optionally with headings/code-fences excluded if that
matters for the `sanctionedTemplateFillInMarker` scope) to `$scanParts` in stage 5's disk-mode
branch, and add a seventeenth fixture (or amend an existing disk-mode fixture) that places its
single deliberately-disguised placeholder in body prose rather than frontmatter/headings, so this
path is mechanically guarded going forward.

**Does it change a locked decision?** No — it is a straightforward bug-fix bringing the
implementation into conformance with the spec's own already-pinned stage-5 text; no frozen
contract or ratified decision needs to change.

### F2 — MAJOR — Builder unilaterally overrode the spec's own pinned `expected.result` for `positive-real-spec-p3b-cross-check.json` and narrowed AC-P4-05's cross-verifier-agreement scope, in direct tension with the Gate 2 dispatch record's explicit "STOP-AND-REPORT on any deviation" instruction, with no ratified amendment record.

**Claim:** P4.md (Required document contract 2) explicitly states, for
`positive-real-spec-p3b-cross-check.json`: *"`expected.result` is `"valid"`"* and names the
fixture's purpose as proving the general linter and `verify-p3b.ps1` *"agree on the one outcome
check 10 actually establishes for a real, previously-reviewed artifact."* AC-P4-05 names the same
expectation. The shipped fixture instead carries `"expected.result": "invalid",
"expected.reason": "missing-required-section"` (naming `contracts`) — the diametric opposite of
what the spec pins, for the one fixture whose entire job is proving a positive, real-file,
cross-verifier agreement outcome. `verify-p4.ps1`'s check 7 was correspondingly rewritten to
compare only the capability-field-binding sub-computation (a scope the spec's own check-7 text
does also mention as the narrower reading, but only as a parenthetical aside, not as the stated
primary claim), not `validate-spec.ps1`'s actual top-level `result`/`reason` for the file, which
is what AC-P4-05's prose and the fixture's own name ("`positive-`...") claim to prove.

**Evidence:**
- `docs/INITIATIVES/biostack-governed-delivery/parcels/P4.md` (Required document contract 2,
  `positive-real-spec-p3b-cross-check.json` bullet): `` `expected.result` is `"valid"` ``.
- `docs/specs/schemas/fixtures/p4/positive-real-spec-p3b-cross-check.json`:
  `"expected": { "result": "invalid", "reason": "missing-required-section", "detail": { "term":
  "contracts" }, ... }`.
- `docs/specs/scripts/verify-p4.ps1`, header comment block "DISCLOSED DEVIATIONS", items D1/D2,
  and check 7's implementation (scoped to `Test-CapabilityFieldBinding`/
  `Test-P3BCapabilitySafetyOverlay` only, never comparing `validate-spec.ps1`'s actual top-level
  `result`/`reason` for the real file against anything).
- `docs/INITIATIVES/biostack-governed-delivery/dispatch/GATE2-P4-IMPLEMENTATION.md`, dispatch
  notes: *"Standard verifier lessons: ... **STOP-AND-REPORT on any deviation** (D-L enforcement)."*
- No `docs/INITIATIVES/biostack-governed-delivery/amendments/` or equivalent ratified-amendment
  record exists for this deviation (searched `docs/INITIATIVES/biostack-governed-delivery` for any
  P4 amendment artifact beyond `P4.md` itself and the Gate 2 dispatch record; none found). The
  P4.md file content itself still states the original `"valid"` expectation verbatim — it was not
  amended to match the builder's own, independently-derived conclusion.

I independently re-derived the builder's underlying technical claim and found it **substantively
correct**: `parcels/P3-B.md`'s real H3 headings (`### Required document contract 1: ...` etc.) do
not satisfy `SECTION-HEADING-MAP.md`'s heading-length-bound rule for the one-token terms
`contracts`/`tests`, and the file genuinely carries no bare `## Contracts` or `## Tests` heading
(`grep -n "^#\{2,3\} " docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md`, verified).
So the deviation is not dishonest or hidden — it is disclosed in detail in code comments, the
commit message, and a verification-summary `disclosedDeviations` array. But P4.md's own Stop
conditions section names exactly this situation as a *required stop*: *"A fixture's expected
result cannot be derived deterministically from a live read of ... `SECTION-HEADING-MAP.md` ...
without a human policy call"* → *"Stop and return to the coordinator."* The builder instead chose
to resolve the conflict itself, change the fixture's `expected` value, and correspondingly narrow
what AC-P4-05's check 7 actually proves — none of which is an authority a builder holds under this
governance model, and which directly disregards the dispatch record's own explicit directive.

**Why this matters:** AC-P4-05, as actually delivered, proves a materially weaker claim than the
spec and the fixture's own `positive-` naming assert. A reviewer or coordinator skimming
`fixture-results.json`/`verification-summary.json` for "P3-B.md cross-check: pass" would
reasonably believe the general linter validates a real, previously-reviewed, merged parcel spec as
structurally sound end-to-end; it does not — it fails at required-section resolution, and the
"agreement" check only ever compares the one narrow sub-computation that was never actually at
risk of disagreeing (both sides are now near-duplicate ported logic against the same frontmatter).

**Smallest amendment:** this is exactly a "stop and return to coordinator" scenario under the
spec's own Stop conditions. The correct remediation is a ratified P4 amendment (explicitly scoping
AC-P4-05/check 7 to the capability-field-binding overlay, as the implementation already
functionally does) or a `P3-B.md` heading realignment decision from the coordinator — not a
unilateral builder resolution. At minimum, the fixture should be renamed away from the
`positive-` prefix (it is not a positive-outcome fixture) and AC-P4-05's prose should be corrected
to match what check 7 actually proves, under an approved amendment.

**Does it change a locked decision?** Yes — it silently changes what AC-P4-05 (an already-approved
acceptance criterion) actually certifies, without going through this governance chain's own
amendment process.

### F3 — MAJOR — `validate-spec.ps1` emits a 13th `reason` literal (`"missing-required-field"`) outside the spec's own pinned twelve-literal closed vocabulary, on an ordinary input (`delivery_classes: [migration]` without `conditionalInputs`), and no fixture exercises this path.

**Claim:** Hard constraints pins: *"`validate-spec.ps1`'s own output `reason` field is a closed,
twelve-literal vocabulary: the eleven already established ... (`unknown-label`,
`empty-required-axis`, `incompatible-controls`, `missing-required-frontmatter-key`,
`missing-required-section`, `placeholder-violation`, `unknown-extension-point`,
`unrecognized-shape`, `capability-claim-drift`, `invalid-function-review-status`,
`premature-public-enablement-claim`) plus exactly one new leaf ... `invalid-status` ... this
parcel adds no other new `reason` literal to `validate-spec.ps1`'s own output."*

`Invoke-SpecFold` in `validate-spec.ps1` (ported from `fold-engine.md`'s own algorithm for the
`migration` class's conditional `reviewers` field) returns `stopReason.reason =
'missing-required-field'` when a spec declares `delivery_classes: [migration]` without a
`conditionalInputs.userDataOrProductionSchemaAffected` frontmatter entry — and this value flows
straight through to the top-level output `reason` field. `"missing-required-field"` does not
appear anywhere in Hard constraints' pinned list of eleven-plus-one.

**Reproduced live** (synthetic spec, `delivery_classes: ["migration"]`, no `conditionalInputs`
key, run against the real builder-commit script):
```
pwsh -File docs/specs/scripts/validate-spec.ps1 -SyntheticSpecJson synth-migration.json -RepoRoot /tmp/p4attack
→ { "result": "invalid", "reason": "missing-required-field", "detail": { "field":
    "userDataOrProductionSchemaAffected", "deliveryClass": "migration", ... } }
```
Exit code 1 (nonzero), confirmed by `$LASTEXITCODE`.

**Mitigating context (recorded honestly):** `"missing-required-field"` is not an invented literal
— it is itself one of the four enum values `docs/specs/schemas/routing-output.schema.json`
(frozen, P2) already pins for `stopReason.reason` (`unknown-label`, `empty-required-axis`,
`incompatible-controls`, `missing-required-field`), and `fold-engine.md` (frozen, P2) explicitly
documents it (`"stopping with missing-required-field if a required conditional ... is absent"`).
So the implementation is faithfully composing a genuinely pre-existing, frozen P2 source
"unmodified," as Hard constraints itself requires elsewhere ("Composed, not hardcoded"). The defect
is better characterized as **P4.md's own Hard constraints prose under-counting its sources**: it
lists only 3 of `routing-output.schema.json`'s 4 enum values as "already established," omitting
`missing-required-field`, and names "the eleven" when a faithful composition of the named frozen
P2 sources genuinely surfaces a 12th pre-existing value (for 13 total with `invalid-status`).

**Why it still matters:** regardless of whether the root cause is a spec-drafting gap or an
implementation gap, the shipped behavior is reproducible, realistic (any future `migration`-class
spec that omits `conditionalInputs` hits it), and **zero of the 16 fixtures exercise it** — the one
fixture that does declare `delivery_classes: [..., "migration"]`
(`negative-placeholder-entity-disguised-multilabel.json`) also supplies `conditionalInputs`
explicitly, specifically avoiding this path. `verify-p4.ps1` never asserts the output `reason`
vocabulary is closed to exactly twelve values anywhere (no enumeration/allow-list check exists for
this), so this gap is invisible to the mechanical verification suite.

**Smallest amendment:** either (a) amend P4.md's Hard constraints sentence to name
`missing-required-field` as a thirteenth pre-established (not newly-added) literal, consistent with
`routing-output.schema.json`'s own enum, and add a fixture exercising it; or (b) if the coordinator
intends P4 to exclude conditional-reviewer delivery classes from in-scope coverage, say so
explicitly and add a `verify-p4.ps1` check asserting the output `reason` vocabulary never exceeds
the stated set. Either way this needs an explicit spec/fixture update, not silence.

**Does it change a locked decision?** No — it is a documentation/fixture-coverage gap relative to
already-frozen P2 sources, not a new product-semantic decision.

### F4 — MINOR — `extension_section` frontmatter key (stage 6) is a builder-invented convention with no prior pin anywhere in canon.

**Claim:** Stage 6 ("Extension-point reference check") keys off a frontmatter field literally named
`extension_section` (`Test-PropPresent -Obj $frontmatter -Name 'extension_section'`). This exact
key name does not appear in `parcel-spec.schema.json`, `EXTENSION-POINTS.md`,
`CAPABILITY-FIELD-MAP.md`, or any merged parcel spec. P4.md's own stage-6 text ("if the spec
declares a reference to an `extensionSections` key not present in the live registry") does not pin
a field name, leaving this underspecified in the approved spec itself. The builder's choice is
reasonable and internally consistent (the fixture/check both agree on it), but it is a convention
invented by this parcel without the composed-source backing Hard constraints otherwise demands for
every other stage. Not independently exploitable (no evidence this key is ever populated by a real
spec today), but worth recording for the next parcel that might need to declare a real extension
reference and finds no canonical field name to use.

**Smallest amendment:** pin the field name in `EXTENSION-POINTS.md` or `parcel-spec.schema.json` in
a future amendment, rather than leaving it implementation-defined.

**Does it change a locked decision?** No.

### F5 — MINOR / structural note — Fixture `expected` values are not independently pinned against future weakening; `verify-p4.ps1`'s own re-derivation is the only backstop.

**Observation (not unique to this parcel — same pattern as P2/P3-A/P3-B):** `verify-p4.ps1` check
6 compares each fixture's `expected` block against `validate-spec.ps1`'s *actual, live* output —
it never independently re-computes the expected answer from the composed sources by a second,
diverse code path (except for the five pinned literal values in check 10's mutation-overlay test
and the one real-file cross-check in check 7). This means a future rework that simultaneously
weakens both `validate-spec.ps1`'s logic and a fixture's `expected` value in the same direction
would still pass `verify-p4.ps1` mechanically. This is an inherent property of fixture-driven
self-verification as this entire initiative has designed it (not something P4 introduced), and the
real backstop is human dual review plus check 10's five pinned mutation-overlay probes (which *do*
independently assert direction, not merely self-consistency) — but it is worth naming explicitly
since the task asked whether fixture expected-outcomes are pinned: **they are pinned only in the
narrow sense of check 10's five mutation probes and the two placeholder-evasion JSON-pointer
exclusions in check 8; the other eleven fixtures' `expected` values have no independent
cross-check beyond reviewer reading.**

**Does it change a locked decision?** No — observational.

## Missing pieces / collisions / unknowns

- No ratified amendment record exists for the D1/D2/D3 deviations disclosed in `verify-p4.ps1`'s
  header comment and the builder commit message (see F2). The spec file on `main` still states the
  original, now-contradicted `"valid"` expectation verbatim.
- I did not attempt to enumerate every possible placeholder-evasion variant against the nine-step
  normalization pipeline itself (F1 makes that moot for disk-mode real files regardless of pipeline
  strength, since the pipeline is never invoked against body text in that mode); the pipeline logic
  itself, where it is actually invoked (frontmatter/headings, and the synthetic-mode path), appears
  faithfully ported from P3-A/P3-B and I found no additional evasion in that scope.
- Multi-label fold composition (`positive-coordinator-parcel-multilabel-fold.json`), referential
  capability-claim drift resolution, and the empty/malformed-input resilience path (check 9) all
  checked out clean under live execution and targeted probing; I found no exploitable gap in those
  specific areas beyond what is already named above.

## Verification notes

- Ran `verify-p4.ps1` for real against the actual builder commit (`cbf6e22`, in the pre-existing
  pinned worktree `biostack-wt/p4-impl`) with `-BaseCommit 152ec941fe7645ffc6a196bff3e4c67896476974`:
  result `P4 verification PASS`, all 12 checks green, evidence bundle (`artifacts/p4-verification/`)
  written and left untracked as required by check 12 (removed afterward to leave the worktree
  clean; this was execution of the parcel's own verifier as the spec itself directs, not a repo
  edit).
- Independently confirmed the exact 20-file changed-set for commit `cbf6e22` against its parent
  `152ec94` via `git diff --name-only 152ec94...cbf6e22` — matches the spec's 20 allowed surfaces
  exactly; the Gate-2-created dispatch/queue docs that appear when diffing against current `main`
  HEAD are confirmed (by `git log`) to predate `cbf6e22` on `main` and are unrelated to the P4
  builder diff — not a scope violation.
- Confirmed `product-capability-safety-contract.json`'s C2 cell for `minor-or-age-uncertain` is
  `degraded` (not `allowed`), supporting `negative-capability-claim-drift-referential.json`'s
  claimed drift.
- Confirmed `routing-output.schema.json`'s `stopReason.reason` enum and `fold-engine.md`'s own text
  both already name `missing-required-field` (supports F3's "pre-existing P2 literal, not invented"
  mitigating context).
- Confirmed, by direct `grep` of `parcels/P3-B.md`, that it carries no bare `## Contracts`/`##
  Tests` heading, only `### Required document contract N: ...` H3 subheadings — supporting that the
  builder's D1 technical claim (underlying F2) is itself accurate, even though the *process* by
  which it was resolved was not authorized.
- Did not find any evidence of git-write activity, repo edits, or any file touched by this review
  other than this output file and the disposable `/tmp/p4attack` scratch directory and the
  evidence directory I created and removed inside the existing `p4-impl` worktree (standard
  verifier-execution byproduct, not a source edit).
