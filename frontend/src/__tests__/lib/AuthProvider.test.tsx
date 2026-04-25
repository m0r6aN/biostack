import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider, useAuth } from '@/lib/AuthProvider';

function AuthProbe() {
  const { user, loading } = useAuth();
  return (
    <div>
      <span data-testid="loading">{loading ? 'loading' : 'ready'}</span>
      <span data-testid="user">{user?.email ?? 'anonymous'}</span>
    </div>
  );
}

describe('AuthProvider session handling', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('treats 401 session responses as anonymous state', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
    });
    vi.stubGlobal('fetch', fetchMock);

    render(
      <AuthProvider>
        <AuthProbe />
      </AuthProvider>,
    );

    await waitFor(() => expect(screen.getByTestId('loading')).toHaveTextContent('ready'));
    expect(screen.getByTestId('user')).toHaveTextContent('anonymous');
  });

  it('logs true session server errors without blocking anonymous state', async () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: false,
        status: 500,
      }),
    );

    render(
      <AuthProvider>
        <AuthProbe />
      </AuthProvider>,
    );

    await waitFor(() => expect(screen.getByTestId('loading')).toHaveTextContent('ready'));
    expect(screen.getByTestId('user')).toHaveTextContent('anonymous');
    expect(warn).toHaveBeenCalledWith('BioStack session check failed', expect.objectContaining({ status: 500 }));
  });
});
