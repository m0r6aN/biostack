import { SimulationResult } from '@/lib/types';

interface SimulationTimelineProps {
  simulation: SimulationResult;
}

export function SimulationTimeline({ simulation }: SimulationTimelineProps) {
  return (
    <div className="p-5 rounded-lg border border-white/[0.08] bg-[#121923]/90">
      <div className="mb-5">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Simulator v1</p>
        <h3 className="mt-2 text-xl font-bold text-white">Projected observation timeline</h3>
      </div>

      <div className="space-y-4">
        {simulation.timeline.map((entry) => (
          <div key={entry.dayRange} className="grid gap-3 rounded-lg border border-white/[0.06] bg-white/[0.025] p-4 md:grid-cols-[96px_1fr]">
            <div className="text-sm font-bold text-emerald-300">Day {entry.dayRange}</div>
            <div className="space-y-2">
              {entry.signals.map((signal) => (
                <p key={signal} className="text-sm text-white/70">{signal}</p>
              ))}
            </div>
          </div>
        ))}
      </div>

      {simulation.insights.length > 0 && (
        <div className="mt-5 space-y-2">
          <h4 className="text-sm font-semibold text-white">Key insights</h4>
          {simulation.insights.map((insight) => (
            <p key={insight} className="rounded-lg border border-white/[0.06] bg-white/[0.02] p-3 text-sm text-white/65">
              {insight}
            </p>
          ))}
        </div>
      )}
    </div>
  );
}
