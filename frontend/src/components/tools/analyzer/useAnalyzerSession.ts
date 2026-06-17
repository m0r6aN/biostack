'use client';

import { useEffect, useState } from 'react';
import type { ProtocolAnalyzerInputType, ProtocolAnalyzerResult } from '@/lib/types';
import type { AnalyzerGoalSelection } from '@/lib/analyzerGoals';

export const STORAGE_KEY_V3 = 'biostack.analyzer.session.v3';
export const STORAGE_KEY_V4 = 'biostack.analyzer.session.v4';

export type AnalyzerContextFields = {
  sex: string;
  age: string;
  weight: string;
  existingStack: string;
};

export type AnalyzerSessionSnapshot = {
  mode: ProtocolAnalyzerInputType;
  inputText: string;
  linkUrl: string;
  goals: AnalyzerGoalSelection;
  context: AnalyzerContextFields;
  result: ProtocolAnalyzerResult | null;
};

type V3Snapshot = {
  mode?: ProtocolAnalyzerInputType;
  inputText?: string;
  linkUrl?: string;
  goal?: string;
  result?: ProtocolAnalyzerResult | null;
};

const EMPTY_CONTEXT: AnalyzerContextFields = { sex: '', age: '', weight: '', existingStack: '' };

function defaultSnapshot(): AnalyzerSessionSnapshot {
  return {
    mode: 'Paste',
    inputText: '',
    linkUrl: '',
    goals: { primaryCategory: null, refinementGoalIds: [] },
    context: { ...EMPTY_CONTEXT },
    result: null,
  };
}

// v3 stored the goal as one of '', 'healing', 'fat loss', 'longevity'.
const V3_GOAL_TO_SELECTION: Record<string, AnalyzerGoalSelection> = {
  healing: { primaryCategory: 'recovery', refinementGoalIds: [] },
  'fat loss': { primaryCategory: 'energy', refinementGoalIds: ['energy-fat-loss'] },
  longevity: { primaryCategory: 'longevity', refinementGoalIds: [] },
};

export function migrateV3Snapshot(v3: V3Snapshot): AnalyzerSessionSnapshot {
  return {
    ...defaultSnapshot(),
    mode: v3.mode ?? 'Paste',
    inputText: v3.inputText ?? '',
    linkUrl: v3.linkUrl ?? '',
    goals: V3_GOAL_TO_SELECTION[v3.goal ?? ''] ?? { primaryCategory: null, refinementGoalIds: [] },
    result: v3.result ?? null,
  };
}

export function readAnalyzerSessionSnapshot(): AnalyzerSessionSnapshot {
  if (typeof window === 'undefined') {
    return defaultSnapshot();
  }

  try {
    const v4 = window.localStorage.getItem(STORAGE_KEY_V4);
    if (v4) {
      const parsed = JSON.parse(v4) as Partial<AnalyzerSessionSnapshot>;
      return {
        ...defaultSnapshot(),
        ...parsed,
        goals: parsed.goals ?? { primaryCategory: null, refinementGoalIds: [] },
        context: { ...EMPTY_CONTEXT, ...parsed.context },
      };
    }

    const v3 = window.localStorage.getItem(STORAGE_KEY_V3);
    if (v3) {
      return migrateV3Snapshot(JSON.parse(v3) as V3Snapshot);
    }
  } catch {
    // fall through to default
  }

  return defaultSnapshot();
}

export function useAnalyzerSession() {
  const [snapshot, setSnapshot] = useState<AnalyzerSessionSnapshot>(defaultSnapshot);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    const id = window.setTimeout(() => {
      setSnapshot(readAnalyzerSessionSnapshot());
      setLoaded(true);
    }, 0);
    return () => window.clearTimeout(id);
  }, []);

  useEffect(() => {
    if (!loaded) {
      return;
    }
    try {
      window.localStorage.setItem(STORAGE_KEY_V4, JSON.stringify(snapshot));
    } catch {
      // storage quota or privacy mode — best-effort persistence
    }
  }, [snapshot, loaded]);

  return { snapshot, setSnapshot, loaded };
}
