'use client';

import { diffCategoryTaxonomyAuditEntry, hasCategoryTaxonomyAuditDiff } from '@/lib/research/categoryTaxonomyDiff';
import type { ResearchCategoryTaxonomyAuditEntry } from '@/lib/research/types';
import Link from 'next/link';
import { useMemo } from 'react';

export function TaxonomyAuditEntryCard({
  entry,
  timelineHref,
  focused = false,
}: {
  entry: ResearchCategoryTaxonomyAuditEntry;
  timelineHref?: string;
  focused?: boolean;
}) {
  const diff = useMemo(() => diffCategoryTaxonomyAuditEntry(entry), [entry]);

  return (
    <div id={entry.entryId} className={`rounded-xl border bg-white/[0.03] px-4 py-3 text-sm text-white/80 ${focused ? 'border-violet-300/40 shadow-[0_0_0_1px_rgba(196,181,253,0.25)]' : 'border-white/10'}`}>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="font-semibold text-white">{entry.summary}</p>
        <div className="flex items-center gap-3">
          <span className="rounded-full border border-white/10 bg-black/20 px-2.5 py-1 text-[10px] uppercase tracking-widest text-white/45">{entry.action}</span>
          {timelineHref && (
            <Link href={timelineHref} className="text-[11px] font-semibold text-violet-200/80 transition-colors hover:text-violet-100">
              Open in timeline →
            </Link>
          )}
        </div>
      </div>
      <p className="mt-2 text-[11px] text-white/50">
        {new Date(entry.createdAtUtc).toLocaleString()} · taxonomy {entry.taxonomyVersion}
      </p>
      {entry.applyReceipt && (
        <p className="mt-1 text-[11px] text-white/45">
          Files updated {entry.applyReceipt.counts.totalFilesUpdated} · categories rewritten {entry.applyReceipt.counts.categoriesRewritten}
        </p>
      )}
      {entry.beforeTaxonomy && entry.afterTaxonomy && entry.action === 'save-taxonomy' && (
        <p className="mt-1 text-[11px] text-white/45">
          Categories {entry.beforeTaxonomy.categories.length} → {entry.afterTaxonomy.categories.length}
        </p>
      )}
      {hasCategoryTaxonomyAuditDiff(diff) && (
        <div className="mt-3 space-y-2">
          <div className="flex flex-wrap gap-2">
            {diff.added.map((name) => <AuditDiffPill key={`added-${entry.entryId}-${name}`} tone="emerald" label={`Added: ${name}`} />)}
            {diff.removed.map((name) => <AuditDiffPill key={`removed-${entry.entryId}-${name}`} tone="rose" label={`Removed: ${name}`} />)}
            {diff.renamed.map((change) => <AuditDiffPill key={`renamed-${entry.entryId}-${change.before}-${change.after}`} tone="sky" label={`Renamed: ${change.before} → ${change.after}`} />)}
            {diff.deprecated.map((name) => <AuditDiffPill key={`deprecated-${entry.entryId}-${name}`} tone="amber" label={`Deprecated: ${name}`} />)}
            {diff.restored.map((name) => <AuditDiffPill key={`restored-${entry.entryId}-${name}`} tone="emerald" label={`Restored: ${name}`} />)}
            {diff.replacementChanged.map((change) => <AuditDiffPill key={`replacement-${entry.entryId}-${change.name}`} tone="violet" label={`Replacement: ${change.name} · ${change.before} → ${change.after}`} />)}
            {diff.aliasChanged.map((name) => <AuditDiffPill key={`aliases-${entry.entryId}-${name}`} tone="neutral" label={`Aliases changed: ${name}`} />)}
          </div>
          {diff.migrationDelta && (
            <p className="text-[11px] text-white/45">
              Migration impact Δ total {formatSignedDelta(diff.migrationDelta.totalFindings)} · requests {formatSignedDelta(diff.migrationDelta.requestFindings)} · queued {formatSignedDelta(diff.migrationDelta.taskItemFindings)} · resolved {formatSignedDelta(diff.migrationDelta.resolvedTaskItemFindings)}
            </p>
          )}
        </div>
      )}
    </div>
  );
}

function formatSignedDelta(value: number) {
  return value > 0 ? `+${value}` : `${value}`;
}

function AuditDiffPill({ label, tone }: { label: string; tone: 'emerald' | 'rose' | 'sky' | 'amber' | 'violet' | 'neutral' }) {
  const toneClasses = {
    emerald: 'border-emerald-400/20 bg-emerald-500/10 text-emerald-100',
    rose: 'border-rose-400/20 bg-rose-500/10 text-rose-100',
    sky: 'border-sky-400/20 bg-sky-500/10 text-sky-100',
    amber: 'border-amber-400/20 bg-amber-500/10 text-amber-100',
    violet: 'border-violet-400/20 bg-violet-500/10 text-violet-100',
    neutral: 'border-white/10 bg-white/[0.05] text-white/75',
  } as const;

  return <span className={`rounded-full border px-2.5 py-1 text-[10px] ${toneClasses[tone]}`}>{label}</span>;
}