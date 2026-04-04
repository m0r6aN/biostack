import Link from 'next/link';
import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { pricingTiers } from '@/lib/marketing';

export default function PricingPage() {
  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main className="mx-auto max-w-7xl px-5 py-16 sm:px-8 lg:py-20">
        <section className="max-w-4xl">
          <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
            Plans
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-5xl">
            One platform. Built for serious use.
          </h1>
          <p className="mt-5 max-w-3xl text-lg leading-8 text-white/62">
            Observer is free to start. Operator is the primary workflow tier. Commander is the
            advanced intelligence layer. Annual billing is the default economic path because the
            real value compounds over months, not days.
          </p>
        </section>

        <section className="mt-14 grid gap-6 lg:grid-cols-3">
          {pricingTiers.map((tier) => (
            <article
              key={tier.name}
              className={`rounded-[2rem] border p-7 ${
                tier.featured
                  ? 'border-emerald-400/28 bg-emerald-500/8 shadow-[0_20px_80px_rgba(16,185,129,0.08)]'
                  : 'border-white/10 bg-white/[0.03]'
              }`}
            >
              <div className="flex items-center justify-between">
                <h2 className="text-2xl font-semibold text-white">{tier.name}</h2>
                {tier.featured && (
                  <span className="rounded-full border border-emerald-300/20 px-3 py-1 text-xs uppercase tracking-[0.2em] text-emerald-200">
                    Primary
                  </span>
                )}
              </div>

              <p className="mt-4 text-white/60">{tier.description}</p>

              <div className="mt-6 border-y border-white/8 py-5">
                <p className="text-3xl font-semibold text-white">{tier.monthly}</p>
                <p className="mt-2 text-sm text-white/50">or {tier.annual} billed annually</p>
                <p className="mt-1 text-sm text-emerald-200/85">{tier.annualEffective}</p>
              </div>

              <ul className="mt-6 space-y-3 text-sm text-white/64">
                {tier.highlights.map((highlight) => (
                  <li key={highlight} className="flex gap-3">
                    <span className="mt-1 h-1.5 w-1.5 rounded-full bg-emerald-300" />
                    <span>{highlight}</span>
                  </li>
                ))}
              </ul>

              <Link
                href={tier.href}
                className={`mt-8 inline-flex rounded-full px-5 py-3 text-sm font-semibold transition-transform hover:-translate-y-0.5 ${
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
