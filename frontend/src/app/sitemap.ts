import type { MetadataRoute } from 'next';

const baseUrl = 'https://biostack.app';

export default function sitemap(): MetadataRoute.Sitemap {
  return [
    '',
    '/start',
    '/map',
    '/providers',
    '/how-it-works',
    '/safety',
    '/pricing',
    '/faq',
    '/terms',
    '/privacy',
    '/tools',
    '/tools/reconstitution-calculator',
    '/tools/volume-calculator',
    '/tools/unit-converter',
  ].map((path) => ({
    url: `${baseUrl}${path}`,
    lastModified: new Date(),
  }));
}
