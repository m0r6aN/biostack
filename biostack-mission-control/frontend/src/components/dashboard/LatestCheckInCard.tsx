'use client';

import { CheckIn } from '@/lib/types';
import { formatDate, formatWeight } from '@/lib/utils';
import { useSettings } from '@/lib/settings';
import Link from 'next/link';

interface LatestCheckInCardProps {
  checkIn: CheckIn | null;
}

export function LatestCheckInCard({ checkIn }: LatestCheckInCardProps) {
  const { settings } = useSettings();
  if (!checkIn) {
    return (
      <div className="p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90 shadow-[0_8px_24px_rgba(0,0,0,0.35)]">
        <h3 className="text-lg font-semibold text-white mb-2">Latest Check-in</h3>
        <p className="text-sm text-white/45">No check-ins recorded yet</p>
      </div>
    );
  }

  return (
    <div className="p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90 shadow-[0_8px_24px_rgba(0,0,0,0.35)]">
      <div className="flex items-start justify-between mb-4">
        <h3 className="text-lg font-semibold text-white">Latest Check-in</h3>
        <p className="text-xs text-white/35">{formatDate(checkIn.date)}</p>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="p-3 rounded-xl bg-white/[0.03] border border-white/[0.06]">
          <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-1">Weight</p>
          <p className="text-lg font-semibold text-white/90">{formatWeight(checkIn.weight, settings.weightUnit)}</p>
        </div>
        <div className="p-3 rounded-xl bg-white/[0.03] border border-white/[0.06]">
          <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-1">Energy</p>
          <p className="text-lg font-semibold text-white/90">{checkIn.energy}/10</p>
        </div>
        <div className="p-3 rounded-xl bg-white/[0.03] border border-white/[0.06]">
          <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-1">Sleep Quality</p>
          <p className="text-lg font-semibold text-white/90">{checkIn.sleepQuality}/10</p>
        </div>
        <div className="p-3 rounded-xl bg-white/[0.03] border border-white/[0.06]">
          <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-1">Recovery</p>
          <p className="text-lg font-semibold text-white/90">{checkIn.recovery}/10</p>
        </div>
      </div>

      {checkIn.notes && (
        <div className="mt-4 p-3 rounded-xl bg-white/[0.03] border border-white/[0.06]">
          <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-1">Notes</p>
          <p className="text-sm text-white/65">{checkIn.notes}</p>
        </div>
      )}

      <Link
        href="/checkins"
        className="mt-4 inline-block text-emerald-400 hover:text-emerald-300 text-sm transition-all duration-150"
      >
        View all check-ins →
      </Link>
    </div>
  );
}
