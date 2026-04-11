import { LandingHero } from '@/components/marketing/LandingHero';
import { fireEvent, render, screen } from '@testing-library/react';
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
  it('defaults to the low-friction onboarding mode for new users', () => {
    render(<LandingHero />);

    expect(screen.getByRole('button', { name: 'New to this' })).toHaveAttribute('aria-pressed', 'true');
    expect(
      screen.getByRole('heading', { name: 'Not Sure Where to Start?' })
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        "BioStack helps you track what you're taking, understand how things interact, and avoid common mistakes from day one."
      )
    ).toBeInTheDocument();
    expect(
      screen.getByText('Start from scratch or add what you already have')
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Start Free' })).toHaveAttribute('href', '/onboarding');
    expect(screen.getByRole('link', { name: 'Try the Calculator First' })).toHaveAttribute(
      'href',
      '/tools/reconstitution-calculator'
    );
  });

  it('switches to the existing-stack message when requested', () => {
    render(<LandingHero />);

    fireEvent.click(screen.getByRole('button', { name: 'Already have a stack' }));

    expect(
      screen.getByRole('heading', { name: 'See How Your Stack Works Together' })
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        "Add what you're taking and BioStack shows what overlaps, what’s unnecessary, and what actually makes sense together."
      )
    ).toBeInTheDocument();
    expect(screen.getByText('Add 2–3 things and see how they connect')).toBeInTheDocument();
    expect(
      screen.getByText(
        'See what overlaps, what may be unnecessary, and what could work better together.'
      )
    ).toBeInTheDocument();
  });
});