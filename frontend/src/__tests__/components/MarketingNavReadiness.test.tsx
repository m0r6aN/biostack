import { MarketingNav } from '@/components/marketing/MarketingNav';
import { render, screen } from '@testing-library/react';
import type { ComponentProps } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

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

describe('MarketingNav sticky header anchor offset', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
    document.documentElement.style.removeProperty('scroll-padding-top');
  });

  it('tracks the live header height as root scroll padding and removes it on unmount', () => {
    let height = 124.4;
    let notifyResize: (() => void) | undefined;
    const disconnect = vi.fn();
    vi.stubGlobal(
      'ResizeObserver',
      class {
        constructor(callback: () => void) {
          notifyResize = callback;
        }
        observe() {}
        disconnect = disconnect;
      }
    );
    vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockImplementation(
      () => ({ height }) as DOMRect
    );
    const root = document.documentElement;

    const { unmount } = render(<MarketingNav />);
    expect(root.style.getPropertyValue('scroll-padding-top')).toBe('125px');

    height = 79;
    notifyResize?.();
    expect(root.style.getPropertyValue('scroll-padding-top')).toBe('79px');

    unmount();
    expect(disconnect).toHaveBeenCalled();
    expect(root.style.getPropertyValue('scroll-padding-top')).toBe('');
  });
});
