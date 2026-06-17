'use client';

import { ChevronLeft, ChevronRight, Info } from 'lucide-react';
import type { PortalPhase, WeekDay } from '@/lib/types';
import { GlassCard } from '@/components/ui/GlassCard';
import { cn } from '@/lib/utils';

interface CalendarTabProps {
  week: WeekDay[];
  activePhase: PortalPhase;
  weekLabel: string;
  doseHeadline: string;
  onSelectDay: (dateIso: string) => void;
  onPreviousWeek: () => void;
  onNextWeek: () => void;
}

const WEEKDAY_HEADERS = ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'];

export function CalendarTab({
  week,
  activePhase,
  weekLabel,
  doseHeadline,
  onSelectDay,
  onPreviousWeek,
  onNextWeek,
}: CalendarTabProps) {
  return (
    <GlassCard variant="base" className="p-6">
      <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h3 className="text-xl font-semibold text-white">Weekly Schedule</h3>
          <p className="text-sm text-white/45">
            Week {activePhase.currentWeek} · {activePhase.label} · {doseHeadline}
          </p>
        </div>
        <div className="flex items-center gap-2 text-sm">
          <button
            type="button"
            onClick={onPreviousWeek}
            aria-label="Previous week"
            className="rounded-xl border border-white/[0.1] px-3 py-1.5 text-white/70 transition-colors hover:border-white/20"
          >
            <ChevronLeft className="h-4 w-4" />
          </button>
          <span className="px-2 font-medium text-white/80">{weekLabel}</span>
          <button
            type="button"
            onClick={onNextWeek}
            aria-label="Next week"
            className="rounded-xl border border-white/[0.1] px-3 py-1.5 text-white/70 transition-colors hover:border-white/20"
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>
      </div>

      <div className="grid grid-cols-7 gap-2 sm:gap-3">
        {WEEKDAY_HEADERS.map((header) => (
          <div key={header} className="py-2 text-center text-[10px] font-semibold tracking-wide text-white/40 sm:text-xs">
            {header}
          </div>
        ))}

        {week.map((day) => (
          <button
            key={day.dateIso}
            type="button"
            onClick={() => onSelectDay(day.dateIso)}
            className={cn(
              'rounded-2xl border p-3 text-center transition-colors',
              day.isToday
                ? 'border-emerald-400/40 bg-emerald-500/10'
                : 'border-white/[0.08] hover:bg-white/[0.04]'
            )}
          >
            <div className={cn('text-xs', day.isToday ? 'text-emerald-300' : 'text-white/40')}>
              {day.dayLabel}
            </div>
            <div className="mt-1 text-sm font-medium text-white">{day.weekdayLabel}</div>
            {day.tag ? (
              <div className="mt-1 rounded px-1.5 py-px text-[10px] text-emerald-200">{day.tag}</div>
            ) : day.isToday ? (
              <div className="mt-1 rounded bg-white/10 px-1.5 py-px text-[10px] text-white/70">
                {day.itemCount} items
              </div>
            ) : null}
          </button>
        ))}
      </div>

      <div className="mt-6 flex items-center gap-2 text-xs text-white/45">
        <Info className="h-3.5 w-3.5" />
        <span>Tap any day to view full scheduled substances and times.</span>
      </div>
    </GlassCard>
  );
}
