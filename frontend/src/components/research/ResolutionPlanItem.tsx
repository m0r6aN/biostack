import type { ReviewResolutionPlanItem } from '@/lib/research/types';
import { ReadinessBadge } from './ReadinessBadge';

export function ResolutionPlanItem({ item }: { item: ReviewResolutionPlanItem }) {
  return (
    <div className="rounded-xl border border-white/[0.08] bg-white/[0.025] px-3 py-2.5 flex flex-col gap-2">
      <div className="flex items-center justify-between gap-2">
        <span className="text-[10px] font-bold text-amber-400">Suggested task: {item.resolutionType}</span>
        <ReadinessBadge readiness={item.readiness as 'blocked' | 'review-required' | 'candidate-for-promotion'} />
      </div>
      <p className="text-[11px] text-white/70">Why it exists: {item.issue}</p>
      <div className="rounded-lg bg-blue-900/20 border border-blue-400/20 px-2.5 py-2">
        <p className="text-[11px] text-blue-300">Recommended remediation — not automatic: {item.recommendedAction}</p>
      </div>
      {item.relatedQualityFlags.length > 0 && (
        <div className="flex flex-wrap gap-1.5">
          {item.relatedQualityFlags.map(f => (
            <span key={f} className="text-[9px] px-2 py-0.5 rounded-full bg-white/[0.05] border border-white/10 text-white/40">{f}</span>
          ))}
        </div>
      )}
    </div>
  );
}
