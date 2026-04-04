'use client';

import { Sidebar } from '@/components/Sidebar';
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

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const isAppRoute = APP_ROUTE_PREFIXES.some((prefix) => pathname.startsWith(prefix));

  if (!isAppRoute) {
    return <>{children}</>;
  }

  return (
    <div className="flex h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <Sidebar />
      <main className="flex-1 overflow-y-auto">{children}</main>
    </div>
  );
}
