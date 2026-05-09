'use client';
import { useState } from 'react';
import { EvidenceTierBadge } from '@/components/knowledge/EvidenceTierBadge';
import type { EvidenceClaim } from '@/lib/research/types';

interface ClaimCardProps { claim: EvidenceClaim }

export function ClaimCard({ claim }: ClaimCardProps) {
  const [showEvidence, setShowEvidence] = useState(false);

  return (
    <div className="rounded-xl border border-white/[0.08] bg-white/[0.025] px-3 py-2.5 flex flex-col gap-2">
      <div className="flex items-center justify-between gap-2">
        <span className="text-[9px] font-bold tracking-widest uppercase text-white/40">{claim.claimType}</span>
        <EvidenceTierBadge tier={claim.evidenceTier} variant="research" />
      </div>
      <p className="text-[12px] text-white/90 leading-relaxed">{claim.statement}</p>
      <div className="grid grid-cols-2 gap-x-4 gap-y-0.5">
        {[
          { label: 'Population', value: claim.context.population },
          { label: 'Route',      value: claim.context.route },
          { label: 'Formulation',value: claim.context.formulation },
          { label: 'Use Case',   value: claim.context.useCase },
          { label: 'Dose',       value: claim.context.doseText },
        ].map(({ label, value }) => (
          <span key={label} className="text-[10px] text-white/35">
            {label}: <span className="text-white/60">{value ?? '—'}</span>
          </span>
        ))}
      </div>
      <div className="flex flex-wrap gap-1.5">
        {claim.fieldAuthorityRequired && (
          <span className="text-[9px] px-2 py-0.5 rounded-full bg-rose-500/15 border border-rose-400/25 text-rose-300">
            Field Authority Required
          </span>
        )}
        {claim.reviewFlags.map(flag => (
          <span key={flag} className="text-[9px] px-2 py-0.5 rounded-full bg-white/[0.05] border border-white/10 text-white/40">{flag}</span>
        ))}
      </div>
      {claim.extractedEvidence.length > 0 && (
        <div>
          <button onClick={() => setShowEvidence(v => !v)}
            className="text-[10px] text-white/30 hover:text-white/60 transition-colors">
            {showEvidence ? 'Hide evidence' : `Show evidence (${claim.extractedEvidence.length})`}
          </button>
          {showEvidence && (
            <div className="mt-2 flex flex-col gap-2">
              {claim.extractedEvidence.map((ev, i) => (
                <div key={i} className="rounded-lg bg-white/[0.03] border border-white/[0.06] px-2.5 py-2">
                  {ev.quote && <p className="text-[11px] text-white/70 italic">"{ev.quote}"</p>}
                  <p className="text-[10px] text-white/35 mt-1">{ev.sourceRef}{ev.pageOrSection && ` · ${ev.pageOrSection}`}</p>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
