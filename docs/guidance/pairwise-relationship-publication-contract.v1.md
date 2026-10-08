# Pairwise Relationship Publication Contract v1

| Field | Value |
|---|---|
| Ticket | `BIO-PAIRWISE-002` |
| Version | **1.0.0** |
| Status | **Ratified** |
| Scope | v1 publishable-subset ratification for `relationship-packet.schema.json`. Negative relationships only; positive/synergy types deferred (not denied). |
| Governing contract | `docs/guidance/biostack-guidance-content-contract.v1.md` (Guidance Content Contract v1) |
| Ratification record | `docs/guidance/RATIFICATION.md` |
| Schema (byte-unchanged) | `backend/src/BioStack.KnowledgeWorker/Schemas/relationship-packet.schema.json` |
| Date | 2026-10-08 |
| P0 inputs consumed (no re-census) | `research/output/pairwise-lane-20260917/p0-substrate-inventory.md`, `research/output/pairwise-lane-20260917/p0-label-interaction-census.md` |
| P0 gate decision | D-A (schema sufficiency) — RESOLVED sufficient; `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` |

## Purpose

This record states which existing `relationship-packet.schema.json` values and record shapes
`relationship-packet.schema.json` may already express are **publishable** to any public surface in
v1, and which are withheld. It changes no schema, no code, no migration, no fixture, no test. The
schema stays byte-unchanged (verified: this ratification edits only the two files listed in the
spec's Allowed Files section).

This ratification governs **sourced relationship records** only (the schema's `relationship`
object, as defined at `backend/src/BioStack.KnowledgeWorker/Schemas/relationship-packet.schema.json`).
It does **not** govern unsourced per-pair *reasoning* derived from `CompoundInteractionHint`,
which the 2026-09-16 owner ruling (B3; `docs/guidance/RATIFICATION.md`, "Per-pair interaction
reasoning gated to Operator on every surface") already gates behind the `reviewed_relationship_graph`
entitlement on every surface. See §6 for the explicit distinction.

Absent an entry in the allowlists below, a value is **unpublishable by default**, including any
`relationshipType` or `assertionClass` value added to the schema's enums after this ratification.

---

## 1. Publishable `relationshipType` values (v1)

Source enum (14 values): `backend/src/BioStack.KnowledgeWorker/Schemas/relationship-packet.schema.json:78-93`.

Per the spec's constraint, v1 ratifies **negative relationships only**; positive and synergy types
are deferred, not denied.

### 1a. Publishable in v1 (allowlist)

| `relationshipType` | Reason publishable |
|---|---|
| `contraindicated` | Unambiguous negative/safety relationship: a combination the record asserts should not occur. Core v1 negative type. |
| `caution` | Unambiguous negative/safety relationship: a combination the record asserts requires care. Core v1 negative type. |
| `conflict` | Unambiguous negative relationship: the record asserts the two compounds act against each other in a way the source frames as a problem. Core v1 negative type. |

### 1b. Withheld in v1 — deferred (positive / synergy family; one-line reason each)

| `relationshipType` | Reason withheld |
|---|---|
| `synergy` | Positive relationship type. Spec constraint: "v1 ratifies negative relationships only... Positive and synergy types are named as deferred." Deferred, not denied. |
| `complementary` | Positive relationship type, same deferral as `synergy`. |
| `redundant` | Describes overlapping/additive effect profile, the same effect-polarity family as `complementary`; deferred alongside the positive family rather than ratified as negative. |

### 1c. Withheld in v1 — not a sourced negative-evidence relationship (one-line reason each)

| `relationshipType` | Reason withheld |
|---|---|
| `timing-sensitive` | Describes a timing consideration, not an inherent safety polarity; the same token could support either a sequencing suggestion or a caution. No v1 ratification states which. Deferred to a future ratification cycle. |
| `dose-dependent` | Describes a dose-dependency without inherent polarity; could underlie a positive or negative relationship. Deferred to a future ratification cycle. |
| `mechanism-overlap` | Descriptive mechanistic finding (a basis, comparable to the schema's own `mechanismBasis` field), not itself a polarity-bearing evidentiary relationship class. Deferred. |
| `opposing-effect` | Polarity is context-dependent (counteracting effects can be protective or harmful depending on goal); not inherently a negative/safety claim. Deferred. |
| `community-stack` | Evidentiary basis is inherently a community/popularity pattern. Its natural `assertionClass` is `community-signal`, which §2 names as never publishable as evidence. Withheld on that basis. |
| `popular-but-unsupported` | Self-describes an *unsupported* popularity pattern; cannot satisfy any evidentiary sourcing bar by definition. Withheld. |
| `vendor-claimed` | Self-describes a vendor claim; its natural `assertionClass` is `vendor-claim`, which §2 names as never publishable as evidence. Withheld. |
| `misinformation-pattern` | Describes a curatorial characterization of a pattern as misinformation — a judgment call, not sourced observational evidence; aligned with `curator-hypothesis`, which §2 names as never publishable. Withheld. |

A new `relationshipType` enum value added later is unpublishable until a future ratification
cycle names it in §1a.

---

## 2. Publishable `assertionClass` values (v1)

Source enum (7 values): `backend/src/BioStack.KnowledgeWorker/Schemas/relationship-packet.schema.json:166-173`.

### 2a. Publishable in v1 (allowlist)

| `assertionClass` | Reason publishable |
|---|---|
| `direct-evidence` | Directly observed/reported evidence from a source, the strongest class the schema defines. |
| `authoritative-caution` | A caution issued by an authoritative source (e.g. a regulator or official label) — sourced, not inferred. |

### 2b. Never publishable as evidence (named explicitly, per spec constraint)

| `assertionClass` | Reason never publishable |
|---|---|
| `vendor-claim` | Named by the spec as never publishable as evidence. A commercial claim, not independent evidence. |
| `community-signal` | Named by the spec as never publishable as evidence. A popularity/anecdote signal, not evidence. |
| `curator-hypothesis` | Named by the spec as never publishable as evidence. An internal hypothesis, not sourced observation. |

### 2c. Withheld in v1 — inference classes deferred (one-line reason each)

| `assertionClass` | Reason withheld |
|---|---|
| `mechanistic-inference` | Inferred reasoning from mechanism rather than directly sourced observation. Publishing inferred per-pair reasoning as a relationship record in v1 risks the exact conflation §6 prohibits — the 2026-09-16 ruling already gates unsourced per-pair *reasoning* to the Operator entitlement; keeping inference-typed assertions out of the v1 publishable set preserves that line cleanly. Deferred to a future ratification cycle, not denied. |
| `category-inference` | Generalization from drug-class membership rather than compound-pair-specific sourced evidence. Deferred to a future ratification cycle. |

A new `assertionClass` enum value added later is unpublishable until a future ratification cycle
names it in §2a.

---

## 3. Sourcing bar — checkable conjunction

A relationship record is **publishable in v1** if and only if **all** of the following hold. Any
one failure makes the record unpublishable at any tier; it is not demoted to a lesser public
rendering (spec constraint).

1. `relationship.relationshipType` is one of the §1a values: `contraindicated`, `caution`,
   `conflict`. (`relationship-packet.schema.json:78-93` for the full enum this is drawn from.)
2. `relationship.assertionClass` is one of the §2a values: `direct-evidence`,
   `authoritative-caution`. (`relationship-packet.schema.json:166-173`.)
3. `relationship.evidenceTier` is present and is **not** `Unknown`
   (`relationship-packet.schema.json:104-106`, enum
   `Strong, Moderate, Limited, Anecdotal, Insufficient, Unknown`; any value other than `Unknown`
   satisfies this clause).
4. `relationship.relationshipReviewStatus` equals `accepted-as-evidence-backed`
   (`relationship-packet.schema.json:207-215`). No other enum value
   (`unreviewed`, `review-required`, `human-reviewed`, `rejected`, `accepted-as-signal`)
   is sufficient: `human-reviewed` alone does not state the review's outcome, and
   `accepted-as-signal` is explicitly a signal-level acceptance, not an evidence-backed one —
   consistent with §2b excluding `community-signal` from evidence.
5. `relationship.sourceRefs` contains at least one entry (`relationship-packet.schema.json:103`,
   `sourceRefArray`, minimum 1 item) whose referenced `sourceSnapshot.authorityTier`
   (`relationship-packet.schema.json:241`) is in the existing codebase's authoritative-tier set
   `{A1, A2}` — this is **not** a new definition invented by this ratification; it cites the
   existing policy already enforced for safety-critical claim types including `interaction`,
   `contraindication`, and `warning`
   (`backend/src/BioStack.KnowledgeWorker/Pipeline/FieldAuthorityPolicy.cs:5-8,16-22`:
   `AuthoritativeTiers = { "A1", "A2" }`; `SafetyCriticalClaimTypes` includes `"interaction"`,
   `"contraindication"`, `"warning"`). This is the "authorized source lane" the spec's constraints
   name. Per the P0 census method, sources classified `product-label`, `approval-package`, or
   `warning-letter` are the FDA/DailyMed-lane tokens observed to typically carry `A1`
   (`research/output/pairwise-lane-20260917/p0-label-interaction-census.md`, Method §, e.g. the
   `dailymed-ozempic`/`dailymed-wegovy` sources at `research/input/evidence/semaglutide.evidence.json:13-14`,
   `authorityTier: "A1"`); this ratification does not re-census or re-classify any source — it
   states the field-level rule a validator checks per record.

A validator checks clauses 1–5 as an unconditional AND, with no further interpretation required.

---

## 4. Permitted / forbidden phrasing

Governed by Guidance Content Contract v1 (`docs/guidance/biostack-guidance-content-contract.v1.md`).
A rendered relationship under this ratification is Class A sourced observation only; it is never
Class D direction (prohibited outright, per the spec's constraints).

### 4a. Permitted (describes what a source reports)

| # | Permitted phrasing |
|---|---|
| P1 | "The cited source lists [Compound A] and [Compound B] as contraindicated." |
| P2 | "The cited source advises caution when combining [Compound A] and [Compound B]." |
| P3 | "The cited source describes a conflict between [Compound A] and [Compound B]: [sourced summary]." |

### 4b. Forbidden (tells a reader what to do, combine, avoid, take, or adjust — breaches a
Guidance Content Contract class)

| # | Forbidden phrasing | Contract class breached |
|---|---|---|
| F1 | "Do not combine [Compound A] and [Compound B]." | Class D — uncited/direct imperative instruction ("Uncited start / stop / increase / decrease / combine / substitute instructions" — contract §Class D Prohibited). |
| F2 | "Avoid taking [Compound A] with [Compound B]." | Class D — imperative "avoid" instruction (contract §Class D Prohibited; §Copy-guard banned patterns analog to "Stop …"). |
| F3 | "[Compound A] and [Compound B] are safe to combine." | Class D — declares safety for the user (contract §Class D Prohibited, "Declaring an amount safe for the user"). |
| F4 | "[Compound A] and [Compound B] will harm you." | Class D — predicts a certain personal outcome (contract §Class D Prohibited, "Predicting that a user will experience a particular outcome"; contract §Bad examples, "12 mg will harm you."). |
| F5 | "You should stop [Compound A] before starting [Compound B]." | Class D — uncited personalized titration/sequencing instruction (contract §Class D Prohibited, "Personalized titration schedules"; "Uncited start / stop ... instructions"). |
| F6 | "Based on your other compounds, we recommend avoiding [Compound B]." | Class D — manufactures a personalized recommendation from context (contract §Class D Prohibited, "Using age, weight, sex, goals, or symptoms to manufacture a prescription"; "AI recommends …" banned pattern). |

No permitted entry (P1–P3) survives a hostile reading as direction: each states only what a named
source reports, with no verb directed at the reader ("take," "avoid," "stop," "should") and no
safety/harm declaration about the reader.

---

## 5. Source/evidence-tier display and absence handling

- A published relationship record must display, alongside its rendered text: the source(s) it
  cites (from `sourceRefs`, resolvable to the `sourceSnapshot` entries required to carry
  `title` and `url` — `relationship-packet.schema.json:236-241`) and its `evidenceTier`
  (`relationship-packet.schema.json:104-106`). A rendering that states a relationship without
  showing both its source and its evidence tier does not satisfy this ratification.
- **Absence of a relationship record must never render as reassurance.** Per the spec's
  constraint, this ratification states explicitly: v1 does not render any "no known
  interaction," "compatible," "no conflicts found," or equivalent reassurance statement when no
  publishable relationship record exists for a pair. The absence of a publishable record is not
  presented to a reader at all under this ratification — no sentence, icon, or status value is
  authorized to represent "absence" as a result. A future ratification may define an explicit,
  honestly-labeled "not reviewed" / "no publishable record" indicator; this ratification does not
  create one, and no code change is implied or authorized by this record.

---

## 6. Reasoning-gate distinction (explicit, to prevent misreading)

The 2026-09-16 owner ruling (B3) and its B4/B5 extensions gate unsourced **per-pair reasoning**
derived from `CompoundInteractionHint` behind the `reviewed_relationship_graph` entitlement, on
every surface, for every caller tier below Operator (`docs/guidance/RATIFICATION.md`, "Per-pair
interaction reasoning gated to Operator on every surface (B3)" and its B4/B5 entries). That ruling
governs **unsourced inference** — mechanism, direction, consequence, evidence narrative, or source
text an Observer or anonymous caller does not receive without the entitlement.

This ratification governs a categorically different thing: **sourced relationship records** that
meet §3's checkable sourcing bar (named source, non-`Unknown` evidence tier, `accepted-as-evidence-backed`
review status, authoritative-tier source). A record that clears §3 is public evidence, not
per-pair reasoning, and this ratification's publishable set is not an expansion, reversal, or
loosening of the 2026-09-16 entitlement gate. The two are governed independently:

- A record can be §3-publishable and still never render the gated reasoning shape (mechanism,
  direction, consequence, confidence narrative) that B3/B4/B5 reserve for Operator — this
  ratification does not authorize rendering that shape to non-Operator callers.
- The 2026-09-16 gate is not loosened, widened, or bypassed by anything in this record: no
  `CompoundInteractionHint`-derived reasoning becomes publishable evidence by virtue of this
  ratification, regardless of entitlement.

A reader cannot read this ratification as reversing the 2026-09-16 ruling: this record never
authorizes rendering per-pair reasoning to a non-Operator caller, and the 2026-09-16 gate's scope
(reasoning) and this ratification's scope (sourced evidence records meeting §3) are named here as
non-overlapping.

---

## Out of scope / unchanged

- `relationship-packet.schema.json` is byte-unchanged by this ratification (no edit made).
- No code, DTO, projection, migration, fixture, or test is authored or implied by this record.
- No file outside `docs/guidance/` is created or modified by this ratification.
- Positive/synergy types (`synergy`, `complementary`, `redundant`) and the inference-typed
  assertion classes (`mechanistic-inference`, `category-inference`) remain deferred, not denied;
  a future ratification cycle may name them publishable without needing to revisit §1a/§2a's
  current entries.
- Auto-fetching substances absent from the library (named by the owner 2026-09-17 as a future
  direction) is not shaped, authorized, or designed here.
- The 2026-09-16 reasoning-gating ruling is not reopened by this record (see §6).
