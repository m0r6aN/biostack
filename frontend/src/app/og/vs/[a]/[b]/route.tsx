import { ImageResponse } from 'next/og';
import { CHIP_STYLE } from '@/components/og/VerdictCardImage';
import { parseCardQuery } from '@/lib/cardResponse';
import { fetchKnowledgeEntry } from '@/lib/knowledgeServer';
import { CARD_SIZES } from '@/lib/verdictCard';
import { isCuratedPair, rulingFor, vsAsOf, vsPath, vsSide, type VsSide } from '@/lib/vsPairs';

// Growth Phase 4: /vs card (04 §3). Header, both verdict chips, Hype Gap row, the templated
// ruling (curated pairs with two published verdicts only), footer and "Settle it." tagline.
export const revalidate = 3600;

function Chip({ side, big }: { side: VsSide; big: boolean }) {
  const c = CHIP_STYLE[side.verdict] ?? CHIP_STYLE['NOT YET GRADED'];
  return (
    <div style={{ display: 'flex', fontSize: big ? 40 : 30, fontWeight: 800, padding: '8px 18px', borderRadius: 12, border: `3px solid ${c.border}`, color: c.color }}>
      {`${c.icon} ${side.verdict}`}
    </div>
  );
}

export async function GET(request: Request, { params }: { params: Promise<{ a: string; b: string }> }) {
  const { a, b } = await params;
  const { format } = parseCardQuery(request);
  const story = format === 'story';
  const [ea, eb] = await Promise.all([fetchKnowledgeEntry(a), fetchKnowledgeEntry(b)]);
  const left = ea ? vsSide(ea) : null;
  const right = eb ? vsSide(eb) : null;
  const ruling = left && right && isCuratedPair(a, b) ? rulingFor(left, right) : null;
  const asOf = left && right ? vsAsOf(left, right) : '';
  const url = `biostack.cc${vsPath(a, b)}`;
  return new ImageResponse(
    (
      <div style={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column', justifyContent: 'space-between',
        padding: story ? '250px 80px 340px' : '56px 72px', background: '#0B1117', color: '#FFFFFF', fontFamily: 'sans-serif' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 28, fontWeight: 700, letterSpacing: 2 }}>
          <div style={{ display: 'flex', color: '#6EE7B7' }}>BIOSTACK</div>
          <div style={{ display: 'flex', color: 'rgba(255,255,255,0.8)' }}>SETTLE IT.</div>
        </div>
        <div style={{ display: 'flex', flexDirection: story ? 'column' : 'row', justifyContent: 'space-between', gap: 24 }}>
          {[{ e: ea, s: left }, { e: eb, s: right }].map(({ e, s }, i) => (
            <div key={i} style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
              <div style={{ display: 'flex', fontSize: story ? 72 : 56, fontWeight: 800 }}>{e?.canonicalName ?? '?'}</div>
              {s && <Chip side={s} big={story} />}
              <div style={{ display: 'flex', fontSize: 26, color: 'rgba(255,255,255,0.75)' }}>{`Hype Gap: ${s?.hypeBand ? s.hypeBand.toUpperCase() : 'n/a'}`}</div>
            </div>
          ))}
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          {ruling && <div style={{ display: 'flex', fontSize: story ? 40 : 32, fontWeight: 700 }}>{`“${ruling}”`}</div>}
          <div style={{ display: 'flex', fontSize: 24, color: 'rgba(255,255,255,0.6)' }}>{asOf ? `Evidence as of ${asOf} · ${url}` : url}</div>
        </div>
      </div>
    ),
    { ...CARD_SIZES[format], status: ea && eb ? 200 : 404,
      headers: { 'Cache-Control': 'public, max-age=3600, s-maxage=86400, stale-while-revalidate=86400' } },
  );
}
