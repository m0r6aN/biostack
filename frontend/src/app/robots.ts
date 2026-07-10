import type { MetadataRoute } from 'next';

export default function robots(): MetadataRoute.Robots {
  return {
    rules: [
      {
        userAgent: '*',
        allow: ['/', '/start', '/providers', '/tools', '/knowledge', '/how-it-works', '/safety', '/pricing', '/faq'],
        disallow: ['/protocol-console', '/mission-control', '/profiles', '/compounds', '/billing', '/checkins', '/timeline', '/admin'],
      },
    ],
    sitemap: 'https://biostack.app/sitemap.xml',
  };
}
