import { StackIntelligencePanel, panelContent } from '@/components/marketing/StackIntelligencePanel';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

describe('StackIntelligencePanel', () => {
  it('defaults to simple mode with plain-language guidance', () => {
    render(<StackIntelligencePanel />);

    expect(screen.getByText('Your stack is a system')).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Simple' })).toHaveAttribute('aria-selected', 'true');
    expect(
      screen.getByText(
        "BioStack shows how the things you're taking relate to each other — so you can spot overlap, avoid mistakes, and make smarter choices."
      )
    ).toBeInTheDocument();
    expect(screen.getByText('These work on the same thing')).toBeInTheDocument();
    expect(screen.getByText('You might not need both')).toBeInTheDocument();
    expect(screen.getByText('Some of these may overlap')).toBeInTheDocument();
    expect(panelContent.simple.insights).toContain('This is easy to miss when you’re tracking it in your head');
    expect(panelContent.simple.insights).toContain('Most people don’t realize this until weeks later');
    expect(screen.getByText('BPC-157')).toBeInTheDocument();
    expect(screen.getByText('TB-500')).toBeInTheDocument();
    expect(screen.getByText('NAD+')).toBeInTheDocument();
    expect(screen.getByText('MOTS-C')).toBeInTheDocument();
  });

  it('switches to technical mode without changing the panel structure', async () => {
    render(<StackIntelligencePanel />);

    fireEvent.click(screen.getByRole('tab', { name: 'Technical' }));

    expect(screen.getByText('Shared pathway')).toBeInTheDocument();
    expect(screen.getByText('Potential redundancy')).toBeInTheDocument();
    expect(
      await screen.findByText(
        'BioStack surfaces interaction structure, pathway overlap, and compound relationships inside a protocol before mistakes compound.'
      )
    ).toBeInTheDocument();
    expect(await screen.findByText('Synergy cluster identified')).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Technical' })).toHaveAttribute('aria-selected', 'true');
  });
});