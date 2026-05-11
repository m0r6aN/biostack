import CompoundDetail from '@/app/admin/research/compounds/[slug]/page';
import { fetchEvidencePacket, fetchPromotionManifest, fetchResearchSummary, fetchResearchTaskQueue, fetchReviewQueue, fetchReviewResolutionPlan } from '@/lib/research/loader';
import { fireEvent, render, screen } from '@testing-library/react';

const pushMock = vi.hoisted(() => vi.fn());

vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <div>{title}</div>,
}));

vi.mock('@/components/ui/GlassCard', () => ({
  GlassCard: ({ children, className }: { children: React.ReactNode; className?: string }) => (
    <div className={className}>{children}</div>
  ),
}));

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: pushMock }),
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
  fetchResearchTaskQueue: vi.fn().mockResolvedValue({
    queueVersion: '1.0.0', generatedAtUtc: '', counts: { totalItems: 0, urgent: 0, high: 0, normal: 0, low: 0, resolvedItems: 0 }, items: [], resolvedItems: [],
  }),
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
  beforeEach(() => {
    pushMock.mockReset();
  });

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

  it('shows queued evidence-task details for a research-requested compound', async () => {
    vi.mocked(fetchResearchSummary).mockResolvedValueOnce({
      draftSubstanceCount: 0, reviewQueueItemCount: 0, researchRequestCount: 1,
      compounds: [{
        name: 'BPC-157', classification: 'Peptide', overallEvidenceTier: 'Unknown',
        completeness: 'requested', needsReview: true, reviewQueueItemCount: 0,
        promotionReadiness: 'research-requested',
        promotionBlockers: ['research-requested: evidence packet has not been generated'],
        reviewDecisionIds: [], hasResearchRequest: true, researchRequestIds: ['research-bpc-157-001'], qualityFlags: ['research-requested'], reviewReasons: [],
      }],
      reviewCategories: [], promotionReadiness: [], qualityFlags: [], reviewReasons: [], classifications: [], evidenceTiers: [],
    });
    vi.mocked(fetchPromotionManifest).mockResolvedValueOnce({
      manifestVersion: '1.0.0', generatedAtUtc: '',
      counts: { totalDrafts: 0, blocked: 0, reviewRequired: 0, researchRequested: 1, candidatesForPromotion: 0 },
      blocked: [], reviewRequired: [],
      researchRequested: [{
        name: 'BPC-157', classification: 'Peptide', readiness: 'research-requested', overallEvidenceTier: 'Unknown', completeness: 'requested', reviewQueueItemCount: 0,
        reviewDecisionIds: [], hasResearchRequest: true, researchRequestIds: ['research-bpc-157-001'], blockers: ['research-requested: evidence packet has not been generated'], qualityFlags: ['research-requested'],
        requiredNextActions: ['Create a compound evidence packet from the research request.'],
      }],
      candidatesForPromotion: [],
    });
    vi.mocked(fetchReviewResolutionPlan).mockResolvedValueOnce({
      planVersion: '1.0.0', generatedAtUtc: '', counts: { totalItems: 1, blockedItems: 0, reviewRequiredItems: 0, resolutionTypes: [] },
      items: [{
        itemId: 'i1', compoundName: 'BPC-157', readiness: 'research-requested', severity: 'research', resolutionType: 'perform-initial-research',
        issue: 'research-requested: evidence packet has not been generated', recommendedAction: 'Perform initial evidence research.', relatedReviewQueueItemIds: [], relatedBlockers: [], relatedQualityFlags: [],
      }],
    });
    vi.mocked(fetchReviewQueue).mockResolvedValueOnce([]);
    vi.mocked(fetchResearchTaskQueue).mockResolvedValueOnce({
      queueVersion: '1.0.0', generatedAtUtc: '', counts: { totalItems: 1, urgent: 0, high: 1, normal: 0, low: 0 },
      items: [{
        taskId: 'bpc-157-initial-research', taskType: 'generate-evidence-packet', compoundName: 'BPC-157', aliases: [], categories: ['Peptides'], classification: 'Peptide', priority: 'high',
        requestIds: ['research-bpc-157-001'], requesterIds: ['operator-1'], firstRequestedAtUtc: '', latestRequestedAtUtc: '',
        rationales: ['Queue an evidence packet for BPC-157.'], notes: [],
        suggestedResearchDirectives: ['Use the source registry agent first.'], targetEvidencePath: 'research/input/evidence/bpc-157.evidence.json', requiredSchema: 'evidence-packet.schema.json',
      }],
      resolvedItems: [],
    });
    vi.mocked(fetchEvidencePacket).mockRejectedValueOnce(new Error('404'));

    render(<CompoundDetail />);

    expect(await screen.findByText('Queued Evidence Task')).toBeInTheDocument();
    expect(screen.getByText('research/input/evidence/bpc-157.evidence.json')).toBeInTheDocument();
    expect(screen.getByText('evidence-packet.schema.json')).toBeInTheDocument();
    expect(screen.getByText('Peptides')).toBeInTheDocument();
    expect(screen.getByText(/Use the source registry agent first/i)).toBeInTheDocument();
  });

  it('routes the queued evidence card into the task board', async () => {
    vi.mocked(fetchResearchSummary).mockResolvedValueOnce({
      draftSubstanceCount: 0, reviewQueueItemCount: 0, researchRequestCount: 1,
      compounds: [{
        name: 'BPC-157', classification: 'Peptide', overallEvidenceTier: 'Unknown',
        completeness: 'requested', needsReview: true, reviewQueueItemCount: 0,
        promotionReadiness: 'research-requested',
        promotionBlockers: ['research-requested: evidence packet has not been generated'],
        reviewDecisionIds: [], hasResearchRequest: true, researchRequestIds: ['research-bpc-157-001'], qualityFlags: ['research-requested'], reviewReasons: [],
      }],
      reviewCategories: [], promotionReadiness: [], qualityFlags: [], reviewReasons: [], classifications: [], evidenceTiers: [],
    });
    vi.mocked(fetchPromotionManifest).mockResolvedValueOnce({
      manifestVersion: '1.0.0', generatedAtUtc: '',
      counts: { totalDrafts: 0, blocked: 0, reviewRequired: 0, researchRequested: 1, candidatesForPromotion: 0 },
      blocked: [], reviewRequired: [],
      researchRequested: [{
        name: 'BPC-157', classification: 'Peptide', readiness: 'research-requested', overallEvidenceTier: 'Unknown', completeness: 'requested', reviewQueueItemCount: 0,
        reviewDecisionIds: [], hasResearchRequest: true, researchRequestIds: ['research-bpc-157-001'], blockers: ['research-requested: evidence packet has not been generated'], qualityFlags: ['research-requested'],
        requiredNextActions: ['Create a compound evidence packet from the research request.'],
      }],
      candidatesForPromotion: [],
    });
    vi.mocked(fetchReviewResolutionPlan).mockResolvedValueOnce({
      planVersion: '1.0.0', generatedAtUtc: '', counts: { totalItems: 1, blockedItems: 0, reviewRequiredItems: 0, resolutionTypes: [] },
      items: [{
        itemId: 'i1', compoundName: 'BPC-157', readiness: 'research-requested', severity: 'research', resolutionType: 'perform-initial-research',
        issue: 'research-requested: evidence packet has not been generated', recommendedAction: 'Perform initial evidence research.', relatedReviewQueueItemIds: [], relatedBlockers: [], relatedQualityFlags: [],
      }],
    });
    vi.mocked(fetchReviewQueue).mockResolvedValueOnce([]);
    vi.mocked(fetchResearchTaskQueue).mockResolvedValueOnce({
      queueVersion: '1.0.0', generatedAtUtc: '', counts: { totalItems: 1, urgent: 0, high: 1, normal: 0, low: 0 },
      items: [{
        taskId: 'bpc-157-initial-research', taskType: 'generate-evidence-packet', compoundName: 'BPC-157', aliases: [], categories: ['Peptides'], classification: 'Peptide', priority: 'high',
        requestIds: ['research-bpc-157-001'], requesterIds: ['operator-1'], firstRequestedAtUtc: '', latestRequestedAtUtc: '',
        rationales: ['Queue an evidence packet for BPC-157.'], notes: [],
        suggestedResearchDirectives: ['Use the source registry agent first.'], targetEvidencePath: 'research/input/evidence/bpc-157.evidence.json', requiredSchema: 'evidence-packet.schema.json',
      }],
      resolvedItems: [],
    });
    vi.mocked(fetchEvidencePacket).mockRejectedValueOnce(new Error('404'));

    render(<CompoundDetail />);

    fireEvent.click(await screen.findByRole('button', { name: /Open in task board/i }));
    expect(pushMock).toHaveBeenCalledWith('/admin/research/tasks?compound=bpc-157');
  });

  it('shows a consumed-task signal after evidence clears the queue', async () => {
    vi.mocked(fetchResearchSummary).mockResolvedValueOnce({
      draftSubstanceCount: 1, reviewQueueItemCount: 1, researchRequestCount: 0,
      compounds: [{
        name: 'BPC-157', classification: 'Peptide', overallEvidenceTier: 'Limited',
        completeness: 'partial', needsReview: true, reviewQueueItemCount: 1,
        promotionReadiness: 'review-required',
        promotionBlockers: ['review-required: compiled draft requires human review'],
        reviewDecisionIds: [], hasResearchRequest: true, researchRequestIds: ['research-bpc-157-001'], qualityFlags: [], reviewReasons: [],
      }],
      reviewCategories: [], promotionReadiness: [], qualityFlags: [], reviewReasons: [], classifications: [], evidenceTiers: [],
    });
    vi.mocked(fetchPromotionManifest).mockResolvedValueOnce({
      manifestVersion: '1.0.0', generatedAtUtc: '',
      counts: { totalDrafts: 1, blocked: 0, reviewRequired: 1, researchRequested: 0, candidatesForPromotion: 0 },
      blocked: [], reviewRequired: [{
        name: 'BPC-157', classification: 'Peptide', readiness: 'review-required', overallEvidenceTier: 'Limited', completeness: 'partial', reviewQueueItemCount: 1,
        reviewDecisionIds: [], hasResearchRequest: true, researchRequestIds: ['research-bpc-157-001'], blockers: ['review-required: compiled draft requires human review'], qualityFlags: [], requiredNextActions: ['Review compiled evidence.'],
      }], candidatesForPromotion: [], researchRequested: [],
    });
    vi.mocked(fetchReviewResolutionPlan).mockResolvedValueOnce({
      planVersion: '1.0.0', generatedAtUtc: '', counts: { totalItems: 1, blockedItems: 0, reviewRequiredItems: 1, resolutionTypes: [] },
      items: [{
        itemId: 'i1', compoundName: 'BPC-157', readiness: 'review-required', severity: 'review', resolutionType: 'review-evidence',
        issue: 'review-required: compiled draft requires human review', recommendedAction: 'Review evidence.', relatedReviewQueueItemIds: [], relatedBlockers: [], relatedQualityFlags: [],
      }],
    });
    vi.mocked(fetchReviewQueue).mockResolvedValueOnce([]);
    vi.mocked(fetchResearchTaskQueue).mockResolvedValueOnce({
      queueVersion: '1.0.0', generatedAtUtc: '', counts: { totalItems: 0, urgent: 0, high: 0, normal: 0, low: 0, resolvedItems: 1 }, items: [],
      resolvedItems: [{
        taskId: 'bpc-157-initial-research', compoundName: 'BPC-157', aliases: [], categories: ['Peptides'], classification: 'Peptide', priority: 'high',
        requestIds: ['research-bpc-157-001'], requesterIds: ['operator-1'], firstRequestedAtUtc: '', latestRequestedAtUtc: '', resolvedAtUtc: '',
        currentReadiness: 'review-required', resolution: 'evidence-detected', resolutionReason: 'Evidence is now present.', targetEvidencePath: 'research/input/evidence/bpc-157.evidence.json',
      }],
    });
    vi.mocked(fetchEvidencePacket).mockRejectedValueOnce(new Error('404'));

    render(<CompoundDetail />);

    expect(await screen.findByText('Research Task Consumed')).toBeInTheDocument();
    expect(screen.getByText(/Evidence is now present/i)).toBeInTheDocument();
  });
});
