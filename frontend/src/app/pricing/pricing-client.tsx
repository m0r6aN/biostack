'use client';

import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { apiClient } from '@/lib/api';
import { useEntitlements } from '@/lib/entitlements';
import { pricingTiers } from '@/lib/marketing';
import Link from 'next/link';
import { useEffect, useState } from 'react';

export default function PricingClient() {
  const { startCheckout, entitlements } = useEntitlements();
  const [email, setEmail] = useState('');
  const [leadState, setLeadState] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');
  const [checkoutError, setCheckoutError] = useState<string | null>(null);
  const [currentTier, setCurrentTier] = useState<string | null>(null);

  useEffect(() => {
    void apiClient.getCurrentSubscription()
      .then((subscription) => setCurrentTier(subscription.tier))
      .catch(() => setCurrentTier(null));
  }, []);

  async function captureClinicLead(event: React.FormEvent) {
    event.preventDefault();
    if (!email.trim()) return;

    try {
      setLeadState('saving');
      await apiClient.captureLead(email, 'clinics-coaches-pricing');
      setLeadState('saved');
      setEmail('');
    } catch {
      setLeadState('error');
    }
  }

  async function handleProCheckout() {
    try {
      setCheckoutError(null);
      await startCheckout('/pricing');
    } catch {
      setCheckoutError('Stripe checkout is not ready yet. Confirm billing config or sign in again.');
    }
  }

  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main id="main" tabIndex={-1} className="mx-auto max-w-7xl px-5 py-10 sm:px-8 sm:py-12 lg:py-14">
        <section className="max-w-3xl">
          <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
            Pricing
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-5xl">
            Simple pricing for smarter protocol tracking.
          </h1>
          <p className="mt-4 max-w-3xl text-lg leading-8 text-white/62">
            Observer is free forever. Upgrade to Operator for full stack analysis, or Commander for longitudinal intelligence.
          </p>
        </section>

        <section className="mt-10 grid gap-5 lg:grid-cols-3">
          {pricingTiers.map((tier) => (
            <article
              key={tier.name}
              className={`rounded-lg border p-6 ${tier.featured ? 'border-emerald-400/24 bg-emerald-500/[0.07] shadow-[0_16px_52px_rgba(16,185,129,0.08)]' : 'border-white/10 bg-white/[0.03]'}`}
            >
              <div className="flex items-start justify-between gap-3">
                <h2 className="text-2xl font-semibold text-white">{tier.name}</h2>
                {tier.featured && (
                  <span className="shrink-0 rounded-lg border border-emerald-300/20 px-3 py-1 text-xs uppercase tracking-[0.16em] text-emerald-200">
                    Most popular
                  </span>
                )}
              </div>
              <p className="mt-1 text-sm font-semibold text-white/80">{tier.description}</p>
              <p className="mt-4 text-4xl font-semibold text-white">{tier.monthly}</p>
              <p className="mt-2 text-sm leading-7 text-white/62">{tier.detail}</p>
              <ul className="mt-6 space-y-3 text-sm text-white/64">
                {tier.highlights.map((feature) => (
                  <li key={feature} className="flex gap-3">
                    <span className={`mt-1.5 h-1.5 w-1.5 rounded-full ${tier.featured ? 'bg-emerald-300' : 'bg-white/35'}`} />
                    <span>{feature}</span>
                  </li>
                ))}
              </ul>
              {currentTier === tier.name && tier.name !== 'Observer' ? (
                <p className="mt-7 inline-flex items-center gap-2 rounded-lg border border-emerald-300/30 bg-emerald-400/10 px-5 py-3 text-sm font-semibold text-emerald-100">
                  <span aria-hidden="true">✓</span> Subscribed
                </p>
              ) : tier.name === 'Operator' ? (
                <button
                  onClick={handleProCheckout}
                  className="mt-7 rounded-lg bg-emerald-400 px-5 py-3 text-sm font-bold text-slate-950 transition-colors hover:bg-emerald-300"
                >
                  {entitlements.isPro ? 'Manage Operator access' : tier.ctaLabel}
                </button>
              ) : (
                <Link
                  href={tier.href}
                  className="mt-7 inline-flex rounded-lg border border-white/12 px-5 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
                >
                  {tier.ctaLabel}
                </Link>
              )}
            </article>
          ))}
        </section>
        {checkoutError && <p className="mt-3 text-sm text-amber-100/80">{checkoutError}</p>}

        <section className="mt-10 rounded-lg border border-white/10 bg-[#101820]/90 p-6">
          <div className="grid gap-6 lg:grid-cols-[1fr_420px] lg:items-end">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-white/35">Clinics / Coaches</p>
              <h2 className="mt-3 text-2xl font-semibold text-white">Scale protocol intelligence across clients.</h2>
              <p className="mt-3 max-w-3xl text-sm leading-7 text-white/60">
                Built to support client seats, white-label surfaces, retail bundles, and co-branded protocol views without turning the product into an ad board.
              </p>
              <div className="mt-5 flex flex-wrap gap-2">
                {['client licenses', 'co-branded views', 'retail bundle slots', 'affiliate attribution ready'].map((item) => (
                  <span key={item} className="rounded-lg border border-white/10 px-3 py-1.5 text-xs text-white/55">
                    {item}
                  </span>
                ))}
              </div>
            </div>

            <form onSubmit={captureClinicLead} className="space-y-3">
              <label className="text-sm font-semibold text-white/70" htmlFor="clinic-email">
                Clinic or coach email
              </label>
              <div className="flex gap-2">
                <input
                  id="clinic-email"
                  type="email"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  placeholder="you@clinic.com"
                  className="min-w-0 flex-1 rounded-lg border border-white/[0.08] bg-black/20 px-3 py-2 text-sm text-white outline-none focus:border-emerald-400/50"
                />
                <button className="rounded-lg border border-white/12 px-4 py-2 text-sm font-semibold text-white transition-colors hover:border-white/24">
                  {leadState === 'saving' ? 'Sending' : 'Contact'}
                </button>
              </div>
              {leadState === 'saved' && <p className="text-sm text-emerald-200/80">Received. We will follow up with clinic options.</p>}
              {leadState === 'error' && <p className="text-sm text-amber-100/80">Could not save this lead right now.</p>}
            </form>
          </div>
        </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
