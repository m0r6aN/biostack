# BioStack Protocol Intelligence Canon

Date: 2026-06-18
Source research: `research/protocol-intelligence/biostack-ai-model-data-asset-research-memo.md`
Status: Canonical product foundation

## Product Posture

BioStack is an observational protocol intelligence product. It helps users organize protocol timelines, evidence quality, source quality, phase context, symptom changes, biomarkers, and uncertainty. It does not prescribe, diagnose, treat, prevent, cure, source, dose, inject, cycle, recover, or recommend substance changes.

The canonical product posture is:

- Evidence-bounded: every claim and relationship keeps its evidence tier, source class, and uncertainty visible.
- Phase-aware: protocol events are interpreted against baseline, preparation, loading, active intervention, escalation, support, maintenance, washout, recovery, and reassessment phases.
- Source-aware: substance identity and quality are tracked separately from mechanism or outcome claims.
- Timeline-first: BioStack asks what changed before an observed outcome instead of asserting causality.
- Warning-first for high-risk categories: high-risk substances are surfaced through risk, regulatory, and observability context before any benefit framing.
- Human-review-gated: runtime product surfaces consume reviewed artifacts; they do not invent new relationship claims.

## Observational-Only Boundary

BioStack may:

- Show observed protocol events, phase changes, symptoms, biomarkers, wearables, subjective notes, and source-quality metadata.
- Explain that a relationship is evidence-backed, mechanistically plausible, anecdotal, speculative, conflicting, unsupported, or unknown.
- Surface uncertainty, contradiction, source limitations, regulatory status, sports-ban status, and monitoring domains to discuss with a qualified professional.
- Produce baseline, observation, reassessment, and clinician-discussion prompts.
- Track GLP-1 observability domains such as appetite, GI symptoms, hydration, protein adequacy, lean-mass proxy, mood, alcohol-craving notes, and discontinuation events.

BioStack must not:

- Give clinical dosing instructions.
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
| Human clinical evidence | Eligible for `Strong` or `Moderate` only when the exact claim, population, endpoint, form, and context match. |
| Mechanistic human evidence | Mechanism support; not outcome proof by itself. |
| Animal evidence | Preclinical context; cannot establish human efficacy. |
| In vitro evidence | Mechanism discovery only. |
| Expert opinion | Context signal; cannot override source-reviewed evidence. |
| Practitioner pattern | Market or practice signal; review-gated before user-facing use. |
| Community anecdote | Popularity, alias, adverse-self-report, or research-priority signal only. |
| Market signal only | Product-discovery signal; never evidence of efficacy. |
| Speculative hypothesis | Internal hypothesis or research gap; warning labels must stay visible. |
| Conflicting evidence | Contradiction surface or review queue trigger. |
| No meaningful evidence found | `Unsupported`, `Insufficient`, `Unknown`, or unpublished, depending on review result. |

Evidence tier is separate from confidence. Confidence means confidence in the grade or classification, not confidence that a substance works.

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

BioStack must not provide GLP-1 medication advice, dose changes, switching guidance, tapering instructions, compounded sourcing guidance, or product-specific directions.

## High-Risk Warning-First Categories

The following categories are warning-first:

- SARMs.
- SERMs.
- Investigational peptides.
- Gray-market substances.
- Compounded GLP-1s.
- Prescription-only substances.
- Banned-in-sport substances.

Warning-first means the product surface leads with regulatory status, source-quality uncertainty, evidence limits, known risk domains, review state, and clinician-discussion language. Benefit claims, if any, must be narrower than the source evidence and must never normalize unsupervised use.

## Promotion Canon

Research can become runtime-visible only through promotion-gated artifacts. Promotion requires:

- Structured artifact format.
- Source references.
- Evidence tier and confidence.
- Source authority mix.
- Safety concern level.
- Product handling decision.
- High-risk category review.
- Forbidden-output scan.
- Human review for safety-critical, regulatory, high-risk, prescription, hormone-axis, GLP-1, SARM, SERM, peptide, or source-quality claims.

Community and market signal may be promoted as signal only, never as proof.
