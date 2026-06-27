import type { ProtocolIntelligencePhaseMapItem } from '@/lib/types';
import { EvidenceMeta, UnknownPanel } from './ProtocolIntelligenceShared';

export function PhaseMapPanel({ items }: { items: ProtocolIntelligencePhaseMapItem[] }) {
  if (items.length === 0) {
    return <UnknownPanel title="Phase map" question="What phase is this event in?" />;
  }

  return (
    <section className="rounded-lg border border-white/[0.08] bg-[#101820]/95 p-5">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Phase map</p>
      <h3 className="mt-2 text-lg font-semibold text-white">What phase is this event in?</h3>
      <div className="mt-4 space-y-3">
        {items.map((item) => (
          <article key={`${item.phase}-${item.label}`} className="rounded-lg border border-sky-300/15 bg-sky-400/[0.06] p-4">
            <p className="font-semibold text-white">{item.label}</p>
            <p className="mt-1 text-sm text-white/55">{item.phase}</p>
            <EvidenceMeta {...item} />
          </article>
        ))}
      </div>
    </section>
  );
}
