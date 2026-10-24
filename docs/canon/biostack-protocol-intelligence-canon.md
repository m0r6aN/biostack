# BioStack Protocol Intelligence Canon

Date: 2026-06-18 (rewritten 2026-10-24)
Source research: `research/protocol-intelligence/biostack-ai-model-data-asset-research-memo.md`
Status: Canonical product foundation
Owner and sole decision maker: Clinton Morgan. This canon changes only by his decision.

## What changed on 2026-10-24

- BioStack shows everything it gathered about a compound: no do-not-display gates. Fields that
  come only from non-official sources are labeled "Unofficial source", never hidden.
- Every compound page shows doses as found, from both sides, each with its source and grade (see
  "Doses: what was found, from both sides"). A side with nothing found shows `n/a`.
- Grades use the 7-tier methodology scale (`docs/biostack/evidence-grading-methodology-v1.md`).
  "Bro-Science" is renamed **Hype Exceeds Evidence** everywhere.
- Review is done by a two-reviewer AI council. Consensus passes; disagreement escalates to the
  owner with distinct options. There are no reviewer-role approval steps.
- Legal items are logged as known risks (below), not blockers.

## Product Posture

BioStack is an observational protocol intelligence product. It helps users organize protocol timelines, evidence quality, source quality, phase context, symptom changes, biomarkers, and uncertainty. It does not prescribe, diagnose, treat, prevent, cure, source, dose, inject, cycle, recover, or recommend substance changes. **BioStack never prescribes.** It reports what sources say, including the doses they report, and says how well each is supported.

The canonical product posture is:

- Evidence-bounded: every claim and relationship keeps its evidence tier, source class, and uncertainty visible.
- Phase-aware: protocol events are interpreted against baseline, preparation, loading, active intervention, escalation, support, maintenance, washout, recovery, and reassessment phases.
- Source-aware: substance identity and quality are tracked separately from mechanism or outcome claims.
- Timeline-first: BioStack asks what changed before an observed outcome instead of asserting causality.
- Warning-first for high-risk categories: high-risk substances are surfaced through risk, regulatory, and observability context before any benefit framing.
- Show-everything: every field gathered for a compound is shown, concise and without redundancy, with known vs unknown separated and every source linked.
- Council-reviewed: runtime surfaces consume artifacts that passed the two-reviewer AI council, or are labeled pending review; they do not invent new relationship claims.

## Observational-Only Boundary

BioStack may:

- Show observed protocol events, phase changes, symptoms, biomarkers, wearables, subjective notes, and source-quality metadata.
- Explain that a relationship is evidence-backed, mechanistically plausible, anecdotal, speculative, conflicting, unsupported, or unknown.
- Surface uncertainty, contradiction, source limitations, regulatory status, sports-ban status, and monitoring domains to discuss with a qualified professional.
- Produce baseline, observation, reassessment, and clinician-discussion prompts.
- Track GLP-1 observability domains such as appetite, GI symptoms, hydration, protein adequacy, lean-mass proxy, mood, alcohol-craving notes, and discontinuation events.

BioStack must not:

- Give clinical dosing instructions, or present a found dose as a direction, a "typical dose", or a recommendation.
- Give injection instructions.
- Design SARM cycles.
- Design SERM recovery protocols.
- Provide post-cycle therapy instructions.
- Provide sourcing guidance.
- Claim investigational peptides are safe or effective for human use.
- Treat community anecdotes as proof of efficacy.
- Create protocol-builder flows for SARMs, SERMs, investigational peptides, gray-market compounds, or other high-risk substances.
- Recommend starting, stopping, tapering, combining, escalating, or substituting substances.

## Evidence Categories

BioStack recognizes the market-research evidence categories below and maps them into product evidence tiers only after source review.

| Research category | Product handling |
| --- | --- |
| Human clinical evidence | Eligible for `Established` or `Early Evidence` only when the exact claim, population, endpoint, form, and context match. |
| Mechanistic human evidence | `Plausible Theory`; mechanism support, not outcome proof by itself. |
| Animal evidence | Preclinical context; cannot establish human efficacy. |
| In vitro evidence | Mechanism discovery only. |
| Expert opinion | Context signal; cannot override source-reviewed evidence. |
| Practitioner pattern | Market or practice signal; review-gated before user-facing use. |
| Community anecdote | `Anecdotal Signal`; shown on the reported side with its source, never as proof. |
| Market signal only | Product-discovery signal; never evidence of efficacy. Promotional certainty beyond the evidence is graded `Hype Exceeds Evidence`. |
| Speculative hypothesis | Internal hypothesis or research gap; warning labels must stay visible. |
| Conflicting evidence | `Contradicted` when better-matched evidence conflicts; shown, never hidden. |
| No meaningful evidence found | `Unsupported`, or `Not graded` before review. |

Evidence tier is separate from confidence. Confidence means confidence in the grade or classification, not confidence that a substance works.

## Doses: what was found, from both sides

Every compound page has a "Doses found" panel with two columns:

| Column | Sources |
| --- | --- |
| Studies, trials and professional sources | Randomized and other trials, trial registries, labels, guidelines, regulators, peer-reviewed studies, clinician or pharmacist references. |
| Reddit, Facebook, Instagram, forums and blogs | Online reports, each as a cited link or a manual entry. |

Rules:

1. Each row is one dose from one source: amount and unit, frequency, route, duration, population, outcome where the source reports one, the source title and link, and a grade on the 7-tier scale.
2. A column with no rows shows `n/a`. BioStack never fills a gap with an estimate, a range or a "typical dose".
3. The panel carries one fixed line: "Doses as found in each source, with its grade. What was found, not a direction."
4. Online rows are graded on their own merits (usually `Anecdotal Signal` or `Hype Exceeds Evidence`) and never upgrade the study side.
5. Facebook and Instagram rows are cited links or manual entries only. No automated collection (Meta terms; see Known risks).
6. Records store these rows in `doseReports[]` (`side: study | reported`) in the substance-record schema.

## Phase Taxonomy

The canonical phase set is:

- Baseline: stable pre-change observation window.
- Preparation: stabilizing context that helps later interpretation.
- Loading: exposure or habit consistency ramp, without instruction semantics.
- Active Intervention: main observed protocol event begins.
- Escalation: intensity or exposure changes; no dosing values or instructions.
- Support: observed support agents or behaviors that may help or confuse attribution.
- Maintenance: lower-burden or steady-state observation period.
- Washout: non-prescriptive pause observation for non-prescribed substances only.
- Recovery: reassessment after discontinuation, adverse events, or high-risk exposure.
- Reassessment: evidence, burden, safety, cost, and signal review.

Phase labels explain timeline context. They are not instructions to enter or leave a phase.

## Relationship Semantics

Protocol Intelligence relationships describe evidence-bound knowledge graph edges. They do not imply recommendation.

Canonical relationship families:

- `substance_affects_pathway`
- `substance_supports_goal`
- `substance_requires_monitoring_with_biomarker`
- `substance_complicates_attribution_of_symptom`
- `substance_commonly_paired_with`
- `substance_commonly_separated_from`
- `substance_has_source_quality_concern`
- `substance_has_regulatory_status`
- `substance_to_phase_role`
- `substance_has_discontinuation_consideration`
- `substance_may_mask_side_effect`
- `substance_increases_protocol_complexity`

Every relationship artifact must carry:

- Subject and object.
- Relationship type.
- Phase context when applicable.
- Goal context when applicable.
- Evidence tier.
- Confidence in classification.
- Source references.
- Source authority mix.
- Safety concern level.
- Product handling: include now, include later, warning-only, avoid, or review-required.
- Review status.
- User-facing explanation that avoids medical advice.

Runtime behavior must prefer `Unknown` over unsupported inference. If a relationship is absent, unpublished, or unreviewed, BioStack should say it has not evaluated that relationship in that context.

## Side-Effect Ambiguity Detection

Side-effect ambiguity detection is a core Protocol Intelligence capability. It answers: "What changed before this outcome?"

The system may compare:

- Recent start, stop, pause, escalation, support, source, or phase events.
- Symptom onset windows.
- Known adverse-effect domains.
- Pathway overlap.
- Source-quality risk.
- High-risk category flags.
- User notes and wearables.

The system must not diagnose causality. It should use language such as "may complicate attribution," "plausibly overlaps with," "observed after," or "worth discussing with a qualified professional."

## Source-Quality Classification

Source quality is modeled independently from evidence of benefit. Canonical source classes:

- FDA-approved product.
- Prescription-only product.
- Compounded product.
- Dietary supplement.
- Third-party certified supplement.
- Research chemical.
- Gray-market product.
- Investigational compound.
- Banned-in-sport substance.
- Unknown or user-entered source.

Source-quality artifacts may support warning, review, regulatory, identity, and observability surfaces. They must not provide vendor selection, sourcing guidance, or purchase recommendations.

## GLP-1 Observability

GLP-1 intelligence is observational and source-aware. BioStack may track:

- Appetite.
- GI symptoms.
- Hydration.
- Protein adequacy.
- Fiber adequacy.
- Weight trend.
- Strength or lean-mass proxy.
- Mood.
- Alcohol-craving notes.
- Discontinuation or pause events.
- Compounded-product and source-quality uncertainty.

BioStack does not practice medicine and does not provide GLP-1 information on a prescriptive basis. BioStack must not select or change a dose for a user, direct switching or tapering, provide compounded sourcing guidance, or issue product-specific instructions.

BioStack presents published evidence context and evidence-based comparison — including trial-reported amounts, escalation and discontinuation approaches used in cited sources, monitored parameters, doses reported online (on the reported side, with source and grade), and how a user-recorded entry differs from reviewed research — under the output classes defined in the Guidance Content Contract (`docs/guidance/biostack-guidance-content-contract.v1.md`). The intent is to give users factual, source-backed material with which to make their own informed choices in consultation with a qualified clinician, rather than leaving them to follow unsourced social-media protocol advice. All such material must remain non-prescriptive, cited, and labeled with its applicability limits and uncertainty markers.

## High-Risk Warning-First Categories

The following categories are warning-first:

- SARMs.
- SERMs.
- Investigational peptides.
- Gray-market substances.
- Compounded GLP-1s.
- Prescription-only substances.
- Banned-in-sport substances.

Warning-first means the product surface leads with regulatory status, source-quality uncertainty, evidence limits, known risk domains, review state, and clinician-discussion language. Benefit claims, if any, must be narrower than the source evidence and must never normalize unsupervised use. Warning-first is an ordering rule, not a hiding rule: everything gathered is still shown.

## Review Canon

Research becomes runtime-visible through the knowledge worker. A record carries:

- Structured artifact format (substance-record schema).
- Source references with titles and links.
- Evidence tier (7-tier scale) and confidence, per claim.
- Source authority mix (official vs unofficial fields).
- Safety concern level and dangers.
- Review status: `pending-review`, `reviewed`, `changes-requested`, `escalated-to-owner`.

Review is by the two-reviewer AI council (`docs/operations/ai-council-review.md`):

1. Two independently configured models review each record against the methodology.
2. If both agree, the decision passes.
3. If they disagree, the record is escalated to the owner, Clinton Morgan, with distinct options. He decides.
4. Legal questions are logged as known risks; they do not block.

A record that has not passed review is still shown, labeled `pending review`. Only archived or rejected records are withheld. Community and market signal are shown as signal on the reported side, never as proof.

## Known risks (logged, not blockers)

| Risk | Detail | Handling |
| --- | --- | --- |
| Guidance Contract Class D wording | The Class D wording predates the show-everything canon. | Rewritten 2026-10-24 in `docs/guidance/biostack-guidance-content-contract.v1.md`. |
| Meta platform terms | Facebook Terms §3.2.3 and Instagram Terms §4.2 ban automated collection. | Facebook/Instagram dose rows are cited links or manual entries only. No scrapers unless an allowed API exists. |
| Reddit Data API terms | Commercial use needs a separate agreement (Data API Terms §3.1, revised 2026-07-20); the Responsible Builder Policy forbids inferring health traits about users. | Reddit rows are cited links or manual entries until an agreement exists; no user-level health inference. |
| MHMDA (Washington My Health My Data Act) | Doses users log about themselves are consumer health data. | Consent, deletion and disclosure handling required before any user-logged dose feature ships. |
| Gray-market products | Online dose reports refer to products whose identity, purity and dose cannot be verified. | Shown with that caveat on the reported side. |
