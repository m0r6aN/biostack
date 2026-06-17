'use client';

import { useState } from 'react';
import { motion, useReducedMotion } from 'framer-motion';
import { HelpTip } from '@/components/ui/HelpTip';
import type { HelpTipKey } from '@/lib/helpTips';
import type { ProtocolAnalyzerResult } from '@/lib/types';
import { getScoreLabel } from '../analyzerView';

// ── ScoreChip ─────────────────────────────────────────────────────────────────
// Moved from monolith ~1262-1283, preserved verbatim.

function ScoreChip({ label, value, tone, helpKey }: {
  label: string;
  value: number;
  tone: 'positive' | 'negative' | 'neutral';
  helpKey?: HelpTipKey;
}) {
  const toneClass =
    tone === 'positive'
      ? 'border-emerald-300/20 bg-emerald-400/10 text-emerald-50'
      : tone === 'negative'
        ? 'border-red-300/20 bg-red-400/10 text-red-50'
        : 'border-white/10 bg-white/[0.04] text-white';

  return (
    <div className={`rounded-lg border p-3 ${toneClass}`}>
      <p className="text-xs font-semibold uppercase tracking-[0.16em] opacity-70">
        {helpKey ? <HelpTip tipKey={helpKey}>{label}</HelpTip> : label}
      </p>
      <p className="mt-2 text-lg font-semibold">{value > 0 ? `+${value}` : value}</p>
    </div>
  );
}

// ── Arc gauge helpers ─────────────────────────────────────────────────────────

// 270° sweep, starting at 135° (bottom-left), ending at 45° (bottom-right).
// Centre (cx,cy) = (72,72), radius 64, strokeWidth 10.
const CX = 72;
const CY = 72;
const RADIUS = 64;
const START_ANGLE_DEG = 135; // where the arc begins (clockwise)
const SWEEP_DEG = 270;

function polarToCartesian(angle: number): { x: number; y: number } {
  const rad = (angle * Math.PI) / 180;
  return {
    x: CX + RADIUS * Math.cos(rad),
    y: CY + RADIUS * Math.sin(rad),
  };
}

function arcPath(fraction: number): string {
  if (fraction <= 0) return '';
  const endAngle = START_ANGLE_DEG + fraction * SWEEP_DEG;
  const clamped = Math.min(endAngle, START_ANGLE_DEG + SWEEP_DEG - 0.001);
  const start = polarToCartesian(START_ANGLE_DEG);
  const end = polarToCartesian(clamped);
  const largeArc = fraction * SWEEP_DEG > 180 ? 1 : 0;
  return `M ${start.x} ${start.y} A ${RADIUS} ${RADIUS} 0 ${largeArc} 1 ${end.x} ${end.y}`;
}

const TRACK_PATH = arcPath(1);

function bandColor(score: number): string {
  if (score >= 80) return '#34d399'; // emerald-400
  if (score >= 60) return '#fbbf24'; // amber-400
  return '#f87171'; // red-400
}

// ── ScoreHero ─────────────────────────────────────────────────────────────────

export interface ScoreHeroProps {
  result: ProtocolAnalyzerResult;
  scoreInsight: string;
  whatThisMeans: string;
}

export function ScoreHero({ result, scoreInsight, whatThisMeans }: ScoreHeroProps) {
  const reducedMotion = useReducedMotion();
  const [showWhyScore, setShowWhyScore] = useState(false);
  const [displayScore, setDisplayScore] = useState(reducedMotion ? result.score : 0);

  const fraction = result.score / 100;
  const color = bandColor(result.score);
  const scoreLabel = getScoreLabel(result.score);

  // Band tone for container border/bg (same thresholds as monolith ~160-165).
  const scoreTone =
    result.score >= 80
      ? 'border-emerald-300/25 bg-emerald-400/[0.12]'
      : result.score >= 60
        ? 'border-amber-300/25 bg-amber-400/[0.12]'
        : 'border-red-300/25 bg-red-400/[0.12]';

  const foregroundPath = arcPath(fraction);

  return (
    <section className={`rounded-lg border p-5 ${scoreTone}`}>
      {/* Arc gauge */}
      <div
        className="relative mx-auto flex items-center justify-center"
        style={{ width: 144, height: 144 }}
        aria-label={`BioStack score ${result.score} out of 100`}
      >
        <svg
          viewBox="0 0 144 144"
          width={144}
          height={144}
          className="absolute inset-0"
          aria-hidden="true"
        >
          {/* Track */}
          <path
            d={TRACK_PATH}
            fill="none"
            stroke="rgba(255,255,255,0.08)"
            strokeWidth={10}
            strokeLinecap="round"
          />
          {/* Foreground arc */}
          {reducedMotion ? (
            <path
              d={foregroundPath}
              fill="none"
              stroke={color}
              strokeWidth={10}
              strokeLinecap="round"
            />
          ) : (
            <motion.path
              d={foregroundPath}
              fill="none"
              stroke={color}
              strokeWidth={10}
              strokeLinecap="round"
              initial={{ pathLength: 0 }}
              animate={{ pathLength: fraction > 0 ? 1 : 0 }}
              transition={{ duration: 1.2, ease: 'easeOut' }}
              onUpdate={(latest) => {
                const pct = typeof latest.pathLength === 'number' ? latest.pathLength : 0;
                setDisplayScore(Math.round(pct * result.score));
              }}
            />
          )}
        </svg>

        {/* Numeric score — plain text so it is always queryable */}
        <div className="relative z-10 text-center">
          <span className="block text-3xl font-semibold tracking-tight text-white">
            {displayScore}
          </span>
          <span className="block text-xs text-white/42">/ 100</span>
        </div>
      </div>

      {/* Label + insight */}
      <p className="mt-4 text-center text-base font-semibold text-white/85">{scoreLabel}</p>
      <p className="mt-2 text-center text-sm leading-6 text-white/62">{scoreInsight}</p>

      {/* Why this score toggle */}
      <button
        type="button"
        onClick={() => setShowWhyScore((v) => !v)}
        aria-expanded={showWhyScore}
        className="mt-4 w-full text-sm font-semibold text-white/72 transition-colors hover:text-white"
      >
        {showWhyScore ? 'Hide score breakdown' : 'Why this score?'}
      </button>

      {showWhyScore && (
        <div className="mt-4 grid gap-3 sm:grid-cols-2">
          <ScoreChip label="Base" value={result.scoreExplanation.baseScore} tone="neutral" />
          <ScoreChip label="Synergy"      helpKey="synergy"      value={result.scoreExplanation.synergy}      tone="positive" />
          <ScoreChip label="Redundancy"   helpKey="redundancy"   value={result.scoreExplanation.redundancy}   tone="negative" />
          <ScoreChip label="Interference" helpKey="interference" value={result.scoreExplanation.interference} tone="negative" />
        </div>
      )}

      {/* What this means callout — copy from monolith WhatThisMeansPanel ~853 */}
      {whatThisMeans && (
        <section className="mt-4 rounded-lg border border-emerald-300/18 bg-emerald-400/[0.08] p-4">
          <h2 className="text-lg font-semibold text-white">What this means</h2>
          <p className="mt-2 text-sm leading-6 text-white/68">{whatThisMeans}</p>
        </section>
      )}
    </section>
  );
}
