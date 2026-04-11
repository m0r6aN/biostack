import { describe, it, expect, beforeEach, vi } from 'vitest';
import {
  GOAL_CATEGORIES,
  GOAL_DEFINITIONS,
  getCategoryMeta,
  getGoalsByCategory,
  resolveGoalDefinitions,
  getMockProfileGoalIds,
  setMockProfileGoalIds,
} from '@/lib/goals';

// ── seed data shape ───────────────────────────────────────────────────────────
describe('GOAL_CATEGORIES', () => {
  it('contains exactly 7 categories', () => {
    expect(GOAL_CATEGORIES).toHaveLength(7);
  });
  it('each category has required fields', () => {
    for (const cat of GOAL_CATEGORIES) {
      expect(cat).toHaveProperty('key');
      expect(cat).toHaveProperty('label');
      expect(cat).toHaveProperty('pillClasses');
      expect(cat).toHaveProperty('dotColor');
    }
  });
});

describe('GOAL_DEFINITIONS', () => {
  it('contains at least 20 goals', () => {
    expect(GOAL_DEFINITIONS.length).toBeGreaterThanOrEqual(20);
  });
  it('every goal has a unique id', () => {
    const ids = GOAL_DEFINITIONS.map(g => g.id);
    expect(new Set(ids).size).toBe(ids.length);
  });
  it('every goal references a valid category', () => {
    const validKeys = new Set(GOAL_CATEGORIES.map(c => c.key));
    for (const goal of GOAL_DEFINITIONS) {
      expect(validKeys.has(goal.category)).toBe(true);
    }
  });
});

// ── getCategoryMeta ───────────────────────────────────────────────────────────
describe('getCategoryMeta', () => {
  it('returns the correct meta for a known category', () => {
    const meta = getCategoryMeta('recovery');
    expect(meta.key).toBe('recovery');
    expect(meta.pillClasses).toContain('emerald');
  });
  it('returns a fallback for an unknown category', () => {
    const meta = getCategoryMeta('nonexistent');
    expect(meta.key).toBe('nonexistent');
    expect(meta.label).toBe('nonexistent');
    expect(meta.pillClasses).toBeTruthy();
  });
});

// ── getGoalsByCategory ────────────────────────────────────────────────────────
describe('getGoalsByCategory', () => {
  it('returns a Map with an entry for every category', () => {
    const map = getGoalsByCategory();
    for (const cat of GOAL_CATEGORIES) {
      expect(map.has(cat.key)).toBe(true);
    }
  });
  it('each entry contains only goals for that category', () => {
    const map = getGoalsByCategory();
    for (const [key, goals] of map) {
      for (const goal of goals) {
        expect(goal.category).toBe(key);
      }
    }
  });
  it('total goals across all categories equals GOAL_DEFINITIONS (active only)', () => {
    const map = getGoalsByCategory();
    const total = [...map.values()].reduce((sum, g) => sum + g.length, 0);
    const activeCount = GOAL_DEFINITIONS.filter(g => g.isActive).length;
    expect(total).toBe(activeCount);
  });
});

// ── resolveGoalDefinitions ────────────────────────────────────────────────────
describe('resolveGoalDefinitions', () => {
  it('resolves known IDs to GoalDefinition objects', () => {
    const id = GOAL_DEFINITIONS[0].id;
    const result = resolveGoalDefinitions([id]);
    expect(result).toHaveLength(1);
    expect(result[0].id).toBe(id);
  });
  it('silently drops unknown IDs', () => {
    const result = resolveGoalDefinitions(['unknown-id']);
    expect(result).toHaveLength(0);
  });
  it('handles empty input', () => {
    expect(resolveGoalDefinitions([])).toEqual([]);
  });
  it('handles a mix of valid and invalid IDs', () => {
    const ids = [GOAL_DEFINITIONS[0].id, 'bogus'];
    const result = resolveGoalDefinitions(ids);
    expect(result).toHaveLength(1);
  });
});

// ── localStorage mock helpers ─────────────────────────────────────────────────
describe('getMockProfileGoalIds / setMockProfileGoalIds', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('returns empty array when nothing stored for profile', () => {
    expect(getMockProfileGoalIds('profile-1')).toEqual([]);
  });

  it('round-trips goal ids through localStorage', () => {
    const ids = ['recovery-muscles', 'energy-levels'];
    setMockProfileGoalIds('profile-1', ids);
    expect(getMockProfileGoalIds('profile-1')).toEqual(ids);
  });

  it('stores goals independently per profile', () => {
    setMockProfileGoalIds('profile-a', ['recovery-muscles']);
    setMockProfileGoalIds('profile-b', ['energy-levels']);
    expect(getMockProfileGoalIds('profile-a')).toEqual(['recovery-muscles']);
    expect(getMockProfileGoalIds('profile-b')).toEqual(['energy-levels']);
  });

  it('overwrites existing goals on set', () => {
    setMockProfileGoalIds('profile-1', ['recovery-muscles']);
    setMockProfileGoalIds('profile-1', ['energy-levels', 'cognitive-focus']);
    expect(getMockProfileGoalIds('profile-1')).toEqual(['energy-levels', 'cognitive-focus']);
  });
});
