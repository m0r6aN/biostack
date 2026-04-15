import {
  getOnboardingIntelligenceState,
  getRelationshipCandidatesFromOverlaps,
} from '@/lib/onboardingIntelligence';
import { describe, expect, it } from 'vitest';

describe('onboardingIntelligence', () => {
  it('returns empty state for 0 inputs', () => {
    expect(getOnboardingIntelligenceState([])).toEqual({
      count: 0,
      stage: 'empty',
      relationship: null,
      isRelationshipAllowed: false,
    });
  });

  it('returns context state for 1 input without relationship access', () => {
    expect(getOnboardingIntelligenceState(['BPC-157'])).toEqual({
      count: 1,
      stage: 'context',
      relationship: null,
      isRelationshipAllowed: false,
    });
  });

  it('returns one prioritized relationship outcome for 2 inputs', () => {
    expect(
      getOnboardingIntelligenceState(
        ['BPC-157', 'TB-500'],
        [
          { type: 'support', label: 'Support relationship' },
          { type: 'overlap', label: 'Overlap relationship' },
          { type: 'synergy', label: 'Synergy relationship' },
        ]
      )
    ).toEqual({
      count: 2,
      stage: 'relationship',
      relationship: {
        type: 'synergy',
        label: 'Synergy relationship',
      },
      isRelationshipAllowed: true,
    });
  });

  it('returns no relationship for 2 inputs without relationship evidence', () => {
    expect(getOnboardingIntelligenceState(['BPC-157', 'NAD+'])).toEqual({
      count: 2,
      stage: 'relationship',
      relationship: {
        type: 'none',
        label: 'No relationship detected',
      },
      isRelationshipAllowed: true,
    });
  });

  it('returns pattern state for 3+ inputs without adding fake insights', () => {
    expect(
      getOnboardingIntelligenceState(
        ['BPC-157', 'TB-500', 'Creatine'],
        [{ type: 'overlap', label: 'BPC-157 + TB-500' }]
      )
    ).toEqual({
      count: 3,
      stage: 'pattern',
      relationship: {
        type: 'overlap',
        label: 'BPC-157 + TB-500',
      },
      isRelationshipAllowed: true,
    });
  });

  it('maps overlap flags into overlap relationship candidates', () => {
    expect(
      getRelationshipCandidatesFromOverlaps([
        {
          compoundNames: ['BPC-157', 'TB-500'],
          pathwayTag: 'tissue-repair',
          description: 'Educational reference only.',
        },
      ])
    ).toEqual([
      {
        type: 'overlap',
        label: 'BPC-157 + TB-500',
        compounds: ['BPC-157', 'TB-500'],
        detail: 'tissue-repair: Educational reference only.',
      },
    ]);
  });
});
