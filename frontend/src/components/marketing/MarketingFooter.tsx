import Link from 'next/link';

export function MarketingFooter() {
  return (
    <footer className="border-t border-white/8 bg-black/20">
      <div className="mx-auto flex max-w-7xl flex-col gap-5 px-5 py-10 text-sm text-white/60 sm:px-8 md:flex-row md:items-center md:justify-between">
        <p>BioStack. What the research says, graded by evidence strength.</p>

        <div className="flex flex-wrap items-center gap-4">
          <Link href="/how-it-works" className="transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2">
            How it works
          </Link>
          <Link href="/tools" className="transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2">
            Tools
          </Link>
          <Link href="/knowledge" className="transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2">
            Compounds & Evidence
          </Link>
          <Link href="/pricing" className="transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2">
            Pricing
          </Link>
          <Link href="/start" className="transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2">
            Start Free
          </Link>
          <Link href="/providers" className="transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2">
            For Providers
          </Link>
          <Link href="/safety" className="transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2">
            Safety
          </Link>
          <Link href="/terms" className="transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2">
            Terms
          </Link>
          <Link href="/privacy" className="transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2">
            Privacy
          </Link>
        </div>
      </div>
    </footer>
  );
}
