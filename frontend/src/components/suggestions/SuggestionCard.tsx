'use client';

import type { EarnedSuggestion } from '@/lib/earnedSuggestions';

interface SuggestionCardProps {
  suggestion: EarnedSuggestion;
  onDismiss?: () => void;
}

export function SuggestionCard({ suggestion, onDismiss }: SuggestionCardProps) {
  return (
    <aside
      aria-label="Earned suggestion"
      className="rounded-lg border border-cyan-300/15 bg-cyan-300/[0.045] p-4"
    >
      <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
        <div className="max-w-3xl">
          <p className="text-xs font-black uppercase tracking-widest text-cyan-200/60">Optional context</p>
          <h3 className="mt-2 text-base font-black text-white">{suggestion.title}</h3>
          <p className="mt-2 text-sm leading-6 text-white/65">{suggestion.explanation}</p>
          <p className="mt-3 text-xs leading-5 text-white/42">{suggestion.reasoning}</p>
        </div>

        <div className="flex shrink-0 items-center gap-2">
          {suggestion.actionLabel && (
            <button
              type="button"
              className="rounded-lg border border-cyan-200/20 px-3 py-2 text-xs font-bold text-cyan-100 transition-colors hover:border-cyan-200/35 hover:bg-cyan-200/10"
            >
              {suggestion.actionLabel}
            </button>
          )}
          {onDismiss && (
            <button
              type="button"
              onClick={onDismiss}
              className="rounded-lg border border-white/[0.08] px-3 py-2 text-xs font-bold text-white/45 transition-colors hover:bg-white/[0.04] hover:text-white/70"
            >
              Not now
            </button>
          )}
        </div>
      </div>
    </aside>
  );
}
