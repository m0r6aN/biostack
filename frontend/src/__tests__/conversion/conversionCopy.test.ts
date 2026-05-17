import { describe, expect, it } from 'vitest';

const ALL_NEW_COPY = [
  // LockedTierCard
  'See how your protocol fits together',
  "Score your active stack, surface synergies and conflicts, and run counterfactual scenarios — all included in Operator.",
  'Protocol simulation unlocks with Operator',
  "Model compound timing across phases, visualize your protocol's structure, and see how your stack is projected to play out over time.",
  // UpgradeBanner
  'Pattern memory, drift analysis, and sequence intelligence unlock with Commander',
  // Billing — Observer
  'Observer includes up to 8 active compounds. Existing data stays available if a paid plan ends.',
  // Billing — Operator card
  'See how your compounds interact — score your protocol, identify synergies and conflicts, and model what changes with counterfactual scenarios. Removes the active compound limit.',
  'Stack score across all compounds',
  'Synergy and conflict surface',
  'Counterfactual scenarios',
  'No compound cap',
  // Billing — Commander card
  'Track how your protocols evolve — detect trends and drift, predict what comes next, and get structured reviews across all your protocol runs.',
  'Trend and drift detection',
  'Sequence expectation modeling',
  'Structured protocol reviews',
  'Cross-run comparison',
  'Pattern intelligence',
  // Analyzer
  'Track whether these patterns hold',
  "Save this stack as a protocol and check in over time to see whether the synergies and conflicts playing out now actually hold.",
  // InteractionIntelligenceCard CTA
  'Tracking this protocol over time will show whether these interaction patterns hold.',
  'Start tracking',
];

const BANNED = [
  'you should',
  'dosage',
  'diagnosis',
  'recommend',
  ' take ',
  'medical advice',
  'clinical guidance',
  'dose',
];

describe('conversion copy safety', () => {
  it('contains no banned medical or recommendation language', () => {
    for (const line of ALL_NEW_COPY) {
      for (const phrase of BANNED) {
        expect(line.toLowerCase(), `"${line}" contains banned phrase "${phrase}"`).not.toContain(phrase.toLowerCase());
      }
    }
  });

  it('free-tier compound limit references 8 not 5 in copy that mentions compounds', () => {
    const limitLines = ALL_NEW_COPY.filter((l) => /\d+ active compound/.test(l));
    for (const line of limitLines) {
      expect(line, `"${line}" still references old limit 5`).not.toMatch(/\b5\b/);
    }
  });
});
