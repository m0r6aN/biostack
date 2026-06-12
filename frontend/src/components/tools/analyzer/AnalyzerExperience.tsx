'use client';

import { trackAnalyzerEvent } from '@/lib/analyzerAnalytics';
import { buildAnalyzerGoalPayload, prefillFromProfileGoals } from '@/lib/analyzerGoals';
import type { AnalyzerGoalSelection } from '@/lib/analyzerGoals';
import { saveAnalyzerAnalysis, saveAnalyzerProtocolDraft } from '@/lib/analyzerStorage';
import { apiClient } from '@/lib/api';
import { useAuth } from '@/lib/AuthProvider';
import { getMockProfileGoalIds } from '@/lib/goals';
import { FREE_TIER_COMPOUND_LIMIT } from '@/lib/tiers';
import type { PersonProfile, ProtocolAnalyzerInputType, ProtocolAnalyzerResult } from '@/lib/types';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useEffect, useMemo, useRef, useState, useTransition } from 'react';

import { AnalyzingState } from './AnalyzingState';
import { InputStage } from './InputStage';
import { ReportSummaryBar } from './ReportSummaryBar';
import { AlternativeScenarios } from './report/AlternativeScenarios';
import { ComparisonSection } from './report/ComparisonSection';
import { FindingsSection } from './report/FindingsSection';
import { NextSteps } from './report/NextSteps';
import { ParsedProtocolSection } from './report/ParsedProtocolSection';
import { ScoreHero } from './report/ScoreHero';
import {
  currentRawInput,
  exampleProtocols,
  formatAnalyzerError,
  getScoreBand,
  getScoreInsight,
  getWhatThisMeans,
  pickOptimizedProtocol,
  recommendationCount,
} from './analyzerView';
import { useAnalyzerSession } from './useAnalyzerSession';
import type { AnalyzerContextFields } from './useAnalyzerSession';

const ANALYZER_PRICING_HREF = '/pricing?intent=analyzer';

// Maps the exampleProtocols keys to the legacy analytics goal label the monolith
// sent (monolith ~261): fatLoss → 'fat loss', otherwise the example name.
const EXAMPLE_GOAL_SELECTION: Record<keyof typeof exampleProtocols, AnalyzerGoalSelection> = {
  healing: { primaryCategory: 'recovery', refinementGoalIds: [] },
  fatLoss: { primaryCategory: 'energy', refinementGoalIds: ['energy-fat-loss'] },
  longevity: { primaryCategory: 'longevity', refinementGoalIds: [] },
};

export function AnalyzerExperience() {
  const { snapshot, setSnapshot, loaded } = useAnalyzerSession();
  const { user, loading: authLoading } = useAuth();
  const router = useRouter();

  const { mode, inputText, linkUrl, goals, context, result } = snapshot;

  const setMode = (next: ProtocolAnalyzerInputType) => setSnapshot((s) => ({ ...s, mode: next }));
  const setInputText = (next: string) => setSnapshot((s) => ({ ...s, inputText: next }));
  const setLinkUrl = (next: string) => setSnapshot((s) => ({ ...s, linkUrl: next }));
  const setGoals = (next: AnalyzerGoalSelection) => setSnapshot((s) => ({ ...s, goals: next }));
  const setContext = (next: AnalyzerContextFields) => setSnapshot((s) => ({ ...s, context: next }));
  const setResult = (next: ProtocolAnalyzerResult | null) => setSnapshot((s) => ({ ...s, result: next }));

  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [error, setError] = useState('');
  const [showSaveNotice, setShowSaveNotice] = useState(false);
  const [savedAnalysisId, setSavedAnalysisId] = useState('');
  const [showExtractedText, setShowExtractedText] = useState(false);
  const [editing, setEditing] = useState(false);
  const [isPending, startTransition] = useTransition();
  const [profile, setProfile] = useState<PersonProfile | null>(null);

  const prefillDoneRef = useRef(false);
  const viewedFiredRef = useRef(false);

  // ── Stage derivation ────────────────────────────────────────────────────────
  const stage: 'input' | 'analyzing' | 'report' = isPending
    ? 'analyzing'
    : editing
      ? 'input'
      : result
        ? 'report'
        : 'input';

  // ── Derived values ──────────────────────────────────────────────────────────
  const optimizedProtocol = useMemo(() => pickOptimizedProtocol(result), [result]);
  const scoreInsight = getScoreInsight(result, optimizedProtocol, goals.primaryCategory !== null);
  const whatThisMeans = getWhatThisMeans(result, optimizedProtocol);
  const premiumLocked = Boolean(result);
  const hasProfile = profile !== null;
  const isAuthenticated = Boolean(user);

  // ── Profile fetch + goal prefill (once, when authed) ─────────────────────────
  useEffect(() => {
    if (!loaded || prefillDoneRef.current || !user) {
      return;
    }
    prefillDoneRef.current = true;

    let cancelled = false;
    (async () => {
      let fetched: PersonProfile | null = null;
      try {
        const profiles = await apiClient.getProfiles();
        fetched = profiles[0] ?? null;
      } catch {
        fetched = null;
      }
      if (cancelled) {
        return;
      }
      setProfile(fetched);

      if (!fetched) {
        return;
      }

      const goalsEmpty = goals.primaryCategory === null && goals.refinementGoalIds.length === 0;
      if (!goalsEmpty) {
        return;
      }

      const profileGoalIds =
        fetched.goals && fetched.goals.length > 0
          ? fetched.goals.map((g) => g.goalDefinitionId)
          : getMockProfileGoalIds(fetched.id);

      const prefilled = prefillFromProfileGoals(profileGoalIds);
      if (prefilled.primaryCategory !== null) {
        setGoals(prefilled);
      }
    })();

    return () => {
      cancelled = true;
    };
    // Prefill runs once after the session loads and the user is known.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [loaded, user]);

  // ── Analytics: mount-once page view ──────────────────────────────────────────
  useEffect(() => {
    if (viewedFiredRef.current) {
      return;
    }
    viewedFiredRef.current = true;
    trackAnalyzerEvent('analyzer_viewed', {
      inputType: mode,
      hasRestoredResult: Boolean(result),
    });
    // Fire once for this mounted analyzer surface.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // ── Analytics: result-change quartet ─────────────────────────────────────────
  useEffect(() => {
    if (!result) {
      return;
    }

    trackAnalyzerEvent('analyzer_result_viewed', {
      inputType: result.inputType,
      scoreBand: getScoreBand(result.score),
      issueCount: result.issues.length,
      recommendationCount: recommendationCount(result),
    });
    trackAnalyzerEvent('analyzer_score_visible', {
      inputType: result.inputType,
      scoreBand: getScoreBand(result.score),
      issueCount: result.issues.length,
    });
    trackAnalyzerEvent('analyzer_why_section_viewed', {
      inputType: result.inputType,
      scoreBand: getScoreBand(result.score),
      recommendationCount: recommendationCount(result),
    });
    trackAnalyzerEvent('analyzer_comparison_viewed', {
      inputType: result.inputType,
      scoreBand: getScoreBand(result.score),
      recommendationCount: recommendationCount(result),
    });
  }, [result]);

  // ── runAnalysis ──────────────────────────────────────────────────────────────
  function runAnalysis(input: {
    inputType: ProtocolAnalyzerInputType;
    inputText?: string;
    linkUrl?: string;
    file?: File | null;
    sourceName?: string;
    goalSelection: AnalyzerGoalSelection;
    contextFields: AnalyzerContextFields;
    exampleType?: keyof typeof exampleProtocols;
  }) {
    setError('');
    setShowSaveNotice(false);
    setSavedAnalysisId('');

    const { goal, secondaryGoals } = buildAnalyzerGoalPayload(
      input.goalSelection.primaryCategory,
      input.goalSelection.refinementGoalIds,
    );
    const ctx = input.contextFields;
    const contextPayload = {
      sex: ctx.sex || undefined,
      age: ctx.age ? Number(ctx.age) : undefined,
      weight: ctx.weight ? Number(ctx.weight) : undefined,
      existingStackContext: ctx.existingStack
        ? ctx.existingStack.split('\n').map((l) => l.trim()).filter(Boolean)
        : undefined,
    };

    trackAnalyzerEvent('analyzer_analysis_started', {
      inputType: input.inputType,
      goal,
      exampleType: input.exampleType,
    });

    startTransition(async () => {
      try {
        const payload =
          input.inputType === 'Paste'
            ? {
                inputType: input.inputType,
                inputText: input.inputText ?? '',
                goal,
                secondaryGoals,
                maxCompounds: FREE_TIER_COMPOUND_LIMIT,
                ...contextPayload,
              }
            : input.inputType === 'Link'
              ? {
                  inputType: input.inputType,
                  linkUrl: input.linkUrl ?? '',
                  goal,
                  secondaryGoals,
                  maxCompounds: FREE_TIER_COMPOUND_LIMIT,
                  ...contextPayload,
                }
              : {
                  inputType: input.inputType,
                  file: input.file ?? undefined,
                  sourceName: input.sourceName,
                  goal,
                  secondaryGoals,
                  maxCompounds: FREE_TIER_COMPOUND_LIMIT,
                  ...contextPayload,
                };

        const analysis = await apiClient.analyzeProtocol(payload);
        setResult(analysis);
        setEditing(false);
      } catch (requestError) {
        setResult(null);
        setError(formatAnalyzerError(requestError, input.inputType));
      }
    });
  }

  // ── Handlers ─────────────────────────────────────────────────────────────────
  function analyzeProtocol() {
    runAnalysis({
      inputType: mode,
      inputText,
      linkUrl,
      file: selectedFile,
      sourceName: selectedFile?.name,
      goalSelection: goals,
      contextFields: context,
    });
  }

  function clearInput() {
    // Per plan: preserve goals and context (user identity); clear inputs/results.
    setSnapshot((s) => ({ ...s, inputText: '', linkUrl: '', result: null }));
    setSelectedFile(null);
    setError('');
    setShowSaveNotice(false);
    setSavedAnalysisId('');
  }

  function loadExample(example: keyof typeof exampleProtocols) {
    const exampleGoal = example === 'fatLoss' ? 'fat loss' : example;
    const exampleSelection = EXAMPLE_GOAL_SELECTION[example];
    setSnapshot((s) => ({
      ...s,
      mode: 'Paste',
      goals: exampleSelection,
      inputText: exampleProtocols[example],
      result: null,
    }));
    setError('');
    setSavedAnalysisId('');

    trackAnalyzerEvent('analyzer_example_loaded', {
      exampleType: example,
      goal: exampleGoal,
    });

    runAnalysis({
      inputType: 'Paste',
      inputText: exampleProtocols[example],
      goalSelection: exampleSelection,
      contextFields: context,
      exampleType: example,
    });
  }

  function onScanRequested() {
    setSnapshot((s) => ({ ...s, mode: 'CameraScan', result: null }));
    setError('');
    trackAnalyzerEvent('analyzer_scan_selected', { inputType: 'CameraScan' });
    trackAnalyzerEvent('analyzer_input_mode_selected', { inputType: 'CameraScan' });
  }

  function saveAnalysisLocally() {
    if (!result) {
      return;
    }

    const analysis = saveAnalyzerAnalysis({
      inputType: mode,
      sourceName: result.sourceName,
      rawInput: currentRawInput(mode, inputText, linkUrl, selectedFile),
      result,
    });

    setSavedAnalysisId(analysis.id);
    trackAnalyzerEvent('analyzer_save_clicked', {
      inputType: result.inputType,
      goal: goals.primaryCategory,
      scoreBand: getScoreBand(result.score),
      issueCount: result.issues.length,
      recommendationCount: recommendationCount(result),
      locked: premiumLocked,
    });
    setShowSaveNotice(true);
  }

  function convertToProtocol() {
    if (!result) {
      return;
    }

    const { goal } = buildAnalyzerGoalPayload(goals.primaryCategory, goals.refinementGoalIds);

    const analysis = saveAnalyzerAnalysis({
      inputType: mode,
      sourceName: result.sourceName,
      rawInput: currentRawInput(mode, inputText, linkUrl, selectedFile),
      result,
    });

    saveAnalyzerProtocolDraft({
      sourceAnalysisId: analysis.id,
      goal,
      protocol: result.protocol,
      optimizedProtocol: optimizedProtocol?.protocol ?? result.protocol,
    });

    setSavedAnalysisId(analysis.id);
    setShowSaveNotice(true);
    trackAnalyzerEvent('analyzer_convert_clicked', {
      inputType: result.inputType,
      goal,
      scoreBand: getScoreBand(result.score),
      issueCount: result.issues.length,
      recommendationCount: recommendationCount(result),
      locked: premiumLocked,
    });

    if (!user && !authLoading) {
      router.push('/auth/signin?callbackUrl=/protocol-console');
      return;
    }

    router.push('/protocol-console');
  }

  function onUnlockClicked() {
    trackAnalyzerEvent('analyzer_unlock_clicked', {
      inputType: result?.inputType,
      scoreBand: getScoreBand(result?.score),
      issueCount: result?.issues.length,
      recommendationCount: result ? recommendationCount(result) : 0,
      locked: true,
    });
  }

  function handleFileSelected(file: File | null) {
    setSelectedFile(file);
    setResult(null);
    setError('');
  }

  function handleModeChange(next: ProtocolAnalyzerInputType) {
    // InputStage fires analyzer_input_mode_selected (and analyzer_scan_selected)
    // itself on the tab click — do NOT double-fire here.
    setMode(next);
    setError('');
    setResult(null);
  }

  return (
    <main className={`mx-auto max-w-3xl px-4 pt-8 sm:px-6 lg:px-8 ${result ? 'pb-40 md:pb-28' : 'pb-28'}`}>
      <section className="mb-6 border-b border-white/[0.08] pb-6">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-emerald-300/70">Protocol Analyzer</p>
        <h1 className="mt-3 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
          Analyze any protocol, in any format you actually have
        </h1>
        <p className="mt-3 max-w-3xl text-base leading-7 text-white/62">
          Paste, upload, scan, or link any protocol. BioStack extracts the structure, scores the stack, and surfaces alternative scenarios.
        </p>
        <div className="mt-4 flex flex-wrap gap-2 text-sm text-white/60">
          <FeatureChip label="Extract document text" />
          <FeatureChip label="Infer protocol structure" />
          <FeatureChip label="Detect overlap" />
          <FeatureChip label="Compare alternatives" />
        </div>
      </section>

      {stage === 'input' && (
        <InputStage
          mode={mode}
          inputText={inputText}
          linkUrl={linkUrl}
          selectedFile={selectedFile}
          goals={goals}
          context={context}
          profile={profile}
          isAuthenticated={isAuthenticated}
          isPending={isPending}
          error={error}
          onModeChange={handleModeChange}
          onInputTextChange={setInputText}
          onLinkUrlChange={setLinkUrl}
          onFileSelected={handleFileSelected}
          onGoalsChange={setGoals}
          onContextChange={setContext}
          onAnalyze={analyzeProtocol}
          onClear={clearInput}
          onLoadExample={loadExample}
          onScanRequested={onScanRequested}
        />
      )}

      {stage === 'analyzing' && <AnalyzingState mode={mode} />}

      {stage === 'report' && result && (
        <div className="space-y-5">
          <ReportSummaryBar result={result} primaryCategory={goals.primaryCategory} onEdit={() => setEditing(true)} />
          <ScoreHero result={result} scoreInsight={scoreInsight} whatThisMeans={whatThisMeans} />
          <FindingsSection
            result={result}
            showExtractedText={showExtractedText}
            onToggleExtractedText={() => setShowExtractedText((v) => !v)}
          />
          <ParsedProtocolSection result={result} />
          {optimizedProtocol && <ComparisonSection result={result} optimized={optimizedProtocol} />}
          <AlternativeScenarios result={result} optimized={optimizedProtocol} />
          <ShareableSummaryStub />
          <NextSteps
            result={result}
            savedAnalysisId={savedAnalysisId}
            showSaveNotice={showSaveNotice}
            isAuthenticated={isAuthenticated}
            hasProfile={hasProfile}
            onSave={saveAnalysisLocally}
            onConvert={convertToProtocol}
            onUnlockClicked={onUnlockClicked}
          />
        </div>
      )}

      {stage === 'report' && result && (
        <div className="fixed inset-x-0 bottom-0 z-20 border-t border-white/10 bg-[#0B1118]/95 p-3 backdrop-blur md:hidden">
          <div className="mx-auto max-w-3xl">
            {premiumLocked ? (
              <Link href={ANALYZER_PRICING_HREF} onClick={onUnlockClicked} className="block rounded-lg bg-emerald-400 px-4 py-3 text-center text-sm font-semibold text-slate-950">
                Unlock full analysis
              </Link>
            ) : user ? (
              <button type="button" onClick={convertToProtocol} className="block w-full rounded-lg bg-emerald-400 px-4 py-3 text-center text-sm font-semibold text-slate-950">
                Convert to BioStack Protocol
              </button>
            ) : (
              <button type="button" onClick={saveAnalysisLocally} className="block w-full rounded-lg bg-emerald-400 px-4 py-3 text-center text-sm font-semibold text-slate-950">
                Save analysis
              </button>
            )}
          </div>
        </div>
      )}
    </main>
  );
}

function FeatureChip({ label }: { label: string }) {
  return <span className="rounded-lg border border-white/10 px-3 py-1.5">{label}</span>;
}

function ShareableSummaryStub() {
  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.025] p-4 opacity-80">
      <h2 className="text-base font-semibold text-white">Shareable protocol summary</h2>
      <p className="mt-2 text-sm leading-6 text-white/50">
        Coming soon: share a BioStack protocol score card without exposing private details.
      </p>
    </section>
  );
}
