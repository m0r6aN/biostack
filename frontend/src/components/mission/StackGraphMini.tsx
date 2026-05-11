'use client';

import Link from 'next/link';
import { StackGraph } from '@/components/protocol/StackGraph';
import { cn } from '@/lib/utils';
import { track } from '@/lib/telemetry';
import type { InteractionIntelligence, CompoundRecord } from '@/lib/types';

interface StackGraphMiniProps {
  intelligence: InteractionIntelligence | null;
  compounds: CompoundRecord[];
  activeProtocolId?: string | null;
  className?: string;
}

export function StackGraphMini({ intelligence, compounds, activeProtocolId, className }: StackGraphMiniProps) {
  const activeCount = compounds.filter((c) => c.status === 'Active').length;
  const topFindingCount = intelligence?.topFindings?.length ?? 0;

  const labHref = activeProtocolId
    ? `/protocols/${activeProtocolId}?tab=graph`
    : '/protocols';

  return (
    <div className={cn('rounded-3xl border border-white/8 bg-[#0F141B] overflow-hidden', className)}>
      {/* Header */}
      <div className="flex items-center justify-between px-4 pt-4 pb-2">
        <div>
          <p className="text-[10px] font-bold text-white/20 uppercase tracking-widest">Stack Graph</p>
          <p className="text-xs text-white/50 mt-0.5">{activeCount} compound{activeCount !== 1 ? 's' : ''}{topFindingCount > 0 ? ` · ${topFindingCount} finding${topFindingCount !== 1 ? 's' : ''}` : ''}</p>
        </div>
        <Link
          href={labHref}
          onClick={() => track({ name: 'surface_view', surface: 'stack-graph-full' })}
          className="text-[11px] font-semibold text-emerald-400/70 hover:text-emerald-300 transition-colors flex items-center gap-1"
        >
          Open Lab
          <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
            <path strokeLinecap="round" strokeLinejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
          </svg>
        </Link>
      </div>

      {/* Graph (mini mode — no controls, no filters) */}
      <div className="h-56 w-full">
        <StackGraph
          intelligence={intelligence}
          compounds={compounds}
          variant="mini"
        />
      </div>
    </div>
  );
}
