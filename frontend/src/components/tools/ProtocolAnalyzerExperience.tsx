'use client';

import Link from 'next/link';
import { ChangeEvent, useEffect, useMemo, useRef, useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import { apiClient } from '@/lib/api';
import { trackAnalyzerEvent } from '@/lib/analyzerAnalytics';
import { saveAnalyzerAnalysis, saveAnalyzerProtocolDraft } from '@/lib/analyzerStorage';
import { useAuth } from '@/lib/AuthProvider';
import type {
  ProtocolAnalyzerArtifact,
  ProtocolAnalyzerCounterfactual,
  ProtocolAnalyzerGoalAwareOption,
  ProtocolAnalyzerInputType,
  ProtocolAnalyzerResult,
  ProtocolAnalyzerSwap,
} from '@/lib/types';

const STORAGE_KEY = 'biostack.analyzer.session.v3';
const supportedFileTypes = '.pdf,.docx,.xlsx,.csv,.txt,.jpg,.jpeg,.png,.webp';
const maxUploadLabel = 'Up to 12 MB';

const exampleProtocols = {
  healing: `GLOW Blend (GHK-cu, BPC-157, TB-500)
BPC-157 500mcg daily
TB-500 2mg twice weekly
8 weeks on, 8 weeks off`,
  fatLoss: `Semaglutide weekly
Tirzepatide weekly
NAD+ 100mg 2x weekly
8 weeks on, 8 weeks off`,
  longevity: `NAD+ 250mg daily
MOTS-C 5mg 3x weekly
CoQ10 daily
8 weeks on, 8 weeks off`,
};

const modeTabs: Array<{
  id: ProtocolAnalyzerInputType;
  label: string;
  title: string;
  description: string;
}> = [
  { id: 'Paste', label: 'Paste', title: 'Paste protocol text', description: 'Paste raw notes, tables, or cheat sheet text.' },
  { id: 'FileUpload', label: 'Upload', title: 'Upload a protocol file', description: 'PDF, DOCX, XLSX, CSV, TXT, JPG, PNG, or WEBP.' },
  { id: 'CameraScan', label: 'Scan', title: 'Scan a protocol', description: 'Use your camera on mobile or choose a photo on desktop.' },
  { id: 'Link', label: 'Link', title: 'Analyze a shared document link', description: 'Paste a public document URL and we will fetch it when supported.' },
];

type OptimizedProtocolView = {
  label: string;
  protocol: ProtocolAnalyzerResult['protocol'];
  score: number;
  removed: string[];
};

type AnalyzerSessionSnapshot = {
  mode?: ProtocolAnalyzerInputType;
  inputText?: string;
  linkUrl?: string;
  goal?: string;
  result?: ProtocolAnalyzerResult | null;
};

function readAnalyzerSessionSnapshot(): AnalyzerSessionSnapshot {
  if (typeof window === 'undefined') {
    return {};
  }

  try {
    const saved = window.localStorage.getItem(STORAGE_KEY);
    return saved ? (JSON.parse(saved) as AnalyzerSessionSnapshot) : {};
  } catch {
    return {};
  }
}

export function ProtocolAnalyzerExperience() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const [sessionLoaded, setSessionLoaded] = useState(false);
  const [mode, setMode] = useState<ProtocolAnalyzerInputType>('Paste');
  const [inputText, setInputText] = useState(exampleProtocols.healing);
  const [linkUrl, setLinkUrl] = useState('');
  const [goal, setGoal] = useState('healing');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [result, setResult] = useState<ProtocolAnalyzerResult | null>(null);
  const [error, setError] = useState('');
  const [isPending, startTransition] = useTransition();
  const [showWhyScore, setShowWhyScore] = useState(false);
  const [showSaveNotice, setShowSaveNotice] = useState(false);
  const [savedAnalysisId, setSavedAnalysisId] = useState('');
  const [showExtractedText, setShowExtractedText] = useState(false);
  const uploadInputRef = useRef<HTMLInputElement | null>(null);
  const cameraInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    const id = window.setTimeout(() => {
      const restored = readAnalyzerSessionSnapshot();
      setMode(restored.mode ?? 'Paste');
      setInputText(restored.inputText || exampleProtocols.healing);
      setLinkUrl(restored.linkUrl || '');
      setGoal(restored.goal || 'healing');
      setResult(restored.result ?? null);
      setSessionLoaded(true);
    }, 0);

    return () => window.clearTimeout(id);
  }, []);

  useEffect(() => {
    if (!sessionLoaded) {
      return;
    }

    try {
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify({ mode, inputText, linkUrl, goal, result }));
    } catch {
    }
  }, [goal, inputText, linkUrl, mode, result, sessionLoaded]);

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

  const scoreTone = useMemo(() => {
    if (!result) return 'border-white/10 bg-white/[0.03]';
    if (result.score >= 80) return 'border-emerald-300/25 bg-emerald-400/[0.12]';
    if (result.score >= 60) return 'border-amber-300/25 bg-amber-400/[0.12]';
    return 'border-red-300/25 bg-red-400/[0.12]';
  }, [result]);

  const counterfactuals = result?.counterfactuals;
  const primaryRemoval = counterfactuals?.bestRemoveOne?.[0] ?? null;
  const primarySwap = counterfactuals?.bestSwapOne?.[0] ?? null;
  const simplified = counterfactuals?.bestSimplifiedProtocol ?? null;
  const goalAware = counterfactuals?.goalAwareOptions?.[0] ?? null;
  const modeConfig = modeTabs.find((tab) => tab.id === mode) ?? modeTabs[0];
  const optimizedProtocol = useMemo(() => pickOptimizedProtocol(result), [result]);
  const scoreLabel = getScoreLabel(result?.score);
  const scoreInsight = getScoreInsight(result, optimizedProtocol);
  const whatThisMeans = getWhatThisMeans(result, optimizedProtocol);
  const premiumLocked = Boolean(result);

  useEffect(() => {
    trackAnalyzerEvent('analyzer_viewed', {
      inputType: mode,
      hasRestoredResult: Boolean(result),
    });
    // Page view should fire once for this mounted analyzer surface.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function runAnalysis(input: {
    inputType: ProtocolAnalyzerInputType;
    inputText?: string;
    linkUrl?: string;
    file?: File | null;
    sourceName?: string;
    goal: string;
    exampleType?: keyof typeof exampleProtocols;
  }) {
    setError('');
    setShowSaveNotice(false);
    setSavedAnalysisId('');
    trackAnalyzerEvent('analyzer_analysis_started', {
      inputType: input.inputType,
      goal: input.goal,
      exampleType: input.exampleType,
    });

    startTransition(async () => {
      try {
        const payload =
          input.inputType === 'Paste'
            ? { inputType: input.inputType, inputText: input.inputText ?? '', goal: input.goal, maxCompounds: 5 as const }
            : input.inputType === 'Link'
              ? { inputType: input.inputType, linkUrl: input.linkUrl ?? '', goal: input.goal, maxCompounds: 5 as const }
              : {
                  inputType: input.inputType,
                  file: input.file ?? undefined,
                  sourceName: input.sourceName,
                  goal: input.goal,
                  maxCompounds: 5 as const,
                };

        setResult(await apiClient.analyzeProtocol(payload));
      } catch (requestError) {
        setResult(null);
        setError(formatAnalyzerError(requestError, input.inputType));
      }
    });
  }

  function analyzeProtocol() {
    runAnalysis({
      inputType: mode,
      inputText,
      linkUrl,
      file: selectedFile,
      sourceName: selectedFile?.name,
      goal,
    });
  }

  function clearInput() {
    setInputText('');
    setLinkUrl('');
    setSelectedFile(null);
    if (uploadInputRef.current) uploadInputRef.current.value = '';
    if (cameraInputRef.current) cameraInputRef.current.value = '';
    setResult(null);
    setError('');
    setShowSaveNotice(false);
    setSavedAnalysisId('');
  }

  function loadExample(example: keyof typeof exampleProtocols) {
    const exampleGoal = example === 'fatLoss' ? 'fat loss' : example;
    setMode('Paste');
    setGoal(exampleGoal);
    setInputText(exampleProtocols[example]);
    setResult(null);
    setError('');
    setSavedAnalysisId('');
    trackAnalyzerEvent('analyzer_example_loaded', {
      exampleType: example,
      goal: exampleGoal,
    });
    runAnalysis({
      inputType: 'Paste',
      inputText: exampleProtocols[example],
      goal: exampleGoal,
      exampleType: example,
    });
  }

  function selectScanModeAndOpenCamera() {
    setMode('CameraScan');
    setError('');
    setResult(null);
    trackAnalyzerEvent('analyzer_scan_selected', { inputType: 'CameraScan' });
    trackAnalyzerEvent('analyzer_input_mode_selected', { inputType: 'CameraScan' });
    window.setTimeout(() => cameraInputRef.current?.click(), 0);
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
      goal,
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

  function onFileSelected(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0] ?? null;
    setSelectedFile(file);
    setResult(null);
    setError('');
  }

  const analyzeDisabled =
    isPending ||
    (mode === 'Paste' && inputText.trim().length === 0) ||
    (mode === 'Link' && linkUrl.trim().length === 0) ||
    ((mode === 'FileUpload' || mode === 'CameraScan') && !selectedFile);

  return (
    <main className={`mx-auto max-w-7xl px-4 pt-8 sm:px-6 lg:px-8 lg:pt-10 ${result ? 'pb-40 md:pb-28' : 'pb-28'}`}>
      <section className="mb-6 border-b border-white/[0.08] pb-6">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-emerald-300/70">Protocol Analyzer</p>
        <h1 className="mt-3 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
          Analyze any protocol, in any format you actually have
        </h1>
        <p className="mt-3 max-w-3xl text-base leading-7 text-white/62">
          Paste, upload, scan, or link any protocol. BioStack extracts the structure, scores the stack, and shows better options.
        </p>
        <div className="mt-4 flex flex-wrap gap-2 text-sm text-white/60">
          <FeatureChip label="Extract document text" />
          <FeatureChip label="Infer protocol structure" />
          <FeatureChip label="Detect overlap" />
          <FeatureChip label="Optimize the stack" />
        </div>
      </section>

      <section className="grid gap-6 xl:grid-cols-[1.02fr_0.98fr]">
        <div className="space-y-5">
          <section className="rounded-lg border border-white/[0.08] bg-[#121923]/95 p-4 shadow-[0_16px_48px_rgba(0,0,0,0.24)] sm:p-5">
            <div className="flex flex-wrap gap-2">
              {modeTabs.map((tab) => (
                <button
                  key={tab.id}
                  type="button"
                  onClick={() => {
                    setMode(tab.id);
                    setError('');
                    setResult(null);
                    trackAnalyzerEvent('analyzer_input_mode_selected', { inputType: tab.id });
                    if (tab.id === 'CameraScan') {
                      trackAnalyzerEvent('analyzer_scan_selected', { inputType: tab.id });
                    }
                  }}
                  className={`rounded-full border px-4 py-2 text-sm font-semibold transition-colors ${
                    mode === tab.id
                      ? 'border-emerald-300/45 bg-emerald-400/14 text-emerald-100'
                      : 'border-white/10 text-white/65 hover:border-white/20 hover:text-white'
                  }`}
                >
                  {tab.label}
                </button>
              ))}
            </div>
            <button
              type="button"
              onClick={selectScanModeAndOpenCamera}
              className="mt-4 inline-flex min-h-11 w-full items-center justify-center rounded-lg border border-emerald-300/35 bg-emerald-400/12 px-4 text-sm font-bold text-emerald-50 transition-colors hover:bg-emerald-400/18 md:hidden"
            >
              Scan protocol
            </button>

            <div className="mt-5 grid gap-4 lg:grid-cols-[1fr_auto]">
              <div>
                <h2 className="text-lg font-semibold text-white">{modeConfig.title}</h2>
                <p className="mt-2 text-sm leading-6 text-white/58">{modeConfig.description}</p>

                {mode === 'Paste' && (
                  <>
                    <label className="mt-4 block">
                      <span className="mb-3 block text-sm font-semibold text-white/72">Protocol text</span>
                      <textarea
                        value={inputText}
                        onChange={(event) => setInputText(event.target.value)}
                        rows={16}
                        className="min-h-80 w-full resize-y rounded-lg border border-white/10 bg-[#0F141B] p-4 text-sm leading-6 text-white outline-none transition-colors placeholder:text-white/30 focus:border-emerald-400/45"
                        placeholder={`BPC-157 500mcg daily\nTB-500 500mcg daily\nNAD+ 100mg 2x weekly\n8 weeks on, 8 weeks off`}
                      />
                    </label>
                    <p className="mt-3 text-sm leading-6 text-white/52">
                      Paste raw protocol text, tables, dosing notes, or cheat sheet content. We will extract compounds, doses, frequencies, and stack structure automatically.
                    </p>
                  </>
                )}

                {mode === 'FileUpload' && (
                  <UploadPanel
                    selectedFile={selectedFile}
                    inputRef={uploadInputRef}
                    onTrigger={() => uploadInputRef.current?.click()}
                    onChange={onFileSelected}
                    title="Drop in a protocol file"
                    description="Upload PDF, DOCX, XLSX, CSV, TXT, JPG, JPEG, PNG, or WEBP."
                  />
                )}

                {mode === 'CameraScan' && (
                  <UploadPanel
                    selectedFile={selectedFile}
                    inputRef={cameraInputRef}
                    onTrigger={() => cameraInputRef.current?.click()}
                    onChange={onFileSelected}
                    title="Scan protocol"
                    description="Use your camera on mobile or choose a protocol photo. BioStack will extract text from the image."
                    capture="environment"
                    accept="image/*"
                    helper="Camera scan works best on mobile. On desktop, you can still choose an image from your device."
                  />
                )}

                {mode === 'Link' && (
                  <>
                    <label className="mt-4 block">
                      <span className="mb-3 block text-sm font-semibold text-white/72">Shared document URL</span>
                      <input
                        type="url"
                        value={linkUrl}
                        onChange={(event) => setLinkUrl(event.target.value)}
                        className="min-h-12 w-full rounded-lg border border-white/10 bg-[#0F141B] px-4 text-sm text-white outline-none transition-colors placeholder:text-white/30 focus:border-emerald-400/45"
                        placeholder="https://example.com/shared-protocol.pdf"
                      />
                    </label>
                    <p className="mt-3 text-sm leading-6 text-white/52">
                      Public direct-file links work best right now. Unsupported or protected sources will return a clear message instead of failing silently.
                    </p>
                  </>
                )}

                <p className="mt-2 text-xs leading-5 text-white/38">
                  Educational analysis only. Verify all dosing math manually.
                </p>
              </div>

              <div className="space-y-4 lg:w-56">
                <div>
                  <p className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-white/42">Goal</p>
                  <div className="grid gap-2">
                    {[
                      ['healing', 'Healing'],
                      ['fat loss', 'Fat loss'],
                      ['longevity', 'Longevity'],
                    ].map(([value, label]) => (
                      <button
                        key={value}
                        type="button"
                  onClick={() => setGoal(value)}
                        className={`rounded-lg border px-3 py-2 text-left text-sm font-semibold transition-colors ${
                          goal === value
                            ? 'border-emerald-300/45 bg-emerald-400/14 text-emerald-100'
                            : 'border-white/10 text-white/65 hover:border-white/20 hover:text-white'
                        }`}
                      >
                        {label}
                      </button>
                    ))}
                  </div>
                </div>

                <div>
                  <p className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-white/42">Try an example</p>
                  <div className="flex flex-wrap gap-2 lg:flex-col">
                    <ExampleButton label="Healing stack" onClick={() => loadExample('healing')} />
                    <ExampleButton label="Fat loss stack" onClick={() => loadExample('fatLoss')} />
                    <ExampleButton label="Longevity stack" onClick={() => loadExample('longevity')} />
                  </div>
                </div>

                <div className="rounded-lg border border-white/10 bg-black/20 p-3 text-sm text-white/55">
                  <p className="font-semibold text-white/78">Accepted files</p>
                  <p className="mt-2">PDF, DOCX, XLSX, CSV, TXT, JPG, PNG, WEBP</p>
                  <p className="mt-2 text-xs text-white/42">{maxUploadLabel}</p>
                </div>
              </div>
            </div>

            <div className="mt-5 flex flex-col gap-3 sm:flex-row sm:flex-wrap">
              <button
                type="button"
                onClick={analyzeProtocol}
                disabled={analyzeDisabled}
                className="inline-flex min-h-12 items-center justify-center rounded-lg bg-emerald-400 px-5 text-sm font-bold text-[#07110c] transition-colors hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isPending ? 'Analyzing Protocol...' : 'Analyze Protocol'}
              </button>
              <button
                type="button"
                onClick={clearInput}
                className="min-h-12 rounded-lg border border-white/10 px-4 text-sm font-semibold text-white/72 transition-colors hover:border-white/20 hover:text-white"
              >
                Clear
              </button>
              <button
                type="button"
                onClick={saveAnalysisLocally}
                disabled={!result}
                className="min-h-12 rounded-lg border border-white/10 px-4 text-sm font-semibold text-white/72 transition-colors hover:border-white/20 hover:text-white"
              >
                Save Analysis
              </button>
            </div>

            {isPending && <AnalyzerProgressCard mode={mode} />}
            {error && <p className="mt-4 text-sm leading-6 text-red-100/85">{error}</p>}
            {showSaveNotice && (
              <p className="mt-4 text-sm leading-6 text-emerald-100/80">
                Analysis saved locally{savedAnalysisId ? ` as ${savedAnalysisId}` : ''}. It will stay available through sign-in and protocol conversion.
              </p>
            )}
          </section>

          {!result && !isPending && (
            <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
              <h2 className="text-lg font-semibold text-white">Try an example</h2>
              <div className="mt-4 flex flex-wrap gap-3">
                <ExampleButton label="Healing stack example" onClick={() => loadExample('healing')} />
                <ExampleButton label="Fat loss stack example" onClick={() => loadExample('fatLoss')} />
                <ExampleButton label="Longevity stack example" onClick={() => loadExample('longevity')} />
              </div>
            </section>
          )}

          {result && (
            <ExtractionNotesPanel
              result={result}
              showExtractedText={showExtractedText}
              onToggleExtractedText={() => setShowExtractedText((current) => !current)}
            />
          )}
          {result && <OriginalVsOptimizedSection result={result} optimized={optimizedProtocol} />}
          <ParsedProtocolSection result={result} />
          {result && <ShareableSummaryStub />}

          <section className="grid gap-4 md:grid-cols-2">
            <ResultList title="What BioStack found" empty="No issues yet." items={result?.issues.map((issue) => issue.message) ?? []} />
            <ResultList title="Parser notes" empty="Parser confidence looks clean." items={result?.parserWarnings ?? buildParserWarnings(result)} />
          </section>
        </div>

        <aside className="space-y-4 xl:sticky xl:top-24 xl:self-start">
          <section className={`rounded-lg border p-5 ${scoreTone}`}>
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-white/55">BioStack Score</p>
            <p className="mt-3 text-5xl font-semibold tracking-tight text-white">
              {result ? result.score : '--'} <span className="text-2xl text-white/42">/ 100</span>
            </p>
            <p className="mt-2 text-base font-semibold text-white/85">{scoreLabel}</p>
            <div className="mt-4 h-2 rounded-full bg-black/20">
              <div className="h-2 rounded-full bg-white/80 transition-all" style={{ width: `${Math.max(4, result?.score ?? 0)}%` }} />
            </div>
            <p className="mt-3 text-sm leading-6 text-white/62">
              {scoreInsight}
            </p>
            <button
              type="button"
              onClick={() => setShowWhyScore((current) => !current)}
              className="mt-4 text-sm font-semibold text-white/72 transition-colors hover:text-white"
            >
              Why this score?
            </button>
            {showWhyScore && result && (
              <div className="mt-4 grid gap-3 sm:grid-cols-2">
                <ScoreChip label="Base" value={result.scoreExplanation.baseScore} tone="neutral" />
                <ScoreChip label="Synergy" value={result.scoreExplanation.synergy} tone="positive" />
                <ScoreChip label="Redundancy" value={result.scoreExplanation.redundancy} tone="negative" />
                <ScoreChip label="Interference" value={result.scoreExplanation.interference} tone="negative" />
              </div>
            )}
          </section>

          {result && <WhatThisMeansPanel message={whatThisMeans} />}

          <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
            <h2 className="text-lg font-semibold text-white">Why this is better</h2>
            <p className="mt-2 text-sm leading-6 text-white/55">
              BioStack is testing cleaner ways to reach the same goal with less noise.
            </p>
            <div className="mt-4 space-y-3">
              {result ? <WhyBetterBlocks result={result} optimized={optimizedProtocol} /> : null}
              <ImprovementCard
                title="Best removal"
                teaser={primaryRemoval}
                empty="No obvious removal surfaced."
                emptyDetail="BioStack did not find a high-confidence compound to remove for this goal. This may mean the stack is already relatively lean, or that more context is needed."
                kind="remove"
              />
              <ImprovementCard
                title="Best swap"
                teaser={primarySwap}
                empty="No strong swap surfaced."
                emptyDetail="BioStack did not find a higher-scoring replacement from the current knowledge set. Full optimization may still identify goal-specific refinements."
                kind="swap"
              />
              <SimplifiedProtocolCard protocol={simplified} />
              <GoalAwareCard option={goalAware} />
            </div>
          </section>

          <section className="rounded-lg border border-white/[0.08] bg-[#121923]/95 p-4">
            <h2 className="text-lg font-semibold text-white">Turn this into a BioStack protocol</h2>
            <p className="mt-3 text-sm leading-6 text-white/58">
              Save the analysis, convert it into a protocol, and track it in Mission Control.
            </p>
            <div className="mt-4 flex flex-wrap gap-3">
              <button
                type="button"
                onClick={saveAnalysisLocally}
                disabled={!result}
                className="rounded-lg border border-white/10 px-4 py-2 text-sm font-semibold text-white/72 hover:border-white/20 hover:text-white"
              >
                Save Analysis
              </button>
              <button
                type="button"
                onClick={convertToProtocol}
                disabled={!result}
                className="rounded-lg bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-60"
              >
                Convert to BioStack Protocol
              </button>
              <Link
                href="/billing"
                onClick={onUnlockClicked}
                className="rounded-lg border border-white/10 px-4 py-2 text-sm font-semibold text-white/72 hover:border-white/20 hover:text-white"
              >
                Unlock full analysis
              </Link>
            </div>
          </section>
        </aside>
      </section>

      {result && (
        <div className="fixed inset-x-0 bottom-0 z-20 border-t border-white/10 bg-[#0B1118]/95 p-3 backdrop-blur md:hidden">
          <div className="mx-auto max-w-7xl">
            {premiumLocked ? (
              <Link href="/billing" onClick={onUnlockClicked} className="block rounded-lg bg-emerald-400 px-4 py-3 text-center text-sm font-semibold text-slate-950">
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

function UploadPanel({
  selectedFile,
  inputRef,
  onTrigger,
  onChange,
  title,
  description,
  accept = supportedFileTypes,
  capture,
  helper,
}: {
  selectedFile: File | null;
  inputRef: React.RefObject<HTMLInputElement | null>;
  onTrigger: () => void;
  onChange: (event: ChangeEvent<HTMLInputElement>) => void;
  title: string;
  description: string;
  accept?: string;
  capture?: 'environment';
  helper?: string;
}) {
  return (
    <div className="mt-4 rounded-2xl border border-dashed border-white/15 bg-[#0F141B] p-5">
      <input ref={inputRef} type="file" accept={accept} capture={capture} onChange={onChange} className="hidden" />
      <p className="text-sm font-semibold text-white">{title}</p>
      <p className="mt-2 text-sm leading-6 text-white/56">{description}</p>
      <div className="mt-4 flex flex-wrap gap-3">
        <button type="button" onClick={onTrigger} className="rounded-lg bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-300">
          {capture ? 'Use camera' : 'Choose file'}
        </button>
        <span className="rounded-lg border border-white/10 px-3 py-2 text-sm text-white/50">{maxUploadLabel}</span>
      </div>
      {selectedFile ? (
        <p className="mt-4 text-sm text-emerald-100/85">
          Ready: {selectedFile.name} ({Math.max(1, Math.round(selectedFile.size / 1024))} KB)
        </p>
      ) : (
        <p className="mt-4 text-sm text-white/42">Supported types: PDF, DOCX, XLSX, CSV, TXT, JPG, PNG, WEBP</p>
      )}
      {helper ? <p className="mt-2 text-xs leading-5 text-white/38">{helper}</p> : null}
    </div>
  );
}

function ExtractionNotesPanel({
  result,
  showExtractedText,
  onToggleExtractedText,
}: {
  result: ProtocolAnalyzerResult;
  showExtractedText: boolean;
  onToggleExtractedText: () => void;
}) {
  const notes = [...result.extractionWarnings];
  if (result.lowConfidenceExtraction) {
    notes.unshift('Low-confidence extraction. Review the parsed protocol carefully before acting on it.');
  }
  const inferredCount = result.protocol.length;
  const normalizedCount = Math.max(0, result.protocol.length - result.unknownCompounds.length);

  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-semibold text-white">Extraction notes</h2>
        <span className="rounded-full border border-white/10 px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-white/55">
          {result.inputType}
        </span>
      </div>
      <div className="mt-4 grid gap-3 sm:grid-cols-3">
        <TrustMetric label="Source type" value={sourceTypeLabel(result)} />
        <TrustMetric label="Confidence" value={confidenceLabel(result)} />
        <TrustMetric label="Items inferred" value={`${inferredCount} found, ${normalizedCount} normalized`} />
      </div>
      <div className="mt-4 grid gap-4 lg:grid-cols-[1fr_1fr]">
        <div className="space-y-2 text-sm text-white/62">
          {result.sourceName ? <p>Source: {result.sourceName}</p> : null}
          {notes.length > 0 ? notes.map((note) => <p key={note}>{note}</p>) : <p>Text extraction looked stable for this source.</p>}
          {result.artifacts.length > 0 ? result.artifacts.slice(0, 3).map((artifact) => <ArtifactPreview key={`${artifact.kind}-${artifact.label}`} artifact={artifact} />) : null}
        </div>
        <div className="rounded-lg border border-white/10 bg-black/20 p-3">
          <button
            type="button"
            onClick={onToggleExtractedText}
            className="text-xs font-semibold uppercase tracking-[0.16em] text-white/60 transition-colors hover:text-white"
          >
            {showExtractedText ? 'Hide extracted text' : 'View extracted text'}
          </button>
          {showExtractedText ? (
            <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-white/72">
              {result.extractedTextPreview || 'No extraction preview available.'}
            </p>
          ) : (
            <p className="mt-3 text-sm leading-6 text-white/45">
              Preview is available for review before converting this into a BioStack protocol.
            </p>
          )}
        </div>
      </div>
    </section>
  );
}

function TrustMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-white/10 bg-black/20 p-3">
      <p className="text-xs font-semibold uppercase tracking-[0.14em] text-white/40">{label}</p>
      <p className="mt-2 text-sm font-semibold text-white/82">{value}</p>
    </div>
  );
}

function ArtifactPreview({ artifact }: { artifact: ProtocolAnalyzerArtifact }) {
  return (
    <div className="rounded-lg border border-white/10 bg-black/20 p-3">
      <p className="text-xs font-semibold uppercase tracking-[0.14em] text-white/42">{artifact.label}</p>
      <p className="mt-2 text-sm leading-6 text-white/68">{artifact.preview}</p>
    </div>
  );
}

function WhatThisMeansPanel({ message }: { message: string }) {
  return (
    <section className="rounded-lg border border-emerald-300/18 bg-emerald-400/[0.08] p-4">
      <h2 className="text-lg font-semibold text-white">What this means</h2>
      <p className="mt-2 text-sm leading-6 text-white/68">{message}</p>
    </section>
  );
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

function OriginalVsOptimizedSection({
  result,
  optimized,
}: {
  result: ProtocolAnalyzerResult;
  optimized: OptimizedProtocolView | null;
}) {
  const optimizedScore = optimized?.score ?? result.score;
  const delta = optimizedScore - result.score;

  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-semibold text-white">Original vs BioStack Version</h2>
        <span className="rounded-lg border border-emerald-300/20 bg-emerald-400/10 px-3 py-1.5 text-sm font-semibold text-emerald-100">
          {result.score} -&gt; {optimizedScore} ({formatDelta(delta)})
        </span>
      </div>
      <div className="mt-4 grid gap-4 lg:grid-cols-[1fr_auto_1fr]">
        <ProtocolComparisonList title="Original protocol" entries={result.protocol} empty="No original compounds parsed." />
        <div className="hidden items-center justify-center text-white/35 lg:flex">-&gt;</div>
        <ProtocolComparisonList
          title={optimized?.label ?? 'BioStack optimized protocol'}
          entries={optimized?.protocol ?? result.protocol}
          empty="No optimized protocol surfaced yet."
          accent
        />
      </div>
    </section>
  );
}

function ProtocolComparisonList({
  title,
  entries,
  empty,
  accent = false,
}: {
  title: string;
  entries: ProtocolAnalyzerResult['protocol'];
  empty: string;
  accent?: boolean;
}) {
  return (
    <div className={`rounded-lg border p-3 ${accent ? 'border-emerald-300/20 bg-emerald-400/[0.07]' : 'border-white/10 bg-black/20'}`}>
      <p className="text-sm font-semibold text-white">{title}</p>
      {entries.length > 0 ? (
        <ul className="mt-3 space-y-2">
          {entries.map((entry) => (
            <li key={`${title}-${entry.compoundName}-${entry.dose}-${entry.frequency}`} className="text-sm leading-6 text-white/68">
              <span className="font-semibold text-white/86">{entry.compoundName}</span>
              <span> {formatDose(entry)} {entry.frequency || 'frequency unclear'} {entry.duration ? `for ${entry.duration}` : ''}</span>
            </li>
          ))}
        </ul>
      ) : (
        <p className="mt-3 text-sm text-white/45">{empty}</p>
      )}
    </div>
  );
}

function WhyBetterBlocks({
  result,
  optimized,
}: {
  result: ProtocolAnalyzerResult;
  optimized: OptimizedProtocolView | null;
}) {
  const counterfactuals = result.counterfactuals;
  const removed = optimized?.removed ?? counterfactuals?.bestRemoveOne?.slice(0, 2).map((item) => item.removedCompound) ?? [];
  const optimizedNames = new Set((optimized?.protocol ?? result.protocol).map((entry) => entry.compoundName.toLowerCase()));
  const originalNames = result.protocol.map((entry) => entry.compoundName);
  const retained = originalNames.filter((name) => optimizedNames.has(name.toLowerCase()));
  const issueCompounds = result.issues.flatMap((issue) => issue.compounds).filter(Boolean);
  const swap = counterfactuals?.bestSwapOne?.[0] ?? null;

  const blocks = [
    {
      title: 'Reduced pathway overlap',
      body:
        issueCompounds.length > 0
          ? `BioStack flagged overlap around ${unique(issueCompounds).slice(0, 3).join(', ')} and tested a cleaner version.`
          : retained.length > 1
            ? `The improved version keeps ${retained.slice(0, 3).join(', ')} while reducing noisy stack interactions.`
            : 'No major overlap pattern dominated this protocol.',
    },
    {
      title: 'Removed redundant compounds',
      body:
        removed.length > 0
          ? `${removed.slice(0, 3).join(', ')} ${removed.length === 1 ? 'was' : 'were'} the clearest simplification target.`
          : 'No obvious removal improved the stack under the current rules.',
    },
    {
      title: 'Improved goal alignment',
      body:
        swap
          ? `${swap.candidateCompound} scored better than ${swap.originalCompound} for this goal-aware path.`
          : optimized
            ? `${optimized.label} scored ${optimized.score}, giving the ${goalText(optimized)} path a clearer fit.`
            : `The current stack scored ${result.score}, with no stronger goal-aware variant available yet.`,
    },
    {
      title: 'Simplified protocol complexity',
      body:
        optimized && optimized.protocol.length < result.protocol.length
          ? `Compound count drops from ${result.protocol.length} to ${optimized.protocol.length}, making attribution easier.`
          : `BioStack parsed ${result.protocol.length} item${result.protocol.length === 1 ? '' : 's'} and highlighted where clarity is still missing.`,
    },
  ];

  return (
    <div className="grid gap-3">
      {blocks.map((block) => (
        <article key={block.title} className="rounded-lg border border-white/10 bg-black/20 p-3">
          <p className="text-sm font-semibold text-white">{block.title}</p>
          <p className="mt-1 text-sm leading-6 text-white/58">{block.body}</p>
        </article>
      ))}
    </div>
  );
}

function FeatureChip({ label }: { label: string }) {
  return <span className="rounded-lg border border-white/10 px-3 py-1.5">{label}</span>;
}

function ExampleButton({ label, onClick }: { label: string; onClick: () => void }) {
  return (
    <button type="button" onClick={onClick} className="rounded-lg border border-white/10 px-3 py-2 text-left text-sm font-semibold text-white/70 transition-colors hover:border-white/20 hover:text-white">
      {label}
    </button>
  );
}

function AnalyzerProgressCard({ mode }: { mode: ProtocolAnalyzerInputType }) {
  const progressSteps =
    mode === 'CameraScan'
      ? ['Reading image', 'Extracting text from photo', 'Resolving compound aliases', 'Checking pathway overlap', 'Scoring protocol', 'Testing improvements']
      : mode === 'Link'
        ? ['Fetching shared document', 'Extracting text', 'Resolving compound aliases', 'Checking pathway overlap', 'Scoring protocol', 'Testing improvements']
        : mode === 'FileUpload'
          ? ['Extracting text', 'Reading table structure', 'Resolving compound aliases', 'Checking pathway overlap', 'Scoring protocol', 'Testing improvements']
          : ['Extracting text', 'Normalizing protocol rows', 'Resolving compound aliases', 'Checking pathway overlap', 'Scoring protocol', 'Testing improvements'];

  return (
    <section className="mt-4 rounded-lg border border-white/10 bg-black/20 p-4">
      <p className="text-sm font-semibold text-white">Analysis in progress</p>
      <ul className="mt-3 space-y-2 text-sm text-white/62">
        {progressSteps.map((step, index) => (
          <li key={step} className="flex items-center gap-3">
            <span className="inline-flex h-6 w-6 items-center justify-center rounded-full border border-white/10 text-xs text-white/75">{index + 1}</span>
            <span>{step}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}

function ParsedProtocolSection({ result }: { result: ProtocolAnalyzerResult | null }) {
  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      <div className="flex items-center justify-between gap-3">
        <h2 className="text-lg font-semibold text-white">Parsed Protocol</h2>
        {result?.decomposedBlends.length ? (
          <span className="rounded-lg border border-emerald-300/20 bg-emerald-400/10 px-3 py-1 text-xs font-semibold uppercase tracking-[0.14em] text-emerald-100">
            Blend detected
          </span>
        ) : null}
      </div>

      {result?.decomposedBlends.length ? (
        <div className="mt-4 rounded-lg border border-white/10 bg-black/20 p-3 text-sm text-white/58">
          {result.decomposedBlends.map((blend) => (
            <p key={blend.blendName}>
              {blend.blendName}: {blend.components.join(', ')}
            </p>
          ))}
        </div>
      ) : null}

      <div className="mt-4 hidden overflow-x-auto md:block">
        <table className="w-full min-w-[560px] text-left text-sm">
          <thead className="border-b border-white/10 text-xs uppercase tracking-[0.16em] text-white/35">
            <tr>
              <th className="py-3 font-semibold">Compound</th>
              <th className="py-3 font-semibold">Dose</th>
              <th className="py-3 font-semibold">Frequency</th>
              <th className="py-3 font-semibold">Duration</th>
              <th className="py-3 font-semibold">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/[0.06] text-white/72">
            {result && result.protocol.length > 0 ? (
              result.protocol.map((entry) => (
                <tr key={`${entry.compoundName}-${entry.dose}-${entry.frequency}`}>
                  <td className="py-3 font-semibold text-white">{entry.compoundName}</td>
                  <td className="py-3">{entry.dose > 0 ? `${entry.dose} ${entry.unit}` : 'Unknown'}</td>
                  <td className="py-3">{entry.frequency || 'Unknown'}</td>
                  <td className="py-3">{entry.duration || 'Unspecified'}</td>
                  <td className="py-3">
                    {entry.dose > 0 ? (
                      <span className="rounded-lg border border-emerald-300/20 bg-emerald-400/10 px-2 py-1 text-xs font-semibold text-emerald-100">Canonicalized</span>
                    ) : (
                      <span className="rounded-lg border border-amber-300/20 bg-amber-400/10 px-2 py-1 text-xs font-semibold text-amber-100">Partial</span>
                    )}
                  </td>
                </tr>
              ))
            ) : (
              <tr>
                <td className="py-5 text-white/45" colSpan={5}>No parsed compounds yet.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="mt-4 grid gap-3 md:hidden">
        {result?.protocol.length ? (
          result.protocol.map((entry) => (
            <article key={`${entry.compoundName}-${entry.dose}-${entry.frequency}`} className="rounded-lg border border-white/10 bg-black/20 p-3">
              <p className="font-semibold text-white">{entry.compoundName}</p>
              <p className="mt-2 text-sm text-white/65">Dose: {entry.dose > 0 ? `${entry.dose} ${entry.unit}` : 'Unknown'}</p>
              <p className="text-sm text-white/65">Frequency: {entry.frequency || 'Unknown'}</p>
              <p className="text-sm text-white/65">Duration: {entry.duration || 'Unspecified'}</p>
            </article>
          ))
        ) : (
          <p className="text-sm text-white/45">No parsed compounds yet.</p>
        )}
      </div>
    </section>
  );
}

function ResultList({ title, empty, items }: { title: string; empty: string; items: string[] }) {
  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      <h2 className="text-lg font-semibold text-white">{title}</h2>
      {items.length > 0 ? (
        <ul className="mt-3 space-y-2">
          {items.map((item) => (
            <li key={item} className="flex gap-2 text-sm leading-6 text-white/68">
              <span aria-hidden="true" className="mt-1 shrink-0 text-amber-200/80">!</span>
              <span>{item}</span>
            </li>
          ))}
        </ul>
      ) : (
        <p className="mt-3 text-sm leading-6 text-white/45">{empty}</p>
      )}
    </section>
  );
}

function ImprovementCard({
  title,
  teaser,
  empty,
  emptyDetail,
  kind,
}: {
  title: string;
  teaser: ProtocolAnalyzerCounterfactual | ProtocolAnalyzerSwap | null;
  empty: string;
  emptyDetail: string;
  kind: 'remove' | 'swap';
}) {
  return (
    <article className="rounded-lg border border-white/10 bg-black/20 p-4">
      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/40">{title}</p>
      {teaser ? (
        <>
          <p className="mt-2 text-base font-semibold text-white">
            {kind === 'remove'
              ? `Remove ${(teaser as ProtocolAnalyzerCounterfactual).removedCompound}`
              : `Swap ${(teaser as ProtocolAnalyzerSwap).originalCompound} -> ${(teaser as ProtocolAnalyzerSwap).candidateCompound}`}
          </p>
          <p className="mt-1 text-sm text-emerald-100/85">New score {Math.round(teaser.variantScore)} ({formatDelta(teaser.deltaScore)})</p>
          <p className="mt-2 text-sm leading-6 text-white/58">{teaser.recommendation}</p>
        </>
      ) : (
        <>
          <p className="mt-2 text-base font-semibold text-white">{empty}</p>
          <p className="mt-2 text-sm leading-6 text-white/55">{emptyDetail}</p>
        </>
      )}
    </article>
  );
}

function SimplifiedProtocolCard({ protocol }: { protocol: ProtocolAnalyzerResult['counterfactuals']['bestSimplifiedProtocol'] }) {
  return (
    <article className="rounded-lg border border-white/10 bg-black/20 p-4">
      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/40">Simplified protocol</p>
      {protocol ? (
        <>
          <p className="mt-2 text-base font-semibold text-white">{protocol.score} / 100 after simplification</p>
          <p className="mt-2 text-sm leading-6 text-white/58">Removed: {protocol.removed.join(', ')}</p>
          <p className="mt-2 text-sm leading-6 text-white/58">{protocol.reasons?.[0] ?? 'BioStack found a simpler variant, but did not return a detailed reason.'}</p>
        </>
      ) : (
        <>
          <p className="mt-2 text-base font-semibold text-white">No cleaner version beat the current stack.</p>
          <p className="mt-2 text-sm leading-6 text-white/55">
            This protocol may already be compact, or the system needs more profile context before simplifying it safely.
          </p>
        </>
      )}
    </article>
  );
}

function GoalAwareCard({ option }: { option: ProtocolAnalyzerGoalAwareOption | null }) {
  return (
    <article className="rounded-lg border border-white/10 bg-black/20 p-4">
      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/40">Goal-aware optimization</p>
      {option ? (
        <>
          <p className="mt-2 text-base font-semibold text-white">Best fit for {option.goal}</p>
          <p className="mt-1 text-sm text-emerald-100/85">Score {option.score}</p>
          <p className="mt-2 text-sm leading-6 text-white/58">{option.reasons?.[0] ?? 'BioStack found a goal-aware variant, but did not return a detailed reason.'}</p>
        </>
      ) : (
        <>
          <p className="mt-2 text-base font-semibold text-white">Goal-aware optimization needs more context.</p>
          <p className="mt-2 text-sm leading-6 text-white/55">
            Add profile details or unlock full optimization to compare stronger alternatives for this goal.
          </p>
        </>
      )}
    </article>
  );
}

function ScoreChip({ label, value, tone }: { label: string; value: number; tone: 'positive' | 'negative' | 'neutral' }) {
  const toneClass =
    tone === 'positive'
      ? 'border-emerald-300/20 bg-emerald-400/10 text-emerald-50'
      : tone === 'negative'
        ? 'border-red-300/20 bg-red-400/10 text-red-50'
        : 'border-white/10 bg-white/[0.04] text-white';

  return (
    <div className={`rounded-lg border p-3 ${toneClass}`}>
      <p className="text-xs font-semibold uppercase tracking-[0.16em] opacity-70">{label}</p>
      <p className="mt-2 text-lg font-semibold">{value > 0 ? `+${value}` : value}</p>
    </div>
  );
}

function buildParserWarnings(result: ProtocolAnalyzerResult | null): string[] {
  if (!result) return [];
  const warnings: string[] = [];
  if (result.unknownCompounds.length > 0) {
    warnings.push(`${result.unknownCompounds.length} compound${result.unknownCompounds.length === 1 ? '' : 's'} could not be fully normalized.`);
  }
  if (result.decomposedBlends.length > 0) {
    warnings.push(`${result.decomposedBlends.length} blend${result.decomposedBlends.length === 1 ? '' : 's'} exploded into constituent compounds.`);
  }
  if (result.protocol.some((entry) => !entry.frequency || entry.dose === 0)) {
    warnings.push('One or more items were only partially extracted from the source text.');
  }
  return warnings;
}

function scoreSummary(score: number): string {
  if (score >= 80) return 'Strong base with room to tighten the protocol further.';
  if (score >= 60) return 'Solid starting point, but there are cleaner ways to structure it.';
  return 'Mixed stack with avoidable redundancy or weak attribution clarity.';
}

function getScoreLabel(score: number | undefined): string {
  if (score === undefined) return 'Not scored yet';
  if (score >= 85) return 'Excellent fit';
  if (score >= 70) return 'Strong fit';
  if (score >= 55) return 'Mixed fit';
  if (score >= 40) return 'Inefficient';
  return 'High concern';
}

function getScoreBand(score: number | undefined): string {
  if (score === undefined) return 'unknown';
  if (score >= 85) return 'excellent_fit';
  if (score >= 70) return 'strong_fit';
  if (score >= 55) return 'mixed_fit';
  if (score >= 40) return 'inefficient';
  return 'high_concern';
}

function getScoreInsight(result: ProtocolAnalyzerResult | null, optimized: OptimizedProtocolView | null): string {
  if (!result) {
    return 'Run an analysis to score the protocol.';
  }

  const redundancy = result.issues.find((issue) => issue.type === 'redundancy');
  const overlap = result.issues.find((issue) => issue.type === 'overlap');
  const excessive = result.issues.find((issue) => issue.type === 'excessive_compounds');
  const removal = result.counterfactuals?.bestRemoveOne?.[0];

  if (overlap?.compounds.length) {
    return `Overlap involving ${overlap.compounds.slice(0, 2).join(' and ')} is reducing protocol efficiency.`;
  }

  if (redundancy?.compounds.length) {
    return `Useful coverage is present, but redundancy around ${redundancy.compounds.slice(0, 2).join(' and ')} is lowering the score.`;
  }

  if (removal && removal.deltaScore > 0) {
    return `Removing ${removal.removedCompound} may improve clarity by ${formatDelta(removal.deltaScore)} points.`;
  }

  if (excessive) {
    return 'This stack may be harder to evaluate cleanly because several compounds are layered together.';
  }

  if (optimized && optimized.score > result.score) {
    return `BioStack found a cleaner version that scores ${formatDelta(optimized.score - result.score)} points higher.`;
  }

  if (result.protocol.length <= 3 && result.score >= 55) {
    return 'This protocol is compact and reasonably aligned with the selected goal.';
  }

  return scoreSummary(result.score);
}

function getWhatThisMeans(result: ProtocolAnalyzerResult | null, optimized: OptimizedProtocolView | null): string {
  if (!result) {
    return '';
  }

  const issueCompounds = unique(result.issues.flatMap((issue) => issue.compounds).filter(Boolean));
  if (optimized && optimized.score > result.score && optimized.protocol.length < result.protocol.length) {
    return 'BioStack found a cleaner path toward the same goal with fewer overlapping signals.';
  }

  if (issueCompounds.length > 1) {
    return `This stack may be harder to evaluate cleanly because ${issueCompounds.slice(0, 3).join(', ')} create overlapping signals.`;
  }

  if ((result.counterfactuals?.bestSwapOne?.length ?? 0) > 0) {
    return 'BioStack found a possible replacement path worth comparing before committing this protocol to tracking.';
  }

  return 'This protocol appears relatively lean, but saving it lets you track whether the expected effects show up over time.';
}

function recommendationCount(result: ProtocolAnalyzerResult): number {
  return (
    (result.counterfactuals?.bestRemoveOne?.length ?? 0) +
    (result.counterfactuals?.bestSwapOne?.length ?? 0) +
    (result.counterfactuals?.bestSimplifiedProtocol ? 1 : 0) +
    (result.counterfactuals?.goalAwareOptions?.length ?? 0)
  );
}

function formatDelta(value: number): string {
  const rounded = Math.round(value);
  return rounded > 0 ? `+${rounded}` : `${rounded}`;
}

function pickOptimizedProtocol(result: ProtocolAnalyzerResult | null): OptimizedProtocolView | null {
  if (!result) {
    return null;
  }

  const counterfactuals = result.counterfactuals;
  const simplified = counterfactuals?.bestSimplifiedProtocol;
  if (simplified) {
    return {
      label: 'BioStack simplified protocol',
      protocol: simplified.compounds,
      score: simplified.score,
      removed: simplified.removed,
    };
  }

  const goalAware = counterfactuals?.goalAwareOptions?.[0];
  if (goalAware) {
    return {
      label: `BioStack version for ${goalAware.goal}`,
      protocol: goalAware.compounds,
      score: goalAware.score,
      removed: result.protocol
        .filter((entry) => !goalAware.compounds.some((candidate) => candidate.compoundName.toLowerCase() === entry.compoundName.toLowerCase()))
        .map((entry) => entry.compoundName),
    };
  }

  return null;
}

function currentRawInput(mode: ProtocolAnalyzerInputType, inputText: string, linkUrl: string, selectedFile: File | null): string {
  if (mode === 'Paste') {
    return inputText;
  }

  if (mode === 'Link') {
    return linkUrl;
  }

  return selectedFile ? `${selectedFile.name} (${selectedFile.type || 'unknown type'}, ${selectedFile.size} bytes)` : '';
}

function formatAnalyzerError(error: unknown, mode: ProtocolAnalyzerInputType): string {
  const message = error instanceof Error ? error.message : 'Protocol analysis failed.';
  if (
    mode === 'CameraScan' &&
    (/ocr/i.test(message) || /image/i.test(message) || /read text/i.test(message) || /not configured/i.test(message))
  ) {
    return 'Scan is temporarily unavailable. Upload a PDF, spreadsheet, or paste text to analyze now.';
  }

  return message;
}

function sourceTypeLabel(result: ProtocolAnalyzerResult): string {
  if (result.inputType === 'Link') {
    return 'Link';
  }

  if (result.inputType === 'CameraScan') {
    return 'Image scan';
  }

  if (result.inputType === 'Paste') {
    return 'Pasted text';
  }

  const extension = result.sourceName?.split('.').pop()?.toUpperCase();
  return extension ? extension : 'File';
}

function confidenceLabel(result: ProtocolAnalyzerResult): string {
  if (result.lowConfidenceExtraction) {
    return 'Low';
  }

  if (result.extractionWarnings.length > 0 || result.parserWarnings.length > 0) {
    return 'Medium';
  }

  return 'High';
}

function formatDose(entry: ProtocolAnalyzerResult['protocol'][number]): string {
  if (entry.dose <= 0) {
    return '';
  }

  return `${entry.dose} ${entry.unit}`.trim();
}

function unique(values: string[]): string[] {
  return Array.from(new Set(values.map((value) => value.trim()).filter(Boolean)));
}

function goalText(optimized: OptimizedProtocolView): string {
  return optimized.label.replace(/^BioStack version for /, '');
}
