import type { OnboardingIntelligenceState } from './onboardingIntelligence';

export type SystemStatusKey =
  | 'empty'
  | 'context_established'
  | 'relationship_unavailable'
  | 'relationship_detected'
  | 'no_relationship_detected'
  | 'map_expanding'
  | 'ready_for_persistence'
  | 'inputs_recovered'
  | 'profile_not_instantiated';

export type SystemStatusTone = 'neutral' | 'positive' | 'dim';

export type SystemStatusDescriptor = {
  eyebrow?: string;
  title: string;
  subtitle?: string;
  tone?: SystemStatusTone;
};

export const SYSTEM_STATUS_COPY: Record<SystemStatusKey, SystemStatusDescriptor> = {
  empty: {
    eyebrow: 'Input State',
    title: 'No inputs detected.',
    subtitle: 'Context not established.',
    tone: 'dim',
  },
  context_established: {
    eyebrow: 'Input State',
    title: 'Context established.',
    subtitle: 'Identity context available.',
    tone: 'positive',
  },
  relationship_unavailable: {
    eyebrow: 'Relationship State',
    title: 'Relationship analysis unavailable.',
    subtitle: 'Requires additional inputs.',
    tone: 'dim',
  },
  relationship_detected: {
    eyebrow: 'Relationship State',
    title: 'Relationship detected.',
    subtitle: 'Derived from selected inputs.',
    tone: 'positive',
  },
  no_relationship_detected: {
    eyebrow: 'Relationship State',
    title: 'No relationship detected.',
    subtitle: 'No known relationship found for this set.',
    tone: 'neutral',
  },
  map_expanding: {
    eyebrow: 'Pattern State',
    title: 'Map expanding.',
    subtitle: 'Additional inputs included.',
    tone: 'positive',
  },
  ready_for_persistence: {
    eyebrow: 'Persistence State',
    title: 'Protocol ready for persistence.',
    subtitle: 'Inputs staged for profile attachment.',
    tone: 'positive',
  },
  inputs_recovered: {
    eyebrow: 'Continuation State',
    title: 'Inputs recovered.',
    subtitle: 'Onboarding inputs remain staged.',
    tone: 'positive',
  },
  profile_not_instantiated: {
    eyebrow: 'Profile State',
    title: 'Profile not yet instantiated.',
    subtitle: 'Create a profile to attach staged inputs.',
    tone: 'neutral',
  },
};

export function getSystemStatusDescriptor(key: SystemStatusKey): SystemStatusDescriptor {
  return SYSTEM_STATUS_COPY[key];
}

export function getOnboardingSystemStatus(state: OnboardingIntelligenceState): SystemStatusDescriptor {
  if (state.stage === 'empty') {
    return getSystemStatusDescriptor('empty');
  }

  if (state.stage === 'context') {
    return getSystemStatusDescriptor('context_established');
  }

  if (state.stage === 'pattern') {
    return getSystemStatusDescriptor('map_expanding');
  }

  if (state.relationship?.type === 'none') {
    return getSystemStatusDescriptor('no_relationship_detected');
  }

  return getSystemStatusDescriptor('relationship_detected');
}

export function getRelationshipAvailabilityStatus(state: OnboardingIntelligenceState): SystemStatusDescriptor {
  if (!state.isRelationshipAllowed) {
    return getSystemStatusDescriptor('relationship_unavailable');
  }

  return getOnboardingSystemStatus(state);
}

export function getProfilesContinuationStatuses(hasRecoveredInputs: boolean) {
  return hasRecoveredInputs
    ? {
        recovered: getSystemStatusDescriptor('inputs_recovered'),
        profile: getSystemStatusDescriptor('profile_not_instantiated'),
        persistence: getSystemStatusDescriptor('ready_for_persistence'),
      }
    : null;
}
