import {
  getOnboardingSystemStatus,
  getRelationshipAvailabilityStatus,
  getSystemStatusDescriptor,
} from './systemStatus';

export type OnboardingIntelligenceStage = 'empty' | 'context' | 'relationship' | 'pattern';

export type OnboardingRelationshipType = 'synergy' | 'overlap' | 'support' | 'none';

export interface OnboardingRelationship {
  type: OnboardingRelationshipType;
  label: string;
}

export interface OnboardingRelationshipCandidate extends OnboardingRelationship {
  compounds?: string[];
  detail?: string;
}

export interface OnboardingIntelligenceState {
  count: number;
  stage: OnboardingIntelligenceStage;
  relationship: OnboardingRelationship | null;
  isRelationshipAllowed: boolean;
}

export interface OnboardingKnowledgeContext {
  classification?: string;
  evidenceTier?: string;
  mechanismSummary?: string;
  notes?: string;
  frequency?: string;
}

export interface OnboardingPanelRelationshipGroup {
  type: 'Context' | 'Overlap' | 'Synergy' | 'Support';
  label: string;
  detail: string;
}

export interface OnboardingPanelContent {
  subtext: string;
  insightLabel: string;
  summary: string;
  stats: Array<[string, string]>;
  stageLabels: string[];
  relationshipGroups: OnboardingPanelRelationshipGroup[];
  insights: string[];
  nextAction: string;
}

export interface OnboardingRewardContent {
  eyebrow: string;
  title: string;
  body: string;
  status: string;
  rows: Array<[string, string]>;
}

const relationshipPriority: OnboardingRelationshipType[] = ['synergy', 'overlap', 'support', 'none'];

function normalizeCompoundName(name: string) {
  return name.trim().replace(/\s+/g, ' ');
}

export function getValidOnboardingCompounds(compounds: string[]) {
  const seen = new Set<string>();
  const validCompounds: string[] = [];

  for (const compound of compounds) {
    const normalized = normalizeCompoundName(compound);
    if (!normalized) {
      continue;
    }

    const key = normalized.toLowerCase();
    if (seen.has(key)) {
      continue;
    }

    seen.add(key);
    validCompounds.push(normalized);
  }

  return validCompounds;
}

function pickRelationship(candidates: OnboardingRelationshipCandidate[]) {
  for (const type of relationshipPriority) {
    const match = candidates.find((candidate) => candidate.type === type);
    if (match) {
      return {
        type: match.type,
        label: match.label,
      };
    }
  }

  return null;
}

export function getOnboardingIntelligenceState(
  compounds: string[],
  relationshipCandidates: OnboardingRelationshipCandidate[] = []
): OnboardingIntelligenceState {
  const count = getValidOnboardingCompounds(compounds).length;

  if (count === 0) {
    return {
      count,
      stage: 'empty',
      relationship: null,
      isRelationshipAllowed: false,
    };
  }

  if (count === 1) {
    return {
      count,
      stage: 'context',
      relationship: null,
      isRelationshipAllowed: false,
    };
  }

  return {
    count,
    stage: count === 2 ? 'relationship' : 'pattern',
    relationship: pickRelationship(relationshipCandidates) ?? {
      type: 'none',
      label: 'No relationship detected',
    },
    isRelationshipAllowed: true,
  };
}

export function getRelationshipCandidatesFromOverlaps(
  overlaps: Array<{ compoundNames: string[]; pathwayTag: string; description: string }>
): OnboardingRelationshipCandidate[] {
  return overlaps.map((overlap) => ({
    type: 'overlap',
    label: overlap.compoundNames.join(' + '),
    compounds: overlap.compoundNames,
    detail: `${overlap.pathwayTag}: ${overlap.description}`,
  }));
}

export function getOnboardingPanelContent(
  state: OnboardingIntelligenceState,
  compounds: string[],
  options: {
    isCheckingRelationships?: boolean;
    knowledgeContext?: OnboardingKnowledgeContext | null;
    relationshipCandidates?: OnboardingRelationshipCandidate[];
  } = {}
): OnboardingPanelContent {
  const validCompounds = getValidOnboardingCompounds(compounds);
  const compoundList = validCompounds.join(', ');
  const firstCompound = validCompounds[0] ?? '';
  const knowledgeContext = options.knowledgeContext;
  const evidenceTier = knowledgeContext?.evidenceTier || 'Needs review';
  const classification = knowledgeContext?.classification || 'Manual entry';
  const mechanism =
    knowledgeContext?.mechanismSummary ||
    'Context is available. Relationship analysis requires another input.';
  const typicalPattern = knowledgeContext?.frequency || 'Add schedule details later when ready.';
  const relationshipDetail = options.relationshipCandidates?.find(
    (candidate) => candidate.type === state.relationship?.type && candidate.label === state.relationship?.label
  )?.detail;
  const status = getOnboardingSystemStatus(state);
  const relationshipStatus = getRelationshipAvailabilityStatus(state);

  if (state.stage === 'empty') {
    return {
      subtext: `${status.title} ${status.subtitle}`,
      stageLabels: ['0 items', 'Nothing added yet', 'Checks start at 2 items'],
      stats: [
        ['Items', 'None'],
        ['Context', 'Add anything you take'],
        ['Overlap check', 'Starts at 2 items'],
      ],
      relationshipGroups: [],
      insightLabel: status.title,
      summary: status.subtitle ?? status.title,
      insights: [relationshipStatus.title],
      nextAction: 'Type anything you take.',
    };
  }

  if (state.stage === 'context') {
    return {
      subtext: `${status.title} ${relationshipStatus.title}`,
      stageLabels: ['1 item', 'Context established', 'Add one more for checks'],
      stats: [
        ['Item', firstCompound],
        ['Evidence tier', evidenceTier],
        ['Overlap check', 'Starts at 2 items'],
      ],
      relationshipGroups: [
        {
          type: 'Context',
          label: classification,
          detail: mechanism,
        },
        {
          type: 'Context',
          label: 'Typical pattern',
          detail: typicalPattern,
        },
      ],
      insightLabel: status.title,
      summary: relationshipStatus.title,
      insights: [
        status.subtitle ?? status.title,
        relationshipStatus.subtitle ?? relationshipStatus.title,
      ],
      nextAction: 'Add one more item to unlock relationship analysis.',
    };
  }

  if (options.isCheckingRelationships) {
    const checkingStatus = getSystemStatusDescriptor('relationship_unavailable');
    return {
      subtext: 'Relationship analysis active.',
      stageLabels: [`${state.count} items`, 'Relationship eligible', 'Checking'],
      stats: [
        ['Compounds', compoundList],
        ['Relationship check', 'Checking'],
        ['Timeline', 'Ready after save'],
      ],
      relationshipGroups: [
        {
          type: 'Context',
          label: 'Checking relationships',
          detail: 'The check is limited to selected inputs.',
        },
      ],
      insightLabel: 'Relationship pending.',
      summary: 'Selected inputs queued.',
      insights: [checkingStatus.subtitle ?? checkingStatus.title],
      nextAction: 'Wait for the relationship check.',
    };
  }

  if (state.relationship?.type === 'none') {
    return {
      subtext:
        state.stage === 'pattern'
          ? `${status.title} ${getSystemStatusDescriptor('no_relationship_detected').title}`
          : status.title,
      stageLabels: [`${state.count} items`, 'Relationship eligible', 'No relationship detected'],
      stats: [
        ['Compounds', compoundList],
        ['Relationship check', 'No relationship detected'],
        ['Timeline', 'Ready after save'],
      ],
      relationshipGroups: [
        {
          type: 'Context',
          label: 'No relationship detected',
          detail: status.subtitle ?? 'No known relationship found for this set.',
        },
      ],
      insightLabel: status.title,
      summary: status.subtitle ?? status.title,
      insights: [
        state.stage === 'pattern' ? 'Additional inputs included.' : 'No relationship claim emitted.',
        getSystemStatusDescriptor('ready_for_persistence').title,
      ],
    nextAction: state.stage === 'pattern' ? 'Save the list or adjust it.' : 'Save the list or add another item.',
    };
  }

  const relationshipType = state.relationship?.type === 'synergy'
    ? 'Synergy'
    : state.relationship?.type === 'support'
      ? 'Support'
      : 'Overlap';

  return {
    subtext:
      state.stage === 'pattern'
        ? `${status.title} ${getSystemStatusDescriptor('relationship_detected').title}`
        : status.title,
    stageLabels: [`${state.count} items`, 'Relationship eligible', `${relationshipType} detected`],
    stats: [
      ['Items', compoundList],
      ['Relationship check', `${relationshipType} detected`],
      ['Timeline', 'Ready after save'],
    ],
    relationshipGroups: [
      {
        type: relationshipType,
        label: state.relationship?.label ?? 'Relationship detected',
        detail: relationshipDetail ?? 'Detected from selected inputs.',
      },
    ],
    insightLabel: status.title,
    summary: status.subtitle ?? status.title,
    insights: [
      'One earned relationship outcome emitted.',
      state.stage === 'pattern' ? 'Additional inputs included.' : getSystemStatusDescriptor('ready_for_persistence').title,
    ],
    nextAction: state.stage === 'pattern' ? 'Save the list or adjust it.' : 'Save the list or add another item.',
  };
}

export function getOnboardingRewardContent(
  state: OnboardingIntelligenceState,
  compounds: string[],
  selectedGoalLabels: string[],
  options: { isGoalsStep?: boolean; isCheckingRelationships?: boolean } = {}
): OnboardingRewardContent {
  const validCompounds = getValidOnboardingCompounds(compounds);
  const hasInputs = validCompounds.length > 0;
  const compoundList = hasInputs ? validCompounds.join(', ') : 'No inputs';

  if (options.isGoalsStep) {
    return {
      eyebrow: 'Goal State',
      title: selectedGoalLabels.length > 0 ? 'Goals selected.' : 'Choose what matters first.',
      body:
        selectedGoalLabels.length > 0
          ? 'We will use these priorities when you build your list.'
          : 'Pick one or skip for now. You can change this later.',
      status: selectedGoalLabels.length > 0 ? 'Selected' : 'Optional',
      rows: [
        ['Goals', selectedGoalLabels.length > 0 ? selectedGoalLabels.join(', ') : 'None yet'],
        ['List', hasInputs ? compoundList : 'Next step'],
        ['Next', 'Build your list'],
      ],
    };
  }

  if (state.stage === 'empty') {
    const emptyStatus = getSystemStatusDescriptor('empty');
    return {
      eyebrow: emptyStatus.eyebrow ?? 'Input State',
      title: emptyStatus.title,
      body: emptyStatus.subtitle ?? '',
      status: 'Empty',
      rows: [
        ['List', 'None'],
        ['Unlocked', 'Nothing yet'],
        ['Next', 'Add anything you take'],
      ],
    };
  }

  if (state.stage === 'context') {
    const contextStatus = getSystemStatusDescriptor('context_established');
    const unavailableStatus = getSystemStatusDescriptor('relationship_unavailable');
    return {
      eyebrow: contextStatus.eyebrow ?? 'Input State',
      title: contextStatus.title,
      body: unavailableStatus.title,
      status: 'Context',
      rows: [
        ['List', compoundList],
        ['Unlocked', 'Item context'],
        ['Next', 'Add one more'],
      ],
    };
  }

  if (options.isCheckingRelationships) {
    return {
      eyebrow: 'Relationship Check',
      title: 'Relationship analysis active.',
      body: 'Selected inputs queued.',
      status: 'Checking',
      rows: [
        ['List', compoundList],
        ['Unlocked', 'Overlap check'],
        ['Next', 'Wait for result'],
      ],
    };
  }

  const rewardStatus = getOnboardingSystemStatus(state);
  return {
    eyebrow: rewardStatus.eyebrow ?? (state.stage === 'pattern' ? 'Pattern State' : 'Relationship State'),
    title: rewardStatus.title,
    body: rewardStatus.subtitle ?? rewardStatus.title,
    status:
      state.relationship?.type === 'none'
        ? 'No relationship'
        : state.relationship?.type ?? 'Detected',
    rows: [
      ['List', compoundList],
      ['Unlocked', state.stage === 'pattern' ? 'Pattern map' : 'Overlap check'],
      ['Next', state.stage === 'pattern' ? 'Save list' : 'Save list'],
    ],
  };
}
