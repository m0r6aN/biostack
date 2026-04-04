import { TimelineEvent } from '@/lib/types';
import { formatDateTime, getEventIcon } from '@/lib/utils';

interface TimelineEventCardProps {
  event: TimelineEvent;
}

export function TimelineEventCard({ event }: TimelineEventCardProps) {
  return (
    <div className="flex gap-4 pb-6">
      <div className="flex flex-col items-center">
        <div className="text-2xl">{getEventIcon(event.eventType)}</div>
        <div className="w-1 h-12 bg-white/[0.08] mt-2"></div>
      </div>
      <div className="flex-1 pt-1">
        <h4 className="font-semibold text-white">{event.title}</h4>
        <p className="text-sm text-white/65 mt-1">{event.description}</p>
        <p className="text-xs text-white/35 mt-2">{formatDateTime(event.occurredAtUtc)}</p>
      </div>
    </div>
  );
}
