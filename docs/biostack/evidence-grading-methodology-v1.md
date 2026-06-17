<!-- markdownlint-disable MD013 -->

# BioStack Evidence Grading Methodology v1

## Purpose

BioStack is an evidence-aware protocol intelligence product for organizing supplement, compound, protocol, and wellness-claim information. It is not a medical device, diagnostic tool, treatment system, prescribing engine, clinician replacement, or source of personalized medical guidance.

This methodology defines how BioStack grades claims across evidence tiers. It is the operating standard for the first 50 compound records, user-facing evidence labels, the Stack Report, evidence changelogs, contradiction surfacing, and marketing-claim review.

The goal is to help users distinguish established evidence, plausible theory, early evidence, anecdotal signal, contradictions, unsupported claims, and bro-science. BioStack grades the support behind a claim. It does not tell a user what to take, start, stop, combine, or dose.

## Scope And V1 Boundary

This v1 methodology is anchored to the first beta vertical: longevity and healthspan supplement stacks, OTC only.

In scope:

- Evidence grading for compound, protocol, mechanism, outcome, safety, tolerability, interaction-awareness, and commonly studied range claims.
- Public and internal language rules for evidence labels, Stack Report explanations, evidence changelogs, and contradiction notes.
- Compound evidence record structure for corpus creation.
- User-specific note handling that separates personal tracking observations from general scientific evidence.

Out of scope:

- Hypothesis generation.
- Personalized decision support.
- Medical diagnosis.
- Treatment, prevention, or cure claims.
- Dosing instructions.
- AI chat assistant behavior.
- Prescription-supported protocol workflows.

Prescription-adjacent compounds such as rapamycin, metformin, and peptides may be discussed editorially with evidence grades, but they must not be protocol-supported in v1.

## Core Principle: Grade Claims, Not Compounds

BioStack grades claims, not compounds as a whole. A single compound may have different grades for different claims depending on outcome, population, dose, form, study design, and evidence consistency.

Example:

Creatine may be Established for improving strength or power performance in certain studied contexts, while a claim about longevity, cognition, or unrelated wellness outcomes may receive a lower grade.

Every graded claim should answer:

- What is being claimed?
- What evidence supports or weakens it?
- Which population, form, dose range, and context were studied?
- What uncertainty, contradiction, or safety signal must be surfaced?
- How should this be explained without implying medical advice?

## Evidence Tier System

BioStack uses seven beta evidence tiers:

- Established
- Plausible Theory
- Early Evidence
- Anecdotal Signal
- Contradicted
- Unsupported
- Bro-Science

These tiers are claim-level labels. They are not endorsements, instructions, or clinical determinations.

| Tier | Plain-English definition | Minimum evidence pattern | What qualifies | What does not qualify | User-facing label guidance | Example phrasing | Prohibited phrasing |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Established | The specific claim is supported by consistent, relevant human evidence. | Preferably systematic reviews or meta-analyses, or multiple well-designed human studies with consistent outcomes and source-verified context. | Reproduced human evidence with relevant outcome, population, dose or form, and limited serious contradiction. | Mechanistic-only arguments, isolated small studies, claims beyond the studied outcome, or claims carried by marketing language. | Use narrow, context-bound wording. Name the outcome and context. Avoid universal or guaranteed language. | "This claim has strong human evidence in the studied context." | "Clinically proven to optimize longevity." |
| Plausible Theory | The claim has a coherent biological or mechanistic rationale, but direct human evidence is weak, indirect, absent, or not yet outcome-relevant. | Mechanistic, preclinical, biomarker, or adjacent human evidence that reasonably explains why the claim is being studied. | Clear mechanism, pathway relevance, credible preclinical work, or human evidence for nearby but not identical outcomes. | Human outcome certainty, user-specific predictions, disease claims, or mechanism stretched into proof. | Make the gap explicit. Use "plausible" only when the mechanism is source-supported. | "This claim is biologically plausible, but direct human outcome evidence is limited." | "This mechanism proves the compound works." |
| Early Evidence | The claim has preliminary human evidence, but the evidence base is small, mixed, early-phase, underpowered, or not yet reproduced. | At least one relevant human study or credible early clinical signal, with limitations clearly documented. | Pilot studies, small RCTs, early controlled trials, exploratory endpoints, or observational findings that are not yet settled. | Claims treated as settled, broad generalization from narrow populations, or direct instructions based on early results. | Use "early" and "mixed" where appropriate. Explain major limits. | "Evidence for this claim is early and mixed." | "This will improve your biomarkers." |
| Anecdotal Signal | The claim is supported mainly by user reports, community reports, case narratives, or n-of-1 notes, not generalizable scientific evidence. | Traceable anecdotal or self-reported signal, clearly separated from general evidence. | User notes, community reports, case reports when not enough for broader evidence, recurring qualitative observations. | Upgrading general evidence based on popularity, subreddit consensus, testimonials, or personal tracking alone. | Label as anecdotal. Keep personal and general evidence visually and textually separate. | "Some users report this pattern, but it is not established scientific evidence." | "Users report it, so it works." |
| Contradicted | Credible evidence conflicts with or directly weakens the claim, especially when higher-quality or better-matched evidence is negative. | A credible negative or conflicting evidence base that is stronger, more relevant, or more recent than evidence supporting the claim. | Failed replication, better-designed negative studies, systematic reviews finding no meaningful effect, or safety findings that undermine a benefit claim. | Merely limited evidence, weak mechanism without outcome evidence, or a single small negative study against several stronger positive studies. | State that evidence conflicts with the claim. Where credible evidence is mixed, show both sides. | "Higher-quality evidence conflicts with this claim in the studied context." | "This claim is false in every case." |
| Unsupported | The claim lacks adequate source-verified support. | No credible evidence source directly supporting the specific claim after source review. | Claims with no citations, citations that do not match the claim, or evidence that is too remote to support the claim. | Mechanistically plausible claims, early human evidence, or clearly labeled anecdotal signal. | Keep it plain. Say what is missing. | "BioStack could not verify reliable evidence for this specific claim." | "No evidence means it cannot happen." |
| Bro-Science | The claim is driven by hype, influencer repetition, distorted citations, or community certainty that substantially exceeds the evidence. | Evidence review finds unsupported certainty, misrepresented studies, affiliate-driven exaggeration, or claim inflation. | Claims that cite studies but change the outcome, product pages posing as proof, confident social claims, or optimization language without support. | Good-faith early evidence, credible mechanistic theory, or cautious anecdotal reports. | Use sparingly and only when the claim quality and presentation justify it. Explain the distortion without mocking users. | "This claim is commonly repeated online, but the cited evidence does not support it." | "Only uninformed people believe this." |

## Claim Grading Unit

The graded unit is a specific claim in a specific context. A valid claim record should be precise enough that a reviewer can decide whether the cited evidence matches it.

BioStack may grade:

- Compound claims: claims about a compound, ingredient, form, or class.
- Protocol claims: claims about a structured stack, timing pattern, or protocol concept, without instructing the user to follow it.
- Mechanism claims: claims about pathways, receptors, biomarkers, absorption, metabolism, or plausible biological rationale.
- Outcome claims: claims about studied outcomes such as performance, sleep metrics, subjective reports, lab-associated markers, or quality-of-life measures.
- Dosing-range claims: claims that a range is commonly studied, only when presented as a sourced research context and not as an instruction.
- Safety and tolerability claims: claims about reported adverse events, tolerability, warnings, or known safety context.
- Interaction and contraindication awareness claims: claims that certain combinations, conditions, medications, pregnancy status, surgery, or lab-related decisions deserve qualified professional review, without giving medical advice.

Each claim should be decomposed when needed:

- "Magnesium helps sleep" is too broad.
- "Magnesium glycinate has limited human evidence for subjective sleep quality in adults with sleep complaints" is gradeable.
- "Berberine lowers glucose" is too broad and may cross into disease-management framing.
- "Berberine has human evidence for glucose-related markers in studied metabolic contexts" is more gradeable, but must still avoid personal guidance.

## Evidence Inputs

### Allowed Evidence Sources

BioStack may consider:

- Systematic reviews and meta-analyses.
- Randomized controlled trials.
- Controlled human studies.
- Observational studies.
- Mechanistic and preclinical studies.
- Case reports and case series.
- Expert consensus or guidelines, where applicable.
- Regulatory labels, safety notices, and public agency materials for safety, status, warnings, and approved-use context.
- User-reported n-of-1 notes, clearly separated from general evidence.
- Anecdotal and community reports, clearly labeled as anecdotal and not treated as general evidence.

### Disallowed Or Low-Trust Sources

These sources may be useful for claim discovery, misinformation monitoring, or popularity context, but they cannot establish a claim:

- Influencer claims without citations.
- Affiliate-driven claims without evidence.
- Unsourced Reddit or forum claims.
- Vendor or marketing pages.
- AI-generated summaries without source verification.
- Claims that cite studies but misrepresent outcomes, populations, dose, form, direction, or certainty.
- Product pages that borrow scientific language from ingredient studies without showing product-specific evidence.

### Source Use Rules

- Every nontrivial claim must cite source IDs.
- Low-trust sources may identify claims to investigate, not truth to publish.
- Safety-critical, regulatory, contraindication, medication, pregnancy, surgery, or lab-related claims require authoritative review.
- AI may propose candidate summaries, but source verification and final grade assignment require human review for beta corpus records.
- Runtime surfaces should consume reviewed artifacts. They should not invent new compound claims.

## Grading Dimensions

Reviewers assign a tier by weighing the following dimensions together. No single dimension automatically determines the grade.

| Dimension | Reviewer question |
| --- | --- |
| Human evidence strength | Is there direct human evidence for the exact claim? |
| Study quality | Are the design, control, sample size, endpoints, and analysis credible for the claim? |
| Reproducibility | Has the finding been repeated across independent studies or study groups? |
| Outcome relevance | Does the studied endpoint match the user-facing claim, or is it a proxy? |
| Population relevance | Do the studied participants match the population implied by the claim? |
| Dose and form relevance | Does the evidence match the compound form, route, timing, and commonly studied range being discussed? |
| Safety and tolerability signal | Are adverse events, tolerability limits, warnings, and uncertainty clearly represented? |
| Consistency across sources | Do credible sources agree, disagree, or vary by context? |
| Recency and evidence drift | Is the evidence current enough, or has newer evidence changed the interpretation? |
| Conflict or contradiction presence | Are there credible negative findings or unresolved conflicts? |
| Commercial bias or conflict of interest | Is the evidence influenced by sponsor, affiliate, product, or promotional bias? |
| Mechanistic plausibility | Does the mechanism support the claim, and is it being kept separate from human outcome proof? |

Confidence is separate from evidence tier. A claim may be "Early Evidence" with high confidence that the evidence is early, or "Unsupported" with high confidence that no adequate support was found during review.

## Tier Assignment Rules

Use the highest tier that the exact claim can support after constraints are applied.

1. Match the claim to evidence before grading.
2. Narrow the claim when the evidence is narrow.
3. Downgrade when evidence is indirect, mixed, commercially biased, stale, form-mismatched, dose-mismatched, or population-mismatched.
4. Surface safety and contradiction context even when the benefit evidence is strong.
5. Do not upgrade general evidence based on user notes, popularity, influencer certainty, or community consensus.
6. Do not use "Established" unless the exact outcome and context are supported by consistent relevant human evidence.
7. Use "Bro-Science" only when the claim is not merely unsupported, but is presented with inflated certainty, distorted citations, or hype-driven confidence.

## Contradiction Handling

BioStack does not hide contradictions. When credible sources conflict, the product should show both sides where credible and explain the likely reason for disagreement.

Contradictions may arise across:

- Study type.
- Outcome definition.
- Population.
- Dose or form.
- Duration.
- Comparator.
- Endpoint quality.
- Safety signal.
- Publication recency.
- Sponsor or commercial bias.

Use mixed-evidence language when the evidence is unresolved:

- "Evidence is mixed across studied outcomes."
- "Findings differ by population and form."
- "The mechanism is plausible, but human outcome findings are inconsistent."
- "A newer review found weaker support than earlier individual studies."

Use "Contradicted" when the specific claim is weakened by credible, better-matched, higher-quality, or more recent evidence. Do not use "Contradicted" simply because a claim is early, mechanistic, or unsupported.

Decision guide:

| Situation | Likely label |
| --- | --- |
| Mechanism is plausible, but direct human evidence is absent | Plausible Theory |
| One or more small human studies suggest a signal, but findings are limited or mixed | Early Evidence |
| Community reports exist, but no reliable generalizable evidence supports the claim | Anecdotal Signal |
| The claim cites evidence that does not actually support the outcome | Unsupported or Bro-Science |
| Stronger or better-matched evidence directly conflicts with the claim | Contradicted |
| Promotional certainty materially exceeds evidence and misleads users | Bro-Science |

## Evidence Changelog Rules

Evidence grades are expected to change as the corpus improves. A grade change is not a product failure. It is part of evidence-aware design.

### Grade Change Triggers

A claim grade may change when BioStack identifies:

- A new higher-quality study.
- A new systematic review or meta-analysis.
- A safety warning or major adverse signal.
- A retraction, correction, expression of concern, or material publication issue.
- A strong contradiction from better evidence.
- A better match for the relevant population, dose, form, or outcome.
- Evidence decay, stale citation review, or a review interval that reveals outdated interpretation.
- Source verification failure.
- Discovery that a cited source was misrepresented.

### Changelog Entry Format

Each changelog entry should include:

| Field | Requirement |
| --- | --- |
| Claim | The exact claim whose grade changed. |
| Previous grade | The prior evidence tier. |
| New grade | The updated evidence tier. |
| Reason changed | One-sentence reason for the grade movement. |
| Evidence added or removed | Source IDs and short source descriptions. |
| User-facing explanation | Plain-language explanation without medical advice. |
| Internal reviewer note | Review context, uncertainty, and any unresolved issues. |

Example:

| Field | Example |
| --- | --- |
| Claim | "Compound X supports outcome Y in healthy adults." |
| Previous grade | Early Evidence |
| New grade | Contradicted |
| Reason changed | A newer, better-matched review found no consistent effect for the stated outcome. |
| Evidence added or removed | Added source IDs: `review-2026-x-y`; removed source ID: `blog-summary-x`. |
| User-facing explanation | "Newer evidence conflicts with this claim in the studied context, so BioStack now labels it as contradicted." |
| Internal reviewer note | Check again after any larger RCTs in the same population. |

## User-Specific Signal Rules

BioStack may help users organize personal tracking notes, but user-specific observations do not upgrade general scientific evidence.

Rules:

- User-specific notes can support personal tracking insights.
- User-specific notes do not upgrade general evidence tiers.
- N-of-1 signal must be labeled separately from general evidence.
- Personal notes may identify a pattern worth tracking, not a proven cause.
- Avoid causal claims unless the evidence supports causality.
- Use careful association language.
- When medication, condition, pregnancy, surgery, lab, or adverse-event context appears, point the user to a qualified professional.

Approved user-specific wording:

- "You reported better sleep on weeks when this was present."
- "Your notes suggest a possible personal pattern, but this is not proof of causation."
- "This may be associated with your logged pattern, but other factors may explain it."
- "Worth discussing with a qualified professional if this relates to medication, a condition, pregnancy, surgery, or lab results."

Prohibited user-specific wording:

- "This caused your improvement."
- "This treats your condition."
- "This proves the stack works."
- "BioStack recommends this."
- "You should take this."
- "This is personalized medical guidance."

## Wellness And Compliance Boundary

BioStack must keep a clear wellness, education, and evidence-organization boundary.

Rules:

- No diagnosis.
- No treatment recommendations.
- No prevention or cure claims.
- No dosing advice.
- No "AI doctor" language.
- No "AI health advisor" language.
- No outcome guarantees.
- No "clinically proven" language unless the exact claim is supported and phrased narrowly.
- Present commonly studied ranges only when sourced, contextualized, and not framed as instructions.
- Human decides. BioStack informs.
- Medication, condition, pregnancy, surgery, lab-related, or adverse-event decisions should be discussed with a qualified professional.

Commonly studied range language:

- Approved: "Human studies reviewed by BioStack commonly used ranges of X to Y in the studied context. This is evidence context, not a dosing instruction."
- Prohibited: "BioStack recommends X to Y."

## User-Facing Copy Rules

### Approved Examples

- "Evidence for this claim is early and mixed."
- "This claim is supported mainly by mechanistic or preliminary human evidence."
- "This compound has stronger evidence for X than for Y."
- "Your notes suggest a possible personal pattern, but this is not proof of causation."
- "Discuss medication, condition, pregnancy, surgery, or lab-related decisions with a qualified professional."
- "BioStack could not verify reliable evidence for this specific claim."
- "Evidence differs by population, form, and outcome."
- "This is a commonly repeated claim, but the cited evidence does not support the stated outcome."

### Prohibited Examples

- "This treats..."
- "This prevents..."
- "This cures..."
- "BioStack recommends this dose..."
- "Clinically proven to optimize longevity..."
- "This will improve your biomarkers..."
- "Personalized medical guidance..."
- "You should start..."
- "You should stop..."
- "Safe for everyone..."
- "Guaranteed results..."

### Label Tone Rules

- Be direct and plain.
- Explain uncertainty without burying it.
- Do not mock users for encountering weak claims.
- Do not imply that absence of evidence proves impossibility.
- Do not use credential-borrowing language such as "doctor-approved" unless formally documented and legally reviewed.
- Do not hide commercial relationships or affiliate context.

## Compound Evidence Record Template

Use this template for the first 50 compound records. It is designed for corpus creation, human review, Stack Report rendering, and future evidence changelogs.

```markdown
# Compound Evidence Record

## Compound Name

<Canonical compound name>

## Common Forms

- <Form 1>
- <Form 2>

## Category

<Supplement category or beta vertical category>

## Summary

<Plain-language summary of what BioStack can and cannot say about the compound. No medical advice. No broad claims beyond the reviewed evidence.>

## Claims Table

| Claim | Evidence tier | Confidence | Evidence basis | Key citations | Population/context | Dose/form studied | Safety notes | Contradictions | User-facing explanation | Last reviewed |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| <Specific claim> | <Established/Plausible Theory/Early Evidence/Anecdotal Signal/Contradicted/Unsupported/Bro-Science> | <High/Moderate/Low confidence in grade assignment> | <Short basis by study type and source quality> | <Source IDs> | <Population and context> | <Commonly studied range or form, if sourced. Not an instruction.> | <Known warning, tolerability context, or unknown> | <Mixed findings, negative findings, or none identified> | <Plain user-facing copy> | <YYYY-MM-DD> |

## Changelog

| Date | Claim | Previous grade | New grade | Reason changed | Evidence added or removed | User-facing explanation | Internal reviewer note |
| --- | --- | --- | --- | --- | --- | --- | --- |
| <YYYY-MM-DD> | <Claim> | <Grade> | <Grade> | <Reason> | <Source IDs> | <Plain explanation> | <Reviewer note> |

## Internal Notes

- Source verification notes:
- Open questions:
- Commercial bias checks:
- Safety review notes:
- Contraindication or interaction-awareness notes:
- Evidence drift watchlist:

## Review Status

- Status: <Draft/In review/Approved for beta/Needs revision/Retired>
- Reviewer:
- Last reviewed:
- Next review due:
- Review blockers:
```

## Stack Report Grading Behavior

The Stack Report is the first-session activation moment. It should organize evidence for the user's entered stack without giving instructions.

Behavior:

- User enters current stack.
- BioStack displays evidence grades per claim, not one global verdict per compound.
- BioStack highlights grade surprises, such as stronger evidence for one claim than another.
- BioStack surfaces contradictions and mixed evidence.
- BioStack separates general evidence from user-specific notes.
- BioStack uses commonly studied ranges only as sourced research context.
- BioStack avoids recommendations and instead provides evidence-aware organization.
- BioStack keeps safety, interaction, medication, condition, pregnancy, surgery, and lab-related signals framed as topics to discuss with a qualified professional.

Stack Report should not:

- Recommend adding, removing, combining, or dosing a compound.
- Present a stack score as a health outcome prediction.
- Claim that a protocol will change biomarkers or longevity.
- Convert personal notes into scientific proof.
- Hide weak evidence behind confident language.

Example Stack Report phrasing:

- "Creatine has stronger evidence for strength and power performance claims than for longevity claims."
- "Evidence for this NAD+ precursor claim is early and mixed."
- "BioStack found contradiction signals for this outcome. Review the cited sources before treating the claim as settled."
- "Your notes are shown separately from general evidence and do not change the evidence grade."

## Beta Vertical Alignment

V1 is limited to longevity and healthspan supplement stacks, OTC only.

Initial examples may include:

- Creatine.
- Magnesium forms.
- Omega-3s.
- Berberine.
- Glycine.
- NAC.
- NAD+ precursors.

These examples should be graded claim by claim. The presence of a compound in the corpus does not imply endorsement, recommendation, or protocol support.

Prescription-adjacent compounds such as rapamycin, metformin, and peptides may be discussed editorially only with evidence grades. They must not be protocol-supported in v1.

## Quality Control And Review Process

### Minimum Citation Expectations

- Aim for 2 to 5 citations per nontrivial claim where possible.
- Use at least one directly relevant source for any published claim.
- If no adequate source supports the claim, label it Unsupported or do not publish it.
- Use higher-authority sources for safety, regulatory, contraindication, medication, pregnancy, surgery, and lab-related context.
- Do not pad citation count with irrelevant studies.

### Required Source Verification

Before approval, reviewers must verify:

- The source exists.
- The citation matches the compound, form, and claim.
- The studied population matches or limits the user-facing statement.
- The outcome matches the claim.
- The direction and magnitude are not overstated.
- The study type is represented correctly.
- Commercial bias is documented.
- Contradictions are surfaced.
- Safety and tolerability context is not omitted.

### Review Cadence

- High-demand claims: review at least quarterly during beta.
- Claims with active new literature or known controversy: review monthly or when new material evidence appears.
- Safety-sensitive claims: review promptly when alerts, corrections, or credible adverse signals appear.
- Low-traffic stable claims: review at least twice per year.
- Retired or unsupported claims: keep an internal record explaining why they are not surfaced.

### Internal Reviewer Checklist

Before approving a claim record, confirm:

- The claim is specific and gradeable.
- The evidence tier is one of the seven beta tiers.
- Confidence is separate from evidence tier.
- Evidence basis names source type and quality.
- Key citations are source-verified.
- Population, context, dose, and form limits are documented.
- Safety notes are present or explicitly unknown.
- Contradictions are present or explicitly not identified.
- User-facing explanation avoids medical advice.
- Commonly studied ranges are not phrased as instructions.
- User-specific notes, if any, are separate from general evidence.
- Changelog status is initialized.
- Review status and next review due date are filled.

### Handling Uncertainty

- Use uncertainty language when the evidence is early, indirect, mixed, or stale.
- Prefer narrow claims over broad hedging.
- Mark missing evidence explicitly.
- Do not create confident copy to improve conversion.
- Do not infer outcomes from mechanisms alone.

### Handling Missing Evidence

When evidence is missing:

- Mark the claim Unsupported, leave it unpublished, or route it to review.
- Do not backfill with marketing pages.
- Do not use AI-generated summaries as evidence.
- Do not treat user notes as general evidence.
- Keep an internal note describing the search performed and the missing source type.

### Handling Retractions And Corrections

When a source is retracted, corrected, or materially challenged:

- Reopen every claim that relies on that source.
- Remove or downgrade the source contribution.
- Add a changelog entry if the user-facing grade or explanation changes.
- Preserve an internal audit note.
- Recheck related claims that may inherit the same evidence.

### Handling Affiliate Or Commercial Bias

When evidence or claims originate from commercially interested sources:

- Document the conflict or sponsor relationship where known.
- Prefer independent replication before assigning higher tiers.
- Do not allow affiliate copy to establish evidence.
- Separate shopping or affiliate surfaces from evidence-grade explanations.
- Avoid wording that turns evidence labels into purchase prompts.

## Marketing Claims Discipline

Every public sentence about BioStack evidence intelligence should pass this filter:

- Does the sentence make a health, outcome, performance, or safety claim?
- Does the sentence imply personalized guidance?
- Does it use treatment, prevention, cure, diagnosis, prescription, or dosing language?
- Does it overstate the evidence tier?
- Does it imply a result BioStack cannot verify?
- Does it hide uncertainty or contradiction?
- Does it rely on a user testimonial as proof?
- Does it imply clinical status BioStack does not have?

Safer public framing:

- "Evidence-graded protocol intelligence."
- "Grades supplement and protocol claims across seven evidence tiers."
- "Citations you can inspect."
- "Evidence updates when grades change."
- "Personal notes shown separately from general evidence."
- "Not medical advice. Human decides. BioStack informs."

## What BioStack Does Not Do

BioStack does not:

- Diagnose.
- Prescribe.
- Replace a clinician.
- Guarantee outcomes.
- Create treatment plans.
- Provide prevention or cure guidance.
- Recommend compounds, combinations, starts, stops, or doses.
- Treat user notes as scientific proof.
- Upgrade general evidence based on popularity.
- Hide weak evidence behind confident language.
- Fake evidence or operational data.
- Use AI summaries without source verification.
- Present hypothesis generation or personalized decision support in beta.
- Protocol-support prescription-adjacent compounds in v1.

## Version Notes

- Version: v1.
- Source of truth: Keon Systems + BioStack 90-Day Execution Pack, Version 1.0, June 2026.
- Beta vertical: longevity and healthspan supplement stacks, OTC only.
- Primary product surfaces supported: first 50 compound records, evidence labels, Stack Report, evidence changelog, contradiction surfacing, and marketing-claim review.
