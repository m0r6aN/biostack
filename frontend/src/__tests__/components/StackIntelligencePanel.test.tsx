import { StackIntelligencePanel, panelContent } from '@/components/marketing/StackIntelligencePanel';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

describe('StackIntelligencePanel', () => {
  it('defaults to an empty helper-driven state without demo compounds', () => {
    render(<StackIntelligencePanel />);

    expect(screen.getByText('Protocol preview')).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Protocol' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getAllByText('No inputs detected')[0]).toBeInTheDocument();
    expect(screen.getByText('No intelligence is shown until an input exists.')).toBeInTheDocument();
    expect(screen.getByText('Add one item.')).toBeInTheDocument();
    expect(screen.queryByText('BPC-157, TB-500, Creatine')).not.toBeInTheDocument();
    expect(panelContent.simple.relationshipGroups).toEqual([]);
  });

  it('shows one earned relationship when relationship evidence is supplied', async () => {
    render(
      <StackIntelligencePanel
        compoundNames={['BPC-157', 'TB-500']}
        relationshipCandidates={[{ type: 'overlap', label: 'BPC-157 + TB-500', detail: 'tissue-repair: Educational reference only.' }]}
      />
    );

    expect(screen.getByText('Relationship detected. One earned outcome is shown.')).toBeInTheDocument();
    expect(screen.getByText('BPC-157 + TB-500')).toBeInTheDocument();
    expect(screen.getByText('tissue-repair: Educational reference only.')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('tab', { name: 'Evidence' }));
    expect(screen.getByRole('tab', { name: 'Evidence' })).toHaveAttribute('aria-selected', 'true');
    expect(await screen.findByText('Only one earned relationship outcome is shown.')).toBeInTheDocument();
  });
});
