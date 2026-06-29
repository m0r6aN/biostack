'use client';

import type {
  ProtocolIntelligenceRelationshipCard,
  ProtocolIntelligenceResponse,
  ProtocolIntelligenceUpgradeHook,
} from '@/lib/types';
import { HighRiskWarningPanel } from './HighRiskWarningPanel';
import { PhaseMapPanel } from './PhaseMapPanel';
import { EvidenceMeta, UnknownPanel } from './ProtocolIntelligenceShared';
import { SideEffectAmbiguityPanel } from './SideEffectAmbiguityPanel';
import { SourceQualityPanel } from './SourceQualityPanel';

interface ProtocolIntelligencePanelProps {
  intelligence: ProtocolIntelligenceResponse | null;
}

export function ProtocolIntelligencePanel({ intelligence }: ProtocolIntelligencePanelProps) {
  if (!intelligence) {
    return (
      <section className="rounded-lg border border-white/[0.08] bg-[#101820]/95 p-5">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Protocol Intelligence</p>
        <h2 className="mt-2 text-2xl font-black text-white">Unknown</h2>
        <p className="mt-2 text-sm text-white/55">No reviewed Protocol Intelligence artifact exists for this protocol context.</p>
      </section>
    );
  }

  return (
    <section className="space-y-4" aria-label="Protocol Intelligence">
      <div className="rounded-lg border border-white/[0.08] bg-[#101820]/95 p-5">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Protocol Intelligence</p>
        <h2 className="mt-2 text-2xl font-black text-white">{intelligence.status}</h2>
        <p className="mt-2 max-w-3xl text-sm leading-6 text-white/55">
          Reviewed relationship and source-quality context only. Unknown states are shown when no reviewed artifact exists.
        </p>
        {intelligence.unknowns.length > 0 && (
          <div className="mt-4 rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
            <p className="text-sm font-semibold text-white">Unknown state</p>
            {intelligence.unknowns.map((unknown) => (
              <p key={unknown} className="mt-1 text-sm text-white/55">{unknown}</p>
            ))}
          </div>
        )}
        {intelligence.safetyNotes.length > 0 && (
          <div className="mt-4 flex flex-wrap gap-2">
            {intelligence.safetyNotes.map((note) => (
              <span key={note} className="rounded-lg border border-white/[0.08] px-3 py-1.5 text-xs text-white/45">
                {note}
              </span>
            ))}
          </div>
        )}
      </div>

      {intelligence.highRiskWarnings.length > 0 && (
        <HighRiskWarningPanel warnings={intelligence.highRiskWarnings} />
      )}

      <PhaseMapPanel items={intelligence.phaseMap} />
      <RelationshipPanel relationships={intelligence.relationships} />
      <UpgradeHooks hooks={intelligence.upgradeHooks.filter((hook) => hook.requiredTier === 'Operator')} />
      <SideEffectAmbiguityPanel signals={intelligence.ambiguitySignals} />
      <UpgradeHooks hooks={intelligence.upgradeHooks.filter((hook) => hook.requiredTier === 'Commander')} />
      <SourceQualityPanel warnings={intelligence.sourceQualityWarnings} />
      <HighRiskWarningPanel warnings={intelligence.highRiskWarnings} />
    </section>
  );
}

function RelationshipPanel({ relationships }: { relationships: ProtocolIntelligenceRelationshipCard[] }) {
  if (relationships.length === 0) {
    return <UnknownPanel title="Evidence relationships" question="What reviewed relationships exist?" />;
  }

  return (
    <section className="rounded-lg border border-white/[0.08] bg-[#101820]/95 p-5">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Evidence relationships</p>
      <h3 className="mt-2 text-lg font-semibold text-white">What reviewed relationships exist?</h3>
      <div className="mt-4 space-y-3">
        {relationships.map((relationship) => (
          <article
            key={`${relationship.relationshipType}-${relationship.subject}-${relationship.object}`}
            className="rounded-lg border border-emerald-300/15 bg-emerald-400/[0.06] p-4"
          >
            <p className="font-semibold text-white">{relationship.subject} → {relationship.object}</p>
            <p className="mt-1 text-sm text-emerald-100/75">reviewed relationship: {relationship.relationshipType.replace(/_/g, ' ')}</p>
            <p className="mt-2 text-sm leading-6 text-white/60">{relationship.userFacingExplanation}</p>
            <EvidenceMeta {...relationship} />
          </article>
        ))}
      </div>
    </section>
  );
}

function UpgradeHooks({ hooks }: { hooks: ProtocolIntelligenceUpgradeHook[] }) {
  if (hooks.length === 0) {
    return null;
  }

  return (
    <div className="rounded-lg border border-amber-300/15 bg-amber-400/[0.06] p-4">
      {hooks.map((hook) => (
        <p key={hook.featureCode} className="text-sm font-semibold text-amber-100">{hook.message}</p>
      ))}
    </div>
  );
}
