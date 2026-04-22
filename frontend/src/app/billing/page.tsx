'use client';

import Link from 'next/link';
import { ErrorState } from '@/components/ErrorState';
import { Header } from '@/components/Header';
import { LoadingSkeleton } from '@/components/LoadingState';
import { apiClient } from '@/lib/api';
import { CurrentSubscription } from '@/lib/types';
import { useEffect, useState } from 'react';

function formatPeriodEnd(value: string | null) {
  if (!value) return '';
  return new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(value));
}

function stateCopy(subscription: CurrentSubscription) {
  if (subscription.tier === 'Observer') {
    return {
      label: 'Observer',
      title: 'Core tracking is active.',
      detail: 'Observer includes up to 5 active compounds. Existing data stays available if a paid plan ends.',
    };
  }

  if (subscription.cancelAtPeriodEnd) {
    return {
      label: `${subscription.tier} canceling`,
      title: `${subscription.tier} access continues until ${formatPeriodEnd(subscription.currentPeriodEndUtc)}.`,
      detail: 'After the period ends, the account returns to Observer. Data stays saved, and new gated actions lock again if you are over the free limit.',
    };
  }

  return {
    label: subscription.tier,
    title: `${subscription.tier} is active.`,
    detail: subscription.tier === 'Commander'
      ? 'Advanced protocol review, pattern, drift, sequence, and mission-control surfaces are unlocked.'
      : 'Stack intelligence and unlimited active compounds are unlocked.',
  };
}

export default function BillingPage() {
  const [subscription, setSubscription] = useState<CurrentSubscription | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);

  const load = async () => {
    try {
      setLoading(true);
      setSubscription(await apiClient.getCurrentSubscription());
      setError(null);
    } catch {
      setError('Billing state could not be loaded.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const startCheckout = async (planCode: 'operator' | 'commander') => {
    try {
      setBusy(planCode);
      const session = await apiClient.createCheckoutSession(planCode);
      window.location.href = session.url;
    } catch {
      setError('Checkout is not available yet. Confirm Stripe price configuration.');
    } finally {
      setBusy(null);
    }
  };

  const manageBilling = async () => {
    try {
      setBusy('portal');
      const session = await apiClient.createBillingPortalSession();
      window.location.href = session.url;
    } catch {
      setError('Billing management is not available for this account yet.');
    } finally {
      setBusy(null);
    }
  };

  const copy = subscription ? stateCopy(subscription) : null;
  const activeLimit = subscription?.limits.active_compounds;

  return (
    <div className="w-full">
      <Header title="Billing" />

      <div className="max-w-4xl space-y-6 p-4 sm:p-8">
        {loading ? (
          <LoadingSkeleton />
        ) : error ? (
          <ErrorState message={error} onRetry={load} />
        ) : subscription && copy ? (
          <>
            <section className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-6">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-200/70">
                Current plan
              </p>
              <div className="mt-3 flex flex-wrap items-start justify-between gap-4">
                <div>
                  <h2 className="text-2xl font-semibold text-white">{copy.label}</h2>
                  <p className="mt-2 text-sm leading-6 text-white/65">{copy.title}</p>
                  <p className="mt-1 max-w-2xl text-sm leading-6 text-white/45">{copy.detail}</p>
                </div>
                {subscription.isPaid ? (
                  <button
                    onClick={manageBilling}
                    disabled={busy === 'portal'}
                    className="rounded-lg bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-300 disabled:opacity-50"
                  >
                    {busy === 'portal' ? 'Opening...' : 'Manage Billing'}
                  </button>
                ) : (
                  <Link
                    href="/pricing"
                    className="rounded-lg border border-white/[0.1] px-5 py-3 text-sm font-semibold text-white/80 hover:border-emerald-300/30"
                  >
                    Compare plans
                  </Link>
                )}
              </div>
            </section>

            {subscription.tier === 'Observer' && activeLimit === 5 && (
              <section className="rounded-lg border border-amber-300/15 bg-amber-400/[0.06] p-5">
                <h3 className="text-base font-semibold text-amber-100">Observer active compound limit</h3>
                <p className="mt-2 text-sm leading-6 text-amber-50/70">
                  Observer is capped at 5 active compounds. If a paid plan ends while more are active, your data remains saved, but adding or reactivating active compounds is blocked until enough records are paused or completed.
                </p>
              </section>
            )}

            <section className="grid gap-4 md:grid-cols-2">
              <div className="rounded-lg border border-white/[0.08] bg-white/[0.025] p-5">
                <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/35">Operator</p>
                <h3 className="mt-2 text-xl font-semibold text-white">Stack intelligence</h3>
                <p className="mt-2 text-sm leading-6 text-white/55">Unlock current stack scoring, interaction intelligence, and remove the Observer active compound cap.</p>
                <button
                  onClick={() => startCheckout('operator')}
                  disabled={busy !== null}
                  className="mt-5 rounded-lg border border-emerald-300/25 px-4 py-2 text-sm font-semibold text-emerald-100 transition-colors hover:bg-emerald-400/10 disabled:opacity-50"
                >
                  {busy === 'operator' ? 'Opening...' : 'Upgrade to Operator'}
                </button>
              </div>

              <div className="rounded-lg border border-white/[0.08] bg-white/[0.025] p-5">
                <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/35">Commander</p>
                <h3 className="mt-2 text-xl font-semibold text-white">Historical intelligence</h3>
                <p className="mt-2 text-sm leading-6 text-white/55">Unlock protocol review, pattern memory, drift, sequence expectation, and mission control.</p>
                <button
                  onClick={() => startCheckout('commander')}
                  disabled={busy !== null}
                  className="mt-5 rounded-lg bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-300 disabled:opacity-50"
                >
                  {busy === 'commander' ? 'Opening...' : 'Upgrade to Commander'}
                </button>
              </div>
            </section>
          </>
        ) : null}
      </div>
    </div>
  );
}
