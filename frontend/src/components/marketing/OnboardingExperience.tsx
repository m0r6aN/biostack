'use client';

import { StackIntelligencePanel } from '@/components/marketing/StackIntelligencePanel';
import { apiClient } from '@/lib/api';
import type { KnowledgeEntry } from '@/lib/types';
import { cn } from '@/lib/utils';
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';
import Link from 'next/link';
import { useEffect, useMemo, useRef, useState } from 'react';

const QUICK_ADD_ITEMS = ['BPC-157', 'NAD+', 'Creatine', 'Vitamin D', 'TRT'] as const;
const GOAL_OPTIONS = [
  { id: 'energy-fat-loss', label: 'Fat loss' },
  { id: 'recovery-post-workout', label: 'Recovery' },
  { id: 'energy-levels', label: 'Energy' },
  { id: 'longevity-pathways', label: 'Longevity' },
  { id: 'performance-strength', label: 'Strength' },
  { id: 'cognitive-focus', label: 'Focus' },
] as const;
const STORAGE_KEY = 'biostack_onboarding_preview';
const STEP_ITEMS: Array<{ id: OnboardingStep; label: string; shortLabel: string }> = [
  { id: 'input', label: '1. Add protocol inputs', shortLabel: 'Add' },
  { id: 'aha', label: '2. See the overlap', shortLabel: 'See' },
  { id: 'goals', label: '3. Set your goals', shortLabel: 'Goals' },
];

type OnboardingStep = 'input' | 'aha' | 'goals';

function getStepIndex(step: OnboardingStep) {
  return STEP_ITEMS.findIndex((item) => item.id === step);
}

function readStoredPreview() {
  if (typeof window === 'undefined') {
    return { compounds: [] as string[], goals: [] as string[] };
  }

  try {
    const stored = JSON.parse(window.localStorage.getItem(STORAGE_KEY) || '{}') as {
      compounds?: string[];
      goals?: string[];
    };

    return {
      compounds: stored.compounds ?? [],
      goals: stored.goals ?? [],
    };
  } catch {
    window.localStorage.removeItem(STORAGE_KEY);
    return { compounds: [] as string[], goals: [] as string[] };
  }
}

function normalizeName(name: string) {
  return name.trim().replace(/\s+/g, ' ');
}

export function OnboardingExperience() {
  const [storedPreview] = useState(readStoredPreview);
  const inputRef = useRef<HTMLInputElement>(null);
  const reduceMotion = useReducedMotion();
  const [step, setStep] = useState<OnboardingStep>('input');
  const [query, setQuery] = useState('');
  const [knowledgeBase, setKnowledgeBase] = useState<KnowledgeEntry[]>([]);
  const [selectedCompounds, setSelectedCompounds] = useState<string[]>(() => storedPreview.compounds);
  const [selectedGoals, setSelectedGoals] = useState<string[]>(() => storedPreview.goals);

  useEffect(() => {
    apiClient.getAllKnowledgeCompounds().then(setKnowledgeBase).catch(() => setKnowledgeBase([]));
  }, []);

  useEffect(() => {
    if (typeof window === 'undefined') {
      return;
    }

    window.localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({ compounds: selectedCompounds, goals: selectedGoals })
    );
  }, [selectedCompounds, selectedGoals]);

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
  const previewPanelOverrides = useMemo(
    () => ({
      simple: {
        subtext:
          'BioStack turns a few protocol inputs into a fast overlap preview so you can spot what deserves a second look.',
        nodes: [
          {
            label: 'These may be doing the same job',
            x: 49,
            y: 44,
            bubbleClassName: '-translate-x-[18%] -translate-y-[145%]',
          },
          {
            label: 'You might not need both',
            x: 41,
            y: 60,
            bubbleClassName: '-translate-x-[8%] translate-y-4',
          },
        ],
        insights: [
          'A lot of people don’t notice this at first',
          'You could be doubling up without realizing it',
        ],
      },
    }),
    []
  );

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
          Protocol Setup
        </p>
        <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-5xl">
          Get to the first aha in a few seconds.
        </h1>
        <p className="mt-5 max-w-2xl text-lg leading-8 text-white/62">
          Add a few things, preview how they connect, and decide if BioStack earns a place in your workflow.
        </p>
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
        {step === 'input' && (
          <motion.form
            key="input"
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

                <h2 className="mt-3 text-3xl font-semibold tracking-tight text-white">Add what you already know.</h2>
                <p className="mt-4 max-w-xl text-base leading-7 text-white/60">
                  Search, choose a suggestion, or enter something manually. You only need a couple of items to see value.
                </p>

                <label className="mt-6 block">
                  <span className="mb-2 block text-sm text-white/62">Search your protocol</span>
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
                        <p className="text-sm text-white/46">No direct matches yet. You can still add this manually.</p>
                      )}
                      <p className="mt-2 text-sm text-white/42">Press Enter to add &ldquo;{query.trim()}&rdquo; manually.</p>
                    </motion.div>
                  )}
                </AnimatePresence>

                <div className="mt-6">
                  <p className="mb-3 text-sm text-white/55">Quick add</p>
                  <div className="flex flex-wrap gap-2">
                    {QUICK_ADD_ITEMS.map((item) => (
                      <button
                        key={item}
                        type="button"
                        onClick={() => addCompound(item)}
                        className="rounded-full border border-white/10 bg-white/[0.03] px-3 py-1.5 text-sm text-white/70 transition-colors hover:border-white/20 hover:text-white"
                      >
                        {item}
                      </button>
                    ))}
                  </div>
                </div>

                <div className="mt-6 min-h-24 rounded-2xl border border-white/8 bg-black/15 p-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-xs uppercase tracking-[0.2em] text-white/35">Your protocol</p>
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
                      <p className="text-sm text-white/38">Add 2–3 items to get the first relationship map.</p>
                    )}
                  </div>
                </div>

                <div className="mt-6 flex flex-wrap items-center gap-3">
                  <button
                    type="submit"
                    disabled={!canSubmit}
                    className="rounded-full bg-emerald-400 px-6 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5 disabled:translate-y-0 disabled:opacity-50"
                  >
                    Add to My Protocol
                  </button>
                  <p className="text-sm text-white/45">You&apos;ll see the first visualization right away.</p>
                </div>
              </div>

              <div className="space-y-4">
                <div className="rounded-[1.75rem] border border-white/10 bg-black/20 p-5">
                  <p className="text-xs uppercase tracking-[0.2em] text-emerald-300/70">What happens next</p>
                  <div className="mt-4 space-y-4 text-sm leading-7 text-white/62">
                    <p>1. Add a couple of things you&apos;re taking.</p>
                    <p>2. BioStack shows where the overlap or redundancy might be.</p>
                    <p>3. Decide whether it&apos;s worth going deeper — no long setup first.</p>
                  </div>
                </div>

                <div className="rounded-[1.75rem] border border-emerald-300/12 bg-emerald-500/[0.05] p-5">
                  <p className="text-xs uppercase tracking-[0.2em] text-emerald-200/70">Suggested starting point</p>
                  <p className="mt-3 text-base leading-7 text-white/62">
                    Start with the things you actually take most often. Even two items is enough to show where the hidden overlap might be.
                  </p>
                </div>
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
                eyebrowLabel="Preview your protocol"
                contentOverrides={previewPanelOverrides}
              />
            </div>

            <div className="rounded-[2rem] border border-white/10 bg-white/[0.03] p-6 shadow-[0_18px_60px_rgba(0,0,0,0.28)]">
              <div className="flex flex-wrap items-center gap-3">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-emerald-300/72">Your first aha</p>
                <span className="rounded-full border border-emerald-300/20 bg-emerald-500/10 px-3 py-1 text-xs text-emerald-100/80">
                  Live preview
                </span>
              </div>
              <h2 className="mt-4 text-3xl font-semibold tracking-tight text-white">This is the point.</h2>
              <p className="mt-4 text-base leading-7 text-white/60">
                You put in a few items and BioStack immediately starts surfacing what might overlap, what may be redundant, and what deserves a closer look.
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
                  <p className="text-xs uppercase tracking-[0.2em] text-white/35">Why this matters</p>
                  <p className="mt-3 text-sm leading-7 text-white/58">
                    This is where guesswork starts turning into structure. You can spot likely redundancy before it becomes a habit.
                  </p>
                </div>
              </div>

              <div className="mt-6 flex flex-wrap gap-3">
                <button
                  type="button"
                  onClick={() => goToStep('goals')}
                  className="rounded-full bg-emerald-400 px-6 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5"
                >
                  Continue
                </button>
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
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-emerald-300/72">Optional goal setup</p>
              <h2 className="mt-4 text-3xl font-semibold tracking-tight text-white">What are you trying to improve?</h2>
              <p className="mt-4 text-base leading-7 text-white/60">Pick anything that fits right now. You can change this later.</p>

              <div className="mt-6 flex flex-wrap gap-3">
                {GOAL_OPTIONS.map((goal) => {
                  const isSelected = selectedGoals.includes(goal.id);

                  return (
                    <button
                      key={goal.id}
                      type="button"
                      onClick={() => toggleGoal(goal.id)}
                      className={cn(
                        'rounded-full border px-4 py-2 text-sm font-medium transition-colors',
                        isSelected
                          ? 'border-emerald-300/35 bg-emerald-500/12 text-emerald-200'
                          : 'border-white/10 bg-white/[0.03] text-white/70 hover:text-white'
                      )}
                    >
                      {goal.label}
                    </button>
                  );
                })}
              </div>

              <div className="mt-8 flex flex-wrap gap-3">
                <Link
                  href="/profiles"
                  className="rounded-full bg-emerald-400 px-6 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5"
                >
                  Finish Setup
                </Link>
                <button
                  type="button"
                  onClick={() => goToStep('aha')}
                  className="rounded-full border border-white/12 px-5 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
                >
                  Back
                </button>
              </div>
            </div>

            <div className="space-y-4">
              <div className="rounded-[1.75rem] border border-white/10 bg-black/20 p-5">
                <p className="text-xs uppercase tracking-[0.2em] text-emerald-300/70">Your preview protocol</p>
                <div className="mt-4 flex flex-wrap gap-2">
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

              <div className="rounded-[1.75rem] border border-emerald-300/12 bg-emerald-500/[0.05] p-5">
                <p className="text-xs uppercase tracking-[0.2em] text-emerald-200/70">What setup unlocks</p>
                <p className="mt-3 text-sm leading-7 text-white/62">
                  Save your protocol, keep building, and connect what you take to the outcomes you actually care about.
                </p>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </section>
  );
}
