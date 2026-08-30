import VerifyPage from '@/app/auth/verify/page';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
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

const passkeyMocks = vi.hoisted(() => ({
  supported: false,
  registerPasskey: vi.fn(),
}));

vi.mock('@/lib/passkeys', () => ({
  passkeysSupported: () => passkeyMocks.supported,
  registerPasskey: passkeyMocks.registerPasskey,
}));

function mockAuthEndpoints({
  redirectPath = '/profiles',
  passkeysEnabled = true,
  existingPasskeys = [] as unknown[],
} = {}) {
  fetchMock.mockImplementation((url: string) => {
    if (url === '/api/v1/auth/verify') {
      return Promise.resolve({ ok: true, json: async () => ({ redirectPath }) });
    }
    if (url === '/api/v1/auth/passkeys/status') {
      return Promise.resolve({ ok: true, json: async () => ({ enabled: passkeysEnabled }) });
    }
    if (url === '/api/v1/auth/passkeys') {
      return Promise.resolve({ ok: true, json: async () => existingPasskeys });
    }
    return Promise.resolve({ ok: false, json: async () => ({}) });
  });
}

describe('VerifyPage', () => {
  beforeEach(() => {
    locationMock.replace.mockReset();
    locationMock.hash = '';
    search = 'token=abc123';
    fetchMock.mockReset();
    passkeyMocks.supported = false;
    passkeyMocks.registerPasskey.mockReset();
    window.localStorage.clear();
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

  it('offers passkey enrollment after sign-in when none is enrolled', async () => {
    passkeyMocks.supported = true;
    mockAuthEndpoints();

    render(<VerifyPage />);

    expect(await screen.findByRole('button', { name: 'Add a passkey' })).toBeInTheDocument();
    expect(locationMock.replace).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: 'Not now' }));

    await waitFor(() => {
      expect(locationMock.replace).toHaveBeenCalledWith('/profiles');
    });
    expect(window.localStorage.getItem('biostack.passkeyNudgeDismissed')).toBe('1');
    expect(passkeyMocks.registerPasskey).not.toHaveBeenCalled();
  });

  it('enrolls a passkey from the offer and then follows the redirect', async () => {
    passkeyMocks.supported = true;
    passkeyMocks.registerPasskey.mockResolvedValue({});
    mockAuthEndpoints();

    render(<VerifyPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Add a passkey' }));

    await waitFor(() => {
      expect(passkeyMocks.registerPasskey).toHaveBeenCalledWith('My passkey');
      expect(locationMock.replace).toHaveBeenCalledWith('/profiles');
    });
  });

  it('does not interrupt onboarding redirects with the passkey offer', async () => {
    passkeyMocks.supported = true;
    mockAuthEndpoints({ redirectPath: '/onboarding/consent?returnTo=%2Fprofiles' });

    render(<VerifyPage />);

    await waitFor(() => {
      expect(locationMock.replace).toHaveBeenCalledWith('/onboarding/consent?returnTo=%2Fprofiles');
    });
    expect(screen.queryByRole('button', { name: 'Add a passkey' })).not.toBeInTheDocument();
  });

  it('skips the offer when the account already has a passkey', async () => {
    passkeyMocks.supported = true;
    mockAuthEndpoints({ existingPasskeys: [{ id: 'cred-1' }] });

    render(<VerifyPage />);

    await waitFor(() => {
      expect(locationMock.replace).toHaveBeenCalledWith('/profiles');
    });
    expect(screen.queryByRole('button', { name: 'Add a passkey' })).not.toBeInTheDocument();
  });

  it('skips the offer once it has been dismissed on this device', async () => {
    passkeyMocks.supported = true;
    window.localStorage.setItem('biostack.passkeyNudgeDismissed', '1');
    mockAuthEndpoints();

    render(<VerifyPage />);

    await waitFor(() => {
      expect(locationMock.replace).toHaveBeenCalledWith('/profiles');
    });
    expect(screen.queryByRole('button', { name: 'Add a passkey' })).not.toBeInTheDocument();
  });
});
