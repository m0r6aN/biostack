import type { MetadataRoute } from 'next';

export default function robots(): MetadataRoute.Robots {
  return {
    rules: [
      {
        userAgent: '*',
        allow: ['/', '/start', '/map', '/providers', '/calculators', '/how-it-works', '/safety', '/pricing', '/faq', '/tools'],
        disallow: ['/protocol-console', '/mission-control', '/profiles', '/compounds', '/checkins', '/timeline', '/knowledge', '/admin'],
      },
    ],
    sitemap: 'https://biostack.app/sitemap.xml',
  };
}
