'use client';

import { LoadingState } from '@/components/LoadingState';
import { apiClient } from '@/lib/api';
import { useEntitlements } from '@/lib/entitlements';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';

export default function BillingSuccessPage() {
  return (
    <Suspense fallback={<LoadingState />}>
      <BillingSuccessContent />
    </Suspense>
  );
}

function BillingSuccessContent() {
  const searchParams = useSearchParams();
  const returnPath = searchParams.get('return') || '/protocol-console';
  const { refreshEntitlements } = useEntitlements();
  const [status, setStatus] = useState<'checking' | 'pro' | 'pending'>('checking');

  useEffect(() => {
    let cancelled = false;

    async function refresh() {
      const next = await apiClient.refreshEntitlements().catch(() => null);
      if (cancelled) return;

      if (next?.isPro) {
        setStatus('pro');
        await refreshEntitlements();
        return;
      }

      setStatus('pending');
    }

    void refresh();
    return () => {
      cancelled = true;
    };
  }, [refreshEntitlements]);

  return (
    <main className="mx-auto flex min-h-screen max-w-2xl flex-col justify-center px-6">
      <div className="rounded-lg border border-emerald-300/20 bg-[#121923]/90 p-8">
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-emerald-200/70">Billing</p>
        <h1 className="mt-3 text-3xl font-bold text-white">
          {status === 'checking' ? 'Confirming Pro access' : status === 'pro' ? 'Pro intelligence is active' : 'Payment received'}
        </h1>
        <p className="mt-3 text-sm leading-6 text-white/60">
          {status === 'checking'
            ? 'We are refreshing your entitlement record from the backend source of truth.'
            : status === 'pro'
              ? 'Your Pro gates are now open. Stack intelligence, protocol planning, and observability correlations are available.'
              : 'Stripe is still sending confirmation. This usually clears in a moment after the webhook arrives.'}
        </p>
        <div className="mt-6 flex flex-wrap gap-3">
          <Link href={returnPath} className="rounded-lg bg-emerald-400 px-5 py-3 text-sm font-bold text-slate-950">
            Return to BioStack
          </Link>
          <Link href="/pricing" className="rounded-lg border border-white/12 px-5 py-3 text-sm font-semibold text-white">
            View plans
          </Link>
        </div>
      </div>
    </main>
  );
}
