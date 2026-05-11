'use client';

import { useState } from 'react';
import { DeltaBadge } from '@/components/intel/DeltaBadge';
import { ConfidenceChip } from '@/components/intel/ConfidenceChip';
import { cn } from '@/lib/utils';
import { track } from '@/lib/telemetry';
import type { InteractionIntelligence } from '@/lib/types';

interface CounterfactualLabProps {
  intelligence: InteractionIntelligence | null;
  onHighlightCompound?: (name: string | null) => void;
  className?: string;
}

type TabKey = 'remove' | 'swap' | 'simplified';

export function CounterfactualLab({ intelligence, onHighlightCompound, className }: CounterfactualLabProps) {
  const [tab, setTab] = useState<TabKey>('remove');
  const [savedDraft, setSavedDraft] = useState<string | null>(null);

  if (!intelligence) {
    return (
      <div className={cn('rounded-3xl border border-white/5 bg-white/[0.02] p-5', className)}>
        <p className="text-[10px] font-bold text-white/20 uppercase tracking-widest mb-3">Counterfactual Lab</p>
        <p className="text-sm text-white/40">Stack intelligence data is required to run counterfactual variants.</p>
      </div>
    );
  }

  const { counterfactuals = [], swaps = [], compositeScore } = intelligence;

  // Best removal variant
  const bestRemoval = [...counterfactuals].sort((a, b) => b.deltaScore - a.deltaScore)[0];
  // Best swap
  const bestSwap = [...swaps].filter((s) => s.verdict === 'likely_improves').sort((a, b) => b.deltaScore - a.deltaScore)[0];

  const tabs: Array<{ key: TabKey; label: string; count: number }> = [
    { key: 'remove', label: 'Remove', count: counterfactuals.length },
    { key: 'swap', label: 'Swap', count: swaps.length },
    { key: 'simplified', label: 'Simplified', count: bestRemoval ? 1 : 0 },
  ];

  function saveAsDraft(compoundName: string) {
    setSavedDraft(compoundName);
    track({ name: 'counterfactual_variant_save', variantType: tab });
  }

  return (
    <div className={cn('rounded-3xl border border-white/8 bg-white/[0.02]', className)}>
      {/* Header */}
      <div className="px-5 pt-5 pb-3">
        <p className="text-[10px] font-bold text-white/20 uppercase tracking-widest mb-1">Counterfactual Lab</p>
        <p className="text-xs text-white/40">Explore what the stack would look like with different configurations. These are not recommendations.</p>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 px-5 pb-3">
        {tabs.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={cn(
              'flex items-center gap-1.5 text-[11px] font-semibold px-3 py-1.5 rounded-xl border transition-all',
              tab === t.key
                ? 'bg-white/5 border-white/15 text-white/80'
                : 'border-transparent text-white/35 hover:text-white/55',
            )}
          >
            {t.label}
            {t.count > 0 && (
              <span className="text-[9px] font-bold text-white/30 bg-white/5 rounded-full w-4 h-4 flex items-center justify-center">
                {t.count}
              </span>
            )}
          </button>
        ))}
      </div>

      <div className="px-4 pb-4 space-y-2">
        {tab === 'remove' && (
          <>
            {counterfactuals.length === 0 ? (
              <p className="text-xs text-white/30 px-1 py-3">No removal variants available. Add more compounds to enable counterfactual analysis.</p>
            ) : (
              counterfactuals.map((cf) => (
                <VariantCard
                  key={cf.removedCompound}
                  title={`Remove ${cf.removedCompound}`}
                  description={cf.recommendation}
                  verdict={cf.verdict}
                  delta={cf.deltaScore}
                  baseScore={compositeScore}
                  variantScore={cf.variantScore}
                  findings={cf.topFindings?.map((f) => f.message) ?? []}
                  onHover={() => onHighlightCompound?.(cf.removedCompound)}
                  onLeave={() => onHighlightCompound?.(null)}
                  onSaveDraft={() => saveAsDraft(cf.removedCompound)}
                  savedDraft={savedDraft === cf.removedCompound}
                />
              ))
            )}
          </>
        )}

        {tab === 'swap' && (
          <>
            {swaps.length === 0 ? (
              <p className="text-xs text-white/30 px-1 py-3">No swap candidates found.</p>
            ) : (
              swaps.slice(0, 5).map((sw, i) => (
                <VariantCard
                  key={`${sw.originalCompound}-${sw.candidateCompound}-${i}`}
                  title={`Swap ${sw.originalCompound} → ${sw.candidateCompound}`}
                  description={sw.recommendation}
                  verdict={sw.verdict}
                  delta={sw.deltaScore}
                  baseScore={sw.baselineScore}
                  variantScore={sw.variantScore}
                  findings={sw.reasons}
                  onHover={() => onHighlightCompound?.(sw.originalCompound)}
                  onLeave={() => onHighlightCompound?.(null)}
                  onSaveDraft={() => saveAsDraft(`${sw.originalCompound}→${sw.candidateCompound}`)}
                  savedDraft={savedDraft === `${sw.originalCompound}→${sw.candidateCompound}`}
                />
              ))
            )}
          </>
        )}

        {tab === 'simplified' && (
          <>
            {!bestRemoval ? (
              <p className="text-xs text-white/30 px-1 py-3">Simplified variant requires at least one removal counterfactual.</p>
            ) : (
              <VariantCard
                title={`Simplified: Remove ${bestRemoval.removedCompound}`}
                description={`Best single-compound removal for signal clarity. ${bestRemoval.recommendation}`}
                verdict={bestRemoval.verdict}
                delta={bestRemoval.deltaScore}
                baseScore={compositeScore}
                variantScore={bestRemoval.variantScore}
                findings={bestRemoval.topFindings?.map((f) => f.message) ?? []}
                onHover={() => onHighlightCompound?.(bestRemoval.removedCompound)}
                onLeave={() => onHighlightCompound?.(null)}
                onSaveDraft={() => saveAsDraft(`simplified-${bestRemoval.removedCompound}`)}
                savedDraft={savedDraft === `simplified-${bestRemoval.removedCompound}`}
              />
            )}
          </>
        )}
      </div>

      {/* Disclaimer */}
      <div className="mx-4 mb-4 px-3 py-2.5 rounded-2xl border border-white/5 bg-white/[0.02]">
        <p className="text-[10px] text-white/25 leading-relaxed">
          These variants are observational simulations based on existing interaction data. They are not recommendations. Consult a healthcare provider before making any protocol changes.
        </p>
      </div>
    </div>
  );
}

function VariantCard({
  title, description, verdict, delta, baseScore, variantScore, findings,
  onHover, onLeave, onSaveDraft, savedDraft,
}: {
  title: string;
  description: string;
  verdict: string;
  delta: number;
  baseScore: number;
  variantScore: number;
  findings: string[];
  onHover: () => void;
  onLeave: () => void;
  onSaveDraft: () => void;
  savedDraft: boolean;
}) {
  const positive = verdict.includes('improves') || verdict.includes('improve');
  const negative = verdict.includes('worsen');

  return (
    <div
      className="rounded-2xl border border-white/5 bg-white/[0.02] p-4 hover:bg-white/[0.04] transition-colors"
      onMouseEnter={onHover}
      onMouseLeave={onLeave}
    >
      <div className="flex items-start justify-between gap-3 mb-2">
        <p className="text-xs font-semibold text-white/85 leading-snug">{title}</p>
        <DeltaBadge value={delta} unit="pts" className="shrink-0" />
      </div>

      <div className="flex items-center gap-2 mb-2">
        <span className="text-[11px] font-mono text-white/40">{baseScore.toFixed(0)} → {variantScore.toFixed(0)}</span>
        {positive && <span className="text-[10px] text-emerald-400/70 font-medium">↑ improves</span>}
        {negative && <span className="text-[10px] text-rose-400/70 font-medium">↓ worsens</span>}
      </div>

      <p className="text-[11px] text-white/50 leading-relaxed mb-2">{description}</p>

      {findings.slice(0, 2).map((f, i) => (
        <div key={i} className="flex items-start gap-1.5 text-[11px] text-white/35">
          <span className="mt-0.5 shrink-0">·</span>
          <span className="leading-snug">{f}</span>
        </div>
      ))}

      <div className="mt-3 flex justify-end">
        {savedDraft ? (
          <span className="text-[11px] text-emerald-400/70 font-medium">✓ Saved as draft</span>
        ) : (
          <button
            onClick={onSaveDraft}
            className="text-[11px] font-semibold text-white/40 hover:text-white/70 transition-colors"
          >
            Save as Protocol Draft
          </button>
        )}
      </div>
    </div>
  );
}
