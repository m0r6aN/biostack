import {
  authenticateWithPasskey,
  registerPasskey,
  decodeCreationOptions,
  decodeRequestOptions,
  passkeyAuthenticationErrorMessage,
  serializeAuthenticationCredential,
  serializeRegistrationCredential,
  PasskeyRequestError,
} from '@/lib/passkeys';
import { afterEach, describe, expect, it, vi } from 'vitest';

afterEach(() => vi.unstubAllGlobals());

function bytes(...values: number[]) {
  return new Uint8Array(values).buffer;
}

describe('passkey WebAuthn codecs', () => {
  it.each([401, 429, 500])('preserves API status %s without treating it as authenticator cancellation', async status => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false, status, json: async () => ({ message: 'private server detail' }) }));
    await expect(registerPasskey('Test passkey')).rejects.toMatchObject({ name: 'PasskeyRequestError', status });
  });

  it('preserves the verified-email error code without exposing the server message', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false, status: 403, json: async () => ({ code: 'verified_email_required', message: 'private server detail' }),
    }));
    await expect(registerPasskey('Test passkey')).rejects.toMatchObject({ code: 'verified_email_required' });
    await expect(registerPasskey('Test passkey')).rejects.not.toThrow('private server detail');
  });
  it('decodes base64url registration fields into browser buffers', () => {
    const options = decodeCreationOptions({
      challenge: 'AQID',
      rp: { id: 'localhost', name: 'BioStack' },
      user: { id: 'BAUG', name: 'user@example.com', displayName: 'User' },
      pubKeyCredParams: [{ type: 'public-key', alg: -7 }],
      excludeCredentials: [{ type: 'public-key', id: 'BwgJ' }],
    });

    expect(Array.from(new Uint8Array(options.challenge as ArrayBuffer))).toEqual([1, 2, 3]);
    expect(Array.from(new Uint8Array(options.user.id as ArrayBuffer))).toEqual([4, 5, 6]);
    expect(Array.from(new Uint8Array(options.excludeCredentials![0].id as ArrayBuffer))).toEqual([7, 8, 9]);
  });

  it('decodes a discoverable assertion challenge without adding an allow-list', () => {
    const options = decodeRequestOptions({
      challenge: 'AQID',
      rpId: 'localhost',
      allowCredentials: [],
      userVerification: 'required',
    });

    expect(Array.from(new Uint8Array(options.challenge as ArrayBuffer))).toEqual([1, 2, 3]);
    expect(options.allowCredentials).toEqual([]);
    expect(options.userVerification).toBe('required');
  });

  it('serializes attestation and assertion byte fields as unpadded base64url', () => {
    const registration = serializeRegistrationCredential({
      id: 'credential',
      rawId: bytes(251, 255),
      type: 'public-key',
      response: {
        attestationObject: bytes(1, 2),
        clientDataJSON: bytes(3, 4),
        getTransports: () => ['internal'],
      },
      getClientExtensionResults: () => ({ credProps: { rk: true } }),
    } as unknown as PublicKeyCredential);
    const authentication = serializeAuthenticationCredential({
      id: 'credential',
      rawId: bytes(251, 255),
      type: 'public-key',
      response: {
        authenticatorData: bytes(5),
        signature: bytes(6),
        clientDataJSON: bytes(7),
        userHandle: bytes(8),
      },
      getClientExtensionResults: () => ({}),
    } as unknown as PublicKeyCredential);

    expect(registration.rawId).toBe('-_8');
    expect(registration.response.attestationObject).toBe('AQI');
    expect(registration.response.transports).toEqual(['internal']);
    expect(authentication.response.userHandle).toBe('CA');
    expect(authentication.response.signature).toBe('Bg');
  });

  it('runs a discoverable assertion and returns only a validated local redirect', async () => {
    class FakePublicKeyCredential {}
    const credential = Object.assign(new FakePublicKeyCredential(), {
      id: 'credential',
      rawId: bytes(1, 2),
      type: 'public-key',
      response: {
        authenticatorData: bytes(3),
        signature: bytes(4),
        clientDataJSON: bytes(5),
        userHandle: bytes(6),
      },
      getClientExtensionResults: () => ({}),
    }) as unknown as PublicKeyCredential;
    const get = vi.fn().mockResolvedValue(credential);
    Object.defineProperty(navigator, 'credentials', { configurable: true, value: { get } });
    vi.stubGlobal('PublicKeyCredential', FakePublicKeyCredential);
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          requestId: 'request-id',
          publicKey: {
            challenge: 'AQID',
            rpId: 'localhost',
            allowCredentials: [],
            userVerification: 'required',
          },
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ redirectPath: '/profiles' }),
      });
    vi.stubGlobal('fetch', fetchMock);

    await expect(authenticateWithPasskey('/profiles')).resolves.toBe('/profiles');
    expect(get).toHaveBeenCalledWith(expect.objectContaining({
      mediation: 'optional',
      publicKey: expect.objectContaining({ userVerification: 'required', allowCredentials: [] }),
    }));
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/v1/auth/passkeys/authenticate/complete',
      expect.objectContaining({ body: expect.stringContaining('"requestId":"request-id"') }),
    );
  });
});

describe('passkeyAuthenticationErrorMessage', () => {
  it('tells the user their passkey was not recognized, distinct from a cancelled prompt', () => {
    const unrecognized = passkeyAuthenticationErrorMessage(new PasskeyRequestError(400, 'invalid_passkey'));
    const cancelled = passkeyAuthenticationErrorMessage(Object.assign(new Error('cancelled'), { name: 'NotAllowedError' }));

    expect(unrecognized).toBe("BioStack didn't recognize that passkey. Add it again from Account settings, or use your email link.");
    expect(cancelled).toBe('The passkey request was cancelled or timed out. Choose Sign in with a passkey to try again.');
    expect(unrecognized).not.toBe(cancelled);
  });

  it('does not confuse a server outage with an unrecognized credential', () => {
    expect(passkeyAuthenticationErrorMessage(new PasskeyRequestError(503))).toBe(
      'BioStack could not check your passkey right now. Please try again shortly.'
    );
    expect(passkeyAuthenticationErrorMessage(new PasskeyRequestError(429))).toBe(
      'Too many attempts. Wait a few minutes, then try your passkey again.'
    );
  });

  it('falls back to a generic message for an unclassified error', () => {
    expect(passkeyAuthenticationErrorMessage(new Error('boom'))).toBe(
      'We could not use that passkey. Try again or use your email link.'
    );
  });
});
