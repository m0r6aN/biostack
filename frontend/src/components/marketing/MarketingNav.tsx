import { BioStackLogo } from '@/components/ui/BioStackLogo';
import Link from 'next/link';

export function MarketingNav() {
  return (
    <header className="sticky top-0 z-30 border-b border-white/8 bg-[#0B0F14]/75 backdrop-blur-xl">
      <div className="mx-auto flex max-w-7xl items-center justify-between px-5 py-4 sm:px-8">
        <Link href="/" aria-label="BioStack home">
          <BioStackLogo variant="horizontal" theme="dark" size="md" hoverable />
        </Link>

        <nav className="hidden items-center gap-6 text-sm text-white/55 md:flex">
          <Link href="/pricing" className="transition-colors hover:text-white">
            Pricing
          </Link>
          <Link href="/faq" className="transition-colors hover:text-white">
            FAQ
          </Link>
          <Link href="/tools" className="transition-colors hover:text-white">
            Tools
          </Link>
        </nav>

        <div className="flex items-center gap-3">
          <Link
            href="/tools"
            className="hidden rounded-full border border-white/12 px-4 py-2 text-sm text-white/75 transition-colors hover:text-white sm:inline-flex"
          >
            Explore Calculators
          </Link>
          <Link
            href="/onboarding"
            className="rounded-full bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5"
          >
            <span className="sm:hidden">Build Protocol</span>
            <span className="hidden sm:inline">Build My Protocol</span>
          </Link>
        </div>
      </div>
    </header>
  );
}
