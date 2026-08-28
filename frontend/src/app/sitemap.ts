import type { MetadataRoute } from 'next';
import { getApiBaseUrl } from '@/lib/apiBase';
import { absoluteSiteUrl } from '@/lib/site';

const STATIC_PATHS = [
  '',
  '/start',
  '/providers',
  '/knowledge',
  '/knowledge/methodology',
  '/how-it-works',
  '/safety',
  '/pricing',
  '/faq',
  '/tools',
  '/tools/analyzer',
  '/tools/reconstitution-calculator',
  '/tools/volume-calculator',
  '/tools/unit-converter',
];

async function fetchCompoundPaths(): Promise<string[]> {
  try {
    const response = await fetch(`${getApiBaseUrl()}/api/v1/knowledge/compounds`, {
      next: { revalidate: 3600 },
      signal: AbortSignal.timeout(5000),
    });

    if (!response.ok) {
      return [];
    }

    const entries = (await response.json()) as Array<{ canonicalName?: string }>;

    return entries
      .map((entry) => entry.canonicalName)
      .filter((name): name is string => Boolean(name && name.trim()))
      .map((name) => `/knowledge/${encodeURIComponent(name)}`);
  } catch {
    // Sitemap generation is best-effort; static routes always ship.
    return [];
  }
}

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const compoundPaths = await fetchCompoundPaths();

  return [...STATIC_PATHS, ...compoundPaths].map((path) => ({
    url: absoluteSiteUrl(path || '/'),
    lastModified: new Date(),
  }));
}
