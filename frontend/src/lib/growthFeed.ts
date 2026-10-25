import { fetchAllEntries, publishedLibrary, type LibraryClaim } from '@/lib/claimLibrary';
import { biggestHypeGap, publishedMyths, todayKey, todaysClaim } from '@/lib/editorialCalendar';
import { fetchTrending } from '@/lib/trending';
import { CURATED_PAIRS, vsPath } from '@/lib/vsPairs';
import { compoundSlug } from '@/lib/compoundSlug';
import type { ClaimVerdict } from '@/lib/types';

export interface FeedClaim { compound: string; slug: string; id: string; statement: string; verdict: ClaimVerdict; hypeBand: string; href: string }
export interface GrowthFeed {
  today: (FeedClaim & { permalink: string }) | null;
  myth: (FeedClaim & { mythHref: string }) | null;
  hypeGap: FeedClaim[];
  trending: Array<{ slug: string; name: string; href: string }>;
  headToHead: Array<{ href: string; label: string }>;
}

function toFeed(c: LibraryClaim): FeedClaim {
  return {
    compound: c.entry.canonicalName, slug: c.slug, id: c.claim.id, statement: c.claim.statement,
    verdict: c.claim.verdict, hypeBand: c.claim.hypeBand, href: `/knowledge/${c.slug}/claims/${c.claim.id}`,
  };
}

/** Everything the homepage and /knowledge lists need, from published fresh claims only. */
export async function buildGrowthFeed(now: Date = new Date()): Promise<GrowthFeed> {
  const [entries, trending] = await Promise.all([fetchAllEntries(), fetchTrending()]);
  const library = publishedLibrary(entries, now);
  const day = todayKey(now);
  const today = todaysClaim(library, day);
  const myth = publishedMyths(library, now)[0] ?? null;
  const names = new Map(entries.map(e => [compoundSlug(e.canonicalName), e.canonicalName]));
  return {
    today: today ? { ...toFeed(today), permalink: `/today/${day}` } : null,
    myth: myth ? { ...toFeed(myth.hit), mythHref: `/myths/${myth.slot.slug}` } : null,
    hypeGap: biggestHypeGap(library).map(toFeed),
    trending: trending.filter(t => names.has(t.slug)).map(t => ({ slug: t.slug, name: names.get(t.slug)!, href: `/knowledge/${t.slug}` })),
    headToHead: CURATED_PAIRS.slice(0, 3).map(([a, b]) => ({ href: vsPath(a, b), label: `${names.get(a) ?? a} vs ${names.get(b) ?? b}` })),
  };
}
