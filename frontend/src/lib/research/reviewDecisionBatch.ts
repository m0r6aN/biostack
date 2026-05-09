import type { ReviewDecision, ReviewDecisionBatch } from './types';

export function createBatch(reviewerId: string): ReviewDecisionBatch {
  return {
    schemaVersion: '1.0.0',
    recordType: 'review-decision-batch',
    batch: {
      batchId: `batch-${Date.now()}`,
      reviewerId,
      reviewedAt: new Date().toISOString(),
      notes: [],
    },
    decisions: [],
  };
}

export function addDecision(
  batch: ReviewDecisionBatch,
  decision: ReviewDecision
): ReviewDecisionBatch {
  return { ...batch, decisions: [...batch.decisions, decision] };
}

export function toJson(batch: ReviewDecisionBatch): string {
  return JSON.stringify(batch, null, 2);
}
