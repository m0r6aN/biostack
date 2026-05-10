import type { ResearchSummaryCompound } from '@/lib/research/types';
import { cn } from '@/lib/utils';
import { ReadinessBadge } from './ReadinessBadge';

interface CompoundCardProps {
  compound: ResearchSummaryCompound;
  selected: boolean;
  onClick: () => void;
}

const borderColor: Record<string, string> = {
  'research-requested': 'border-l-violet-500',
  'blocked': 'border-l-rose-500',
  'review-required': 'border-l-amber-500',
  'candidate-for-promotion': 'border-l-emerald-500',
};

export function CompoundCard({ compound, selected, onClick }: CompoundCardProps) {
  const firstBlocker = compound.promotionBlockers[0];
  const showBlocker =
    firstBlocker && compound.promotionReadiness !== 'candidate-for-promotion';

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onClick}
      onKeyDown={(e) => e.key === 'Enter' && onClick()}
      className={cn(
        'rounded-xl border-l-4 border border-white/10 px-3 py-2.5 cursor-pointer transition-all',
        borderColor[compound.promotionReadiness] ?? 'border-l-white/20',
        selected ? 'bg-blue-900/30 border-white/20' : 'bg-white/[0.03] hover:bg-white/[0.06]'
      )}
    >
      <div className="flex items-start justify-between gap-2 mb-1.5">
        <span className="text-[13px] font-semibold text-white leading-tight">
          {compound.name}
        </span>
        <ReadinessBadge readiness={compound.promotionReadiness} />
      </div>
      <div className="flex flex-wrap gap-x-3 gap-y-1">
        <MetaItem label="Class" value={compound.classification} />
        <MetaItem label="Tier" value={compound.overallEvidenceTier} />
        <MetaItem label="Complete" value={compound.completeness} />
        {compound.reviewQueueItemCount > 0 && (
          <MetaItem label="Queue" value={String(compound.reviewQueueItemCount)} />
        )}
      </div>
      {showBlocker && (
        <p className="mt-1.5 text-[10px] text-rose-400/70 italic truncate">
          {firstBlocker}
        </p>
      )}
    </div>
  );
}

function MetaItem({ label, value }: { label: string; value: string }) {
  return (
    <span className="text-[11px] text-white/40">
      {label}: <span className="text-white/70">{value}</span>
    </span>
  );
}
