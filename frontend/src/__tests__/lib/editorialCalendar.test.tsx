import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { biggestHypeGap, mythKicker, MYTH_SCHEDULE, publishedMyths, receiptsForMonth, todayKey, todaysClaim } from '@/lib/editorialCalendar';
import { publishedLibrary, type LibraryClaim } from '@/lib/claimLibrary';
import { GrowthLists } from '@/components/growth/GrowthLists';
import type { KnowledgeEntry, KnowledgePublicClaim } from '@/lib/types';

function claim(id: string, o: Partial<KnowledgePublicClaim> = {}): KnowledgePublicClaim {
  return {
    id, statement: `claim ${id}`, population: '', outcome: '', tier: 'Unsupported', verdict: 'THIN', qualifier: '', note: '',
    hypeBand: 'High', hypeMethodNote: '', sources: [], version: 1, verifiedAt: '2026-10-01T00:00:00Z', recheckDueAt: '2027-04-01T00:00:00Z',
    publishedAt: '2026-10-02T00:00:00Z', published: true, status: 'published', reviewers: [], changelog: [], ...o,
  } as KnowledgePublicClaim;
}
const lib = (claims: KnowledgePublicClaim[]): LibraryClaim[] =>
  publishedLibrary([{ canonicalName: 'BPC-157', details: { publicClaims: claims } } as unknown as KnowledgeEntry], new Date('2026-11-20T12:00:00Z'));

describe('editorial calendar (America/New_York)', () => {
  it("rotates today's claim at 6:00 AM ET", () => {
    expect(todayKey(new Date('2026-10-24T09:59:00Z'))).toBe('2026-10-23'); // 5:59 AM EDT
    expect(todayKey(new Date('2026-10-24T10:00:00Z'))).toBe('2026-10-24'); // 6:00 AM EDT
    expect(todayKey(new Date('2026-11-05T10:30:00Z'))).toBe('2026-11-04'); // 5:30 AM EST
  });

  it("picks today's claim deterministically from published, fresh claims only", () => {
    const l = lib([claim('a'), claim('b'), claim('c', { published: false, verdict: 'NOT YET GRADED' }), claim('d', { recheckDueAt: '2026-11-01T00:00:00Z' })]);
    expect(l.map(x => x.claim.id)).toEqual(['a', 'b']);
    expect(todaysClaim(l, '2026-11-20')?.claim.id).toBe(todaysClaim(l, '2026-11-20')?.claim.id);
    expect(['a', 'b']).toContain(todaysClaim(l, '2026-11-20')?.claim.id);
    expect(todaysClaim([], '2026-11-20')).toBeNull();
  });

  it('ranks the Hype Gap list by band', () => {
    const l = lib([claim('low', { hypeBand: 'Low' }), claim('ext', { hypeBand: 'Extreme' }), claim('none', { hypeBand: '' })]);
    expect(biggestHypeGap(l).map(c => c.claim.id)).toEqual(['ext', 'low']);
  });

  it('publishes a myth only after its Tuesday 9:00 AM ET slot and only with a published claim', () => {
    const slot = MYTH_SCHEDULE[0];
    const l = lib([claim(slot.claimId, { verdict: 'MYTH' })]);
    expect(publishedMyths(l, new Date('2026-10-27T12:59:00Z'))).toHaveLength(0); // 8:59 AM EDT
    expect(publishedMyths(l, new Date('2026-10-27T13:00:00Z'))).toHaveLength(1);
    expect(publishedMyths(lib([claim(slot.claimId, { published: false, verdict: 'NOT YET GRADED' })]), new Date('2026-12-01'))).toHaveLength(0);
    expect(MYTH_SCHEDULE.every(s => /T09:00:00-0[45]:00$/.test(s.publishAt))).toBe(true);
    expect(mythKicker('MYTH')).toBe('MYTH OF THE WEEK');
    expect(mythKicker('THIN')).toBe('MYTH OF THE WEEK? NOT QUITE.');
  });

  it('collects a month of published verdict changes for the Receipts drop', () => {
    const l = lib([claim('a', { changelog: [{ version: 1, kind: 'initial', verdict: 'THIN', reason: 'First verdict.', publishedAt: '2026-11-03T15:00:00Z', reviewers: [] }] })]);
    expect(receiptsForMonth(l, '2026-11')).toHaveLength(1);
    expect(receiptsForMonth(l, '2026-10')).toHaveLength(0);
  });
});

describe('GrowthLists', () => {
  it('renders only blocks that have published data, with the method note and no gamification', () => {
    render(<GrowthLists feed={{ today: null, myth: null, trending: [], headToHead: [],
      hypeGap: [{ compound: 'BPC-157', slug: 'bpc-157', id: 'x', statement: 'Heals everything', verdict: 'ANIMAL ONLY', hypeBand: 'Extreme', href: '/k' }] }} />);
    expect(screen.queryByTestId('todays-claim')).toBeNull();
    expect(screen.queryByTestId('trending-list')).toBeNull();
    expect(screen.getByTestId('hype-gap-list')).toHaveTextContent('It measures talk, not use.');
    expect(document.body.textContent).not.toMatch(/streak|points|badge|leaderboard/i);
  });
});
