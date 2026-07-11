import Link from 'next/link';
import type { ProtocolTier } from '@/lib/types';

interface TierGateProps {
  requiredTier: ProtocolTier;
  currentTier: ProtocolTier;
  children: React.ReactNode;
}

const TIER_RANK: Record<ProtocolTier, number> = {
  observer: 0,
  operator: 1,
  commander: 2,
};

const TIER_LABEL: Record<ProtocolTier, string> = {
  observer: 'Observer — Free',
  operator: 'Operator — Track & Analyze',
  commander: 'Commander — Longitudinal Intelligence',
};

export function TierGate({ requiredTier, currentTier, children }: TierGateProps) {
  if (TIER_RANK[currentTier] >= TIER_RANK[requiredTier]) {
    return <>{children}</>;
  }

  return (
    <section className="rounded-2xl border border-emerald-300/15 bg-emerald-400/[0.05] p-6">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-200/70">
        {TIER_LABEL[requiredTier]}
      </p>
      <h2 className="mt-2 text-xl font-semibold text-white">Upgrade to unlock this protocol view</h2>
      <p className="mt-2 max-w-2xl text-sm leading-6 text-white/55">
        Your current plan stays active everywhere else. Compare plans to unlock this section.
      </p>
      <Link
        href="/pricing"
        className="mt-5 inline-flex rounded-lg bg-emerald-400 px-4 py-2.5 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-300"
      >
        Compare plans
      </Link>
    </section>
  );
}
