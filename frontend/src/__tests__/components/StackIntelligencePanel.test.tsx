import { StackIntelligencePanel, panelContent } from '@/components/marketing/StackIntelligencePanel';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

describe('StackIntelligencePanel', () => {
  it('defaults to an empty helper-driven state without demo compounds', () => {
    render(<StackIntelligencePanel />);

    expect(screen.getByText('Stack Check')).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'List' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getAllByText('No stack items added yet.')[0]).toBeInTheDocument();
    expect(screen.getByText('Need one more item for a stack finding.')).toBeInTheDocument();
    expect(screen.getByText('Paste your stack one item per line.')).toBeInTheDocument();
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

    expect(screen.getAllByText('Possible overlap found.')[0]).toBeInTheDocument();
    expect(screen.getByText('BPC-157 + TB-500')).toBeInTheDocument();
    expect(screen.getByText('tissue-repair: Educational reference only.')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('tab', { name: 'Evidence' }));
    expect(screen.getByRole('tab', { name: 'Evidence' })).toHaveAttribute('aria-selected', 'true');
    expect(await screen.findByText('Evidence view')).toBeInTheDocument();
    expect(screen.getByText('Public evidence + paid analysis')).toBeInTheDocument();
    expect(screen.queryByText(/free analysis/i)).not.toBeInTheDocument();
    await waitFor(() => {
      expect(screen.queryByText('One earned relationship outcome emitted.')).not.toBeInTheDocument();
    });
  });
});
