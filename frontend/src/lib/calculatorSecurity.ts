/**
 * Network lock-down for the public calculator pages (directive Phase 1, item 8).
 *
 * The calculator math runs entirely in the browser, so these routes need no third-party network access at all.
 * Browser calls are same-origin (see getApiBaseUrl: the Next rewrite proxies /api/v1), so `connect-src 'self'`
 * is enough for the session check, knowledge lookup and overlap check the page already makes. Everything else
 * (analytics beacons, third-party XHR/fetch/WebSocket/EventSource) is blocked by the browser.
 *
 * Only these exact routes get the policy. /tools/analyzer is deliberately NOT included: it is not a calculator.
 */
export const CALCULATOR_ROUTES = [
  '/tools',
  '/tools/reconstitution-calculator',
  '/tools/volume-calculator',
  '/tools/unit-converter',
] as const;

export function buildCalculatorCsp(nodeEnv: string | undefined = process.env.NODE_ENV): string {
  // Next.js emits inline hydration scripts; React refresh needs eval in development only.
  const scriptSrc = nodeEnv === 'production' ? "'self' 'unsafe-inline'" : "'self' 'unsafe-inline' 'unsafe-eval'";
  return [
    "connect-src 'self'",
    `script-src ${scriptSrc}`,
    "frame-src 'none'",
    "object-src 'none'",
    "base-uri 'self'",
    "form-action 'self'",
  ].join('; ');
}

/** Entries for next.config.ts `headers()`. */
export function calculatorSecurityHeaders(nodeEnv?: string): Array<{ source: string; headers: Array<{ key: string; value: string }> }> {
  return CALCULATOR_ROUTES.map((source) => ({
    source,
    headers: [{ key: 'Content-Security-Policy', value: buildCalculatorCsp(nodeEnv) }],
  }));
}
