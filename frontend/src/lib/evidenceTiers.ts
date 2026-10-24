/**
 * The 7-tier methodology scale (docs/biostack/evidence-grading-methodology-v1.md).
 * The API returns enum names ("EarlyEvidence"); records and docs use labels
 * ("Early Evidence"). Both normalize to the same key.
 */
export const METHODOLOGY_TIERS = [
  {
    key: 'established',
    label: 'Established',
    definition: 'The specific claim is supported by consistent, relevant human evidence: systematic reviews, meta-analyses, or multiple well-designed human studies with consistent outcomes.',
  },
  {
    key: 'plausibletheory',
    label: 'Plausible Theory',
    definition: 'A coherent biological or mechanistic rationale exists, but direct human evidence is weak, indirect, absent, or not yet outcome-relevant. A plausible mechanism is not an outcome.',
  },
  {
    key: 'earlyevidence',
    label: 'Early Evidence',
    definition: 'Preliminary human evidence exists, but the base is small, mixed, early-phase, underpowered, or not yet reproduced.',
  },
  {
    key: 'anecdotalsignal',
    label: 'Anecdotal Signal',
    definition: 'Supported mainly by user reports, community reports, case narratives, or n-of-1 notes, kept separate from general evidence.',
  },
  {
    key: 'contradicted',
    label: 'Contradicted',
    definition: 'Credible evidence conflicts with or directly weakens the claim, especially when better-matched or higher-quality evidence is negative.',
  },
  {
    key: 'unsupported',
    label: 'Unsupported',
    definition: 'No adequate source-verified support was found for the specific claim.',
  },
  {
    key: 'hypeexceedsevidence',
    label: 'Hype Exceeds Evidence',
    definition: 'Hype, influencer repetition, distorted citations, or community certainty substantially exceeds the evidence.',
  },
] as const;

export function tierKey(tier: string | null | undefined): string {
  return (tier ?? '').toLowerCase().replace(/[\s_-]/g, '');
}

export function tierLabel(tier: string | null | undefined): string {
  const key = tierKey(tier);
  const match = METHODOLOGY_TIERS.find(t => t.key === key);
  if (match) return match.label;
  if (key === '' || key === 'unknown' || key === 'notgraded') return 'Not graded';
  return tier ?? 'Not graded';
}
