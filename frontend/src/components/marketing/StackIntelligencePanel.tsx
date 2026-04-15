'use client';

import {
  getOnboardingIntelligenceState,
  getOnboardingPanelContent,
  type OnboardingRelationshipCandidate,
} from '@/lib/onboardingIntelligence';
import { cn } from '@/lib/utils';
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';
import { useEffect, useMemo, useState } from 'react';

const LOOP_DURATION = 7.2;

export type PanelMode = 'simple' | 'technical';

const modeOptions = [
  { id: 'simple', label: 'Protocol' },
  { id: 'technical', label: 'Evidence' },
] as const;

export interface StackIntelligencePanelContent {
  subtext: string;
  insightLabel: string;
  summary: string;
  stats?: Array<[string, string]>;
  stageLabels?: string[];
  relationshipGroups: Array<{
    type: 'Context' | 'Overlap' | 'Synergy' | 'Support';
    label: string;
    detail: string;
  }>;
  insights: string[];
  nextAction?: string;
}

export const panelContent: Record<PanelMode, StackIntelligencePanelContent> = {
  simple: {
    subtext: 'No inputs detected. Add an item to establish context.',
    insightLabel: 'No inputs detected',
    summary: 'Add an item to establish context.',
    relationshipGroups: [],
    insights: ['No intelligence is shown until an input exists.'],
  },
  technical: {
    subtext: 'No inputs detected. Evidence context unavailable.',
    insightLabel: 'No inputs detected',
    summary: 'Add an item to establish evidence context.',
    relationshipGroups: [],
    insights: ['No evidence context is shown until an input exists.'],
  },
};

function buildCompounds(compoundNames?: string[]) {
  const orderedNames: string[] = [];
  const seen = new Set<string>();

  for (const name of compoundNames ?? []) {
    const trimmed = name.trim();
    if (!trimmed) {
      continue;
    }

    const key = trimmed.toLowerCase();
    if (seen.has(key)) {
      continue;
    }

    seen.add(key);
    orderedNames.push(trimmed);
  }

  return orderedNames.map((name, index) => ({
    id: `compound-${index}`,
    name,
  }));
}

interface StackIntelligencePanelProps {
  className?: string;
  compoundNames?: string[];
  initialMode?: PanelMode;
  showModeToggle?: boolean;
  eyebrowLabel?: string;
  contentOverrides?: Partial<Record<PanelMode, Partial<StackIntelligencePanelContent>>>;
  relationshipCandidates?: OnboardingRelationshipCandidate[];
  isCheckingRelationships?: boolean;
}

function mergePanelContent(
  base: StackIntelligencePanelContent,
  override?: Partial<StackIntelligencePanelContent>
): StackIntelligencePanelContent {
  if (!override) {
    return base;
  }

  return {
    ...base,
    ...override,
    relationshipGroups: override.relationshipGroups ?? base.relationshipGroups,
    insights: override.insights ?? base.insights,
  };
}

function relationshipTone(type: StackIntelligencePanelContent['relationshipGroups'][number]['type']) {
  if (type === 'Context') {
    return 'border-white/12 bg-white/[0.045] text-white/78';
  }

  if (type === 'Overlap') {
    return 'border-amber-300/20 bg-amber-300/[0.055] text-amber-100/85';
  }

  if (type === 'Synergy') {
    return 'border-emerald-300/20 bg-emerald-300/[0.06] text-emerald-100/85';
  }

  return 'border-sky-300/20 bg-sky-300/[0.055] text-sky-100/85';
}

export function StackIntelligencePanel({
  className,
  compoundNames,
  initialMode = 'simple',
  showModeToggle = true,
  eyebrowLabel = 'Protocol preview',
  contentOverrides,
  relationshipCandidates = [],
  isCheckingRelationships = false,
}: StackIntelligencePanelProps) {
  const [mode, setMode] = useState<PanelMode>(initialMode);
  const [insightIndex, setInsightIndex] = useState(0);
  const reduceMotion = useReducedMotion();
  const displayedCompounds = useMemo(() => buildCompounds(compoundNames), [compoundNames]);
  const intelligence = useMemo(
    () => getOnboardingIntelligenceState(compoundNames ?? [], relationshipCandidates),
    [compoundNames, relationshipCandidates]
  );
  const helperContent = useMemo(
    () =>
      getOnboardingPanelContent(intelligence, compoundNames ?? [], {
        relationshipCandidates,
        isCheckingRelationships,
      }),
    [compoundNames, intelligence, isCheckingRelationships, relationshipCandidates]
  );
  const mergedContent = useMemo(
    () => ({
      simple: mergePanelContent(helperContent, contentOverrides?.simple),
      technical: mergePanelContent(helperContent, contentOverrides?.technical),
    }),
    [contentOverrides, helperContent]
  );
  const activeContent = mergedContent[mode];
  const stageLabels = activeContent.stageLabels ?? ['Compounds added', 'Relationships mapped', 'Next step ready'];
  const stats = activeContent.stats ?? [
    ['Compounds', displayedCompounds.map((compound) => compound.name).join(', ') || 'None yet'],
    ['Context', displayedCompounds.length > 0 ? 'Established' : 'Unavailable'],
    ['Relationship map', intelligence.isRelationshipAllowed ? 'Eligible' : 'Locked'],
  ];

  useEffect(() => {
    if (reduceMotion) {
      return;
    }

    const interval = window.setInterval(() => {
      setInsightIndex((current) => (current + 1) % activeContent.insights.length);
    }, (LOOP_DURATION * 1000) / activeContent.insights.length);

    return () => window.clearInterval(interval);
  }, [activeContent.insights.length, mode, reduceMotion]);

  return (
    <div
      className={cn(
        'relative rounded-lg bg-[linear-gradient(135deg,rgba(16,185,129,0.4),rgba(255,255,255,0.08),rgba(59,130,246,0.38))] p-px shadow-[0_24px_90px_rgba(0,0,0,0.45)]',
        className
      )}
    >
      <div className="relative overflow-hidden rounded-lg bg-[#0B0F14] p-4 sm:p-5">
        <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_18%_18%,rgba(16,185,129,0.16),transparent_34%),radial-gradient(circle_at_82%_20%,rgba(59,130,246,0.16),transparent_32%),linear-gradient(180deg,rgba(255,255,255,0.04),transparent_42%)]" />
        <div className="pointer-events-none absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-white/35 to-transparent" />

        <div className="relative">
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0">
              <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-emerald-300/72">
                {eyebrowLabel}
              </p>
              <div className="mt-2 max-w-md">
                <AnimatePresence mode="wait" initial={false}>
                  <motion.p
                    key={mode}
                    className="text-sm leading-5 text-white/62"
                    initial={reduceMotion ? false : { opacity: 0, y: 8 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={reduceMotion ? undefined : { opacity: 0, y: -8 }}
                    transition={{ duration: reduceMotion ? 0 : 0.24, ease: 'easeOut' }}
                  >
                    {activeContent.subtext}
                  </motion.p>
                </AnimatePresence>
              </div>
            </div>

            {showModeToggle && (
              <div
                className="rounded-full border border-white/10 bg-white/[0.04] p-1 shadow-[0_0_28px_rgba(34,197,94,0.08)] backdrop-blur-xl"
                role="tablist"
                aria-label="Intelligence mode"
              >
                <div className="grid grid-cols-2 gap-1">
                  {modeOptions.map((option) => {
                    const isActive = mode === option.id;

                    return (
                      <button
                        key={option.id}
                        type="button"
                        role="tab"
                        aria-selected={isActive}
                        onClick={() => {
                          setMode(option.id);
                          setInsightIndex(0);
                        }}
                        className="relative min-w-[88px] rounded-full px-3 py-1.5 text-xs font-semibold"
                      >
                        {isActive && (
                          <motion.span
                            layoutId="intelligence-mode-pill"
                            className="absolute inset-0 rounded-full bg-gradient-to-r from-emerald-300 to-sky-300 shadow-[0_0_20px_rgba(52,211,153,0.28)]"
                            transition={{ type: 'spring', stiffness: 380, damping: 32 }}
                          />
                        )}
                        <span className={cn('relative z-10', isActive ? 'text-slate-950' : 'text-white/68')}>
                          {option.label}
                        </span>
                      </button>
                    );
                  })}
                </div>
              </div>
            )}
          </div>

          <div className="relative mt-3 flex flex-wrap gap-x-3 gap-y-1 rounded-lg border border-white/8 bg-black/20 px-3 py-2 text-[11px] font-medium text-white/42">
            {stageLabels.map((label, index) => (
              <span key={label} className="contents">
                <span>{label}</span>
                {index < stageLabels.length - 1 && <span className="text-white/18">/</span>}
              </span>
            ))}
          </div>

          <div className="relative mt-3 grid gap-3 rounded-lg border border-white/8 bg-white/[0.025] p-3 sm:grid-cols-[0.9fr_1.15fr_0.95fr]">
            {stats.map(([label, value]) => (
              <div key={label} className="rounded-lg border border-white/7 bg-black/20 px-3 py-2">
                <p className="text-[10px] font-semibold uppercase tracking-[0.16em] text-white/35">{label}</p>
                <p className="mt-1 text-sm font-semibold leading-5 text-white/82">{value}</p>
              </div>
            ))}
          </div>

          <div className="relative mt-3 grid gap-2.5">
            {activeContent.relationshipGroups.map((relationship) => (
              <motion.div
                key={`${mode}-${relationship.type}-${relationship.label}`}
                className="grid gap-3 rounded-lg border border-white/8 bg-black/20 p-3 sm:grid-cols-[116px_1fr]"
                initial={false}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: reduceMotion ? 0 : 0.2, ease: 'easeOut' }}
              >
                <div
                  className={cn(
                    'inline-flex w-fit items-center rounded-md border px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em]',
                    relationshipTone(relationship.type)
                  )}
                >
                  {relationship.type}
                </div>
                <div>
                  <p className="text-sm font-semibold text-white/88">{relationship.label}</p>
                  <p className="mt-1 text-sm leading-5 text-white/58">{relationship.detail}</p>
                </div>
              </motion.div>
            ))}
          </div>

          <div className="relative mt-3 rounded-lg border border-white/8 bg-white/[0.03] px-4 py-3.5">
            <div className="flex items-start gap-3">
              <div className="mt-1 h-2 w-2 shrink-0 rounded-full bg-emerald-300 shadow-[0_0_14px_rgba(52,211,153,0.8)]" />
              <div className="min-w-0 flex-1">
                <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-emerald-200/72">
                  {activeContent.insightLabel}
                </p>
                <p className="mt-1 text-sm font-medium leading-6 text-white/88">{activeContent.summary}</p>
              </div>
            </div>
          </div>

          <div className="relative mt-3 rounded-lg border border-white/8 bg-white/[0.025] px-4 py-3">
            <AnimatePresence mode="wait" initial={false}>
              <motion.p
                key={`${mode}-${insightIndex}`}
                className="text-sm leading-6 text-white/66"
                initial={reduceMotion ? false : { opacity: 0, y: 8 }}
                animate={{ opacity: 1, y: 0 }}
                exit={reduceMotion ? undefined : { opacity: 0, y: -8 }}
                transition={{ duration: reduceMotion ? 0 : 0.24, ease: 'easeOut' }}
              >
                {activeContent.insights[insightIndex]}
              </motion.p>
            </AnimatePresence>
          </div>

          <div className="relative mt-3 rounded-lg border border-emerald-300/14 bg-emerald-500/[0.055] px-4 py-3">
            <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-emerald-200/72">
              Suggested next action
            </p>
            <p className="mt-1 text-sm leading-6 text-white/76">
              {activeContent.nextAction ?? 'Add one item.'}
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
