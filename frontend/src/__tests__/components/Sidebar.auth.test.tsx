import { Sidebar } from '@/components/Sidebar';
import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const useAuthMock = vi.fn();
const setSidebarOpen = vi.fn();

vi.mock('next/navigation', () => ({
  usePathname: () => '/profiles',
}));

vi.mock('@/components/ui/BioStackLogo', () => ({
  BioStackLogo: () => <span>BioStack</span>,
}));

vi.mock('@/lib/context', () => ({
  useProfile: () => ({ isSidebarOpen: true, setSidebarOpen }),
}));

vi.mock('@/lib/AuthProvider', () => ({
  useAuth: () => useAuthMock(),
}));

describe('Sidebar authentication affordances', () => {
  beforeEach(() => {
    setSidebarOpen.mockReset();
  });

  it('shows a visible sign-in control for anonymous or stale sessions', () => {
    useAuthMock.mockReturnValue({ user: null, loading: false, logout: vi.fn() });

    render(<Sidebar />);

    const signIn = screen.getByRole('link', { name: 'Sign in' });
    expect(signIn).toHaveAttribute('href', '/auth/signin?callbackUrl=%2Fprofiles');
    expect(screen.queryByRole('button', { name: 'Sign out' })).not.toBeInTheDocument();
  });

  it('shows a visible sign-out control for authenticated users', () => {
    const logout = vi.fn().mockResolvedValue(undefined);
    useAuthMock.mockReturnValue({
      user: { id: '1', email: 'user@example.com', displayName: 'User', role: 0 },
      loading: false,
      logout,
    });

    render(<Sidebar />);
    fireEvent.click(screen.getByRole('button', { name: 'Sign out' }));

    expect(logout).toHaveBeenCalledOnce();
    expect(screen.queryByRole('link', { name: 'Sign in' })).not.toBeInTheDocument();
  });
});
