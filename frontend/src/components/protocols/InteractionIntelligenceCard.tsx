'use client';

import { InteractionIntelligence, ReducedInteractionIntelligence, isReducedInteractionIntelligence } from '@/lib/types';
import { HelpTip } from '@/components/ui/HelpTip';
import Link from 'next/link';

interface InteractionIntelligenceCardProps {
  intelligence: InteractionIntelligence | ReducedInteractionIntelligence;
  title?: string;
  showTrackingCta?: boolean;
  onTrackingRequest?: () => void;
}

const toneByType: Record<string, string> = {
  Synergistic: 'border-emerald-400/20 bg-emerald-500/10 text-emerald-100',
  Complementary: 'border-teal-400/20 bg-teal-500/10 text-teal-100',
  Redundant: 'border-amber-400/20 bg-amber-500/10 text-amber-100',
  Interfering: 'border-rose-400/20 bg-rose-500/10 text-rose-100',
  Neutral: 'border-white/[0.08] bg-white/[0.04] text-white/70',
};

const swapReasonLabels: Record<string, string> = {
  reduces_redundancy: 'reduces redundancy',
  preserves_synergy: 'preserves synergy',
  lowers_interference: 'lowers interference',
  improves_goal_alignment: 'closer goal alignment',
  improves_signal_clarity: 'clearer signal',
  stronger_evidence: 'stronger evidence',
  lower_estimated_cost: 'lower estimated cost',
};

export function InteractionIntelligenceCard({
  intelligence,
  title = 'Interaction Intelligence',
  showTrackingCta = false,
  onTrackingRequest,
}: InteractionIntelligenceCardProps) {
  if (isReducedInteractionIntelligence(intelligence)) {
    return (
      <ReducedInteractionIntelligenceView
        intelligence={intelligence}
        title={title}
        showTrackingCta={showTrackingCta}
        onTrackingRequest={onTrackingRequest}
      />
    );
  }

  const summary = intelligence.summary;
  const topFindings = intelligence.topFindings;
  const bestRemoval = intelligence.counterfactuals[0];
  const bestSwap = intelligence.swaps?.[0];

  // "Why this score" groups the same synergy/redundancy/interference contributions the score
  // already computes, by pair, so an Operator viewer can see which flagged pairs pushed the
  // score in which direction without leaving this card.
  const scoreGroups: Array<{ key: string; label: string; tone: string }> = [
    { key: 'Synergistic', label: 'Synergy contributions', tone: toneByType.Synergistic },
    { key: 'Redundant', label: 'Redundancy contributions', tone: toneByType.Redundant },
    { key: 'Interfering', label: 'Interference contributions', tone: toneByType.Interfering },
  ];
  const groupedInteractions = scoreGroups
    .map((group) => ({
      ...group,
      items: (intelligence.interactions ?? []).filter((interaction) => interaction.type === group.key),
    }))
    .filter((group) => group.items.length > 0);

  return (
    <div className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">{title}</p>
          <h3 className="mt-2 text-lg font-bold text-white">What the stack is doing together</h3>
        </div>
        <div className="min-w-0 text-right text-xs text-white/45">
          <div>{intelligence.compositeScore.toFixed(1)} predicted score</div>
          <div>+{intelligence.score.synergyScore.toFixed(2)} synergy</div>
          <div>-{intelligence.score.redundancyPenalty.toFixed(2)} redundancy</div>
          <div>-{intelligence.score.interferencePenalty.toFixed(2)} interference</div>
        </div>
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-3">
        <div className="rounded-lg border border-emerald-400/15 bg-emerald-500/10 p-3">
          <p className="text-xs uppercase tracking-[0.16em] text-emerald-200/60"><HelpTip tipKey="synergy">Synergies</HelpTip></p>
          <p className="mt-2 text-2xl font-bold text-emerald-100">{summary.synergies}</p>
        </div>
        <div className="rounded-lg border border-amber-400/15 bg-amber-500/10 p-3">
          <p className="text-xs uppercase tracking-[0.16em] text-amber-200/60"><HelpTip tipKey="redundancy">Redundancies</HelpTip></p>
          <p className="mt-2 text-2xl font-bold text-amber-100">{summary.redundancies}</p>
        </div>
        <div className="rounded-lg border border-rose-400/15 bg-rose-500/10 p-3">
          <p className="text-xs uppercase tracking-[0.16em] text-rose-200/60"><HelpTip tipKey="interference">Interferences</HelpTip></p>
          <p className="mt-2 text-2xl font-bold text-rose-100">{summary.interferences}</p>
        </div>
      </div>

      {groupedInteractions.length > 0 && (
        <div className="mt-4 rounded-lg border border-white/[0.08] bg-white/[0.02] p-4">
          <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/40">Why this score</p>
          <div className="mt-3 space-y-3">
            {groupedInteractions.map((group) => (
              <div key={group.key}>
                <p className={`inline-block rounded-lg border px-2 py-1 text-xs font-semibold ${group.tone}`}>
                  {group.label}
                </p>
                <ul className="mt-2 space-y-1">
                  {group.items.map((item) => (
                    <li key={`${item.compoundA}-${item.compoundB}`} className="text-sm text-white/60">
                      {item.compoundA} + {item.compoundB}
                      <span className="ml-2 text-xs text-white/40">{Math.round(item.confidence * 100)}% confidence</span>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="mt-4 space-y-3">
        {bestRemoval && (
          <div className="rounded-lg border border-sky-400/20 bg-sky-500/10 p-4">
            <p className="text-xs font-semibold uppercase tracking-[0.16em] text-sky-200/60"><HelpTip tipKey="counterfactual">Counterfactual</HelpTip></p>
            <p className="mt-2 text-sm font-semibold text-white">
              Remove-one scenario: {bestRemoval.removedCompound}
            </p>
            <p className="mt-2 text-sm leading-6 text-white/65">{bestRemoval.recommendation}</p>
          </div>
        )}

        {bestSwap && (
          <div className="rounded-lg border border-violet-400/20 bg-violet-500/10 p-4">
            <p className="text-xs font-semibold uppercase tracking-[0.16em] text-violet-200/60">What-if comparison</p>
            <p className="mt-2 text-sm font-semibold text-white">
              Compare {bestSwap.originalCompound} vs {bestSwap.candidateCompound}
            </p>
            <p className="mt-1 text-xs text-white/45">
              internal score delta: +{bestSwap.deltaScore.toFixed(1)}
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

      {showTrackingCta && onTrackingRequest && (
        <div className="mt-4 rounded-lg border border-white/[0.06] bg-white/[0.02] p-4">
          <p className="text-sm text-white/60">
            Tracking this protocol over time will show whether these interaction patterns hold.
          </p>
          <button
            type="button"
            onClick={onTrackingRequest}
            className="mt-3 inline-block rounded-lg border border-emerald-300/25 px-4 py-2 text-sm font-semibold text-emerald-100 hover:bg-emerald-400/10"
          >
            Save this stack to start tracking
          </button>
        </div>
      )}
    </div>
  );
}

interface ReducedInteractionIntelligenceViewProps {
  intelligence: ReducedInteractionIntelligence;
  title: string;
  showTrackingCta: boolean;
  onTrackingRequest?: () => void;
}

function ReducedInteractionIntelligenceView({
  intelligence,
  title,
  showTrackingCta,
  onTrackingRequest,
}: ReducedInteractionIntelligenceViewProps) {
  const summary = intelligence.summary;
  const pairs = intelligence.pairs;

  return (
    <div className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
      <div>
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">{title}</p>
        <h3 className="mt-2 text-lg font-bold text-white">Flagged pairs in this stack</h3>
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-3">
        <div className="rounded-lg border border-emerald-400/15 bg-emerald-500/10 p-3">
          <p className="text-xs uppercase tracking-[0.16em] text-emerald-200/60"><HelpTip tipKey="synergy">Synergies</HelpTip></p>
          <p className="mt-2 text-2xl font-bold text-emerald-100">{summary.synergies}</p>
        </div>
        <div className="rounded-lg border border-amber-400/15 bg-amber-500/10 p-3">
          <p className="text-xs uppercase tracking-[0.16em] text-amber-200/60"><HelpTip tipKey="redundancy">Redundancies</HelpTip></p>
          <p className="mt-2 text-2xl font-bold text-amber-100">{summary.redundancies}</p>
        </div>
        <div className="rounded-lg border border-rose-400/15 bg-rose-500/10 p-3">
          <p className="text-xs uppercase tracking-[0.16em] text-rose-200/60"><HelpTip tipKey="interference">Interferences</HelpTip></p>
          <p className="mt-2 text-2xl font-bold text-rose-100">{summary.interferences}</p>
        </div>
      </div>

      <div className="mt-4 space-y-2">
        {pairs.length === 0 ? (
          <div className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4 text-sm text-white/50">
            No flagged pairs in this stack right now.
          </div>
        ) : (
          pairs.map((pair) => (
            <div
              key={`${pair.compoundA}-${pair.compoundB}`}
              className="flex flex-wrap items-center gap-2 rounded-lg border border-white/[0.08] bg-white/[0.03] p-3"
            >
              <span className={`rounded-lg border px-2 py-1 text-xs font-semibold ${toneByType[pair.severity] ?? toneByType.Neutral}`}>
                {pair.severity.toLowerCase()}
              </span>
              <span className="text-sm font-semibold text-white">{pair.compoundA} + {pair.compoundB}</span>
            </div>
          ))
        )}
      </div>

      <div className="mt-4 rounded-lg border border-emerald-300/15 bg-emerald-400/[0.05] p-4">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-200/70">Operator — Track & Analyze</p>
        <p className="mt-2 text-sm text-white">See why these pairs are flagged</p>
        <p className="mt-2 text-sm leading-6 text-white/60">
          Operator adds the reasoning behind each flagged pair, including shared pathways and a confidence read, plus counterfactual and swap comparisons for this stack.
        </p>
        <Link
          href="/pricing"
          className="mt-3 inline-flex rounded-lg border border-emerald-300/25 px-4 py-2 text-sm font-semibold text-emerald-100 hover:bg-emerald-400/10"
        >
          Compare plans
        </Link>
      </div>

      {showTrackingCta && onTrackingRequest && (
        <div className="mt-4 rounded-lg border border-white/[0.06] bg-white/[0.02] p-4">
          <p className="text-sm text-white/60">
            Tracking this protocol over time will show whether these interaction patterns hold.
          </p>
          <button
            type="button"
            onClick={onTrackingRequest}
            className="mt-3 inline-block rounded-lg border border-emerald-300/25 px-4 py-2 text-sm font-semibold text-emerald-100 hover:bg-emerald-400/10"
          >
            Save this stack to start tracking
          </button>
        </div>
      )}
    </div>
  );
}
