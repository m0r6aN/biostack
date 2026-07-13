import ConsentPage from '@/app/onboarding/consent/page';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const fetchMock = vi.fn();
const locationMock = { replace: vi.fn() };
let returnTo = '%2Fprofiles%3Fbootstrap%3Dtools';

vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams(`returnTo=${returnTo}`),
}));

const pendingStatus = {
  accepted: false,
  declined: false,
  currentVersion: 'bio-observational-v1',
};

describe('ConsentPage', () => {
  beforeEach(() => {
    fetchMock.mockReset();
    locationMock.replace.mockReset();
    returnTo = '%2Fprofiles%3Fbootstrap%3Dtools';
    vi.stubGlobal('fetch', fetchMock);
    vi.stubGlobal('location', locationMock);
  });

  it('records the server consent version and restores the approved return path', async () => {
    fetchMock
      .mockResolvedValueOnce({ ok: true, status: 200, json: async () => pendingStatus })
      .mockResolvedValueOnce({ ok: true, status: 200, json: async () => ({ ...pendingStatus, accepted: true }) });

    render(<ConsentPage />);

    expect(await screen.findByText('Consent record: bio-observational-v1')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'I agree and want to continue' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenLastCalledWith(
        '/api/v1/consent',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ consentVersion: 'bio-observational-v1' }),
        }),
      );
      expect(locationMock.replace).toHaveBeenCalledWith('/profiles?bootstrap=tools');
    });
  });

  it('persists refusal, logs out, and leaves unsaved preview on the device', async () => {
    fetchMock
      .mockResolvedValueOnce({ ok: true, status: 200, json: async () => pendingStatus })
      .mockResolvedValueOnce({ ok: true, status: 200, json: async () => ({ ...pendingStatus, declined: true }) })
      .mockResolvedValueOnce({ ok: true, status: 204 });

    render(<ConsentPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Not now' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/v1/consent/decline',
        expect.objectContaining({ method: 'POST' }),
      );
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/v1/auth/logout',
        expect.objectContaining({ method: 'POST' }),
      );
      expect(locationMock.replace).toHaveBeenCalledWith('/?consent=declined');
    });
  });

  it('sends an expired session back through sign-in without losing the return path', async () => {
    fetchMock.mockResolvedValueOnce({ ok: false, status: 401 });

    render(<ConsentPage />);

    await waitFor(() => {
      expect(locationMock.replace).toHaveBeenCalledWith(
        '/auth/signin?callbackUrl=%2Fonboarding%2Fconsent%3FreturnTo%3D%252Fprofiles%253Fbootstrap%253Dtools',
      );
    });
  });
});
