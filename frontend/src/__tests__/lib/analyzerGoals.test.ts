import { describe, expect, it } from 'vitest';
import {
  ANALYZER_CATEGORY_TOKENS,
  ANALYZER_GOAL_TOKENS,
  buildAnalyzerGoalPayload,
  prefillFromProfileGoals,
} from '@/lib/analyzerGoals';
import { GOAL_CATEGORIES, GOAL_DEFINITIONS } from '@/lib/goals';

describe('analyzerGoals', () => {
  it('has a token string for every category', () => {
    for (const category of GOAL_CATEGORIES) {
      expect(ANALYZER_CATEGORY_TOKENS[category.key], `missing tokens for ${category.key}`).toBeTruthy();
    }
  });

  it('has a token string for every active goal definition', () => {
    for (const goal of GOAL_DEFINITIONS.filter((g) => g.isActive)) {
      expect(ANALYZER_GOAL_TOKENS[goal.id], `missing tokens for ${goal.id}`).toBeTruthy();
    }
  });

  it('builds an empty payload for the no-goal state', () => {
    expect(buildAnalyzerGoalPayload(null, [])).toEqual({ goal: '', secondaryGoals: [] });
  });

  it('maps primary category and refinements to API tokens', () => {
    const payload = buildAnalyzerGoalPayload('energy', ['energy-fat-loss']);
    expect(payload.goal).toBe(ANALYZER_CATEGORY_TOKENS['energy']);
    expect(payload.secondaryGoals).toEqual([ANALYZER_GOAL_TOKENS['energy-fat-loss']]);
  });

  it('ignores refinements without a known token', () => {
    expect(buildAnalyzerGoalPayload('energy', ['nonexistent']).secondaryGoals).toEqual([]);
  });

  it('prefills primary category and refinement from profile goal ids', () => {
    expect(prefillFromProfileGoals(['energy-fat-loss', 'cognitive-focus'])).toEqual({
      primaryCategory: 'energy',
      refinementGoalIds: ['energy-fat-loss'],
    });
  });

  it('prefills nothing from empty profile goals', () => {
    expect(prefillFromProfileGoals([])).toEqual({ primaryCategory: null, refinementGoalIds: [] });
  });
});
