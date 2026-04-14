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
    expect(screen.getByText('Stop guessing what your protocol is actually doing.')).toBeInTheDocument();
    expect(
      screen.getByText(
        "Start simple. Get it right. Then go deeper when you're ready. Track compounds, avoid overlap, and turn daily signal into clarity."
      )
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Build My Protocol/ })).toHaveAttribute('href', '/onboarding');
    expect(screen.getByRole('link', { name: /Map My Current Stack/ })).toHaveAttribute('href', '/onboarding?mode=existing');
    expect(screen.getByRole('link', { name: 'Explore Calculators' })).toHaveAttribute('href', '/tools');
    expect(
      screen.getByText('Most protocols fail from overlap, drift, and conflicting guidance - not intent.')
    ).toBeInTheDocument();
  });
});
