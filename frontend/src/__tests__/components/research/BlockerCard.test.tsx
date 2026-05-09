import { render, screen } from '@testing-library/react';
import { BlockerCard } from '@/components/research/BlockerCard';

describe('BlockerCard', () => {
  it('renders the blocker text', () => {
    render(<BlockerCard blocker="blocked: missing required authoritative support" />);
    expect(screen.getByText(/missing required authoritative support/)).toBeInTheDocument();
  });

  it('applies red styling for blocked: prefix', () => {
    const { container } = render(<BlockerCard blocker="blocked: some issue" />);
    expect(container.firstChild).toHaveClass('border-rose-400/25');
  });

  it('applies amber styling for review-required: prefix', () => {
    const { container } = render(<BlockerCard blocker="review-required: needs review" />);
    expect(container.firstChild).toHaveClass('border-amber-400/25');
  });

  it('shows ✕ icon for hard blockers', () => {
    render(<BlockerCard blocker="blocked: something" />);
    expect(screen.getByText('✕')).toBeInTheDocument();
  });

  it('shows ⚠ icon for review-required', () => {
    render(<BlockerCard blocker="review-required: something" />);
    expect(screen.getByText('⚠')).toBeInTheDocument();
  });
});
