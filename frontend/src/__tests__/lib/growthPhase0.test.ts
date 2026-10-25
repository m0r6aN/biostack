import sitemap from '@/app/sitemap';
import { compoundPath, compoundSlug } from '@/lib/compoundSlug';
import { dossierCardUrl, dossierDescription, dossierTitle } from '@/lib/dossierMeta';
import { compoundSitemapEntries } from '@/lib/sitemapEntries';
import { createPublicPageMetadata } from '@/lib/site';
import type { KnowledgeEntry } from '@/lib/types';
import { afterEach, describe, expect, it, vi } from 'vitest';

function entry(overrides: Partial<KnowledgeEntry> = {}): KnowledgeEntry {
  return {
    canonicalName: 'Retatrutide',
    aliases: ['LY3437943'],
    classification: 'Peptide',
    regulatoryStatus: '',
    mechanismSummary: 'Retatrutide is a GIP, GLP-1 and glucagon receptor triple agonist.',
    evidenceTier: 'EarlyEvidence',
    sourceReferences: ['https://www.nejm.org/doi/full/10.1056/NEJMoa2301972'],
    notes: '',
    pathways: [],
    benefits: [],
    pairsWellWith: [],
    avoidWith: [],
    compatibleBlends: [],
    vialCompatibility: '',
    recommendedDosage: '',
    standardDosageRange: '',
    maxReportedDose: '',
    frequency: '',
    preferredTimeOfDay: '',
    weeklyDosageSchedule: [],
    incrementalEscalationSteps: [],
    drugInteractions: [],
    ...overrides,
  } as KnowledgeEntry;
}

describe('compound slugs (must match backend CompoundSlug.ForDossier)', () => {
  it.each([
    ['Retatrutide', 'retatrutide'],
    ['BPC-157', 'bpc-157'],
    ['NAD+', 'nad'],
    ['St John\u2019s wort', 'st-johns-wort'],
    ["Lion's mane", 'lions-mane'],
    ['Creatine monohydrate', 'creatine-monohydrate'],
    ['  IGF-1 LR3 ', 'igf-1-lr3'],
    ['Omega-3 ethyl esters', 'omega-3-ethyl-esters'],
    ['', ''],
  ])('%s -> %s', (name, slug) => {
    expect(compoundSlug(name)).toBe(slug);
  });

  it('builds dossier paths', () => {
    expect(compoundPath('NAD+')).toBe('/knowledge/nad');
  });
});

describe('dossier metadata', () => {
  it('uses a unique per-compound title and grade-led description', () => {
    expect(dossierTitle('Retatrutide')).toBe('Retatrutide: what the evidence actually says | BioStack');
    const description = dossierDescription(entry());
    expect(description.startsWith('BioStack evidence grade: Early Evidence.')).toBe(true);
    expect(description.length).toBeLessThanOrEqual(155);
    expect(dossierDescription(entry({ evidenceTier: 'Unknown' }))).toMatch(/^Not yet graded/);
  });

  it('points og:image at the generated card, versioned by review date', () => {
    const versioned = entry({ details: { lastReviewedAt: '2026-10-14T00:00:00Z' } as KnowledgeEntry['details'] });
    expect(dossierCardUrl(versioned)).toBe('/og/compound/retatrutide?v=2026-10-14');
    expect(dossierCardUrl(entry())).toBe('/og/compound/retatrutide');

    const metadata = createPublicPageMetadata({
      title: 't',
      description: 'd',
      path: '/knowledge/retatrutide',
      image: { url: dossierCardUrl(versioned), width: 1200, height: 630, alt: 'a' },
    });
    expect(metadata.openGraph?.images).toEqual([{ url: '/og/compound/retatrutide?v=2026-10-14', width: 1200, height: 630, alt: 'a' }]);
    expect(metadata.twitter?.images).toEqual(['/og/compound/retatrutide?v=2026-10-14']);
  });
});

describe('sitemap dossier entries', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('lists every compound once by slug, with the review date as lastmod', () => {
    const result = compoundSitemapEntries([
      { canonicalName: 'Retatrutide', details: { lastReviewedAt: '2026-10-14T00:00:00Z' } },
      { canonicalName: 'retatrutide' },
      { canonicalName: 'NAD+' },
      { canonicalName: '  ' },
    ]);
    expect(result.map((e) => e.url)).toEqual(['https://biostack.cc/knowledge/retatrutide', 'https://biostack.cc/knowledge/nad']);
    expect(result[0].lastModified).toEqual(new Date('2026-10-14T00:00:00Z'));
    expect(result[1].lastModified).toBeUndefined();
  });

  it('includes all 158 dossiers when the API answers', async () => {
    const compounds = Array.from({ length: 158 }, (_, i) => ({ canonicalName: `Compound ${i}` }));
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => compounds }));
    const entries = await sitemap();
    expect(entries.filter((e) => new URL(e.url).pathname.startsWith('/knowledge/compound-'))).toHaveLength(158);
  });
});
