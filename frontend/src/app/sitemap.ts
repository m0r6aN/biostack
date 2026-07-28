import type { MetadataRoute } from 'next';
import { absoluteSiteUrl } from '@/lib/site';

export default function sitemap(): MetadataRoute.Sitemap {
  return [
    '',
    '/start',
    '/providers',
    '/knowledge',
    '/how-it-works',
    '/safety',
    '/pricing',
    '/faq',
    '/tools',
    '/tools/analyzer',
    '/tools/reconstitution-calculator',
    '/tools/volume-calculator',
    '/tools/unit-converter',
  ].map((path) => ({
    url: absoluteSiteUrl(path || '/'),
    lastModified: new Date(),
  }));
}
