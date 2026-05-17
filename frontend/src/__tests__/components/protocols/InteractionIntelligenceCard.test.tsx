import { render, screen } from '@testing-library/react';
import { expect, it } from 'vitest';
import { InteractionIntelligenceCard } from '@/components/protocols/InteractionIntelligenceCard';
import type { InteractionIntelligence } from '@/lib/types';

const mockIntelligence = {
  compositeScore: 82,
  score: { synergyScore: 14, redundancyPenalty: 3, interferencePenalty: 7 },
  summary: { synergies: 3, redundancies: 1, interferences: 1 },
  topFindings: [],
  counterfactuals: [{ removedCompound: 'X', deltaScore: 5, recommendation: 'test' }],
  swaps: [],
};

it('exposes HelpTip buttons for synergy, redundancy, interference, and counterfactual', () => {
  render(<InteractionIntelligenceCard intelligence={mockIntelligence as any} />);
  const buttons = screen.getAllByRole('button');
  expect(buttons.some(b => b.textContent?.includes('Synergies'))).toBe(true);
  expect(buttons.some(b => b.textContent?.includes('Redundancies'))).toBe(true);
  expect(buttons.some(b => b.textContent?.includes('Interferences'))).toBe(true);
  expect(buttons.some(b => b.textContent?.includes('Counterfactual'))).toBe(true);
});

it('renders a tracking CTA when showTrackingCta is true', () => {
  render(
    <InteractionIntelligenceCard
      intelligence={mockIntelligence as any}
      showTrackingCta
    />,
  );
  const link = screen.getByRole('link', { name: /start tracking/i });
  expect(link).toBeInTheDocument();
  expect(link).toHaveAttribute('href', '/protocols');
});

it('does not render a tracking CTA when showTrackingCta is omitted', () => {
  render(<InteractionIntelligenceCard intelligence={mockIntelligence as any} />);
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
