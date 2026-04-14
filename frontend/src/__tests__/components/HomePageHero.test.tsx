import { LandingHero } from '@/components/marketing/LandingHero';
import { render, screen } from '@testing-library/react';
import type { ComponentProps } from 'react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: ComponentProps<'a'>) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

describe('HomePage hero', () => {
  it('frames BioStack as the protocol intelligence system', () => {
    render(<LandingHero />);

    expect(screen.getByRole('heading', { name: 'BioStack Protocol Console' })).toBeInTheDocument();
    expect(screen.getByText('Stop guessing where to start - or what your stack is actually doing.')).toBeInTheDocument();
    expect(
      screen.getByText(
        "Add what you're using - or thinking about using. BioStack shows how it fits, what overlaps, and what actually works together."
      )
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Build My Protocol/ })).toHaveAttribute('href', '/onboarding');
    expect(screen.getByRole('link', { name: /Map My Current Stack/ })).toHaveAttribute('href', '/onboarding?mode=existing');
    expect(screen.getByRole('link', { name: 'Explore Calculators' })).toHaveAttribute('href', '/tools');
    expect(screen.getByText('Start with one compound or build a full stack.')).toBeInTheDocument();
    expect(screen.getByText('See how your current compounds fit together.')).toBeInTheDocument();
    expect(
      screen.getByText('Some compounds overlap. Some work better together. BioStack shows the difference.')
    ).toBeInTheDocument();
    expect(
      screen.getByText('Conflicting advice, overlapping compounds, and guesswork make this harder than it should be.')
    ).toBeInTheDocument();
  });
});
