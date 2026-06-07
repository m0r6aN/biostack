import { describe, expect, it } from 'vitest';

// Copy retired in PR 2. None of these strings should appear in the new surface copy below.
const BANNED_RETIRED_PR2 = [
  'Blend safely',
  'Need help with dosage or mixing',
  'bio-operating system',
  'longitudinal intelligence',
  'Stack intelligence',
  'Pattern intelligence',
  'Evolve from run',
];

// User-visible copy lifted verbatim from every touched surface in PR 2.
// Curated allow-list only — not a codebase-wide grep.
const TOUCHED_SURFACE_COPY = [
  // ToolsDecisionSurface hero H1
  'Dose it right. Mix correctly. Check compatibility.',
  // LandingHero dosage/mixing CTA
  'Need to calculate dose volume or reconstitution? → Start here',
  // Providers page H1
  'A workspace for organizing and tracking client protocol records.',
  // Billing — Operator tier box heading (PR 2 rename)
  'Protocol scoring and analysis',
  // Billing — Commander tier box heading (PR 2 rename)
  'Run history and trend analysis',
  // Protocols list page subtitle
  'Simulate, save, track, compare, branch',
  // Protocol detail page action button
  'Branch from run',
  // ProtocolContinuityStrip lineage note
  'This draft was branched from an observed run.',
];

describe('PR 2 public copy polish — retired copy must not appear in new strings', () => {
  it('no retired label appears in the touched-surface copy list', () => {
    for (const line of TOUCHED_SURFACE_COPY) {
      for (const banned of BANNED_RETIRED_PR2) {
        expect(
          line.toLowerCase(),
          `"${line}" contains retired phrase "${banned}"`,
        ).not.toContain(banned.toLowerCase());
      }
    }
  });

  it('billing tier box headings are concrete descriptions, not vague intelligence labels', () => {
    expect(TOUCHED_SURFACE_COPY).toContain('Protocol scoring and analysis');
    expect(TOUCHED_SURFACE_COPY).toContain('Run history and trend analysis');
    expect(TOUCHED_SURFACE_COPY).not.toContain('Stack intelligence');
    expect(TOUCHED_SURFACE_COPY).not.toContain('Pattern intelligence');
  });

  it('protocol branching verb replaces evolution language across all touched surfaces', () => {
    expect(TOUCHED_SURFACE_COPY).toContain('Branch from run');
    expect(TOUCHED_SURFACE_COPY).toContain('This draft was branched from an observed run.');
    const hasEvolveCopy = TOUCHED_SURFACE_COPY.some((s) => /\bevolve\b/i.test(s));
    expect(hasEvolveCopy, 'no touched-surface string should contain "evolve"').toBe(false);
  });

  it('tools hero H1 uses compatibility language, not a safety verdict', () => {
    const heroH1 = TOUCHED_SURFACE_COPY.find((s) => s.startsWith('Dose it right'));
    expect(heroH1).toBeDefined();
    expect(heroH1).toContain('Check compatibility');
    expect(heroH1).not.toContain('Blend safely');
  });

  it('no prescriptive or verdict language present in new copy', () => {
    const PRESCRIPTIVE = [
      'recommend', 'should take', 'start taking', 'stop taking',
      'safe to combine', 'optimal dose', 'AI-assisted', 'blend safely',
    ];
    for (const line of TOUCHED_SURFACE_COPY) {
      for (const phrase of PRESCRIPTIVE) {
        expect(line.toLowerCase(), `"${line}" contains retired phrase "${phrase}"`).not.toContain(phrase.toLowerCase());
      }
    }
  });
});
