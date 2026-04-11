'use client';

import { useSettings } from '@/lib/settings';
import { cn } from '@/lib/utils';

interface WeightUnitToggleProps {
  className?: string;
}

/**
 * Inline unit toggle for weight input fields only.
 * Should appear adjacent to weight inputs on Profile and Check-in forms.
 * NOT for use in global navigation or shell components.
 */
export function WeightUnitToggle({ className }: WeightUnitToggleProps) {
  const { settings, setWeightUnit } = useSettings();

  return (
    <div
      className={cn(
        'flex items-center rounded-lg border border-white/[0.08] bg-white/[0.03] p-0.5',
        className
      )}
    >
      <button
        type="button"
        onClick={() => setWeightUnit('metric')}
        className={cn(
          'px-2.5 py-1 text-xs font-medium rounded-md transition-all duration-150',
          settings.weightUnit === 'metric'
            ? 'bg-emerald-500/20 text-emerald-300'
            : 'text-white/35 hover:text-white/60'
        )}
      >
        kg
      </button>
      <button
        type="button"
        onClick={() => setWeightUnit('imperial')}
        className={cn(
          'px-2.5 py-1 text-xs font-medium rounded-md transition-all duration-150',
          settings.weightUnit === 'imperial'
            ? 'bg-emerald-500/20 text-emerald-300'
            : 'text-white/35 hover:text-white/60'
        )}
      >
        lbs
      </button>
    </div>
  );
}
