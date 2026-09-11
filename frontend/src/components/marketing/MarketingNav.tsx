'use client';

import Link from 'next/link';
import { useEffect, useRef } from 'react';
import { BioStackLogo } from '@/components/ui/BioStackLogo';
import { useAuth } from '@/lib/AuthProvider';
import { MobileStickyCta } from './MobileStickyCta';

export function MarketingNav() {
  const { user, loading, logout } = useAuth();
  const isAuthenticated = !loading && user !== null;
  const headerRef = useRef<HTMLElement>(null);

  // Keep in-page anchor targets (e.g. the #main skip link) clear of this sticky header,
  // tracking its live height across wraps/font loads and releasing the offset on unmount.
  useEffect(() => {
    const header = headerRef.current;
    if (!header) return;
    const root = document.documentElement;
    const applyOffset = () => {
      root.style.setProperty('scroll-padding-top', `${Math.ceil(header.getBoundingClientRect().height)}px`);
    };
    applyOffset();
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(applyOffset);
    observer?.observe(header);
    return () => {
      observer?.disconnect();
      root.style.removeProperty('scroll-padding-top');
    };
  }, []);

  return (
    <>
      <header ref={headerRef} className="sticky top-0 z-30 border-b border-white/8 bg-[#0B0F14]/75 backdrop-blur-xl">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-x-4 gap-y-2 px-5 py-3 sm:px-8 sm:py-4">
          <Link href="/" aria-label="BioStack home" className="shrink-0 focus-visible:outline-none focus-visible:ring-2">
            <BioStackLogo variant="horizontal" theme="dark" size="md" animated hoverable />
          </Link>
          <nav className="hidden min-w-0 grow basis-0 flex-wrap items-center justify-end gap-x-5 gap-y-1 whitespace-nowrap text-sm text-white/55 md:flex xl:grow-0 xl:basis-auto xl:gap-x-6">
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
          <div className="ml-auto flex shrink-0 items-center gap-3 whitespace-nowrap md:ml-0 md:w-full md:justify-end xl:w-auto">
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
