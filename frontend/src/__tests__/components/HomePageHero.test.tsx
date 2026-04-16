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
      screen.getByText("Start from scratch, analyze what you're already using, or manage protocols at scale.")
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /I'm getting started/ })).toHaveAttribute('href', '/start');
    expect(screen.getByRole('link', { name: /I already have a stack/ })).toHaveAttribute('href', '/map');
    expect(screen.getByRole('link', { name: /I work with clients/ })).toHaveAttribute('href', '/providers');
    expect(screen.getByText('Starter')).toBeInTheDocument();
    expect(screen.getByText('Help me figure out what to take and how to begin')).toBeInTheDocument();
    expect(screen.getByText('Experienced')).toBeInTheDocument();
    expect(screen.getByText("Show me what overlaps, what works, and what doesn't")).toBeInTheDocument();
    expect(screen.getByText('Retail / Provider')).toBeInTheDocument();
    expect(screen.getByText('Manage client protocols with structure and clarity')).toBeInTheDocument();
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
