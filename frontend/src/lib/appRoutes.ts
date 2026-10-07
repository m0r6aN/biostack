// Route policy for authenticated application surfaces. Kept out of the
// product-contract JSON (a synced artifact) — this is app routing policy.
//
// The middleware and the client gates (AuthProvider, AppShell) must agree:
// only these surfaces bounce visitors to sign-in. Anything outside them
// and the public allowlist falls through to the router, which renders the
// 404 page with recovery links. APIs remain the authorization boundary.
//
// Keep in sync with the page routes under frontend/src/app: every real page
// that is not in the contract's public list must appear here.
export const PROTECTED_ROUTE_PREFIXES = [
  '/account',
  '/admin',
  '/billing',
  '/checkins',
  '/compounds',
  '/governance',
  '/map-my-stack',
  '/mission-control',
  '/my-protocol',
  '/profiles',
  '/protocol-console',
  '/protocols',
  '/receipts',
  '/timeline',
];

export function isProtectedRoutePath(pathname: string): boolean {
  return PROTECTED_ROUTE_PREFIXES.some(
    (prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`),
  );
}
