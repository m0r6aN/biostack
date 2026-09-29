import { ContextualRecommendations } from '@/components/recommendations/ContextualRecommendations';
import { getContextTagsForOverlapFlags, getRecommendationsForOverlapFlags } from '@/lib/recommendations';
import { InteractionFlag } from '@/lib/types';
import { SafetyDisclaimer } from '../SafetyDisclaimer';
import Link from 'next/link';

interface OverlapResultsProps {
  flags: InteractionFlag[];
  inputCount: number;
}

// Owner ruling 2026-09-16, extended 2026-09-17: this is the live public compatibility tool
// (POST /api/v1/knowledge/overlap-check) every visitor calls, signed in or not. Without the
// reviewed_relationship_graph entitlement (Operator), each flag arrives with `description` and
// `evidenceConfidence` omitted — the flag itself (which pair, how severe) is public.
function hasReasoning(flag: InteractionFlag): flag is InteractionFlag & { description: string } {
  return typeof flag.description === 'string' && flag.description.length > 0;
}

export function OverlapResults({ flags, inputCount }: OverlapResultsProps) {
  if (inputCount < 2) {
    return null;
  }

  const recommendationTags = getContextTagsForOverlapFlags(flags);
  const recommendations = getRecommendationsForOverlapFlags(flags, 3, 'overlap-results');

  if (flags.length === 0) {
    return (
      <div className="p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90">
        <p className="text-sm text-white/50 text-center">No pathway overlaps detected for selected compounds.</p>
      </div>
    );
  }

  // The reduced wire contract explicitly marks unavailable severity; empty full prose is not an entitlement signal.
  const isReduced = flags.every(flag => flag.severity === null);

  return (
    <div className="space-y-4">
      {flags.map((flag, i) => (
        <div key={i} className={`p-5 rounded-2xl border ${isReduced ? 'border-white/10 bg-white/[0.03]' : 'border-amber-400/15 bg-amber-500/10'}`}>
          <div className="flex items-start justify-between mb-3">
            <div>
              <h4 className={`font-semibold ${isReduced ? 'text-white/80' : 'text-amber-200'}`}>
                {flag.compoundNames.join(' × ')}
              </h4>
              <p className={`text-xs mt-1 ${isReduced ? 'text-white/60' : 'text-amber-300/70'}`}>{flag.severity === null ? 'Severity unavailable' : flag.overlapType}</p>
            </div>
          </div>

          {hasReasoning(flag) && <p className="text-sm text-amber-100/80 mb-3">{flag.description}</p>}

          <div className="flex items-center gap-2">
            {flag.pathwayTag?.trim() && (
            <span className="text-xs px-2.5 py-1 rounded-full border border-amber-400/20 bg-amber-500/15 text-amber-300">
              Pathway: {flag.pathwayTag}
            </span>
            )}
            {flag.evidenceConfidence && (
              <span className="text-xs px-2.5 py-1 rounded-full border border-white/[0.08] bg-white/[0.03] text-white/65">
                Confidence: {flag.evidenceConfidence}
              </span>
            )}
          </div>
        </div>
      ))}

      {isReduced && (
        <div className="rounded-2xl border border-emerald-300/15 bg-emerald-400/[0.05] p-4">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-200/70">Operator — Track & Analyze</p>
          <p className="mt-2 text-sm text-white">See why these pairs are flagged</p>
          <p className="mt-2 text-sm leading-6 text-white/60">
            Operator adds the reasoning behind each flagged pair, including shared pathways and a confidence read.
          </p>
          <Link
            href="/pricing"
            className="mt-3 inline-flex rounded-lg border border-emerald-300/25 px-4 py-2 text-sm font-semibold text-emerald-100 hover:bg-emerald-400/10"
          >
            Compare plans
          </Link>
        </div>
      )}

      <ContextualRecommendations
        recommendations={recommendations}
        surface="overlap-results"
        contextTags={recommendationTags}
      />

      <SafetyDisclaimer type="observation" />
    </div>
  );
}
