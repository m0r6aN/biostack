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
  if (mode === 'CameraScan') {
    return [
      'Reading image',
      'Extracting text from photo',
      'Resolving compound aliases',
      'Checking pathway overlap',
      'Scoring protocol',
      'Comparing alternatives',
    ];
  }
  if (mode === 'Link') {
    return [
      'Fetching shared document',
      'Extracting text',
      'Resolving compound aliases',
      'Checking pathway overlap',
      'Scoring protocol',
      'Comparing alternatives',
    ];
  }
  if (mode === 'FileUpload') {
    return [
      'Extracting text',
      'Reading table structure',
      'Resolving compound aliases',
      'Checking pathway overlap',
      'Scoring protocol',
      'Comparing alternatives',
    ];
  }
  return [
    'Extracting text',
    'Normalizing protocol rows',
    'Resolving compound aliases',
    'Checking pathway overlap',
    'Scoring protocol',
    'Comparing alternatives',
  ];
}
