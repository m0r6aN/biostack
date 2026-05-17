'use client';

import { useProfile } from '@/lib/context';
import { useEffect, useRef, useState } from 'react';
import { useSettings } from '@/lib/settings';
import { formatWeight } from '@/lib/utils';

export function ProfileSwitcher() {
  const { currentProfileId, setCurrentProfileId, profiles } = useProfile();
  const [isOpen, setIsOpen] = useState(false);
  const [toast, setToast] = useState<string | null>(null);
  const toastTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const currentProfile = profiles.find(p => p.id === currentProfileId);
  const { settings } = useSettings();

  useEffect(() => {
    return () => {
      if (toastTimer.current) {
        clearTimeout(toastTimer.current);
      }
    };
  }, []);

  const handleSelect = (profileId: string, displayName: string) => {
    const isSwitch = profileId !== currentProfileId;
    setCurrentProfileId(profileId);
    setIsOpen(false);
    if (isSwitch) {
      if (toastTimer.current) {
        clearTimeout(toastTimer.current);
      }
      setToast(`Switched to ${displayName}`);
      toastTimer.current = setTimeout(() => setToast(null), 2600);
    }
  };

  return (
    <div className="relative">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-2 border border-white/10 bg-white/[0.04] rounded-xl px-3 py-2 hover:bg-white/[0.08] transition-all"
      >
        <span className="text-sm font-medium text-white/80">
          {currentProfile?.displayName || 'Select Profile'}
        </span>
        <span className="text-xs text-white/45">▼</span>
      </button>

      {isOpen && (
        <div className="absolute top-full mt-2 left-0 bg-[#121923] border border-white/10 rounded-xl shadow-[0_8px_40px_rgba(0,0,0,0.6)] backdrop-blur-xl z-50 min-w-48">
          <div className="py-1">
            {profiles.map((profile) => (
              <button
                key={profile.id}
                onClick={() => handleSelect(profile.id, profile.displayName)}
                className={`w-full text-left px-4 py-2 text-sm transition-colors ${
                  profile.id === currentProfileId
                    ? 'bg-emerald-500/10 text-emerald-300'
                    : 'text-white/70 hover:bg-white/[0.06]'
                }`}
              >
                <div className="font-medium">{profile.displayName}</div>
                <div className="text-xs text-white/45">{formatWeight(profile.weight, settings.weightUnit)}</div>
              </button>
            ))}
          </div>
        </div>
      )}

      {toast && (
        <div
          role="status"
          aria-live="polite"
          data-testid="profile-switch-toast"
          className="absolute top-full mt-2 right-0 z-50 rounded-xl border border-emerald-400/20 bg-[#121923]/95 px-3 py-2 text-xs font-medium text-emerald-100/90 shadow-[0_8px_40px_rgba(0,0,0,0.5)] backdrop-blur-xl"
        >
          {toast}
        </div>
      )}
    </div>
  );
}
