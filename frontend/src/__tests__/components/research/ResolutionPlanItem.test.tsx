import { render, screen } from '@testing-library/react';
import { ResolutionPlanItem } from '@/components/research/ResolutionPlanItem';
import type { ReviewResolutionPlanItem } from '@/lib/research/types';

const item: ReviewResolutionPlanItem = {
  itemId: 'bpc-157-resolution-1', compoundName: 'BPC-157',
  readiness: 'blocked', severity: 'blocked',
  resolutionType: 'add-authoritative-source',
  issue: 'blocked: missing required authoritative support',
  recommendedAction: 'Attach an A1/A2 source before promotion.',
  relatedBlockers: ['blocked: missing required authoritative support'],
  relatedQualityFlags: ['missing-authoritative-support'],
};

describe('ResolutionPlanItem', () => {
  it('renders resolution type', () => {
    render(<ResolutionPlanItem item={item} />);
    expect(screen.getByText('add-authoritative-source')).toBeInTheDocument();
  });

  it('renders the issue text', () => {
    render(<ResolutionPlanItem item={item} />);
    expect(screen.getByText(/missing required authoritative support/)).toBeInTheDocument();
  });

  it('renders the recommended action', () => {
    render(<ResolutionPlanItem item={item} />);
    expect(screen.getByText(/Attach an A1\/A2 source before promotion/i)).toBeInTheDocument();
  });

  it('renders quality flags', () => {
    render(<ResolutionPlanItem item={item} />);
    expect(screen.getByText('missing-authoritative-support')).toBeInTheDocument();
  });
});
