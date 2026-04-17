'use client';

import { StackIntelligencePanel } from '@/components/marketing/StackIntelligencePanel';
import { apiClient } from '@/lib/api';
import {
  getOnboardingIntelligenceState,
  getOnboardingPanelContent,
  getOnboardingRewardContent,
  getRelationshipCandidatesFromOverlaps,
} from '@/lib/onboardingIntelligence';
import { getOnboardingSystemStatus } from '@/lib/systemStatus';
import {
  readOnboardingPreview,
  writeOnboardingPreview,
} from '@/lib/onboardingPreview';
import { starterStacks } from '@/lib/starterStacks';
import type { InteractionFlag, KnowledgeEntry } from '@/lib/types';
import { cn } from '@/lib/utils';
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';
import Link from 'next/link';
import { useEffect, useMemo, useRef, useState } from 'react';

const GOAL_OPTIONS = [
  { id: 'energy-fat-loss', label: 'Fat loss', signal: "We'll watch weight trends, appetite, and energy" },
  { id: 'recovery-post-workout', label: 'Recovery', signal: "We'll watch sleep quality, soreness, and joint comfort" },
  { id: 'energy-levels', label: 'Energy', signal: "We'll watch daytime energy, sleep, and mood" },
  { id: 'longevity-pathways', label: 'Longevity', signal: "We'll watch consistency, recovery, and baseline changes" },
  { id: 'performance-strength', label: 'Strength', signal: "We'll watch strength, endurance, and recovery" },
  { id: 'cognitive-focus', label: 'Focus', signal: "We'll watch focus, clarity, and sleep" },
] as const;
const STEP_ITEMS: Array<{ id: OnboardingStep; label: string; shortLabel: string }> = [
  { id: 'goals', label: '1. Choose what matters', shortLabel: 'Goals' },
  { id: 'input', label: '2. Build your list', shortLabel: 'List' },
  { id: 'aha', label: '3. See what is known', shortLabel: 'See' },
];

type OnboardingStep = 'input' | 'aha' | 'goals';

function getStepIndex(step: OnboardingStep) {
  return STEP_ITEMS.findIndex((item) => item.id === step);
}

function normalizeName(name: string) {
  return name.trim().replace(/\s+/g, ' ');
}

function isSameCompound(left: string, right: string) {
  return left.trim().toLowerCase() === right.trim().toLowerCase();
}

function findKnowledgeEntry(knowledgeBase: KnowledgeEntry[], compoundName: string) {
  const normalizedName = compoundName.trim().toLowerCase();

  return knowledgeBase.find((entry) => {
    if (entry.canonicalName.toLowerCase() === normalizedName) {
      return true;
    }

    return entry.aliases.some((alias) => alias.toLowerCase() === normalizedName);
  });
}

function parseBulkCompounds(value: string) {
  return value
    .split(/[\n,;]+/)
    .map(normalizeName)
    .filter(Boolean);
}

interface OnboardingExperienceProps {
  mode?: 'new' | 'existing';
}

interface RewardPanelProps {
  step: OnboardingStep;
  compounds: string[];
  selectedGoalIds: string[];
  overlaps: InteractionFlag[];
  isCheckingOverlaps: boolean;
}

function getSelectedGoalLabels(goalIds: string[]) {
  return GOAL_OPTIONS.filter((goal) => goalIds.includes(goal.id)).map((goal) => goal.label);
}

function RewardPanel({
  step,
  compounds,
  selectedGoalIds,
  overlaps,
  isCheckingOverlaps,
}: RewardPanelProps) {
  const selectedGoalLabels = getSelectedGoalLabels(selectedGoalIds);
  const isGoalsStep = step === 'goals';
  const relationshipCandidates = getRelationshipCandidatesFromOverlaps(overlaps);
  const intelligence = getOnboardingIntelligenceState(compounds, relationshipCandidates);
  const state = getOnboardingRewardContent(intelligence, compounds, selectedGoalLabels, {
    isGoalsStep,
    isCheckingRelationships: isCheckingOverlaps,
  });

  return (
    <div className="rounded-[1.75rem] border border-emerald-300/14 bg-[linear-gradient(180deg,rgba(16,185,129,0.095),rgba(255,255,255,0.03))] p-5 shadow-[0_18px_55px_rgba(0,0,0,0.28)]">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-emerald-200/75">{state.eyebrow}</p>
        <span className="rounded-full border border-emerald-300/18 bg-emerald-500/10 px-3 py-1 text-xs font-semibold text-emerald-100/80">
          {state.status}
        </span>
      </div>
      <h3 className="mt-4 text-2xl font-semibold tracking-tight text-white">{state.title}</h3>
      <p className="mt-3 text-sm leading-6 text-white/62">{state.body}</p>

      <div className="mt-5 grid gap-2">
        {state.rows.map(([label, value]) => (
          <div key={label} className="grid grid-cols-[82px_1fr] gap-3 rounded-lg border border-white/8 bg-black/20 px-3 py-2">
            <p className="text-[10px] font-semibold uppercase tracking-[0.16em] text-white/35">{label}</p>
            <p className="min-w-0 text-sm font-medium text-white/78">{value}</p>
          </div>
        ))}
      </div>

      {isGoalsStep && (
        <div className="mt-5 rounded-lg border border-white/8 bg-black/20 p-3">
          <p className="text-[10px] font-semibold uppercase tracking-[0.16em] text-white/35">Priority Signal</p>
          <p className="mt-2 text-sm leading-6 text-white/66">
            {selectedGoalLabels.length > 0
              ? selectedGoalLabels.join(', ')
              : 'Choose one or skip. Your list still saves.'}
          </p>
        </div>
      )}
    </div>
  );
}

export function OnboardingExperience({ mode = 'new' }: OnboardingExperienceProps) {
  const [storedPreview] = useState(readOnboardingPreview);
  const inputRef = useRef<HTMLInputElement>(null);
  const reduceMotion = useReducedMotion();
  const isExistingMode = mode === 'existing';
  const [step, setStep] = useState<OnboardingStep>('goals');
  const [query, setQuery] = useState('');
  const [bulkInput, setBulkInput] = useState('');
  const [knowledgeBase, setKnowledgeBase] = useState<KnowledgeEntry[]>([]);
  const [overlapResult, setOverlapResult] = useState<{ key: string; overlaps: InteractionFlag[] }>({
    key: '',
    overlaps: [],
  });
  const [selectedCompounds, setSelectedCompounds] = useState<string[]>(() => storedPreview.compounds);
  const [selectedGoals, setSelectedGoals] = useState<string[]>(() => storedPreview.goals);
  const relationshipCheckKey = selectedCompounds.map((compound) => compound.toLowerCase()).join('|');
  const overlaps = useMemo(
    () => (overlapResult.key === relationshipCheckKey ? overlapResult.overlaps : []),
    [overlapResult, relationshipCheckKey]
  );
  const isCheckingOverlaps = selectedCompounds.length > 1 && overlapResult.key !== relationshipCheckKey;

  useEffect(() => {
    apiClient.getAllKnowledgeCompounds().then(setKnowledgeBase).catch(() => setKnowledgeBase([]));
  }, []);

  useEffect(() => {
    if (typeof window === 'undefined') {
      return;
    }

    writeOnboardingPreview({ compounds: selectedCompounds, goals: selectedGoals });
  }, [selectedCompounds, selectedGoals]);

  useEffect(() => {
    if (selectedCompounds.length < 2) {
      return;
    }

    let isActive = true;
    const checkOverlap =
      typeof apiClient.checkOverlap === 'function'
        ? apiClient.checkOverlap.bind(apiClient)
        : async () => [] as InteractionFlag[];

    checkOverlap(selectedCompounds)
      .then((flags) => {
        if (isActive) {
          setOverlapResult({ key: relationshipCheckKey, overlaps: flags });
        }
      })
      .catch(() => {
        if (isActive) {
          setOverlapResult({ key: relationshipCheckKey, overlaps: [] });
        }
      });

    return () => {
      isActive = false;
    };
  }, [relationshipCheckKey, selectedCompounds]);

  const suggestions = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    if (!normalizedQuery) {
      return [];
    }

    return knowledgeBase
      .filter((entry) => {
        const matchesName = entry.canonicalName.toLowerCase().includes(normalizedQuery);
        const matchesAlias = entry.aliases.some((alias) => alias.toLowerCase().includes(normalizedQuery));
        const alreadySelected = selectedCompounds.some(
          (compound) => compound.toLowerCase() === entry.canonicalName.toLowerCase()
        );

        return (matchesName || matchesAlias) && !alreadySelected;
      })
      .slice(0, 6)
      .map((entry) => entry.canonicalName);
  }, [knowledgeBase, query, selectedCompounds]);

  const currentStepIndex = getStepIndex(step);
  const progressWidth = `${((currentStepIndex + 1) / STEP_ITEMS.length) * 100}%`;
  const canSubmit = selectedCompounds.length > 0 || Boolean(query.trim());
  const selectedKnowledgeEntries = useMemo(
    () =>
      selectedCompounds
        .map((compound) => findKnowledgeEntry(knowledgeBase, compound))
        .filter((entry): entry is KnowledgeEntry => Boolean(entry)),
    [knowledgeBase, selectedCompounds]
  );
  const firstKnowledgeEntry = selectedKnowledgeEntries[0];
  const relationshipCandidates = useMemo(() => getRelationshipCandidatesFromOverlaps(overlaps), [overlaps]);
  const intelligence = useMemo(
    () => getOnboardingIntelligenceState(selectedCompounds, relationshipCandidates),
    [relationshipCandidates, selectedCompounds]
  );
  const previewPanelContent = useMemo(
    () =>
      getOnboardingPanelContent(intelligence, selectedCompounds, {
        isCheckingRelationships: isCheckingOverlaps,
        relationshipCandidates,
        knowledgeContext: firstKnowledgeEntry
          ? {
              classification: firstKnowledgeEntry.classification,
              evidenceTier: firstKnowledgeEntry.evidenceTier,
              mechanismSummary: firstKnowledgeEntry.mechanismSummary,
              notes: firstKnowledgeEntry.notes,
              frequency: firstKnowledgeEntry.frequency,
            }
          : null,
      }),
    [firstKnowledgeEntry, intelligence, isCheckingOverlaps, relationshipCandidates, selectedCompounds]
  );
  const systemStatus = getOnboardingSystemStatus(intelligence);

  function addCompound(name: string) {
    const normalized = normalizeName(name);
    if (!normalized) {
      return;
    }

    setSelectedCompounds((current) => {
      if (current.some((compound) => compound.toLowerCase() === normalized.toLowerCase())) {
        return current;
      }

      return [...current, normalized];
    });
    setQuery('');
    inputRef.current?.focus();
  }

  function addBulkCompounds() {
    const compounds = parseBulkCompounds(bulkInput);
    if (compounds.length === 0) {
      return;
    }

    setSelectedCompounds((current) => {
      const next = [...current];

      for (const compound of compounds) {
        if (!next.some((selected) => isSameCompound(selected, compound))) {
          next.push(compound);
        }
      }

      return next;
    });
    setBulkInput('');
  }

  function selectStarterStack(compounds: readonly string[]) {
    setSelectedCompounds((current) => {
      const next = [...current];

      for (const compound of compounds) {
        if (!next.some((selected) => isSameCompound(selected, compound))) {
          next.push(compound);
        }
      }

      return next;
    });
    setQuery('');
    inputRef.current?.focus();
  }

  function removeCompound(name: string) {
    setSelectedCompounds((current) => current.filter((compound) => compound !== name));
  }

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const pending = normalizeName(query);
    const nextCompounds = pending
      ? selectedCompounds.some((compound) => compound.toLowerCase() === pending.toLowerCase())
        ? selectedCompounds
        : [...selectedCompounds, pending]
      : selectedCompounds;

    if (pending) {
      setSelectedCompounds(nextCompounds);
      setQuery('');
    }

    if (nextCompounds.length > 0) {
      setStep('aha');
    }
  }

  function toggleGoal(goalId: string) {
    setSelectedGoals((current) =>
      current.includes(goalId) ? current.filter((id) => id !== goalId) : [...current, goalId]
    );
  }

  function goToStep(nextStep: OnboardingStep) {
    setStep(nextStep);
  }

  return (
    <section className="mx-auto max-w-7xl px-5 py-12 sm:px-8 lg:py-16">
      <div className="max-w-3xl">
        <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
          {isExistingMode ? 'Stack Mapping' : 'Getting Started'}
        </p>
        <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-5xl">
          {isExistingMode ? 'Drop in the stack. We will sort what fits.' : 'Tell us what you take — or start from an example.'}
        </h1>
        <p className="mt-5 max-w-2xl text-lg leading-8 text-white/62">
          {isExistingMode
            ? 'Paste the current list. BioStack checks only what you entered.'
            : "Add anything you already take. If you're not sure, start from a sample."}
        </p>
        {!isExistingMode && (
          <p className="mt-4 max-w-2xl text-base leading-7 text-white/55">
            A protocol is just the list of things you take. We&apos;ll show overlaps and conflicts as you add items.
          </p>
        )}
      </div>

      <div className="mt-8">
        <div className="overflow-hidden rounded-2xl border border-white/8 bg-white/[0.02] p-3 sm:p-4">
          <div className="relative h-2 overflow-hidden rounded-full bg-white/[0.05]">
            <motion.div
              className="absolute inset-y-0 left-0 rounded-full bg-gradient-to-r from-emerald-300 via-emerald-400 to-sky-400"
              animate={{ width: progressWidth }}
              transition={reduceMotion ? { duration: 0 } : { type: 'spring', stiffness: 120, damping: 22 }}
            />
          </div>

          <div className="mt-4 grid grid-cols-3 gap-2">
            {STEP_ITEMS.map((item, index) => {
              const isActive = step === item.id;
              const isComplete = currentStepIndex > index;

              return (
                <div
                  key={item.id}
                  className={cn(
                    'rounded-2xl border px-3 py-3 text-left transition-colors',
                    isActive || isComplete
                      ? 'border-emerald-300/18 bg-emerald-500/[0.08]'
                      : 'border-white/8 bg-white/[0.03]'
                  )}
                >
                  <div className="flex items-center gap-2">
                    <span
                      className={cn(
                        'flex h-6 w-6 items-center justify-center rounded-full border text-[11px] font-semibold',
                        isActive || isComplete
                          ? 'border-emerald-300/40 bg-emerald-300/18 text-emerald-100'
                          : 'border-white/10 text-white/42'
                      )}
                    >
                      {index + 1}
                    </span>
                    <span className={cn('text-xs font-medium sm:hidden', isActive ? 'text-white' : 'text-white/55')}>
                      {item.shortLabel}
                    </span>
                    <span className={cn('hidden text-xs font-medium sm:inline', isActive ? 'text-white' : 'text-white/55')}>
                      {item.label}
                    </span>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      <AnimatePresence mode="wait" initial={false}>
        {step === 'goals' && (
          <motion.div
            key="goals"
            className="mt-8 grid gap-6 lg:grid-cols-[1fr_0.84fr]"
            initial={reduceMotion ? false : { opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            exit={reduceMotion ? undefined : { opacity: 0, y: -12 }}
            transition={{ duration: reduceMotion ? 0 : 0.26, ease: 'easeOut' }}
          >
            <div className="rounded-[2rem] border border-white/10 bg-white/[0.03] p-6 shadow-[0_20px_70px_rgba(0,0,0,0.35)] sm:p-8">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-emerald-300/72">What Matters Most</p>
              <h2 className="mt-4 text-3xl font-semibold tracking-tight text-white">What do you want help with first?</h2>
              <p className="mt-4 text-base leading-7 text-white/60">Pick one or more goals. You can change this later.</p>

              <div className="mt-6 flex flex-wrap gap-3">
                {GOAL_OPTIONS.map((goal) => {
                  const isSelected = selectedGoals.includes(goal.id);

                  return (
                    <button
                      key={goal.id}
                      type="button"
                      aria-label={goal.label}
                      onClick={() => toggleGoal(goal.id)}
                      className={cn(
                        'rounded-lg border px-4 py-3 text-left transition-colors',
                        isSelected
                          ? 'border-emerald-300/35 bg-emerald-500/12 text-emerald-200'
                          : 'border-white/10 bg-white/[0.03] text-white/70 hover:text-white'
                      )}
                    >
                      <span className="block text-sm font-semibold">{goal.label}</span>
                      <span className="mt-1 block text-xs leading-5 text-white/45">{goal.signal}</span>
                    </button>
                  );
                })}
              </div>

              <div className="mt-8 flex flex-wrap gap-3">
                <button
                  type="button"
                  onClick={() => goToStep('input')}
                  className="rounded-full bg-emerald-400 px-6 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5"
                >
                  Continue
                </button>
                <button
                  type="button"
                  onClick={() => goToStep('input')}
                  className="rounded-full border border-white/12 px-5 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
                >
                  Skip for now
                </button>
              </div>
            </div>

            <div className="space-y-4">
              <RewardPanel
                step={step}
                compounds={selectedCompounds}
                selectedGoalIds={selectedGoals}
                overlaps={overlaps}
                isCheckingOverlaps={isCheckingOverlaps}
              />
            </div>
          </motion.div>
        )}

        {step === 'input' && (
          <motion.form
            key="input"
            id="starter-stacks"
            onSubmit={handleSubmit}
            className="mt-8 rounded-[2rem] border border-white/10 bg-white/[0.03] p-6 shadow-[0_20px_70px_rgba(0,0,0,0.35)] backdrop-blur-xl sm:p-8"
            initial={reduceMotion ? false : { opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            exit={reduceMotion ? undefined : { opacity: 0, y: -12 }}
            transition={{ duration: reduceMotion ? 0 : 0.26, ease: 'easeOut' }}
          >
            <div className="grid gap-8 lg:grid-cols-[1.08fr_0.92fr]">
              <div>
                <div className="flex flex-wrap items-center gap-3">
                  <p className="text-sm font-medium text-emerald-200/85">Build a quick preview</p>
                  <span className="rounded-full border border-white/10 bg-black/20 px-3 py-1 text-xs text-white/52">
                    {selectedCompounds.length} item{selectedCompounds.length === 1 ? '' : 's'} added
                  </span>
                </div>

                <h2 className="mt-3 text-3xl font-semibold tracking-tight text-white">
                  {isExistingMode ? 'Paste what is already in the mix.' : 'Add what you already know.'}
                </h2>
                <p className="mt-4 max-w-xl text-base leading-7 text-white/60">
                  {isExistingMode
                    ? 'Use commas or new lines. Clean it up later.'
                    : 'Search, quick add, type manually, or pick an example below.'}
                </p>

                {isExistingMode && (
                  <div className="mt-6 rounded-2xl border border-emerald-300/12 bg-emerald-500/[0.045] p-4">
                    <label className="block">
                      <span className="mb-2 block text-sm text-emerald-100/75">Bulk add current stack</span>
                      <textarea
                        value={bulkInput}
                        onChange={(event) => setBulkInput(event.target.value)}
                        placeholder="Enter one item per line or separate items with commas."
                        rows={4}
                        className="w-full resize-none rounded-2xl border border-white/10 bg-black/20 px-4 py-3 text-white outline-none transition-colors placeholder:text-white/30 focus:border-emerald-400/40"
                      />
                    </label>
                    <div className="mt-3 flex flex-wrap items-center gap-3">
                      <button
                        type="button"
                        onClick={addBulkCompounds}
                        disabled={!bulkInput.trim()}
                        className="rounded-full border border-emerald-300/20 bg-emerald-400/12 px-4 py-2 text-sm font-semibold text-emerald-100 transition-colors hover:border-emerald-300/35 disabled:cursor-not-allowed disabled:opacity-45"
                      >
                        Add Stack
                      </button>
                      <p className="text-sm text-white/45">Fast entry first. Honest read next.</p>
                    </div>
                  </div>
                )}

                {!isExistingMode && (
                  <div className="mt-6">
                    <p className="text-sm font-semibold text-white/78">Not sure yet? Start from an example</p>
                    <div className="-mx-6 mt-3 flex gap-3 overflow-x-auto px-6 pb-2 sm:mx-0 sm:grid sm:grid-cols-2 sm:px-0 lg:grid-cols-4">
                      {starterStacks.map((starterStack) => (
                        <button
                          key={starterStack.id}
                          type="button"
                          onClick={() => selectStarterStack(starterStack.compounds)}
                          className="min-w-[220px] rounded-lg border border-white/10 bg-white/[0.035] p-4 text-left transition-colors hover:border-emerald-300/30 hover:bg-emerald-500/[0.07] sm:min-w-0"
                        >
                          <span className="block text-sm font-semibold text-white">{starterStack.name}</span>
                          <span className="mt-2 block text-xs leading-5 text-white/52">{starterStack.description}</span>
                          <span className="mt-3 block text-xs leading-5 text-emerald-100/72">
                            {starterStack.compounds.join(', ')}
                          </span>
                        </button>
                      ))}
                    </div>
                  </div>
                )}

                <label className="mt-6 block">
                  <span className="mb-2 block text-sm text-white/62">Search what you take</span>
                  <div className="flex flex-col gap-3 sm:flex-row">
                    <input
                      ref={inputRef}
                      type="text"
                      value={query}
                      onChange={(event) => setQuery(event.target.value)}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter') {
                          event.preventDefault();
                          addCompound(query);
                        }
                      }}
                      placeholder="Type a compound, supplement, or medication…"
                      className="w-full rounded-2xl border border-white/10 bg-black/20 px-4 py-3 text-white outline-none transition-colors placeholder:text-white/30 focus:border-emerald-400/40"
                    />
                    <button
                      type="button"
                      onClick={() => addCompound(query)}
                      disabled={!query.trim()}
                      className="rounded-2xl border border-white/10 bg-white/[0.04] px-4 py-3 text-sm font-semibold text-white transition-colors hover:border-white/20 disabled:cursor-not-allowed disabled:opacity-45"
                    >
                      Add
                    </button>
                  </div>
                </label>

                <AnimatePresence>
                  {query.trim() && (
                    <motion.div
                      className="mt-3 overflow-hidden rounded-2xl border border-white/10 bg-black/20 p-3"
                      initial={reduceMotion ? false : { opacity: 0, y: 8 }}
                      animate={{ opacity: 1, y: 0 }}
                      exit={reduceMotion ? undefined : { opacity: 0, y: -8 }}
                      transition={{ duration: reduceMotion ? 0 : 0.18, ease: 'easeOut' }}
                    >
                      {suggestions.length > 0 ? (
                        <div className="flex flex-wrap gap-2">
                          {suggestions.map((suggestion) => (
                            <button
                              key={suggestion}
                              type="button"
                              onClick={() => addCompound(suggestion)}
                              className="rounded-full border border-white/10 bg-white/[0.04] px-3 py-1.5 text-sm text-white/75 transition-colors hover:border-emerald-300/25 hover:text-white"
                            >
                              {suggestion}
                            </button>
                          ))}
                        </div>
                      ) : (
                        <p className="text-sm text-white/46">We don&apos;t have deep data on this one yet, but we&apos;ll still track it for you.</p>
                      )}
                      <p className="mt-2 text-sm text-white/42">Press Enter to add &ldquo;{query.trim()}&rdquo; manually.</p>
                    </motion.div>
                  )}
                </AnimatePresence>

                <div className="mt-6 min-h-24 rounded-2xl border border-white/8 bg-black/15 p-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-xs uppercase tracking-[0.2em] text-white/35">Your list</p>
                    <p className="text-xs text-white/38">Remove anything with one tap</p>
                  </div>
                  <div className="mt-3 flex flex-wrap gap-2">
                    {selectedCompounds.length > 0 ? (
                      selectedCompounds.map((compound) => (
                        <button
                          key={compound}
                          type="button"
                          onClick={() => removeCompound(compound)}
                          aria-label={`Remove ${compound}`}
                          className="rounded-full border border-emerald-300/18 bg-emerald-500/10 px-3 py-1.5 text-sm text-emerald-100/90 transition-colors hover:border-emerald-300/32"
                        >
                          {compound} <span className="text-emerald-200/60">×</span>
                        </button>
                      ))
                    ) : (
                      <p className="text-sm text-white/38">
                        {isExistingMode
                          ? 'Paste or add the compounds, supplements, or medications you already have in mind.'
                          : "Type anything you take — a supplement, medication, or peptide. Don't worry about getting it perfect."}
                      </p>
                    )}
                  </div>
                </div>

                <div className="mt-6 flex flex-wrap items-center gap-3">
                  <button
                    type="submit"
                    disabled={!canSubmit}
                    className="rounded-full bg-emerald-400 px-6 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5 disabled:translate-y-0 disabled:opacity-50"
                  >
                    Add to My List
                  </button>
                  <p className="text-sm text-white/45">We&apos;ll flag overlaps and conflicts once you&apos;ve added two items.</p>
                </div>
              </div>

              <div className="space-y-4">
                <RewardPanel
                  step={step}
                  compounds={selectedCompounds}
                  selectedGoalIds={selectedGoals}
                  overlaps={overlaps}
                  isCheckingOverlaps={isCheckingOverlaps}
                />
              </div>
            </div>
          </motion.form>
        )}

        {step === 'aha' && (
          <motion.div
            key="aha"
            className="mt-8 grid gap-6 lg:grid-cols-[1.08fr_0.92fr]"
            initial={reduceMotion ? false : { opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            exit={reduceMotion ? undefined : { opacity: 0, y: -12 }}
            transition={{ duration: reduceMotion ? 0 : 0.26, ease: 'easeOut' }}
          >
            <div className="relative">
              <div className="absolute inset-x-8 top-0 h-24 rounded-full bg-emerald-500/10 blur-3xl" />
              <StackIntelligencePanel
                compoundNames={selectedCompounds}
                eyebrowLabel="Preview your list"
                contentOverrides={{
                  simple: previewPanelContent,
                  technical: previewPanelContent,
                }}
                relationshipCandidates={relationshipCandidates}
                isCheckingRelationships={isCheckingOverlaps}
              />
            </div>

            <div className="rounded-[2rem] border border-white/10 bg-white/[0.03] p-6 shadow-[0_18px_60px_rgba(0,0,0,0.28)]">
              <div className="flex flex-wrap items-center gap-3">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-emerald-300/72">
                  {systemStatus.eyebrow ?? 'System State'}
                </p>
                <span className="rounded-full border border-emerald-300/20 bg-emerald-500/10 px-3 py-1 text-xs text-emerald-100/80">
                  {systemStatus.title}
                </span>
              </div>
              <h2 className="mt-4 text-3xl font-semibold tracking-tight text-white">{systemStatus.title}</h2>
              <p className="mt-4 text-base leading-7 text-white/60">
                {intelligence.stage === 'context'
                  ? "We'll flag overlaps and conflicts once you've added two items."
                  : systemStatus.subtitle ?? 'Selected inputs staged.'}
              </p>

              <div className="mt-6 grid gap-4 sm:grid-cols-2">
                <div className="rounded-2xl border border-white/8 bg-black/20 p-4">
                  <p className="text-xs uppercase tracking-[0.2em] text-white/35">Selected items</p>
                  <div className="mt-3 flex flex-wrap gap-2">
                    {selectedCompounds.map((compound) => (
                      <span
                        key={compound}
                        className="rounded-full border border-emerald-300/18 bg-emerald-500/10 px-3 py-1.5 text-sm text-emerald-100/90"
                      >
                        {compound}
                      </span>
                    ))}
                  </div>
                </div>

                <div className="rounded-2xl border border-white/8 bg-black/20 p-4">
                  <p className="text-xs uppercase tracking-[0.2em] text-white/35">Current state</p>
                  <p className="mt-3 text-sm leading-7 text-white/58">
                    {intelligence.stage === 'context'
                      ? "Add one more item when you're ready for overlap and conflict checks."
                      : systemStatus.subtitle ?? systemStatus.title}
                  </p>
                </div>
              </div>

              <div className="mt-5">
                <RewardPanel
                  step={step}
                  compounds={selectedCompounds}
                  selectedGoalIds={selectedGoals}
                  overlaps={overlaps}
                  isCheckingOverlaps={isCheckingOverlaps}
                />
              </div>

              <div className="mt-6 flex flex-wrap gap-3">
                <Link
                  href="/profiles"
                  className="rounded-full bg-emerald-400 px-6 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5"
                >
                  Finish Setup
                </Link>
                <button
                  type="button"
                  onClick={() => goToStep('input')}
                  className="rounded-full border border-white/12 px-5 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
                >
                  Back
                </button>
              </div>
            </div>
          </motion.div>
        )}

      </AnimatePresence>
    </section>
  );
}
