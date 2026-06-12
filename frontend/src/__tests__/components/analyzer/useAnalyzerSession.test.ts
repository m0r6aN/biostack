import { describe, expect, it, beforeEach } from 'vitest';
import {
  migrateV3Snapshot,
  readAnalyzerSessionSnapshot,
  STORAGE_KEY_V3,
  STORAGE_KEY_V4,
} from '@/components/tools/analyzer/useAnalyzerSession';

describe('analyzer session v4', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it('migrates a v3 goal string to a primary category', () => {
    expect(migrateV3Snapshot({ goal: 'healing' }).goals.primaryCategory).toBe('recovery');
    expect(migrateV3Snapshot({ goal: 'fat loss' }).goals).toEqual({
      primaryCategory: 'energy',
      refinementGoalIds: ['energy-fat-loss'],
    });
    expect(migrateV3Snapshot({ goal: 'longevity' }).goals.primaryCategory).toBe('longevity');
    expect(migrateV3Snapshot({ goal: '' }).goals.primaryCategory).toBeNull();
    expect(migrateV3Snapshot({ goal: 'something else' }).goals.primaryCategory).toBeNull();
  });

  it('carries forward v3 input fields and result', () => {
    const migrated = migrateV3Snapshot({
      mode: 'Link',
      inputText: 'BPC-157 500mcg daily',
      linkUrl: 'https://example.com/doc.pdf',
      result: null,
    });
    expect(migrated.mode).toBe('Link');
    expect(migrated.inputText).toBe('BPC-157 500mcg daily');
    expect(migrated.linkUrl).toBe('https://example.com/doc.pdf');
    expect(migrated.context).toEqual({ sex: '', age: '', weight: '', existingStack: '' });
  });

  it('reads v4 from storage, falling back to migrated v3', () => {
    window.localStorage.setItem(STORAGE_KEY_V3, JSON.stringify({ goal: 'healing', inputText: 'x' }));
    const snapshot = readAnalyzerSessionSnapshot();
    expect(snapshot.goals.primaryCategory).toBe('recovery');
    expect(snapshot.inputText).toBe('x');
  });

  it('prefers v4 over v3 when both exist', () => {
    window.localStorage.setItem(STORAGE_KEY_V3, JSON.stringify({ goal: 'healing' }));
    window.localStorage.setItem(
      STORAGE_KEY_V4,
      JSON.stringify({ goals: { primaryCategory: 'cognitive', refinementGoalIds: [] } }),
    );
    expect(readAnalyzerSessionSnapshot().goals.primaryCategory).toBe('cognitive');
  });

  it('returns a clean default snapshot when storage is empty or corrupt', () => {
    window.localStorage.setItem(STORAGE_KEY_V4, '{not json');
    const snapshot = readAnalyzerSessionSnapshot();
    expect(snapshot.goals).toEqual({ primaryCategory: null, refinementGoalIds: [] });
    expect(snapshot.result).toBeNull();
  });
});
