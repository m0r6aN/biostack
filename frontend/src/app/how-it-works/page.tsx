import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import Link from 'next/link';

const sections = [
  {
    eyebrow: 'Current State',
    title: 'Your protocol is more complex than your system for tracking it.',
    body: [
      'You have compounds, notes, papers, and intent. The hard part is keeping them together under pressure.',
      'BioStack closes the gap between a spreadsheet and a clinical system without pretending to prescribe.',
    ],
  },
  {
    eyebrow: 'Overlap',
    title: 'You may not be getting more benefit just because you added more.',
    body: [
      'BPC-157 and TB-500 can share a tissue-repair focus and still make sense together.',
      'BioStack shows what overlaps, what may complement the goal, and what deserves a closer look.',
    ],
  },
  {
    eyebrow: 'Guidance Clarity',
    title: 'When guidance conflicts, BioStack gives you structure.',
    body: [
      'One source says 4mg weekly. Another says start at 0.25mg multiple times per week.',
      'BioStack shows typical ranges, evidence tiers, and response tracking without choosing sides.',
    ],
  },
  {
    eyebrow: 'Context',
    title: 'General ranges are only the start.',
    body: [
      'Age, weight, goals, and tracking history turn generic guidance into useful context.',
      'BioStack gets more useful as your protocol history becomes clearer.',
    ],
  },
];

const provides = [
  'Compound tracking with precision structure',
  'Pathway overlap intelligence across your active protocol',
  'Reconstitution, volume, and unit conversion math',
  'Evidence-tiered knowledge base entries',
  'Daily check-ins that turn subjectivity into signal',
  'Unified timeline correlation across compounds, phases, and check-ins',
];

export default function HowItWorksPage() {
  return (
    <div className="min-h-screen pb-24 md:pb-0" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main>
        <section className="mx-auto max-w-7xl px-5 py-12 sm:px-8 lg:py-16">
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-emerald-300/70">
            How it works
          </p>
          <h1 className="mt-4 max-w-4xl text-4xl font-semibold tracking-tight text-white sm:text-6xl">
            The deeper explanation lives here.
          </h1>
          <p className="mt-5 max-w-2xl text-lg leading-8 text-white/62">
            BioStack gives complex protocols a place for tracking, calculators, overlap checks, and timeline context.
          </p>
        </section>

        <section className="border-y border-white/8 bg-black/15">
          <div className="mx-auto grid max-w-7xl gap-4 px-5 py-10 sm:px-8 md:grid-cols-2 lg:grid-cols-3">
            {provides.map((item) => (
              <div key={item} className="rounded-lg border border-white/8 bg-white/[0.03] p-4">
                <p className="text-base leading-6 text-white/76">{item}</p>
              </div>
            ))}
          </div>
        </section>

        <section className="mx-auto grid max-w-7xl gap-5 px-5 py-10 sm:px-8 lg:py-14">
          {sections.map((section) => (
            <article key={section.eyebrow} className="grid gap-5 border-b border-white/8 pb-8 lg:grid-cols-[0.8fr_1.2fr]">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.28em] text-emerald-300/68">
                  {section.eyebrow}
                </p>
                <h2 className="mt-3 text-2xl font-semibold tracking-tight text-white sm:text-3xl">
                  {section.title}
                </h2>
              </div>
              <div className="space-y-4 text-base leading-7 text-white/60">
                {section.body.map((paragraph) => (
                  <p key={paragraph}>{paragraph}</p>
                ))}
              </div>
            </article>
          ))}
        </section>

        <section className="mx-auto max-w-7xl px-5 pb-14 sm:px-8">
          <div className="flex flex-wrap gap-3">
            <Link
              href="/start"
              className="rounded-lg bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5"
            >
              Start a Protocol
            </Link>
            <Link
              href="/map"
              className="rounded-lg border border-white/12 px-5 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
            >
              Map My Stack
            </Link>
          </div>
        </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
