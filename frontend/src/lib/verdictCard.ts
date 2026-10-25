import { displayVerdict, formatAsOf, headlineClaim, isPublished } from '@/lib/claimVerdicts';
import { compoundSlug } from '@/lib/compoundSlug';
import { assertCleanCopy } from '@/lib/copyLint';
import { DOSE_TOKEN } from '@/lib/doseRedaction';
import type { ClaimVerdict, KnowledgeEntry, KnowledgePublicClaim } from '@/lib/types';

/**
 * Card model for the verdict / claim cards (04-card-templates.md). Every value comes from
 * the published claim record; nothing is typed in by hand. Unpublished claims render as
 * NOT YET GRADED. Build fails (CardBuildError) when a PROVEN card has no qualifier, or when
 * any slot contains a dose token or a banned phrase.
 */
export type CardFormat = 'og' | 'story';
export type CardKind = 'compound' | 'claim';

export const CARD_SIZES: Record<CardFormat, { width: number; height: number }> = {
  og: { width: 1200, height: 630 },
  story: { width: 1080, height: 1920 },
};

export const TAGLINES = ['Settle it.', 'Ask BioStack.', 'Receipts, not reels.', 'The science vs. the feed.'] as const;

export interface CardModel {
  kind: CardKind;
  format: CardFormat;
  kicker: string | null;
  compound: string;
  claimLine: string;
  verdict: ClaimVerdict;
  qualifier: string | null;
  receiptLine: string | null;
  hypeLine: string | null;
  footer: string;
  tagline: (typeof TAGLINES)[number];
  alt: string;
  version: number | null;
  superseded: boolean;
}

export class CardBuildError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'CardBuildError';
  }
}

export function truncateClaim(text: string, max: number): string {
  return text.length <= max ? text : `${text.slice(0, max - 1).trimEnd()}…`;
}

function fail(condition: boolean, message: string) {
  if (condition) throw new CardBuildError(message);
}

export function claimShortPath(claim: KnowledgePublicClaim): string {
  return `/c/${claim.id}`;
}

function footerFor(asOf: string, path: string): string {
  const url = `biostack.cc${path}`;
  const full = asOf ? `Evidence as of ${asOf} · ${url}` : url;
  return full;
}

export function buildCardModel(input: {
  kind: CardKind;
  entry: KnowledgeEntry;
  claim?: KnowledgePublicClaim | null;
  format?: CardFormat;
  requestedVersion?: number | null;
}): CardModel {
  const format = input.format ?? 'og';
  const claims = input.entry.details?.publicClaims ?? [];
  const claim = input.claim ?? headlineClaim(claims);
  const compound = input.entry.canonicalName;
  const slug = compoundSlug(compound);
  const published = claim ? isPublished(claim) : false;
  const verdict: ClaimVerdict = claim ? displayVerdict(claim) : 'NOT YET GRADED';
  const qualifier = published && verdict === 'PROVEN IN HUMANS' ? (claim?.qualifier?.trim() || null) : null;
  fail(verdict === 'PROVEN IN HUMANS' && !qualifier, 'PROVEN IN HUMANS card without its qualifier');

  const asOf = published && claim ? formatAsOf(claim.publishedAt ?? claim.verifiedAt) : '';
  const longPath = input.kind === 'claim' && claim ? `/knowledge/${slug}/claims/${claim.id}` : `/knowledge/${slug}`;
  let footer = footerFor(asOf, longPath);
  if (footer.length > 64 && claim && input.kind === 'claim') footer = footerFor(asOf, claimShortPath(claim));

  const claimMax = input.kind === 'claim' ? 80 : 60;
  const claimLine = claim ? `“${truncateClaim(claim.statement, claimMax - 2)}”` : 'The evidence record';
  const receiptLine = input.kind === 'claim' && published && claim?.note ? truncateClaim(claim.note, 90) : null;
  const hypeLine = published && claim?.hypeBand ? `Hype Gap: ${claim.hypeBand.toUpperCase()}` : null;
  const tagline = input.kind === 'claim' ? 'Receipts, not reels.' : 'Settle it.';
  const version = claim?.version ?? null;
  const superseded = input.requestedVersion != null && version != null && input.requestedVersion < version;

  const alt = `BioStack verdict card for ${compound}: ${claim ? `“${claim.statement}”` : 'evidence record'}, ${verdict}${qualifier ? `. ${qualifier.replace(/\.$/, '')}` : ''}${asOf ? `. Evidence as of ${asOf}.` : '.'}`;

  // 04 global rules: no doses, units or banned phrases anywhere on a card.
  const slots = [compound, claimLine, qualifier ?? '', receiptLine ?? '', hypeLine ?? '', footer, tagline];
  fail(slots.some(s => new RegExp(DOSE_TOKEN.source, 'i').test(s)), 'Card slot contains a dose');
  try {
    assertCleanCopy(slots.filter(Boolean), { allowProven: true });
  } catch (e) {
    throw new CardBuildError((e as Error).message);
  }

  return {
    kind: input.kind,
    format,
    kicker: input.kind === 'claim' ? 'THE CLAIM' : null,
    compound: truncateClaim(compound, 28),
    claimLine,
    verdict,
    qualifier,
    receiptLine,
    hypeLine,
    footer,
    tagline,
    alt,
    version,
    superseded,
  };
}

/** Cache key / URL for a card image: path + version + format (regenerates on a new version). */
export function cardImageUrl(kind: CardKind, id: string, version: number | null | undefined, format: CardFormat = 'og'): string {
  const base = kind === 'compound' ? `/og/compound/${id}` : `/og/claim/${id}`;
  const params = new URLSearchParams();
  if (version != null) params.set('v', String(version));
  if (format === 'story') params.set('format', 'story');
  const q = params.toString();
  return q ? `${base}?${q}` : base;
}
