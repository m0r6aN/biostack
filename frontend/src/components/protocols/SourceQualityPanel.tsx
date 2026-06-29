import type { ProtocolIntelligenceSourceQualityWarning } from '@/lib/types';
import { EvidenceMeta, UnknownPanel } from './ProtocolIntelligenceShared';

export function SourceQualityPanel({ warnings }: { warnings: ProtocolIntelligenceSourceQualityWarning[] }) {
  if (warnings.length === 0) {
    return <UnknownPanel title="Source quality" question="What is uncertain about identity, label, source, or regulatory status?" />;
  }

  return (
    <section className="rounded-lg border border-white/[0.08] bg-[#101820]/95 p-5">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Source quality</p>
      <h3 className="mt-2 text-lg font-semibold text-white">What is uncertain about identity, label, source, or regulatory status?</h3>
      <div className="mt-4 space-y-3">
        {warnings.map((warning) => (
          <article key={`${warning.subject}-${warning.sourceClass}`} className="rounded-lg border border-amber-300/15 bg-amber-400/[0.06] p-4">
            <p className="font-semibold text-white">{warning.subject}</p>
            <p className="mt-1 text-sm text-amber-100/75">source-quality warning: {warning.sourceClass.replace(/_/g, ' ')}</p>
            {warning.blockedOutputs.length > 0 && (
              <p className="mt-2 text-xs text-white/45">Blocked outputs: {warning.blockedOutputs.join(', ')}</p>
            )}
            <EvidenceMeta {...warning} />
          </article>
        ))}
      </div>
    </section>
  );
}
