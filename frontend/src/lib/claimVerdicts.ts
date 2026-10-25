import type { ClaimVerdict, KnowledgePublicClaim } from '@/lib/types';

/** Growth spec 02 §6: the headline is the published claim with the biggest Hype Gap. */
const BAND_RANK: Record<string, number> = { Extreme: 4, High: 3, Moderate: 2, Low: 1 };

export function isPublished(claim: KnowledgePublicClaim): boolean {
  return claim.published && claim.verdict !== 'NOT YET GRADED';
}

export function displayVerdict(claim: KnowledgePublicClaim): ClaimVerdict {
  return isPublished(claim) ? claim.verdict : 'NOT YET GRADED';
}

/**
 * Headline claim: among published claims, the highest Hype Gap band (ties keep record order).
 * With nothing published, the first claim, shown NOT YET GRADED.
 */
export function headlineClaim(claims: KnowledgePublicClaim[] | undefined): KnowledgePublicClaim | null {
  if (!claims || claims.length === 0) return null;
  const published = claims.filter(isPublished);
  if (published.length === 0) return claims[0];
  return published.reduce((best, c) => ((BAND_RANK[c.hypeBand] ?? 0) > (BAND_RANK[best.hypeBand] ?? 0) ? c : best), published[0]);
}

export function formatAsOf(value?: string | null): string {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric', timeZone: 'UTC' });
}

/** Voice guide §4 share text: "[Compound] for [claim]: [VERDICT]. Receipts → biostack.cc/[path]". */
export function shareText(compound: string, claim: KnowledgePublicClaim, path: string): string {
  const verdict = displayVerdict(claim);
  const receipts = `biostack.cc${path}`;
  if (verdict === 'NOT YET GRADED') return `${compound}: the evidence record. Receipts → ${receipts}`;
  return `${compound}, "${claim.statement}": ${verdict}. Receipts → ${receipts}`;
}

/** The only place trial doses may render (06 §A). Used by the dose-leak tests. */
export const DOSE_PATTERN = /\b\d+(\.\d+)?\s?(mg|mcg|µg|ug|iu|units|ml)\b/i;

export const STATUS_COPY: Record<string, string> = {
  'pending-council': 'Awaiting the two-reviewer evidence council.',
  'changes-requested': 'The evidence council asked for changes before grading.',
  'escalated-to-owner': 'The evidence council disagreed. Awaiting a final decision.',
};
