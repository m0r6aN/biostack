import { ProtocolActualComparison } from '@/lib/types';

interface ProtocolComparisonProps {
  comparison: ProtocolActualComparison | null;
}

export function ProtocolComparison({ comparison }: ProtocolComparisonProps) {
  if (!comparison) {
    return null;
  }

  return (
    <div className="p-5 rounded-lg border border-white/[0.08] bg-[#121923]/90">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Protocol vs Actual</p>
      <h3 className="mt-2 text-xl font-bold text-white">Observed trends</h3>
      <p className="mt-2 text-sm text-white/45">Trend comparison only. BioStack does not assign causation.</p>

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
