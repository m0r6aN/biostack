import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import Link from 'next/link';

const providerUses = [
  'Repeatable protocol structure',
  'Client-level tracking workflows',
  'Clear calculator and overlap context',
];

export default function ProvidersPage() {
  return (
    <div className="min-h-screen pb-24 md:pb-0" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main className="mx-auto max-w-6xl px-5 py-12 sm:px-8 lg:py-16">
        <section className="max-w-3xl">
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-amber-200/72">
            Retail / Provider
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-6xl">
            Structure protocols across multiple people.
          </h1>
          <p className="mt-5 max-w-2xl text-base leading-7 text-white/62 sm:text-lg">
            A reserved path for retail teams, providers, and operators who need repeatable client workflows.
          </p>
        </section>

        <section className="mt-10 grid gap-3 md:grid-cols-3">
          {providerUses.map((item) => (
            <div key={item} className="rounded-lg border border-white/10 bg-white/[0.035] p-5">
              <p className="text-base font-medium leading-6 text-white/78">{item}</p>
            </div>
          ))}
        </section>

        <div className="mt-10 flex flex-wrap gap-3">
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
            Map a Stack
          </Link>
        </div>
      </main>

      <MarketingFooter />
    </div>
  );
}
