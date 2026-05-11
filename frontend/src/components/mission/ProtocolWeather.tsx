'use client';

import { DRIFT_TOKENS } from '@/styles/tokens';
import { WhyDrawer } from '@/components/intel/WhyDrawer';
import { cn } from '@/lib/utils';
import type { ProtocolDriftSnapshot } from '@/lib/types';

interface ProtocolWeatherProps {
  driftSnapshot: ProtocolDriftSnapshot | null;
  className?: string;
}

export function ProtocolWeather({ driftSnapshot, className }: ProtocolWeatherProps) {
  const state = driftSnapshot?.driftState ?? 'unknown';
  const t = DRIFT_TOKENS[state.toLowerCase()] ?? DRIFT_TOKENS.unknown;
  const signals = driftSnapshot?.signals ?? [];
  const classification = driftSnapshot?.regimeClassification;

  const topFactors = [
    ...(classification?.contributingFactors ?? []),
    ...signals.slice(0, 2).map((s) => s.description),
  ].slice(0, 3);

  const whyInputs = [
    { label: 'Drift state', value: driftSnapshot?.driftState ?? 'unknown' },
    { label: 'Baseline source', value: driftSnapshot?.baselineSource ?? 'unknown' },
    { label: 'Signals detected', value: signals.length },
    { label: 'Regime', value: classification?.state ?? 'unknown' },
  ];

  const reasoning = driftSnapshot
    ? `Drift state "${state}" was derived from ${signals.length} drift signal${signals.length !== 1 ? 's' : ''} against a "${driftSnapshot.baselineSource}" baseline. ${classification ? `Regime classification: ${classification.state}.` : ''}`
    : 'No drift data is available. Drift analysis requires at least one completed run for baseline comparison.';

  return (
    <div className={cn('rounded-3xl border bg-white/[0.02] p-5', t.border, className)}>
      <div className="flex items-center justify-between mb-4">
        <p className="text-[10px] font-bold text-white/20 uppercase tracking-widest">Protocol Weather</p>
        <WhyDrawer
          surface="Protocol Weather"
          title="How is drift state calculated?"
          inputs={whyInputs}
          reasoning={reasoning}
          caveats={['Drift reflects timing and cadence patterns, not biomarker outcomes.']}
        />
      </div>

      <div className="flex items-center gap-3 mb-4">
        <span className={cn('text-2xl leading-none', t.color)}>{t.emoji}</span>
        <div>
          <p className={cn('text-sm font-bold', t.color)}>{t.label}</p>
          {driftSnapshot?.baselineSource === 'insufficient_history' && (
            <p className="text-[10px] text-white/30 mt-0.5">Building baseline…</p>
          )}
        </div>
      </div>

      {topFactors.length > 0 ? (
        <div className="space-y-1.5">
          {topFactors.map((factor, i) => (
            <div key={i} className="flex items-start gap-2">
              <span className="text-white/20 text-[10px] mt-0.5 shrink-0">·</span>
              <span className="text-[11px] text-white/50 leading-snug">{factor}</span>
            </div>
          ))}
        </div>
      ) : (
        <p className="text-[11px] text-white/30 italic">No contributing factors detected.</p>
      )}
    </div>
  );
}
