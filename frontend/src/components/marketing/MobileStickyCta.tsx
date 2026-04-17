'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useEffect, useState } from 'react';

export function MobileStickyCta() {
  const [isVisible, setIsVisible] = useState(false);
  const pathname = usePathname();
  const isStartRoute = pathname === '/start';

  useEffect(() => {
    function updateVisibility() {
      setIsVisible(window.scrollY > 220);
    }

    updateVisibility();
    window.addEventListener('scroll', updateVisibility, { passive: true });

    return () => window.removeEventListener('scroll', updateVisibility);
  }, []);

  return (
    <nav
      aria-label="Primary actions"
      className={`fixed inset-x-0 bottom-0 z-40 border-t border-white/10 bg-[#0B0F14]/92 px-4 pb-[calc(0.75rem+env(safe-area-inset-bottom))] pt-3 backdrop-blur-xl transition duration-200 md:hidden ${
        isVisible ? 'translate-y-0 opacity-100' : 'pointer-events-none translate-y-full opacity-0'
      }`}
    >
      <div className={`grid gap-2 ${isStartRoute ? 'grid-cols-2' : 'grid-cols-3'}`}>
        {!isStartRoute && (
          <Link
            href="/start"
            className="flex min-h-12 items-center justify-center rounded-lg bg-emerald-400 px-2 text-sm font-semibold text-slate-950"
          >
            Start
          </Link>
        )}
        <Link
          href="/map"
          className="flex min-h-12 items-center justify-center rounded-lg border border-sky-300/16 bg-sky-400/[0.06] px-2 text-sm font-semibold text-white"
        >
          Map
        </Link>
        <Link
          href="/providers"
          className="flex min-h-12 items-center justify-center rounded-lg border border-amber-300/16 bg-amber-300/[0.06] px-2 text-sm font-semibold text-white"
        >
          Provider
        </Link>
      </div>
    </nav>
  );
}
