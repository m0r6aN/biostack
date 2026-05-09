'use client';
import { createContext, useContext, useState, type ReactNode } from 'react';
import type { ReviewDecision, ReviewDecisionBatch } from './types';
import { createBatch, addDecision } from './reviewDecisionBatch';

interface ReviewDecisionContextValue {
  batch: ReviewDecisionBatch;
  reviewerId: string;
  setReviewerId: (id: string) => void;
  addToSession: (decision: ReviewDecision) => void;
}

const ReviewDecisionContext = createContext<ReviewDecisionContextValue | null>(null);

export function ReviewDecisionProvider({ children }: { children: ReactNode }) {
  const [reviewerId, setReviewerIdState] = useState('');
  const [batch, setBatch] = useState<ReviewDecisionBatch>(() => createBatch(''));

  function setReviewerId(id: string) {
    setReviewerIdState(id);
    setBatch(prev => ({ ...prev, batch: { ...prev.batch, reviewerId: id } }));
  }

  function addToSession(decision: ReviewDecision) {
    setBatch(prev => addDecision(prev, decision));
  }

  return (
    <ReviewDecisionContext.Provider value={{ batch, reviewerId, setReviewerId, addToSession }}>
      {children}
    </ReviewDecisionContext.Provider>
  );
}

export function useReviewDecision() {
  const ctx = useContext(ReviewDecisionContext);
  if (!ctx) throw new Error('useReviewDecision must be within ReviewDecisionProvider');
  return ctx;
}
