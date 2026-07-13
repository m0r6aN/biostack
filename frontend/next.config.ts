import type { NextConfig } from "next";
import path from "path";

const apiUrl = (process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5050').replace(/\/$/, '');

const nextConfig: NextConfig = {
  output: 'standalone',
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
    ];
  },
  async redirects() {
    return [
      {
        source: '/calculators',
        destination: '/tools',
        permanent: true,
      },
    ];
  },
};

export default nextConfig;
