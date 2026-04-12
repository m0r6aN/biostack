import { ProtocolDriftSnapshot, ProtocolPatternSnapshot, ProtocolReview, ProtocolReviewTimelineEvent } from '@/lib/types';

interface ProtocolIntelligenceReviewProps {
  review: ProtocolReview | null;
  patterns?: ProtocolPatternSnapshot | null;
  drift?: ProtocolDriftSnapshot | null;
}

const sectionStyles: Record<string, string> = {
  alignment: 'border-emerald-400/20 bg-emerald-500/[0.06] text-emerald-100',
  divergence: 'border-amber-400/20 bg-amber-500/[0.06] text-amber-100',
  neutral: 'border-sky-400/20 bg-sky-500/[0.06] text-sky-100',
  change: 'border-fuchsia-400/20 bg-fuchsia-500/[0.06] text-fuchsia-100',
  gap: 'border-white/[0.1] bg-white/[0.035] text-white/65',
};

export function ProtocolIntelligenceReview({ review, patterns, drift }: ProtocolIntelligenceReviewProps) {
  if (!review) {
    return (
      <section className="rounded-lg border border-white/[0.08] bg-[#101820]/95 p-5">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Protocol Intelligence Review</p>
        <h2 className="mt-2 text-2xl font-black text-white">Review pending</h2>
        <p className="mt-2 max-w-2xl text-sm text-white/50">Review available after this lineage has run history.</p>
      </section>
    );
  }

  const runCount = review.versions.reduce((total, version) => total + version.runs.length, 0);
  const checkInCount = review.versions.reduce(
    (total, version) => total + version.runs.reduce((runTotal, run) => runTotal + run.observations.length, 0),
    0
  );

  return (
    <section className="rounded-lg border border-white/[0.08] bg-[#101820]/95 p-5">
      <div className="grid gap-5 lg:grid-cols-[1fr_280px]">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Protocol Intelligence Review</p>
          <h2 className="mt-2 text-2xl font-black text-white">Lineage observations</h2>
          <p className="mt-2 max-w-2xl text-sm text-white/50">
            Rule-based synthesis across versions, runs, and attached check-ins. Observed patterns only.
          </p>
        </div>
        <div className="grid grid-cols-3 gap-2 text-center">
          <ReviewStat label="Versions" value={review.versions.length} />
          <ReviewStat label="Runs" value={runCount} />
          <ReviewStat label="Check-ins" value={checkInCount} />
        </div>
      </div>

      <div className="mt-6 grid gap-3 md:grid-cols-2">
        {review.sections.map((section) => (
          <article
            key={`${section.type}-${section.title}`}
            className={`rounded-lg border p-4 ${sectionStyles[section.type] ?? sectionStyles.gap}`}
          >
            <div className="flex items-start justify-between gap-3">
              <h3 className="font-semibold text-white">{section.title}</h3>
              <span className="rounded-lg border border-current/20 px-2 py-1 text-[11px] font-semibold uppercase tracking-[0.12em]">
                {section.type}
              </span>
            </div>
            <p className="mt-3 text-sm leading-6 text-white/70">{section.summary}</p>
            {section.evidence.length > 0 && (
              <div className="mt-4 space-y-2">
                {section.evidence.slice(0, 4).map((evidence) => (
                  <p key={evidence} className="border-l border-current/25 pl-3 text-xs leading-5 text-white/55">
                    {evidence}
                  </p>
                ))}
              </div>
            )}
          </article>
        ))}
      </div>

      <div className="mt-6 grid gap-5 lg:grid-cols-[320px_1fr]">
        <div>
          <h3 className="font-semibold text-white">Versions and runs</h3>
          <div className="mt-3 space-y-3">
            {review.versions.map((version) => (
              <div key={version.protocolId} className="rounded-lg border border-white/[0.06] bg-white/[0.025] p-4">
                <div className="flex items-center justify-between gap-3">
                  <p className="font-semibold text-white">v{version.version} {version.isDraft ? 'draft' : 'snapshot'}</p>
                  <span className="text-xs text-white/40">{version.runs.length} run{version.runs.length === 1 ? '' : 's'}</span>
                </div>
                {version.versionDiff && (
                  <p className="mt-2 text-xs text-white/45">
                    {version.versionDiff.changes.slice(0, 2).map((change) => change.subject).join(' | ')}
                  </p>
                )}
                {version.runs.length > 0 && (
                  <div className="mt-3 space-y-2">
                    {version.runs.map((run) => (
                      <div key={run.run.id} className="flex items-center justify-between gap-3 text-xs text-white/55">
                        <span>{formatDate(run.run.startedAtUtc)} - {run.run.status}</span>
                        <span>{run.observations.length} check-in{run.observations.length === 1 ? '' : 's'}</span>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>

        <div>
          <h3 className="font-semibold text-white">Longitudinal timeline</h3>
          <div className="mt-3 max-h-[520px] space-y-3 overflow-y-auto pr-2">
            {review.timeline.map((event, index) => {
              const detail = timelineDetail(event, patterns, drift);
              const tags = timelineTags(event, drift);
              return (
              <div key={`${event.eventType}-${event.occurredAtUtc}-${index}`} className="grid grid-cols-[92px_1fr] gap-3">
                <time className="pt-1 text-xs text-white/35">{formatDate(event.occurredAtUtc)}</time>
                <div className="relative border-l border-white/[0.08] pl-4">
                  <span className={`absolute -left-1.5 top-1.5 h-3 w-3 rounded-full ${eventDotClass(event.eventType)}`} />
                  <p className="text-sm font-semibold text-white">{event.label}</p>
                  <p className="mt-1 text-xs leading-5 text-white/50">{detail}</p>
                  {tags.length > 0 && (
                    <div className="mt-2 flex flex-wrap gap-1.5">
                      {tags.map((tag) => (
                        <span key={tag} className="rounded-lg border border-white/[0.08] px-2 py-1 text-[11px] text-white/40">
                          {tag}
                        </span>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            )})}
          </div>
        </div>
      </div>

      <div className="mt-6 flex flex-wrap gap-2 border-t border-white/[0.06] pt-4">
        {review.safetyNotes.map((note) => (
          <span key={note} className="rounded-lg border border-white/[0.08] px-3 py-1.5 text-xs text-white/45">
            {note}
          </span>
        ))}
      </div>
    </section>
  );
}

function timelineDetail(
  event: ProtocolReviewTimelineEvent,
  patterns?: ProtocolPatternSnapshot | null,
  drift?: ProtocolDriftSnapshot | null
) {
  const comparison = patterns?.currentRunComparison;
  const annotation = comparison && event.runId
    ? annotationForEvent(event.eventType, comparison.matchingSignals, comparison.divergentSignals)
    : null;
  const driftPhrase = driftDetailPhrase(event.eventType, drift);
  return [event.detail, annotation ? `Pattern memory: ${annotation}.` : null, driftPhrase].filter(Boolean).join(' ');
}

function annotationForEvent(eventType: string, matchingSignals: string[], divergentSignals: string[]) {
  if (eventType === 'check_in' && matchingSignals.some((signal) => signal.includes('Check-in timing aligns'))) {
    return 'matches prior pattern';
  }

  if ((eventType === 'check_in' || eventType === 'computation') && divergentSignals.some((signal) => signal.includes('later'))) {
    return 'later than usual';
  }

  if ((eventType === 'check_in' || eventType === 'computation') && divergentSignals.some((signal) => signal.includes('earlier'))) {
    return 'earlier than typical';
  }

  if (eventType === 'computation' && matchingSignals.some((signal) => signal.includes('Computation timing'))) {
    return 'matches prior pattern';
  }

  return null;
}

function driftDetailPhrase(eventType: string, drift?: ProtocolDriftSnapshot | null) {
  if (!drift || drift.signals.length === 0) {
    return null;
  }

  if ((eventType === 'check_in' || eventType === 'computation') && drift.signals.some((signal) => signal.description.includes('outside historical timing range'))) {
    return 'Drift tag: outside typical timing.';
  }

  if (drift.signals.some((signal) => signal.type === 'sequence_break')) {
    return 'Drift tag: sequence break.';
  }

  if (drift.driftState === 'mild' || drift.driftState === 'moderate') {
    return 'Drift tag: drift accumulating.';
  }

  return null;
}

function timelineTags(event: ProtocolReviewTimelineEvent, drift?: ProtocolDriftSnapshot | null) {
  if (!drift || !event.runId) {
    return [];
  }

  const tags: string[] = [];
  if ((event.eventType === 'check_in' || event.eventType === 'computation') && drift.signals.some((signal) => signal.type.endsWith('_timing'))) {
    tags.push('outside typical timing');
  }

  if (drift.signals.some((signal) => signal.type === 'sequence_break')) {
    tags.push('sequence break');
  }

  if ((drift.driftState === 'mild' || drift.driftState === 'moderate') && tags.length < 2) {
    tags.push('drift accumulating');
  }

  return tags;
}

function ReviewStat({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border border-white/[0.06] bg-white/[0.025] p-3">
      <p className="text-lg font-black text-white">{value}</p>
      <p className="mt-1 text-[11px] uppercase tracking-[0.12em] text-white/35">{label}</p>
    </div>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' }).format(new Date(value));
}

function eventDotClass(eventType: string) {
  if (eventType === 'check_in') return 'bg-sky-300';
  if (eventType === 'evolution') return 'bg-fuchsia-300';
  if (eventType.startsWith('run_')) return 'bg-emerald-300';
  return 'bg-white/45';
}
