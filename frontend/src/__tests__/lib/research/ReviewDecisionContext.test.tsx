import { ReviewDecisionProvider, useReviewDecision } from '@/lib/research/ReviewDecisionContext';
import type { ReviewDecision } from '@/lib/research/types';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';

const STORAGE_KEY = 'biostack.reviewDecisionBatch.v1';

const decision: ReviewDecision = {
  decisionId: 'dec-001',
  compoundName: 'Creatine',
  decision: 'request-changes',
  reviewerId: 'reviewer-1',
  reviewedAt: '2026-05-10T12:00:00.000Z',
  scope: { claimIds: [], reviewQueueItemIds: [], qualityFlags: [], reviewCategories: [], promotionBlockers: [] },
  clearsSoftPromotionBlockers: false,
  expiresAt: null,
  notes: [],
};

function Harness() {
  const { batch, reviewerId, setReviewerId, addToSession, removeFromSession, resetSession } = useReviewDecision();
  return (
    <div>
      <p>Reviewer: {reviewerId || 'none'}</p>
      <p>Decisions: {batch.decisions.length}</p>
      <button onClick={() => setReviewerId('reviewer-1')}>Set reviewer</button>
      <button onClick={() => addToSession(decision)}>Add decision</button>
      <button onClick={() => removeFromSession('dec-001')}>Remove decision</button>
      <button onClick={resetSession}>Reset</button>
    </div>
  );
}

describe('ReviewDecisionProvider localStorage persistence', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it('persists reviewer and pending decisions', async () => {
    render(<ReviewDecisionProvider><Harness /></ReviewDecisionProvider>);

    fireEvent.click(screen.getByText('Set reviewer'));
    fireEvent.click(screen.getByText('Add decision'));

    await waitFor(() => {
      const saved = JSON.parse(window.localStorage.getItem(STORAGE_KEY) ?? '{}');
      expect(saved.batch.reviewerId).toBe('reviewer-1');
      expect(saved.decisions).toHaveLength(1);
    });
  });

  it('hydrates a saved pending batch', async () => {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify({
      schemaVersion: '1.0.0',
      recordType: 'review-decision-batch',
      batch: { batchId: 'batch-saved', reviewerId: 'reviewer-1', reviewedAt: '2026-05-10T12:00:00.000Z', notes: [] },
      decisions: [decision],
    }));

    render(<ReviewDecisionProvider><Harness /></ReviewDecisionProvider>);

    expect(await screen.findByText('Reviewer: reviewer-1')).toBeInTheDocument();
    expect(screen.getByText('Decisions: 1')).toBeInTheDocument();
  });

  it('keeps reviewer but clears pending decisions on reset', async () => {
    render(<ReviewDecisionProvider><Harness /></ReviewDecisionProvider>);
    fireEvent.click(screen.getByText('Set reviewer'));
    fireEvent.click(screen.getByText('Add decision'));
    fireEvent.click(screen.getByText('Reset'));

    await waitFor(() => {
      const saved = JSON.parse(window.localStorage.getItem(STORAGE_KEY) ?? '{}');
      expect(saved.batch.reviewerId).toBe('reviewer-1');
      expect(saved.decisions).toHaveLength(0);
    });
  });

  it('removes a pending decision by id', async () => {
    render(<ReviewDecisionProvider><Harness /></ReviewDecisionProvider>);
    fireEvent.click(screen.getByText('Add decision'));
    fireEvent.click(screen.getByText('Remove decision'));

    await waitFor(() => {
      const saved = JSON.parse(window.localStorage.getItem(STORAGE_KEY) ?? '{}');
      expect(saved.decisions).toHaveLength(0);
    });
  });
});