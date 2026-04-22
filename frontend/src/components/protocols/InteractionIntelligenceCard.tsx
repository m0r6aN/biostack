'use client';

import { SafetyDisclaimer } from '@/components/SafetyDisclaimer';
import { InteractionFinding, InteractionIntelligence, InteractionResult } from '@/lib/types';
import { useMemo, useState } from 'react';

interface InteractionIntelligenceCardProps {
  intelligence: InteractionIntelligence;
  title?: string;
  mode?: 'free' | 'paid';
}

const toneByType: Record<string, string> = {
  Synergistic: 'border-emerald-400/20 bg-emerald-500/10 text-emerald-100',
  Redundant: 'border-amber-400/20 bg-amber-500/10 text-amber-100',
  Interfering: 'border-rose-400/20 bg-rose-500/10 text-rose-100',
  Neutral: 'border-white/[0.08] bg-white/[0.04] text-white/70',
};

const typeLabel: Record<string, string> = {
  Synergistic: 'Supportive finding',
  Redundant: 'Redundancy finding',
  Interfering: 'Conflict finding',
  Neutral: 'Stack finding',
};

const swapReasonLabels: Record<string, string> = {
  reduces_redundancy: 'Reduces redundancy',
  preserves_synergy: 'Preserves useful support',
  lowers_interference: 'Lowers conflict risk',
  improves_goal_alignment: 'Improves goal alignment',
  improves_signal_clarity: 'Reduces redundancy',
  stronger_evidence: 'Stronger evidence',
  lower_estimated_cost: 'Lower estimated cost',
};

export function InteractionIntelligenceCard({
  intelligence,
  title = 'Stack Intelligence',
  mode = 'free',
}: InteractionIntelligenceCardProps) {
  const [showReasoning, setShowReasoning] = useState(false);
  const [showPaywall, setShowPaywall] = useState(false);
  const headlineFinding = intelligence.topFindings[0] ?? null;
  const moreFindingsCount = Math.max(0, intelligence.topFindings.length - 1);
  const swapCount = intelligence.swaps?.length ?? 0;
  const removeOneCount = intelligence.counterfactuals?.length ?? 0;
  const bestRemoval = intelligence.counterfactuals[0] ?? null;
  const bestSwap = intelligence.swaps?.[0] ?? null;
  const reasoning = useMemo(
    () => buildReasoningPreview(headlineFinding, intelligence.interactions),
    [headlineFinding, intelligence.interactions]
  );

  return (
    <div className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">{title}</p>
          <h3 className="mt-2 text-lg font-bold text-white">What this stack is doing together</h3>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-white/58">
            Free shows your composite score and the first strong finding. Scenarios, deeper reasoning, and history unlock next.
          </p>
        </div>
        <div className="min-w-[132px] rounded-2xl border border-emerald-400/20 bg-emerald-500/10 px-4 py-3 text-right">
          <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-emerald-200/60">Composite Score</p>
          <div className="mt-2 text-4xl font-black text-white">{Math.round(intelligence.compositeScore)}</div>
          <div className="mt-1 text-xs text-white/45">0-100 stack read</div>
        </div>
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-3">
        <MetricCard label="Findings" value={intelligence.topFindings.length} tone="emerald" />
        <MetricCard label="Drop-one scenarios" value={removeOneCount} tone="sky" />
        <MetricCard label="Swap scenarios" value={swapCount} tone="violet" />
      </div>

      <div className="mt-5">
        {headlineFinding ? (
          <div className="rounded-2xl border border-white/[0.08] bg-white/[0.03] p-4">
            <div className="flex flex-wrap items-center gap-2">
              <span className={`rounded-lg border px-2 py-1 text-xs font-semibold ${toneByType[headlineFinding.type] ?? toneByType.Neutral}`}>
                {typeLabel[headlineFinding.type] ?? 'Stack finding'}
              </span>
              <span className="text-sm font-semibold text-white">{headlineFinding.compounds.join(' + ')}</span>
              <span className="text-xs text-white/40">{formatConfidence(headlineFinding.confidence)} confidence</span>
            </div>
            <p className="mt-3 text-sm leading-6 text-white/72">{headlineFinding.message}</p>
            <div className="mt-4 flex flex-wrap items-center gap-3">
              <button
                type="button"
                onClick={() => setShowReasoning((current) => !current)}
                className="rounded-full border border-white/[0.1] px-3 py-1.5 text-xs font-semibold text-white/75 transition-colors hover:border-white/25 hover:text-white"
              >
                Why this finding
              </button>
              {mode === 'free' && (
                <span className="text-xs text-white/42">
                  Preview available now. Full reasoning unlocks with scenarios and tracked history.
                </span>
              )}
            </div>

            {showReasoning && (
              <div className="mt-4 rounded-2xl border border-white/[0.08] bg-black/20 p-4">
                <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/38">Why this finding</p>
                <div className="mt-3 grid gap-3 sm:grid-cols-2">
                  <ReasoningCell label="Contributing compounds" value={headlineFinding.compounds.join(', ')} />
                  <ReasoningCell label="Confidence band" value={formatConfidence(headlineFinding.confidence)} />
                  <ReasoningCell label="Basis" value={reasoning.basis} />
                  <ReasoningCell label="Reasoning count" value={reasoning.reasoningCount} />
                </div>
                {reasoning.pathways && (
                  <p className="mt-3 text-sm text-white/58">Pathway or overlap basis: {reasoning.pathways}</p>
                )}
                {mode === 'free' && (
                  <button
                    type="button"
                    onClick={() => setShowPaywall(true)}
                    className="mt-4 rounded-full border border-emerald-300/20 bg-emerald-500/10 px-4 py-2 text-sm font-semibold text-emerald-100 transition-colors hover:border-emerald-300/40"
                  >
                    Unlock full reasoning
                  </button>
                )}
              </div>
            )}
          </div>
        ) : (
          <div className="rounded-2xl border border-emerald-400/18 bg-emerald-500/8 p-4">
            <p className="text-sm font-semibold text-white">No major interactions or redundancies detected</p>
            <p className="mt-2 text-sm leading-6 text-white/65">
              At the current confidence threshold, this stack reads as relatively clean.
            </p>
            <button
              type="button"
              onClick={() => setShowPaywall(true)}
              className="mt-4 rounded-full border border-emerald-300/20 bg-emerald-500/10 px-4 py-2 text-sm font-semibold text-emerald-100 transition-colors hover:border-emerald-300/40"
            >
              See what changes if you simplify one item
            </button>
          </div>
        )}
      </div>

      <div className="mt-5 grid gap-3 sm:grid-cols-3">
        <TeaserButton
          label={`${moreFindingsCount || 0} more finding${moreFindingsCount === 1 ? '' : 's'}`}
          detail={moreFindingsCount > 0 ? 'See the next highest-confidence insights.' : 'No additional headline findings surfaced.'}
          disabled={moreFindingsCount === 0}
          onClick={() => setShowPaywall(true)}
        />
        <TeaserButton
          label={bestRemoval ? 'Drop one and compare' : `${removeOneCount} drop-one scenario${removeOneCount === 1 ? '' : 's'}`}
          detail={bestRemoval ? buildRemovalTeaser(bestRemoval) : 'See what changes if you remove one item.'}
          disabled={removeOneCount === 0}
          onClick={() => setShowPaywall(true)}
        />
        <TeaserButton
          label={bestSwap ? 'See the best swap' : `${swapCount} swap scenario${swapCount === 1 ? '' : 's'}`}
          detail={bestSwap ? buildSwapTeaser(bestSwap) : 'See constrained swap ideas for this stack.'}
          disabled={swapCount === 0}
          onClick={() => setShowPaywall(true)}
        />
      </div>

      <SafetyDisclaimer type="observation" />

      {showPaywall && (
        <div className="mt-5 rounded-2xl border border-emerald-400/18 bg-[linear-gradient(180deg,rgba(16,185,129,0.1),rgba(255,255,255,0.03))] p-5">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-200/65">Unlock the next layer</p>
          <h4 className="mt-2 text-xl font-bold text-white">Get the rest of the stack story</h4>
          <p className="mt-3 text-sm leading-6 text-white/64">
            Free shows your score and first finding. Paid unlocks scenarios, reasoning, and tracking over time.
          </p>
          <div className="mt-4 flex flex-wrap gap-2">
            <LockedCount>{moreFindingsCount} more findings</LockedCount>
            <LockedCount>{swapCount} swap scenarios</LockedCount>
            <LockedCount>{removeOneCount} drop-one scenarios</LockedCount>
          </div>
          <div className="mt-4 flex flex-wrap items-center gap-3">
            <button
              type="button"
              className="rounded-full bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5"
            >
              Start 7-day trial
            </button>
            <div className="text-sm text-white/55">
              <span className="font-semibold text-white">$12/mo</span> Operator
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function MetricCard({
  label,
  value,
  tone,
}: {
  label: string;
  value: number;
  tone: 'emerald' | 'sky' | 'violet';
}) {
  const toneClass = {
    emerald: 'border-emerald-400/15 bg-emerald-500/10 text-emerald-100',
    sky: 'border-sky-400/15 bg-sky-500/10 text-sky-100',
    violet: 'border-violet-400/15 bg-violet-500/10 text-violet-100',
  }[tone];

  return (
    <div className={`rounded-lg border p-3 ${toneClass}`}>
      <p className="text-xs uppercase tracking-[0.16em] opacity-70">{label}</p>
      <p className="mt-2 text-2xl font-bold">{value}</p>
    </div>
  );
}

function TeaserButton({
  label,
  detail,
  onClick,
  disabled,
}: {
  label: string;
  detail: string;
  onClick: () => void;
  disabled?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className="rounded-2xl border border-white/[0.08] bg-white/[0.025] p-4 text-left transition-colors hover:border-white/20 disabled:cursor-not-allowed disabled:opacity-55"
    >
      <p className="text-sm font-semibold text-white">{label}</p>
      <p className="mt-2 text-sm leading-6 text-white/55">{detail}</p>
    </button>
  );
}

function ReasoningCell({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-3">
      <p className="text-[11px] font-semibold uppercase tracking-[0.14em] text-white/35">{label}</p>
      <p className="mt-2 text-sm text-white/72">{value}</p>
    </div>
  );
}

function LockedCount({ children }: { children: React.ReactNode }) {
  return (
    <span className="rounded-full border border-white/[0.1] bg-black/15 px-3 py-1.5 text-xs font-semibold text-white/72">
      {children}
    </span>
  );
}

function formatConfidence(confidence: number) {
  if (confidence >= 0.75) {
    return 'High';
  }

  if (confidence >= 0.5) {
    return 'Medium';
  }

  return 'Low';
}

function buildReasoningPreview(
  finding: InteractionFinding | null,
  interactions: InteractionResult[]
) {
  if (!finding) {
    return {
      basis: 'No pairwise basis available',
      pathways: '',
      reasoningCount: '0 signals',
    };
  }

  const related = interactions.find((interaction) => {
    const interactionNames = [interaction.compoundA, interaction.compoundB].sort().join('|');
    const findingNames = [...finding.compounds].sort().join('|');
    return interactionNames === findingNames;
  });

  return {
    basis: related?.reason ?? 'Derived from the overlap profile of the compounds above.',
    pathways: related?.sharedPathways?.join(', ') ?? '',
    reasoningCount: `${1 + (related?.sharedPathways?.length ?? 0)} signals`,
  };
}

function buildRemovalTeaser(removal: InteractionIntelligence['counterfactuals'][number]) {
  return `${removal.removedCompound} changes the score from ${Math.round(removal.variantScore - removal.deltaScore)} to ${Math.round(removal.variantScore)}.`;
}

function buildSwapTeaser(swap: InteractionIntelligence['swaps'][number]) {
  const reasons = swap.reasons
    .slice(0, 2)
    .map((reason) => swapReasonLabels[reason] ?? reason.replace(/_/g, ' '))
    .join(' · ');

  return `Replace ${swap.originalCompound} with ${swap.candidateCompound}. ${reasons || 'See why this scenario ranks highest.'}`;
}
