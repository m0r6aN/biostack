import { ContextualRecommendations } from '@/components/recommendations/ContextualRecommendations';
import { getContextTagsForOverlapFlags, getRecommendationsForOverlapFlags } from '@/lib/recommendations';
import { InteractionFlag } from '@/lib/types';
import { SafetyDisclaimer } from '../SafetyDisclaimer';

interface OverlapResultsProps {
  flags: InteractionFlag[];
}

export function OverlapResults({ flags }: OverlapResultsProps) {
  const recommendationTags = getContextTagsForOverlapFlags(flags);
  const recommendations = getRecommendationsForOverlapFlags(flags, 3, 'overlap-results');

  if (flags.length === 0) {
    return (
      <div className="p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90">
        <p className="text-sm text-white/50 text-center">No pathway overlaps detected for selected compounds.</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {flags.map((flag, i) => (
        <div key={i} className="p-5 rounded-2xl border border-amber-400/15 bg-amber-500/10">
          <div className="flex items-start justify-between mb-3">
            <div>
              <h4 className="font-semibold text-amber-200">
                {flag.compoundNames.join(' × ')}
              </h4>
              <p className="text-xs text-amber-300/70 mt-1">{flag.overlapType}</p>
            </div>
          </div>

          <p className="text-sm text-amber-100/80 mb-3">{flag.description}</p>

          <div className="flex items-center gap-2">
            <span className="text-xs px-2.5 py-1 rounded-full border border-amber-400/20 bg-amber-500/15 text-amber-300">
              Pathway: {flag.pathwayTag}
            </span>
            <span className="text-xs px-2.5 py-1 rounded-full border border-white/[0.08] bg-white/[0.03] text-white/65">
              Confidence: {flag.evidenceConfidence}
            </span>
          </div>
        </div>
      ))}

      <ContextualRecommendations
        recommendations={recommendations}
        surface="overlap-results"
        contextTags={recommendationTags}
      />

      <SafetyDisclaimer type="observation" />
    </div>
  );
}
