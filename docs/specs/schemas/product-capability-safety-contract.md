# Product Capability and Safety Contract (P0-B)

This document and `product-capability-safety-contract.json` must never diverge; `matrix-fidelity`
(Deterministic verification) fails closed if they do.

`"schema": "biostack.product-capability-safety-contract.v1"` · `"contractVersion": "1.0.0"`.

## How to read this contract

Every clause below carries one of two provenance markers, exactly as `docs/INITIATIVES/
biostack-governed-delivery/parcels/P0-B.md`'s Deliverables section defines them:

- **`[RULED — verbatim]`** — a verbatim transcription of a clause the owner already ruled in
  Coordinator Decision **D-I** (`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md#D-I`),
  sourced from `docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` §3 (D-B1..D-B6).
  These clauses are **owner-ruled and immutable without reopening the owner gate** — this parcel,
  its builder, and its reviewers transcribe them; they do not re-litigate, soften, strengthen, or
  extend them. Any apparent need to deviate is a stop-and-report to the owner, never a judgment
  call.
- **`[OPERATIONALIZED — bounded]`** — a deterministic, narrowly-scoped test this parcel authors to
  decide *whether* a closed-vocabulary label attaches to a given function invocation. These
  clauses are reviewable and amendable at P0-B's own review tier (dual independent review) without
  reopening the owner gate, because they decide *how a label is detected*, never *what the product
  may do once detected* — that remains the ruled matrix.

A future reader (P0-C's fixture builder, a runtime-integration parcel's builder) can therefore
tell, clause by clause, which content is frozen doctrine and which is this parcel's own bounded,
reviewable operationalization.

## Normative inputs

Every source this contract transcribes or binds, named by exact path and its SHA-256 at
`BaseCommit`:

| Source | Path | SHA-256 at BaseCommit | Role |
|---|---|---|---|
| Design-gate decision package | `docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` | `34928AC8C54FE8AF9CB7B3A01D36820FE435F2ADF4F02F56C20E7CF548D57F5B` | sole source for every `[RULED — verbatim]` clause this contract transcribes (§3, D-B1..D-B6) |
| Coordinator decisions ledger | `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` | `4B960CF2C5E84F5631996FDE46AF67CC984A498A864FBD53140CEAE2547574CA` | ruling record and authorization-to-shape chain (D-G, D-H, D-I, D-J, D-K); whole-file hash is a freshness check only — the load-bearing anchors are the named entry text spans |
| Governed-delivery charter | `docs/INITIATIVES/biostack-governed-delivery/CHARTER.md` | `4CD390D631487DA8E97509205A186F242C4A83B762FFFA894BEEC8A21F4EFE22` | ratified product doctrine: may/must-not lists, the four product guidance classes, the closed substance/function-risk vocabulary, D12-D16 |
| Guidance-content-contract v1.0.0 | `docs/guidance/biostack-guidance-content-contract.v1.md` | `60729197B1609AB489B921AE6A9840C1DA485CBA6F9B4B1A5CA6BD0FE4E8291F` | still-governing Class A/B/C/D taxonomy, copy-guard terms, warning/uncertainty markers, approval-level vocabulary, reused verbatim for the now posture |
| Classification-axes schema | `docs/specs/schemas/classification-axes.schema.json` | `49F9FCFA18AF08202069812BA74BEBD0ADF1A836ADC62DA2E5063CAC80FA0A14` | P2 substrate this parcel binds: the ten closed `substanceFunctionRisk` labels and their deferred applicability fields, which this parcel's `labels` object defines |
| Delivery-class controls | `docs/specs/schemas/delivery-class-controls.json` | `E1E545CC01D1EC316FC3F137E80792ABAC1F5D071BD09E15C8B5C804B536B068` | P2 substrate this parcel's Deterministic verification folds against |

## Enablement state — the D-B1(c) staged split

**`[RULED — verbatim, structurally encoded]`** — `P0-B-DESIGN-GATE.md` §3 "D-B1," lines 81-91;
Coordinator Decision D-I, item 1.

```json
{
  "biostackRecommendedOrigination": {
    "definedInContract": true,
    "publiclyEnabled": false,
    "currentPosture": "deterministic-math-on-user-entered-values-plus-guidance-contract-v1.0.0-class-A-B-C",
    "governingGuidanceContractVersion": "1.0.0",
    "requiredGuidanceContractVersionForPublicEnablement": "2.0.0",
    "requiredApprovalLevelForPublicEnablement": "legal_product_ratification",
    "publicEnablementEvent": "not-yet-occurred-separate-future-owner-event",
    "rulingReference": "COORDINATOR-DECISIONS-2026-10-07.md#D-I"
  }
}
```

`publiclyEnabled` **is, and must remain, the literal boolean `false`** in every dispatch of this
contract until a future, separately gated parcel records the v2.0.0 re-ratification +
`legal_product_ratification` event and flips it. This parcel does not flip it, schedule its
flipping, or define the mechanism that will flip it.

## Preemption order

**`[RULED — verbatim]`** — `P0-B-DESIGN-GATE.md` §3 "D-B2," the paragraph following the per-label
table (lines 115-119):

> Multi-label composition follows D14's fieldwise fold with this preemption order:
> `acute-red-flag-or-emergency` → `controlled-or-illegal-sourcing` (surface-scoped) → refusal-cap
> labels (`minor-or-age-uncertain`, `pregnancy-or-lactation`, `prescription-treatment-involved`
> for the prescribed treatment) → calibrating labels union their obligations. "Most restrictive
> wins" only on a genuine scalar conflict; no label erases another's obligations.

Encoded as four ordered stages:

1. `acute-red-flag-or-emergency` — preempts ordinary guidance.
2. `controlled-or-illegal-sourcing` — suppresses sourcing, evasion, and concealment assistance
   (surface-scoped).
3. Refusal-cap labels: `minor-or-age-uncertain`, `pregnancy-or-lactation`,
   `prescription-treatment-involved` (for the prescribed treatment only).
4. Calibrating labels union their obligations.

`compositionNote`: *"Most restrictive wins" only on a genuine scalar conflict; no label erases
another's obligations.*

## Numeric provenance (D-B3)

**`[RULED — verbatim]`** — `P0-B-DESIGN-GATE.md` §3 "D-B3" (lines 121-131).

Five locked origins: `user-entered`, `label-or-prescription-transcribed`, `source-studied`,
`biostack-recommended`, `deterministically-derived`.

1. Every displayed number carries a machine-readable origin from the five locked values and a
   visible origin marker on dosing-context surfaces.
2. `biostack-recommended` values additionally require rationale, evidence applicability,
   uncertainty, and risk controls rendered alongside — never as bare numbers.
3. No prefilled or recommended value may render in a neutral-arithmetic frame (e.g., inside a
   calculator input without its provenance marker).
4. Missing provenance → the numeric output is **refused** (fail-closed), not downgraded.

No file this parcel produces frames a prefilled or recommended value as neutral arithmetic, and
missing provenance is never silently downgraded — it is a hard refusal.

## Missing-input ladder (D-B4)

**`[RULED — verbatim]`** — `P0-B-DESIGN-GATE.md` §3 "D-B4" (lines 133-140).

1. Arithmetic on declared inputs: refuse silently-invalid math (units, plausibility bounds,
   decimal-error flags per Class B); never guess an input.
2. Recommendation origination (post-v2.0.0): required-input set is function-declared; any missing
   required input → degraded output naming the missingness, or refusal when the missingness is
   safety-material (age, pregnancy status, identity/concentration, prescribed-treatment scope).
3. `PARTIAL_PACKET` / `OUTSIDE_REVIEWED_CONTEXT` markers fire per the v1.0.0 marker vocabulary
   (`docs/guidance/biostack-guidance-content-contract.v1.md`, "Required warning and uncertainty
   language" table — reused by reference, not re-defined).

## Function-review status (D-B5)

**`[RULED — verbatim]`** — `P0-B-DESIGN-GATE.md` §3 "D-B5" (lines 142-150).

1. `unreviewed` functions are **internal staging only** (mirrors `automated_candidate`) — never
   public-facing output.
2. Public dosing-context UX requires `legal_product_ratification` against the governing contract
   version (existing v1.0.0 rule, preserved).
3. `review-required` names the human owner and blocks public enablement until `reviewed`.
4. Class-triggered merge approvals (health-boundary, privacy, legal-policy, knowledge-promotion)
   continue to fire regardless of any standing Gate 3 authorization (D14 fold, D9).

## Escalation semantics (D-B6)

**`[RULED — verbatim]`** — `P0-B-DESIGN-GATE.md` §3 "D-B6" (lines 152-161).

1. `acute-red-flag-or-emergency`: output stops ordinary guidance immediately; surfaces urgent
   professional/emergency language; suppresses calculators and dose context.
2. `prescription-treatment-involved`: alteration-of-treatment surfaces refuse; the
   professional-involvement template escalates; evidence comparison remains available.
3. Escalation is a distinct output type (class `safety-escalation`), never a footnote on an
   otherwise ordinary recommendation.
4. Escalation language is drawn from approved templates (v1.0.0 approval levels: high-impact
   safety wording requires `clinical_safety_copy_review`).

## Cell semantics

**`[RULED — verbatim]`** — `P0-B-DESIGN-GATE.md` §3 "D-B2," opening sentence (lines 95-96):

> Cell values: **A** = allowed, **D** = degraded (allowed with mandatory
> evidence/explanation/validation additions), **R** = refused, **E** = escalate (stop ordinary
> output, surface escalation).

| Literal | Meaning |
|---|---|
| `allowed` | A — allowed |
| `degraded` | D — degraded (allowed with mandatory evidence/explanation/validation additions) |
| `refused` | R — refused |
| `escalated` | E — escalate (stop ordinary output, surface escalation) |

A `degraded` cell's "mandatory evidence/explanation/validation additions" is a structural
obligation of the literal itself, not satisfied by a cosmetic reduction (for example, a bare
disclaimer sentence with no evidence, explanation, or validation content added).

## Dose-context definition (D-J)

**`[RULED — verbatim]`** — Coordinator Decision D-J
(`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md#D-J`):

> **Dose-context output.** An output is in dose context if and only if any value it presents or
> derives is a **compound amount or an amount-derived quantity**: a dose or target amount,
> concentration, reconstitution/dilution volume, split or load amount, per-administration or
> per-period amount, cumulative amount, or a syringe-unit rendering of any of these. The five
> D-B3 numeric-provenance origins attach exactly to these values. Outputs carrying no
> compound-amount value (e.g., calendar/interval arithmetic over non-amount quantities) are not
> dose-context. The guidance-contract v1.0.0 term "public dosing-context UX" denotes user-facing
> surfaces that render dose-context outputs, and therefore gates identically.

This definition governs the `"dose-context-only"` scope value on the `pregnancy-or-lactation` C1
cell, below. It changes no ruled cell, row, or rule — it only pins the previously-undefined term
"dose-context" the owner-ruled `pregnancy-or-lactation` × C1 = `R (dose-context)` cell already
used.

## Precedence directional constraint (D-K) — context only

Coordinator Decision D-K (`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md#D-K`) travels with
this contract as context, not as a D-B1..D-B6 clause:

> The precedence manifest answers **document authority order only**. Where the mechanically
> resolved order favors a more permissive text over a narrower safety prohibition, the
> prohibition **stands unchanged** until an explicit owner ruling supersedes it. Such rows route
> to the owner through P0-D's disposition process; a P0-D builder must never weaken a safety
> prohibition by rank alone.

None of this contract's ruled matrix, preemption order, or locked-label behavior is weakened,
softened, or made conditional by any precedence-rank argument. D-K is transcribed here solely so a
reader of this contract cannot cite rank mechanics as license to do so.

## Per-label behavior matrix (required content floor)

**`[RULED — verbatim]`** — `P0-B-DESIGN-GATE.md` §3 "D-B2," lines 93-119. Guidance-class order:
**C1** `deterministic-calculation`, **C2** `curated-evidence-guidance`, **C3**
`personalized-protocol-recommendation` (every `C3` cell is additionally gated by
`enablementState.biostackRecommendedOrigination.publiclyEnabled` — while `false`, no `C3` behavior
for any label may be exposed on a public surface regardless of its table value, per
`"enablementGatesC3": true` on every label).

| Label | C1 | C2 | C3 (post-v2.0.0) | Calibration required |
|---|---|---|---|---|
| `ordinary` | A | A | A | none beyond base contract |
| `prescription-treatment-involved` | D | D | **R** (for the prescribed treatment) | no alteration directives; "discuss with prescriber" template; comparison to reviewed evidence allowed |
| `investigational-or-unapproved` | A | D | D | `EVIDENCE_LIMITED`/`EVIDENCE_CASE_REPORT` markers; no "established" framing; mandatory monitoring + uncertainty blocks |
| `gray-market-or-identity-uncertain` | D | D | D | identity/purity uncertainty surfaced; potency-dependent math refused when identity/concentration unverified (missing-input rule) |
| `injection-or-sterile-preparation` | A | D | D | sterility/prep safety context section required; refusal when sterility inputs missing |
| `interaction-or-contraindication-signal` | A | D | D→E | conflict disclosure mandatory (never hidden); "review before proceeding" Class C template; escalate on strong signal |
| `minor-or-age-uncertain` | D | D | **R** | age-dependent inputs refused; `POPULATION_MISMATCH` markers; professional escalation language |
| `pregnancy-or-lactation` | R (dose-context) | D | **R** | no "proceed anyway" framing; mandatory professional escalation |
| `acute-red-flag-or-emergency` | R | R | **R** + **E** | LOCKED — preempts ordinary guidance; urgent professional/emergency language |
| `controlled-or-illegal-sourcing` | A¹ | A¹ | A¹ | LOCKED — suppresses sourcing/evasion/concealment surfaces only; evidence content unaffected |

¹ Except any surface that would source, evade, or conceal — those are refused with no fallback.

`interaction-or-contraindication-signal`'s `C3` cell, `D→E`, is encoded as
`"degraded-escalates-on-strong-signal"` in the JSON artifact (not a fifth behavior literal) — a
strong interaction/contraindication signal (as defined by the function's own declared
evidence-source match threshold) upgrades the cell from `D` to `escalated` at evaluation time; the
table's static value is the floor, not the ceiling, for this one cell.

`acute-red-flag-or-emergency`'s `C3` cell, `R` + `E`, is encoded as `"refused-and-escalated"` in
the JSON artifact (a `compositeOf: ["refused", "escalated"]`, not a fifth behavior literal),
composing D-B6 rule 1 (ordinary guidance stops immediately) with D-B6 rule 3 (escalation is a
distinct output type) — the same composite-cell encoding discipline the `D→E` cell above uses.

## Per-label applicability criteria (`[OPERATIONALIZED — bounded]`)

Deterministic tests over a function's own declared capability-contract fields, evaluated against
the function's actual declared inputs/outputs at invocation time. Multiple criteria may be true
simultaneously (multi-label, per D14); `ordinary` is the residual label.

| Label | Deterministic applicability test |
|---|---|
| `ordinary` | True when none of the other nine labels' tests below are true for this invocation. |
| `prescription-treatment-involved` | True when a declared input or output names a substance/treatment that the function's own capability contract, or a `label-or-prescription-transcribed` input, marks prescription-status, or the user has declared it as a currently prescribed/clinician-directed treatment. |
| `investigational-or-unapproved` | True when the cited evidence source's own regulatory-status field states investigational, unapproved, or off-label for the declared use, or no approved-use record exists in the evidence source for the declared use. Deterministic threshold: an output with **zero cited evidence sources** for the declared use is **out-of-scope** for this criterion (not vacuously true) and instead defers to that function's own missing-input ladder behavior (`missingInputLadder`, rule 2) — this criterion only attaches once at least one evidence source is actually cited and its regulatory-status field is read. |
| `gray-market-or-identity-uncertain` | True when a required identity/concentration/manufacturing-source provenance field (per `numericProvenance` and the function's declared inputs) is absent, unverified, or not resolvable to one of the five locked numeric origins. |
| `injection-or-sterile-preparation` | True when the function's declared output type or route is injection, reconstitution, or any sterile-preparation step. |
| `interaction-or-contraindication-signal` | True when a matching interaction/contraindication record exists in the cited evidence source for the user's declared concurrent substances, medications, or conditions. Deterministic criterion: the base-applicability matching algorithm itself (exact substance-name match vs. drug-class/mechanism match vs. any broader match) is **function-declared**, not fixed by this contract — the same deferral-to-function pattern already used for `acute-red-flag-or-emergency`'s "triggering criterion set is itself function-declared," below. That same function-declared algorithm also carries the function's own declared strength grading — exactly the quantity this table's `C3` cell's `D→E` strong-signal escalation `[cite: D-B2.interaction-or-contraindication-signal.C3]` names as "the function's own declared evidence-source match threshold": base applicability (does a record match at all) and the `D→E` upgrade (is the matched record's signal strong) are therefore the matching-vs-grading components of one function-declared matching specification, not two independently defined, unrelated specifications. |
| `minor-or-age-uncertain` | True when the user's declared age is below the function's declared minimum-age threshold, or age is a function-declared required input and is missing or unverified. |
| `pregnancy-or-lactation` | True when the user has declared current pregnancy or lactation status, or that status is a function-declared required input and is missing/unverified for a function whose substance or output carries a source-labeled pregnancy/lactation signal. |
| `acute-red-flag-or-emergency` | **LOCKED.** True when declared symptoms, vitals, or context match any function-declared red-flag/emergency criterion; the triggering criterion set is itself function-declared (per D15's function-specific-review model), not invented by this contract. |
| `controlled-or-illegal-sourcing` | **LOCKED.** True whenever the declared request or context seeks sourcing, acquisition, legal/regulatory evasion, or concealment of a controlled-or-illegal substance or its acquisition — evaluated against the request/context, never against the mere mention of a controlled substance's evidence content. |

**`[OPERATIONALIZED — bounded]`** — a criterion that cannot be evaluated deterministically from a
function's own declared fields at invocation time (for example, an undeclared capability-contract
field a later function needs) defers to that function's own missing-input ladder behavior
(`missingInputLadder`, rule 2) — it never causes this contract itself to guess an applicability
determination.

## Labels — full per-label record

Each of the ten closed `substanceFunctionRisk` labels (`classification-axes.schema.json`), in the
JSON artifact's `"labels"` object, each carrying `"locked"`, `"applicabilityCriterion"`,
`"behavior"` (`C1`/`C2`/`C3`, with `"scope"` where the ruled table carries a parenthetical
qualifier), and `"calibrationRequired"`. The table and criteria above are this record's
required-content floor, reproduced per label in the JSON artifact with full citation apparatus
(`"basis"` on every criterion). `"locked": true` for exactly `acute-red-flag-or-emergency` and
`controlled-or-illegal-sourcing`; `"locked": false` for the other eight. `"enablementGatesC3":
true` on every label object.

Qualified cells (`"scope"` non-null):

- `pregnancy-or-lactation` `C1`: `"dose-context-only"` (governed by D-J, above).
- `prescription-treatment-involved` `C3`: `"prescribed-treatment-only"`.

Every other cell's `"scope"` is `null`.
