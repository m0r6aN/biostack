import type { ScheduleItem } from '@/lib/types';

/**
 * The user's own schedule choice and the amounts they logged for one schedule row. Weekly-target and
 * as-needed entries are labelled as such because they have no fixed day.
 */
export function ScheduleItemDetails({ item }: { item: ScheduleItem }) {
  const logged = item.logged ?? [];
  return (
    <>
      {item.cadence && item.due === 'scheduled' && (
        <div className="text-xs text-white/55">Your schedule: {item.cadence}</div>
      )}
      {item.due === 'weekly_target' && (
        <div className="text-xs text-sky-200/80">
          Weekly target: {item.cadence?.replace(' (weekly target)', '') ?? 'set by you'}
          {item.weekLogged != null && ` · ${item.weekLogged} logged this week`}
        </div>
      )}
      {item.due === 'as_needed' && <div className="text-xs text-sky-200/80">As needed (no fixed day)</div>}
      {logged.length > 0 && (
        <ul aria-label={`Logged amounts for ${item.name}`} className="mt-1 space-y-0.5">
          {logged.map((entry) => (
            <li key={entry.id} className="text-xs font-medium text-emerald-300">
              Logged: {entry.amount} {entry.unit}
              {entry.note ? ` · ${entry.note}` : ''}
            </li>
          ))}
        </ul>
      )}
    </>
  );
}
