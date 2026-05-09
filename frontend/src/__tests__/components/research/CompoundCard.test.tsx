import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { CompoundCard } from '@/components/research/CompoundCard';
import type { ResearchSummaryCompound } from '@/lib/research/types';

const blocked: ResearchSummaryCompound = {
  name: 'BPC-157',
  classification: 'Peptide',
  overallEvidenceTier: 'Limited',
  completeness: 'partial',
  needsReview: true,
  reviewQueueItemCount: 3,
  promotionReadiness: 'blocked',
  promotionBlockers: ['blocked: missing required authoritative support'],
  reviewDecisionIds: [],
  qualityFlags: [],
  reviewReasons: [],
};

const candidate: ResearchSummaryCompound = {
  name: 'Creatine',
  classification: 'Supplement',
  overallEvidenceTier: 'Strong',
  completeness: 'substantial',
  needsReview: false,
  reviewQueueItemCount: 0,
  promotionReadiness: 'candidate-for-promotion',
  promotionBlockers: [],
  reviewDecisionIds: [],
  qualityFlags: [],
  reviewReasons: [],
};

describe('CompoundCard', () => {
  it('renders compound name', () => {
    render(<CompoundCard compound={blocked} selected={false} onClick={() => {}} />);
    expect(screen.getByText('BPC-157')).toBeInTheDocument();
  });

  it('shows ReadinessBadge text for blocked', () => {
    render(<CompoundCard compound={blocked} selected={false} onClick={() => {}} />);
    expect(screen.getByText('Blocked')).toBeInTheDocument();
  });

  it('shows first blocker preview for blocked compounds', () => {
    render(<CompoundCard compound={blocked} selected={false} onClick={() => {}} />);
    expect(screen.getByText(/missing required authoritative support/)).toBeInTheDocument();
  });

  it('does not show blocker preview for candidates', () => {
    render(<CompoundCard compound={candidate} selected={false} onClick={() => {}} />);
    expect(screen.queryByText(/blocked:/)).not.toBeInTheDocument();
  });

  it('shows metadata: classification, tier, completeness', () => {
    render(<CompoundCard compound={blocked} selected={false} onClick={() => {}} />);
    expect(screen.getByText('Peptide')).toBeInTheDocument();
    expect(screen.getByText('Limited')).toBeInTheDocument();
    expect(screen.getByText('partial')).toBeInTheDocument();
  });

  it('calls onClick when clicked', () => {
    const onClick = vi.fn();
    render(<CompoundCard compound={blocked} selected={false} onClick={onClick} />);
    screen.getByRole('button').click();
    expect(onClick).toHaveBeenCalled();
  });
});
