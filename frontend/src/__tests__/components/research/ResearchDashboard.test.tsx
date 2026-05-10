import ResearchDashboard from '@/app/admin/research/page';
import { render, screen } from '@testing-library/react';

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
  fetchResearchSummary: vi.fn().mockResolvedValue({
    draftSubstanceCount: 3,
    reviewQueueItemCount: 5,
    compounds: [],
    reviewCategories: [
      { name: 'Safety Critical', count: 1, compounds: [], signals: [], recommendedActions: [] }
    ],
    promotionReadiness: [
      { name: 'blocked', count: 1, compounds: [] },
      { name: 'review-required', count: 1, compounds: [] },
      { name: 'candidate-for-promotion', count: 1, compounds: [] }
    ],
    qualityFlags: [], reviewReasons: [], classifications: [], evidenceTiers: [],
  }),
  fetchPromotionManifest: vi.fn().mockResolvedValue({
    manifestVersion: '1.0.0', generatedAtUtc: '',
    counts: { totalDrafts: 3, blocked: 1, reviewRequired: 1, candidatesForPromotion: 1 },
    blocked: [], reviewRequired: [], candidatesForPromotion: [],
  }),
  fetchReviewResolutionPlan: vi.fn().mockResolvedValue({
    planVersion: '1.0.0', generatedAtUtc: '',
    counts: { totalItems: 2, blockedItems: 1, reviewRequiredItems: 1, resolutionTypes: [] },
    items: [],
  }),
  fetchImportPreview: vi.fn().mockResolvedValue({
    previewVersion: '1.0.0', generatedAtUtc: '',
    counts: {
      totalExported: 1, wouldCreate: 1, wouldUpdate: 0, wouldSkip: 0,
      schemaValid: 1, schemaInvalid: 0, duplicateSlugs: 0,
      duplicateCanonicalIds: 0, activeRecords: 0, inactiveRecords: 1,
    },
    items: [],
  }),
  fetchDryRunReport: vi.fn().mockResolvedValue({
    reportVersion: '1.0.0', generatedAtUtc: '', previewPath: '', aggregatePath: '',
    safeToApply: true, refusalReasons: [],
    previewCounts: {
      totalExported: 1, wouldCreate: 1, wouldUpdate: 0, wouldSkip: 0,
      schemaValid: 1, schemaInvalid: 0, duplicateSlugs: 0,
      duplicateCanonicalIds: 0, activeRecords: 0, inactiveRecords: 1,
    },
    items: [],
  }),
}));

describe('ResearchDashboard', () => {
  it('renders the page heading', async () => {
    render(<ResearchDashboard />);
    expect(await screen.findByText('Research Runs')).toBeInTheDocument();
  });

  it('renders stat chips showing draft count', async () => {
    render(<ResearchDashboard />);
    // Total Drafts chip shows value 3
    expect(await screen.findByText('3')).toBeInTheDocument();
  });

  it('renders a review category', async () => {
    render(<ResearchDashboard />);
    expect(await screen.findByText('Safety Critical')).toBeInTheDocument();
  });

  it('surfaces dry-run safety and inactive export status', async () => {
    render(<ResearchDashboard />);
    expect(await screen.findByText('Dry-Run Safe')).toBeInTheDocument();
    expect(await screen.findByText('Active Exports')).toBeInTheDocument();
    expect(await screen.findByText(/Safe to apply:/i)).toBeInTheDocument();
  });
});
