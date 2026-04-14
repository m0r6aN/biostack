'use client';

import { cn } from '@/lib/utils';
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';
import { useEffect, useMemo, useState } from 'react';

const LOOP_DURATION = 7.2;

export type PanelMode = 'simple' | 'technical';

const modeOptions = [
  { id: 'simple', label: 'Protocol' },
  { id: 'technical', label: 'Evidence' },
] as const;

const DEFAULT_COMPOUND_NAMES = ['BPC-157', 'TB-500', 'Creatine'] as const;

const compoundSlots = [
  { x: 18, y: 20 },
  { x: 76, y: 22 },
  { x: 42, y: 78 },
] as const;

const connections = [
  [0, 1],
  [0, 2],
  [1, 2],
] as const;

export interface StackIntelligencePanelContent {
  subtext: string;
  insightLabel: string;
  nodes: Array<{ label: string; x: number; y: number; bubbleClassName: string }>;
  insights: string[];
}

export const panelContent: Record<
  PanelMode,
  StackIntelligencePanelContent
> = {
  simple: {
    subtext:
      'Live protocol state with compounds added, guidance structured, overlap surfaced, and the next tracking step ready.',
    insightLabel: 'Detected overlap',
    nodes: [
      {
        label: 'Shared tissue-repair pathway',
        x: 49,
        y: 44,
        bubbleClassName: '-translate-x-[22%] -translate-y-[145%]',
      },
      {
        label: 'Correlation ready',
        x: 41,
        y: 60,
        bubbleClassName: '-translate-x-[8%] translate-y-4',
      },
    ],
    insights: [
      'BPC-157 + TB-500 flagged for overlapping tissue-repair pathways.',
      'Typical range and common frequency are separated from evidence strength.',
      'Timeline snippet ready: recovery + sleep signal, 7-day review.',
    ],
  },
  technical: {
    subtext:
      'BioStack ties protocol inputs to typical ranges, evidence confidence, pathway structure, and observable signal over time.',
    insightLabel: 'Detected signal',
    nodes: [
      {
        label: 'Moderate evidence',
        x: 49,
        y: 44,
        bubbleClassName: '-translate-x-[12%] -translate-y-[140%]',
      },
      {
        label: 'Signal baseline',
        x: 41,
        y: 60,
        bubbleClassName: '-translate-x-[6%] translate-y-4',
      },
    ],
    insights: [
      '2 overlapping pathways detected',
      'Evidence tier: Limited -> Moderate',
      'Recovery, sleep, and joint signal ready for timeline correlation',
    ],
  },
};

function buildCompounds(compoundNames: string[] = []) {
  const orderedNames: string[] = [];
  const seen = new Set<string>();

  for (const name of compoundNames) {
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

    if (orderedNames.length === compoundSlots.length) {
      break;
    }
  }

  for (const fallback of DEFAULT_COMPOUND_NAMES) {
    const key = fallback.toLowerCase();
    if (seen.has(key)) {
      continue;
    }

    orderedNames.push(fallback);
    if (orderedNames.length === compoundSlots.length) {
      break;
    }
  }

  return compoundSlots.map((slot, index) => ({
    id: `compound-${index}`,
    name: orderedNames[index],
    x: slot.x,
    y: slot.y,
  }));
}

interface StackIntelligencePanelProps {
  className?: string;
  compoundNames?: string[];
  initialMode?: PanelMode;
  showModeToggle?: boolean;
  eyebrowLabel?: string;
  contentOverrides?: Partial<Record<PanelMode, Partial<StackIntelligencePanelContent>>>;
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
    nodes: override.nodes ?? base.nodes,
    insights: override.insights ?? base.insights,
  };
}

export function StackIntelligencePanel({
  className,
  compoundNames,
  initialMode = 'simple',
  showModeToggle = true,
  eyebrowLabel = 'Protocol preview',
  contentOverrides,
}: StackIntelligencePanelProps) {
  const [mode, setMode] = useState<PanelMode>(initialMode);
  const [insightIndex, setInsightIndex] = useState(0);
  const reduceMotion = useReducedMotion();
  const displayedCompounds = useMemo(() => buildCompounds(compoundNames), [compoundNames]);
  const mergedContent = useMemo(
    () => ({
      simple: mergePanelContent(panelContent.simple, contentOverrides?.simple),
      technical: mergePanelContent(panelContent.technical, contentOverrides?.technical),
    }),
    [contentOverrides]
  );
  const activeContent = mergedContent[mode];

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
        'relative rounded-2xl bg-[linear-gradient(135deg,rgba(16,185,129,0.4),rgba(255,255,255,0.08),rgba(59,130,246,0.38))] p-px shadow-[0_24px_90px_rgba(0,0,0,0.45)]',
        className
      )}
    >
      <div className="relative overflow-hidden rounded-[15px] bg-[#0B0F14] p-5 sm:p-5">
        <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_18%_18%,rgba(16,185,129,0.16),transparent_34%),radial-gradient(circle_at_82%_20%,rgba(59,130,246,0.16),transparent_32%),linear-gradient(180deg,rgba(255,255,255,0.04),transparent_42%)]" />
        <div className="pointer-events-none absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-white/35 to-transparent" />

        <div className="relative">
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0">
              <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-emerald-300/72">
                {eyebrowLabel}
              </p>
              <div className="mt-2 min-h-[68px] max-w-md">
                <AnimatePresence mode="wait" initial={false}>
                  <motion.p
                    key={mode}
                    className="text-sm leading-5 text-white/58"
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
            <span>Tracking started</span>
            <span className="text-white/18">/</span>
            <span>Baseline captured</span>
            <span className="text-white/18">/</span>
            <span>Day 7 review pending</span>
          </div>

          <div className="relative mt-3 grid gap-3 rounded-lg border border-white/8 bg-white/[0.025] p-3 sm:grid-cols-[0.9fr_1.15fr_0.95fr]">
            {[
              ['Compounds', '3 active'],
              ['Evidence tier', mode === 'technical' ? 'Limited -> Moderate' : 'Review ready'],
              ['Timeline', 'Day 0 baseline'],
            ].map(([label, value]) => (
              <div key={label} className="rounded-lg border border-white/7 bg-black/20 px-3 py-2">
                <p className="text-[10px] font-semibold uppercase tracking-[0.16em] text-white/35">{label}</p>
                <p className="mt-1 text-sm font-semibold text-white/82">{value}</p>
              </div>
            ))}
          </div>

          <div className="relative mt-3 grid gap-3 md:grid-cols-[0.92fr_1.08fr]">
            <div className="rounded-lg border border-white/8 bg-black/20 p-3">
              <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-emerald-200/70">
                Guidance layer
              </p>
              <div className="mt-3 space-y-2 text-sm leading-5 text-white/72">
                <p><span className="text-white/38">Typical range:</span> 0.25mg - 4mg weekly</p>
                <p><span className="text-white/38">Common pattern:</span> 1-3 doses/week</p>
                <p><span className="text-white/38">Evidence tier:</span> Limited -&gt; Moderate</p>
              </div>
            </div>

            <div className="rounded-lg border border-emerald-300/14 bg-emerald-500/[0.045] p-3">
              <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-emerald-200/70">
                Context layer
              </p>
              <p className="mt-3 text-sm leading-6 text-white/66">
                Adjusted (your profile): more precise with age, weight, goals, and tracking.
              </p>
            </div>
          </div>

          <div className="relative mt-3 h-[142px] sm:h-[156px]">
            <svg viewBox="0 0 100 100" className="absolute inset-0 h-full w-full" aria-hidden="true">
              <defs>
                <linearGradient id="stack-line-gradient" x1="0" y1="0" x2="100" y2="100" gradientUnits="userSpaceOnUse">
                  <stop offset="0%" stopColor="rgba(52,211,153,0.9)" />
                  <stop offset="100%" stopColor="rgba(96,165,250,0.85)" />
                </linearGradient>
              </defs>

              {connections.map(([fromIndex, toIndex], index) => {
                const from = displayedCompounds[fromIndex];
                const to = displayedCompounds[toIndex];

                return (
                  <motion.line
                    key={`${from.id}-${to.id}`}
                    x1={from.x}
                    y1={from.y}
                    x2={to.x}
                    y2={to.y}
                    stroke="url(#stack-line-gradient)"
                    strokeWidth="1.25"
                    strokeLinecap="round"
                    initial={reduceMotion ? false : { pathLength: 0, opacity: 0 }}
                    animate={reduceMotion ? { pathLength: 1, opacity: 0.45 } : { pathLength: [0, 1, 1, 0], opacity: [0, 0.55, 0.48, 0] }}
                    transition={reduceMotion ? { duration: 0 } : { duration: LOOP_DURATION, delay: 0.55 + index * 0.12, times: [0, 0.26, 0.78, 1], repeat: Infinity, ease: 'easeInOut' }}
                  />
                );
              })}
            </svg>

            {displayedCompounds.map((compound, index) => (
              <motion.div
                key={compound.id}
                className="absolute -translate-x-1/2 -translate-y-1/2"
                style={{ left: `${compound.x}%`, top: `${compound.y}%` }}
                initial={reduceMotion ? false : { opacity: 0, scale: 0.92, y: 10 }}
                animate={reduceMotion ? { opacity: 1, scale: 1, y: 0 } : { opacity: [0, 1, 1, 0], scale: [0.92, 1, 1, 0.98], y: [10, 0, 0, -4] }}
                transition={reduceMotion ? { duration: 0 } : { duration: LOOP_DURATION, delay: index * 0.1, times: [0, 0.16, 0.82, 1], repeat: Infinity, ease: 'easeOut' }}
              >
                <div className="rounded-full border border-white/12 bg-white/[0.05] px-3 py-2 text-xs font-medium tracking-[0.08em] text-white shadow-[0_0_18px_rgba(34,197,94,0.14)] backdrop-blur-xl sm:px-4">
                  {compound.name}
                </div>
              </motion.div>
            ))}

            {activeContent.nodes.map((node, index) => (
              <motion.div
                key={`${mode}-${node.label}`}
                className="absolute -translate-x-1/2 -translate-y-1/2"
                style={{ left: `${node.x}%`, top: `${node.y}%` }}
                initial={reduceMotion ? false : { opacity: 0, scale: 0.7 }}
                animate={
                  reduceMotion
                    ? { opacity: 1, scale: 1 }
                    : { opacity: [0, 0, 1, 1, 0], scale: [0.7, 0.7, 1, 1.08, 0.94] }
                }
                transition={
                  reduceMotion
                    ? { duration: 0 }
                    : {
                        duration: LOOP_DURATION,
                        delay: 1.55 + index * 0.28,
                        times: [0, 0.28, 0.44, 0.76, 1],
                        repeat: Infinity,
                        ease: 'easeInOut',
                      }
                }
              >
                <motion.div
                  className="absolute left-1/2 top-1/2 h-12 w-12 -translate-x-1/2 -translate-y-1/2 rounded-full bg-emerald-400/18 blur-xl"
                  animate={reduceMotion ? { opacity: 0.55, scale: 1 } : { opacity: [0.22, 0.55, 0.22], scale: [0.85, 1.2, 0.85] }}
                  transition={reduceMotion ? { duration: 0 } : { duration: 1.8, repeat: Infinity, ease: 'easeInOut' }}
                />
                <div className="relative h-3 w-3 rounded-full border border-emerald-300/75 bg-emerald-300 shadow-[0_0_18px_rgba(52,211,153,0.8)]" />
                <div
                  className={cn(
                    'absolute min-w-[126px] max-w-[152px] rounded-2xl border border-white/10 bg-[#111922]/94 px-3 py-1.5 text-center text-[10px] font-medium leading-4 text-white/84 shadow-lg backdrop-blur-xl',
                    node.bubbleClassName
                  )}
                >
                  {node.label}
                </div>
              </motion.div>
            ))}
          </div>

          <div className="relative mt-3 rounded-lg border border-white/8 bg-white/[0.03] px-4 py-3.5">
            <div className="flex items-start gap-3">
              <div className="mt-1 h-2 w-2 rounded-full bg-emerald-300 shadow-[0_0_14px_rgba(52,211,153,0.8)]" />
              <div className="min-w-0 flex-1">
                <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-emerald-200/72">
                  {activeContent.insightLabel}
                </p>
                <div className="relative mt-1 min-h-[46px] overflow-hidden">
                  <AnimatePresence mode="wait" initial={false}>
                    <motion.p
                      key={`${mode}-${insightIndex}`}
                      className="absolute inset-0 text-sm font-medium leading-6 text-white/88"
                      initial={reduceMotion ? false : { opacity: 0, y: 10 }}
                      animate={{ opacity: 1, y: 0 }}
                      exit={reduceMotion ? undefined : { opacity: 0, y: -10 }}
                      transition={{ duration: reduceMotion ? 0 : 0.24, ease: 'easeOut' }}
                    >
                      {activeContent.insights[insightIndex]}
                    </motion.p>
                  </AnimatePresence>
                </div>
              </div>
            </div>
          </div>

          <div className="relative mt-3 rounded-lg border border-emerald-300/14 bg-emerald-500/[0.055] px-4 py-3">
            <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-emerald-200/72">
              Suggested next action
            </p>
            <p className="mt-1 text-sm leading-6 text-white/76">
              Add dose schedule -&gt; track recovery + sleep -&gt; evaluate after 7 days
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
