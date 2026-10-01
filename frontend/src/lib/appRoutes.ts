// Route policy for authenticated application surfaces. Kept out of the
// product-contract JSON (a synced artifact) — this is app routing policy.
//
// The middleware and the client AuthProvider must agree: only these
// surfaces bounce visitors to sign-in. Anything outside them (and the
// public allowlist) falls through to the router, which renders the 404
// page with recovery links. APIs remain the authorization boundary.
export const PROTECTED_ROUTE_PREFIXES = [
  '/admin',
  '/billing',
  '/compounds',
  '/map-my-stack',
  '/my-protocol',
  '/profiles',
  '/protocol-console',
  '/protocols',
  '/settings',
];

export function isProtectedRoutePath(pathname: string): boolean {
  return PROTECTED_ROUTE_PREFIXES.some(
    (prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`),
  );
}
