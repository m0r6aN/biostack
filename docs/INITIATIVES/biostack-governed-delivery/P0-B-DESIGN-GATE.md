# P0-B Design Gate — What the Product May Say

Status: **OPEN — awaiting owner rulings (D-B1..D-B6 below)**

Coordinator presentation for the owner's fresh P0-B gate, as required by Coordinator Decision
**D-G** ("P0-B ... returns to the owner for a fresh gate **with its design presented** (it decides
product allowed-outputs)"). Opened by the owner directive of 2026-10-08 (Coordinator Decision
**D-H**): *"gate 3 is cleared and open. On to P0-B design gate — the one where we decide what the
product may say."*

This document is a **decision package, not a spec**. It presents the design decisions that
constitute P0-B's Product Capability and Safety Contract so the owner can rule on them. No
product behavior changes here, nothing is dispatched, and no canon document is modified. After
rulings land, the ratified matrix is encoded in the P0-B parcel spec — which still sequences
after P0-A freezes canon precedence (charter dependency spine; see "Sequencing" below).

---

## 1. What P0-B decides (charter scope, fixed)

From the governed-delivery charter, P0-B is the machine-readable **Product Capability and Safety
Contract**:

1. **Deterministic applicability criteria** for each substance/function-risk label (when does a
   label attach to a user-facing function or output).
2. **Allowed / degraded / refused / escalated behavior** for each label across the four product
   guidance classes (`deterministic-calculation`, `curated-evidence-guidance`,
   `personalized-protocol-recommendation`, `safety-escalation`).
3. **Numeric provenance** rules for the five locked origins (`user-entered`,
   `label-or-prescription-transcribed`, `source-studied`, `biostack-recommended`,
   `deterministically-derived`) and presentation rules that keep a prefilled or recommended value
   from masquerading as neutral arithmetic.
4. **Function-review status** semantics (`unreviewed` / `review-required` / `reviewed` /
   `not-applicable`) and what each status permits publicly.

Already locked by the ratified charter — **not open at this gate** (changing these requires a
charter amendment and re-review):

- `acute-red-flag-or-emergency` **always preempts** ordinary guidance.
- `controlled-or-illegal-sourcing` **always suppresses** sourcing, evasion, and concealment
  assistance (that surface family only — it does not blanket-suppress evidence content).
- Other labels **calibrate** evidence, explanation, validation, review, and escalation rather
  than automatically suppressing useful guidance.
- Governing principle: **high risk requires more evidence, explanation, validation, review, and
  escalation — not less useful information.**
- The prohibition set (diagnose, prescribe, impersonate, direct alteration of prescribed
  treatment without professional involvement, hide uncertainty/signals, guarantee outcomes,
  emergency bypass, sourcing facilitation).

---

## 2. The central conflict this gate must resolve

Two fully ratified canons disagree about the headline question — **may the product originate
personalized numerical guidance?**

| Canon | Position |
|---|---|
| `docs/guidance/biostack-guidance-content-contract.v1.md` (v1.0.0, fully ratified 2026-08-02) | Class D — personalized medical direction — **Prohibited**. Banned patterns include "The best dose for you", "Recommended dose for your profile", "Based on your weight, start at 1 mg". Its Versioning clause: *"Any change that alters permitted/prohibited classes or public wording authority requires a new minor/major version and re-ratification for public surfaces."* |
| Governed-delivery charter (ratified 2026-10), **D13** + product doctrine | **Personalized numerical guidance is allowed**: "BioStack may originate evidence-bounded numerical recommendations, including dose targets, reconstitution choices, and schedules, when the applicable capability contract permits it" — as recommendations, not prescriptions, with numeric provenance, rationale, uncertainty, risk controls, and escalation. |

P0-A's contradiction inventory will catalogue this conflict and its precedence manifest will make
authority order answerable by lookup. But the **product decision itself is the owner's** — that is
exactly what this gate is. The owner's ruling below (D-B1) becomes the doctrine P0-B encodes;
P0-A's manifest then records which canon document carries it.

Note the boundary that both canons already agree on: **deterministic math on user-entered values**
(reconstitution, concentration, split, volume, syringe-unit display) is arithmetic on declared
inputs — the charter lists it under "may", and the v1.0.0 contract permits "deterministic unit/
frequency normalization (no LLM for the math)". The contested zone is **origination** — BioStack
itself producing a recommended target (dose, schedule) from profile context.

---

## 3. Decision set (owner rulings requested)

### D-B1 — Personalized-numerical posture (the headline "what may the product say" ruling)

| Option | Meaning |
|---|---|
| (a) Full D13 enablement now | Product may originate profile-aware recommendations immediately; requires guidance-content-contract **v2.0.0** (replaces Class D's blanket prohibition with the charter's calibrated regime) + re-ratification + `legal_product_ratification` before any public surface ships it. |
| (b) v1.0.0 stands | Personalized direction stays prohibited. Charter D13 capabilities remain defined but dormant. Product says Class A/B/C only indefinitely. |
| **(c) Staged split (RECOMMENDED)** | **Now:** Class A/B/C surfaces + deterministic math on user-entered values (provenance `user-entered`/`deterministically-derived`) + Class B comparisons + Class C harm-reduction templates — all of which v1.0.0 already permits. **Gated behind v2.0.0:** `biostack-recommended` origination (dose targets, schedules, profile-aware picks) under the full P0-B capability contract (provenance, rationale, evidence applicability, uncertainty, risk controls, escalation). P0-B defines the entire matrix now; public enablement of origination waits for v2.0.0 ratification + `legal_product_ratification`. |

Recommendation: **(c)**. It keeps D13's ratified capability real (the contract is designed and
testable from day one) without any surface violating a still-ratified prohibition, and it makes
the v2.0.0 re-ratification an explicit, single, owner-controlled public-enablement event.

### D-B2 — Per-label behavior matrix (calibration for the eight non-locked labels)

Proposed default matrix. Cell values: **A** = allowed, **D** = degraded (allowed with mandatory
evidence/explanation/validation additions), **R** = refused, **E** = escalate (stop ordinary
output, surface escalation). Guidance-class order: C1 `deterministic-calculation`, C2
`curated-evidence-guidance`, C3 `personalized-protocol-recommendation`, C4 `safety-escalation`.

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

Multi-label composition follows D14's fieldwise fold with this preemption order:
`acute-red-flag-or-emergency` → `controlled-or-illegal-sourcing` (surface-scoped) → refusal-cap
labels (`minor-or-age-uncertain`, `pregnancy-or-lactation`, `prescription-treatment-involved` for
the prescribed treatment) → calibrating labels union their obligations. "Most restrictive wins"
only on a genuine scalar conflict; no label erases another's obligations.

### D-B3 — Numeric provenance and presentation

Proposed rules (origins are charter-locked; the rules below are the gate's):

1. Every displayed number carries a machine-readable origin from the five locked values and a
   visible origin marker on dosing-context surfaces.
2. `biostack-recommended` values additionally require rationale, evidence applicability,
   uncertainty, and risk controls rendered alongside — never as bare numbers.
3. No prefilled or recommended value may render in a neutral-arithmetic frame (e.g., inside a
   calculator input without its provenance marker).
4. Missing provenance → the numeric output is **refused** (fail-closed), not downgraded.

### D-B4 — Missing-input behavior ladder

1. Arithmetic on declared inputs: refuse silently-invalid math (units, plausibility bounds,
   decimal-error flags per Class B); never guess an input.
2. Recommendation origination (post-v2.0.0): required-input set is function-declared; any missing
   required input → degraded output naming the missingness, or refusal when the missingness is
   safety-material (age, pregnancy status, identity/concentration, prescribed-treatment scope).
3. `PARTIAL_PACKET` / `OUTSIDE_REVIEWED_CONTEXT` markers fire per the v1.0.0 marker vocabulary.

### D-B5 — Function-review status and public enablement

1. `unreviewed` functions are **internal staging only** (mirrors `automated_candidate`) — never
   public-facing output.
2. Public dosing-context UX requires `legal_product_ratification` against the governing contract
   version (existing v1.0.0 rule, preserved).
3. `review-required` names the human owner and blocks public enablement until `reviewed`.
4. Class-triggered merge approvals (health-boundary, privacy, legal-policy, knowledge-promotion)
   continue to fire regardless of any standing Gate 3 authorization (D14 fold, D9).

### D-B6 — Escalation semantics

1. `acute-red-flag-or-emergency`: output stops ordinary guidance immediately; surfaces urgent
   professional/emergency language; suppresses calculators and dose context.
2. `prescription-treatment-involved`: alteration-of-treatment surfaces refuse; the professional-
   involvement template escalates; evidence comparison remains available.
3. Escalation is a distinct output type (class `safety-escalation`), never a footnote on an
   otherwise ordinary recommendation.
4. Escalation language is drawn from approved templates (v1.0.0 approval levels: high-impact
   safety wording requires `clinical_safety_copy_review`).

---

## 4. Ruling format

Reply in any form that resolves each item, e.g.:

> D-B1: c · D-B2: as recommended · D-B3: as recommended · D-B4: as recommended · D-B5: as
> recommended · D-B6: as recommended

Any override is recorded verbatim in the decision ledger. If a ruling would change a **locked**
item in §1, the coordinator stops and routes it as a charter amendment instead.

---

## 5. Sequencing after the gate (what the ruling does and does not unlock)

1. **Now:** owner rulings recorded (D-H continuation); P0-B design frozen as the ratified matrix
   in the decision ledger.
2. **P3-A** review → dispatch → build → close (its spec is review-candidate; P2 is closed and
   unblocks it).
3. **P0-A** dual review → dispatch → build → merge → canon precedence frozen. Its contradiction
   inventory will catalogue the §2 conflict as resolved by this gate's D-B1 ruling.
4. **P0-B spec** authored encoding the ratified matrix (this gate's rulings are its normative
   input), dual review, dispatch — the charter's dependency spine, unchanged.
5. **P0-C** fixtures prove allowed/degraded/refused/escalated behavior and that useful guidance
   survives enforcement. **P3-B** binds the parcel schema to the frozen contract.
6. Public enablement of `biostack-recommended` origination additionally requires
   guidance-content-contract **v2.0.0** re-ratification (D-B1(c)) — a separate owner event.

**This gate does not authorize:** production release or any change to the production-readiness
verdict (charter D17), any dispatch (Gate 2 remains parcel-by-parcel under the standing
authorization terms), or any merge of a non-green chain (D9's voiding conditions stand).

---

## 6. Coordinator recommendation summary

| ID | Recommended ruling |
|---|---|
| D-B1 | **(c) staged split** — deterministic math + Class A/B/C now; `biostack-recommended` origination behind v2.0.0 |
| D-B2 | matrix as proposed |
| D-B3 | rules 1–4 as proposed (fail-closed provenance) |
| D-B4 | ladder as proposed |
| D-B5 | rules 1–4 as proposed |
| D-B6 | semantics 1–4 as proposed |
