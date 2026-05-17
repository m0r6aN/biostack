export const helpTips = {
  evidenceTier:
    "A research quality rating showing how well a compound's effects are supported by clinical or scientific evidence.",
  synergy:
    'Compounds that may support related outcomes through different biological mechanisms.',
  redundancy:
    'Compounds that may share similar biological mechanisms, which can reduce the value of stacking them together.',
  interference:
    "Compounds whose mechanisms may counteract or complicate each other's intended effects.",
  communitySignal:
    'A pattern reported by users in research communities, not yet verified by clinical trials.',
  reviewRequired:
    'This relationship has been flagged by BioStack for human review before treating it as reliable.',
  counterfactual:
    'A "what if" scenario estimating how your stack score would change if one compound were removed.',
  pathwayOverlap:
    'Two compounds may act on the same biological pathway, which can compound or complicate their combined effects.',
  mechanisticEvidence:
    'The mechanism of action is biologically understood, but direct human trial data is limited or absent.',
} as const;

export type HelpTipKey = keyof typeof helpTips;
