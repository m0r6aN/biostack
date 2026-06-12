# Protocol Analyzer Premium Redesign — Design

**Date:** 2026-06-12
**Status:** Approved
**Surface:** `biostack.cc/tools/analyzer` ([frontend/src/app/tools/analyzer/page.tsx](../../../frontend/src/app/tools/analyzer/page.tsx), [frontend/src/components/tools/ProtocolAnalyzerExperience.tsx](../../../frontend/src/components/tools/ProtocolAnalyzerExperience.tsx))

## Problem

The analyzer is BioStack's flagship free acquisition tool, but the experience undersells it:

1. **IA**: input panel, score sidebar, findings grid, parsed table, extraction notes, alternative scenarios, and conversion CTAs all render simultaneously in a two-column grid. Nothing tells the user what to read first; the score — the product's "wow" beat — competes with everything else.
2. **Goals**: four hardcoded options (`Not sure yet`, `Healing`, `Fat loss`, `Longevity`) despite the backend treating goal as free text token-matched against compound benefits/pathways/mechanisms (`CounterfactualCandidateService.GoalAlignment`).
3. **Unused personalization**: the analyze request accepts `Sex`, `Age`, `Weight`, `ExistingStackContext` (`ProtocolAnalyzerService` → `BuildAnalysisContext`), but the UI never collects them and `apiClient.analyzeProtocol` never sends them.
4. **Maintainability**: `ProtocolAnalyzerExperience.tsx` is a 1,586-line monolith.

## Decision summary

Full redesign (Approach A: guided flow with results takeover), elevating the existing dark + emerald visual language. Goals become a 12-goal taxonomy with primary + secondary selection. Personalization context is an optional, collapsed panel — auto-filled from the user's profile when one exists, with a create-profile nudge when it doesn't.

## 1. Page flow & IA

Three stages in one route. No new routing; results continue to live in localStorage.

### Input stage
One focused card:
- Mode tabs: Paste / Upload / Scan / Link (unchanged behavior)
- Goal picker (see §2)
- Collapsed "Refine analysis" context panel (see §3)
- Examples reduced to small "try one" links under the input
- Single primary CTA: **Analyze Protocol**

### Analyzing stage
- Existing staged step list upgraded: steps check off progressively with motion
- Skeleton of the report renders beneath so layout doesn't jump

### Report stage
Input card collapses into a **sticky summary bar**: "Pasted protocol · 5 compounds · Goal: Fat loss · Edit". Edit returns to the input stage with all values preserved.

Report sections, in narrative order:
1. **Score hero** — arc gauge with count-up animation, band label, verdict sentence, expandable "Why this score" (Base / Synergy / Redundancy / Interference chips, existing `HelpTip` keys)
2. **What BioStack found** — findings consolidated with extraction/parser notes; confidence strip (source type · confidence · items inferred)
3. **Parsed protocol** — collapsible table, blend badge preserved
4. **Original vs BioStack alternative** — only when a meaningful improvement exists (same `hasMeaningfulImprovement` rule as today)
5. **Alternative scenarios** — remove-one, swap, simplified, goal-aware cards
6. **Next steps** — Save Analysis / Convert to BioStack Protocol / Unlock full analysis as the narrative conclusion; create-profile nudge for anonymous users

Mobile sticky bottom CTA bar is retained with its existing tier logic (unlock / convert / save).

## 2. Goals system

**Amended 2026-06-12 (pre-plan):** the product already has a canonical goals taxonomy — `frontend/src/lib/goals.ts` defines 7 categories (`GOAL_CATEGORIES`) and 23 specific goals (`GOAL_DEFINITIONS`), with existing `GoalPicker` / `GoalBadge` / `GoalDisplay` components used by profiles, check-ins, and the dashboard. The analyzer reuses this taxonomy instead of introducing a new 12-goal list, so the product keeps one goal vocabulary.

### Taxonomy & selection model: categories + refine
- **Primary** (single-select, filled chip): one of the 7 existing categories — Recovery & Repair, Energy & Metabolism, Cognitive & Neurological, Longevity & Aging, Performance, Skin & Appearance, Organ & System Health — plus "Not sure yet" (valid no-goal state, empty goal string).
- **Refine** (optional, up to 2): picking a primary category reveals its specific goals from `GOAL_DEFINITIONS` as outline chips. Selected refinements become secondary goals at lower scoring weight.
- Category metadata (labels, colors) comes from `GOAL_CATEGORIES`; the analyzer adds a compact picker variant, not a fork of the taxonomy.
- Example buttons map to categories (healing → `recovery`, fat loss → `energy` + `energy-fat-loss` refinement, longevity → `longevity`).

### API token mapping
A new module `frontend/src/lib/analyzerGoals.ts` maps taxonomy keys to API token strings (the backend token-matches the goal string against knowledge-base benefits/pathways/mechanisms):
- Per-category primary tokens (e.g. `recovery` → `"healing injury recovery tissue repair"`)
- Per-specific-goal secondary tokens (e.g. `energy-fat-loss` → `"fat loss weight loss"`)

Token strings are an implementation draft — final values must be validated against the knowledge base by the guard test (below) and adjusted to whatever actually matches.

### Profile prefill
Signed-in users with profile goals (same goal IDs, via `profile.goals` / `getMockProfileGoalIds`) get the first goal's category preselected as primary and the specific goal as a refinement. Fallback: a profile `GoalSummary` matching a category label preselects that category.

### Guard test (backend)
Every category token string and specific-goal token string must match at least one knowledge entry's benefits, pathways, or mechanism summary — same spirit as `StackScoreChipVocabularyTests`. Build fails if a goal would silently score zero alignment. The token map is mirrored in a backend test fixture; the test file documents the frontend module path (`frontend/src/lib/analyzerGoals.ts`) it mirrors.

## 3. Context panel ("Refine analysis")

Collapsed by default, explicitly optional, with copy explaining why the data improves the analysis.

Fields: sex, age, weight (with unit), current medications/stack (free text → `ExistingStackContext`).

- **Signed in with profile**: auto-filled, labeled "From your profile". Edits apply to the current analysis only — no write-back. Link to the profile page for permanent changes.
- **Anonymous / no profile**: empty fields + inline nudge: "Create a profile to autofill this and track your results over time" → sign-in / profile creation flow. This is the conversion hook.
- Values persist in the local session snapshot only.
- `apiClient.analyzeProtocol` payload extended to send sex / age / weight / existingStackContext (backend already accepts them).

## 4. Visual elevation

Stay within the dark + emerald identity; raise the craft:
- Arc-gauge score visualization with count-up and band-color transition (emerald ≥80, amber ≥60, red below — same bands as today)
- Consistent eyebrow/heading typographic scale across all sections
- Staged progress with animated check-offs; skeleton loading for the report
- Chip select/hover micro-interactions; expand/collapse transitions; sticky-bar shadow on scroll
- `prefers-reduced-motion` respected for all animation
- Trust copy preserved verbatim — "Educational analysis only. Verify all dosing math manually." and related strings are asserted by launch-safety copy tests

## 5. Component architecture

`ProtocolAnalyzerExperience.tsx` (1,586 lines) splits into `frontend/src/components/tools/analyzer/`:

```
AnalyzerExperience.tsx        — thin orchestrator: stage state, analysis call, session
InputStage.tsx                — mode tabs + input panels (paste/upload/scan/link)
AnalyzerGoalPicker.tsx        — category-primary + refine selection (reuses lib/goals.ts taxonomy)
RefineAnalysisPanel.tsx       — context fields, profile prefill, profile nudge
AnalyzingState.tsx            — staged progress + report skeleton
ReportSummaryBar.tsx          — sticky collapsed-input bar with Edit
report/
  ScoreHero.tsx
  FindingsSection.tsx
  ParsedProtocolSection.tsx
  ComparisonSection.tsx
  AlternativeScenarios.tsx
  NextSteps.tsx
useAnalyzerSession.ts         — localStorage persistence + migration
```

Plus `frontend/src/lib/analyzerGoals.ts` (API token mapping over the existing `lib/goals.ts` taxonomy).

### Session schema
Storage key bumps `biostack.analyzer.session.v3` → `v4`. New shape stores `{ primaryGoal, secondaryGoals, context: { sex, age, weight, existingStack } }` alongside the existing fields. v3 snapshots migrate on read: old `goal` string maps to the closest taxonomy primary (or is carried as-is if unmatched); absent fields default empty.

## 6. Backend changes

Deliberately small:
- `AnalyzeProtocolRequest`: add optional `SecondaryGoals: List<string>`, threaded through `BuildAnalysisContext` into `AnalysisContext` and the analysis cache key (`ProtocolFingerprintService.GetAnalysisKey`) so cached results can't ignore secondary goals
- `CounterfactualCandidateService`: combined goal alignment — primary goal weight 1.0, each secondary goal 0.5
- Goal-vocabulary guard test (§2)
- No schema or DB changes

## 7. Analytics & testing

### Analytics
All existing analyzer events preserved with unchanged names and payloads (`analyzer_viewed`, `analyzer_analysis_started`, `analyzer_result_viewed`, `analyzer_score_visible`, `analyzer_unlock_clicked`, etc.). New events:
- `analyzer_goal_selected` — `{ goal, isPrimary }`
- `analyzer_context_opened`
- `analyzer_context_prefilled` — fired when profile data auto-fills
- `analyzer_profile_nudge_clicked`

### Testing
- The monolith test file (`ProtocolAnalyzerExperience.test.tsx`) splits alongside the components
- Existing behavioral assertions carry over: session restore, example loading, mode switching, unlock tracking, save/convert flows, failure state + retry
- New coverage: goal picker selection rules (primary/secondary limits), v3→v4 session migration, profile prefill vs nudge branching, report stage Edit returning to input with state intact
- Backend: guard test + `GoalAlignment` weighting unit tests

## Error handling

Unchanged in substance: `AnalyzerFailureState` (retry + calculators-still-work messaging) renders in the input stage. A failed analysis never enters the report stage. The Edit path from report → input must preserve the last successful result until a new analysis replaces it (matching current behavior where `result` clears only on mode switch, clear, or new analysis).

## Out of scope

- Shareable protocol summary (separate planned feature; the stub section can remain in Next steps)
- Provider Review Packet export
- Any change to scoring logic beyond secondary-goal weighting
- Other `/tools/*` calculators (this design's patterns should inform them later)
