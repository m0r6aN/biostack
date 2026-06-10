'use client';

import type { SupplementEntry, SupplementPlan } from '@/lib/types';
import { GlassCard } from '@/components/ui/GlassCard';
import { cn } from '@/lib/utils';

export function SupplementationTab({ supplements }: { supplements: SupplementPlan }) {
  const midpoint = Math.ceil(supplements.entries.length / 2);
  const columns = [supplements.entries.slice(0, midpoint), supplements.entries.slice(midpoint)];

  return (
    <GlassCard variant="base" className="p-6 sm:p-8">
      <h3 className="text-2xl font-semibold text-white">{supplements.title}</h3>
      <p className="mt-1 text-white/55">{supplements.summary}</p>

      <div className="mt-6 grid gap-x-8 gap-y-6 text-sm md:grid-cols-2">
        {columns.map((column, columnIndex) => (
          <div key={columnIndex} className="space-y-4">
            {column.map((entry) => (
              <SupplementItem key={entry.name} entry={entry} />
            ))}

            {/* Append the "additional" list to the second column */}
            {columnIndex === 1 && supplements.additional.length > 0 && (
              <div className="border-t border-white/[0.08] pt-3">
                <div className="text-xs text-white/45">Additional (recommended):</div>
                <div className="text-sm text-white/75">{supplements.additional.join(', ')}</div>
              </div>
            )}
          </div>
        ))}
      </div>
    </GlassCard>
  );
}

function SupplementItem({ entry }: { entry: SupplementEntry }) {
  return (
    <div>
      <strong className="block font-semibold text-white">
        {entry.name} — {entry.dose}
      </strong>
      {entry.note && (
        <span className={cn('text-xs', entry.emphasis ? 'font-medium text-emerald-300' : 'text-white/45')}>
          {entry.note}
        </span>
      )}
    </div>
  );
}
