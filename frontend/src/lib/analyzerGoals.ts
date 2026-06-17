import { GOAL_DEFINITIONS } from './goals';

// API token strings sent as the analyzer `goal` / `secondaryGoals` values.
// The backend token-matches these against knowledge-base benefits, pathways,
// and mechanism summaries (CounterfactualCandidateService.GoalAlignment).
// MIRRORED by backend/tests/.../AnalyzerGoalVocabularyTests.cs — keep both
// lists byte-identical (that test is the authority).
export const ANALYZER_CATEGORY_TOKENS: Record<string, string> = {
  recovery: 'healing injury recovery tissue repair',
  energy: 'energy metabolic health',
  cognitive: 'cognitive enhancement',
  longevity: 'anti-aging cellular repair longevity',
  performance: 'performance muscle endurance recovery',
  skin: 'skin collagen anti-aging',
  organ: 'gut health cardiovascular organ health',
};

export const ANALYZER_GOAL_TOKENS: Record<string, string> = {
  'recovery-muscles': 'tissue repair muscle joint tendon',
  'recovery-inflammation': 'reduced inflammation',
  'recovery-injury': 'injury recovery healing',
  'recovery-post-workout': 'recovery healing',
  'energy-levels': 'energy',
  'energy-mitochondrial': 'cellular energy mitochondrial',
  'energy-metabolic': 'metabolic health insulin sensitivity',
  'energy-fat-loss': 'fat loss weight loss',
  'cognitive-focus': 'cognitive enhancement focus',
  'cognitive-memory': 'cognitive enhancement memory',
  'cognitive-performance': 'cognitive enhancement',
  'cognitive-neuro-health': 'cognitive neurological health',
  'longevity-aging': 'anti-aging',
  'longevity-cellular': 'DNA repair cellular repair',
  'longevity-pathways': 'longevity anti-aging',
  'performance-endurance': 'endurance energy',
  'performance-strength': 'muscle strength tissue repair',
  'performance-training': 'recovery energy',
  'skin-elasticity': 'skin elasticity anti-aging',
  'skin-appearance': 'skin anti-aging',
  'skin-collagen': 'collagen skin anti-aging',
  'organ-health': 'organ health liver kidney',
  'organ-gut': 'gut health',
  'organ-cardiovascular': 'cardiovascular heart metabolic',
};

export type AnalyzerGoalSelection = {
  primaryCategory: string | null;
  refinementGoalIds: string[];
};

export function buildAnalyzerGoalPayload(
  primaryCategory: string | null,
  refinementGoalIds: string[],
): { goal: string; secondaryGoals: string[] } {
  if (!primaryCategory) {
    return { goal: '', secondaryGoals: [] };
  }

  return {
    goal: ANALYZER_CATEGORY_TOKENS[primaryCategory] ?? '',
    secondaryGoals: refinementGoalIds
      .map((id) => ANALYZER_GOAL_TOKENS[id])
      .filter((tokens): tokens is string => Boolean(tokens)),
  };
}

export function prefillFromProfileGoals(profileGoalIds: string[]): AnalyzerGoalSelection {
  const first = profileGoalIds
    .map((id) => GOAL_DEFINITIONS.find((goal) => goal.id === id))
    .find((goal) => goal !== undefined);

  if (!first) {
    return { primaryCategory: null, refinementGoalIds: [] };
  }

  return { primaryCategory: first.category, refinementGoalIds: [first.id] };
}
