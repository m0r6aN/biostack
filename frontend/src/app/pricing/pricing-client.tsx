'use client';

import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { apiClient } from '@/lib/api';
import { useEntitlements } from '@/lib/entitlements';
import { useState } from 'react';

const freeFeatures = [
  '2 compounds',
  'Basic timeline and tracking',
  'Basic check-ins',
  'Calculator access',
  'Compound lookup and selection',
  'Single profile',
];

const proFeatures = [
  'Unlimited compounds',
  'Full stack intelligence',
  'Overlap, synergy, and compatibility analysis',
  'Protocol builder with phases and cycles',
  'Observability correlations',
  'Stack optimization suggestions',
  'Saved advanced protocol views',
];

export default function PricingClient() {
  const { startCheckout, entitlements } = useEntitlements();
  const [email, setEmail] = useState('');
  const [leadState, setLeadState] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');
  const [checkoutError, setCheckoutError] = useState<string | null>(null);

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
            Free tracks the stack. Pro explains it.
          </h1>
          <p className="mt-4 max-w-3xl text-lg leading-8 text-white/62">
            Start with useful tracking. Upgrade when BioStack has enough signal to explain overlap, compatibility, protocol timing, and optimization.
          </p>
        </section>

        <section className="mt-10 grid gap-5 lg:grid-cols-[1fr_1.1fr]">
          <article className="rounded-lg border border-white/10 bg-white/[0.03] p-6">
            <div className="flex items-center justify-between">
              <h2 className="text-2xl font-semibold text-white">Free</h2>
              <span className="rounded-lg border border-white/10 px-3 py-1 text-xs uppercase tracking-[0.16em] text-white/45">
                Tracking
              </span>
            </div>
            <p className="mt-4 text-4xl font-semibold text-white">$0</p>
            <p className="mt-2 text-sm leading-7 text-white/60">
              Useful from day one. Enough structure to log compounds, check in, calculate dosing math, and see the first stack signal.
            </p>
            <ul className="mt-6 space-y-3 text-sm text-white/64">
              {freeFeatures.map((feature) => (
                <li key={feature} className="flex gap-3">
                  <span className="mt-1.5 h-1.5 w-1.5 rounded-full bg-white/35" />
                  <span>{feature}</span>
                </li>
              ))}
            </ul>
            <a
              href="/onboarding"
              className="mt-7 inline-flex rounded-lg border border-white/12 px-5 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
            >
              Start free
            </a>
          </article>

          <article className="rounded-lg border border-emerald-400/24 bg-emerald-500/[0.07] p-6 shadow-[0_16px_52px_rgba(16,185,129,0.08)]">
            <div className="flex items-center justify-between gap-4">
              <h2 className="text-2xl font-semibold text-white">Pro</h2>
              <span className="rounded-lg border border-emerald-300/20 px-3 py-1 text-xs uppercase tracking-[0.16em] text-emerald-200">
                Intelligence
              </span>
            </div>
            <p className="mt-4 text-4xl font-semibold text-white">$19<span className="text-lg text-white/55">/mo</span></p>
            <p className="mt-2 text-sm leading-7 text-white/68">
              The actual engine: stack intelligence, observability correlations, protocol planning, and optimization suggestions grounded in your data.
            </p>
            <ul className="mt-6 space-y-3 text-sm text-white/70">
              {proFeatures.map((feature) => (
                <li key={feature} className="flex gap-3">
                  <span className="mt-1.5 h-1.5 w-1.5 rounded-full bg-emerald-300" />
                  <span>{feature}</span>
                </li>
              ))}
            </ul>
            <button
              onClick={handleProCheckout}
              className="mt-7 rounded-lg bg-emerald-400 px-5 py-3 text-sm font-bold text-slate-950 transition-colors hover:bg-emerald-300"
            >
              {entitlements.isPro ? 'Manage Pro access' : 'Upgrade to Pro'}
            </button>
            {checkoutError && <p className="mt-3 text-sm text-amber-100/80">{checkoutError}</p>}
          </article>
        </section>

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
