# Protocol Analyzer Premium Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild `/tools/analyzer` as a guided three-stage experience (input → analyzing → report takeover) with comprehensive goals built on the existing `lib/goals.ts` taxonomy, an optional profile-aware context panel, and the 1,586-line monolith split into focused components.

**Architecture:** Frontend-first. Small backend contract addition (`SecondaryGoals`) lands first so the frontend has a real API to target. The monolith `ProtocolAnalyzerExperience.tsx` is decomposed into `components/tools/analyzer/` with a thin orchestrator owning a `stage` state machine. All existing analytics events, trust copy, and tier gating are preserved verbatim.

**Tech Stack:** Next.js 16 (see `frontend/AGENTS.md` — read `node_modules/next/dist/docs/` before using Next APIs), React 19, Tailwind 4, framer-motion (already a dependency), vitest + @testing-library/react, .NET 8 minimal APIs, xUnit.

**Spec:** `docs/superpowers/specs/2026-06-12-analyzer-premium-redesign-design.md`

---

## Agent selection guide (token efficiency)

Each task carries an **Agent** annotation: the cheapest Claude Code agent tier that can execute it correctly.

| Tier | Use for | In this plan |
|---|---|---|
| `haiku` | Mechanical edits where the plan supplies complete code or exact moves; no design judgment | Tasks 4, 7, 11, 13, 16 |
| `sonnet` | Standard TDD implementation, component extraction, test migration | Tasks 1, 2, 3, 5, 6, 8, 9, 10, 12 |
| `opus` | High-risk integration where many pieces must compose correctly in one pass | Task 14 (orchestrator swap), Task 15 (test migration sweep) |

Dispatch via `Agent` tool with `model` override (e.g. `subagent_type: "general-purpose", model: "haiku"`). Every subagent prompt must include: the task text from this plan, the spec path, and the repo conventions (`pnpm` in `frontend/`, `dotnet test` in `backend/`).

**Verification commands** (used throughout):
- Frontend tests: `cd D:\Repos\BioStack\frontend; pnpm test -- <pattern>` (vitest run)
- Frontend typecheck: `cd D:\Repos\BioStack\frontend; pnpm exec tsc --noEmit`
- Backend tests: `cd D:\Repos\BioStack\backend; dotnet test --filter "FullyQualifiedName~<TestClass>"`

---

## File structure

```
backend/src/BioStack.Contracts/Requests/AnalyzeProtocolRequest.cs        (modify: + SecondaryGoals)
backend/src/BioStack.Application/Services/ProtocolAnalysisModels.cs      (modify: AnalysisContext + SecondaryGoals)
backend/src/BioStack.Application/Services/ProtocolNormalizationService.cs(modify: BuildAnalysisContext signature)
backend/src/BioStack.Application/Services/ProtocolAnalyzerService.cs     (modify: thread SecondaryGoals)
backend/src/BioStack.Application/Services/ProtocolFingerprintService.cs  (modify: GetAnalysisKey)
backend/src/BioStack.Application/Services/CounterfactualCandidateService.cs (modify: combined alignment)
backend/src/BioStack.Api/Endpoints/AnalyzeEndpoints.cs                   (modify: form parsing)
backend/tests/BioStack.Application.Tests/Services/AnalyzerSecondaryGoalsTests.cs (create)
backend/tests/BioStack.Application.Tests/Services/AnalyzerGoalVocabularyTests.cs (create)

frontend/src/lib/analyzerGoals.ts                                        (create: token mapping)
frontend/src/lib/analyzerAnalytics.ts                                    (modify: + 4 events)
frontend/src/lib/api.ts                                                  (modify: analyzeProtocol payload)
frontend/src/components/tools/analyzer/useAnalyzerSession.ts             (create: v4 session + migration)
frontend/src/components/tools/analyzer/AnalyzerGoalPicker.tsx            (create)
frontend/src/components/tools/analyzer/RefineAnalysisPanel.tsx           (create)
frontend/src/components/tools/analyzer/InputStage.tsx                    (create: extracted)
frontend/src/components/tools/analyzer/AnalyzingState.tsx                (create: extracted + skeleton)
frontend/src/components/tools/analyzer/ReportSummaryBar.tsx              (create)
frontend/src/components/tools/analyzer/report/ScoreHero.tsx              (create: arc gauge)
frontend/src/components/tools/analyzer/report/FindingsSection.tsx        (create: extracted, consolidated)
frontend/src/components/tools/analyzer/report/ParsedProtocolSection.tsx  (create: extracted, collapsible)
frontend/src/components/tools/analyzer/report/ComparisonSection.tsx      (create: extracted)
frontend/src/components/tools/analyzer/report/AlternativeScenarios.tsx   (create: extracted)
frontend/src/components/tools/analyzer/report/NextSteps.tsx              (create: extracted + nudge)
frontend/src/components/tools/analyzer/analyzerView.ts                   (create: shared pure helpers)
frontend/src/components/tools/analyzer/AnalyzerExperience.tsx            (create: orchestrator)
frontend/src/components/tools/ProtocolAnalyzerExperience.tsx             (delete in Task 14)
frontend/src/app/tools/analyzer/page.tsx                                 (modify: new import)

frontend/src/__tests__/lib/analyzerGoals.test.ts                         (create)
frontend/src/__tests__/components/analyzer/*.test.tsx                    (create, replaces monolith test)
frontend/src/__tests__/components/ProtocolAnalyzerExperience.test.tsx    (delete in Task 15)
```

**Source-of-truth note:** `frontend/src/components/tools/ProtocolAnalyzerExperience.tsx` (the monolith) is the source for all extracted JSX and helpers. Tasks below reference it as **MONOLITH** with line numbers as of commit `b5547b8`. Do not delete it until Task 14.

---

## Phase 1 — Backend contract

### Task 1: `SecondaryGoals` through the analyze pipeline

**Agent:** `sonnet` — multi-file threading with cache-correctness implications.

**Files:**
- Modify: `backend/src/BioStack.Contracts/Requests/AnalyzeProtocolRequest.cs`
- Modify: `backend/src/BioStack.Application/Services/ProtocolAnalysisModels.cs:19-27`
- Modify: `backend/src/BioStack.Application/Services/ProtocolNormalizationService.cs:58-73,164`
- Modify: `backend/src/BioStack.Application/Services/ProtocolAnalyzerService.cs:86`
- Modify: `backend/src/BioStack.Application/Services/ProtocolFingerprintService.cs:54-65`
- Modify: `backend/src/BioStack.Api/Endpoints/AnalyzeEndpoints.cs:106-118`
- Test: `backend/tests/BioStack.Application.Tests/Services/AnalyzerSecondaryGoalsTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using Xunit;

public sealed class AnalyzerSecondaryGoalsTests
{
    private static ProtocolNormalizationService CreateNormalizationService()
    {
        // Match the constructor usage in ProtocolAnalyzerServiceTests — if the
        // service has dependencies, copy the arrangement from that file.
        return new ProtocolNormalizationService();
    }

    [Fact]
    public void BuildAnalysisContext_NormalizesSecondaryGoals()
    {
        var service = CreateNormalizationService();

        var context = service.BuildAnalysisContext(
            "healing", new[] { " Fat Loss ", "fat loss", "", "energy" }, null, null, null, null);

        Assert.Equal(new List<string> { "energy", "fat loss" }, context.SecondaryGoals);
    }

    [Fact]
    public void BuildAnalysisContext_NullSecondaryGoals_YieldsEmptyList()
    {
        var service = CreateNormalizationService();

        var context = service.BuildAnalysisContext("healing", null, null, null, null, null);

        Assert.Empty(context.SecondaryGoals);
    }

    [Fact]
    public void GetAnalysisKey_DiffersWhenSecondaryGoalsDiffer()
    {
        var fingerprint = new ProtocolFingerprintService();
        var service = CreateNormalizationService();
        var protocol = new NormalizedProtocol(new List<NormalizedCompound>());
        // If NormalizedProtocol construction differs, copy the arrangement from
        // ProtocolFingerprintService usages in existing tests.

        var without = service.BuildAnalysisContext("healing", null, null, null, null, null);
        var with = service.BuildAnalysisContext("healing", new[] { "energy" }, null, null, null, null);

        Assert.NotEqual(
            fingerprint.GetAnalysisKey(protocol, without),
            fingerprint.GetAnalysisKey(protocol, with));
    }
}
```

Note: the existing 5-arg `BuildAnalysisContext(goal, sex, age, weight, existingStackContext)` becomes 6-arg with `secondaryGoals` as the **second** parameter. Adjust the test arrangements above to match the real constructors found in `ProtocolAnalyzerServiceTests.cs` / `ProtocolAnalyzerCachingTests.cs` — those files show how to instantiate `ProtocolNormalizationService` and `NormalizedProtocol` correctly.

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd D:\Repos\BioStack\backend; dotnet test --filter "FullyQualifiedName~AnalyzerSecondaryGoalsTests"`
Expected: compilation failure (no 6-arg overload, no `SecondaryGoals` member) — that counts as red.

- [ ] **Step 3: Implement**

`AnalyzeProtocolRequest.cs` — add parameter before `ExistingStackContext`:

```csharp
public sealed record AnalyzeProtocolRequest(
    ProtocolInputType InputType = ProtocolInputType.Paste,
    string? InputText = null,
    string? LinkUrl = null,
    string? SourceName = null,
    string? Goal = null,
    string? Sex = null,
    int? Age = null,
    double? Weight = null,
    int? MaxCompounds = null,
    List<string>? RequiredCompoundIds = null,
    List<string>? ExcludedCompoundIds = null,
    List<string>? ExistingStackContext = null,
    List<string>? SecondaryGoals = null)
```

(Appending last keeps every existing positional construction compiling. Check call sites: `AnalyzeEndpoints.cs:106` uses positional args — it gains a final `null` until Step 4.)

`ProtocolAnalysisModels.cs` — `AnalysisContext` gains `SecondaryGoals` after `Goal`:

```csharp
public sealed record AnalysisContext(
    string Goal,
    List<string> SecondaryGoals,
    string Sex,
    string AgeBand,
    string WeightBand,
    List<string> ExistingStackContext,
    string ParserVersion,
    string KnowledgeVersion,
    string ScoringVersion);
```

`ProtocolNormalizationService.cs` — signature + normalization (lowercase, trim, drop blanks, distinct, ordered):

```csharp
public AnalysisContext BuildAnalysisContext(
    string? goal,
    IEnumerable<string>? secondaryGoals,
    string? sex,
    int? age,
    double? weight,
    IEnumerable<string>? existingStackContext)
{
    return new AnalysisContext(
        goal?.Trim() ?? string.Empty,
        secondaryGoals?
            .Select(item => item?.Trim().ToLowerInvariant() ?? string.Empty)
            .Where(item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>(),
        sex?.Trim() ?? string.Empty,
        ToAgeBand(age),
        ToWeightBand(weight),
        existingStackContext?.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList()
            ?? new List<string>(),
        ProtocolFingerprintService.ParserVersion,
        ProtocolFingerprintService.KnowledgeVersion,
        ProtocolFingerprintService.ScoringVersion);
}
```

Update the interface at line 164 to match.

`ProtocolAnalyzerService.cs:86`:

```csharp
var analysisContext = _normalizationService.BuildAnalysisContext(request.Goal, request.SecondaryGoals, request.Sex, request.Age, request.Weight, request.ExistingStackContext);
```

`ProtocolFingerprintService.cs` — `GetAnalysisKey` contextKey gains the secondary goals (cache correctness — a cached analysis must not be reused across different secondary goals):

```csharp
var contextKey = string.Join(":",
    NormalizeToken(context.Goal),
    string.Join(",", context.SecondaryGoals.Select(NormalizeToken)),
    NormalizeToken(context.Sex),
    NormalizeToken(context.AgeBand),
    NormalizeToken(context.WeightBand),
    string.Join(",", context.ExistingStackContext.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).Select(NormalizeToken)));
```

`AnalyzeEndpoints.cs` multipart branch (line ~106) — parse the new form field and pass it positionally (plus the trailing slot added to the record):

```csharp
var analyzeRequest = new AnalyzeProtocolRequest(
    inputType,
    null,
    null,
    file.FileName,
    EmptyToNull(form["goal"]),
    EmptyToNull(form["sex"]),
    ParseNullableInt(form["age"]),
    ParseNullableDouble(form["weight"]),
    ParseNullableInt(form["maxCompounds"]),
    null,
    null,
    ParseStringList(form["existingStackContext"]),
    ParseStringList(form["secondaryGoals"]));
```

with helper (in the same static class):

```csharp
private static List<string>? ParseStringList(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }

    var items = value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    return items.Count > 0 ? items : null;
}
```

(Newline-delimited form values; the frontend joins with `\n` in Task 6. This also fixes the pre-existing gap where multipart uploads couldn't carry `existingStackContext`.)

Fix any other `BuildAnalysisContext` call sites the compiler finds (search: `BuildAnalysisContext(`) by inserting `null` as the second argument.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd D:\Repos\BioStack\backend; dotnet test --filter "FullyQualifiedName~AnalyzerSecondaryGoalsTests"`
Expected: 3 passed.

- [ ] **Step 5: Run the full backend suite (catches positional-arg and cache-test fallout)**

Run: `cd D:\Repos\BioStack\backend; dotnet test`
Expected: all green. `ProtocolAnalyzerCachingTests` is the likely place a context-shape change surfaces — fix arrangements there, never the production cache logic.

- [ ] **Step 6: Commit**

```bash
git add backend/
git commit -m "feat(analyzer): thread SecondaryGoals through analyze pipeline and cache key"
```

---

### Task 2: Combined goal alignment (primary 1.0, secondary 0.5)

**Agent:** `sonnet`

**Files:**
- Modify: `backend/src/BioStack.Application/Services/CounterfactualCandidateService.cs`
- Test: `backend/tests/BioStack.Application.Tests/Services/AnalyzerSecondaryGoalsTests.cs` (extend)

- [ ] **Step 1: Read the current implementation**

Read `CounterfactualCandidateService.cs` fully. Key facts from prior exploration: `GoalAlignment(KnowledgeEntry candidate, string goal)` is a private static returning `benefitHits + pathwayHits * 0.5 + mechanismHits * 0.25`-style score (exact formula at lines ~87-100); `GetSwapCandidatesAsync` calls `GoalAlignment(candidate, context.Goal)` at line ~34; `GetGoalCandidatesAsync(string goal, ...)` filters on `> 0.2` at line ~55.

- [ ] **Step 2: Write the failing test** (append to `AnalyzerSecondaryGoalsTests.cs`)

```csharp
[Fact]
public void CombinedGoalAlignment_WeightsSecondaryAtHalf()
{
    // Use a KnowledgeEntry whose benefits match ONLY the secondary goal.
    var entry = new KnowledgeEntry
    {
        CanonicalName = "TestCompound",
        Benefits = new List<string> { "weight loss" },
        Pathways = new List<string>(),
        MechanismSummary = string.Empty,
    };
    // Copy required-member initialization from LocalKnowledgeSource.cs entries
    // if KnowledgeEntry has more required members.

    var primaryOnly = CounterfactualCandidateService.CombinedGoalAlignment(entry, "healing", new List<string>());
    var withSecondary = CounterfactualCandidateService.CombinedGoalAlignment(entry, "healing", new List<string> { "weight loss" });

    Assert.Equal(0d, primaryOnly);
    Assert.True(withSecondary > 0d);

    var directPrimary = CounterfactualCandidateService.CombinedGoalAlignment(entry, "weight loss", new List<string>());
    Assert.Equal(directPrimary * 0.5d, withSecondary, precision: 10);
}
```

- [ ] **Step 3: Run to verify it fails**

Run: `cd D:\Repos\BioStack\backend; dotnet test --filter "FullyQualifiedName~CombinedGoalAlignment_WeightsSecondaryAtHalf"`
Expected: compile error — `CombinedGoalAlignment` does not exist.

- [ ] **Step 4: Implement**

In `CounterfactualCandidateService`, add an `internal static` method and route the context-bearing call sites through it:

```csharp
internal static double CombinedGoalAlignment(KnowledgeEntry candidate, string goal, IReadOnlyList<string> secondaryGoals)
{
    var combined = GoalAlignment(candidate, goal);
    foreach (var secondary in secondaryGoals)
    {
        combined += 0.5d * GoalAlignment(candidate, secondary);
    }

    return combined;
}
```

At the `GetSwapCandidatesAsync` call site (~line 34) replace `GoalAlignment(candidate, context.Goal)` with `CombinedGoalAlignment(candidate, context.Goal, context.SecondaryGoals)`. Leave `GetGoalCandidatesAsync(string goal, ...)` untouched — it is invoked per-goal by `CounterfactualEngine.BuildGoalAwareOptionsAsync` with `context.Goal` and operates on a single goal by design.

Make the test compile: if `InternalsVisibleTo` for the test assembly isn't already configured in `BioStack.Application.csproj`, check how other tests reach internals (search `InternalsVisibleTo` in `backend/src/BioStack.Application`); it exists for `ProtocolAnalyzerServiceTests` to test services — if not, add `[assembly: InternalsVisibleTo("BioStack.Application.Tests")]` consistent with project conventions.

- [ ] **Step 5: Run tests, then full suite**

Run: `cd D:\Repos\BioStack\backend; dotnet test`
Expected: all green.

- [ ] **Step 6: Commit**

```bash
git add backend/
git commit -m "feat(analyzer): weight secondary goals at 0.5 in swap candidate alignment"
```

---

### Task 3: Goal vocabulary guard test

**Agent:** `sonnet` — requires iterating token strings against the real knowledge source until they match.

**Files:**
- Create: `backend/tests/BioStack.Application.Tests/Services/AnalyzerGoalVocabularyTests.cs`

- [ ] **Step 1: Write the guard test**

The token map below MUST stay byte-identical to the one shipped in Task 4's `frontend/src/lib/analyzerGoals.ts`. The test header documents that pairing.

```csharp
namespace BioStack.Application.Tests.Services;

using BioStack.Infrastructure.Knowledge;
using Xunit;

/// <summary>
/// Guards the analyzer goal token vocabulary. Mirrors
/// frontend/src/lib/analyzerGoals.ts — keep the two lists byte-identical.
/// Every token string the analyzer can send as a goal must match at least one
/// knowledge entry's benefits, pathways, or mechanism summary; otherwise the
/// goal silently scores zero alignment (CounterfactualCandidateService.GoalAlignment).
/// </summary>
public sealed class AnalyzerGoalVocabularyTests
{
    // Primary (category) tokens — keys are GOAL_CATEGORIES keys.
    public static TheoryData<string, string> CategoryTokens => new()
    {
        { "recovery", "healing injury recovery tissue repair" },
        { "energy", "energy metabolic health" },
        { "cognitive", "cognitive enhancement" },
        { "longevity", "anti-aging cellular repair longevity" },
        { "performance", "performance muscle endurance recovery" },
        { "skin", "skin collagen anti-aging" },
        { "organ", "gut health cardiovascular organ health" },
    };

    // Secondary (specific-goal) tokens — keys are GOAL_DEFINITIONS ids.
    public static TheoryData<string, string> GoalTokens => new()
    {
        { "recovery-muscles", "tissue repair muscle joint tendon" },
        { "recovery-inflammation", "reduced inflammation" },
        { "recovery-injury", "injury recovery healing" },
        { "recovery-post-workout", "recovery healing" },
        { "energy-levels", "energy" },
        { "energy-mitochondrial", "cellular energy mitochondrial" },
        { "energy-metabolic", "metabolic health insulin sensitivity" },
        { "energy-fat-loss", "fat loss weight loss" },
        { "cognitive-focus", "cognitive enhancement focus" },
        { "cognitive-memory", "cognitive enhancement memory" },
        { "cognitive-performance", "cognitive enhancement" },
        { "cognitive-neuro-health", "cognitive neurological health" },
        { "longevity-aging", "anti-aging" },
        { "longevity-cellular", "DNA repair cellular repair" },
        { "longevity-pathways", "longevity anti-aging" },
        { "performance-endurance", "endurance energy" },
        { "performance-strength", "muscle strength" },
        { "performance-training", "recovery energy" },
        { "skin-elasticity", "skin elasticity collagen" },
        { "skin-appearance", "skin anti-aging" },
        { "skin-collagen", "collagen skin" },
        { "organ-health", "organ health liver kidney" },
        { "organ-gut", "gut health" },
        { "organ-cardiovascular", "cardiovascular heart" },
    };

    [Theory]
    [MemberData(nameof(CategoryTokens))]
    public void CategoryTokens_MatchAtLeastOneKnowledgeEntry(string key, string tokens)
        => AssertTokensMatchKnowledge(key, tokens);

    [Theory]
    [MemberData(nameof(GoalTokens))]
    public void GoalTokens_MatchAtLeastOneKnowledgeEntry(string key, string tokens)
        => AssertTokensMatchKnowledge(key, tokens);

    private static void AssertTokensMatchKnowledge(string key, string tokens)
    {
        var source = new LocalKnowledgeSource();
        var entries = source.GetAllEntriesAsync(CancellationToken.None).GetAwaiter().GetResult();
        // ^ Adjust to the actual LocalKnowledgeSource API — see how
        // ProtocolAnalyzerServiceTests obtains knowledge entries and copy that.

        var goalTokens = tokens.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var matched = entries.Any(entry =>
            entry.Benefits.Any(benefit => goalTokens.Any(token => benefit.Contains(token, StringComparison.OrdinalIgnoreCase)))
            || entry.Pathways.Any(pathway => goalTokens.Any(token => pathway.Contains(token, StringComparison.OrdinalIgnoreCase)))
            || goalTokens.Any(token => entry.MechanismSummary.Contains(token, StringComparison.OrdinalIgnoreCase)));

        Assert.True(matched, $"Goal '{key}' tokens '{tokens}' match no knowledge entry benefits/pathways/mechanisms — it would silently score zero alignment. Adjust the tokens here AND in frontend/src/lib/analyzerGoals.ts.");
    }
}
```

- [ ] **Step 2: Run and iterate**

Run: `cd D:\Repos\BioStack\backend; dotnet test --filter "FullyQualifiedName~AnalyzerGoalVocabularyTests"`

Expected: several failures on first run — the local seed (`LocalKnowledgeSource.cs`) covers a handful of compounds. For each failing goal: inspect the seed's `Benefits`/`Pathways`/`MechanismSummary` strings and adjust the token string until at least one token genuinely matches, **keeping tokens user-meaningful** (they describe the goal, not the database). If a goal truly has zero knowledge coverage (plausible for e.g. `organ-cardiovascular` in a small seed), add the matching benefit term to the relevant existing seed entry ONLY if accurate; otherwise widen the token string with an adjacent term that is accurate (e.g. `cardiovascular heart blood`). Record final strings — Task 4 must copy them exactly.

- [ ] **Step 3: Run full backend suite**

Run: `cd D:\Repos\BioStack\backend; dotnet test`
Expected: all green.

- [ ] **Step 4: Commit**

```bash
git add backend/tests/
git commit -m "test(analyzer): guard goal token vocabulary against knowledge base"
```

---

## Phase 2 — Frontend foundation

### Task 4: `analyzerGoals.ts` token mapping module

**Agent:** `haiku` — complete code provided; only adjustment is syncing token strings with Task 3's final values.

**Files:**
- Create: `frontend/src/lib/analyzerGoals.ts`
- Test: `frontend/src/__tests__/lib/analyzerGoals.test.ts`

- [ ] **Step 1: Write the failing test**

```typescript
import { describe, expect, it } from 'vitest';
import {
  ANALYZER_CATEGORY_TOKENS,
  ANALYZER_GOAL_TOKENS,
  buildAnalyzerGoalPayload,
  prefillFromProfileGoals,
} from '@/lib/analyzerGoals';
import { GOAL_CATEGORIES, GOAL_DEFINITIONS } from '@/lib/goals';

describe('analyzerGoals', () => {
  it('has a token string for every category', () => {
    for (const category of GOAL_CATEGORIES) {
      expect(ANALYZER_CATEGORY_TOKENS[category.key], `missing tokens for ${category.key}`).toBeTruthy();
    }
  });

  it('has a token string for every active goal definition', () => {
    for (const goal of GOAL_DEFINITIONS.filter((g) => g.isActive)) {
      expect(ANALYZER_GOAL_TOKENS[goal.id], `missing tokens for ${goal.id}`).toBeTruthy();
    }
  });

  it('builds an empty payload for the no-goal state', () => {
    expect(buildAnalyzerGoalPayload(null, [])).toEqual({ goal: '', secondaryGoals: [] });
  });

  it('maps primary category and refinements to API tokens', () => {
    const payload = buildAnalyzerGoalPayload('energy', ['energy-fat-loss']);
    expect(payload.goal).toBe(ANALYZER_CATEGORY_TOKENS['energy']);
    expect(payload.secondaryGoals).toEqual([ANALYZER_GOAL_TOKENS['energy-fat-loss']]);
  });

  it('ignores refinements without a known token', () => {
    expect(buildAnalyzerGoalPayload('energy', ['nonexistent']).secondaryGoals).toEqual([]);
  });

  it('prefills primary category and refinement from profile goal ids', () => {
    expect(prefillFromProfileGoals(['energy-fat-loss', 'cognitive-focus'])).toEqual({
      primaryCategory: 'energy',
      refinementGoalIds: ['energy-fat-loss'],
    });
  });

  it('prefills nothing from empty profile goals', () => {
    expect(prefillFromProfileGoals([])).toEqual({ primaryCategory: null, refinementGoalIds: [] });
  });
});
```

- [ ] **Step 2: Run to verify failure**

Run: `cd D:\Repos\BioStack\frontend; pnpm test -- analyzerGoals`
Expected: FAIL — module not found.

- [ ] **Step 3: Implement**

```typescript
import { GOAL_DEFINITIONS } from './goals';

// API token strings sent as the analyzer `goal` / `secondaryGoals` values.
// The backend token-matches these against knowledge-base benefits, pathways,
// and mechanism summaries (CounterfactualCandidateService.GoalAlignment).
// MIRRORED by backend/tests/.../AnalyzerGoalVocabularyTests.cs — keep both
// lists byte-identical. Copy final strings from that test after Task 3.
export const ANALYZER_CATEGORY_TOKENS: Record<string, string> = {
  recovery: 'healing injury recovery tissue repair',
  energy: 'energy metabolic health',
  cognitive: 'cognitive enhancement',
  longevity: 'anti-aging cellular repair longevity',
  performance: 'performance muscle endurance recovery',
  skin: 'skin collagen anti-aging',
  organ: 'gut health cardiovascular organ health',
};

export const ANALYZER_GOAL_TOKENS: Record<string, string> = {
  'recovery-muscles': 'tissue repair muscle joint tendon',
  'recovery-inflammation': 'reduced inflammation',
  'recovery-injury': 'injury recovery healing',
  'recovery-post-workout': 'recovery healing',
  'energy-levels': 'energy',
  'energy-mitochondrial': 'cellular energy mitochondrial',
  'energy-metabolic': 'metabolic health insulin sensitivity',
  'energy-fat-loss': 'fat loss weight loss',
  'cognitive-focus': 'cognitive enhancement focus',
  'cognitive-memory': 'cognitive enhancement memory',
  'cognitive-performance': 'cognitive enhancement',
  'cognitive-neuro-health': 'cognitive neurological health',
  'longevity-aging': 'anti-aging',
  'longevity-cellular': 'DNA repair cellular repair',
  'longevity-pathways': 'longevity anti-aging',
  'performance-endurance': 'endurance energy',
  'performance-strength': 'muscle strength',
  'performance-training': 'recovery energy',
  'skin-elasticity': 'skin elasticity collagen',
  'skin-appearance': 'skin anti-aging',
  'skin-collagen': 'collagen skin',
  'organ-health': 'organ health liver kidney',
  'organ-gut': 'gut health',
  'organ-cardiovascular': 'cardiovascular heart',
};

export type AnalyzerGoalSelection = {
  primaryCategory: string | null;
  refinementGoalIds: string[];
};

export function buildAnalyzerGoalPayload(
  primaryCategory: string | null,
  refinementGoalIds: string[],
): { goal: string; secondaryGoals: string[] } {
  if (!primaryCategory) {
    return { goal: '', secondaryGoals: [] };
  }

  return {
    goal: ANALYZER_CATEGORY_TOKENS[primaryCategory] ?? '',
    secondaryGoals: refinementGoalIds
      .map((id) => ANALYZER_GOAL_TOKENS[id])
      .filter((tokens): tokens is string => Boolean(tokens)),
  };
}

export function prefillFromProfileGoals(profileGoalIds: string[]): AnalyzerGoalSelection {
  const first = profileGoalIds
    .map((id) => GOAL_DEFINITIONS.find((goal) => goal.id === id))
    .find((goal) => goal !== undefined);

  if (!first) {
    return { primaryCategory: null, refinementGoalIds: [] };
  }

  return { primaryCategory: first.category, refinementGoalIds: [first.id] };
}
```

**IMPORTANT:** after Task 3 lands, copy its final token strings here verbatim (the guard test is the authority).

- [ ] **Step 4: Run tests**

Run: `cd D:\Repos\BioStack\frontend; pnpm test -- analyzerGoals`
Expected: 7 passed.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/lib/analyzerGoals.ts frontend/src/__tests__/lib/analyzerGoals.test.ts
git commit -m "feat(analyzer): goal token mapping over existing goals taxonomy"
```

---

### Task 5: `useAnalyzerSession` hook — v4 schema + migration

**Agent:** `sonnet` — stateful hook with migration logic.

**Files:**
- Create: `frontend/src/components/tools/analyzer/useAnalyzerSession.ts`
- Test: `frontend/src/__tests__/components/analyzer/useAnalyzerSession.test.ts`

- [ ] **Step 1: Write the failing tests**

```typescript
import { describe, expect, it, beforeEach } from 'vitest';
import {
  migrateV3Snapshot,
  readAnalyzerSessionSnapshot,
  STORAGE_KEY_V3,
  STORAGE_KEY_V4,
} from '@/components/tools/analyzer/useAnalyzerSession';

describe('analyzer session v4', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it('migrates a v3 goal string to a primary category', () => {
    expect(migrateV3Snapshot({ goal: 'healing' }).goals.primaryCategory).toBe('recovery');
    expect(migrateV3Snapshot({ goal: 'fat loss' }).goals).toEqual({
      primaryCategory: 'energy',
      refinementGoalIds: ['energy-fat-loss'],
    });
    expect(migrateV3Snapshot({ goal: 'longevity' }).goals.primaryCategory).toBe('longevity');
    expect(migrateV3Snapshot({ goal: '' }).goals.primaryCategory).toBeNull();
    expect(migrateV3Snapshot({ goal: 'something else' }).goals.primaryCategory).toBeNull();
  });

  it('carries forward v3 input fields and result', () => {
    const migrated = migrateV3Snapshot({
      mode: 'Link',
      inputText: 'BPC-157 500mcg daily',
      linkUrl: 'https://example.com/doc.pdf',
      result: null,
    });
    expect(migrated.mode).toBe('Link');
    expect(migrated.inputText).toBe('BPC-157 500mcg daily');
    expect(migrated.linkUrl).toBe('https://example.com/doc.pdf');
    expect(migrated.context).toEqual({ sex: '', age: '', weight: '', existingStack: '' });
  });

  it('reads v4 from storage, falling back to migrated v3', () => {
    window.localStorage.setItem(STORAGE_KEY_V3, JSON.stringify({ goal: 'healing', inputText: 'x' }));
    const snapshot = readAnalyzerSessionSnapshot();
    expect(snapshot.goals.primaryCategory).toBe('recovery');
    expect(snapshot.inputText).toBe('x');
  });

  it('prefers v4 over v3 when both exist', () => {
    window.localStorage.setItem(STORAGE_KEY_V3, JSON.stringify({ goal: 'healing' }));
    window.localStorage.setItem(
      STORAGE_KEY_V4,
      JSON.stringify({ goals: { primaryCategory: 'cognitive', refinementGoalIds: [] } }),
    );
    expect(readAnalyzerSessionSnapshot().goals.primaryCategory).toBe('cognitive');
  });

  it('returns a clean default snapshot when storage is empty or corrupt', () => {
    window.localStorage.setItem(STORAGE_KEY_V4, '{not json');
    const snapshot = readAnalyzerSessionSnapshot();
    expect(snapshot.goals).toEqual({ primaryCategory: null, refinementGoalIds: [] });
    expect(snapshot.result).toBeNull();
  });
});
```

- [ ] **Step 2: Run to verify failure**

Run: `cd D:\Repos\BioStack\frontend; pnpm test -- useAnalyzerSession`
Expected: FAIL — module not found.

- [ ] **Step 3: Implement**

```typescript
'use client';

import { useEffect, useState } from 'react';
import type { ProtocolAnalyzerInputType, ProtocolAnalyzerResult } from '@/lib/types';
import type { AnalyzerGoalSelection } from '@/lib/analyzerGoals';

export const STORAGE_KEY_V3 = 'biostack.analyzer.session.v3';
export const STORAGE_KEY_V4 = 'biostack.analyzer.session.v4';

export type AnalyzerContextFields = {
  sex: string;
  age: string;
  weight: string;
  existingStack: string;
};

export type AnalyzerSessionSnapshot = {
  mode: ProtocolAnalyzerInputType;
  inputText: string;
  linkUrl: string;
  goals: AnalyzerGoalSelection;
  context: AnalyzerContextFields;
  result: ProtocolAnalyzerResult | null;
};

type V3Snapshot = {
  mode?: ProtocolAnalyzerInputType;
  inputText?: string;
  linkUrl?: string;
  goal?: string;
  result?: ProtocolAnalyzerResult | null;
};

const EMPTY_CONTEXT: AnalyzerContextFields = { sex: '', age: '', weight: '', existingStack: '' };

function defaultSnapshot(): AnalyzerSessionSnapshot {
  return {
    mode: 'Paste',
    inputText: '',
    linkUrl: '',
    goals: { primaryCategory: null, refinementGoalIds: [] },
    context: { ...EMPTY_CONTEXT },
    result: null,
  };
}

// v3 stored the goal as one of '', 'healing', 'fat loss', 'longevity'.
const V3_GOAL_TO_SELECTION: Record<string, AnalyzerGoalSelection> = {
  healing: { primaryCategory: 'recovery', refinementGoalIds: [] },
  'fat loss': { primaryCategory: 'energy', refinementGoalIds: ['energy-fat-loss'] },
  longevity: { primaryCategory: 'longevity', refinementGoalIds: [] },
};

export function migrateV3Snapshot(v3: V3Snapshot): AnalyzerSessionSnapshot {
  return {
    ...defaultSnapshot(),
    mode: v3.mode ?? 'Paste',
    inputText: v3.inputText ?? '',
    linkUrl: v3.linkUrl ?? '',
    goals: V3_GOAL_TO_SELECTION[v3.goal ?? ''] ?? { primaryCategory: null, refinementGoalIds: [] },
    result: v3.result ?? null,
  };
}

export function readAnalyzerSessionSnapshot(): AnalyzerSessionSnapshot {
  if (typeof window === 'undefined') {
    return defaultSnapshot();
  }

  try {
    const v4 = window.localStorage.getItem(STORAGE_KEY_V4);
    if (v4) {
      const parsed = JSON.parse(v4) as Partial<AnalyzerSessionSnapshot>;
      return {
        ...defaultSnapshot(),
        ...parsed,
        goals: parsed.goals ?? { primaryCategory: null, refinementGoalIds: [] },
        context: { ...EMPTY_CONTEXT, ...parsed.context },
      };
    }

    const v3 = window.localStorage.getItem(STORAGE_KEY_V3);
    if (v3) {
      return migrateV3Snapshot(JSON.parse(v3) as V3Snapshot);
    }
  } catch {
    // fall through to default
  }

  return defaultSnapshot();
}

export function useAnalyzerSession() {
  const [snapshot, setSnapshot] = useState<AnalyzerSessionSnapshot>(defaultSnapshot);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    const id = window.setTimeout(() => {
      setSnapshot(readAnalyzerSessionSnapshot());
      setLoaded(true);
    }, 0);
    return () => window.clearTimeout(id);
  }, []);

  useEffect(() => {
    if (!loaded) {
      return;
    }
    try {
      window.localStorage.setItem(STORAGE_KEY_V4, JSON.stringify(snapshot));
    } catch {
      // storage quota or privacy mode — session persistence is best-effort
    }
  }, [snapshot, loaded]);

  return { snapshot, setSnapshot, loaded };
}
```

(The deferred-`setTimeout` restore preserves the monolith's hydration-safety pattern at MONOLITH:107-119.)

- [ ] **Step 4: Run tests**

Run: `cd D:\Repos\BioStack\frontend; pnpm test -- useAnalyzerSession`
Expected: 5 passed.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/tools/analyzer/ frontend/src/__tests__/components/analyzer/
git commit -m "feat(analyzer): v4 session hook with v3 migration"
```

---

### Task 6: Extend `apiClient.analyzeProtocol` payload

**Agent:** `sonnet` — touches both JSON and FormData paths plus existing api tests.

**Files:**
- Modify: `frontend/src/lib/api.ts:332-375`
- Test: `frontend/src/__tests__/lib/api.test.ts` (extend — read its existing analyzeProtocol coverage first and follow its mocking pattern)

- [ ] **Step 1: Write failing tests** (follow the file's existing fetch-mock pattern; add cases)

Test cases to add:
1. JSON path: `analyzeProtocol({ inputType: 'Paste', inputText: 'x', goal: 'healing', secondaryGoals: ['fat loss'], sex: 'male', age: 40, weight: 90, existingStackContext: ['creatine'] })` → request body includes all new fields.
2. FormData path: same fields with a `file` → form fields `secondaryGoals` (newline-joined), `sex`, `age`, `weight`, `existingStackContext` (newline-joined) are appended; absent fields are not appended.

- [ ] **Step 2: Run to verify failure**

Run: `cd D:\Repos\BioStack\frontend; pnpm test -- api.test`

- [ ] **Step 3: Implement**

Extend the payload type and both branches:

```typescript
async analyzeProtocol(payload: {
  inputType?: 'Paste' | 'FileUpload' | 'CameraScan' | 'Link';
  inputText?: string;
  linkUrl?: string;
  sourceName?: string;
  file?: File;
  goal?: string;
  secondaryGoals?: string[];
  sex?: string;
  age?: number;
  weight?: number;
  existingStackContext?: string[];
  maxCompounds?: number;
}): Promise<ProtocolAnalyzerResult> {
```

In the FormData branch (after the existing `goal`/`maxCompounds` appends):

```typescript
if (payload.secondaryGoals?.length) {
  formData.append('secondaryGoals', payload.secondaryGoals.join('\n'));
}
if (payload.sex) {
  formData.append('sex', payload.sex);
}
if (payload.age) {
  formData.append('age', String(payload.age));
}
if (payload.weight) {
  formData.append('weight', String(payload.weight));
}
if (payload.existingStackContext?.length) {
  formData.append('existingStackContext', payload.existingStackContext.join('\n'));
}
```

The JSON branch already serializes the whole payload object (`JSON.stringify(payload)`); the new fields flow through automatically — the test proves it.

- [ ] **Step 4: Run tests**

Run: `cd D:\Repos\BioStack\frontend; pnpm test -- api.test`
Expected: all pass, including pre-existing cases.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/lib/api.ts frontend/src/__tests__/lib/api.test.ts
git commit -m "feat(analyzer): send goals + context fields from api client"
```

---

### Task 7: New analytics events

**Agent:** `haiku` — additive union-type change, complete code below.

**Files:**
- Modify: `frontend/src/lib/analyzerAnalytics.ts:1-13`

- [ ] **Step 1: Add the four event names to the union**

```typescript
export type AnalyzerAnalyticsEvent =
  | 'analyzer_viewed'
  | 'analyzer_input_mode_selected'
  | 'analyzer_analysis_started'
  | 'analyzer_result_viewed'
  | 'analyzer_score_visible'
  | 'analyzer_why_section_viewed'
  | 'analyzer_comparison_viewed'
  | 'analyzer_unlock_clicked'
  | 'analyzer_save_clicked'
  | 'analyzer_convert_clicked'
  | 'analyzer_example_loaded'
  | 'analyzer_scan_selected'
  | 'analyzer_goal_selected'
  | 'analyzer_context_opened'
  | 'analyzer_context_prefilled'
  | 'analyzer_profile_nudge_clicked';
```

- [ ] **Step 2: Typecheck**

Run: `cd D:\Repos\BioStack\frontend; pnpm exec tsc --noEmit`
Expected: clean.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/lib/analyzerAnalytics.ts
git commit -m "feat(analyzer): add goal and context analytics events"
```

---

## Phase 3 — Components

**Shared conventions for all Phase 3 tasks:** components live in `frontend/src/components/tools/analyzer/`; tests in `frontend/src/__tests__/components/analyzer/`. Match the monolith's Tailwind vocabulary (`rounded-lg border border-white/[0.08] bg-white/[0.03]`, emerald accents). Use framer-motion for animation and gate every animation behind `useReducedMotion()` from framer-motion. Test render pattern: copy the provider/mocking setup from `frontend/src/__tests__/components/ProtocolAnalyzerExperience.test.tsx` (read it first — it shows how `useAuth`, `apiClient`, and `next/navigation` are mocked).

### Task 8: `AnalyzerGoalPicker` — category primary + refine

**Agent:** `sonnet`

**Files:**
- Create: `frontend/src/components/tools/analyzer/AnalyzerGoalPicker.tsx`
- Test: `frontend/src/__tests__/components/analyzer/AnalyzerGoalPicker.test.tsx`

- [ ] **Step 1: Write failing tests**

Behaviors to assert (render with plain props, no providers needed):
1. Renders a "Not sure yet" chip plus one chip per `GOAL_CATEGORIES` entry (8 total).
2. No refinement chips visible when `selection.primaryCategory` is null.
3. With `primaryCategory: 'energy'`, the 4 energy goals from `GOAL_DEFINITIONS` render as refinement chips.
4. Clicking a category chip calls `onChange` with `{ primaryCategory: 'energy', refinementGoalIds: [] }` (switching primary clears refinements).
5. Clicking a refinement chip toggles it into `refinementGoalIds`; a third selection is ignored (max 2).
6. Clicking "Not sure yet" calls `onChange` with `{ primaryCategory: null, refinementGoalIds: [] }`.

```tsx
import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { AnalyzerGoalPicker } from '@/components/tools/analyzer/AnalyzerGoalPicker';
import { GOAL_CATEGORIES } from '@/lib/goals';

const noSelection = { primaryCategory: null, refinementGoalIds: [] };

describe('AnalyzerGoalPicker', () => {
  it('renders Not sure yet plus every category', () => {
    render(<AnalyzerGoalPicker selection={noSelection} onChange={() => {}} />);
    expect(screen.getByRole('button', { name: 'Not sure yet' })).toBeInTheDocument();
    for (const category of GOAL_CATEGORIES) {
      expect(screen.getByRole('button', { name: category.label })).toBeInTheDocument();
    }
  });

  it('hides refinements until a primary is chosen', () => {
    render(<AnalyzerGoalPicker selection={noSelection} onChange={() => {}} />);
    expect(screen.queryByText('Refine (optional)')).not.toBeInTheDocument();
  });

  it('selects a primary category and clears refinements', () => {
    const onChange = vi.fn();
    render(
      <AnalyzerGoalPicker
        selection={{ primaryCategory: 'recovery', refinementGoalIds: ['recovery-injury'] }}
        onChange={onChange}
      />,
    );
    fireEvent.click(screen.getByRole('button', { name: 'Energy & Metabolism' }));
    expect(onChange).toHaveBeenCalledWith({ primaryCategory: 'energy', refinementGoalIds: [] });
  });

  it('shows refinement goals for the selected category and toggles up to two', () => {
    const onChange = vi.fn();
    render(
      <AnalyzerGoalPicker
        selection={{ primaryCategory: 'energy', refinementGoalIds: ['energy-levels', 'energy-fat-loss'] }}
        onChange={onChange}
      />,
    );
    expect(screen.getByText('Refine (optional)')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /Enhance mitochondrial function/ }));
    expect(onChange).not.toHaveBeenCalled(); // max 2 — ignored

    fireEvent.click(screen.getByRole('button', { name: /Improve energy levels/ }));
    expect(onChange).toHaveBeenCalledWith({ primaryCategory: 'energy', refinementGoalIds: ['energy-fat-loss'] });
  });

  it('resets to no goal', () => {
    const onChange = vi.fn();
    render(
      <AnalyzerGoalPicker selection={{ primaryCategory: 'energy', refinementGoalIds: [] }} onChange={onChange} />,
    );
    fireEvent.click(screen.getByRole('button', { name: 'Not sure yet' }));
    expect(onChange).toHaveBeenCalledWith({ primaryCategory: null, refinementGoalIds: [] });
  });
});
```

- [ ] **Step 2: Run to verify failure**

Run: `cd D:\Repos\BioStack\frontend; pnpm test -- AnalyzerGoalPicker`

- [ ] **Step 3: Implement**

```tsx
'use client';

import { GOAL_CATEGORIES, getGoalsByCategory } from '@/lib/goals';
import type { AnalyzerGoalSelection } from '@/lib/analyzerGoals';
import { trackAnalyzerEvent } from '@/lib/analyzerAnalytics';

const MAX_REFINEMENTS = 2;

export function AnalyzerGoalPicker({
  selection,
  onChange,
}: {
  selection: AnalyzerGoalSelection;
  onChange: (selection: AnalyzerGoalSelection) => void;
}) {
  const goalsByCategory = getGoalsByCategory();
  const refinements = selection.primaryCategory
    ? goalsByCategory.get(selection.primaryCategory) ?? []
    : [];

  function selectPrimary(categoryKey: string | null) {
    onChange({ primaryCategory: categoryKey, refinementGoalIds: [] });
    trackAnalyzerEvent('analyzer_goal_selected', { goal: categoryKey ?? 'none', isPrimary: true });
  }

  function toggleRefinement(goalId: string) {
    const current = selection.refinementGoalIds;
    if (current.includes(goalId)) {
      onChange({ ...selection, refinementGoalIds: current.filter((id) => id !== goalId) });
      return;
    }
    if (current.length >= MAX_REFINEMENTS) {
      return;
    }
    onChange({ ...selection, refinementGoalIds: [...current, goalId] });
    trackAnalyzerEvent('analyzer_goal_selected', { goal: goalId, isPrimary: false });
  }

  return (
    <div>
      <p className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-white/42">Goal</p>
      <div className="flex flex-wrap gap-2">
        <GoalChip
          label="Not sure yet"
          selected={selection.primaryCategory === null}
          onClick={() => selectPrimary(null)}
        />
        {GOAL_CATEGORIES.map((category) => (
          <GoalChip
            key={category.key}
            label={category.label}
            selected={selection.primaryCategory === category.key}
            onClick={() => selectPrimary(category.key)}
          />
        ))}
      </div>

      {refinements.length > 0 && (
        <div className="mt-4">
          <p className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-white/42">
            Refine (optional)
          </p>
          <div className="flex flex-wrap gap-2">
            {refinements.map((goal) => (
              <button
                key={goal.id}
                type="button"
                onClick={() => toggleRefinement(goal.id)}
                className={`rounded-full border px-3 py-1.5 text-left text-sm transition-colors ${
                  selection.refinementGoalIds.includes(goal.id)
                    ? 'border-emerald-300/45 bg-emerald-400/14 text-emerald-100'
                    : 'border-white/10 text-white/65 hover:border-white/20 hover:text-white'
                }`}
              >
                {goal.name}
              </button>
            ))}
          </div>
          <p className="mt-2 text-xs leading-5 text-white/38">
            Pick up to {MAX_REFINEMENTS}. Refinements sharpen the goal-aware alternatives.
          </p>
        </div>
      )}
    </div>
  );
}

function GoalChip({ label, selected, onClick }: { label: string; selected: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`rounded-full border px-4 py-2 text-sm font-semibold transition-colors ${
        selected
          ? 'border-emerald-300/45 bg-emerald-400/14 text-emerald-100'
          : 'border-white/10 text-white/65 hover:border-white/20 hover:text-white'
      }`}
    >
      {label}
    </button>
  );
}
```

- [ ] **Step 4: Run tests**

Run: `cd D:\Repos\BioStack\frontend; pnpm test -- AnalyzerGoalPicker`
Expected: 5 passed.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/tools/analyzer/AnalyzerGoalPicker.tsx frontend/src/__tests__/components/analyzer/AnalyzerGoalPicker.test.tsx
git commit -m "feat(analyzer): category-primary goal picker with refinements"
```

---

### Task 9: `RefineAnalysisPanel` — context fields, profile prefill, nudge

**Agent:** `sonnet`

**Files:**
- Create: `frontend/src/components/tools/analyzer/RefineAnalysisPanel.tsx`
- Test: `frontend/src/__tests__/components/analyzer/RefineAnalysisPanel.test.tsx`

**Contract:**

```tsx
type RefineAnalysisPanelProps = {
  context: AnalyzerContextFields;            // from useAnalyzerSession
  onChange: (context: AnalyzerContextFields) => void;
  profile: PersonProfile | null;             // first profile of signed-in user, or null
  isAuthenticated: boolean;
};
```

**Behavior:**
- Renders a collapsed disclosure: button "Refine analysis (optional)" with helper copy "Add context to sharpen scoring. Nothing here is required." Clicking expands and fires `trackAnalyzerEvent('analyzer_context_opened')` (first open only).
- Expanded fields: Sex (`select`: empty / `male` / `female`), Age (`input type="number"`, min 18 max 120), Weight (`input type="number"` + suffix `kg`), Current medications or stack (`textarea`, one item per line — split on newlines into `existingStack` lines preserved as the raw string in state; the orchestrator splits when building the payload).
- If `profile` is non-null and the panel's fields are all empty on mount: prefill `sex`/`age`/`weight` from the profile via `onChange`, render a "From your profile" badge, and fire `trackAnalyzerEvent('analyzer_context_prefilled')` once. Edits affect only the panel state (never write back to the profile); render a `Link` to `/profiles` labeled "Edit profile".
- If `isAuthenticated` is false OR `profile` is null: render the nudge — "Create a profile to autofill this and track your results over time." as a `Link` to `/auth/signin?callbackUrl=/tools/analyzer` (anonymous) or `/profiles` (signed in, no profile), firing `trackAnalyzerEvent('analyzer_profile_nudge_clicked')` on click.

- [ ] **Step 1: Write failing tests** — assert: collapsed by default (fields absent); expands on click; prefills from profile and shows badge; shows sign-in nudge when anonymous; shows create-profile nudge when authenticated without profile; typing in age calls `onChange` with updated context. Mock `next/link` the same way the monolith test does.

- [ ] **Step 2: Run to verify failure**

Run: `cd D:\Repos\BioStack\frontend; pnpm test -- RefineAnalysisPanel`

- [ ] **Step 3: Implement** per the contract above. Keep styling consistent: panel = `rounded-lg border border-white/10 bg-black/20 p-3`; inputs = the monolith's input classes (MONOLITH:483, `min-h-12 w-full rounded-lg border border-white/10 bg-[#0F141B] px-4 text-sm text-white ...`).

- [ ] **Step 4: Run tests**

Run: `cd D:\Repos\BioStack\frontend; pnpm test -- RefineAnalysisPanel`

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/tools/analyzer/RefineAnalysisPanel.tsx frontend/src/__tests__/components/analyzer/RefineAnalysisPanel.test.tsx
git commit -m "feat(analyzer): refine-analysis context panel with profile prefill and nudge"
```

---

### Task 10: `InputStage` + shared view helpers

**Agent:** `sonnet` — careful extraction, no behavior change.

**Files:**
- Create: `frontend/src/components/tools/analyzer/analyzerView.ts`
- Create: `frontend/src/components/tools/analyzer/InputStage.tsx`
- Test: `frontend/src/__tests__/components/analyzer/InputStage.test.tsx`

- [ ] **Step 1: Create `analyzerView.ts`** — move these pure helpers verbatim from MONOLITH (they are shared by input, report, and orchestrator): `getScoreBand` (1379), `getScoreLabel` (1370), `getScoreInsight` (1388), `getWhatThisMeans` (1433), `recommendationCount` (1454), `formatDelta` (1463), `pickOptimizedProtocol` (1468) + the `OptimizedProtocolView` type (54), `currentRawInput` (1504), `formatAnalyzerError` (1516), `sourceTypeLabel` (1542), `confidenceLabel` (1559), `formatDose` (1571), `unique` (1579), `goalText` (1583), `buildAnalyzerFindings` (1285) + `AnalyzerFinding` type (69), `findingLabelForIssue` (1334), `buildParserWarnings` (1349), `scoreSummary` (1364), and the `exampleProtocols` constant (27). Export everything the components need. No logic edits.

- [ ] **Step 2: Create `InputStage.tsx`** — extract from MONOLITH: mode tab row (395-418), mobile scan button (419-425), per-mode panels — Paste textarea (432-448), `UploadPanel` (731-773) for Upload (450-459) and Scan (461-473), Link input (475-491) — trust line (493-495), accepted-files card (533-537), action row Analyze/Clear (541-556, drop the Save button — saving moves to the report's NextSteps), failure state `AnalyzerFailureState` (1163-1180), and example links (576-585 as compact links below the actions). Replace the old goal column (498-531) with `<AnalyzerGoalPicker />` and add `<RefineAnalysisPanel />` below it. Props:

```tsx
type InputStageProps = {
  mode: ProtocolAnalyzerInputType;
  inputText: string;
  linkUrl: string;
  selectedFile: File | null;
  goals: AnalyzerGoalSelection;
  context: AnalyzerContextFields;
  profile: PersonProfile | null;
  isAuthenticated: boolean;
  isPending: boolean;
  error: string;
  onModeChange: (mode: ProtocolAnalyzerInputType) => void;
  onInputTextChange: (text: string) => void;
  onLinkUrlChange: (url: string) => void;
  onFileSelected: (file: File | null) => void;
  onGoalsChange: (goals: AnalyzerGoalSelection) => void;
  onContextChange: (context: AnalyzerContextFields) => void;
  onAnalyze: () => void;
  onClear: () => void;
  onLoadExample: (example: 'healing' | 'fatLoss' | 'longevity') => void;
  onScanRequested: () => void;
};
```

Keep all `trackAnalyzerEvent` calls exactly where they fire today (mode select 404-407, scan 284-286). The file input refs stay inside `InputStage`; `onScanRequested` is invoked by the orchestrator-driven mobile flow the same way `selectScanModeAndOpenCamera` (280-287) works today — move that function's body into `InputStage` and call the `onModeChange` prop inside it.

Layout: single column. Order: mode tabs → mode panel → trust line → `AnalyzerGoalPicker` → `RefineAnalysisPanel` → action row → example links → failure state.

- [ ] **Step 3: Write tests** — assert: all four mode tabs render and switch panels; Analyze disabled when paste text empty (replicate `analyzeDisabled` logic, MONOLITH:368-372); clicking Analyze calls `onAnalyze`; example link calls `onLoadExample('healing')`; error prop renders the failure card with Try again wired to `onAnalyze`.

- [ ] **Step 4: Run tests + typecheck**

Run: `cd D:\Repos\BioStack\frontend; pnpm test -- InputStage; pnpm exec tsc --noEmit`

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/tools/analyzer/ frontend/src/__tests__/components/analyzer/
git commit -m "feat(analyzer): extract input stage with goal picker and context panel"
```

---

### Task 11: `AnalyzingState` — progress + skeleton

**Agent:** `haiku` — complete structure specified; mostly verbatim extraction plus presentational additions.

**Files:**
- Create: `frontend/src/components/tools/analyzer/AnalyzingState.tsx`

- [ ] **Step 1: Implement** — move `AnalyzerProgressCard` (MONOLITH:1007-1030) including its per-mode `progressSteps` arrays. Upgrade: wrap in framer-motion, each step's number circle transitions to an emerald check as a timer advances one step every 900ms (purely cosmetic pacing; analysis completes whenever the API returns). Gate animation behind `useReducedMotion()` — when reduced, render the static list exactly as today. Below the progress card, render a skeleton report: three pulsing placeholder cards (`animate-pulse rounded-lg border border-white/[0.06] bg-white/[0.02]`, heights 160px / 240px / 120px).

```tsx
'use client';

import { useEffect, useState } from 'react';
import { motion, useReducedMotion } from 'framer-motion';
import type { ProtocolAnalyzerInputType } from '@/lib/types';

const STEP_INTERVAL_MS = 900;

export function AnalyzingState({ mode }: { mode: ProtocolAnalyzerInputType }) {
  const reducedMotion = useReducedMotion();
  const steps = progressStepsFor(mode);
  const [completed, setCompleted] = useState(0);

  useEffect(() => {
    if (reducedMotion) {
      return;
    }
    const id = window.setInterval(
      () => setCompleted((current) => Math.min(current + 1, steps.length - 1)),
      STEP_INTERVAL_MS,
    );
    return () => window.clearInterval(id);
  }, [reducedMotion, steps.length]);

  return (
    <div className="space-y-4">
      <section className="rounded-lg border border-white/10 bg-black/20 p-4" aria-live="polite">
        <p className="text-sm font-semibold text-white">Analysis in progress</p>
        <ul className="mt-3 space-y-2 text-sm text-white/62">
          {steps.map((step, index) => {
            const done = !reducedMotion && index < completed;
            return (
              <li key={step} className="flex items-center gap-3">
                <motion.span
                  animate={done ? { scale: [1, 1.15, 1] } : {}}
                  className={`inline-flex h-6 w-6 items-center justify-center rounded-full border text-xs ${
                    done
                      ? 'border-emerald-300/45 bg-emerald-400/14 text-emerald-100'
                      : 'border-white/10 text-white/75'
                  }`}
                >
                  {done ? '✓' : index + 1}
                </motion.span>
                <span className={done ? 'text-white/85' : undefined}>{step}</span>
              </li>
            );
          })}
        </ul>
      </section>
      <div className="space-y-4" aria-hidden="true">
        <div className="h-40 animate-pulse rounded-lg border border-white/[0.06] bg-white/[0.02]" />
        <div className="h-60 animate-pulse rounded-lg border border-white/[0.06] bg-white/[0.02]" />
        <div className="h-28 animate-pulse rounded-lg border border-white/[0.06] bg-white/[0.02]" />
      </div>
    </div>
  );
}

function progressStepsFor(mode: ProtocolAnalyzerInputType): string[] {
  // Copy the four per-mode arrays verbatim from MONOLITH AnalyzerProgressCard (1008-1015).
  ...
}
```

- [ ] **Step 2: Typecheck**

Run: `cd D:\Repos\BioStack\frontend; pnpm exec tsc --noEmit`

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/tools/analyzer/AnalyzingState.tsx
git commit -m "feat(analyzer): animated analyzing state with report skeleton"
```

---

### Task 12: Report components

**Agent:** `sonnet` — six related components; extraction plus the new ScoreHero gauge. Dispatch as ONE subagent task (the components share imports from `analyzerView.ts` and are reviewed together).

**Files:**
- Create: `frontend/src/components/tools/analyzer/report/ScoreHero.tsx`
- Create: `frontend/src/components/tools/analyzer/report/FindingsSection.tsx`
- Create: `frontend/src/components/tools/analyzer/report/ParsedProtocolSection.tsx`
- Create: `frontend/src/components/tools/analyzer/report/ComparisonSection.tsx`
- Create: `frontend/src/components/tools/analyzer/report/AlternativeScenarios.tsx`
- Create: `frontend/src/components/tools/analyzer/report/NextSteps.tsx`
- Test: `frontend/src/__tests__/components/analyzer/ScoreHero.test.tsx`, `.../NextSteps.test.tsx`

- [ ] **Step 1: `ScoreHero.tsx`** — new component replacing the score sidebar card (MONOLITH:611-638) and `WhatThisMeansPanel` (851-858). Props: `{ result: ProtocolAnalyzerResult; scoreInsight: string; whatThisMeans: string }`. Layout: SVG arc gauge (radius 64, stroke 10, 270° sweep) where the arc fills proportionally to score with framer-motion `animate={{ pathLength: score / 100 }}` (duration 1.2s, ease-out; static at full value under `useReducedMotion`); a count-up number (framer-motion `animate` on a motion value, rendered via `useTransform` rounding; static under reduced motion); band color = emerald-400 ≥80 / amber-400 ≥60 / red-400 below (same thresholds as `scoreTone`, MONOLITH:160-165); `getScoreLabel` text beside it; `scoreInsight` sentence; "Why this score?" toggle revealing the four `ScoreChip`s (move `ScoreChip` 1262-1283 into this file, preserving `HelpTip` keys `synergy`/`redundancy`/`interference`); `whatThisMeans` in an emerald callout below (copy classes from 853). Keep `aria-label={\`BioStack score ${score} out of 100\`}` on the gauge group and render the numeric score as text (not only SVG).

Test: renders score text, band label, insight; toggle reveals the four chips.

- [ ] **Step 2: `FindingsSection.tsx`** — consolidates `ExtractionNotesPanel` (775-831), `TrustMetric` (833-840), `ArtifactPreview` (842-849), `FindingList` (1129-1161), and `ResultList` (1109-1127) into one section titled "What BioStack found": confidence strip (3 `TrustMetric`s) on top, findings list (from `buildAnalyzerFindings`), then parser/extraction notes and the extracted-text toggle. Props: `{ result; showExtractedText; onToggleExtractedText }`. All copy verbatim from the monolith.

- [ ] **Step 3: `ParsedProtocolSection.tsx`** — move 1032-1107 verbatim, then wrap the table in a disclosure: header row shows "Parsed Protocol · N items" + blend badge; body collapsed by default when the protocol has more than 6 entries, expanded otherwise.

- [ ] **Step 4: `ComparisonSection.tsx`** — move `OriginalVsOptimizedSection` (871-901), `ProtocolComparisonList` (903-931), and `WhyBetterBlocks` (933-993) verbatim. One export: `ComparisonSection({ result, optimized })` rendering OriginalVsOptimized with the WhyBetter blocks beneath.

- [ ] **Step 5: `AlternativeScenarios.tsx`** — move the section at 642-672 with `ImprovementCard` (1182-1216), `SimplifiedProtocolCard` (1218-1238), `GoalAwareCard` (1240-1260). Props: `{ result; optimized }`; compute `primaryRemoval`/`primarySwap`/`simplified`/`goalAware` from `result.counterfactuals` inside (logic from 167-171). Render nothing when `!hasMeaningfulImprovement` (rule + comment from 180-185 — keep the comment).

- [ ] **Step 6: `NextSteps.tsx`** — from the CTA section (674-704) plus save notice (569-573): Save Analysis, Convert to BioStack Protocol, Unlock full analysis (the `Link` to `/pricing?intent=analyzer` with `onUnlockClicked`), save-notice line, and — for `!isAuthenticated || !hasProfile` — the create-profile nudge line reused from Task 9's copy. Props: `{ result; savedAnalysisId; showSaveNotice; isAuthenticated; hasProfile; onSave; onConvert; onUnlockClicked }`.

Test: all three CTAs render; Save disabled without result is N/A (NextSteps only renders in report stage); clicking each fires its callback; nudge appears only when `hasProfile` is false.

- [ ] **Step 7: Run tests + typecheck**

Run: `cd D:\Repos\BioStack\frontend; pnpm test -- analyzer; pnpm exec tsc --noEmit`

- [ ] **Step 8: Commit**

```bash
git add frontend/src/components/tools/analyzer/report/ frontend/src/__tests__/components/analyzer/
git commit -m "feat(analyzer): report stage components with animated score hero"
```

---

### Task 13: `ReportSummaryBar`

**Agent:** `haiku` — small presentational component, complete code below.

**Files:**
- Create: `frontend/src/components/tools/analyzer/ReportSummaryBar.tsx`

- [ ] **Step 1: Implement**

```tsx
'use client';

import type { ProtocolAnalyzerResult } from '@/lib/types';
import { GOAL_CATEGORIES } from '@/lib/goals';
import { sourceTypeLabel } from './analyzerView';

export function ReportSummaryBar({
  result,
  primaryCategory,
  onEdit,
}: {
  result: ProtocolAnalyzerResult;
  primaryCategory: string | null;
  onEdit: () => void;
}) {
  const goalLabel = primaryCategory
    ? GOAL_CATEGORIES.find((category) => category.key === primaryCategory)?.label ?? 'Goal set'
    : 'No goal selected';

  return (
    <div className="sticky top-16 z-10 flex flex-wrap items-center justify-between gap-3 rounded-lg border border-white/10 bg-[#0B1118]/95 px-4 py-3 backdrop-blur">
      <p className="text-sm text-white/72">
        <span className="font-semibold text-white">{sourceTypeLabel(result)}</span>
        <span className="text-white/40"> · </span>
        {result.protocol.length} compound{result.protocol.length === 1 ? '' : 's'}
        <span className="text-white/40"> · </span>
        {goalLabel}
      </p>
      <button
        type="button"
        onClick={onEdit}
        className="rounded-lg border border-white/10 px-3 py-1.5 text-sm font-semibold text-white/72 transition-colors hover:border-white/20 hover:text-white"
      >
        Edit
      </button>
    </div>
  );
}
```

- [ ] **Step 2: Typecheck, commit**

Run: `cd D:\Repos\BioStack\frontend; pnpm exec tsc --noEmit`

```bash
git add frontend/src/components/tools/analyzer/ReportSummaryBar.tsx
git commit -m "feat(analyzer): sticky report summary bar"
```

---

### Task 14: `AnalyzerExperience` orchestrator + page swap + delete monolith

**Agent:** `opus` — highest-risk integration: stage machine, session, analytics parity, tier gating, and the page swap must all compose correctly.

**Files:**
- Create: `frontend/src/components/tools/analyzer/AnalyzerExperience.tsx`
- Modify: `frontend/src/app/tools/analyzer/page.tsx:3,15`
- Delete: `frontend/src/components/tools/ProtocolAnalyzerExperience.tsx`

- [ ] **Step 1: Implement the orchestrator.** It owns:

**State:** `useAnalyzerSession()` for `{ mode, inputText, linkUrl, goals, context, result }`; local state for `selectedFile`, `error`, `isPending` (`useTransition`), `showSaveNotice`, `savedAnalysisId`, `showExtractedText`, `stage`. Stage is **derived with an override**: `stage = isPending ? 'analyzing' : editing ? 'input' : result ? 'report' : 'input'` where `editing` is a boolean set true by ReportSummaryBar's `onEdit` and reset false on the next successful analysis. A restored session with a `result` therefore lands on the report stage (preserves today's restore behavior, MONOLITH:107-119).

**Profile:** fetch once on mount when authenticated — `apiClient.getProfiles()` → first profile or null; never throw (catch → null). Pass to `InputStage`/`NextSteps`. Compute the goal prefill: if no stored goals and the profile has goals (`profile.goals` ids, else `getMockProfileGoalIds(profile.id)`), apply `prefillFromProfileGoals`.

**Analysis call** (adapted from `runAnalysis`, MONOLITH:196-235):

```tsx
const { goal, secondaryGoals } = buildAnalyzerGoalPayload(goals.primaryCategory, goals.refinementGoalIds);
const contextPayload = {
  sex: context.sex || undefined,
  age: context.age ? Number(context.age) : undefined,
  weight: context.weight ? Number(context.weight) : undefined,
  existingStackContext: context.existingStack
    ? context.existingStack.split('\n').map((line) => line.trim()).filter(Boolean)
    : undefined,
};
```

merged into the existing payload shapes for Paste/Link/file (keep `maxCompounds: FREE_TIER_COMPOUND_LIMIT`). `analyzer_analysis_started` keeps firing with `{ inputType, goal, exampleType }` where `goal` is the **token string** (same field the old code sent).

**Behavior parity checklist** (each item maps to monolith code — port exactly):
- result-effect analytics quartet (132-158): `analyzer_result_viewed`, `analyzer_score_visible`, `analyzer_why_section_viewed`, `analyzer_comparison_viewed`
- mount event `analyzer_viewed` with `hasRestoredResult` (187-194)
- `loadExample` (260-278): sets mode/goals (healing→`{recovery,[]}`, fatLoss→`{energy,['energy-fat-loss']}`, longevity→`{longevity,[]}`), fires `analyzer_example_loaded` with the legacy goal strings (`healing`/`fat loss`/`longevity`), runs analysis
- `saveAnalysisLocally` (289-311) and `convertToProtocol` (313-349) verbatim including the signin redirect and `/protocol-console` push
- `onUnlockClicked` (351-359)
- `clearInput` (248-258) plus reset `goals`/`context`? **No** — Clear resets inputs and result but PRESERVES goals and context (they're user identity, not input)
- mobile sticky CTA (708-726) with its exact tier branching
- `premiumLocked = Boolean(result)` (179)

**Render:**

```tsx
<main className={`mx-auto max-w-3xl px-4 pt-8 sm:px-6 lg:px-8 ${result ? 'pb-40 md:pb-28' : 'pb-28'}`}>
  {/* header section from MONOLITH 376-390, verbatim copy */}
  {stage === 'input' && <InputStage ... />}
  {stage === 'analyzing' && <AnalyzingState mode={mode} />}
  {stage === 'report' && result && (
    <div className="space-y-5">
      <ReportSummaryBar result={result} primaryCategory={goals.primaryCategory} onEdit={() => setEditing(true)} />
      <ScoreHero result={result} scoreInsight={scoreInsight} whatThisMeans={whatThisMeans} />
      <FindingsSection result={result} showExtractedText={showExtractedText} onToggleExtractedText={...} />
      <ParsedProtocolSection result={result} />
      {optimizedProtocol && <ComparisonSection result={result} optimized={optimizedProtocol} />}
      <AlternativeScenarios result={result} optimized={optimizedProtocol} />
      <ShareableSummaryStub />   {/* move 860-869 into AnalyzerExperience or NextSteps file */}
      <NextSteps ... />
    </div>
  )}
  {/* mobile sticky CTA, 708-726 */}
</main>
```

Single column `max-w-3xl` — the report takeover replaces the old `xl:grid-cols-[1.02fr_0.98fr]` split.

- [ ] **Step 2: Swap the page import** in `frontend/src/app/tools/analyzer/page.tsx`:

```tsx
import { AnalyzerExperience } from '@/components/tools/analyzer/AnalyzerExperience';
```

and render `<AnalyzerExperience />`. Metadata unchanged.

- [ ] **Step 3: Check for other importers, then delete the monolith**

Run: `cd D:\Repos\BioStack\frontend; pnpm exec grep -r "ProtocolAnalyzerExperience" src --include="*.tsx" --include="*.ts" -l` (or use ripgrep). Expected importers: the page (now swapped) and the old test file (deleted in Task 15). Delete `frontend/src/components/tools/ProtocolAnalyzerExperience.tsx`.

- [ ] **Step 4: Typecheck + targeted tests**

Run: `cd D:\Repos\BioStack\frontend; pnpm exec tsc --noEmit; pnpm test -- analyzer`
Expected: typecheck clean; new analyzer tests pass. The old monolith test file will fail (its import is gone) — that is Task 15's job; run it with the old file temporarily excluded if needed: `pnpm test -- --exclude "**/ProtocolAnalyzerExperience.test.tsx"` is NOT a vitest flag — instead just note the failure and proceed to Task 15 in the same review cycle.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/tools/ frontend/src/app/tools/analyzer/page.tsx
git commit -m "feat(analyzer): three-stage experience orchestrator, replace monolith"
```

---

## Phase 4 — Test migration & verification

### Task 15: Migrate the monolith test suite

**Agent:** `opus` — requires judgment about which of ~1,000+ lines of assertions map where, and which represent behavior that intentionally changed.

**Files:**
- Read: `frontend/src/__tests__/components/ProtocolAnalyzerExperience.test.tsx` (full)
- Create: `frontend/src/__tests__/components/analyzer/AnalyzerExperience.test.tsx`
- Delete: `frontend/src/__tests__/components/ProtocolAnalyzerExperience.test.tsx`
- Check: `frontend/src/__tests__/conversion/launchSafetyCopy.test.ts` (must keep passing untouched)

- [ ] **Step 1: Inventory the old test file.** For each `it(...)` block, classify: (a) ports directly (same behavior, new render target `<AnalyzerExperience />`), (b) moved to a component test in Tasks 8-12 (drop here, verify covered), (c) behavior intentionally changed by the redesign (rewrite against the new flow — e.g. results now render in a report stage, the goal buttons are categories). Produce the list in the task report.

- [ ] **Step 2: Write `AnalyzerExperience.test.tsx`** covering at minimum:
- session restore: seeded v4 snapshot with result → report stage visible
- v3 snapshot seeded → migrated, goal category selected
- analyze happy path: paste → Analyze → mocked `apiClient.analyzeProtocol` resolves → score hero shows score; payload included `goal` tokens, `secondaryGoals`, context fields
- analyze failure → failure card + retry
- example load fires `analyzer_example_loaded` and analysis
- Edit from report returns to input with text intact; result still present after Edit (no data loss)
- unlock click fires `analyzer_unlock_clicked`
- convert: unauthenticated → `/auth/signin?callbackUrl=/protocol-console` push
- mount fires `analyzer_viewed`

Reuse the old file's mock setup (apiClient, AuthProvider, next/navigation, analytics event listener pattern) — copy it, don't reinvent it.

- [ ] **Step 3: Delete the old test file. Run the FULL frontend suite.**

Run: `cd D:\Repos\BioStack\frontend; pnpm test`
Expected: all green, including `launchSafetyCopy.test.ts` and the untouched `ProtocolComponents`/`DashboardComponents` suites. Any failure in an untouched suite means the redesign broke a shared dependency — fix forward, do not modify unrelated tests to pass.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/__tests__/
git commit -m "test(analyzer): migrate monolith suite to staged experience"
```

---

### Task 16: Full verification sweep

**Agent:** `haiku` — runs commands and reports; escalate failures to `sonnet`.

- [ ] **Step 1: Frontend**: `cd D:\Repos\BioStack\frontend; pnpm test; pnpm exec tsc --noEmit; pnpm lint` — all clean.
- [ ] **Step 2: Backend**: `cd D:\Repos\BioStack\backend; dotnet test` — all green.
- [ ] **Step 3: Build**: `cd D:\Repos\BioStack\frontend; pnpm build` — succeeds (Next 16; if build errors reference changed Next APIs, consult `node_modules/next/dist/docs/` per frontend/AGENTS.md).
- [ ] **Step 4: Manual smoke (if a dev environment is available)**: load `/tools/analyzer`, run the healing example, confirm: staged progress → report takeover → score gauge animates → Edit returns to input → goals show categories → Refine appears after picking a category → context panel collapses/expands.
- [ ] **Step 5: Commit any stragglers; report status.**

---

## Self-review notes (already applied)

- Spec §1-§7 each map to tasks: flow/IA → 10/11/13/14, goals → 3/4/8, context → 9 (+6 transport), visual → 11/12, components → 10-14, backend → 1/2/3, analytics/testing → 7/15, error handling → 10/14 (failure state stays in input stage).
- Save button moves from input actions into NextSteps (report-only) — intentional IA change per spec §1; Task 10 notes it so the extraction agent doesn't "fix" it back.
- Clear preserves goals/context (Task 14) — deliberate deviation from old `clearInput`, documented inline.
- Token strings appear in two places by design (Task 3 test = authority, Task 4 copies); both files carry mirror comments.
