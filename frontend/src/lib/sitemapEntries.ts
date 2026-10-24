import type { MetadataRoute } from 'next';
import { compoundPath } from '@/lib/compoundSlug';
import { absoluteSiteUrl } from '@/lib/site';

export type SitemapCompound = {
  canonicalName?: string;
  details?: { lastReviewedAt?: string | null } | null;
};

export function compoundSitemapEntries(entries: SitemapCompound[]): MetadataRoute.Sitemap {
  const seen = new Set<string>();
  const result: MetadataRoute.Sitemap = [];
  for (const entry of entries) {
    const name = entry.canonicalName?.trim();
    if (!name) continue;
    const path = compoundPath(name);
    if (path === '/knowledge/' || seen.has(path)) continue;
    seen.add(path);
    const reviewed = entry.details?.lastReviewedAt ? new Date(entry.details.lastReviewedAt) : null;
    result.push({
      url: absoluteSiteUrl(path),
      // lastmod is the record's review (version) date; omitted when the record has none.
      ...(reviewed && !Number.isNaN(reviewed.getTime()) ? { lastModified: reviewed } : {}),
    });
  }
  return result;
}

