import { render, screen } from '@testing-library/react';
import { InteractionIntelligenceCard } from '@/components/protocols/InteractionIntelligenceCard';

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
