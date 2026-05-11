// BioStack Feature Flags
// Controls progressive rollout of Phase 1+ intelligence surfaces.
// In production: read from env or remote config. In dev: all on.

export type FeatureFlag =
  | 'missionControl2'
  | 'stackGraph'
  | 'flightRecorder'
  | 'trustLedger'
  | 'decisionTheater'
  | 'observationDebtInbox'
  | 'counterfactualLab';

const DEV_OVERRIDES: Partial<Record<FeatureFlag, boolean>> = {};

function isDevMode(): boolean {
  return process.env.NODE_ENV === 'development';
}

const ENV_FLAGS: Record<FeatureFlag, string> = {
  missionControl2: 'NEXT_PUBLIC_FLAG_MISSION_CONTROL_2',
  stackGraph: 'NEXT_PUBLIC_FLAG_STACK_GRAPH',
  flightRecorder: 'NEXT_PUBLIC_FLAG_FLIGHT_RECORDER',
  trustLedger: 'NEXT_PUBLIC_FLAG_TRUST_LEDGER',
  decisionTheater: 'NEXT_PUBLIC_FLAG_DECISION_THEATER',
  observationDebtInbox: 'NEXT_PUBLIC_FLAG_OD_INBOX',
  counterfactualLab: 'NEXT_PUBLIC_FLAG_COUNTERFACTUAL_LAB',
};

export function isEnabled(flag: FeatureFlag): boolean {
  if (flag in DEV_OVERRIDES) {
    return DEV_OVERRIDES[flag]!;
  }
  const envKey = ENV_FLAGS[flag];
  const envVal = process.env[envKey];
  if (envVal !== undefined) {
    return envVal === 'true' || envVal === '1';
  }
  // Default: on in dev, off in prod (opt-in rollout)
  return isDevMode();
}

export function enableFlag(flag: FeatureFlag) {
  DEV_OVERRIDES[flag] = true;
}

export function disableFlag(flag: FeatureFlag) {
  DEV_OVERRIDES[flag] = false;
}
