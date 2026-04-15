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
});
