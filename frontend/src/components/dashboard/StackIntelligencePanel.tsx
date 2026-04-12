import { CurrentStackIntelligence, StackSignal } from '@/lib/types';

interface StackIntelligencePanelProps {
  intelligence: CurrentStackIntelligence | null;
}

const signalStyles: Record<string, string> = {
  caution: 'border-amber-400/25 bg-amber-500/10 text-amber-100',
  positive: 'border-emerald-400/20 bg-emerald-500/10 text-emerald-100',
  neutral: 'border-sky-400/20 bg-sky-500/10 text-sky-100',
};

const signalLabels: Record<string, string> = {
  avoid_with: 'Caution',
  drug_interaction: 'Interaction note',
  pathway_overlap: 'Overlap',
  compatible_blend: 'Blend note',
  vial_compatibility: 'Compatibility',
  pairs_well_with: 'Pairing signal',
};

export function StackIntelligencePanel({ intelligence }: StackIntelligencePanelProps) {
  if (!intelligence || intelligence.activeCompounds.length === 0) {
    return null;
  }

  const topSignals = intelligence.signals.slice(0, 6);
  const schedulePreview = intelligence.activeCompounds.find((compound) => compound.schedulePreview)?.schedulePreview;

  return (
    <div className="p-5 bg-[#121923]/90 border border-white/[0.08] rounded-2xl shadow-[0_8px_24px_rgba(0,0,0,0.35)]">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold text-white">Current Stack Intelligence</h2>
          <p className="mt-1 text-sm text-white/45">
            Educational signals from linked knowledge entries and active compound timing.
          </p>
        </div>
        <div className="rounded-lg border border-white/[0.08] bg-white/[0.03] px-3 py-2 text-xs text-white/55">
          {intelligence.activeCompounds.filter((compound) => compound.isCanonical).length} canonical · {intelligence.unresolvedCompounds.length} manual
        </div>
      </div>

      {topSignals.length > 0 ? (
        <div className="mt-5 grid grid-cols-1 gap-3 lg:grid-cols-2">
          {topSignals.map((signal) => (
            <SignalCard key={`${signal.kind}-${signal.title}-${signal.detail}`} signal={signal} />
          ))}
        </div>
      ) : (
        <div className="mt-5 rounded-lg border border-white/[0.08] bg-white/[0.03] p-4 text-sm text-white/55">
          No stack-level cautions or overlaps found for the currently linked compounds.
        </div>
      )}

      <div className="mt-5 grid grid-cols-1 gap-3 md:grid-cols-3">
        <div className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
          <p className="text-xs uppercase tracking-[0.12em] text-white/35">Evidence</p>
          <div className="mt-2 space-y-1">
            {intelligence.evidenceTierSummary.length > 0 ? (
              intelligence.evidenceTierSummary.map((summary) => (
                <p key={summary.evidenceTier} className="text-sm text-white/70">
                  {summary.count} {summary.evidenceTier}
                </p>
              ))
            ) : (
              <p className="text-sm text-white/45">No linked evidence tiers yet.</p>
            )}
          </div>
        </div>

        <div className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
          <p className="text-xs uppercase tracking-[0.12em] text-white/35">Reference Schedule</p>
          {schedulePreview ? (
            <p className="mt-2 text-sm text-white/70">
              {schedulePreview.frequency || 'Frequency not listed'}
              {schedulePreview.preferredTimeOfDay ? ` · ${schedulePreview.preferredTimeOfDay}` : ''}
            </p>
          ) : (
            <p className="mt-2 text-sm text-white/45">No knowledge-base schedule snippet available.</p>
          )}
        </div>

        <div className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
          <p className="text-xs uppercase tracking-[0.12em] text-white/35">Manual Entries</p>
          <p className="mt-2 text-sm text-white/70">
            {intelligence.unresolvedCompounds.length === 0
              ? 'All active compounds are linked.'
              : `${intelligence.unresolvedCompounds.length} active compound cannot be enriched yet.`}
          </p>
        </div>
      </div>
    </div>
  );
}

function SignalCard({ signal }: { signal: StackSignal }) {
  const style = signalStyles[signal.severity] ?? signalStyles.neutral;
  return (
    <div className={`rounded-lg border p-4 ${style}`}>
      <div className="flex flex-wrap items-center gap-2">
        <span className="rounded-md border border-current/20 px-2 py-1 text-[11px] font-semibold uppercase tracking-[0.08em]">
          {signalLabels[signal.kind] ?? signal.kind}
        </span>
        <span className="text-xs opacity-70">{signal.source}</span>
      </div>
      <h3 className="mt-3 text-sm font-semibold">{signal.title}</h3>
      <p className="mt-1 text-sm opacity-75">{signal.detail}</p>
      {signal.compoundNames.length > 0 && (
        <p className="mt-3 text-xs opacity-60">{signal.compoundNames.join(' + ')}</p>
      )}
    </div>
  );
}
