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

  if (state.stage === 'empty') {
    return {
      subtext: 'No inputs detected. Add an item to establish context.',
      stageLabels: ['0 items', 'No context', 'Relationship locked'],
      stats: [
        ['Compounds', 'None'],
        ['Context', 'Unavailable'],
        ['Relationship map', 'Locked'],
      ],
      relationshipGroups: [],
      insightLabel: 'No inputs detected',
      summary: 'Add an item to establish context.',
      insights: ['No intelligence is shown until an input exists.'],
      nextAction: 'Add one item.',
    };
  }

  if (state.stage === 'context') {
    return {
      subtext: 'Context established. Relationship analysis unavailable - requires additional inputs.',
      stageLabels: ['1 item', 'Context established', 'Relationship locked'],
      stats: [
        ['Compound', firstCompound],
        ['Evidence tier', evidenceTier],
        ['Relationship map', 'Requires 2 inputs'],
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
      insightLabel: 'Context established',
      summary: `${firstCompound} is the only active onboarding input.`,
      insights: [
        'Identity and evidence context are available.',
        'Relationship analysis remains locked with one input.',
      ],
      nextAction: 'Add one more item to unlock relationship analysis.',
    };
  }

  if (options.isCheckingRelationships) {
    return {
      subtext: 'Relationship analysis active. Checking only selected inputs.',
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
      insightLabel: 'Relationship pending',
      summary: 'Selected inputs are being checked.',
      insights: ['No relationship claim is shown until the check completes.'],
      nextAction: 'Wait for the relationship check.',
    };
  }

  if (state.relationship?.type === 'none') {
    return {
      subtext:
        state.stage === 'pattern'
          ? 'Map expanding. No relationship detected for the current inputs.'
          : 'Relationship analysis complete. No relationship detected.',
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
          detail: 'No known overlap, support, or synergy was found for this exact set.',
        },
      ],
      insightLabel: 'No relationship detected',
      summary: 'No known relationship was found for this exact set.',
      insights: [
        'No relationship claim was forced.',
        state.stage === 'pattern' ? 'Additional inputs expand the map without adding unsupported claims.' : 'Add goals or save the current inputs.',
      ],
      nextAction: state.stage === 'pattern' ? 'Save the inputs or adjust the set.' : 'Pick a goal or save the inputs.',
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
        ? 'Map expanding. One earned relationship is shown.'
        : 'Relationship detected. One earned outcome is shown.',
    stageLabels: [`${state.count} items`, 'Relationship eligible', `${relationshipType} detected`],
    stats: [
      ['Compounds', compoundList],
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
    insightLabel: `${relationshipType} detected`,
    summary: 'This relationship comes from selected inputs.',
    insights: [
      'Only one earned relationship outcome is shown.',
      state.stage === 'pattern' ? 'Additional inputs expand the map without adding unsupported claims.' : 'Save or add another input to expand the map.',
    ],
    nextAction: state.stage === 'pattern' ? 'Save the inputs or adjust the set.' : 'Pick a goal or add another item.',
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
      eyebrow: 'Priority Tuning',
      title: selectedGoalLabels.length > 0 ? 'Protocol aimed.' : 'Aim pending.',
      body:
        selectedGoalLabels.length > 0
          ? 'Selected outcomes are staged for profile creation.'
          : 'Profile creation can continue without a selected goal.',
      status: selectedGoalLabels.length > 0 ? 'Tuned' : 'Untuned',
      rows: [
        ['Input', compoundList],
        ['Unlocked', hasInputs ? 'Profile attachment' : 'Profile creation'],
        ['Next', 'Save profile'],
      ],
    };
  }

  if (state.stage === 'empty') {
    return {
      eyebrow: 'Input State',
      title: 'No inputs detected.',
      body: 'Add an item to establish context.',
      status: 'Empty',
      rows: [
        ['Input', 'None'],
        ['Unlocked', 'Nothing yet'],
        ['Next', 'Add one item'],
      ],
    };
  }

  if (state.stage === 'context') {
    return {
      eyebrow: 'Input State',
      title: 'Context established.',
      body: 'Relationship analysis unavailable - requires additional inputs.',
      status: 'Context',
      rows: [
        ['Input', compoundList],
        ['Unlocked', 'Identity context'],
        ['Next', 'Add one more'],
      ],
    };
  }

  if (options.isCheckingRelationships) {
    return {
      eyebrow: 'Relationship Check',
      title: 'Relationship analysis active.',
      body: 'Checking only selected inputs.',
      status: 'Checking',
      rows: [
        ['Input', compoundList],
        ['Unlocked', 'Relationship check'],
        ['Next', 'Wait for result'],
      ],
    };
  }

  return {
    eyebrow: state.stage === 'pattern' ? 'Pattern State' : 'Relationship State',
    title:
      state.relationship?.type === 'none'
        ? 'No relationship detected.'
        : 'Relationship detected.',
    body:
      state.relationship?.type === 'none'
        ? 'No known relationship was found for this exact set.'
        : 'One earned relationship outcome is available.',
    status:
      state.relationship?.type === 'none'
        ? 'No relationship'
        : state.relationship?.type ?? 'Detected',
    rows: [
      ['Input', compoundList],
      ['Unlocked', state.stage === 'pattern' ? 'Pattern map' : 'Relationship check'],
      ['Next', state.stage === 'pattern' ? 'Save inputs' : 'Aim protocol'],
    ],
  };
}
