import { describe, expect, it } from 'vitest';
import nextConfig from '../../../next.config';

describe('next config API rewrites', () => {
  it('defaults local API rewrites to the backend launch URL', async () => {
    const rewrites = await nextConfig.rewrites?.();

    expect(rewrites).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          source: '/api/v1/:path*',
          destination: 'http://localhost:5050/api/v1/:path*',
        }),
        expect.objectContaining({
          source: '/api/analyze/:path*',
          destination: 'http://localhost:5050/api/analyze/:path*',
        }),
      ]),
    );
  });

  it('prevents verify-page token referrers and caching', async () => {
    const headers = await nextConfig.headers?.();

    expect(headers).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          source: '/auth/verify',
          headers: expect.arrayContaining([
            { key: 'Cache-Control', value: 'no-store' },
            { key: 'Referrer-Policy', value: 'no-referrer' },
          ]),
        }),
      ]),
    );
  });
});
