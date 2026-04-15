import { describe, expect, it } from 'vitest';
import { getEarnedSuggestion } from '@/lib/earnedSuggestions';
import type { Day7Review, InteractionFlag } from '@/lib/types';

describe('earnedSuggestions', () => {
  it('chooses tighten_stack over clarify_signal when both could apply', () => {
    const suggestion = getEarnedSuggestion({
      day7Review: weakEarnedReview,
      overlaps: [overlapFlag],
      stackShape: { activeInputCount: 4, categoryCounts: { recovery: 4 } },
    });

    expect(suggestion?.type).toBe('tighten_stack');
    expect(suggestion?.reasoning).toBe('Based on overlapping inputs and a weak or unclear 7-day review.');
  });

  it('returns clarify_signal for an unclear or weak Day-7 review', () => {
    const suggestion = getEarnedSuggestion({
      day7Review: weakEarnedReview,
      overlaps: [],
      stackShape: { activeInputCount: 1, categoryCounts: { recovery: 1 } },
    });

    expect(suggestion?.type).toBe('clarify_signal');
    expect(suggestion?.reasoning).toMatch(/7-day review/i);
  });

  it('returns balance_stack only when narrowly justified by current stack shape', () => {
    const suggestion = getEarnedSuggestion({
      day7Review: clearEarnedReview,
      overlaps: [],
      stackShape: { activeInputCount: 5, categoryCounts: { recovery: 4, energy: 1 } },
    });

    expect(suggestion?.type).toBe('balance_stack');
    expect(suggestion?.reasoning).toBe('Based on your current stack shape.');
  });

  it('does not return balance_stack for a smaller or less concentrated stack', () => {
    const suggestion = getEarnedSuggestion({
      day7Review: clearEarnedReview,
      overlaps: [],
      stackShape: { activeInputCount: 4, categoryCounts: { recovery: 3, energy: 1 } },
    });

    expect(suggestion).toBeNull();
  });

  it('returns null when nothing is truly earned', () => {
    const suggestion = getEarnedSuggestion({
      day7Review: pendingReview,
      overlaps: [],
      stackShape: { activeInputCount: 2, categoryCounts: { recovery: 1, energy: 1 } },
    });

    expect(suggestion).toBeNull();
  });

  it('returns exactly one suggestion max', () => {
    const suggestion = getEarnedSuggestion({
      day7Review: weakEarnedReview,
      overlaps: [overlapFlag],
      stackShape: { activeInputCount: 5, categoryCounts: { recovery: 5 } },
    });

    expect(Array.isArray(suggestion)).toBe(false);
    expect(suggestion).toMatchObject({ type: 'tighten_stack' });
  });
});

const pendingReview: Day7Review = {
  isEarned: false,
  coveredDays: 2,
  requiredDays: 5,
  sleepTrend: 'insufficient_data',
  energyTrend: 'insufficient_data',
  recoveryTrend: 'insufficient_data',
  trendSummary: 'Not enough check-ins yet to form a 7-day review.',
  signalStrength: 'weak',
  alignmentWithExpected: 'unclear',
  nextStep: 'track_longer',
  confidenceNote: 'Record at least 5 check-ins across a 7-day window before reviewing patterns.',
};

const weakEarnedReview: Day7Review = {
  isEarned: true,
  coveredDays: 5,
  requiredDays: 5,
  sleepTrend: 'flat',
  energyTrend: 'improving',
  recoveryTrend: 'declining',
  trendSummary: 'Recent check-ins show a mixed pattern without one dominant direction.',
  signalStrength: 'weak',
  alignmentWithExpected: 'unclear',
  nextStep: 'track_longer',
  confidenceNote: 'This review compares simple direction across recent check-ins and stays observational.',
};

const clearEarnedReview: Day7Review = {
  isEarned: true,
  coveredDays: 5,
  requiredDays: 5,
  sleepTrend: 'improving',
  energyTrend: 'improving',
  recoveryTrend: 'flat',
  trendSummary: 'Sleep, energy, and recovery are mostly moving upward across recent check-ins.',
  signalStrength: 'clear',
  alignmentWithExpected: 'yes',
  nextStep: 'continue',
  confidenceNote: 'This review compares simple direction across recent check-ins and stays observational.',
};

const overlapFlag: InteractionFlag = {
  compoundNames: ['BPC-157', 'TB-500'],
  overlapType: 'Potential redundancy',
  pathwayTag: 'recovery',
  description: 'Shared recovery pathway.',
  evidenceConfidence: 'medium',
};
