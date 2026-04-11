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
  it('shows a quiet local-mode notice on app routes when providers are not configured', () => {
    usePathnameMock.mockReturnValue('/mission-control');

    render(
      <AppShell authProvidersConfigured={false}>
        <div>Dashboard content</div>
      </AppShell>
    );

    expect(screen.getByText('Local mode')).toBeInTheDocument();
    expect(
      screen.getByText(
        'OAuth providers are not configured yet. Profiles and protocol data stay usable on this device until sign-in is enabled.'
      )
    ).toBeInTheDocument();
    expect(screen.getByText('Dashboard content')).toBeInTheDocument();
  });

  it('does not show the notice when providers are configured', () => {
    usePathnameMock.mockReturnValue('/profiles');

    render(
      <AppShell authProvidersConfigured>
        <div>Profiles content</div>
      </AppShell>
    );

    expect(screen.queryByText('Local mode')).not.toBeInTheDocument();
  });
});