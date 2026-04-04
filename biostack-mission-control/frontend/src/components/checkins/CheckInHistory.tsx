'use client';

import { useSettings } from '@/lib/settings';
import { CheckIn } from '@/lib/types';
import { formatDate, formatWeight } from '@/lib/utils';

interface CheckInHistoryProps {
  checkIns: CheckIn[];
}

export function CheckInHistory({ checkIns }: CheckInHistoryProps) {
  const { settings } = useSettings();
  const sorted = [...checkIns].sort((a, b) =>
    new Date(b.date).getTime() - new Date(a.date).getTime()
  );

  return (
    <div className="space-y-4">
      {sorted.map((checkIn) => (
        <div
          key={checkIn.id}
          className="group p-5 rounded-2xl border border-white/[0.06] bg-[#121923]/50 hover:bg-[#121923]/80 hover:border-emerald-500/20 transition-all duration-300"
        >
          <div className="flex items-start justify-between mb-4">
            <div>
              <h4 className="font-bold text-white text-lg">{formatDate(checkIn.date)}</h4>
              <div className="flex items-center gap-2 mt-1">
                 <span className="text-xs font-medium px-2 py-0.5 rounded-full bg-white/5 border border-white/10 text-white/50 uppercase tracking-wider">
                  Weight: {formatWeight(checkIn.weight, settings.weightUnit)}
                </span>
                {checkIn.mood && (
                  <span className={`text-xs font-medium px-2 py-0.5 rounded-full uppercase tracking-wider ${
                    checkIn.mood === 'excellent' ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' :
                    checkIn.mood === 'positive' ? 'bg-blue-500/10 text-blue-400 border border-blue-500/20' :
                    checkIn.mood === 'negative' ? 'bg-red-500/10 text-red-400 border border-red-500/20' :
                    'bg-white/5 text-white/40 border border-white/10'
                  }`}>
                    {checkIn.mood}
                  </span>
                )}
              </div>
            </div>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-6 gap-3 mb-4">
            <MetricBadge label="Sleep" value={checkIn.sleepQuality} />
            <MetricBadge label="Energy" value={checkIn.energy} />
            <MetricBadge label="Recovery" value={checkIn.recovery} />
            <MetricBadge label="Appetite" value={checkIn.appetite} />
            
            {checkIn.focus !== undefined && <MetricBadge label="Focus" value={checkIn.focus} />}
            {checkIn.thoughtClarity !== undefined && <MetricBadge label="Clarity" value={checkIn.thoughtClarity} />}
            {checkIn.skinQuality !== undefined && <MetricBadge label="Skin" value={checkIn.skinQuality} />}
            {checkIn.digestiveHealth !== undefined && <MetricBadge label="Digestion" value={checkIn.digestiveHealth} />}
            {checkIn.strength !== undefined && <MetricBadge label="Strength" value={checkIn.strength} />}
            {checkIn.endurance !== undefined && <MetricBadge label="Endurance" value={checkIn.endurance} />}
            {checkIn.jointPain !== undefined && <MetricBadge label="Pain" value={checkIn.jointPain} invert />}
            {checkIn.eyesight !== undefined && <MetricBadge label="Vision" value={checkIn.eyesight} />}
          </div>

          {(checkIn.sideEffects || checkIn.notes) && (
            <div className="space-y-3 mt-4 pt-4 border-t border-white/[0.05]">
              {checkIn.sideEffects && (
                <div className="p-3 rounded-xl bg-red-500/5 border border-red-500/10">
                  <p className="text-[10px] font-black uppercase text-red-400 mb-1 tracking-widest">Side Effects</p>
                  <p className="text-sm text-red-200/70 leading-relaxed italic">{checkIn.sideEffects}</p>
                </div>
              )}
              {checkIn.notes && (
                <div>
                  <p className="text-[10px] font-black uppercase text-emerald-400/40 mb-1 tracking-widest">Narrative</p>
                  <p className="text-sm text-white/60 leading-relaxed">{checkIn.notes}</p>
                </div>
              )}
            </div>
          )}

          {checkIn.taggedPhotoUrls && checkIn.taggedPhotoUrls.length > 0 && (
            <div className="mt-4 pt-4 border-t border-white/[0.05]">
              <p className="text-[10px] font-black uppercase text-white/20 mb-2 tracking-widest">Visual Evidence</p>
              <div className="flex flex-wrap gap-2">
                {checkIn.taggedPhotoUrls.map((url, i) => (
                  <div key={i} className="relative w-16 h-16 rounded-lg overflow-hidden border border-white/[0.08] hover:border-emerald-500/50 transition-all cursor-zoom-in">
                    <img src={url} alt={`Check-in track ${i}`} className="w-full h-full object-cover" />
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      ))}
    </div>
  );
}

function MetricBadge({ label, value, invert = false }: { label: string; value: number; invert?: boolean }) {
  const isGood = invert ? value <= 3 : value >= 7;
  const isBad = invert ? value >= 7 : value <= 3;
  
  return (
    <div className="p-3 bg-white/[0.02] border border-white/[0.06] rounded-xl flex flex-col items-center justify-center transition-colors hover:bg-white/[0.04]">
      <p className="text-[10px] font-black text-white/20 uppercase tracking-wider mb-1">{label}</p>
      <p className={`text-sm font-black ${isGood ? 'text-emerald-400' : isBad ? 'text-red-400' : 'text-amber-400'}`}>
        {value}
      </p>
    </div>
  );
}
