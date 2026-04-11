import { OverlapResults } from '@/components/knowledge/OverlapResults';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

describe('OverlapResults', () => {
  it('shows contextual recommendations only after overlap insight exists', () => {
    render(
      <OverlapResults
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
    render(<OverlapResults flags={[]} />);

    expect(screen.queryByText('Common additions')).not.toBeInTheDocument();
  });
});