import {
  buildFilteredTaxonomyAuditExport,
  buildFocusedTaxonomyAuditExport,
  matchesTaxonomyAuditSearch,
} from '@/lib/research/categoryTaxonomyTimeline';
import type { ResearchCategoryTaxonomyAuditEntry } from '@/lib/research/types';

const entry: ResearchCategoryTaxonomyAuditEntry = {
  entryId: 'audit-1',
  action: 'save-taxonomy',
  createdAtUtc: '2026-05-10T12:00:00Z',
  taxonomyVersion: '1.0.0',
  summary: 'Saved taxonomy with 3 categories.',
  beforeTaxonomy: {
    taxonomyVersion: '1.0.0',
    updatedAtUtc: '2026-05-10T11:00:00Z',
    categories: [{ name: 'Nootropics', aliases: ['nootropic'] }],
  },
  afterTaxonomy: {
    taxonomyVersion: '1.0.0',
    updatedAtUtc: '2026-05-10T12:00:00Z',
    categories: [
      { name: 'Nootropics', aliases: ['nootropic', 'cognitive support'] },
      { name: 'Longevity', aliases: [] },
    ],
  },
};

describe('categoryTaxonomyTimeline', () => {
  it('matches search queries against diff content and builds export payloads', () => {
    expect(matchesTaxonomyAuditSearch(entry, 'Longevity')).toBe(true);
    expect(matchesTaxonomyAuditSearch(entry, 'Aliases changed')).toBe(true);
    expect(matchesTaxonomyAuditSearch(entry, 'Migration Applies')).toBe(false);

    const filtered = buildFilteredTaxonomyAuditExport([entry], 'save-taxonomy', 'Longevity');
    const focused = buildFocusedTaxonomyAuditExport(entry);

    expect(filtered.filters).toEqual({ action: 'save-taxonomy', searchQuery: 'Longevity' });
    expect(filtered.entries[0]).toEqual(expect.objectContaining({ entryId: 'audit-1', diff: expect.objectContaining({ added: ['Longevity'] }) }));
    expect(focused).toEqual(expect.objectContaining({
      entry: expect.objectContaining({ entryId: 'audit-1' }),
      diff: expect.objectContaining({ added: ['Longevity'], aliasChanged: ['Nootropics'] }),
    }));
  });
});