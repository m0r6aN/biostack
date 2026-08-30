import { MarketingNav } from '@/components/marketing/MarketingNav';
import { render, screen } from '@testing-library/react';
import type { ComponentProps } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: ComponentProps<'a'>) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/',
}));

vi.mock('@/components/ui/BioStackLogo', () => ({
  BioStackLogo: () => <span>BioStack</span>,
}));

const authState = vi.hoisted(() => ({
  user: null as null | { id: string; email: string; displayName: string; role: number },
  loading: false,
  logout: vi.fn(),
  refresh: vi.fn(),
}));

vi.mock('@/lib/AuthProvider', () => ({
  useAuth: () => authState,
}));

beforeEach(() => {
  authState.user = null;
  authState.loading = false;
});

describe('MarketingNav readiness CTAs', () => {
  it('surfaces public evidence and uses a clear free analyzer CTA', () => {
    render(<MarketingNav />);

    expect(screen.getByRole('link', { name: 'Compounds & Evidence' })).toHaveAttribute(
      'href',
      '/knowledge'
    );
    expect(screen.getByRole('link', { name: 'Analyze My Stack' })).toHaveAttribute(
      'href',
      '/tools/analyzer'
    );
    expect(screen.getByRole('link', { name: 'Start Free' })).toHaveAttribute('href', '/start');
    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/auth/signin');
    expect(screen.queryByRole('link', { name: 'Map Stack' })).not.toBeInTheDocument();
  });

  it('swaps Sign in / Start Free for Sign out / Dashboard when authenticated', () => {
    authState.user = { id: 'u1', email: 'user@example.com', displayName: 'User', role: 0 };

    render(<MarketingNav />);

    expect(screen.getByRole('button', { name: 'Sign out' })).toBeInTheDocument();
    const dashboardLinks = screen.getAllByRole('link', { name: 'Dashboard' });
    expect(dashboardLinks.length).toBeGreaterThan(0);
    for (const link of dashboardLinks) {
      expect(link).toHaveAttribute('href', '/protocol-console');
    }
    expect(screen.queryByRole('link', { name: 'Sign in' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Start Free' })).not.toBeInTheDocument();
  });

  it('renders the signed-out CTAs while the session is still loading (SSR-stable)', () => {
    authState.loading = true;

    render(<MarketingNav />);

    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/auth/signin');
    expect(screen.getByRole('link', { name: 'Start Free' })).toHaveAttribute('href', '/start');
    expect(screen.queryByRole('button', { name: 'Sign out' })).not.toBeInTheDocument();
  });
});
