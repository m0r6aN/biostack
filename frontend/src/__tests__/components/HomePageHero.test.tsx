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
  it('routes users into the three audience entry paths', async () => {
    const user = userEvent.setup();
    const listener = vi.fn();
    window.addEventListener('biostack:landing_path_selected', listener);

    render(<LandingHero />);

    expect(
      screen.getByRole('heading', { name: /What to take\. How to use it\.\s*See what it's doing\./ })
    ).toBeInTheDocument();
    expect(
      screen.getByText('Start with answers. Then choose to track, compare, and optimize over time.')
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /I'm getting started/ })).toHaveAttribute('href', '/start');
    expect(screen.getByRole('link', { name: /I already have a stack/ })).toHaveAttribute('href', '/map');
    expect(screen.getByRole('link', { name: /I work with clients/ })).toHaveAttribute('href', '/providers');
    expect(screen.getByRole('link', { name: 'Need help with dosage or mixing? → Start here' })).toHaveAttribute('href', '/tools');
    expect(screen.getByText('Starter')).toBeInTheDocument();
    expect(screen.getByText('Set up compound tracking without rebuilding a spreadsheet.')).toBeInTheDocument();
    expect(screen.getByText('Experienced')).toBeInTheDocument();
    expect(screen.getByText('Map active compounds, overlap signals, and timeline context.')).toBeInTheDocument();
    expect(screen.getByText('Provider')).toBeInTheDocument();
    expect(screen.getByText('Track client protocol changes, notes, and check-ins.')).toBeInTheDocument();
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
