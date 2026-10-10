# p3a_spec_review_1 — P3-A spec review

**Verdict:** REJECT
**Spec file:** docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md
**Spec SHA-256:** 901a4cc086ca887f36aff9ede032a176b3e197497c9ba1724fe61f2e04c40a32
**Date:** 2026-10-09 (repository-relative; see cited commit timestamps)

## Ranked findings

### F1 — BLOCKER — Document contract 1's "exactly these top-level keys" list omits `extensionSections`, which checks 7/AC-P3A-03 then require on the same file

**Claim:** `parcel-spec.schema.json`'s own authoring contract is internally contradictory: one
check requires the file to have *exactly* a 14-key closed set, and a separate check requires an
additional 15th key to also be present on that same file. A builder cannot satisfy both as
literally written, and two honest builders would diverge (one drops `extensionSections` to satisfy
check 5's "exactly," the other adds it to satisfy check 7/AC-P3A-03, each failing the other check).

**Evidence:**
- Document contract 1 (lines 225–~266) opens "A single JSON object with exactly these top-level
  keys:" followed by a fenced JSON block whose top-level keys are exactly: `schema`, `specShapes`,
  `requiredFrontmatterKeysCommonToBothShapes`, `statusClosedVocabulary`, `axisSource`,
  `controlSource`, `foldEngineSource`, `sectionHeadingMapSource`, `extensionPointsSource`,
  `requiredSectionDerivation`, `noPlaceholderPatterns`, `sanctionedTemplateFillInMarker`,
  `sanctionedTemplateFillInMarkerScope`, `placeholderNormalizationSteps` — 14 keys. No
  `extensionSections` key appears anywhere in that block.
- Deterministic verification check 5 (line 649): "Parse `parcel-spec.schema.json`; require exactly
  the top-level keys pinned in document contract 1; ..." — an explicit cardinality ("exactly")
  bound to the 14-key list above.
- Deterministic verification check 7 (lines 660–662): "...require `parcel-spec.schema.json`'s
  `extensionSections` key to be present and equal to an empty object `{}`."
- AC-P3A-03 (line 581): "...the schema file's `extensionSections` registry object is present and
  empty at this parcel's shipped hash."
- Document contract 3 (`EXTENSION-POINTS.md`, line 420) independently asserts `extensionSections:
  {}` must be "present in the schema file as an explicit empty object."

Three independent places (check 7, AC-P3A-03, document contract 3) require a 15th top-level key
that document contract 1's own "exactly these top-level keys" enumeration, and check 5's "exactly"
cross-check against it, do not include. This is not a stylistic gap — it is a direct, checkable,
self-contradiction inside the single most load-bearing deliverable (the schema file itself), and it
is the kind of defect dual adversarial review exists to catch before dispatch.

**Smallest amendment:** add `"extensionSections": {}` to the fenced JSON block in document contract
1 (making it 15 pinned keys), and have check 5 assert exactly those 15 keys (dropping the need for
check 7's separate presence assertion, or keeping it as a value-equality check only, not a
presence check).

**Does it change a locked decision?** No — it corrects an internal inconsistency within P3-A's own
new deliverable; it does not touch any frozen/charter/P1/P2 content.

---

### F2 — BLOCKER — The claimed closure of "markdown/HTML-syntax-splitting evasion" (rework round 2) does not cover HTML character-reference (entity) splitting, a well-known, equally trivial bypass

**Claim:** AC-P3A-06 and the hard-constraints section assert, in absolute language, "a disguised
`TBD`, however disguised, is exactly as invalid as a literal one" after the pinned
`placeholderNormalizationSteps` pipeline runs. This claim is false as written: an author can write
`T&#66;D` (or `T&#x42;D`, or spread across all three letters) in a Markdown/HTML document. Per the
CommonMark/GFM spec (which `docs/specs/active/*.md` and `docs/specs/done/*.md` are rendered under,
and which the "renders visually as TBD" threat model this pipeline targets is explicitly built
around — see the chosen examples `T<!--x-->BD`, `T<span></span>BD`, which are only threats because
they *render* as "TBD"), HTML/numeric character references are decoded to their literal character
anywhere in text, not only inside raw HTML blocks — so this disguise renders as the literal
characters "TBD" to any human or tool consuming the rendered document, while the raw source text
never contains the contiguous substring `TBD`.

**Evidence:**
- Pinned pipeline (document contract 1, lines ~280–322) lists exactly eight steps: strip HTML
  comments, strip HTML/XML tags, strip inline-code backtick delimiters, strip Markdown emphasis
  delimiters, strip Markdown escape backslashes, strip Unicode `Cf`/`Cc`, NFKC-normalize,
  confusables-skeleton. None of the eight decodes or strips HTML entity/numeric-character
  references (`&...;`, `&#NN;`, `&#xNN;`).
- `strip-html-tags`'s own pinned regex (line 282): `</?[a-zA-Z][a-zA-Z0-9]*(?:\s[^>]*)?>` — matches
  only `<tag>`/`</tag>` forms; `&#66;` contains no `<`/`>` and is untouched by this or any other
  step.
- The hard-constraints section (around "No `TBD`.") states the pipeline "exists specifically to
  defeat ... markdown/HTML-syntax splitting of the banned literal ... a disguised `TBD`, however
  disguised, is exactly as invalid as a literal one" — an absolute claim this construction falsifies.
- Git history confirms this is precisely the class of finding rework round 2 (commit `1a5a326`,
  "P3-A rework 2 — markup-splitting evasion closed in normalization") was dispatched to close, and
  it enumerated HTML comments, tags, backticks, emphasis, and escape-backslash splitting — but not
  entity/character-reference splitting, leaving the stated closure incomplete.

**Smallest amendment:** add a ninth pinned normalization step, `decode-html-entities` (applying the
standard named/decimal/hex HTML5 entity decode table, offline, deterministic, no network), ordered
before `strip-unicode-category-Cf-and-Cc`, and update check 12 and the `negative-tbd-violation`-
style reasoning to match. Also re-run the real-spec compatibility set and all fixtures, since this
changes the normalized-text computation.

**Does it change a locked decision?** No — it strengthens an already-pinned P3-A-owned mechanism;
it touches no frozen P1/P2 surface.

---

### F3 — MAJOR — The anti-heading-soup "one-to-one bipartite match" is under-specified: it does not pin a deterministic resolution when a required term has *no* matching maximum matching that covers it among several structurally different maximum matchings, risking non-reproducible `Reason`/evidence between two independent, correct implementations

**Claim:** Document contract 2 says: "The resolver assigns headings to terms via a one-to-one
bipartite match (each heading used at most once); if the only headings that textually match a
given term are already consumed by other terms' matches, that term resolves `satisfied: false`."
This correctly makes the aggregate valid/invalid verdict well-defined (valid iff a perfect matching
covering every required term exists — an existence property, not a choice), but it does **not**
pin which specific term(s) are reported unsatisfied, or which specific heading occurrence is
attributed to which term, when the real-world graph has multiple maximum (non-perfect) matchings of
equal size that leave different term nodes uncovered. Two conformant implementations (e.g. the
builder's `verify-p3a.ps1` today, and P4's future general linter reimplementing this same document
contract, per the explicit "every future ... validates against" / reuse language in the Objective
section) could legitimately choose different maximum matchings and report a different `Reason`
(which term is "missing") for the same real-spec input, even though both report `invalid`.

**Evidence:**
- Document contract 2, "Anti-heading-soup constraints," item 1 ("Distinct-heading-per-term"):
  pins only that the match is "one-to-one," not an algorithm, tie-break order, or priority rule for
  choosing among multiple maximum matchings.
- Check 9 and check 10 require the recorded `Reason` to equal "what the validator independently
  produces" (check 10) — this is self-consistent only because there is one actual implementation;
  the spec's own stated reuse/portability goal (Objective section: "...a contract every future
  parcel spec and ticket-level spec is *defined* to validate against...") implies a second honest
  implementation (P4) must be able to reproduce the same `Reason`, which the text as written does
  not guarantee for ambiguous real-corpus documents.
- `REAL-SPEC-COMPATIBILITY-SET.md`'s `Reason` column (document contract 7) is exactly the surface
  where this ambiguity would silently manifest, against real (not synthetic, carefully
  single-interpretation) documents.

**Smallest amendment:** pin a deterministic tie-break for the bipartite resolver, e.g. "assign
headings to terms in fixed term order (the order each term is introduced in the live
`requiredSpecAdditions` union for the declared class set, ties broken by document order of
candidate headings), via a deterministic augmenting-path algorithm; a term is `satisfied: false`
only if no augmenting path exists for it under this fixed assignment order" — or explicitly scope
`Reason` as "named term(s) known-unsatisfiable" only when the *aggregate* result is invalid and
state that the identity of a reported term among multiple equally-valid maximum matchings is
implementation-defined and non-normative.

**Does it change a locked decision?** No.

---

### F4 — MAJOR — The "single-anchor dispatch model" anchor cited in Lineage is already stale relative to current `main`, and the spec's own language does not clearly state whether it is frozen or must be re-pinned at actual Gate 2 time

**Claim:** The spec names `main@6f46310a6113fae60805feeb65cac50d6b847ba3` as "the Reconciled
shaping base anchor" and states "`BaseCommit` below is that single anchor" (Lineage and
dependencies). Since that commit was recorded, `main` has advanced by dozens of commits, including
at least one new file added under `docs/specs/active/` (`BIO-FE-002-onboarding-route-
consolidation.md`, first committed at `fadb968`, confirmed absent from tree `6f46310`) and
substantial unrelated coordinator/design-gate activity for P0-A. The spec does not say whether the
literal named SHA is the immutable anchor the builder must use regardless of elapsed drift, or
whether the coordinator is expected to re-derive a fresh "commit that most recently touched the
registry/P2 substrate" anchor at actual Gate 2 time (which would make the "single anchor" claim in
Lineage purely illustrative, not binding) — the single-anchor model's entire stated purpose (per P2
precedent, reused verbatim here) is to avoid exactly this kind of shaping-to-dispatch drift.

**Evidence:**
- `docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md`, Lineage and dependencies: "
  Reconciled shaping base anchor: `main@6f46310a6113fae60805feeb65cac50d6b847ba3` (the commit that
  closed P2)." / "...the builder branch/worktree starts at that exact commit (`BaseCommit` below is
  that single anchor)."
- `git log --oneline main` shows current `main` HEAD (`ca7b11b` at review time) is dozens of commits
  ahead of `6f46310`, including P0-A design-gate commits (`4be1c00`, `ca7b11b`) dated after P2's
  close.
- `git show 6f46310:docs/specs/active/BIO-FE-002-onboarding-route-consolidation.md` fails (file
  absent); `git ls-files docs/specs/active docs/specs/done` at current HEAD returns 29 paths (27
  excluding the two READMEs) versus the 26-row set `AXIS-REGRESSION-MAP.md` and this spec's
  document contract 7 both describe as the fixed real-spec compatibility set "at `BaseCommit`."

**Smallest amendment:** state explicitly in Lineage that `6f46310` is the anchor *as observed at
shaping time*, and that the coordinator must re-verify/re-pin `BaseCommit` to the actual registry
HEAD at Gate 2 creation (not reuse a stale shaping-time SHA), with the real-spec compatibility set
scoped to whatever file set exists at that re-pinned anchor — removing the ambiguity about which of
the two behaviors ("frozen forever" vs "re-pinned at dispatch") governs.

**Does it change a locked decision?** No — it clarifies an ambiguity in this parcel's own dispatch
mechanics; it does not alter the charter's dependency spine or P1/P2 content.

---

### F5 — MINOR — `EXTENSION-POINTS.md`'s `domain-overlay-insertion` worked example ("for example a guidance-class label") sits close to, but does not cross, the P3-B capability-semantics line

**Claim:** Document contract 3's `domain-overlay-insertion` mechanic description uses "a future
parcel operating under its own approved spec may append a new named key to this object (for example
a guidance-class label) binding it to an additional required-section term" as its illustrative
example. This is the exact shape of binding the charter reserves for P3-B/P0-B. The spec's
repeated non-binding disclaimers ("adds no required section... by existing; it only becomes active
when a future parcel's own approved spec uses it") correctly keep this inert today, so this is not
a violation, but the specific choice of "guidance-class label" as the illustrative example (rather
than a neutral placeholder label) invites a reviewer or future implementer to treat the association
between extension points and guidance classes as pre-decided rather than merely illustrative.

**Evidence:** `docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md`, document contract 3,
`domain-overlay-insertion` bullet (around line 417–431).

**Smallest amendment:** replace the illustrative example with a domain-neutral placeholder (e.g.
"a future domain label, to be determined by that future parcel's own approved spec") to remove any
appearance of pre-committing to guidance-class binding ahead of P0-B/P3-B.

**Does it change a locked decision?** No.

## Missing pieces / collisions / unknowns

- F1 (the `extensionSections` key contradiction) must be resolved before any builder can produce a
  schema file that passes both check 5 and check 7/AC-P3A-03 simultaneously; as written, the parcel
  is not buildable to a PASS state.
- F2 means the "closes... the markdown/HTML-syntax-splitting evasion class" claim that justified
  closing the prior reviewer finding in rework round 2 is not fully true; the finding this parcel
  claims to have resolved should be treated as still partially open.
- The `coordinator-parcel`-shape testing gap (Carry-over item 3) is honestly disclosed and pushed to
  P3-B with a named obligation — this is good practice and not a finding, but it does mean
  AC-P3A-11's "all three carry-over items satisfied" claim for item 3 is really "explicitly
  deferred with a tracked obligation," not "satisfied," and the acceptance criterion's phrasing
  ("have a named, evidenced disposition... not a bare restatement") should be read as accepting
  deferral-with-obligation as satisfying, which this reviewer accepts as consistent with the text,
  but flags because a future closure reviewer could read AC-P3A-11 more strictly.
- No frozen-surface or stale-hash problems were found: every cited hash (charter, plan-review, P1,
  P2, closures/P2) matches the current repository content exactly.
- The 46-distinct-term count cited for `SECTION-HEADING-MAP.md` is correctly computed from the live
  `delivery-class-controls.json` (independently recomputed: 46) and is explicitly marked
  "informative context only," not a hardcoded enforcement value — this is correctly done.
- The INDEX.md ten-column append rule (document contract 9) matches the live `docs/specs/INDEX.md`
  header exactly (`Parcel | Status | Spec | Goal Charter | Delivery classes | Guidance classes |
  Branch/worktree | Owner | Review requirement | Closure`), and the `coordinator-assigns-at-gate-2`
  literal matches the same literal P2's own spec (and `verify-p2.ps1`) used at its review-candidate
  stage — this part of the spec is accurate (f) as written, independent of the staleness concern in
  F4.
- The P3-A/P3-B capability-semantics split, dependency spine prefix
  (`plan-review closure -> P1 -> P2 -> P3-A -> P0-A -> P0-B -> P3-B -> ...`), and D8 dual-review
  citation all match the charter verbatim (charter lines 131–133, 152; D8 at line 93).

## Verification notes

- Computed SHA-256 of the reviewed spec file directly; recorded above.
- Cross-checked all five cited canon hashes (charter, plan-review, closed P1, closed P2,
  closures/P2) against current repository file contents: all match exactly (no stale/forged hash).
- Independently recomputed the live union of `requiredSpecAdditions` across all eight classes in
  `docs/specs/schemas/delivery-class-controls.json`: 46 distinct terms, matching the spec's
  informative citation.
- Read `docs/specs/schemas/fold-engine.md` in full and confirmed P3-A's description of
  `union-set`/`max-scalar`/`intersection-boolean` fold mechanics, stop-reason vocabulary
  (`unknown-label`, `empty-required-axis`), and guidance-class/substance-risk pass-through stance
  are accurate restatements, not redefinitions.
- Read `docs/specs/schemas/classification-axes.schema.json` and confirmed the eight delivery-class
  labels match the charter and P3-A's own references; confirmed `elevated` (used by legacy
  `BIO-LOCAL-003/004/005/...` rows in `docs/specs/INDEX.md`) is correctly *not* in the closed
  vocabulary, which P3-A's own compatibility-set design already anticipates as an expected
  non-conforming legacy case, not a defect.
- Verified `docs/specs/README.md` and `docs/specs/INDEX.md` current structure against document
  contract 9's append rules: column order, cell counts, and literal values all consistent.
- Walked the full git history of the spec file (`dd451ae` author, `199df76` rework 1, `1a5a326`
  rework 2) and read the full diff of rework 2 to confirm exactly which markup-splitting vectors
  were and were not added — basis for F2.
- Did not read any other reviewer's output file, per independence instructions.
- Did not execute, edit, or create any file other than this review output.
