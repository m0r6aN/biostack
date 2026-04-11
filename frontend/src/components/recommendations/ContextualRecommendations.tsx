import {
    getRecommendationSectionCopy,
    type ContextualRecommendation,
    type RecommendationContextTag,
    type RecommendationSurface,
} from '@/lib/recommendations';
import { AffiliateDisclosure } from './AffiliateDisclosure';
import { RecommendationCard } from './RecommendationCard';

interface ContextualRecommendationsProps {
  recommendations: ContextualRecommendation[];
  surface?: RecommendationSurface;
  contextTags?: RecommendationContextTag[];
  title?: string;
  description?: string;
  className?: string;
}

export function ContextualRecommendations({
  recommendations,
  surface = 'compound-detail',
  contextTags = [],
  title,
  description,
  className,
}: ContextualRecommendationsProps) {
  if (recommendations.length === 0) {
    return null;
  }

  const showDisclosure = recommendations.some((recommendation) => recommendation.disclosureRequired);
  const contextualCopy = getRecommendationSectionCopy(surface, contextTags);
  const resolvedTitle = title ?? contextualCopy.title;
  const resolvedDescription = description ?? contextualCopy.description;

  return (
    <section
      className={[
        'rounded-2xl border border-white/8 bg-black/15 p-4 sm:p-5',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
      aria-label="Contextual recommendations"
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="max-w-2xl">
          <p className="text-[10px] font-semibold uppercase tracking-[0.2em] text-emerald-300/60">
            {resolvedTitle}
          </p>
          <p className="mt-2 text-sm leading-6 text-white/56">{resolvedDescription}</p>
        </div>
        {showDisclosure && <AffiliateDisclosure className="max-w-[220px] text-right" />}
      </div>

      <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
        {recommendations.slice(0, 3).map((recommendation) => (
          <RecommendationCard key={recommendation.id} recommendation={recommendation} />
        ))}
      </div>
    </section>
  );
}