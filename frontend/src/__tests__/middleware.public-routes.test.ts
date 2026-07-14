import middleware from '@/middleware';
import { NextRequest } from 'next/server';
import { describe, expect, it } from 'vitest';

function requestFor(pathname: string, cookie?: string) {
  return new NextRequest(`https://biostack.test${pathname}`, {
    headers: cookie ? { cookie } : undefined,
  });
}

describe('middleware public route access', () => {
  it.each(['/knowledge', '/knowledge/creatine', '/start', '/onboarding', '/map', '/tools/analyzer'])(
    'allows anonymous evidence browsing at %s',
    (pathname) => {
      const response = middleware(requestFor(pathname));

      expect(response.status).not.toBe(307);
      expect(response.headers.get('location')).toBeNull();
    }
  );

  it.each(['/profiles', '/profiles/abc', '/compounds', '/billing', '/admin/research'])(
    'keeps private route %s behind sign-in',
    (pathname) => {
      const response = middleware(requestFor(pathname));

      expect(response.status).toBe(307);
      expect(response.headers.get('location')).toContain('/auth/signin');
    }
  );

  it.each(['/knowledge-private', '/toolshed', '/apiary']) (
    'does not treat a near-prefix route %s as public',
    (pathname) => {
      const response = middleware(requestFor(pathname));

      expect(response.status).toBe(307);
      expect(response.headers.get('location')).toContain('/auth/signin');
    },
  );
});
