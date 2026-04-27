# BioStack Intelligence Supply Chain — Architecture Audit & Recommendation

> Date: 2026-04-25
> Author: Claude (architecture audit)
> Status: Draft for review by Clint
> Scope: Compound/protocol intelligence pipeline — ingestion, enrichment, persistence, runtime analysis, and AI assistance

---

## 1. Executive Summary

**Recommendation: Hybrid — option F, biased toward C + D.**

BioStack should move its intelligence layer from *runtime-recompute* to *runtime-consume*. The right shape is:

> AI-assisted offline enrichment → deterministic validators → human review → versioned `IntelligenceArtifact` rows → a runtime analyzer that only **looks up** verdicts, never invents them.

A small relationship graph (compound-to-compound, with goal/phase/in-vial dimensions) is the keystone. Embeddings and retrieval support enrichment. **A fine-tuned SLM is premature** — defer until there is a curated dataset, an eval harness, and a real cost driver. RAG over structured artifacts will outperform a hastily fine-tuned SLM for the next 6–12 months.

Critically: **the supply-chain seam already exists in the repo.** `BioStack.KnowledgeWorker` ships a real ingestion pipeline (`SubstanceRecordLoader → Validator → Normalizer → TrustGate → Canonicalizer`), a JSON Schema, and seed/refresh job dispatch. The problem is that the canonicalizer flattens the rich `stackIntelligence` block into anemic `KnowledgeEntry` rows, and the runtime analyzer compensates with two narrow string-match whitelists that don't share governance. We have a Lamborghini chassis and a lawnmower engine bolted to it.

The fix is structural, not a rule patch.

---

## 2. Current Architecture Diagnosis

### 2.1 What exists

**Ingestion side (`BioStack.KnowledgeWorker/`):**

```
substances-seed.json
  └─ ISubstanceRecordLoader.Load           [Pipeline/SubstanceRecordLoader.cs]
  └─ ISubstanceRecordValidator.Validate    [JSON Schema: Schemas/substance-record.schema.json]
  └─ Deserialize → SubstanceRecord         [Models/SubstanceRecord.cs]
  └─ ISubstanceRecordNormalizer.Normalize  [Pipeline/SubstanceRecordNormalizer.cs]
  └─ ITrustGate.Apply (ClassA/ClassB)      [Pipeline/TrustGate.cs]
  └─ ISubstanceCanonicalizer.ToKnowledgeEntry
  └─ persisted as KnowledgeEntry           [Domain/Entities/KnowledgeEntry.cs]
```

The pipeline is invoked one-shot via `IngestionWorker` (a `BackgroundService` that exits after dispatching `SeedJob` or `RefreshJob`). It is designed to run as an Azure Container Apps Job, externally triggered. Good seam. No project reference from the API — the worker writes to the shared SQLite/Postgres DB through its own `BioStackDbContext` instance.

**Persistence side:**

- `KnowledgeEntry` — pipe-delimited `Pathways: List<string>` plus `PairsWellWith`, `AvoidWith`, `DrugInteractions`, `CompatibleBlends` (also pipe-delimited free-text lists).
- `CompoundInteractionHint` — `(CompoundA, CompoundB, InteractionType enum, Strength decimal, MechanismOverlap List<string>, Notes)`. **No version, goal context, evidence tier, source refs, review status, confidence calibration, or phase/dose dimensions.**
- `InteractionFlag` — overlapping concept, weaker shape: `OverlapType enum`, `PathwayTag string`, `EvidenceConfidence string("Unknown")`. Effectively a vestigial table.
- No `Pathway` table — pathway is just a free-text string passed around.

**Runtime side (`BioStack.Application/`):**

- `IKnowledgeSource` is bound to `DatabaseKnowledgeSource` in `Program.cs:252`. The hardcoded `LocalKnowledgeSource` (with the *correct* pathway lists for BPC-157, TB-500, MOTS-C, NAD+, Retatrutide) is **dead code at runtime**. It exists in the assembly but is never injected.
- `InteractionIntelligenceService.EvaluatePairAsync` (`Application/Services/InteractionIntelligenceService.cs:118–225`) decides verdicts in this order:
  1. `_hintRepository.FindPairAsync(canonicalA, canonicalB)` — exact-string match against the hint table.
  2. `AvoidWith` named-match → `Interfering`.
  3. `DrugInteractions` named-match → `Interfering`.
  4. `PairsWellWith` / `CompatibleBlends` named-match → `Synergistic`.
  5. `AllPathwaysInComplementaryDomain(sharedPathways)` against a **hardcoded 6-keyword whitelist** (`tissue-repair`, `angiogenesis`, `wound-healing`, `extracellular-matrix-remodeling`, `actin-binding`, `nitric-oxide-signaling`) → `Complementary`. **This is the only path that distinguishes synergy from redundancy when no hint exists.**
  6. Any shared pathway → `Redundant`.
  7. No shared pathway → `Neutral`.
- `CompoundInteractionHintCatalog.SeedDefaultsAsync` (`Program.cs:360`) seeds 14 hardcoded pairs at API startup, including BPC-157 + TB-500 → `Complementary, 0.85`. This is a finger in the dam, not a supply chain.

### 2.2 What's structurally wrong

| Problem | Evidence | Consequence |
|---|---|---|
| Runtime *recomputes* verdicts from low-fidelity inputs | `InteractionIntelligenceService.EvaluatePairAsync` walks pathway lists every request | Brittle to pathway-name drift; can't express nuance |
| Rich relationship semantics dropped during canonicalization | `SubstanceRecord.stackIntelligence.{pairsWellWith, avoidWith, synergyRules, redundancyRules, conflictRules, overlapRules, timingRules}` (~7 typed structures with rationale and sources) → flattened to four free-text lists on `KnowledgeEntry` | The pipeline's most valuable output is thrown away |
| Two parallel governance surfaces | `CompoundInteractionHintCatalog.Defaults` (14 hardcoded pairs) AND `ComplementaryPathwayKeywords` (6 hardcoded strings) | They drift. The catalog uses `BPC-157`/`TB-500` strings; the override uses pathway strings; neither can vouch for the other |
| No relationship goal/phase/dose context | `CompoundInteractionHint` schema | Can't say "complementary in a healing phase, redundant in an aggressive cut" |
| No version, no review status, no source refs persisted | Same | Can't audit, can't roll back, can't surface confidence/citation in UI honestly |
| No pathway normalization at the type level | `KnowledgeEntry.Pathways` is `List<string>` | "Tissue Repair" ≠ "tissue-repair" ≠ "tissue_repair" — silent miss |
| No human review queue | Not present anywhere | TrustGate marks `ReviewReasons` and the data goes to logs only |
| Worker → Api isolation has no contract layer | `BioStack.KnowledgeWorker` runs the canonicalizer but persists to a schema co-owned by Api | Cross-team change risk; no compile-time gate on what the worker can produce vs what the runtime expects |
| `LocalKnowledgeSource` is dead | `Program.cs:252` binds `DatabaseKnowledgeSource` only | Unit tests/devs may believe BPC-157's pathway list is `["tissue-repair", "gi-protective", "angiogenesis"]` (it is in the source file) when at runtime the DB might have `["Tissue Repair", "GI Protection", "Angiogenesis"]` |

---

## 3. Failure Mode Analysis: Why BPC-157 + TB-500 Breaks Trust

The catalog seeds the correct verdict (`Complementary, 0.85`). The override has the right intent. Yet the pairing can still misfire. Three independent failure surfaces, any one of which produces the credibility hit:

### 3.1 Failure Surface A — Compound-name resolution mismatch
`InteractionIntelligenceService.EvaluatePairAsync:128`:
```csharp
var hint = await _hintRepository.FindPairAsync(
    compoundA.CanonicalName,
    compoundB.CanonicalName,
    cancellationToken);
```
`CompoundInteractionHintRepository.NormalizePair` only does `Trim()` and alphabetical ordering — *not* normalization, not aliasing. The hint table stores the literal strings `"BPC-157"` and `"TB-500"` (line 11 of the catalog). If the user's stack imports a compound whose `CanonicalName` is `"BPC157"`, `"BPC 157"`, `"Body Protective Compound 157"`, or `"Thymosin Beta-4"` (TB-500's chemical name), **the hint lookup fails silently** and we fall through to pathway logic.

### 3.2 Failure Surface B — Pathway-string drift
The complementary override (`InteractionIntelligenceService.cs:175–189`) requires *all* shared pathways to be in a 6-word whitelist (case-insensitive but format-sensitive: dash-case only). The seed JSON (`substances-seed.json`) is the source of truth for `KnowledgeEntry.Pathways`. If the seed expresses BPC-157's pathways as `["Tissue Repair", "GI Protection", "Angiogenesis"]`, the override fails — `"Tissue Repair"` is not in the whitelist (it's not dash-case). The intersection between BPC-157 and TB-500 collapses to a redundant-shaped overlap by string equality alone. **Verdict drops from `Complementary, 0.85` to `Redundant, ~0.55` in one keystroke.**

### 3.3 Failure Surface C — Whitelist incompleteness
Even with perfectly canonical names, BPC-157 and TB-500 have plausible shared pathways outside the whitelist (e.g. `"cell-migration"`, `"anti-inflammatory"`, `"growth-factor-signaling"`). The override insists on `AllPathwaysInComplementaryDomain` — *all* shared. If the intersection contains *one* keyword that isn't in the 6-word list, the whole override fails and the verdict is `Redundant`. The whitelist's inclusion criterion is "I remembered to add it"; that does not scale to MOTS-C, NAD+, or any compound whose mechanism profile lives outside the healing domain.

### 3.4 The user-facing language compounds the trust hit
On the redundant fallback (line 207):
> "Shared pathway overlap detected: tissue-repair." with confidence shown.

Even when the pathway-shared verdict is technically correct (e.g. for two GLP-1 agonists), the phrase "appear to overlap enough that attribution may get muddy" reads as a problem flag. For a known synergistic pairing like BPC+TB, this language is the kind of failure that costs us a peptide-literate user on first impression.

### 3.4½ Failure Surface D (the smoking gun) — Hint seeding is dev-only

`Program.cs:342` wraps the hint-catalog seeding in `if (!app.Environment.IsProduction())`. In production, the API does **not** seed `CompoundInteractionHint` rows. The hint table is populated *only* by `BioStack.KnowledgeWorker/Program.cs:92` — which is a one-shot, externally-triggered Container Apps Job. **If the seed job hasn't been run against the production DB, the hint table is empty**, every `FindPairAsync` returns `null`, and every pair falls into the pathway-whitelist override path (Surface B/C above). This single line of environment gating turns a working dev environment into a degraded prod environment with no warning. A health-check or admin endpoint asserting "≥ N hints present" should exist but does not.

### 3.5 Synthesis
The architecture allows four independent string-match / configuration failures to produce the same wrong verdict, and the governance for each whitelist is divorced from the data that flows past it. *No amount of patching* will close the gap as long as the system computes verdicts from pathway-string intersections at request time.

---

## 4. Recommended Target Architecture

```
RAW SOURCES                  ENRICHMENT (offline)              RUNTIME (deterministic)
──────────                   ───────────────────               ───────────────────────
substance-records (JSON)  ─┐
research summaries        ─┤  ┌─ Normalize          ┐
vendor docs               ─┼─►│  Canonicalize       │
expert annotations        ─┤  │  Validate (schema)  │
LLM-extracted structures  ─┘  │  Trust Gate         │   ┌─ Stack normalize
                              │  Pathway-resolve    │   │  Compound resolve (alias-aware)
                              │  Relationship build │   │  Goal/phase context
                              │   ├─ from stackIntel │   │  Lookup IntelligenceArtifact
                              │   ├─ from rules     │   │  Apply safety/policy guards
                              │   └─ from LLM proposals  Render approved explanation
                              │  Conflict detect    │   │  Surface confidence + citation
                              │  Confidence score   │   └─→ user
                              │  Review queue ──────┼─→ HUMAN
                              │  Publish (versioned)│
                              └─────────────────────┘
                                       │
                               IntelligenceArtifact
                               CompoundRelationship
                               PathwayCanonical
                               EvidenceCitation
```

Hard rules:

1. **Runtime never invents a relationship.** If no published artifact resolves the pair (with the requested goal/phase context), the verdict is `Unknown` — *not* `Redundant`. Unknown is honest. Redundant is a claim.
2. **Every artifact is versioned.** `(version, publishedAt, reviewStatus, reviewerId, sourceRefs[])` are required columns.
3. **Pathways are first-class.** `Pathway` is a table with canonical IDs, aliases, and a domain classification. Free-text pathways are accepted only as ingestion input; persisted pathway references are FK-typed.
4. **Compound aliases are first-class.** `CompoundAlias` table maps `"BPC157" | "BPC 157" | "Body Protective Compound 157" → CompoundId`.
5. **AI proposes, deterministic validators constrain, humans approve, runtime consumes.** Never an LLM call on the request hot path that generates a *new* claim.
6. **Safety guard runs last and is immutable.** It can downgrade a verdict, never upgrade. It can strip language, never add medical authority.

---

## 5. Background Processing Pipeline Proposal

Extend the existing `BioStack.KnowledgeWorker` rather than replace it. New stages in **bold**:

| Stage | Existing? | Owner | Output |
|---|---|---|---|
| Source Import | Yes (`SubstanceRecordLoader`) | Worker | Raw `SubstanceRecord` JSON |
| Schema Validation | Yes (`SubstanceRecordValidator`) | Worker | Validation errors / pass |
| Normalization | Yes (`SubstanceRecordNormalizer`) | Worker | Idempotent record |
| Trust Gate | Yes (`TrustGate`) | Worker | ClassA/ClassB filtered record + review reasons |
| Canonicalization (compound) | Partial (`SubstanceCanonicalizer`) | Worker | `KnowledgeEntry` + **`CompoundAlias[]`** |
| **Pathway Canonicalization** | **No** | **Worker** | **`PathwayCanonical[]` + alias map** |
| **Relationship Extraction** | **No** | **Worker** | **`CompoundRelationship[]` from `stackIntelligence.{pairsWellWith,avoidWith,synergyRules,redundancyRules,conflictRules,overlapRules,timingRules}`** |
| **Cross-Source Conflict Detection** | **No** | **Worker** | **Contradiction reports** |
| **LLM Enrichment Pass** | **No** | **Worker (gated)** | **Proposed `CompoundRelationship[]` with citations** |
| **Confidence Scorer** | **No** | **Worker** | **Numeric calibrated `confidence` (0–1)** |
| **Review Queue Materialization** | **No** | **Worker** | **`ReviewQueueItem` rows** |
| **Publish (versioned)** | **No** | **Worker** | **`IntelligenceArtifact` row, prior version archived** |
| Runtime Consumption | Recompute today | Api | Lookup `IntelligenceArtifact` by `(compoundIdA, compoundIdB, goal, phase)` |

Execution model:

- **Local development**: a `dotnet run --project BioStack.KnowledgeWorker -- --mode seed` invocation rebuilds the entire knowledge graph against SQLite. Idempotent. Versioned.
- **Production v1**: scheduled Azure Container Apps Job, weekly cadence, with manual trigger for hotfixes.
- **Eventually**: queue-driven (`Refresh` job per source change).

The pipeline is a **build process**, not a request-time service. Its outputs are projections; the runtime is a thin reader.

---

## 6. AI / LLM / SLM Feasibility

### 6.1 Where AI helps offline (recommended now)

| Task | Model class | Risk profile | Recommended |
|---|---|---|---|
| Structured extraction from papers/vendor PDFs into `SubstanceRecord` JSON | LLM (Claude/GPT-class) with strict schema-out | Low (validators downstream) | **Yes — phase 3** |
| Relationship proposal: "given A and B's mechanisms, propose a `CompoundRelationship` row" | LLM with retrieval over published artifacts | Low (review-gated) | **Yes — phase 3** |
| Mechanism summarization for explanation copy | LLM, snapshot-tested | Medium (medical-tone drift) | **Yes — phase 4, snapshot tests required** |
| Conflict explanation drafting | LLM with citation requirement | Medium | **Yes — phase 4, must cite** |
| Pathway clustering / similarity | Embedding model | Low | **Yes — phase 4** |
| Compound-similarity / "find related" | Embeddings + vector index | Low | **Yes — phase 4** |
| "Common pairing" detection from user telemetry | Classifier (no need for LLM) | Low | **Yes — phase 5+** |
| Synthetic test scenario generation | LLM | Low | **Yes — phase 6 test build** |

### 6.2 Where AI does not belong (now or later)

| Task | Why not |
|---|---|
| Runtime generative claims about a stack | Hallucinates, can't cite, drifts into medical advice |
| Personalized dose recommendations | Boundary violation regardless of model |
| Replacing deterministic safety guards | Guards must be auditable code |
| Generating new relationships at request time | Same as guards — must be reviewed before serving |

### 6.3 Runtime usage of AI (allowed, narrow)

A tiny templated step is acceptable:
- Render an *approved* `IntelligenceArtifact.Explanation` template with the user's compound names substituted.
- Translate stored explanation into the user's locale.
- Re-rank already-approved artifacts by relevance to the user's stated goal.

If we use an LLM at runtime, it is **populating templates, not making claims**.

### 6.4 SLM / fine-tune verdict

**Premature.** Reasons:
- No curated training set yet. Building one is the same work as building the relationship graph; do that first and the graph alone may close the gap.
- Eval harness doesn't exist. Without it we can't tell if a fine-tune helps or hurts.
- BioStack's value is *correctness* over *coverage*. Fine-tunes optimize for fluency, which is the wrong gradient.
- A 7B–13B model running locally is appealing for the local-first ethos but adds operational surface for ambiguous gain.
- Rule-of-thumb: revisit when the relationship graph is published, the eval harness gates CI, and we have ≥10k human-reviewed relationship rows. Likely 9–15 months out.

**The correct sequencing is**: structured graph → embeddings + RAG → eval-gated LLM enrichment → *consider* SLM only if cost or privacy forces it.

---

## 7. Proposed Data Model Changes

New tables (SQLite-compatible, EF migration plan):

### 7.1 `Compound` (promote from `KnowledgeEntry`)
```
Id              GUID PK
CanonicalName   string (unique, indexed)
Category        CompoundCategory enum
Slug            string (URL-safe, unique)
CreatedAtUtc    DateTime
UpdatedAtUtc    DateTime
ArtifactVersion int
```

### 7.2 `CompoundAlias`
```
Id           GUID PK
CompoundId   FK Compound
Alias        string (unique-per-compound, indexed)
AliasType    enum { CommercialName, ChemicalName, AbbreviationVariant, ColloquialName }
Source       string
```

### 7.3 `Pathway`
```
Id            GUID PK
CanonicalKey  string (unique, dash-case enforced, indexed)  -- e.g. "tissue-repair"
DisplayName   string                                         -- e.g. "Tissue Repair"
Domain        enum { Healing, Metabolic, Hormonal, Neurological, ImmuneInflammatory, Other }
Description   string
```

### 7.4 `PathwayAlias`
```
Id        GUID PK
PathwayId FK Pathway
Alias     string (case-insensitive unique)
```

### 7.5 `CompoundPathway` (M:M)
```
CompoundId GUID FK
PathwayId  GUID FK
Role       enum { PrimaryMechanism, DownstreamEffect, ContextualEffect }
Confidence decimal(3,2)
SourceRefs string[] (JSON)
```

### 7.6 `CompoundRelationship` (the keystone)
```
Id                    GUID PK
SourceCompoundId      FK Compound  (canonical lower-id ordering for uniqueness)
TargetCompoundId      FK Compound
RelationshipType      enum { Synergy, Complementary, Redundant, Caution, Avoid,
                              CommonPairing, InVialCompatible, InVialIncompatible,
                              GoalSpecific, Unknown }
GoalContext           enum?  { TissueRepair, MetabolicHealth, BodyComposition,
                                Longevity, NeuroSupport, GeneralWellbeing, None }
PhaseContext          enum?  { Loading, Maintenance, Cycling, Off, Any }
DoseContext           string? (free text; "low/standard/high" tags later)
MechanismRationale    string  (educational, no medical advice)
EvidenceTier          EvidenceTier enum { Strong, Moderate, Limited, Anecdotal, None }
ConfidenceScore       decimal(3,2)
PopularityScore       decimal(3,2)?  -- for "common pairing" weighting
RiskCategory          enum { None, Caution, Avoid }
SourceRefs            JSON string[]  -- citation list (DOI, URL, or seed-source-id)
ProposedBy            enum { Authored, ImportedSubstanceRecord, LLMProposed, ClinicalSource }
ReviewStatus          enum { Draft, NeedsReview, Approved, Deprecated }
ReviewerId            GUID?
ReviewedAtUtc         DateTime?
ArtifactVersion       int
PublishedAtUtc        DateTime?
SupersededByVersion   int?
CreatedAtUtc          DateTime
UpdatedAtUtc          DateTime

UNIQUE (SourceCompoundId, TargetCompoundId, GoalContext, PhaseContext, ArtifactVersion)
```

### 7.7 `IntelligenceArtifact` (versioned snapshot)
```
Id              GUID PK
ArtifactVersion int (monotonic)
PublishedAtUtc  DateTime
Description     string
RowCounts       JSON  -- {compounds: N, relationships: M, pathways: K}
PipelineRunId   GUID
ContentHash     string  -- for cache invalidation
```

### 7.8 `ReviewQueueItem`
```
Id              GUID PK
EntityType      enum { CompoundRelationship, Compound, Pathway }
EntityId        GUID
Reason          string
ProposedBy      enum (mirrors above)
ConfidenceScore decimal(3,2)?
DiffJson        string  -- proposed change for reviewer
Status          enum { Pending, Approved, Rejected, Snoozed }
AssignedTo      GUID?
CreatedAtUtc    DateTime
ResolvedAtUtc   DateTime?
ResolverId      GUID?
ResolverNotes   string?
```

### 7.9 `EvidenceCitation`
```
Id            GUID PK
EntityType    enum
EntityId      GUID
SourceType    enum { PeerReview, Preprint, ClinicalGuideline, VendorTechSheet,
                      AuthoredAnalysis, Anecdotal }
Title         string
Url           string?
Doi           string?
QuotedExcerpt string? (with attribution)
EvidenceTier  EvidenceTier
```

### 7.10 Deprecate, do not delete
- `CompoundInteractionHint` — keep migration path; back the existing table with a view that projects approved `CompoundRelationship` rows where `GoalContext is null`. Then remove after the runtime cuts over.
- `InteractionFlag` — same.

### 7.11 Why this is still local-first
All tables are SQLite-compatible. The graph is *modeled* relationally; it does not require a graph DB. Postgres scaling path remains available. No new infrastructure.

---

## 8. Runtime Analyzer Redesign

### 8.1 New `EvaluatePairAsync` flow

```
Input: (userCompoundA, userCompoundB, goal?, phase?)

1. Resolve compounds via CompoundAlias → (compoundIdA, compoundIdB)
   - If unresolved on either side → Verdict.Unknown("compound not in catalog")

2. Look up CompoundRelationship by:
   (compoundIdA, compoundIdB) ordered, where ReviewStatus=Approved,
   matching GoalContext and PhaseContext (most-specific first):
     a) (goal, phase) → b) (goal, Any) → c) (None, phase) → d) (None, Any)

3. If found → return verdict directly: type, confidence, rationale, citations.

4. If not found → return Verdict.Unknown
   ("we don't have an evaluated relationship for this pair in this context").
   Surface honestly. Do not invent.

5. SafetyGuard.Apply(verdict)
   - Strip prohibited phrases.
   - Downgrade explanation severity if user has unrelated active flags.
   - Never upgrade.

6. Render UI payload from artifact's Explanation template.
```

### 8.2 What this removes
- `ComplementaryPathwayKeywords` (deleted; replaced by `CompoundRelationship.RelationshipType`)
- The shared-pathway `Redundant` fallback (deleted; absent relationship = `Unknown`)
- Free-text pathway intersection at request time (replaced by relationship lookup)

### 8.3 What this preserves
- `ProtocolAnalysisCache` two-layer cache (in-memory + distributed) — even more useful when responses are pure lookups.
- `IProtocolFingerprintService` keying.
- `AnalyzerPrewarmService` — repurpose to prewarm the *artifact cache*, not seed analyses.

### 8.4 Confidence and citation surface
Every UI verdict response carries:
```
{
  type: "Complementary",
  confidence: 0.85,
  evidenceTier: "Limited",
  rationale: "...",
  citations: [...],
  artifactVersion: 47,
  reviewStatus: "Approved"
}
```
The frontend renders confidence and citation always. If `Unknown`, the frontend renders "we haven't evaluated this combination yet" — calmer, more credible than an over-confident wrong verdict.

---

## 9. Testing & Evaluation Plan

### 9.1 Golden fixture set (start here)

| Pair | Goal | Expected | Confidence floor | Evidence |
|---|---|---|---|---|
| BPC-157 + TB-500 | TissueRepair | Complementary | 0.80 | Limited |
| BPC-157 + GHK-Cu | TissueRepair | Complementary | 0.65 | Limited |
| TB-500 + GHK-Cu | TissueRepair | Complementary | 0.65 | Limited |
| Semaglutide + Tirzepatide | BodyComposition | Redundant | 0.85 | Moderate |
| Semaglutide + Liraglutide | BodyComposition | Redundant | 0.80 | Moderate |
| Tirzepatide + Retatrutide | BodyComposition | Redundant | 0.70 | Limited |
| Tesamorelin + CJC-1295 | NeuroSupport | Redundant | 0.65 | Limited |
| Tesamorelin + Ipamorelin | NeuroSupport | Synergy | 0.60 | Limited |
| Metformin + Berberine | MetabolicHealth | Redundant | 0.60 | Moderate |
| Creatine + L-Carnitine | NeuroSupport | Synergy | 0.45 | Limited |
| Clomiphene + Testosterone Cypionate | NeuroSupport | Caution | 0.55 | Moderate |
| BPC-157 + Random unknown peptide | TissueRepair | Unknown | n/a | n/a |

### 9.2 Test types

- **Resolution tests**: alias variants resolve to canonical IDs (`"BPC157"`, `"BPC 157"`, `"Body Protective Compound 157"` → same `Compound`).
- **Lookup tests**: `(A, B)` and `(B, A)` return identical artifact.
- **Goal-context tests**: `(A, B, TissueRepair)` and `(A, B, MetabolicHealth)` may differ; missing goal falls back per §8.1.
- **Snapshot tests**: full UI payload snapshotted and reviewed on diff (frontend regression).
- **Forbidden-phrase tests**: unsafe strings ("you should", "we recommend you take", "treatment for", "diagnose", "cure", any dose imperative) fail CI when emitted by any artifact.
- **Coverage tests**: each enum value of `RelationshipType` has at least N=3 reviewed artifacts.
- **Calibration tests**: when the model says `confidence ≥ 0.8`, reviewer agreement should be ≥ 90% on a sampled set.
- **Eval harness regression**: golden fixtures are snapshotted into a JSON file; CI fails on any verdict drift without a corresponding migration.

### 9.3 Where AI testing helps
- LLM-generated *adversarial* pairs to surface gaps in the relationship graph.
- LLM-generated user-facing prompts that should be safely rejected.
- These are tooling, not verdict authority.

---

## 10. Migration Plan (phased)

### Phase 0 — Hotfix the BPC+TB credibility hit (this week)
Goal: stop the bleeding. Do not refactor.

1. **Remove the `IsProduction()` gate on hint seeding** (`Program.cs:342`). Hint seeding is data, not a dev convenience; it must run everywhere. Wrap the call in an idempotent check (which `SeedDefaultsAsync` already is — it skips existing pairs).
2. Add a startup health-check that fails readiness if the hint table or `KnowledgeEntry` table is empty.
3. Add an integration test that asserts BPC-157 + TB-500 returns `Complementary` against a fresh DB after API startup.
4. Add pathway-string normalization (`tolower + replace spaces/underscores with dash`) on both ingestion *and* on the override comparison so casing/format drift cannot hide a known pair.
5. Add compound-name alias resolution in `IKnowledgeSource.GetCompoundAsync` for the seeded peptides (`BPC157` → `BPC-157`, `TB500` → `TB-500`, `Thymosin Beta-4` → `TB-500`).
6. Soften the `Redundant` fallback's user-facing string when `EvidenceTier < Moderate` ("we have limited evidence on this combination" instead of "attribution may get muddy").

### Phase 1 — Promote stackIntelligence to first-class relationships (week 2–3)
1. Migration: add `CompoundRelationship`, `Pathway`, `PathwayAlias`, `CompoundAlias` tables.
2. Extend `SubstanceCanonicalizer` to populate `CompoundRelationship` rows from `stackIntelligence.{pairsWellWith, avoidWith, synergyRules, redundancyRules, conflictRules, overlapRules}`.
3. Hint catalog re-modeled as seed `CompoundRelationship` rows (keep old table as a view for backward compat).
4. `IKnowledgeSource` grows a `GetRelationshipAsync(compoundIdA, compoundIdB, goal?, phase?)`.

### Phase 2 — Versioning + review queue (week 3–4)
1. Add `IntelligenceArtifact`, `ReviewQueueItem`, `EvidenceCitation`.
2. Pipeline writes to a *new version* on each run; runtime reads "latest published".
3. Build a minimal admin review UI (table + approve/reject — does not need to be pretty).
4. `ReviewStatus = Approved` is required for runtime visibility; `Draft` and `NeedsReview` are admin-only.

### Phase 3 — LLM-assisted offline enrichment (week 5–6)
1. New pipeline stage: `LLMRelationshipProposer` (Claude/GPT-class, schema-constrained).
2. Output: `CompoundRelationship` rows with `ProposedBy = LLMProposed`, `ReviewStatus = NeedsReview`.
3. Hard rule: a proposed row is **never visible at runtime** until reviewed.
4. Citations required; a proposal without citations is auto-rejected.

### Phase 4 — Embeddings + retrieval (week 6–7)
1. Generate compound and pathway embeddings during canonicalization.
2. Add a small vector store (SQLite-VSS or Postgres pgvector when we move).
3. Use embeddings only for: similarity search in admin UI, retrieval in LLM enrichment, and "find related compounds" suggestions.
4. **Not** used for runtime verdicts.

### Phase 5 — Runtime cutover (week 7–8)
1. Rebuild `EvaluatePairAsync` per §8.
2. Delete `ComplementaryPathwayKeywords` and the redundant pathway fallback.
3. Strangler-fig: feature flag `analyzer.useArtifactBackedVerdicts`, default off in prod, on in staging. Compare verdicts side-by-side for a week. Cut over on parity.
4. Remove the LocalKnowledgeSource dead code or keep it as a test-only `IKnowledgeSource`.

### Phase 6 — Eval harness + CI gate (week 8–9)
1. Golden fixture set in JSON.
2. CI runs the runtime against the fixtures on every PR; verdict drift fails the build.
3. Forbidden-phrase scanner.
4. Calibration report on review queue resolutions (weekly).

### Phase 7 — (Defer) SLM evaluation (month 6+, conditional)
- Only revisit if: (a) eval harness shows a coverage ceiling that RAG can't break through, (b) operational cost of Claude/GPT-class enrichment exceeds an SLM's TCO, or (c) privacy requirements demand a local model.

---

## 11. Risks & Tradeoffs

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Schema migration breaks existing protocols/check-ins | Medium | High | Keep old tables as views; staged migration; integration tests on protocol load |
| Reviewer becomes the bottleneck | High | Medium | Auto-approve high-confidence rule-derived rows (`ProposedBy=ImportedSubstanceRecord`, evidence ≥ Moderate); only LLM proposals enter human queue |
| LLM cost growth | Medium | Medium | Offline only; cache enrichment outputs by source-content hash; throttle re-enrichment to source-changed records |
| LLM hallucinates citations | Medium | High | Pipeline rejects any LLM proposal whose citations don't resolve to real DOIs/URLs; require excerpt from source |
| Verdict drift between artifact versions confuses users | Medium | Medium | UI shows "since artifact v47" footer; changelog page; admin diffs in review queue |
| Local-first goal vs LLM dependency | Low | Medium | Enrichment runs offline; runtime is local-only and never calls an external LLM |
| Boundary creep into medical advice as graph grows | Medium | High | Forbidden-phrase scanner in CI; safety guard runs on every render; review queue rejects medical-tone proposals |
| Pathway whitelist ossifies into the new graph | Low | Medium | `Pathway.Domain` enum is small (≤10); rejection criteria documented; resist the urge to add per-edge pathway logic |
| KnowledgeWorker assembly drift from Api expectations | Medium | High | Add `BioStack.Contracts.Knowledge` package; both projects compile against the contract; pipeline emits validated DTOs |
| Underestimating the "Unknown" UX | High | Medium | Design `Unknown` UI early; A/B against current behavior to confirm it does not degrade trust |

---

## 12. Immediate Next Steps (this week)

In priority order. Each step is a discrete piece of work. None require new infra.

1. **Reproduce the BPC+TB failure** in a deterministic integration test. Run the live pipeline against a fresh DB; assert the verdict; capture the exact failure surface (A, B, or C from §3).
2. **Add `BioStack.Application.Tests/InteractionIntelligenceServiceTests.cs::EvaluatesBpcAndTbAsComplementary`** as a regression gate.
3. **Patch pathway normalization** on both ingestion and the override comparison (Phase 0, item 3).
4. **Patch compound alias resolution** for the seeded peptides (Phase 0, item 4).
5. **Soften the redundant fallback's user-facing string** (Phase 0, item 5).
6. **Open a tracking ticket** for Phase 1 (relationship-graph migration) with the schema in §7 attached.
7. **Write a one-page architecture brief** (a 1-page summary of this doc) for sharing with anyone touching `BioStack.KnowledgeWorker` or `InteractionIntelligenceService`.
8. **Audit `substances-seed.json`** for pathway-name format. If pathways aren't already dash-case, the override is silently failing for many compounds.

---

## 13. Specific Implementation Directives for Codex / Augment

These are concrete, sequential, ready-to-hand-off tickets. Each is a single PR.

### Ticket BS-INT-001 — Add regression test for BPC-157 + TB-500
- **Project**: `tests/BioStack.Application.Tests`
- **Add**: `InteractionIntelligenceServiceTests.EvaluatesBpcAndTbAsComplementary`
- **Behavior**: Build a `KnowledgeEntry` for BPC-157 with `Pathways = ["tissue-repair","gi-protective","angiogenesis"]` and one for TB-500 with `Pathways = ["tissue-repair","anti-inflammatory","cell-migration"]`. Wire a fake `ICompoundInteractionHintRepository` returning the catalog hint. Assert verdict is `Complementary`, `confidence >= 0.80`, `notes` is the catalog `Notes`.
- **Then**: a second test variant with the hint repo returning `null`, asserting verdict is still `Complementary` (override path) — this nails Failure Surface A and B together.

### Ticket BS-INT-002 — Pathway normalization helper + ingestion enforcement
- **Add**: `BioStack.Domain.ValueObjects.PathwayKey` (a `record struct` wrapping a string with constructor that lowercases, trims, replaces `_` and ` ` with `-`).
- **Apply** in `SubstanceRecordNormalizer.Normalize` to all pathway lists.
- **Apply** in `InteractionIntelligenceService` when reading `KnowledgeEntry.Pathways` for the override (defensive double-normalization).
- **Migration**: a one-shot data migration that rewrites existing pipe-delimited `Pathways` columns through `PathwayKey`.

### Ticket BS-INT-003 — Compound alias resolution at runtime
- **Add**: `BioStack.Domain.Entities.CompoundAlias` (per §7.2).
- **Add**: migration creating the `CompoundAlias` table.
- **Add**: `CompoundAliasRepository` and a `IKnowledgeSource.ResolveCompoundAsync(string nameOrAlias, ...)` method.
- **Seed**: aliases for the 14 catalog compounds (`BPC157`, `BPC 157`, `Body Protective Compound 157`, `Thymosin Beta-4`, `T-Beta-4`, `MOTS-c`, `MotsC`, etc.).
- **Wire**: `EvaluateByNamesAsync` calls `ResolveCompoundAsync` instead of `GetCompoundAsync`.

### Ticket BS-INT-004 — Soften the redundant-fallback explanation language
- **Edit**: `InteractionIntelligenceService.cs` ~line 207.
- **Replace**: "appear to overlap enough that attribution may get muddy" with two strings — keep the attribution language only when `EvidenceTier >= Moderate`, otherwise: "we have limited evidence on this combination; pathway overlap noted at {pathways}". Both strings live in a `ResponseStrings` static class so the snapshot tests can lock them.

### Ticket BS-INT-005 — Schema migration for relationship graph (Phase 1)
- **Add**: entities per §7.1–§7.6 + EF configuration.
- **Add**: a new migration `AddCompoundRelationshipGraph`.
- **Migrate**: existing `CompoundInteractionHint` rows into `CompoundRelationship` (1:1 mapping; `GoalContext = None`, `PhaseContext = Any`, `ProposedBy = Authored`, `ReviewStatus = Approved`, `ArtifactVersion = 1`).
- **Keep**: the old `CompoundInteractionHint` table as a SQL view for one release.

### Ticket BS-INT-006 — Extend canonicalizer to emit relationships
- **Edit**: `SubstanceCanonicalizer`.
- **Add**: `EmitRelationshipsFromStackIntelligence(SubstanceRecord)` that walks `pairsWellWith`, `avoidWith`, `synergyRules`, `redundancyRules`, `conflictRules`, `overlapRules` and produces `CompoundRelationship` rows.
- **Resolve**: target compound names through `CompoundAlias`. Unresolved targets enqueue a `ReviewQueueItem` with reason `"target alias not in catalog"`.
- **Citations**: every emitted row carries `SourceRefs` from the input (or empty, with `EvidenceTier = Anecdotal`).

### Ticket BS-INT-007 — IntelligenceArtifact versioning
- **Add**: `IntelligenceArtifact` table per §7.7.
- **Wire**: `IngestionWorker` writes a new artifact row at the end of each pipeline run; previous version remains queryable.
- **Add**: an `IArtifactReader` service for runtime; reads "latest published" by default.

### Ticket BS-INT-008 — Review queue + admin UI scaffold
- **Add**: `ReviewQueueItem` table + `ReviewQueueRepository`.
- **Endpoint**: `GET /admin/review-queue?status=Pending`, `POST /admin/review-queue/{id}/approve`, `POST /admin/review-queue/{id}/reject`.
- **UI**: a single Next.js admin page at `/admin/intelligence/review-queue` with table, diff view, approve/reject actions. Tailwind, no design polish required.

### Ticket BS-INT-009 — Runtime cutover behind a flag
- **Add**: feature flag `analyzer.useArtifactBackedVerdicts`, configured per-environment.
- **Implement**: a second `EvaluatePairAsync` that consults `CompoundRelationship` first (per §8) and only falls back to today's logic when the flag is off.
- **Telemetry**: log verdict + flag state for a week to compare drift.

### Ticket BS-INT-010 — Forbidden-phrase scanner + golden fixture eval
- **Add**: `tests/BioStack.Eval/IntelligenceFixtures.json` per §9.1.
- **Add**: a `ForbiddenPhraseScanner` that fails CI on any `IntelligenceArtifact.Explanation` containing the medical-tone phrases.
- **Wire**: into the existing test runner; gate PRs.

### Ticket BS-INT-011 — LLM enrichment stage (Phase 3, separate PR series)
- Defer; spec when Phase 1–2 land. Will require: API key plumbing, schema-constrained LLM client, citation resolver, deduper against existing relationships, review-queue routing.

---

## Appendix A — File reference (for Codex/Augment quick orientation)

Runtime intelligence path:
- `backend/src/BioStack.Api/Endpoints/AnalyzeEndpoints.cs`
- `backend/src/BioStack.Application/Services/ProtocolAnalyzerService.cs`
- `backend/src/BioStack.Application/Services/InteractionIntelligenceService.cs`
- `backend/src/BioStack.Application/Services/OverlapService.cs`
- `backend/src/BioStack.Application/Services/CounterfactualEngine.cs`
- `backend/src/BioStack.Infrastructure/Knowledge/{IKnowledgeSource,DatabaseKnowledgeSource,LocalKnowledgeSource,CompoundInteractionHintCatalog}.cs`
- `backend/src/BioStack.Infrastructure/Repositories/CompoundInteractionHintRepository.cs`
- DI binding: `backend/src/BioStack.Api/Program.cs:252` (`IKnowledgeSource → DatabaseKnowledgeSource`)
- Hint seed: `backend/src/BioStack.Api/Program.cs:360` (`CompoundInteractionHintCatalog.SeedDefaultsAsync`)

Ingestion pipeline:
- `backend/src/BioStack.KnowledgeWorker/Program.cs`
- `backend/src/BioStack.KnowledgeWorker/Workers/IngestionWorker.cs`
- `backend/src/BioStack.KnowledgeWorker/Pipeline/IngestionPipeline.cs`
- `backend/src/BioStack.KnowledgeWorker/Pipeline/{SubstanceRecordLoader,SubstanceRecordValidator,SubstanceRecordNormalizer,TrustGate,SubstanceCanonicalizer}.cs`
- `backend/src/BioStack.KnowledgeWorker/Schemas/substance-record.schema.json`
- `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`
- `backend/src/BioStack.KnowledgeWorker/Models/SubstanceRecord.cs` (+ `SubstanceRecord.Safety.cs`)
- `backend/src/BioStack.KnowledgeWorker/Jobs/{IIngestionJob,IngestionJobBase,SeedJob,RefreshJob,IngestionContext,JobRunResult}.cs`

Domain entities to evolve:
- `backend/src/BioStack.Domain/Entities/{KnowledgeEntry,CompoundInteractionHint,InteractionFlag,CompoundRecord}.cs`
- `backend/src/BioStack.Domain/Enums/{InteractionType,OverlapType,EvidenceTier,CompoundCategory}.cs`

Persistence:
- `backend/src/BioStack.Infrastructure/Persistence/BioStackDbContext.cs`
- `backend/src/BioStack.Infrastructure/Persistence/InteractionSchemaBootstrapper.cs`
- `backend/src/BioStack.Infrastructure/Persistence/Migrations/`

---

## Appendix B — One-paragraph TL;DR

BioStack already has the right ingestion seam in `BioStack.KnowledgeWorker`. The structural failure is that the canonicalizer flattens rich `stackIntelligence` into pathway strings, the runtime then *recomputes* relationship verdicts from those strings against two narrow hardcoded whitelists, and `Unknown` collapses into `Redundant`. Move to a precomputed, versioned, reviewed `CompoundRelationship` table; let the runtime *look up* verdicts; never compute them from raw pathways at request time. Use LLMs offline to propose relationships with citations, gated by human review. Defer SLM training. Phase 0 is a five-PR hotfix; Phase 1 is the schema; Phase 5 is the runtime cutover behind a flag. Total effort: ~7–9 weeks of focused work for a real intelligence supply chain.
