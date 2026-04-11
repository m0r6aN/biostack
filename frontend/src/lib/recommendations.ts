import type { InteractionFlag, KnowledgeEntry } from '@/lib/types';

export type RecommendationContextTag = 'recovery' | 'energy' | 'mitochondrial' | 'performance';
export type RecommendationSurface = 'compound-detail' | 'knowledge-search' | 'overlap-results';

interface RecommendationSource {
  id: string;
  sourceName: string;
  sourceLabel: string;
  affiliateUrl: string;
}

export interface ContextualRecommendation {
  id: string;
  displayName: string;
  contextTags: RecommendationContextTag[];
  shortDescriptor: string;
  whyItAppears?: string;
  affiliateUrl: string;
  sourceName?: string;
  sourceLabel?: string;
  sourceId?: string;
  disclosureRequired: boolean;
}

interface RecommendationSectionCopy {
  title: string;
  description: string;
}

interface ContextualRecommendationSeed {
  id: string;
  displayName: string;
  contextTags: RecommendationContextTag[];
  shortDescriptor: string;
  whyItAppears?: string;
  exampleSources: RecommendationSource[];
  disclosureRequired: boolean;
}

const CONTEXT_KEYWORDS: Record<RecommendationContextTag, string[]> = {
  recovery: ['bpc-157', 'tb-500', 'recovery', 'repair', 'tissue-repair', 'anti-inflammatory', 'healing'],
  energy: ['nad+', 'mots-c', 'energy', 'cellular-energy', 'metabolic', 'fatigue'],
  mitochondrial: ['nad+', 'mots-c', 'mitochondrial', 'cellular-energy', 'atp'],
  performance: ['creatine', 'performance', 'strength', 'exercise', 'training'],
};

const SURFACE_SOURCE_OFFSET: Record<RecommendationSurface, number> = {
  'compound-detail': 0,
  'knowledge-search': 1,
  'overlap-results': 2,
};

const recommendationCatalog: ContextualRecommendationSeed[] = [
  {
    id: 'creatine',
    displayName: 'Creatine',
    contextTags: ['recovery', 'energy', 'performance'],
    shortDescriptor: 'Often included in performance, recovery, and energy-focused routines',
    whyItAppears: 'Appears here because people often explore it in similar recovery or energy-support contexts.',
    exampleSources: [
      {
        id: 'amazon',
        sourceName: 'Amazon',
        sourceLabel: 'Example marketplace search',
        affiliateUrl: 'https://www.amazon.com/s?k=creatine+monohydrate',
      },
      {
        id: 'iherb',
        sourceName: 'iHerb',
        sourceLabel: 'Example supplement search',
        affiliateUrl: 'https://www.iherb.com/search?kw=creatine',
      },
      {
        id: 'vitacost',
        sourceName: 'Vitacost',
        sourceLabel: 'Example catalog search',
        affiliateUrl: 'https://www.vitacost.com/search.aspx?t=creatine',
      },
    ],
    disclosureRequired: true,
  },
  {
    id: 'collagen-peptides',
    displayName: 'Collagen peptides',
    contextTags: ['recovery'],
    shortDescriptor: 'Often included in connective-tissue and recovery-focused routines',
    whyItAppears: 'Appears here because this context is tied to repair and recovery-oriented setups.',
    exampleSources: [
      {
        id: 'iherb',
        sourceName: 'iHerb',
        sourceLabel: 'Example supplement search',
        affiliateUrl: 'https://www.iherb.com/search?kw=collagen+peptides',
      },
      {
        id: 'amazon',
        sourceName: 'Amazon',
        sourceLabel: 'Example marketplace search',
        affiliateUrl: 'https://www.amazon.com/s?k=collagen+peptides',
      },
      {
        id: 'vitacost',
        sourceName: 'Vitacost',
        sourceLabel: 'Example catalog search',
        affiliateUrl: 'https://www.vitacost.com/search.aspx?t=collagen+peptides',
      },
    ],
    disclosureRequired: true,
  },
  {
    id: 'coq10',
    displayName: 'CoQ10',
    contextTags: ['energy', 'mitochondrial'],
    shortDescriptor: 'Often explored in cellular-energy and mitochondrial-support routines',
    whyItAppears: 'Appears here because people commonly look at it in mitochondrial or energy-support contexts.',
    exampleSources: [
      {
        id: 'vitacost',
        sourceName: 'Vitacost',
        sourceLabel: 'Example catalog search',
        affiliateUrl: 'https://www.vitacost.com/search.aspx?t=coq10',
      },
      {
        id: 'amazon',
        sourceName: 'Amazon',
        sourceLabel: 'Example marketplace search',
        affiliateUrl: 'https://www.amazon.com/s?k=coq10',
      },
      {
        id: 'iherb',
        sourceName: 'iHerb',
        sourceLabel: 'Example supplement search',
        affiliateUrl: 'https://www.iherb.com/search?kw=coq10',
      },
    ],
    disclosureRequired: true,
  },
];

const SECTION_COPY: Record<RecommendationSurface, Record<string, RecommendationSectionCopy>> = {
  'compound-detail': {
    default: {
      title: 'Common additions',
      description: 'Optional examples often used alongside this kind of setup.',
    },
    recovery: {
      title: 'Common additions',
      description: 'Some people include these in similar recovery-focused setups.',
    },
    energy: {
      title: 'Common additions',
      description: 'Here are a few common examples people look at in similar energy-support contexts.',
    },
    mitochondrial: {
      title: 'Common additions',
      description: 'Here are a few common examples people look at in similar mitochondrial-support contexts.',
    },
  },
  'knowledge-search': {
    default: {
      title: 'Common additions',
      description: 'Optional examples people often explore after reading about this compound or category.',
    },
    recovery: {
      title: 'Common additions',
      description: 'People exploring recovery-oriented compounds often look at these examples next.',
    },
    energy: {
      title: 'Common additions',
      description: 'People exploring energy-support compounds often look at these examples next.',
    },
    mitochondrial: {
      title: 'Common additions',
      description: 'People exploring mitochondrial-support compounds often look at these examples next.',
    },
  },
  'overlap-results': {
    default: {
      title: 'Common additions',
      description: 'Some people include these in similar setups once they understand the overlap context.',
    },
    recovery: {
      title: 'Common additions',
      description: 'Some people look at these examples in similar recovery-oriented overlap contexts.',
    },
    energy: {
      title: 'Common additions',
      description: 'Some people look at these examples in similar energy-support overlap contexts.',
    },
    mitochondrial: {
      title: 'Common additions',
      description: 'Some people look at these examples in similar mitochondrial-support overlap contexts.',
    },
  },
};

function normalize(value: string) {
  return value.trim().toLowerCase();
}

function collectContextTags(values: string[]) {
  const normalizedValues = values.map(normalize).filter(Boolean);
  const matches = new Set<RecommendationContextTag>();

  for (const [tag, keywords] of Object.entries(CONTEXT_KEYWORDS) as Array<[
    RecommendationContextTag,
    string[],
  ]>) {
    const hasMatch = keywords.some((keyword) =>
      normalizedValues.some((value) => value.includes(keyword) || keyword.includes(value))
    );

    if (hasMatch) {
      matches.add(tag);
    }
  }

  return Array.from(matches);
}

function pickPrimaryTag(tags: RecommendationContextTag[]) {
  const priority: RecommendationContextTag[] = ['recovery', 'mitochondrial', 'energy', 'performance'];
  return priority.find((tag) => tags.includes(tag));
}

function resolveRecommendationSource(
  recommendation: ContextualRecommendationSeed,
  surface: RecommendationSurface,
  position: number,
  usedSourceIds: Set<string>
) {
  const sourceCount = recommendation.exampleSources.length;
  const startIndex = (SURFACE_SOURCE_OFFSET[surface] + position) % sourceCount;

  for (let offset = 0; offset < sourceCount; offset += 1) {
    const candidate = recommendation.exampleSources[(startIndex + offset) % sourceCount];
    if (!usedSourceIds.has(candidate.id)) {
      usedSourceIds.add(candidate.id);
      return candidate;
    }
  }

  return recommendation.exampleSources[startIndex];
}

export function getRecommendationsForTags(
  tags: RecommendationContextTag[],
  limit = 3,
  surface: RecommendationSurface = 'compound-detail'
) {
  if (tags.length === 0) {
    return [];
  }

  const tagSet = new Set(tags);
  const usedSourceIds = new Set<string>();

  return recommendationCatalog
    .map((recommendation) => ({
      recommendation,
      score: recommendation.contextTags.filter((tag) => tagSet.has(tag)).length,
    }))
    .filter((entry) => entry.score > 0)
    .sort((left, right) => right.score - left.score)
    .slice(0, limit)
    .map((entry, index) => {
      const resolvedSource = resolveRecommendationSource(entry.recommendation, surface, index, usedSourceIds);

      return {
        id: entry.recommendation.id,
        displayName: entry.recommendation.displayName,
        contextTags: entry.recommendation.contextTags,
        shortDescriptor: entry.recommendation.shortDescriptor,
        whyItAppears: entry.recommendation.whyItAppears,
        affiliateUrl: resolvedSource.affiliateUrl,
        sourceId: resolvedSource.id,
        sourceName: resolvedSource.sourceName,
        sourceLabel: resolvedSource.sourceLabel,
        disclosureRequired: entry.recommendation.disclosureRequired,
      };
    });
}

export function getRecommendationSectionCopy(
  surface: RecommendationSurface,
  tags: RecommendationContextTag[]
) {
  const primaryTag = pickPrimaryTag(tags);

  if (primaryTag && SECTION_COPY[surface][primaryTag]) {
    return SECTION_COPY[surface][primaryTag];
  }

  return SECTION_COPY[surface].default;
}

export function getContextTagsForKnowledgeEntry(entry: KnowledgeEntry) {
  return collectContextTags([
    entry.canonicalName,
    entry.classification,
    ...entry.pathways,
    ...entry.benefits,
    ...entry.pairsWellWith,
  ]);
}

export function getContextTagsForOverlapFlags(flags: InteractionFlag[]) {
  return collectContextTags(
    flags.flatMap((flag) => [flag.pathwayTag, flag.overlapType, flag.description, ...flag.compoundNames])
  );
}

export function getRecommendationsForKnowledgeEntry(
  entry: KnowledgeEntry,
  limit = 3,
  surface: RecommendationSurface = 'compound-detail'
) {
  const tags = getContextTagsForKnowledgeEntry(entry);

  return getRecommendationsForTags(tags, limit, surface);
}

export function getRecommendationsForOverlapFlags(
  flags: InteractionFlag[],
  limit = 3,
  surface: RecommendationSurface = 'overlap-results'
) {
  const tags = getContextTagsForOverlapFlags(flags);

  return getRecommendationsForTags(tags, limit, surface);
}