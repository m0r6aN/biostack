import type { ReactNode } from 'react';

/**
 * 06 §A blocker: the only component that may render trial or reported doses. Closed by
 * default, labelled "Study details: trial conditions, not guidance".
 */
export const STUDY_DETAILS_LABEL = 'Study details: trial conditions, not guidance';
export const STUDY_DETAILS_TOP_LINE =
  "These are the conditions researchers used in a controlled trial with medical supervision. They aren't instructions.";

export function StudyDetailsFold({ children }: { children: ReactNode }) {
  return (
    <details data-testid="study-details-fold" className="rounded-xl border border-white/10 bg-white/[0.02] p-3 group">
      <summary className="cursor-pointer text-sm font-medium text-white/75 focus-visible:outline-2 focus-visible:outline-cyan-300">
        {STUDY_DETAILS_LABEL}
      </summary>
      <div className="mt-3 space-y-2">
        <p className="text-xs text-white/55">{STUDY_DETAILS_TOP_LINE}</p>
        {children}
      </div>
    </details>
  );
}
