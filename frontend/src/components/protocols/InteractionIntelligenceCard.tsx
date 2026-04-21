'use client';

import { InteractionIntelligence } from '@/lib/types';

interface InteractionIntelligenceCardProps {
  intelligence: InteractionIntelligence;
  title?: string;
}

const toneByType: Record<string, string> = {
  Synergistic: 'border-emerald-400/20 bg-emerald-500/10 text-emerald-100',
  Redundant: 'border-amber-400/20 bg-amber-500/10 text-amber-100',
  Interfering: 'border-rose-400/20 bg-rose-500/10 text-rose-100',
  Neutral: 'border-white/[0.08] bg-white/[0.04] text-white/70',
};

const swapReasonLabels: Record<string, string> = {
  reduces_redundancy: 'reduces redundancy',
  preserves_synergy: 'preserves synergy',
  lowers_interference: 'lowers interference',
  improves_goal_alignment: 'improves goal alignment',
  improves_signal_clarity: 'improves signal clarity',
  stronger_evidence: 'stronger evidence',
  lower_estimated_cost: 'lower estimated cost',
};

export function InteractionIntelligenceCard({
  intelligence,
  title = 'Interaction Intelligence',
}: InteractionIntelligenceCardProps) {
  const summary = intelligence.summary;
  const topFindings = intelligence.topFindings;
  const bestRemoval = intelligence.counterfactuals[0];
  const bestSwap = intelligence.swaps?.[0];

  return (
    <div className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">{title}</p>
          <h3 className="mt-2 text-lg font-bold text-white">What the stack is doing together</h3>
        </div>
        <div className="text-right text-xs text-white/45">
          <div>{intelligence.compositeScore.toFixed(1)} predicted score</div>
          <div>+{intelligence.score.synergyScore.toFixed(2)} synergy</div>
          <div>-{intelligence.score.redundancyPenalty.toFixed(2)} redundancy</div>
          <div>-{intelligence.score.interferencePenalty.toFixed(2)} interference</div>
        </div>
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-3">
        <div className="rounded-lg border border-emerald-400/15 bg-emerald-500/10 p-3">
          <p className="text-xs uppercase tracking-[0.16em] text-emerald-200/60">Synergies</p>
          <p className="mt-2 text-2xl font-bold text-emerald-100">{summary.synergies}</p>
        </div>
        <div className="rounded-lg border border-amber-400/15 bg-amber-500/10 p-3">
          <p className="text-xs uppercase tracking-[0.16em] text-amber-200/60">Redundancies</p>
          <p className="mt-2 text-2xl font-bold text-amber-100">{summary.redundancies}</p>
        </div>
        <div className="rounded-lg border border-rose-400/15 bg-rose-500/10 p-3">
          <p className="text-xs uppercase tracking-[0.16em] text-rose-200/60">Interferences</p>
          <p className="mt-2 text-2xl font-bold text-rose-100">{summary.interferences}</p>
        </div>
      </div>

      <div className="mt-4 space-y-3">
        {bestRemoval && (
          <div className="rounded-lg border border-sky-400/20 bg-sky-500/10 p-4">
            <p className="text-xs font-semibold uppercase tracking-[0.16em] text-sky-200/60">Counterfactual</p>
            <p className="mt-2 text-sm font-semibold text-white">
              Best remove-one scenario: {bestRemoval.removedCompound}
            </p>
            <p className="mt-2 text-sm leading-6 text-white/65">{bestRemoval.recommendation}</p>
          </div>
        )}

        {bestSwap && (
          <div className="rounded-lg border border-violet-400/20 bg-violet-500/10 p-4">
            <div className="flex items-start justify-between gap-3">
              <p className="text-xs font-semibold uppercase tracking-[0.16em] text-violet-200/60">Best swap</p>
              <span className="text-xs text-white/40">
                +{bestSwap.deltaScore.toFixed(1)} pts
              </span>
            </div>
            <p className="mt-2 text-sm font-semibold text-white">
              Replace {bestSwap.originalCompound} → {bestSwap.candidateCompound}
            </p>
            <p className="mt-2 text-sm leading-6 text-white/65">{bestSwap.recommendation}</p>
            {bestSwap.reasons.length > 0 && (
              <div className="mt-3 flex flex-wrap gap-1.5">
                {bestSwap.reasons.map((reason) => (
                  <span
                    key={reason}
                    className="rounded border border-violet-400/20 bg-violet-500/10 px-2 py-0.5 text-[11px] text-violet-200/80"
                  >
                    {swapReasonLabels[reason] ?? reason.replace(/_/g, ' ')}
                  </span>
                ))}
              </div>
            )}
          </div>
        )}

        {topFindings.length === 0 ? (
          <div className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4 text-sm text-white/50">
            No strong pairwise signals yet. The stack currently reads as low-interaction under the active rule set.
          </div>
        ) : (
          topFindings.map((finding) => (
            <div
              key={`${finding.type}-${finding.compounds.join('-')}`}
              className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4"
            >
              <div className="flex flex-wrap items-center gap-2">
                <span className={`rounded-lg border px-2 py-1 text-xs font-semibold ${toneByType[finding.type] ?? toneByType.Neutral}`}>
                  {finding.type.toLowerCase()}
                </span>
                <span className="text-sm font-semibold text-white">{finding.compounds.join(' + ')}</span>
                <span className="text-xs text-white/40">{Math.round(finding.confidence * 100)}% confidence</span>
              </div>
              <p className="mt-2 text-sm leading-6 text-white/60">{finding.message}</p>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
