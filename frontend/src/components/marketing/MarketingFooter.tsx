import Link from 'next/link';

export function MarketingFooter() {
  return (
    <footer className="border-t border-white/8 bg-black/20">
      <div className="mx-auto flex max-w-7xl flex-col gap-5 px-5 py-10 text-sm text-white/45 sm:px-8 md:flex-row md:items-center md:justify-between">
        <p>BioStack. Tracking, math, and clarity for complex stacks.</p>

        <div className="flex flex-wrap items-center gap-4">
          <Link href="/how-it-works" className="transition-colors hover:text-white">
            How it works
          </Link>
          <Link href="/calculators" className="transition-colors hover:text-white">
            Calculators
          </Link>
          <Link href="/providers" className="transition-colors hover:text-white">
            Provider
          </Link>
          <Link href="/safety" className="transition-colors hover:text-white">
            Safety
          </Link>
          <Link href="/terms" className="transition-colors hover:text-white">
            Terms
          </Link>
          <Link href="/privacy" className="transition-colors hover:text-white">
            Privacy
          </Link>
        </div>
      </div>
    </footer>
  );
}
