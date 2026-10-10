# p3a_spec_review_2 — P3-A spec review

**Verdict:** REJECT
**Spec file:** docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md
**Spec SHA-256:** 901a4cc086ca887f36aff9ede032a176b3e197497c9ba1724fe61f2e04c40a32
**Date:** 2026-10-08

## Ranked findings

### F1 (BLOCKER) — The headline anti-evasion mechanic (disguised-`TBD` detection) is unprovable and unenforced by any check or fixture; a builder can ship a stub and pass everything.

**Claim:** The spec devotes an entire hard-constraint paragraph and check 12 to asserting that the
`placeholderNormalizationSteps` pipeline, including a full "Unicode Technical Standard #39
confusables-skeleton transform," "is not optional or informative-only text" and "exists
specifically to defeat homoglyph substitution..., zero-width-character insertion, soft hyphens,
fullwidth forms, and mathematical-alphanumeric disguises," and markdown/HTML-syntax splitting.

**Evidence:** Document contract 1 (P3-A.md, "Hard constraints" section and the
`placeholderNormalizationSteps` block) and check 12 (`## Deterministic verification`, item 12).
Document contract 6 defines exactly three negative fixtures
(`negative-tbd-violation.json`, `negative-missing-required-field.json`,
`negative-unknown-extension-point.json`). `negative-tbd-violation.json`'s own spec text says it is
"a `syntheticSpec` whose body contains the literal substring `TBD` outside any sanctioned-marker
scope" — a **literal**, undisguised case. No fixture in document contract 6, and no numbered check
(1–14) in "Deterministic verification," ever feeds a homoglyph, zero-width, soft-hyphen,
fullwidth, mathematical-alphanumeric, or markdown/HTML-split disguised `TBD` string through the
validator and asserts it is caught. Check 12 only scans "every file changed or added by this
parcel" — i.e., the builder's own deliverables, which the builder fully controls and has no reason
to deliberately poison with a disguised placeholder (doing so would simply make their own build
red). There is no surface in the 27 allowed surfaces dedicated to a bundled UTS#39 confusables data
table either (`verify-p3a.ps1` is the only script surface, item 25); a faithful UTS#39
confusables-skeleton table has thousands of entries, so either the whole table must be crammed
inline into one PowerShell file with no check verifying its completeness/fidelity against the
authoritative source, or (more likely) the builder implements a token stub (e.g., a regex covering
only the two example characters the spec quotes, Cyrillic `Т`/`В`) and calls it the "confusables
transform."

**Why this is the letter-vs-spirit evasion the reviewer brief asks for:** A builder can implement
`unicode-confusables-skeleton` as a near-no-op (or skip Unicode normalization logic entirely,
implementing only steps that happen to be unreachable in the parcel's own self-authored files),
and still score 100% on AC-P3A-01 through AC-P3A-11, because no acceptance criterion or check ever
exercises the pipeline against an adversarial disguised input. The spec's own prose claims this
"closes" a security-relevant loophole; nothing in the deterministic verification proves it.

**Smallest amendment:** Add at least 4–6 additional fixtures to document contract 6 (and checks
9/12) that are explicitly disguised-`TBD` cases — one per disguise class named in the hard
constraint (homoglyph, zero-width, fullwidth, math-alphanumeric, HTML-comment-split,
inline-code-split) — each with `expected.result: "invalid"`, `expected.reason:
"placeholder-violation"`, proving the pipeline's actual behavior rather than only its presence.

**Changes a locked decision?** No — it strengthens an existing requirement's verifiability; it
does not alter D1–D18, the charter, or any frozen P1/P2 artifact.

---

### F2 (MAJOR) — Document contract 4's content-quality obligations for templates ("instructive
prose," non-blank heading bodies, the "Extension points used" section) are asserted in prose but
never checked by any numbered check or acceptance criterion.

**Claim:** Document contract 4 requires each template heading's "body containing instructive prose
plus a `[REPLACE: ...]` marker ... never a blank heading," and requires "one additional
`## Extension points used` section, pre-filled to the literal sentence 'None.'"

**Evidence:** Check 8 (`## Deterministic verification`, item 8) only (a) parses frontmatter keys,
(b) checks `delivery_classes`/`guidance_classes`/`substance_function_risk` values, (c) substitutes
`[REPLACE: ...]` markers and runs the fold-live heading resolver, and (d) scans for leftover
placeholder literals. It never asserts a heading's body is non-empty beyond containing the
`[REPLACE:...]` marker itself (which trivially satisfies "non-blank" even if the entire body is the
four-word marker and nothing else), and it never checks for the presence of an
`## Extension points used` section or its literal content. AC-P3A-04 only requires "each template
independently resolves to `valid`" and the distinct/length-bounded heading resolution — it is
silent on body content and the `Extension points used` section. A builder can therefore ship eight
templates whose every required heading's entire body is exactly `[REPLACE: fill this in]` with no
instructive prose at all, and omit "Extension points used" entirely from every template, and still
pass every deterministic check and AC-P3A-04/05 cleanly — directly contradicting document contract
4's description of a "fully instantiable... scaffold."

**Smallest amendment:** Add a check-8 sub-rule requiring each resolved heading's body (after
marker substitution) to contain a minimum non-marker token count (e.g., ≥8 words outside the
`[REPLACE:...]` span), and a check requiring literal presence of a `## Extension points used`
heading with body text equal to `None.` in every template; wire both into AC-P3A-04.

**Changes a locked decision?** No.

---

### F3 (MAJOR) — Carry-over item 1 and document contract 9 cite a frozen-artifact fact
(`INDEX.md`'s P2 row using the literal `coordinator-assigns-at-gate-2`) that is false against the
actual current, frozen `docs/specs/INDEX.md`.

**Claim:** "using the closed-vocabulary registry cell literal `coordinator-assigns-at-gate-2`...
(the same literal P2's own row used, continuing the practice P2 itself started..." (Deliverable 9,
and repeated in document contract 9 and Carry-over item 1).

**Evidence:** `docs/specs/INDEX.md` line 3 (the only P2 row... actually verified row for P2 at
the bottom of the current table) reads `[Gate 2 record](...)` for Branch/worktree and
`[p2_builder](...)` for Owner — **not** the literal string `coordinator-assigns-at-gate-2`.
`grep -n "coordinator-assigns-at-gate-2" docs/specs/INDEX.md` returns zero matches in the current
repository state. The literal only ever appears in `parcels/P2.md` (lines 392–394, 527) as what
P2's *spec* said its row would carry at Gate 2 dispatch time, before the Gate 2 record resolved
real builder/branch identities — i.e., it was a transient dispatch-time placeholder, already
superseded in the closed, frozen registry artifact by the time P2 closed. P3-A's Carry-over
section and AC-P3A-11 present this as still-standing, citable evidence inside a frozen surface; a
reviewer attempting to verify "the same literal P2's own row used" against `INDEX.md` as it
actually exists today cannot do so — the claim is only true of P2.md's prose, not of the frozen
registry state P3-A points to for precedent.

**Why this matters for reviewability:** AC-P3A-11 requires Carry-over items to have "a named,
evidenced disposition... not a bare restatement." This item's own evidence citation
mischaracterizes the current content of a frozen, already-closed artifact — the exact failure mode
("frozen-surface integrity: does the spec cite a stale/wrong... fact") this review is charged to
find.

**Smallest amendment:** Correct the citation to say the literal was P2's *spec-declared intent at
dispatch time* (citing `parcels/P2.md` lines 392–394/527), not a still-present fact of the current
`INDEX.md`, and note explicitly that P3-A's own new row carries the same transient, Gate-2-pending
semantics and is expected to be similarly superseded at this parcel's own closure.

**Changes a locked decision?** No.

---

### F4 (MAJOR) — `EXTENSION-POINTS.md`'s `domain-overlay-insertion` mechanic pre-designs the shape
of P3-B's future capability-binding before P0-B freezes the Product Capability and Safety Contract,
risking exactly the boundary the charter reserves to P3-B.

**Claim:** The charter's P3 split states P3-A "must not invent product capability semantics" and
is explicit that binding "required capability, claim, provenance, missingness, function-review, and
escalation fields" is P3-B's territory "after P0-B freezes the Product Capability and Safety
Contract." P3-A's own Frozen-surfaces section repeats: "P3-A must not bind required capability...
field to the schema — that is P3-B's territory."

**Evidence:** Document contract 3's `domain-overlay-insertion` mechanic specifies: "A future parcel
operating under its own approved spec may append a new named key to this object (for example a
guidance-class label) **binding it to an additional required-section term**." This pre-decides the
*topology* of the eventual binding — one extension-section key maps to exactly one additional
required-section term, via a flat, non-conditional, single-axis registry — before P0-B has defined
what the Product Capability and Safety Contract actually needs (which, per the charter, may require
conditional sections, multi-section bindings, or cross-axis interaction between guidance class and
substance/function risk, none of which this flat one-key-to-one-term registry can express without
redesigning it). Because `EXTENSION-POINTS.md` is itself a frozen P3-A surface once shipped (only
additive, non-redesigning edits are sanctioned per the pinned invariant sentences in check 7), if
P0-B's eventual contract needs a richer binding shape, P3-B is forced either to violate the
"additive-only, no redesign" guarantee this extension point makes, or to constrain the Product
Capability and Safety Contract to fit a structural assumption P3-A (not P0-B) made first. This is a
genuine pre-emption of P3-B/P0-B's design space disguised as "bounded, additive mechanics only" —
textbook extension-point abuse under this review's explicit focus area.

**Smallest amendment:** Either (a) narrow the `domain-overlay-insertion` mechanic's committed shape
to state explicitly that the one-key-to-one-term topology is a non-binding illustrative default
and that P0-B/P3-B may redesign this specific extension point's registry shape without that being
treated as a "frozen surface" violation, or (b) defer specifying the binding topology itself to
P3-B/P0-B and let P3-A define only the *existence* of the seam (`extensionSections: {}`), not its
internal shape.

**Changes a locked decision?** No — it is a request to avoid implicitly deciding something the
charter already locked to a later parcel; it does not itself reopen D1–D18.

---

### F5 (MINOR) — Document contract 1 misattributes the snake_case frontmatter-key mapping to
`classification-axes.schema.json` itself, when the JSON artifact only contains camelCase.

**Claim:** "`requiredFrontmatterKeysCommonToBothShapes` uses exactly the snake_case keys P2's
`classification-axes.schema.json` already pins as the authoritative frontmatter-key mapping for
`delivery_classes`/`guidance_classes`/`substance_function_risk`."

**Evidence:** `docs/specs/schemas/classification-axes.schema.json` contains only camelCase
`fieldName` values (`deliveryClasses`, `guidanceClasses`, `substanceFunctionRisk` — verified by
`grep -n "fieldName"` across the file; zero snake_case occurrences). The actual snake_case mapping
is documented only in `parcels/P2.md` prose (document contract 1, lines 189–196: "The corresponding
**frontmatter key**... is the snake_case form... `fieldName` pins the fold-input key; the
frontmatter-key mapping above is the authoritative camelCase-to-snake_case correspondence"). P3-A's
parenthetical "(document contract 1 of P2's spec)" is technically correct, but the leading clause
("P2's `classification-axes.schema.json` already pins") attributes the pin to the wrong artifact —
a reviewer or future automated composer reading only the cited JSON file (as the "composed, not
hardcoded, read live" discipline elsewhere in this spec instructs) would find no snake_case mapping
there at all.

**Smallest amendment:** Reword to "...the snake_case keys P2's spec (document contract 1 of
`parcels/P2.md`) already pins as the authoritative camelCase-to-snake_case correspondence for the
fold-input keys `classification-axes.schema.json` declares" — crediting the prose source, not the
JSON file.

**Changes a locked decision?** No.

---

### F6 (MINOR) — `BaseCommit` anchor staleness: the pinned single anchor
(`main@6f46310a6113fae60805feeb65cac50d6b847ba3`) already predates at least one file
(`docs/specs/active/BIO-FE-002-onboarding-route-consolidation.md`) that exists on current `main`,
confirming the "26-file"/27-row compatibility-set scope is correct *only* at the pinned anchor and
will already be stale relative to live `main` by dispatch time.

**Evidence:** `git ls-tree -r --name-only 6f46310a6113fae60805feeb65cac50d6b847ba3 -- docs/specs/active docs/specs/done`
returns 27 paths (25 `.md` + 2 `.shaping-result.json`, excluding the two READMEs = 26 — matching
`AXIS-REGRESSION-MAP.md`'s 26-row table and P3-A's "26-file"/"27-row-minus-2-READMEs" claims). The
same command against the live worktree (`git ls-files docs/specs/active docs/specs/done`) returns
29 paths, including `BIO-FE-002-onboarding-route-consolidation.md`, which is absent at the pinned
anchor and also has no corresponding row anywhere in current `docs/specs/INDEX.md`. This confirms
the 26-file claim is internally consistent *at the pinned anchor* (not a bug in the arithmetic), but
flags that other, apparently uncoordinated spec work has already landed on `main` since P2 closed,
outside this governed-delivery chain's visibility — a process risk for whoever cuts P3-A's isolated
worktree from the stated single anchor, and a reconciliation item neither P3-A nor any later parcel
in its dependency spine currently names.

**Smallest amendment:** Add one sentence to "Lineage and dependencies" naming that `main` has
diverged since the `BaseCommit` anchor (citing `BIO-FE-002` by path) and that this file is outside
P3-A's scope and will need coverage by a future compatibility-set refresh.

**Changes a locked decision?** No.

---

### F7 (MINOR) — `placeholderNormalizationSteps` steps 4–5 (strip all `*`/`_`/`\` globally) are
maximally aggressive, context-free deletions that can manufacture false `placeholder-violation`
results on real corpus text containing underscore- or backslash-adjacent substrings that merely
happen to collapse into a banned token, undermining the `Reason` column's accuracy promise in
`REAL-SPEC-COMPATIBILITY-SET.md`.

**Evidence:** Document contract 1, steps 4 and 5: "removes every `*` and `_` character used as
Markdown emphasis delimiters" and "removes every literal backslash (`\`) character" — both are
specified as blanket character-class strips over the *entire* document text, not scoped to
plausible emphasis/escape contexts (e.g., paired delimiters, backslash-followed-by-punctuation).
Document contract 7 requires `Reason` to "name the specific failing check," implying fidelity; a
corpus file containing, say, a literal identifier or path fragment that collapses to a banned
3-letter sequence only because unrelated underscores/backslashes elsewhere in the same line were
stripped would be misreported as a genuine `placeholder-violation` rather than a normalization
artifact.

**Smallest amendment:** Scope steps 4–5 to paired/contextual markers (matched `*`/`_` runs, or
backslash immediately followed by ASCII punctuation) rather than unconditional global character
deletion, or note explicitly in document contract 7 that `Reason: placeholder-violation` on
corpus rows may reflect a normalization artifact and is not dispositive of a real unresolved
decision.

**Changes a locked decision?** No.

## Missing pieces / collisions / unknowns

- No fixture or check proves the `unicode-confusables-skeleton` transform (UTS #39) is
  implemented faithfully rather than as a token stub covering only the two example characters
  quoted in the spec's own prose (see F1). This is the single largest gap between what this parcel
  claims to deliver and what its deterministic verification can actually prove.
- Document contract 4's "instructive prose" and "Extension points used" requirements have no
  enforcement path (F2); a minimal, low-effort builder interpretation of the templates deliverable
  would technically pass while producing scaffolds a human author could not actually use without
  reverse-engineering `SECTION-HEADING-MAP.md` and `delivery-class-controls.json` themselves.
- `BIO-FE-002-onboarding-route-consolidation.md` exists on current `main` with no `INDEX.md` row
  and is invisible to this parcel's frozen-surface and compatibility-set scope because it postdates
  the pinned `BaseCommit` (F6) — worth coordinator awareness even though it is not a P3-A defect.
- Carry-over item 1's load-bearing evidence citation does not hold against the current, actual
  `INDEX.md` (F3) — this weakens the self-asserted "named, evidenced disposition" standard
  AC-P3A-11 claims to meet for all three carry-over items; items 2 and 3 were not independently
  re-verified against repository state in this review (time-bounded) and should be spot-checked by
  the coordinator before dispatch.

## Verification notes

- Confirmed by SHA-256: charter (`4cd390d6...`), plan-review (`fbe40053...`), `parcels/P1.md`
  (`a55235e8...`), `parcels/P2.md` (`527d930f...`), `closures/P2.md` (`cc5a4ee1...`) all exactly
  match the hashes P3-A cites in "Lineage and dependencies." No lineage-hash discrepancy found.
- Confirmed the "46 distinct `requiredSpecAdditions` terms" arithmetic: 49 raw entries across the
  eight classes in `delivery-class-controls.json`, minus 2 duplicate `rollback` occurrences
  (standard/migration/knowledge-promotion → 1 distinct) minus 1 duplicate `retention` occurrence
  (privacy/provider-pilot → 1 distinct) = 46. Matches the spec's informative claim exactly.
  Clean.
- Confirmed `docs/specs/INDEX.md`'s current header row is exactly the ten columns P3-A's document
  contract 9 assumes (`Parcel | Status | Spec | Goal Charter | Delivery classes | Guidance classes
  | Branch/worktree | Owner | Review requirement | Closure`). Clean.
- Confirmed the pre-existing ad hoc `TBD (coordinator to assign Gate 2)` / `[TBD]` cells Carry-over
  item 1 names actually exist on the `BIO-PAIRWISE-00x` rows of the current `INDEX.md`. Clean
  (this specific sub-claim is accurate).
- Did not find any edit to a frozen surface's actual content proposed anywhere in P3-A's
  deliverables or allowed-surfaces list; the 27-surface enumeration and frozen-surfaces list appear
  mutually exclusive and jointly exhaustive against what the charter/P1/P2 own.
- Did not independently execute or hand-simulate the fold-live resolver against all eight templates
  or all twelve fixtures (no builder artifacts exist yet to inspect — this is a spec-only review of
  a review-candidate with no implementation); findings above are about the *spec's* own internal
  completeness and citation accuracy, not about a specific builder run's output.
