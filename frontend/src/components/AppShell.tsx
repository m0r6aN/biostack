'use client';

import { Sidebar } from '@/components/Sidebar';
import { cn } from '@/lib/utils';
import { usePathname } from 'next/navigation';

const APP_ROUTE_PREFIXES = [
  '/mission-control',
  '/profiles',
  '/compounds',
  '/checkins',
  '/timeline',
  '/calculators',
  '/knowledge',
  '/admin',
];

interface AppShellProps {
  children: React.ReactNode;
  authProvidersConfigured: boolean;
}

export function AppShell({ children, authProvidersConfigured }: AppShellProps) {
  const pathname = usePathname();
  const isAppRoute = APP_ROUTE_PREFIXES.some((prefix) => pathname.startsWith(prefix));

  if (!isAppRoute) {
    return <>{children}</>;
  }

  return (
    <div className="flex h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <Sidebar />
      <main className="flex-1 overflow-y-auto">
        {!authProvidersConfigured && (
          <div className="border-b border-amber-300/12 bg-amber-500/[0.05] px-5 py-3 sm:px-8">
            <div
              className={cn(
                'flex flex-wrap items-center gap-2 text-sm leading-6',
                'text-amber-100/78'
              )}
            >
              <span className="rounded-full border border-amber-300/16 bg-amber-400/10 px-2.5 py-1 text-[10px] font-semibold uppercase tracking-[0.18em] text-amber-100/78">
                Local mode
              </span>
              <p className="text-sm text-amber-100/70">
                OAuth providers are not configured yet. Profiles and protocol data stay usable on this device until sign-in is enabled.
              </p>
            </div>
          </div>
        )}
        {children}
      </main>
    </div>
  );
}
