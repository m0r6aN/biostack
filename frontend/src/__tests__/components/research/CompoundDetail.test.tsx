import CompoundDetail from '@/app/admin/research/compounds/[slug]/page';
import { fetchEvidencePacket } from '@/lib/research/loader';
import { fireEvent, render, screen } from '@testing-library/react';

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
      relatedReviewQueueItemIds: [],
      relatedBlockers: [], relatedQualityFlags: [],
    }],
  }),
  fetchReviewQueue: vi.fn().mockResolvedValue([{ itemId: 'bpc-157-ops-review-1', compoundName: 'BPC-157', severity: 'review', reason: 'Pilot review.', references: [] }]),
  fetchEvidencePacket: vi.fn().mockResolvedValue({
    schemaVersion: '1.0.0', recordType: 'compound-evidence-packet',
    packet: { packetId: 'bpc-packet', category: 'peptides', agentId: 'agent', generatedAt: '', sourceRegistryVersion: 'sources' },
    compound: { canonicalName: 'BPC-157', aliases: [], classification: 'Peptide', compoundFamily: null, externalIdentifiers: {} },
    sources: [{ sourceId: 'src-1', sourceType: 'clinical', authorityTier: 'B2', title: 'BPC source', publisher: null, url: null, doi: null, pmid: null, publishedAt: null, accessedAt: '' }],
    claims: [{
      claimId: 'claim-1', claimType: 'evidence-gap',
      statement: 'BPC-157 requires human review before promotion.',
      context: { population: null, route: null, formulation: null, useCase: null, doseText: null },
      evidenceTier: 'Limited', confidence: 'moderate', fieldAuthorityRequired: false,
      sourceRefs: ['src-1'], extractedEvidence: [{ sourceRef: 'src-1', quote: 'Review quote.', pageOrSection: null }],
      reviewFlags: ['pilot-input'],
    }],
    conflicts: [], ops: { completeness: 'partial', needsReview: true, reviewReasons: [], qualityFlags: [] },
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

  it('renders evidence packet claims', async () => {
    render(<CompoundDetail />);
    await screen.findAllByText('BPC-157');

    fireEvent.click(screen.getByRole('button', { name: 'Claims' }));

    expect(await screen.findByText('BPC-157 requires human review before promotion.')).toBeInTheDocument();
    expect(screen.getByText('Evidence support: Limited')).toBeInTheDocument();
    expect(screen.getByText('Extraction confidence: moderate')).toBeInTheDocument();
    expect(screen.getByText(/Reviewer guardrails/)).toBeInTheDocument();
    expect(screen.getByText('B2 · BPC source')).toBeInTheDocument();
    expect(fetchEvidencePacket).toHaveBeenCalledWith('bpc-157', expect.any(String));
  });

  it('shows a section-level empty state when the evidence packet is missing', async () => {
    vi.mocked(fetchEvidencePacket).mockRejectedValueOnce(new Error('404'));

    render(<CompoundDetail />);
    await screen.findAllByText('BPC-157');

    fireEvent.click(screen.getByRole('button', { name: 'Claims' }));

    expect(await screen.findByText(/Research artifacts not yet generated/)).toBeInTheDocument();
  });
});
