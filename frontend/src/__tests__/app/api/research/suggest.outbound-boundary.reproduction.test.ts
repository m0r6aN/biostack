import { POST } from '@/app/api/research/suggest/route';

const CONSENT_URL = 'http://trusted-biostack.test/api/v1/consent';
const PROVIDER_URL = 'https://api.openai.com/v1/responses';
const SESSION_COOKIE = 'biostack_session=synthetic-session';

const unauthenticatedBody = {
  compound: { name: 'Synthetic Compound Alpha' },
  candidate: { blockers: [] },
  evidencePacket: null,
  planItems: [],
};

function canonicalConsent(accepted: boolean) {
  return {
    accepted,
    consentAcceptedAtUtc: accepted ? '2026-09-02T00:00:00Z' : null,
    consentVersion: accepted ? 'v1' : null,
    declined: false,
    consentDeclinedAtUtc: null,
    consentDeclinedVersion: null,
    currentVersion: 'v1',
  };
}

function consentResponse(value: unknown) {
  return new Response(JSON.stringify(value), {
    status: 200,
    headers: { 'content-type': 'application/json; charset=utf-8' },
  });
}

function inboundRequest(cookie?: string, body: unknown = unauthenticatedBody, extraHeaders?: HeadersInit) {
  const headers = new Headers(extraHeaders);
  if (cookie !== undefined) headers.set('cookie', cookie);
  return new Request('https://hostile-client.invalid/api/research/suggest?consented=true', {
    method: 'POST',
    headers,
    body: JSON.stringify(body),
  });
}

function requestUrl(input: string | URL | Request) {
  return typeof input === 'string' ? input : input instanceof URL ? input.toString() : input.url;
}

function assertConsentRequest(init: RequestInit | undefined) {
  expect(init?.method).toBe('GET');
  expect(init?.body).toBeUndefined();
  expect(init?.cache).toBe('no-store');
  expect(init?.redirect).toBe('manual');
  expect(init?.signal).toBeInstanceOf(AbortSignal);
  expect([...new Headers(init?.headers).entries()]).toEqual([
    ['cookie', SESSION_COOKIE],
  ]);
}

function expectDenied(response: Response) {
  expect(response.status >= 200 && response.status < 300).toBe(false);
}

describe('research suggestion outbound boundary reproduction', () => {
  const originalApiKey = process.env.OPENAI_API_KEY;
  const originalSuggestEnabled = process.env.RESEARCH_AI_SUGGEST_ENABLED;
  const originalApiUrl = process.env.API_URL;
  const originalNextPublicApiUrl = process.env.NEXT_PUBLIC_API_URL;

  beforeEach(() => {
    process.env.OPENAI_API_KEY = 'synthetic-test-key';
    process.env.RESEARCH_AI_SUGGEST_ENABLED = 'true';
    process.env.API_URL = 'http://trusted-biostack.test/';
    delete process.env.NEXT_PUBLIC_API_URL;
  });

  afterEach(() => {
    if (originalApiKey === undefined) delete process.env.OPENAI_API_KEY;
    else process.env.OPENAI_API_KEY = originalApiKey;

    if (originalSuggestEnabled === undefined) delete process.env.RESEARCH_AI_SUGGEST_ENABLED;
    else process.env.RESEARCH_AI_SUGGEST_ENABLED = originalSuggestEnabled;

    if (originalApiUrl === undefined) delete process.env.API_URL;
    else process.env.API_URL = originalApiUrl;

    if (originalNextPublicApiUrl === undefined) delete process.env.NEXT_PUBLIC_API_URL;
    else process.env.NEXT_PUBLIC_API_URL = originalNextPublicApiUrl;

    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it('does not relay a provider request without an authenticated or consented caller', async () => {
    let consentCalls = 0;
    let providerCalls = 0;
    const localFetch = vi.fn(async (input: string | URL | Request) => {
      const url = requestUrl(input);
      if (url === CONSENT_URL) consentCalls += 1;
      if (url === PROVIDER_URL) providerCalls += 1;
      throw new Error(`Unexpected fetch without one valid session cookie: ${url}`);
    });
    vi.stubGlobal('fetch', localFetch);

    const variants: Array<string | undefined> = [
      undefined,
      'decoy=one; biostack_session=',
      'biostack_session',
      'biostack_session =synthetic-session',
      'biostack_session= synthetic-session',
      'biostack_session=first; decoy=one; biostack_session=second',
    ];
    for (const cookie of variants) {
      const response = await POST(inboundRequest(cookie));
      expectDenied(response);
    }

    expect(consentCalls).toBe(0);
    expect(providerCalls).toBe(0);
    expect(localFetch).not.toHaveBeenCalled();
  });

  it('denies when the consent request fails', async () => {
    let consentCalls = 0;
    let providerCalls = 0;
    const localFetch = vi.fn(async (input: string | URL | Request, init?: RequestInit) => {
      const url = requestUrl(input);
      if (url === CONSENT_URL) {
        consentCalls += 1;
        assertConsentRequest(init);
        throw new Error('Synthetic local consent failure');
      }
      if (url === PROVIDER_URL) providerCalls += 1;
      throw new Error(`Unexpected non-local fetch target: ${url}`);
    });
    vi.stubGlobal('fetch', localFetch);

    const response = await POST(inboundRequest(SESSION_COOKIE));

    expectDenied(response);
    expect(consentCalls).toBe(1);
    expect(providerCalls).toBe(0);
  });

  it('denies when the consent request exceeds the exact timeout', async () => {
    vi.useFakeTimers();
    let consentCalls = 0;
    let providerCalls = 0;
    let observedSignal: AbortSignal | undefined;
    let markConsentStarted: (() => void) | undefined;
    const consentStarted = new Promise<void>(resolve => { markConsentStarted = resolve; });
    const localFetch = vi.fn((input: string | URL | Request, init?: RequestInit) => {
      const url = requestUrl(input);
      if (url === CONSENT_URL) {
        consentCalls += 1;
        assertConsentRequest(init);
        observedSignal = init?.signal ?? undefined;
        markConsentStarted?.();
        return new Promise<Response>((_resolve, reject) => {
          observedSignal?.addEventListener('abort', () => reject(new Error('Synthetic abort')), { once: true });
        });
      }
      if (url === PROVIDER_URL) providerCalls += 1;
      throw new Error(`Unexpected non-local fetch target: ${url}`);
    });
    vi.stubGlobal('fetch', localFetch);

    const pendingResponse = POST(inboundRequest(SESSION_COOKIE));
    await consentStarted;
    expect(observedSignal?.aborted).toBe(false);
    await vi.advanceTimersByTimeAsync(1_999);
    expect(observedSignal?.aborted).toBe(false);
    await vi.advanceTimersByTimeAsync(1);
    const response = await pendingResponse;

    expectDenied(response);
    expect(observedSignal?.aborted).toBe(true);
    expect(consentCalls).toBe(1);
    expect(providerCalls).toBe(0);
  });

  it('denies a consent redirect without following it', async () => {
    let consentCalls = 0;
    let providerCalls = 0;
    const localFetch = vi.fn(async (input: string | URL | Request, init?: RequestInit) => {
      const url = requestUrl(input);
      if (url === CONSENT_URL) {
        consentCalls += 1;
        assertConsentRequest(init);
        return new Response(null, { status: 302, headers: { location: 'https://redirect.invalid/consent' } });
      }
      if (url === PROVIDER_URL) providerCalls += 1;
      throw new Error(`Unexpected non-local fetch target: ${url}`);
    });
    vi.stubGlobal('fetch', localFetch);

    const response = await POST(inboundRequest(SESSION_COOKIE));

    expectDenied(response);
    expect(consentCalls).toBe(1);
    expect(providerCalls).toBe(0);
  });

  it('denies a non-success consent response', async () => {
    let consentCalls = 0;
    let providerCalls = 0;
    const localFetch = vi.fn(async (input: string | URL | Request, init?: RequestInit) => {
      const url = requestUrl(input);
      if (url === CONSENT_URL) {
        consentCalls += 1;
        assertConsentRequest(init);
        return new Response('Synthetic unavailable', { status: 503 });
      }
      if (url === PROVIDER_URL) providerCalls += 1;
      throw new Error(`Unexpected non-local fetch target: ${url}`);
    });
    vi.stubGlobal('fetch', localFetch);

    const response = await POST(inboundRequest(SESSION_COOKIE));

    expectDenied(response);
    expect(consentCalls).toBe(1);
    expect(providerCalls).toBe(0);
  });

  it('denies a malformed consent response', async () => {
    let consentCalls = 0;
    let providerCalls = 0;
    const localFetch = vi.fn(async (input: string | URL | Request, init?: RequestInit) => {
      const url = requestUrl(input);
      if (url === CONSENT_URL) {
        consentCalls += 1;
        assertConsentRequest(init);
        return consentResponse({ ...canonicalConsent(true), callerSuppliedAuthority: true });
      }
      if (url === PROVIDER_URL) providerCalls += 1;
      throw new Error(`Unexpected non-local fetch target: ${url}`);
    });
    vi.stubGlobal('fetch', localFetch);

    const response = await POST(inboundRequest(SESSION_COOKIE));

    expectDenied(response);
    expect(consentCalls).toBe(1);
    expect(providerCalls).toBe(0);
  });

  it('denies an oversized streamed consent response', async () => {
    let consentCalls = 0;
    let providerCalls = 0;
    let streamCancelled = false;
    const oversizedBody = new ReadableStream<Uint8Array>({
      start(controller) {
        controller.enqueue(new TextEncoder().encode(`{"padding":"${'x'.repeat(16_384)}"}`));
      },
      cancel() {
        streamCancelled = true;
      },
    });
    const localFetch = vi.fn(async (input: string | URL | Request, init?: RequestInit) => {
      const url = requestUrl(input);
      if (url === CONSENT_URL) {
        consentCalls += 1;
        assertConsentRequest(init);
        return new Response(oversizedBody, {
          status: 200,
          headers: { 'content-type': 'application/json' },
        });
      }
      if (url === PROVIDER_URL) providerCalls += 1;
      throw new Error(`Unexpected non-local fetch target: ${url}`);
    });
    vi.stubGlobal('fetch', localFetch);

    const response = await POST(inboundRequest(SESSION_COOKIE));

    expectDenied(response);
    expect(streamCancelled).toBe(true);
    expect(consentCalls).toBe(1);
    expect(providerCalls).toBe(0);
  });

  it('denies when current server consent is not accepted despite caller spoofing', async () => {
    let consentCalls = 0;
    let providerCalls = 0;
    const localFetch = vi.fn(async (input: string | URL | Request, init?: RequestInit) => {
      const url = requestUrl(input);
      if (url === CONSENT_URL) {
        consentCalls += 1;
        assertConsentRequest(init);
        return consentResponse(canonicalConsent(false));
      }
      if (url === PROVIDER_URL) providerCalls += 1;
      throw new Error(`Unexpected non-local fetch target: ${url}`);
    });
    vi.stubGlobal('fetch', localFetch);

    const response = await POST(inboundRequest(
      SESSION_COOKIE,
      { ...unauthenticatedBody, authenticated: true, consentAccepted: true },
      {
        authorization: 'Bearer caller-supplied-token',
        'x-authenticated': 'true',
        'x-consent-accepted': 'true',
      },
    ));

    expectDenied(response);
    expect(consentCalls).toBe(1);
    expect(providerCalls).toBe(0);
  });
});
