import { ProtocolDriftSnapshot, ProtocolPatternSnapshot } from '@/lib/types';

interface DriftRegimePanelProps {
  drift: ProtocolDriftSnapshot | null;
  patterns?: ProtocolPatternSnapshot | null;
  compact?: boolean;
}

export function DriftRegimePanel({ drift, patterns, compact = false }: DriftRegimePanelProps) {
  if (!drift) {
    return (
      <section className="rounded-lg border border-white/[0.08] bg-[#101820]/95 p-5">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Drift & Regime</p>
        <h2 className="mt-2 text-xl font-black text-white">No drift snapshot</h2>
        <p className="mt-2 text-sm text-white/50">Drift classification appears after protocol run context is available.</p>
      </section>
    );
  }

  const regime = drift.regimeClassification?.state ?? 'stable';
  const signals = drift.signals.slice(0, 3);

  return (
    <section className="rounded-lg border border-white/[0.08] bg-[#101820]/95 p-5">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Drift & Regime</p>
          <h2 className="mt-2 text-xl font-black text-white">Current regime: {formatValue(regime)}</h2>
          <p className="mt-2 text-sm text-white/50">{baselineText(drift, patterns)}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <StatePill label="Drift" value={formatDriftState(drift.driftState)} />
          <StatePill label="Regime" value={formatValue(regime)} />
        </div>
      </div>

      {signals.length > 0 ? (
        <div className={`mt-5 grid gap-3 ${compact ? '' : 'md:grid-cols-3'}`}>
          {signals.map((signal) => (
            <div key={`${signal.type}-${signal.description}`} className="rounded-lg border border-white/[0.07] bg-white/[0.025] p-3">
              <div className="flex items-center justify-between gap-2">
                <p className="text-sm font-semibold text-white">{formatSignalType(signal.type)}</p>
                <span className="rounded-lg border border-white/[0.08] px-2 py-1 text-[11px] uppercase tracking-[0.12em] text-white/45">
                  {signal.severity}
                </span>
              </div>
              <p className="mt-2 text-xs leading-5 text-white/55">{signal.description}</p>
            </div>
          ))}
        </div>
      ) : (
        <p className="mt-5 rounded-lg border border-white/[0.06] bg-white/[0.025] px-3 py-2 text-sm text-white/50">
          No drift signals detected.
        </p>
      )}

      {!compact && drift.regimeClassification?.contributingFactors.length ? (
        <div className="mt-4 flex flex-wrap gap-2">
          {drift.regimeClassification.contributingFactors.map((factor) => (
            <span key={factor} className="rounded-lg border border-white/[0.08] px-3 py-1.5 text-xs text-white/45">
              {formatSignalType(factor)}
            </span>
          ))}
        </div>
      ) : null}
    </section>
  );
}

function StatePill({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-white/[0.08] bg-white/[0.025] px-3 py-2">
      <p className="text-[11px] uppercase tracking-[0.12em] text-white/35">{label}</p>
      <p className="mt-1 text-sm font-semibold text-white">{value}</p>
    </div>
  );
}

function baselineText(drift: ProtocolDriftSnapshot, patterns?: ProtocolPatternSnapshot | null) {
  if (drift.baselineSource === 'historical_runs') {
    const count = patterns?.historicalRunCount ?? 0;
    return `Baseline built from ${count} prior run${count === 1 ? '' : 's'}`;
  }

  return 'Insufficient historical run baseline';
}

function formatDriftState(value: string) {
  return value === 'regime_shift' ? 'shifted' : formatValue(value);
}

function formatSignalType(value: string) {
  return value.replaceAll('_', ' ');
}

function formatValue(value: string) {
  return value.replaceAll('_', ' ');
}
