'use client';

import type { ProtocolAnalyzerResult } from '@/lib/types';
import { formatDelta, goalText, unique, type OptimizedProtocolView } from '../analyzerView';

// ── ProtocolComparisonList ─────────────────────────────────────────────────────
// Moved verbatim from monolith ~903-931.

function ProtocolComparisonList({
  title,
  entries,
  empty,
  accent = false,
}: {
  title: string;
  entries: ProtocolAnalyzerResult['protocol'];
  empty: string;
  accent?: boolean;
}) {
  return (
    <div className={`rounded-lg border p-3 ${accent ? 'border-emerald-300/20 bg-emerald-400/[0.07]' : 'border-white/10 bg-black/20'}`}>
      <p className="text-sm font-semibold text-white">{title}</p>
      {entries.length > 0 ? (
        <ul className="mt-3 space-y-2">
          {entries.map((entry) => (
            <li key={`${title}-${entry.compoundName}-${entry.dose}-${entry.frequency}`} className="text-sm leading-6 text-white/68">
              <span className="font-semibold text-white/86">{entry.compoundName}</span>
              <span> {entry.dose > 0 ? `${entry.dose} ${entry.unit}` : ''} {entry.frequency || 'frequency unclear'} {entry.duration ? `for ${entry.duration}` : ''}</span>
            </li>
          ))}
        </ul>
      ) : (
        <p className="mt-3 text-sm text-white/45">{empty}</p>
      )}
    </div>
  );
}

// ── OriginalVsOptimizedSection ────────────────────────────────────────────────
// Moved verbatim from monolith ~871-901.

function OriginalVsOptimizedSection({
  result,
  optimized,
}: {
  result: ProtocolAnalyzerResult;
  optimized: OptimizedProtocolView | null;
}) {
  const optimizedScore = optimized?.score ?? result.score;
  const delta = optimizedScore - result.score;

  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-semibold text-white">Original vs BioStack alternative</h2>
        <span className="rounded-lg border border-emerald-300/20 bg-emerald-400/10 px-3 py-1.5 text-sm font-semibold text-emerald-100">
          {result.score} vs {optimizedScore} · model delta {formatDelta(delta)}
        </span>
      </div>
      <div className="mt-4 grid gap-4 lg:grid-cols-[1fr_auto_1fr]">
        <ProtocolComparisonList title="Original protocol" entries={result.protocol} empty="No original compounds parsed." />
        <div className="hidden items-center justify-center text-white/35 lg:flex">vs</div>
        <ProtocolComparisonList
          title={optimized?.label ?? 'BioStack alternative protocol'}
          entries={optimized?.protocol ?? result.protocol}
          empty="No alternative protocol surfaced yet."
          accent
        />
      </div>
    </section>
  );
}

// ── WhyBetterBlocks ───────────────────────────────────────────────────────────
// Moved verbatim from monolith ~933-993.

function WhyBetterBlocks({
  result,
  optimized,
}: {
  result: ProtocolAnalyzerResult;
  optimized: OptimizedProtocolView | null;
}) {
  const counterfactuals = result.counterfactuals;
  const removed = optimized?.removed ?? counterfactuals?.bestRemoveOne?.slice(0, 2).map((item) => item.removedCompound) ?? [];
  const optimizedNames = new Set((optimized?.protocol ?? result.protocol).map((entry) => entry.compoundName.toLowerCase()));
  const originalNames = result.protocol.map((entry) => entry.compoundName);
  const retained = originalNames.filter((name) => optimizedNames.has(name.toLowerCase()));
  const issueCompounds = result.issues.flatMap((issue) => issue.compounds).filter(Boolean);
  const swap = counterfactuals?.bestSwapOne?.[0] ?? null;

  const blocks = [
    {
      title: 'Reduced pathway overlap',
      body:
        issueCompounds.length > 0
          ? `BioStack flagged overlap around ${unique(issueCompounds).slice(0, 3).join(', ')} and tested an alternative arrangement.`
          : retained.length > 1
            ? `The alternative arrangement keeps ${retained.slice(0, 3).join(', ')} while reducing noisy stack interactions.`
            : 'No major overlap pattern dominated this protocol.',
    },
    {
      title: 'Redundant compounds in this arrangement',
      body:
        removed.length > 0
          ? `${removed.slice(0, 3).join(', ')} ${removed.length === 1 ? 'was' : 'were'} the clearest simplification target on the internal model.`
          : 'No obvious removal moved the score on the internal model under the current rules.',
    },
    {
      title: 'Goal alignment delta',
      body:
        swap
          ? `${swap.candidateCompound} scored higher than ${swap.originalCompound} on the goal-aware internal model.`
          : optimized
            ? `${optimized.label} scored ${optimized.score} on the internal model, giving the ${goalText(optimized)} path a clearer fit.`
            : `The current stack scored ${result.score}, with no goal-aware variant ranking higher yet.`,
    },
    {
      title: 'Protocol complexity',
      body:
        optimized && optimized.protocol.length < result.protocol.length
          ? `Compound count drops from ${result.protocol.length} to ${optimized.protocol.length} in this arrangement, making attribution easier.`
          : `BioStack parsed ${result.protocol.length} item${result.protocol.length === 1 ? '' : 's'} and highlighted where clarity is still missing.`,
    },
  ];

  return (
    <div className="grid gap-3">
      {blocks.map((block) => (
        <article key={block.title} className="rounded-lg border border-white/10 bg-black/20 p-3">
          <p className="text-sm font-semibold text-white">{block.title}</p>
          <p className="mt-1 text-sm leading-6 text-white/58">{block.body}</p>
        </article>
      ))}
    </div>
  );
}

// ── ComparisonSection ─────────────────────────────────────────────────────────

export interface ComparisonSectionProps {
  result: ProtocolAnalyzerResult;
  optimized: OptimizedProtocolView | null;
}

export function ComparisonSection({ result, optimized }: ComparisonSectionProps) {
  return (
    <div className="space-y-4">
      <OriginalVsOptimizedSection result={result} optimized={optimized} />
      {optimized && <WhyBetterBlocks result={result} optimized={optimized} />}
    </div>
  );
}
