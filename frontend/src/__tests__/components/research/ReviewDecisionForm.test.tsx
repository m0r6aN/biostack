import { render, screen, fireEvent } from '@testing-library/react';
import { ReviewDecisionForm } from '@/components/research/ReviewDecisionForm';
import type { PromotionManifestCandidate } from '@/lib/research/types';

const mockContext = {
  batch: { schemaVersion: '1.0.0' as const, recordType: 'review-decision-batch' as const, batch: { batchId: 'b1', reviewerId: 'r1', reviewedAt: '', notes: [] }, decisions: [] },
  reviewerId: 'r1', setReviewerId: vi.fn(), addToSession: vi.fn(),
};
vi.mock('@/lib/research/ReviewDecisionContext', () => ({ useReviewDecision: () => mockContext }));

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

describe('ReviewDecisionForm — blocked compound', () => {
  it('shows hard blocker guard banner', () => {
    render(<ReviewDecisionForm candidate={blocked} />);
    expect(screen.getByText(/unresolved hard blockers/i)).toBeInTheDocument();
  });

  it('does not show Add to Batch button', () => {
    render(<ReviewDecisionForm candidate={blocked} />);
    expect(screen.queryByText(/Add to Decision Batch/i)).not.toBeInTheDocument();
  });
});

describe('ReviewDecisionForm — review-required compound', () => {
  it('renders all decision radio options', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} />);
    expect(screen.getByLabelText('approve-for-promotion')).toBeInTheDocument();
    expect(screen.getByLabelText('approve-claims')).toBeInTheDocument();
    expect(screen.getByLabelText('request-changes')).toBeInTheDocument();
    expect(screen.getByLabelText('reject')).toBeInTheDocument();
  });

  it('shows Add to Decision Batch button', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} />);
    expect(screen.getByText(/Add to Decision Batch/i)).toBeInTheDocument();
  });

  it('blocks submit without selecting a decision', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} />);
    fireEvent.click(screen.getByText(/Add to Decision Batch/i));
    expect(screen.getByText(/Select a decision/i)).toBeInTheDocument();
  });

  it('requires notes when approve-for-promotion selected with soft blockers remaining', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} />);
    fireEvent.click(screen.getByLabelText('approve-for-promotion'));
    fireEvent.click(screen.getByText(/Add to Decision Batch/i));
    expect(screen.getByText(/Notes required/i)).toBeInTheDocument();
  });
});
