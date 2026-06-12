'use client';

import type { Milestone } from '@/lib/types';
import { GlassCard } from '@/components/ui/GlassCard';
import { cn } from '@/lib/utils';

export function ProgressMilestonesTab({ milestones }: { milestones: Milestone[] }) {
  return (
    <GlassCard variant="base" className="p-6 sm:p-8">
      <h3 className="mb-6 text-2xl font-semibold text-white">Expected Milestones</h3>

      <div className="space-y-6">
        {milestones.map((milestone) => (
          <MilestoneStep key={milestone.order} milestone={milestone} />
        ))}
      </div>
    </GlassCard>
  );
}

function MilestoneStep({ milestone }: { milestone: Milestone }) {
  return (
    <div className="flex gap-4">
      <div
        className={cn(
          'flex h-8 w-8 shrink-0 items-center justify-center rounded-2xl text-sm font-bold',
          milestone.current
            ? 'bg-emerald-500/20 text-emerald-300 ring-1 ring-emerald-400/30'
            : 'bg-white/[0.05] text-white/50'
        )}
      >
        {milestone.order}
      </div>
      <div>
        <div className="font-semibold text-white">{milestone.period}</div>
        <div className="text-sm text-white/55">{milestone.detail}</div>
      </div>
    </div>
  );
}
