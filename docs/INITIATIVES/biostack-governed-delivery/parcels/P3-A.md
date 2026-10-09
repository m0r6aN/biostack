# P3-A — Generic Extensible Parcel Schema Parcel Spec

Status: **REVIEW CANDIDATE — builder dispatch blocked**

Parcel ID: `P3-A`

Risk and routing: `standard` (self-declared delivery class, folded against
`delivery-class-controls.json`), architecture (charter parcel-tree designation); one builder; two
independent read-only adversarial reviewers (charter D8: architecture parcels receive dual review
regardless of the folded delivery class's baseline reviewer count — the same combination P1 and
P2 both carried).

## Lineage and dependencies

- Governed-delivery charter SHA-256: `4CD390D631487DA8E97509205A186F242C4A83B762FFFA894BEEC8A21F4EFE22`
  (unchanged since P1 and P2; re-verified at this shaping time).
- Closed plan review: `docs/INITIATIVES/biostack-governed-delivery/PLAN-REVIEW.md`, SHA-256
  `FBE40053DAE9508AC498B1331D2AED565A4BB68025CA24E07E0DC7BB69E8E256`.
- Closed P1 spec: `docs/INITIATIVES/biostack-governed-delivery/parcels/P1.md`, approved hash
  `A55235E81FF251B7A67620917522B9173F697D9776336C98D03B441F366C22BA`. Closure:
  `docs/INITIATIVES/biostack-governed-delivery/closures/P1.md` — status `DONE`.
- Closed P2 spec: `docs/INITIATIVES/biostack-governed-delivery/parcels/P2.md`, shipped hash
  `527D930FC17FB78ED9398F36A3D75A068E314517D696EE1A94397807CE2BE26A`. Closure:
  `docs/INITIATIVES/biostack-governed-delivery/closures/P2.md`, SHA-256
  `CC5A4EE1F735BBE005417035B0017D667B9609C3EA61AB07E5C409BE16DA1854` — status `DONE`. P2's
  implementation (PR #505, commit `253d07e`) shipped the 17-surface risk-taxonomy and routing
  substrate P3-A builds on: `docs/specs/schemas/classification-axes.schema.json`,
  `docs/specs/schemas/delivery-class-controls.json`, `docs/specs/schemas/fold-engine.md`,
  `docs/specs/schemas/routing-output.schema.json`, nine fixtures, and
  `docs/specs/schemas/AXIS-REGRESSION-MAP.md`.
- Gate dependency: Gate 1 (charter ratification) and plan review are closed; P1 and P2 are merged
  and closed; P3-A is next on the dependency spine
  (`... -> P1 -> P2 -> P3-A -> P0-A -> ...`) and is covered by the charter's P1-P7 standing
  authorization.
- Reconciled shaping base anchor **as observed at this shaping time**: `main@6f46310a6113fae60805feeb65cac50d6b847ba3`
  (the commit that closed P2). This literal SHA is cited for lineage traceability only and is
  **not** presented as the current `BaseCommit` — `main` has continued to advance since this
  shaping pass (including further spec-corpus and coordinator activity unrelated to this parcel),
  so this literal must never be read, cited, or dispatched as if it were still current.
- P3-A uses P2's **single-anchor dispatch model**: the registry (`docs/specs/README.md`,
  `docs/specs/INDEX.md`) and the schema substrate P3-A composes already exist after P2, so the
  coordinator creates and commits the Gate 2 record before the builder branch starts, and the
  builder branch/worktree starts at that exact commit. **`BaseCommit` is pinned at Gate 2 record
  creation time, not at this spec's shaping/approval time**: the coordinator re-resolves the
  registry/P2-substrate HEAD at the moment the Gate 2 record is written (the commit the isolated
  builder worktree actually starts from), records that freshly re-verified SHA as `BaseCommit` in
  the Gate 2 record (see "Gate 2 builder handoff requirements" below), and the real-spec
  compatibility set (document contract 7) is scoped to whatever `docs/specs/active`/
  `docs/specs/done` file set exists at that re-pinned `BaseCommit` — not to the file set that
  existed at `6f46310a6113fae60805feeb65cac50d6b847ba3`, which this parcel's checks never cite as
  `BaseCommit` and never treat as current.

## Objective

Define BioStack's **generic, extensible, machine-checkable parcel-spec schema**: a contract every
future parcel spec and ticket-level spec is *defined* to validate against, with required-section
obligations **composed live from P2's `delivery-class-controls.json`** (never hardcoded or
copy-pasted into P3-A's own files), an explicit, enforceable no-`TBD` rule, a conforming template
for each of the charter's eight delivery classes, and three named, closed-vocabulary extension
points so that a future delivery class, a future domain-overlay binding (P3-B, P0-B), or a future
per-class template can extend this substrate through a bounded, additive mechanic without
requiring P3-A's own files to be redesigned. **Scope of what P3-A actually ships (reusability
correction):** this parcel ships the contract and a self-check verifier (`verify-p3a.ps1`) scoped
to this parcel's own 28 surfaces — it is not yet a general `validate <path>` entrypoint any other
BIO-* ticket or future parcel spec can invoke directly; that reusable invocation path is P4's
general-linter deliverable (see Stop Conditions). Until P4 ships, "every future parcel spec...
validates against" means *is defined to be checked against this contract*, not *is mechanically
invoked through a shared tool today* — the same scope P2's own `verify-p2.ps1` already carries for
its own surfaces.

P3-A is schema, template, and extension-point mechanics only. It composes P2's axis and
control-fold substrate; it does not redefine, duplicate, or fork any control content P2 already
owns. It **must not invent product capability semantics**: it defines no guidance-class or
substance/function-risk control binding (that split is explicitly P3-B's, after P0-B freezes the
Product Capability and Safety Contract), and it decides no allowed/degraded/refused/escalated
product behavior. It proves itself against its own fixtures and against the **real, existing spec
corpus** as a read-only compatibility set; it does not edit, "fix," or reconcile any existing
active/done spec's frontmatter or body.

## Deliverables

1. `docs/specs/schemas/parcel-spec.schema.json` — the generic parcel-spec contract: recognized
   spec shapes, required frontmatter keys, the closed `status` vocabulary, pointers (not copies)
   into P2's axis/control/fold files, the live required-section derivation rule, the no-placeholder
   rule, and the extension-point registry pointer.
2. `docs/specs/schemas/SECTION-HEADING-MAP.md` — the deterministic bridge from
   `delivery-class-controls.json`'s free-text `requiredSpecAdditions` terms to acceptable Markdown
   heading text, built by reading the term set live from P2's file rather than re-declaring it.
3. `docs/specs/schemas/EXTENSION-POINTS.md` — the three named, closed-vocabulary extension points
   (future delivery class, domain-overlay section insertion, future per-class template) and the
   exact bounded mechanic for using each without modifying this parcel's frozen files.
4. Eight per-delivery-class templates under `docs/specs/templates/`, one per charter delivery
   class, each a fully instantiable, zero-TBD scaffold that independently validates against
   `parcel-spec.schema.json` once its sanctioned fill-in markers are replaced.
5. `docs/specs/templates/README.md` — how to choose a template, declare multiple delivery classes
   (multi-label composition via the fold), and use the domain-overlay extension point.
6. Thirteen fixtures under `docs/specs/schemas/fixtures/p3a/`: eight positive per-class
   template-derived fixtures, two independently dispositive no-`TBD` violation cases (one literal,
   one HTML-entity-disguised — see document contract 6), one missing-required-field case, one
   unknown-extension-point case, and the real-spec compatibility-set manifest.
7. `docs/specs/scripts/verify-p3a.ps1` — the parcel-specific deterministic verifier, including the
   embedded live fold-and-heading-match implementation.
8. `docs/specs/README.md` — one appended section (`## Parcel-spec schema, templates, and
   extension points (P3-A)`), zero removed/reordered lines.
9. `docs/specs/INDEX.md` — one appended `P3-A` row, zero removed lines, zero other-row changes,
   using the closed-vocabulary registry cell literal `coordinator-assigns-at-gate-2` rather than
   an ad hoc `TBD` cell (see "Carry-over" below).

## Exact allowed surfaces

The builder may create or modify only:

1. `docs/specs/schemas/parcel-spec.schema.json` — new.
2. `docs/specs/schemas/SECTION-HEADING-MAP.md` — new.
3. `docs/specs/schemas/EXTENSION-POINTS.md` — new.
4. `docs/specs/templates/parcel-template.standard.md` — new.
5. `docs/specs/templates/parcel-template.health-boundary.md` — new.
6. `docs/specs/templates/parcel-template.privacy.md` — new.
7. `docs/specs/templates/parcel-template.migration.md` — new.
8. `docs/specs/templates/parcel-template.trust-path.md` — new.
9. `docs/specs/templates/parcel-template.provider-pilot.md` — new.
10. `docs/specs/templates/parcel-template.legal-policy.md` — new.
11. `docs/specs/templates/parcel-template.knowledge-promotion.md` — new.
12. `docs/specs/templates/README.md` — new.
13. `docs/specs/schemas/fixtures/p3a/positive-template-standard.json` — new.
14. `docs/specs/schemas/fixtures/p3a/positive-template-health-boundary.json` — new.
15. `docs/specs/schemas/fixtures/p3a/positive-template-privacy.json` — new.
16. `docs/specs/schemas/fixtures/p3a/positive-template-migration.json` — new.
17. `docs/specs/schemas/fixtures/p3a/positive-template-trust-path.json` — new.
18. `docs/specs/schemas/fixtures/p3a/positive-template-provider-pilot.json` — new.
19. `docs/specs/schemas/fixtures/p3a/positive-template-legal-policy.json` — new.
20. `docs/specs/schemas/fixtures/p3a/positive-template-knowledge-promotion.json` — new.
21. `docs/specs/schemas/fixtures/p3a/negative-tbd-violation-literal.json` — new.
22. `docs/specs/schemas/fixtures/p3a/negative-tbd-violation-entity-disguised.json` — new.
23. `docs/specs/schemas/fixtures/p3a/negative-missing-required-field.json` — new.
24. `docs/specs/schemas/fixtures/p3a/negative-unknown-extension-point.json` — new.
25. `docs/specs/schemas/fixtures/p3a/REAL-SPEC-COMPATIBILITY-SET.md` — new.
26. `docs/specs/scripts/verify-p3a.ps1` — new.
27. `docs/specs/README.md` — modified. Exactly one appended section (zero removed or reordered
    lines).
28. `docs/specs/INDEX.md` — modified. Exactly one appended table row for `P3-A`, in the existing
    column order, with zero removed lines, zero column changes, and zero edits to any other row.

No other path may change. In particular, the builder must not edit any file under
`docs/specs/active/`, `docs/specs/done/`, `docs/specs/CORE-CONTEXT.md`,
`docs/specs/active/README.md`, `docs/specs/done/README.md`, or any of P2's five schema/fold/
routing/regression-map files — those are read-only composed inputs, not editable surfaces.

### Frozen surfaces

The builder must not change or reinterpret:

- The governed-delivery charter, the plan-review record, and every closed P1 and P2 artifact
  (`parcels/P1.md`, `parcels/P2.md`, `dispatch/P1-GATE2.md`, `closures/P1.md`, `closures/P2.md`).
- `docs/specs/schemas/classification-axes.schema.json`,
  `docs/specs/schemas/delivery-class-controls.json`, `docs/specs/schemas/fold-engine.md`,
  `docs/specs/schemas/routing-output.schema.json`, `docs/specs/schemas/AXIS-REGRESSION-MAP.md`,
  and every P2 fixture under `docs/specs/schemas/fixtures/*.json` (not `fixtures/p3a/`, which is
  this parcel's own surface). P3-A **consumes these read-only**; it never copies their content
  into its own files, only references them by path and reads them live at verification time.
- `docs/specs/CORE-CONTEXT.md`, `docs/specs/active/README.md`, `docs/specs/done/README.md`.
- Every existing file under `docs/specs/active/` and `docs/specs/done/` (read for the
  compatibility-set manifest only; never edited).
- Root `AGENTS.md`.
- `docs/INITIATIVES/biostack-production-readiness/` and its `NO-GO / HOLD` verdict.
- `frontend/`, `backend/`, `contracts/`, `.github/`.
- D1-D18 and the ratified product doctrine.
- The charter's eight delivery classes, four product guidance classes, ten substance/function
  risk labels, parcel dependency spine, standing authorizations, stop conditions, and exit
  criterion — P3-A composes these; it does not redefine them.
- The charter's P3 split itself: P3-A must not bind required capability, claim, provenance,
  missingness, function-review, or escalation **product-semantic** fields to the schema — that is
  P3-B's territory, after P0-B freezes the Product Capability and Safety Contract.

If an allowed deliverable appears to require any frozen-surface or frozen-contract change, stop
without editing it.

## Hard constraints

- **No product capability semantics.** P3-A's schema, templates, and extension points define
  structure (frontmatter keys, closed status vocabulary, required-section presence, placeholder
  prohibition, extension-point naming) only. No deliverable may define what a capability contract,
  claim, evidence threshold, or escalation behavior *means* for any guidance-class or
  substance/function-risk label — that binding is explicitly deferred to P3-B/P0-B, mirroring
  P2's own `deferred-to-P3-B` / `deferred-to-P0-B` stance verbatim.
- **Composed, not hardcoded.** `parcel-spec.schema.json` and `SECTION-HEADING-MAP.md` must
  reference `delivery-class-controls.json`'s `requiredSpecAdditions` arrays **by path, read live at
  verification time** — never copy a delivery class's required-section list as a static array
  inside a P3-A file. `SECTION-HEADING-MAP.md`'s heading-alias table may only add *presentation*
  metadata (how a term's presence is recognized in a heading); it must never add, drop, rename, or
  reorder a control term, and the verifier must assert the map's term set is byte-for-byte the live
  union of every `requiredSpecAdditions` entry across all eight classes in
  `delivery-class-controls.json` at verification time — not a cached or manually copied list.
- **Shared mechanics remain generic (D1); thin overlay (D2); production-independent (D4).** No
  file under this parcel's surfaces may add a runtime, package, build, workflow, or production
  dependency. The schema and templates are generic shaping mechanics usable by any future parcel,
  not BioStack product content; the domain-overlay extension point (EP-2 below) is the single,
  named seam where BioStack's thin overlay attaches later, keeping D2's overlay thin and explicit.
- **No `TBD`.** No file created or modified by this parcel may contain the literal markers `TBD`,
  `TODO`, `FIXME`, `{{...}}` placeholder syntax, or an unresolved decision, with one narrowly
  scoped exception: files under `docs/specs/templates/**` may use the sanctioned fill-in marker
  `[REPLACE: <short instruction>]` as their only permitted incomplete-value syntax. This marker is
  invalid everywhere else; a spec outside `docs/specs/templates/**` containing it fails validation
  identically to a literal `TBD`. This closes the loophole of copying a template into
  `docs/specs/active/` without filling it in. **Before either `noPlaceholderPatterns` regex is
  applied to any file's text, the verifier must run the pinned `placeholderNormalizationSteps`
  pipeline (document contract 1: strip HTML comments, strip HTML/XML tags, strip inline-code
  backtick delimiters, strip paired Markdown emphasis delimiters, strip punctuation-adjacent
  Markdown escape backslashes, decode HTML5 character references, strip Unicode `Cf`/`Cc`
  format/control characters, NFKC-normalize, then apply the Unicode confusables-skeleton
  transform) in that exact order.** Check 12 implements this pipeline literally; it exists to
  defeat homoglyph substitution (e.g. Cyrillic lookalikes of `TBD`), zero-width-character
  insertion, soft hyphens, fullwidth forms, and mathematical-alphanumeric disguises of the banned
  literals, and to defeat markdown/HTML-syntax splitting of the banned literal (e.g.
  `T<!--x-->BD`, `` T`BD ``, `T**BD**`, `T<span></span>BD`) and HTML-character-reference splitting
  (e.g. `T&#66;D`, `T&#x42;D`) by collapsing that markup and those references away before the
  literal is matched. **Scope of this claim (what is fixture-proven, not merely pinned):**
  `negative-tbd-violation-literal.json` and `negative-tbd-violation-entity-disguised.json`
  (document contract 6) are each independently dispositive, single-violation fixtures: the first
  proves the literal case alone is caught, and the second proves the HTML-character-reference-
  splitting case alone is caught (a validator whose `decode-html-entities` step is a no-op passes
  the first fixture and fails only the second, isolating exactly which pipeline step regressed).
  No fixture in this parcel independently exercises the markdown/HTML-syntax-splitting disguise
  classes (HTML comments, HTML/XML tags, inline-code backtick spans, paired emphasis markers,
  escape backslashes) or every individual homoglyph, zero-width, fullwidth, or
  mathematical-alphanumeric disguise, in isolation — those pipeline steps (document contract 1,
  steps 1-5 and 7-9) remain pinned, mandatory obligations enforced by check 12 against every file
  this parcel ships, not independently fixture-proven. The pipeline steps themselves remain a pinned,
  mandatory obligation — check 12 fails any file where the normalized text still matches
  `noPlaceholderPatterns`, regardless of disguise class — but a reviewer evaluating this
  parcel's deterministic proof should read "defeats disguise class X" as "the corresponding
  pipeline step is pinned, applied to every file, and its literal/markup-splitting/
  entity-splitting behavior is fixture-proven against this parcel's own deliverables," not as an
  independent adversarial-corpus proof of UTS #39 confusables-table fidelity, which remains a
  structural (implementation/code-review-time) obligation on the verifier rather than a
  spec-level fixture claim, consistent with the "Structural validation only" scope disclaimer
  below.
- **Structural validation only (scope disclaimer).** A `"valid"`/PASS result from
  `parcel-spec.schema.json` or `verify-p3a.ps1` asserts frontmatter-key presence, closed-vocabulary
  membership, fold-live required-section resolution (including the distinct-heading and
  token-count-bound constraints in document contract 2), and placeholder absence **only**. It does
  not assert, and must never be read or cited as asserting, that a section's content is
  substantive, correct, non-duplicative, or product-accurate — verifying content truth remains
  human reviewer judgment at Gate review (dual review, charter D8). No deliverable, acceptance
  criterion, check, or evidence artifact in this parcel may be represented to a future automation
  consumer (a P4 linter, CI gate, or coordinator) as certifying content quality; it certifies
  structure and declared-contract conformance only.
- **Real-corpus compatibility, not reconciliation.** `parcel-spec.schema.json` must validate
  cleanly, deterministically, and without crashing against every file already enumerated by P2's
  `AXIS-REGRESSION-MAP.md` (the full `docs/specs/active/`/`docs/specs/done/` corpus at a pinned
  `BaseCommit`, excluding the two README files) as a read-only compatibility fixture set. P3-A does
  not edit, reconcile, or "fix" any of these specs; a non-conforming result (expected for most of
  the corpus, since it predates P2's and P3-A's schemas) is a correctly named, evidenced finding,
  not a validator defect and not grounds to silently pass.

## Required document contracts

### 1. `docs/specs/schemas/parcel-spec.schema.json`

A single JSON object with exactly these 15 top-level keys:

```json
{
  "schema": "biostack.parcel-spec.v1",
  "specShapes": {
    "coordinator-parcel": {
      "pathPattern": "docs/INITIATIVES/*/parcels/*.md",
      "idFrontmatterKey": "parcel_id",
      "requiresLeadingYamlFrontmatter": true,
      "appliesFrom": "P3-B.md-forward; P1.md, P2.md, and P3-A.md itself predate this convention and are frozen, not retrofitted"
    },
    "ticket-spec": {
      "pathPattern": "docs/specs/active/*.md|docs/specs/done/*.md",
      "idFrontmatterKey": "ticket",
      "requiresLeadingYamlFrontmatter": true,
      "appliesFrom": "already the existing convention; unchanged by P3-A"
    }
  },
  "requiredFrontmatterKeysCommonToBothShapes": [
    "title", "status", "owner", "created", "updated",
    "delivery_classes", "guidance_classes", "substance_function_risk", "surfaces"
  ],
  "statusClosedVocabulary": ["review-candidate", "active", "done", "superseded"],
  "axisSource": "docs/specs/schemas/classification-axes.schema.json",
  "controlSource": "docs/specs/schemas/delivery-class-controls.json",
  "foldEngineSource": "docs/specs/schemas/fold-engine.md",
  "sectionHeadingMapSource": "docs/specs/schemas/SECTION-HEADING-MAP.md",
  "extensionPointsSource": "docs/specs/schemas/EXTENSION-POINTS.md",
  "requiredSectionDerivation": "fold-live",
  "noPlaceholderPatterns": [
    "(?im)(^\\s*(TBD|TODO|FIXME)\\s*[:|\\-])|(\\{\\{[^}]+\\}\\})",
    "(?i)\\b(TBD|TODO|FIXME)\\b"
  ],
  "sanctionedTemplateFillInMarker": "\\[REPLACE:[^\\]]+\\]",
  "sanctionedTemplateFillInMarkerScope": "docs/specs/templates/** only",
  "placeholderNormalizationSteps": [
    "strip-html-comments",
    "strip-html-tags",
    "strip-inline-code-delimiters",
    "strip-emphasis-markers",
    "strip-markdown-escape-backslashes",
    "decode-html-entities",
    "strip-unicode-category-Cf-and-Cc",
    "nfkc-normalize",
    "unicode-confusables-skeleton"
  ],
  "extensionSections": {}
}
```

`placeholderNormalizationSteps` is a pinned, ordered, mandatory preprocessing pipeline that the
verifier must apply to a file's text **before** either `noPlaceholderPatterns` regex is evaluated
(document contract 1, check 12), in this exact order:

1. `strip-html-comments` removes every `<!--…-->` span (regex `<!--[\s\S]*?-->`, non-greedy,
   dot-matches-newline) with no substitution, so a literal split across an HTML comment (e.g.
   `T<!-- spacer -->BD`) collapses back to contiguous text.
2. `strip-html-tags` removes every HTML/XML-style tag, opening or closing, with or without
   attributes (regex `</?[a-zA-Z][a-zA-Z0-9]*(?:\s[^>]*)?>`), leaving any text between tags
   intact, so a literal split by an empty element (e.g. `T<span></span>BD`) collapses to
   contiguous text.
3. `strip-inline-code-delimiters` removes every run of one or more consecutive backtick
   characters (regex `` `+ ``) used as Markdown inline-code-span delimiters, leaving any text
   between them intact, so a literal split by an empty or populated code span (e.g. `` T``BD ``
   or `` T`B`D ``) collapses to contiguous text.
4. `strip-emphasis-markers` removes every `*` and `_` character that is part of a **paired**
   Markdown emphasis run — a `*`/`_` run matched by a corresponding closing run of the same
   character on the same line (regex-paired, e.g. `\*{1,3}[^*]+\*{1,3}` / `_{1,3}[^_]+_{1,3}`) —
   leaving any unpaired, stray `*`/`_` character untouched, so a literal split by genuine
   bold/italic markup (e.g. `T**BD**`, `T_BD_`) still collapses to contiguous text, while an
   unrelated unpaired underscore or asterisk elsewhere in the same file (e.g. inside a code
   identifier or file path fragment) is left alone and cannot manufacture a false match by
   accidental adjacency.
5. `strip-markdown-escape-backslashes` removes every literal backslash (`\`) character that is
   **immediately followed by ASCII punctuation** (regex `\\(?=[!-/:-@\[-\`{-~])`) — the
   Markdown escape convention is a backslash immediately preceding a punctuation character to
   force it to be read literally, and stripping only that paired backslash collapses the escape
   to the escaped character, so a literal split by a stray escape backslash immediately before a
   letter-adjacent punctuation boundary (e.g. `T\BD` has no punctuation after the backslash and is
   therefore **not** altered by this narrowed step; the in-scope disguise shape is a backslash
   placed before punctuation flanking the split, e.g. `T\.BD` collapsing the escaped `.`) still
   collapses as intended, while a backslash with no following punctuation (an ordinary Windows-
   path-style or prose backslash elsewhere in a real corpus file) is left untouched and cannot
   manufacture a false match.
6. `decode-html-entities` decodes every HTML5 named, decimal (`&#NN;`), and hexadecimal
   (`&#xNN;`) character reference to its literal Unicode codepoint, using the standard HTML5
   entity-decode table applied deterministically and offline (no network access), so that a
   literal split by an HTML character reference naming or encoding one of its letters (e.g.
   `T&#66;D`, `T&#x42;D`) collapses to contiguous text before the Unicode-disguise steps run; an
   entity reference that does not decode to an ASCII letter (e.g. `&amp;`, `&nbsp;`) decodes to
   its own literal character exactly as HTML5 defines and is otherwise inert to this pipeline.
7. `strip-unicode-category-Cf-and-Cc` removes every Unicode format character (category `Cf` —
   including zero-width space U+200B, zero-width non-joiner U+200C, zero-width joiner U+200D,
   soft hyphen U+00AD, and all other `Cf`/`Cc` control/format codepoints) with no substitution.
8. `nfkc-normalize` applies Unicode Normalization Form KC to the result, collapsing fullwidth
   forms (e.g. `ＴＢＤ`) and mathematical-alphanumeric lookalike blocks to their
   canonical ASCII equivalents.
9. `unicode-confusables-skeleton` applies the Unicode Technical Standard #39 confusables-skeleton
   transform (a deterministic, offline, bundled-data-table lookup — no network access) to the
   NFKC-normalized text, mapping visually-confusable codepoints from other scripts (for example
   Cyrillic `Т`/`В` look-alikes of Latin `T`/`B`) onto their skeletal Latin equivalents.

Steps 1–6 are deterministic, offline, regex/table-based markup- and reference-collapsing passes
(no Markdown/HTML rendering engine is invoked, and the entity-decode table in step 6 requires no
network access) and run first so that any markup- or character-reference-mediated splitting of the
banned literal (HTML comments, HTML/XML tags, inline-code backtick spans, paired emphasis `*`/`_`
markers, punctuation-adjacent escape backslashes, and HTML character references) collapses to the
plain literal before the Unicode-disguise steps 7–9 run. Steps 4 and 5 are deliberately scoped to
**plausible placeholder-disguise context** (a paired emphasis run, or a backslash immediately
before ASCII punctuation) rather than unconditional global character deletion, so that an
unrelated `*`/`_`/`\` occurrence elsewhere in a real corpus file cannot manufacture a false
`placeholder-violation` by accidental adjacency; `REAL-SPEC-COMPATIBILITY-SET.md`'s `Reason`
column (document contract 7) therefore reflects a genuine disguised-or-literal match, not a
normalization artifact. Only the output of all nine steps, applied in this exact order, is passed
to `noPlaceholderPatterns`. This closes the homoglyph/zero-width/fullwidth disguise class, the
markdown/HTML-syntax-splitting disguise class, and the HTML-character-reference-splitting
disguise class (document contract 1, check 12) — literal `TBD` is unaffected by this pipeline (it
already matches both patterns without normalization), and a dedicated
`negative-tbd-violation-entity-disguised.json` fixture (document contract 6), containing only the
entity-disguised occurrence and no co-located literal `TBD`, is added — not folded into the
existing literal-case fixture — so that it alone, independently, proves the entity-splitting
disguise case this addition introduces.

`specShapes.*.pathPattern` deterministically selects which shape rule applies to a given file path
(no content sniffing): a file under `docs/INITIATIVES/*/parcels/*.md` is `coordinator-parcel`
shape; a file under `docs/specs/active/*.md` or `docs/specs/done/*.md` is `ticket-spec` shape. Any
other path given to the validator is `unrecognized-shape` and fails deterministically rather than
guessing.

`requiredFrontmatterKeysCommonToBothShapes` uses exactly the snake_case keys P2's spec (document
contract 1 of `parcels/P2.md`) already pins as the authoritative camelCase-to-snake_case
correspondence for the fold-input keys `classification-axes.schema.json` itself declares in
camelCase (`deliveryClasses`/`guidanceClasses`/`substanceFunctionRisk`) — P3-A reuses that
prose mapping verbatim rather than redeclaring it.

`requiredSectionDerivation: "fold-live"` means: at validation time, read the spec's declared
`delivery_classes` array, run it through `fold-engine.md`'s `union-set` resolution against
`delivery-class-controls.json` (exactly the same algorithm P2 already specifies — P3-A invokes it,
it does not reimplement a divergent copy) to obtain the live `requiredSpecAdditions` union for the
spec's declared classes, then resolves each resulting term to satisfied/unsatisfied using
`SECTION-HEADING-MAP.md` against the spec's actual Markdown ATX heading set (`^#{2,3}\s+.+$`
lines). A spec with an empty, unknown, or invalid `delivery_classes` value fails exactly as
`fold-engine.md`'s `empty-required-axis` / `unknown-label` stops already define — P3-A does not
invent a parallel stop vocabulary; it reuses P2's.

`guidance_classes` and `substance_function_risk` are validated only for closed-vocabulary
membership (each declared label must appear in `classification-axes.schema.json`'s corresponding
axis, or the array may be empty) and for passthrough recording — never bound to any required
section, exactly mirroring P2's pass-through stance. P3-A assigns these two axes no section
obligations.

### 2. `docs/specs/schemas/SECTION-HEADING-MAP.md`

A Markdown document with exactly one table, header `Control term | Canonical alias(es) | Source`,
and exactly one row per **distinct** `requiredSpecAdditions` string found anywhere in
`delivery-class-controls.json` (46 distinct terms as of P2's shipped content; the builder must
derive this count live from the file, not copy the number from this sentence, which is informative
context only). `Source` is always the literal `delivery-class-controls.json` (never a P3-A
invention). `Canonical alias(es)` lists one or more short phrases; a term is satisfied by a
document's heading set if any heading, after normalizing (lowercase; `/` and `-` replaced with a
space; whitespace collapsed; each token's single trailing `s` stripped) contains, as a contiguous
token subsequence, the same normalization applied to the term itself **or** to any listed alias.
Terms whose normalized form a reasonable heading already satisfies without an alias (for example
`objective`, `rollback`, `consent`) may list only themselves as their own alias. Terms whose
natural heading phrasing uses a materially different word (for example `tests`, satisfied by a
heading named "Deterministic verification"; `missingness`, satisfied by "Missing-input behavior";
`data inventory`, satisfied by "Data map") must list that alias explicitly. The verifier
cross-reads `delivery-class-controls.json` at run time and fails if the map's term column is not
exactly the live union set (extra, missing, or misspelled terms are each a named failure).

**Anti-heading-soup constraints (mandatory, evaluated together with the matching rule above, not
as an optional refinement):** a contiguous-token-subsequence match alone is not sufficient to mark
a required term `satisfied: true`. Both of the following must also hold for a given term/heading
pair, deterministically, from the document's actual ATX heading set:

1. **Distinct-heading-per-term.** Each required term for a spec's declared, folded class set must
   resolve to its own distinct heading *occurrence* (identified by position in document order, not
   by text) — no single heading occurrence may be counted as satisfying more than one required
   term. The resolver assigns headings to terms via a one-to-one bipartite match (each heading used
   at most once), computed by this **pinned, deterministic resolution** (not merely an existence
   property of *some* maximum matching — the identity of the reported satisfied/unsatisfied term
   set and the specific heading attributed to each term must be identical between any two
   independent, correct implementations of this document contract): process required terms in
   strict ordinal (byte-value) lexicographic ascending order of the live, deduplicated
   `requiredSpecAdditions` union's term strings for the spec's declared, folded class set — not
   `delivery_classes` declaration order, not `delivery-class-controls.json`'s key order, and not
   any other insertion-dependent order; this ordinal-lexicographic sort of the union's own term
   strings is the one textually pinned iteration rule this document contract defines, independent
   of how `fold-engine.md`'s internal `union-set` construction happens to iterate (document
   contract 1's fold-live derivation obtains the union's *membership*, not its processing order;
   this document contract alone pins the order). For each term in that ordinal-lexicographic
   order, assign it the lexicographically least (by normalized heading text, ties broken by
   earlier document-order position) still-unconsumed candidate heading occurrence that textually
   matches it, per the matching rule above; a term with no remaining unconsumed matching candidate
   at its turn resolves `satisfied: false`. This greedy-by-ordinal-lexicographic-term-order,
   lexicographically-least-candidate assignment is the one resolution every conformant
   implementation (this parcel's own `verify-p3a.ps1` and any future reimplementer, e.g. P4's
   general linter) must produce identically for the same input, so `Reason` (document contract 7,
   check 10) is reproducible across implementations, not implementation-defined.
2. **Heading-length bound.** A candidate heading's own normalized token count (after the lowercase/
   `/`-and-`-`-to-space/whitespace-collapse/trailing-`s`-strip normalization above) must not exceed
   the matched term's (or matched alias's) normalized token count by more than 4 tokens, and must
   never exceed 10 normalized tokens in total, whichever bound is smaller. A heading that
   concatenates many unrelated required terms into one oversized line (the heading-soup exploit:
   e.g. `## Objective Surfaces Contracts Acceptance Criteria Tests Rollback Summary` for a
   6-term/≤2-token-per-term class) exceeds this bound and therefore cannot satisfy *any* term
   through it — it is deterministically disqualified as a candidate before the one-to-one match
   in constraint 1 is attempted, not merely de-duplicated after the fact.

A real, honestly authored multi-section document (one short, on-topic heading per required term)
satisfies both constraints trivially; a single polluted heading line satisfies neither. A term that
fails either constraint resolves `satisfied: false` and the containing document fails with reason
`missing-required-section` naming that term, exactly as a genuinely absent heading would —
there is no separate reason code for a heading-soup failure, because from the schema's perspective
it *is* a missing required section (the apparent heading does not count as any term's heading).

### 3. `docs/specs/schemas/EXTENSION-POINTS.md`

Declares exactly three named, closed-vocabulary extension points, each with an "Exact mechanic"
subsection naming precisely which file(s) a future parcel may append to and what it may never
touch:

- **`delivery-class-extension`** — for a future charter amendment adding a ninth+ delivery class.
  Mechanic: the new class is added to `classification-axes.schema.json`'s `deliveryClass.labels`
  and a new top-level key in `delivery-class-controls.json` (both P2-owned, frozen to P3-A, and
  requiring their own charter-amendment review — P3-A does not perform this). Because
  `parcel-spec.schema.json` derives required sections live (fold-live), no P3-A file changes
  automatically once those two P2-owned files are amended, **except** that any genuinely new
  `requiredSpecAdditions` term the new class introduces must be appended, additively, as one new
  row in `SECTION-HEADING-MAP.md` — this is the one sanctioned, bounded edit this extension point
  permits to an otherwise-frozen P3-A file, and it is additive-only (no existing row may be
  removed, renamed, or reordered). This subsection must pin, verbatim, the sentence: "A newly
  appended `SECTION-HEADING-MAP.md` row's normalized term (same normalization as document contract
  2, including the anti-heading-soup constraints) must not duplicate any term already present in
  the live `requiredSpecAdditions` union, and no existing row may be removed, renamed, value-
  mutated, or reordered by this mechanic; the amending parcel's own deterministic verifier must
  assert this append-only, non-duplicating invariant as a named check, failing
  `extension-point-not-additive` on violation."
  (check 7 asserts this exact sentence is present, byte-for-byte, as a pinned obligation, not
  unchecked prose.)
- **`domain-overlay-insertion`** — the single named seam where a thin domain overlay (D2) attaches
  without redesigning `parcel-spec.schema.json`; which future parcel(s) use this seam, and for
  what purpose, is determined entirely by that future parcel's own approved spec, not by this one.
  Mechanic: `parcel-spec.schema.json` declares a reserved, currently-empty closed registry object
  `extensionSections: {}` (present in the schema file as an explicit empty object, not a `TBD`). A
  future parcel operating under its own approved spec may append a new named key to this object
  (for example a generic, non-product-semantic delivery-class-adjacent label, to be named and
  defined entirely by that future parcel's own approved spec — not a product capability, claim,
  guidance-class, or substance/function-risk binding, which remains P3-B/P0-B's exclusive
  territory per this parcel's own Frozen-surfaces and Hard-constraints sections) binding it to an
  additional required-section term, but may not alter any other key already present. **Non-binding
  topology note:** the one-key-to-one-required-section-term shape just described is this
  extension point's minimal, illustrative default only, not a structural ceiling this parcel
  freezes. A future parcel's own approved spec may extend or redesign the internal shape of what
  an `extensionSections` key binds to (for example binding one key to more than one
  required-section term, or to a conditional or cross-axis rule) without that redesign being
  treated as a violation of this parcel's additive-only, frozen-surface guarantee for
  `EXTENSION-POINTS.md` — the guarantee this parcel freezes is the *existence* of the
  `extensionSections` seam and the disjointness/non-removal invariant below, not the internal
  shape of a future binding. A spec that declares a reference to an extension-section key **not
  present** in this registry fails validation with the named error `unknown-extension-point` —
  this is the mechanic the `negative-unknown-extension-point` fixture proves. This subsection must
  pin, verbatim, the
  sentence: "A newly appended `extensionSections` key's normalized form (same normalization as
  `SECTION-HEADING-MAP.md`) must not equal any term already present in the live
  `requiredSpecAdditions` union at append time, nor equal any other `extensionSections` key; no
  existing `extensionSections` key may be removed, renamed, or value-mutated by any future append;
  and the appending parcel's own deterministic verifier must assert this disjointness-and-
  non-removal invariant as a named check, failing `extension-point-not-additive` on violation."
  (check 7 asserts this exact sentence is present, byte-for-byte, as a pinned obligation every
  future consuming parcel inherits, not merely descriptive prose that could be silently ignored.)
- **`template-set-extension`** — the mechanic for adding a new per-delivery-class template.
  Mechanic: a new file named exactly `docs/specs/templates/parcel-template.<label>.md`, where
  `<label>` must already exist in `classification-axes.schema.json`'s `deliveryClass.labels` at
  the time of addition (closed-vocabulary gate — an invented, non-charter label is rejected, not
  silently accepted). No existing template file is reordered or renamed by this mechanic.

Each extension point's subsection must also state, verbatim, the sentence: "This extension point
adds no required section, check, reviewer weight, standing-authorization change, or stop condition
by existing; it only becomes active when a future parcel's own approved spec uses it." This
mirrors P2's `fold-engine.md` non-binding sentences and keeps P3-A itself from silently widening
any spec's obligations the moment this file is merged.

### 4. Eight per-delivery-class templates

Each `docs/specs/templates/parcel-template.<class>.md` is a complete, standalone Markdown
document usable as the starting point for a new `coordinator-parcel`-shape spec declaring exactly
that one delivery class. Each template:

- Opens with a YAML frontmatter block carrying every key in
  `requiredFrontmatterKeysCommonToBothShapes` plus `parcel_id`, with `delivery_classes` pre-filled
  to the template's one class, `guidance_classes: []`, `substance_function_risk: []`, and every
  other value-bearing key using the sanctioned `[REPLACE: ...]` marker (for example
  `owner: "[REPLACE: human owner id]"`), never `TBD`.
- Contains, as literal `##`/`###` ATX headings, a heading satisfying every `requiredSpecAdditions`
  term the fold resolves for that one class per `SECTION-HEADING-MAP.md`'s canonical aliases (for
  example the `standard` template has headings satisfying `objective`, `surfaces`, `contracts`,
  `acceptance criteria`, `tests`, and `rollback`), each heading's body containing instructive prose
  plus a `[REPLACE: ...]` marker for the author to fill in, never a blank heading and never `TBD`.
  **Deterministic content-quality floor (check 8):** each such heading's body, after every
  `[REPLACE:[^\]]+]` span is removed, must contain at least 8 whitespace-delimited word tokens of
  prose — a heading whose entire body is the `[REPLACE: ...]` marker and nothing else fails this
  floor. This is a deterministic, mechanically checkable lower bound on "instructive prose," not a
  content-truth judgment (the "Structural validation only" scope disclaimer in Hard constraints
  still applies to whether the 8+ words are substantively correct — only their presence and count
  are checked).
- Contains one additional `## Extension points used` heading, literal text exactly `## Extension
  points used`, whose body is the literal sentence "None." and nothing else, for all eight
  templates (no template presupposes a domain-overlay or future-class extension; a spec author
  adds that section's content only if a later parcel's approved spec requires it). **Deterministic
  check (check 8):** the verifier requires this exact heading text to be present in each template
  and its body, after trimming leading/trailing whitespace, to equal the literal string `None.`
  exactly — any other body content, or the heading's absence, fails check 8.
- Is, once every `[REPLACE: ...]` marker is replaced with concrete values, a fully conforming
  `parcel-spec.schema.json` instance for its one declared class — this is the basis for the eight
  `positive-template-<class>.json` fixtures below, which the verifier derives mechanically from
  each template file rather than hand-authoring independently, preventing fixture/template drift.

### 5. `docs/specs/templates/README.md`

States, in prose: how to pick a starting template; how to declare more than one delivery class on
a single spec (list every applicable template's required-section union, since the fold unions
`requiredSpecAdditions` across every declared class — the templates are starting points, not a
ceiling); that `guidance_classes`/`substance_function_risk` labels never add a required section
under this schema (P3-A pass-through stance, same as P2); and how to reference the
`domain-overlay-insertion` extension point once a future parcel defines an entry in it. No file
under `docs/specs/templates/` other than the nine listed deliverables (eight class templates plus
this README) may be added by this parcel.

### 6. Thirteen fixtures under `docs/specs/schemas/fixtures/p3a/`

Each JSON fixture has exactly the keys `input` and `expected`, matching P2's fixture shape
convention. `input` carries `specPath` (a path string; for the eight positive fixtures, the actual
path of the corresponding template file in this parcel's own deliverables — the verifier reads
that real file rather than an inlined copy, so the fixture and the template can never silently
diverge), or, for the four negative fixtures, an inlined `syntheticSpec` object with
`frontmatter` and `headings` keys standing in for a file (so the negative cases do not require a
separate throwaway Markdown file under an active/done-like path). `expected` carries `result`
(`"valid"` or `"invalid"`), and, when `"invalid"`, a `reason` naming exactly one of
`missing-required-frontmatter-key`, `unknown-label`, `empty-required-axis`,
`missing-required-section`, `placeholder-violation`, or `unknown-extension-point`.

**Independently dispositive negative fixtures (mandatory design rule):** each negative fixture's
`syntheticSpec` must contain **exactly one** violation — never two or more co-located violations
in the same `syntheticSpec` — so that `expected.result`/`expected.reason` is attributable to that
one fixture's one named mechanism alone. A validator with a correctly implemented mechanism passes
the fixture; a validator with a no-op or missing implementation of that one mechanism fails it;
no fixture's pass/fail outcome may be explainable by any other mechanism also present in the same
`syntheticSpec`. The two no-`TBD` fixtures below apply this rule to split what would otherwise be
a single, non-dispositive multi-violation fixture into two single-violation fixtures.

- `positive-template-<class>.json` (eight files, one per delivery class): `input.specPath` points
  at that class's template file with every `[REPLACE: ...]` marker mechanically substituted by the
  verifier with a short deterministic literal (for example the literal string `filled`) before
  validation, so the fixture proves the template's *structure* is conforming independent of a
  human author's specific word choices; `expected.result` is `"valid"`.
- `negative-tbd-violation-literal.json`: a `syntheticSpec` whose body contains the literal
  substring `TBD` outside any sanctioned-marker scope, and **no other** placeholder-pattern
  occurrence anywhere in the `syntheticSpec` (in particular, no HTML-entity-disguised or other
  disguised occurrence); `expected.result` is `"invalid"`, `expected.reason` is
  `"placeholder-violation"`. This fixture alone proves the literal-match path independent of any
  normalization step.
- `negative-tbd-violation-entity-disguised.json`: a `syntheticSpec` whose body contains **only**
  a single occurrence of `TBD` disguised as an HTML character reference (e.g. `T&#66;D`), and **no
  co-located literal `TBD`** anywhere else in the `syntheticSpec`; `expected.result` is
  `"invalid"`, `expected.reason` is `"placeholder-violation"`. Because this is the fixture's
  **only** violation, a validator whose `decode-html-entities` normalization step (document
  contract 1) is a no-op or absent cannot find any literal `TBD` to fall back on and therefore
  fails this fixture — making the fixture independently dispositive proof that the
  `decode-html-entities` step actually runs and actually collapses the entity-split disguise,
  not merely that the pipeline step is pinned in `placeholderNormalizationSteps`.
- `negative-missing-required-field.json`: a `syntheticSpec` whose `frontmatter` omits `owner`;
  `expected.result` is `"invalid"`, `expected.reason` is `"missing-required-frontmatter-key"`,
  naming `owner`.
- `negative-unknown-extension-point.json`: a `syntheticSpec` whose frontmatter declares an
  extension-section reference (for example `extension_section: "pricing-tier-overlay"`) not
  present in `EXTENSION-POINTS.md`'s `domain-overlay-insertion` registry; `expected.result` is
  `"invalid"`, `expected.reason` is `"unknown-extension-point"`.

### 7. `docs/specs/schemas/fixtures/p3a/REAL-SPEC-COMPATIBILITY-SET.md`

A Markdown document with exactly one table, header `Spec file | Shape | AXIS-REGRESSION-MAP
disposition | parcel-spec.schema.json result | Reason | Agreement`, and exactly one data row for
every file path that appears as a `Spec file` row in P2's **frozen**
`docs/specs/schemas/AXIS-REGRESSION-MAP.md` (26 files, fixed by that file's own shipped content at
its own pinned `BaseCommit` lineage) — not a live `git ls-files docs/specs/active docs/specs/done`
query at this parcel's own `BaseCommit`. The row set is bound to the frozen P2 census, not to
whatever the live `docs/specs/active`/`docs/specs/done` directory listing happens to contain at
verification time: corpus growth after P2's `BaseCommit` (for example a new ticket spec added to
`docs/specs/active/` after P2 closed) adds no row to this file and does not change the row count,
and a file removed from the live corpus after P2's `BaseCommit` does not remove its row either —
the set is exactly, and only, P2's own frozen enumeration, so this file's row count and content
cannot be falsified by ordinary corpus growth. For each row:

- `Shape` is `ticket-spec` for every `.md` file and `n/a (non-spec artifact)` for the two
  `*.shaping-result.json` files (which are excluded from schema validation entirely, exactly as
  P2 excluded them from axis mapping, and are recorded `n/a` across the remaining columns).
- `AXIS-REGRESSION-MAP disposition` is copied verbatim (read-only citation, not re-derived) from
  P2's shipped `AXIS-REGRESSION-MAP.md` row for that file.
- `parcel-spec.schema.json result` is `valid` or `invalid`, from actually running the file through
  this parcel's own validator logic (frontmatter keys, closed vocabulary, fold-live required
  sections, placeholder scan).
- `Reason` names the specific failing check (reusing this parcel's own closed reason vocabulary)
  or is `n/a` when `result` is `valid`.
- `Agreement` is `consistent` when a P2 `needs-reconciliation` disposition co-occurs with a P3-A
  `invalid` result (or a P2 `conforms` disposition co-occurs with a P3-A `valid` result), and
  `flagged-for-human-review` otherwise — a structural-layer finding P2's frontmatter-only census
  could not surface (see "Carry-over" below). No row may assert `consistent` without the builder
  having actually executed both checks; this file does not edit, reconcile, or alter any inspected
  spec.

### 8. `verify-p3a.ps1`

Must implement, at minimum and in the same no-network, PowerShell-only, nonzero-on-failure style
as `verify-p1.ps1`/`verify-p2.ps1`: scope-and-frozen-surface checks (surface enumeration, frozen
byte-identity); schema shape-correctness checks on `parcel-spec.schema.json`,
`SECTION-HEADING-MAP.md` (including the live term-set cross-check against
`delivery-class-controls.json`), and `EXTENSION-POINTS.md`; a fold-live required-section resolver
reused identically across all thirteen `fixtures/p3a/` fixtures and the frozen-P2-census real-spec
compatibility pass (26 rows, bound to `AXIS-REGRESSION-MAP.md`'s own enumeration); a placeholder
scan scoped exactly as the hard constraints section specifies;
and the standard evidence-bundle and clean-tree checks. The full, numbered check list is specified
exactly in "Deterministic verification" below.

### 9. `docs/specs/README.md` and `docs/specs/INDEX.md` amendments

`README.md` gets exactly one appended section titled `## Parcel-spec schema, templates, and
extension points (P3-A)`, linking `../schemas/parcel-spec.schema.json`,
`../schemas/SECTION-HEADING-MAP.md`, `../schemas/EXTENSION-POINTS.md`, and
`../templates/README.md`, and stating in one sentence that P3-A composes P2's axis/fold substrate
into a generic spec contract and invents no product capability semantics. No existing line may be
removed, reordered, or reworded.

`INDEX.md` gets exactly one appended row, in the existing ten-column order, for `P3-A`: `Status` =
`review-candidate`; `Spec` links this file; `Goal Charter` links the charter; `Delivery classes` =
`standard; architecture`; `Guidance classes` = `not-applicable`; `Branch/worktree` and `Owner` both
use the literal closed status value `coordinator-assigns-at-gate-2` (the literal `parcels/P2.md`
itself declared, at its own dispatch time, that its `INDEX.md` row would carry — see
`parcels/P2.md` document contract 8, lines 392-394 and 527 — as a dispatch-time registry-cell
convention instead of repeating the registry's pre-existing ad hoc `TBD` cells; the current,
closed `docs/specs/INDEX.md` P2 row no longer carries this literal, because P2's Gate 2 record
later resolved it to the real branch/worktree and owner links once those identities were assigned
— continuing this same practice means P3-A's new row is expected to be similarly superseded by
real links once this parcel's own Gate 2 record is created, not that the literal remains
permanently in the registry; see "Carry-over"); `Review requirement` = `2 independent reviewers`;
`Closure` = `not-yet-closed`. No existing row may change.

## Acceptance criteria

- **AC-P3A-01 — Schema shape correctness:** `parcel-spec.schema.json` declares exactly the two
  spec shapes, the common required-frontmatter-key list, the closed `status` vocabulary, the five
  P2-source pointers, the `fold-live` required-section derivation, the two placeholder patterns,
  the pinned `placeholderNormalizationSteps` pipeline, and the sanctioned template fill-in marker
  and its scope, exactly as pinned above.
- **AC-P3A-02 — Live composition, not duplication:** `SECTION-HEADING-MAP.md`'s term column is,
  at verification time, byte-for-byte the live union of every `requiredSpecAdditions` entry in
  `delivery-class-controls.json`; no P3-A file contains a `requiredSpecAdditions`,
  `minimumChecks`, `mandatoryStopConditions`, `requiredClosureEvidence`, or `reviewers` key copied
  from `delivery-class-controls.json`.
- **AC-P3A-03 — Extension-point mechanics:** `EXTENSION-POINTS.md` defines exactly the three named
  extension points, each with an exact, bounded mechanic, the non-binding verbatim sentence, and
  (for `delivery-class-extension`/`domain-overlay-insertion`) the pinned append-only/disjointness
  invariant sentence (document contract 3, check 7); the schema file's `extensionSections` registry
  object is present and empty at this parcel's shipped hash.
- **AC-P3A-04 — Template conformance, one per delivery class:** all eight templates exist, each
  independently resolves to `"valid"` against `parcel-spec.schema.json` once fill-in markers are
  substituted, each template's declared `delivery_classes` fold resolves every
  `requiredSpecAdditions` term for that one class to a **distinct, length-bounded** satisfied
  heading (document contract 2's anti-heading-soup constraints, check 8), each required heading's
  pre-substitution body meets the 8-word deterministic content-quality floor, and the literal
  `## Extension points used` heading with body exactly `None.` is present (document contract 4's
  deterministic content-quality checks, check 8).
- **AC-P3A-05 — Fixture proof, including violation and extension cases:** all thirteen
  `fixtures/p3a` fixtures parse, and the verifier's embedded fold-live resolver reproduces every
  fixture's `expected` result and (when invalid) `reason` exactly from its `input`.
- **AC-P3A-06 — No `TBD` anywhere, including disguised forms, with the sole sanctioned
  exception:** no file under this parcel's surfaces outside `docs/specs/templates/**` contains the
  sanctioned marker or any of the banned placeholder patterns **after** the pinned
  `placeholderNormalizationSteps` pipeline is applied — not only the literal ASCII form, but also
  the literal and HTML-character-reference-splitting disguise cases, which are the only two
  disguise classes this parcel's own fixtures independently prove (document contract 6,
  `negative-tbd-violation-literal.json` / `negative-tbd-violation-entity-disguised.json`). The
  pipeline additionally pins, as a mandatory structural obligation enforced by check 12 against
  every file this parcel ships (but not independently fixture-proven in isolation; see the Hard
  constraints "Scope of this claim" disclaimer), normalization steps for homoglyph, zero-width,
  soft-hyphen, fullwidth, and mathematical-alphanumeric disguises, and for
  markdown/HTML-syntax-splitting disguises via HTML comments, HTML/XML tags, inline-code backtick
  spans, emphasis markers, and escape backslashes — this acceptance criterion's coverage claim is
  therefore scoped to the fixture-proven literal and entity-reference-splitting cases, not to an
  independent adversarial-corpus proof of every disguise class the pipeline structurally defends
  against; files under `docs/specs/templates/**` contain only the sanctioned marker as their
  incomplete-value syntax.
- **AC-P3A-07 — Real-spec compatibility, not reconciliation:** `REAL-SPEC-COMPATIBILITY-SET.md`
  has exactly one row per file enumerated in P2's frozen `AXIS-REGRESSION-MAP.md` (26 files, fixed
  by that file's own shipped content — not re-derived from a live `git ls-files` listing of
  `docs/specs/active`/`docs/specs/done`, so later corpus growth cannot change this row set or
  falsify this criterion), every row's `parcel-spec.schema.json result` and `Reason` are
  independently and actually derived (not copied from P2), and no inspected spec is edited.
- **AC-P3A-08 — No product capability semantics:** no file under this parcel's surfaces assigns
  meaning, claim, evidence, or allowed/degraded/refused/escalated behavior to any guidance-class or
  substance/function-risk label.
- **AC-P3A-09 — Scope integrity:** the changed-file set equals exactly the 28 allowed surfaces;
  every frozen surface (charter, plan-review, every closed P1/P2 artifact, all five P2 schema/
  fold/routing/regression files and P2's own fixtures, `CORE-CONTEXT.md`, `active/README.md`,
  `done/README.md`, every existing active/done spec, `AGENTS.md`, production-readiness tree,
  `frontend/`, `backend/`, `contracts/`, `.github/`) is byte-identical to `BaseCommit`.
- **AC-P3A-10 — Bounded registry/lifecycle edits:** the `INDEX.md` diff is exactly one appended row
  with zero removed lines and zero column changes, using `coordinator-assigns-at-gate-2` (not
  `TBD`) in the branch/worktree and owner cells; the `README.md` diff is exactly one appended
  section with zero removed lines.
- **AC-P3A-11 — Carry-over satisfied:** all three items identified in "Carry-over" below (the two
  review-2 low-amendment items plus the self-identified coordinator-parcel-shape testing gap) have
  a named, evidenced disposition inside this parcel's own deliverables (not a bare restatement).

## Deterministic verification

Run from the coordinator-named isolated P3-A worktree after committing all 28 deliverables:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File docs/specs/scripts/verify-p3a.ps1 `
  -BaseCommit <40-character-dispatch-anchor-SHA> `
  -BuilderId <dispatch-builder-id> `
  -ReviewerIds <reviewer-1-id>,<reviewer-2-id> `
  -EvidenceDirectory artifacts/p3a-verification
```

`verify-p3a.ps1` must implement exactly these checks, without network access or non-PowerShell
dependencies, and must exit nonzero on any failure:

1. Set `$ErrorActionPreference = 'Stop'`; resolve and enter `git rev-parse --show-toplevel`;
   require `HEAD` to descend from `BaseCommit` via
   `git merge-base --is-ancestor BaseCommit HEAD`.
2. Run `git diff --check "$BaseCommit...HEAD"`; require exit `0`.
3. Read `git diff --name-only "$BaseCommit...HEAD" --`; require the changed set to equal exactly
   the 28 allowed surfaces listed in this spec, sorted ordinally — no more, no fewer.
4. Require `git diff --quiet "$BaseCommit...HEAD" --` to exit `0` for each of: the charter path,
   the plan-review path, `parcels/P1.md`, `parcels/P2.md`, `dispatch/P1-GATE2.md`,
   `closures/P1.md`, `closures/P2.md`, the five P2 schema/fold/routing/regression-map files, every
   path under `docs/specs/schemas/fixtures/` that is not under `docs/specs/schemas/fixtures/p3a/`,
   `docs/specs/CORE-CONTEXT.md`, `docs/specs/active/README.md`, `docs/specs/done/README.md`,
   `AGENTS.md`, `docs/INITIATIVES/biostack-production-readiness`, `frontend`, `backend`,
   `contracts`, `.github`, and every path returned by
   `git ls-files docs/specs/active docs/specs/done` at `BaseCommit` excluding the two README
   files.
5. Parse `parcel-spec.schema.json`; require exactly the 15 top-level keys pinned in document
   contract 1 (no more, no fewer); require `specShapes` to have exactly the two named shapes with
   their pinned `pathPattern`/`idFrontmatterKey` values; require
   `requiredFrontmatterKeysCommonToBothShapes` to equal the pinned nine-key array in order; require
   `statusClosedVocabulary` to equal the pinned four-value array in order; require the five
   `*Source` keys to equal their pinned literal paths; require `noPlaceholderPatterns` to equal the
   pinned two-pattern array; require `sanctionedTemplateFillInMarker` and its scope to equal the
   pinned literals; require `placeholderNormalizationSteps` to equal the pinned nine-step array in
   order; require the `extensionSections` key's **value**, not merely its presence, to equal an
   empty object `{}` exactly — this is the value-level assertion that resolves the 14-vs-15-key
   contradiction: check 5 alone, against the 15-key list, is the single place this parcel asserts
   both `extensionSections`'s presence and its value; check 7 below does not re-assert presence.
6. Parse `delivery-class-controls.json` (read-only); compute the live union of every
   `requiredSpecAdditions` array; parse `SECTION-HEADING-MAP.md`'s table; require its term column,
   as a set, to equal that live union exactly (AC-P3A-02); require every row to have a non-empty
   `Canonical alias(es)` cell and `Source` equal to the literal `delivery-class-controls.json`.
7. Parse `EXTENSION-POINTS.md`; require exactly the three named extension-point headings and each
   one's non-binding verbatim sentence to appear at least once (`parcel-spec.schema.json`'s
   `extensionSections` key's presence and `{}` value are already asserted by check 5; this check
   does not re-assert them). Additionally require `delivery-class-extension`'s subsection to
   contain, byte-for-byte, the pinned
   append-only/non-duplicating invariant sentence, and `domain-overlay-insertion`'s subsection to
   contain, byte-for-byte, the pinned disjointness-and-non-removal invariant sentence, exactly as
   both are quoted in document contract 3 — making the additive-only extension-point promise a
   pinned, parseable, string-matched check (`extension-point-obligation-missing` on absence) rather
   than unchecked prose, even though `extensionSections` itself is empty at this parcel's shipped
   hash and so has no append to evaluate yet.
8. For each of the eight template files: parse YAML frontmatter and Markdown headings; require the
   nine common frontmatter keys plus `parcel_id` to be present; require `delivery_classes` to
   equal an array containing exactly that one class; require `guidance_classes` and
   `substance_function_risk` to each equal `[]`; mechanically substitute every
   `\[REPLACE:[^\]]+\]` occurrence with the literal `filled`; re-parse the substituted document;
   run the fold-live resolver (reading `fold-engine.md`'s algorithm against
   `delivery-class-controls.json` for that one declared class) and require every resulting
   `requiredSpecAdditions` term to resolve `satisfied: true` against `SECTION-HEADING-MAP.md`,
   applying the distinct-heading-per-term and heading-length-bound anti-heading-soup constraints
   from document contract 2 (a term that only resolves via a heading already consumed by another
   term, or via a heading exceeding the length bound, resolves `satisfied: false` and fails the
   check); require zero remaining occurrence of `TBD`/`TODO`/`FIXME`/`{{`/`[REPLACE:` in the
   substituted document, applying the `placeholderNormalizationSteps` pipeline (document contract
   1) before scanning; on the **pre-substitution** document (before the `filled`-literal
   substitution above), for each required heading located by the fold-live resolver, remove every
   `\[REPLACE:[^\]]+\]` span from that heading's body text and require at least 8 remaining
   whitespace-delimited word tokens (document contract 4's deterministic content-quality floor;
   fewer than 8 fails `template-content-floor-not-met` naming the heading); require a heading with
   literal text exactly `## Extension points used` to be present and its body, trimmed of leading/
   trailing whitespace, to equal exactly the literal string `None.` (absence or any other body
   content fails `extension-points-used-section-missing-or-malformed`).
9. For each of the twelve `fixtures/p3a/*.json` fixtures (the eight positive and four negative
   JSON fixtures; `REAL-SPEC-COMPATIBILITY-SET.md` is handled separately by check 10): parse JSON; require exactly the keys
   `input`/`expected`; for the eight positive fixtures, resolve `input.specPath` to the real
   template file (re-running the same substitution and fold-live resolution as check 8, including
   its anti-heading-soup and placeholder-normalization sub-constraints) and require
   `expected.result` to equal `"valid"`; for the four negative fixtures, run the validator
   directly against the inlined `syntheticSpec` — applying the same `placeholderNormalizationSteps`
   pipeline and anti-heading-soup constraints the resolver uses everywhere else — and require
   `expected.result` to equal `"invalid"` with the pinned `expected.reason` value exactly as named
   in document contract 6; additionally, for `negative-tbd-violation-literal.json` and
   `negative-tbd-violation-entity-disguised.json` specifically, require each `syntheticSpec`'s body
   to contain exactly one placeholder-pattern-matching occurrence after normalization (never two
   or more), failing `fixture-not-independently-dispositive` if either fixture's body contains any
   second occurrence — enforcing document contract 6's independently-dispositive-fixture design
   rule as a named, checked invariant rather than unchecked prose.
10. Parse `REAL-SPEC-COMPATIBILITY-SET.md`; require the header row to equal the exact six-column
    header pinned in document contract 7; require the row set's `Spec file` column, as a set, to
    equal exactly the set of `Spec file` values in P2's frozen `docs/specs/schemas/
    AXIS-REGRESSION-MAP.md` (read live from that frozen file, 26 rows as of P2's shipped content —
    not re-derived from `git ls-files docs/specs/active docs/specs/done`, which may return a
    different, larger set at this parcel's own `BaseCommit` and is irrelevant to this check); for
    every `.md` row, actually run the validator against the real file at `BaseCommit` and require
    the recorded `parcel-spec.schema.json result`/`Reason` to equal what the validator
    independently produces (not merely present); require every `*.shaping-result.json` row to
    record `Shape` = `n/a (non-spec artifact)` and all remaining columns `n/a`; require the
    `Agreement` column to be correctly computed per the rule in document contract 7 for every row
    (independent recomputation, not copied from the file).
11. Require the `INDEX.md` diff to add exactly one line matching `^\| P3-A \|` and remove zero
    lines; parse the added row and require its ten cells to equal exactly the pinned values in
    document contract 9, specifically asserting the literal `coordinator-assigns-at-gate-2` (not
    `TBD`) in both the branch/worktree and owner cells. Require the `README.md` diff to remove zero
    lines, add one contiguous block, and require the added section's heading and four links to
    match document contract 9 exactly.
12. For every file changed or added by this parcel, first apply the pinned
    `placeholderNormalizationSteps` pipeline (document contract 1) to its text, in order: (a) strip
    HTML comments (`<!--...-->`); (b) strip HTML/XML tags; (c) strip inline-code backtick
    delimiters; (d) strip paired Markdown emphasis delimiters (`*`, `_`); (e) strip
    punctuation-adjacent Markdown escape backslashes (`\` immediately followed by ASCII
    punctuation); (f) decode HTML5 named/decimal/hexadecimal character references; (g) strip all
    Unicode category `Cf` and `Cc` characters (zero-width space/non-joiner/joiner, soft hyphen, and
    all other format/control codepoints); (h) NFKC-normalize the result (collapsing fullwidth and
    mathematical-alphanumeric forms to ASCII); (i) apply the Unicode confusables-skeleton transform
    (UTS #39) to the result. Only then search the normalized text for
    `(?im)(^\s*(TBD|TODO|FIXME)\s*[:|\-])|(\{\{[^}]+\}\})` and,
    independently, `(?i)\b(TBD|TODO|FIXME)\b`; require zero matches in every file — this is
    the check that makes homoglyph (e.g. Cyrillic lookalikes), zero-width-character, soft-hyphen,
    fullwidth, and mathematical-alphanumeric disguises, **and** markdown/HTML-syntax-splitting
    disguises (HTML comments, tags, inline-code spans, paired emphasis markers,
    punctuation-adjacent escape backslashes), **and** HTML-character-reference-splitting disguises,
    of `TBD`/`TODO`/`FIXME` fail identically to the literal form; separately search every file
    **not** under `docs/specs/templates/` for
    `\[REPLACE:[^\]]+\]` (on the normalized text); require zero matches there (AC-P3A-06); require
    every file under `docs/specs/templates/` to contain at least one `[REPLACE: ...]` occurrence (a
    template with none would be suspiciously over-filled, not genuinely a scaffold).
13. Require `EvidenceDirectory`, resolved against the repository root, to equal
    `<repo>/artifacts/p3a-verification`; create it; write UTF-8/LF `changed-files.txt`,
    `schema-check.json`, `heading-map-check.json`, `extension-points-check.json`,
    `template-check.json` (one entry per template), `fixture-results.json` (one entry per
    fixture), `compatibility-set-check.json` (one entry per real-spec row), and
    `verification-summary.json` naming every numbered check with `pass: true`, `BaseCommit`,
    `HEAD`, `BuilderId`, and ordered `ReviewerIds`.
14. Require `git ls-files --error-unmatch -- artifacts/p3a-verification` to fail (untracked).
    Parse `git status --porcelain=v1 --untracked-files=all`; every line must begin
    `?? artifacts/p3a-verification/`; any staged, unstaged, or other untracked path fails. Print
    `P3-A verification PASS` and exit `0` only after every check and this final clean-tree check
    pass.

Any exception, nonzero child-command exit where zero is required, missing evidence file, or
fixture/template/compatibility-set mismatch is red. There is no exclusion list and no
warning-only acceptance.

## Acceptance-to-evidence map

| Acceptance criterion | Required evidence |
|---|---|
| AC-P3A-01 | `schema-check.json` |
| AC-P3A-02 | `heading-map-check.json` |
| AC-P3A-03 | `extension-points-check.json` |
| AC-P3A-04 | `template-check.json` |
| AC-P3A-05 | `fixture-results.json` (all twelve `.json` fixtures) |
| AC-P3A-06 | placeholder-scan output (two P2-style patterns plus the template-marker scope checks) |
| AC-P3A-07 | `compatibility-set-check.json` (all 26 rows, bound to `AXIS-REGRESSION-MAP.md`'s frozen enumeration) |
| AC-P3A-08 | `schema-check.json`/`extension-points-check.json` plus reviewer scan |
| AC-P3A-09 | `changed-files.txt` plus frozen-path quiet-diff result |
| AC-P3A-10 | `changed-files.txt` plus bounded-diff result for `INDEX.md`/`README.md` |
| AC-P3A-11 | `compatibility-set-check.json` `Agreement` column plus the `INDEX.md` literal-cell check |

## Dual review

Per the charter's parcel-tree entry (architecture) and D8, this is a dual-review parcel: **two
independent, fresh-session, read-only adversarial reviewers**, who do not see each other's
findings, each receive the same approved spec hash, `BaseCommit`, builder commit, worktree path,
Gate 2 record, authorization identities, and the complete evidence bundle. Each review covers:
live-composition fidelity (AC-P3A-02 — that no control content was hardcoded or drifted from
`delivery-class-controls.json`), template-to-fixture non-drift (AC-P3A-04/05), the no-`TBD`/
sanctioned-marker boundary (AC-P3A-06), the real-spec compatibility set's independent derivation
and the two carry-over items' disposition (AC-P3A-07/11), the no-product-capability-semantics
constraint (AC-P3A-08), scope and frozen-surface integrity, and every other acceptance item. The
coordinator reproduces any disputed finding before triage; reviewers never fix the parcel; rework
returns to the same builder only through a new scoped directive and reruns every deterministic
check and both reviews.

## Standing authorization and tripwire

P3-A remains inside the charter's P1-P7 standing authorization **only while both of these hold**:

1. P3-A changes only generic spec-shaping mechanics — schema, heading-map, extension-point
   registry, templates, and their proof fixtures — and touches no production surface (`frontend`,
   `backend`, `contracts`, `.github` all remain byte-identical to `BaseCommit`, enforced by check
   4/AC-P3A-09).
2. P3-A makes no product capability-semantics decision — it assigns no meaning, claim, evidence,
   or allowed/degraded/refused/escalated behavior to any guidance-class or substance/
   function-risk label (AC-P3A-08), and defers all such binding to P3-B/P0-B.

**Tripwire:** if, during shaping, review, or rework, any proposed change to this parcel would (a)
touch a production surface, (b) assign capability, claim, evidence, or allowed/degraded/refused/
escalated meaning to any guidance-class or substance/function-risk label, (c) edit, reconcile, or
"fix" any existing active/done spec's frontmatter or body rather than merely report its
compatibility result, or (d) hardcode or duplicate any `delivery-class-controls.json` content
inside a P3-A file instead of composing it live, the coordinator stops immediately, does not
dispatch or merge under standing authorization, and returns to the developer for an explicit human
gate, per the charter's stop conditions and the exclusion that no standing authorization is
inferred for product allowed-output decisions.

## Carry-over

P2's closure record (`closures/P2.md`) states that review 2 (`p2_impl_review_2`) returned a PASS
with "two LOW non-blocking amendments noted for P3+ carry-over," without transcribing the
amendment text into repository canon (the live review session output was not itself a committed
artifact). The two items below are the coordinator's reconstruction from the only preserved
evidence — the closure record's description of review 2's observed scope (deep, independently
re-executed fixture verification; genuine PowerShell execution; no false-PASS constructible) read
together with P2's own shipped deliverables, which contain two concrete, still-open gaps of
exactly that character. This parcel satisfies both explicitly rather than re-litigating P2:

1. **Pre-existing ad hoc `TBD` cells in `docs/specs/INDEX.md`.** P2's own spec explicitly
   acknowledged, without fixing, that several existing `INDEX.md` rows (the `BIO-PAIRWISE-00x`
   rows) carry ad hoc `TBD (coordinator to assign Gate 2)` / `[TBD]` cells predating any enforced
   no-`TBD` rule, and that P2's own new row "must not repeat that pattern" — P2 satisfied this only
   for its own row. P3-A is the parcel that introduces the enforceable no-`TBD` rule charter-wide;
   it is the correct and only appropriate place to (a) continue the non-`TBD` registry-cell
   convention P2 started (`coordinator-assigns-at-gate-2`, used in this parcel's own `INDEX.md`
   row per document contract 9/AC-P3A-10), and (b) make the existing `BIO-PAIRWISE-00x` `TBD`
   cells a **named, surfaced finding** rather than a silent gap. `REAL-SPEC-COMPATIBILITY-SET.md`
   does not edit `INDEX.md` (frozen surface, out of scope, same restraint P2 applied to active/done
   specs) but its introductory prose must name this exact gap and point to the future
   reconciliation parcel (or P4's general linter) that will fix it — satisfying the amendment by
   making it visible and traceable rather than by performing an out-of-scope edit.
2. **Structural (body-section) compatibility was not yet proven, only frontmatter-field
   compatibility.** P2's `AXIS-REGRESSION-MAP.md` census checked only whether each existing spec's
   ad hoc classification *frontmatter* fields map onto the new closed-vocabulary axes; it did not
   check whether each spec's *body* contains the sections a fully composed schema would require.
   Review 2's observed rigor (independently re-deriving every fixture, not merely trusting the
   builder's transcript) is exactly the standard this gap needs applied one layer up: a genuine
   structural pass, not just a frontmatter pass. `REAL-SPEC-COMPATIBILITY-SET.md`'s
   `parcel-spec.schema.json result`/`Reason`/`Agreement` columns (document contract 7, AC-P3A-07)
   are precisely this second, independent, structural-conformance layer, cross-referenced against
   P2's frontmatter-only dispositions so any disagreement between the two layers is itself a
   named, surfaced `flagged-for-human-review` row rather than silently absorbed.

Both items are satisfied by naming and evidencing the gap inside P3-A's own read-only deliverables
— neither requires, nor is permitted, an edit to any frozen surface (`INDEX.md`'s existing rows,
or any existing active/done spec).

3. **Self-identified gap: the `coordinator-parcel` shape is specified but exercised by no fixture
   or compatibility-set row in this parcel.** All twelve `fixtures/p3a/*.json` fixtures are
   `ticket-spec`-shape-adjacent synthetics or template-derived (also destined to become
   `coordinator-parcel`-shape files once used, but not themselves parsed as one in this parcel's
   own fixture set), and `REAL-SPEC-COMPATIBILITY-SET.md`'s 26-row frozen-P2-census pass is scoped
   exactly to `docs/specs/active`/`docs/specs/done` (`ticket-spec` shape only), per P2's
   `AXIS-REGRESSION-MAP.md` lineage — it never runs the schema against an actual
   `docs/INITIATIVES/*/parcels/*.md` file. Per the corrected `appliesFrom` value (document contract
   1: `coordinator-parcel` binds `P3-B.md`-forward; `P1.md`, `P2.md`, and `P3-A.md` itself all
   predate and are exempt from the convention), no conforming `coordinator-parcel`-shape file can
   exist yet for this parcel to test against — `P3-B.md` will be the first. This is named here,
   explicitly, as an accepted, bounded, carry-forward gap rather than a silent omission: **P3-B's
   own dispatch must either (a) include a fixture or compatibility-set row exercising the
   `coordinator-parcel` branch against its own conforming spec file, or (b) explicitly re-affirm
   this gap's continuation with a named reason**, and this obligation is also recorded in Stop
   Conditions below so a future coordinator cannot silently skip it.

## Stop conditions

Stop and return to the coordinator if:

- Any required target path is absent or materially different from the contract assumed here.
- A fixture's or template's `expected`/resolved result cannot be derived deterministically from
  the fold-live rule and `SECTION-HEADING-MAP.md` without a human policy call.
- P3-A would need to define P3-B's capability/claim/evidence/escalation binding, P0-A/P0-B's canon
  or capability-contract work, P4's general linter, or P6/P7's closure/review-enforcement
  contracts.
- The compatibility-set pass would require editing any existing active/done spec or `INDEX.md`'s
  existing rows to "fix" a finding.
- The production-readiness initiative or release verdict would change.
- A branch, worktree, base commit, spec hash, owner, or reviewer assignment is ambiguous.
- Any deterministic check fails twice, a frozen contract must change, a reviewer finds an
  out-of-scope effect, or the standing-authorization tripwire above fires.
- P3-B's shaping does not name, per Carry-over item 3 above, how it discharges or continues the
  untested `coordinator-parcel`-shape gap.

## Rollback

P3-A is documentation/schema/template-only. Rollback is a normal revert of the P3-A implementation
commit on its isolated branch, followed by the same scope, frozen-surface, fixture, template, and
placeholder checks. Do not delete or rewrite unrelated worktrees, branches, history, or ambient
untracked files.

## Gate 2 builder handoff requirements

The coordinator writes the Gate 2 record before builder dispatch, naming: the approved spec path
and SHA-256, the single `BaseCommit` dispatch anchor SHA, the isolated branch, the isolated
worktree path starting at that anchor, the builder identity, the 28 builder-editable surfaces, the
permission envelope, the deterministic verification command with both identities and `BaseCommit`
realized, the evidence destination `artifacts/p3a-verification`, and the two reviewers. Like P2,
P3-A's Gate 2 record is created out-of-band before the dispatch anchor and is not part of the
builder's diff — the registry and composed substrate P3-A depends on already exist.

**`BaseCommit` is pinned at the moment the coordinator writes this Gate 2 record, not at this
spec's shaping/approval time.** The coordinator re-verifies the registry/P2-substrate HEAD at
Gate 2 record creation and records that commit's full 40-character SHA as `BaseCommit`; the
`main@6f46310a6113fae60805feeb65cac50d6b847ba3` literal in "Lineage and dependencies" above is a
shaping-time observation, not a stale-but-current `BaseCommit` value, and must not be copied into
the Gate 2 record without first re-verifying it is still the registry/P2-substrate HEAD.

Step 0 is mandatory: before editing, the builder restates the objective, allowed and forbidden
surfaces, frozen contracts, acceptance criteria, branch/worktree/base, checks, evidence, and stop
conditions, then stops for coordinator confirmation. No implementation begins from an ambient
checkout or from this shaping worktree.
