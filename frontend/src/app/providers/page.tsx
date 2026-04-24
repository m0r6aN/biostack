import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import Link from 'next/link';

const providerUses = [
  'Repeatable protocol structure',
  'Client protocol changes and notes',
  'Check-ins organized across active clients',
];

export default function ProvidersPage() {
  return (
    <div className="min-h-screen pb-24 md:pb-0" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main className="mx-auto max-w-6xl px-5 py-12 sm:px-8 lg:py-16">
        <section className="max-w-3xl">
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-amber-200/72">
            Provider
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-6xl">
            A bio-operating system for managing protocols with longitudinal intelligence.
          </h1>
          <p className="mt-5 max-w-2xl text-base leading-7 text-white/62 sm:text-lg">
            Organize & track multi-client protocol workflows with clearer dates, notes, and check-ins.
          </p>
        </section>

        <section className="mt-10 grid gap-3 md:grid-cols-3">
          {providerUses.map((item) => (
            <div key={item} className="rounded-lg border border-white/10 bg-white/[0.035] p-5">
              <p className="text-base font-medium leading-6 text-white/78">{item}</p>
            </div>
          ))}
        </section>

        <section className="mt-8 rounded-lg border border-amber-300/15 bg-amber-400/[0.06] p-5 sm:p-6">
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-amber-200/72">
            Audit client protocols
          </p>
          <p className="mt-3 text-lg font-semibold tracking-tight text-white">
            Turn client documents into structured BioStack reviews.
          </p>
          <p className="mt-2 text-sm leading-6 text-white/60">
            Upload or scan clinic handouts, client PDFs, and coach spreadsheets. BioStack extracts the structure, scores the stack, and surfaces overlap and optimization signals.
          </p>
          <Link
            href="/tools/analyzer"
            className="mt-4 inline-flex rounded-lg border border-amber-200/20 px-4 py-2.5 text-sm font-semibold text-amber-100 transition-colors hover:border-amber-200/40 hover:text-white"
          >
            Open Protocol Analyzer →
          </Link>
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
