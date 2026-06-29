import type { ProtocolIntelligenceAmbiguitySignal } from '@/lib/types';
import { EvidenceMeta, UnknownPanel } from './ProtocolIntelligenceShared';

export function SideEffectAmbiguityPanel({ signals }: { signals: ProtocolIntelligenceAmbiguitySignal[] }) {
  if (signals.length === 0) {
    return <UnknownPanel title="Side-effect ambiguity" question="What changed before this outcome?" />;
  }

  return (
    <section className="rounded-lg border border-white/[0.08] bg-[#101820]/95 p-5">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Side-effect ambiguity</p>
      <h3 className="mt-2 text-lg font-semibold text-white">What changed before this outcome?</h3>
      <div className="mt-4 space-y-3">
        {signals.map((signal) => (
          <article key={`${signal.symptomOrOutcome}-${signal.onsetWindow}`} className="rounded-lg border border-fuchsia-300/15 bg-fuchsia-400/[0.06] p-4">
            <p className="font-semibold text-white">{signal.symptomOrOutcome}</p>
            <p className="mt-1 text-sm text-white/55">{signal.onsetWindow}</p>
            <p className="mt-2 text-xs text-white/45">Recent changes: {signal.recentChanges.join(', ') || 'Unknown'}</p>
            <p className="mt-1 text-xs text-white/45">Overlap domains: {signal.overlapDomains.join(', ') || 'Unknown'}</p>
            <EvidenceMeta {...signal} />
          </article>
        ))}
      </div>
    </section>
  );
}
