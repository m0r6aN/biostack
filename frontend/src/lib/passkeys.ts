import { getApiBaseUrl } from './apiBase';

const API_URL = getApiBaseUrl();

type PasskeyOptionsEnvelope = {
  requestId: string;
  publicKey: Record<string, unknown>;
};

export class PasskeyRequestError extends Error {
  constructor(readonly status: number, readonly code?: string) {
    super(`passkey-request-${status}`);
    this.name = 'PasskeyRequestError';
  }
}

export function passkeyRegistrationErrorMessage(error: unknown): string {
  if (error instanceof PasskeyRequestError) {
    if (error.status === 401) return 'Your session has expired. Sign in again, then add your passkey.';
    if (error.status === 403 && error.code === 'verified_email_required') return 'Verify your email using an email sign-in link, then add your passkey.';
    if (error.status === 429) return 'Too many attempts. Wait a few minutes, then try adding your passkey again.';
    if (error.status >= 500) return 'BioStack could not save your passkey. Please try again shortly.';
    if (error.code === 'invalid_passkey') return 'BioStack could not verify this passkey request. Start again with Add passkey.';
    return 'BioStack could not complete passkey registration. Please try again or contact support@biostack.cc.';
  }
  if (error instanceof Error || (typeof DOMException !== 'undefined' && error instanceof DOMException)) {
    if (error.name === 'NotAllowedError' || error.name === 'AbortError') return 'The passkey request was cancelled or timed out. Choose Add passkey to try again.';
    if (error.name === 'InvalidStateError') return 'This authenticator already has a passkey for your account. Try another device or passkey manager.';
    if (error.name === 'SecurityError') return 'Passkeys could not be used on this address. Open https://biostack.cc and try again.';
    if (error.name === 'NotSupportedError') return 'This device or passkey manager could not create a passkey. Try another supported option.';
  }
  return 'Your passkey could not be added. Check your connection and try again, or contact support@biostack.cc.';
}

// Distinct from passkeyRegistrationErrorMessage: sign-in failures need their own copy because
// the most common real-world cause here is a *different* one — a passkey that a passkey manager
// still offers locally but that BioStack's server no longer has a matching record for (for
// example, one created during an earlier registration attempt that never completed). That case
// and a merely cancelled or timed-out browser prompt need to read differently to the person
// choosing between "try the passkey again" and "add a new one from Account settings".
export function passkeyAuthenticationErrorMessage(error: unknown): string {
  if (error instanceof PasskeyRequestError) {
    if (error.status === 429) return 'Too many attempts. Wait a few minutes, then try your passkey again.';
    if (error.status >= 500) return 'BioStack could not check your passkey right now. Please try again shortly.';
    if (error.code === 'invalid_passkey') return "BioStack didn't recognize that passkey. Add it again from Account settings, or use your email link.";
    return 'BioStack could not complete passkey sign-in. Try again or use your email link.';
  }
  if (error instanceof Error || (typeof DOMException !== 'undefined' && error instanceof DOMException)) {
    if (error.name === 'NotAllowedError' || error.name === 'AbortError') return 'The passkey request was cancelled or timed out. Choose Sign in with a passkey to try again.';
    if (error.name === 'SecurityError') return 'Passkeys could not be used on this address. Open https://biostack.cc and try again.';
    if (error.name === 'NotSupportedError') return 'This device or passkey manager could not complete sign-in. Use your email link instead.';
    if (error.message === 'passkey-not-selected') return 'No passkey was selected. Choose Sign in with a passkey to try again.';
    if (error.message === 'invalid-return-path') return 'BioStack could not verify where to send you next. Use your email link instead.';
  }
  return 'We could not use that passkey. Try again or use your email link.';
}

function decodeBase64Url(value: string): ArrayBuffer {
  const normalized = value.replace(/-/g, '+').replace(/_/g, '/');
  const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
  const bytes = Uint8Array.from(atob(padded), character => character.charCodeAt(0));
  return bytes.buffer;
}

function encodeBase64Url(value: ArrayBuffer): string {
  const bytes = new Uint8Array(value);
  let binary = '';
  bytes.forEach(byte => {
    binary += String.fromCharCode(byte);
  });
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}

export function decodeCreationOptions(value: Record<string, unknown>): PublicKeyCredentialCreationOptions {
  const options = structuredClone(value) as unknown as PublicKeyCredentialCreationOptions & {
    challenge: string | ArrayBuffer;
    user: PublicKeyCredentialUserEntity & { id: string | ArrayBuffer };
    excludeCredentials?: Array<PublicKeyCredentialDescriptor & { id: string | ArrayBuffer }>;
  };
  options.challenge = decodeBase64Url(options.challenge as unknown as string);
  options.user.id = decodeBase64Url(options.user.id as unknown as string);
  options.excludeCredentials = options.excludeCredentials?.map(credential => ({
    ...credential,
    id: decodeBase64Url(credential.id as unknown as string),
  }));
  return options as PublicKeyCredentialCreationOptions;
}

export function decodeRequestOptions(value: Record<string, unknown>): PublicKeyCredentialRequestOptions {
  const options = structuredClone(value) as unknown as PublicKeyCredentialRequestOptions & {
    challenge: string | ArrayBuffer;
    allowCredentials?: Array<PublicKeyCredentialDescriptor & { id: string | ArrayBuffer }>;
  };
  options.challenge = decodeBase64Url(options.challenge as unknown as string);
  options.allowCredentials = options.allowCredentials?.map(credential => ({
    ...credential,
    id: decodeBase64Url(credential.id as unknown as string),
  }));
  return options as PublicKeyCredentialRequestOptions;
}

export function serializeRegistrationCredential(credential: PublicKeyCredential) {
  const response = credential.response as AuthenticatorAttestationResponse;
  return {
    id: credential.id,
    rawId: encodeBase64Url(credential.rawId),
    type: credential.type,
    response: {
      attestationObject: encodeBase64Url(response.attestationObject),
      clientDataJSON: encodeBase64Url(response.clientDataJSON),
      transports: response.getTransports?.() ?? [],
    },
    clientExtensionResults: credential.getClientExtensionResults(),
  };
}

export function serializeAuthenticationCredential(credential: PublicKeyCredential) {
  const response = credential.response as AuthenticatorAssertionResponse;
  return {
    id: credential.id,
    rawId: encodeBase64Url(credential.rawId),
    type: credential.type,
    response: {
      authenticatorData: encodeBase64Url(response.authenticatorData),
      signature: encodeBase64Url(response.signature),
      clientDataJSON: encodeBase64Url(response.clientDataJSON),
      userHandle: response.userHandle ? encodeBase64Url(response.userHandle) : null,
    },
    clientExtensionResults: credential.getClientExtensionResults(),
  };
}

export function passkeysSupported() {
  return typeof window !== 'undefined' && 'PublicKeyCredential' in window && Boolean(navigator.credentials);
}

async function postJson<T>(path: string, body: unknown): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    method: 'POST',
    credentials: 'include',
    cache: 'no-store',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { code?: unknown } | null;
    throw new PasskeyRequestError(response.status, typeof body?.code === 'string' ? body.code : undefined);
  }
  return response.json() as Promise<T>;
}

export async function authenticateWithPasskey(redirectPath: string): Promise<string> {
  const envelope = await postJson<PasskeyOptionsEnvelope>('/api/v1/auth/passkeys/authenticate/options', { redirectPath });
  const credential = await navigator.credentials.get({
    publicKey: decodeRequestOptions(envelope.publicKey),
    mediation: 'optional',
  });
  if (!(credential instanceof PublicKeyCredential)) {
    throw new Error('passkey-not-selected');
  }
  const result = await postJson<{ redirectPath: string }>('/api/v1/auth/passkeys/authenticate/complete', {
    requestId: envelope.requestId,
    credential: serializeAuthenticationCredential(credential),
  });
  if (!result.redirectPath.startsWith('/') || result.redirectPath.startsWith('//') || result.redirectPath.includes('\\')) {
    throw new Error('invalid-return-path');
  }
  return result.redirectPath;
}

export async function registerPasskey(displayName: string) {
  const envelope = await postJson<PasskeyOptionsEnvelope>('/api/v1/auth/passkeys/register/options', { displayName });
  const credential = await navigator.credentials.create({
    publicKey: decodeCreationOptions(envelope.publicKey),
  });
  if (!(credential instanceof PublicKeyCredential)) {
    throw new Error('passkey-not-created');
  }
  return postJson('/api/v1/auth/passkeys/register/complete', {
    requestId: envelope.requestId,
    displayName,
    credential: serializeRegistrationCredential(credential),
  });
}
