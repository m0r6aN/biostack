import { displayVerdict, formatAsOf, headlineClaim, isPublished } from '@/lib/claimVerdicts';
import { lintCopy } from '@/lib/copyLint';
import type { ClaimVerdict, KnowledgeEntry, KnowledgePublicClaim } from '@/lib/types';

/**
 * Growth Phase 4: /vs/ debate pages. Only curated pairs are indexable (03 §4 seed list).
 * The ruling line is never written by hand: it is one of the approved templates, filled
 * from the two compounds' published (AI-council-reviewed) headline verdicts, so it changes
 * version whenever either verdict does. No ruling until both headlines are published.
 */
export const CURATED_PAIRS: ReadonlyArray<readonly [string, string]> = [
  ['bpc-157', 'tb-500'],
  ['retatrutide', 'tirzepatide'],
  ['retatrutide', 'semaglutide'],
  ['semaglutide', 'tirzepatide'],
  ['ligandrol', 'ostarine'],
  ['ibutamoren', 'ipamorelin'],
  ['cjc-1295', 'ipamorelin'],
  ['clomiphene', 'enclomiphene'],
  ['raloxifene', 'tamoxifen'],
  ['selank', 'semax'],
];

export function canonicalPair(a: string, b: string): [string, string] {
  const x = a.toLowerCase();
  const y = b.toLowerCase();
  return x <= y ? [x, y] : [y, x];
}

export function isCuratedPair(a: string, b: string): boolean {
  const [x, y] = canonicalPair(a, b);
  return CURATED_PAIRS.some(([p, q]) => canonicalPair(p, q).join('|') === `${x}|${y}`);
}

export function curatedPairsFor(slug: string): Array<[string, string]> {
  return CURATED_PAIRS.filter(([p, q]) => p === slug || q === slug).map(([p, q]) => canonicalPair(p, q));
}

export function vsPath(a: string, b: string): `/vs/${string}` {
  const [x, y] = canonicalPair(a, b);
  return `/vs/${x}/${y}`;
}

/** Ruling shapes from 03 §4. Anything else is rejected. */
const RULING_SHAPES = [
  /^More human data: (?:neither|.+)\.$/,
  /^Higher-quality evidence: .+ \(.+\)\.$/,
  /^Same tier\. Different hype\.$/,
  /^Both .+\. The argument is louder than the evidence\.$/,
];
const VS_BANNED = /\b(?:better|stronger|pick|winner|go with|for .+ use)\b/i;

export function isAllowedRuling(text: string): boolean {
  return RULING_SHAPES.some(r => r.test(text)) && !VS_BANNED.test(text) && lintCopy(text, { allowProven: true }).length === 0;
}

const HUMAN: ClaimVerdict[] = ['PROVEN IN HUMANS', 'PROMISING'];
const RANK: Record<string, number> = { 'PROVEN IN HUMANS': 2, PROMISING: 1 };

export interface VsSide { name: string; claim: KnowledgePublicClaim | null; verdict: ClaimVerdict; hypeBand: string }

export function vsSide(entry: KnowledgeEntry): VsSide {
  const claim = headlineClaim(entry.details?.publicClaims);
  const published = claim ? isPublished(claim) : false;
  return {
    name: entry.canonicalName,
    claim,
    verdict: claim ? displayVerdict(claim) : 'NOT YET GRADED',
    hypeBand: published && claim?.hypeBand ? claim.hypeBand : '',
  };
}

/** The ruling, or null when it can't be derived from two published verdicts. */
export function rulingFor(a: VsSide, b: VsSide): string | null {
  if (a.verdict === 'NOT YET GRADED' || b.verdict === 'NOT YET GRADED') return null;
  if (a.verdict === 'MYTH' || b.verdict === 'MYTH') {
    return a.verdict === b.verdict ? `Both ${a.verdict}. The argument is louder than the evidence.` : null;
  }
  const ha = HUMAN.includes(a.verdict);
  const hb = HUMAN.includes(b.verdict);
  let ruling: string;
  if (a.verdict === b.verdict) {
    ruling = a.hypeBand && b.hypeBand && a.hypeBand !== b.hypeBand
      ? 'Same tier. Different hype.'
      : `Both ${a.verdict}. The argument is louder than the evidence.`;
  } else if (!ha && !hb) {
    ruling = 'More human data: neither.';
  } else if (ha !== hb) {
    ruling = `More human data: ${ha ? a.name : b.name}.`;
  } else {
    const [hi, lo] = RANK[a.verdict] > RANK[b.verdict] ? [a, b] : [b, a];
    ruling = `Higher-quality evidence: ${hi.name} (${hi.verdict} vs ${lo.verdict}).`;
  }
  return isAllowedRuling(ruling) ? ruling : null;
}

export function sharedPathways(a: KnowledgeEntry, b: KnowledgeEntry): string[] {
  const right = new Set((b.pathways ?? []).map(p => p.trim().toLowerCase()));
  return (a.pathways ?? []).filter(p => p.trim() && right.has(p.trim().toLowerCase()) && p.length <= 60);
}

/** Claims both compounds have a published verdict on (matched by outcome). */
export function sharedClaims(a: KnowledgeEntry, b: KnowledgeEntry): Array<{ outcome: string; left: KnowledgePublicClaim; right: KnowledgePublicClaim }> {
  const right = (b.details?.publicClaims ?? []).filter(isPublished);
  return (a.details?.publicClaims ?? []).filter(isPublished).flatMap(l => {
    const r = right.find(c => c.outcome && c.outcome.toLowerCase() === l.outcome?.toLowerCase());
    return r ? [{ outcome: l.outcome, left: l, right: r }] : [];
  });
}

export function vsAsOf(a: VsSide, b: VsSide): string {
  const dates = [a.claim, b.claim].filter((c): c is KnowledgePublicClaim => !!c && isPublished(c))
    .map(c => c.publishedAt ?? c.verifiedAt).filter(Boolean) as string[];
  if (dates.length < 2) return '';
  return formatAsOf(dates.sort().at(-1));
}
