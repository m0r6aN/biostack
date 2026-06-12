'use client';

import { Header } from '@/components/Header';
import { GlassCard } from '@/components/ui/GlassCard';
import { getApiBaseUrl } from '@/lib/apiBase';
import { getResearchCategoryPresets, mergeResearchCategoryOptions } from '@/lib/research/categoryRegistry';
import { fetchResearchCategoryTaxonomy, fetchResearchTaskQueue } from '@/lib/research/loader';
import { toSlug } from '@/lib/research/slugs';
import { buildResearchTaskExportHref, buildResearchTaskHandoffPayload } from '@/lib/research/taskHandoff';
import type { ResearchCategoryTaxonomy, ResearchTaskQueue, ResearchTaskQueueItem, ResearchTaskQueueResolvedItem } from '@/lib/research/types';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense, useCallback, useEffect, useMemo, useRef, useState } from 'react';

type TaskFilterable = {
  compoundName: string;
  requesterIds: string[];
  categories?: string[];
  priority: string;
};

export default function ResearchTasksPage() {
  return (
    <Suspense fallback={<ResearchTasksPageFallback />}>
      <ResearchTasksPageContent />
    </Suspense>
  );
}

function ResearchTasksPageContent() {
  const searchParams = useSearchParams();
  const tokenRef = useRef<string | null>(null);
  const [taskQueue, setTaskQueue] = useState<ResearchTaskQueue | null>(null);
  const [categoryTaxonomy, setCategoryTaxonomy] = useState<ResearchCategoryTaxonomy | null>(null);
  const [error, setError] = useState('');
  const [copyStatus, setCopyStatus] = useState('');
  const [priorityFilter, setPriorityFilter] = useState('all');
  const [requesterFilter, setRequesterFilter] = useState('all');
  const [categoryFilter, setCategoryFilter] = useState('all');

  async function acquireToken() {
    try {
      const res = await fetch(`${getApiBaseUrl()}/api/v1/auth/dev-token`, { method: 'POST' });
      if (res.ok) tokenRef.current = (await res.json()).token;
    } catch {
      // no-op
    }
  }

  async function load() {
    try {
      const [queue, taxonomy] = await Promise.all([
        fetchResearchTaskQueue(tokenRef.current ?? ''),
        fetchResearchCategoryTaxonomy(tokenRef.current ?? '').catch(() => null),
      ]);
      setTaskQueue(queue);
      setCategoryTaxonomy(taxonomy);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load research tasks');
    }
  }

  useEffect(() => {
    acquireToken().then(load);
  }, []);

  const focusedCompoundSlug = searchParams.get('compound');
  const activeItems = useMemo(() => taskQueue?.items ?? [], [taskQueue?.items]);
  const resolvedItems = useMemo(() => taskQueue?.resolvedItems ?? [], [taskQueue?.resolvedItems]);
  const priorityOptions = useMemo(
    () => ['urgent', 'high', 'normal', 'low'].filter((priority) => [...activeItems, ...resolvedItems].some((item) => item.priority === priority)),
    [activeItems, resolvedItems],
  );
  const requesterOptions = useMemo(
    () => [...new Set([...activeItems, ...resolvedItems].flatMap((item) => item.requesterIds).filter(Boolean))].sort((a, b) => a.localeCompare(b)),
    [activeItems, resolvedItems],
  );
  const categoryPresets = useMemo(() => getResearchCategoryPresets(categoryTaxonomy), [categoryTaxonomy]);
  const categoryOptions = useMemo(
    () => mergeResearchCategoryOptions(categoryTaxonomy, ...[activeItems, resolvedItems].map((items) => items.flatMap((item) => item.categories ?? []))),
    [categoryTaxonomy, activeItems, resolvedItems],
  );

  const matchesFilters = useCallback((item: TaskFilterable) => {
    if (focusedCompoundSlug && toSlug(item.compoundName) !== focusedCompoundSlug) return false;
    if (priorityFilter !== 'all' && item.priority !== priorityFilter) return false;
    if (requesterFilter !== 'all' && !item.requesterIds.includes(requesterFilter)) return false;
    if (categoryFilter !== 'all' && !(item.categories ?? []).includes(categoryFilter)) return false;
    return true;
  }, [focusedCompoundSlug, priorityFilter, requesterFilter, categoryFilter]);

  const filteredActiveItems = useMemo(
    () => activeItems.filter(matchesFilters),
    [activeItems, matchesFilters],
  );
  const filteredResolvedItems = useMemo(
    () => resolvedItems.filter(matchesFilters),
    [resolvedItems, matchesFilters],
  );
  const focusedCompoundName = useMemo(
    () => [...activeItems, ...resolvedItems].find((item) => toSlug(item.compoundName) === focusedCompoundSlug)?.compoundName ?? null,
    [focusedCompoundSlug, activeItems, resolvedItems],
  );
  const exportHref = useMemo(() => buildResearchTaskExportHref(buildResearchTaskHandoffPayload(
    filteredActiveItems,
    taskQueue?.generatedAtUtc ?? new Date().toISOString(),
    {
      focusedCompoundSlug: focusedCompoundSlug ?? undefined,
      priority: priorityFilter !== 'all' ? priorityFilter : undefined,
      requester: requesterFilter !== 'all' ? requesterFilter : undefined,
      category: categoryFilter !== 'all' ? categoryFilter : undefined,
    },
  )), [filteredActiveItems, taskQueue?.generatedAtUtc, focusedCompoundSlug, priorityFilter, requesterFilter, categoryFilter]);

  async function copyTask(item: ResearchTaskQueueItem) {
    try {
      await navigator.clipboard.writeText(JSON.stringify(buildResearchTaskHandoffPayload(
        [item],
        taskQueue?.generatedAtUtc ?? new Date().toISOString(),
        { focusedCompoundSlug: toSlug(item.compoundName) },
      ), null, 2));
      setCopyStatus(`Copied handoff payload for ${item.compoundName}.`);
    } catch {
      setCopyStatus(`Unable to copy handoff payload for ${item.compoundName}.`);
    }
  }

  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header title="Research Tasks" subtitle="Evidence-generation intake queue · Internal" />
      <main className="flex-1 p-6 max-w-6xl mx-auto w-full space-y-6">
        {error && (
          <div className="rounded-xl border border-rose-400/20 bg-rose-500/10 px-4 py-3 text-sm text-rose-300">
            {error}
          </div>
        )}
        {copyStatus && (
          <div className="rounded-xl border border-emerald-400/20 bg-emerald-500/10 px-4 py-3 text-sm text-emerald-200">
            {copyStatus}
          </div>
        )}

        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-6">
          <TaskStat label="Queued Tasks" value={taskQueue?.counts.totalItems ?? '—'} tone="neutral" />
          <TaskStat label="Consumed" value={taskQueue?.counts.resolvedItems ?? resolvedItems.length} tone="emerald" />
          <TaskStat label="Urgent" value={taskQueue?.counts.urgent ?? '—'} tone="rose" />
          <TaskStat label="High" value={taskQueue?.counts.high ?? '—'} tone="violet" />
          <TaskStat label="Normal" value={taskQueue?.counts.normal ?? '—'} tone="blue" />
          <TaskStat label="Low" value={taskQueue?.counts.low ?? '—'} tone="emerald" />
        </div>

        <GlassCard className="p-5">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h2 className="text-sm font-semibold text-white">Evidence task queue</h2>
              <p className="mt-1 text-[11px] leading-5 text-white/40">
                Filter active and consumed intake tasks, then copy or export filtered handoff payloads for agent execution.
              </p>
              {focusedCompoundSlug && (
                <p className="mt-3 text-[10px] font-semibold uppercase tracking-[0.16em] text-violet-200/80">
                  Focused on {focusedCompoundName ?? focusedCompoundSlug.replace(/-/g, ' ')}
                </p>
              )}
            </div>
            <div className="flex flex-wrap items-center gap-3 text-[11px]">
              <a href={exportHref} download="research-task-handoff.json" className="text-violet-200/80 transition-colors hover:text-violet-100">
                Export filtered handoff →
              </a>
              {focusedCompoundSlug && (
                <Link href="/admin/research/tasks" className="text-violet-200/80 transition-colors hover:text-violet-100">
                  Clear focus
                </Link>
              )}
              <Link href="/admin/research/compounds" className="text-emerald-300 transition-colors hover:text-emerald-200">
                Back to compounds →
              </Link>
            </div>
          </div>

          <div className="mt-5 grid gap-3 md:grid-cols-3">
            <FilterSelect label="Priority" value={priorityFilter} onChange={setPriorityFilter} options={priorityOptions} />
            <FilterSelect label="Requester" value={requesterFilter} onChange={setRequesterFilter} options={requesterOptions} />
            <FilterSelect label="Category" value={categoryFilter} onChange={setCategoryFilter} options={categoryOptions} />
          </div>
          <div className="mt-3 flex flex-wrap gap-2">
            {categoryPresets.map((category) => (
              <button
                key={category}
                type="button"
                onClick={() => setCategoryFilter(category)}
                className={`rounded-full border px-2.5 py-1 text-[10px] transition-colors ${categoryFilter === category ? 'border-violet-300/40 bg-violet-500/20 text-violet-100' : 'border-violet-400/20 bg-violet-500/10 text-violet-200 hover:bg-violet-500/20'}`}
              >
                {category}
              </button>
            ))}
            {categoryFilter !== 'all' && (
              <button
                type="button"
                onClick={() => setCategoryFilter('all')}
                className="rounded-full border border-white/10 bg-white/[0.04] px-2.5 py-1 text-[10px] text-white/60 transition-colors hover:bg-white/[0.08]"
              >
                Clear category
              </button>
            )}
          </div>
        </GlassCard>

        <section className="space-y-4">
          <div className="flex items-center justify-between gap-3">
            <div>
              <h3 className="text-sm font-semibold text-white">Queued for agent pickup</h3>
              <p className="mt-1 text-[11px] leading-5 text-white/40">
                These are still waiting for the first evidence packet.
              </p>
            </div>
            <span className="rounded-full border border-violet-400/20 bg-violet-500/10 px-2.5 py-1 text-[11px] font-semibold text-violet-200">
              {filteredActiveItems.length}
            </span>
          </div>
          {taskQueue && filteredActiveItems.length === 0 ? (
            <GlassCard className="p-6">
              <p className="text-sm text-white/35">No queued task matches the current filters.</p>
            </GlassCard>
          ) : (
            <div className="grid gap-4 xl:grid-cols-2">
              {filteredActiveItems.map((item) => (
                <TaskCard key={item.taskId} item={item} onCopy={() => copyTask(item)} />
              ))}
            </div>
          )}
        </section>

        <section className="space-y-4">
          <div className="flex items-center justify-between gap-3">
            <div>
              <h3 className="text-sm font-semibold text-white">Consumed on the latest run</h3>
              <p className="mt-1 text-[11px] leading-5 text-white/40">
                These requests cleared out of the active queue because evidence is now present in the pipeline.
              </p>
            </div>
            <span className="rounded-full border border-emerald-400/20 bg-emerald-500/10 px-2.5 py-1 text-[11px] font-semibold text-emerald-200">
              {filteredResolvedItems.length}
            </span>
          </div>
          {taskQueue && filteredResolvedItems.length === 0 ? (
            <GlassCard className="p-6">
              <p className="text-sm text-white/35">No consumed task matches the current filters.</p>
            </GlassCard>
          ) : (
            <div className="grid gap-4 xl:grid-cols-2">
              {filteredResolvedItems.map((item) => (
                <ResolvedTaskCard key={item.taskId} item={item} />
              ))}
            </div>
          )}
        </section>
      </main>
    </div>
  );
}

function ResearchTasksPageFallback() {
  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header title="Research Tasks" subtitle="Evidence-generation intake queue · Internal" />
      <main className="flex-1 p-6 max-w-6xl mx-auto w-full">
        <p className="text-sm text-white/30">Loading...</p>
      </main>
    </div>
  );
}

function TaskCard({ item, onCopy }: { item: ResearchTaskQueueItem; onCopy: () => void }) {
  const priorityTone = {
    urgent: 'border-rose-400/20 bg-rose-500/10 text-rose-200',
    high: 'border-violet-400/20 bg-violet-500/10 text-violet-200',
    normal: 'border-blue-400/20 bg-blue-500/10 text-blue-200',
    low: 'border-emerald-400/20 bg-emerald-500/10 text-emerald-200',
  }[item.priority];

  return (
    <GlassCard className="p-5">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-[9px] font-bold uppercase tracking-widest text-white/35">Queued evidence task</p>
          <h3 className="mt-2 text-lg font-semibold text-white">{item.compoundName}</h3>
          <p className="mt-1 text-[11px] text-white/45">
            {item.classification} · {item.taskType}
          </p>
        </div>
        <div className="flex flex-col items-end gap-2">
          <span className={`rounded-full border px-2.5 py-1 text-[10px] font-semibold uppercase tracking-[0.18em] ${priorityTone}`}>
            {item.priority}
          </span>
          <button type="button" onClick={onCopy} className="text-[10px] font-semibold uppercase tracking-[0.16em] text-violet-100/80 transition-colors hover:text-violet-100">
            Copy handoff payload
          </button>
        </div>
      </div>

      <TaskTags categories={item.categories ?? []} requesterIds={item.requesterIds} aliases={item.aliases} />

      <div className="mt-4 grid gap-3 sm:grid-cols-2">
        <TaskMeta label="Target evidence path" value={item.targetEvidencePath} />
        <TaskMeta label="Required schema" value={item.requiredSchema} />
        <TaskMeta label="Request IDs" value={item.requestIds.join(', ') || 'None'} />
        <TaskMeta label="Requester IDs" value={item.requesterIds.join(', ') || 'Unknown'} />
      </div>

      {item.rationales.length > 0 && (
        <div className="mt-4 rounded-xl border border-violet-400/15 bg-violet-500/[0.04] px-3 py-3">
          <p className="text-[9px] font-bold uppercase tracking-widest text-violet-200/70">Operator rationale</p>
          <div className="mt-2 flex flex-col gap-2 text-[11px] leading-5 text-white/70">
            {item.rationales.map((rationale) => <p key={rationale}>{rationale}</p>)}
          </div>
        </div>
      )}

      {item.notes.length > 0 && (
        <div className="mt-4">
          <p className="text-[9px] font-bold uppercase tracking-widest text-white/35">Operator notes</p>
          <ul className="mt-2 flex flex-col gap-2 text-[11px] leading-5 text-white/65">
            {item.notes.map((note) => (
              <li key={note} className="rounded-xl border border-white/[0.06] bg-white/[0.03] px-3 py-2">
                {note}
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="mt-4">
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/35">Suggested directives</p>
        <ul className="mt-2 flex flex-col gap-2 text-[11px] leading-5 text-white/65">
          {item.suggestedResearchDirectives.map((directive) => (
            <li key={directive} className="rounded-xl border border-white/[0.06] bg-white/[0.03] px-3 py-2">
              → {directive}
            </li>
          ))}
        </ul>
      </div>

      <div className="mt-4 flex flex-wrap items-center gap-3 text-[11px]">
        <Link href={`/admin/research/compounds/${toSlug(item.compoundName)}`} className="text-emerald-300 transition-colors hover:text-emerald-200">
          Open compound detail →
        </Link>
      </div>
    </GlassCard>
  );
}

function ResolvedTaskCard({ item }: { item: ResearchTaskQueueResolvedItem }) {
  return (
    <GlassCard className="p-5">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-[9px] font-bold uppercase tracking-widest text-white/35">Consumed task</p>
          <h3 className="mt-2 text-lg font-semibold text-white">{item.compoundName}</h3>
          <p className="mt-1 text-[11px] text-white/45">
            {item.classification} · moved to {item.currentReadiness.replace(/-/g, ' ')}
          </p>
        </div>
        <span className="rounded-full border border-emerald-400/20 bg-emerald-500/10 px-2.5 py-1 text-[10px] font-semibold uppercase tracking-[0.18em] text-emerald-200">
          consumed
        </span>
      </div>

      <TaskTags categories={item.categories ?? []} requesterIds={item.requesterIds} aliases={item.aliases} />

      <div className="mt-4 grid gap-3 sm:grid-cols-2">
        <TaskMeta label="Target evidence path" value={item.targetEvidencePath} />
        <TaskMeta label="Resolution" value={item.resolution} />
        <TaskMeta label="Current readiness" value={item.currentReadiness} />
        <TaskMeta label="Requester IDs" value={item.requesterIds.join(', ') || 'Unknown'} />
      </div>

      <div className="mt-4 rounded-xl border border-emerald-400/15 bg-emerald-500/[0.04] px-3 py-3">
        <p className="text-[9px] font-bold uppercase tracking-widest text-emerald-200/70">Consumption signal</p>
        <p className="mt-2 text-[11px] leading-5 text-white/70">{item.resolutionReason}</p>
      </div>

      <div className="mt-4 flex flex-wrap items-center gap-3 text-[11px]">
        <Link href={`/admin/research/compounds/${toSlug(item.compoundName)}`} className="text-emerald-300 transition-colors hover:text-emerald-200">
          Open compound detail →
        </Link>
      </div>
    </GlassCard>
  );
}

function TaskTags({ categories, requesterIds, aliases }: { categories: string[]; requesterIds: string[]; aliases: string[] }) {
  if (categories.length === 0 && requesterIds.length === 0 && aliases.length === 0) return null;

  return (
    <div className="mt-4 flex flex-wrap gap-2 text-[10px]">
      {categories.map((category) => (
        <span key={category} className="rounded-full border border-violet-400/20 bg-violet-500/10 px-2.5 py-1 text-violet-200">
          {category}
        </span>
      ))}
      {requesterIds.map((requester) => (
        <span key={requester} className="rounded-full border border-white/10 bg-white/[0.04] px-2.5 py-1 text-white/55">
          requester: {requester}
        </span>
      ))}
      {aliases.map((alias) => (
        <span key={alias} className="rounded-full border border-white/10 bg-white/[0.04] px-2.5 py-1 text-white/45">
          alias: {alias}
        </span>
      ))}
    </div>
  );
}

function TaskMeta({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="rounded-xl border border-white/[0.06] bg-white/[0.03] px-3 py-2">
      <p className="text-[9px] uppercase tracking-widest text-white/35">{label}</p>
      <p className="mt-1 break-all text-[11px] font-medium text-white/75">{value}</p>
    </div>
  );
}

function FilterSelect({ label, value, onChange, options }: { label: string; value: string; onChange: (value: string) => void; options: string[] }) {
  return (
    <label className="flex flex-col gap-1 text-[11px] text-white/55">
      <span>{label}</span>
      <select value={value} onChange={(event) => onChange(event.target.value)} className="rounded-lg border border-white/10 bg-black/20 px-3 py-2 text-sm text-white outline-none focus:border-violet-300/40">
        <option value="all">All</option>
        {options.map((option) => (
          <option key={option} value={option}>{option}</option>
        ))}
      </select>
    </label>
  );
}

function TaskStat({ label, value, tone }: { label: string; value: string | number; tone: 'neutral' | 'rose' | 'violet' | 'blue' | 'emerald' }) {
  const toneClass = {
    neutral: 'border-white/10 bg-white/[0.03] text-white/75',
    rose: 'border-rose-400/20 bg-rose-500/10 text-rose-200',
    violet: 'border-violet-400/20 bg-violet-500/10 text-violet-200',
    blue: 'border-blue-400/20 bg-blue-500/10 text-blue-200',
    emerald: 'border-emerald-400/20 bg-emerald-500/10 text-emerald-200',
  }[tone];

  return (
    <div className={`rounded-2xl border px-4 py-3 ${toneClass}`}>
      <p className="text-[9px] uppercase tracking-widest text-white/45">{label}</p>
      <p className="mt-2 text-2xl font-semibold">{value}</p>
    </div>
  );
}
