import Link from 'next/link';
import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';

const tools = [
  {
    title: 'Reconstitution Calculator',
    href: '/tools/reconstitution-calculator',
    description: 'mg powder plus mL diluent into transparent concentration math.',
  },
  {
    title: 'Volume Calculator',
    href: '/tools/volume-calculator',
    description: 'Target dose and concentration into exact draw volume.',
  },
  {
    title: 'Unit Converter',
    href: '/tools/unit-converter',
    description: 'Fast mg, mcg, and gram conversion without context switching.',
  },
];

export default function ToolsPage() {
  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main className="mx-auto max-w-6xl px-5 py-16 sm:px-8 lg:py-20">
        <section className="max-w-3xl">
          <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
            Tools
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-5xl">
            The calculators that should have existed years ago.
          </h1>
          <p className="mt-5 text-lg leading-8 text-white/62">
            No account required. The math is the first value delivery point in the funnel, so the
            tools stay public and transparent by design.
          </p>
        </section>

        <section className="mt-14 grid gap-5 md:grid-cols-3">
          {tools.map((tool) => (
            <Link
              key={tool.title}
              href={tool.href}
              className="rounded-[2rem] border border-white/10 bg-white/[0.03] p-6 transition-transform hover:-translate-y-1"
            >
              <p className="text-sm uppercase tracking-[0.2em] text-emerald-300/65">Live</p>
              <h2 className="mt-4 text-2xl font-semibold text-white">{tool.title}</h2>
              <p className="mt-3 leading-7 text-white/60">{tool.description}</p>
            </Link>
          ))}
        </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
