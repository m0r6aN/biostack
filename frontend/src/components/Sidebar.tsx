'use client';

import { BioStackLogo } from '@/components/ui/BioStackLogo';
import { useAuth } from '@/lib/AuthProvider';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { usePathname } from 'next/navigation';

// ─── Custom SVG icons ─────────────────────────────────────────────────────────

function IconProtocolConsole() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.35" strokeLinecap="round" strokeLinejoin="round" className="w-4 h-4">
      <rect x="2" y="2" width="5" height="5" rx="1.25" />
      <rect x="9" y="2" width="5" height="5" rx="1.25" />
      <rect x="2" y="9" width="5" height="5" rx="1.25" />
      <rect x="9" y="9" width="5" height="5" rx="1.25" />
    </svg>
  );
}

function IconProfiles() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.35" strokeLinecap="round" strokeLinejoin="round" className="w-4 h-4">
      <circle cx="8" cy="5.5" r="2.5" />
      <path d="M2.5 14c0-3.038 2.462-5.5 5.5-5.5s5.5 2.462 5.5 5.5" />
    </svg>
  );
}

function IconCompounds() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.35" strokeLinecap="round" strokeLinejoin="round" className="w-4 h-4">
      <path d="M8 2 L13.5 5.25 L13.5 10.75 L8 14 L2.5 10.75 L2.5 5.25 Z" />
      <circle cx="8" cy="8" r="1.5" />
    </svg>
  );
}

function IconCheckins() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.35" strokeLinecap="round" strokeLinejoin="round" className="w-4 h-4">
      <polyline points="0.5,8 3.5,8 5,5.5 7,10.5 9,2.5 11,8 12.5,8 15.5,8" />
    </svg>
  );
}

function IconProtocols() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.35" strokeLinecap="round" strokeLinejoin="round" className="w-4 h-4">
      <path d="M3 2.5h10v11H3z" />
      <path d="M5.5 5h5" />
      <path d="M5.5 8h5" />
      <path d="M5.5 11h3" />
    </svg>
  );
}

function IconTimeline() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.35" strokeLinecap="round" strokeLinejoin="round" className="w-4 h-4">
      <line x1="4.5" y1="2" x2="4.5" y2="14" />
      <circle cx="4.5" cy="4"  r="1.5" />
      <circle cx="4.5" cy="8"  r="1.5" />
      <circle cx="4.5" cy="12" r="1.5" />
      <line x1="7.5" y1="4"  x2="14"   y2="4" />
      <line x1="7.5" y1="8"  x2="12.5" y2="8" />
      <line x1="7.5" y1="12" x2="11"   y2="12" />
    </svg>
  );
}

function IconCalculators() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.35" strokeLinecap="round" className="w-4 h-4">
      <circle cx="8" cy="8" r="4.5" />
      <line x1="8" y1="0.5" x2="8" y2="3.5" />
      <line x1="8" y1="12.5" x2="8" y2="15.5" />
      <line x1="0.5" y1="8" x2="3.5" y2="8" />
      <line x1="12.5" y1="8" x2="15.5" y2="8" />
      <circle cx="8" cy="8" r="1.25" fill="currentColor" stroke="none" />
    </svg>
  );
}

function IconKnowledge() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.35" strokeLinecap="round" strokeLinejoin="round" className="w-4 h-4">
      <rect x="2.5" y="2" width="11" height="12" rx="1.5" />
      <line x1="5.5" y1="5.5" x2="10.5" y2="5.5" />
      <line x1="5.5" y1="8"   x2="10.5" y2="8" />
      <line x1="5.5" y1="10.5" x2="8.5" y2="10.5" />
    </svg>
  );
}

function IconAdmin() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.35" strokeLinecap="round" strokeLinejoin="round" className="w-4 h-4">
      <rect x="3" y="7" width="10" height="7" rx="1" />
      <path d="M5 7V5a3 3 0 0 1 6 0v2" />
    </svg>
  );
}

// ─── Nav items ────────────────────────────────────────────────────────────────

const navItems = [
  { label: 'Protocol Console', href: '/protocol-console', icon: <IconProtocolConsole />, adminOnly: false },
  { label: 'Profiles',    href: '/profiles',    icon: <IconProfiles />,    adminOnly: false },
  { label: 'Compounds',   href: '/compounds',   icon: <IconCompounds />,   adminOnly: false },
  { label: 'Protocols',   href: '/protocols',   icon: <IconProtocols />,   adminOnly: false },
  { label: 'Check-ins',   href: '/checkins',    icon: <IconCheckins />,    adminOnly: false },
  { label: 'Timeline',    href: '/timeline',    icon: <IconTimeline />,    adminOnly: false },
  { label: 'Calculators', href: '/calculators', icon: <IconCalculators />, adminOnly: false },
  { label: 'Knowledge',   href: '/knowledge',   icon: <IconKnowledge />,   adminOnly: false },
  // Admin nav item — only rendered when role === 1
  { label: 'Admin',       href: '/admin',       icon: <IconAdmin />,       adminOnly: true  },
];

// ─── Component ────────────────────────────────────────────────────────────────

import { useProfile } from '@/lib/context';

export function Sidebar() {
  const pathname = usePathname();
  const { isSidebarOpen, setSidebarOpen } = useProfile();
  const { user, logout } = useAuth();

  const isAdmin = user?.role === 1;

  const visibleNavItems = navItems.filter(
    (item) => !item.adminOnly || isAdmin
  );

  return (
    <>
      {/* Mobile Overlay Backdrop */}
      {isSidebarOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/60 backdrop-blur-sm lg:hidden transition-opacity duration-300"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-50 w-72 lg:w-64 h-screen flex flex-col border-r border-white/5 bg-[#0B0F14]/95 lg:bg-[#0B0F14]/80 backdrop-blur-2xl shrink-0 transition-transform duration-300 ease-in-out lg:static lg:translate-x-0',
          isSidebarOpen ? 'translate-x-0' : '-translate-x-full'
        )}
      >
        {/* ── Brand zone ────────────────────────────────────────────────────── */}
        <div className="px-5 py-5 border-b border-white/[0.05] flex items-center justify-between">
          <Link href="/protocol-console" onClick={() => setSidebarOpen(false)} aria-label="BioStack Protocol Console home">
            <BioStackLogo variant="horizontal" theme="dark" size="md" hoverable />
          </Link>

          {/* Mobile close button */}
          <button
            onClick={() => setSidebarOpen(false)}
            className="lg:hidden p-2 rounded-xl hover:bg-white/5 text-white/40 transition-colors"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
        <div className="px-6 py-2">
          <p className="text-[10px] font-bold text-white/10 uppercase tracking-[0.3em] pl-0.5">
            Protocol Console
          </p>
        </div>

        {/* ── Navigation zone ───────────────────────────────────────────────── */}
        <nav className="flex-1 overflow-y-auto py-4 px-4 min-h-0">
          <div className="space-y-1">
            {visibleNavItems.map((item) => {
              const isActive =
                item.href === '/protocol-console'
                  ? pathname.startsWith('/protocol-console') || pathname.startsWith('/mission-control')
                  : pathname.startsWith(item.href);

              return (
                <Link
                  key={item.href}
                  href={item.href}
                  onClick={() => setSidebarOpen(false)}
                  className={cn(
                    'group relative flex items-center gap-3.5 px-3 py-3 rounded-2xl text-[13px] font-semibold transition-all duration-200',
                    isActive
                      ? 'text-emerald-400 bg-emerald-400/5 shadow-[inset_0_0_12px_rgba(52,211,153,0.03)]'
                      : 'text-white/40 hover:text-white/80 hover:bg-white/[0.03]'
                  )}
                >
                  {/* Active edge illumination */}
                  {isActive && (
                    <span className="absolute left-[-4px] top-1/2 -translate-y-1/2 w-1.5 h-6 rounded-full bg-emerald-500 shadow-[0_0_15px_rgba(16,185,129,0.8)]" />
                  )}

                  {/* Icon container */}
                  <span
                    className={cn(
                      'flex items-center justify-center w-8 h-8 rounded-xl border transition-all duration-200 shrink-0',
                      isActive
                        ? 'bg-emerald-500/10 border-emerald-400/20 text-emerald-400 shadow-[0_0_20px_rgba(16,185,129,0.1)]'
                        : 'bg-white/[0.02] border-white/5 text-white/30 group-hover:bg-white/[0.05] group-hover:border-white/10 group-hover:text-white/60'
                    )}
                  >
                    {item.icon}
                  </span>

                  {/* Label */}
                  <span className="tracking-tight">{item.label}</span>
                </Link>
              );
            })}
          </div>
        </nav>

        {/* ── User zone ──────────────────────────────────────────────────────── */}
        <div className="px-4 pb-4">
          {user && (
            <div className="p-3 rounded-2xl border border-white/5 bg-white/[0.02] flex items-center gap-3 mb-3">
              {/* Avatar */}
              {user.avatarUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={user.avatarUrl}
                  alt={user.displayName || user.email || 'User'}
                  className="w-8 h-8 rounded-full border border-white/10 shrink-0"
                />
              ) : (
                <div className="w-8 h-8 rounded-full bg-emerald-500/20 border border-emerald-400/20 flex items-center justify-center shrink-0">
                  <span className="text-emerald-400 text-xs font-bold">
                    {(user.displayName || user.email || 'U')[0].toUpperCase()}
                  </span>
                </div>
              )}
              <div className="min-w-0 flex-1">
                <p className="text-[11px] font-semibold text-white/70 leading-none truncate">
                  {user.displayName || user.email}
                </p>
                {isAdmin && (
                  <span className="inline-block mt-1 text-[9px] font-bold tracking-widest text-amber-400/80 uppercase">
                    Admin
                  </span>
                )}
              </div>
              <button
                onClick={() => void logout()}
                title="Sign out"
                className="shrink-0 p-1.5 rounded-xl hover:bg-white/5 text-white/25 hover:text-white/50 transition-colors"
              >
                <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.35" strokeLinecap="round" className="w-4 h-4">
                  <path d="M6 2H3a1 1 0 0 0-1 1v10a1 1 0 0 0 1 1h3" />
                  <polyline points="10 11 14 8 10 5" />
                  <line x1="14" y1="8" x2="6" y2="8" />
                </svg>
              </button>
            </div>
          )}

          {/* ── System zone ─────────────────────────────────────────────────── */}
          <div className="p-4 rounded-2xl border border-white/5 bg-gradient-to-b from-white/[0.03] to-transparent">
            <div className="flex items-center gap-3">
              {/* Pulsing status indicator */}
              <div className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-40"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
              </div>

              <div className="min-w-0">
                <p className="text-[11px] font-bold text-white/50 leading-none">SYSTEM ACTIVE</p>
                <p className="text-[10px] text-white/20 mt-1 font-medium tracking-wide">
                  ESTABLISHED · 128-BIT
                </p>
              </div>
            </div>
          </div>
        </div>
      </aside>
    </>
  );
}
