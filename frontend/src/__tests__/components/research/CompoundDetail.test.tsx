import { render, screen } from '@testing-library/react';
import CompoundDetail from '@/app/admin/research/compounds/[slug]/page';

vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <div>{title}</div>,
}));

vi.mock('@/components/ui/GlassCard', () => ({
  GlassCard: ({ children, className }: { children: React.ReactNode; className?: string }) => (
    <div className={className}>{children}</div>
  ),
}));

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn() }),
  useParams: () => ({ slug: 'bpc-157' }),
}));
vi.mock('@/lib/apiBase', () => ({ getApiBaseUrl: () => 'http://localhost' }));
vi.mock('@/lib/research/ReviewDecisionContext', () => ({
  useReviewDecision: () => ({
    batch: { schemaVersion: '1.0.0', recordType: 'review-decision-batch', batch: { batchId: 'b1', reviewerId: 'r1', reviewedAt: '', notes: [] }, decisions: [] },
    reviewerId: 'r1', setReviewerId: vi.fn(), addToSession: vi.fn(),
  }),
}));
vi.mock('@/lib/research/loader', () => ({
  fetchResearchSummary: vi.fn().mockResolvedValue({
    draftSubstanceCount: 1, reviewQueueItemCount: 3,
    compounds: [{
      name: 'BPC-157', classification: 'Peptide', overallEvidenceTier: 'Limited',
      completeness: 'partial', needsReview: true, reviewQueueItemCount: 3,
      promotionReadiness: 'blocked',
      promotionBlockers: ['blocked: missing required authoritative support'],
      reviewDecisionIds: [], qualityFlags: ['missing-authoritative-support'], reviewReasons: [],
    }],
    reviewCategories: [], promotionReadiness: [], qualityFlags: [], reviewReasons: [], classifications: [], evidenceTiers: [],
  }),
  fetchPromotionManifest: vi.fn().mockResolvedValue({
    manifestVersion: '1.0.0', generatedAtUtc: '',
    counts: { totalDrafts: 1, blocked: 1, reviewRequired: 0, candidatesForPromotion: 0 },
    blocked: [{
      name: 'BPC-157', classification: 'Peptide', readiness: 'blocked',
      overallEvidenceTier: 'Limited', completeness: 'partial', reviewQueueItemCount: 3,
      reviewDecisionIds: [],
      blockers: ['blocked: missing required authoritative support'],
      qualityFlags: ['missing-authoritative-support'],
      requiredNextActions: ['Attach an A1/A2 source before promotion.'],
    }],
    reviewRequired: [], candidatesForPromotion: [],
  }),
  fetchReviewResolutionPlan: vi.fn().mockResolvedValue({
    planVersion: '1.0.0', generatedAtUtc: '',
    counts: { totalItems: 1, blockedItems: 1, reviewRequiredItems: 0, resolutionTypes: [] },
    items: [{
      itemId: 'i1', compoundName: 'BPC-157', readiness: 'blocked', severity: 'blocked',
      resolutionType: 'add-authoritative-source',
      issue: 'blocked: missing required authoritative support',
      recommendedAction: 'Attach A1/A2.',
      relatedBlockers: [], relatedQualityFlags: [],
    }],
  }),
}));

describe('CompoundDetail', () => {
  it('renders the compound name', async () => {
    render(<CompoundDetail />);
    expect(await screen.findAllByText('BPC-157')).not.toHaveLength(0);
  });

  it('shows the blocked readiness badge', async () => {
    render(<CompoundDetail />);
    expect(await screen.findByText('Blocked')).toBeInTheDocument();
  });

  it('shows promotion blockers', async () => {
    render(<CompoundDetail />);
    expect(await screen.findByText(/missing required authoritative support/)).toBeInTheDocument();
  });
});
