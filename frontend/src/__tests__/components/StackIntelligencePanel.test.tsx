import { StackIntelligencePanel, panelContent } from '@/components/marketing/StackIntelligencePanel';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

describe('StackIntelligencePanel', () => {
  it('defaults to simple mode with plain-language guidance', () => {
    render(<StackIntelligencePanel />);

    expect(screen.getByText('Protocol preview')).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Protocol' })).toHaveAttribute('aria-selected', 'true');
    expect(
      screen.getByText(
        'See how your compounds relate - where they overlap, where they support each other, and what to do next.'
      )
    ).toBeInTheDocument();
    expect(screen.getByText('Compounds added')).toBeInTheDocument();
    expect(screen.getByText('Relationships mapped')).toBeInTheDocument();
    expect(screen.getByText('Next step ready')).toBeInTheDocument();
    expect(screen.getByText('Overlap')).toBeInTheDocument();
    expect(screen.getByText('Synergy')).toBeInTheDocument();
    expect(screen.getByText('Support')).toBeInTheDocument();
    expect(screen.getByText('Shared tissue-repair focus')).toBeInTheDocument();
    expect(screen.getByText('Common recovery stack pairing')).toBeInTheDocument();
    expect(screen.getByText('Recovery and performance baseline')).toBeInTheDocument();
    expect(screen.getByText('Relationship summary: overlap + synergy')).toBeInTheDocument();
    expect(
      screen.getByText('BPC-157 and TB-500 share tissue-repair focus, and are often used together for recovery support.')
    ).toBeInTheDocument();
    expect(panelContent.simple.insights).toContain(
      'Overlap does not automatically mean bad. It means the shared role is worth understanding.'
    );
    expect(screen.getByText('BPC-157, TB-500, Creatine')).toBeInTheDocument();
    expect(screen.getByText('Add dose schedule -> track recovery + sleep -> evaluate after 7 days')).toBeInTheDocument();
  });

  it('switches to technical mode without changing the panel structure', async () => {
    render(<StackIntelligencePanel />);

    fireEvent.click(screen.getByRole('tab', { name: 'Evidence' }));

    expect(screen.getByText('Tissue-repair pathway alignment')).toBeInTheDocument();
    expect(screen.getByText('Recovery context may justify pairing')).toBeInTheDocument();
    expect(
      await screen.findByText(
        'BioStack ties your inputs to relationship type, evidence confidence, pathway structure, and observable signal over time.'
      )
    ).toBeInTheDocument();
    expect(await screen.findByText('Relationship summary: overlap + support')).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Evidence' })).toHaveAttribute('aria-selected', 'true');
  });
});
