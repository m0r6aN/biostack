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
    eyebrow: 'Stack Check',
    title: 'No stack items added yet.',
    subtitle: 'Paste what you take to start the analysis.',
    tone: 'dim',
  },
  context_established: {
    eyebrow: 'Stack Check',
    title: 'First item recognized.',
    subtitle: 'Add one more item to look for overlap or conflict.',
    tone: 'positive',
  },
  relationship_unavailable: {
    eyebrow: 'Finding Preview',
    title: 'Need one more item for a stack finding.',
    subtitle: 'Overlap and conflict checks start once at least two items are present.',
    tone: 'dim',
  },
  relationship_detected: {
    eyebrow: 'Finding Preview',
    title: 'Possible overlap found.',
    subtitle: 'BioStack found a believable relationship in the items you entered.',
    tone: 'positive',
  },
  no_relationship_detected: {
    eyebrow: 'Finding Preview',
    title: 'No major overlap found.',
    subtitle: 'Nothing strong surfaced from the current stack at this confidence threshold.',
    tone: 'neutral',
  },
  map_expanding: {
    eyebrow: 'Stack Check',
    title: 'Stack mapped.',
    subtitle: 'More items give BioStack more context for findings and scenarios.',
    tone: 'positive',
  },
  ready_for_persistence: {
    eyebrow: 'Tracked Stack',
    title: 'Your stack is ready to save.',
    subtitle: 'Save it to reopen this snapshot and track changes over time.',
    tone: 'positive',
  },
  inputs_recovered: {
    eyebrow: 'Tracked Stack',
    title: 'Stack inputs recovered.',
    subtitle: 'Your staged stack is still here.',
    tone: 'positive',
  },
  profile_not_instantiated: {
    eyebrow: 'Profile Setup',
    title: 'Profile not yet created.',
    subtitle: 'Create a profile to attach this staged stack.',
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
