'use client';

import { useEffect, useRef, useState } from 'react';
import Link from 'next/link';
import type { AnalyzerContextFields } from './useAnalyzerSession';
import type { PersonProfile } from '@/lib/types';
import { trackAnalyzerEvent } from '@/lib/analyzerAnalytics';

type RefineAnalysisPanelProps = {
  context: AnalyzerContextFields;
  onChange: (context: AnalyzerContextFields) => void;
  profile: PersonProfile | null;
  isAuthenticated: boolean;
};

const inputClass =
  'min-h-12 w-full rounded-lg border border-white/10 bg-[#0F141B] px-4 text-sm text-white outline-none transition-colors placeholder:text-white/30 focus:border-emerald-400/45';

const labelClass = 'block text-xs text-white/50 mb-1';

export function RefineAnalysisPanel({
  context,
  onChange,
  profile,
  isAuthenticated,
}: RefineAnalysisPanelProps) {
  const [expanded, setExpanded] = useState(false);
  const [prefillApplied, setPrefillApplied] = useState(false);
  const hasOpenedOnce = useRef(false);
  const hasPrefilled = useRef(false);

  // Profile prefill: fire once if profile exists and all context fields are empty
  useEffect(() => {
    if (hasPrefilled.current) return;
    if (!profile) return;
    const allEmpty =
      context.sex === '' &&
      context.age === '' &&
      context.weight === '' &&
      context.existingStack === '';
    if (!allEmpty) return;

    hasPrefilled.current = true;
    onChange({
      sex: profile.sex ?? '',
      age: profile.age != null ? String(profile.age) : '',
      weight: profile.weight != null ? String(profile.weight) : '',
      existingStack: '',
    });
    setPrefillApplied(true);
    trackAnalyzerEvent('analyzer_context_prefilled');
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function handleToggle() {
    const next = !expanded;
    setExpanded(next);
    if (next && !hasOpenedOnce.current) {
      hasOpenedOnce.current = true;
      trackAnalyzerEvent('analyzer_context_opened');
    }
  }

  function handleNudgeClick() {
    trackAnalyzerEvent('analyzer_profile_nudge_clicked');
  }

  const profilePrefilled =
    (profile !== null && prefillApplied) ||
    (profile !== null && (context.sex !== '' || context.age !== '' || context.weight !== ''));

  return (
    <div className="rounded-lg border border-white/10 bg-black/20 p-3">
      {/* Toggle row */}
      <div className="flex items-center justify-between gap-2">
        <div>
          <button
            type="button"
            onClick={handleToggle}
            className="text-sm font-medium text-white/80 hover:text-white transition-colors"
          >
            Refine analysis (optional)
          </button>
          {!expanded && (
            <p className="text-xs text-white/40 mt-0.5">
              Add context to sharpen scoring. Nothing here is required.
            </p>
          )}
        </div>
        <button
          type="button"
          onClick={handleToggle}
          aria-label={expanded ? 'Collapse refine panel' : 'Expand refine panel'}
          className="text-white/40 hover:text-white/70 transition-colors text-xs"
        >
          {expanded ? '▲' : '▼'}
        </button>
      </div>

      {/* Expanded section */}
      {expanded && (
        <div className="mt-3 space-y-3">
          {/* Profile badge or nudge */}
          {profilePrefilled ? (
            <div className="flex items-center justify-between gap-2">
              <span className="text-xs text-emerald-400 bg-emerald-400/10 rounded px-2 py-0.5">
                From your profile
              </span>
              <Link href="/profiles" className="text-xs text-white/40 hover:text-white/60 transition-colors">
                Edit profile
              </Link>
            </div>
          ) : (
            <div>
              {!isAuthenticated ? (
                <Link
                  href="/auth/signin?callbackUrl=/tools/analyzer"
                  onClick={handleNudgeClick}
                  className="text-xs text-emerald-400/80 hover:text-emerald-400 transition-colors underline underline-offset-2"
                >
                  Create a profile to autofill this and track your results over time.
                </Link>
              ) : (
                <Link
                  href="/profiles"
                  onClick={handleNudgeClick}
                  className="text-xs text-emerald-400/80 hover:text-emerald-400 transition-colors underline underline-offset-2"
                >
                  Create a profile to autofill this and track your results over time.
                </Link>
              )}
            </div>
          )}

          {/* Sex */}
          <div>
            <label htmlFor="refine-sex" className={labelClass}>
              Sex
            </label>
            <select
              id="refine-sex"
              value={context.sex}
              onChange={(e) => onChange({ ...context, sex: e.target.value })}
              className={inputClass}
            >
              <option value="">Not specified</option>
              <option value="male">Male</option>
              <option value="female">Female</option>
            </select>
          </div>

          {/* Age */}
          <div>
            <label htmlFor="refine-age" className={labelClass}>
              Age
            </label>
            <input
              id="refine-age"
              type="number"
              min={18}
              max={120}
              value={context.age}
              onChange={(e) => onChange({ ...context, age: e.target.value })}
              placeholder="e.g. 35"
              className={inputClass}
            />
          </div>

          {/* Weight */}
          <div>
            <label htmlFor="refine-weight" className={labelClass}>
              Weight
            </label>
            <div className="relative flex items-center">
              <input
                id="refine-weight"
                type="number"
                value={context.weight}
                onChange={(e) => onChange({ ...context, weight: e.target.value })}
                placeholder="e.g. 80"
                className={`${inputClass} pr-12`}
              />
              <span className="absolute right-4 text-xs text-white/40 pointer-events-none">kg</span>
            </div>
          </div>

          {/* Existing stack */}
          <div>
            <label htmlFor="refine-stack" className={labelClass}>
              Current medications or stack
            </label>
            <textarea
              id="refine-stack"
              rows={3}
              value={context.existingStack}
              onChange={(e) => onChange({ ...context, existingStack: e.target.value })}
              placeholder="One item per line"
              className={`${inputClass} min-h-[5rem] resize-y py-3`}
            />
          </div>
        </div>
      )}
    </div>
  );
}
