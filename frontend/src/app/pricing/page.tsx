import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { pricingTiers } from '@/lib/marketing';
import Link from 'next/link';

export default function PricingPage() {
  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main className="mx-auto max-w-7xl px-5 py-10 sm:px-8 sm:py-12 lg:py-14">
        <section className="max-w-3xl">
          <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
            Plans
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-5xl">
            Simple pricing for smarter protocol tracking
          </h1>
          <p className="mt-4 max-w-3xl text-lg leading-8 text-white/62">
            Start free with the essentials. Upgrade when you want better tracking, deeper insights,
            and a clearer understanding of how your stack is working.
          </p>
        </section>

        <section className="mt-10 grid gap-5 lg:grid-cols-3">
          {pricingTiers.map((tier) => (
            <article
              key={tier.name}
              className={`rounded-[2rem] border p-6 ${
                tier.featured
                  ? 'border-emerald-400/24 bg-emerald-500/[0.07] shadow-[0_16px_52px_rgba(16,185,129,0.08)]'
                  : 'border-white/10 bg-white/[0.03]'
              }`}
            >
              <div className="flex items-center justify-between">
                <h2 className="text-2xl font-semibold text-white">{tier.name}</h2>
                {tier.featured && (
                  <span className="rounded-full border border-emerald-300/20 px-3 py-1 text-xs uppercase tracking-[0.2em] text-emerald-200">
                    Most Popular
                  </span>
                )}
              </div>

              <p className="mt-4 text-base font-semibold text-white/92">{tier.description}</p>
              <p className="mt-2 text-sm leading-7 text-white/60">{tier.detail}</p>

              <div className="mt-5 border-y border-white/8 py-4">
                <p className="text-3xl font-semibold text-white">{tier.monthly}</p>
                <p className="mt-2 text-sm text-white/50">or {tier.annual}</p>
                <p className="mt-1 text-sm text-emerald-200/85">{tier.annualEffective}</p>
              </div>

              <p className="mt-5 text-xs font-semibold uppercase tracking-[0.2em] text-white/38">
                Includes
              </p>

              <ul className="mt-4 space-y-2.5 text-sm text-white/64">
                {tier.highlights.map((highlight) => (
                  <li key={highlight} className="flex gap-3">
                    <span className="mt-1 h-1.5 w-1.5 rounded-full bg-emerald-300" />
                    <span>{highlight}</span>
                  </li>
                ))}
              </ul>

              <Link
                href={tier.href}
                className={`mt-6 inline-flex rounded-full px-5 py-3 text-sm font-semibold transition-transform hover:-translate-y-0.5 ${
                  tier.featured
                    ? 'bg-emerald-400 text-slate-950'
                    : 'border border-white/12 text-white'
                }`}
              >
                {tier.ctaLabel}
              </Link>
            </article>
          ))}
        </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
