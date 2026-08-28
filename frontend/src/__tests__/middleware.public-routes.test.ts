import middleware from '@/middleware';
import { NextRequest } from 'next/server';
import { afterEach, describe, expect, it, vi } from 'vitest';

function requestFor(pathname: string, cookie?: string) {
  return new NextRequest(`https://biostack.test${pathname}`, {
    headers: cookie ? { cookie } : undefined,
  });
}

describe('middleware public route access', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it.each(['/knowledge', '/knowledge/creatine', '/start', '/onboarding', '/map', '/tools/analyzer'])(
    'allows anonymous evidence browsing at %s',
    async (pathname) => {
      const response = await middleware(requestFor(pathname));

      expect(response.status).not.toBe(307);
      expect(response.headers.get('location')).toBeNull();
    }
  );

  it.each(['/profiles', '/profiles/abc', '/compounds', '/billing', '/admin/research'])(
    'keeps private route %s behind sign-in',
    async (pathname) => {
      const response = await middleware(requestFor(pathname));

      expect(response.status).toBe(307);
      expect(response.headers.get('location')).toContain('/auth/signin');
    }
  );

  it.each(['/knowledge-private', '/toolshed', '/apiary']) (
    'does not treat a near-prefix route %s as public',
    async (pathname) => {
      const response = await middleware(requestFor(pathname));

      expect(response.status).toBe(307);
      expect(response.headers.get('location')).toContain('/auth/signin');
    },
  );

  it('admits a protected route only after the backend validates the cookie', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(
      JSON.stringify({ authenticated: true, user: { id: '1' } }),
      { status: 200, headers: { 'content-type': 'application/json' } },
    ));
    vi.stubGlobal('fetch', fetchMock);

    const response = await middleware(requestFor('/profiles', 'biostack_session=valid-ticket'));

    expect(response.status).toBe(200);
    expect(response.headers.get('location')).toBeNull();
    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5050/api/v1/auth/session',
      expect.objectContaining({
        headers: { cookie: 'biostack_session=valid-ticket' },
        cache: 'no-store',
      }),
    );
  });

  it('forwards only the BioStack session cookie to backend validation', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(
      JSON.stringify({ authenticated: true }),
      { status: 200, headers: { 'content-type': 'application/json' } },
    ));
    vi.stubGlobal('fetch', fetchMock);

    await middleware(requestFor(
      '/profiles',
      'analytics_id=do-not-forward; biostack_session=valid-ticket; preferences=private',
    ));

    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5050/api/v1/auth/session',
      expect.objectContaining({
        headers: { cookie: 'biostack_session=valid-ticket' },
      }),
    );
  });

  it('rejects an unreadable cookie and forwards the backend cookie deletion', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(
      JSON.stringify({ authenticated: false, user: null }),
      {
        status: 200,
        headers: {
          'content-type': 'application/json',
          'set-cookie': 'biostack_session=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; httponly',
        },
      },
    )));

    const response = await middleware(requestFor('/billing?plan=operator', 'biostack_session=stale-ticket'));

    expect(response.status).toBe(307);
    expect(response.headers.get('location')).toContain('callbackUrl=%2Fbilling%3Fplan%3Doperator');
    expect(response.headers.get('location')).toContain('error=session-expired');
    expect(response.headers.get('set-cookie')).toContain('biostack_session=;');
  });

  it('fails closed when backend session validation is unavailable', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('offline')));

    const response = await middleware(requestFor('/profiles', 'biostack_session=unknown-ticket'));

    expect(response.status).toBe(307);
    expect(response.headers.get('location')).toContain('error=session-unavailable');
  });
});
