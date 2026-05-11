import type { CurrentStackIntelligence, CompoundRecord } from '@/lib/types';

export interface SignalClarityLimiter {
  label: string;
  delta: number;
  description: string;
}

export interface SignalClarityResult {
  score: number; // 0–100
  band: string;
  limiters: SignalClarityLimiter[];
  whyInputs: Array<{ label: string; value: string | number | null | undefined }>;
  reasoning: string;
}

export function deriveSignalClarity(
  stackIntelligence: CurrentStackIntelligence | null,
  compounds: CompoundRecord[],
  checkInCount: number,
  hasActiveRun: boolean,
): SignalClarityResult {
  const empty: SignalClarityResult = {
    score: 0,
    band: 'Unclear Signal',
    limiters: [{ label: 'No stack data', delta: -100, description: 'Stack intelligence is unavailable.' }],
    whyInputs: [{ label: 'Stack intelligence', value: 'unavailable' }],
    reasoning: 'Stack intelligence data is not available. This score requires an active protocol with at least one compound.',
  };

  if (!stackIntelligence) return empty;

  const baseScore = stackIntelligence.stackScore?.score ?? 0;
  const activeCompounds = compounds.filter((c) => c.status === 'Active');
  const limiters: SignalClarityLimiter[] = [];
  let adjustedScore = baseScore;

  // Penalty: no active run
  if (!hasActiveRun) {
    const penalty = 15;
    adjustedScore -= penalty;
    limiters.push({ label: 'No active run', delta: -penalty, description: 'Starting a run enables cadence and drift tracking.' });
  }

  // Penalty: low check-in cadence
  if (checkInCount === 0) {
    const penalty = 20;
    adjustedScore -= penalty;
    limiters.push({ label: 'No observations logged', delta: -penalty, description: 'Check-ins are required to build signal confidence.' });
  } else if (checkInCount < 3) {
    const penalty = 10;
    adjustedScore -= penalty;
    limiters.push({ label: 'Sparse observations', delta: -penalty, description: `Only ${checkInCount} check-in${checkInCount !== 1 ? 's' : ''} logged. 7+ recommended for drift detection.` });
  }

  // Penalty: no compound attached
  if (activeCompounds.length === 0) {
    const penalty = 25;
    adjustedScore -= penalty;
    limiters.push({ label: 'No active compounds', delta: -penalty, description: 'Add compounds to enable interaction and overlap analysis.' });
  }

  // Score from stack interactions (redundancy and interference from stack score breakdown)
  // stackScore is already 0–100 from the API; we adjust for data completeness
  adjustedScore = Math.max(0, Math.min(100, adjustedScore));

  let band = 'Unclear Signal';
  if (adjustedScore >= 80) band = 'High Clarity';
  else if (adjustedScore >= 60) band = 'Good Clarity';
  else if (adjustedScore >= 40) band = 'Moderate Clarity';
  else if (adjustedScore >= 20) band = 'Low Clarity';

  const topLimiters = limiters.slice(0, 2);

  return {
    score: Math.round(adjustedScore),
    band,
    limiters: topLimiters,
    whyInputs: [
      { label: 'Base stack score', value: stackIntelligence.stackScore?.score ?? 0 },
      { label: 'Active compounds', value: activeCompounds.length },
      { label: 'Check-ins logged', value: checkInCount },
      { label: 'Has active run', value: hasActiveRun ? 'yes' : 'no' },
    ],
    reasoning: `Signal clarity is derived from the stack interaction score (${stackIntelligence.stackScore?.score ?? 0}), adjusted for data completeness. ${topLimiters.map((l) => l.description).join(' ')}`,
  };
}
