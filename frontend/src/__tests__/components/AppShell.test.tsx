import { AppShell } from '@/components/AppShell';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const usePathnameMock = vi.fn();

vi.mock('next/navigation', () => ({
  usePathname: () => usePathnameMock(),
}));

vi.mock('@/components/Sidebar', () => ({
  Sidebar: () => <aside>Sidebar</aside>,
}));

describe('AppShell', () => {
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
});
