'use client';

import { useState } from 'react';
import Link from 'next/link';
import { deriveObservationDebt, type ObservationDebtItem } from '@/lib/derive/observationDebt';
import { cn } from '@/lib/utils';
import { track } from '@/lib/telemetry';
import type { ProtocolConsolePayload, CheckIn, CompoundRecord, GoalDefinition } from '@/lib/types';

interface ObservationDebtInboxProps {
  payload: ProtocolConsolePayload | null;
  checkIns: CheckIn[];
  compounds: CompoundRecord[];
  goals: GoalDefinition[];
  className?: string;
}

const PRIORITY_COLOR: Record<number, string> = {
  1: 'text-rose-400',
  2: 'text-amber-400',
  3: 'text-amber-300',
  4: 'text-blue-400',
  5: 'text-white/50',
};

export function ObservationDebtInbox({ payload, checkIns, compounds, goals, className }: ObservationDebtInboxProps) {
  const [dismissed, setDismissed] = useState<Set<string>>(new Set());
  const allItems = deriveObservationDebt(payload, checkIns, compounds, goals);
  const items = allItems.filter((i) => !dismissed.has(i.type));

  if (items.length === 0) {
    return (
      <div className={cn('rounded-3xl border border-white/5 bg-white/[0.02] p-5', className)}>
        <div className="flex items-center gap-3">
          <span className="flex items-center justify-center w-8 h-8 rounded-xl bg-emerald-500/10 border border-emerald-400/20 text-emerald-400">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
              <path strokeLinecap="round" strokeLinejoin="round" d="m4.5 12.75 6 6 9-13.5" />
            </svg>
          </span>
          <div>
            <p className="text-sm font-semibold text-white/80">Inbox clear</p>
            <p className="text-xs text-white/40">No check-ins due. All signal paths current.</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={cn('rounded-3xl border border-white/8 bg-white/[0.02]', className)}>
      {/* Header */}
      <div className="flex items-center justify-between px-5 pt-5 pb-3">
        <div className="flex items-center gap-2.5">
          <p className="text-[10px] font-bold text-white/20 uppercase tracking-widest">Check-ins Due</p>
          <span className="text-[10px] font-bold text-white/30 bg-white/5 rounded-full px-2 py-0.5">{items.length}</span>
        </div>
        <p className="text-[10px] text-white/25">Ranked by impact</p>
      </div>

      {/* Items */}
      <div className="px-4 pb-4 space-y-2">
        {items.map((item) => (
          <InboxItem
            key={item.type}
            item={item}
            onDismiss={item.dismissable ? () => {
              setDismissed((prev) => new Set([...prev, item.type]));
              track({ name: 'observation_debt_resolve', itemType: item.type });
            } : undefined}
          />
        ))}
      </div>
    </div>
  );
}

function InboxItem({ item, onDismiss }: { item: ObservationDebtItem; onDismiss?: () => void }) {
  const color = PRIORITY_COLOR[item.priority] ?? 'text-white/40';

  return (
    <div className="flex items-start gap-3 rounded-2xl border border-white/5 bg-white/[0.02] px-4 py-3 group hover:bg-white/[0.04] transition-colors">
      {/* Priority dot */}
      <div className={cn('mt-1 w-1.5 h-1.5 rounded-full shrink-0 bg-current', color)} />

      <div className="flex-1 min-w-0">
        <p className="text-xs font-semibold text-white/80 leading-snug">{item.title}</p>
        <p className="text-[11px] text-white/45 mt-0.5 leading-relaxed">{item.reason}</p>
        {item.impact && (
          <p className="text-[10px] text-emerald-400/60 mt-1 font-medium">↑ {item.impact}</p>
        )}
      </div>

      <div className="flex items-center gap-1.5 shrink-0">
        <Link
          href={item.ctaHref}
          className="text-[11px] font-semibold text-emerald-400/80 hover:text-emerald-300 transition-colors whitespace-nowrap"
        >
          {item.ctaLabel}
        </Link>
        {onDismiss && (
          <button
            onClick={onDismiss}
            className="p-1 rounded-lg hover:bg-white/5 text-white/20 hover:text-white/40 transition-colors"
            title="Dismiss"
          >
            <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
            </svg>
          </button>
        )}
      </div>
    </div>
  );
}
