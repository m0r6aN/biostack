import { ImageResponse } from 'next/og';
import { VerdictCardImage } from '@/components/og/VerdictCardImage';
import { CARD_SIZES, CardBuildError, buildCardModel, type CardFormat, type CardKind, type CardModel } from '@/lib/verdictCard';
import type { KnowledgeEntry, KnowledgePublicClaim } from '@/lib/types';

const HEADERS = { 'Cache-Control': 'public, max-age=3600, s-maxage=86400, stale-while-revalidate=86400' };

export function parseCardQuery(request: Request): { format: CardFormat; version: number | null } {
  const url = new URL(request.url);
  const format: CardFormat = url.searchParams.get('format') === 'story' ? 'story' : 'og';
  const v = Number.parseInt(url.searchParams.get('v') ?? '', 10);
  return { format, version: Number.isFinite(v) ? v : null };
}

/** Fallback when a card can't be built (unknown compound, or a failed card check). */
function fallback(name: string, format: CardFormat, status: number) {
  return new ImageResponse(
    (
      <div style={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column', justifyContent: 'center',
        padding: '72px', background: '#0B1117', color: '#FFFFFF', fontFamily: 'sans-serif' }}>
        <div style={{ display: 'flex', fontSize: 30, fontWeight: 700, color: '#6EE7B7', letterSpacing: 2 }}>BIOSTACK</div>
        <div style={{ display: 'flex', fontSize: 64, fontWeight: 800, marginTop: 24 }}>{name}</div>
        <div style={{ display: 'flex', fontSize: 28, marginTop: 24, color: 'rgba(255,255,255,0.6)' }}>Receipts, not reels.</div>
      </div>
    ),
    { ...CARD_SIZES[format], status, headers: HEADERS },
  );
}

export function renderCard(opts: {
  kind: CardKind; entry: KnowledgeEntry | null; claim?: KnowledgePublicClaim | null; request: Request; fallbackName: string;
}) {
  const { format, version } = parseCardQuery(opts.request);
  if (!opts.entry) return fallback(opts.fallbackName, format, 404);
  let model: CardModel;
  try {
    model = buildCardModel({ kind: opts.kind, entry: opts.entry, claim: opts.claim, format, requestedVersion: version });
  } catch (e) {
    if (e instanceof CardBuildError) {
      console.error(`card build failed for ${opts.entry.canonicalName}: ${e.message}`);
      return fallback(opts.entry.canonicalName, format, 200);
    }
    throw e;
  }
  return new ImageResponse(<VerdictCardImage model={model} />, { ...CARD_SIZES[format], headers: HEADERS });
}
