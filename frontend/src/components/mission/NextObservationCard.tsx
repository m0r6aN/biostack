'use client';

import Link from 'next/link';
import { deriveObservationDebt, type ObservationDebtItem } from '@/lib/derive/observationDebt';
import { cn } from '@/lib/utils';
import type { ProtocolConsolePayload, CheckIn, CompoundRecord, GoalDefinition } from '@/lib/types';

interface NextObservationCardProps {
  payload: ProtocolConsolePayload | null;
  checkIns: CheckIn[];
  compounds: CompoundRecord[];
  goals: GoalDefinition[];
  className?: string;
}

const TYPE_ICONS: Record<string, string> = {
  'missing-first-checkin': '●',
  'expected-next-event-due': '◷',
  'cadence-gap': '○',
  'metric-missing-for-goal': '◻',
  'review-not-completed': '☐',
};

export function NextObservationCard({ payload, checkIns, compounds, goals, className }: NextObservationCardProps) {
  const items = deriveObservationDebt(payload, checkIns, compounds, goals);
  const top = items[0];

  if (!top) {
    return (
      <div className={cn('rounded-3xl border border-white/5 bg-white/[0.02] p-5', className)}>
        <p className="text-[10px] font-bold text-white/20 uppercase tracking-widest mb-2">Next Best Observation</p>
        <div className="flex items-center gap-3">
          <span className="text-emerald-400 text-xl">✓</span>
          <div>
            <p className="text-sm font-semibold text-white/80">All signals current</p>
            <p className="text-xs text-white/40 mt-0.5">No observation debt detected. Keep up the cadence.</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={cn('rounded-3xl border border-white/5 bg-white/[0.02] p-5', className)}>
      <div className="flex items-center justify-between mb-4">
        <p className="text-[10px] font-bold text-white/20 uppercase tracking-widest">Next Best Observation</p>
        {items.length > 1 && (
          <span className="text-[10px] text-white/30 font-medium">{items.length} items</span>
        )}
      </div>

      <div className="space-y-3">
        {/* Top priority item */}
        <div className="rounded-2xl border border-white/8 bg-white/[0.03] p-4">
          <div className="flex items-start gap-3">
            <span className="text-white/30 mt-0.5 text-sm leading-none">{TYPE_ICONS[top.type] ?? '○'}</span>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-white/90 leading-snug">{top.title}</p>
              <p className="text-xs text-white/50 mt-1 leading-relaxed">{top.reason}</p>
              {top.impact && (
                <p className="text-xs text-emerald-400/70 mt-1.5 font-medium">↑ {top.impact}</p>
              )}
            </div>
          </div>
          <div className="mt-3 flex justify-end">
            <Link
              href={top.ctaHref}
              className="inline-flex items-center gap-1.5 text-xs font-semibold text-emerald-400 hover:text-emerald-300 transition-colors"
            >
              {top.ctaLabel}
              <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
                <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5 21 12m0 0-7.5 7.5M21 12H3" />
              </svg>
            </Link>
          </div>
        </div>

        {/* Secondary items (condensed) */}
        {items.slice(1, 3).map((item) => (
          <Link
            key={item.type}
            href={item.ctaHref}
            className="flex items-center gap-3 rounded-2xl border border-white/5 bg-white/[0.02] px-4 py-2.5 hover:bg-white/[0.04] transition-colors group"
          >
            <span className="text-white/20 text-xs">{TYPE_ICONS[item.type] ?? '○'}</span>
            <span className="text-xs text-white/50 flex-1 truncate">{item.title}</span>
            <svg className="w-3 h-3 text-white/20 group-hover:text-white/40 transition-colors shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
              <path strokeLinecap="round" strokeLinejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
            </svg>
          </Link>
        ))}
      </div>
    </div>
  );
}
