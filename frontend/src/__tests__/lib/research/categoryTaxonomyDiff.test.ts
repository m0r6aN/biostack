import { diffCategoryTaxonomyAuditEntry, hasCategoryTaxonomyAuditDiff } from '@/lib/research/categoryTaxonomyDiff';
import type { ResearchCategoryTaxonomyAuditEntry } from '@/lib/research/types';

describe('categoryTaxonomyDiff', () => {
  it('detects taxonomy changes and migration impact deltas', () => {
    const diff = diffCategoryTaxonomyAuditEntry({
      entryId: 'audit-1',
      action: 'apply-migration-fixup',
      createdAtUtc: '2026-05-10T12:00:00Z',
      taxonomyVersion: '1.0.0',
      summary: 'Applied migration.',
      beforeTaxonomy: {
        taxonomyVersion: '1.0.0',
        updatedAtUtc: '2026-05-10T11:00:00Z',
        categories: [
          { name: 'Nootropics', aliases: ['nootropic'] },
          { name: 'Aging Support', aliases: ['anti-aging'] },
          { name: 'Recovery', aliases: ['repair'] },
        ],
      },
      afterTaxonomy: {
        taxonomyVersion: '1.0.0',
        updatedAtUtc: '2026-05-10T12:00:00Z',
        categories: [
          { name: 'Nootropics', aliases: ['nootropic', 'cognitive support'] },
          { name: 'Anti-Aging', aliases: ['anti-aging'], deprecated: true, replacedBy: 'Longevity' },
          { name: 'Longevity', aliases: [] },
        ],
      },
      beforeMigrationReport: {
        generatedAtUtc: '2026-05-10T11:00:00Z',
        taxonomyVersion: '1.0.0',
        counts: { requestFilesScanned: 1, requestFindings: 2, taskArtifactsScanned: 1, taskItemFindings: 2, resolvedTaskItemFindings: 1, totalFindings: 5 },
        deprecatedCategories: [],
        findings: [],
      },
      afterMigrationReport: {
        generatedAtUtc: '2026-05-10T12:00:00Z',
        taxonomyVersion: '1.0.0',
        counts: { requestFilesScanned: 1, requestFindings: 1, taskArtifactsScanned: 1, taskItemFindings: 0, resolvedTaskItemFindings: 0, totalFindings: 1 },
        deprecatedCategories: [],
        findings: [],
      },
    } satisfies ResearchCategoryTaxonomyAuditEntry);

    expect(diff.added).toEqual(['Longevity']);
    expect(diff.removed).toEqual(['Recovery']);
    expect(diff.renamed).toEqual([{ before: 'Aging Support', after: 'Anti-Aging' }]);
    expect(diff.aliasChanged).toEqual(['Nootropics']);
    expect(diff.migrationDelta).toEqual({
      totalFindings: -4,
      requestFindings: -1,
      taskItemFindings: -2,
      resolvedTaskItemFindings: -1,
    });
    expect(hasCategoryTaxonomyAuditDiff(diff)).toBe(true);
  });
});