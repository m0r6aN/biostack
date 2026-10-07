import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import Link from 'next/link';

export default function NotFound() {
  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main id="main" tabIndex={-1} className="mx-auto max-w-3xl px-5 py-20 sm:px-8">
        <p className="text-xs font-semibold uppercase tracking-[0.3em] text-emerald-300/70">404</p>
        <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-5xl">
          Page not found.
        </h1>
        <p className="mt-5 text-lg leading-8 text-white/62">
          The page you asked for does not exist — the link may be wrong, or the page may have moved.
          The library, the tools, and your protocol are one step away.
        </p>
        <div className="mt-8 flex flex-wrap gap-3">
          <Link
            href="/"
            className="rounded-lg bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-300"
          >
            Back to home
          </Link>
          <Link
            href="/knowledge"
            className="rounded-lg border border-white/12 px-5 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
          >
            Browse the library
          </Link>
          <Link
            href="/tools"
            className="rounded-lg border border-white/12 px-5 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
          >
            Open the tools
          </Link>
          <Link
            href="/start"
            className="rounded-lg border border-white/12 px-5 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
          >
            Start free
          </Link>
        </div>
      </main>

      <MarketingFooter />
    </div>
  );
}
