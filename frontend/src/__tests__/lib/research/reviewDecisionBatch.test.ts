import { addDecision, createBatch, toJson } from '@/lib/research/reviewDecisionBatch';
import type { ReviewDecision } from '@/lib/research/types';

const makeDecision = (overrides: Partial<ReviewDecision> = {}): ReviewDecision => ({
  decisionId: 'dec-001',
  compoundName: 'BPC-157',
  decision: 'request-changes',
  reviewerId: 'reviewer-1',
  reviewedAt: '2026-05-05T10:00:00Z',
  scope: { claimIds: [], reviewQueueItemIds: [], qualityFlags: [], reviewCategories: [], promotionBlockers: [] },
  clearsSoftPromotionBlockers: false,
  expiresAt: null,
  notes: ['Needs authoritative source'],
  ...overrides,
});

describe('createBatch', () => {
  it('creates empty batch with given reviewerId', () => {
    const b = createBatch('reviewer-1');
    expect(b.decisions).toHaveLength(0);
    expect(b.batch.reviewerId).toBe('reviewer-1');
    expect(b.recordType).toBe('review-decision-batch');
    expect(b.schemaVersion).toBe('1.0.0');
  });
});

describe('addDecision', () => {
  it('appends a decision', () => {
    const b = addDecision(createBatch('reviewer-1'), makeDecision());
    expect(b.decisions).toHaveLength(1);
    expect(b.decisions[0].compoundName).toBe('BPC-157');
  });

  it('does not mutate the original', () => {
    const original = createBatch('reviewer-1');
    addDecision(original, makeDecision());
    expect(original.decisions).toHaveLength(0);
  });

  it('accumulates multiple decisions', () => {
    let b = createBatch('reviewer-1');
    b = addDecision(b, makeDecision({ decisionId: 'dec-001', compoundName: 'BPC-157' }));
    b = addDecision(b, makeDecision({ decisionId: 'dec-002', compoundName: 'Creatine' }));
    expect(b.decisions).toHaveLength(2);
  });
});

describe('toJson', () => {
  it('returns valid JSON matching schema structure', () => {
    const b = addDecision(createBatch('reviewer-1'), makeDecision());
    const parsed = JSON.parse(toJson(b));
    expect(parsed.schemaVersion).toBe('1.0.0');
    expect(parsed.recordType).toBe('review-decision-batch');
    expect(parsed.decisions).toHaveLength(1);
  });

  it('pretty-prints', () => {
    expect(toJson(createBatch('r'))).toContain('\n');
  });
});
