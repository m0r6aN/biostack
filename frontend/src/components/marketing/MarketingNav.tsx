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
            <Link href="/tools" className="transition-colors hover:text-white">
              Tools
            </Link>
            <Link href="/providers" className="transition-colors hover:text-white">
              Provider
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
              className="rounded-full border border-emerald-300/30 bg-emerald-400/12 px-4 py-2 text-sm font-semibold text-emerald-100 transition-colors hover:border-emerald-200/50 hover:text-white"
            >
              Start free
            </Link>
          </div>
        </div>
      </header>

      <MobileStickyCta />
    </>
  );
}
