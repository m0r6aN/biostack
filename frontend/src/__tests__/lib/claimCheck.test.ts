import { describe, expect, it } from 'vitest';
import { asksPersonalAdvice, checkClaim, detectCompounds, hasDoseIntent, matchClaims } from '@/lib/claimCheck';
import { claimLibrary } from '@/lib/claimLibrary';
import type { KnowledgeEntry } from '@/lib/types';

const claim = (id: string, statement: string, verdict = 'THIN', outcome = '') => ({
  id, statement, verdict, outcome, population: '', published: true, status: 'published', version: 1,
  publishedAt: '2026-10-01T00:00:00Z', tier: 'Limited', sources: [], changelog: [],
});
const entries = [
  { canonicalName: 'Ibutamoren', aliases: ['MK-677', 'MK677'], details: { publicClaims: [
    claim('ibu-side-effects', 'Ibutamoren has no side effects', 'MYTH', 'adverse events'),
    claim('ibu-muscle', 'Ibutamoren builds muscle in healthy adults', 'THIN', 'lean mass'),
  ] } },
  { canonicalName: 'BPC-157', aliases: ['Body Protection Compound'], details: { publicClaims: [
    claim('bpc-tendon', 'BPC-157 heals tendons in humans', 'ANIMAL ONLY', 'tendon healing'),
  ] } },
] as unknown as KnowledgeEntry[];
const library = claimLibrary(entries);

describe('claim checker (curated library only)', () => {
  it('resolves aliases to the canonical compound', () => {
    expect(detectCompounds('mk-677 has no side effects', entries).map(e => e.canonicalName)).toEqual(['Ibutamoren']);
    expect(detectCompounds('MK677 gains', entries).map(e => e.canonicalName)).toEqual(['Ibutamoren']);
  });

  it('ranks the matching claim first and never crosses compounds', () => {
    const m = matchClaims('MK-677 has no side effects', library, entries);
    expect(m[0].item.claim.id).toBe('ibu-side-effects');
    expect(m.every(x => x.item.entry.canonicalName === 'Ibutamoren')).toBe(true);
    expect(matchClaims('bpc heals tendon injuries', library, entries)[0].item.claim.id).toBe('bpc-tendon');
  });

  it('returns nothing (never a made-up verdict) when the library has no match', () => {
    const r = checkClaim('Semaglutide cures baldness', library, entries);
    expect(r.matches).toEqual([]);
  });

  it('caps results at five', () => {
    const many = claimLibrary([{ canonicalName: 'X-1', aliases: [], details: { publicClaims:
      Array.from({ length: 9 }, (_, i) => claim(`x${i}`, `X-1 improves sleep quality ${i}`)) } }] as unknown as KnowledgeEntry[]);
    expect(matchClaims('X-1 improves sleep', many, entries).length).toBeLessThanOrEqual(5);
  });

  it.each(['how much bpc should I run', 'BPC-157 250mcg twice a day?', 'best dose for mk-677', 'what to stack with bpc', 'mk677 cycle length'])(
    'flags dose intent: %s', q => expect(hasDoseIntent(q)).toBe(true));

  it('does not flag plain claims as dose intent, and flags personal-advice questions', () => {
    expect(hasDoseIntent('MK-677 has no side effects')).toBe(false);
    expect(asksPersonalAdvice('Should I take BPC-157?')).toBe(true);
    expect(asksPersonalAdvice('Is it safe for me with diabetes')).toBe(true);
  });
});
