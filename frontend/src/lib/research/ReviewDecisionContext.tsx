'use client';
import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { addDecision, createBatch } from './reviewDecisionBatch';
import type { ReviewDecision, ReviewDecisionBatch } from './types';

const STORAGE_KEY = 'biostack.reviewDecisionBatch.v1';

interface ReviewDecisionContextValue {
  batch: ReviewDecisionBatch;
  reviewerId: string;
  setReviewerId: (id: string) => void;
  addToSession: (decision: ReviewDecision) => void;
  resetSession: () => void;
}

const ReviewDecisionContext = createContext<ReviewDecisionContextValue | null>(null);

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every(item => typeof item === 'string');
}

function isReviewDecisionBatch(value: unknown): value is ReviewDecisionBatch {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return false;
  const batch = value as Partial<ReviewDecisionBatch>;
  return batch.schemaVersion === '1.0.0'
    && batch.recordType === 'review-decision-batch'
    && Boolean(batch.batch)
    && typeof batch.batch?.batchId === 'string'
    && typeof batch.batch?.reviewerId === 'string'
    && typeof batch.batch?.reviewedAt === 'string'
    && isStringArray(batch.batch?.notes)
    && Array.isArray(batch.decisions);
}

function loadSavedBatch(): ReviewDecisionBatch {
  if (typeof window === 'undefined') return createBatch('');
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return createBatch('');
    const parsed = JSON.parse(raw) as unknown;
    return isReviewDecisionBatch(parsed) ? parsed : createBatch('');
  } catch {
    return createBatch('');
  }
}

export function ReviewDecisionProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState(() => {
    const batch = loadSavedBatch();
    return { batch, reviewerId: batch.batch.reviewerId };
  });

  useEffect(() => {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(state.batch));
  }, [state.batch]);

  function setReviewerId(id: string) {
    setState(prev => ({
      reviewerId: id,
      batch: { ...prev.batch, batch: { ...prev.batch.batch, reviewerId: id } },
    }));
  }

  function addToSession(decision: ReviewDecision) {
    setState(prev => ({ ...prev, batch: addDecision(prev.batch, decision) }));
  }

  function resetSession() {
    setState(prev => ({ ...prev, batch: createBatch(prev.reviewerId) }));
  }

  return (
    <ReviewDecisionContext.Provider value={{ batch: state.batch, reviewerId: state.reviewerId, setReviewerId, addToSession, resetSession }}>
      {children}
    </ReviewDecisionContext.Provider>
  );
}

export function useReviewDecision() {
  const ctx = useContext(ReviewDecisionContext);
  if (!ctx) throw new Error('useReviewDecision must be within ReviewDecisionProvider');
  return ctx;
}
