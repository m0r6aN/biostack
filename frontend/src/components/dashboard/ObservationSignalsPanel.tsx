import { ProtocolConsoleObservationSignal } from '@/lib/types';

interface ObservationSignalsPanelProps {
  signals: ProtocolConsoleObservationSignal[];
}

export function ObservationSignalsPanel({ signals }: ObservationSignalsPanelProps) {
  const grouped = signals.reduce<Record<string, ProtocolConsoleObservationSignal[]>>((groups, signal) => {
    const key = signal.type;
    groups[key] = [...(groups[key] ?? []), signal];
    return groups;
  }, {});

  return (
    <section className="rounded-lg border border-white/[0.08] bg-[#101820]/95 p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Observation Signals</p>
          <h2 className="mt-2 text-lg font-bold text-white">{signals.length} current signal{signals.length === 1 ? '' : 's'}</h2>
        </div>
        <span className="rounded-lg border border-white/[0.08] px-3 py-1.5 text-xs font-semibold uppercase tracking-[0.12em] text-white/45">
          timeline anchored
        </span>
      </div>

      {signals.length === 0 ? (
        <p className="mt-4 text-sm text-white/50">No observations recorded for the current sequence.</p>
      ) : (
        <div className="mt-4 space-y-3">
          {Object.entries(grouped).map(([type, items]) => (
            <div key={type} className="rounded-lg border border-white/[0.06] bg-white/[0.025] p-3">
              <p className="text-sm font-semibold text-white">{signalLabel(type, items[0]?.metric ?? null)}</p>
              <div className="mt-2 space-y-2">
                {items.map((signal, index) => (
                  <div key={`${signal.type}-${signal.metric ?? 'none'}-${index}`} className="flex items-start gap-2">
                    <span className={`mt-1 h-2 w-2 rounded-full ${severityDot(signal.severity)}`} />
                    <div>
                      <p className="text-xs leading-5 text-white/55">{signal.detail}</p>
                      <p className="mt-0.5 text-[11px] uppercase tracking-[0.12em] text-white/30">{signal.severity}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

function signalLabel(type: string, metric: string | null) {
  if (type === 'gap') return 'Observation gap';
  if (type === 'trend_shift') return `${metric ?? 'Metric'} trend shift`;
  return type.replaceAll('_', ' ');
}

function severityDot(severity: string) {
  if (severity === 'high') return 'bg-red-300';
  if (severity === 'medium') return 'bg-amber-300';
  return 'bg-sky-300';
}
