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
      title: 'First item recognized.',
      subtitle: 'Add one more item to look for overlap or conflict.',
      tone: 'positive',
    });
  });

  it('maps onboarding context state to the first recognized item state', () => {
    const status = getOnboardingSystemStatus({
      count: 1,
      stage: 'context',
      relationship: null,
      isRelationshipAllowed: false,
    });

    expect(status.title).toBe('First item recognized.');
  });

  it('maps unavailable relationship state separately from context', () => {
    const status = getRelationshipAvailabilityStatus({
      count: 1,
      stage: 'context',
      relationship: null,
      isRelationshipAllowed: false,
    });

    expect(status.title).toBe('Need one more item for a stack finding.');
  });

  it('maps detected and no-relationship states', () => {
    expect(
      getOnboardingSystemStatus({
        count: 2,
        stage: 'relationship',
        relationship: { type: 'overlap', label: 'BPC-157 + TB-500' },
        isRelationshipAllowed: true,
      }).title
    ).toBe('Possible overlap found.');

    expect(
      getOnboardingSystemStatus({
        count: 2,
        stage: 'relationship',
        relationship: { type: 'none', label: 'No relationship detected' },
        isRelationshipAllowed: true,
      }).title
    ).toBe('No major overlap found.');
  });

  it('maps 3+ onboarding state to map expanding', () => {
    expect(
      getOnboardingSystemStatus({
        count: 3,
        stage: 'pattern',
        relationship: { type: 'none', label: 'No relationship detected' },
        isRelationshipAllowed: true,
      }).title
    ).toBe('Stack mapped.');
  });

  it('returns profile continuation statuses only when inputs are recovered', () => {
    expect(getProfilesContinuationStatuses(false)).toBeNull();
    expect(getProfilesContinuationStatuses(true)).toMatchObject({
      recovered: { title: 'Stack inputs recovered.' },
      profile: { title: 'Profile not yet created.' },
      persistence: { title: 'Your stack is ready to save.' },
    });
  });
});
