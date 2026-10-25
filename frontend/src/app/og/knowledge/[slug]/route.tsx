import { ImageResponse } from 'next/og';
import { compoundSlug } from '@/lib/compoundSlug';
import { tierLabel } from '@/lib/evidenceTiers';
import { dossierVersionDate, fetchKnowledgeEntry } from '@/lib/knowledgeServer';

// Per-compound share card (1200×630) for /knowledge/{slug}. Growth spec Phase 0: unique
// og:image per dossier. It shows the compound, the BioStack evidence grade, the "as of" date
// and the short URL — never a dose, a vendor, or a person. Cache key = ?v={version date}.
export const revalidate = 3600;

const SIZE = { width: 1200, height: 630 };

export async function GET(_request: Request, { params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const entry = await fetchKnowledgeEntry(slug);
  const name = entry?.canonicalName ?? 'Compound dossier';
  const grade = entry ? tierLabel(entry.evidenceTier) : 'Not graded';
  const asOf = entry ? dossierVersionDate(entry) : null;
  const shortUrl = `biostack.cc/knowledge/${entry ? compoundSlug(entry.canonicalName) : compoundSlug(slug)}`;

  return new ImageResponse(
    (
      <div
        style={{
          width: '100%',
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between',
          padding: '64px 72px',
          background: '#0B1117',
          color: '#FFFFFF',
          fontFamily: 'sans-serif',
        }}
      >
        <div style={{ display: 'flex', fontSize: 30, fontWeight: 700, color: '#6EE7B7', letterSpacing: 2 }}>BIOSTACK</div>
        <div style={{ display: 'flex', flexDirection: 'column' }}>
          <div style={{ display: 'flex', fontSize: name.length > 24 ? 64 : 84, fontWeight: 800, lineHeight: 1.05 }}>{name}</div>
          <div style={{ display: 'flex', marginTop: 28, alignItems: 'center' }}>
            <div style={{ display: 'flex', fontSize: 26, color: 'rgba(255,255,255,0.6)', marginRight: 18 }}>Evidence grade</div>
            <div
              style={{
                display: 'flex',
                fontSize: 34,
                fontWeight: 700,
                padding: '10px 22px',
                borderRadius: 12,
                border: '2px solid rgba(110,231,183,0.6)',
                color: '#A7F3D0',
              }}
            >
              {grade}
            </div>
          </div>
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 26, color: 'rgba(255,255,255,0.55)' }}>
          <div style={{ display: 'flex' }}>{asOf ? `As of ${asOf} · ${shortUrl}` : shortUrl}</div>
          <div style={{ display: 'flex', color: 'rgba(255,255,255,0.75)' }}>Receipts, not reels.</div>
        </div>
      </div>
    ),
    {
      ...SIZE,
      headers: { 'Cache-Control': 'public, max-age=3600, s-maxage=86400, stale-while-revalidate=86400' },
    }
  );
}
