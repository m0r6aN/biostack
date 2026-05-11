import { ReviewDecisionForm } from '@/components/research/ReviewDecisionForm';
import type { EvidencePacket, PromotionManifestCandidate, ResearchSummaryCompound, ReviewDecisionBatch } from '@/lib/research/types';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';

const emptyBatch: ReviewDecisionBatch = {
  schemaVersion: '1.0.0',
  recordType: 'review-decision-batch',
  batch: { batchId: 'b1', reviewerId: 'r1', reviewedAt: '2026-05-10T12:00:00.000Z', notes: [] },
  decisions: [],
};

const mockContext: {
  batch: ReviewDecisionBatch;
  reviewerId: string;
  setReviewerId: ReturnType<typeof vi.fn>;
  addToSession: ReturnType<typeof vi.fn>;
  resetSession: ReturnType<typeof vi.fn>;
} = {
  batch: { schemaVersion: '1.0.0' as const, recordType: 'review-decision-batch' as const, batch: { batchId: 'b1', reviewerId: 'r1', reviewedAt: '', notes: [] }, decisions: [] },
  reviewerId: 'r1', setReviewerId: vi.fn(), addToSession: vi.fn(), resetSession: vi.fn(),
};
vi.mock('@/lib/research/ReviewDecisionContext', () => ({ useReviewDecision: () => mockContext }));

beforeEach(() => {
  mockContext.batch = { ...emptyBatch, decisions: [] };
  mockContext.setReviewerId.mockClear();
  mockContext.addToSession.mockClear();
  mockContext.resetSession.mockClear();
  vi.unstubAllGlobals();
});

const blocked: PromotionManifestCandidate = {
  name: 'BPC-157', classification: 'Peptide', readiness: 'blocked',
  overallEvidenceTier: 'Limited', completeness: 'partial', reviewQueueItemCount: 3,
  reviewDecisionIds: [], blockers: ['blocked: missing required authoritative support'],
  qualityFlags: ['missing-authoritative-support'], requiredNextActions: [],
};

const reviewRequired: PromotionManifestCandidate = {
  name: 'Semaglutide', classification: 'Pharmaceutical', readiness: 'review-required',
  overallEvidenceTier: 'Strong', completeness: 'substantial', reviewQueueItemCount: 2,
  reviewDecisionIds: [], blockers: ['review-required: draft is marked needsReview'],
  qualityFlags: [], requiredNextActions: [],
};

const compound: ResearchSummaryCompound = {
  name: 'Semaglutide', classification: 'Pharmaceutical', overallEvidenceTier: 'Strong', completeness: 'substantial',
  needsReview: true, reviewQueueItemCount: 2, promotionReadiness: 'review-required',
  promotionBlockers: ['review-required: draft is marked needsReview'], reviewDecisionIds: [], qualityFlags: [],
  reviewReasons: ['Compiled draft requires human review before publication.'],
};

const blockedCompound: ResearchSummaryCompound = { ...compound, name: 'BPC-157', classification: 'Peptide', promotionReadiness: 'blocked', promotionBlockers: blocked.blockers };

const evidencePacket: EvidencePacket = {
  schemaVersion: '1.0.0', recordType: 'compound-evidence-packet',
  packet: { packetId: 'semaglutide-packet', category: 'glp-1', agentId: 'agent', generatedAt: '', sourceRegistryVersion: 'sources' },
  compound: { canonicalName: 'Semaglutide', aliases: [], classification: 'Pharmaceutical', compoundFamily: null, externalIdentifiers: {} },
  sources: [], conflicts: [], ops: { completeness: 'substantial', needsReview: true, reviewReasons: [], qualityFlags: [] },
  claims: [{
    claimId: 'semaglutide-claim-001', claimType: 'studied-use', statement: 'Semaglutide has clinical trial evidence in scoped indications.',
    context: { population: null, route: null, formulation: null, useCase: null, doseText: null },
    evidenceTier: 'Strong', confidence: 'high', fieldAuthorityRequired: false, sourceRefs: [], extractedEvidence: [], reviewFlags: [],
  }],
};

const reviewQueueItems = [{
  itemId: 'semaglutide-ops-review-1', compoundName: 'Semaglutide', severity: 'review',
  reason: 'Pilot regulatory evidence packet requires human review before publication.', references: [],
}];

describe('ReviewDecisionForm — blocked compound', () => {
  it('shows hard blocker guard banner', () => {
    render(<ReviewDecisionForm candidate={blocked} compound={blockedCompound} evidencePacket={evidencePacket} />);
    expect(screen.getByText(/Hard blockers/i)).toBeInTheDocument();
  });

  it('allows non-promotion decisions while hard blockers remain visible', () => {
    render(<ReviewDecisionForm candidate={blocked} compound={blockedCompound} evidencePacket={evidencePacket} />);
    expect(screen.getByText(/Add to Decision Batch/i)).toBeInTheDocument();
  });
});

describe('ReviewDecisionForm — review-required compound', () => {
  it('renders all decision radio options', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} compound={compound} evidencePacket={evidencePacket} />);
    expect(screen.getByLabelText('approve-for-promotion')).toBeInTheDocument();
    expect(screen.getByLabelText('approve-claims')).toBeInTheDocument();
    expect(screen.getByLabelText('resolve-review-items')).toBeInTheDocument();
    expect(screen.getByLabelText('archive-draft')).toBeInTheDocument();
    expect(screen.getByLabelText('request-changes')).toBeInTheDocument();
    expect(screen.getByLabelText('reject')).toBeInTheDocument();
  });

  it('shows Add to Decision Batch button', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} compound={compound} evidencePacket={evidencePacket} />);
    expect(screen.getByText(/Add to Decision Batch/i)).toBeInTheDocument();
  });

  it('shows disabled Save & Process Batch action until the batch has decisions', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} compound={compound} evidencePacket={evidencePacket} />);
    expect(screen.getByRole('button', { name: /Save & Process Batch/i })).toBeDisabled();
  });

  it('blocks submit without selecting a decision', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} compound={compound} evidencePacket={evidencePacket} />);
    fireEvent.click(screen.getByText(/Add to Decision Batch/i));
    expect(screen.getByText(/Select a decision/i)).toBeInTheDocument();
  });

  it('requires notes when approve-for-promotion selected with soft blockers remaining', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} compound={compound} evidencePacket={evidencePacket} />);
    fireEvent.click(screen.getByLabelText('approve-for-promotion'));
    fireEvent.click(screen.getByText(/Add to Decision Batch/i));
    expect(screen.getByText(/Notes required/i)).toBeInTheDocument();
  });

  it('records selected claim IDs for approve-claims decisions', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} compound={compound} evidencePacket={evidencePacket} />);
    fireEvent.click(screen.getByLabelText('approve-claims'));
    fireEvent.click(screen.getByText('semaglutide-claim-001'));
    fireEvent.click(screen.getByText(/Add to Decision Batch/i));

    expect(mockContext.addToSession).toHaveBeenCalledWith(expect.objectContaining({
      decision: 'approve-claims',
      scope: expect.objectContaining({ claimIds: ['semaglutide-claim-001'] }),
      clearsSoftPromotionBlockers: false,
    }));
  });

  it('records selected review queue item IDs for resolve-review-items decisions', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} compound={compound} evidencePacket={evidencePacket} reviewQueueItems={reviewQueueItems} />);
    fireEvent.click(screen.getByLabelText('resolve-review-items'));
    fireEvent.click(screen.getByText('semaglutide-ops-review-1'));
    fireEvent.click(screen.getByText(/Add to Decision Batch/i));

    expect(mockContext.addToSession).toHaveBeenCalledWith(expect.objectContaining({
      decision: 'resolve-review-items',
      scope: expect.objectContaining({ reviewQueueItemIds: ['semaglutide-ops-review-1'] }),
      clearsSoftPromotionBlockers: false,
    }));
  });

  it('requires notes before archiving a draft', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} compound={compound} evidencePacket={evidencePacket} />);
    fireEvent.click(screen.getByLabelText('archive-draft'));
    fireEvent.click(screen.getByText(/Add to Decision Batch/i));
    expect(screen.getByText(/Notes required.*archived/i)).toBeInTheDocument();
  });

  it('requests and applies an AI suggestion', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      modelUsed: 'gpt-5.5',
      suggestion: {
        decision: 'approve-claims', confidence: 'moderate', summary: 'Approve the narrow sourced claim only.',
        rationale: ['The claim is source-scoped and strong.'], claimIdsToApprove: ['semaglutide-claim-001'],
        reviewQueueItemIdsToResolve: [],
        clearsSoftPromotionBlockers: false, draftNotes: 'Approve only semaglutide-claim-001; request draft changes before promotion.',
        safetyWarnings: ['Do not clear soft blockers from claim approval.'], openQuestions: [],
      },
    })));
    vi.stubGlobal('fetch', fetchMock);

    render(<ReviewDecisionForm candidate={reviewRequired} compound={compound} evidencePacket={evidencePacket} />);
    fireEvent.click(screen.getByRole('button', { name: 'AI Suggest' }));

    expect(await screen.findByText('Approve the narrow sourced claim only.')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledWith('/api/research/suggest', expect.objectContaining({ method: 'POST' }));

    fireEvent.click(screen.getByRole('button', { name: /Apply suggestion/i }));
    await waitFor(() => expect(screen.getByLabelText('approve-claims')).toBeChecked());
    fireEvent.click(screen.getByText(/Add to Decision Batch/i));

    expect(mockContext.addToSession).toHaveBeenCalledWith(expect.objectContaining({
      decision: 'approve-claims',
      scope: expect.objectContaining({ claimIds: ['semaglutide-claim-001'] }),
      notes: ['Approve only semaglutide-claim-001; request draft changes before promotion.'],
    }));
  });

  it('saves and processes the current decision batch', async () => {
    mockContext.batch = {
      ...emptyBatch,
      decisions: [{
        decisionId: 'dec-1', compoundName: 'Semaglutide', decision: 'resolve-review-items', reviewerId: 'r1',
        reviewedAt: '2026-05-10T12:01:00.000Z', clearsSoftPromotionBlockers: false, expiresAt: null, notes: [],
        scope: { claimIds: [], reviewQueueItemIds: ['semaglutide-ops-review-1'], qualityFlags: [], reviewCategories: [], promotionBlockers: [] },
      }],
    };
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      savedFilename: 'review-decision-batch-test.json', exitCode: 0, stdout: '[BioStack Research] Complete.', stderr: '',
    }), { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);

    render(<ReviewDecisionForm candidate={reviewRequired} compound={compound} evidencePacket={evidencePacket} />);
    fireEvent.click(screen.getByRole('button', { name: /Save & Process Batch/i }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/research/process-decisions', expect.objectContaining({ method: 'POST' })));
    const body = JSON.parse(String(fetchMock.mock.calls[0][1]?.body)) as ReviewDecisionBatch;
    expect(body.decisions).toHaveLength(1);
    expect(await screen.findByText(/Processed review-decision-batch-test.json/i)).toBeInTheDocument();
    expect(mockContext.resetSession).toHaveBeenCalled();
  });
});
