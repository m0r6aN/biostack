'use client';

import { Header } from '@/components/Header';
import { TaxonomyAuditEntryCard } from '@/components/research/TaxonomyAuditEntryCard';
import { GlassCard } from '@/components/ui/GlassCard';
import {
    buildFilteredTaxonomyAuditExport,
    buildFocusedTaxonomyAuditExport,
    buildJsonDownloadHref,
    buildTaxonomyAuditLogExport,
    matchesTaxonomyAuditSearch,
} from '@/lib/research/categoryTaxonomyTimeline';
import type { ResearchCategoryTaxonomyAuditEntry, ResearchCategoryTaxonomyAuditLog } from '@/lib/research/types';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useMemo, useState } from 'react';

type AuditFilter = 'all' | ResearchCategoryTaxonomyAuditEntry['action'];

const FILTERS: Array<{ id: AuditFilter; label: string }> = [
  { id: 'all', label: 'All Events' },
  { id: 'save-taxonomy', label: 'Taxonomy Saves' },
  { id: 'apply-migration-fixup', label: 'Migration Applies' },
];

export default function ResearchTaxonomyHistoryPage() {
  return (
    <Suspense fallback={<ResearchTaxonomyHistoryFallback />}>
      <ResearchTaxonomyHistoryContent />
    </Suspense>
  );
}

function ResearchTaxonomyHistoryContent() {
  const searchParams = useSearchParams();
  const focusedEntryId = searchParams.get('entry');
  const [auditLog, setAuditLog] = useState<ResearchCategoryTaxonomyAuditLog | null>(null);
  const [error, setError] = useState('');
  const [activeFilter, setActiveFilter] = useState<AuditFilter>('all');
  const [searchQuery, setSearchQuery] = useState('');

  useEffect(() => {
    loadHistory();
  }, []);

  useEffect(() => {
    if (!focusedEntryId || !auditLog) return;
    const focusedEntry = auditLog.entries.find((entry) => entry.entryId === focusedEntryId);
    if (focusedEntry) setActiveFilter(focusedEntry.action);
  }, [focusedEntryId, auditLog]);

  async function loadHistory() {
    try {
      const response = await fetch('/api/research/category-taxonomy/history');
      const payload = await response.json().catch(() => null) as ResearchCategoryTaxonomyAuditLog | { error?: string } | null;
      if (!response.ok) {
        throw new Error(payload && 'error' in payload ? payload.error ?? 'Failed to load taxonomy audit history.' : 'Failed to load taxonomy audit history.');
      }
      setAuditLog(payload as ResearchCategoryTaxonomyAuditLog);
      setError('');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load taxonomy audit history');
    }
  }

  const entries = useMemo(() => auditLog?.entries ?? [], [auditLog]);
  const filteredEntries = useMemo(
    () => entries.filter((entry) => {
      if (activeFilter !== 'all' && entry.action !== activeFilter) return false;
      return matchesTaxonomyAuditSearch(entry, searchQuery);
    }),
    [entries, activeFilter, searchQuery],
  );
  const focusedEntry = useMemo(() => entries.find((entry) => entry.entryId === focusedEntryId) ?? null, [entries, focusedEntryId]);
  const fullAuditExportHref = useMemo(() => buildJsonDownloadHref(buildTaxonomyAuditLogExport(auditLog)), [auditLog]);
  const filteredAuditExportHref = useMemo(() => buildJsonDownloadHref(buildFilteredTaxonomyAuditExport(filteredEntries, activeFilter, searchQuery)), [filteredEntries, activeFilter, searchQuery]);
  const focusedAuditExportHref = useMemo(() => {
    const payload = buildFocusedTaxonomyAuditExport(focusedEntry);
    return payload ? buildJsonDownloadHref(payload) : null;
  }, [focusedEntry]);

  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header title="Taxonomy Timeline" subtitle="Governance event stream · Internal" />
      <main className="flex-1 p-6 max-w-6xl mx-auto w-full space-y-6">
        {error && <div className="rounded-xl border border-rose-400/20 bg-rose-500/10 px-4 py-3 text-sm text-rose-300">{error}</div>}

        <GlassCard className="p-5 space-y-4">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h2 className="text-sm font-semibold text-white">Taxonomy governance event stream</h2>
              <p className="mt-1 text-[11px] leading-5 text-white/40">
                Review saved taxonomy revisions and migration receipts as a chronological operator timeline.
              </p>
              {focusedEntry && (
                <p className="mt-3 text-[10px] font-semibold uppercase tracking-[0.16em] text-violet-200/80">
                  Focused on {focusedEntry.summary}
                </p>
              )}
            </div>
            <div className="flex items-center gap-3 text-[11px]">
              <a href={fullAuditExportHref} download="taxonomy-audit-log.json" className="text-sky-200/80 transition-colors hover:text-sky-100">
                Export full audit log →
              </a>
              <a href={filteredAuditExportHref} download="taxonomy-audit-filtered.json" className="text-violet-200/80 transition-colors hover:text-violet-100">
                Export filtered stream →
              </a>
              {focusedAuditExportHref && (
                <a href={focusedAuditExportHref} download={`taxonomy-audit-entry-${focusedEntry?.entryId ?? 'focused'}.json`} className="text-amber-200/80 transition-colors hover:text-amber-100">
                  Export focused entry →
                </a>
              )}
              <Link href="/admin/research/taxonomy" className="text-emerald-300 transition-colors hover:text-emerald-200">
                Back to taxonomy →
              </Link>
            </div>
          </div>

          <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_auto] md:items-center">
            <input
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Search taxonomy history"
              className="w-full rounded-lg border border-white/10 bg-black/20 px-3 py-2 text-sm text-white outline-none placeholder:text-white/25 focus:border-violet-300/40"
            />
            {searchQuery && (
              <button
                type="button"
                onClick={() => setSearchQuery('')}
                className="rounded-lg border border-white/10 bg-white/[0.04] px-3 py-2 text-xs font-semibold text-white/70 transition-colors hover:bg-white/[0.08]"
              >
                Clear search
              </button>
            )}
          </div>

          <div className="flex flex-wrap gap-2">
            {FILTERS.map((filter) => (
              <button
                key={filter.id}
                type="button"
                onClick={() => setActiveFilter(filter.id)}
                className={`rounded-full border px-3 py-1.5 text-xs transition-colors ${activeFilter === filter.id ? 'border-violet-300/40 bg-violet-500/20 text-violet-100' : 'border-white/10 bg-white/[0.04] text-white/60 hover:bg-white/[0.08]'}`}
              >
                {filter.label}
              </button>
            ))}
          </div>

          <div className="grid gap-3 sm:grid-cols-3">
            <TimelineStat label="All Events" value={entries.length} />
            <TimelineStat label="Taxonomy Saves" value={entries.filter((entry) => entry.action === 'save-taxonomy').length} />
            <TimelineStat label="Migration Applies" value={entries.filter((entry) => entry.action === 'apply-migration-fixup').length} />
          </div>
        </GlassCard>

        <section className="space-y-4">
          {filteredEntries.length === 0 ? (
            <GlassCard className="p-6">
              <p className="text-sm text-white/35">No taxonomy governance events match the current filter/search.</p>
            </GlassCard>
          ) : (
            <div className="space-y-4">
              {filteredEntries.map((entry) => (
                <TaxonomyAuditEntryCard key={entry.entryId} entry={entry} focused={entry.entryId === focusedEntryId} />
              ))}
            </div>
          )}
        </section>
      </main>
    </div>
  );
}

function ResearchTaxonomyHistoryFallback() {
  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header title="Taxonomy Timeline" subtitle="Governance event stream · Internal" />
      <main className="flex-1 p-6 max-w-6xl mx-auto w-full">
        <p className="text-sm text-white/30">Loading...</p>
      </main>
    </div>
  );
}

function TimelineStat({ label, value }: { label: string; value: number }) {
  return (
    <GlassCard className="p-4">
      <p className="text-[10px] font-bold uppercase tracking-widest text-white/35">{label}</p>
      <p className="mt-2 text-lg font-semibold text-white">{value}</p>
    </GlassCard>
  );
}
