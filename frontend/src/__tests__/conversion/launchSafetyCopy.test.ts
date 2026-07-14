import { pricingTiers } from '@/lib/marketing';
import { describe, expect, it } from 'vitest';

// Strings that BioStack must not show as user-visible labels on the
// launch-risk surfaces touched by PR 1. The list is intentionally narrow:
// it covers prescriptive product framing only. Safety-boundary disclaimers
// (e.g. "Not medical advice", "BioStack does not recommend starting,
// stopping, combining, or dosing any substance") are protective phrasing
// and stay verbatim — this guard does NOT lint them.
const BANNED_PRESCRIPTIVE = [
  'What to take',
  'optimize over time',
  'Why this is better',
  'Best swap',
  'Best removal',
  'AI-assisted',
  'predict what comes next',
  'should take',
  'start taking',
  'stop taking',
  'safe to combine',
  'recommended dose',
  'optimal dose',
];

// User-visible copy lifted verbatim from the touched surfaces in PR 1.
// This is intentionally a curated allow-list of rendered strings, not a
// codebase-wide grep — that is the agreed scope.
const TOUCHED_SURFACE_COPY = [
  // LandingHero
  "What you're taking. How it's structured.",
  "See what it's doing.",
  'Start with clarity. Then track, compare, and observe changes over time.',
  // ProtocolAnalyzerExperience
  'Operator and Commander members can paste, upload, scan, or link a protocol for structural scoring and observational alternative scenarios.',
  'Compare alternatives',
  'Alternative scenarios',
  'BioStack is comparing other arrangements that reach the same goal with less overlap on the internal model.',
  'Remove-one scenario',
  'What-if comparison',
  'Original vs BioStack alternative',
  'BioStack alternative protocol',
  'No alternative scored above the current stack on the internal model.',
  'Goal-aware alternative',
  'Goal-aware alternative needs more context.',
  'Add profile details or unlock full analysis to compare alternative scenarios for this goal.',
  // Analyzer score-band labels
  'Few overlaps detected and clear attribution across compounds.',
  'Workable structure with some redundancy or attribution gaps.',
  'Multiple overlaps or weak attribution issues detected in the parsed stack.',
  // CompoundIntelligenceCard
  'Profile Context',
  'Reference Data',
  'Published reference range (literature)',
  'Reference only. Published ranges are not BioStack recommendations.',
  // Billing — Commander
  'Track how your protocols evolve — detect trends and drift, anticipate the next phase from prior runs, and get structured reviews across all your protocol runs.',
  'Pattern intelligence',
  // InteractionIntelligenceCard
  'Remove-one scenario',
  'What-if comparison',
  'internal score delta',
  'closer goal alignment',
  'clearer signal',
];

describe('PR 1 launch-risk surfaces — banned prescriptive copy', () => {
  it('no banned prescriptive label appears in the touched-surface copy list', () => {
    for (const line of TOUCHED_SURFACE_COPY) {
      for (const banned of BANNED_PRESCRIPTIVE) {
        expect(
          line.toLowerCase(),
          `"${line}" contains banned prescriptive phrase "${banned}"`,
        ).not.toContain(banned.toLowerCase());
      }
    }
  });

  it('Commander tier highlights replace the retired AI/side-effect/predict copy', () => {
    const commander = pricingTiers.find((t) => t.name === 'Commander');
    expect(commander).toBeDefined();
    const allCommanderCopy = [
      commander!.description,
      commander!.detail,
      ...commander!.highlights,
    ];
    for (const line of allCommanderCopy) {
      expect(line.toLowerCase(), `"${line}" still contains "AI-assisted"`).not.toContain('ai-assisted');
      expect(line.toLowerCase(), `"${line}" still contains "predict what comes next"`).not.toContain('predict what comes next');
      expect(line.toLowerCase(), `"${line}" still contains "side-effect"`).not.toContain('side-effect');
      expect(line.toLowerCase(), `"${line}" still contains "pattern optimization"`).not.toContain('pattern optimization');
    }
    // And the new strings are present.
    expect(commander!.highlights).toContain('Protocol review across run history');
    expect(commander!.highlights).toContain('Protocol drift snapshots');
    expect(commander!.description).toBe('Longitudinal Intelligence.');
  });

  it('Commander tier highlights do not contain prescriptive recommendation verbs', () => {
    const commander = pricingTiers.find((t) => t.name === 'Commander');
    for (const line of commander!.highlights) {
      expect(line.toLowerCase()).not.toMatch(/\brecommend(s|ed|ation|ations)?\b/);
      expect(line.toLowerCase()).not.toMatch(/\bprescrib(e|es|ed|ing|tion)\b/);
    }
  });

  it('Operator tier copy still passes the same guard (regression-proof)', () => {
    const operator = pricingTiers.find((t) => t.name === 'Operator');
    const lines = [operator!.description, operator!.detail, ...operator!.highlights];
    for (const line of lines) {
      for (const banned of BANNED_PRESCRIPTIVE) {
        expect(line.toLowerCase()).not.toContain(banned.toLowerCase());
      }
    }
  });
});
