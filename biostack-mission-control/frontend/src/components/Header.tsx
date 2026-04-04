'use client';

import { useProfile } from '@/lib/context';
import { useSettings } from '@/lib/settings';
import { cn, formatWeight } from '@/lib/utils';

interface HeaderProps {
  title: string;
  subtitle?: string;
  /** Page-specific action buttons (e.g. "New Profile", "Record Check-in") */
  actions?: React.ReactNode;
  className?: string;
}

/**
 * Premium system command header — shared across all pages.
 *
 * Structure:
 *   [title + subtitle]  ···  [LOCAL chip]  [profile chip]  [actions?]
 *
 * Always sticky, glass-surfaced, brand-aligned.
 */
export function Header({ title, subtitle, actions, className }: HeaderProps) {
  const { currentProfileId, profiles, isSidebarOpen, setSidebarOpen } = useProfile();
  const { settings } = useSettings();
  const currentProfile = profiles.find(p => p.id === currentProfileId);

  return (
    <header
      className={cn(
        'sticky top-0 z-20 border-b border-white/[0.05] bg-[#0B0F14]/88 backdrop-blur-xl',
        className
      )}
    >
      {/* Brand accent line — subtly marks the system boundary */}
      <div className="h-px bg-gradient-to-r from-transparent via-emerald-500/22 to-transparent" />

      <div className="px-5 sm:px-8 h-[60px] flex items-center justify-between gap-6">

        <div className="flex items-center gap-4 min-w-0">
          {/* Mobile hamburger — only visible on small screens */}
          <button
            onClick={() => setSidebarOpen(!isSidebarOpen)}
            className="flex lg:hidden flex-shrink-0 items-center justify-center w-9 h-9 rounded-xl border border-white/10 bg-white/[0.03] text-white/40 hover:text-white transition-all active:scale-95"
            aria-label="Toggle navigation"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
              {isSidebarOpen ? (
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
              ) : (
                <path strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 12h16M4 18h16" />
              )}
            </svg>
          </button>

          {/* ── Left: Page identity ─────────────────────────────────────── */}
          <div className="flex items-baseline gap-3 min-w-0">
            <h1 className="text-[17px] font-semibold tracking-tight text-white leading-none truncate">
              {title}
            </h1>
            {subtitle && (
              <span className="hidden sm:block text-xs text-white/28 font-medium tracking-wide whitespace-nowrap">
                {subtitle}
              </span>
            )}
          </div>
        </div>

        {/* ── Right: System status + profile + actions ────────────────── */}
        <div className="flex items-center gap-2.5 shrink-0">

          {/* LOCAL status chip — always visible */}
          <div className="flex items-center gap-1.5 px-2.5 py-1.5 rounded-full border border-white/[0.07] bg-white/[0.03]">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 shadow-[0_0_5px_rgba(34,197,94,0.55)]" />
            <span className="text-[10px] font-semibold text-white/38 tracking-[0.14em]">LOCAL</span>
          </div>

          {/* Profile chip — shown when a profile is active and no custom actions */}
          {currentProfile && !actions && (
            <div className="flex items-center gap-2 px-3 py-1.5 rounded-xl border border-white/[0.07] bg-white/[0.03]">
              {/* Avatar ring */}
              <span className="flex items-center justify-center w-5 h-5 rounded-full border border-emerald-400/30 bg-emerald-500/15 shrink-0">
                <span className="text-[9px] font-bold text-emerald-300 leading-none">
                  {currentProfile.displayName[0].toUpperCase()}
                </span>
              </span>
              <span className="text-[13px] font-medium text-white/65 leading-none">
                {currentProfile.displayName}
              </span>
              <span className="text-[11px] text-white/28 leading-none hidden md:block">
                {currentProfile.age ? `${currentProfile.age}y · ` : ''}
                {formatWeight(currentProfile.weight, settings.weightUnit)}
              </span>
            </div>
          )}

          {/* Page actions */}
          {actions && (
            <div className="flex items-center gap-2">
              {actions}
            </div>
          )}
        </div>

      </div>
    </header>
  );
}
