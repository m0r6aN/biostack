'use client';

import Link from 'next/link';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { apiClient } from '@/lib/api';
import { useAuth } from '@/lib/AuthProvider';
import {
  deleteAnonymousToolArtifact,
  readAnonymousToolPayload,
  saveAnonymousToolArtifact,
  type AnonymousToolPayload,
  type SavedToolArtifact,
  type ToolMode,
} from '@/lib/anonymousTools';
import { useProfile } from '@/lib/context';
import {
  calculateUnifiedDosing,
  DEFAULT_UNIFIED_DOSING_INPUT,
  formatDose,
  formatNumber,
  toMcg,
  type ConcentrationUnit,
  type MassUnit,
  type UnifiedDosingInput,
} from '@/lib/dosingCalculator';
import { type CompoundRecord, type InteractionFlag, type KnowledgeEntry } from '@/lib/types';

type SurfaceMode = ToolMode;
type BlendStatus = 'compatible' | 'caution' | 'avoid' | 'unknown';

interface ToolsDecisionSurfaceProps {
  initialMode?: SurfaceMode;
  compactIntro?: boolean;
}

const quickCompounds = ['BPC-157', 'TB-500', 'NAD+'];
const massUnits: MassUnit[] = ['mcg', 'mg', 'g'];
const concentrationUnits: ConcentrationUnit[] = ['mcg/mL', 'mg/mL'];
const conversionUnits: MassUnit[] = ['mcg', 'mg', 'g'];
const RECENT_COMPOUNDS_KEY = 'biostack.tools.recentCompounds.v1';

const modeCopy: Record<SurfaceMode, { label: string; title: string; description: string }> = {
  dose: {
    label: 'Dose it right',
    title: 'Dose volume',
    description: 'Use powder, solution volume, and target dose to get a draw amount.',
  },
  mix: {
    label: 'Mix correctly',
    title: 'Reconstitution',
    description: 'Calculate concentration and keep handling steps next to the result.',
  },
  convert: {
    label: 'Convert units',
    title: 'Unit converter',
    description: 'Convert mcg, mg, and g without leaving the tool.',
  },
};

export function ToolsDecisionSurface({ initialMode = 'dose', compactIntro = false }: ToolsDecisionSurfaceProps) {
  const { user } = useAuth();
  const { currentProfileId, profiles, setProfiles, setCurrentProfileId } = useProfile();
  const [mode, setMode] = useState<SurfaceMode>(initialMode);
  const [compound, setCompound] = useState('BPC-157');
  const [additionalCompound, setAdditionalCompound] = useState('TB-500');
  const [input, setInput] = useState<UnifiedDosingInput>(DEFAULT_UNIFIED_DOSING_INPUT);
  const [conversion, setConversion] = useState({ amount: 1000, fromUnit: 'mcg' as MassUnit, toUnit: 'mg' as MassUnit });
  const [knowledge, setKnowledge] = useState<KnowledgeEntry[]>([]);
  const [stackCompounds, setStackCompounds] = useState<CompoundRecord[]>([]);
  const [recentCompounds, setRecentCompounds] = useState<string[]>([]);
  const [compatibility, setCompatibility] = useState<InteractionFlag[]>([]);
  const [compatibilityState, setCompatibilityState] = useState<'idle' | 'checking' | 'checked' | 'error'>('idle');
  const [stackFlags, setStackFlags] = useState<InteractionFlag[]>([]);
  const [stackInsightState, setStackInsightState] = useState<'idle' | 'checking' | 'checked' | 'error'>('idle');
  const [saveMessage, setSaveMessage] = useState('');
  const [savedPayload, setSavedPayload] = useState<AnonymousToolPayload | null>(null);
  const [showSaved, setShowSaved] = useState(false);
  const [trackState, setTrackState] = useState<'idle' | 'tracking'>('idle');
  const [mobileOpen, setMobileOpen] = useState({
    reconstitution: false,
    storage: false,
    blend: false,
    email: false,
  });

  const activeProfileId = currentProfileId ?? profiles[0]?.id ?? null;
  const knowledgeNames = useMemo(() => knowledge.map((entry) => entry.canonicalName).filter(Boolean), [knowledge]);
  const savedCalculations = useMemo(
    () => [...(savedPayload?.savedCalculations ?? [])].sort((a, b) => b.updatedAt.localeCompare(a.updatedAt)),
    [savedPayload]
  );

  const dosing = useMemo(() => {
    try {
      return { result: calculateUnifiedDosing(input), error: '' };
    } catch (error) {
      return { result: null, error: error instanceof Error ? error.message : 'Calculation failed' };
    }
  }, [input]);

  const conversionResult = useMemo(() => {
    try {
      const from = toMcg(conversion.amount, conversion.fromUnit);
      const divisor = toMcg(1, conversion.toUnit);
      return { value: from / divisor, error: '' };
    } catch {
      return { value: 0, error: 'Conversion failed' };
    }
  }, [conversion]);

  const reconstitutionInstructions = useMemo(() => {
    const solutionVolume = formatNumber(input.diluentVolumeMl, 2);
    return [
      'Clean vial tops with alcohol.',
      `Draw ${solutionVolume} mL of solution.`,
      'Insert the needle into the vial.',
      'Tilt the vial slightly.',
      'Slowly inject solution down the inside wall so it dissolves gently.',
      'Do not shake.',
      'Gently swirl until dissolved.',
      'Discard the used needle appropriately.',
    ];
  }, [input.diluentVolumeMl]);

  const storageInstructions = [
    'Store reconstituted solution in the refrigerator.',
    'Avoid door shelves because of temperature fluctuation.',
    'Store unmixed powder cold for longevity.',
  ];

  const primaryAnswer = dosing.result
    ? `Draw to ${formatNumber(dosing.result.u100UnitsPerAdministration, 1)} units on a 1 mL insulin syringe`
    : 'Enter valid numbers to calculate';
  const secondaryAnswer = dosing.result ? `${formatNumber(dosing.result.volumePerAdministrationMl, 4)} mL per dose` : dosing.error;
  const blendResult = useMemo(
    () => summarizeBlend(compatibilityState, compatibility, compound, additionalCompound, knowledge),
    [additionalCompound, compatibility, compatibilityState, compound, knowledge]
  );
  const stackInsights = useMemo(
    () => buildStackInsights(compound, stackCompounds, stackFlags, stackInsightState),
    [compound, stackCompounds, stackFlags, stackInsightState]
  );

  const refreshSaved = useCallback(() => setSavedPayload(readAnonymousToolPayload()), []);
  const selectCompound = useCallback((value: string) => {
    const next = value.trim();
    setCompound(next);
    setRecentCompounds((current) => persistRecent(next, current));
  }, []);

  useEffect(() => {
    refreshSaved();
    setRecentCompounds(readRecentCompounds());
    const params = new URLSearchParams(window.location.search);
    if (params.get('saved') === 'open') setShowSaved(true);
    if (params.get('blend') === 'compatible') setCompatibilityState('checked');
    if (params.get('stackDemo') === '1') {
      setStackCompounds([
        demoCompound('BPC-157'),
        demoCompound('TB-500'),
        demoCompound('NAD+'),
      ]);
    }
    void apiClient.getAllKnowledgeCompounds().then(setKnowledge).catch(() => setKnowledge([]));
  }, [refreshSaved]);

  useEffect(() => {
    if (!user) {
      setStackCompounds([]);
      return;
    }

    async function loadStack() {
      try {
        let profileId = activeProfileId;
        if (!profileId) {
          const loadedProfiles = await apiClient.getProfiles();
          setProfiles(loadedProfiles);
          profileId = loadedProfiles[0]?.id ?? null;
          if (profileId) setCurrentProfileId(profileId);
        }
        if (profileId) setStackCompounds(await apiClient.getCompounds(profileId));
      } catch {
        setStackCompounds([]);
      }
    }

    void loadStack();
  }, [activeProfileId, setCurrentProfileId, setProfiles, user]);

  useEffect(() => {
    const names = [compound, ...stackCompounds.map((item) => item.name)].map((name) => name.trim()).filter(Boolean);
    if (!compound.trim() || names.length < 2) {
      setStackFlags([]);
      setStackInsightState('idle');
      return;
    }

    const timeout = window.setTimeout(() => {
      setStackInsightState('checking');
      void apiClient
        .checkOverlap(Array.from(new Set(names)))
        .then((findings) => {
          setStackFlags(findings);
          setStackInsightState('checked');
        })
        .catch(() => {
          setStackFlags([]);
          setStackInsightState('error');
        });
    }, 250);

    return () => window.clearTimeout(timeout);
  }, [compound, stackCompounds]);

  async function checkCompatibility() {
    const stackNames = user ? stackCompounds.map((item) => item.name) : [];
    const names = [compound, additionalCompound, ...stackNames].map((name) => name.trim()).filter(Boolean);
    if (names.length < 2) {
      setCompatibilityState('checked');
      setCompatibility([]);
      return;
    }

    try {
      setCompatibilityState('checking');
      setCompatibility(await apiClient.checkOverlap(Array.from(new Set(names))));
      setCompatibilityState('checked');
    } catch {
      setCompatibility([]);
      setCompatibilityState('error');
    }
  }

  function saveCalculation() {
    const payload = saveCurrentArtifact();
    setSavedPayload(payload);
    setShowSaved(true);
    setSaveMessage(
      user
        ? 'Saved with your current tool data on this device.'
        : 'Saved on this device. Create a profile to keep it permanently across devices.'
    );
  }

  function saveCurrentArtifact(): AnonymousToolPayload {
    if (mode === 'convert') {
      return saveAnonymousToolArtifact({
        calculatorType: 'convert',
        substances: [],
        inputs: conversion,
        outputs: {
          primaryAnswer: `${formatNumber(conversionResult.value, 4)} ${conversion.toUnit}`,
          result: conversionResult.value,
        },
        reconstitutionInstructions: [],
        storageInstructions: [],
        compatibilityFindings: [],
      });
    }

    if (!dosing.result) {
      throw new Error('Calculation is not ready.');
    }

    return saveAnonymousToolArtifact({
      calculatorType: mode,
      substances: [compound, additionalCompound].filter((name) => name.trim()),
      inputs: { ...input, primaryCompound: compound, additionalCompound },
      outputs: {
        primaryAnswer,
        secondaryAnswer,
        concentrationMcgPerMl: dosing.result.concentrationMcgPerMl,
        concentrationMgPerMl: dosing.result.concentrationMgPerMl,
        dosePerAdministrationMcg: dosing.result.dosePerAdministrationMcg,
        volumePerAdministrationMl: dosing.result.volumePerAdministrationMl,
        u100UnitsPerAdministration: dosing.result.u100UnitsPerAdministration,
        formula: dosing.result.formula,
      },
      reconstitutionInstructions: input.concentrationSource === 'reconstitution' ? reconstitutionInstructions : [],
      storageInstructions: input.concentrationSource === 'reconstitution' ? storageInstructions : [],
      compatibilityFindings: compatibility,
    });
  }

  function openSaved(artifact: SavedToolArtifact) {
    setMode(artifact.calculatorType);
    if (artifact.calculatorType === 'convert') {
      setConversion({
        amount: numberFromInput(artifact.inputs.amount, 1000),
        fromUnit: massUnitFromInput(artifact.inputs.fromUnit, 'mcg'),
        toUnit: massUnitFromInput(artifact.inputs.toUnit, 'mg'),
      });
    } else {
      setInput(restoreDosingInput(artifact.inputs));
      selectCompound(stringFromInput(artifact.inputs.primaryCompound, artifact.substances[0] ?? compound));
      setAdditionalCompound(stringFromInput(artifact.inputs.additionalCompound, artifact.substances[1] ?? ''));
      setCompatibility(artifact.compatibilityFindings as InteractionFlag[]);
      setCompatibilityState(artifact.compatibilityFindings.length > 0 ? 'checked' : 'idle');
    }
    setSaveMessage('Calculation opened. Edit any field, then save again.');
  }

  function deleteSaved(artifactId: string) {
    setSavedPayload(deleteAnonymousToolArtifact(artifactId));
    setSaveMessage('Saved calculation deleted.');
  }

  async function trackInStack() {
    const payload = saveCurrentArtifact();
    setSavedPayload(payload);

    if (!user || !activeProfileId) {
      window.location.href = '/auth/signin?callbackUrl=/profiles?bootstrap=tools';
      return;
    }

    try {
      setTrackState('tracking');
      const exists = stackCompounds.some((item) => item.name.toLowerCase() === compound.trim().toLowerCase());
      if (!exists && compound.trim()) {
        const created = await apiClient.createCompound(activeProfileId, {
          personId: activeProfileId,
          name: compound.trim(),
          category: 'Unknown',
          startDate: new Date().toISOString(),
          endDate: null,
          status: 'Active',
          notes: `Tracked from tools. ${primaryAnswer}`,
          sourceType: 'Manual',
          goal: '',
          source: 'Tools',
        });
        setStackCompounds((current) => [...current, created]);
      }

      await checkCompatibility();
      setSaveMessage(exists ? 'Already in your stack. Calculation saved.' : 'Tracked in your stack. Calculation saved.');
    } catch {
      setSaveMessage('Could not update your stack. Calculation stayed saved on this device.');
    } finally {
      setTrackState('idle');
    }
  }

  return (
    <main className="pb-24 md:pb-0">
      <section className="mx-auto grid max-w-7xl gap-6 px-4 pb-8 pt-8 sm:px-6 lg:grid-cols-[0.92fr_1.08fr] lg:px-8 lg:pb-12 lg:pt-12">
        <div className="space-y-5">
          {!compactIntro && (
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-emerald-300/70">Start here</p>
              <h1 className="mt-3 max-w-3xl text-4xl font-semibold tracking-tight text-white sm:text-5xl">
                Dose it right. Mix correctly. Blend safely.
              </h1>
              <p className="mt-4 max-w-2xl text-base leading-7 text-white/62 sm:text-lg">
                Free calculators, reconstitution guidance, and compatibility checks. No account required.
              </p>
            </div>
          )}

          <CompoundChooser
            compound={compound}
            onSelect={selectCompound}
            quickCompounds={quickCompounds}
            knowledgeNames={knowledgeNames}
            stackCompounds={stackCompounds}
            recentCompounds={recentCompounds}
            isAuthenticated={Boolean(user) || stackCompounds.length > 0}
          />

          <ModeTabs mode={mode} setMode={setMode} />

          <section className="rounded-lg border border-white/[0.08] bg-[#121923]/95 p-4 shadow-[0_16px_48px_rgba(0,0,0,0.24)] sm:p-5">
            <div className="border-b border-white/[0.06] pb-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-300/70">{modeCopy[mode].label}</p>
              <h2 className="mt-2 text-2xl font-semibold tracking-tight text-white">{modeCopy[mode].title}</h2>
              <p className="mt-2 text-sm leading-6 text-white/55">{modeCopy[mode].description}</p>
            </div>

            {mode === 'convert' ? (
              <div className="mt-5 grid gap-4 sm:grid-cols-[1fr_0.8fr_0.8fr]">
                <NumberField label="Value" value={conversion.amount} onChange={(amount) => setConversion((current) => ({ ...current, amount }))} />
                <SelectField label="From" value={conversion.fromUnit} values={conversionUnits} onChange={(fromUnit) => setConversion((current) => ({ ...current, fromUnit }))} />
                <SelectField label="To" value={conversion.toUnit} values={conversionUnits} onChange={(toUnit) => setConversion((current) => ({ ...current, toUnit }))} />
              </div>
            ) : (
              <div className="mt-5 space-y-5">
                <div className="grid gap-4 sm:grid-cols-2">
                  <NumberWithUnitField label="Powder amount" value={input.powderAmount} unit={input.powderUnit} units={massUnits} onValueChange={(powderAmount) => setInput((current) => ({ ...current, powderAmount }))} onUnitChange={(powderUnit) => setInput((current) => ({ ...current, powderUnit }))} />
                  <NumberField label="Solution volume" suffix="mL" value={input.diluentVolumeMl} onChange={(diluentVolumeMl) => setInput((current) => ({ ...current, diluentVolumeMl }))} />
                  <NumberWithUnitField label="Desired dose" value={input.desiredDose} unit={input.desiredDoseUnit} units={massUnits} onValueChange={(desiredDose) => setInput((current) => ({ ...current, desiredDose }))} onUnitChange={(desiredDoseUnit) => setInput((current) => ({ ...current, desiredDoseUnit }))} />
                </div>

                <details className="rounded-lg border border-white/[0.08] bg-black/15 p-4">
                  <summary className="cursor-pointer text-sm font-semibold text-white/78">Advanced controls</summary>
                  <div className="mt-4 grid gap-4 sm:grid-cols-2">
                    <SelectField label="Concentration source" value={input.concentrationSource} values={['reconstitution', 'known']} onChange={(concentrationSource) => setInput((current) => ({ ...current, concentrationSource }))} />
                    {input.concentrationSource === 'known' && (
                      <NumberWithUnitField label="Known concentration" value={input.knownConcentration} unit={input.concentrationUnit} units={concentrationUnits} onValueChange={(knownConcentration) => setInput((current) => ({ ...current, knownConcentration }))} onUnitChange={(concentrationUnit) => setInput((current) => ({ ...current, concentrationUnit }))} />
                    )}
                    <SelectField label="Dose basis" value={input.doseBasis} values={['per-dose', 'daily-total', 'weekly-total']} onChange={(doseBasis) => setInput((current) => ({ ...current, doseBasis }))} />
                    <NumberField label={input.doseBasis === 'daily-total' ? 'Doses per day' : 'Doses per week'} value={input.splitCount} step="1" onChange={(splitCount) => setInput((current) => ({ ...current, splitCount }))} />
                  </div>
                </details>
              </div>
            )}
          </section>
        </div>

        <aside className="space-y-4 lg:sticky lg:top-24 lg:self-start">
          <ResultPanel mode={mode} primary={mode === 'convert' ? `${formatNumber(conversionResult.value, 4)} ${conversion.toUnit}` : primaryAnswer} secondary={mode === 'convert' ? `${formatNumber(conversion.amount)} ${conversion.fromUnit}` : secondaryAnswer} hasError={mode === 'convert' ? Boolean(conversionResult.error) : Boolean(dosing.error)} />
          {mode !== 'convert' && dosing.result && (
            <div className="grid gap-3 sm:grid-cols-2">
              <Metric label="Dose" value={formatDose(dosing.result.dosePerAdministrationMcg)} detail="per administration" />
              <Metric label="Concentration" value={`${formatNumber(dosing.result.concentrationMcgPerMl)} mcg/mL`} detail={`${formatNumber(dosing.result.concentrationMgPerMl, 4)} mg/mL`} />
            </div>
          )}
          {stackInsights.length > 0 && <InsightPanel title="Stack insights" items={stackInsights} />}

          {mode !== 'convert' && dosing.result && input.concentrationSource === 'reconstitution' && (
            <MobileAccordion title="Reconstitution steps" open={mobileOpen.reconstitution} onToggle={() => setMobileOpen((current) => ({ ...current, reconstitution: !current.reconstitution }))}>
              <InstructionList items={reconstitutionInstructions} />
            </MobileAccordion>
          )}

          {mode !== 'convert' && dosing.result && input.concentrationSource === 'reconstitution' && (
            <MobileAccordion title="Storage" open={mobileOpen.storage} onToggle={() => setMobileOpen((current) => ({ ...current, storage: !current.storage }))}>
              <InstructionList items={storageInstructions} />
            </MobileAccordion>
          )}

          <MobileAccordion title="Check blend safety" open={mobileOpen.blend} onToggle={() => setMobileOpen((current) => ({ ...current, blend: !current.blend }))}>
            <BlendSafetyPanel
              additionalCompound={additionalCompound}
              setAdditionalCompound={setAdditionalCompound}
              onCheck={() => void checkCompatibility()}
              state={compatibilityState}
              result={blendResult}
              knowledgeNames={knowledgeNames}
            />
          </MobileAccordion>

          <section className="rounded-lg border border-white/[0.08] bg-[#121923]/95 p-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <button type="button" onClick={saveCalculation} className="rounded-lg bg-emerald-400 px-4 py-3 text-sm font-bold text-[#07110c] transition-colors hover:bg-emerald-300">
                Save this calculation
              </button>
              <button type="button" onClick={() => void trackInStack()} disabled={trackState === 'tracking'} className="rounded-lg border border-white/12 px-4 py-3 text-center text-sm font-semibold text-white/82 transition-colors hover:border-white/24 hover:text-white disabled:opacity-60">
                {trackState === 'tracking' ? 'Tracking...' : 'Track this in my stack'}
              </button>
            </div>
            {saveMessage && <p className="mt-3 text-sm leading-6 text-emerald-100/78">{saveMessage}</p>}
            <p className="mt-3 text-xs leading-5 text-white/42">Math-only output. Not medical advice. Verify against your source.</p>
            <div className="mt-4 flex flex-wrap gap-3 text-sm">
              <Link href="/start" className="font-semibold text-emerald-200 hover:text-white">New to this? Start here</Link>
              <Link href="/map" className="font-semibold text-white/65 hover:text-white">Already have a stack? Open map</Link>
            </div>
            <button type="button" onClick={() => setShowSaved((current) => !current)} className="mt-4 w-full rounded-lg border border-white/10 px-4 py-3 text-sm font-semibold text-white/72 transition-colors hover:border-white/20 hover:text-white">
              Saved calculations ({savedCalculations.length})
            </button>
            {showSaved && <SavedCalculationsPanel items={savedCalculations} onOpen={openSaved} onDelete={deleteSaved} />}
          </section>

          <MobileAccordion title="Email me a printable reference card" open={mobileOpen.email} onToggle={() => setMobileOpen((current) => ({ ...current, email: !current.email }))}>
            <p className="text-sm leading-6 text-white/45">Optional. The tool works without email.</p>
          </MobileAccordion>
        </aside>
      </section>
    </main>
  );
}

function CompoundChooser({
  compound,
  onSelect,
  quickCompounds,
  knowledgeNames,
  stackCompounds,
  recentCompounds,
  isAuthenticated,
}: {
  compound: string;
  onSelect: (value: string) => void;
  quickCompounds: string[];
  knowledgeNames: string[];
  stackCompounds: CompoundRecord[];
  recentCompounds: string[];
  isAuthenticated: boolean;
}) {
  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      {isAuthenticated && stackCompounds.length > 0 && (
        <label className="mb-4 block">
          <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.18em] text-emerald-300/70">From your stack</span>
          <select value={compound} onChange={(event) => onSelect(event.target.value)} className="min-h-12 w-full rounded-lg border border-white/10 bg-[#0F141B] px-4 text-white outline-none transition-colors focus:border-emerald-400/45">
            <option value="">Choose a stack compound</option>
            {stackCompounds.map((item) => <option key={item.id} value={item.name}>{item.name}</option>)}
          </select>
        </label>
      )}

      <div className="flex flex-wrap gap-2" aria-label="Example compounds">
        {quickCompounds.map((item) => (
          <button key={item} type="button" onClick={() => onSelect(item)} className={`rounded-lg border px-3 py-2 text-sm font-semibold transition-colors ${compound === item ? 'border-emerald-300/45 bg-emerald-400/14 text-emerald-100' : 'border-white/10 bg-white/[0.03] text-white/62 hover:border-white/20 hover:text-white'}`}>
            {item}
          </button>
        ))}
      </div>

      <label className="mt-4 block">
        <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.18em] text-white/40">Search all compounds</span>
        <input list="biostack-compounds" value={compound} onChange={(event) => onSelect(event.target.value)} placeholder="Search or enter compound" className="min-h-12 w-full rounded-lg border border-white/10 bg-[#0F141B] px-4 text-white outline-none transition-colors placeholder:text-white/30 focus:border-emerald-400/45" />
        <datalist id="biostack-compounds">{knowledgeNames.map((name) => <option key={name} value={name} />)}</datalist>
      </label>

      {recentCompounds.length > 0 && (
        <div className="mt-4">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Recent</p>
          <div className="mt-2 flex flex-wrap gap-2">
            {recentCompounds.map((item) => (
              <button key={item} type="button" onClick={() => onSelect(item)} className="rounded-lg border border-white/10 px-3 py-1.5 text-sm text-white/62 hover:text-white">
                {item}
              </button>
            ))}
          </div>
        </div>
      )}
    </section>
  );
}

function ModeTabs({ mode, setMode }: { mode: SurfaceMode; setMode: (mode: SurfaceMode) => void }) {
  return (
    <div className="rounded-lg border border-white/10 bg-[#101820]/95 p-3">
      <div className="grid gap-2 sm:grid-cols-3">
        {(Object.keys(modeCopy) as SurfaceMode[]).map((item) => (
          <button key={item} type="button" onClick={() => setMode(item)} aria-pressed={mode === item} className={`rounded-lg px-3 py-3 text-left transition-colors ${mode === item ? 'bg-emerald-400 text-[#07110c]' : 'bg-white/[0.035] text-white/64 hover:bg-white/[0.07]'}`}>
            <span className="block text-sm font-bold">{modeCopy[item].label}</span>
            <span className="mt-1 block text-xs opacity-75">{modeCopy[item].title}</span>
          </button>
        ))}
      </div>
    </div>
  );
}

function ResultPanel({ mode, primary, secondary, hasError }: { mode: SurfaceMode; primary: string; secondary: string; hasError: boolean }) {
  return (
    <section className="rounded-lg border border-emerald-300/24 bg-emerald-400/[0.13] p-5 shadow-[0_24px_70px_rgba(16,185,129,0.13)]">
      <p className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-100/70">{modeCopy[mode].title} result</p>
      <p className="mt-3 text-3xl font-semibold leading-tight tracking-tight text-white sm:text-4xl">{primary}</p>
      <p className={`mt-3 text-sm leading-6 ${hasError ? 'text-amber-100/85' : 'text-emerald-50/75'}`}>{secondary}</p>
    </section>
  );
}

function MobileAccordion({ title, open, onToggle, children }: { title: string; open: boolean; onToggle: () => void; children: React.ReactNode }) {
  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      <button type="button" onClick={onToggle} className="flex w-full items-center justify-between gap-3 text-left sm:pointer-events-none">
        <h3 className="text-base font-semibold text-white">{title}</h3>
        <span className="text-sm text-white/45 sm:hidden">{open ? 'Hide' : 'Show'}</span>
      </button>
      <div className={`overflow-hidden transition-[max-height,opacity] duration-300 sm:mt-3 sm:max-h-none sm:opacity-100 ${open ? 'mt-3 max-h-[900px] opacity-100' : 'max-h-0 opacity-0'}`}>
        {children}
      </div>
    </section>
  );
}

function InstructionList({ items }: { items: string[] }) {
  return (
    <ol className="space-y-2">
      {items.map((item) => <li key={item} className="text-sm leading-6 text-white/62">{item}</li>)}
    </ol>
  );
}

function BlendSafetyPanel({
  additionalCompound,
  setAdditionalCompound,
  onCheck,
  state,
  result,
  knowledgeNames,
}: {
  additionalCompound: string;
  setAdditionalCompound: (value: string) => void;
  onCheck: () => void;
  state: string;
  result: { status: BlendStatus; reasons: string[] };
  knowledgeNames: string[];
}) {
  return (
    <div>
      <p className="text-sm leading-6 text-white/52">Add another compound and check overlap, compatibility, or caution flags.</p>
      <div className="mt-4 grid gap-3 sm:grid-cols-[1fr_auto]">
        <label className="block">
          <span className="mb-2 block text-sm text-white/62">Additional compound</span>
          <input list="biostack-blend-compounds" value={additionalCompound} onChange={(event) => setAdditionalCompound(event.target.value)} placeholder="Search or enter compound" className="min-h-12 w-full rounded-lg border border-white/10 bg-[#0F141B] px-4 text-white outline-none transition-colors placeholder:text-white/30 focus:border-emerald-400/45" />
          <datalist id="biostack-blend-compounds">{knowledgeNames.map((name) => <option key={name} value={name} />)}</datalist>
        </label>
        <button type="button" onClick={onCheck} className="self-end rounded-lg border border-emerald-300/25 bg-emerald-400/12 px-4 py-3 text-sm font-semibold text-emerald-100 transition-colors hover:border-emerald-200/50">
          {state === 'checking' ? 'Checking...' : 'Check'}
        </button>
      </div>
      <div className={`mt-4 rounded-lg border p-3 ${result.status === 'compatible' ? 'border-emerald-300/20 bg-emerald-500/10' : result.status === 'avoid' ? 'border-red-300/20 bg-red-500/10' : result.status === 'caution' ? 'border-amber-300/20 bg-amber-500/10' : 'border-white/10 bg-black/18'}`}>
        <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/48">{result.status}</p>
        <ul className="mt-2 space-y-1">
          {result.reasons.map((reason) => <li key={reason} className="text-sm leading-6 text-white/72">{reason}</li>)}
        </ul>
      </div>
    </div>
  );
}

function SavedCalculationsPanel({ items, onOpen, onDelete }: { items: SavedToolArtifact[]; onOpen: (artifact: SavedToolArtifact) => void; onDelete: (id: string) => void }) {
  return (
    <div className="mt-4 rounded-lg border border-white/[0.08] bg-black/18 p-3">
      <h3 className="text-sm font-semibold text-white">Saved calculations</h3>
      {items.length === 0 ? (
        <p className="mt-3 text-sm leading-6 text-white/45">No saved calculations yet.</p>
      ) : (
        <div className="mt-3 space-y-3">
          {items.map((item) => (
            <article key={item.id} className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-3">
              <p className="text-sm font-semibold text-white">{item.substances.join(' + ') || modeCopy[item.calculatorType].title}</p>
              <p className="mt-1 text-sm text-white/55">{summaryForArtifact(item)}</p>
              <p className="mt-1 text-xs text-white/35">{formatTimestamp(item.updatedAt)}</p>
              <div className="mt-3 flex flex-wrap gap-2">
                <button type="button" onClick={() => onOpen(item)} className="rounded-lg border border-emerald-300/25 px-3 py-1.5 text-sm font-semibold text-emerald-100">Open</button>
                <button type="button" onClick={() => onOpen(item)} className="rounded-lg border border-white/10 px-3 py-1.5 text-sm font-semibold text-white/70">Edit</button>
                <button type="button" onClick={() => onDelete(item.id)} className="rounded-lg border border-red-300/20 px-3 py-1.5 text-sm font-semibold text-red-100/80">Delete</button>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}

function InsightPanel({ title, items }: { title: string; items: string[] }) {
  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      <h3 className="text-base font-semibold text-white">{title}</h3>
      <ul className="mt-3 space-y-2">
        {items.map((item) => <li key={item} className="text-sm leading-6 text-white/62">{item}</li>)}
      </ul>
    </section>
  );
}

function Metric({ label, value, detail }: { label: string; value: string; detail: string }) {
  return (
    <div className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      <p className="text-xs uppercase tracking-[0.16em] text-white/35">{label}</p>
      <p className="mt-2 text-lg font-semibold text-white">{value}</p>
      <p className="mt-1 text-xs text-white/45">{detail}</p>
    </div>
  );
}

function NumberField({ label, value, onChange, suffix, step = '0.1' }: { label: string; value: number; onChange: (value: number) => void; suffix?: string; step?: string }) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm text-white/62">{label}</span>
      <div className="flex min-h-12 overflow-hidden rounded-lg border border-white/10 bg-[#0F141B] focus-within:border-emerald-400/45">
        <input type="number" min="0" step={step} value={Number.isNaN(value) ? '' : value} onChange={(event) => onChange(Number(event.target.value))} className="min-w-0 flex-1 bg-transparent px-4 text-white outline-none placeholder:text-white/30" />
        {suffix && <span className="border-l border-white/10 px-3 py-3 text-sm text-white/50">{suffix}</span>}
      </div>
    </label>
  );
}

function SelectField<TValue extends string>({ label, value, values, onChange }: { label: string; value: TValue; values: readonly TValue[]; onChange: (value: TValue) => void }) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm text-white/62">{label}</span>
      <select value={value} onChange={(event) => onChange(event.target.value as TValue)} className="min-h-12 w-full rounded-lg border border-white/10 bg-[#0F141B] px-4 text-white outline-none transition-colors focus:border-emerald-400/45">
        {values.map((item) => <option key={item} value={item}>{item}</option>)}
      </select>
    </label>
  );
}

function NumberWithUnitField<TUnit extends string>({ label, value, unit, units, onValueChange, onUnitChange }: { label: string; value: number; unit: TUnit; units: TUnit[]; onValueChange: (value: number) => void; onUnitChange: (unit: TUnit) => void }) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm text-white/62">{label}</span>
      <div className="flex min-h-12 overflow-hidden rounded-lg border border-white/10 bg-[#0F141B] focus-within:border-emerald-400/45">
        <input type="number" min="0" step="0.1" value={Number.isNaN(value) ? '' : value} onChange={(event) => onValueChange(Number(event.target.value))} className="min-w-0 flex-1 bg-transparent px-4 text-white outline-none placeholder:text-white/30" />
        <select value={unit} onChange={(event) => onUnitChange(event.target.value as TUnit)} className="border-l border-white/10 bg-[#111821] px-3 text-sm text-white outline-none">
          {units.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
      </div>
    </label>
  );
}

function summarizeBlend(state: string, findings: InteractionFlag[], compound: string, additionalCompound: string, knowledge: KnowledgeEntry[]): { status: BlendStatus; reasons: string[] } {
  if (state === 'error') {
    return { status: 'unknown', reasons: ['Compatibility data unavailable.', 'Verify against your source.'] };
  }
  if (state !== 'checked') {
    return { status: 'unknown', reasons: ['Run a check to see blend findings.'] };
  }
  if (findings.length === 0) {
    const pairNote = pairedNote(compound, additionalCompound, knowledge);
    return { status: 'compatible', reasons: pairNote ? ['No known conflicts.', pairNote] : ['No known conflicts.'] };
  }
  const avoid = findings.some((item) => /avoid|contra|conflict/i.test(`${item.overlapType} ${item.description}`));
  return {
    status: avoid ? 'avoid' : 'caution',
    reasons: findings.slice(0, 3).map((item) => item.pathwayTag ? `Overlap in ${item.pathwayTag}.` : item.description),
  };
}

function buildStackInsights(compound: string, stackCompounds: CompoundRecord[], flags: InteractionFlag[], state: string): string[] {
  const insights: string[] = [];
  const inStack = stackCompounds.find((item) => item.name.toLowerCase() === compound.trim().toLowerCase());
  if (inStack) insights.push('Already in your stack.');
  if (state === 'checking') insights.push('Checking against your stack.');
  flags.slice(0, 2).forEach((item) => {
    const other = item.compoundNames.find((name) => name.toLowerCase() !== compound.trim().toLowerCase());
    insights.push(other ? `Overlaps with ${other} (${item.pathwayTag}).` : `Potential redundancy (${item.pathwayTag}).`);
  });
  return Array.from(new Set(insights));
}

function pairedNote(compound: string, additionalCompound: string, knowledge: KnowledgeEntry[]): string {
  const entry = knowledge.find((item) => item.canonicalName.toLowerCase() === compound.trim().toLowerCase());
  if (!entry) return '';
  const pair = [...entry.pairsWellWith, ...entry.compatibleBlends].find((item) => item.toLowerCase().includes(additionalCompound.trim().toLowerCase()));
  return pair ? `Often paired for ${pair}.` : '';
}

function restoreDosingInput(inputs: Record<string, unknown>): UnifiedDosingInput {
  return {
    ...DEFAULT_UNIFIED_DOSING_INPUT,
    powderAmount: numberFromInput(inputs.powderAmount, DEFAULT_UNIFIED_DOSING_INPUT.powderAmount),
    powderUnit: massUnitFromInput(inputs.powderUnit, DEFAULT_UNIFIED_DOSING_INPUT.powderUnit),
    diluentVolumeMl: numberFromInput(inputs.diluentVolumeMl, DEFAULT_UNIFIED_DOSING_INPUT.diluentVolumeMl),
    concentrationSource: inputs.concentrationSource === 'known' ? 'known' : 'reconstitution',
    knownConcentration: numberFromInput(inputs.knownConcentration, DEFAULT_UNIFIED_DOSING_INPUT.knownConcentration),
    concentrationUnit: inputs.concentrationUnit === 'mg/mL' ? 'mg/mL' : 'mcg/mL',
    desiredDose: numberFromInput(inputs.desiredDose, DEFAULT_UNIFIED_DOSING_INPUT.desiredDose),
    desiredDoseUnit: massUnitFromInput(inputs.desiredDoseUnit, DEFAULT_UNIFIED_DOSING_INPUT.desiredDoseUnit),
    doseBasis: inputs.doseBasis === 'daily-total' || inputs.doseBasis === 'weekly-total' ? inputs.doseBasis : 'per-dose',
    splitCount: numberFromInput(inputs.splitCount, DEFAULT_UNIFIED_DOSING_INPUT.splitCount),
  };
}

function persistRecent(value: string, current: string[]): string[] {
  if (!value) return current;
  const next = [value, ...current.filter((item) => item.toLowerCase() !== value.toLowerCase())].slice(0, 5);
  window.localStorage.setItem(RECENT_COMPOUNDS_KEY, JSON.stringify(next));
  return next;
}

function readRecentCompounds(): string[] {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(RECENT_COMPOUNDS_KEY) ?? '[]') as unknown;
    return Array.isArray(parsed) ? parsed.filter((item): item is string => typeof item === 'string').slice(0, 5) : [];
  } catch {
    return [];
  }
}

function summaryForArtifact(item: SavedToolArtifact): string {
  const primary = item.outputs.primaryAnswer ?? item.outputs.result ?? 'Saved calculation';
  const secondary = item.outputs.secondaryAnswer ? ` · ${String(item.outputs.secondaryAnswer)}` : '';
  return `${String(primary)}${secondary}`;
}

function formatTimestamp(value: string): string {
  return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' }).format(new Date(value));
}

function numberFromInput(value: unknown, fallback: number): number {
  return typeof value === 'number' && Number.isFinite(value) ? value : fallback;
}

function stringFromInput(value: unknown, fallback: string): string {
  return typeof value === 'string' ? value : fallback;
}

function massUnitFromInput(value: unknown, fallback: MassUnit): MassUnit {
  return value === 'mcg' || value === 'mg' || value === 'g' ? value : fallback;
}

function demoCompound(name: string): CompoundRecord {
  return {
    id: `demo-${name}`,
    personId: 'demo-profile',
    name,
    category: 'Unknown',
    startDate: new Date().toISOString(),
    endDate: null,
    status: 'Active',
    notes: '',
    sourceType: 'Manual',
  };
}
