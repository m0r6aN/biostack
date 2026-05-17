import { render, screen } from '@testing-library/react';
import { CounterfactualLab } from '@/components/protocol/CounterfactualLab';

it('exposes HelpTip button for counterfactual in empty state', () => {
  render(<CounterfactualLab intelligence={null} />);
  const buttons = screen.getAllByRole('button');
  expect(buttons.some(b => b.textContent?.includes('Counterfactual'))).toBe(true);
});
