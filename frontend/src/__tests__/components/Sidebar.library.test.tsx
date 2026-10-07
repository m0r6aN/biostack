import { Sidebar } from '@/components/Sidebar';
import { SIDEBAR_COLLAPSED_KEY, writeSidebarCollapsed } from '@/lib/sidebarCollapse';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const setSidebarOpen = vi.fn();

vi.mock('next/navigation', () => ({
  usePathname: () => '/protocol-console',
}));

vi.mock('@/components/ui/BioStackLogo', () => ({
  BioStackLogo: () => <span>BioStack</span>,
}));

vi.mock('@/lib/context', () => ({
  useProfile: () => ({ isSidebarOpen: false, setSidebarOpen }),
}));

vi.mock('@/lib/AuthProvider', () => ({
  useAuth: () => ({ user: null, loading: false, logout: vi.fn() }),
}));

describe('Sidebar — Library as the primary destination', () => {
  beforeEach(() => {
    setSidebarOpen.mockReset();
    window.localStorage.clear();
    writeSidebarCollapsed(false);
  });

  it('lists Library first, ahead of every other nav item, linking to /knowledge', () => {
    render(<Sidebar />);
    const nav = screen.getByRole('navigation');
    const links = within(nav).getAllByRole('link');
    expect(links[0]).toHaveAccessibleName('Library');
    expect(links[0]).toHaveAttribute('href', '/knowledge');
  });

  it('shows the "Compounds & evidence" sublabel under the Library label when expanded', () => {
    render(<Sidebar />);
    const libraryLink = screen.getByRole('link', { name: 'Library' });
    expect(within(libraryLink).getByText('Compounds & evidence')).toBeVisible();
  });

  it('still exposes an accessible name for the Library link when the rail is collapsed', () => {
    window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, '1');
    render(<Sidebar />);
    expect(screen.getByRole('link', { name: 'Library' })).toHaveAttribute('href', '/knowledge');
  });

  it('shows a rail tooltip labeled Library on hover when collapsed', () => {
    window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, '1');
    render(<Sidebar />);
    fireEvent.mouseEnter(screen.getByRole('link', { name: 'Library' }));
    expect(screen.getByRole('tooltip', { hidden: true })).toHaveTextContent('Library');
  });

  it('keeps the existing Compounds tracking link distinct from Library', () => {
    render(<Sidebar />);
    expect(screen.getByRole('link', { name: 'Compounds' })).toHaveAttribute('href', '/compounds');
    expect(screen.getByRole('link', { name: 'Library' })).toHaveAttribute('href', '/knowledge');
  });
});
