# BioStack Guidance Content Contract

| Field | Value |
|---|---|
| Version | **1.1.0** |
| Status | **Fully ratified** — see `RATIFICATION.md` |
| Effective for implementation | Class A/B/C engineering and product surfaces may use this contract under copy-guards and the AI council review gate |
| Effective for public UX | **Unblocked** for Classes A–C when wording obeys this contract; Class D remains prohibited |
| Date | 2026-08-02 |
| Fully ratified | 2026-08-02 (Clint Morgan — all gates passed; no remaining blockers) |
| Revised | 2026-10-24 (Clinton Morgan, sole owner and decision maker): Class D rewritten; reviewer-role approval levels replaced by the AI council gate; doses from online sources added to Class A |
| Ratification package | `docs/guidance/RATIFICATION.md` |
| Product canon | `docs/canon/biostack-protocol-intelligence-canon.md` (evidence-context §) |

---

## Purpose

Define what BioStack may and must not say when surfacing scientific research, published exposure context, and comparisons between user-recorded protocol entries and reviewed evidence.

This contract does **not** authorize BioStack to prescribe, diagnose, treat, or calculate a personalized medical dose.

---

## Output classes

### Class A — Published evidence context

**Status:** Permitted when source-backed and correctly labeled.

Permitted content:

- Trial initiation amounts, escalation schedules, maintenance ranges
- Frequency and route used in a cited source
- Study duration, population characteristics, inclusion/exclusion criteria
- Outcomes, adverse events, discontinuation rates
- Regulatory label information and official safety communications
- Case-report amounts (labeled as case reports)
- Observational or community-reported patterns (**clearly labeled**)
- Doses reported on Reddit, Facebook, Instagram, forums and blogs, shown on the **reported** side of the "Doses found" panel with source link and grade (`Anecdotal Signal` or `Hype Exceeds Evidence` unless the source supports more). Facebook/Instagram items are cited links or manual entries only.
- `n/a` where a side has nothing found

Required fields when stating amounts:

| Field | Required |
|---|---|
| Source citation / identifier | Yes |
| Population summary | Yes when available; else explicit “population not stated in source” |
| Amount + unit | Yes |
| Route | Yes when available; else explicit unknown |
| Frequency | Yes when available; else explicit unknown |
| Exposure role | initiation / escalation / maintenance / maximum / not classified |
| Evidence class | Yes (see Phase 5 taxonomy) |
| Grade | Yes — 7-tier methodology scale |
| Uncertainty / limitations | Yes when material |

Example (permitted):

> Reviewed trials initiated participants between 0.5 and 1.0 mg weekly and used the following escalation schedules. [citations]

---

### Class B — Evidence comparison

**Status:** Permitted with reviewed wording templates and deterministic math.

Permitted:

- Compare a user-recorded amount with published study ranges
- State how many times higher or lower an entry is than a researched amount
- Identify that no reviewed study used a comparable initiation amount
- Show evidence unavailable for a route, frequency, combination, or population
- Flag unit mismatches and likely decimal errors
- Identify animal / in-vitro / case-report / uncontrolled evidence
- Show that an entered plan differs materially from reviewed research protocols

Required:

- Deterministic unit/frequency normalization (no LLM for the math)
- Attached source references for the compared ranges
- Explicit applicability limitations when population differs
- No inference of personal harm certainty from magnitude alone

Example (permitted):

> The recorded 12 mg amount is 12 to 24 times the initiation range used in the reviewed trials. No reviewed trial in this evidence set initiated participants at 12 mg.

---

### Class C — Evidence-guided harm-reduction context

**Status:** Permitted through content templates that passed the AI council review gate (see "Review gate").

Permitted:

- Highlight lower-exposure initiation patterns found in credible human evidence
- Show escalation approaches used in trials
- Explain that slower or lower initiation was used to manage tolerability **in a cited source**
- Show what researchers monitored
- Show conditions that led to interruption or discontinuation
- Surface official contraindications/warnings with exact scope
- Encourage review before proceeding when an entry is materially outside available evidence

Must not morph into “you should start at X.”

Example (permitted):

> The reviewed evidence supports a lower-exposure initiation context than the amount entered. Discuss material differences with a qualified clinician before proceeding.

---

### Class D — Personalized medical direction (BioStack never prescribes)

**Status:** **Out of scope.** BioStack never prescribes. Class D is not a gated or deferred feature; it is something BioStack does not do.

Showing what sources report — including every dose found on the study side and the reported side, each with its source and grade — is **Class A**, not Class D. Class D is only the step from "here is what was found" to "here is what you should do".

BioStack does not:

- Select a dose for a person
- Build a personal titration schedule
- Diagnose
- Tell a user an amount is safe for them
- Tell a user a protocol is appropriate for them
- Replace clinical monitoring
- Predict that a user will experience a particular outcome
- Change a protocol automatically
- Instruct a user to start, stop, increase, decrease, combine or substitute
- Turn age, weight, sex, goals or symptoms into a prescription

Personal context **may** be used only to explain **applicability** of evidence (e.g., study population differs), not to invent a dose.

---

## Required warning and uncertainty language

When evidence is limited, conflicting, non-human, or out-of-context, surfaces must include at least one applicable marker:

| Code | Meaning |
|---|---|
| `EVIDENCE_LIMITED` | Sparse human data |
| `EVIDENCE_CONFLICTING` | Material disagreement across sources |
| `EVIDENCE_NON_HUMAN` | Animal or in-vitro only |
| `EVIDENCE_CASE_REPORT` | Case report level |
| `POPULATION_MISMATCH` | User context differs from study population |
| `ROUTE_NOT_STUDIED` | Route not in reviewed set |
| `PARTIAL_PACKET` | Research incomplete |
| `OUTSIDE_REVIEWED_CONTEXT` | Entry materially outside reviewed ranges |

User-facing phrasing must remain non-prescriptive.

---

## Review gate

Reviewer-role approval levels (`automated_candidate`, `scientific_review`, `copy_review`, `clinical_safety_copy_review`, `legal_product_ratification`) were removed on 2026-10-24.

| Step | Who | Result |
|---|---|---|
| Review | Two-reviewer AI council (two independently configured models; `docs/operations/ai-council-review.md`) | Consensus passes |
| Disagreement | Escalated to the owner, Clinton Morgan, with distinct options | He decides |
| Legal question | Logged as a known risk (canon "Known risks") | Does not block |

Content that has not completed review is shown labeled `pending review`.

---

## Required evidence fields (minimum for promoted claims)

- Canonical subject identity (or explicit unresolved)
- Claim text
- Evidence class
- Source identifiers and locations
- Extraction provenance (workflow, versions, hashes)
- Review decision (AI council reviewers and outcome, or owner decision)
- Partial/conflict flags

High-impact fields (amounts, AE rates, contraindications) additionally require:

- Verbatim or offset-addressable source location
- Deterministic unit validation where applicable
- AI council review; disagreement escalates to the owner

---

## Copy-guard terms (non-exhaustive)

### Banned patterns (user-facing)

- “You should take …”
- “Start at …”
- “Increase to …”
- “Stop …”
- “The best dose for you …”
- “Safe for you …”
- “Recommended dose for your profile …”
- “AI recommends …”
- Uncited imperative treatment language

### Preferred patterns

- “Reviewed trials used …”
- “The entered amount is N times the reviewed initiation range …”
- “No reviewed trial in this evidence set …”
- “Evidence is limited / conflicting / non-human …”
- “Discuss with a qualified clinician …”
- “Applicability is uncertain because …”

Enforcement: DoctrineSanitizer / EvidenceGate banned-phrase scans + contract tests.

---

## Escalation rules

| Condition | Action |
|---|---|
| High-impact extraction without source location | Fail extraction; do not stage as valid |
| Class D language detected | Refuse or rewrite to Class A/B/C; log safety receipt |
| Outside reviewed context (large multiple) | Surface Class B flag + Class C “review before proceeding” template |
| Only FAERS / spontaneous signals | Never present as incidence or proven causation |
| Model-only statement without source | Not promotable; candidate triage only |

---

## Policy and consent impacts

- Does not expand medical-device claims.
- Does not change consent to allow personalized prescribing.
- Educational / harm-reduction framing must remain consistent with legal policies; open legal items are tracked as known risks.
- Hosted model fallbacks must not receive user health data merely because local GPU/Ollama failed.

---

## Examples and counterexamples

### Good

> Reviewed trials initiated participants between 0.5 and 1.0 mg weekly.  
> The recorded 12 mg amount is 12 to 24 times that initiation range.  
> No reviewed trial in this evidence set initiated participants at 12 mg.

### Bad

> You should take 0.5 mg.  
> 0.5 mg is safe for you.  
> 12 mg will harm you.  
> Based on your weight, start at 1 mg.

---

## Versioning

Any change that alters permitted/prohibited classes or public wording authority requires a new minor/major version and the owner's decision.

| Version | Notes |
|---|---|
| 1.0.0 | Initial contract from kickoff Phase 1 |
| 1.1.0 | 2026-10-24: Class D rewritten (never prescribes; showing found doses is Class A); online-reported doses added to Class A; approval levels replaced by AI council gate with owner escalation |

---

## Ratification checklist

- [x] Product canon updated or explicitly cross-referenced — `docs/canon/biostack-protocol-intelligence-canon.md` (2026-08-02, Clint Morgan)
- [x] Legal drafts reconciled — owner direction: no legal blockers (2026-08-02)
- [x] Governance manual update — contract is governing authority for Classes A–D (2026-08-02)
- [x] Human approval record stored — `docs/guidance/RATIFICATION.md` (fully ratified)
- [x] Copy-guard tests green — `GuidanceContentContractCopyGuardTests` / `DoctrineSanitizerTests`
- [x] Comparison service tests green — illustrative 12 mg vs 0.5–1.0 mg initiation case (harm-reduction pattern; not compound-exclusive)
