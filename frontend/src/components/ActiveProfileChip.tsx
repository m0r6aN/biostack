'use client';

import { useProfile } from '@/lib/context';

interface ActiveProfileChipProps {
  className?: string;
}

export function ActiveProfileChip({ className }: ActiveProfileChipProps) {
  const { currentProfileId, profiles } = useProfile();
  const currentProfile = profiles.find((profile) => profile.id === currentProfileId);

  if (!currentProfile) {
    return null;
  }

  return (
    <div
      data-testid="active-profile-chip"
      aria-label={`Active profile: ${currentProfile.displayName}`}
      className={
        'inline-flex items-center gap-2 rounded-full border border-emerald-400/20 bg-emerald-500/[0.06] px-3 py-1 text-xs font-medium text-emerald-100/80 ' +
        (className ?? '')
      }
    >
      <span
        aria-hidden="true"
        className="flex items-center justify-center w-4 h-4 rounded-full border border-emerald-400/30 bg-emerald-500/15"
      >
        <span className="text-[9px] font-bold text-emerald-300 leading-none">
          {currentProfile.displayName.charAt(0).toUpperCase()}
        </span>
      </span>
      <span className="text-[11px] uppercase tracking-[0.14em] text-emerald-200/60">Viewing</span>
      <span className="text-[13px] font-semibold text-white/82">{currentProfile.displayName}</span>
    </div>
  );
}
