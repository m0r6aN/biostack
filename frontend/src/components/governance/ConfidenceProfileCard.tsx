import { cn } from '@/lib/utils';

interface ConfidenceProfile {
  model: number;
  epistemic: number;
  contradictionDensity: number;
  evidenceSupport: number;
  calibrationVersion: string;
}

interface ConfidenceProfileCardProps {
  profile: ConfidenceProfile;
  compact?: boolean;
  className?: string;
}

const DIMENSIONS = [
  {
    key: 'model' as const,
    label: 'Model Confidence',
    text: 'text-emerald-400',
    bar: 'bg-emerald-400',
  },
  {
    key: 'epistemic' as const,
    label: 'Epistemic Uncertainty',
    text: 'text-amber-400',
    bar: 'bg-amber-400',
  },
  {
    key: 'contradictionDensity' as const,
    label: 'Contradiction Pressure',
    text: 'text-orange-400',
    bar: 'bg-orange-400',
  },
  {
    key: 'evidenceSupport' as const,
    label: 'Evidence Support',
    text: 'text-blue-400',
    bar: 'bg-blue-400',
  },
];

export function ConfidenceProfileCard({ profile, compact = false, className }: ConfidenceProfileCardProps) {
  if (compact) {
    return (
      <div className={cn('space-y-1.5', className)}>
        <div className="flex items-center gap-1.5">
          {DIMENSIONS.map(({ key, label, bar }) => {
            const pct = Math.round(profile[key] * 100);
            return (
              <div
                key={key}
                className="flex-1 relative h-1.5 rounded-full bg-white/10 overflow-hidden"
                title={`${label}: ${pct}%`}
              >
                <div
                  className={cn('absolute inset-y-0 left-0 rounded-full transition-all duration-500', bar)}
                  style={{ width: `${pct}%` }}
                />
              </div>
            );
          })}
        </div>
        <div className="flex justify-end">
          <span className="text-[9px] text-white/30 font-mono">{profile.calibrationVersion}</span>
        </div>
      </div>
    );
  }

  return (
    <div className={cn('space-y-3', className)}>
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="text-[10px] font-bold text-white/30 uppercase tracking-widest mb-0.5">
            Confidence Profile
          </p>
          <p
            className="text-[10px] text-white/40 italic"
            title="Confidence speaks. It does not command."
          >
            Confidence speaks. It does not command.
          </p>
        </div>
        <span className="text-[9px] text-white/30 font-mono shrink-0 pt-0.5">
          {profile.calibrationVersion}
        </span>
      </div>

      <div className="space-y-2">
        {DIMENSIONS.map(({ key, label, text, bar }) => {
          const pct = Math.round(profile[key] * 100);
          return (
            <div key={key} className="flex items-center gap-2">
              <span className="text-[10px] text-white/50 w-40 shrink-0">{label}</span>
              <div className="flex-1 relative h-1.5 rounded-full bg-white/10 overflow-hidden">
                <div
                  className={cn('absolute inset-y-0 left-0 rounded-full transition-all duration-500', bar)}
                  style={{ width: `${pct}%` }}
                />
              </div>
              <span className={cn('text-[10px] font-medium w-7 text-right shrink-0', text)}>
                {pct}%
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}
