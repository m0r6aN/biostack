import { BADGE_JS } from '@/lib/embed';

// Growth Phase 8: <script src="https://biostack.cc/embed/badge.js" async></script> turns each
// <div data-biostack-claim="claim-id"> into the live verdict iframe. No cookies, no tracking.
export const dynamic = 'force-static';

export function GET() {
  return new Response(BADGE_JS, {
    headers: {
      'Content-Type': 'application/javascript; charset=utf-8',
      'Cache-Control': 'public, max-age=3600, s-maxage=86400',
      'X-Robots-Tag': 'noindex',
      'Access-Control-Allow-Origin': '*',
    },
  });
}
