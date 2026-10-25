import type { CardModel } from '@/lib/verdictCard';

/** Verdict chip colours + an ASCII mark (the default card font has no symbol glyphs); the label carries the verdict without colour (06 §H.6). */
export const CHIP_STYLE: Record<string, { color: string; border: string; icon: string }> = {
  'PROVEN IN HUMANS': { color: '#A7F3D0', border: 'rgba(110,231,183,0.7)', icon: '+' },
  PROMISING: { color: '#BAE6FD', border: 'rgba(125,211,252,0.7)', icon: '^' },
  'ANIMAL ONLY': { color: '#FDE68A', border: 'rgba(252,211,77,0.7)', icon: '~' },
  THIN: { color: '#E5E7EB', border: 'rgba(229,231,235,0.6)', icon: '-' },
  MYTH: { color: '#FECACA', border: 'rgba(248,113,113,0.75)', icon: 'x' },
  'NOT YET GRADED': { color: 'rgba(255,255,255,0.75)', border: 'rgba(255,255,255,0.35)', icon: '?' },
};

/** Satori-compatible JSX for a verdict or claim card (04-card-templates.md). */
export function VerdictCardImage({ model }: { model: CardModel }) {
  const story = model.format === 'story';
  const chip = CHIP_STYLE[model.verdict] ?? CHIP_STYLE['NOT YET GRADED'];
  const pad = story ? '250px 80px 340px' : '56px 72px';
  return (
    <div style={{
      width: '100%', height: '100%', display: 'flex', flexDirection: 'column', justifyContent: 'space-between',
      padding: pad, background: '#0B1117', color: '#FFFFFF', fontFamily: 'sans-serif', position: 'relative',
    }}>
      {model.superseded && (
        <div style={{ position: 'absolute', bottom: story ? 260 : 100, right: 72, display: 'flex', fontSize: 24, fontWeight: 700,
          padding: '6px 16px', background: '#7C2D12', color: '#FFEDD5', borderRadius: 8 }}>
          Superseded · current verdict shown
        </div>
      )}
      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 28, fontWeight: 700, letterSpacing: 2 }}>
        <div style={{ display: 'flex', color: '#6EE7B7' }}>BIOSTACK</div>
        <div style={{ display: 'flex', color: 'rgba(255,255,255,0.8)' }}>{model.tagline.toUpperCase()}</div>
      </div>
      <div style={{ display: 'flex', flexDirection: 'column' }}>
        {model.kicker && <div style={{ display: 'flex', fontSize: 24, color: 'rgba(255,255,255,0.55)', letterSpacing: 3 }}>{model.kicker}</div>}
        <div style={{ display: 'flex', fontSize: story ? 84 : 64, fontWeight: 800, lineHeight: 1.05 }}>{model.compound}</div>
        <div style={{ display: 'flex', fontSize: story ? 48 : 36, marginTop: 14, color: 'rgba(255,255,255,0.88)' }}>{model.claimLine}</div>
        {model.kind === 'claim' && <div style={{ display: 'flex', fontSize: 24, marginTop: 26, color: 'rgba(255,255,255,0.55)', letterSpacing: 3 }}>THE RECEIPTS</div>}
        <div style={{ display: 'flex', marginTop: story ? 60 : 22 }}>
          <div style={{ display: 'flex', fontSize: story ? 64 : 44, fontWeight: 800, padding: '10px 24px', borderRadius: 14,
            border: `3px solid ${chip.border}`, color: chip.color }}>
            {`${chip.icon} ${model.verdict}`}
          </div>
        </div>
        {model.qualifier && <div style={{ display: 'flex', fontSize: story ? 36 : 26, marginTop: 16, color: '#A7F3D0' }}>{model.qualifier}</div>}
        {model.receiptLine && <div style={{ display: 'flex', fontSize: story ? 34 : 26, marginTop: 14, color: 'rgba(255,255,255,0.75)' }}>{model.receiptLine}</div>}
        {model.hypeLine && <div style={{ display: 'flex', fontSize: story ? 38 : 28, marginTop: 16, fontWeight: 700 }}>{model.hypeLine}</div>}
      </div>
      <div style={{ display: 'flex', fontSize: story ? 30 : 24, color: 'rgba(255,255,255,0.6)' }}>{model.footer}</div>
    </div>
  );
}
