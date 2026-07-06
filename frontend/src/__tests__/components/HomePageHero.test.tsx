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
      screen.getByRole('heading', { name: /What you're taking\. How it's structured\.\s*See what it's doing\./ })
    ).toBeInTheDocument();
    expect(
      screen.getByText('Start with clarity. Then track, compare, and observe changes over time.')
    ).toBeInTheDocument();
    // Banned prescriptive copy must not appear on the landing hero.
    expect(screen.queryByText(/What to take\. How to use it\./)).not.toBeInTheDocument();
    expect(screen.queryByText(/optimize over time/)).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: /I am getting started/ })).toHaveAttribute('href', '/start');
    expect(screen.getByRole('link', { name: /I already have a stack/ })).toHaveAttribute('href', '/start?mode=existing');
    expect(screen.getByRole('link', { name: /I work with clients/ })).toHaveAttribute('href', '/providers');
    expect(screen.getByRole('link', { name: /Analyze a protocol/ })).toHaveAttribute('href', '/tools/analyzer');
    expect(screen.getByRole('link', { name: 'Need to calculate dose volume or reconstitution? → Start here' })).toHaveAttribute('href', '/tools');
    expect(screen.getByRole('link', { name: 'See free vs Operator' })).toHaveAttribute('href', '/pricing');
    expect(screen.getByText('Starter')).toBeInTheDocument();
    expect(screen.getByText('Set up compound tracking without rebuilding a spreadsheet.')).toBeInTheDocument();
    expect(screen.getByText('Experienced')).toBeInTheDocument();
    expect(screen.getByText('Map active compounds, overlap signals, and timeline context.')).toBeInTheDocument();
    expect(screen.getByText('Provider')).toBeInTheDocument();
    expect(screen.getByText('Track client protocol changes, notes, and check-ins.')).toBeInTheDocument();
    expect(screen.getByText('Analyzer')).toBeInTheDocument();
    expect(screen.getByText('Paste, upload, scan, or link any stack and see what BioStack finds.')).toBeInTheDocument();
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
