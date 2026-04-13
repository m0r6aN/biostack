import { ProtocolSequenceExpectationSnapshot } from '@/lib/types';

interface SequenceExpectationPanelProps {
  snapshot: ProtocolSequenceExpectationSnapshot | null;
  compact?: boolean;
}

export function SequenceExpectationPanel({ snapshot, compact = false }: SequenceExpectationPanelProps) {
  if (!snapshot || snapshot.baselineSource === 'insufficient_history') {
    return (
      <section className="rounded-lg border border-white/[0.08] bg-[#111821]/95 p-5">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Sequence Expectation</p>
        <h2 className="mt-2 text-lg font-bold text-white">Not enough sequence history yet</h2>
        <p className="mt-2 text-sm text-white/50">Sequence patterns will appear after multiple completed runs.</p>
      </section>
    );
  }

  const event = snapshot.expectedNextEvent;
  const status = snapshot.currentStatus?.state ?? 'unknown';

  return (
    <section className="rounded-lg border border-cyan-300/15 bg-[#111821]/95 p-5 shadow-[0_0_28px_rgba(34,211,238,0.04)]">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-cyan-100/45">Sequence Expectation</p>
          <h2 className="mt-2 text-lg font-bold text-white">Built from {snapshot.historicalRunCount} completed runs</h2>
        </div>
        <span className={`rounded-lg border px-3 py-1.5 text-xs font-semibold uppercase tracking-[0.12em] ${statusClass(status)}`}>
          {formatValue(status)}
        </span>
      </div>

      {event ? (
        <div className="mt-4 space-y-3">
          <div className="rounded-lg border border-cyan-300/15 bg-cyan-400/[0.06] p-4">
            <p className="text-[11px] font-semibold uppercase tracking-[0.14em] text-cyan-100/45">Next commonly observed event</p>
            <p className="mt-2 text-xl font-black text-white">{formatEvent(event.eventType)}</p>
            <p className="mt-2 text-sm leading-6 text-white/60">{event.timingWindow}</p>
          </div>
          {snapshot.currentStatus?.notes.slice(0, 2).map((note) => (
            <p key={note} className="border-l border-cyan-200/20 pl-3 text-sm leading-6 text-white/55">
              {note}
            </p>
          ))}
        </div>
      ) : (
        <p className="mt-4 text-sm text-white/50">Current run has not yet entered a usual next state.</p>
      )}

      {!compact && snapshot.commonTransitions.length > 0 && (
        <div className="mt-4 grid gap-2">
          {snapshot.commonTransitions.slice(0, 3).map((transition) => (
            <div key={`${transition.fromState}-${transition.toEventType}`} className="rounded-lg border border-white/[0.06] bg-white/[0.025] p-3">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <p className="text-sm font-semibold text-white">
                  {formatEvent(transition.fromState)} {'->'} {formatEvent(transition.toEventType)}
                </p>
                <span className="text-xs text-white/35">{transition.observedCount} observed</span>
              </div>
              <p className="mt-1 text-xs leading-5 text-white/45">{transition.timingPattern}</p>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

function formatEvent(value: string) {
  return value
    .replace('RunStarted', 'run start')
    .replace('FirstCheckIn', 'first check-in')
    .replace('ComputationRecorded', 'computation recorded')
    .replace('RunClosed', 'run close')
    .replace('ReviewCompleted', 'review completion')
    .replace('EvolutionEvent', 'evolution event');
}

function formatValue(value: string) {
  return value.replaceAll('_', ' ');
}

function statusClass(status: string) {
  if (status === 'aligned') return 'border-emerald-300/25 bg-emerald-500/10 text-emerald-100';
  if (status === 'late' || status === 'diverging') return 'border-amber-300/25 bg-amber-500/10 text-amber-100';
  if (status === 'pending') return 'border-cyan-300/25 bg-cyan-500/10 text-cyan-100';
  return 'border-white/[0.1] bg-white/[0.035] text-white/55';
}
