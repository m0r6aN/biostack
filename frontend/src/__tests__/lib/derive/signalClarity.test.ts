import { describe, it, expect } from 'vitest';
import { deriveSignalClarity } from '@/lib/derive/signalClarity';
import type { CurrentStackIntelligence, CompoundRecord } from '@/lib/types';

function makeStack(score: number): CurrentStackIntelligence {
  return {
    stackScore: { score, breakdown: { synergy: score, redundancy: 0, conflicts: 0, evidence: 0 }, chips: [] },
    simulation: { timeline: [], insights: [] },
    interactionIntelligence: { summary: { synergies: 0, redundancies: 0, interferences: 0 }, score: { synergyScore: 0, redundancyPenalty: 0, interferencePenalty: 0 }, compositeScore: score, topFindings: [], interactions: [], counterfactuals: [], swaps: [] },
  };
}

function makeCompound(status = 'Active'): CompoundRecord {
  return { id: 'c1', personId: 'p1', name: 'Magnesium', category: 'Mineral', startDate: '2026-01-01', endDate: null, status, notes: '', sourceType: 'manual' };
}

describe('deriveSignalClarity', () => {
  it('returns 0 score when stack is null', () => {
    const result = deriveSignalClarity(null, [], 0, false);
    expect(result.score).toBe(0);
  });

  it('applies penalty for no active run', () => {
    const full = deriveSignalClarity(makeStack(80), [makeCompound()], 10, true);
    const noRun = deriveSignalClarity(makeStack(80), [makeCompound()], 10, false);
    expect(noRun.score).toBeLessThan(full.score);
  });

  it('applies penalty for no check-ins', () => {
    const withCheckins = deriveSignalClarity(makeStack(70), [makeCompound()], 5, true);
    const noCheckins = deriveSignalClarity(makeStack(70), [makeCompound()], 0, true);
    expect(noCheckins.score).toBeLessThan(withCheckins.score);
  });

  it('applies penalty for no active compounds', () => {
    const withCompounds = deriveSignalClarity(makeStack(70), [makeCompound()], 5, true);
    const noCompounds = deriveSignalClarity(makeStack(70), [], 5, true);
    expect(noCompounds.score).toBeLessThan(withCompounds.score);
  });

  it('caps score at 100', () => {
    const result = deriveSignalClarity(makeStack(100), [makeCompound()], 20, true);
    expect(result.score).toBeLessThanOrEqual(100);
  });

  it('score is never negative', () => {
    const result = deriveSignalClarity(makeStack(0), [], 0, false);
    expect(result.score).toBeGreaterThanOrEqual(0);
  });

  it('returns top-2 limiters', () => {
    const result = deriveSignalClarity(makeStack(80), [], 0, false);
    expect(result.limiters.length).toBeLessThanOrEqual(2);
  });

  it('returns named band for each score range', () => {
    expect(deriveSignalClarity(makeStack(90), [makeCompound()], 10, true).band).toBe('High Clarity');
    expect(deriveSignalClarity(makeStack(0), [], 0, false).band).toBe('Unclear Signal');
  });
});
