import { fireEvent, render, screen } from '@testing-library/react';
import { expect, it, vi } from 'vitest';
import { InteractionIntelligenceCard } from '@/components/protocols/InteractionIntelligenceCard';
import type { InteractionIntelligence } from '@/lib/types';

const mockIntelligence: InteractionIntelligence = {
  compositeScore: 82,
  score: { synergyScore: 14, redundancyPenalty: 3, interferencePenalty: 7 },
  summary: { synergies: 3, redundancies: 1, interferences: 1 },
  topFindings: [],
  interactions: [],
  counterfactuals: [{
    removedCompound: 'X',
    variantScore: 87,
    deltaScore: 5,
    deltaPercent: (5 / 82) * 100,
    verdict: 'improves',
    recommendation: 'test',
    summary: { synergies: 3, redundancies: 0, interferences: 1 },
    topFindings: [],
  }],
  swaps: [],
};

it('exposes HelpTip buttons for synergy, redundancy, interference, and counterfactual', () => {
  render(<InteractionIntelligenceCard intelligence={mockIntelligence} />);
  const buttons = screen.getAllByRole('button');
  expect(buttons.some(b => b.textContent?.includes('Synergies'))).toBe(true);
  expect(buttons.some(b => b.textContent?.includes('Redundancies'))).toBe(true);
  expect(buttons.some(b => b.textContent?.includes('Interferences'))).toBe(true);
  expect(buttons.some(b => b.textContent?.includes('Counterfactual'))).toBe(true);
});

it('hands tracking to the parent instead of linking to the current protocols route', () => {
  const onTrackingRequest = vi.fn();
  render(<InteractionIntelligenceCard intelligence={mockIntelligence} showTrackingCta onTrackingRequest={onTrackingRequest} />);
  fireEvent.click(screen.getByRole('button', { name: 'Save this stack to start tracking' }));
  expect(onTrackingRequest).toHaveBeenCalledTimes(1);
  expect(screen.queryByRole('link', { name: /start tracking/i })).not.toBeInTheDocument();
});

it('does not render a tracking CTA when showTrackingCta is omitted', () => {
  render(<InteractionIntelligenceCard intelligence={mockIntelligence} />);
  expect(screen.queryByRole('link', { name: /start tracking/i })).not.toBeInTheDocument();
});

const intelligenceWithSwap = {
  compositeScore: 82,
  score: { synergyScore: 14, redundancyPenalty: 3, interferencePenalty: 7 },
  summary: { synergies: 3, redundancies: 1, interferences: 1 },
  topFindings: [],
  counterfactuals: [{ removedCompound: 'BPC-157', deltaScore: 5, recommendation: 'remove note' }],
  swaps: [
    {
      originalCompound: 'TB-500',
      candidateCompound: 'BPC-157',
      deltaScore: 4.2,
      recommendation: 'swap note',
      reasons: ['improves_goal_alignment', 'stronger_evidence'],
    },
  ],
};

it('renders the swap as a what-if comparison without prescriptive "Replace" or "+X pts" headline labels', () => {
  const { container } = render(
    <InteractionIntelligenceCard intelligence={intelligenceWithSwap as unknown as InteractionIntelligence} />
  );

  // New observational labels render.
  expect(screen.getByText('What-if comparison')).toBeInTheDocument();
  expect(screen.getByText('Compare TB-500 vs BPC-157')).toBeInTheDocument();
  expect(screen.getByText('internal score delta: +4.2')).toBeInTheDocument();
  expect(screen.getByText('Remove-one scenario: BPC-157')).toBeInTheDocument();

  // Retired prescriptive labels and headline-row delta badges must be gone.
  expect(screen.queryByText('Best swap')).not.toBeInTheDocument();
  expect(screen.queryByText('Best remove-one scenario: BPC-157')).not.toBeInTheDocument();
  expect(screen.queryByText('Replace TB-500 → BPC-157')).not.toBeInTheDocument();
  expect(screen.queryByText(/^\+4\.2 pts$/)).not.toBeInTheDocument();

  // Defensive: the bare "+N.N pts" badge format must not appear anywhere in the card.
  expect(container.textContent ?? '').not.toMatch(/\+\d+(?:\.\d+)?\s+pts/);

  // Reason chip rename: 'improves goal alignment' was prescriptive; the new label is observational.
  expect(screen.getByText('closer goal alignment')).toBeInTheDocument();
  expect(screen.queryByText('improves goal alignment')).not.toBeInTheDocument();
});
