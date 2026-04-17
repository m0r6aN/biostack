import { LandingHero } from '@/components/marketing/LandingHero';
import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import Link from 'next/link';

const valueItems = [
  {
    label: 'Structure',
    title: 'Every compound gets a date and role.',
    body: 'Keep peptides, supplements, and protocol phases tied to a timeline instead of scattered notes.',
  },
  {
    label: 'Overlap',
    title: 'Shared pathways are easier to spot.',
    body: 'Surface compatibility context and active-window overlap before your stack becomes hard to read.',
  },
  {
    label: 'Follow-up',
    title: 'Check-ins stay connected to changes.',
    body: 'Link energy, appetite, recovery, weight, and notes back to the protocol moment that created them.',
  },
];

const previews = [
  {
    title: 'Overlap Intelligence',
    body: 'Shared pathways, active windows, and flags in a focused map.',
    stat: 'MAP',
  },
  {
    title: 'Timeline Tracking',
    body: 'Compound starts, edits, check-ins, and reviews attached to dates.',
    stat: 'DAY 7',
  },
  {
    title: 'Reconstitution Calculator',
    body: 'Convert powder and diluent into concentration without spreadsheet drift.',
    stat: 'mg/mL',
  },
  {
    title: 'Dose + Volume Calculator',
    body: 'Translate target dose into draw volume with visible math.',
    stat: 'mL',
  },
];

export default function HomePage() {
  const softwareSchema = {
    '@context': 'https://schema.org',
    '@type': 'SoftwareApplication',
    name: 'BioStack',
    applicationCategory: 'HealthApplication',
    operatingSystem: 'Web',
    description: 'Tracking, calculator, and stack mapping infrastructure for compound protocols.',
  };

  return (
    <div className="min-h-screen pb-24 md:pb-0" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main>
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(softwareSchema) }}
        />
        <LandingHero />

        <section id="value" className="scroll-mt-20 mx-auto max-w-7xl px-5 py-9 sm:px-8 lg:py-12">
          <div className="grid gap-8 lg:grid-cols-[0.72fr_1.28fr] lg:items-start">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-emerald-300/70">
                What you get
              </p>
              <h2 className="mt-3 max-w-md text-3xl font-semibold tracking-tight text-white sm:text-4xl">
                A clearer operating surface for complex stacks.
              </h2>
              <p className="mt-4 max-w-md text-base leading-7 text-white/58">
                Start free while you map your stack. No payment required to begin.
              </p>
            </div>

            <div className="grid gap-3 md:grid-cols-3 lg:gap-4">
              {valueItems.map((item) => (
              <div
                key={item.title}
                className="min-h-52 rounded-lg border border-white/8 bg-white/[0.03] p-4 transition duration-200 hover:-translate-y-0.5 hover:border-white/16 hover:bg-white/[0.045] sm:p-5"
              >
                <span className="flex h-11 w-11 items-center justify-center rounded-lg border border-emerald-300/16 bg-emerald-400/10 text-xs font-semibold uppercase tracking-[0.12em] text-emerald-100">
                  {item.label}
                </span>
                <h3 className="mt-5 text-xl font-semibold leading-6 text-white">{item.title}</h3>
                <p className="mt-3 text-sm leading-6 text-white/58">{item.body}</p>
              </div>
              ))}
            </div>
          </div>
        </section>

        <section id="tools" className="scroll-mt-20 border-y border-white/8 bg-black/15 py-9 lg:py-12">
          <div className="mx-auto max-w-7xl px-5 sm:px-8">
            <div className="flex items-end justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.3em] text-emerald-300/70">
                  Core tools
                </p>
                <h2 className="mt-3 text-3xl font-semibold tracking-tight text-white">
                  Track, compare, and calculate clearly.
                </h2>
              </div>
              <Link href="/calculators" className="hidden text-sm font-semibold text-emerald-200 hover:text-white sm:inline">
                Open calculators
              </Link>
            </div>

            <div className="-mx-5 mt-6 flex snap-x snap-mandatory scroll-px-5 gap-3 overflow-x-auto px-5 pb-3 sm:-mx-8 sm:scroll-px-8 sm:gap-4 sm:px-8 md:mx-0 md:grid md:grid-cols-2 md:overflow-visible md:px-0 md:pb-0 lg:grid-cols-4">
              {previews.map((preview) => (
                <article
                  key={preview.title}
                  className="min-h-44 w-[74vw] shrink-0 snap-start rounded-lg border border-white/10 bg-white/[0.035] p-4 shadow-[0_12px_34px_rgba(0,0,0,0.18)] transition duration-200 hover:-translate-y-0.5 hover:border-white/16 hover:bg-white/[0.048] sm:min-h-48 sm:w-80 sm:p-5 md:w-auto"
                >
                  <div className="flex h-12 w-12 items-center justify-center rounded-lg border border-emerald-300/16 bg-emerald-400/10 text-xs font-semibold text-emerald-100 sm:h-16 sm:w-16 sm:text-sm">
                    {preview.stat}
                  </div>
                  <h3 className="mt-4 text-xl font-semibold text-white sm:mt-5">{preview.title}</h3>
                  <p className="mt-2 text-sm leading-6 text-white/58">{preview.body}</p>
                </article>
              ))}
            </div>
          </div>
        </section>

        <section className="mx-auto max-w-7xl px-5 py-8 sm:px-8 lg:py-10">
          <div className="rounded-lg border border-white/8 bg-white/[0.025] px-4 py-4 sm:flex sm:items-center sm:justify-between sm:gap-6 sm:px-5">
            <p className="text-base font-semibold tracking-tight text-white sm:text-lg">
              BioStack is not a doctor.
            </p>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-white/56 sm:mt-0">
              BioStack organizes tracking, math, overlap context, and evidence references. It does not prescribe, diagnose, recommend compounds, or replace qualified medical care.
            </p>
          </div>
        </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
