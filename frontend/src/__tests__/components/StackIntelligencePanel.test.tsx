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
        'Live protocol state with compounds added, guidance structured, overlap surfaced, and the next tracking step ready.'
      )
    ).toBeInTheDocument();
    expect(screen.getByText('Tracking started')).toBeInTheDocument();
    expect(screen.getByText('Baseline captured')).toBeInTheDocument();
    expect(screen.getByText('Day 7 review pending')).toBeInTheDocument();
    expect(screen.getByText('Typical range:')).toBeInTheDocument();
    expect(screen.getByText('0.25mg - 4mg weekly')).toBeInTheDocument();
    expect(screen.getByText('Common pattern:')).toBeInTheDocument();
    expect(screen.getByText('1-3 doses/week')).toBeInTheDocument();
    expect(screen.getByText(/Adjusted \(your profile\): more precise/)).toBeInTheDocument();
    expect(screen.getByText('Shared tissue-repair pathway')).toBeInTheDocument();
    expect(screen.getByText('Correlation ready')).toBeInTheDocument();
    expect(screen.getByText('BPC-157 + TB-500 flagged for overlapping tissue-repair pathways.')).toBeInTheDocument();
    expect(panelContent.simple.insights).toContain('Typical range and common frequency are separated from evidence strength.');
    expect(screen.getByText('BPC-157')).toBeInTheDocument();
    expect(screen.getByText('TB-500')).toBeInTheDocument();
    expect(screen.getByText('Creatine')).toBeInTheDocument();
    expect(screen.getByText('Add dose schedule -> track recovery + sleep -> evaluate after 7 days')).toBeInTheDocument();
  });

  it('switches to technical mode without changing the panel structure', async () => {
    render(<StackIntelligencePanel />);

    fireEvent.click(screen.getByRole('tab', { name: 'Evidence' }));

    expect(screen.getByText('Moderate evidence')).toBeInTheDocument();
    expect(screen.getByText('Signal baseline')).toBeInTheDocument();
    expect(
      await screen.findByText(
        'BioStack ties protocol inputs to typical ranges, evidence confidence, pathway structure, and observable signal over time.'
      )
    ).toBeInTheDocument();
    expect(await screen.findByText('2 overlapping pathways detected')).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Evidence' })).toHaveAttribute('aria-selected', 'true');
  });
});
