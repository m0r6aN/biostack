import type { ProtocolIntelligenceHighRiskWarning } from '@/lib/types';
import { EvidenceMeta, UnknownPanel } from './ProtocolIntelligenceShared';

export function HighRiskWarningPanel({ warnings }: { warnings: ProtocolIntelligenceHighRiskWarning[] }) {
  if (warnings.length === 0) {
    return <UnknownPanel title="High-risk warnings" question="What must stay warning-first or blocked?" />;
  }

  return (
    <section className="rounded-lg border border-rose-300/20 bg-rose-500/[0.08] p-5">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-rose-100/70">High-risk warnings</p>
      <h3 className="mt-2 text-lg font-semibold text-white">What must stay warning-first or blocked?</h3>
      <div className="mt-4 space-y-3">
        {warnings.map((warning) => (
          <article key={warning.category} className="rounded-lg border border-rose-200/15 bg-rose-500/[0.08] p-4">
            <p className="font-semibold text-white">{warning.category.replace(/_/g, ' ')}</p>
            <p className="mt-2 text-sm text-rose-100/75">{warning.userFacingBoundary}</p>
            {warning.requiredWarnings.length > 0 && (
              <p className="mt-2 text-xs text-white/50">Required warnings: {warning.requiredWarnings.join(', ')}</p>
            )}
            <EvidenceMeta {...warning} />
          </article>
        ))}
      </div>
    </section>
  );
}
