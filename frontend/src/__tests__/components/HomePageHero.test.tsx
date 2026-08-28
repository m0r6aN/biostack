import { LandingHero } from '@/components/marketing/LandingHero';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
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
  it('routes users into the audience and analyzer entry paths', async () => {
    const user = userEvent.setup();
    const listener = vi.fn();
    window.addEventListener('biostack:landing_path_selected', listener);

    render(<LandingHero />);

    expect(
      screen.getByRole('heading', { name: /No prescriptions\. No guesswork\.\s*Just what's known\./ })
    ).toBeInTheDocument();
    expect(
      screen.getByText(/A free, public library of what the research says about peptides and similar compounds/)
    ).toBeInTheDocument();
    // Banned prescriptive copy must not appear on the landing hero.
    expect(screen.queryByText(/What to take\. How to use it\./)).not.toBeInTheDocument();
    expect(screen.queryByText(/optimize over time/)).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Start free/ })).toHaveAttribute('href', '/start');
    expect(screen.getByRole('link', { name: /Explore the evidence/ })).toHaveAttribute('href', '/knowledge');
    expect(screen.getByRole('link', { name: /I work with clients/ })).toHaveAttribute('href', '/providers');
    expect(screen.getByRole('link', { name: /Analyze a protocol/ })).toHaveAttribute('href', '/tools/analyzer');
    // Exactly one entry path may target the analyzer (Round 1 F1: no duplicate destinations).
    expect(screen.getAllByRole('link', { name: /Analyze/ })).toHaveLength(1);
    expect(screen.getByRole('link', { name: 'Need to calculate dose volume or reconstitution? → Start here' })).toHaveAttribute('href', '/tools');
    expect(screen.getByRole('link', { name: 'See Observer, Operator, and Commander' })).toHaveAttribute('href', '/pricing');
    expect(screen.getByText('Library')).toBeInTheDocument();
    expect(screen.getByText('Browse compound dossiers with evidence tiers, sources, and mechanism summaries.')).toBeInTheDocument();
    expect(screen.getByText('Keep what you look up and set up your own records, guided from the first step.')).toBeInTheDocument();
    expect(screen.getByText('Provider')).toBeInTheDocument();
    expect(screen.getByText('Request access to the provider pilot for permissioned observational workflows.')).toBeInTheDocument();
    expect(screen.getByText('Analyzer')).toBeInTheDocument();
    expect(screen.getByText('Review a pasted or uploaded stack against the evidence, with overlap and timeline context.')).toBeInTheDocument();
    expect(screen.getByText('Pilot request')).toBeInTheDocument();
    expect(screen.queryByText('Multi-client')).not.toBeInTheDocument();
    expect(screen.queryByText('Protocol Surface')).not.toBeInTheDocument();
    expect(screen.queryByText('Stop guessing what to take—or what your stack is actually doing.')).not.toBeInTheDocument();
    expect(screen.queryByText('Track peptides, compounds, and layered protocols')).not.toBeInTheDocument();
    expect(screen.queryByText('Learn more')).not.toBeInTheDocument();
    expect(screen.queryByText('Live')).not.toBeInTheDocument();
    expect(screen.queryByText(/No inputs detected/)).not.toBeInTheDocument();

    await user.click(screen.getByRole('link', { name: /I work with clients/ }));
    expect(listener).toHaveBeenCalledWith(
      expect.objectContaining({
        detail: expect.objectContaining({
          eventName: 'landing_path_selected_provider',
          path: 'provider',
        }),
      })
    );

    window.removeEventListener('biostack:landing_path_selected', listener);
  });
});
