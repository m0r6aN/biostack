'use client';

import { Check, Dumbbell, Utensils } from 'lucide-react';
import type { DietFramework } from '@/lib/types';
import { GlassCard } from '@/components/ui/GlassCard';
import { cn } from '@/lib/utils';

export function DietLifestyleTab({ diet }: { diet: DietFramework }) {
  return (
    <GlassCard variant="base" className="p-6 sm:p-8">
      <h3 className="text-2xl font-semibold text-white">{diet.title}</h3>
      <p className="mt-1 text-white/55">{diet.summary}</p>

      <div className="mt-6 grid gap-8 md:grid-cols-2">
        <div>
          <h4 className="mb-3 flex items-center gap-2 font-semibold text-white">
            <Utensils className="h-4 w-4 text-emerald-400" />
            <span>Daily Nutrition Targets</span>
          </h4>
          <ul className="space-y-2 text-sm">
            {diet.targets.map((target) => (
              <li key={target.label} className="flex justify-between gap-4">
                <span className="text-white/55">{target.label}</span>
                <span className={cn('font-semibold', target.caution ? 'text-red-300' : 'text-white')}>
                  {target.value}
                </span>
              </li>
            ))}
          </ul>

          <div className="mt-6">
            <h4 className="mb-2 font-semibold text-white">Why This Matters</h4>
            <p className="text-sm leading-6 text-white/55">{diet.rationale}</p>
          </div>
        </div>

        <div>
          <h4 className="mb-3 flex items-center gap-2 font-semibold text-white">
            <Dumbbell className="h-4 w-4 text-violet-400" />
            <span>Lifestyle Requirements</span>
          </h4>
          <div className="space-y-3 text-sm">
            {diet.lifestyle.map((requirement) => (
              <div key={requirement} className="flex gap-3">
                <Check className="mt-0.5 h-4 w-4 shrink-0 text-emerald-400" />
                <div className="text-white/75">{requirement}</div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </GlassCard>
  );
}
