import { Sidebar } from '@/components/Sidebar';
import { SIDEBAR_COLLAPSED_KEY, writeSidebarCollapsed } from '@/lib/sidebarCollapse';
import { useSidebarCollapsed } from '@/lib/useSidebarCollapsed';
import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const setSidebarOpen = vi.fn();
const auth = vi.hoisted(() => ({ signedIn: false, logout: vi.fn() }));

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
  useAuth: () => ({ user: auth.signedIn ? { displayName: 'Fixture User', email: 'fixture@example.test', role: 0 } : null, loading: false, logout: auth.logout }),
}));

describe('Sidebar desktop collapse', () => {
  beforeEach(() => {
    setSidebarOpen.mockReset();
    window.localStorage.clear();
    writeSidebarCollapsed(false);
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

describe('collapsed rail regressions', () => {
  afterEach(() => { vi.restoreAllMocks(); writeSidebarCollapsed(false); });

  it('keeps toggling when reads and writes are denied', () => {
    window.localStorage.clear();
    writeSidebarCollapsed(false);
    render(<Sidebar />);
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => { throw new Error('denied'); });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('denied'); });
    fireEvent.click(screen.getByRole('button', { name: 'Collapse sidebar' }));
    expect(screen.getByRole('button', { name: 'Expand sidebar' })).toHaveAttribute('aria-expanded', 'false');
    fireEvent.click(screen.getByRole('button', { name: 'Expand sidebar' }));
    expect(screen.getByRole('button', { name: 'Collapse sidebar' })).toBeInTheDocument();
  });

  it('places a focused rail label outside the scrolling navigation', () => {
    window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, '1');
    render(<Sidebar />);
    fireEvent.focus(screen.getByRole('link', { name: 'Compounds' }));
    const tip = screen.getByRole('tooltip', { hidden: true });
    expect(tip).toHaveTextContent('Compounds');
    expect(screen.getByRole('navigation').contains(tip)).toBe(false);
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('tooltip', { hidden: true })).not.toBeInTheDocument();
  });
});

it('keeps compact account actions named and operable without the wide footer', () => {
  auth.signedIn = true;
  writeSidebarCollapsed(true);
  try {
    render(<Sidebar />);
    const compact = screen.getByRole('group', { name: 'Account and support' });
    expect(within(compact).getByRole('link', { name: 'BioStack Support' })).toHaveAttribute('href', 'mailto:support@biostack.cc');
    fireEvent.click(within(compact).getByRole('button', { name: 'Sign out' }));
    expect(auth.logout).toHaveBeenCalledOnce();
    expect(compact.className).toContain('lg:flex');
    expect(compact.nextElementSibling?.className).toContain('lg:hidden');
  } finally { auth.signedIn = false; writeSidebarCollapsed(false); }
});

it('updates two subscribers in memory and accepts a subsequent cross-tab preference', () => {
  writeSidebarCollapsed(false);
  render(<><Sidebar /><Sidebar /></>);
  const write = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('quota'); });
  fireEvent.click(screen.getAllByRole('button', { name: 'Collapse sidebar' })[0]);
  expect(screen.getAllByRole('button', { name: 'Expand sidebar' })).toHaveLength(2);
  write.mockRestore();
  window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, '0');
  act(() => window.dispatchEvent(new StorageEvent('storage', { key: SIDEBAR_COLLAPSED_KEY })));
  expect(screen.getAllByRole('button', { name: 'Collapse sidebar' })).toHaveLength(2);
});

function PreferenceObserver() {
  const [collapsed] = useSidebarCollapsed();
  return <output aria-label="Observed sidebar state">{collapsed ? 'collapsed' : 'expanded'}</output>;
}

it('keeps receiving cross-tab changes after another subscriber unmounts', () => {
  writeSidebarCollapsed(false);
  const view = render(<><Sidebar /><PreferenceObserver /></>);
  expect(screen.getByLabelText('Observed sidebar state')).toHaveTextContent('expanded');
  view.rerender(<><Sidebar /></>);
  window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, '1');
  act(() => window.dispatchEvent(new StorageEvent('storage', { key: SIDEBAR_COLLAPSED_KEY })));
  expect(screen.getByRole('button', { name: 'Expand sidebar' })).toHaveAttribute('aria-expanded', 'false');
  window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, '0');
  act(() => window.dispatchEvent(new StorageEvent('storage', { key: SIDEBAR_COLLAPSED_KEY })));
  expect(screen.getByRole('button', { name: 'Collapse sidebar' })).toHaveAttribute('aria-expanded', 'true');
});
