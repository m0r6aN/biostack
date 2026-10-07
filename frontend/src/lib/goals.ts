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
  { id: 'recovery-muscles', name: 'Muscle, joint, and tendon recovery', category: 'recovery', description: 'Observe comfort, function, and recovery patterns over time', isActive: true },
  { id: 'recovery-inflammation', name: 'Inflammation-related patterns', category: 'recovery', description: 'Track user-reported inflammation-related changes over time', isActive: true },
  { id: 'recovery-injury', name: 'Injury recovery', category: 'recovery', description: 'Observe recovery trends following an injury', isActive: true },
  { id: 'recovery-post-workout', name: 'Post-workout recovery', category: 'recovery', description: 'Track soreness and return-to-baseline after training', isActive: true },

  // Energy & Metabolism
  { id: 'energy-levels', name: 'Daily energy', category: 'energy', description: 'Observe self-reported energy patterns across daily routines', isActive: true },
  { id: 'energy-mitochondrial', name: 'Cellular energy context', category: 'energy', description: 'Organize observations related to cellular energy evidence', isActive: true },
  { id: 'energy-metabolic', name: 'Metabolic patterns', category: 'energy', description: 'Track weight, appetite, and energy trends over time', isActive: true },
  { id: 'energy-fat-loss', name: 'Body composition', category: 'energy', description: 'Observe weight and body-composition trends without prescribing a target', isActive: true },

  // Cognitive & Neurological
  { id: 'cognitive-focus', name: 'Focus and clarity', category: 'cognitive', description: 'Track self-reported attention and clarity patterns', isActive: true },
  { id: 'cognitive-memory', name: 'Memory', category: 'cognitive', description: 'Observe self-reported working and long-term memory patterns', isActive: true },
  { id: 'cognitive-performance', name: 'Cognitive performance', category: 'cognitive', description: 'Track self-reported mental processing and output', isActive: true },
  { id: 'cognitive-neuro-health', name: 'Neurological health context', category: 'cognitive', description: 'Organize neurological observations for longitudinal review', isActive: true },

  // Longevity & Aging
  { id: 'longevity-aging', name: 'Aging-related changes', category: 'longevity', description: 'Observe visible and functional changes over time', isActive: true },
  { id: 'longevity-cellular', name: 'Cellular repair context', category: 'longevity', description: 'Organize observations related to cellular repair evidence', isActive: true },
  { id: 'longevity-pathways', name: 'Longevity pathway context', category: 'longevity', description: 'Track observations alongside evidence about longevity-associated pathways', isActive: true },

  // Performance
  { id: 'performance-endurance', name: 'Endurance', category: 'performance', description: 'Track stamina and aerobic-capacity observations', isActive: true },
  { id: 'performance-strength', name: 'Strength output', category: 'performance', description: 'Observe strength and power trends over time', isActive: true },
  { id: 'performance-training', name: 'Training capacity', category: 'performance', description: 'Track training volume, intensity, and recovery patterns', isActive: true },

  // Skin & Appearance
  { id: 'skin-elasticity', name: 'Skin elasticity', category: 'skin', description: 'Observe changes in skin firmness and elasticity', isActive: true },
  { id: 'skin-appearance', name: 'Skin appearance', category: 'skin', description: 'Track self-reported tone, texture, and skin quality', isActive: true },
  { id: 'skin-collagen', name: 'Collagen context', category: 'skin', description: 'Organize skin observations alongside collagen-related evidence', isActive: true },

  // Organ & System Health
  { id: 'organ-health', name: 'Organ health context', category: 'organ', description: 'Organize user-entered observations for longitudinal review', isActive: true },
  { id: 'organ-gut', name: 'Digestive patterns', category: 'organ', description: 'Track self-reported digestive and gastrointestinal patterns', isActive: true },
  { id: 'organ-cardiovascular', name: 'Cardiovascular context', category: 'organ', description: 'Organize cardiovascular observations for longitudinal review', isActive: true },
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
    // Keep the picker immediately available from the matching versioned catalog;
    // authenticated profile persistence uses the backend goals contract.
    setGoals(GOAL_DEFINITIONS);
    setLoading(false);
  }, []);

  return { goals, loading };
}
