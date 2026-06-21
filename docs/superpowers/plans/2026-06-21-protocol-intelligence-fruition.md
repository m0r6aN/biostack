# Protocol Intelligence Fruition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Convert the Protocol Intelligence canon and PR #112/#114 artifacts into a validated, enforceable, user-facing, and monetizable BioStack implementation.

**Architecture:** Use the merged PR docs and JSON artifacts as product contracts, then bind them to existing KnowledgeWorker, promotion-review, EvidenceGate, Knowledge API, protocol/analyzer, and billing surfaces. Runtime remains source-first and review-gated: reviewed artifacts may power user-facing guidance; missing citations, unreviewed claims, unsafe outputs, or licensing uncertainty fail closed.

**Tech Stack:** .NET 10/xUnit backend, BioStack KnowledgeWorker/Application/Api projects, Next.js 16/React 19/Vitest frontend, JSON contract artifacts under research/protocol-intelligence, existing Stripe tier model: Observer, Operator, Commander.

---

## Evidence Reviewed

- PR #112, "Define evidence-guided protocol intelligence architecture", merged. It added architecture/product/safety docs, model/data roadmap, source-registry proposal, evaluation-harness plan, research memo, and KnowledgeEngineDocumentationTests.
- PR #114, "Add protocol intelligence canon artifacts", merged. It added canon docs and JSON artifacts for phases, relationships, side-effect ambiguity, source quality, GLP-1 observability, high-risk guardrails, promotion targets, and MVP backlog.
- PR #112 references #114 explicitly as the separated delivery for canon, artifact index, MVP backlog, and structured JSON artifacts.
- origin/main includes both PRs together. Current local checkout is still on docs/protocol-intelligence-canon-artifacts at PR #114 head, so implementation should start from a fresh worktree or branch off origin/main.
- Focused checks against origin/main found:
  - research/output/2026-06-18-biostack-knowledge-engine-market-research.md is still missing, but docs/artifacts cite it.
  - All research/protocol-intelligence/*.json files parse.
  - promotion-target-specs.json still uses snake_case runtime requirement tokens while runtime schemas use camelCase.
  - promotion-target-specs.json blocked-output IDs do not align with high-risk-guardrails.json.
  - side_effect_ambiguity_artifact fields in promotion-target-specs.json do not match side-effect-ambiguity-detector.json.
  - Most relationship taxonomy types still omit evidenceTier and/or sourceRefs.
  - source-quality-taxonomy.json still uses free-text blocked-output phrases instead of normalized identifiers.
  - Existing doc tests validate presence/posture, not cross-artifact consistency or runtime enforcement.

## Agent Allocation

Use four independent lanes with explicit merge order:

1. Contract Agent: repair docs/JSON contracts and add contract validation tests.
2. Backend Agent: implement typed loaders, promotion gates, guardrail scanning, and runtime query services.
3. Frontend Agent: implement Protocol Intelligence IA and tiered UX using backend contracts.
4. Commercial/Infra Agent: wire entitlements, telemetry, eval jobs, source-refresh operations, and paid packaging.

Merge order: Contract Agent first, Backend Agent second, Frontend and Commercial/Infra in parallel after backend response contracts stabilize.

## Task 0: Start From a Clean Main Worktree

**Files:**
- Read: docs/superpowers/plans/2026-06-21-protocol-intelligence-fruition.md

- [ ] **Step 1: Create an isolated worktree from current main**

Run:

    git fetch origin main
    git worktree add ../BioStack-protocol-intelligence-fruition origin/main
    Set-Location ../BioStack-protocol-intelligence-fruition
    git switch -c feature/protocol-intelligence-fruition

Expected:

    HEAD starts from origin/main and includes both PR #112 and PR #114.
    git status --short --branch shows no local modifications except later task work.

- [ ] **Step 2: Prove the starting state**

Run:

    git status --short --branch
    git ls-tree -r --name-only HEAD | rg "protocol-intelligence|KnowledgeEngineDocumentationTests|source-registry"

Expected:

    The protocol-intelligence docs, JSON artifacts, safety guardrails, and KnowledgeEngineDocumentationTests are present.

## Task 1: Contract Agent - Repair and Validate Protocol Intelligence Contracts

**Files:**
- Modify: docs/canon/biostack-protocol-intelligence-canon.md
- Modify: docs/knowledge-engine/protocol-intelligence-artifact-index.md
- Modify: docs/knowledge-engine/protocol-intelligence-mvp-backlog.md
- Modify: docs/knowledge-engine/protocol-intelligence-safety-guardrails.md
- Modify: research/protocol-intelligence/promotion-target-specs.json
- Modify: research/protocol-intelligence/relationship-taxonomy.json
- Modify: research/protocol-intelligence/source-quality-taxonomy.json
- Modify: research/protocol-intelligence/high-risk-guardrails.json
- Modify: research/protocol-intelligence/side-effect-ambiguity-detector.json
- Create: backend/tests/BioStack.KnowledgeWorker.Tests/ProtocolIntelligenceContractTests.cs

- [ ] **Step 1: Replace broken source references**

Change every Source research reference and every JSON sourceResearch pointing at research/output/2026-06-18-biostack-knowledge-engine-market-research.md to an existing durable source path:

    research/protocol-intelligence/biostack-ai-model-data-asset-research-memo.md

If the original market-research memo is required as a separate source artifact, create it in research/protocol-intelligence/ and cite that exact path consistently. Do not leave any citation pointing at research/output/ unless the file exists in the repo.

- [ ] **Step 2: Normalize global requirement keys to runtime field names**

In research/protocol-intelligence/promotion-target-specs.json, replace the runtimeVisibilityRequires array with:

    structuredArtifact
    sourceRefs
    evidenceTier
    confidence
    reviewStatusApproved
    forbiddenOutputScanPassed

- [ ] **Step 3: Canonicalize blocked-output IDs**

Use high-risk-guardrails.json globalBlockedOutputs as the canonical blocked-output vocabulary. Replace mismatched IDs in promotion-target-specs.json with:

    clinical_dosing_instructions
    injection_instructions
    sarm_cycle_design
    serm_recovery_protocols
    post_cycle_therapy_instructions
    sourcing_guidance
    claims_investigational_peptides_safe_or_effective
    claims_community_anecdotes_prove_efficacy
    high_risk_protocol_builder_flows
    recommend_start_stop_taper_combine_or_escalate

Then convert source-quality-taxonomy.json sourceClasses blockedOutputs from free text to the same normalized ID vocabulary. Preserve user-facing labels by adding blockedOutputDescriptions if descriptions are needed.

- [ ] **Step 4: Align side-effect artifact fields**

In promotion-target-specs.json, set side_effect_ambiguity_artifact requiredFields to exactly:

    symptomOrOutcome
    onsetWindow
    recentChanges
    phaseContext
    overlapDomains
    sourceQualityFlags
    highRiskCategoryFlags
    evidenceTier
    confidence
    userFacingBoundary
    reviewStatus

- [ ] **Step 5: Require evidence and citations on every relationship type**

In relationship-taxonomy.json, ensure every relationshipTypes item includes these requiredFields:

    evidenceTier
    confidence
    sourceRefs
    reviewStatus
    userFacingExplanation

Do not remove relationship-specific fields such as biomarker, sourceClass, fdaStatus, or domainsToReassess.

- [ ] **Step 6: Add contract tests**

Create backend/tests/BioStack.KnowledgeWorker.Tests/ProtocolIntelligenceContractTests.cs with tests that prove:

    every JSON sourceResearch path exists
    promotion targets use sourceRefs, evidenceTier, and reviewStatusApproved, not source_refs or evidence_tier
    promotion blockedOutputs are members of high-risk globalBlockedOutputs
    side-effect promotion target fields exactly match side-effect detector requiredArtifactFields
    every relationship type requires evidenceTier, confidence, sourceRefs, reviewStatus, and userFacingExplanation
    source-quality blockedOutputs are normalized identifiers without spaces or hyphens

- [ ] **Step 7: Run contract validation**

Run:

    dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter ProtocolIntelligenceContractTests --no-restore --nologo --verbosity minimal

Expected:

    ProtocolIntelligenceContractTests pass.

## Task 2: Backend Agent - Typed Runtime Contracts and Guardrail Enforcement

**Files:**
- Create: backend/src/BioStack.Application/ProtocolIntelligence/ProtocolIntelligenceContracts.cs
- Create: backend/src/BioStack.Application/ProtocolIntelligence/ProtocolIntelligenceArtifactLoader.cs
- Create: backend/src/BioStack.Application/ProtocolIntelligence/ForbiddenOutputScanner.cs
- Create: backend/src/BioStack.Application/ProtocolIntelligence/ProtocolIntelligenceGate.cs
- Modify: backend/src/BioStack.Application/BioStack.Application.csproj
- Modify: backend/src/BioStack.Application/Services/EvidenceGate.cs
- Create: backend/tests/BioStack.Application.Tests/Services/ProtocolIntelligenceGateTests.cs

- [ ] **Step 1: Define typed contract records**

Define records for ProtocolIntelligenceArtifactSet, PromotionGateRequest, and PromotionGateResult. PromotionGateResult must return CanPromote, BlockingReasons, RequiredFieldsMissing, ForbiddenOutputMatches, and RequiresHumanReview.

- [ ] **Step 2: Load JSON artifacts deterministically**

ProtocolIntelligenceArtifactLoader should read artifacts from research/protocol-intelligence relative to repository root or an injected base path, deserialize them with System.Text.Json, and fail closed if any required artifact is missing or malformed.

- [ ] **Step 3: Implement forbidden-output scanner**

ForbiddenOutputScanner should load canonical blocked-output IDs from high-risk-guardrails.json globalBlockedOutputs plus category-level blockedOutputs. It should map each ID to deterministic phrase patterns from protocol-intelligence-safety-guardrails.md and block exact unsafe phrases such as:

    you should start
    you should stop
    take
    inject
    run this cycle
    post-cycle therapy
    best source
    safe and effective
    proven by user reports

It may reuse DoctrineSanitizer, but must return matched rule IDs, not just true/false.

- [ ] **Step 4: Implement ProtocolIntelligenceGate**

The gate should:

    1. Look up the promotion target by ArtifactType.
    2. Require every target.requiredFields key.
    3. Require reviewStatus == approved for runtime visibility.
    4. Require sourceRefs and evidenceTier for evidence-bearing artifacts.
    5. Require human review for high-risk, regulatory, safety, prescription, hormone-axis, GLP-1, SARM, SERM, peptide, source-quality, adverse-effect, and contradiction claims.
    6. Run ForbiddenOutputScanner over user-facing explanation, summary, rationale, and boundary text.
    7. Return CanPromote=false with explicit BlockingReasons on any miss.

- [ ] **Step 5: Bridge existing EvidenceGate**

Extend EvidenceGate so transcript promotion candidates can call ProtocolIntelligenceGate when SourceMetadata artifactType or TargetCanonicalName maps to a protocol-intelligence target. Preserve existing fail-closed behavior for current transcript candidates.

- [ ] **Step 6: Add backend tests**

Add tests proving:

    missing sourceRefs blocks relationship_artifact
    missing userFacingExplanation blocks relationship_artifact
    unapproved reviewStatus blocks runtime visibility
    side_effect_ambiguity_artifact accepts symptomOrOutcome and userFacingBoundary, not symptom and boundaryText
    forbidden output text blocks promotion and returns the matched rule ID
    high-risk source-quality claims require human review
    Unknown is returned when no reviewed artifact exists

- [ ] **Step 7: Run backend verification**

Run:

    dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolIntelligenceGateTests --no-restore --nologo --verbosity minimal
    dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter "ProtocolIntelligenceContractTests|KnowledgeEngineDocumentationTests" --no-restore --nologo --verbosity minimal

Expected:

    All focused Protocol Intelligence contract and gate tests pass.

## Task 3: Backend Agent - Runtime API and Processing Pipeline

**Files:**
- Create: backend/src/BioStack.Contracts/Responses/ProtocolIntelligenceResponse.cs
- Create: backend/src/BioStack.Application/Services/ProtocolIntelligenceService.cs
- Modify: backend/src/BioStack.Api/Endpoints/KnowledgeEndpoints.cs
- Modify: backend/src/BioStack.Api/Endpoints/ProtocolEndpoints.cs
- Create: backend/tests/BioStack.Api.Tests/Integration/ProtocolIntelligenceEndpointsIntegrationTests.cs

- [ ] **Step 1: Add response contracts**

Create ProtocolIntelligenceResponse with Status, PhaseMap, Relationships, AmbiguitySignals, SourceQualityWarnings, HighRiskWarnings, Unknowns, SafetyNotes, and UpgradeHooks. Use Status = Unknown when reviewed artifacts are absent.

- [ ] **Step 2: Add service behavior**

ProtocolIntelligenceService should combine user protocol events, phases, compound records, reviewed relationship artifacts, side-effect/check-in fields, source-quality classifications, high-risk category flags, and billing tier. It must not synthesize new claims at request time. It should return reviewed relationships or Unknown, with sourceRefs and safety notes attached.

- [ ] **Step 3: Add endpoints**

Add:

    GET /api/v1/protocols/{protocolId}/intelligence
    POST /api/v1/protocols/{protocolId}/intelligence/preview
    GET /api/v1/knowledge/protocol-intelligence/contracts

The contracts endpoint should expose artifact versions, supported relationship IDs, blocked-output IDs, and available observability modules. Do not expose restricted source text.

- [ ] **Step 4: Gate by tier**

Use existing billing feature/tier behavior:

    Observer: contract metadata, limited source-quality warnings, Unknown states, upgrade hooks.
    Operator: phase map, reviewed relationship cards, source-quality tracker, GLP-1 observability basics.
    Commander: side-effect ambiguity, longitudinal protocol intelligence review, report-generation hooks, deeper correlation panels.

Safety warnings and high-risk guardrails are never hidden behind payment.

- [ ] **Step 5: Add integration tests**

Prove:

    Observer receives upgrade hooks and no Commander-only ambiguity panel.
    Operator receives reviewed relationship/source-quality outputs.
    Commander receives ambiguity and longitudinal review payloads.
    Unreviewed artifacts never appear in user-facing output.
    Forbidden phrases are absent from every response string field.
    No reviewed artifact returns Status=Unknown instead of inferred content.

- [ ] **Step 6: Run API verification**

Run:

    dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --filter ProtocolIntelligenceEndpointsIntegrationTests --no-restore --nologo --verbosity minimal

Expected:

    Protocol Intelligence endpoints pass focused integration tests.

## Task 4: Frontend Agent - Protocol Intelligence IA and UX

**Files:**
- Modify: frontend/src/lib/types.ts
- Modify: frontend/src/lib/api.ts
- Create: frontend/src/components/protocols/ProtocolIntelligencePanel.tsx
- Create: frontend/src/components/protocols/PhaseMapPanel.tsx
- Create: frontend/src/components/protocols/SourceQualityPanel.tsx
- Create: frontend/src/components/protocols/SideEffectAmbiguityPanel.tsx
- Create: frontend/src/components/protocols/HighRiskWarningPanel.tsx
- Modify: frontend/src/components/protocols/ProtocolIntelligenceReview.tsx
- Modify: frontend/src/app/protocols/[id]/page.tsx
- Modify: frontend/src/app/pricing/page.tsx
- Create: frontend/src/__tests__/components/protocols/ProtocolIntelligencePanel.test.tsx

- [ ] **Step 1: Add client types and API method**

Add ProtocolIntelligenceResponse and nested response types in frontend/src/lib/types.ts. Add getProtocolIntelligence(protocolId) to frontend/src/lib/api.ts and call /api/v1/protocols/{protocolId}/intelligence.

- [ ] **Step 2: Build IA around five panels**

Render panels in this order:

    1. Phase map: what phase is this event in?
    2. Evidence relationships: what reviewed relationships exist?
    3. Side-effect ambiguity: what changed before this outcome?
    4. Source quality: what is uncertain about identity, label, source, or regulatory status?
    5. High-risk warnings: what must stay warning-first or blocked?

Each panel must show evidence tier, confidence, sourceRefs count, review status, and user-facing boundary text. If a section has no reviewed artifact, show an Unknown state instead of empty confidence.

- [ ] **Step 3: Monetize with tier-specific upgrade hooks**

Use copy:

    Observer: Unlock reviewed protocol relationships and source-quality context with Operator.
    Operator: Unlock side-effect ambiguity and longitudinal Protocol Intelligence with Commander.
    Commander: Included in Commander.

Never imply Commander provides medical advice. It provides deeper reviewed intelligence, ambiguity analysis, and report-ready observational summaries.

- [ ] **Step 4: Remove unsafe optimization language**

Avoid: optimize your dose, best dose, you should start, you should stop, switch to, taper, cycle, PCT, source this.

Use: what changed, what is uncertain, reviewed relationship, source-quality warning, observation prompt, discuss with a qualified professional.

- [ ] **Step 5: Add frontend tests**

Test:

    Unknown state renders when no reviewed relationships exist.
    High-risk warnings render above benefit/relationship cards.
    Source refs and evidence tiers are visible.
    Observer upgrade hook appears for gated relationship details.
    Commander upgrade hook appears for side-effect ambiguity on Operator.
    Forbidden phrases are not present in rendered text.

- [ ] **Step 6: Run frontend verification**

Run:

    Set-Location frontend
    npm test -- ProtocolIntelligencePanel.test.tsx
    npm run lint

Expected:

    Focused component tests and lint pass.

## Task 5: Commercial/Infra Agent - Entitlements, Telemetry, Eval Jobs, and Operations

**Files:**
- Modify: docs/billing/tier-enforcement.md
- Modify: docs/commercialization/01-pricing-and-packaging.md
- Modify: backend/src/BioStack.Application/Services/FeatureGate.cs
- Modify: backend/src/BioStack.Api/Endpoints/BillingEndpoints.cs
- Create: backend/src/BioStack.KnowledgeWorker/Workers/ProtocolIntelligenceEvaluationWorker.cs
- Create: backend/tests/BioStack.KnowledgeWorker.Tests/ProtocolIntelligenceEvaluationWorkerTests.cs
- Create: docs/operations/protocol-intelligence-runbook.md

- [ ] **Step 1: Add feature flags to tier docs**

Add explicit features:

    protocol_intelligence_contracts: Observer+
    protocol_phase_map: Operator+
    reviewed_relationship_graph: Operator+
    source_quality_tracker: Operator+
    glp1_observability_pack: Operator+
    side_effect_ambiguity_detector: Commander
    longitudinal_protocol_intelligence_report: Commander
    high_risk_warning_first_guardrails: all tiers, never gated off

- [ ] **Step 2: Enforce features in backend billing output**

CurrentSubscription.features should expose those keys. Safety guardrails and high-risk warnings must be available to all tiers and cannot be hidden behind payment.

- [ ] **Step 3: Add evaluation worker**

The worker should run the golden examples from docs/testing/knowledge-engine-evaluation-harness.md as deterministic checks for retrieval/citation presence, forbidden-output absence, license boundary state, review gate state, FAERS caveat, ClinicalTrials.gov registry-vs-outcome distinction, WADA stale-source blocking, and Retatrutide investigational handling.

Store results as JSON in a configured output path and fail CI/release gates when any safety-critical check fails.

- [ ] **Step 4: Add operations runbook**

docs/operations/protocol-intelligence-runbook.md must include source refresh cadence, license review workflow, artifact promotion workflow, review SLA, rollback process for bad artifacts, stale WADA/label/regulatory status handling, production safety telemetry, and incident response when forbidden output is detected.

- [ ] **Step 5: Add monetization analytics**

Track events:

    protocol_intelligence_viewed
    protocol_intelligence_unknown_state_viewed
    operator_upgrade_from_relationship_gate_clicked
    commander_upgrade_from_ambiguity_gate_clicked
    high_risk_warning_viewed
    source_quality_warning_viewed

Do not log sensitive protocol text or medical details in analytics payloads.

- [ ] **Step 6: Run commercial/infra verification**

Run:

    dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter ProtocolIntelligenceEvaluationWorkerTests --no-restore --nologo --verbosity minimal
    dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --filter BillingTierIntegrationTests --no-restore --nologo --verbosity minimal

Expected:

    Eval worker and tier feature output tests pass.

## Task 6: Final Integration and Release Gate

**Files:**
- Modify only as needed from failed checks.
- Create or update: docs/release/protocol-intelligence-fruition-verification.md

- [ ] **Step 1: Run full focused backend suite**

Run:

    dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore --nologo --verbosity minimal
    dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --nologo --verbosity minimal
    dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --nologo --verbosity minimal

- [ ] **Step 2: Run frontend verification**

Run:

    Set-Location frontend
    npm test
    npm run lint
    npm run build

- [ ] **Step 3: Write verification evidence**

Create docs/release/protocol-intelligence-fruition-verification.md with commit SHA, PR numbers reviewed, contract test results, backend test results, frontend test results, known unrelated failures if any, manual browser route checks, and remaining explicit risks.

- [ ] **Step 4: Manual browser checks**

Verify:

    /protocols/[id] shows Protocol Intelligence panel with Unknown states when no reviewed artifacts exist.
    /protocols/[id] shows high-risk warnings above relationship or benefit cards.
    /pricing accurately positions Operator and Commander without medical-authority copy.
    /admin/research/staged-reviews shows promotion gate blocking reasons for missing evidence/citations/review.
    /knowledge/[slug] relationship section still renders existing evidence tier and review-required states.

- [ ] **Step 5: Completion standard**

Do not claim fruition until:

    broken source references are gone
    artifact contracts are cross-validated
    runtime gates enforce required fields, review status, citations, and forbidden-output scans
    user-facing endpoints return Unknown instead of unsupported inference
    frontend renders source/evidence/review/safety state visibly
    paid-tier gates monetize deeper intelligence while keeping safety warnings free
    eval/ops runbook exists for ongoing refresh and safety regression checks
