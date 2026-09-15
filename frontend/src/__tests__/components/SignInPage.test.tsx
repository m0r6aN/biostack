import SignInPage from '@/app/auth/signin/page';
import { ANALYZER_PROTOCOL_DRAFT_KEY, markAnalyzerProtocolDraftImported, saveAnalyzerProtocolDraft } from '@/lib/analyzerStorage';
import { PasskeyRequestError } from '@/lib/passkeys';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const fetchMock = vi.fn();
const authenticateWithPasskeyMock = vi.fn();
const passkeysSupportedMock = vi.fn(() => false);
let callbackUrl = '%2Fprofiles';

vi.stubGlobal('fetch', fetchMock);

vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams(`callbackUrl=${callbackUrl}`),
}));

vi.mock('@/lib/passkeys', async () => {
  const actual = await vi.importActual<typeof import('@/lib/passkeys')>('@/lib/passkeys');
  return {
    ...actual,
    passkeysSupported: () => passkeysSupportedMock(),
    authenticateWithPasskey: (...args: Parameters<typeof actual.authenticateWithPasskey>) =>
      authenticateWithPasskeyMock(...args),
  };
});

describe('SignInPage', () => {
  beforeEach(() => {
    fetchMock.mockReset();
    authenticateWithPasskeyMock.mockReset();
    passkeysSupportedMock.mockReset().mockReturnValue(false);
    callbackUrl = '%2Fprofiles';
    localStorage.clear();
  });

  it('starts passwordless email auth and moves to the inbox step', async () => {
    fetchMock.mockResolvedValue({ ok: true, json: async () => ({}) });

    render(<SignInPage />);

    fireEvent.change(screen.getByLabelText('Email'), {
      target: { value: 'User@Example.com' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Email me a sign-in link' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/v1/auth/start',
        expect.objectContaining({
          method: 'POST',
          credentials: 'include',
          body: JSON.stringify({
            contact: 'user@example.com',
            channel: 'email',
            redirectPath: '/profiles',
          }),
        })
      );
    });

    expect(screen.getByText('Check your inbox')).toBeInTheDocument();
    expect(screen.getByText('ur**@example.com')).toBeInTheDocument();
  });

  it('keeps the form visible when the sign-in link cannot be sent', async () => {
    fetchMock.mockResolvedValue({ ok: false });

    render(<SignInPage />);

    fireEvent.change(screen.getByLabelText('Email'), {
      target: { value: 'User@Example.com' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Email me a sign-in link' }));

    expect(await screen.findByText('We could not send that sign-in link. Try again in a moment.')).toBeInTheDocument();
    expect(screen.queryByText('Check your inbox')).not.toBeInTheDocument();
  });

  it('reassures analyzer conversions that saved work carries through sign-in', () => {
    callbackUrl = '%2Fprotocol-console';
    saveAnalyzerProtocolDraft({ sourceAnalysisId: 'analysis-1', goal: 'Tracking', protocol: [{ compoundName: 'Example', dose: 1, unit: 'mg', frequency: 'daily', duration: '' }], optimizedProtocol: [] });

    render(<SignInPage />);

    expect(screen.getByText('Your saved analysis is waiting on this browser.')).toBeInTheDocument();
    expect(
      screen.getByText('Finish sign-in here, then review the compounds you entered before they are added to a profile. Nothing is applied automatically.')
    ).toBeInTheDocument();
  });

  it.each(['missing', 'malformed', 'imported'])('does not promise saved work for a %s draft on a console return', (state) => {
    callbackUrl = '%2Fprotocol-console';
    if (state === 'malformed') localStorage.setItem(ANALYZER_PROTOCOL_DRAFT_KEY, '{invalid');
    if (state === 'imported') {
      const draft = saveAnalyzerProtocolDraft({ sourceAnalysisId: 'analysis-1', goal: 'Tracking', protocol: [{ compoundName: 'Example', dose: 1, unit: 'mg', frequency: 'daily', duration: '' }], optimizedProtocol: [] });
      markAnalyzerProtocolDraftImported('profile-1', draft);
    }

    render(<SignInPage />);

    expect(screen.queryByText('Your saved analysis is waiting on this browser.')).not.toBeInTheDocument();
  });

  it('does not translate an absolute callback URL into a local return path', async () => {
    callbackUrl = 'https%3A%2F%2Fevil.example%2Fprofiles';
    fetchMock.mockResolvedValue({ ok: true, json: async () => ({}) });

    render(<SignInPage />);
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'user@example.com' } });
    fireEvent.click(screen.getByRole('button', { name: 'Email me a sign-in link' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/v1/auth/start',
        expect.objectContaining({
          body: JSON.stringify({
            contact: 'user@example.com',
            channel: 'email',
            redirectPath: '/protocol-console',
          }),
        }),
      );
    });
  });

  it('shows a distinct message when the server does not recognize the selected passkey', async () => {
    passkeysSupportedMock.mockReturnValue(true);
    fetchMock.mockResolvedValue({ ok: true, json: async () => ({ enabled: true }) });
    authenticateWithPasskeyMock.mockRejectedValue(new PasskeyRequestError(400, 'invalid_passkey'));

    render(<SignInPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Sign in with a passkey' }));

    expect(
      await screen.findByText("BioStack didn't recognize that passkey. Add it again from Account settings, or use your email link.")
    ).toBeInTheDocument();
  });

  it('shows a distinct message when the browser prompt is cancelled or times out', async () => {
    passkeysSupportedMock.mockReturnValue(true);
    fetchMock.mockResolvedValue({ ok: true, json: async () => ({ enabled: true }) });
    authenticateWithPasskeyMock.mockRejectedValue(Object.assign(new Error('cancelled'), { name: 'NotAllowedError' }));

    render(<SignInPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Sign in with a passkey' }));

    expect(
      await screen.findByText('The passkey request was cancelled or timed out. Choose Sign in with a passkey to try again.')
    ).toBeInTheDocument();
  });

  it('redirects to the server-issued path after a successful passkey sign-in', async () => {
    passkeysSupportedMock.mockReturnValue(true);
    fetchMock.mockResolvedValue({ ok: true, json: async () => ({ enabled: true }) });
    authenticateWithPasskeyMock.mockResolvedValue('/profiles');
    const replace = vi.fn();
    Object.defineProperty(window, 'location', { configurable: true, value: { replace } });

    render(<SignInPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Sign in with a passkey' }));

    await waitFor(() => expect(replace).toHaveBeenCalledWith('/profiles'));
  });

  it('does not translate a scheme-relative callback URL into a local return path', async () => {
    callbackUrl = '%2F%2Fevil.example%2Fprofiles';
    fetchMock.mockResolvedValue({ ok: true, json: async () => ({}) });

    render(<SignInPage />);
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'user@example.com' } });
    fireEvent.click(screen.getByRole('button', { name: 'Email me a sign-in link' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/v1/auth/start',
        expect.objectContaining({
          body: JSON.stringify({
            contact: 'user@example.com',
            channel: 'email',
            redirectPath: '/protocol-console',
          }),
        }),
      );
    });
  });
});
