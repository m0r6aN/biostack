import type { ProtocolTier } from '@/lib/types';

interface TierGateProps {
  /** Tier this section is intended to require once billing is wired. */
  requiredTier: ProtocolTier;
  children: React.ReactNode;
}

/**
 * Paywall seam (currently INERT).
 *
 * Gating is not enforced in PR B. This wrapper exists so sections can declare
 * their intended tier today and be gated with a one-spot change later.
 *
 * To enable: resolve the current subscription (apiClient.getCurrentSubscription)
 * and, when the user's tier is below `requiredTier`, render the existing
 * LockedTierCard pattern (see app/protocols/page.tsx) instead of `children`.
 */
export function TierGate({ requiredTier, children }: TierGateProps) {
  // TODO: enforce when billing wiring lands. `requiredTier` is intentionally
  // referenced so the prop is not flagged as unused.
  void requiredTier;
  return <>{children}</>;
}
