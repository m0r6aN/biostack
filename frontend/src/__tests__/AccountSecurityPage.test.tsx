import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import AccountSecurityPage from '@/app/account/security/page';
import { PasskeyRequestError, registerPasskey } from '@/lib/passkeys';

vi.mock('@/components/Header', () => ({ Header: () => <header>Account security</header> }));
vi.mock('@/lib/passkeys', async importOriginal => ({
  ...await importOriginal<typeof import('@/lib/passkeys')>(),
  passkeysSupported: () => true,
  registerPasskey: vi.fn(),
}));

const ok = (body: unknown) => ({ ok: true, json: async () => body });
let fetchMock: ReturnType<typeof vi.fn>;

beforeEach(() => {
  vi.mocked(registerPasskey).mockReset();
  fetchMock = vi.fn().mockResolvedValueOnce(ok({ enabled: true })).mockResolvedValueOnce(ok([]));
  vi.stubGlobal('fetch', fetchMock);
});
afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

describe('account passkey registration', () => {
  it.each([
    [new PasskeyRequestError(401), /session has expired/i],
    [new PasskeyRequestError(500), /BioStack could not save your passkey/i],
    [new DOMException('denied', 'NotAllowedError'), /cancelled or timed out/i],
  ])('shows the relevant registration failure and allows retry', async (error, expected) => {
    vi.mocked(registerPasskey).mockRejectedValue(error);
    render(<AccountSecurityPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Add passkey' }));
    expect(await screen.findByText(expected)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Add passkey' })).toBeEnabled();
    expect(screen.getByRole('textbox', { name: 'Passkey name' })).toHaveValue('My passkey');
  });

  it('lists a successfully saved passkey', async () => {
    vi.mocked(registerPasskey).mockResolvedValue({});
    fetchMock.mockResolvedValueOnce(ok({ enabled: true })).mockResolvedValueOnce(ok([
      { id: 'one', displayName: 'My passkey', transports: ['internal'], createdAtUtc: '2026-09-14T12:00:00Z' },
    ]));
    render(<AccountSecurityPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Add passkey' }));
    expect(await screen.findByText('Passkey added. You can use it the next time you sign in.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Remove' })).toBeInTheDocument();
  });

  it('does not report registration failure when only the subsequent list refresh fails', async () => {
    vi.mocked(registerPasskey).mockResolvedValue({});
    fetchMock.mockResolvedValueOnce(ok({ enabled: true })).mockResolvedValueOnce({ ok: false });
    render(<AccountSecurityPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Add passkey' }));
    expect(await screen.findByText(/Your passkey was added, but the list could not be refreshed/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Add passkey' })).not.toBeInTheDocument();
    fetchMock.mockResolvedValueOnce(ok({ enabled: true })).mockResolvedValueOnce(ok([]));
    fireEvent.click(screen.getByRole('button', { name: 'Reload passkey settings' }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Add passkey' })).toBeEnabled());
    expect(registerPasskey).toHaveBeenCalledTimes(1);
  });

  it('reports a settings outage rather than falsely saying passkeys are disabled', async () => {
    fetchMock.mockReset().mockResolvedValueOnce({ ok: false });
    render(<AccountSecurityPage />);
    expect(await screen.findByRole('button', { name: 'Reload passkey settings' })).toBeInTheDocument();
    expect(screen.queryByText(/Passkeys are not enabled/)).not.toBeInTheDocument();
    expect(screen.queryByText(/No passkeys enrolled/)).not.toBeInTheDocument();
  });
});
