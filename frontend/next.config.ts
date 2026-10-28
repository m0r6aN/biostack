import type { NextConfig } from "next";
import path from "path";
import { calculatorSecurityHeaders } from "./src/lib/calculatorSecurity";
import { mergedRecordRedirects } from "./src/lib/mergedRecords";

const apiUrl = (process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5050').replace(/\/$/, '');

const nextConfig: NextConfig = {
  output: 'standalone',
  typescript: {
    tsconfigPath: 'tsconfig.build.json',
  },
  turbopack: {
    root: path.resolve(__dirname),
  },
  async rewrites() {
    return [
      {
        source: '/api/v1/:path*',
        destination: `${apiUrl}/api/v1/:path*`,
      },
      {
        source: '/api/analyze/:path*',
        destination: `${apiUrl}/api/analyze/:path*`,
      },
    ];
  },
  async headers() {
    return [
      {
        source: '/auth/verify',
        headers: [
          { key: 'Cache-Control', value: 'no-store' },
          { key: 'Referrer-Policy', value: 'no-referrer' },
        ],
      },
      // Calculator pages: same-origin network only, no third-party scripts (see src/lib/calculatorSecurity.ts).
      ...calculatorSecurityHeaders(),
    ];
  },
  async redirects() {
    return [
      {
        source: '/calculators',
        destination: '/tools',
        permanent: true,
      },
      // Retired duplicate compound records (2026-10-25 consolidation).
      ...mergedRecordRedirects(),
    ];
  },
};

export default nextConfig;
