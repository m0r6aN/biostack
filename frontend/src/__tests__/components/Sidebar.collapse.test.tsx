import { Sidebar } from '@/components/Sidebar';
import { SIDEBAR_COLLAPSED_KEY } from '@/lib/sidebarCollapse';
import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const setSidebarOpen = vi.fn();

vi.mock('next/navigation', () => ({
  usePathname: () => '/protocol-console',
}));

vi.mock('@/components/ui/BioStackLogo', () => ({
  BioStackLogo: ({ wordmarkClassName }: { wordmarkClassName?: string }) => (
    <span data-testid="logo" data-wordmark-class={wordmarkClassName}>
      BioStack
    </span>
  ),
}));

vi.mock('@/lib/context', () => ({
  useProfile: () => ({ isSidebarOpen: false, setSidebarOpen }),
}));

vi.mock('@/lib/AuthProvider', () => ({
  useAuth: () => ({ user: null, loading: false, logout: vi.fn() }),
}));

describe('Sidebar desktop collapse', () => {
  beforeEach(() => {
    setSidebarOpen.mockReset();
    window.localStorage.clear();
  });

  it('renders expanded by default when no preference is stored', () => {
    render(<Sidebar />);

    const toggle = screen.getByRole('button', { name: 'Collapse sidebar' });
    expect(toggle).toHaveAttribute('aria-expanded', 'true');
    expect(toggle).toHaveAttribute('aria-controls', 'app-sidebar-nav');
    expect(screen.getByRole('navigation')).toHaveAttribute('id', 'app-sidebar-nav');
  });

  it('reads a persisted collapsed preference on mount (hydration-safe read)', () => {
    window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, '1');

    render(<Sidebar />);

    expect(screen.getByRole('button', { name: 'Expand sidebar' })).toHaveAttribute('aria-expanded', 'false');
  });

  it('toggles collapse state and persists the choice to localStorage', () => {
    render(<Sidebar />);

    const toggle = screen.getByRole('button', { name: 'Collapse sidebar' });
    fireEvent.click(toggle);

    expect(window.localStorage.getItem(SIDEBAR_COLLAPSED_KEY)).toBe('1');
    expect(screen.getByRole('button', { name: 'Expand sidebar' })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Expand sidebar' }));

    expect(window.localStorage.getItem(SIDEBAR_COLLAPSED_KEY)).toBe('0');
    expect(screen.getByRole('button', { name: 'Collapse sidebar' })).toBeInTheDocument();
  });

  it('gives every nav link an accessible name regardless of collapse state', () => {
    window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, '1');
    render(<Sidebar />);

    expect(screen.getByRole('link', { name: 'Compounds' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Dashboard' })).toBeInTheDocument();
  });

  it('handles a localStorage that throws (private browsing / disabled storage) without crashing', () => {
    const getItemSpy = vi
      .spyOn(window.localStorage.__proto__, 'getItem')
      .mockImplementation(() => {
        throw new Error('storage disabled');
      });

    expect(() => render(<Sidebar />)).not.toThrow();
    expect(screen.getByRole('button', { name: 'Collapse sidebar' })).toBeInTheDocument();

    getItemSpy.mockRestore();
  });
});
