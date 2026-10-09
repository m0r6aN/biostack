---
parcel_id: P3-B
title: Parcel-schema binding to the frozen Product Capability and Safety Contract
status: review-candidate
owner: coordinator-assigns-at-gate-2
created: 2026-10-09
updated: 2026-10-09
delivery_classes: [standard]
guidance_classes: []
substance_function_risk: []
surfaces:
  - docs/specs/schemas/parcel-spec.schema.json
  - docs/specs/schemas/CAPABILITY-FIELD-MAP.md
  - docs/specs/schemas/classification-axes.schema.json
  - docs/specs/schemas/fixtures/p3b/positive-capability-bearing.json
  - docs/specs/schemas/fixtures/p3b/positive-non-capability-bearing.json
  - docs/specs/schemas/fixtures/p3b/positive-coordinator-parcel-p3b-self.json
  - docs/specs/schemas/fixtures/p3b/negative-capability-claim-missing.json
  - docs/specs/schemas/fixtures/p3b/negative-claim-behavior-drift.json
  - docs/specs/schemas/fixtures/p3b/negative-numeric-provenance-missing.json
  - docs/specs/schemas/fixtures/p3b/negative-escalation-missing.json
  - docs/specs/schemas/fixtures/p3b/negative-function-review-status-invalid.json
  - docs/specs/schemas/fixtures/p3b/negative-premature-public-enablement-claim.json
  - docs/specs/schemas/fixtures/p3b/positive-escalation-stage4-interaction-signal.json
  - docs/specs/scripts/verify-p3b.ps1
  - docs/specs/README.md
  - docs/specs/INDEX.md
---

# P3-B — Parcel-Schema Binding to the Product Capability and Safety Contract

Status: **REVIEW CANDIDATE — builder dispatch blocked**

Parcel ID: `P3-B`

Risk and routing: `standard` (self-declared delivery class, folded against
`delivery-class-controls.json`); architecture (charter parcel-tree designation — not itself a
`deliveryClass` label; "architecture" names one of the eight charter-tree rows, exactly as P3-A's
and P0-A's own headers already distinguish); one builder; two independent read-only adversarial
reviewers (charter D8: architecture parcels receive dual review regardless of the folded delivery
class's baseline reviewer count — the same combination P1, P2, P3-A, P0-A, and P0-B all carried).

This parcel is also the **first `coordinator-parcel`-shape file** under
`parcel-spec.schema.json`'s `specShapes.coordinator-parcel.appliesFrom: "P3-B.md-forward"` rule —
this file's own leading YAML frontmatter (above) is itself the first conforming instance of that
shape, carrying every frontmatter key `requiredFrontmatterKeysCommonToBothShapes` pins plus
`parcel_id`. See "Carry-over," below, for how this discharges P3-A's self-identified
carry-over item 3.

## Lineage and dependencies

- Governed-delivery charter SHA-256: `4CD390D631487DA8E97509205A186F242C4A83B762FFFA894BEEC8A21F4EFE22`
  (unchanged since P1, P2, P3-A, P0-A, and P0-B shaping; re-verified at this shaping time by
  direct `sha256sum docs/INITIATIVES/biostack-governed-delivery/CHARTER.md`). Charter line 133
  names this parcel's exact scope: *"P3-B: after P0-B freezes the Product Capability and Safety
  Contract, bind the parcel schema to required capability, claim, provenance, missingness,
  function-review, and escalation fields."* The dependency spine (charter line 152) reads
  `... -> P0-B -> P3-B -> P4 -> P6 -> P7 -> P0-C -> ...`.
- Closed P3-A spec: `docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md`, content SHA-256
  `4B928FBB80F042934B01E1F66371BF14967307B0D99BEA5BC02ACBDBF3BA633A`. Closure:
  `docs/INITIATIVES/biostack-governed-delivery/closures/P3-A.md`, SHA-256
  `FC27D09D543F8C4CE7E88107ED97405B14EB6FE080AADA3195C4BA9483C71CC7` — status `DONE`
  (implementation PR #525, commit `c3b4007`, merged `4fe5825`; remediation PR #527, merged
  `0fa8eec`). P3-A shipped `docs/specs/schemas/parcel-spec.schema.json` (the 15-key generic
  contract, `extensionSections: {}` present and empty), `docs/specs/schemas/
  SECTION-HEADING-MAP.md`, `docs/specs/schemas/EXTENSION-POINTS.md` (the three named extension
  points this parcel uses the `domain-overlay-insertion` one of), eight per-delivery-class
  templates, thirteen fixtures, and `docs/specs/scripts/verify-p3a.ps1`. P3-A explicitly reserved
  binding capability/claim/provenance/missingness/function-review/escalation fields as "P3-B's
  territory" (P3-A.md, Frozen surfaces, final bullet) and never performed it.
- Closed P0-B spec: `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md`, content SHA-256
  `4A49E3D6086670857D0190AD0032E8DE0C50DED14F9830C2ECFABE1A4B414347`. Closure:
  `docs/INITIATIVES/biostack-governed-delivery/closures/P0-B.md`, SHA-256
  `56EFADB44BA1D4F2439190B52269A63DF71CFC0830FF3CCFB147AC33A146981B` — status `DONE`
  (implementation PR #531, commit `36fa450`, merged `725a47e`; hardening fixes 1-3, commits
  `95252e6`/`67dd246`/`922ec87`; closure commit `1f17405`). P0-B shipped the **frozen** Product
  Capability and Safety Contract this parcel binds to:
  `docs/specs/schemas/product-capability-safety-contract.json` (SHA-256
  `020554BA39DEE79C5FD5A27CE2CC411B9A597848AB2EED1DA09059141DB7B05D`) and its human-readable
  mirror `docs/specs/schemas/product-capability-safety-contract.md` (SHA-256
  `C01E90877B7DB0C251824A3AAAECECEB56BF5832FC392F090205F2AD1B4159BA`). Both files are **read-only,
  frozen inputs to this parcel** — this spec references their IDs and live values; it never
  restates, copies, paraphrases, or alters a ruled cell, rule, or literal they carry. Per P0-B's
  own closure note, the next step on the dependency spine is exactly this parcel: *"P3-B binds the
  parcel schema (P3-A) to the frozen contract's capability/claim/provenance/missingness/
  function-review/escalation fields; P0-C then proves the allowed/degraded/refused/escalated
  behavior with fixtures."*
- P2 substrate (unchanged since P2's `done` closure, re-verified by direct `sha256sum` at this
  shaping time): `docs/specs/schemas/classification-axes.schema.json`, SHA-256
  `24121BC29321A2DF4B40FFA1499ECE9CBEDFEBA9887895A5F7939B41F3A64A2B`. Its `productGuidanceClass`
  axis currently carries `"controlSource": "none"` and `"controlBindingStatus":
  "deferred-to-P3-B"` — this parcel is that deferral's **named destination**, exactly mirroring
  how P0-B was the named destination for `substanceFunctionRisk`'s own, now-resolved,
  `"deferred-to-P0-B"` deferral. `docs/specs/schemas/delivery-class-controls.json`, SHA-256
  `E1E545CC01D1EC316FC3F137E80792ABAC1F5D071BD09E15C8B5C804B536B068` — read-only, consulted only
  to prove the new `extensionSections` key's normalized form is disjoint from its live
  `requiredSpecAdditions` union (the pinned `domain-overlay-insertion` invariant).
- Shaping base anchor **as observed at this shaping time**: the commit that closed P0-B
  (`1f17405…`, per `git log` on this worktree). This literal is cited for lineage traceability
  only and is **not** presented as the current `BaseCommit` — `main` may continue to advance after
  this shaping pass, so this literal must never be read, cited, or dispatched as if it were still
  current. `BaseCommit` is pinned at Gate 2 record creation time (see "Gate 2 builder handoff
  requirements," below), mirroring P3-A's, P0-A's, and P0-B's own `BaseCommit` discipline.
- Gate dependency: the charter dependency spine requires P0-B merged and closed before P3-B
  dispatch. P0-B is merged and closed (above). P3-B may therefore be **shaped, reviewed, and (once
  approved) dispatched** — this spec carries no further, not-yet-closed precondition the way P0-B's
  own spec did against P3-A/P0-A.
- P3-B uses the **single-anchor dispatch model** P2/P3-A established: the registry
  (`docs/specs/README.md`, `docs/specs/INDEX.md`) and the schema substrate this parcel composes
  already exist after P0-B, so the coordinator creates and commits the Gate 2 record before the
  builder branch starts, and the builder branch/worktree starts at that exact commit.

## Authorization boundary

P3-B is covered by the charter's **P1-P7 standing authorization** (standard class, architecture
dual review) — unlike P0-B, no separate owner design-gate ruling is required, because this parcel
decides no new product allowed-output: it **binds schema fields that point at** P0-B's
already-owner-ruled contract; it introduces no behavior P0-B did not already rule. Shaping and dual
review are authorized now; Gate 2 dispatch follows ordinary parcel lifecycle (approved spec, two
PASS reviews, coordinator-created Gate 2 record) — no additional owner gate is a precondition, and
this spec does not claim or imply one. Nothing in this spec authorizes production release; the
production-readiness `NO-GO / HOLD` verdict (charter D17) is untouched.

## Objective

Bind BioStack's generic parcel-spec schema (P3-A) to the **fields** — not the meanings — a
user-facing-function spec must carry once it declares a product-guidance-class or
substance/function-risk capability: a machine-checkable requirement that such a spec state (1)
which capability (guidance-class × substance/function-risk label pair) it claims, (2) what
allowed/degraded/refused/escalated behavior it claims for that pair, (3) each numeric output's
provenance origin, (4) how it handles missing required input per the ruled missing-input ladder,
(5) its function-review status and, when applicable, its named human owner, and (6) its escalation
handling when the claimed behavior is refused, escalated, or composite. **Every one of these six
bound fields is validated, live, against `product-capability-safety-contract.json`'s own IDs and
values** — this parcel never restates, copies, paraphrases, weakens, or strengthens a cell, rule,
or literal P0-B already froze; a spec's claim either matches the frozen contract's live value
exactly, or validation fails.

**Scope boundary (the load-bearing distinction this parcel exists to honor):** P3-B binds *fields*
— schema mechanics determining which frontmatter keys a spec must carry and what live cross-check
each key's value must satisfy. It does **not** decide, add, remove, redefine, or soften any
product allowed-output, applicability criterion, or behavior cell — every such decision is P0-B's,
already made and frozen. P3-B adds **no new required Markdown section or heading** — P3-A's
fold-live, heading-based required-section mechanism (`SECTION-HEADING-MAP.md`) remains
exclusively delivery-class-driven; this parcel's binding is **frontmatter-field-driven only**, a
deliberately distinct mechanism from P3-A's own, because "capability, claim, provenance,
missingness, function-review, and escalation" are per-function structured data a human reviewer
and a future automated consumer (P4's linter, an eventual runtime-integration parcel) both need to
read as values, not prose a heading-presence check could verify. P0-C's fixtures (proving the
allowed/degraded/refused/escalated behavior itself is achievable and that useful guidance survives
enforcement) remain **entirely out of scope** here — P3-B proves the schema is bound; P0-C proves
the bound contract's behavior.

## Normative input and authority sources

1. **`docs/INITIATIVES/biostack-governed-delivery/CHARTER.md`** (hash above) — this parcel's
   parcel-tree row (line 133) and the P1-P7 standing-authorization grant.
2. **`docs/INITIATIVES/biostack-governed-delivery/parcels/P0-B.md`** and
   **`docs/specs/schemas/product-capability-safety-contract.json`/`.md`** (hashes above) — the
   **sole, frozen source** for every capability/claim/provenance/missingness/function-review/
   escalation value this parcel's bound fields are checked against. No other document may be
   substituted or blended in for a bound field's live cross-check.
3. **`docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md`**,
   **`docs/specs/schemas/parcel-spec.schema.json`**, and
   **`docs/specs/schemas/EXTENSION-POINTS.md`** (hashes above) — the generic contract and the
   `domain-overlay-insertion` extension point this parcel uses verbatim, per its own pinned
   mechanic and the two pinned invariant sentences (document contract 1, below).
4. **`docs/specs/schemas/classification-axes.schema.json`** (hash above) — the closed
   `productGuidanceClass` (four labels) and `substanceFunctionRisk` (ten labels) vocabularies this
   parcel's bound fields are keyed to, and the one field this parcel is authorized to flip
   (`productGuidanceClass.controlBindingStatus`/`controlSource`).

## Deliverables

### Required document contract 1: `docs/specs/schemas/parcel-spec.schema.json` (modified)

The builder appends **exactly one** new key to the existing, empty `extensionSections: {}` object
— every one of the file's other 14 top-level keys, and every other key already nested under
`extensionSections` (there are none at this parcel's `BaseCommit`), remains byte-identical. The one
appended key is:

```json
"product-capability-safety-overlay": {
  "addedBy": "P3-B",
  "appliesToShapes": ["coordinator-parcel", "ticket-spec"],
  "bindsProductSemantics": false,
  "contractSource": "docs/specs/schemas/product-capability-safety-contract.json",
  "fieldMapSource": "docs/specs/schemas/CAPABILITY-FIELD-MAP.md",
  "boundFrontmatterKeys": [
    "function_review_status",
    "function_review_owner",
    "capability_claim",
    "numeric_provenance",
    "missingness",
    "escalation"
  ]
}
```

`"bindsProductSemantics": false` is this parcel's own structural-validation-only disclaimer,
mirroring P3-A's: a `"valid"` result under this overlay asserts field presence, closed-vocabulary
membership, and live-value agreement with the frozen contract **only** — it never asserts that a
function's actual runtime behavior matches its declared claim (that remains P0-C's and an eventual
runtime-integration parcel's job). The six `boundFrontmatterKeys` and their exact required-when
triggers, shapes, and live cross-reference rules are defined **once**, in
`CAPABILITY-FIELD-MAP.md` (document contract 2) — this schema file only names and points at them,
per the same composed-not-hardcoded discipline P3-A's own `SECTION-HEADING-MAP.md` pointer uses
for `delivery-class-controls.json`.

**Additive-only invariant (pinned, per `EXTENSION-POINTS.md`'s `domain-overlay-insertion`
subsection, quoted there verbatim):** the appended key's normalized form (`product-capability-
safety-overlay`, normalized identically to `SECTION-HEADING-MAP.md`'s own term normalization —
lowercase; `/` and `-` to space; whitespace collapsed; trailing `s` stripped per token) must not
equal any term already present in the live `requiredSpecAdditions` union
(`delivery-class-controls.json`, read live) and must not equal any other `extensionSections` key
(there are none at this `BaseCommit`, so this check is vacuously true but still asserted, not
skipped). No existing `extensionSections` key (none exist) is removed, renamed, or value-mutated.
`verify-p3b.ps1` asserts this invariant as a named check, failing `extension-point-not-additive`
on violation, exactly as `EXTENSION-POINTS.md` requires of every appending parcel.

### Required document contract 2: `docs/specs/schemas/CAPABILITY-FIELD-MAP.md` (new)

A Markdown document, structurally analogous to `SECTION-HEADING-MAP.md` but binding frontmatter
**fields** instead of Markdown **headings**. Contains exactly one table, header `Frontmatter key |
Bound category | Required when | Value shape | Live cross-reference rule | Frozen source`, and
exactly one row per bound key (six rows: `function_review_status`, `function_review_owner`,
`capability_claim`, `numeric_provenance`, `missingness`, `escalation`), populated exactly as
follows (the builder transcribes this table verbatim; no row's content is inventable or
approximate):

| Frontmatter key | Bound category | Required when | Value shape | Live cross-reference rule | Frozen source |
|---|---|---|---|---|---|
| `function_review_status` | function-review | unconditional — every spec of either shape (`coordinator-parcel` and `ticket-spec`) | one of the closed values `unreviewed`, `review-required`, `reviewed`, `not-applicable` | value must be a member of this closed four-value set; no other literal, including any disguised or cased variant, is accepted | `product-capability-safety-contract.json` `functionReviewStatus.rules` (D-B5 rule 1 names `unreviewed`; rule 3 names `review-required`/`reviewed`) |
| `function_review_owner` | function-review | `function_review_status` equals `review-required` | a non-empty string naming one human owner | present and non-empty if and only if `function_review_status` is `review-required`; absent (not merely empty) for the other three values | `product-capability-safety-contract.json` `functionReviewStatus.rules[2]` ("`review-required` names the human owner and blocks public enablement until `reviewed`") |
| `capability_claim` | capability + claim | `guidance_classes` is non-empty **or** `substance_function_risk` is non-empty | array of objects `{guidanceClass, label, behavior}`, one object per distinct declared `(guidanceClass, label)` pair the function actually occupies, plus (only for `guidanceClass: personalized-protocol-recommendation` entries) a fourth key `publiclyEnabled`, (only for `guidanceClass: safety-escalation` entries) a fourth key `escalationRule`, and (only for `guidanceClass: curated-evidence-guidance` entries presenting a dose-context numeric output, per the frozen contract's own `doseContextDefinition` test — referenced, never restated as a new rule) a fourth key `dosageContext: true` | `guidanceClass` is one of the four closed `productGuidanceClass` labels; `label` is one of the ten closed `substanceFunctionRisk` labels; for `guidanceClass` in `{deterministic-calculation, curated-evidence-guidance, personalized-protocol-recommendation}` (mapped to the contract's `C1`/`C2`/`C3` columns respectively), `behavior` must equal, byte-for-byte, `product-capability-safety-contract.json`'s `labels.<label>.behavior.<C1\|C2\|C3>.value` for that column; for `guidanceClass: safety-escalation`, `behavior` must equal the literal `escalated` (the literal defined once, live, at `cellSemantics.escalated`) and `escalationRule` must be an integer 1-4 indexing `escalationSemantics.rules`; a `personalized-protocol-recommendation` entry's `publiclyEnabled` must equal, live, `enablementState.biostackRecommendedOrigination.publiclyEnabled` (`false` at this parcel's `BaseCommit`, per D-B1(c)); a `curated-evidence-guidance` entry's `dosageContext` key, when present, must be the literal boolean `true` and is the sole self-declared trigger signal for the `numeric_provenance` row below (the spec author sets it if, and only if, `doseContextDefinition`'s own test is met — the test itself is never redefined here) | `product-capability-safety-contract.json` `labels.*.behavior.*.value`, `cellSemantics.escalated`, `escalationSemantics.rules`, `enablementState.biostackRecommendedOrigination.publiclyEnabled`, `doseContextDefinition` |
| `numeric_provenance` | provenance | at least one `capability_claim` entry's `guidanceClass` is `deterministic-calculation` or `personalized-protocol-recommendation`, **or** at least one `capability_claim` entry's `guidanceClass` is `curated-evidence-guidance` and that entry carries `dosageContext: true` (matching the Objective's unqualified "each numeric output's provenance origin" promise: the frozen contract's own `numericProvenance` rules apply to "every displayed number," and `doseContextDefinition` defines dose context by the *shape* of the output value, not by which `guidanceClass` produced it, so a `curated-evidence-guidance` function presenting a dose-context numeric output triggers this key exactly as a `deterministic-calculation`/`personalized-protocol-recommendation` function would) | array of one or more distinct string values | every entry must be a member of the live `numericProvenance.lockedOrigins` five-value array; the key's total absence (not an empty array) is the signal that no `capability_claim` entry meets the trigger condition | `product-capability-safety-contract.json` `numericProvenance.lockedOrigins`, `doseContextDefinition` |
| `missingness` | missingness | `capability_claim` is non-empty | object with keys `requiredInputs` (array of input-name strings) and `rungApplied` (object mapping each `requiredInputs` entry to one of `rung-1-refuse-invalid`, `rung-2-degrade-naming-missingness`, `rung-2-refuse-safety-material`, `rung-3-marker`) and, when any input is safety-material, `safetyMaterialInputs` (array, subset of `requiredInputs`) | every `safetyMaterialInputs` entry (an input the spec itself names as age, pregnancy/lactation status, identity/concentration, or prescribed-treatment scope) must map, in `rungApplied`, to `rung-2-refuse-safety-material` — never `rung-2-degrade-naming-missingness`, per D-B4 rule 2's safety-material refusal requirement; a `rungApplied` value outside the four-literal closed set fails | `product-capability-safety-contract.json` `missingInputLadder.rungs` (three rungs; rung 2's own text distinguishes degrade-naming-missingness from refuse-when-safety-material, hence the two `rung-2-*` literals) |
| `escalation` | escalation | any `capability_claim` entry's `behavior` is `refused`, `escalated`, `refused-and-escalated`, or `degraded-escalates-on-strong-signal`, **or** any entry's `label` is `acute-red-flag-or-emergency` (unconditionally, matching `preemptionOrder.stages[0]`'s unscoped membership and `escalationSemantics.rules[0]`'s unscoped "stops ordinary guidance immediately"), **or** any entry's `label` is `prescription-treatment-involved` **and** that entry's `guidanceClass` is `personalized-protocol-recommendation` (narrowed to the C3 column precisely because the live contract scopes this label's only `refused`/escalating cell there "prescribed-treatment-only" — `labels.prescription-treatment-involved.behavior.C3.scope` — per `escalationSemantics.rules[1]`'s own "alteration-of-treatment surfaces refuse" text; this label's C1/C2 `degraded` cells carry no such scope and do not trigger `escalation`) | object with keys `preemptionStage` (integer 1-4, the `preemptionOrder.stages[].stage` this function's escalating label(s) fall under) and `outputType` (must equal the literal `safety-escalation`) | `preemptionStage` must correctly place every escalating `capability_claim` label in its live `preemptionOrder` stage (e.g. `acute-red-flag-or-emergency` → stage 1); for any escalating label absent from `preemptionOrder.stages[0].labels` through `stages[2].labels` (stages 1-3's explicit lists), `preemptionStage` is determinately `4` — `preemptionOrder.stages[3]`, whose pinned role text, "calibrating labels union their obligations," is this contract's own residual-membership rule for every such label, not an invented assignment; this is the deterministic resolution for `interaction-or-contraindication-signal`'s own D→E escalating case (`capability_claim.behavior: degraded-escalates-on-strong-signal`, the live `labels.interaction-or-contraindication-signal.behavior.C3.value`), which names no stage in stages 1-3 and therefore resolves, by this same residual rule, to `preemptionStage: 4` — exercised by fixture `positive-escalation-stage4-interaction-signal.json` (document contract 4); `outputType` must equal the literal `safety-escalation`, per `escalationSemantics.rules[2]` ("Escalation is a distinct output type ... never a footnote") | `product-capability-safety-contract.json` `preemptionOrder.stages`, `escalationSemantics.rules[1]`, `escalationSemantics.rules[2]` |

**Trigger evaluation is cumulative, not exclusive:** a spec may trigger several of these five
conditional keys simultaneously (for example a `pregnancy-or-lactation` × `curated-evidence-
guidance` function triggers `capability_claim` and `missingness` but not `numeric_provenance`
unless it also claims a `deterministic-calculation`/`personalized-protocol-recommendation`
pairing, or its `curated-evidence-guidance` entry itself carries `dosageContext: true`, and not
`escalation` unless its claimed behavior or label also meets that key's own, now-scoped trigger).
`function_review_status` is always required, independent of every other trigger.

### Required document contract 3: `docs/specs/schemas/classification-axes.schema.json` (modified)

Exactly two field changes under `axes.productGuidanceClass`, and no other diff: `"controlSource"`
changes from `"none"` to `"docs/specs/schemas/CAPABILITY-FIELD-MAP.md"`; `"controlBindingStatus"`
changes from `"deferred-to-P3-B"` to `"bound-at-this-parcel"`. No label is added, removed, renamed,
or redefined; `applicabilityField` entries (already `"status": "defined"` since P2) are untouched;
`deliveryClass` and `substanceFunctionRisk` axis objects are untouched.

### Required document contract 4: ten fixtures under `docs/specs/schemas/fixtures/p3b/`

Each JSON fixture carries exactly the keys `input`/`expected`, matching P2's and P3-A's fixture
shape convention, with the same **independently-dispositive, single-violation** design rule P3-A's
document contract 6 pins (no fixture co-locates two violations).

- `positive-capability-bearing.json`: `input.syntheticSpec` is a `ticket-spec`-shape spec declaring
  `guidance_classes: [curated-evidence-guidance]`, `substance_function_risk:
  [interaction-or-contraindication-signal]`, and all six bound fields correctly populated —
  `capability_claim` carries one entry `{guidanceClass: curated-evidence-guidance, label:
  interaction-or-contraindication-signal, behavior: degraded}` (the live `C2` cell value for that
  label), `missingness` populated, `escalation` omitted (this entry's behavior, `degraded`, does
  not meet the escalation trigger), `numeric_provenance` omitted (no `deterministic-calculation`/
  `personalized-protocol-recommendation` entry present), `function_review_status:
  review-required` with `function_review_owner` present. `expected.result` is `"valid"`.
- `positive-non-capability-bearing.json`: `input.syntheticSpec` declares `guidance_classes: []`,
  `substance_function_risk: []`, `function_review_status: not-applicable`, and none of the other
  five bound fields present. `expected.result` is `"valid"` — proving the overlay's non-trigger
  path (empty axes → no conditional field required) resolves cleanly, exactly mirroring P3-A's own
  pass-through stance for these two axes.
- `positive-coordinator-parcel-p3b-self.json`: `input.specPath` points at this parcel's own file,
  `docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md` — the verifier reads the real,
  merged file (frozen, read-only input to the builder) rather than an inlined copy. Because this
  file's own frontmatter declares `guidance_classes: []` and `substance_function_risk: []`, the
  overlay's conditional fields do not trigger, and `function_review_status` is not present in this
  file's own frontmatter at shaping time (the coordinator-parcel shape's `appliesFrom` clause does
  not retroactively require every field this parcel itself introduces on the very file that
  introduces them; see "Hard constraints," `self-reference carve-out`). `expected.result` is
  `"valid"` under the `coordinator-parcel` shape — this is the fixture that discharges P3-A's
  carry-over item 3 (see "Carry-over," below).
- `negative-capability-claim-missing.json`: `guidance_classes` non-empty, `capability_claim` key
  absent; `expected.result` `"invalid"`, `expected.reason`
  `"missing-required-frontmatter-key"` naming `capability_claim`.
- `negative-claim-behavior-drift.json`: a `capability_claim` entry for `{guidanceClass:
  curated-evidence-guidance, label: pregnancy-or-lactation}` asserting `behavior: "allowed"`
  — the live contract's `C2` cell for that label is `degraded`, not `allowed`; `expected.result`
  `"invalid"`, `expected.reason` `"capability-claim-drift"`.
- `negative-numeric-provenance-missing.json`: a `capability_claim` entry with `guidanceClass:
  deterministic-calculation`, `numeric_provenance` key absent; `expected.result` `"invalid"`,
  `expected.reason` `"missing-required-frontmatter-key"` naming `numeric_provenance`.
- `negative-escalation-missing.json`: a `capability_claim` entry for label
  `acute-red-flag-or-emergency`, `escalation` key absent; `expected.result` `"invalid"`,
  `expected.reason` `"missing-required-frontmatter-key"` naming `escalation`.
- `negative-function-review-status-invalid.json`: `function_review_status: "pending"` (not a
  member of the closed four-value vocabulary); `expected.result` `"invalid"`, `expected.reason`
  `"invalid-function-review-status"`.
- `negative-premature-public-enablement-claim.json`: a `capability_claim` entry with
  `guidanceClass: personalized-protocol-recommendation`, `publiclyEnabled: true` (the live
  contract's `enablementState.biostackRecommendedOrigination.publiclyEnabled` is `false`);
  `expected.result` `"invalid"`, `expected.reason` `"premature-public-enablement-claim"`.
- `positive-escalation-stage4-interaction-signal.json`: `input.syntheticSpec` declares
  `guidance_classes: [personalized-protocol-recommendation]`, `substance_function_risk:
  [interaction-or-contraindication-signal]`, and a `capability_claim` entry
  `{guidanceClass: personalized-protocol-recommendation, label:
  interaction-or-contraindication-signal, behavior: degraded-escalates-on-strong-signal,
  publiclyEnabled: false}` — the live `C3` cell value for that label, the exact D→E escalating
  case `CAPABILITY-FIELD-MAP.md`'s `escalation` row's "Live cross-reference rule" column resolves
  by its stage-4 residual-membership rule (the label names no stage in `preemptionOrder.stages[0]`
  through `stages[2]`'s explicit lists). `escalation: {preemptionStage: 4, outputType:
  "safety-escalation"}`; `missingness` and `function_review_status`/`function_review_owner`
  populated correctly for the other triggered keys; `numeric_provenance` omitted (no
  `deterministic-calculation` entry and no `curated-evidence-guidance` entry present).
  `expected.result` is `"valid"` — this fixture is the sole, named discharge of the
  `preemptionStage` indeterminacy this parcel's own field map resolves for stage-4-residual
  labels; no other fixture exercises this label's escalating case.

### Required document contract 5: `docs/specs/scripts/verify-p3b.ps1`

Must implement, at minimum and in the same no-network, PowerShell-only, nonzero-on-failure style
as `verify-p3a.ps1`/`verify-p0b.ps1`: scope-and-frozen-surface checks, the additive-only
extension-point check, `CAPABILITY-FIELD-MAP.md` structural checks, the `classification-axes.
schema.json` diff-scoped check, the ten fixture checks (including the self-referential
`coordinator-parcel`-shape fixture and the stage-4-residual escalation fixture), and the standard
evidence-bundle and clean-tree checks. The full, numbered check list is specified exactly in
"Deterministic verification," below.

### Required document contract 6: `docs/specs/README.md` and `docs/specs/INDEX.md` amendments

`README.md` gets exactly one appended section titled `## Parcel-schema binding to the Product
Capability and Safety Contract (P3-B)`, linking `schemas/CAPABILITY-FIELD-MAP.md` and the
`extensionSections.product-capability-safety-overlay` entry in `schemas/parcel-spec.schema.json`,
stating in one sentence that P3-B binds six frontmatter fields (capability, claim, provenance,
missingness, function-review, escalation) to P0-B's frozen contract and decides no product
allowed-output of its own. No existing line removed, reordered, or reworded.

`INDEX.md` gets exactly one appended row, in the existing ten-column order, for `P3-B`: `Status` =
`review-candidate`; `Spec` links this file; `Goal Charter` links the charter; `Delivery classes` =
`standard; architecture`; `Guidance classes` = `not-applicable`; `Branch/worktree` and `Owner` both
use the literal closed registry value `coordinator-assigns-at-gate-2` (the same P2-originated,
P3-A/P0-A/P0-B-continued convention); `Review requirement` = `2 independent reviewers`; `Closure` =
`not-yet-closed`. No existing row changes.

## Exact allowed surfaces

The builder may create or modify only:

1. `docs/specs/schemas/parcel-spec.schema.json` — modified (one `extensionSections` key appended).
2. `docs/specs/schemas/CAPABILITY-FIELD-MAP.md` — new.
3. `docs/specs/schemas/classification-axes.schema.json` — modified (two fields under
   `productGuidanceClass` only).
4. `docs/specs/schemas/fixtures/p3b/positive-capability-bearing.json` — new.
5. `docs/specs/schemas/fixtures/p3b/positive-non-capability-bearing.json` — new.
6. `docs/specs/schemas/fixtures/p3b/positive-coordinator-parcel-p3b-self.json` — new.
7. `docs/specs/schemas/fixtures/p3b/negative-capability-claim-missing.json` — new.
8. `docs/specs/schemas/fixtures/p3b/negative-claim-behavior-drift.json` — new.
9. `docs/specs/schemas/fixtures/p3b/negative-numeric-provenance-missing.json` — new.
10. `docs/specs/schemas/fixtures/p3b/negative-escalation-missing.json` — new.
11. `docs/specs/schemas/fixtures/p3b/negative-function-review-status-invalid.json` — new.
12. `docs/specs/schemas/fixtures/p3b/negative-premature-public-enablement-claim.json` — new.
13. `docs/specs/schemas/fixtures/p3b/positive-escalation-stage4-interaction-signal.json` — new.
14. `docs/specs/scripts/verify-p3b.ps1` — new.
15. `docs/specs/README.md` — modified. Exactly one appended section, zero removed/reordered lines.
16. `docs/specs/INDEX.md` — modified. Exactly one appended row for `P3-B`, zero removed lines,
    zero other-row changes, using `coordinator-assigns-at-gate-2` in the branch/worktree and owner
    cells.

No other path may change.

### Frozen surfaces

The builder must not change or reinterpret:

- The governed-delivery charter, the plan-review record, and every closed artifact of P1, P2,
  P3-A, P0-A, and P0-B — including, specifically, `docs/specs/schemas/product-capability-safety-
  contract.json` and `.md` (this parcel's sole, frozen semantic source — read and cross-referenced
  live, never copied or edited), `docs/specs/schemas/SECTION-HEADING-MAP.md`, `docs/specs/schemas/
  EXTENSION-POINTS.md`, `docs/specs/schemas/delivery-class-controls.json`, `docs/specs/schemas/
  fold-engine.md`, `docs/specs/schemas/routing-output.schema.json`, `docs/specs/schemas/AXIS-
  REGRESSION-MAP.md`, every P2/P3-A/P0-B fixture, and `docs/specs/templates/**`.
- This parcel's own spec file, `docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md` — the
  self-referential fixture (document contract 4) reads it, never edits it; it is a frozen,
  already-merged input to the builder's own dispatch, exactly as `P0-B-DESIGN-GATE.md` was a
  frozen input to P0-B's builder.
- `classification-axes.schema.json`'s `deliveryClass` and `substanceFunctionRisk` axis objects,
  and every `productGuidanceClass.applicabilityField` entry (already `"defined"` since P2) — only
  the two named top-level fields under `productGuidanceClass` (document contract 3) may change.
- `docs/specs/CORE-CONTEXT.md`, `docs/specs/active/README.md`, `docs/specs/done/README.md`, every
  existing active/done spec.
- Root `AGENTS.md`.
- `docs/INITIATIVES/biostack-production-readiness/` and its `NO-GO / HOLD` verdict (charter D17).
- `frontend/`, `backend/`, `contracts/`, `.github/` — no runtime code, no product surface, no CI
  workflow. This parcel ships zero code.
- D1-D18, the ratified product doctrine, the four product guidance classes, the ten
  substance/function-risk labels, every ruled D-B1..D-B6 cell/rule/literal, the charter's parcel
  dependency spine, standing authorizations, stop conditions, and exit criterion.
- P0-C's territory: no fixture or check in this parcel proves that a declared capability claim's
  behavior is actually achievable at runtime, or that useful guidance survives enforcement — that
  remains P0-C's, under P0-C's own, separate, not-yet-granted gate.

If an allowed deliverable appears to require any frozen-surface or frozen-contract change, the
builder stops without editing it.

## Hard constraints

- **No product-semantic decision.** Every live cross-reference rule in `CAPABILITY-FIELD-MAP.md`
  compares a spec's declared value against `product-capability-safety-contract.json`'s own,
  already-frozen value; this parcel invents no new allowed/degraded/refused/escalated behavior,
  applicability criterion, provenance origin, missing-input rung, function-review rule, or
  escalation rule. A `capability_claim` entry's `behavior` must equal the live cell value exactly
  — never a looser or stricter paraphrase — and any mismatch is `capability-claim-drift`, a hard
  validation failure, not a judgment call for the builder or either reviewer.
- **Composed, not hardcoded.** `CAPABILITY-FIELD-MAP.md`'s "Live cross-reference rule" column
  describes *how* to look a value up in `product-capability-safety-contract.json` at verification
  time; it never embeds a copy of a matrix cell, a locked-origin literal, a ladder rung, or a
  review-status rule as a static value inside a P3-B file. `verify-p3b.ps1` must read the contract
  file live for every check; a verifier that hardcodes a copy of any contract value fails review.
- **No premature public enablement.** Every `capability_claim` entry whose `guidanceClass` is
  `personalized-protocol-recommendation` must carry `publiclyEnabled` equal to the live
  `enablementState.biostackRecommendedOrigination.publiclyEnabled` value (`false` at this parcel's
  `BaseCommit`); no file this parcel creates may state or imply that any `C3` capability is
  publicly available today.
- **No unresolved placeholder.** No file created or modified by this parcel may contain any of the
  banned placeholder markers or double-brace interpolation syntax `parcel-spec.schema.json`'s
  `noPlaceholderPatterns` names (read live, not redeclared, including its double-curly-brace
  pattern), or an unresolved decision, after the pinned `placeholderNormalizationSteps` pipeline is
  applied, reusing both P3-A mechanisms rather than redeclaring them.
- **Self-reference carve-out (narrowly scoped).** This parcel's own spec file, `parcels/P3-B.md`,
  is the first `coordinator-parcel`-shape file and therefore predates this parcel's own
  `boundFrontmatterKeys` by construction — the file that introduces the binding cannot itself have
  been authored against a binding that did not yet exist at its own authoring time. This spec's
  frontmatter therefore carries only the nine `requiredFrontmatterKeysCommonToBothShapes` plus
  `parcel_id` (P3-A's own, already-existing obligation), and the six fields this parcel's own
  deliverables introduce are validated against this file for **presence only where this file's
  own declared `guidance_classes`/`substance_function_risk` would trigger them** (they are both
  empty, so none trigger) — this file is not required to retroactively carry
  `function_review_status` as an unconditional field, because that unconditional requirement is
  itself a product of this parcel's own deliverables and does not apply backward onto the file
  that ships it. This carve-out is exactly as narrow as stated: it applies to this one file only,
  never to any future `coordinator-parcel`-shape file authored after this parcel merges.

## Deterministic verification

Run from the coordinator-named isolated P3-B worktree after committing all 16 deliverables:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File docs/specs/scripts/verify-p3b.ps1 `
  -BaseCommit <40-character-dispatch-anchor-SHA> `
  -BuilderId <dispatch-builder-id> `
  -ReviewerIds <reviewer-1-id>,<reviewer-2-id> `
  -EvidenceDirectory artifacts/p3b-verification
```

`verify-p3b.ps1` must implement exactly these checks, without network access or non-PowerShell
dependencies, and must exit nonzero on any failure:

1. Set `$ErrorActionPreference = 'Stop'`; resolve and enter `git rev-parse --show-toplevel`;
   require `HEAD` to descend from `BaseCommit` via `git merge-base --is-ancestor BaseCommit HEAD`.
2. Run `git diff --check "$BaseCommit...HEAD"`; require exit `0`.
3. Read `git diff --name-only "$BaseCommit...HEAD" --`; require the changed set to equal exactly
   the 16 allowed surfaces listed in this spec, sorted ordinally — no more, no fewer.
4. Require `git diff --quiet "$BaseCommit...HEAD" --` to exit `0` for: the charter path, every
   closed P1/P2/P3-A/P0-A/P0-B artifact (including `product-capability-safety-contract.json`/`.md`
   byte-for-byte), `SECTION-HEADING-MAP.md`, `EXTENSION-POINTS.md`, `delivery-class-controls.json`,
   `fold-engine.md`, `routing-output.schema.json`, `AXIS-REGRESSION-MAP.md`, every P2/P3-A/P0-B
   fixture path, `docs/specs/templates/`, `docs/specs/CORE-CONTEXT.md`, `docs/specs/active/
   README.md`, `docs/specs/done/README.md`, this parcel's own spec file `parcels/P3-B.md`,
   `AGENTS.md`, `docs/INITIATIVES/biostack-production-readiness`, `frontend`, `backend`,
   `contracts`, `.github`, and every path returned by `git ls-files docs/specs/active docs/specs/
   done` at `BaseCommit`.
5. Parse `parcel-spec.schema.json`; require exactly the same 15 top-level keys P3-A's own check 5
   pins, byte-identical values for all 14 keys other than `extensionSections`; require
   `extensionSections` to equal exactly `{"product-capability-safety-overlay": {...}}` with the
   six fields pinned in document contract 1, in the exact values pinned there.
6. Compute the live union of every `requiredSpecAdditions` entry in `delivery-class-controls.json`
   (read-only); normalize the string `product-capability-safety-overlay` identically to
   `SECTION-HEADING-MAP.md`'s own term normalization; require it to be absent from that live union
   and to not equal any other `extensionSections` key (none exist); fail
   `extension-point-not-additive` on violation — the pinned `domain-overlay-insertion` invariant.
7. Parse `CAPABILITY-FIELD-MAP.md`; require exactly the pinned six-row table with the exact header
   and the exact `Frontmatter key`/`Bound category`/`Required when`/`Value shape`/`Live
   cross-reference rule`/`Frozen source` cell content pinned in document contract 2.
8. Parse `classification-axes.schema.json`; require the diff since `BaseCommit` to touch only
   `axes.productGuidanceClass.controlSource` (new value: the literal path to `CAPABILITY-FIELD-
   MAP.md`) and `axes.productGuidanceClass.controlBindingStatus` (new value: `bound-at-this-
   parcel`); require every other byte of the file unchanged; fail `axis-binding-diff-scoped` on
   any other diff.
9. For each of the ten `fixtures/p3b/*.json` fixtures: parse JSON; require exactly the keys
   `input`/`expected`; for `positive-coordinator-parcel-p3b-self.json`, resolve `input.specPath`
   to the real file `parcels/P3-B.md` and evaluate it under the `coordinator-parcel` shape,
   applying the self-reference carve-out (Hard constraints) exactly as pinned; for the other three
   positive and six negative fixtures, evaluate the inlined `syntheticSpec` directly; require
   `expected.result`/`expected.reason` to match the validator's own independently computed result
   exactly, including every live cross-reference against `product-capability-safety-contract.json`
   (behavior-value match for `capability_claim`, locked-origin membership for `numeric_provenance`,
   safety-material rung enforcement for `missingness`, preemption-stage correctness and literal
   `safety-escalation` output type for `escalation`, closed four-value membership for
   `function_review_status`, presence-iff-review-required for `function_review_owner`, and live
   `publiclyEnabled` agreement).
10. Require the `INDEX.md` diff to add exactly one line matching `^\| P3-B \|` and remove zero
    lines; parse the added row and require its ten cells to equal exactly the pinned values in
    document contract 6. Require the `README.md` diff to remove zero lines, add one contiguous
    block, and require the added section's heading and links to match document contract 6 exactly.
11. For every file changed or added by this parcel, apply `parcel-spec.schema.json`'s pinned
    `placeholderNormalizationSteps` pipeline (read live) then search for its two
    `noPlaceholderPatterns`; require zero matches in every file (AC-P3B-06).
12. Require `EvidenceDirectory`, resolved against the repository root, to equal
    `<repo>/artifacts/p3b-verification`; create it; write UTF-8/LF `changed-files.txt`,
    `schema-check.json`, `extension-point-check.json`, `field-map-check.json`,
    `axis-binding-check.json`, `fixture-results.json` (one entry per fixture), and
    `verification-summary.json` naming every numbered check with `pass: true`, `BaseCommit`,
    `HEAD`, `BuilderId`, and ordered `ReviewerIds`.
13. Require `git ls-files --error-unmatch -- artifacts/p3b-verification` to fail (untracked).
    Parse `git status --porcelain=v1 --untracked-files=all`; every line must begin
    `?? artifacts/p3b-verification/`; any staged, unstaged, or other untracked path fails. Print
    `P3-B verification PASS` and exit `0` only after every check and this final clean-tree check
    pass.

Any exception, nonzero child-command exit where zero is required, missing evidence file, or
fixture mismatch is red. There is no exclusion list and no warning-only acceptance.

## Acceptance criteria

- **AC-P3B-01 — Overlay registered, additive-only:** `parcel-spec.schema.json`'s
  `extensionSections` carries exactly the one pinned `product-capability-safety-overlay` key, all
  14 other top-level keys byte-identical, and `extension-point-not-additive` does not fire.
- **AC-P3B-02 — Field map complete and composed:** `CAPABILITY-FIELD-MAP.md` carries exactly the
  pinned six-row table; no row's "Live cross-reference rule" embeds a copied contract value.
- **AC-P3B-03 — Axis binding flipped, scoped:** `classification-axes.schema.json`'s diff touches
  only `productGuidanceClass.controlSource`/`controlBindingStatus`, to exactly the pinned values.
- **AC-P3B-04 — Fixture proof, including the self-referential coordinator-parcel case:** all ten
  `fixtures/p3b` fixtures parse and the verifier reproduces every fixture's `expected` result and
  (when invalid) `reason` exactly, including `positive-coordinator-parcel-p3b-self.json` against
  this parcel's own merged spec file and `positive-escalation-stage4-interaction-signal.json`
  against the stage-4-residual `preemptionStage` resolution.
- **AC-P3B-05 — Live agreement, no drift:** every `capability_claim.behavior`, `numeric_provenance`
  entry, `missingness.rungApplied` safety-material mapping, and `escalation.preemptionStage`/
  `outputType` in every positive fixture resolves to the exact live value
  `product-capability-safety-contract.json` carries at verification time.
- **AC-P3B-06 — No unresolved placeholder anywhere:** no file under this parcel's surfaces
  contains a banned placeholder pattern after the pinned normalization pipeline.
- **AC-P3B-07 — No product-semantic invention:** no file under this parcel's surfaces assigns a
  new meaning, behavior, provenance origin, missing-input rule, or escalation rule beyond what
  `product-capability-safety-contract.json` already freezes.
- **AC-P3B-08 — Scope integrity:** the changed-file set equals exactly the 16 allowed surfaces;
  every frozen surface is byte-identical to `BaseCommit`.
- **AC-P3B-09 — Bounded registry edits:** the `INDEX.md` diff is exactly one appended row with
  zero removed lines and zero column changes; the `README.md` diff is exactly one appended section
  with zero removed lines.
- **AC-P3B-10 — Carry-over discharged:** P3-A's carry-over item 3 (the untested `coordinator-
  parcel`-shape branch) is discharged by `positive-coordinator-parcel-p3b-self.json` exercising
  this parcel's own, now-real, conforming `coordinator-parcel`-shape spec file.

## Acceptance-to-evidence map

| Acceptance criterion | Required evidence |
|---|---|
| AC-P3B-01 | `extension-point-check.json` |
| AC-P3B-02 | `field-map-check.json` |
| AC-P3B-03 | `axis-binding-check.json` |
| AC-P3B-04 | `fixture-results.json` (all ten fixtures) |
| AC-P3B-05 | `fixture-results.json` (live cross-reference sub-results) |
| AC-P3B-06 | placeholder-scan output (check 11) |
| AC-P3B-07 | `schema-check.json`/`field-map-check.json` plus reviewer scan |
| AC-P3B-08 | `changed-files.txt` plus frozen-path quiet-diff result |
| AC-P3B-09 | `changed-files.txt` plus bounded-diff result for `INDEX.md`/`README.md` |
| AC-P3B-10 | `fixture-results.json` entry for `positive-coordinator-parcel-p3b-self.json` |

## Dual review

Per the charter's parcel-tree entry (architecture) and D8, this is a dual-review parcel: two
independent, fresh-session, read-only adversarial reviewers, who do not see each other's findings,
each receive the same approved spec hash, `BaseCommit`, builder commit, worktree path, Gate 2
record, authorization identities, and the complete evidence bundle. Each review covers: live-
agreement fidelity (AC-P3B-05 — that no bound field's cross-check was hardcoded, copied, or
drifted from `product-capability-safety-contract.json`'s live values), the additive-only extension-
point invariant (AC-P3B-01), the self-referential `coordinator-parcel`-shape fixture's correctness
(AC-P3B-04/10), the no-product-semantic-invention constraint (AC-P3B-07), scope and frozen-surface
integrity, and every other acceptance item. The coordinator reproduces any disputed finding before
triage; reviewers never fix the parcel; rework returns to the same builder only through a new
scoped directive and reruns every deterministic check and both reviews.

## Standing authorization and tripwire

P3-B remains inside the charter's P1-P7 standing authorization **only while both of these hold:**

1. P3-B changes only schema-binding mechanics — the one `extensionSections` append, the field-map
   document, the one scoped `classification-axes.schema.json` flip, and proof fixtures — and
   touches no production surface (`frontend`, `backend`, `contracts`, `.github` all remain
   byte-identical to `BaseCommit`, enforced by check 4/AC-P3B-08).
2. P3-B makes no product-semantic decision of its own — every bound field's live cross-reference
   resolves against `product-capability-safety-contract.json`'s already-frozen values (AC-P3B-05/
   07); it adds, removes, or redefines no cell, rule, origin, rung, or review/escalation rule.

**Tripwire:** if, during shaping, review, or rework, any proposed change to this parcel would (a)
touch a production surface, (b) add, remove, redefine, soften, or strengthen any product allowed-
output, applicability criterion, provenance origin, missing-input rule, review rule, or escalation
rule beyond what `product-capability-safety-contract.json` already freezes, (c) hardcode or copy
any contract value into a P3-B file instead of cross-referencing it live, or (d) imply any `C3`/
`personalized-protocol-recommendation` capability is publicly available today, the coordinator
stops immediately, does not dispatch or merge under standing authorization, and returns to the
developer for an explicit human gate.

## Carry-over

P3-A's own closure record and spec text (`parcels/P3-A.md`, "Carry-over," item 3; also recorded as
a named Stop Condition) identified a self-disclosed, bounded gap: all of P3-A's own fixtures and
its real-spec compatibility set are `ticket-spec`-shape-adjacent only — no fixture or
compatibility-set row in P3-A ever exercised the schema's `coordinator-parcel` branch against an
actual `docs/INITIATIVES/*/parcels/*.md` file, because `parcel-spec.schema.json`'s own
`appliesFrom` clause scopes that shape to `P3-B.md`-forward, and no conforming file existed yet.
P3-A's spec named two acceptable dispositions: *"P3-B's own dispatch must either (a) include a
fixture or compatibility-set row exercising the `coordinator-parcel` branch against its own
conforming spec file, or (b) explicitly re-affirm this gap's continuation with a named reason."*

**This parcel discharges the gap via option (a), directly and concretely:** this spec's own file,
`docs/INITIATIVES/biostack-governed-delivery/parcels/P3-B.md`, is authored with the leading YAML
frontmatter `parcel-spec.schema.json`'s `coordinator-parcel` shape requires (every key in
`requiredFrontmatterKeysCommonToBothShapes` plus `parcel_id`), making it the first real,
conforming `coordinator-parcel`-shape file in the repository. `positive-coordinator-parcel-p3b-
self.json` (document contract 4) points `input.specPath` at this exact file and requires the
verifier to actually validate it under the `coordinator-parcel` shape, applying the narrowly scoped
self-reference carve-out (Hard constraints) for the six fields this parcel's own deliverables
introduce. This is not a synthetic stand-in: it is this parcel's own, real, merged artifact,
exercising the previously-untested code path against genuine content rather than a throwaway
fixture file.

## Stop conditions

Stop and return to the coordinator if:

- Any required target path is absent or materially different from the contract assumed here.
- A fixture's expected result cannot be derived deterministically from a live read of
  `product-capability-safety-contract.json` without a human policy call.
- P3-B would need to define, add, remove, redefine, soften, or strengthen any product allowed-
  output, applicability criterion, provenance origin, missing-input rule, function-review rule, or
  escalation rule — that is exclusively P0-B's already-closed territory.
- P3-B would need to prove, with fixtures, that a declared capability claim's behavior is actually
  achievable at runtime, or that useful guidance survives enforcement — that is exclusively P0-C's,
  under P0-C's own, separate, not-yet-granted gate.
- Any file would state, imply, or could reasonably be read as stating that any `personalized-
  protocol-recommendation`/`C3` capability is publicly available today.
- The compatibility-set pass (if a reviewer requests one) would require editing any existing
  active/done spec or `INDEX.md`'s existing rows to "fix" a finding.
- The production-readiness initiative or release verdict would change.
- A branch, worktree, base commit, spec hash, owner, or reviewer assignment is ambiguous.
- Any deterministic check fails twice, a frozen contract must change, a reviewer finds an
  out-of-scope effect, or the standing-authorization tripwire above fires.

## Rollback

P3-B is documentation/schema-only. Rollback is a normal revert of the P3-B implementation commit
on its isolated branch, followed by the same scope, frozen-surface, fixture, and placeholder
checks. Do not delete or rewrite unrelated worktrees, branches, history, or ambient untracked
files. Nothing downstream depends on this parcel's output at runtime — no product surface reads or
enforces the bound fields until P0-C (fixtures) and an eventual, separately gated runtime-
integration parcel exist and are themselves dispatched.

## Gate 2 builder handoff requirements

The coordinator writes the Gate 2 record before builder dispatch, naming: the approved spec path
and SHA-256, the single `BaseCommit` dispatch anchor SHA, the isolated branch, the isolated
worktree path starting at that anchor, the builder identity, the 16 builder-editable surfaces, the
permission envelope, the deterministic verification command with both identities and `BaseCommit`
realized, the evidence destination `artifacts/p3b-verification`, and the two reviewers. Like P2,
P3-A, and P0-B, P3-B's Gate 2 record is created out-of-band before the dispatch anchor and is not
part of the builder's diff — the registry and composed substrate P3-B depends on already exist.

**`BaseCommit` is pinned at the moment the coordinator writes this Gate 2 record, not at this
spec's shaping/approval time.** The coordinator re-verifies the registry/P0-B-substrate HEAD at
Gate 2 record creation and records that commit's full 40-character SHA as `BaseCommit`; the
shaping-time anchor named in "Lineage and dependencies" above is a shaping-time observation, not a
stale-but-current `BaseCommit` value, and must not be copied into the Gate 2 record without first
re-verifying it is still the registry/P0-B-substrate HEAD.

Step 0 is mandatory: before editing, the builder restates the objective, allowed and forbidden
surfaces, frozen contracts, acceptance criteria, branch/worktree/base, checks, evidence, and stop
conditions, then stops for coordinator confirmation. No implementation begins from an ambient
checkout or from this shaping worktree.
