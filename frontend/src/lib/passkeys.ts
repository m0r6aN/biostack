import { getApiBaseUrl } from './apiBase';

const API_URL = getApiBaseUrl();

type PasskeyOptionsEnvelope = {
  requestId: string;
  publicKey: Record<string, unknown>;
};

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
    throw new Error(`passkey-request-${response.status}`);
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
