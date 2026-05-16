import { ResolutionPlanItem } from '@/components/research/ResolutionPlanItem';
import type { ResearchTaskQueueItem, ReviewResolutionPlanItem } from '@/lib/research/types';
import { fireEvent, render, screen } from '@testing-library/react';

const item: ReviewResolutionPlanItem = {
  itemId: 'bpc-157-resolution-1', compoundName: 'BPC-157',
  readiness: 'blocked', severity: 'blocked',
  resolutionType: 'add-authoritative-source',
  issue: 'blocked: missing required authoritative support',
  recommendedAction: 'Attach an A1/A2 source before promotion.',
  relatedBlockers: ['blocked: missing required authoritative support'],
  relatedReviewQueueItemIds: [],
  relatedQualityFlags: ['missing-authoritative-support'],
};

describe('ResolutionPlanItem', () => {
  it('renders resolution type', () => {
    render(<ResolutionPlanItem item={item} />);
    expect(screen.getByText(/Suggested task:\s*add-authoritative-source/i)).toBeInTheDocument();
  });

  it('renders the issue text', () => {
    render(<ResolutionPlanItem item={item} />);
    expect(screen.getByText(/missing required authoritative support/)).toBeInTheDocument();
  });

  it('renders the recommended action', () => {
    render(<ResolutionPlanItem item={item} />);
    expect(screen.getByText(/Attach an A1\/A2 source before promotion/i)).toBeInTheDocument();
  });

  it('does not label remediation as not automatic', () => {
    render(<ResolutionPlanItem item={item} />);
    expect(screen.queryByText(/not automatic/i)).not.toBeInTheDocument();
  });

  it('marks matching remediation task as queued for next round', () => {
    const task: ResearchTaskQueueItem = {
      taskId: 'bpc-157-review-source-expansion',
      taskType: 'expand-review-sources',
      compoundName: 'BPC-157',
      aliases: [],
      categories: [],
      classification: 'Peptide',
      priority: 'high',
      requestIds: [],
      requesterIds: [],
      firstRequestedAtUtc: '2026-05-13T00:00:00Z',
      latestRequestedAtUtc: '2026-05-13T00:00:00Z',
      rationales: [],
      notes: [],
      suggestedResearchDirectives: [],
      targetEvidencePath: 'research/input/evidence/bpc-157.evidence.json',
      requiredSchema: 'evidence-packet.schema.json',
      remediationPlanItemIds: ['bpc-157-resolution-1'],
    };

    render(<ResolutionPlanItem item={item} queuedTask={task} />);

    expect(screen.getByText('Auto-queued for next agent round')).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: /apply bpc-157-resolution-1/i })).toBeChecked();
  });

  it('calls toggle handler when an unqueued remediation item is clicked', () => {
    const onToggle = vi.fn();
    render(<ResolutionPlanItem item={item} onToggleNextRound={onToggle} />);

    fireEvent.click(screen.getByRole('checkbox', { name: /apply bpc-157-resolution-1/i }));

    expect(onToggle).toHaveBeenCalledTimes(1);
  });

  it('marks pending decision batch item as queued', () => {
    render(<ResolutionPlanItem item={item} pendingDecisionId="apply-remediation-bpc-157-resolution-1" onToggleNextRound={vi.fn()} />);

    expect(screen.getByText('Queued in decision batch')).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: /apply bpc-157-resolution-1/i })).toBeChecked();
  });

  it('renders quality flags', () => {
    render(<ResolutionPlanItem item={item} />);
    expect(screen.getByText('missing-authoritative-support')).toBeInTheDocument();
  });
});
