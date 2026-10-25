import type { MetadataRoute } from 'next';
import { compoundPath, compoundSlug } from '@/lib/compoundSlug';
import { RETIRED_SLUGS } from '@/lib/mergedRecords';
import { absoluteSiteUrl } from '@/lib/site';

export type SitemapCompound = {
  canonicalName?: string;
  details?: {
    lastReviewedAt?: string | null;
    publicClaims?: Array<{ id: string; published: boolean; verdict: string; publishedAt?: string | null }>;
  } | null;
};

export function compoundSitemapEntries(entries: SitemapCompound[]): MetadataRoute.Sitemap {
  const seen = new Set<string>();
  const result: MetadataRoute.Sitemap = [];
  for (const entry of entries) {
    const name = entry.canonicalName?.trim();
    if (!name) continue;
    // A record folded into another redirects; never list its old URL.
    if (RETIRED_SLUGS.has(compoundSlug(name))) continue;
    const path = compoundPath(name);
    if (path === '/knowledge/' || seen.has(path)) continue;
    seen.add(path);
    const reviewed = entry.details?.lastReviewedAt ? new Date(entry.details.lastReviewedAt) : null;
    result.push({
      url: absoluteSiteUrl(path),
      // lastmod is the record's review (version) date; omitted when the record has none.
      ...(reviewed && !Number.isNaN(reviewed.getTime()) ? { lastModified: reviewed } : {}),
    });
    // Phase 3: published claim pages only (ungraded claim pages are noindex).
    for (const claim of entry.details?.publicClaims ?? []) {
      if (!claim.published || claim.verdict === 'NOT YET GRADED') continue;
      const at = claim.publishedAt ? new Date(claim.publishedAt) : null;
      result.push({
        url: absoluteSiteUrl(`${path}/claims/${claim.id}`),
        ...(at && !Number.isNaN(at.getTime()) ? { lastModified: at } : {}),
      });
    }
  }
  return result;
}

