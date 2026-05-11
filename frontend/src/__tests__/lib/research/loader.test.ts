import { normalizeResearchArtifact } from '@/lib/research/loader';

describe('normalizeResearchArtifact', () => {
  it('converts PascalCase KnowledgeWorker artifacts to camelCase recursively', () => {
    const normalized = normalizeResearchArtifact<{
      safeToApply: boolean;
      refusalReasons: string[];
      previewCounts: { activeRecords: number };
      items: Array<{ reviewDecisionIds: string[] }>;
    }>({
      SafeToApply: true,
      RefusalReasons: [],
      PreviewCounts: { ActiveRecords: 0 },
      Items: [{ ReviewDecisionIds: ['approve-creatine-fixture-001'] }],
    });

    expect(normalized.safeToApply).toBe(true);
    expect(normalized.previewCounts.activeRecords).toBe(0);
    expect(normalized.items[0].reviewDecisionIds).toEqual(['approve-creatine-fixture-001']);
  });

  it('leaves existing camelCase fixture artifacts compatible', () => {
    const normalized = normalizeResearchArtifact<{ safeToApply: boolean }>({ safeToApply: true });
    expect(normalized.safeToApply).toBe(true);
  });
});