'use client';

import Link from 'next/link';
import { TIMELINE_TAG_TOKENS, type TimelineEventTag } from '@/styles/tokens';
import { cn } from '@/lib/utils';
import type {
  ProtocolReviewTimelineEvent,
  ProtocolSequenceExpectationSnapshot,
  ProtocolDriftSnapshot,
} from '@/lib/types';

interface CohesionTimelinePanelProps {
  events: ProtocolReviewTimelineEvent[];
  sequence?: ProtocolSequenceExpectationSnapshot | null;
  drift?: ProtocolDriftSnapshot | null;
}

// Derive an alignment tag from event data and context snapshots
function deriveTag(
  event: ProtocolReviewTimelineEvent,
  sequence: ProtocolSequenceExpectationSnapshot | null | undefined,
  drift: ProtocolDriftSnapshot | null | undefined,
): TimelineEventTag | null {
  const detail = event.detail.toLowerCase();
  const status = sequence?.currentStatus?.state;
  const driftState = drift?.driftState;

  if (detail.includes('regime shift') || driftState === 'regime_shift') return 'regime-shift';
  if (detail.includes('sequence break') || detail.includes('sequence diverging')) return 'diverging';
  if (detail.includes('outside typical timing') || detail.includes('later than usual') || status === 'late') return 'late';
  if (detail.includes('matches prior pattern') || detail.includes('within common sequence window') || status === 'aligned') return 'aligned';
  if (detail.includes('usual next event') || status === 'pending') return 'expected-pending';

  return null;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' }).format(new Date(value));
}

const EVENT_DOT: Record<string, string> = {
  check_in: 'bg-sky-400',
  computation: 'bg-amber-400',
  review_completed: 'bg-lime-400',
  evolution: 'bg-fuchsia-400',
  version_created: 'bg-fuchsia-400',
};

const EVENT_BAND: Record<string, string> = {
  check_in: 'border-sky-400/20',
  computation: 'border-amber-400/20',
  review_completed: 'border-lime-400/20',
  evolution: 'border-fuchsia-400/20',
  version_created: 'border-fuchsia-400/20',
};

const EVENT_ICON: Record<string, string> = {
  check_in: 'OBS',
  computation: 'MATH',
  review_completed: 'REV',
  evolution: 'EVO',
  version_created: 'VER',
};

function getEventStyle(eventType: string) {
  const key = eventType.startsWith('run_') ? 'run' : eventType;
  return {
    dot: EVENT_DOT[key] ?? (eventType.startsWith('run_') ? 'bg-emerald-400' : 'bg-white/40'),
    band: EVENT_BAND[key] ?? (eventType.startsWith('run_') ? 'border-emerald-400/20' : 'border-white/8'),
    icon: EVENT_ICON[key] ?? (eventType.startsWith('run_') ? 'RUN' : 'EVT'),
  };
}

export function CohesionTimelinePanel({ events, sequence, drift }: CohesionTimelinePanelProps) {
  const hasData = events.length > 0;
  const hasSnapshots = !!(sequence || drift);

  if (!hasData) {
    return (
      <section className="rounded-3xl border border-white/8 bg-[#121923]/90 p-5">
        <p className="text-[10px] font-bold text-white/20 uppercase tracking-widest" title="How consistently your protocol holds together over time">Cohesion Timeline</p>
        <div className="mt-4 rounded-2xl border border-white/5 bg-white/[0.02] p-4">
          <p className="text-sm text-white/50 leading-relaxed">
            Protocol events from runs, check-ins, and computations will appear here as data is collected. Alignment annotations require at least one completed run.
          </p>
          {!hasSnapshots && (
            <p className="mt-2 text-xs text-white/30">Start a protocol run to begin collecting timeline data.</p>
          )}
        </div>
      </section>
    );
  }

  return (
    <section className="rounded-3xl border border-white/8 bg-[#121923]/90 p-5">
      <div className="flex items-center justify-between gap-3 mb-5">
        <div>
          <p className="text-[10px] font-bold text-white/20 uppercase tracking-widest mb-1" title="How consistently your protocol holds together over time">Cohesion Timeline</p>
          <h2 className="text-base font-bold text-white/90">
            Recent sequence
            {sequence?.currentStatus && (
              <span className="ml-2 text-sm font-normal text-white/40">
                Current sequence status: {sequence.currentStatus.state}
              </span>
            )}
          </h2>
        </div>
        <Link href="/timeline" className="text-xs font-semibold text-emerald-400/70 hover:text-emerald-300 transition-colors flex items-center gap-1">
          Full timeline
          <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
            <path strokeLinecap="round" strokeLinejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
          </svg>
        </Link>
      </div>

      <div className="space-y-2">
        {events.map((event, index) => {
          const { dot, band, icon } = getEventStyle(event.eventType);
          const tag = deriveTag(event, sequence, drift);
          const tagToken = tag ? TIMELINE_TAG_TOKENS[tag] : null;

          return (
            <div key={`${event.eventType}-${event.occurredAtUtc}-${index}`} className="grid grid-cols-[72px_1fr] gap-3">
              <time className="pt-1 text-[11px] text-white/30 font-mono">{formatDate(event.occurredAtUtc)}</time>

              <div className={cn('relative border-l pl-4', band)}>
                <span className={cn('absolute -left-[5px] top-1.5 h-2.5 w-2.5 rounded-full', dot)} />

                <div className="group rounded-2xl px-3 py-2 transition-colors hover:bg-white/[0.03]">
                  <div className="flex flex-wrap items-center gap-2 mb-1">
                    <span className="text-[9px] font-bold text-white/20 tracking-widest">{icon}</span>
                    <p className="text-sm font-semibold text-white/85">{event.label}</p>
                    {tagToken && (
                      <span
                        className={cn(
                          'text-[10px] font-semibold px-2 py-0.5 rounded-full border',
                          tagToken.bg, tagToken.color, tagToken.border,
                        )}
                        title={`Alignment tag: ${tag}`}
                      >
                        {tagToken.label}
                      </span>
                    )}
                  </div>
                  <p className="text-xs text-white/40 leading-relaxed">{event.detail}</p>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {!hasSnapshots && hasData && (
        <p className="mt-4 text-[10px] text-white/20 italic">
          Alignment annotations will appear once historical runs provide a baseline for comparison.
        </p>
      )}
    </section>
  );
}
