import { describe, expect, it } from 'vitest';
import { buildDay7Review } from '@/lib/day7Review';
import { CheckIn } from '@/lib/types';

describe('buildDay7Review', () => {
  it('returns an improving Day-7 review when the recent window moves upward', () => {
    const review = buildDay7Review(createCheckIns({
      sleep: [4, 4, 5, 6, 7],
      energy: [4, 5, 5, 7, 8],
      recovery: [5, 5, 6, 7, 8],
    }));

    expect(review.isEarned).toBe(true);
    expect(review.sleepTrend).toBe('improving');
    expect(review.energyTrend).toBe('improving');
    expect(review.recoveryTrend).toBe('improving');
    expect(review.signalStrength).toBe('clear');
    expect(review.nextStep).toBe('continue');
    expect(review.alignmentWithExpected).toBe('unclear');
  });

  it('returns an insufficient-data state before the 5-check-in threshold', () => {
    const review = buildDay7Review(createCheckIns({
      sleep: [5, 6, 7, 8],
      energy: [5, 6, 7, 8],
      recovery: [5, 6, 7, 8],
    }));

    expect(review.isEarned).toBe(false);
    expect(review.sleepTrend).toBe('insufficient_data');
    expect(review.signalStrength).toBe('weak');
    expect(review.nextStep).toBe('track_longer');
  });

  it('returns a mixed weak signal when directions conflict', () => {
    const review = buildDay7Review(createCheckIns({
      sleep: [4, 5, 6, 7, 8],
      energy: [8, 7, 6, 5, 4],
      recovery: [6, 6, 6, 6, 6],
    }));

    expect(review.isEarned).toBe(true);
    expect(review.sleepTrend).toBe('improving');
    expect(review.energyTrend).toBe('declining');
    expect(review.recoveryTrend).toBe('flat');
    expect(review.signalStrength).toBe('weak');
    expect(review.trendSummary).toMatch(/mixed/i);
  });
});

function createCheckIns(values: {
  sleep: number[];
  energy: number[];
  recovery: number[];
}): CheckIn[] {
  return values.sleep.map((sleepQuality, index) => ({
    id: `check-in-${index}`,
    personId: 'profile-1',
    protocolRunId: null,
    date: `2026-04-0${index + 1}T00:00:00.000Z`,
    weight: 0,
    sleepQuality,
    energy: values.energy[index],
    appetite: 5,
    recovery: values.recovery[index],
    giSymptoms: '',
    mood: '',
    notes: '',
  }));
}
