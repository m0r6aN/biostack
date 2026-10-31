import type { MetadataRoute } from 'next';
import { getApiBaseUrl } from '@/lib/apiBase';
import { compoundSitemapEntries, type SitemapCompound } from '@/lib/sitemapEntries';
import { absoluteSiteUrl } from '@/lib/site';
import { CURATED_PAIRS, vsPath } from '@/lib/vsPairs';

// Generated per request (cached for an hour) rather than at build time: the production build
// runs without API access, which froze the sitemap at the 15 static routes and listed no
// dossiers (growth spec O3).
export const dynamic = 'force-dynamic';

const STATIC_PATHS = [
  '',
  '/start',
  '/providers',
  '/knowledge',
  '/knowledge/methodology',
  '/knowledge/insights/fda-pcac-2026-peptide-vote',
  '/how-it-works',
  '/safety',
  '/pricing',
  '/faq',
  '/support',
  '/tools',
  '/tools/analyzer',
  '/tools/reconstitution-calculator',
  '/tools/volume-calculator',
  '/tools/unit-converter',
];

async function fetchCompounds(): Promise<SitemapCompound[]> {
  try {
    const response = await fetch(`${getApiBaseUrl()}/api/v1/knowledge/compounds`, {
      next: { revalidate: 3600 },
      signal: AbortSignal.timeout(8000),
    });

    if (!response.ok) {
      return [];
    }

    return (await response.json()) as SitemapCompound[];
  } catch {
    // Best-effort; static routes always ship.
    return [];
  }
}

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const compounds = await fetchCompounds();

  return [
    ...STATIC_PATHS.map((path) => ({ url: absoluteSiteUrl(path || '/') })),
    ...compoundSitemapEntries(compounds),
    // Only curated /vs pairs are indexable (growth Phase 4).
    { url: absoluteSiteUrl('/vs') },
    ...CURATED_PAIRS.map(([a, b]) => ({ url: absoluteSiteUrl(vsPath(a, b)) })),
  ];
}
