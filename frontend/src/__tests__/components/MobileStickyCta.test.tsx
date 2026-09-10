import { MobileStickyCta } from '@/components/marketing/MobileStickyCta';
import { act, render, screen } from '@testing-library/react';
import type { ComponentProps } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: ComponentProps<'a'>) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

const navState = vi.hoisted(() => ({ pathname: '/' }));

vi.mock('next/navigation', () => ({
  usePathname: () => navState.pathname,
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

const LINK_LABELS = ['Start free', 'Evidence', 'Analyze', 'Pricing'] as const;

function setScrollY(value: number) {
  Object.defineProperty(window, 'scrollY', { configurable: true, writable: true, value });
}

function scrollTo(value: number) {
  act(() => {
    setScrollY(value);
    window.dispatchEvent(new Event('scroll'));
  });
}

function getNav() {
  // aria-hidden subtrees have an empty accessible name, so locate by the label attribute.
  const nav = document.querySelector('nav[aria-label="Primary actions"]');
  if (!(nav instanceof HTMLElement)) {
    throw new Error('Primary actions nav not rendered');
  }
  return nav;
}

function isKeyboardReachable(element: HTMLElement) {
  return (
    !element.hasAttribute('inert') &&
    element.closest('[inert]') === null &&
    element.closest('[aria-hidden="true"]') === null
  );
}

beforeEach(() => {
  navState.pathname = '/';
  authState.user = null;
  authState.loading = false;
  setScrollY(0);
});

describe('MobileStickyCta keyboard visibility (KEO-84 D2)', () => {
  it('keeps the hidden nav and its links out of keyboard and accessibility navigation', () => {
    render(<MobileStickyCta />);

    const nav = getNav();
    expect(nav).toHaveAttribute('inert');
    expect(nav).toHaveAttribute('aria-hidden', 'true');
    expect(nav.className).toContain('pointer-events-none');

    for (const label of LINK_LABELS) {
      expect(screen.queryByRole('link', { name: label })).not.toBeInTheDocument();
      const link = screen.getByRole('link', { name: label, hidden: true });
      expect(isKeyboardReachable(link)).toBe(false);
    }
  });

  it('exposes operable links with a keyboard focus indicator once past the scroll threshold', () => {
    render(<MobileStickyCta />);
    scrollTo(221);

    const nav = getNav();
    expect(nav).not.toHaveAttribute('inert');
    expect(nav).not.toHaveAttribute('aria-hidden', 'true');
    expect(nav.className).not.toContain('pointer-events-none');

    for (const label of LINK_LABELS) {
      const link = screen.getByRole('link', { name: label });
      expect(isKeyboardReachable(link)).toBe(true);
      expect(link.className).toContain('focus-visible:ring-2');
      expect(link.className).toContain('focus-visible:outline-none');
      act(() => link.focus());
      expect(document.activeElement).toBe(link);
    }
  });

  it('preserves the 220px threshold: hidden at 220, visible at 221', () => {
    render(<MobileStickyCta />);

    scrollTo(220);
    expect(getNav()).toHaveAttribute('inert');

    scrollTo(221);
    expect(getNav()).not.toHaveAttribute('inert');
  });

  it('returns the nav to the inert state when scrolling back above the threshold', () => {
    render(<MobileStickyCta />);

    scrollTo(300);
    expect(getNav()).not.toHaveAttribute('inert');
    expect(screen.getByRole('link', { name: 'Evidence' })).toBeInTheDocument();

    scrollTo(0);
    expect(getNav()).toHaveAttribute('inert');
    expect(getNav()).toHaveAttribute('aria-hidden', 'true');
    expect(screen.queryByRole('link', { name: 'Evidence' })).not.toBeInTheDocument();
  });

  it('marks the nav inert when it hides while one of its links holds focus', () => {
    render(<MobileStickyCta />);

    scrollTo(300);
    const link = screen.getByRole('link', { name: 'Analyze' });
    act(() => link.focus());
    expect(document.activeElement).toBe(link);

    scrollTo(0);

    // Focus retention is a browser concern (inert subtrees are un-focusable and
    // browsers apply the focus fixup rule); the component's contract is that the
    // subtree is inert so the stale focus cannot be reached or announced.
    expect(link.closest('[inert]')).toBe(getNav());
    expect(isKeyboardReachable(link)).toBe(false);
  });

  it('keeps anonymous and authenticated first-link destinations unchanged', () => {
    const { unmount } = render(<MobileStickyCta />);
    scrollTo(300);

    expect(screen.getByRole('link', { name: 'Start free' })).toHaveAttribute('href', '/start');
    expect(screen.getByRole('link', { name: 'Evidence' })).toHaveAttribute('href', '/knowledge');
    expect(screen.getByRole('link', { name: 'Analyze' })).toHaveAttribute('href', '/tools/analyzer');
    expect(screen.getByRole('link', { name: 'Pricing' })).toHaveAttribute('href', '/pricing');
    unmount();

    authState.user = { id: 'u1', email: 'user@example.com', displayName: 'User', role: 0 };
    render(<MobileStickyCta />);
    scrollTo(300);

    expect(screen.getByRole('link', { name: 'Dashboard' })).toHaveAttribute(
      'href',
      '/protocol-console'
    );
    expect(screen.queryByRole('link', { name: 'Start free' })).not.toBeInTheDocument();
    expect(screen.getAllByRole('link')).toHaveLength(4);
  });

  it('still renders nothing on /start and keeps the desktop md:hidden guard', () => {
    navState.pathname = '/start';
    const { unmount } = render(<MobileStickyCta />);
    expect(document.querySelector('nav[aria-label="Primary actions"]')).toBeNull();
    unmount();

    navState.pathname = '/tools/analyzer';
    render(<MobileStickyCta />);
    expect(getNav().className).toContain('md:hidden');
  });
});
