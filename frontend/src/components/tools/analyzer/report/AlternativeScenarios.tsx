'use client';

import type {
  ProtocolAnalyzerCounterfactual,
  ProtocolAnalyzerGoalAwareOption,
  ProtocolAnalyzerResult,
  ProtocolAnalyzerSwap,
} from '@/lib/types';
import { formatDelta, type OptimizedProtocolView } from '../analyzerView';

// ── ImprovementCard ───────────────────────────────────────────────────────────
// Moved verbatim from monolith ~1182-1216.

function ImprovementCard({
  title,
  teaser,
  empty,
  emptyDetail,
  kind,
}: {
  title: string;
  teaser: ProtocolAnalyzerCounterfactual | ProtocolAnalyzerSwap | null;
  empty: string;
  emptyDetail: string;
  kind: 'remove' | 'swap';
}) {
  return (
    <article className="rounded-lg border border-white/10 bg-black/20 p-4">
      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/40">{title}</p>
      {teaser ? (
        <>
          <p className="mt-2 text-base font-semibold text-white">
            {kind === 'remove'
              ? `Without ${(teaser as ProtocolAnalyzerCounterfactual).removedCompound}`
              : `Compare ${(teaser as ProtocolAnalyzerSwap).originalCompound} vs ${(teaser as ProtocolAnalyzerSwap).candidateCompound}`}
          </p>
          <p className="mt-1 text-sm text-emerald-100/85">Internal model score {Math.round(teaser.variantScore)} · model delta {formatDelta(teaser.deltaScore)}</p>
          <p className="mt-2 text-sm leading-6 text-white/58">{teaser.recommendation}</p>
        </>
      ) : (
        <>
          <p className="mt-2 text-base font-semibold text-white">{empty}</p>
          <p className="mt-2 text-sm leading-6 text-white/55">{emptyDetail}</p>
        </>
      )}
    </article>
  );
}

// ── SimplifiedProtocolCard ────────────────────────────────────────────────────
// Moved verbatim from monolith ~1218-1238.

function SimplifiedProtocolCard({ protocol }: { protocol: ProtocolAnalyzerResult['counterfactuals']['bestSimplifiedProtocol'] }) {
  return (
    <article className="rounded-lg border border-white/10 bg-black/20 p-4">
      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/40">Simplified protocol</p>
      {protocol ? (
        <>
          <p className="mt-2 text-base font-semibold text-white">{protocol.score} / 100 after simplification</p>
          <p className="mt-2 text-sm leading-6 text-white/58">Removed: {protocol.removed.join(', ')}</p>
          <p className="mt-2 text-sm leading-6 text-white/58">{protocol.reasons?.[0] ?? 'BioStack found a simpler variant, but did not return a detailed reason.'}</p>
        </>
      ) : (
        <>
          <p className="mt-2 text-base font-semibold text-white">No alternative scored above the current stack on the internal model.</p>
          <p className="mt-2 text-sm leading-6 text-white/55">
            This protocol may already be compact, or the system needs more profile context before surfacing a simpler arrangement.
          </p>
        </>
      )}
    </article>
  );
}

// ── GoalAwareCard ─────────────────────────────────────────────────────────────
// Moved verbatim from monolith ~1240-1260.

function GoalAwareCard({ option }: { option: ProtocolAnalyzerGoalAwareOption | null }) {
  return (
    <article className="rounded-lg border border-white/10 bg-black/20 p-4">
      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/40">Goal-aware alternative</p>
      {option ? (
        <>
          <p className="mt-2 text-base font-semibold text-white">Closest fit for {option.goal}</p>
          <p className="mt-1 text-sm text-emerald-100/85">Internal model score {option.score}</p>
          <p className="mt-2 text-sm leading-6 text-white/58">{option.reasons?.[0] ?? 'BioStack found a goal-aware variant, but did not return a detailed reason.'}</p>
        </>
      ) : (
        <>
          <p className="mt-2 text-base font-semibold text-white">Goal-aware alternative needs more context.</p>
          <p className="mt-2 text-sm leading-6 text-white/55">
            Add profile details or unlock full analysis to compare alternative scenarios for this goal.
          </p>
        </>
      )}
    </article>
  );
}

// ── AlternativeScenarios ──────────────────────────────────────────────────────

export interface AlternativeScenariosProps {
  result: ProtocolAnalyzerResult;
  optimized: OptimizedProtocolView | null;
}

export function AlternativeScenarios({ result, optimized }: AlternativeScenariosProps) {
  const counterfactuals = result.counterfactuals;
  const primaryRemoval = counterfactuals?.bestRemoveOne?.[0] ?? null;
  const primarySwap = counterfactuals?.bestSwapOne?.[0] ?? null;
  const simplified = counterfactuals?.bestSimplifiedProtocol ?? null;
  const goalAware = counterfactuals?.goalAwareOptions?.[0] ?? null;

  // BioStack only surfaces alternatives when one actually scores higher. If
  // nothing crosses the meaningful-improvement threshold, we hide the comparison
  // and the "Alternative scenarios" section instead of presenting an empty pretense.
  const hasMeaningfulImprovement = Boolean(optimized || primaryRemoval || primarySwap);

  if (!hasMeaningfulImprovement) {
    return null;
  }

  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      <h2 className="text-lg font-semibold text-white">Alternative scenarios</h2>
      <p className="mt-2 text-sm leading-6 text-white/55">
        BioStack is comparing other arrangements that reach the same goal with less overlap on the internal model.
      </p>
      <div className="mt-4 space-y-3">
        {optimized ? (
          // WhyBetterBlocks are rendered at the ComparisonSection level;
          // here we just surface the optimized label as a reference card.
          <article className="rounded-lg border border-emerald-300/20 bg-emerald-400/[0.07] p-4">
            <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/40">Optimized arrangement</p>
            <p className="mt-2 text-base font-semibold text-white">{optimized.label}</p>
            <p className="mt-1 text-sm text-emerald-100/85">Internal model score {optimized.score}</p>
          </article>
        ) : null}
        {primaryRemoval && (
          <ImprovementCard
            title="Remove-one scenario"
            teaser={primaryRemoval}
            empty="No obvious removal surfaced."
            emptyDetail="BioStack did not find a high-confidence compound to surface as a remove-one scenario for this goal."
            kind="remove"
          />
        )}
        {primarySwap && (
          <ImprovementCard
            title="What-if comparison"
            teaser={primarySwap}
            empty="No alternative surfaced."
            emptyDetail="BioStack did not find an alternative that scored higher on the internal model from the current knowledge set."
            kind="swap"
          />
        )}
        {simplified && <SimplifiedProtocolCard protocol={simplified} />}
        {goalAware && <GoalAwareCard option={goalAware} />}
      </div>
    </section>
  );
}
