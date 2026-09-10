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
    useAuthMock.mockReturnValue({
      user: { id: '1', email: 'test@test.com', displayName: 'Test', role: 0 },
      loading: false,
    });
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
    useAuthMock.mockReturnValue({ user: null, loading: false });
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
    useAuthMock.mockReturnValue({
      user: { id: '1', email: 'test@test.com', displayName: 'Test', role: 0 },
      loading: false,
    });
    usePathnameMock.mockReturnValue('/knowledge');

    render(
      <AppShell>
        <div>Knowledge content</div>
      </AppShell>
    );

    expect(screen.getByText('Sidebar')).toBeInTheDocument();
    expect(screen.getByText('Knowledge content')).toBeInTheDocument();
  });

  it('withholds protected content while the session is being checked', () => {
    useAuthMock.mockReturnValue({ user: null, loading: true });
    usePathnameMock.mockReturnValue('/profiles');

    render(
      <AppShell>
        <div>Sensitive profile content</div>
      </AppShell>,
    );

    expect(screen.getByRole('status')).toHaveTextContent('Checking your session');
    expect(screen.queryByText('Sensitive profile content')).not.toBeInTheDocument();
    expect(screen.queryByText('Sidebar')).not.toBeInTheDocument();
  });

  it('withholds protected content after an invalid session while redirect begins', () => {
    useAuthMock.mockReturnValue({ user: null, loading: false });
    usePathnameMock.mockReturnValue('/profiles');

    render(
      <AppShell>
        <div>Sensitive profile content</div>
      </AppShell>,
    );

    expect(screen.getByRole('status')).toHaveTextContent('Redirecting to sign in');
    expect(screen.queryByText('Sensitive profile content')).not.toBeInTheDocument();
  });
});

describe('AppShell skip-link target (#main)', () => {
  const authenticated = {
    user: { id: '1', email: 'test@test.com', displayName: 'Test', role: 0 },
    loading: false,
  };

  function renderShell(pathname: string, auth: { user: unknown; loading: boolean }, children: React.ReactNode) {
    useAuthMock.mockReturnValue(auth);
    usePathnameMock.mockReturnValue(pathname);
    return render(<AppShell>{children}</AppShell>);
  }

  function expectSingleShellTarget() {
    const targets = document.querySelectorAll('#main');
    expect(targets).toHaveLength(1);
    const target = targets[0] as HTMLElement;
    expect(target.tagName).toBe('MAIN');
    expect(target).toHaveAttribute('tabindex', '-1');
    target.focus();
    expect(document.activeElement).toBe(target);
    return target;
  }

  it('exposes a single focusable #main while a protected session is being checked', () => {
    renderShell('/profiles', { user: null, loading: true }, <div>Sensitive profile content</div>);

    const target = expectSingleShellTarget();
    expect(target).toContainElement(screen.getByRole('status'));
    expect(document.querySelectorAll('main')).toHaveLength(1);
    expect(screen.queryByText('Sensitive profile content')).not.toBeInTheDocument();
  });

  it('exposes a single focusable #main while an anonymous protected visit redirects', () => {
    renderShell('/profiles', { user: null, loading: false }, <div>Sensitive profile content</div>);

    const target = expectSingleShellTarget();
    expect(target).toHaveTextContent('Redirecting to sign in');
    expect(document.querySelectorAll('main')).toHaveLength(1);
    expect(screen.queryByText('Sensitive profile content')).not.toBeInTheDocument();
  });

  it('exposes a single focusable shell-owned #main on authenticated app routes', () => {
    renderShell('/protocol-console', authenticated, <div>Protocol Console content</div>);

    const target = expectSingleShellTarget();
    expect(target).toContainElement(screen.getByText('Protocol Console content'));
    expect(target).toContainElement(screen.getByLabelText('App-wide disclaimer'));
    expect(target).not.toContainElement(screen.getByText('Sidebar'));
    expect(document.querySelectorAll('main')).toHaveLength(1);
  });

  it('exposes a single focusable shell-owned #main on /knowledge for authenticated users', () => {
    renderShell('/knowledge', authenticated, <div>Knowledge content</div>);

    const target = expectSingleShellTarget();
    expect(target).toContainElement(screen.getByText('Knowledge content'));
    expect(screen.getByText('Sidebar')).toBeInTheDocument();
  });

  it('keeps #main unique when an authenticated knowledge child brings its own id-less <main> (pre-existing nesting)', () => {
    renderShell(
      '/knowledge/methodology',
      authenticated,
      <main data-testid="child-main">Methodology content</main>,
    );

    const target = expectSingleShellTarget();
    const childMain = screen.getByTestId('child-main');
    // Baseline: the child landmark is still rendered and still nested — this lane does not flatten it.
    expect(document.querySelectorAll('main')).toHaveLength(2);
    expect(target).toContainElement(childMain);
    expect(childMain).not.toHaveAttribute('id');
    expect(childMain).not.toHaveAttribute('tabindex');
  });

  it('keeps #main unique when an admin child brings its own id-less <main> (pre-existing nesting)', () => {
    renderShell('/admin/research', authenticated, <main data-testid="child-main">Admin content</main>);

    const target = expectSingleShellTarget();
    expect(document.querySelectorAll('main')).toHaveLength(2);
    expect(target).toContainElement(screen.getByTestId('child-main'));
  });

  it('passes public children through untouched, including their own #main, with no shell main', () => {
    renderShell(
      '/pricing',
      { user: null, loading: false },
      <main id="main" tabIndex={-1} data-testid="page-main">
        Pricing content
      </main>,
    );

    const mains = document.querySelectorAll('main');
    expect(mains).toHaveLength(1);
    expect(mains[0]).toBe(screen.getByTestId('page-main'));
    expect(document.querySelectorAll('#main')).toHaveLength(1);
    expect(screen.queryByText('Sidebar')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('App-wide disclaimer')).not.toBeInTheDocument();
  });

  it('passes anonymous /knowledge children through unchanged (documented control: no shell target)', () => {
    renderShell(
      '/knowledge/methodology',
      { user: null, loading: false },
      <main data-testid="child-main">Methodology content</main>,
    );

    const mains = document.querySelectorAll('main');
    expect(mains).toHaveLength(1);
    expect(mains[0]).toBe(screen.getByTestId('child-main'));
    expect(document.getElementById('main')).toBeNull();
    expect(screen.queryByText('Sidebar')).not.toBeInTheDocument();
  });
});
