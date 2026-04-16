import { LandingHero } from '@/components/marketing/LandingHero';
import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import Link from 'next/link';

const valueItems = [
  'See overlap and conflicts',
  'Track what changed and when',
  'Use calculators without spreadsheets',
];

const previews = [
  {
    title: 'Overlap Intelligence',
    body: 'Shared pathways and flags.',
    stat: 'MAP',
  },
  {
    title: 'Timeline Tracking',
    body: 'Changes attached to dates.',
    stat: 'DAY 7',
  },
  {
    title: 'Reconstitution Calculator',
    body: 'Powder to concentration.',
    stat: 'mg/mL',
  },
  {
    title: 'Dose + Volume Calculator',
    body: 'Target dose to draw volume.',
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

        <section className="mx-auto max-w-7xl px-5 py-9 sm:px-8 lg:py-12">
          <div className="grid gap-3 md:grid-cols-3">
            {valueItems.map((item, index) => (
              <div
                key={item}
                className="flex min-h-20 items-center gap-4 rounded-lg border border-white/8 bg-white/[0.03] px-4 py-4"
              >
                <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg border border-emerald-300/16 bg-emerald-400/10 text-sm font-semibold text-emerald-100">
                  0{index + 1}
                </span>
                <p className="text-base font-medium leading-6 text-white">{item}</p>
              </div>
            ))}
          </div>
        </section>

        <section className="border-y border-white/8 bg-black/15 py-9 lg:py-12">
          <div className="mx-auto max-w-7xl px-5 sm:px-8">
            <div className="flex items-end justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.3em] text-emerald-300/70">
                  Core tools
                </p>
                <h2 className="mt-3 text-3xl font-semibold tracking-tight text-white">
                  Swipe. Pick what you need.
                </h2>
              </div>
              <Link href="/calculators" className="hidden text-sm font-semibold text-emerald-200 hover:text-white sm:inline">
                Open calculators
              </Link>
            </div>

            <div className="-mx-5 mt-6 flex snap-x snap-mandatory scroll-px-5 gap-3 overflow-x-auto px-5 pb-3 sm:-mx-8 sm:scroll-px-8 sm:gap-4 sm:px-8">
              {previews.map((preview) => (
                <article
                  key={preview.title}
                  className="min-h-40 w-[78vw] shrink-0 snap-start rounded-lg border border-white/10 bg-white/[0.035] p-4 shadow-[0_12px_34px_rgba(0,0,0,0.18)] sm:min-h-48 sm:w-80 sm:p-5"
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

        <section className="mx-auto max-w-7xl px-5 py-10 sm:px-8 lg:py-14">
          <div className="max-w-2xl">
            <p className="text-3xl font-semibold tracking-tight text-white sm:text-4xl">
              BioStack is not a doctor.
            </p>
            <p className="mt-3 text-lg leading-7 text-white/62">
              It keeps tracking, math, and overlap clear.
            </p>
            <p className="mt-4 text-sm font-semibold text-emerald-200/78">
              Decisions stay with you.
            </p>
          </div>
        </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
