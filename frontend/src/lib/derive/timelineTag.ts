import type { ProtocolReviewTimelineEvent, ProtocolSequenceExpectationSnapshot, ProtocolDriftSnapshot } from '@/lib/types';
import type { TimelineEventTag } from '@/styles/tokens';

export function deriveTag(
  event: ProtocolReviewTimelineEvent,
  sequence: ProtocolSequenceExpectationSnapshot | null | undefined,
  drift: ProtocolDriftSnapshot | null | undefined,
): TimelineEventTag | null {
  const detail = event.detail.toLowerCase();
  const status = sequence?.currentStatus?.state;
  const driftState = drift?.driftState;

  if (detail.includes('regime shift') || driftState === 'regime_shift') return 'regime-shift';
  if (detail.includes('sequence break') || detail.includes('sequence diverging')) return 'diverging';
  if (detail.includes('outside typical timing') || detail.includes('later than usual') || status === 'late') return 'late';
  if (detail.includes('matches prior pattern') || detail.includes('within common sequence window') || status === 'aligned') return 'aligned';
  if (detail.includes('usual next event') || status === 'pending') return 'expected-pending';

  return null;
}
