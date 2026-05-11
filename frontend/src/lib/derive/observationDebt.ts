import type { ProtocolConsolePayload, CheckIn, CompoundRecord, GoalDefinition } from '@/lib/types';

export type ObservationDebtType =
  | 'missing-first-checkin'
  | 'expected-next-event-due'
  | 'cadence-gap'
  | 'metric-missing-for-goal'
  | 'review-not-completed';

export interface ObservationDebtItem {
  type: ObservationDebtType;
  priority: number; // 1 = highest
  title: string;
  reason: string;
  impact: string;
  ctaLabel: string;
  ctaHref: string;
  dismissable: boolean;
}

const MS_PER_DAY = 86_400_000;

function daysSince(dateStr: string | null | undefined): number {
  if (!dateStr) return Infinity;
  return (Date.now() - new Date(dateStr).getTime()) / MS_PER_DAY;
}

export function deriveObservationDebt(
  payload: ProtocolConsolePayload | null,
  checkIns: CheckIn[],
  compounds: CompoundRecord[],
  goals: GoalDefinition[],
): ObservationDebtItem[] {
  if (!payload) return [];

  const items: ObservationDebtItem[] = [];
  const { latestCheckInSignal, sequenceExpectationSnapshot, latestReviewSummary, observationSignals } = payload;
  const activeCompounds = compounds.filter((c) => c.status === 'Active');

  // 1. Missing first check-in (highest priority)
  if (!latestCheckInSignal?.checkInId && payload.activeRun) {
    items.push({
      type: 'missing-first-checkin',
      priority: 1,
      title: 'First observation not logged',
      reason: 'A run is active but no check-ins have been recorded. The signal window cannot open until the first observation is logged.',
      impact: 'Unlocks signal quality tracking and sequence expectation.',
      ctaLabel: 'Log First Observation',
      ctaHref: '/checkins/new',
      dismissable: false,
    });
  }

  // 2. Expected next event due
  const expectedNext = sequenceExpectationSnapshot?.expectedNextEvent;
  if (expectedNext && (sequenceExpectationSnapshot?.currentStatus?.state === 'late' || sequenceExpectationSnapshot?.currentStatus?.state === 'pending')) {
    items.push({
      type: 'expected-next-event-due',
      priority: 2,
      title: `Expected event: ${expectedNext.description}`,
      reason: `Based on historical patterns, a "${expectedNext.eventType}" was expected ${expectedNext.timingWindow}.`,
      impact: 'Logging this observation improves sequence confidence and drift detection.',
      ctaLabel: 'Log Observation',
      ctaHref: '/checkins/new',
      dismissable: true,
    });
  }

  // 3. Cadence gap (>= 3 days since last check-in)
  const lastCheckInDate = checkIns.length > 0 ? checkIns[0].date : null;
  const gapDays = daysSince(lastCheckInDate);
  if (gapDays >= 3 && payload.activeRun) {
    items.push({
      type: 'cadence-gap',
      priority: 3,
      title: `${Math.floor(gapDays)}-day observation gap`,
      reason: `No check-in has been logged in ${Math.floor(gapDays)} days. Gaps reduce drift detection accuracy.`,
      impact: 'Consistent observations prevent false drift signals.',
      ctaLabel: 'Log Observation',
      ctaHref: '/checkins/new',
      dismissable: true,
    });
  }

  // 4. Metric missing for active goal
  const goalMetricMap: Record<string, string[]> = {
    'sleep': ['sleepQuality'],
    'energy': ['energy'],
    'focus': ['focus', 'thoughtClarity'],
    'recovery': ['recovery'],
    'pain': ['jointPain'],
  };
  const recentCheckIn = checkIns[0];
  if (recentCheckIn && activeCompounds.length > 0) {
    for (const goal of goals.filter((g) => g.isActive)) {
      const category = goal.category?.toLowerCase() ?? '';
      const targetFields = goalMetricMap[category] ?? [];
      const allMissing = targetFields.length > 0 && targetFields.every((f) => recentCheckIn[f as keyof CheckIn] == null);
      if (allMissing) {
        items.push({
          type: 'metric-missing-for-goal',
          priority: 4,
          title: `No ${goal.category} metric in last check-in`,
          reason: `Your active goal "${goal.name}" tracks ${goal.category}, but the latest check-in has no data for this metric.`,
          impact: `Adding this metric improves goal-specific signal quality.`,
          ctaLabel: 'Update Last Check-in',
          ctaHref: '/checkins',
          dismissable: true,
        });
        break; // one per session
      }
    }
  }

  // 5. Review not completed
  if (latestReviewSummary) {
    items.push({
      type: 'review-not-completed',
      priority: 5,
      title: 'Earned review awaiting completion',
      reason: `Protocol "${latestReviewSummary.lineageName}" has enough data for a review (${latestReviewSummary.checkInCount} check-ins, ${latestReviewSummary.runCount} runs).`,
      impact: 'Completing the review unlocks the next run phase and improves pattern recognition.',
      ctaLabel: 'Start Review',
      ctaHref: '/protocols',
      dismissable: false,
    });
  }

  // Also surface observation signals from the API
  for (const sig of (observationSignals ?? []).filter((s) => s.severity === 'high')) {
    if (!items.find((i) => i.type === 'cadence-gap')) {
      items.push({
        type: 'cadence-gap',
        priority: 3,
        title: sig.detail,
        reason: `Signal type: ${sig.type}. Severity: ${sig.severity}.`,
        impact: 'Addressing this signal improves overall clarity score.',
        ctaLabel: 'Log Observation',
        ctaHref: '/checkins/new',
        dismissable: true,
      });
    }
  }

  return items.sort((a, b) => a.priority - b.priority);
}
