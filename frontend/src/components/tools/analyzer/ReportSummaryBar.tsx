'use client';

import type { ProtocolAnalyzerResult } from '@/lib/types';
import { GOAL_CATEGORIES } from '@/lib/goals';
import { sourceTypeLabel } from './analyzerView';

export function ReportSummaryBar({
  result,
  primaryCategory,
  onEdit,
}: {
  result: ProtocolAnalyzerResult;
  primaryCategory: string | null;
  onEdit: () => void;
}) {
  const goalLabel = primaryCategory
    ? GOAL_CATEGORIES.find((category) => category.key === primaryCategory)?.label ?? 'Goal set'
    : 'No goal selected';

  return (
    <div className="sticky top-16 z-10 flex flex-wrap items-center justify-between gap-3 rounded-lg border border-white/10 bg-[#0B1118]/95 px-4 py-3 backdrop-blur">
      <p className="text-sm text-white/72">
        <span className="font-semibold text-white">{sourceTypeLabel(result)}</span>
        <span className="text-white/40"> · </span>
        {result.protocol.length} compound{result.protocol.length === 1 ? '' : 's'}
        <span className="text-white/40"> · </span>
        {goalLabel}
      </p>
      <button
        type="button"
        onClick={onEdit}
        className="rounded-lg border border-white/10 px-3 py-1.5 text-sm font-semibold text-white/72 transition-colors hover:border-white/20 hover:text-white"
      >
        Edit
      </button>
    </div>
  );
}
