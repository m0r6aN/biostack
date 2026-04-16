import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import Link from 'next/link';

const calculators = [
  {
    title: 'Reconstitution Calculator',
    body: 'Powder plus diluent to concentration.',
    href: '/tools/reconstitution-calculator',
  },
  {
    title: 'Dose + Volume Calculator',
    body: 'Target dose to exact draw volume.',
    href: '/tools/volume-calculator',
  },
  {
    title: 'Unit Converter',
    body: 'mg, mcg, and g without spreadsheet drift.',
    href: '/tools/unit-converter',
  },
];

export default function CalculatorsPage() {
  return (
    <div className="min-h-screen pb-24 md:pb-0" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main className="mx-auto max-w-6xl px-5 py-12 sm:px-8 lg:py-16">
        <section className="max-w-3xl">
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-emerald-300/70">
            Calculators
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-6xl">
            Math without the spreadsheet.
          </h1>
          <p className="mt-5 max-w-xl text-base leading-6 text-white/62">
            Public tools for concentration, dose volume, and unit conversion.
          </p>
        </section>

        <section className="mt-10 grid gap-4 md:grid-cols-3">
          {calculators.map((calculator) => (
            <Link
              key={calculator.href}
              href={calculator.href}
              className="flex min-h-40 flex-col justify-between rounded-lg border border-white/10 bg-white/[0.035] p-5 transition duration-200 hover:-translate-y-0.5 hover:border-emerald-300/24 hover:bg-emerald-400/[0.07]"
            >
              <span>
                <span className="block text-xl font-semibold text-white">{calculator.title}</span>
                <span className="mt-2 block text-sm leading-6 text-white/58">{calculator.body}</span>
              </span>
              <span className="mt-6 text-sm font-semibold text-emerald-200">Open tool</span>
            </Link>
          ))}
        </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
