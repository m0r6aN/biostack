'use client';

import Link from 'next/link';
import { ProtocolReviewTimelineEvent } from '@/lib/types';

interface CohesionTimelinePanelProps {
  events: ProtocolReviewTimelineEvent[];
}

export function CohesionTimelinePanel({ events }: CohesionTimelinePanelProps) {
  if (events.length === 0) {
    return (
      <section className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Closed Loop Timeline</p>
        <p className="mt-3 text-sm text-white/45">Version changes, run boundaries, and attached check-ins will appear here.</p>
      </section>
    );
  }

  return (
    <section className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
      <div className="flex items-center justify-between gap-3">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Closed Loop Timeline</p>
          <h2 className="mt-2 text-lg font-bold text-white">Recent sequence</h2>
        </div>
        <Link href="/timeline" className="text-sm font-semibold text-emerald-200/80 hover:text-emerald-100">
          Full timeline
        </Link>
      </div>
      <div className="mt-5 space-y-3">
        {events.map((event, index) => (
          <div key={`${event.eventType}-${event.occurredAtUtc}-${index}`} className="grid grid-cols-[88px_1fr] gap-3">
            <time className="pt-0.5 text-xs text-white/35">{formatDate(event.occurredAtUtc)}</time>
            <div className="relative border-l border-white/[0.08] pl-4">
              <span className={`absolute -left-1.5 top-1 h-3 w-3 rounded-full ${eventDotClass(event.eventType)}`} />
              <p className="text-sm font-semibold text-white">{event.label}</p>
              <p className="mt-1 text-xs leading-5 text-white/50">{event.detail}</p>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' }).format(new Date(value));
}

function eventDotClass(eventType: string) {
  if (eventType === 'check_in') return 'bg-sky-300';
  if (eventType === 'evolution' || eventType === 'version_created') return 'bg-fuchsia-300';
  if (eventType.startsWith('run_')) return 'bg-emerald-300';
  return 'bg-white/45';
}
