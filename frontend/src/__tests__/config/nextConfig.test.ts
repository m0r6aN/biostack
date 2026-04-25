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
});
