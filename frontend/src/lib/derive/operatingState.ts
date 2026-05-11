import type { ProtocolConsolePayload, ProtocolRun } from '@/lib/types';
import type { OperatingState } from '@/styles/tokens';

export interface OperatingStateResult {
  state: OperatingState;
  activeRun: ProtocolRun | null;
  whyInputs: Array<{ label: string; value: string | number | null | undefined }>;
  reasoning: string;
  caveats: string[];
}

function isRunActive(run: ProtocolRun | null): run is ProtocolRun {
  return run !== null && run.status === 'active';
}

export function deriveOperatingState(
  payload: ProtocolConsolePayload | null,
  compoundCount: number,
): OperatingStateResult {
  const noData: OperatingStateResult = {
    state: 'no-active-run',
    activeRun: null,
    whyInputs: [{ label: 'Protocol payload', value: 'unavailable' }],
    reasoning: 'No protocol data is available. Start a protocol to begin tracking.',
    caveats: [],
  };

  if (!payload) return noData;

  const { activeRun, latestClosedRun, latestReviewSummary, driftSnapshot, sequenceExpectationSnapshot, latestCheckInSignal } = payload;

  const inputs: Array<{ label: string; value: string | number | null | undefined }> = [
    { label: 'Active run', value: activeRun?.id ?? 'none' },
    { label: 'Drift state', value: driftSnapshot?.driftState ?? 'unknown' },
    { label: 'Sequence status', value: sequenceExpectationSnapshot?.currentStatus?.state ?? 'unknown' },
    { label: 'Latest check-in', value: latestCheckInSignal?.date ?? 'none' },
    { label: 'Compounds', value: compoundCount },
    { label: 'Review summary', value: latestReviewSummary ? 'available' : 'none' },
  ];

  // Priority cascade
  if (!isRunActive(activeRun)) {
    if (latestClosedRun) {
      return {
        state: 'stable-baseline',
        activeRun: null,
        whyInputs: inputs,
        reasoning: `No run is currently active. The last run (${latestClosedRun.id}) provides a historical baseline. Start a new run when ready to resume tracking.`,
        caveats: ['Historical baseline may not reflect current state if time has elapsed.'],
      };
    }
    return {
      state: 'no-active-run',
      activeRun: null,
      whyInputs: inputs,
      reasoning: 'No active or historical protocol run found. Create a protocol and start a run to begin signal collection.',
      caveats: [],
    };
  }

  // Review pending takes priority over drift (user action required first)
  if (latestReviewSummary) {
    return {
      state: 'review-pending',
      activeRun,
      whyInputs: inputs,
      reasoning: `Protocol "${latestReviewSummary.lineageName}" has an earned review ready. Completing it closes the current observation window and unlocks the next run phase.`,
      caveats: [],
    };
  }

  // Drift accumulating
  const driftState = driftSnapshot?.driftState ?? 'none';
  if (driftState === 'regime_shift' || driftState === 'moderate') {
    return {
      state: 'drift-accumulating',
      activeRun,
      whyInputs: inputs,
      reasoning: `Drift signals (${driftState}) suggest the current regime is diverging from the baseline. Logging an observation re-anchors the signal window.`,
      caveats: ['Drift is derived from check-in cadence and computation timing, not from biomarker outcomes.'],
    };
  }

  // Awaiting first observation
  if (!latestCheckInSignal?.checkInId) {
    return {
      state: 'awaiting-first-observation',
      activeRun,
      whyInputs: inputs,
      reasoning: 'The run is active but no check-ins have been logged yet. The first observation opens the signal tracking window.',
      caveats: ['Signal quality metrics are unavailable until the first check-in is logged.'],
    };
  }

  return {
    state: 'running',
    activeRun,
    whyInputs: inputs,
    reasoning: 'A protocol run is active with at least one observation logged. The system is collecting signal data.',
    caveats: [],
  };
}
