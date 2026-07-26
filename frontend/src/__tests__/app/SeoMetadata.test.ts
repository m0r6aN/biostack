import { metadata as faqMetadata } from '@/app/faq/page';
import { metadata as howItWorksMetadata } from '@/app/how-it-works/page';
import { metadata as knowledgeMetadata } from '@/app/knowledge/layout';
import { metadata as homeMetadata } from '@/app/page';
import { metadata as pricingMetadata } from '@/app/pricing/page';
import { metadata as providersMetadata } from '@/app/providers/page';
import robots from '@/app/robots';
import { metadata as safetyMetadata } from '@/app/safety/page';
import sitemap from '@/app/sitemap';
import { metadata as startMetadata } from '@/app/start/page';
import { metadata as analyzerMetadata } from '@/app/tools/analyzer/page';
import { metadata as toolsMetadata } from '@/app/tools/page';
import { metadata as reconstitutionMetadata } from '@/app/tools/reconstitution-calculator/page';
import { metadata as unitConverterMetadata } from '@/app/tools/unit-converter/page';
import { metadata as volumeCalculatorMetadata } from '@/app/tools/volume-calculator/page';
import {
  ROOT_METADATA,
  SITE_ORIGIN,
  SITE_URL,
  absoluteSiteUrl,
  createPublicPageMetadata,
} from '@/lib/site';
import type { Metadata } from 'next';
import { describe, expect, it } from 'vitest';

const publicPages: Array<{ path: string; metadata: Metadata }> = [
  { path: '/', metadata: homeMetadata },
  { path: '/start', metadata: startMetadata },
  { path: '/providers', metadata: providersMetadata },
  { path: '/knowledge', metadata: knowledgeMetadata },
  { path: '/how-it-works', metadata: howItWorksMetadata },
  { path: '/safety', metadata: safetyMetadata },
  { path: '/pricing', metadata: pricingMetadata },
  { path: '/faq', metadata: faqMetadata },
  { path: '/tools', metadata: toolsMetadata },
  { path: '/tools/analyzer', metadata: analyzerMetadata },
  { path: '/tools/reconstitution-calculator', metadata: reconstitutionMetadata },
  { path: '/tools/volume-calculator', metadata: volumeCalculatorMetadata },
  { path: '/tools/unit-converter', metadata: unitConverterMetadata },
];

describe('public SEO metadata', () => {
  it('uses the live biostack.cc origin as the single metadata base', () => {
    expect(SITE_ORIGIN).toBe('https://biostack.cc');
    expect(SITE_URL.toString()).toBe('https://biostack.cc/');
    expect(ROOT_METADATA.metadataBase?.toString()).toBe(SITE_URL.toString());
    expect(absoluteSiteUrl('/pricing')).toBe('https://biostack.cc/pricing');
  });

  it('publishes the sitemap at the live canonical host', () => {
    const config = robots();

    expect(config.sitemap).toBe('https://biostack.cc/sitemap.xml');
    expect(config.sitemap).not.toContain('biostack.app');
  });

  it('publishes the exact public route set on the live canonical host', () => {
    const entries = sitemap();

    expect(entries.map((entry) => new URL(entry.url).pathname)).toEqual(
      publicPages.map(({ path }) => path),
    );
    expect(entries.every((entry) => new URL(entry.url).origin === SITE_ORIGIN)).toBe(true);
    expect(entries.some((entry) => entry.url.includes('biostack.app'))).toBe(false);
  });

  it.each(publicPages)(
    'exports complete page-specific metadata for $path',
    ({ path, metadata }) => {
      expect(metadata.title).toBeTruthy();
      expect(metadata.description).toBeTruthy();
      expect(metadata.alternates?.canonical).toBe(path);
      expect(metadata.openGraph?.url).toBe(path);
      expect(metadata.openGraph?.title).toBe(metadata.title);
      expect(metadata.openGraph?.description).toBe(metadata.description);
      expect(metadata.openGraph?.siteName).toBe('BioStack');
      expect(metadata.twitter?.card).toBe('summary');
      expect(metadata.twitter?.title).toBe(metadata.title);
      expect(metadata.twitter?.description).toBe(metadata.description);
      expect(new URL(String(metadata.alternates?.canonical), SITE_URL).toString()).toBe(
        absoluteSiteUrl(path),
      );
    },
  );

  it('builds metadata through the shared helper', () => {
    expect(
      createPublicPageMetadata({
        title: 'Pricing | BioStack',
        description: 'Compare BioStack plans.',
        path: '/pricing',
      }).alternates?.canonical,
    ).toBe('/pricing');
  });
});
