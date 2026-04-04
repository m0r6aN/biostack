import { TimelineEvent } from '@/lib/types';
import { formatDateTime, getEventIcon } from '@/lib/utils';

interface TimelineSnapshotProps {
  events: TimelineEvent[];
}

export function TimelineSnapshot({ events }: TimelineSnapshotProps) {
  const recent = events.slice(0, 5);

  return (
    <div className="p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90 shadow-[0_8px_24px_rgba(0,0,0,0.35)]">
      <h3 className="text-lg font-semibold text-white mb-4">Recent Events</h3>
      {recent.length === 0 ? (
        <p className="text-sm text-white/45">No events yet</p>
      ) : (
        <div className="space-y-3">
          {recent.map((event) => (
            <div key={event.id} className="flex gap-3 pb-3 border-b border-white/[0.06] last:border-0">
              <span className="text-lg">{getEventIcon(event.eventType)}</span>
              <div className="flex-1 min-w-0">
                <p className="font-medium text-white text-sm">{event.title}</p>
                <p className="text-xs text-white/35 mt-1">
                  {formatDateTime(event.occurredAtUtc)}
                </p>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
