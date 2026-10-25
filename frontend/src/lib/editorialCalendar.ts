import { isPublished } from '@/lib/claimVerdicts';
import type { LibraryClaim } from '@/lib/claimLibrary';

/**
 * Growth Phase 5 (05-myth-of-the-week.md §2). All slots are America/New_York.
 * Everything here selects from published, fresh claim records only: no copy is written
 * here, and a slot with no published claim renders nothing.
 */
export const TIME_ZONE = 'America/New_York';

function etParts(date: Date) {
  const f = new Intl.DateTimeFormat('en-CA', { timeZone: TIME_ZONE, year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', hourCycle: 'h23', weekday: 'short' });
  const p = Object.fromEntries(f.formatToParts(date).map(x => [x.type, x.value]));
  return { ymd: `${p.year}-${p.month}-${p.day}`, ym: `${p.year}-${p.month}`, hour: Number(p.hour), weekday: p.weekday as string };
}

/** "Today's claim" day key: rotates at 6:00 AM ET. */
export function todayKey(now: Date = new Date()): string {
  return etParts(new Date(now.getTime() - 6 * 3600_000)).ymd;
}

export function monthKey(now: Date = new Date()): string {
  return etParts(now).ym;
}

function hash(s: string): number {
  let h = 2166136261;
  for (let i = 0; i < s.length; i++) h = Math.imul(h ^ s.charCodeAt(i), 16777619);
  return h >>> 0;
}

/** Deterministic daily pick from the published library (sorted by id, so it's stable per day). */
export function todaysClaim(library: LibraryClaim[], dayKey: string): LibraryClaim | null {
  const pool = library.filter(c => isPublished(c.claim)).sort((a, b) => a.claim.id.localeCompare(b.claim.id));
  if (pool.length === 0) return null;
  return pool[hash(dayKey) % pool.length];
}

const BAND_RANK: Record<string, number> = { Extreme: 4, High: 3, Moderate: 2, Low: 1 };

/** "Biggest Hype Gap this week": top published claims by band (recomputed Mondays 5:00 AM ET via revalidate). */
export function biggestHypeGap(library: LibraryClaim[], n = 5): LibraryClaim[] {
  return library
    .filter(c => isPublished(c.claim) && BAND_RANK[c.claim.hypeBand])
    .sort((a, b) => (BAND_RANK[b.claim.hypeBand] - BAND_RANK[a.claim.hypeBand])
      || (b.claim.publishedAt ?? '').localeCompare(a.claim.publishedAt ?? ''))
    .slice(0, n);
}

export const HYPE_LIST_METHOD_NOTE =
  'Ranked by Hype Gap band among graded claims. Hype is scored by our editors from public search and social signals. It measures talk, not use.';
export const TRENDING_LABEL = 'Most looked-up on BioStack this week';

/**
 * Myth of the Week slots: Tuesday 9:00 AM ET. Topics are the approved launch set
 * (05 §4); each points at a claim record, and the page shows whatever verdict the
 * council published, MYTH or not. A topic whose claim isn't in the library or isn't
 * published yet is skipped.
 */
export interface MythSlot { slug: string; claimId: string; publishAt: string }
export const MYTH_SCHEDULE: MythSlot[] = [
  { slug: 'the-fda-approved-bpc-157', claimId: 'bpc-157-fda-approved', publishAt: '2026-10-27T09:00:00-04:00' },
  { slug: 'gray-market-retatrutide-is-the-trial-drug', claimId: 'retatrutide-gray-market-same-drug', publishAt: '2026-11-03T09:00:00-05:00' },
  { slug: 'tb-500-is-thymosin-beta-4', claimId: 'tb-500-is-thymosin-beta-4', publishAt: '2026-11-10T09:00:00-05:00' },
  { slug: 'mk-677-has-no-side-effects', claimId: 'ibutamoren-no-side-effects', publishAt: '2026-11-17T09:00:00-05:00' },
];

export interface PublishedMyth { slot: MythSlot; hit: LibraryClaim }

export function publishedMyths(library: LibraryClaim[], now: Date = new Date()): PublishedMyth[] {
  return MYTH_SCHEDULE
    .filter(s => new Date(s.publishAt).getTime() <= now.getTime())
    .flatMap(slot => {
      const hit = library.find(c => c.claim.id === slot.claimId && isPublished(c.claim));
      return hit ? [{ slot, hit }] : [];
    })
    .sort((a, b) => b.slot.publishAt.localeCompare(a.slot.publishAt));
}

export function mythKicker(verdict: string): string {
  return verdict === 'MYTH' ? 'MYTH OF THE WEEK' : 'MYTH OF THE WEEK? NOT QUITE.';
}

/** Receipts drop: every published verdict change in a month (from claim changelogs). */
export function receiptsForMonth(library: LibraryClaim[], ym: string) {
  return library.flatMap(c => c.claim.changelog
    .filter(ch => ch.publishedAt && etParts(new Date(ch.publishedAt)).ym === ym)
    .map(ch => ({ ...c, change: ch })))
    .sort((a, b) => (a.change.publishedAt ?? '').localeCompare(b.change.publishedAt ?? ''));
}
