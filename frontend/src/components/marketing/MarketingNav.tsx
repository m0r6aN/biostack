import { BioStackLogo } from '@/components/ui/BioStackLogo';
import Link from 'next/link';
import { MobileStickyCta } from './MobileStickyCta';

export function MarketingNav() {
  return (
    <>
      <header className="sticky top-0 z-30 border-b border-white/8 bg-[#0B0F14]/75 backdrop-blur-xl">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-5 py-3 sm:px-8 sm:py-4">
          <Link href="/" aria-label="BioStack home">
            <BioStackLogo variant="horizontal" theme="dark" size="md" hoverable />
          </Link>

          <nav className="hidden items-center gap-6 text-sm text-white/55 md:flex">
            <Link href="/how-it-works" className="transition-colors hover:text-white">
              How it works
            </Link>
            <Link href="/calculators" className="transition-colors hover:text-white">
              Calculators
            </Link>
            <Link href="/providers" className="transition-colors hover:text-white">
              Providers
            </Link>
            <Link href="/safety" className="transition-colors hover:text-white">
              Safety
            </Link>
          </nav>

          <div className="flex items-center gap-3">
            <Link
              href="/map"
              className="hidden rounded-full border border-white/12 px-4 py-2 text-sm text-white/75 transition-colors hover:text-white sm:inline-flex"
            >
              Map Stack
            </Link>
            <Link
              href="/start"
              className="rounded-full bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5"
            >
              Start Protocol
            </Link>
          </div>
        </div>
      </header>

      <MobileStickyCta />
    </>
  );
}
