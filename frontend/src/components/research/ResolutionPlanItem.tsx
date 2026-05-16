import type { ResearchTaskQueueItem, ReviewResolutionPlanItem } from '@/lib/research/types';
import { ReadinessBadge } from './ReadinessBadge';

export function ResolutionPlanItem({ item, queuedTask, pendingDecisionId = null, onToggleNextRound }: { item: ReviewResolutionPlanItem; queuedTask?: ResearchTaskQueueItem | null; pendingDecisionId?: string | null; onToggleNextRound?: () => void }) {
  const isAutoQueuedForNextRound = queuedTask?.taskType === 'expand-review-sources'
    && (queuedTask.remediationPlanItemIds ?? []).includes(item.itemId);
  const isQueuedForNextRound = isAutoQueuedForNextRound || Boolean(pendingDecisionId);
  const canToggle = !isAutoQueuedForNextRound && Boolean(onToggleNextRound);

  return (
    <div className="rounded-xl border border-white/[0.08] bg-white/[0.025] px-3 py-2.5 flex flex-col gap-2">
      <div className="flex items-center justify-between gap-2">
        <span className="text-[10px] font-bold text-amber-400">Suggested task: {item.resolutionType}</span>
        <ReadinessBadge readiness={item.readiness as 'blocked' | 'review-required' | 'candidate-for-promotion'} />
      </div>
      <p className="text-[11px] text-white/70">Why it exists: {item.issue}</p>
      <div className="rounded-lg bg-blue-900/20 border border-blue-400/20 px-2.5 py-2">
        <p className="text-[11px] text-blue-300">Recommended remediation: {item.recommendedAction}</p>
      </div>
      <label className={`flex items-start gap-2 rounded-lg border px-2.5 py-2 text-[11px] ${isQueuedForNextRound ? 'border-emerald-400/25 bg-emerald-500/10 text-emerald-100/80' : 'border-white/10 bg-white/[0.025] text-white/45'}`}>
        <input
          type="checkbox"
          checked={isQueuedForNextRound}
          disabled={!canToggle}
          onChange={onToggleNextRound}
          aria-label={`Apply ${item.itemId} in next research round`}
          className="mt-0.5 accent-emerald-500"
        />
        <span className="flex flex-col gap-1">
          <span className="font-semibold">
            {isAutoQueuedForNextRound ? 'Auto-queued for next agent round' : isQueuedForNextRound ? 'Queued in decision batch' : 'Apply in next agent round'}
          </span>
          <span className="leading-5 opacity-75">
            {isAutoQueuedForNextRound
              ? `The generated task ${queuedTask?.taskId} already carries this remediation item and its original recommended action forward.`
              : isQueuedForNextRound
                ? `Decision ${pendingDecisionId} will ask the next worker round to apply this remediation item.`
                : 'Click to add a scoped request-changes decision for the next worker round.'}
          </span>
        </span>
      </label>
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
