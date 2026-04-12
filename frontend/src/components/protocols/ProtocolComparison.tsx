import { ProtocolActualComparison } from '@/lib/types';

interface ProtocolComparisonProps {
  comparison: ProtocolActualComparison | null;
}

export function ProtocolComparison({ comparison }: ProtocolComparisonProps) {
  if (!comparison) {
    return (
      <div className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Observed vs Projected</p>
        <h3 className="mt-2 text-xl font-bold text-white">Comparison pending</h3>
        <p className="mt-2 text-sm text-white/45">Comparison becomes available after this protocol has a run state.</p>
      </div>
    );
  }

  const summary = comparison.runSummary;
  const maxDay = Math.max(14, ...comparison.observations.map((observation) => observation.day));
  const metricColor: Record<string, string> = {
    Energy: 'text-emerald-300',
    Sleep: 'text-sky-300',
    Appetite: 'text-amber-300',
    Recovery: 'text-rose-300',
  };

  return (
    <div className="p-5 rounded-lg border border-white/[0.08] bg-[#121923]/90">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Observed vs Projected</p>
      <h3 className="mt-2 text-xl font-bold text-white">Run intelligence</h3>
      <p className="mt-2 text-sm text-white/45">Observed correlation only. BioStack does not assign causation.</p>

      {comparison.run && comparison.observations.length === 0 && (
        <div className="mt-5 rounded-lg border border-sky-400/15 bg-sky-500/[0.06] px-4 py-3 text-sm text-sky-100/75">
          No check-ins attached to this run yet.
        </div>
      )}

      {summary && (
        <div className="mt-5 grid gap-4 rounded-lg border border-emerald-400/15 bg-emerald-500/[0.04] p-4 lg:grid-cols-[220px_1fr_160px]">
          <div>
            <p className="text-xs uppercase tracking-[0.14em] text-emerald-200/55">Active Run</p>
            <p className="mt-2 text-lg font-bold text-white">Started {summary.daysActive} day{summary.daysActive === 1 ? '' : 's'} ago</p>
          </div>
          <div className="grid gap-2 sm:grid-cols-2">
            {summary.signals.map((signal) => (
              <div key={signal.metric} className="flex items-center justify-between gap-3 rounded-lg border border-white/[0.06] bg-white/[0.025] px-3 py-2">
                <span className="text-sm text-white/65">{signal.metric}</span>
                <span className="text-sm font-semibold text-white">{directionGlyph(signal.direction)} {signal.magnitude}</span>
              </div>
            ))}
          </div>
          <div className="rounded-lg border border-white/[0.06] bg-white/[0.025] p-3">
            <p className="text-xs uppercase tracking-[0.14em] text-white/35">Alignment</p>
            <p className="mt-2 text-sm font-semibold text-white">{summary.alignedCount} aligned</p>
            <p className="mt-1 text-sm font-semibold text-white">{summary.divergingCount} diverging</p>
          </div>
        </div>
      )}

      <div className="mt-5 rounded-lg border border-white/[0.06] bg-black/15 p-4">
        <div className="relative h-52 overflow-hidden rounded-lg border border-white/[0.06] bg-[#0d131b]">
          <div className="absolute inset-y-0 left-0 w-[21%] bg-emerald-500/[0.06]" />
          <div className="absolute inset-y-0 left-[21%] w-[29%] bg-sky-500/[0.06]" />
          <div className="absolute inset-y-0 left-[50%] w-[50%] bg-amber-500/[0.05]" />
          <div className="absolute inset-x-0 top-3 flex justify-between px-3 text-[11px] font-semibold text-white/35">
            <span>Days 1-3</span>
            <span>Days 4-7</span>
            <span>Days 7-14</span>
          </div>
          {comparison.run && (
            <div className="absolute bottom-0 top-0 left-0 border-l border-emerald-300/80">
              <span className="absolute left-2 top-9 whitespace-nowrap rounded-lg bg-emerald-400/10 px-2 py-1 text-[11px] font-semibold text-emerald-200">start</span>
            </div>
          )}
          <div className="absolute inset-x-4 bottom-8 top-14">
            {comparison.observations.map((observation) => {
              const left = `${Math.min(100, ((observation.day - 1) / Math.max(1, maxDay - 1)) * 100)}%`;
              return (
                <div key={observation.checkInId} className="absolute h-full -translate-x-1/2" style={{ left }}>
                  {[
                    ['Energy', observation.energy],
                    ['Sleep', observation.sleepQuality],
                    ['Appetite', observation.appetite],
                    ['Recovery', observation.recovery],
                  ].map(([metric, value]) => (
                    <span
                      key={metric}
                      title={`${metric}: ${value}/10 on day ${observation.day}`}
                      className={`absolute h-2.5 w-2.5 -translate-x-1/2 rounded-full border border-white/30 ${metricDotClass(metric as string)}`}
                      style={{ bottom: `${Number(value) * 9}%` }}
                    />
                  ))}
                </div>
              );
            })}
          </div>
          <div className="absolute bottom-3 left-4 right-4 flex items-center justify-between text-[11px] text-white/30">
            <span>0/10</span>
            <span>actual check-in markers over projected bands</span>
            <span>10/10</span>
          </div>
        </div>
        <div className="mt-3 flex flex-wrap gap-3 text-xs">
          {Object.entries(metricColor).map(([metric, color]) => (
            <span key={metric} className={color}>{metric}</span>
          ))}
        </div>
      </div>

      <div className="mt-5 grid gap-3 md:grid-cols-2">
        {comparison.actualTrends.map((trend) => (
          <div key={trend.metric} className="rounded-lg border border-white/[0.06] bg-white/[0.025] p-4">
            <div className="flex items-center justify-between gap-3">
              <span className="font-semibold text-white">{trend.metric}</span>
              <span className="rounded-lg border border-white/[0.08] px-2 py-1 text-xs text-white/55">{trend.direction}</span>
            </div>
            <p className="mt-3 text-sm text-white/45">
              Before {trend.beforeAverage ?? 'n/a'} · After {trend.afterAverage ?? 'n/a'}
            </p>
          </div>
        ))}
      </div>

      {comparison.insights.length > 0 && (
        <div className="mt-5 grid gap-3 md:grid-cols-2">
          {comparison.insights.map((insight) => (
            <div key={insight.message} className="rounded-lg border border-white/[0.06] bg-white/[0.025] p-4">
              <span className={`rounded-lg border px-2 py-1 text-xs font-semibold ${insightClass(insight.type)}`}>
                {insight.type}
              </span>
              <p className="mt-3 text-sm text-white/70">{insight.message}</p>
            </div>
          ))}
        </div>
      )}

      {comparison.highlights.length > 0 && (
        <div className="mt-5 space-y-2">
          {comparison.highlights.map((highlight) => (
            <p key={highlight} className="text-sm text-white/60">{highlight}</p>
          ))}
        </div>
      )}
    </div>
  );
}

function directionGlyph(direction: string) {
  if (direction === 'up') return '↑';
  if (direction === 'down') return '↓';
  if (direction === 'flat') return '→';
  return '·';
}

function metricDotClass(metric: string) {
  if (metric === 'Energy') return 'bg-emerald-300';
  if (metric === 'Sleep') return 'bg-sky-300';
  if (metric === 'Appetite') return 'bg-amber-300';
  return 'bg-rose-300';
}

function insightClass(type: string) {
  if (type === 'alignment') return 'border-emerald-400/25 bg-emerald-500/10 text-emerald-200';
  if (type === 'divergence') return 'border-amber-400/25 bg-amber-500/10 text-amber-200';
  return 'border-white/[0.08] bg-white/[0.03] text-white/55';
}
