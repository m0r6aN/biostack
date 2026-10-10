# p4_impl_review_1 — P4 implementation review (retrospective, PR #538)

**Verdict:** PASS

**Commit reviewed (merge, main HEAD):** `01d73fe1d2fb837301e13a997a70d4d8b20d62f7` (PR #538,
"Merge pull request #538 from m0r6aN/feat/p4-spec-linter")
**Content commit (the actual P4 builder payload):** `cbf6e229e9f35d9f4b5035d7ea6e7755ef0d78f3`
("feat(governed-delivery): P4 spec linter implementation")
**BaseCommit (re-anchored, per coordinator's `baf4b46` ANCHOR NOTE and the builder's own commit
message):** `152ec941fe7645ffc6a196bff3e4c67896476974` (PR #537 merge — final reviewed spec state
incl. amendment A-P4-1). Independently re-verified: `git rev-parse 152ec94` resolves to this exact
40-char SHA, and `git diff --name-only 152ec94...cbf6e22` returns exactly the 20 allowed surfaces,
nothing else. The dispatch record's originally-named anchor `fdb83ee` predated PR #537's spec
amendments (single-anchor discipline correctly caught this mid-flight per `baf4b46`); the re-anchor
is itself documented on `main` before the builder's dispatch and is not a retroactive excuse.
**Authority:** `docs/INITIATIVES/biostack-governed-delivery/parcels/P4.md`
(SHA-256 `947DE2167B8528C75EEDE43699EE7454B5104115E70EBD89E0AE0201CFA74B43`, current content),
amended by A-P4-1 (`c97109f`, PR #537).
**Date:** 2026-10-10
**Worktree used for live verifier run:** `/home/cmorgan76/Repos/biostack-wt/p4-impl` (HEAD
`cbf6e229e9f35d9f4b5035d7ea6e7755ef0d78f3`)

## Summary

This is a careful, unusually self-critical implementation. I ran `verify-p4.ps1` live against the
pinned re-anchored `BaseCommit` in the existing `p4-impl` worktree; all 12 checks pass
(`P4 verification PASS`, exit 0) and all sixteen fixtures reproduce their expected
result/reason/detail/foldSummary/boundFieldsChecked exactly by actually invoking
`validate-spec.ps1` (never by re-deriving the expected answer independently). The changed-file set
between the re-anchored `BaseCommit` and the builder's content commit is exactly the 20 allowed
surfaces, no more, no fewer; every frozen path I spot-checked (charter, P3-A/P3-B specs and
closure, P0-B contract `.json`/`.md`, P2 schemas) is byte-identical to its spec-pinned SHA-256. I
independently constructed and ran seven adversarial probes in `/tmp` against the real, shipped
`validate-spec.ps1` (spliced-quote heading smuggling, merged-clause heading smuggling, malformed
JSON, empty JSON, nonexistent `-SpecPath`, and two homoglyph/entity variants) — all fail closed
exactly as the eleven-checks/twelve-literal design requires, with no crash and no stack trace. The
twelve numbered checks are genuinely non-vacuous: check 10's five-source composition-verification
mutates real, disposable scratch-overlay copies of the five composed sources and re-invokes the
real script via subprocess, and I verified live that this actually ran and passed (not merely
asserted). Check 8's RR3 build-time tightening is honored exactly as the dispatch record required:
the one JSON-pointer path (`input.syntheticSpec.frontmatter.placeholderProbe`) is hardcoded, not a
two-branch "guess the exclusion field" scan. The builder's three disclosed deviations (D1/D2/D3,
documented in `verify-p4.ps1`'s own header comment and the builder's commit message) are each
independently reproducible from first principles against the frozen, byte-pinned source documents
they cite, and each is the narrower, more honest reading rather than a convenient escape hatch. One
pre-existing, inherited (not P4-introduced) binding-logic gap is worth recording for P6/P7 forward
work (see Verification notes) but does not change the verdict, because fixing it would require P4
to invent a new validation rule beyond what P3-B's own, already-ratified `verify-p3b.ps1` logic
already enforces — exactly what this parcel's Hard constraints forbid it from doing.

## 1. Non-vacuousness of the twelve checks (read line-by-line; adversarial constructions attempted)

Read `verify-p4.ps1` in full (1100 lines) and `validate-spec.ps1` in full (1007 lines).

- **Check 1-4** (ancestor check, diff-check, exact changed-file set, frozen-path byte-identity):
  mechanical `git` calls against real SHAs; I independently re-ran the same `git diff --name-only`/
  `sha256sum` commands and got identical results (Verification notes, below). Non-vacuous.
- **Check 5** (structural surface + `pathPattern` grammar): asserts the CLI parameter sets and a
  single `Invoke-SpecValidation` definition by **text search** on the real file (`$invokeCount -eq
  1`), then **actually invokes** the real CLI twice, asserting `parcels/P3-B.md` resolves
  `coordinator-parcel` and `CHARTER.md` resolves `unrecognized-shape` from real subprocess output,
  not a hand-computed guess. Non-vacuous; the grammar itself (`ConvertTo-GlobRegex`/
  `Test-PathPattern`, `validate-spec.ps1` lines ~378-398) implements the single-segment-`*`,
  `|`-split-alternative rule pinned in the spec's "pathPattern matching grammar" paragraph exactly.
- **Check 6** (sixteen fixtures): each fixture is read from disk, `validate-spec.ps1` is invoked as
  a **real subprocess** (`Invoke-ValidateSpecCli`, which shells to `pwsh -File validate-spec.ps1`),
  and the produced object is deep-equality-compared field-by-field, including `foldSummary` and
  `boundFieldsChecked` when present/absent. I re-ran this independently (below) and got the same 16
  results. Non-vacuous.
- **Check 7** (cross-verifier agreement): genuinely dot-sources the real `validate-spec.ps1`'s own
  `Test-CapabilityFieldBinding` function (via `Import-ValidateSpecFunctions`, which extracts the
  real function-definition text between pinned markers and dot-sources it — not a re-derived copy)
  and compares it against a byte-ported copy of `verify-p3b.ps1`'s own `Test-CapabilitySafetyOverlay`
  logic, both evaluated against the real, live `product-capability-safety-contract.json` and the
  real, frozen `parcels/P3-B.md` frontmatter. Non-vacuous, and correctly scoped per disclosed
  deviation D2 (below).
- **Check 8** (placeholder scan, RR3-tightened): scans every one of the 20 allowed surfaces for the
  two live `noPlaceholderPatterns` after the live-read nine-step normalization pipeline, with
  **exactly one** hardcoded JSON-pointer exclusion
  (`input.syntheticSpec.frontmatter.placeholderProbe`) applied only to the two named exclusion
  fixtures, redacting that one field and re-scanning everything else in the file (including
  `expected.*`). I confirmed by direct read that this is a single literal path, not two live
  candidate branches — RR3 is honored.
- **Check 9** (resilience): actually invokes the real CLI against the degenerate fixture, asserts
  exit code `1`, asserts stdout contains no `"Exception"`/`"At line:"` substring (crash/stack-trace
  detection), and asserts the exact result/reason/detail. Non-vacuous; I additionally fed three
  further adversarial payloads not in any fixture (malformed JSON, a truly empty file, and a
  nonexistent `-SpecPath`) directly at the real script and got clean, non-throwing, exit-1
  `unrecognized-shape`/`invalid` results in every case (Verification notes).
- **Check 10** (composition-verification, AC-P4-02): builds five **disposable scratch overlays**
  (`New-ScratchOverlay`, real temp-directory copies of the six schema files), mutates exactly one
  pinned value in each per A-P4-1's own worked examples (except probe (b), corrected per D3, below),
  and re-invokes the real script twice per source (pre/post) via real subprocess calls, asserting
  both the pre-mutation baseline **and** the post-mutation change. This is the strongest possible
  mechanical proof against a hardcoded-linter adversary: I ran it live and it passed for all five
  sources (confirmed in `composition-verification.json`, below).
- **Check 11/12** (evidence bundle, clean tree): real file writes to `artifacts/p4-verification`,
  real `git status --porcelain` parsing requiring every status line to start `?? artifacts/
  p4-verification/`. Non-vacuous.

**I could not construct a nonconforming parcel spec that passes `verify-p4.ps1`.** Every adversarial
construction I attempted (below) was rejected with the correct reason.

## 2. Named evasion classes — independently constructed and verified rejected

I built seven adversarial synthetic-spec JSON payloads in `/tmp/p4_adversarial/` (never written
into the repository) and ran them directly against the real, shipped `docs/specs/scripts/
validate-spec.ps1` via `pwsh -SyntheticSpecJson`/`-SpecPath`:

- **Homoglyph TBD:** the shipped fixture `negative-placeholder-homoglyph-confusables.json` uses
  Cyrillic `ТВD` (U+0422 Т, U+0412 В, Latin D) inside `placeholderProbe`; I confirmed both Cyrillic
  code points are present in `validate-spec.ps1`'s `$script:ConfusablesMap` (lines ~322-330) and
  that the live fixture reproduces `"placeholder-violation"`. **Rejected.**
- **Empty/malformed input:** fed literally malformed JSON (`{not valid json`), a zero-byte file, and
  a nonexistent `-SpecPath` directly at the real script (not the fixture). All three returned a
  clean, non-throwing `result: "invalid"`/`"unrecognized-shape"` with exit code `1`, matching the
  fail-closed discipline check 9 requires generically. **Rejected**, including off-fixture
  variants the spec's own sixteen fixtures never literally enumerate.
- **Spliced quotes** (multi-token term split across two headings to defeat contiguous-token-
  subsequence matching): built `## Acceptance` + `## Criteria` as two separate headings, standing
  in for the two-token term `acceptance criteria`. Result: `invalid`/`missing-required-section`
  naming `acceptance criteria` — splicing across a heading boundary does **not** satisfy the term.
  **Rejected.**
- **Merged-clause smuggle** (one heading attempting to satisfy two distinct terms at once): built
  `## Contracts and Tests` as a single heading standing in for both `contracts` and `tests`.
  Result: `invalid`/`missing-required-section` naming `tests` — the distinct-heading-per-term
  bipartite match (`Resolve-RequiredSections`) consumes the heading once, for the lexicographically
  earlier term (`contracts`), and `tests` has no remaining candidate. **Rejected.**
- **ID mis-reference / referential drift:** the shipped fixture
  `negative-capability-claim-drift-referential.json` claims `behavior: "allowed"` for
  `minor-or-age-uncertain`/`curated-evidence-guidance` against the live `C2` cell, which is actually
  `"degraded"` — I independently re-derived this live cell
  (`labels.minor-or-age-uncertain.behavior.C2.value` in `product-capability-safety-contract.json`)
  and confirmed the mismatch is genuine, not asserted. **Rejected.**
- **Hardcoded-linter AC-P4-02 adversary:** check 10's five-source mutation-overlay test is the
  mechanical proof of this; I ran it live (below) and it passed on all five sources, meaning the
  real, shipped implementation is demonstrably **not** a hardcoded adversary — its output tracks
  every one of the five live-mutated sources. **Rejected** (deterministically, not by reviewer
  judgment alone, per AC-P4-02's own design intent).

One additional construction — not on the task's named list but directly adjacent to "vacuous P3-B
bindings" — is recorded in Verification notes below as a MINOR, non-blocking observation: a
synthetic spec with non-empty `guidance_classes`/`substance_function_risk` but an **empty**
`capability_claim: []` array (rather than an absent key) passes stage 7 vacuously, because
`claims.Count -eq 0` means the per-entry loop never runs and no live cross-reference is ever
attempted. I traced this to `verify-p3b.ps1`'s own, already-ratified, frozen
`Test-CapabilitySafetyOverlay` logic (`verify-p3b.ps1` lines 476-483), which P4 is contractually
required to compose/reproduce faithfully, not reinvent — the identical empty-array acceptance
already exists, unmodified, in P3-B's frozen, previously-closed verifier. This is not a P4-
introduced defect, and closing it would require P4 to invent a new product-semantic rule beyond
what P0-B/P3-B already freeze — forbidden by this parcel's own Hard constraints and Frozen
surfaces. I record it for P6/P7 (or a future P3-B amendment) rather than treating it as a P4
implementation failure.

## 3. Triggered-and-satisfied P3-B binding fixture + negative twin

`positive-synthetic-capability-claim-satisfied.json` declares a genuinely triggered
`guidance_classes: [curated-evidence-guidance]` / `substance_function_risk: [ordinary]` pair with a
fully populated `capability_claim`/`missingness` set; I independently re-derived the live contract
cell it depends on (`labels.ordinary.behavior.C2.value` = `"allowed"`) and it matches the claimed
value exactly. `negative-synthetic-capability-claim-unsatisfied-twin.json` is a byte-level diff of
exactly one field (`behavior: "allowed"` → `"degraded"`) — I ran `diff` on the two fixture files and
confirmed the only substantive differences are that one claimed-behavior value and its
mechanically-derived downstream consequences (`result`, `reason`, `detail`, `boundFieldsChecked`).
This is a genuine single-violation twin pair, not two independently authored fixtures that happen
to differ in several uncontrolled ways.

## 4. RR3 tightening (check 8, single JSON-pointer path)

Confirmed by direct read (`verify-p4.ps1` lines ~836-868): exactly one hardcoded string,
`input.syntheticSpec.frontmatter.placeholderProbe`, used for both exclusion fixtures — no
conditional branch choosing between two or more candidate field names. Both exclusion fixtures
(`negative-placeholder-entity-disguised-multilabel.json`,
`negative-placeholder-homoglyph-confusables.json`) do in fact carry their one disguised occurrence
at exactly that path (`T&#66;D` and `note ТВD only`, respectively, independently confirmed by
reading both fixture files). The dispatch record's RR3 instruction ("no two live candidate branches
left in the verifier") is honored exactly.

## 5. Composition-verification mutations match A-P4-1's pinned mutations

Compared A-P4-1's diff (`c97109f`) worked examples (a)-(e) against `verify-p4.ps1`'s check 10
implementation line-by-line:

| Probe | A-P4-1 pinned mutation | `verify-p4.ps1` check 10 implementation |
|---|---|---|
| (a) | `statusClosedVocabulary` append `status-mutation-probe` | Matches exactly (lines ~917-941) |
| (b) | `SECTION-HEADING-MAP.md` rollback-row alias append, `pilot-rollback-alias` | **Corrected** per disclosed deviation D3 to `pilot wind down plan` (same field mutated; different, non-self-contradictory probe pair — see §6) |
| (c) | `CAPABILITY-FIELD-MAP.md` new row `pilot_capability_probe_field` | Matches exactly (lines ~967-984) |
| (d) | `delivery-class-controls.json` `standard.reviewers` `1`→`2` | Matches exactly (lines ~986-1000) |
| (e) | `product-capability-safety-contract.json` `labels.ordinary.behavior.C2.value` `allowed`→`degraded` | Matches exactly (lines ~1003-1018) |

All five are exercised as real, disposable scratch-overlay mutations with real pre/post subprocess
invocations — not illustrative examples left unimplemented. I ran this live (`composition-
verification.json`, below) and all five passed.

## 6. The disclosed pilot-rollback-alias deviation (D3) — independently assessed as correctly handled

I independently re-derived SECTION-HEADING-MAP.md's own matching rule (`docs/specs/schemas/
SECTION-HEADING-MAP.md`, "Matching rule": "Every term below uses itself as its own canonical
alias... the term `rollback` is satisfied by the literal heading `## Rollback`" — contiguous-token-
subsequence, no anchoring requirement) against the spec's own worked example: a heading literally
titled `## Pilot Rollback Alias` normalizes to tokens `pilot rollback alias`, which already contains
the single-token term `rollback` as a contiguous subsequence **before** any alias-cell mutation, and
is well within the heading-length bound (3 normalized tokens vs. a bound of `min(1+4,10)=5`). This
means the spec's own literal pre/post example cannot mechanically distinguish "pre-mutation fails"
from "post-mutation passes" — both states resolve `satisfied: true` regardless of the mutation,
exactly as the builder's D3 disclosure states. This is a genuine, reproducible defect in the spec's
own worked example, not an excuse. The builder's fix — substituting a probe heading/alias pair that
does **not** already contain the bare term as a substring (`## Pilot Wind-Down Plan` / alias
`pilot wind down plan`), while still mutating the exact same `SECTION-HEADING-MAP.md` field
(`rollback`'s canonical-alias cell) the spec names — is the correct, minimal, honest repair: it
preserves the mutated field, preserves the mechanism under test (alias-cell live-read), and restores
the pre/post differential the check needs to be non-vacuous. I ran it live and confirmed the
pre-mutation probe genuinely fails `missing-required-section`/`rollback` and the post-mutation probe
genuinely passes (`composition-verification.json` below, source `SECTION-HEADING-MAP.md`, `pass:
true`). This is exactly the kind of "discovered, provable conflict between a spec's own narrative
assumption and ground truth, disclosed rather than papered over" that this initiative's own prior
P3-B precedent established as the correct handling pattern, and the builder follows it faithfully,
including naming it in the verifier's own header comment, the evidence bundle's
`disclosedDeviations` array, and the commit message.

I also independently assessed the **other two** disclosed deviations (D1/D2), which are not named
in this review's own charge but are material to AC-P4-05/the real-spec cross-check fixture: D1
claims `parcels/P3-B.md` objectively fails `missing-required-section` (`contracts`) under
SECTION-HEADING-MAP.md's own byte-pinned heading-length-bound rule, because its real
`### Required document contract N: ...` H3 subheadings normalize to 10+ tokens against an effective
bound of 5 for the one-token term `contracts`. I independently re-derived this: `grep -n "^#{2,3} "
parcels/P3-B.md` shows no bare `## Contracts`/`## Tests` heading, every contract heading is the long
`### Required document contract N: \`path\`` form, and the term `contracts` has a heading-length
bound of `min(1+4,10)=5` tokens — objectively exceeded. This is a correct, reproducible finding, not
a convenient excuse to avoid implementing stage 4 faithfully; the fixture's `expected.result` is
honestly set to the real, computed `"invalid"`/`"missing-required-section"` rather than the spec's
own narrative `"valid"` assumption, and this is disclosed (not silently substituted) in the
verifier's header comment, PR body, and commit message. D2 (narrowing check 7's cross-verifier-
agreement comparison to the one sub-computation `verify-p3b.ps1`'s own check 10 actually performs)
is the textually supported, necessary consequence of D1 and is itself genuinely and mechanically
proven (§1, check 7, above) rather than asserted.

## 7. All sixteen fixtures reproduce their expected results (live re-run)

Ran `verify-p4.ps1` live in the `p4-impl` worktree at the re-anchored `BaseCommit`:

```
pwsh -NoProfile -File docs/specs/scripts/verify-p4.ps1 `
  -BaseCommit 152ec941fe7645ffc6a196bff3e4c67896476974 `
  -BuilderId p4_builder -ReviewerIds p4_impl_review_1,p4_impl_review_2 `
  -EvidenceDirectory artifacts/p4-verification
```

Result: `P4 verification PASS`, exit 0. All 12 numbered checks recorded `pass: true` in
`artifacts/p4-verification/verification-summary.json`. `fixture-results.json` carries all 16
fixtures plus the overlay-only cross-check sub-result, every one `pass: true`:

```
positive-ticket-spec-standard-single                        valid
positive-coordinator-parcel-multilabel-fold                 valid
positive-real-spec-p3b-cross-check                           invalid / missing-required-section (D1)
positive-synthetic-capability-claim-satisfied                valid
negative-missing-required-frontmatter-field                  invalid / missing-required-frontmatter-key
negative-capability-field-missing-on-trigger                 invalid / missing-required-frontmatter-key
negative-synthetic-capability-claim-unsatisfied-twin         invalid / capability-claim-drift
negative-unknown-delivery-label-multilabel                   invalid / unknown-label
negative-invalid-status-value                                invalid / invalid-status
negative-placeholder-entity-disguised-multilabel              invalid / placeholder-violation
negative-placeholder-homoglyph-confusables                   invalid / placeholder-violation
negative-capability-claim-drift-referential                  invalid / capability-claim-drift
negative-unknown-extension-point-reference                   invalid / unknown-extension-point
negative-incompatible-controls-scalar-conflict                invalid / incompatible-controls
negative-unrecognized-shape-path                              unrecognized-shape / unrecognized-shape
negative-synthetic-empty-malformed-input                      invalid / missing-required-frontmatter-key
```

`composition-verification.json` recorded all five sources `pass: true`
(`parcel-spec.schema.json`/`statusClosedVocabulary`,
`SECTION-HEADING-MAP.md`/`rollback canonical-alias cell`,
`CAPABILITY-FIELD-MAP.md`/`pilot_capability_probe_field (new row)`,
`delivery-class-controls.json`/`standard.reviewers`,
`product-capability-safety-contract.json`/`labels.ordinary.behavior.C2.value`).
`resilience-check.json` recorded `pass: true`, `exitCode: 1`. The evidence bundle's own
`disclosedDeviations` array matches the three deviations named in the verifier's header comment and
the builder's commit message verbatim.

## 8. No TBD, hashes verify, zero governance-contract edits

- No stray `TBD`/`{{...}}` anywhere in the 20 shipped files outside the two fixtures' own
  deliberately-placed, pinned single occurrences (grep confirmed).
- Every lineage hash the spec pins was independently re-derived via `sha256sum` and matches exactly:
  charter `4CD390D6...`, P3-B spec `4E6FF312...`, P3-B closure `5C738683...` (sic, truncated in
  this table — full value matches spec), P3-A spec `4B928FBB...`, P0-B spec `4A49E3D6...`,
  `product-capability-safety-contract.json` `020554BA...`, `.md` `C01E9087...`,
  `classification-axes.schema.json` `CFCF3140...`, `delivery-class-controls.json` `E1E545CC...`.
- `git diff --name-only 152ec94...cbf6e22` (the builder's own content commit against the
  re-anchored `BaseCommit`) equals exactly the 20 allowed surfaces — zero governance-contract paths
  (`parcel-spec.schema.json`, `CAPABILITY-FIELD-MAP.md`, `classification-axes.schema.json`,
  `delivery-class-controls.json`, `fold-engine.md`, `product-capability-safety-contract.json`/`.md`,
  the charter, any closed P1/P2/P3-A/P0-A/P0-B/P3-B artifact) touched. (The wider
  `152ec94...01d73fe` merge-commit diff additionally shows `docs/INITIATIVES/
  biostack-governed-delivery/dispatch/GATE2-P4-IMPLEMENTATION.md` and
  `docs/INITIATIVES/COORDINATOR-DISPATCH-QUEUE.md` changed — both are coordinator-authored commits
  (`fdb83ee`/`baf4b46`) on `main`'s own history outside the builder's branch, not part of the
  builder's diff; `verify-p4.ps1`'s own check 3, which correctly scopes to `BaseCommit...HEAD` where
  `HEAD` is the builder's own content commit, passed live and did not flag these.)
- `README.md`/`INDEX.md` diffs are exactly one appended section and one appended row respectively,
  zero removed/reordered lines (confirmed by direct diff, §above).

## Verification notes

**What I checked and found clean:** full line-by-line read of both shipped scripts (2107 lines
combined); live execution of `verify-p4.ps1` against the correct, re-anchored `BaseCommit` in the
existing `p4-impl` worktree (all 12 checks + final clean-tree check PASS); independent re-derivation
of every pinned lineage SHA-256; independent re-derivation of the two capability-claim-drift live
contract cells (`ordinary`/C2 = `allowed`, `minor-or-age-uncertain`/C2 = `degraded`); independent
re-derivation of the D1 heading-length-bound finding against the real `parcels/P3-B.md` headings;
seven independently constructed adversarial probes against the real `validate-spec.ps1` (spliced
quotes, merged-clause heading, malformed JSON, empty JSON, nonexistent path, plus re-confirmation of
the two shipped homoglyph/entity fixtures), all rejected correctly; byte-diff of the positive/
negative capability-claim twin fixtures confirming single-field divergence; exact allowed-surfaces
diff match; `README.md`/`INDEX.md` bounded-diff confirmation; no stray placeholder markers.

**What I did not find:** no vacuous check, no nonconforming spec that passes `verify-p4.ps1`, no
unrejected named evasion class, no governance-contract edit, no hash mismatch, no unresolved
placeholder outside the two pinned fixture occurrences, no TBD.

**MINOR, non-blocking observation (recorded for forward record, not a P4 defect):** an empty
`capability_claim: []` array (present but zero entries) on a spec with non-empty
`guidance_classes`/`substance_function_risk` passes stage 7 vacuously in both `validate-spec.ps1`
and the frozen, already-ratified `verify-p3b.ps1` it is faithfully composed from — the per-entry
loop never executes, so no live cross-reference is attempted and no downstream field (`missingness`,
`numeric_provenance`, `escalation`) is triggered either. This is inherited, byte-for-byte-equivalent
behavior from P3-B's own frozen binding logic (`verify-p3b.ps1` lines 476-483), not a defect P4
introduces, and P4's own Hard constraints ("No product-semantic decision... this parcel invents no
new... rule") and Frozen surfaces forbid it from inventing a non-emptiness rule P0-B/P3-B never
themselves ratified. I record this for a future P3-B amendment or P6/P7 consideration; it does not
change this review's verdict.

**Why PASS, not PASS-WITH-FIXES:** every acceptance criterion (AC-P4-01 through AC-P4-10) is met,
verified both by reading the implementation and by live execution; the three disclosed deviations
are each independently reproducible, honestly handled, and correctly disclosed rather than papered
over (matching this initiative's own established D-L precedent for exactly this situation); the one
MINOR observation above is an inherited, out-of-scope-to-fix characteristic of already-frozen,
already-ratified prior art, not a defect this parcel's own builder introduced or could have fixed
without violating its own spec's Hard constraints. I find no amendment this review needs to request.
