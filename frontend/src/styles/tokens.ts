// BioStack Design Token System
// Single source of truth for intelligence surface visual language.

// ── Confidence Levels ──────────────────────────────────────────────────────
export type ConfidenceLevel = 'high' | 'moderate' | 'low' | 'insufficient' | 'review-required';

export const CONFIDENCE_TOKENS: Record<ConfidenceLevel, {
  label: string;
  bg: string;
  text: string;
  border: string;
  dot: string;
}> = {
  high: {
    label: 'High Confidence',
    bg: 'bg-emerald-500/10',
    text: 'text-emerald-300',
    border: 'border-emerald-400/20',
    dot: 'bg-emerald-400',
  },
  moderate: {
    label: 'Moderate Confidence',
    bg: 'bg-blue-500/10',
    text: 'text-blue-300',
    border: 'border-blue-400/20',
    dot: 'bg-blue-400',
  },
  low: {
    label: 'Low Confidence',
    bg: 'bg-amber-500/10',
    text: 'text-amber-300',
    border: 'border-amber-400/20',
    dot: 'bg-amber-400',
  },
  insufficient: {
    label: 'Insufficient Data',
    bg: 'bg-white/5',
    text: 'text-white/50',
    border: 'border-white/10',
    dot: 'bg-white/30',
  },
  'review-required': {
    label: 'Review Required',
    bg: 'bg-rose-500/10',
    text: 'text-rose-300',
    border: 'border-rose-400/20',
    dot: 'bg-rose-400',
  },
};

// ── Operating States ───────────────────────────────────────────────────────
export type OperatingState =
  | 'running'
  | 'awaiting-first-observation'
  | 'review-pending'
  | 'drift-accumulating'
  | 'stable-baseline'
  | 'no-active-run';

export const OPERATING_STATE_TOKENS: Record<OperatingState, {
  label: string;
  sub: string;
  color: string;
  bg: string;
  border: string;
  cta: string;
  ctaHref: string;
}> = {
  running: {
    label: 'Protocol Running',
    sub: 'Active run in progress — observations are being collected.',
    color: 'text-emerald-300',
    bg: 'bg-emerald-500/8',
    border: 'border-emerald-400/20',
    cta: 'Log Observation',
    ctaHref: '/checkins/new',
  },
  'awaiting-first-observation': {
    label: 'Awaiting First Observation',
    sub: 'Run started but no check-ins recorded yet. First observation unlocks signal tracking.',
    color: 'text-blue-300',
    bg: 'bg-blue-500/8',
    border: 'border-blue-400/20',
    cta: 'Log First Observation',
    ctaHref: '/checkins/new',
  },
  'review-pending': {
    label: 'Review Pending',
    sub: 'Earned review is available. Complete it to unlock the next run.',
    color: 'text-amber-300',
    bg: 'bg-amber-500/8',
    border: 'border-amber-400/20',
    cta: 'Start Review',
    ctaHref: '/protocols',
  },
  'drift-accumulating': {
    label: 'Drift Accumulating',
    sub: 'Regime signals are diverging from baseline. Logging an observation will help re-anchor.',
    color: 'text-orange-300',
    bg: 'bg-orange-500/8',
    border: 'border-orange-400/20',
    cta: 'Log Observation',
    ctaHref: '/checkins/new',
  },
  'stable-baseline': {
    label: 'Stable Baseline',
    sub: 'No active run. Historical data is available as a reference baseline.',
    color: 'text-white/60',
    bg: 'bg-white/5',
    border: 'border-white/10',
    cta: 'Start New Run',
    ctaHref: '/protocols',
  },
  'no-active-run': {
    label: 'No Active Protocol',
    sub: 'Start a protocol run to begin tracking signal quality.',
    color: 'text-white/40',
    bg: 'bg-white/3',
    border: 'border-white/8',
    cta: 'Start Protocol',
    ctaHref: '/protocols',
  },
};

// ── Drift / Regime States ──────────────────────────────────────────────────
export type DriftState = 'none' | 'mild' | 'moderate' | 'regime_shift' | 'stable' | 'drifting' | 'shifted' | 'unknown';

export const DRIFT_TOKENS: Record<string, {
  label: string;
  emoji: string;
  color: string;
  bg: string;
  border: string;
}> = {
  none: { label: 'Stable', emoji: '◉', color: 'text-emerald-300', bg: 'bg-emerald-500/10', border: 'border-emerald-400/20' },
  mild: { label: 'Mild Drift', emoji: '◎', color: 'text-amber-300', bg: 'bg-amber-500/10', border: 'border-amber-400/20' },
  moderate: { label: 'Moderate Drift', emoji: '◑', color: 'text-orange-300', bg: 'bg-orange-500/10', border: 'border-orange-400/20' },
  regime_shift: { label: 'Regime Shift', emoji: '◆', color: 'text-rose-300', bg: 'bg-rose-500/10', border: 'border-rose-400/20' },
  stable: { label: 'Stable', emoji: '◉', color: 'text-emerald-300', bg: 'bg-emerald-500/10', border: 'border-emerald-400/20' },
  drifting: { label: 'Drifting', emoji: '◑', color: 'text-amber-300', bg: 'bg-amber-500/10', border: 'border-amber-400/20' },
  shifted: { label: 'Shifted', emoji: '◆', color: 'text-rose-300', bg: 'bg-rose-500/10', border: 'border-rose-400/20' },
  unknown: { label: 'Unknown', emoji: '○', color: 'text-white/40', bg: 'bg-white/5', border: 'border-white/10' },
};

// ── Evidence Tiers ─────────────────────────────────────────────────────────
export type EvidenceTier = 'strong' | 'moderate' | 'limited' | 'theoretical' | 'anecdotal' | 'insufficient' | 'unknown';

export const EVIDENCE_TIER_TOKENS: Record<string, {
  label: string;
  short: string;
  color: string;
  bg: string;
  border: string;
}> = {
  strong: { label: 'Strong Evidence', short: 'Strong', color: 'text-emerald-300', bg: 'bg-emerald-500/10', border: 'border-emerald-400/20' },
  moderate: { label: 'Moderate Evidence', short: 'Moderate', color: 'text-blue-300', bg: 'bg-blue-500/10', border: 'border-blue-400/20' },
  limited: { label: 'Limited Evidence', short: 'Limited', color: 'text-amber-300', bg: 'bg-amber-500/10', border: 'border-amber-400/20' },
  theoretical: { label: 'Theoretical', short: 'Theoretical', color: 'text-white/50', bg: 'bg-white/5', border: 'border-white/10' },
  anecdotal: { label: 'Anecdotal', short: 'Anecdotal', color: 'text-white/40', bg: 'bg-white/5', border: 'border-white/8' },
  insufficient: { label: 'Insufficient Data', short: 'Insufficient', color: 'text-rose-300', bg: 'bg-rose-500/10', border: 'border-rose-400/20' },
  unknown: { label: 'Unknown', short: 'Unknown', color: 'text-white/30', bg: 'bg-white/4', border: 'border-white/8' },
};

// ── Regulatory Boundary ────────────────────────────────────────────────────
export type RegulatoryBoundary = 'otc' | 'supplement' | 'prescription' | 'controlled' | 'research-only' | 'prohibited' | 'unknown';

export const REGULATORY_TOKENS: Record<string, {
  label: string;
  short: string;
  color: string;
  bg: string;
  border: string;
}> = {
  otc: { label: 'Over the Counter', short: 'OTC', color: 'text-emerald-300', bg: 'bg-emerald-500/10', border: 'border-emerald-400/20' },
  supplement: { label: 'Dietary Supplement', short: 'Supplement', color: 'text-blue-300', bg: 'bg-blue-500/10', border: 'border-blue-400/20' },
  prescription: { label: 'Prescription Required', short: 'Rx', color: 'text-amber-300', bg: 'bg-amber-500/10', border: 'border-amber-400/20' },
  controlled: { label: 'Controlled Substance', short: 'Controlled', color: 'text-orange-300', bg: 'bg-orange-500/10', border: 'border-orange-400/20' },
  'research-only': { label: 'Research Use Only', short: 'Research', color: 'text-purple-300', bg: 'bg-purple-500/10', border: 'border-purple-400/20' },
  prohibited: { label: 'Prohibited', short: 'Prohibited', color: 'text-rose-300', bg: 'bg-rose-500/10', border: 'border-rose-400/20' },
  unknown: { label: 'Unknown', short: 'Unknown', color: 'text-white/40', bg: 'bg-white/5', border: 'border-white/10' },
};

// ── Interaction Types ──────────────────────────────────────────────────────
export const INTERACTION_TOKENS: Record<string, {
  label: string;
  color: string;
  edgeColor: string;
  bg: string;
  border: string;
}> = {
  Synergistic: { label: 'Synergy', color: 'text-emerald-300', edgeColor: '#22c55e', bg: 'bg-emerald-500/10', border: 'border-emerald-400/20' },
  Complementary: { label: 'Complementary', color: 'text-blue-300', edgeColor: '#3b82f6', bg: 'bg-blue-500/10', border: 'border-blue-400/20' },
  Neutral: { label: 'Neutral', color: 'text-white/50', edgeColor: 'rgba(255,255,255,0.2)', bg: 'bg-white/5', border: 'border-white/10' },
  Redundant: { label: 'Redundant', color: 'text-amber-300', edgeColor: '#f59e0b', bg: 'bg-amber-500/10', border: 'border-amber-400/20' },
  Interfering: { label: 'Interfering', color: 'text-rose-300', edgeColor: '#f43f5e', bg: 'bg-rose-500/10', border: 'border-rose-400/20' },
};

// ── Timeline Event Tags ────────────────────────────────────────────────────
export type TimelineEventTag = 'aligned' | 'late' | 'diverging' | 'regime-shift' | 'expected-pending';

export const TIMELINE_TAG_TOKENS: Record<TimelineEventTag, {
  label: string;
  color: string;
  bg: string;
  border: string;
}> = {
  aligned: { label: 'Aligned', color: 'text-emerald-300', bg: 'bg-emerald-500/10', border: 'border-emerald-400/20' },
  late: { label: 'Late', color: 'text-amber-300', bg: 'bg-amber-500/10', border: 'border-amber-400/20' },
  diverging: { label: 'Diverging', color: 'text-orange-300', bg: 'bg-orange-500/10', border: 'border-orange-400/20' },
  'regime-shift': { label: 'Regime Shift', color: 'text-rose-300', bg: 'bg-rose-500/10', border: 'border-rose-400/20' },
  'expected-pending': { label: 'Expected Pending', color: 'text-blue-300', bg: 'bg-blue-500/10', border: 'border-blue-400/20' },
};

// ── Stack Clarity Bands ────────────────────────────────────────────────────
export function getStackClarityBand(score: number): { label: string; color: string; bg: string } {
  if (score >= 80) return { label: 'High Clarity', color: 'text-emerald-300', bg: 'bg-emerald-500/10' };
  if (score >= 60) return { label: 'Good Clarity', color: 'text-blue-300', bg: 'bg-blue-500/10' };
  if (score >= 40) return { label: 'Moderate Clarity', color: 'text-amber-300', bg: 'bg-amber-500/10' };
  if (score >= 20) return { label: 'Low Clarity', color: 'text-orange-300', bg: 'bg-orange-500/10' };
  return { label: 'Unclear Signal', color: 'text-rose-300', bg: 'bg-rose-500/10' };
}
