import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { SuggestionCard } from '@/components/suggestions/SuggestionCard';
import type { EarnedSuggestion } from '@/lib/earnedSuggestions';

describe('SuggestionCard', () => {
  it('renders one earned suggestion with grounded reasoning', () => {
    render(<SuggestionCard suggestion={suggestion} />);

    expect(screen.getByLabelText(/earned suggestion/i)).toBeInTheDocument();
    expect(screen.getByText('Reduce stack redundancy')).toBeInTheDocument();
    expect(screen.getByText(/overlapping or crowded items/i)).toBeInTheDocument();
    expect(screen.getByText('Based on overlapping inputs and a weak or unclear 7-day review.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /review findings/i })).toBeInTheDocument();
  });

  it('supports optional dismissal', async () => {
    const onDismiss = vi.fn();
    const user = userEvent.setup();

    render(<SuggestionCard suggestion={suggestion} onDismiss={onDismiss} />);

    await user.click(screen.getByRole('button', { name: /not now/i }));

    expect(onDismiss).toHaveBeenCalledTimes(1);
  });
});

const suggestion: EarnedSuggestion = {
  type: 'tighten_stack',
  title: 'Reduce stack redundancy',
  explanation: 'Overlapping or crowded items can make the next finding harder to trust.',
  reasoning: 'Based on overlapping inputs and a weak or unclear 7-day review.',
  actionLabel: 'Review findings',
};
