import { LandingHero } from '@/components/marketing/LandingHero';
import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { featuredFaqs, landingFeatures, pricingTiers } from '@/lib/marketing';
import Link from 'next/link';

export default function HomePage() {
  const softwareSchema = {
    '@context': 'https://schema.org',
    '@type': 'SoftwareApplication',
    name: 'BioStack Protocol Console',
    applicationCategory: 'HealthApplication',
    operatingSystem: 'Web',
    description:
      'Protocol intelligence platform for serious self-experimenters who manage complex compound stacks.',
  };

  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main>
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(softwareSchema) }}
        />
        <LandingHero />

        <section className="mx-auto max-w-7xl px-5 py-12 sm:px-8 lg:py-16">
          <div className="grid gap-10 lg:grid-cols-[0.9fr_1.1fr]">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
                The Current State
              </p>
              <h2 className="mt-4 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
                Your protocol is more complex than your system for tracking it.
              </h2>
            </div>

            <div className="space-y-5 text-lg leading-8 text-white/60">
              <p>
                You have the compounds, the notes, the papers, and the intent. What breaks down is
                the system that holds all of it together under pressure.
              </p>
              <p>
                BioStack closes the gap between a spreadsheet and a clinical system without pretending
                to be a prescribing tool. It gives serious users protocol infrastructure they actually use.
              </p>
              <p className="text-base font-medium text-emerald-200/76">
                Used by individuals and providers to bring structure and clarity to complex protocols.
              </p>
            </div>
          </div>
        </section>

        <section className="border-y border-white/8 bg-black/15">
          <div className="mx-auto max-w-7xl px-5 py-12 sm:px-8 lg:py-16">
            <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
              What BioStack Provides
            </p>
            <h2 className="mt-4 max-w-3xl text-3xl font-semibold tracking-tight text-white sm:text-4xl">
              Everything your protocol actually requires.
            </h2>
            <p className="mt-5 max-w-2xl text-base leading-7 text-white/56">
              This is where most tracking breaks down: dose math in one place, check-ins in another,
              and guidance scattered across sources you cannot compare cleanly.
            </p>

            <div className="mt-10 grid gap-x-10 gap-y-5 md:grid-cols-2">
              {landingFeatures.map((feature) => (
                <div key={feature} className="border-b border-white/8 py-4">
                  <p className="text-lg text-white">{feature}</p>
                </div>
              ))}
            </div>
          </div>
        </section>

        <section className="mx-auto max-w-7xl px-5 py-12 sm:px-8 lg:py-16">
          <div className="grid gap-10 lg:grid-cols-[0.9fr_1.1fr]">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
                What BioStack Helps You Catch Early
              </p>
              <h2 className="mt-4 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
                You may not be getting more benefit just because you added more.
              </h2>
            </div>

            <div className="space-y-5 text-lg leading-8 text-white/60">
              <p>
                You add BPC-157 and TB-500 for recovery. That seems logical.
              </p>
              <p>
                But both target similar pathways, so you may not be getting additional benefit while increasing exposure.
              </p>
              <p>
                BioStack flags the overlap, shows evidence strength, and helps you track whether
                it is actually working.
              </p>
              <p className="border-l-2 border-emerald-400/50 pl-5 text-base text-white/56">
                The point is not to tell you what to take. The point is to show what deserves a closer look.
              </p>
            </div>
          </div>
        </section>

        <section className="border-y border-white/8 bg-black/15">
          <div className="mx-auto max-w-7xl px-5 py-12 sm:px-8 lg:py-16">
            <div className="grid gap-10 lg:grid-cols-[1fr_0.9fr]">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
                  Guidance Clarity
                </p>
                <h2 className="mt-4 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
                  When guidance conflicts, BioStack gives you structure.
                </h2>
                <div className="mt-5 space-y-4 text-lg leading-8 text-white/62">
                  <p>One source says 4mg weekly. Another says start at 0.25mg multiple times per week.</p>
                  <p>
                    BioStack doesn&apos;t pick sides. It shows typical ranges, how compounds are commonly used,
                    and helps you track what works for you.
                  </p>
                  <p className="text-base font-medium text-emerald-200/76">
                    See how compounds are commonly used - without guessing who to trust.
                  </p>
                </div>
              </div>

              <div className="space-y-3">
                {[
                  'Typical dosage ranges, not prescriptions',
                  'Frequency patterns observed in real protocols',
                  'Evidence tier attached to guidance',
                  'Track your response over time',
                ].map((item) => (
                  <div key={item} className="rounded-lg border border-white/8 bg-white/[0.03] px-4 py-3">
                    <p className="text-base text-white/78">{item}</p>
                  </div>
                ))}
              </div>
            </div>

            <div className="mt-10 border-t border-white/8 pt-8">
              <div className="grid gap-8 lg:grid-cols-[0.8fr_1.2fr]">
                <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
                  Context Improves Precision
                </p>
                <div className="space-y-4 text-lg leading-8 text-white/60">
                  <p>
                    General ranges are a starting point. Your profile - age, weight, goals, and tracking -
                    is what turns guidance into clarity.
                  </p>
                  <p>
                    BioStack gets more useful as it understands you. It doesn&apos;t guess. It uses your
                    inputs and your data to make guidance more relevant over time.
                  </p>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section className="mx-auto max-w-7xl px-5 py-12 sm:px-8 lg:py-16">
          <div className="grid gap-10 lg:grid-cols-[1fr_0.9fr]">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
                Precision Tools
              </p>
              <h2 className="mt-4 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
                The calculators that should have existed years ago.
              </h2>
              <p className="mt-5 max-w-2xl text-lg leading-8 text-white/62">
                Reconstitution, volume, and unit conversion math live on public tool pages because the
                value should arrive before the pitch does.
              </p>
            </div>

            <div className="space-y-4">
              {[
                ['Reconstitution Calculator', 'mg powder + mL diluent → transparent concentration math'],
                ['Volume Calculator', 'target dose + concentration → exact mL to draw'],
                ['Unit Conversion', 'mg, mcg, and g conversion with no spreadsheet overhead'],
              ].map(([title, body]) => (
                <div key={title} className="border-b border-white/8 pb-4">
                  <p className="text-xl text-white">{title}</p>
                  <p className="mt-2 text-white/58">{body}</p>
                </div>
              ))}
            </div>
          </div>

          <Link
            href="/tools"
            className="mt-10 inline-flex rounded-full border border-white/12 px-6 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
          >
            Open the Calculators
          </Link>
        </section>

        <section className="border-y border-white/8 bg-black/20">
          <div className="mx-auto max-w-7xl px-5 py-12 sm:px-8 lg:py-16">
          <div className="grid gap-10 lg:grid-cols-[0.85fr_1.15fr]">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
                Built On Clarity
              </p>
              <h2 className="mt-4 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
                BioStack is not a doctor. It is infrastructure for what you are already doing.
              </h2>
            </div>

            <div className="space-y-5 text-lg leading-8 text-white/60">
              <p>
                BioStack does not prescribe, diagnose, or recommend. The knowledge base cites evidence tiers.
                The overlap engine surfaces flags, not verdicts. The calculators give you math, not interpretation.
              </p>
              <p className="border-l-2 border-emerald-400/50 pl-5 text-base text-white/52">
                BioStack is a personal tracking and intelligence tool. It is not a medical device,
                diagnostic tool, or source of clinical advice.
              </p>
            </div>
          </div>
          </div>
        </section>

        <section className="mx-auto max-w-7xl px-5 py-12 sm:px-8 lg:py-16">
          <div className="flex items-end justify-between gap-6">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
                Plans
              </p>
              <h2 className="mt-4 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
                Start free. Upgrade when your protocol demands more.
              </h2>
              <p className="mt-4 max-w-2xl text-base leading-7 text-white/56">
                More context means more precise insights. Start with structure, then go deeper as the protocol does.
              </p>
            </div>
            <Link href="/pricing" className="text-sm font-semibold text-emerald-200 hover:text-white">
              See full pricing
            </Link>
          </div>

          <div className="mt-10 grid gap-5 lg:grid-cols-3">
            {pricingTiers.map((tier) => (
              <article
                key={tier.name}
                className={`rounded-lg border p-6 ${
                  tier.featured ? 'border-emerald-400/24 bg-emerald-500/8' : 'border-white/10 bg-white/[0.03]'
                }`}
              >
                <div className="flex items-start justify-between gap-4">
                  <p className="text-sm uppercase tracking-[0.22em] text-white/38">{tier.name}</p>
                  {tier.featured && (
                    <span className="rounded-full border border-emerald-300/18 bg-emerald-500/10 px-3 py-1 text-xs font-semibold text-emerald-100/85">
                      For serious protocol tracking
                    </span>
                  )}
                </div>
                <p className="mt-4 text-3xl font-semibold text-white">{tier.monthly}</p>
                <p className="mt-2 text-sm text-emerald-200/80">{tier.annualEffective}</p>
                <p className="mt-4 text-white/58">{tier.description}</p>
                <p className="mt-3 text-sm leading-6 text-white/48">{tier.detail}</p>
                <ul className="mt-5 space-y-2 text-sm text-white/58">
                  {tier.highlights.slice(0, 3).map((highlight) => (
                    <li key={highlight} className="flex gap-2">
                      <span className="mt-2 h-1.5 w-1.5 rounded-full bg-emerald-300/75" />
                      <span>{highlight}</span>
                    </li>
                  ))}
                </ul>
              </article>
            ))}
          </div>
        </section>

        <section className="border-y border-white/8 bg-black/20">
          <div className="mx-auto max-w-7xl px-5 py-12 sm:px-8 lg:py-16">
            <div className="flex items-end justify-between gap-6">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
                  FAQ
                </p>
                <h2 className="mt-4 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
                  Trust starts with explicit answers.
                </h2>
              </div>
              <Link href="/faq" className="text-sm font-semibold text-emerald-200 hover:text-white">
                View all FAQ
              </Link>
            </div>

            <div className="mt-10 grid gap-5 lg:grid-cols-2">
              {featuredFaqs.slice(0, 5).map((faq) => (
                <article key={faq.question} className="border-b border-white/8 pb-5">
                  <h3 className="text-xl text-white">{faq.question}</h3>
                  <p className="mt-3 leading-7 text-white/60">{faq.answer}</p>
                </article>
              ))}
            </div>
          </div>
        </section>

        <section className="mx-auto max-w-7xl px-5 py-16 sm:px-8">
          <div className="max-w-3xl">
            <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
              Start Here
            </p>
            <h2 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-5xl">
              Your protocol is already this complex. Your system should be too.
            </h2>
            <p className="mt-6 text-lg leading-8 text-white/62">
              Precision math. Pathway intelligence. Daily observability. A unified timeline that
              turns protocol data into pattern recognition.
            </p>

            <div className="mt-8 flex flex-wrap gap-3">
              <Link
                href="/onboarding"
                className="rounded-full bg-emerald-400 px-6 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5"
              >
                Begin My Protocol
              </Link>
              <Link
                href="/onboarding?mode=existing"
                className="rounded-full border border-white/12 px-6 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
              >
                Map My Current Stack
              </Link>
            </div>
            <p className="mt-4 text-sm text-white/50">
              Takes less than a minute to start. No setup friction.
            </p>
          </div>
        </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
