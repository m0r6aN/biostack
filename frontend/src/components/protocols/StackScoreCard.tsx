import { StackScore } from '@/lib/types';

interface StackScoreCardProps {
  score: StackScore;
}

export function StackScoreCard({ score }: StackScoreCardProps) {
  return (
    <div className="p-5 rounded-lg border border-white/[0.08] bg-[#121923]/90">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Stack Score</p>
          <h3 className="mt-2 text-4xl font-black text-white">{score.score}</h3>
        </div>
        <div className="text-right text-xs text-white/45">0-100</div>
      </div>

      <div className="mt-4 flex flex-wrap gap-2">
        {score.chips.map((chip) => (
          <span
            key={chip}
            className="rounded-lg border border-emerald-400/20 bg-emerald-500/10 px-2.5 py-1 text-xs font-medium text-emerald-200"
          >
            {chip}
          </span>
        ))}
      </div>

      <div className="mt-5 grid grid-cols-2 gap-3 text-xs">
        <Breakdown label="Synergy" value={score.breakdown.synergy} />
        <Breakdown label="Redundancy" value={score.breakdown.redundancy} />
        <Breakdown label="Conflicts" value={score.breakdown.conflicts} />
        <Breakdown label="Evidence" value={score.breakdown.evidence} />
      </div>
    </div>
  );
}

function Breakdown({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border border-white/[0.06] bg-white/[0.025] p-3">
      <div className="flex items-center justify-between">
        <span className="text-white/45">{label}</span>
        <span className="font-semibold text-white">{value}</span>
      </div>
      <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-white/[0.06]">
        <div className="h-full rounded-full bg-emerald-400" style={{ width: `${Math.min(100, value)}%` }} />
      </div>
    </div>
  );
}
