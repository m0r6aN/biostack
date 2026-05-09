import { render, screen } from '@testing-library/react';
import { ReadinessBadge } from '@/components/research/ReadinessBadge';

describe('ReadinessBadge', () => {
  it('renders "Blocked" for blocked readiness', () => {
    render(<ReadinessBadge readiness="blocked" />);
    expect(screen.getByText('Blocked')).toBeInTheDocument();
  });

  it('renders "Review Required" for review-required', () => {
    render(<ReadinessBadge readiness="review-required" />);
    expect(screen.getByText('Review Required')).toBeInTheDocument();
  });

  it('renders "Candidate" for candidate-for-promotion', () => {
    render(<ReadinessBadge readiness="candidate-for-promotion" />);
    expect(screen.getByText('Candidate')).toBeInTheDocument();
  });

  it('applies red styling for blocked', () => {
    const { container } = render(<ReadinessBadge readiness="blocked" />);
    expect(container.firstChild).toHaveClass('text-rose-400');
  });

  it('applies amber styling for review-required', () => {
    const { container } = render(<ReadinessBadge readiness="review-required" />);
    expect(container.firstChild).toHaveClass('text-amber-400');
  });

  it('applies green styling for candidate', () => {
    const { container } = render(<ReadinessBadge readiness="candidate-for-promotion" />);
    expect(container.firstChild).toHaveClass('text-emerald-400');
  });
});
