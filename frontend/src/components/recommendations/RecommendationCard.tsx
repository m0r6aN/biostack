import type { ContextualRecommendation } from '@/lib/recommendations';

interface RecommendationCardProps {
  recommendation: ContextualRecommendation;
}

export function RecommendationCard({ recommendation }: RecommendationCardProps) {
  return (
    <article className="rounded-2xl border border-white/8 bg-white/[0.025] p-4">
      <div className="space-y-2">
        <h4 className="text-sm font-semibold text-white">{recommendation.displayName}</h4>
        <p className="text-sm leading-6 text-white/58">{recommendation.shortDescriptor}</p>
        {recommendation.whyItAppears && (
          <p className="text-xs leading-5 text-white/42">{recommendation.whyItAppears}</p>
        )}
        {(recommendation.sourceName || recommendation.sourceLabel) && (
          <p className="text-[11px] leading-5 text-white/34">
            Example source
            {recommendation.sourceName ? ` · ${recommendation.sourceName}` : ''}
            {recommendation.sourceLabel ? ` · ${recommendation.sourceLabel}` : ''}
          </p>
        )}
      </div>

      <a
        href={recommendation.affiliateUrl}
        target="_blank"
        rel="noreferrer noopener"
        className="mt-4 inline-flex text-sm font-medium text-emerald-200/85 transition-colors hover:text-emerald-100"
      >
        View example products
      </a>
    </article>
  );
}