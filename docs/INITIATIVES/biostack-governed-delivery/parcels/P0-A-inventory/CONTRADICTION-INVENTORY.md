# P0-A Contradiction Inventory

Schema: `biostack.p0a-contradiction-inventory.v1`

`BaseCommit` for every citation below: `b78e7de8463a3db410c45cf223cd722713fec816`. Every row below
uses exactly the eleven-field row schema defined in `README.md` (this directory), rendered as one
`Field | Value` table per row for readability, grouped by topic cluster. `CI-001` through `CI-010`
are the P0-A parcel spec's seed findings, reproduced here byte-for-byte unchanged in their
`source_a`/`source_b` quotations, per the spec's `seed-regression` deterministic check. `CI-011` is
this parcel's own corpus-read extension.

A note on quotation delimiters, restated from the parcel spec's Hard constraints: the straight
double quotation marks (`"`) used below as citation delimiters are not part of the quoted content
and are exempt from the byte-for-byte test; any quotation mark, bold/italic markdown decoration
(`**`/`*`), or other typographic marker immediately surrounding a cited span is likewise citation
apparatus, not source content, unless it appears *inside* the quoted span, in which case it is
reproduced exactly as it appears in the source.

---

## Cluster: Personalized numerical guidance (the headline canon conflict)

### CI-001

| Field | Value |
|---|---|
| `id` | `CI-001` |
| `topic` | Personalized numerical dose/reconstitution/schedule recommendations |
| `source_a` | CHARTER.md ratified doctrine, "BioStack may": `docs/INITIATIVES/biostack-governed-delivery/CHARTER.md`, "## Ratified product doctrine": *"Originate evidence-bounded numerical recommendations, including dose targets, reconstitution choices, and schedules, when the applicable capability contract permits it. Such outputs are recommendations, not prescriptions, and must carry numeric provenance, rationale, evidence applicability, uncertainty, risk controls, and escalation behavior... Perform deterministic reconstitution, concentration, dose, split, volume, and syringe-unit calculations"*; D13, "## Locked decisions": *"BioStack may originate profile-aware dose, reconstitution, schedule, and support recommendations when the function contract, evidence, provenance, validation, uncertainty, and escalation requirements are satisfied. This does not authorize diagnosis, prescribing, clinician impersonation, or unsupervised alteration of prescribed treatment."* |
| `source_b` | `docs/canon/biostack-protocol-intelligence-canon.md`, "Observational-Only Boundary", `must not`: *"Give clinical dosing instructions... Recommend starting, stopping, tapering, combining, escalating, or substituting substances"*; `README.md`, "Safety and compliance boundary": *"does not provide clinical diagnosis, prescribing, medical dosing recommendations, individualized dosing... start/stop/taper/escalation advice"*; `docs/guidance/biostack-guidance-content-contract.v1.md`, "Output classes", Class D: *"**Status:** **Prohibited** unless product and regulatory posture deliberately change via a new contract version."* *"Selecting the correct dose for a person"* and *"Personalized titration schedules"* are **Prohibited** |
| `conflict` | CHARTER.md's "may" list and locked decision D13 permit BioStack to originate profile-aware, evidence-bounded numerical dose, reconstitution, and schedule recommendations (subject to provenance/rationale/uncertainty/escalation requirements). `docs/canon/biostack-protocol-intelligence-canon.md` forbids "clinical dosing instructions" and starting/stopping/tapering/combining/escalating/substituting-substance recommendations outright; `README.md`'s public safety boundary states BioStack "does not provide" individualized dosing or start/stop/taper/escalation advice; and `docs/guidance/biostack-guidance-content-contract.v1.md` Class D names "selecting the correct dose for a person" and "personalized titration schedules" as **Prohibited**. What CHARTER.md's `may` list and D13 permit, these three sources forbid in the same subject area. |
| `delivery_class_tags` | `health-boundary` |
| `guidance_class_tags` | `personalized-protocol-recommendation` |
| `substance_function_risk_tags` | `injection-or-sterile-preparation` |
| `status` | `contradictory` |
| `proposed_disposition` | `resolved-by-owner-ruling` — Coordinator Decision **D-I** (`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`, `## D-I` heading, lines 152-169) rules **D-B1 = (c) staged split** on exactly this conflict, which `docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` §2 ("The central conflict this gate must resolve," lines 56-76) states in the same terms this row catalogues. D-I states explicitly: *"the §2 canon conflict is resolved in substance by D-B1(c), and P0-A's contradiction inventory records its disposition by this ruling."* D-B1(c)'s resolution (`P0-B-DESIGN-GATE.md` D-B1 table): deterministic math on user-entered values plus Class A/B/C surfaces are the product's current posture now; `biostack-recommended` origination — the dosing/schedule/reconstitution-target behavior this row names — is defined in the P0-B contract but remains publicly gated behind guidance-content-contract **v2.0.0** re-ratification, a separate future owner event. This row keeps `status: contradictory` because the underlying canon texts still disagree on their own terms — the ruling settles which posture governs the product now; it amends neither document's text, and guidance-content-contract v1.0.0's Class D prohibition remains written exactly as quoted above. |
| `handoff_target` | `P0-D1` — the subparcel that encodes this already-ruled resolution into the frozen canon-of-record, not one that re-adjudicates it |

### CI-002

| Field | Value |
|---|---|
| `id` | `CI-002` |
| `topic` | Protocol-builder flows for high-risk substance categories |
| `source_a` | CHARTER.md ratified doctrine, "BioStack may": *"Design and compare protocol options, including compounds, combinations, sequencing, timing, frequency, schedules"* |
| `source_b` | `docs/canon/biostack-protocol-intelligence-canon.md`, `must not`: *"Design SARM cycles. Design SERM recovery protocols. Provide post-cycle therapy instructions... Create protocol-builder flows for SARMs, SERMs, investigational peptides, gray-market compounds, or other high-risk substances."* |
| `conflict` | CHARTER.md permits designing and comparing protocol options broadly (compounds, combinations, sequencing, timing, frequency, schedules) with no high-risk-category carve-out in that `may` clause itself. `docs/canon/biostack-protocol-intelligence-canon.md` forbids exactly the protocol-builder-flow behavior CHARTER.md's clause would otherwise cover, for SARMs, SERMs, investigational peptides, gray-market compounds, or other high-risk substances — a narrower-scope but directly conflicting prohibition on the same general permission. |
| `delivery_class_tags` | `health-boundary` |
| `guidance_class_tags` | `personalized-protocol-recommendation` |
| `substance_function_risk_tags` | `investigational-or-unapproved`, `gray-market-or-identity-uncertain` |
| `status` | `contradictory` |
| `proposed_disposition` | `fix` — cross-referenced to Coordinator Decision D-I (see CI-001): D-I's ruling addresses the headline dose/reconstitution/schedule-origination question (D-B1) and does not, by its own text, resolve this narrower protocol-builder-flow prohibition; a later P0-D1 builder must confirm this narrower text is brought into line with the D-B1(c) posture, not merely assume it is already settled. **D-K constraint notice:** `canon-precedence.md`'s rank order (CHARTER.md rank 3 outranks `docs/canon/biostack-protocol-intelligence-canon.md` rank 5) must not be read as resolving this row — per Coordinator Decision D-K, this narrower safety prohibition stands unchanged until an explicit owner ruling supersedes it; a P0-D builder must never weaken it by rank alone, and this row routes to the owner through P0-D's disposition process. |
| `handoff_target` | `P0-D1` |

### CI-003

| Field | Value |
|---|---|
| `id` | `CI-003` |
| `topic` | Syringe-visualization versus a blanket injection-instruction prohibition |
| `source_a` | CHARTER.md ratified doctrine: *"show what a selected value looks like on the syringe"* |
| `source_b` | `docs/canon/biostack-protocol-intelligence-canon.md`, `must not`: *"Give injection instructions."* |
| `conflict` | CHARTER.md's "may" list permits BioStack to show what a selected value looks like on the syringe — a visual/instructional rendering of an injection-relevant value. `docs/canon/biostack-protocol-intelligence-canon.md`'s `must not` list forbids giving injection instructions outright, without a carve-out distinguishing calculation-visualization from injection instruction. |
| `delivery_class_tags` | `health-boundary` |
| `guidance_class_tags` | `deterministic-calculation` |
| `substance_function_risk_tags` | `injection-or-sterile-preparation` |
| `status` | `contradictory` |
| `proposed_disposition` | `fix` |
| `handoff_target` | `P0-D2` |

### CI-004

| Field | Value |
|---|---|
| `id` | `CI-004` |
| `topic` | Profile-aware recommendation language versus Class C/D's explicit ceiling |
| `source_a` | CHARTER.md ratified doctrine: *"Make educated, profile-aware recommendations using relevant factors such as age, weight, goals, prior experience, current protocol, medications, conditions, tolerance, symptoms, biomarkers, diet, activity, and longitudinal observations."* |
| `source_b` | `docs/guidance/biostack-guidance-content-contract.v1.md`, "Output classes", Class C: *"Must not morph into “you should start at X.”"*; Class D: *"Declaring an amount safe for the user,"* *"Declaring a protocol appropriate for the user"* — both **Prohibited** |
| `conflict` | CHARTER.md permits "educated, profile-aware recommendations" drawing on a broad list of personal factors, without itself ceiling how directive that recommendation may be. The guidance contract's Class C explicitly bars that language from "morph[ing]" into a directive ("you should start at X"), and Class D names "declaring an amount safe for the user" and "declaring a protocol appropriate for the user" as Prohibited outright — a direct ceiling on exactly the kind of profile-aware output CHARTER.md's clause describes. |
| `delivery_class_tags` | `health-boundary` |
| `guidance_class_tags` | `curated-evidence-guidance`, `personalized-protocol-recommendation` |
| `substance_function_risk_tags` | `not-applicable` |
| `status` | `contradictory` |
| `proposed_disposition` | `fix` — cross-referenced to Coordinator Decision D-I (see CI-001): D-I's ruling does not, by its own text, resolve this narrower ceiling; a later P0-D1 builder must confirm it, not assume it is already settled |
| `handoff_target` | `P0-D2` |

### CI-005

| Field | Value |
|---|---|
| `id` | `CI-005` |
| `topic` | Capability map's independent prohibition list versus CHARTER.md `may` list |
| `source_a` | `docs/product/knowledge-engine-capability-map.md`: *"It does not authorize medical authority, diagnosis, prescribing, individualized dosing, treatment planning, start/stop/taper/escalation instructions, cycles, PCT, injection instructions, or sourcing guidance."* |
| `source_b` | CHARTER.md ratified doctrine `may` list (same spans cited in CI-001/CI-002/CI-003) |
| `conflict` | `docs/product/knowledge-engine-capability-map.md` states its own, independent prohibition list — including individualized dosing, start/stop/taper/escalation instructions, injection instructions, and sourcing guidance — framed as an absolute, undated boundary on what the capability map authorizes. CHARTER.md's `may` list permits several of the same named behaviors (profile-aware dosing/reconstitution/schedule recommendations, syringe visualization, protocol design) under its own capability-contract conditions, producing the same shape of conflict CI-001/CI-002/CI-003 catalogue, from a second, independently authored source. |
| `delivery_class_tags` | `health-boundary`, `legal-policy` |
| `guidance_class_tags` | `personalized-protocol-recommendation` |
| `substance_function_risk_tags` | `gray-market-or-identity-uncertain`, `controlled-or-illegal-sourcing` |
| `status` | `contradictory` |
| `proposed_disposition` | `fix` — cross-referenced to Coordinator Decision D-I (see CI-001): D-I's ruling does not, by its own text, resolve the capability map's independent prohibition list; a later P0-D1/P0-D3 builder must confirm it, not assume it is already settled. **D-K constraint notice:** `canon-precedence.md`'s rank order (CHARTER.md rank 3 outranks `docs/product/knowledge-engine-capability-map.md` rank 8) must not be read as resolving this row — per Coordinator Decision D-K, this narrower safety prohibition stands unchanged until an explicit owner ruling supersedes it; a P0-D builder must never weaken it by rank alone, and this row routes to the owner through P0-D's disposition process. |
| `handoff_target` | `P0-D3` — **`priority: P0-D1-first`** — this row's `substance_function_risk_tags` include `controlled-or-illegal-sourcing` (the capability map's prohibition list names "sourcing guidance," and `controlled-or-illegal-sourcing` "always suppresses sourcing, evasion, and concealment assistance" per the charter's governing vocabulary, a label that always preempts/suppresses per D14); the Health-boundary "Red flags" rule requires this priority flag so a later reconciliation parcel does not have to re-discover the urgency ordering. |

---

## Cluster: Non-conflict rows (corpus-coverage proof, not disagreements)

### CI-006

| Field | Value |
|---|---|
| `id` | `CI-006` |
| `topic` | Warning-first framing versus "more evidence, not less information" governing principle |
| `source_a` | CHARTER.md governing principle: *"High risk requires more evidence, explanation, validation, review, and escalation. It does not automatically require less useful information."* |
| `source_b` | `docs/canon/biostack-protocol-intelligence-canon.md`: *"Warning-first for high-risk categories: high-risk substances are surfaced through risk, regulatory, and observability context before any benefit framing."* |
| `conflict` | Not a conflict — recorded here deliberately as a `consistent` row, since the acceptance criteria require the inventory to prove corpus coverage, not just list disagreements. CHARTER.md's governing principle (more evidence/explanation/validation/review/escalation, not automatically less information) and the canon's warning-first framing (risk/regulatory/observability context sequenced before benefit framing) describe compatible, mutually reinforcing postures: warning-first sequencing is one concrete expression of "more evidence, explanation, validation, review, and escalation," not a withholding of useful information. |
| `delivery_class_tags` | `health-boundary` |
| `guidance_class_tags` | `safety-escalation` |
| `substance_function_risk_tags` | `not-applicable` |
| `status` | `consistent` |
| `proposed_disposition` | `not-applicable` |
| `handoff_target` | `not-applicable` |

### CI-007

| Field | Value |
|---|---|
| `id` | `CI-007` |
| `topic` | Privacy/data-custody claim moratorium |
| `source_a` | `README.md`: *"`/privacy` and `/terms` are stubs pending approved policy. No data-custody, storage-location, or privacy guarantee may be claimed on any public surface until they are approved."* |
| `source_b` | (no contradicting source found in the required list; catalogued because it is an explicit, dated legal-policy claim-moratorium other canon must not silently violate) |
| `conflict` | Not a conflict — no required source makes a data-custody, storage-location, or privacy guarantee claim that would violate this moratorium. This row is catalogued as a `consistent` finding precisely because the moratorium is explicit and dated and other canon must not silently violate it; this parcel's corpus read found no violation. |
| `delivery_class_tags` | `privacy`, `legal-policy` |
| `guidance_class_tags` | `not-applicable` |
| `substance_function_risk_tags` | `not-applicable` |
| `status` | `consistent` |
| `proposed_disposition` | `not-applicable` |
| `handoff_target` | `not-applicable` |

### CI-008

| Field | Value |
|---|---|
| `id` | `CI-008` |
| `topic` | Audit verdict language versus named function-review status |
| `source_a` | `BIOSTACK_FRONTEND_READINESS_AUDIT.md`: *"Ready for provider acquisition. Close to ready for paid conversion. Ready as a free tools + evidence destination."* |
| `source_b` | No named human reviewer, approval status, or D15 function-review vocabulary (`unreviewed`/`review-required`/`reviewed`/`not-applicable`) is recorded anywhere in the audit document for this verdict |
| `conflict` | Not a textual conflict between two sources — the `unreviewable` finding here is that a claim exists (the audit's readiness verdict) but this parcel cannot determine `consistent` or `contradictory` without a D15 function-review assignment that does not yet exist in the audit document. Recording that gap is itself the finding; assigning a reviewer crosses into legal-policy adjudication this parcel's D-G authorization does not cover. |
| `delivery_class_tags` | `legal-policy`, `knowledge-promotion` |
| `guidance_class_tags` | `not-applicable` |
| `substance_function_risk_tags` | `not-applicable` |
| `status` | `unreviewable` |
| `proposed_disposition` | `not-applicable` |
| `handoff_target` | `P0-D3` |

### CI-009

| Field | Value |
|---|---|
| `id` | `CI-009` |
| `topic` | Pairwise negative-relationship absence doctrine versus CHARTER.md conflict-identification doctrine |
| `source_a` | `docs/specs/active/BIO-PAIRWISE-001-relationship-substrate-and-label-census.md`: *"Absence of a label is not evidence that no interaction exists"* |
| `source_b` | CHARTER.md ratified doctrine `may` list: *"Identify conflicts, duplication, source-quality problems, contraindication signals, attribution problems, monitoring gaps, and reasons to pause or seek qualified help."* |
| `conflict` | Not a conflict — `source_a`'s absence doctrine ("absence of a label is not evidence that no interaction exists") and `source_b`'s conflict-identification permission ("identify conflicts, duplication, source-quality problems, contraindication signals, attribution problems, monitoring gaps, and reasons to pause or seek qualified help") are compatible: the absence doctrine is a *caution against false negatives* that reinforces, rather than contradicts, `source_b`'s permission to identify genuine conflict signals — it is the same caution CHARTER.md's own `must not` list separately expresses ("Present a studied, common, calculated, or recommended value as automatically safe for a specific person"). |
| `delivery_class_tags` | `health-boundary`, `knowledge-promotion` |
| `guidance_class_tags` | `curated-evidence-guidance` |
| `substance_function_risk_tags` | `interaction-or-contraindication-signal` |
| `status` | `consistent` |
| `proposed_disposition` | `not-applicable` |
| `handoff_target` | `not-applicable` |

---

## Cluster: Knowledge-promotion authority

### CI-010

| Field | Value |
|---|---|
| `id` | `CI-010` |
| `topic` | Knowledge-promotion authority description versus charter's deferred-promotion stance |
| `source_a` | `README.md`, "How knowledge gets in": *"Approved sources are ingested and normalized deterministically, classified for evidence strength and risk, human-reviewed where the contract requires it, and only then promoted into canonical product knowledge... It is not an autonomous authority, and it does not promote knowledge on its own."* |
| `source_b` | CHARTER.md: promotion-authority binding for the knowledge-promotion delivery class is not asserted anywhere in the ratified doctrine (silent, not contradicted) |
| `conflict` | Not a textual conflict — README.md states a specific knowledge-promotion authority model (deterministic ingestion, human review where the contract requires it, no autonomous model promotion). CHARTER.md is silent on binding promotion authority for the `knowledge-promotion` delivery class specifically (it defines the class's required sections and controls via the governance overlay table, but does not itself assert a promotion-authority model the way README.md does). Silence is not contradiction, but the apparent tension — one document stating a specific model the other never ratifies — is intentional/acceptable and should be cross-referenced rather than treated as settled doctrine, which is exactly what `accept-as-documented` names. |
| `delivery_class_tags` | `knowledge-promotion` |
| `guidance_class_tags` | `not-applicable` |
| `substance_function_risk_tags` | `not-applicable` |
| `status` | `contradictory` |
| `proposed_disposition` | `accept-as-documented` |
| `handoff_target` | `P0-D1` |

---

## Cluster: Procedural canon (this parcel's own corpus-read extension)

### CI-011

| Field | Value |
|---|---|
| `id` | `CI-011` |
| `topic` | Literal `TBD` placeholders in `docs/specs/INDEX.md` active-row cells versus the no-unresolved-placeholder lifecycle rule |
| `source_a` | `docs/specs/README.md`, "## Status and ownership": *"Active specs must contain no unresolved placeholders, ambient branch or worktree, or unresolved decision."* |
| `source_b` | `docs/specs/INDEX.md`, line 8 (`BIO-PAIRWISE-001` row, `Branch/worktree` and `Owner` cells; the identical pattern recurs verbatim in lines 9-13 for `BIO-PAIRWISE-002` through `BIO-PAIRWISE-006`): *"TBD (coordinator to assign Gate 2)"* and *"[TBD]"* |
| `conflict` | `docs/specs/README.md`'s own lifecycle rule states that active specs must contain no unresolved placeholders. The six `BIO-PAIRWISE-001` through `BIO-PAIRWISE-006` rows in `docs/specs/INDEX.md` — the authoritative registry for those same active specs — carry the literal, unresolved string `TBD` in their `Branch/worktree` and `Owner` cells rather than a closed-vocabulary placeholder. This project has already established the correct non-ad-hoc convention for exactly this situation: this parcel's own Gate 2 dispatch record and P2's precedent (cited in this parcel's spec, "Exact allowed surfaces," item 9) use the closed-vocabulary registry cell literal `coordinator-assigns-at-gate-2` rather than a raw `TBD` cell, precisely because P2 and P3-A's own no-`TBD` rule treats bare `TBD` as an unresolved placeholder. The six pairwise rows predate that convention's consistent application and still carry raw `TBD`. |
| `delivery_class_tags` | `standard` |
| `guidance_class_tags` | `not-applicable` |
| `substance_function_risk_tags` | `not-applicable` |
| `status` | `contradictory` |
| `proposed_disposition` | `fix` — replace the six rows' raw `TBD`/`[TBD]` cells with the established `coordinator-assigns-at-gate-2` closed-vocabulary literal (or each row's real assigned branch/worktree/owner, once Gate 2 is recorded for that parcel), consistent with the P2 precedent this parcel's own spec already cites |
| `handoff_target` | `P0-D1` — no dedicated P0-D subparcel exists for pure spec-registry hygiene outside the four named handoff targets; `P0-D1` (core doctrine/ADR) is the closest-fit catch-all, since the no-placeholder rule is itself a locked, doctrine-adjacent governance mechanic (charter "Explicit exclusions": "No vague acceptance criteria or unresolved `TBD` in active specs") rather than evidence methodology, product-facing copy, or enforcement tests |

---

## Summary table (navigation aid, not a substitute for the field tables above)

| id | topic | status | proposed_disposition | handoff_target |
|---|---|---|---|---|
| CI-001 | Personalized numerical dose/reconstitution/schedule recommendations | contradictory | resolved-by-owner-ruling | P0-D1 |
| CI-002 | Protocol-builder flows for high-risk substance categories | contradictory | fix | P0-D1 |
| CI-003 | Syringe-visualization versus a blanket injection-instruction prohibition | contradictory | fix | P0-D2 |
| CI-004 | Profile-aware recommendation language versus Class C/D's explicit ceiling | contradictory | fix | P0-D2 |
| CI-005 | Capability map's independent prohibition list versus CHARTER.md `may` list | contradictory | fix | P0-D3 |
| CI-006 | Warning-first framing versus "more evidence, not less information" | consistent | not-applicable | not-applicable |
| CI-007 | Privacy/data-custody claim moratorium | consistent | not-applicable | not-applicable |
| CI-008 | Audit verdict language versus named function-review status | unreviewable | not-applicable | P0-D3 |
| CI-009 | Pairwise negative-relationship absence doctrine versus CHARTER.md | consistent | not-applicable | not-applicable |
| CI-010 | Knowledge-promotion authority description versus charter's deferred-promotion stance | contradictory | accept-as-documented | P0-D1 |
| CI-011 | Literal `TBD` placeholders in `docs/specs/INDEX.md` versus the no-unresolved-placeholder lifecycle rule | contradictory | fix | P0-D1 |
