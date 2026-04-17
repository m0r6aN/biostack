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
      screen.getByRole('heading', { name: 'Stop guessing what to take—or what your stack is actually doing.' })
    ).toBeInTheDocument();
    expect(
      screen.getByText('Track peptides, compounds, and layered protocols')
    ).toBeInTheDocument();
    expect(
      screen.getByText('Organize compounds by date, protocol, overlap, and check-in history before you decide what comes next.')
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /I'm getting started/ })).toHaveAttribute('href', '/start');
    expect(screen.getByRole('link', { name: /I already have a stack/ })).toHaveAttribute('href', '/map');
    expect(screen.getByRole('link', { name: /I work with clients/ })).toHaveAttribute('href', '/providers');
    expect(screen.getByText('Starter')).toBeInTheDocument();
    expect(screen.getByText('Set up compound tracking without rebuilding a spreadsheet.')).toBeInTheDocument();
    expect(screen.getByText('Experienced')).toBeInTheDocument();
    expect(screen.getByText('Map active compounds, overlap signals, and timeline context.')).toBeInTheDocument();
    expect(screen.getByText('Provider')).toBeInTheDocument();
    expect(screen.getByText('Track client protocol changes, notes, and check-ins.')).toBeInTheDocument();
    expect(screen.getByText('Protocol Surface')).toBeInTheDocument();
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
