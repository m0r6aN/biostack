import { render, screen } from '@testing-library/react';
import CompoundList from '@/app/admin/research/compounds/page';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn() }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/admin/research/compounds',
}));
vi.mock('@/lib/apiBase', () => ({ getApiBaseUrl: () => 'http://localhost' }));
vi.mock('@/lib/research/loader', () => ({
  fetchResearchSummary: vi.fn().mockResolvedValue({
    draftSubstanceCount: 2, reviewQueueItemCount: 3,
    compounds: [
      { name: 'BPC-157', classification: 'Peptide', overallEvidenceTier: 'Limited', completeness: 'partial', needsReview: true, reviewQueueItemCount: 3, promotionReadiness: 'blocked', promotionBlockers: ['blocked: missing authoritative support'], reviewDecisionIds: [], qualityFlags: [], reviewReasons: [] },
      { name: 'Creatine', classification: 'Supplement', overallEvidenceTier: 'Strong', completeness: 'substantial', needsReview: false, reviewQueueItemCount: 0, promotionReadiness: 'candidate-for-promotion', promotionBlockers: [], reviewDecisionIds: [], qualityFlags: [], reviewReasons: [] },
    ],
    reviewCategories: [], promotionReadiness: [], qualityFlags: [], reviewReasons: [], classifications: [], evidenceTiers: [],
  }),
  fetchPromotionManifest: vi.fn().mockResolvedValue({
    manifestVersion: '1.0.0', generatedAtUtc: '',
    counts: { totalDrafts: 2, blocked: 1, reviewRequired: 0, candidatesForPromotion: 1 },
    blocked: [], reviewRequired: [], candidatesForPromotion: [],
  }),
}));
vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <div>{title}</div>,
}));

describe('CompoundList', () => {
  it('renders compound names after loading', async () => {
    render(<CompoundList />);
    expect(await screen.findByText('BPC-157')).toBeInTheDocument();
    expect(await screen.findByText('Creatine')).toBeInTheDocument();
  });

  it('renders filter bar with readiness chips', async () => {
    render(<CompoundList />);
    expect(await screen.findByText('Blocked (1)')).toBeInTheDocument();
    expect(await screen.findByText('Candidate (1)')).toBeInTheDocument();
  });
});
