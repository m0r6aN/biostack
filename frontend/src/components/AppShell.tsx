'use client';

import { Sidebar } from '@/components/Sidebar';
import { useAuth } from '@/lib/AuthProvider';
import { isPublicRoutePath } from '@/lib/productContract';
import { usePathname } from 'next/navigation';

const APP_ROUTE_PREFIXES = [
  '/protocol-console',
  '/mission-control',
  '/my-protocol',
  '/profiles',
  '/compounds',
  '/protocols',
  '/checkins',
  '/timeline',
  '/knowledge',
  '/billing',
  '/governance',
  '/receipts',
  '/admin',
];

interface AppShellProps {
  children: React.ReactNode;
}

export function AppShell({ children }: AppShellProps) {
  const pathname = usePathname();
  const { user, loading } = useAuth();
  const isProtectedRoute = !isPublicRoutePath(pathname);
  const isKnowledgeRoute = pathname.startsWith('/knowledge');
  const isAppRoute =
    APP_ROUTE_PREFIXES.some((prefix) => pathname.startsWith(prefix)) &&
    (!isKnowledgeRoute || Boolean(user));

  if (isProtectedRoute && (loading || !user)) {
    return (
      <main className="flex min-h-screen items-center justify-center px-6 text-center text-sm text-white/55">
        <p role="status">{loading ? 'Checking your session…' : 'Redirecting to sign in…'}</p>
      </main>
    );
  }

  if (!isAppRoute) {
    return <>{children}</>;
  }

  return (
    <div className="flex h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <Sidebar />
      <main className="flex-1 overflow-y-auto">
        {children}
        <p
          aria-label="App-wide disclaimer"
          className="sticky bottom-0 border-t border-white/[0.04] bg-[#0B0F14]/80 px-5 py-1.5 text-center text-[11px] leading-tight text-white/35 backdrop-blur-sm"
        >
          Educational and observational only. Not medical advice.
        </p>
      </main>
    </div>
  );
}
