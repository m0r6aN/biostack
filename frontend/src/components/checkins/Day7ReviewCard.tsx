'use client';

import { SuggestionCard } from '@/components/suggestions/SuggestionCard';
import type { EarnedSuggestion } from '@/lib/earnedSuggestions';
import { Day7Review, Day7ReviewTrend } from '@/lib/types';
import { useMemo, useState } from 'react';

interface Day7ReviewCardProps {
  review: Day7Review;
  suggestion?: EarnedSuggestion | null;
}

const trendLabels: Record<Day7ReviewTrend, string> = {
  improving: 'Improving',
  flat: 'Flat',
  declining: 'Declining',
  insufficient_data: 'Needs more data',
};

const nextStepLabels: Record<Day7Review['nextStep'], string> = {
  continue: 'Continue observing this pattern.',
  reassess: 'Reassess before changing anything.',
  track_longer: 'Track longer before drawing a conclusion.',
};

export function Day7ReviewCard({ review, suggestion = null }: Day7ReviewCardProps) {
  const suggestionKey = useMemo(
    () => (suggestion ? `${suggestion.type}:${suggestion.reasoning}` : null),
    [suggestion]
  );
  const [dismissedSuggestionKey, setDismissedSuggestionKey] = useState<string | null>(null);
  const shouldShowSuggestion = Boolean(suggestion && suggestionKey !== dismissedSuggestionKey);

  if (!review.isEarned) {
    return (
      <section
        aria-label="Day 7 Review pending"
        className="rounded-lg border border-white/[0.08] bg-[#121923]/60 p-6"
      >
        <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div>
            <p className="text-xs font-black uppercase tracking-widest text-white/30">Day 7 Review</p>
            <h2 className="mt-2 text-xl font-black text-white">Keep collecting observations</h2>
            <p className="mt-2 max-w-2xl text-sm leading-relaxed text-white/50">{review.confidenceNote}</p>
          </div>
          <div className="rounded-lg border border-white/[0.08] bg-white/[0.03] px-4 py-3 text-sm text-white/60">
            {review.coveredDays}/{review.requiredDays} check-ins
          </div>
        </div>
      </section>
    );
  }

  return (
    <section
      aria-label="Day 7 Review"
      className="rounded-lg border border-emerald-500/20 bg-[#121923]/70 p-6 shadow-[0_16px_50px_rgba(0,0,0,0.25)]"
    >
      <div className="mb-5 flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
        <div>
          <p className="text-xs font-black uppercase tracking-widest text-emerald-300/70">Day 7 Review</p>
          <h2 className="mt-2 text-2xl font-black text-white">Recent signal check</h2>
        </div>
        <div className="rounded-lg border border-white/[0.08] bg-white/[0.03] px-4 py-3 text-sm font-bold text-white/70">
          Signal: {review.signalStrength}
        </div>
      </div>

      <p className="max-w-3xl text-base leading-relaxed text-white/75">{review.trendSummary}</p>

      <div className="mt-5 grid grid-cols-1 gap-3 md:grid-cols-3">
        <TrendPill label="Sleep" trend={review.sleepTrend} />
        <TrendPill label="Energy" trend={review.energyTrend} />
        <TrendPill label="Recovery" trend={review.recoveryTrend} />
      </div>

      <div className="mt-5 grid grid-cols-1 gap-3 md:grid-cols-2">
        <div className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
          <p className="text-xs font-black uppercase tracking-widest text-white/30">Expected timing</p>
          <p className="mt-2 text-sm text-white/65">Alignment is {review.alignmentWithExpected}.</p>
        </div>
        <div className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
          <p className="text-xs font-black uppercase tracking-widest text-white/30">Next step</p>
          <p className="mt-2 text-sm text-white/65">{nextStepLabels[review.nextStep]}</p>
        </div>
      </div>

      <p className="mt-4 text-xs leading-relaxed text-white/35">{review.confidenceNote}</p>

      {suggestion && shouldShowSuggestion && (
        <div className="mt-5">
          <SuggestionCard suggestion={suggestion} onDismiss={() => setDismissedSuggestionKey(suggestionKey)} />
        </div>
      )}
    </section>
  );
}

function TrendPill({ label, trend }: { label: string; trend: Day7ReviewTrend }) {
  const tone =
    trend === 'improving'
      ? 'border-emerald-500/20 text-emerald-300'
      : trend === 'declining'
        ? 'border-red-500/20 text-red-300'
        : 'border-white/[0.08] text-white/55';

  return (
    <div className={`rounded-lg border bg-white/[0.03] p-4 ${tone}`}>
      <p className="text-xs font-black uppercase tracking-widest text-white/30">{label}</p>
      <p className="mt-2 text-sm font-bold">{trendLabels[trend]}</p>
    </div>
  );
}
