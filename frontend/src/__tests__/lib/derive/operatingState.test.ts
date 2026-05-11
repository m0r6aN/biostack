import { describe, it, expect } from 'vitest';
import { deriveOperatingState } from '@/lib/derive/operatingState';
import type { ProtocolConsolePayload } from '@/lib/types';

function makeRun(status: 'active' | 'completed' | 'abandoned') {
  return { id: 'run-1', protocolId: 'p-1', personId: 'person-1', protocolName: 'Test', protocolVersion: 1, startedAtUtc: '2026-05-01T00:00:00Z', endedAtUtc: null, status, notes: '' };
}

const BASE_SIGNAL = { checkInId: null, protocolRunId: null, date: null, cue: '', attachedCheckInCount: 0, hasObservationGap: false };

const EMPTY_PAYLOAD: ProtocolConsolePayload = {
  activeRun: null,
  latestClosedRun: null,
  latestReviewSummary: null,
  recentEvolution: null,
  latestCheckInSignal: BASE_SIGNAL,
  observationSignals: [],
  patternSnapshot: null,
  driftSnapshot: null,
  sequenceExpectationSnapshot: null,
  cohesionTimeline: [],
};

describe('deriveOperatingState', () => {
  it('returns no-active-run when payload is null', () => {
    const result = deriveOperatingState(null, 0);
    expect(result.state).toBe('no-active-run');
  });

  it('returns no-active-run when there is no active or closed run', () => {
    const result = deriveOperatingState(EMPTY_PAYLOAD, 0);
    expect(result.state).toBe('no-active-run');
  });

  it('returns stable-baseline when there is a closed run but no active run', () => {
    const payload = { ...EMPTY_PAYLOAD, latestClosedRun: makeRun('completed') };
    const result = deriveOperatingState(payload, 2);
    expect(result.state).toBe('stable-baseline');
  });

  it('returns awaiting-first-observation when active run has no check-ins', () => {
    const payload = { ...EMPTY_PAYLOAD, activeRun: makeRun('active'), latestCheckInSignal: BASE_SIGNAL };
    const result = deriveOperatingState(payload, 2);
    expect(result.state).toBe('awaiting-first-observation');
  });

  it('returns running when active run has check-ins and no issues', () => {
    const payload = {
      ...EMPTY_PAYLOAD,
      activeRun: makeRun('active'),
      latestCheckInSignal: { ...BASE_SIGNAL, checkInId: 'ci-1', date: '2026-05-09' },
    };
    const result = deriveOperatingState(payload, 2);
    expect(result.state).toBe('running');
  });

  it('returns review-pending when there is a review summary', () => {
    const payload = {
      ...EMPTY_PAYLOAD,
      activeRun: makeRun('active'),
      latestCheckInSignal: { ...BASE_SIGNAL, checkInId: 'ci-1', date: '2026-05-09' },
      latestReviewSummary: { protocolId: 'p-1', lineageRootProtocolId: 'p-1', lineageName: 'Test', cue: '', signalType: '', versionCount: 1, runCount: 1, checkInCount: 7 },
    };
    const result = deriveOperatingState(payload, 2);
    expect(result.state).toBe('review-pending');
  });

  it('returns drift-accumulating when drift state is regime_shift', () => {
    const payload = {
      ...EMPTY_PAYLOAD,
      activeRun: makeRun('active'),
      latestCheckInSignal: { ...BASE_SIGNAL, checkInId: 'ci-1', date: '2026-05-09' },
      driftSnapshot: { protocolId: 'p-1', driftState: 'regime_shift', baselineSource: 'historical_runs', signals: [], regimeClassification: null },
    };
    const result = deriveOperatingState(payload, 2);
    expect(result.state).toBe('drift-accumulating');
  });

  it('provides whyInputs for every state', () => {
    const result = deriveOperatingState(EMPTY_PAYLOAD, 0);
    expect(result.whyInputs.length).toBeGreaterThan(0);
    expect(result.reasoning).toBeTruthy();
  });
});
