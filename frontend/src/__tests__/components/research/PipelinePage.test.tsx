import { fireEvent, render, screen } from '@testing-library/react';
import PipelinePage from '@/app/admin/research/pipeline/page';

vi.mock('@/lib/apiBase', () => ({ getApiBaseUrl: () => 'http://localhost' }));

vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <div>{title}</div>,
}));

vi.mock('@/components/ui/GlassCard', () => ({
  GlassCard: ({ children, className }: { children: React.ReactNode; className?: string }) => (
    <div className={className}>{children}</div>
  ),
}));

vi.mock('@/lib/research/loader', () => ({
  fetchPromotionManifest: vi.fn().mockResolvedValue({
    manifestVersion: '1.0.0', generatedAtUtc: '',
    counts: { totalDrafts: 1, blocked: 0, reviewRequired: 0, candidatesForPromotion: 1 },
    blocked: [], reviewRequired: [],
    candidatesForPromotion: [{
      name: 'Creatine', classification: 'Supplement', readiness: 'candidate-for-promotion',
      overallEvidenceTier: 'Moderate', completeness: 'partial', reviewQueueItemCount: 0,
      reviewDecisionIds: ['approve-creatine-fixture-001'], blockers: [], qualityFlags: [], requiredNextActions: [],
    }],
  }),
  fetchImportPreview: vi.fn().mockResolvedValue({
    previewVersion: '1.0.0', generatedAtUtc: '',
    counts: { totalExported: 1, wouldCreate: 1, wouldUpdate: 0, wouldSkip: 0, schemaValid: 1, schemaInvalid: 0, duplicateSlugs: 0, duplicateCanonicalIds: 0, activeRecords: 0, inactiveRecords: 1 },
    items: [{ name: 'Creatine', slug: 'creatine', canonicalId: 'creatine', action: 'create', schemaValid: true, isActive: false, existingSeedMatch: false, reasons: [], reviewDecisionIds: ['approve-creatine-fixture-001'] }],
  }),
  fetchDryRunReport: vi.fn().mockResolvedValue({
    reportVersion: '1.0.0', generatedAtUtc: '', previewPath: '', aggregatePath: '', safeToApply: true, refusalReasons: [],
    previewCounts: { totalExported: 1, wouldCreate: 1, wouldUpdate: 0, wouldSkip: 0, schemaValid: 1, schemaInvalid: 0, duplicateSlugs: 0, duplicateCanonicalIds: 0, activeRecords: 0, inactiveRecords: 1 },
    items: [{ name: 'Creatine', slug: 'creatine', plannedAction: 'create', schemaValid: true, reasons: [] }],
  }),
  fetchExportManifest: vi.fn().mockResolvedValue({
    manifestVersion: '1.0.0', generatedAtUtc: '', exportedCount: 1,
    candidates: [{ name: 'Creatine', slug: 'creatine', readiness: 'candidate-for-promotion', substanceFile: 'creatine.json', aggregateIndex: 0, reviewDecisionIds: ['approve-creatine-fixture-001'], qualityFlags: [] }],
    skippedCompounds: [],
  }),
}));

describe('PipelinePage', () => {
  it('renders promotion manifest candidates', async () => {
    render(<PipelinePage />);
    expect(await screen.findByText('Creatine')).toBeInTheDocument();
    expect(screen.getByText('Candidate')).toBeInTheDocument();
  });

  it('surfaces typed dry-run report safety', async () => {
    render(<PipelinePage />);
    fireEvent.click(await screen.findByText('Dry-Run Report'));
    expect(await screen.findByText('Safe to apply')).toBeInTheDocument();
    expect(screen.getByText('create')).toBeInTheDocument();
  });
});