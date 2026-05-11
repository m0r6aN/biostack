# BioStack Implementation Plan — From Research to Reality

> **North Star:** BioStack becomes the **front-end operating system for human protocol intelligence**. Keon Systems becomes the **governance substrate**. Keon Collective becomes the **civilization-scale cognition layer**. Together: **Governed Protocol Intelligence** — every recommendation-like surface is evidence-scoped, policy-cleared, confidence-labeled, and audit-receipted.
>
> **First Law:** *Thoughts are free. Effects are governed.* (Keon Collective, Law of Separation)
>
> **Plan Author:** Clint + Claude
> **Plan Date:** 2026-05-10
> **Status:** Approved for execution

---

## Keon Integration Architecture

BioStack is the **cockpit**. Keon is the **flight control system**. The intelligence stack is four layers, top to bottom:

```
┌──────────────────────────────────────────────────────────────┐
│  4. BioStack Frontend Surfaces                               │
│     Mission Control · Stack Graph · Flight Recorder ·        │
│     Trust Ledger · Decision Theater · Branch Theater         │
├──────────────────────────────────────────────────────────────┤
│  3. Keon Governance Layer                                    │
│     Policy Gate · Evidence Gate · Decision Receipts ·        │
│     Governed Spine · PolicyHash · Witness Narratives ·       │
│     Reasoning Graph · Confidence Profile · Cognitive Heat    │
├──────────────────────────────────────────────────────────────┤
│  2. Keon Collective Cognition Plane                          │
│     Temporal Echo Planning · Bounded Refinement ·            │
│     Adversarial Challenge · Contradiction Injection ·        │
│     Role-Based Perspectives · Dream Offerings ·              │
│     Agent-Role Lifecycle · Gradient Authority                │
├──────────────────────────────────────────────────────────────┤
│  1. BioStack Data + Intelligence                             │
│     Compounds · Check-Ins · Protocols · Research Artifacts · │
│     Drift · Pattern Memory · Simulation · Interactions       │
└──────────────────────────────────────────────────────────────┘
```

**The architectural law (Cognition–Execution Separation):**
- Layers 1–2 may **think**, **branch**, **dream**, **simulate**, **contradict** freely. Nothing in these layers can cause effects.
- Layer 3 is the **Governed Execution Boundary**: every effect-bearing action (save a protocol, promote a claim, modify a record, export a packet, render a recommendation-like phrase) MUST traverse `MCP Gateway → Decide → Execute → Receipt`.
- Layer 4 may only render outputs that carry a valid Decision Receipt or are explicitly classified as commentary-only.

**Receipt Supremacy:** Decision Receipts outrank all interpretations. Narratives are derived only and cannot override receipts. The artifact hierarchy is fixed: `Decision Receipt → Evidence Pack → Execution Package → Attestation`.

---

## BioStack Intelligence Governance Doctrine

These eight rules are encoded into code (via `CC-1` Doctrine Guard, `CC-2` Safety Hierarchy lint, and `KE-*` Keon adapters) and into every PR review:

1. **The Collective may think freely.** Branches, contradictions, hypotheses, and dream offerings are unbounded inside the cognition plane.
2. **No Collective thought is effect-bearing by default.** Cannot modify protocols, publish claims, message users, or alter records without governance.
3. **Every user-facing insight must be classified** into one of: `educational | observational | comparative | administrative | provider-review | prohibited`.
4. **Every recommendation-like phrase must be policy-checked** before render. Allowed outcomes: `allowed | allowed-with-disclaimer | rewrite-required | blocked | escalate-to-provider-review`.
5. **Deterministic safety outranks cognitive commentary.** SRB surfaces enforce render order via `SafetyHierarchy` wrapper (CC-2).
6. **Evidence-backed claims outrank generated narratives.** The Trust Ledger (TL-1) is the source of truth for claim admissibility.
7. **Uncertainty must be visible.** Confidence Profile (model, epistemic, contradiction, evidence dimensions) is rendered as a first-class artifact on every governed surface.
8. **Every governed effect produces a Decision Receipt** persisted on the Governed Spine. Receipts are immutable, append-only, and inspectable by the user.

---

## How To Read This Plan

- Tickets are sized **S** (≤1 day), **M** (2–4 days), **L** (1–2 weeks), **XL** (multi-sprint).
- Each ticket lists: **Goal**, **Files**, **Data**, **Acceptance**, **Depends-On**.
- Phases run sequentially. Within a phase, tickets in the same "Track" run in parallel.
- Every phase ends with a **Stabilization Gate**: lint clean, tests green, manual smoke, design review.

---

## Pre-Flight (Sprint 0) — 1 Week

Goal: lock in the foundations so Phase 1 doesn't trip on its own shoelaces.

### PF-1 — Design Token Pass — S
- **Goal:** Codify the "controlled intelligence terminal" visual language as tokens.
- **Files:** `src/styles/tokens.ts` (new), `tailwind.config.*`, root CSS variables.
- **Data:** None.
- **Acceptance:**
  - Confidence levels (`high|moderate|low|insufficient|review-required`) have named colors + chip components.
  - State palette: `stable|drifting|shifted|unknown|insufficient-baseline`.
  - Evidence tier palette + regulatory boundary palette.
  - Typography scale: terminal density (small mono for telemetry, sans for prose).
  - Storybook (or `/dev/tokens` page) renders every token.
- **Depends-On:** —

### PF-2 — Shared Intelligence Primitives — M
- **Goal:** Build the atomic UI vocabulary every Phase 1 feature reuses.
- **Files:** `src/components/intel/` (new):
  - `ConfidenceChip.tsx`
  - `StatePill.tsx` (operating state)
  - `WhyDrawer.tsx` (inputs + reasoning + caveats)
  - `DeltaBadge.tsx` (+/- with reason)
  - `EvidenceTierBadge.tsx`
  - `RegulatoryBoundaryBadge.tsx`
  - `MetricLane.tsx` (timeline lane)
  - `SafetyHierarchy.tsx` (deterministic-first ordering wrapper)
- **Acceptance:** Each has props doc, default story, dark/light, a11y pass.
- **Depends-On:** PF-1.

### PF-3 — Frontend Data Layer Hygiene — S
- **Goal:** Centralize the API surface so new pages don't fetch ad-hoc.
- **Files:** `src/lib/api/` — confirm `apiClient` exports for: `getProtocolConsole`, `getCurrentStackIntelligence`, `getProtocolReview`, `getInteractionIntelligence`, `getKnowledgeEntry`, `getResearchSummary`.
- **Acceptance:** Single typed client; React Query hooks `useProtocolConsole(profileId)` etc. with stable cache keys.
- **Depends-On:** —

### PF-4 — Telemetry / Feature Flags — S
- **Goal:** Know what gets used; ship behind flags.
- **Files:** `src/lib/flags.ts`, `src/lib/telemetry.ts`.
- **Acceptance:** Flags `missionControl2`, `stackGraph`, `flightRecorder`, `trustLedger`, `decisionTheater`. Event emitter for surface views and "Why?" drawer opens.
- **Depends-On:** —

### PF-5 — IA Rename Pass — S
- **Goal:** Nav reflects the lifecycle model from the research doc.
- **Files:** `src/components/Navigation.tsx`, route `meta` titles.
- **Renames:**
  - Dashboard → **Mission Control** (`/protocol-console`)
  - Protocols → **Protocol Lab** (`/protocols`)
  - Check-ins → **Observations** (`/checkins`)
  - Knowledge Base → **Compound Intelligence** (`/knowledge`)
- **Acceptance:** Nav, breadcrumbs, page titles, sitemap updated. Old routes 301 to new where applicable.
- **Depends-On:** —

**Gate:** Tokens live, primitives in Storybook, nav renamed, flags wired. Ship.

---

## Phase 1 — Recompose Existing Data Into Signature Surfaces (Sprints 1–3, ~6 weeks)

> All Phase 1 work is **frontend recomposition over existing backend data**. No schema changes required.

### Track A — Mission Control 2.0 (P1)

#### MC-1 — Current Operating State Hero — M
- **Goal:** Replace the dashboard's stat-card grid with a single operating-state hero.
- **Files:**
  - `src/pages/protocol-console.tsx` (refactor)
  - `src/components/mission/OperatingStateHero.tsx` (new)
  - `src/lib/derive/operatingState.ts` (new)
- **Data:** `ProtocolConsolePayload.activeRun`, `latestClosedRun`, `latestReviewSummary`, `driftSnapshot.driftState`, `sequenceExpectationSnapshot.currentStatus`, `latestCheckInSignal`.
- **Acceptance:**
  - Resolves to exactly one of: `Running` / `Awaiting First Observation` / `Review Pending` / `Drift Accumulating` / `Stable Baseline` / `No Active Run`.
  - Each state has: headline, sub-state explainer, primary CTA, "Why?" drawer with inputs.
  - State changes when underlying data changes (verified with fixtures).
- **Depends-On:** PF-1, PF-2, PF-3.

#### MC-2 — Next Best Observation Card — M
- **Goal:** Tell the user exactly which logging action raises signal quality the most.
- **Files:**
  - `src/components/mission/NextObservationCard.tsx`
  - `src/lib/derive/observationDebt.ts`
- **Data:** `observationSignals`, `sequenceExpectationSnapshot`, recent `CheckIn` cadence, active `ProtocolRun` age.
- **Acceptance:**
  - Ranks candidates: missing first check-in > expected next event due > cadence gap > metric-missing-for-goal > review-not-completed.
  - Click-through wires to `/checkins/new` or relevant surface with context pre-filled.
- **Depends-On:** PF-2.

#### MC-3 — Protocol Weather — S
- **Goal:** One-glance regime state.
- **Files:** `src/components/mission/ProtocolWeather.tsx`.
- **Data:** `ProtocolDriftSnapshot` (state, signals, baseline source).
- **Acceptance:** Pill + 2–3 contributing factors + baseline confidence; "Why?" expands to drift signal list.
- **Depends-On:** PF-2.

#### MC-4 — Stack Clarity Meter — M
- **Goal:** A single "signal clarity" score with reasoning, replacing or augmenting raw `stackScore`.
- **Files:**
  - `src/components/mission/StackClarityMeter.tsx`
  - `src/lib/derive/signalClarity.ts`
- **Data:** `CurrentStackIntelligence.stackScore`, `interactionIntelligence` (redundancy/interference penalties), active compound count, overlap count, check-in cadence, run-attachment ratio.
- **Acceptance:**
  - Score 0–100 with named bands.
  - Surfaces top 2 limiters with deltas ("Redundancy penalty −12: 3 shared pathways across 4 compounds").
  - Action: "Open Counterfactual Lab" (deep link to graph variants).
- **Depends-On:** PF-2.

#### MC-5 — Cohesion Timeline Upgrade — M
- **Goal:** Make the existing cohesion timeline annotated and decision-relevant.
- **Files:** `src/components/mission/CohesionTimelinePanel.tsx` (refactor existing).
- **Data:** `ProtocolConsolePayload.cohesionTimeline`, pattern + drift + sequence snapshots.
- **Acceptance:**
  - Events tagged with `aligned|late|diverging|regime-shift|expected-pending`.
  - Hover shows the snapshot that produced the tag.
  - Empty state explains what would unlock annotations.
- **Depends-On:** PF-2, MC-3.

**Track A Gate:** `/protocol-console` is the default authed home, all five modules wired, "Why?" drawer present on every intelligent output.

---

### Track B — Stack Intelligence Graph + Counterfactual Lab (P2)

#### SG-1 — Stack Graph Visualization — L
- **Goal:** Compounds as nodes, interactions as edges, in a calm, inspectable graph.
- **Files:**
  - `src/components/protocol/StackGraph.tsx` (new)
  - `src/lib/graph/buildStackGraph.ts` (new — pure transform over `InteractionIntelligence`)
  - Library: `react-flow` or `cytoscape` (decide in SG-0 spike — favor `react-flow` for React idiom unless perf forces otherwise)
- **Data:** `InteractionIntelligence.interactions`, `topFindings`, `KnowledgeEntry.pathways`, `evidenceTier`.
- **Acceptance:**
  - Node ring color = evidence tier; node badge = regulatory boundary.
  - Edge color = `synergy|redundancy|interference`; thickness = confidence; dashed = inferred (not hint-backed).
  - Edge labels show shared pathway count; hover reveals pathway list.
  - Filters: type, confidence threshold, "show only concerns", "show synergies".
  - Click node → compound dossier drawer; click edge → reason + pathways + confidence + hint-backed flag.
  - Keyboard navigable; respects `prefers-reduced-motion`.
- **Depends-On:** PF-2.

#### SG-2 — Counterfactual Lab — M
- **Goal:** Side-panel that runs the existing `counterfactuals` and `swaps` over the current stack.
- **Files:**
  - `src/components/protocol/CounterfactualLab.tsx`
  - Reuses `StackGraph` with a "preview variant" overlay.
- **Data:** `InteractionIntelligence.counterfactuals`, `swaps`, recomputed `stackScore` per variant.
- **Acceptance:**
  - Lists: best removal, best swap, simplified variant, goal-aware variant.
  - Each shows score delta, redundancy delta, interference delta, synergy preservation.
  - Toggle to overlay variant on graph (removed nodes greyed; swapped nodes highlighted).
  - "Save as Protocol Draft" CTA (writes to existing protocol draft store).
  - **Non-prescriptive language enforced** — every variant carries "This is not a recommendation" footer.
- **Depends-On:** SG-1.

#### SG-3 — Embed Graph in Mission Control — S
- **Goal:** Mini-graph card on `/protocol-console` with click-through to full lab.
- **Files:** `src/components/mission/StackGraphMini.tsx`.
- **Acceptance:** Renders top 6 nodes by centrality; shows top finding count; click → `/protocols/[activeId]?tab=graph`.
- **Depends-On:** SG-1, MC-1.

**Track B Gate:** Demo-able "holy shit" moment. Record a 60s screen capture for stakeholder review.

---

### Track C — Protocol Flight Recorder v1 (P3)

#### FR-1 — Lane-Based Timeline — L
- **Goal:** Replace the existing `ProtocolComparison` view with a stacked-lane telemetry timeline.
- **Files:**
  - `src/components/protocol/FlightRecorder.tsx` (new)
  - `src/components/protocol/lanes/` (one component per lane)
  - `src/lib/derive/runTelemetry.ts`
- **Data:** `Protocol.simulation`, `Protocol.actualComparison`, `ProtocolRun`, `ProtocolRunObservation`, `CheckIn`, `TimelineEvent`, drift + sequence snapshots.
- **Lanes:**
  1. Protocol (start/complete/abandon/evolve)
  2. Compound (added/paused/stopped)
  3. Expected Signal (simulation bands: d1–3, d4–7, d7–14)
  4. Actual Telemetry (sleep, energy, appetite, recovery, focus, pain, side effects)
  5. Pattern / Drift (aligned, late, diverging, regime shift)
- **Acceptance:**
  - Horizontal scrub; click any event opens a side panel with expected vs actual + commentary.
  - Side-effect markers always visible (cannot be filtered out).
  - Empty-window callouts: "Expected signal window had no observation logged."
  - Annotations use neutral language: `observed`, `expected`, `aligned`, `divergent` — no causal claims.
- **Depends-On:** PF-2.

#### FR-2 — Flight Recorder Entry from Mission Control — S
- **Goal:** Easy jump from current run state into the full recorder.
- **Files:** Update `OperatingStateHero` and `CohesionTimelinePanel` with deep links.
- **Depends-On:** FR-1, MC-1.

**Track C Gate:** Open any completed run → recorder reads like F1 telemetry. Reviewed for tone/safety.

---

### Track D — Observation Debt Inbox (P4)

#### OD-1 — Inbox Surface — M
- **Goal:** Standalone "do this next" list, surfaced on Mission Control and `/checkins`.
- **Files:**
  - `src/components/mission/ObservationDebtInbox.tsx`
  - Reuses `src/lib/derive/observationDebt.ts` from MC-2.
- **Acceptance:**
  - Items: missing first check-in, expected next event pending, cadence gap, weak signal, attribution unclear, review pending, metric missing for active goal.
  - Each item has: reason, impact ("Unlocks earned 7-day review"), one-click resolve.
  - Dismiss-with-reason for non-applicable items (saved per profile, expires).
- **Depends-On:** MC-2.

**Track D Gate:** Inbox is empty for new users (encouraging) and populated for active users (useful).

---

### Phase 1 Closeout

- All flags ON for internal users; gradual rollout for external.
- Telemetry dashboard reviewed: surface views, "Why?" opens, counterfactual variant saves, inbox resolutions.
- Design review against `tone_and_formatting` standards: every score has a "so what?" line, confidence labels everywhere, no causal overreach.

---

## Phase 2 — Light Backend, High Leverage (Sprints 4–6, ~6 weeks)

> Each ticket here introduces a new API surface. Backend lives in the .NET project; contracts defined first, frontend follows.

### Track E — Compound Trust Ledger (P5)

#### TL-1 — Trust Ledger API — L
- **Goal:** Expose research-pipeline trust state via a public-safe endpoint.
- **Files (backend):**
  - `BioStack.Api/Controllers/KnowledgeController.cs` — add `GET /api/v1/knowledge/compounds/{slug}/trust-ledger`
  - `BioStack.Application/Knowledge/TrustLedger/` — query, projection, DTOs
  - `BioStack.Domain/Knowledge/TrustLedger.cs` — value type
- **Data:** `KnowledgeEntry` + `EvidencePacket` + `ResearchSummary` + `PromotionManifest` + `ReviewResolutionPlan`.
- **DTO fields:** `evidenceTier`, `completeness`, `needsReview`, `qualityFlags[]`, `regulatoryBoundary`, `claims[]` (with `confidence`, `sourceRefs`, `extractedQuote`, `reviewFlags`), `conflicts[]`, `promotionBlockers[]`, `requiredNextActions[]`.
- **Safety:** No internal-only fields leak (analyst notes, raw scrape, internal IDs). Add explicit allow-list on projection.
- **Acceptance:**
  - Endpoint returns 200 for promoted compounds, 404 for not-found, 451-style "review-gated" payload for blocked compounds (still shows boundary + flags, withholds claims).
  - Contract test pins the JSON shape.
  - Integration test against a real research fixture.
- **Depends-On:** —

#### TL-2 — Compound Dossier Page — L
- **Goal:** Frontend that lives up to the API.
- **Files:**
  - `src/pages/knowledge/[slug].tsx` (new — `/knowledge/[slug]` route)
  - `src/components/knowledge/CompoundDossier.tsx`
  - `src/components/knowledge/ClaimInspector.tsx`
  - `src/components/knowledge/RegulatoryBoundaryPanel.tsx`
  - `src/components/knowledge/TrustTimeline.tsx`
- **Acceptance:**
  - Sections: Identity, Trust State, Regulatory Boundary, Claim Inspector, Protocol Relevance, Research Ops.
  - Claim Inspector: click claim → drawer with source quote, confidence, review flags.
  - Quality flags rendered as a stack (not a string).
  - Empty / review-gated state is graceful: "This compound is review-required. Here is why."
- **Depends-On:** TL-1, PF-2.

#### TL-3 — Cross-link from Stack Graph / Mission Control — S
- **Goal:** Every compound name in the app becomes a click into the dossier.
- **Files:** Touch up `StackGraph`, `OperatingStateHero`, `CompoundIntelligenceCard`, `OverlapResults`.
- **Acceptance:** Compound name + node click + finding row opens dossier drawer or routes to page.
- **Depends-On:** TL-2.

---

### Track F — Stack Review Board Integration (P6 — Crown Jewel)

> Track F is BioStack's first visible Keon Collective surface. The SRB *is* the Collective's Role-Based Perspective Review applied to a protocol envelope. Every artifact here maps to a Keon canon primitive.

#### SRB-1 — Deliberation Envelope API — L
- **Goal:** Endpoint that produces a `StackDeliberationEnvelope` from a protocol or analyzer result, populated by Keon Collective primitives.
- **Files (backend):**
  - `BioStack.Api/Controllers/StackReviewController.cs` — `POST /api/v1/stack-review/envelope`
  - Wires existing `StackReviewBoardService` to Keon Collective via `KE-1` SDK
  - Generators: deterministic findings (from `InteractionIntelligence` + `KnowledgeEntry` + research flags); Collective envelope (Role-Based Perspective Review across Optimizer/Skeptic/Regulator/Historian).
- **Acceptance:**
  - Request body accepts either `protocolId` or an analyzer payload.
  - Response payload includes (in strict order):
    1. **Deterministic findings** (BioStack-native, fully inspectable)
    2. **Role-Based Perspective Review** — typed findings with severity, confidence, category per role
    3. **Active Contradiction Injection** — counter-position findings (non-executable)
    4. **Confidence Profile** — four dimensions: model, epistemic, contradiction density, evidence support + calibration version
    5. **Cognitive Heat** value with throttling status
    6. **Reasoning Graph** reference (typed DAG of claims/assumptions/risks/decisions/mitigations/contradictions)
    7. **Witness Narrative** (human-readable chronological log)
    8. **Decision Receipt** reference (commentary-only classification with PolicyHash)
  - Contract test enforces ordering, field presence, and that every finding carries `effect_status: "commentary-only"`.
  - **Doctrine Guard (CC-1):** every perspective output runs through non-executable sanitizer. No "you should", no dosing, no medical declarations.
  - Bounded Branch Refinement: envelope generation supports up to N (default 3) append-only refinement iterations; each iteration is lineage-keyed and preserves prior state.
- **Depends-On:** KE-1, KE-3.

#### SRB-2 — Decision Theater Surface — L
- **Goal:** Integrate the existing `StackReviewBoard` component into protocol review with Keon governance visible.
- **Files:**
  - `src/pages/protocols/[id]/review.tsx` (new)
  - Wire existing `src/components/.../StackReviewBoard.tsx` to SRB-1 API
  - `src/components/protocol/ChallengeStackPanel.tsx` (Adversarial Challenge + Contradiction Injection)
  - `src/components/protocol/ConfidenceProfileCard.tsx` (uses KE-10)
  - `src/components/protocol/ReasoningGraphViewer.tsx` (uses KE-8)
  - `src/components/protocol/WitnessNarrativePanel.tsx` (uses KE-7)
- **Acceptance:**
  - Safety hierarchy enforced (CC-2): deterministic findings render before all commentary.
  - Tab structure: Optimizer / Skeptic / Regulator / Historian — with expanded roles added in Phase 2.5 (Librarian / Cartographer / Watchman / Dreamer).
  - "Challenge This Stack" panel surfaces Adversarial Challenge + Contradiction Injection findings; every entry carries a non-executable badge.
  - Confidence Profile rendered with all four dimensions and calibration version stamp.
  - Cognitive Heat indicator visible; high heat collapses lower-confidence findings by default.
  - "Export Review Memo" produces a packet referencing the Decision Receipt URI (`keon://receipt/...`).
  - Completion writes a `TimelineEvent` of type `protocol-review-completed` *and* persists a Decision Receipt on the Governed Spine via KE-2.
- **Depends-On:** SRB-1, PF-2, KE-2, KE-7, KE-8, KE-10.

---

### Track G — Analyzer → Protocol Persistence (P7)

#### AP-1 — Authenticated Convert API — M
- **Goal:** Promote a local analyzer draft into a saved backend protocol.
- **Files (backend):**
  - `BioStack.Api/Controllers/ProtocolsController.cs` — `POST /api/v1/protocols/from-analyzer`
  - `BioStack.Application/Protocols/CreateFromAnalyzer/` — command, handler
- **Data:** Parsed entries, score, score explanation, issues, suggestions, decomposed blends, unknown compounds, counterfactuals, extraction warnings, parser warnings, low-confidence flag, extracted text preview, artifacts.
- **Acceptance:**
  - Preserves provenance (`source = "analyzer"`, parser confidence, warnings) on the saved protocol.
  - Returns the new `protocolId`.
  - Idempotent on `clientDraftId` (resubmitting doesn't duplicate).
- **Depends-On:** —

#### AP-2 — Frontend Conversion Flow — M
- **Goal:** "Save to BioStack" CTA on analyzer results.
- **Files:**
  - `src/components/analyzer/ConvertToProtocolDialog.tsx`
  - Update `ProtocolAnalyzerExperience` to show CTA when authed.
- **Acceptance:**
  - Anonymous users see "Sign up to save" with a single-click path.
  - Authed users get a profile picker + "Convert" button.
  - Post-convert: routes to `/protocols/[newId]` with a "Imported from analyzer" banner showing parser confidence + warnings.
- **Depends-On:** AP-1.

---

### Track H — Research Readiness Radar (P8)

#### RR-1 — Radar Surface — M
- **Goal:** Turn the existing admin research data into a visual command center.
- **Files:**
  - `src/pages/admin/research/index.tsx` (refactor)
  - `src/components/admin/ReadinessFunnel.tsx`
  - `src/components/admin/QualityFlagHeatmap.tsx`
  - `src/components/admin/ReviewResolutionQueue.tsx`
- **Data:** Existing `ResearchSummary`, `PromotionManifest`, `ReviewResolutionPlan` (no backend changes).
- **Acceptance:**
  - Funnel: draft → review-required → resolution-ready → promotable.
  - Heatmap: compounds × flag types, ordered by severity.
  - Queue: sortable by severity + age; row click → `/admin/research/compounds/[slug]`.
- **Depends-On:** PF-2.

#### RR-2 — Import Dry-Run Preview — S
- **Goal:** Visual confirmation of what a promotion run *would* do.
- **Files:** `src/components/admin/PromotionDryRunPanel.tsx`.
- **Acceptance:** Shows candidates, blocked, required actions, with one-click "Run Promotion" gated behind confirmation.
- **Depends-On:** RR-1, existing promotion endpoint.

---

### Track I — Provider Review Packet (P9)

#### PR-1 — Packet Export — M
- **Goal:** Exportable, non-prescriptive packet for taking to a provider.
- **Files:**
  - `src/lib/export/providerPacket.ts` (PDF generation via `@react-pdf/renderer` or server-side)
  - `src/components/protocol/ExportProviderPacket.tsx` (CTA + preview)
- **Content:** Protocol snapshot, active compounds with evidence tier + regulatory boundary, recent check-in trends (no graphs that imply causation), side-effect notes, "questions to discuss with provider", explicit "not medical advice" footer on every page.
- **Acceptance:**
  - PDF renders with consistent header/footer.
  - All claims labeled `observed` or `evidence-limited` — never imperative.
  - File name pattern: `biostack-protocol-{name}-{yyyy-mm-dd}.pdf`.
- **Depends-On:** TL-1 (to source compound evidence tier).

---

### Track J — Keon Foundations (P10 — Underlies Everything Else in Phase 2+)

> Build this first within Phase 2. Tracks E (Trust Ledger), F (SRB), G (Analyzer Persistence), H (Research Radar), and I (Provider Packet) all link into KE-* tickets. Without Keon plumbing, those tracks degrade to "BioStack with disclaimers" — with it, they become governed intelligence surfaces.

#### KE-1 — Keon Client SDK + Adapter — L
- **Goal:** First-class typed client for talking to Keon Runtime + Collective from BioStack.Api.
- **Files (backend):**
  - `BioStack.Infrastructure/Keon/IKeonClient.cs`
  - `BioStack.Infrastructure/Keon/KeonClient.cs` (HTTP + auth)
  - `BioStack.Infrastructure/Keon/Models/` — DTOs for `DecisionReceipt`, `PolicyHash`, `EvidencePack`, `ExecutionPackage`, `Attestation`, `WitnessNarrative`, `ReasoningGraph`, `ConfidenceProfile`, `CognitiveHeat`, `DeliberationEnvelope`
  - `BioStack.Application/Keon/Configuration.cs` — endpoints, key material, fail-closed defaults
- **Acceptance:**
  - Fail-closed default: any client error denies the action it gated.
  - Receipts persisted to local cache for offline verification (CAES Level 2).
  - Health check endpoint with degraded mode that **blocks** effect-bearing operations rather than degrading them (CAES invariant).
  - Integration tests with stub Keon server covering: success, policy-deny, signature-mismatch, network-failure (all paths fail-closed except success).
  - Telemetry on every call: latency, decision outcome, policy version, heat.
- **Depends-On:** —

#### KE-2 — Decision Receipts: Persistence + UI — M
- **Goal:** Every governed effect produces a verifiable, user-inspectable Decision Receipt on the Governed Spine.
- **Files (backend):**
  - `BioStack.Domain/Governance/DecisionReceipt.cs`
  - `BioStack.Infrastructure/Governance/SpineRepository.cs` (append-only)
  - `BioStack.Api/Controllers/ReceiptsController.cs` — `GET /api/v1/receipts/{uri}`, `GET /api/v1/receipts?subject={...}`
- **Files (frontend):**
  - `src/components/governance/ReceiptBadge.tsx` (small badge with URI + status)
  - `src/components/governance/ReceiptDrawer.tsx` (full receipt: actor, tenant, timestamp, input hash, PolicyHash, policy version, decision, evidence refs, allowed/blocked/escalated)
  - `src/pages/receipts/[uri].tsx` (deep-linkable receipt page)
- **Acceptance:**
  - Receipts immutable; spine appends only. Schema enforces no UPDATE/DELETE at the DB layer.
  - Every effect-bearing endpoint in BioStack (AP-1 analyzer convert, SRB-2 review completion, RR-2 promotion, PR-1 packet export, future evolve-protocol) attaches a Receipt URI to its response.
  - UI shows receipt badge on every governed surface; click reveals full receipt.
  - Test: attempting to mutate an existing spine entry throws `SpineImmutabilityViolation`.
- **Depends-On:** KE-1.

#### KE-3 — Policy Gate: Language Classification — L
- **Goal:** Before BioStack renders any "recommendation-like" sentence, classify and policy-check it.
- **Files (backend):**
  - `BioStack.Application/Governance/PolicyGate.cs`
  - `BioStack.Application/Governance/Classification.cs` — enum: `educational | observational | comparative | optimization | prescriptive | medical | prohibited`
  - `BioStack.Api/Controllers/PolicyGateController.cs` — `POST /api/v1/policy/classify`, `POST /api/v1/policy/check`
- **Files (frontend):**
  - `src/lib/governance/policyGate.ts` (client hook + cache)
  - `src/components/governance/GovernedSentence.tsx` (renders only after policy-pass; shows rewrite/block fallback)
- **Acceptance:**
  - Returns one of: `allowed | allowed-with-disclaimer | rewrite-required | blocked | escalate-to-provider-review`.
  - For `allowed-with-disclaimer`, returns required disclaimer text.
  - For `rewrite-required`, returns the rewritten sentence (or null + reason if rewrite not possible).
  - All Counterfactual Lab variant explanations (SG-2) and Operating State commentary (MC-1) pass through this gate before render.
  - Banned-phrase corpus + classifier test suite. Production must achieve 100% classification on the banned-phrase corpus.
  - Each gate call returns a PolicyHash; rendered output stores the hash for audit.
- **Depends-On:** KE-1.

#### KE-4 — Evidence Gate: Tier-Based Claim Visibility — M
- **Goal:** Enforce that claims appear only at UI tiers warranted by their evidence tier.
- **Files (backend):**
  - `BioStack.Application/Governance/EvidenceGate.cs`
  - Augments TL-1 trust ledger response with `visibilityTier`
- **Files (frontend):**
  - `src/components/governance/EvidenceGatedClaim.tsx`
- **Visibility rules (initial, tunable via policy):**
  - `strong | moderate` → user-facing summary OK
  - `limited` → visible with uncertainty framing
  - `unknown | anecdotal` → hidden from summary, available only in "evidence gaps" view
  - Safety-critical claim without authority-tier source → blocked or review-required
- **Acceptance:**
  - Compound dossier (TL-2) renders no claim that violates its tier rule.
  - Override requires explicit user opt-in to "show evidence gaps" view + a one-click receipt.
  - Test fixtures cover each tier × surface combination.
- **Depends-On:** KE-1, TL-1.

#### KE-5 — Claim Governance Badges — S
- **Goal:** Atomic UI vocabulary for governance state on every claim/insight.
- **Files:** `src/components/governance/ClaimBadgeStack.tsx`
- **Badges:** `Source-backed`, `Review required`, `Limited evidence`, `Regulatory boundary`, `Not executable`, `Provider review pressure elevated`, `Commentary only`, `Receipt-verified`.
- **Acceptance:** Used across Dossier, SRB, Mission Control, Counterfactual Lab. Stories cover every badge combination.
- **Depends-On:** PF-2.

#### KE-6 — Audit Receipt Feed — S
- **Goal:** Per-user feed of all Decision Receipts generated on their behalf.
- **Files:** `src/pages/governance/receipts.tsx`, reuses `ReceiptDrawer` from KE-2.
- **Acceptance:** Filterable by subject (protocol, compound, analyzer-conversion, review, export, promotion). Each row shows action, decision, policy version, timestamp.
- **Depends-On:** KE-2.

#### KE-7 — Witness Narrative Panel — M
- **Goal:** Human-readable chronological account of a Collective decision.
- **Files:** `src/components/governance/WitnessNarrativePanel.tsx`.
- **Data:** `WitnessNarrative` from KE-1 (per Keon canon: roles participated, what was proposed, which challenges landed, why surviving candidate selected).
- **Acceptance:** Rendered on SRB-2 review surface; collapsible by default; pinned by event type filter.
- **Depends-On:** KE-1, SRB-1.

#### KE-8 — Reasoning Graph Viewer — L
- **Goal:** Inspect the typed DAG that produced a decision.
- **Files:** `src/components/governance/ReasoningGraphViewer.tsx` (reuses graph library decision from SG-1).
- **Data:** `ReasoningGraph` from KE-1 with node kinds (`claim | assumption | risk | decision | mitigation | contradiction`) and edges (`supports | contradicts | depends_on | mitigates | derives_from`).
- **Acceptance:**
  - Reject artifacts without lineage to originating intent + branch (per Keon canon — lineage is mandatory).
  - Node click reveals supporting evidence pack refs.
  - Filter by node kind / edge type.
- **Depends-On:** SG-1, KE-1.

#### KE-9 — Cognitive Heat Indicator — S
- **Goal:** Surface the Collective's system-wide risk signal so operators see when cognition is strained.
- **Files:** `src/components/governance/CognitiveHeatGauge.tsx`.
- **Acceptance:**
  - Bands: nominal / elevated / high / critical.
  - High heat throttles branching aggressiveness in SRB / Branch Theater (per Keon canon).
  - Critical heat collapses non-essential perspectives and surfaces an explicit warning.
- **Depends-On:** KE-1.

#### KE-10 — Confidence Profile Renderer — M
- **Goal:** First-class four-dimensional confidence display.
- **Files:** `src/components/governance/ConfidenceProfileCard.tsx`.
- **Dimensions (per Keon canon):**
  - Model confidence (utility minus risk)
  - Epistemic uncertainty (gaps in claim refs and evidence anchors)
  - Contradiction density (active opposition pressure)
  - Evidence support (claim/evidence ratio)
- **Acceptance:**
  - Calibration version always visible.
  - Tooltip clarifies: "Confidence speaks. It does not command." (cannot override receipts/policy/heat).
  - Used on SRB-2, Counterfactual Lab variants, Branch Theater branches.
- **Depends-On:** PF-2, KE-1.

---

### Phase 2 Closeout

- All new endpoints have contract tests + integration tests.
- Frontend a11y pass on dossier + review surfaces.
- Safety review: no surface lets AI commentary precede deterministic safety. Confirm via test fixture for each.
- **Governance review:** every effect-bearing endpoint produces a receipt; receipts are immutable; spine append-only verified; fail-closed paths exercised; PolicyHash recorded for every gated render.
- **Receipt Supremacy audit:** any surface that mixes receipts with narrative passes a fixture proving receipt ordering precedence.

---

## Phase 2.5 — Dream State + Expanded Collective Roles (Sprint 6.5, ~3 weeks)

> Idle cognition that produces internal-only candidates. Nothing user-facing without governance promotion. Per Keon canon: *"Quiescence is not silence. The Collective dreams."*

### Track DR — Dream Offerings

#### DR-1 — Dream Run Scheduler — M
- **Goal:** Background process that submits non-effecting deliberation envelopes to Keon Collective during idle windows.
- **Files (backend):**
  - `BioStack.Infrastructure/Dreaming/DreamScheduler.cs`
  - `BioStack.Application/Dreaming/DreamRunCommand.cs`
- **Acceptance:**
  - Runs on cron + quiescence detection (no active user requests for N minutes).
  - Each dream run produces artifacts only — never effects.
  - All dream artifacts persist with `effect_status = "non-effecting"` and require human review to promote.
  - Honors Cognitive Heat: high heat suppresses dream runs.
- **Depends-On:** KE-1, KE-9.

#### DR-2 — Research Dreaming Queue — M
- **Goal:** Internal admin surface for Collective-generated research candidates.
- **Files:**
  - `src/pages/admin/research/dreams.tsx`
  - `src/components/admin/DreamFindingCard.tsx`
- **Candidate types:** evidence-gap maps, claim rewrite suggestions, misinformation watchlists, promotion blockers, source-canonicalization requests.
- **Acceptance:**
  - Each candidate shows: source role(s), reasoning graph ref, witness narrative, confidence profile.
  - Actions: `Promote to Research Task` (writes via TL-1 pipeline + Decision Receipt) / `Reject with reason` (logged) / `Defer`.
  - Dreams that decay past N days auto-archive.
- **Depends-On:** DR-1, KE-2, KE-7, KE-8.

#### DR-3 — Protocol Pattern Dreaming Queue — M
- **Goal:** Internal surface for Collective-generated protocol pattern candidates from longitudinal data.
- **Files:** `src/pages/admin/dreams/patterns.tsx`.
- **Candidate types:** recurring drift signatures, common observation gaps, unclear-attribution patterns, stack shapes producing noisy signals.
- **Acceptance:** Same governance shape as DR-2. Promotion path goes into Phase 3 LP-1 longitudinal engine.
- **Depends-On:** DR-1.

#### DR-4 — UX Dreaming Queue — S
- **Goal:** Internal product-team surface for Collective UX candidates.
- **Files:** `src/pages/admin/dreams/ux.tsx`.
- **Acceptance:** Product team review only; promotion is human-only.
- **Depends-On:** DR-1.

### Track RX — Expanded Collective Roles

> SRB Phase 1 ships with the canonical four roles (Optimizer / Skeptic / Regulator / Historian). Phase 2.5 expands to the BioStack-native civic roster.

#### RX-1 — Research Librarian Role — M
- **Goal:** Collective role focused on source refs, evidence packets, claim confidence, extracted quotes, review flags.
- **Allowed output:** claim provenance, source-linked evidence summaries, missing-source notices.
- **Files:** `BioStack.Application/Collective/Roles/ResearchLibrarian.cs` (BioStack-side adapter that submits role-typed envelope to Keon Collective).
- **Acceptance:** SRB-2 gains a Librarian tab; outputs always carry source URIs.
- **Depends-On:** SRB-1, TL-1.

#### RX-2 — Cartographer Role — M
- **Goal:** Collective role focused on pathway maps, compound relationship graphs, interaction topology, stack shape.
- **Allowed output:** graph structures, relationship maps, overlap clusters.
- **Files:** `BioStack.Application/Collective/Roles/Cartographer.cs`.
- **Acceptance:** Cartographer findings feed the Stack Graph (SG-1) as a deliberation overlay (showing what the role *would highlight* for the user).
- **Depends-On:** SRB-1, SG-1.

#### RX-3 — Watchman Role — M
- **Goal:** Collective role focused on repeated side effects, concerning symptom patterns, drift, unsafe language, provider-review triggers.
- **Allowed output:** non-diagnostic caution flags, provider-review prompts, observation requests.
- **Forbidden output:** any diagnosis, any "X is unsafe" declaration without policy backing.
- **Files:** `BioStack.Application/Collective/Roles/Watchman.cs`.
- **Acceptance:** Watchman findings always render with `Provider review pressure` badge + receipt URI.
- **Depends-On:** SRB-1, KE-3.

#### RX-4 — Dreamer Role — S
- **Goal:** Collective role that synthesizes novel candidate insights, frontier patterns, speculative branches.
- **Allowed output:** internal-only concepts.
- **Acceptance:** Dreamer outputs cannot bypass DR-1 scheduler — they live in the dream queues only, never on user surfaces.
- **Depends-On:** DR-1.

---

## Phase 3 — Category Domination (Sprints 7+, XL)

> Long-arc work. Each item is its own multi-sprint initiative. Plan separately when reached.

### Track BT — Branch Theater (Temporal Echo for BioStack) — XL

> User-facing application of Keon Collective's Temporal Echo Planning, Adversarial Challenge, and Active Contradiction Injection. The Collective branches the future for a given protocol decision; only branches that survive challenge become user-visible options.

#### BT-1 — Temporal Echo Pipeline for Protocols — XL
- **Goal:** Submit a protocol decision intent to Keon Collective; receive scored, challenged, governed branches back.
- **Branch types (initial):** `continue-observing | track-longer | simplify-for-clarity | review-evidence-gaps | prepare-provider-packet | evolve-draft`.
- **Files (backend):**
  - `BioStack.Application/Branches/BranchExplorationCommand.cs`
  - `BioStack.Api/Controllers/BranchesController.cs` — `POST /api/v1/protocols/{id}/branches`
- **Acceptance:**
  - Each branch carries: rationale, Confidence Profile, missing inputs, risks, allowed-next-action, prohibited-action, Decision Receipt URI, Reasoning Graph ref, Witness Narrative.
  - Branches that fail Adversarial Challenge are excluded from the response (Cognition–Execution Separation: dead branches die in the cognition plane).
  - Bounded Branch Refinement: branches may iterate up to N times before collapse; iterations are append-only.
  - Critical heat short-circuits the pipeline (per Keon canon).
- **Depends-On:** KE-1, KE-2, KE-3, SRB-1.

#### BT-2 — Branch Theater Frontend — L
- **Goal:** User surface for inspecting and acting on governed branches.
- **Files:**
  - `src/pages/protocols/[id]/branches.tsx`
  - `src/components/protocol/BranchCard.tsx`
  - `src/components/protocol/ContradictionPanel.tsx`
- **Acceptance:**
  - Branches presented side-by-side with Confidence Profile + Reasoning Graph link + receipt URI.
  - Each branch shows the strongest counter-position (Active Contradiction Injection) so users see what the system argued against itself.
  - Allowed actions per branch (e.g., "Create evolved draft", "Prepare provider packet") route through KE-2 receipt-producing endpoints.
  - **Forbidden:** auto-executing protocol changes. Every effect requires explicit user confirmation + receipt.
- **Depends-On:** BT-1, KE-10.

### LP-1 — Longitudinal Protocol Intelligence Engine — XL
- Multi-run pattern learning, goal-specific outcome clustering, protocol-family comparison.
- Needs: persisted derived metrics, background job for cross-run aggregation, schema for longitudinal indices.
- Predecessor data: at least 3 completed runs per active profile.

### LP-2 — Research-to-Action Workbench — XL
- New knowledge entry promotion → identify affected compounds + protocols → notify users → review/evolve workflow.
- Needs: event bus from research pipeline, in-app notifications, "knowledge update may affect this protocol" banner with diff.

### LP-3 — Evidence Graph — XL
- Claims, sources, compounds, protocols as graph nodes; conflicts as edges; confidence visualized.
- Heaviest frontend lift in the plan; reuse SG-1 patterns.

### LP-4 — Cohort / Comparative Intelligence — XL
- Opt-in, privacy-safe, anonymized pattern baselines.
- Requires: governance doc, opt-in flow, anonymization service, legal review.
- **Do not start until LP-1 is stable.**

### LP-5 — Constrained Protocol Copilot — XL
- Natural language Q&A bounded to BioStack's structured data — no free-form hallucination.
- Tooling: structured retrieval over `KnowledgeEntry` + protocols + runs + check-ins; refusal pattern for out-of-scope.

---

## Cross-Cutting Tracks (Run Continuously)

### CC-1 — Doctrine Guard
- **Goal:** Automated check that every user-facing AI/derived output passes the non-executable doctrine.
- **Files:** `src/lib/doctrine/sanitize.ts`, unit tests with banned-phrase corpus.
- **Acceptance:** Imperative medical phrases, dosage prescriptions, and treatment recommendations are rejected at component-render time in dev; production renders fall back to "review required" copy.

### CC-2 — Safety Hierarchy Lint
- **Goal:** ESLint rule (or test) enforcing that `SafetyHierarchy` wraps any surface mixing deterministic + commentary content.
- **Files:** `tools/eslint/safety-hierarchy.ts`.
- **Acceptance:** CI fails if a commentary component renders ahead of a deterministic findings component in the same parent.

### CC-3 — Confidence-as-First-Class Audit
- **Goal:** Visual regression test catches missing confidence labels on scored outputs.
- **Acceptance:** Snapshot tests for `StackClarityMeter`, `OperatingStateHero`, `CompoundDossier`, `FlightRecorder` insights all include confidence chip.

### CC-4 — Telemetry Review (Weekly)
- Review usage data. Kill modules with <10% surface utilization after 30 days. Refactor or remove.

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Graph library perf at scale (>30 compounds) | M | M | Spike SG-0 with both `react-flow` and `cytoscape`; cap visible nodes; lazy-render edges. |
| Trust Ledger leaks internal research notes | L | H | Allow-list projection on backend; contract test; explicit security review of TL-1 PR. |
| SRB API generates implicit medical advice | M | H | Doctrine Guard sanitizer (CC-1) + Policy Gate (KE-3); fixture-based output review; legal review before flag-on. |
| Phase 1 frontend bloat (too many cards) | M | M | Enforce "every metric must answer 'so what?'" rule in PR review; design lead sign-off. |
| Counterfactual variants over-interpreted by users | M | M | Mandatory "not a recommendation" footer; non-prescriptive language lint; routed through KE-3 Policy Gate before render. |
| Knowledge updates regress existing protocols silently | M | M | LP-2's whole purpose; until then, add a "knowledge version" stamp on saved protocols. |
| Keon Runtime unavailable in production | L | H | Fail-closed default (KE-1): degraded Keon blocks effect-bearing operations entirely. UI shows "governance offline — read-only" banner. Receipts can never be forged or skipped. |
| Decision Receipt forgery or tampering | L | H | Receipts are cryptographically signed; PolicyHash verification on every read; Governed Spine append-only at DB level. Add `SpineImmutabilityTest` to CI. |
| Dream candidates leak into user-facing surfaces | L | H | DR-1 hard-enforces `effect_status="non-effecting"` on every dream artifact. Dreamer role (RX-4) cannot bypass DR-1 scheduler. Audit test verifies no dream artifact reaches a user route. |
| Confidence Profile misread as authority | M | M | Tooltip on every render: "Confidence speaks. It does not command." (per Keon canon). Receipts always render above confidence in the safety hierarchy. |
| Cognitive Heat ignored under load | M | M | High heat throttles branching in SRB and BT-1; critical heat collapses non-essential surfaces. Heat indicator (KE-9) always visible on governed pages. |
| User assumes BioStack-rendered branch is a recommendation | M | H | BT-2 forbids auto-execution; every branch requires explicit user confirmation + Decision Receipt before any effect. Branch cards carry `Commentary only` badge until user opts an effect-bearing action. |

---

## Definition of Done (Per Ticket)

A ticket is **done** when:

1. Acceptance criteria all green (manual + automated).
2. No new TypeScript errors; lint clean.
3. Unit tests added/updated; integration test for any new API.
4. A11y: keyboard nav, focus order, contrast.
5. Telemetry events emitted (where surface-relevant).
6. Feature flag wired; default off in prod.
7. Design review for visible surfaces.
8. Safety review for any new AI/derived output.
9. PR linked to plan ticket ID in title (e.g., `[MC-1] Operating State Hero`).
10. Demo recorded for stakeholder-visible work.

---

## Sequencing Summary

```
Sprint 0    : Pre-Flight (PF-1..PF-5)
Sprint 1    : MC-1, MC-2, MC-3, OD-1 (start), SG-1 (spike+start)
Sprint 2    : MC-4, MC-5, SG-1 (finish), SG-2, FR-1 (start)
Sprint 3    : SG-3, FR-1 (finish), FR-2, OD-1 (finish), Phase 1 Closeout
Sprint 4    : KE-1, KE-2, KE-3, TL-1, AP-1, RR-1                  ← Keon plumbing lands first
Sprint 5    : KE-4, KE-5, KE-6, KE-9, KE-10, TL-2, TL-3, AP-2, SRB-1
Sprint 6    : KE-7, KE-8, SRB-2, RR-2, PR-1, Phase 2 Closeout
Sprint 6.5  : DR-1..DR-4, RX-1..RX-4 (Dream State + Expanded Roles)
Sprint 7+   : BT-1, BT-2 (Branch Theater), then LP-1..LP-5 (re-plan per initiative)
```

**Dependency law:** No ticket in Phase 2+ that produces an effect ships before KE-1 + KE-2 are green. If Keon plumbing slips, effect-bearing Phase 2 tickets slip with it. Commentary-only surfaces (most of SRB-2's read paths, the dossier read view) can ship without KE-2 but must be re-routed through it before going to external users.

---

## The Bet

If we ship Phase 1 in 6 weeks, Phase 2 + Keon Foundations in another 6, Phase 2.5 in 3, and Branch Theater begins by sprint 7, then by 2026-08-30 BioStack stops looking like a tracker and starts demonstrating Keon's thesis in a visceral, public way.

**The strategic move:** Keon Systems is abstract infrastructure until users see it governing something real. BioStack makes Keon **felt**:

- Thoughts become branches (Temporal Echo)
- Branches become reviewed options (Adversarial Challenge + Contradiction Injection)
- Reviewed options become governed surfaces (Policy Gate + Evidence Gate)
- Governed surfaces produce receipts (Decision Receipts on the Governed Spine)
- Receipts create trust

**The category:**

- BioStack is the **cockpit**.
- Keon Collective is the **civilization-scale mind**.
- Keon Runtime is the **law**.

Together:

> A digital civilization thinks freely in the dream state, explores temporal branches, challenges itself through contradiction, and only lets governed effects touch reality.

That is not a supplement app. That is governed protocol intelligence — a civilization thinking beside the user, without pretending to be their doctor.

**Thoughts are free. Effects are governed.**

This is the way.
