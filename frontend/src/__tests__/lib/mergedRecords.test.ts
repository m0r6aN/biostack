import { describe, expect, it } from 'vitest';
import nextConfig from '../../../next.config';
import { MERGED_RECORDS, RETIRED_SLUGS, mergedRecordRedirects } from '@/lib/mergedRecords';
import { compoundSitemapEntries } from '@/lib/sitemapEntries';

describe('merged duplicate records', () => {
  it('retires only testolone into vosilasarm; creatine-monohydrate stays distinct (D-D)', () => {
    expect(MERGED_RECORDS.map((m) => [m.from, m.to])).toEqual([['testolone', 'vosilasarm']]);
    expect(RETIRED_SLUGS.has('creatine-monohydrate')).toBe(false);
  });

  it('permanently redirects the old dossier, claim, short-link, embed and vs URLs', () => {
    const r = mergedRecordRedirects();
    const dest = (source: string) => r.find((x) => x.source === source)?.destination;
    expect(r.every((x) => x.permanent)).toBe(true);
    expect(dest('/knowledge/testolone')).toBe('/knowledge/vosilasarm');
    expect(dest('/knowledge/testolone/:path*')).toBe('/knowledge/vosilasarm/:path*');
    expect(dest('/knowledge/testolone/claims/rad140-record-safe')).toBe('/knowledge/vosilasarm/claims/testolone-safe');
    expect(dest('/embed/verdict/rad140-record-muscle')).toBe('/embed/verdict/testolone-muscle');
    expect(dest('/vs/testolone/:other')).toBe('/vs/vosilasarm/:other');
    expect(dest('/vs/:other/testolone')).toBe('/vs/:other/vosilasarm');
    expect(dest('/c/rad140-record-legal-supplement')).toBe('/c/testolone-legal-supplement');
    expect(r.some((x) => x.source.includes('creatine'))).toBe(false);
    expect(dest('/embed/vs/:other/testolone')).toBe('/embed/vs/:other/vosilasarm');
  });

  it('lists specific claim redirects before the catch-all for the same slug', () => {
    const sources = mergedRecordRedirects().map((x) => x.source);
    expect(sources.indexOf('/knowledge/testolone/claims/rad140-record-muscle')).toBeLessThan(sources.indexOf('/knowledge/testolone/:path*'));
  });

  it('is wired into next.config redirects', async () => {
    const all = await nextConfig.redirects!();
    for (const r of mergedRecordRedirects()) expect(all).toContainEqual(r);
    expect(all).toContainEqual({ source: '/calculators', destination: '/tools', permanent: true });
  });

  it('keeps retired slugs out of the sitemap even if the API still returns a stale row', () => {
    const entries = compoundSitemapEntries([
      { canonicalName: 'Testolone' },
      { canonicalName: 'Creatine monohydrate' },
      { canonicalName: 'Vosilasarm' },
      { canonicalName: 'Creatine' },
    ]);
    const paths = entries.map((e) => new URL(e.url).pathname);
    expect(paths).toEqual(['/knowledge/creatine-monohydrate', '/knowledge/vosilasarm', '/knowledge/creatine']);
    for (const slug of RETIRED_SLUGS) expect(paths).not.toContain(`/knowledge/${slug}`);
  });
});
