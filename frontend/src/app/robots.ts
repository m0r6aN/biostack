import type { MetadataRoute } from 'next';
import { absoluteSiteUrl } from '@/lib/site';

export default function robots(): MetadataRoute.Robots {
  return {
    rules: [
      {
        userAgent: '*',
        allow: ['/', '/start', '/providers', '/tools', '/knowledge', '/how-it-works', '/safety', '/pricing', '/faq'],
        disallow: ['/protocol-console', '/mission-control', '/profiles', '/compounds', '/billing', '/checkins', '/timeline', '/admin'],
      },
    ],
    sitemap: absoluteSiteUrl('/sitemap.xml'),
  };
}
