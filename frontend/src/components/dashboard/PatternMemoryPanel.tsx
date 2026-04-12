import { ProtocolPatternSnapshot } from '@/lib/types';

interface PatternMemoryPanelProps {
  snapshot: ProtocolPatternSnapshot | null;
  compact?: boolean;
}

export function PatternMemoryPanel({ snapshot, compact = false }: PatternMemoryPanelProps) {
  if (!snapshot) {
    return (
      <section className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Pattern Memory</p>
        <p className="mt-3 text-sm text-white/45">Historical run recall appears after protocol runs are recorded.</p>
      </section>
    );
  }

  const keyPatterns = [
    ...snapshot.metricPatterns.map((pattern) => pattern.observation),
    ...snapshot.eventPatterns.map((pattern) => pattern.timingPattern),
  ].slice(0, compact ? 2 : 4);
  const comparison = snapshot.currentRunComparison;

  return (
    <section className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Pattern Memory</p>
          <h2 className="mt-2 text-lg font-bold text-white">
            Built from {snapshot.historicalRunCount} completed run{snapshot.historicalRunCount === 1 ? '' : 's'}
          </h2>
        </div>
        <span className="rounded-lg border border-white/[0.1] px-3 py-1.5 text-xs font-semibold uppercase tracking-[0.12em] text-white/55">
          {snapshot.patternConfidence}
        </span>
      </div>

      {keyPatterns.length > 0 ? (
        <div className="mt-4 space-y-2">
          {keyPatterns.map((pattern) => (
            <p key={pattern} className="border-l border-emerald-300/25 pl-3 text-sm leading-6 text-white/60">
              {pattern}
            </p>
          ))}
        </div>
      ) : (
        <p className="mt-4 text-sm text-white/45">
          {snapshot.historicalRunCount < 2 ? 'Insufficient completed run history for recurring patterns.' : 'Sparse completed run data keeps pattern recall limited.'}
        </p>
      )}

      {!compact && snapshot.sequencePatterns.length > 0 && (
        <div className="mt-4 flex flex-wrap gap-2">
          {snapshot.sequencePatterns.slice(0, 3).map((pattern) => (
            <span key={`${pattern.sequence.join('-')}-${pattern.description}`} className="rounded-lg border border-white/[0.08] px-3 py-1.5 text-xs text-white/50">
              {pattern.sequence.join(' -> ')} · {pattern.description}
            </span>
          ))}
        </div>
      )}

      {comparison && (comparison.matchingSignals.length > 0 || comparison.divergentSignals.length > 0) && (
        <div className="mt-4 grid gap-2 md:grid-cols-2">
          {comparison.matchingSignals.slice(0, 3).map((signal) => (
            <p key={signal} className="rounded-lg border border-emerald-400/15 bg-emerald-500/[0.06] px-3 py-2 text-sm text-emerald-100/75">
              {signal}
            </p>
          ))}
          {comparison.divergentSignals.slice(0, 3).map((signal) => (
            <p key={signal} className="rounded-lg border border-amber-400/15 bg-amber-500/[0.06] px-3 py-2 text-sm text-amber-100/75">
              {signal}
            </p>
          ))}
        </div>
      )}
    </section>
  );
}
