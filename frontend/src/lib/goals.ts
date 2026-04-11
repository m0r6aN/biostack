'use client';

import { useState, useEffect } from 'react';
import { GoalDefinition } from './types';

export interface GoalCategoryMeta {
  key: string;
  label: string;
  pillClasses: string;
  dotColor: string;
}

export const GOAL_CATEGORIES: GoalCategoryMeta[] = [
  {
    key: 'recovery',
    label: 'Recovery & Repair',
    pillClasses: 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30',
    dotColor: 'bg-emerald-400',
  },
  {
    key: 'energy',
    label: 'Energy & Metabolism',
    pillClasses: 'bg-amber-500/20 text-amber-400 border border-amber-500/30',
    dotColor: 'bg-amber-400',
  },
  {
    key: 'cognitive',
    label: 'Cognitive & Neurological',
    pillClasses: 'bg-violet-500/20 text-violet-400 border border-violet-500/30',
    dotColor: 'bg-violet-400',
  },
  {
    key: 'longevity',
    label: 'Longevity & Aging',
    pillClasses: 'bg-blue-500/20 text-blue-400 border border-blue-500/30',
    dotColor: 'bg-blue-400',
  },
  {
    key: 'performance',
    label: 'Performance',
    pillClasses: 'bg-red-500/20 text-red-400 border border-red-500/30',
    dotColor: 'bg-red-400',
  },
  {
    key: 'skin',
    label: 'Skin & Appearance',
    pillClasses: 'bg-rose-500/20 text-rose-400 border border-rose-500/30',
    dotColor: 'bg-rose-400',
  },
  {
    key: 'organ',
    label: 'Organ & System Health',
    pillClasses: 'bg-cyan-500/20 text-cyan-400 border border-cyan-500/30',
    dotColor: 'bg-cyan-400',
  },
];

export const GOAL_DEFINITIONS: GoalDefinition[] = [
  // Recovery & Repair
  { id: 'recovery-muscles', name: 'Repair muscles, joints, and tendons', category: 'recovery', description: 'Support structural tissue recovery and resilience', isActive: true },
  { id: 'recovery-inflammation', name: 'Reduce inflammation', category: 'recovery', description: 'Manage systemic and localized inflammatory response', isActive: true },
  { id: 'recovery-injury', name: 'Accelerate injury healing', category: 'recovery', description: 'Support faster recovery from acute injuries', isActive: true },
  { id: 'recovery-post-workout', name: 'Improve post-workout recovery', category: 'recovery', description: 'Reduce soreness and speed up training recovery', isActive: true },

  // Energy & Metabolism
  { id: 'energy-levels', name: 'Improve energy levels', category: 'energy', description: 'Sustain daily energy without crashes', isActive: true },
  { id: 'energy-mitochondrial', name: 'Enhance mitochondrial function', category: 'energy', description: 'Support cellular energy production pathways', isActive: true },
  { id: 'energy-metabolic', name: 'Improve metabolic efficiency', category: 'energy', description: 'Optimize metabolic rate and nutrient utilization', isActive: true },
  { id: 'energy-fat-loss', name: 'Support fat loss', category: 'energy', description: 'Facilitate body composition changes toward leanness', isActive: true },

  // Cognitive & Neurological
  { id: 'cognitive-focus', name: 'Improve focus and clarity', category: 'cognitive', description: 'Sharpen sustained attention and mental clarity', isActive: true },
  { id: 'cognitive-memory', name: 'Enhance memory', category: 'cognitive', description: 'Support working and long-term memory formation', isActive: true },
  { id: 'cognitive-performance', name: 'Increase cognitive performance', category: 'cognitive', description: 'Elevate overall mental processing and output', isActive: true },
  { id: 'cognitive-neuro-health', name: 'Support neurological health', category: 'cognitive', description: 'Maintain and protect nervous system function', isActive: true },

  // Longevity & Aging
  { id: 'longevity-aging', name: 'Slow signs of aging', category: 'longevity', description: 'Address visible and functional markers of aging', isActive: true },
  { id: 'longevity-cellular', name: 'Improve cellular repair', category: 'longevity', description: 'Support autophagy and DNA repair mechanisms', isActive: true },
  { id: 'longevity-pathways', name: 'Support longevity pathways', category: 'longevity', description: 'Activate pathways associated with extended healthspan', isActive: true },

  // Performance
  { id: 'performance-endurance', name: 'Improve endurance', category: 'performance', description: 'Increase stamina and aerobic capacity', isActive: true },
  { id: 'performance-strength', name: 'Increase strength output', category: 'performance', description: 'Enhance force production and power output', isActive: true },
  { id: 'performance-training', name: 'Improve training capacity', category: 'performance', description: 'Increase volume and intensity tolerance', isActive: true },

  // Skin & Appearance
  { id: 'skin-elasticity', name: 'Improve skin elasticity', category: 'skin', description: 'Restore and maintain skin firmness and bounce', isActive: true },
  { id: 'skin-appearance', name: 'Enhance skin appearance', category: 'skin', description: 'Improve tone, texture, and overall skin quality', isActive: true },
  { id: 'skin-collagen', name: 'Support collagen production', category: 'skin', description: 'Stimulate natural collagen synthesis', isActive: true },

  // Organ & System Health
  { id: 'organ-health', name: 'Support organ health', category: 'organ', description: 'Maintain liver, kidney, and organ function', isActive: true },
  { id: 'organ-gut', name: 'Improve gut health', category: 'organ', description: 'Support microbiome and digestive function', isActive: true },
  { id: 'organ-cardiovascular', name: 'Support cardiovascular function', category: 'organ', description: 'Maintain heart and vascular system health', isActive: true },
];

export function getCategoryMeta(categoryKey: string): GoalCategoryMeta {
  return GOAL_CATEGORIES.find(c => c.key === categoryKey) ?? {
    key: categoryKey,
    label: categoryKey,
    pillClasses: 'bg-white/10 text-white/70 border border-white/15',
    dotColor: 'bg-white/50',
  };
}

export function getGoalsByCategory(): Map<string, GoalDefinition[]> {
  const map = new Map<string, GoalDefinition[]>();
  for (const cat of GOAL_CATEGORIES) {
    map.set(cat.key, GOAL_DEFINITIONS.filter(g => g.category === cat.key && g.isActive));
  }
  return map;
}

export function resolveGoalDefinitions(goalIds: string[]): GoalDefinition[] {
  return goalIds
    .map(id => GOAL_DEFINITIONS.find(g => g.id === id))
    .filter((g): g is GoalDefinition => g !== undefined);
}

// localStorage keys for mock fallback
const MOCK_GOALS_KEY = 'biostack_profile_goals';

function getMockGoals(): Record<string, string[]> {
  if (typeof window === 'undefined') return {};
  try {
    return JSON.parse(localStorage.getItem(MOCK_GOALS_KEY) || '{}');
  } catch {
    return {};
  }
}

function setMockGoals(data: Record<string, string[]>) {
  if (typeof window === 'undefined') return;
  localStorage.setItem(MOCK_GOALS_KEY, JSON.stringify(data));
}

export function getMockProfileGoalIds(profileId: string): string[] {
  return getMockGoals()[profileId] ?? [];
}

export function setMockProfileGoalIds(profileId: string, goalIds: string[]) {
  const data = getMockGoals();
  data[profileId] = goalIds;
  setMockGoals(data);
}

export function useGoalDefinitions(): { goals: GoalDefinition[]; loading: boolean } {
  const [goals, setGoals] = useState<GoalDefinition[]>(GOAL_DEFINITIONS);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    // For now, use local definitions. When backend ships GET /api/v1/goals,
    // replace this with an API call and fall back to GOAL_DEFINITIONS on error.
    setGoals(GOAL_DEFINITIONS);
    setLoading(false);
  }, []);

  return { goals, loading };
}
