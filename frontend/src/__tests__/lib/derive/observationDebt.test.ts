import { describe, it, expect } from 'vitest';
import { deriveObservationDebt } from '@/lib/derive/observationDebt';
import type { ProtocolConsolePayload } from '@/lib/types';

const ACTIVE_RUN = { id: 'run-1', protocolId: 'p-1', personId: 'p1', protocolName: 'Test', protocolVersion: 1, startedAtUtc: '2026-05-01T00:00:00Z', endedAtUtc: null, status: 'active' as const, notes: '' };

const EMPTY_PAYLOAD: ProtocolConsolePayload = {
  activeRun: ACTIVE_RUN,
  latestClosedRun: null,
  latestReviewSummary: null,
  recentEvolution: null,
  latestCheckInSignal: { checkInId: null, protocolRunId: null, date: null, cue: '', attachedCheckInCount: 0, hasObservationGap: false },
  observationSignals: [],
  patternSnapshot: null,
  driftSnapshot: null,
  sequenceExpectationSnapshot: null,
  cohesionTimeline: [],
};

describe('deriveObservationDebt', () => {
  it('returns missing-first-checkin as top priority when no check-ins', () => {
    const items = deriveObservationDebt(EMPTY_PAYLOAD, [], [], []);
    expect(items[0]?.type).toBe('missing-first-checkin');
  });

  it('returns empty list when payload is null', () => {
    const items = deriveObservationDebt(null, [], [], []);
    expect(items).toHaveLength(0);
  });

  it('returns review-not-completed when review summary exists', () => {
    const payload = {
      ...EMPTY_PAYLOAD,
      latestCheckInSignal: { checkInId: 'ci-1', protocolRunId: 'run-1', date: '2026-05-09', cue: '', attachedCheckInCount: 7, hasObservationGap: false },
      latestReviewSummary: { protocolId: 'p-1', lineageRootProtocolId: 'p-1', lineageName: 'Test', cue: '', signalType: '', versionCount: 1, runCount: 1, checkInCount: 7 },
    };
    const items = deriveObservationDebt(payload, [], [], []);
    expect(items.some((i) => i.type === 'review-not-completed')).toBe(true);
  });

  it('detects cadence gap after 3 days', () => {
    const oldDate = new Date(Date.now() - 4 * 86_400_000).toISOString().split('T')[0];
    const checkIns = [{ id: 'ci-1', personId: 'p1', protocolRunId: 'run-1', date: oldDate, weight: 80, sleepQuality: 7, energy: 7, appetite: 7, recovery: 7 } as any];
    const payload = {
      ...EMPTY_PAYLOAD,
      latestCheckInSignal: { checkInId: 'ci-1', protocolRunId: 'run-1', date: oldDate, cue: '', attachedCheckInCount: 1, hasObservationGap: true },
    };
    const items = deriveObservationDebt(payload, checkIns, [], []);
    expect(items.some((i) => i.type === 'cadence-gap')).toBe(true);
  });

  it('items are sorted by priority', () => {
    const oldDate = new Date(Date.now() - 4 * 86_400_000).toISOString().split('T')[0];
    const payload = {
      ...EMPTY_PAYLOAD,
      latestReviewSummary: { protocolId: 'p-1', lineageRootProtocolId: 'p-1', lineageName: 'Test', cue: '', signalType: '', versionCount: 1, runCount: 1, checkInCount: 7 },
      latestCheckInSignal: { checkInId: 'ci-1', protocolRunId: 'run-1', date: oldDate, cue: '', attachedCheckInCount: 1, hasObservationGap: true },
    };
    const checkIns = [{ id: 'ci-1', personId: 'p1', protocolRunId: 'run-1', date: oldDate, weight: 80, sleepQuality: 7, energy: 7, appetite: 7, recovery: 7 } as any];
    const items = deriveObservationDebt(payload, checkIns, [], []);
    for (let i = 1; i < items.length; i++) {
      expect(items[i].priority).toBeGreaterThanOrEqual(items[i - 1].priority);
    }
  });
});
