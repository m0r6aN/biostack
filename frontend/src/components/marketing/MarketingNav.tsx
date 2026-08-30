'use client';

import Link from 'next/link';
import { BioStackLogo } from '@/components/ui/BioStackLogo';
import { useAuth } from '@/lib/AuthProvider';
import { MobileStickyCta } from './MobileStickyCta';

export function MarketingNav() {
  const { user, loading, logout } = useAuth();
  const isAuthenticated = !loading && user !== null;

  return (
    <>
      <header className="sticky top-0 z-30 border-b border-white/8 bg-[#0B0F14]/75 backdrop-blur-xl">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-5 py-3 sm:px-8 sm:py-4">
          <Link href="/" aria-label="BioStack home" className="focus-visible:outline-none focus-visible:ring-2">
            <BioStackLogo variant="horizontal" theme="dark" size="md" animated hoverable />
          </Link>
          <nav className="hidden items-center gap-6 text-sm text-white/55 md:flex">
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
            <Link href="/providers" className="transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2">
              For Providers
            </Link>
            <Link href="/safety" className="transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2">
              Safety
            </Link>
          </nav>
          <div className="flex items-center gap-3">
            <Link
              href="/tools/analyzer"
              className="hidden rounded-full border border-white/12 px-4 py-2 text-sm text-white/75 transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2 sm:inline-flex"
            >
              Analyze My Stack
            </Link>
            {isAuthenticated ? (
              <>
                <button
                  type="button"
                  onClick={() => void logout()}
                  className="px-2 py-2 text-sm text-white/60 transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2"
                >
                  Sign out
                </button>
                <Link
                  href="/protocol-console"
                  className="rounded-full border border-emerald-300/30 bg-emerald-400/12 px-4 py-2 text-sm font-semibold text-emerald-100 transition-colors hover:border-emerald-200/50 hover:text-white focus-visible:outline-none focus-visible:ring-2"
                >
                  Dashboard
                </Link>
              </>
            ) : (
              <>
                <Link
                  href="/auth/signin"
                  className="px-2 py-2 text-sm text-white/60 transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2"
                >
                  Sign in
                </Link>
                <Link
                  href="/start"
                  className="rounded-full border border-emerald-300/30 bg-emerald-400/12 px-4 py-2 text-sm font-semibold text-emerald-100 transition-colors hover:border-emerald-200/50 hover:text-white focus-visible:outline-none focus-visible:ring-2"
                >
                  Start Free
                </Link>
              </>
            )}
          </div>
        </div>
      </header>
      <MobileStickyCta />
    </>
  );
}
