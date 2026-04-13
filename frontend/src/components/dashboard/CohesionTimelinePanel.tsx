'use client';

import Link from 'next/link';
import { ProtocolReviewTimelineEvent, ProtocolSequenceExpectationSnapshot } from '@/lib/types';

interface CohesionTimelinePanelProps {
  events: ProtocolReviewTimelineEvent[];
  sequence?: ProtocolSequenceExpectationSnapshot | null;
}

export function CohesionTimelinePanel({ events, sequence }: CohesionTimelinePanelProps) {
  if (events.length === 0) {
    return (
      <section className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Cohesion Timeline</p>
        <p className="mt-3 text-sm text-white/45">Runs, check-ins, calculator math, reviews, evolution events, and sequence annotations will appear here.</p>
      </section>
    );
  }

  return (
    <section className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
      <div className="flex items-center justify-between gap-3">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Cohesion Timeline</p>
          <h2 className="mt-2 text-lg font-bold text-white">Recent sequence</h2>
          {sequence?.currentStatus && (
            <p className="mt-1 text-sm text-white/45">Current sequence status: {sequence.currentStatus.state}</p>
          )}
        </div>
        <Link href="/timeline" className="text-sm font-semibold text-emerald-200/80 hover:text-emerald-100">
          Full timeline
        </Link>
      </div>
      <div className="mt-5 space-y-3">
        {events.map((event, index) => (
          <div key={`${event.eventType}-${event.occurredAtUtc}-${index}`} className="grid grid-cols-[88px_1fr] gap-3">
            <time className="pt-0.5 text-xs text-white/35">{formatDate(event.occurredAtUtc)}</time>
            <div className={`relative border-l pl-4 ${eventBandClass(event.eventType)}`}>
              <span className={`absolute -left-1.5 top-1 h-3 w-3 rounded-full ${eventDotClass(event.eventType)}`} />
              <div className="group rounded-lg px-2 py-1 transition-colors hover:bg-white/[0.035]">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="text-sm">{eventIcon(event.eventType)}</span>
                  <p className="text-sm font-semibold text-white">{event.label}</p>
                  {timelineBadges(event.detail).map((badge) => (
                    <span key={badge} className="rounded-lg border border-white/[0.08] px-2 py-0.5 text-[11px] text-white/40">
                      {badge}
                    </span>
                  ))}
                </div>
              <p className="mt-1 text-xs leading-5 text-white/50">{event.detail}</p>
              </div>
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
  if (eventType === 'computation') return 'bg-amber-300';
  if (eventType === 'review_completed') return 'bg-lime-300';
  if (eventType === 'evolution' || eventType === 'version_created') return 'bg-fuchsia-300';
  if (eventType.startsWith('run_')) return 'bg-emerald-300';
  return 'bg-white/45';
}

function eventBandClass(eventType: string) {
  if (eventType === 'check_in') return 'border-sky-300/20';
  if (eventType === 'computation') return 'border-amber-300/20';
  if (eventType === 'review_completed') return 'border-lime-300/20';
  if (eventType === 'evolution' || eventType === 'version_created') return 'border-fuchsia-300/20';
  if (eventType.startsWith('run_')) return 'border-emerald-300/20';
  return 'border-white/[0.08]';
}

function eventIcon(eventType: string) {
  if (eventType === 'check_in') return 'CI';
  if (eventType === 'computation') return 'MATH';
  if (eventType === 'review_completed') return 'REV';
  if (eventType === 'evolution' || eventType === 'version_created') return 'EVO';
  if (eventType.startsWith('run_')) return 'RUN';
  return 'EVT';
}

function timelineBadges(detail: string) {
  const badges: string[] = [];
  if (detail.includes('matches prior pattern') || detail.includes('occurred within common sequence window')) {
    badges.push('matches pattern');
  }
  if (detail.includes('outside common sequence window') || detail.includes('outside typical timing') || detail.includes('later than usual')) {
    badges.push('outside typical timing');
  }
  if (detail.includes('sequence diverging') || detail.includes('sequence break')) {
    badges.push('sequence break');
  }
  if (detail.includes('usual next event')) {
    badges.push('usual next event');
  }
  return badges;
}
