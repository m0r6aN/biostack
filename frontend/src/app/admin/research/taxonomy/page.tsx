'use client';

import { Header } from '@/components/Header';
import { TaxonomyAuditEntryCard } from '@/components/research/TaxonomyAuditEntryCard';
import { GlassCard } from '@/components/ui/GlassCard';
import { getApiBaseUrl } from '@/lib/apiBase';
import { fetchResearchCategoryTaxonomy } from '@/lib/research/loader';
import type {
    ResearchCategoryMigrationApplyReceipt,
    ResearchCategoryMigrationReport,
    ResearchCategoryTaxonomy,
    ResearchCategoryTaxonomyAuditLog
} from '@/lib/research/types';
import Link from 'next/link';
import { useCallback, useEffect, useRef, useState } from 'react';

type EditableCategory = {
  id: string;
  name: string;
  aliases: string;
  deprecated: boolean;
  replacedBy: string;
};

function toEditableCategories(taxonomy: ResearchCategoryTaxonomy): EditableCategory[] {
  return taxonomy.categories.map((category, index) => ({
    id: `${index}-${category.name}`,
    name: category.name,
    aliases: (category.aliases ?? []).join(', '),
    deprecated: category.deprecated === true,
    replacedBy: category.replacedBy ?? '',
  }));
}

export default function ResearchTaxonomyPage() {
  const tokenRef = useRef<string | null>(null);
  const nextIdRef = useRef(0);
  const [taxonomy, setTaxonomy] = useState<ResearchCategoryTaxonomy | null>(null);
  const [migrationReport, setMigrationReport] = useState<ResearchCategoryMigrationReport | null>(null);
  const [migrationReceipt, setMigrationReceipt] = useState<ResearchCategoryMigrationApplyReceipt | null>(null);
  const [auditLog, setAuditLog] = useState<ResearchCategoryTaxonomyAuditLog | null>(null);
  const [categories, setCategories] = useState<EditableCategory[]>([]);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [saving, setSaving] = useState(false);
  const [reportLoading, setReportLoading] = useState(false);
  const [migrating, setMigrating] = useState(false);

  const acquireToken = useCallback(async () => {
    try {
      const res = await fetch(`${getApiBaseUrl()}/api/v1/auth/dev-token`, { method: 'POST' });
      if (res.ok) tokenRef.current = (await res.json()).token;
    } catch {
      // no-op
    }
  }, []);

  const loadMigrationReport = useCallback(async () => {
    setReportLoading(true);
    try {
      const response = await fetch('/api/research/category-taxonomy/migration-report');
      const payload = await response.json().catch(() => null) as ResearchCategoryMigrationReport | { error?: string } | null;
      if (!response.ok) {
        throw new Error(payload && 'error' in payload ? payload.error ?? 'Failed to load migration report.' : 'Failed to load migration report.');
      }
      return payload as ResearchCategoryMigrationReport;
    } finally {
      setReportLoading(false);
    }
  }, []);

  const loadAuditHistory = useCallback(async () => {
    const response = await fetch('/api/research/category-taxonomy/history');
    const payload = await response.json().catch(() => null) as ResearchCategoryTaxonomyAuditLog | { error?: string } | null;
    if (!response.ok) {
      throw new Error(payload && 'error' in payload ? payload.error ?? 'Failed to load taxonomy audit history.' : 'Failed to load taxonomy audit history.');
    }
    return payload as ResearchCategoryTaxonomyAuditLog;
  }, []);

  const load = useCallback(async () => {
    try {
      const [loaded, report, history] = await Promise.all([
        fetchResearchCategoryTaxonomy(tokenRef.current ?? ''),
        loadMigrationReport(),
        loadAuditHistory(),
      ]);
      setTaxonomy(loaded);
      setCategories(toEditableCategories(loaded));
      setMigrationReport(report);
      setAuditLog(history);
      setError('');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load category taxonomy');
    }
  }, [loadAuditHistory, loadMigrationReport]);

  useEffect(() => {
    acquireToken().then(load);
  }, [acquireToken, load]);

  function updateCategory(index: number, field: 'name' | 'aliases' | 'replacedBy', value: string) {
    setCategories((current) => current.map((category, currentIndex) => currentIndex === index ? { ...category, [field]: value } : category));
  }

  function updateDeprecation(index: number, deprecated: boolean) {
    setCategories((current) => current.map((category, currentIndex) => currentIndex === index
      ? { ...category, deprecated, replacedBy: deprecated ? category.replacedBy : '' }
      : category));
  }

  function addCategory() {
    nextIdRef.current += 1;
    setCategories((current) => [...current, { id: `new-${nextIdRef.current}`, name: '', aliases: '', deprecated: false, replacedBy: '' }]);
  }

  function removeCategory(index: number) {
    setCategories((current) => current.filter((_, currentIndex) => currentIndex !== index));
  }

  async function save() {
    setSaving(true);
    setError('');
    setMessage('');

    try {
      const response = await fetch('/api/research/category-taxonomy', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          categories: categories.map((category) => ({
            name: category.name,
            aliases: category.aliases,
            deprecated: category.deprecated,
            replacedBy: category.replacedBy,
          })),
        }),
      });

      const payload = await response.json().catch(() => null) as ResearchCategoryTaxonomy | { error?: string } | null;
      if (!response.ok) {
        throw new Error(payload && 'error' in payload ? payload.error ?? 'Failed to save category taxonomy.' : 'Failed to save category taxonomy.');
      }

      const saved = payload as ResearchCategoryTaxonomy;
      setTaxonomy(saved);
      setCategories(toEditableCategories(saved));
      const [report, history] = await Promise.all([loadMigrationReport(), loadAuditHistory()]);
      setMigrationReport(report);
      setAuditLog(history);
      setMessage(`Saved taxonomy ${saved.taxonomyVersion} with ${saved.categories.length} categories.`);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to save category taxonomy');
    } finally {
      setSaving(false);
    }
  }

  async function applyMigrationFixup() {
    setMigrating(true);
    setError('');
    setMessage('');

    try {
      const response = await fetch('/api/research/category-taxonomy/migration-report', { method: 'POST' });
      const payload = await response.json().catch(() => null) as ResearchCategoryMigrationApplyReceipt | { error?: string } | null;
      if (!response.ok) {
        throw new Error(payload && 'error' in payload ? payload.error ?? 'Failed to apply taxonomy migration fix-up.' : 'Failed to apply taxonomy migration fix-up.');
      }

      const receipt = payload as ResearchCategoryMigrationApplyReceipt;
      setMigrationReceipt(receipt);
      const [report, history] = await Promise.all([loadMigrationReport(), loadAuditHistory()]);
      setMigrationReport(report);
      setAuditLog(history);
      if (receipt.counts.totalFilesUpdated === 0) {
        setMessage('No deprecated taxonomy references required migration.');
      } else {
        setMessage(`Applied taxonomy migration fix-up to ${receipt.counts.totalFilesUpdated} file${receipt.counts.totalFilesUpdated === 1 ? '' : 's'} and rewrote ${receipt.counts.categoriesRewritten} category reference${receipt.counts.categoriesRewritten === 1 ? '' : 's'}.`);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to apply taxonomy migration fix-up');
    } finally {
      setMigrating(false);
    }
  }

  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header title="Category Taxonomy" subtitle="Canonical research category governance · Internal" />
      <main className="flex-1 p-6 max-w-6xl mx-auto w-full space-y-6">
        {error && (
          <div className="rounded-xl border border-rose-400/20 bg-rose-500/10 px-4 py-3 text-sm text-rose-300">{error}</div>
        )}
        {message && (
          <div className="rounded-xl border border-emerald-400/20 bg-emerald-500/10 px-4 py-3 text-sm text-emerald-200">{message}</div>
        )}

        <div className="grid gap-3 sm:grid-cols-3">
          <TaxonomyStat label="Version" value={taxonomy?.taxonomyVersion ?? '—'} />
          <TaxonomyStat label="Active" value={categories.filter((category) => !category.deprecated).length} />
          <TaxonomyStat label="Deprecated" value={categories.filter((category) => category.deprecated).length} />
          <TaxonomyStat label="Updated" value={taxonomy?.updatedAtUtc ? new Date(taxonomy.updatedAtUtc).toLocaleDateString() : '—'} />
        </div>

        <GlassCard className="p-5 space-y-4">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h2 className="text-sm font-semibold text-white">Canonical research categories</h2>
              <p className="mt-1 text-[11px] leading-5 text-white/40">
                Edit canonical names and aliases for request normalization, task-board presets, and worker-side category governance.
              </p>
            </div>
            <div className="flex items-center gap-3 text-[11px]">
              <button type="button" onClick={addCategory} className="rounded-lg border border-violet-400/20 bg-violet-500/10 px-3 py-2 font-semibold text-violet-100 transition-colors hover:bg-violet-500/20">
                Add category
              </button>
              <button type="button" onClick={save} disabled={saving} className="rounded-lg bg-emerald-500 px-3 py-2 font-semibold text-slate-950 transition-colors hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50">
                {saving ? 'Saving…' : 'Save taxonomy'}
              </button>
            </div>
          </div>

          <div className="space-y-3">
            {categories.map((category, index) => (
              <div key={category.id} className="rounded-xl border border-white/10 bg-white/[0.03] p-4">
                <div className="grid gap-3 lg:grid-cols-[minmax(0,220px)_minmax(0,1fr)_minmax(0,220px)_auto] lg:items-start">
                  <div className="space-y-1">
                    <label className="text-[10px] font-bold uppercase tracking-widest text-white/35">Canonical name</label>
                    <input
                      value={category.name}
                      onChange={(e) => updateCategory(index, 'name', e.target.value)}
                      placeholder="Canonical category name"
                      className="w-full rounded-lg border border-white/10 bg-black/20 px-3 py-2 text-sm text-white outline-none placeholder:text-white/25 focus:border-violet-300/40"
                    />
                    {category.deprecated && (
                      <p className="text-[10px] leading-5 text-amber-200/70">Deprecated category name; input will normalize to its replacement canon.</p>
                    )}
                  </div>
                  <div className="space-y-1">
                    <label className="text-[10px] font-bold uppercase tracking-widest text-white/35">Aliases</label>
                    <input
                      value={category.aliases}
                      onChange={(e) => updateCategory(index, 'aliases', e.target.value)}
                      placeholder="Comma-separated aliases"
                      className="w-full rounded-lg border border-white/10 bg-black/20 px-3 py-2 text-sm text-white outline-none placeholder:text-white/25 focus:border-violet-300/40"
                    />
                    <p className="text-[10px] leading-5 text-white/35">Aliases are trimmed, deduped, and excluded when they match the canonical name.</p>
                  </div>
                  <div className="space-y-2">
                    <label className="flex items-center gap-2 text-[10px] font-bold uppercase tracking-widest text-white/35">
                      <input
                        type="checkbox"
                        checked={category.deprecated}
                        onChange={(e) => updateDeprecation(index, e.target.checked)}
                        className="h-4 w-4 rounded border-white/15 bg-black/20 text-violet-400"
                      />
                      Deprecated
                    </label>
                    <input
                      value={category.replacedBy}
                      onChange={(e) => updateCategory(index, 'replacedBy', e.target.value)}
                      placeholder="Replacement canonical category"
                      disabled={!category.deprecated}
                      className="w-full rounded-lg border border-white/10 bg-black/20 px-3 py-2 text-sm text-white outline-none placeholder:text-white/25 focus:border-violet-300/40 disabled:cursor-not-allowed disabled:opacity-40"
                    />
                    <p className="text-[10px] leading-5 text-white/35">When deprecated, the category name and aliases normalize to this replacement canon.</p>
                  </div>
                  <button type="button" onClick={() => removeCategory(index)} className="rounded-lg border border-rose-400/20 bg-rose-500/10 px-3 py-2 text-[11px] font-semibold text-rose-200 transition-colors hover:bg-rose-500/20">
                    Remove
                  </button>
                </div>
              </div>
            ))}
            {categories.length === 0 && (
              <div className="rounded-xl border border-dashed border-white/10 bg-black/10 px-4 py-6 text-sm text-white/35">
                No categories configured yet. Add at least one canonical category before saving.
              </div>
            )}
          </div>
        </GlassCard>

        <GlassCard className="p-5 space-y-4">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h2 className="text-sm font-semibold text-white">Migration impact preview</h2>
              <p className="mt-1 text-[11px] leading-5 text-white/40">
                Scan current research requests and task artifacts for deprecated categories that will normalize to a replacement canon.
              </p>
            </div>
            <button type="button" onClick={async () => setMigrationReport(await loadMigrationReport())} disabled={reportLoading} className="rounded-lg border border-sky-400/20 bg-sky-500/10 px-3 py-2 text-[11px] font-semibold text-sky-100 transition-colors hover:bg-sky-500/20 disabled:cursor-not-allowed disabled:opacity-50">
              {reportLoading ? 'Refreshing…' : 'Refresh report'}
            </button>
            <button type="button" onClick={applyMigrationFixup} disabled={migrating || reportLoading || !migrationReport || migrationReport.findings.length === 0} className="rounded-lg border border-amber-400/20 bg-amber-500/10 px-3 py-2 text-[11px] font-semibold text-amber-100 transition-colors hover:bg-amber-500/20 disabled:cursor-not-allowed disabled:opacity-50">
              {migrating ? 'Applying…' : 'Apply migration fix-up'}
            </button>
          </div>

          {migrationReport ? (
            <>
              <div className="grid gap-3 sm:grid-cols-3 xl:grid-cols-6">
                <TaxonomyStat label="Deprecated Mappings" value={migrationReport.deprecatedCategories.length} />
                <TaxonomyStat label="Request Files" value={migrationReport.counts.requestFilesScanned} />
                <TaxonomyStat label="Request Hits" value={migrationReport.counts.requestFindings} />
                <TaxonomyStat label="Task Artifacts" value={migrationReport.counts.taskArtifactsScanned} />
                <TaxonomyStat label="Task Hits" value={migrationReport.counts.taskItemFindings} />
                <TaxonomyStat label="Resolved Hits" value={migrationReport.counts.resolvedTaskItemFindings} />
              </div>

              {migrationReport.findings.length === 0 ? (
                <div className="rounded-xl border border-emerald-400/15 bg-emerald-500/[0.05] px-4 py-4 text-sm text-emerald-100/85">
                  No current research files reference deprecated taxonomy categories.
                </div>
              ) : (
                <>
                  <div className="space-y-2">
                    <p className="text-[10px] font-bold uppercase tracking-widest text-white/35">Deprecated mappings in use</p>
                    <div className="grid gap-3 lg:grid-cols-2">
                      {migrationReport.deprecatedCategories.map((summary) => (
                        <div key={`${summary.deprecatedCategory}-${summary.replacementCategory}`} className="rounded-xl border border-amber-400/15 bg-amber-500/[0.05] px-4 py-3 text-sm text-white/80">
                          <p className="font-semibold text-amber-100">{summary.deprecatedCategory} → {summary.replacementCategory}</p>
                          <p className="mt-1 text-[11px] text-white/50">
                            {summary.findings} hit{summary.findings === 1 ? '' : 's'} · requests {summary.requestFindings} · queued {summary.taskItemFindings} · resolved {summary.resolvedTaskItemFindings}
                          </p>
                        </div>
                      ))}
                    </div>
                  </div>

                  <div className="space-y-3">
                    <p className="text-[10px] font-bold uppercase tracking-widest text-white/35">Findings</p>
                    {migrationReport.findings.map((finding, index) => (
                      <div key={`${finding.sourcePath}-${finding.requestId ?? finding.taskId ?? index}-${finding.matchedCategory}-${index}`} className="rounded-xl border border-white/10 bg-white/[0.03] px-4 py-3 text-sm text-white/80">
                        <div className="flex flex-wrap items-center justify-between gap-3">
                          <p className="font-semibold text-white">{finding.compoundName}</p>
                          <span className="rounded-full border border-white/10 bg-black/20 px-2.5 py-1 text-[10px] uppercase tracking-widest text-white/45">{finding.sourceType}</span>
                        </div>
                        <p className="mt-2 text-[11px] text-white/55">
                          {finding.matchedCategory} will normalize from {finding.deprecatedCategory} to {finding.replacementCategory}.
                        </p>
                        <p className="mt-1 text-[10px] text-white/35">
                          {finding.sourcePath}{finding.requestId ? ` · ${finding.requestId}` : ''}{finding.taskId ? ` · ${finding.taskId}` : ''}
                        </p>
                      </div>
                    ))}
                  </div>
                </>
              )}
            </>
          ) : (
            <div className="rounded-xl border border-white/10 bg-black/10 px-4 py-4 text-sm text-white/40">
              Migration report unavailable.
            </div>
          )}

          {migrationReceipt && (
            <div className="space-y-3">
              <p className="text-[10px] font-bold uppercase tracking-widest text-white/35">Last migration receipt</p>
              <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                <TaxonomyStat label="Files Updated" value={migrationReceipt.counts.totalFilesUpdated} />
                <TaxonomyStat label="Request Files" value={migrationReceipt.counts.requestFilesUpdated} />
                <TaxonomyStat label="Task Artifacts" value={migrationReceipt.counts.taskArtifactsUpdated} />
                <TaxonomyStat label="Categories Rewritten" value={migrationReceipt.counts.categoriesRewritten} />
              </div>

              {migrationReceipt.updatedFiles.length > 0 && (
                <div className="space-y-3">
                  {migrationReceipt.updatedFiles.map((file) => (
                    <div key={`${file.sourceType}-${file.sourcePath}`} className="rounded-xl border border-white/10 bg-white/[0.03] px-4 py-3 text-sm text-white/80">
                      <div className="flex flex-wrap items-center justify-between gap-3">
                        <p className="font-semibold text-white">{file.sourcePath}</p>
                        <span className="rounded-full border border-white/10 bg-black/20 px-2.5 py-1 text-[10px] uppercase tracking-widest text-white/45">{file.sourceType}</span>
                      </div>
                      <p className="mt-2 text-[11px] text-white/50">
                        Rewrote {file.categoriesRewritten} category reference{file.categoriesRewritten === 1 ? '' : 's'} across {file.compounds.join(', ')}.
                      </p>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </GlassCard>

        <GlassCard className="p-5 space-y-4">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h2 className="text-sm font-semibold text-white">Governance audit history</h2>
              <p className="mt-1 text-[11px] leading-5 text-white/40">
                Saved taxonomy revisions and migration-apply receipts are preserved here for operator review.
              </p>
            </div>
            <Link href="/admin/research/taxonomy/history" className="text-[11px] font-semibold text-violet-200/80 transition-colors hover:text-violet-100">
              Open full timeline →
            </Link>
          </div>

          {(auditLog?.entries.length ?? 0) > 0 ? (
            <div className="space-y-3">
              {auditLog?.entries.map((entry) => (
                <TaxonomyAuditEntryCard key={entry.entryId} entry={entry} timelineHref={`/admin/research/taxonomy/history?entry=${encodeURIComponent(entry.entryId)}`} />
              ))}
            </div>
          ) : (
            <div className="rounded-xl border border-white/10 bg-black/10 px-4 py-4 text-sm text-white/40">
              No taxonomy audit history has been recorded yet.
            </div>
          )}
        </GlassCard>

        <div className="flex justify-end gap-4 text-[12px]">
          <Link href="/admin/research/tasks" className="text-violet-300 transition-colors hover:text-violet-200">View task board →</Link>
          <Link href="/admin/research" className="text-emerald-400 transition-colors hover:text-emerald-300">Back to research dashboard →</Link>
        </div>
      </main>
    </div>
  );
}

function TaxonomyStat({ label, value }: { label: string; value: string | number }) {
  return (
    <GlassCard className="p-4">
      <p className="text-[10px] font-bold uppercase tracking-widest text-white/35">{label}</p>
      <p className="mt-2 text-lg font-semibold text-white">{value}</p>
    </GlassCard>
  );
}