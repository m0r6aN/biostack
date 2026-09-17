import { OverlapResults } from '@/components/knowledge/OverlapResults';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

describe('OverlapResults', () => {
  it('shows contextual recommendations only after overlap insight exists', () => {
    render(
      <OverlapResults
        inputCount={2}
        flags={[
          {
            compoundNames: ['BPC-157', 'TB-500'],
            overlapType: 'Potential redundancy',
            pathwayTag: 'tissue-repair',
            description: 'Educational reference only.',
            evidenceConfidence: 'Medium',
          },
        ]}
      />
    );

    expect(screen.getByText('Potential redundancy')).toBeInTheDocument();
    expect(screen.getByText('Common additions')).toBeInTheDocument();
    expect(screen.getByText('Collagen peptides')).toBeInTheDocument();
    expect(
      screen.getByText('Some people look at these examples in similar recovery-oriented overlap contexts.')
    ).toBeInTheDocument();
    expect(screen.getByText('Some links may be affiliate links.')).toBeInTheDocument();
    expect(screen.getAllByText('View example products')[0]).toBeInTheDocument();
  });

  it('does not render the recommendations block when no overlaps exist', () => {
    render(<OverlapResults flags={[]} inputCount={2} />);

    expect(screen.queryByText('Common additions')).not.toBeInTheDocument();
  });

  it('does not render overlap UI with fewer than 2 inputs', () => {
    render(<OverlapResults flags={[]} inputCount={1} />);

    expect(screen.queryByText('No pathway overlaps detected for selected compounds.')).not.toBeInTheDocument();
    expect(screen.queryByText('Common additions')).not.toBeInTheDocument();
  });

  // Owner ruling 2026-09-16, extended 2026-09-17: without the reviewed_relationship_graph
  // entitlement, POST /api/v1/knowledge/overlap-check omits description/evidenceConfidence
  // entirely. The reduced view must still show the flagged pair and severity, with no empty
  // "Description" slot or dangling confidence label, plus a calm affordance for what Operator adds.
  it('renders the reduced shape well when no reasoning is present (anonymous/Observer)', () => {
    render(
      <OverlapResults
        inputCount={2}
        flags={[
          {
            compoundNames: ['BPC-157', 'TB-500'],
            overlapType: 'Potential redundancy',
            pathwayTag: 'tissue-repair',
          },
        ]}
      />
    );

    expect(screen.getByText('BPC-157 × TB-500')).toBeInTheDocument();
    expect(screen.getByText('Potential redundancy')).toBeInTheDocument();
    expect(screen.getByText('Pathway: tissue-repair')).toBeInTheDocument();

    expect(screen.queryByText(/^Confidence:/)).not.toBeInTheDocument();
    expect(screen.getByText('See why these pairs are flagged')).toBeInTheDocument();
    expect(
      screen.getByText(
        'Operator adds the reasoning behind each flagged pair, including shared pathways and a confidence read.'
      )
    ).toBeInTheDocument();
    expect(screen.getByText('Compare plans')).toBeInTheDocument();
  });

  it('does not render the entitlement affordance when reasoning is present (Operator)', () => {
    render(
      <OverlapResults
        inputCount={2}
        flags={[
          {
            compoundNames: ['BPC-157', 'TB-500'],
            overlapType: 'Potential redundancy',
            pathwayTag: 'tissue-repair',
            description: 'Educational reference only.',
            evidenceConfidence: 'Medium',
          },
        ]}
      />
    );

    expect(screen.getByText('Educational reference only.')).toBeInTheDocument();
    expect(screen.getByText('Confidence: Medium')).toBeInTheDocument();
    expect(screen.queryByText('See why these pairs are flagged')).not.toBeInTheDocument();
    expect(screen.queryByText('Compare plans')).not.toBeInTheDocument();
  });
});
