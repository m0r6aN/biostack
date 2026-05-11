import { cn } from '@/lib/utils';

interface CognitiveHeat {
  value: number;
  band: 'nominal' | 'elevated' | 'high' | 'critical';
  throttlingActive: boolean;
  message?: string;
}

interface CognitiveHeatGaugeProps {
  heat: CognitiveHeat;
  compact?: boolean;
  className?: string;
}

const BAND_CONFIG: Record<CognitiveHeat['band'], {
  label: string;
  text: string;
  bar: string;
  pillBg: string;
  pillBorder: string;
}> = {
  nominal: {
    label: 'Nominal',
    text: 'text-emerald-400',
    bar: 'bg-emerald-400',
    pillBg: 'bg-emerald-400/10',
    pillBorder: 'border-emerald-400/20',
  },
  elevated: {
    label: 'Elevated',
    text: 'text-yellow-400',
    bar: 'bg-yellow-400',
    pillBg: 'bg-yellow-400/10',
    pillBorder: 'border-yellow-400/20',
  },
  high: {
    label: 'High',
    text: 'text-orange-400',
    bar: 'bg-orange-400',
    pillBg: 'bg-orange-400/10',
    pillBorder: 'border-orange-400/20',
  },
  critical: {
    label: 'Critical',
    text: 'text-red-400',
    bar: 'bg-red-400',
    pillBg: 'bg-red-400/10',
    pillBorder: 'border-red-400/20',
  },
};

export function CognitiveHeatGauge({ heat, compact = false, className }: CognitiveHeatGaugeProps) {
  const { label, text, bar, pillBg, pillBorder } = BAND_CONFIG[heat.band];
  const pct = Math.round(heat.value * 100);
  const bandLabel = `${label} · ${pct}%`;

  const gauge = (
    <div className="relative h-1.5 w-full rounded-full bg-white/10 overflow-hidden">
      <div
        className={cn('absolute inset-y-0 left-0 rounded-full transition-all duration-500', bar)}
        style={{ width: `${pct}%` }}
      />
    </div>
  );

  if (compact) {
    return (
      <div className={cn('flex items-center gap-2', className)}>
        <div className="flex-1">{gauge}</div>
        <span className={cn('text-[10px] font-semibold shrink-0', text)}>{bandLabel}</span>
      </div>
    );
  }

  return (
    <div className={cn('space-y-2', className)}>
      <div className="flex items-center justify-between">
        <span className="text-[10px] font-bold text-white/30 uppercase tracking-widest">
          Cognitive Heat
        </span>
        <span className={cn('text-[10px] font-semibold', text)}>{bandLabel}</span>
      </div>

      {gauge}

      {heat.throttlingActive && (
        <span
          className={cn(
            'inline-flex items-center rounded border px-1.5 py-0.5 text-[10px] font-medium',
            text,
            pillBg,
            pillBorder,
          )}
        >
          Branching throttled
        </span>
      )}

      {heat.band === 'critical' && (
        <p className="text-[10px] text-red-400/80 leading-relaxed">
          Collective cognition critically strained — non-essential perspectives collapsed
        </p>
      )}

      {heat.message && (
        <p className="text-[10px] text-white/40 leading-relaxed">{heat.message}</p>
      )}
    </div>
  );
}
