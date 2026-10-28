'use client';

import type { DoseLogEntry } from '@/lib/types';
import { GlassCard } from '@/components/ui/GlassCard';

interface DoseHistoryProps {
  entries: DoseLogEntry[];
  onDelete: (entry: DoseLogEntry) => void;
}

function formatLogDate(iso: string) {
  const [year, month, day] = iso.split('-').map(Number);
  if (!year || !month || !day) return iso;
  return new Date(Date.UTC(year, month - 1, day)).toLocaleDateString(undefined, {
    timeZone: 'UTC',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

/** The amounts the user logged, exactly as they typed them. */
export function DoseHistory({ entries, onDelete }: DoseHistoryProps) {
  return (
    <GlassCard variant="base" className="p-6 lg:col-span-3">
      <h3 className="text-lg font-semibold text-white">Your logged amounts</h3>
      <p className="text-sm text-white/45">What you recorded taking, in your own words and numbers.</p>
      {entries.length === 0 ? (
        <p className="mt-4 text-sm text-white/55">No amounts logged yet.</p>
      ) : (
        <ul className="mt-4 space-y-2" aria-label="Logged amounts">
          {entries.map((entry) => (
            <li
              key={entry.id}
              className="flex items-center justify-between gap-4 rounded-xl bg-white/[0.03] p-3 text-sm"
            >
              <div>
                <span className="font-medium text-white">{entry.compoundName}</span>
                <span className="ml-2 text-emerald-200/80">{entry.amount} {entry.unit}</span>
                {entry.note && <div className="text-xs text-white/55">{entry.note}</div>}
              </div>
              <div className="flex items-center gap-3">
                <span className="text-xs text-white/45">{formatLogDate(entry.logDate)}</span>
                <button
                  type="button"
                  onClick={() => onDelete(entry)}
                  aria-label={`Delete logged amount for ${entry.compoundName} on ${entry.logDate}`}
                  className="rounded-lg border border-white/10 px-2 py-1 text-xs text-white/65 hover:border-white/20"
                >
                  Delete
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </GlassCard>
  );
}
