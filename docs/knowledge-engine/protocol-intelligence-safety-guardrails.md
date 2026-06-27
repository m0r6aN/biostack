# Protocol Intelligence Safety Guardrails

Date: 2026-06-18
Source research: `research/protocol-intelligence/biostack-ai-model-data-asset-research-memo.md`
Status: Required operating guardrails

## Non-Negotiable Boundary

BioStack remains observational, educational, evidence-bounded, and safety-aware. Protocol Intelligence artifacts are allowed to organize uncertainty; they are not allowed to create medical, sourcing, dosing, injection, cycle, recovery, or treatment guidance.

## Forbidden Outputs

No BioStack surface, artifact, generated explanation, promotion target, or runtime response may contain:

- Clinical dosing instructions.
- Injection instructions.
- SARM cycle design.
- SERM recovery protocols.
- Post-cycle therapy instructions.
- Sourcing guidance.
- Claims that investigational peptides are safe or effective for human use.
- Claims that community anecdotes prove efficacy.
- Protocol-builder flows for SARMs, SERMs, investigational peptides, gray-market compounds, or other high-risk categories.
- Recommendations to start, stop, taper, combine, escalate, swap, or substitute substances.

## Warning-First High-Risk Categories

High-risk categories must render warning-first and review-gated:

| Category | Required handling |
| --- | --- |
| SARMs | Regulatory warning, hormone-axis risk domains, no cycle design, no recomposition protocol support. |
| SERMs | Prescription/off-label boundary, serious-warning context, no recovery protocol or post-cycle support. |
| Investigational peptides | Human evidence limitations, regulatory status, source-quality uncertainty, no safety or efficacy claims. |
| Gray-market substances | Identity and purity uncertainty, no sourcing or vendor guidance. |
| Compounded GLP-1s | Product-specific source-quality and regulator-warning context, no dose or switching guidance. |
| Prescription-only substances | Label/context separation, qualified-professional escalation, no medical directions. |
| Banned-in-sport substances | Current sport-policy verification requirement, no performance-use normalization. |

## Guardrail Decision Table

| Input pattern | Allowed output | Blocked output |
| --- | --- | --- |
| User logs a high-risk substance | Source class, evidence tier, regulatory status, warning domains, observation prompts. | How to use it, dose it, combine it, source it, or recover from it. |
| User asks about side effects after a change | Recent-change ambiguity panel and qualified-professional language. | Diagnosis, causality assertion, or instruction to stop/start/change. |
| User asks about GLP-1 support | Observability checklist for appetite, GI symptoms, hydration, protein adequacy, lean-mass proxy, mood, alcohol craving, and discontinuation events. | Medication guidance, tapering, switching, dosing, or compounded-product sourcing. |
| Community pairing is common | "Commonly discussed" with evidence limits and research-gap status. | "Recommended pair," "proven synergy," or efficacy based on anecdotes. |
| Evidence is animal or in vitro only | Research-gap or plausible-mechanism framing. | Human outcome, safety, or effectiveness claim. |
| Source is gray market or unknown | Identity, purity, and label uncertainty warning. | Vendor recommendation, sourcing guidance, or confidence in product contents. |

## Artifact-Level Required Fields

Every Protocol Intelligence artifact that reaches promotion review must include:

- `safetyConcernLevel`: `low`, `medium`, `high`, or `critical`.
- `productHandling`: `include_now`, `include_later`, `warning_only`, `avoid`, or `review_required`.
- `highRiskCategory`: nullable, but required when any warning-first category applies.
- `forbiddenOutputScan`: explicit pass/fail with scanned phrases or rules.
- `requiresHumanReview`: true for high-risk, regulatory, safety, prescription, hormone-axis, GLP-1, peptide, SARM, SERM, source-quality, contradiction, and adverse-effect claims.
- `userFacingBoundary`: approved user-facing boundary text.

## Approved Language

- "BioStack has not evaluated this relationship in this context."
- "This is an observation prompt, not medical guidance."
- "Community discussion is treated as market signal, not proof."
- "Evidence is limited, conflicting, or not established for this claim."
- "This source class has identity, purity, or regulatory uncertainty."
- "Discuss medication, condition, pregnancy, surgery, lab, or concerning symptom decisions with a qualified professional."

## Disallowed Language

- "BioStack recommends..."
- "You should start..."
- "You should stop..."
- "Take..."
- "Inject..."
- "Run this cycle..."
- "Use this for post-cycle therapy..."
- "Safe and effective..."
- "Proven by user reports..."
- "Best source..."

## Runtime Safety Rules

1. Unknown beats inference. If no reviewed relationship exists, return `Unknown`.
2. Safety guard runs after lookup and may downgrade or suppress a claim. It may never upgrade a claim.
3. Unreviewed, rejected, draft, or needs-review artifacts are admin-only.
4. Community signal can raise review priority, not evidence tier.
5. High-risk categories cannot enter protocol-builder flows.
6. Evidence categories and source classes must be visible on user-facing claims.
7. Commonly studied ranges, if ever shown in future artifacts, must be source context only and never an instruction.

## Source-First Model And Data Guardrails

These guardrails apply to model/data asset usage in the Evidence-Guided Protocol Intelligence Engine. BioStack is not a passive research database: it may provide evidence-informed educational guidance, risk-aware recommendations, protocol interpretation, and decision support when outputs are grounded in cited sources, clear uncertainty, safety boundaries, and user-observable context.

1. Every user-facing evidence claim must have source provenance.
2. Retrieved sources must be cited by stable IDs where available, including PMID, PMCID, DOI, NCT ID, SPL set ID, NDC, RxCUI, ChEMBL ID, Reactome ID, WADA list year, or source URL.
3. FAERS and FAERS-derived sources must never be presented as causality, incidence, risk rate, or proof.
4. ClinicalTrials.gov registry entries must not be treated as peer-reviewed outcome evidence.
5. Supplement labels and NIH DSLD label claims must be separated from evidence sources such as NIH ODS, PubMed, or licensed reviewed monographs.
6. Open medical LLMs must not be user-facing medical authorities.
7. Regulatory status must never be inferred from LLM memory.
8. High-risk categories must use deterministic rules before any LLM synthesis.
9. Human review is required before canonical knowledge promotion.
10. Licensing status must be checked before redistribution, export, model training, or user-facing display of restricted-source-derived content.
11. A missing citation, missing license status, missing retrieval timestamp, or missing review state must fail closed for evidence claims and risk classifications.

## Allowed BioStack Guidance

BioStack may provide:

- Evidence-context recommendations.
- Risk-aware educational guidance.
- Tracking and baseline recommendations.
- Source-quality warnings.
- Regulatory-status warnings.
- Protocol complexity warnings.
- Side-effect ambiguity analysis.
- Research-gap alerts.
- Clinician-escalation suggestions.
- Safer decision pathways.
- Statements that a claim is speculative, weakly supported, high risk, or unsupported.

Preferred user-facing language includes:

- BioStack guidance.
- Evidence context.
- Risk signal.
- What to track.
- What changed.
- What is uncertain.
- Discuss with a qualified clinician.
- This appears higher risk because...
- This claim should be treated as speculative because...
- This pattern may complicate attribution because...

Avoid user-facing language such as:

- AI recommends.
- AI says you should.
- AI's best guess.
- The best dose for you.
- You should take.
- Start this.
- Increase this.
- Stop this.

## Prohibited Knowledge-Engine Outputs

The knowledge engine must refuse or suppress:

- Individualized medical dosing.
- Diagnosis.
- Treatment plans.
- Prescribing recommendations.
- Peptide injection instructions.
- SARM cycle generation.
- SERM cycle generation.
- Post-cycle therapy guidance.
- Sourcing guidance.
- Individualized medical-device claims.
- Start, stop, taper, or escalation instructions.
- Claims that investigational or gray-market substances are safe or effective for human use.
- User-facing open medical authority without strict retrieval, citation, uncertainty, and refusal controls.

## Retatrutide Handling

Retatrutide must be treated as investigational and not FDA-approved for public use. Trial exposure data may be cited as research context, not user instructions. Gray-market products must trigger source-quality and identity-risk warnings. Influencer claims must be classified as market signal only. Any planned exposure beyond cited trial context must trigger high-risk uncertainty language and clinician-escalation suggestions, not dosing advice, injection instructions, sourcing guidance, or treatment planning.
