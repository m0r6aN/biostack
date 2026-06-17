'use client';

import { CheckCircle2 } from 'lucide-react';
import type { ProtocolOverview, PortalPhase } from '@/lib/types';
import { StatusChip } from '@/components/ui/StatusChip';

interface ProtocolOverviewHeroProps {
  overview: ProtocolOverview;
  /** The phase currently being viewed (may differ from overview.currentPhase). */
  activePhase: PortalPhase;
  onSwitchPhase: (phaseNumber: number) => void;
}

const STATUS_LABEL: Record<ProtocolOverview['status'], string> = {
  active: 'Active',
  paused: 'Paused',
  completed: 'Completed',
  draft: 'Draft',
};

export function ProtocolOverviewHero({ overview, activePhase, onSwitchPhase }: ProtocolOverviewHeroProps) {
  return (
    <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
      <div>
        <div className="flex items-center gap-3">
          <h1 className="text-3xl font-semibold tracking-tight text-white sm:text-4xl">
            {overview.protocolName}
          </h1>
          <StatusChip
            icon={<CheckCircle2 className="h-3.5 w-3.5" />}
            className="border-emerald-400/20 bg-emerald-500/10 text-emerald-200"
          >
            {STATUS_LABEL[overview.status]}
          </StatusChip>
        </div>
        <p className="mt-1 text-lg text-white/55">{overview.objective}</p>
      </div>

      <PhaseBadge overview={overview} activePhase={activePhase} onSwitchPhase={onSwitchPhase} />
    </div>
  );
}

function PhaseBadge({ overview, activePhase, onSwitchPhase }: ProtocolOverviewHeroProps) {
  // Offer to switch to the "other" phase (next, or back to phase 1 if not on it).
  const target =
    overview.phases.find((p) => p.number !== activePhase.number) ?? overview.phases[0];

  return (
    <div className="flex items-center gap-3 rounded-2xl border border-white/[0.08] bg-white/[0.04] px-5 py-2.5 shadow-[0_8px_24px_rgba(0,0,0,0.35)] backdrop-blur-xl">
      <div>
        <div className="text-[10px] font-semibold uppercase tracking-[0.15em] text-white/40">
          Current phase
        </div>
        <div className="font-semibold text-white">
          {activePhase.label} — Week {activePhase.currentWeek} of {activePhase.totalWeeks}
        </div>
      </div>
      {target && target.number !== activePhase.number && (
        <>
          <div className="h-8 w-px bg-white/10" />
          <button
            type="button"
            onClick={() => onSwitchPhase(target.number)}
            className="whitespace-nowrap rounded-xl bg-emerald-500 px-3 py-1.5 text-xs font-semibold text-slate-950 transition-colors hover:bg-emerald-400"
          >
            View {target.label} →
          </button>
        </>
      )}
    </div>
  );
}
