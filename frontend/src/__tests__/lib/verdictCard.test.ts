import { describe, expect, it } from 'vitest';
import { buildCardModel, CardBuildError, cardImageUrl } from '@/lib/verdictCard';
import { lintCopy, NO_DOSING_RESPONSE } from '@/lib/copyLint';
import type { KnowledgeEntry, KnowledgePublicClaim } from '@/lib/types';

function claim(o: Partial<KnowledgePublicClaim> = {}): KnowledgePublicClaim {
  return {
    id: 'retatrutide-weight-loss', statement: 'Retatrutide melts fat', population: 'adults with obesity', outcome: 'weight loss',
    tier: 'Established', verdict: 'PROVEN IN HUMANS', qualifier: 'In adults with obesity, for weight loss, under trial conditions.',
    note: 'Phase 3 trials found weight loss vs placebo.', hypeBand: 'High', hypeMethodNote: '', sources: [], version: 2,
    verifiedAt: '2026-10-20T00:00:00Z', recheckDueAt: '2027-04-20T00:00:00Z', publishedAt: '2026-11-02T00:00:00Z',
    published: true, status: 'published', reviewedBy: 'ai-council', reviewers: ['a/x', 'b/y'], changelog: [], ...o,
  } as KnowledgePublicClaim;
}
const entry = (c: KnowledgePublicClaim[]) => ({ canonicalName: 'Retatrutide', details: { publicClaims: c } }) as unknown as KnowledgeEntry;

describe('verdict card model', () => {
  it('fills every slot from the published claim', () => {
    const m = buildCardModel({ kind: 'compound', entry: entry([claim()]) });
    expect(m.verdict).toBe('PROVEN IN HUMANS');
    expect(m.qualifier).toBe('In adults with obesity, for weight loss, under trial conditions.');
    expect(m.claimLine).toBe('“Retatrutide melts fat”');
    expect(m.hypeLine).toBe('Hype Gap: HIGH');
    expect(m.footer).toBe('Evidence as of Nov 2, 2026 · biostack.cc/knowledge/retatrutide');
    expect(m.footer.length).toBeLessThanOrEqual(64);
    expect(m.tagline).toBe('Settle it.');
    expect(m.alt).toContain('PROVEN IN HUMANS');
  });

  it('fails a PROVEN card without its qualifier', () => {
    expect(() => buildCardModel({ kind: 'compound', entry: entry([claim({ qualifier: '' })]) })).toThrow(CardBuildError);
  });

  it('fails a card whose slots carry a dose', () => {
    expect(() => buildCardModel({ kind: 'claim', entry: entry([claim({ note: 'Lost weight on 12 mg weekly.' })]) })).toThrow(CardBuildError);
  });

  it('shows NOT YET GRADED, no qualifier, no hype and no date for an unpublished claim', () => {
    const m = buildCardModel({ kind: 'claim', entry: entry([claim({ published: false, status: 'pending-council', publishedAt: null })]) });
    expect(m.verdict).toBe('NOT YET GRADED');
    expect(m.qualifier).toBeNull();
    expect(m.hypeLine).toBeNull();
    expect(m.receiptLine).toBeNull();
    expect(m.footer).not.toContain('Evidence as of');
  });

  it('uses the short claim URL when the long one overflows, and marks an old version superseded', () => {
    const m = buildCardModel({ kind: 'claim', entry: entry([claim()]), claim: claim(), requestedVersion: 1 });
    expect(m.footer).toBe('Evidence as of Nov 2, 2026 · biostack.cc/c/retatrutide-weight-loss');
    expect(m.superseded).toBe(true);
    expect(m.kicker).toBe('THE CLAIM');
  });

  it('cache key carries version and format', () => {
    expect(cardImageUrl('compound', 'retatrutide', 3)).toBe('/og/compound/retatrutide?v=3');
    expect(cardImageUrl('claim', 'x', 3, 'story')).toBe('/og/claim/x?v=3&format=story');
  });
});

describe('banned-phrase lint', () => {
  it('flags dosing, endorsement, commerce and people-targeting copy', () => {
    expect(lintCopy('Start at 2 mg per week').map(h => h.rule)).toEqual(expect.arrayContaining(['3a:dose-number', '3a:dosing', '3a:schedule']));
    expect(lintCopy('We recommend this one').map(h => h.rule)).toContain('3b:endorsement');
    expect(lintCopy('Where to buy it').map(h => h.rule)).toContain('3c:commerce');
    expect(lintCopy('As @someguy said').map(h => h.rule)).toContain('3d:people');
    expect(lintCopy('Clinically proven').length).toBeGreaterThan(0);
  });

  it('exempts quoted claim statements and the reviewed refusal string', () => {
    expect(lintCopy('“BPC-157 is proven safe”: MYTH')).toEqual([]);
    expect(lintCopy(NO_DOSING_RESPONSE)).toEqual([]);
    expect(lintCopy('Receipts, not reels.')).toEqual([]);
  });
});
