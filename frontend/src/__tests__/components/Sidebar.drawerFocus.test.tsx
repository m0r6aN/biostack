import { Sidebar } from '@/components/Sidebar';
import * as contextModule from '@/lib/context';
import { fireEvent, render, screen } from '@testing-library/react';
import { useState, type Context } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

// Below the lg breakpoint the sidebar is an off-canvas drawer. Opening it must move keyboard focus
// into the drawer, and closing it must put focus back on the toggle (WCAG 2.4.3).

vi.mock('next/navigation', () => ({ usePathname: () => '/protocol-console' }));
vi.mock('@/components/ui/BioStackLogo', () => ({ BioStackLogo: () => <span>BioStack</span> }));
vi.mock('@/lib/AuthProvider', () => ({
  useAuth: () => ({ user: null, loading: false, logout: vi.fn() }),
}));

vi.mock('@/lib/context', async () => {
  const React = await import('react');
  const Ctx = React.createContext({ isSidebarOpen: false, setSidebarOpen: () => {} });
  return { useProfile: () => React.useContext(Ctx), TestProfileContext: Ctx };
});

// The mocked module above also exports the context so the test can provide it.
const { TestProfileContext } = contextModule as unknown as {
  TestProfileContext: Context<{ isSidebarOpen: boolean; setSidebarOpen: (open: boolean) => void }>;
};

function Shell() {
  const [open, setOpen] = useState(false);
  return (
    <TestProfileContext.Provider value={{ isSidebarOpen: open, setSidebarOpen: setOpen }}>
      <button type="button" aria-label="Toggle navigation" aria-expanded={open} aria-controls="app-sidebar" onClick={() => setOpen(!open)} />
      <Sidebar />
      <main tabIndex={-1}>content</main>
    </TestProfileContext.Provider>
  );
}

function mockViewport(isDesktop: boolean) {
  window.matchMedia = vi.fn().mockImplementation((query: string) => ({
    matches: isDesktop,
    media: query,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
  })) as unknown as typeof window.matchMedia;
}

describe('Sidebar drawer focus (mobile)', () => {
  const original = window.matchMedia;
  beforeEach(() => {
    mockViewport(false);
    window.localStorage.clear();
  });
  afterEach(() => {
    window.matchMedia = original;
  });

  it('moves focus to the Close navigation button inside the drawer when it opens', () => {
    render(<Shell />);
    const toggle = screen.getByRole('button', { name: 'Toggle navigation' });
    toggle.focus();
    fireEvent.click(toggle);

    expect(screen.getByRole('button', { name: 'Close navigation' })).toHaveFocus();
    expect(document.getElementById('app-sidebar')).not.toHaveAttribute('inert');
  });

  it('returns focus to the toggle when Escape closes the drawer', () => {
    render(<Shell />);
    const toggle = screen.getByRole('button', { name: 'Toggle navigation' });
    fireEvent.click(toggle);
    expect(screen.getByRole('button', { name: 'Close navigation' })).toHaveFocus();

    fireEvent.keyDown(document, { key: 'Escape' });

    expect(toggle).toHaveFocus();
    expect(document.getElementById('app-sidebar')).toHaveAttribute('inert');
  });

  it('returns focus to the toggle when the Close navigation button is used', () => {
    render(<Shell />);
    const toggle = screen.getByRole('button', { name: 'Toggle navigation' });
    fireEvent.click(toggle);
    fireEvent.click(screen.getByRole('button', { name: 'Close navigation' }));

    expect(toggle).toHaveFocus();
  });

  it('does not steal focus on first render while the drawer is closed', () => {
    render(<Shell />);
    expect(document.body).toHaveFocus();
  });

  it('does not move focus on desktop, where the sidebar is not a drawer', () => {
    mockViewport(true);
    render(<Shell />);
    const toggle = screen.getByRole('button', { name: 'Toggle navigation' });
    toggle.focus();
    fireEvent.click(toggle);
    expect(toggle).toHaveFocus();
  });
});
