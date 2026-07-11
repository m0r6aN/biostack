import { AppShell } from '@/components/AppShell';
import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const usePathnameMock = vi.fn();
const useAuthMock = vi.fn();

vi.mock('next/navigation', () => ({
  usePathname: () => usePathnameMock(),
}));

vi.mock('@/components/Sidebar', () => ({
  Sidebar: () => <aside>Sidebar</aside>,
}));

vi.mock('@/lib/AuthProvider', () => ({
  useAuth: () => useAuthMock(),
}));

describe('AppShell', () => {
  beforeEach(() => {
    useAuthMock.mockReturnValue({ user: { id: '1', email: 'test@test.com', displayName: 'Test', role: 0 } });
  });

  it('renders app chrome on app routes', () => {
    usePathnameMock.mockReturnValue('/protocol-console');

    render(
      <AppShell>
        <div>Protocol Console content</div>
      </AppShell>
    );

    expect(screen.getByText('Sidebar')).toBeInTheDocument();
    expect(screen.getByText('Protocol Console content')).toBeInTheDocument();
  });

  it.each(['/protocols', '/billing', '/governance/receipts'])(
    'renders app chrome on authenticated product route %s',
    (pathname) => {
      usePathnameMock.mockReturnValue(pathname);

      render(
        <AppShell>
          <div>Authenticated product content</div>
        </AppShell>
      );

      expect(screen.getByText('Sidebar')).toBeInTheDocument();
      expect(screen.getByText('Authenticated product content')).toBeInTheDocument();
    }
  );

  it('does not render app chrome on public routes', () => {
    usePathnameMock.mockReturnValue('/pricing');

    render(
      <AppShell>
        <div>Pricing content</div>
      </AppShell>
    );

    expect(screen.queryByText('Sidebar')).not.toBeInTheDocument();
    expect(screen.getByText('Pricing content')).toBeInTheDocument();
  });

  it('renders the app-wide disclaimer on app routes (H1)', () => {
    usePathnameMock.mockReturnValue('/checkins');

    render(
      <AppShell>
        <div>Check-ins content</div>
      </AppShell>
    );

    const disclaimer = screen.getByLabelText('App-wide disclaimer');
    expect(disclaimer).toBeInTheDocument();
    expect(disclaimer).toHaveTextContent('Educational and observational only. Not medical advice.');
  });

  it('does not render the app-wide disclaimer on public routes', () => {
    usePathnameMock.mockReturnValue('/pricing');

    render(
      <AppShell>
        <div>Pricing content</div>
      </AppShell>
    );

    expect(screen.queryByLabelText('App-wide disclaimer')).not.toBeInTheDocument();
  });

  it('does not render app chrome on /knowledge for anonymous visitors', () => {
    useAuthMock.mockReturnValue({ user: null });
    usePathnameMock.mockReturnValue('/knowledge');

    render(
      <AppShell>
        <div>Knowledge content</div>
      </AppShell>
    );

    expect(screen.queryByText('Sidebar')).not.toBeInTheDocument();
    expect(screen.getByText('Knowledge content')).toBeInTheDocument();
  });

  it('renders app chrome on /knowledge for authenticated users', () => {
    useAuthMock.mockReturnValue({ user: { id: '1', email: 'test@test.com', displayName: 'Test', role: 0 } });
    usePathnameMock.mockReturnValue('/knowledge');

    render(
      <AppShell>
        <div>Knowledge content</div>
      </AppShell>
    );

    expect(screen.getByText('Sidebar')).toBeInTheDocument();
    expect(screen.getByText('Knowledge content')).toBeInTheDocument();
  });
});
