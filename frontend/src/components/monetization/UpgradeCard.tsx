'use client';

import { useEntitlements } from '@/lib/entitlements';
import Link from 'next/link';
import { useState } from 'react';

interface UpgradeCardProps {
  eyebrow?: string;
  title: string;
  description: string;
  cta?: string;
  returnPath?: string;
  bullets?: string[];
  compact?: boolean;
}

export function UpgradeCard({
  eyebrow = 'Pro intelligence',
  title,
  description,
  cta = 'Unlock full stack intelligence',
  returnPath,
  bullets = [],
  compact = false,
}: UpgradeCardProps) {
  const { startCheckout } = useEntitlements();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleCheckout() {
    try {
      setLoading(true);
      setError(null);
      await startCheckout(returnPath);
    } catch {
      setError('Stripe checkout is not ready yet. Confirm billing config or sign in again.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className={`rounded-lg border border-emerald-400/20 bg-emerald-500/[0.07] ${compact ? 'p-4' : 'p-5'}`}>
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-200/70">{eyebrow}</p>
      <h3 className={`${compact ? 'mt-2 text-lg' : 'mt-3 text-xl'} font-bold text-white`}>{title}</h3>
      <p className="mt-2 text-sm leading-6 text-white/62">{description}</p>

      {bullets.length > 0 && (
        <ul className="mt-4 space-y-2 text-sm text-white/62">
          {bullets.map((bullet) => (
            <li key={bullet} className="flex gap-2">
              <span className="mt-2 h-1.5 w-1.5 rounded-full bg-emerald-300" />
              <span>{bullet}</span>
            </li>
          ))}
        </ul>
      )}

      <div className="mt-5 flex flex-wrap items-center gap-3">
        <button
          onClick={handleCheckout}
          disabled={loading}
          className="rounded-lg bg-emerald-400 px-4 py-2.5 text-sm font-bold text-slate-950 transition-colors hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {loading ? 'Opening Stripe...' : cta}
        </button>
        <Link href="/pricing" className="text-sm font-semibold text-white/55 transition-colors hover:text-white">
          Compare plans
        </Link>
      </div>

      {error && <p className="mt-3 text-xs text-amber-200/80">{error}</p>}
    </div>
  );
}
