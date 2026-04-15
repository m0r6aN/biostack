import {
  getOnboardingSystemStatus,
  getProfilesContinuationStatuses,
  getRelationshipAvailabilityStatus,
  getSystemStatusDescriptor,
} from '@/lib/systemStatus';
import { describe, expect, it } from 'vitest';

describe('systemStatus', () => {
  it('returns centralized descriptors by key', () => {
    expect(getSystemStatusDescriptor('context_established')).toMatchObject({
      title: 'Context established.',
      subtitle: 'Identity context available.',
      tone: 'positive',
    });
  });

  it('maps onboarding context state to context established', () => {
    const status = getOnboardingSystemStatus({
      count: 1,
      stage: 'context',
      relationship: null,
      isRelationshipAllowed: false,
    });

    expect(status.title).toBe('Context established.');
  });

  it('maps unavailable relationship state separately from context', () => {
    const status = getRelationshipAvailabilityStatus({
      count: 1,
      stage: 'context',
      relationship: null,
      isRelationshipAllowed: false,
    });

    expect(status.title).toBe('Relationship analysis unavailable.');
  });

  it('maps detected and no-relationship states', () => {
    expect(
      getOnboardingSystemStatus({
        count: 2,
        stage: 'relationship',
        relationship: { type: 'overlap', label: 'BPC-157 + TB-500' },
        isRelationshipAllowed: true,
      }).title
    ).toBe('Relationship detected.');

    expect(
      getOnboardingSystemStatus({
        count: 2,
        stage: 'relationship',
        relationship: { type: 'none', label: 'No relationship detected' },
        isRelationshipAllowed: true,
      }).title
    ).toBe('No relationship detected.');
  });

  it('maps 3+ onboarding state to map expanding', () => {
    expect(
      getOnboardingSystemStatus({
        count: 3,
        stage: 'pattern',
        relationship: { type: 'none', label: 'No relationship detected' },
        isRelationshipAllowed: true,
      }).title
    ).toBe('Map expanding.');
  });

  it('returns profile continuation statuses only when inputs are recovered', () => {
    expect(getProfilesContinuationStatuses(false)).toBeNull();
    expect(getProfilesContinuationStatuses(true)).toMatchObject({
      recovered: { title: 'Inputs recovered.' },
      profile: { title: 'Profile not yet instantiated.' },
      persistence: { title: 'Protocol ready for persistence.' },
    });
  });
});
