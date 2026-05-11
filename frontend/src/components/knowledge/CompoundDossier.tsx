import { cn } from '@/lib/utils';
import { EvidenceTierBadge } from '@/components/intel/EvidenceTierBadge';
import { TrustLedgerResponse } from '@/lib/types';
import { ClaimInspector } from './ClaimInspector';
import { RegulatoryBoundaryPanel } from './RegulatoryBoundaryPanel';
import { TrustTimeline } from './TrustTimeline';

interface CompoundDossierProps {
  data: TrustLedgerResponse;
}

const COMPLETENESS_STYLES: Record<string, { bg: string; text: string; border: string; label: string }> = {
  complete: {
    bg: 'bg-emerald-500/10',
    text: 'text-emerald-300',
    border: 'border-emerald-400/20',
    label: 'Complete',
  },
  partial: {
    bg: 'bg-amber-500/10',
    text: 'text-amber-300',
    border: 'border-amber-400/20',
    label: 'Partial',
  },
  minimal: {
    bg: 'bg-rose-500/10',
    text: 'text-rose-300',
    border: 'border-rose-400/20',
    label: 'Minimal',
  },
};

export function CompoundDossier({ data }: CompoundDossierProps) {
  const completeness = COMPLETENESS_STYLES[data.completeness] ?? COMPLETENESS_STYLES.minimal;
  const isReviewGated = data.status === 'review-gated';
  const hasResearchOps = data.requiredNextActions.length > 0 || data.conflicts.length > 0;

  return (
    <div className="max-w-3xl mx-auto space-y-6">

      {/* ── Section 1: Identity ── */}
      <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-6 space-y-3">
        <div className="flex items-start justify-between gap-4 flex-wrap">
          <div>
            <h1 className="text-2xl font-semibold text-white">{data.canonicalName}</h1>
            <p className="font-mono text-xs text-white/35 mt-1">{data.slug}</p>
          </div>
          <EvidenceTierBadge tier={data.evidenceTier} />
        </div>
      </div>

      {/* ── Section 2: Trust State ── */}
      <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-6 space-y-4">
        <p className="text-[10px] font-bold text-white/30 uppercase tracking-widest">Trust State</p>

        <div className="flex flex-wrap items-center gap-3">
          <span
            className={cn(
              'inline-flex items-center text-[11px] font-medium px-2.5 py-1 rounded-full border',
              completeness.bg,
              completeness.text,
              completeness.border,
            )}
          >
            {completeness.label}
          </span>

          {data.needsReview && (
            <span className="inline-flex items-center gap-1.5 text-[11px] font-medium px-2.5 py-1 rounded-full border border-amber-400/20 bg-amber-500/10 text-amber-300">
              <span className="w-1.5 h-1.5 rounded-full bg-amber-400 shrink-0" />
              Review Required
            </span>
          )}
        </div>

        {data.qualityFlags.length > 0 && (
          <div className="flex flex-wrap gap-1.5">
            {data.qualityFlags.map((flag, i) => (
              <span
                key={i}
                className="text-[11px] px-2 py-0.5 rounded-full bg-slate-500/15 text-slate-300/70 border border-slate-400/15"
              >
                {flag}
              </span>
            ))}
          </div>
        )}

        <TrustTimeline
          completeness={data.completeness}
          needsReview={data.needsReview}
          status={data.status}
          qualityFlags={[]}
        />
      </div>

      {/* ── Section 3: Regulatory Boundary ── */}
      <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-6 space-y-3">
        <p className="text-[10px] font-bold text-white/30 uppercase tracking-widest">Regulatory Boundary</p>
        <RegulatoryBoundaryPanel
          boundary={data.regulatoryBoundary}
          promotionBlockers={data.promotionBlockers}
        />
      </div>

      {/* ── Section 4: Claim Inspector (review-gated gate) ── */}
      <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-6 space-y-3">
        <p className="text-[10px] font-bold text-white/30 uppercase tracking-widest">Claim Inspector</p>

        {isReviewGated ? (
          <div className="rounded-xl border border-amber-400/25 bg-amber-500/[0.07] p-5 space-y-3">
            <p className="text-sm font-semibold text-amber-300">This compound is under review.</p>
            <p className="text-sm text-amber-200/70">
              Claims are not yet public. Here is why:
            </p>
            {data.promotionBlockers.length > 0 && (
              <ul className="space-y-1.5 pt-1">
                {data.promotionBlockers.map((blocker, i) => (
                  <li key={i} className="flex items-start gap-2 text-sm text-amber-200/60">
                    <span className="text-amber-400 shrink-0 mt-0.5">·</span>
                    {blocker}
                  </li>
                ))}
              </ul>
            )}
          </div>
        ) : (
          <ClaimInspector claims={data.claims} />
        )}
      </div>

      {/* ── Section 5: Research Ops ── */}
      {hasResearchOps && (
        <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-6 space-y-5">
          <p className="text-[10px] font-bold text-white/30 uppercase tracking-widest">Research Ops</p>

          {data.requiredNextActions.length > 0 && (
            <div>
              <p className="text-xs font-semibold text-white/50 uppercase tracking-wider mb-2">Required Actions</p>
              <ul className="space-y-2">
                {data.requiredNextActions.map((action, i) => (
                  <li key={i} className="flex items-start gap-2 text-sm text-white/65">
                    <span className="text-emerald-400 shrink-0 mt-1">→</span>
                    {action}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {data.conflicts.length > 0 && (
            <div>
              <p className="text-xs font-semibold text-rose-400/70 uppercase tracking-wider mb-2">Conflicts</p>
              <ul className="space-y-2">
                {data.conflicts.map((conflict, i) => (
                  <li key={i} className="flex items-start gap-2 text-sm text-rose-200/60">
                    <span className="text-rose-400 shrink-0 mt-0.5">·</span>
                    {conflict}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
