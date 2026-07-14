import VerifyPage from '@/app/auth/verify/page';
import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const locationMock = {
  hash: '',
  replace: vi.fn(),
};
const fetchMock = vi.fn();
let search = 'token=abc123';

vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams(search),
}));

describe('VerifyPage', () => {
  beforeEach(() => {
    locationMock.replace.mockReset();
    locationMock.hash = '';
    search = 'token=abc123';
    fetchMock.mockReset();
    vi.stubGlobal('location', locationMock);
    vi.stubGlobal('fetch', fetchMock);
  });

  it('reads production tokens from the URL fragment', async () => {
    search = '';
    locationMock.hash = '#token=fragment123';
    fetchMock.mockResolvedValue({ ok: true, json: async () => ({ redirectPath: '/profiles' }) });

    render(<VerifyPage />);

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/v1/auth/verify',
        expect.objectContaining({ body: JSON.stringify({ token: 'fragment123' }) }),
      );
      expect(locationMock.replace).toHaveBeenCalledWith('/profiles');
    });
  });

  it('exchanges magic links by POST and follows only the server return path', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      json: async () => ({ redirectPath: '/onboarding/consent?returnTo=%2Fprofiles' }),
    });

    render(<VerifyPage />);

    expect(screen.getByText('Signing you in…')).toBeInTheDocument();
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/v1/auth/verify',
        expect.objectContaining({
          method: 'POST',
          credentials: 'include',
          body: JSON.stringify({ token: 'abc123' }),
        }),
      );
      expect(locationMock.replace).toHaveBeenCalledWith('/onboarding/consent?returnTo=%2Fprofiles');
    });
  });

  it('returns to sign-in when the one-time token is rejected', async () => {
    fetchMock.mockResolvedValue({ ok: false });

    render(<VerifyPage />);

    await waitFor(() => {
      expect(locationMock.replace).toHaveBeenCalledWith('/auth/signin?error=invalid-link');
    });
  });
});
