'use client';

import type { DaySchedule } from '@/lib/types';
import { ModalShell } from './ModalShell';

interface DayDetailModalProps {
  day: DaySchedule;
  onClose: () => void;
}

export function DayDetailModal({ day, onClose }: DayDetailModalProps) {
  return (
    <ModalShell onClose={onClose} labelledBy="day-modal-title">
      <div className="pr-8">
        <h2 id="day-modal-title" className="text-2xl font-semibold tracking-tight text-white">
          {day.title}
        </h2>
        <p className="mt-0.5 text-sm text-white/45">{day.subtitle}</p>
      </div>

      <div className="mt-5 space-y-3 text-sm">
        {day.items.map((item, index) => (
          <div
            key={`${item.time}-${item.name}-${index}`}
            className="flex items-start justify-between gap-4 border-b border-white/[0.06] pb-3 last:border-none last:pb-0"
          >
            <div>
              <div className="font-semibold text-white">{item.name}</div>
              <div className="text-xs text-white/45">{item.detail}</div>
            </div>
            <div className="whitespace-nowrap pt-0.5 text-right font-mono text-xs text-white/45">
              {item.time}
            </div>
          </div>
        ))}
      </div>

      <p className="mt-6 border-t border-white/[0.06] pt-4 text-xs text-white/35">
        All times are suggested. Adjust based on your routine while maintaining consistency.
      </p>
    </ModalShell>
  );
}
