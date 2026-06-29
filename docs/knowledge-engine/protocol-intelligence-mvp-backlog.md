# Protocol Intelligence MVP Backlog

Date: 2026-06-18
Source research: `research/protocol-intelligence/biostack-ai-model-data-asset-research-memo.md`
Status: MVP backlog and promotion lanes

## MVP Outcome

BioStack should gain a canonical Protocol Intelligence foundation that supports phase-aware protocol mapping, evidence-tiered relationships, side-effect ambiguity detection, source-quality classification, GLP-1 observability, and high-risk substance guardrails.

## Backlog Priorities

| Priority | Epic | User problem | MVP deliverable | Safety posture |
| ---: | --- | --- | --- | --- |
| 1 | Phase-Aware Protocol Map | Flat logs miss intent and sequencing. | Phase taxonomy and phase-event artifact contract. | Phase labels are descriptive, not instructions. |
| 2 | Evidence-Tiered Relationship Graph | Users cannot tell evidence quality for relationships. | Relationship taxonomy with evidence tier, confidence, source refs, and review status. | Runtime consumes reviewed artifacts only. |
| 3 | Side-Effect Ambiguity Detector | Multi-substance users cannot tell what changed before symptoms. | Recent-change and attribution-risk artifact contract. | No diagnosis or causal certainty. |
| 4 | Source Quality Tracker | Product identity and purity risk are invisible. | Source-quality taxonomy and promotion requirements. | No sourcing guidance. |
| 5 | GLP-1 Observability Pack | GLP-1 users need whole-protocol tracking without medication advice. | Observability checklist and relationship targets for appetite, GI, hydration, protein, lean-mass proxy, mood, alcohol-craving notes, and discontinuation. | No medication advice, tapering, switching, or dosing. |
| 6 | High-Risk Warning Module | SARMs, SERMs, peptides, gray-market, prescription-only, compounded, and sport-banned substances are normalized online. | Warning-first guardrails and category promotion specs. | No protocol-builder support for high-risk categories. |

## Implementation Slices

### Slice 1: Artifact Taxonomies

Create parseable taxonomies for phases, relationship types, source-quality classes, observability domains, and high-risk categories.

Acceptance evidence:

- JSON files parse.
- Canon references each taxonomy.
- Each high-risk category has `warningFirst: true`.

### Slice 2: Promotion-Target Specs

Define promotion targets for initial infrastructure artifacts: phase events, relationships, source-quality classifications, GLP-1 observability, side-effect ambiguity, and high-risk guardrails.

Acceptance evidence:

- Promotion specs include required fields, review gates, blocked output types, and source authority expectations.
- High-risk and source-quality specs require human review.

### Slice 3: Safety Guardrail Scanner Contract

Document forbidden outputs and required artifact fields for a future scanner.

Acceptance evidence:

- Guardrail doc lists forbidden outputs and approved language.
- Promotion specs include `forbiddenOutputScanRequired: true`.

### Slice 4: Runtime Cutover Plan

Keep the existing architecture direction: runtime should look up reviewed artifacts and return `Unknown` when no artifact exists.

Acceptance evidence:

- Canon states "Unknown beats inference."
- Relationship promotion target requires review status and source refs.

### Slice 5: GLP-1 Observability Pack

Define GLP-1 observability fields and user-facing boundaries.

Acceptance evidence:

- GLP-1 domains are present in structured JSON.
- Source-quality and compounded-product warnings are required.
- No medication advice is present.

## Initial Promotion Lanes

| Lane | Include now | Include later | Warning-only | Avoid |
| --- | --- | --- | --- | --- |
| Phase events | Baseline, preparation, loading, active, escalation, support, maintenance, washout, recovery, reassessment labels. | More granular phase heuristics after user testing. | Phase labels on high-risk substance events. | Phase instructions that tell users what to do. |
| Relationships | Evidence-tiered relationships with reviewed source refs. | Embeddings and LLM proposal assist after review queue exists. | Community or weak-evidence relationships. | Runtime-generated unreviewed relationship claims. |
| Source quality | FDA-approved, prescription-only, compounded, supplement, third-party certified, research chemical, gray-market, investigational, sport-banned, unknown. | Cost lineage and source change history. | Compounded, research, gray-market, unknown. | Vendor recommendations or sourcing. |
| GLP-1 observability | Appetite, GI, hydration, protein, fiber, strength proxy, mood, alcohol-craving notes, discontinuation event tracking. | Lab and wearable integration. | Compounded GLP-1 source-quality warnings. | Medication changes, dose guidance, switching, tapering. |
| High-risk categories | Warning-first educational context. | Admin-only research workflows. | SARMs, SERMs, investigational peptides, gray-market, compounded GLP-1s, prescription-only, banned-in-sport. | Protocol builders, cycles, PCT, sourcing, injection help. |
