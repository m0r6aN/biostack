'use client';
import { Header } from '@/components/Header';
import { CompoundCard } from '@/components/research/CompoundCard';
import { FilterBar } from '@/components/research/FilterBar';
import { getApiBaseUrl } from '@/lib/apiBase';
import { appendResearchCategory, getResearchCategoryPresets, normalizeResearchCategories } from '@/lib/research/categoryRegistry';
import { fetchPromotionManifest, fetchResearchCategoryTaxonomy, fetchResearchSummary, fetchResearchTaskQueue } from '@/lib/research/loader';
import { toSlug } from '@/lib/research/slugs';
import type { PromotionManifest, ResearchCategoryTaxonomy, ResearchSummary, ResearchSummaryCompound, ResearchTaskQueue } from '@/lib/research/types';
import { useRouter, useSearchParams } from 'next/navigation';
import { useEffect, useMemo, useRef, useState } from 'react';

function sortCompounds(compounds: ResearchSummaryCompound[], sort: string): ResearchSummaryCompound[] {
  const copy = [...compounds];
  if (sort === 'name') return copy.sort((a, b) => a.name.localeCompare(b.name));
  if (sort === 'tier') return copy.sort((a, b) => a.overallEvidenceTier.localeCompare(b.overallEvidenceTier));
  if (sort === 'completeness') return copy.sort((a, b) => a.completeness.localeCompare(b.completeness));
  const order: Record<string, number> = { 'research-requested': 0, 'blocked': 1, 'review-required': 2, 'candidate-for-promotion': 3 };
  return copy.sort((a, b) => {
    const diff = (order[a.promotionReadiness] ?? 3) - (order[b.promotionReadiness] ?? 3);
    return diff !== 0 ? diff : b.reviewQueueItemCount - a.reviewQueueItemCount;
  });
}

export default function CompoundList() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [summary, setSummary] = useState<ResearchSummary | null>(null);
  const [manifest, setManifest] = useState<PromotionManifest | null>(null);
  const [taskQueue, setTaskQueue] = useState<ResearchTaskQueue | null>(null);
  const [categoryTaxonomy, setCategoryTaxonomy] = useState<ResearchCategoryTaxonomy | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const tokenRef = useRef<string | null>(null);

  useEffect(() => {
    acquireToken().then(load);
  }, []);

  async function acquireToken() {
    try {
      const res = await fetch(`${getApiBaseUrl()}/api/v1/auth/dev-token`, { method: 'POST' });
      if (res.ok) tokenRef.current = (await res.json()).token;
    } catch { /* no-op */ }
  }

  async function load() {
    try {
      const [s, m, q, taxonomy] = await Promise.all([
        fetchResearchSummary(tokenRef.current ?? ''),
        fetchPromotionManifest(tokenRef.current ?? ''),
        fetchResearchTaskQueue(tokenRef.current ?? '').catch(() => null),
        fetchResearchCategoryTaxonomy(tokenRef.current ?? '').catch(() => null),
      ]);
      setSummary(s); setManifest(m); setTaskQueue(q); setCategoryTaxonomy(taxonomy);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load');
    } finally {
      setLoading(false);
    }
  }

  const readinessFilter = searchParams.getAll('readiness');
  const categoryFilter = searchParams.getAll('category');
  const tierFilter = searchParams.getAll('tier');
  const sort = searchParams.get('sort') ?? 'risk';

  const filtered = (summary?.compounds ?? []).filter(c => {
    if (readinessFilter.length > 0 && !readinessFilter.includes(c.promotionReadiness)) return false;
    if (tierFilter.length > 0 && !tierFilter.includes(c.overallEvidenceTier)) return false;
    if (categoryFilter.length > 0) {
      const compoundCategories = summary?.reviewCategories
        .filter(cat => cat.compounds.includes(c.name))
        .map(cat => cat.name) ?? [];
      if (!categoryFilter.some(f => compoundCategories.includes(f))) return false;
    }
    return true;
  });

  const sorted = sortCompounds(filtered, sort);
  const researchRequested = sorted.filter(c => c.promotionReadiness === 'research-requested');
  const readyForReReview = sorted.filter(c => c.hasRequestedChanges && c.promotionReadiness === 'review-required');
  const readyForReview = sorted.filter(c => !['candidate-for-promotion', 'research-requested'].includes(c.promotionReadiness) && !readyForReReview.includes(c));
  const readyForProcessing = sorted.filter(c => c.promotionReadiness === 'candidate-for-promotion');
  const requestedNames = new Set(researchRequested.map(compound => compound.name));
  const queuedResearchTaskCount = (taskQueue?.items ?? []).filter(item => requestedNames.has(item.compoundName)).length;
  const queuedTaskNames = new Set((taskQueue?.items ?? []).map(item => item.compoundName));

  function openCompound(compound: ResearchSummaryCompound) {
    router.push(`/admin/research/compounds/${toSlug(compound.name)}`);
  }

  function openTaskBoard(compound?: ResearchSummaryCompound) {
    const query = compound ? `?compound=${encodeURIComponent(toSlug(compound.name))}` : '';
    router.push(`/admin/research/tasks${query}`);
  }

  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header title="Compound Review" subtitle="Research pipeline triage queue · Internal" />
      <main className="flex-1 p-4 max-w-7xl mx-auto w-full flex flex-col gap-4">
        {summary && manifest && (
          <section className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_360px]">
            <FilterBar
              researchRequestedCount={manifest.counts.researchRequested ?? summary.researchRequestCount ?? 0}
              blockedCount={manifest.counts.blocked}
              reviewCount={manifest.counts.reviewRequired}
              candidateCount={manifest.counts.candidatesForPromotion}
              categories={summary.reviewCategories}
            />
            <ResearchRequestForm onRequested={load} categoryTaxonomy={categoryTaxonomy} />
          </section>
        )}

        {loading && <p className="text-sm text-white/30 px-1">Loading...</p>}
        {error && <p className="text-sm text-rose-300 px-1">{error}</p>}
        {!loading && sorted.length === 0 && (
          <p className="text-sm text-white/30 px-1">No compounds match the current filters.</p>
        )}

        <section className="grid flex-1 gap-4 xl:grid-cols-2 2xl:grid-cols-4">
          <CompoundLane
            title="Research Requested"
            subtitle="New compounds queued for initial evidence research."
            count={researchRequested.length}
            tone="requested"
            compounds={researchRequested}
            queuedTaskCount={queuedResearchTaskCount}
            empty="No filtered compounds are waiting on initial research."
            onOpen={openCompound}
            onOpenTaskBoard={openTaskBoard}
            taskBoardCompounds={queuedTaskNames}
          />
          <CompoundLane
            title="Ready for Review"
            subtitle="Blocked or review-required drafts that still need human decisions."
            count={readyForReview.length}
            tone="review"
            compounds={readyForReview}
            empty="No filtered compounds need review."
            onOpen={openCompound}
          />
          <CompoundLane
            title="Ready for Re-review"
            subtitle="Requested changes have been recorded; verify the updated research before promotion."
            count={readyForReReview.length}
            tone="rereview"
            compounds={readyForReReview}
            empty="No filtered compounds are awaiting re-review."
            onOpen={openCompound}
          />
          <CompoundLane
            title="Ready for Processing"
            subtitle="Candidates cleared for the next worker/import processing step."
            count={readyForProcessing.length}
            tone="processing"
            compounds={readyForProcessing}
            empty="No filtered compounds are ready for processing."
            onOpen={openCompound}
          />
        </section>
      </main>
    </div>
  );
}

function CompoundLane({ title, subtitle, count, tone, compounds, empty, onOpen, queuedTaskCount = 0, onOpenTaskBoard, taskBoardCompounds }: { title: string; subtitle: string; count: number; tone: 'requested' | 'review' | 'rereview' | 'processing'; compounds: ResearchSummaryCompound[]; empty: string; onOpen: (compound: ResearchSummaryCompound) => void; queuedTaskCount?: number; onOpenTaskBoard?: (compound?: ResearchSummaryCompound) => void; taskBoardCompounds?: ReadonlySet<string> }) {
  const toneClass = {
    processing: 'border-emerald-400/20 bg-emerald-500/[0.04] text-emerald-200',
    rereview: 'border-sky-400/20 bg-sky-500/[0.04] text-sky-200',
    requested: 'border-violet-400/20 bg-violet-500/[0.04] text-violet-200',
    review: 'border-amber-400/20 bg-amber-500/[0.04] text-amber-200',
  }[tone];

  return (
    <div className="min-h-[420px] rounded-2xl border border-white/10 bg-white/[0.025] p-4">
      <div className="mb-4 flex items-start justify-between gap-3">
        <div>
          <h2 className="text-sm font-semibold text-white">{title}</h2>
          <p className="mt-1 text-[11px] leading-5 text-white/40">{subtitle}</p>
          {queuedTaskCount > 0 && tone === 'requested' && (
            <div className="mt-2 flex flex-wrap items-center gap-3">
              <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-violet-200/75">
                {queuedTaskCount} evidence task{queuedTaskCount === 1 ? '' : 's'} queued for agent pickup.
              </p>
              {onOpenTaskBoard && (
                <button
                  type="button"
                  onClick={() => onOpenTaskBoard()}
                  className="text-[10px] font-semibold uppercase tracking-[0.16em] text-violet-100/80 transition-colors hover:text-violet-100"
                >
                  Open task board →
                </button>
              )}
            </div>
          )}
        </div>
        <span className={`rounded-full border px-2.5 py-1 text-[11px] font-semibold ${toneClass}`}>{count}</span>
      </div>

      {compounds.length === 0 ? (
        <div className="flex min-h-40 items-center justify-center rounded-xl border border-dashed border-white/10 bg-black/10 px-4 text-center text-sm text-white/25">
          {empty}
        </div>
      ) : (
        <div className="flex flex-col gap-2">
          {compounds.map(compound => (
            <CompoundCard
              key={compound.name}
              compound={compound}
              selected={false}
              onClick={() => onOpen(compound)}
              secondaryAction={onOpenTaskBoard && taskBoardCompounds?.has(compound.name)
                ? { label: 'Open Task Board', onClick: () => onOpenTaskBoard(compound) }
                : undefined}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function ResearchRequestForm({ onRequested, categoryTaxonomy }: { onRequested: () => Promise<void>; categoryTaxonomy: ResearchCategoryTaxonomy | null }) {
  const [compoundName, setCompoundName] = useState('');
  const [rationale, setRationale] = useState('');
  const [notes, setNotes] = useState('');
  const [categories, setCategories] = useState('');
  const [requesterId, setRequesterId] = useState('research-ui');
  const [priority, setPriority] = useState<'low' | 'normal' | 'high' | 'urgent'>('normal');
  const [submitting, setSubmitting] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const categoryPresets = useMemo(() => getResearchCategoryPresets(categoryTaxonomy), [categoryTaxonomy]);
  const normalizedCategories = useMemo(() => normalizeResearchCategories(categoryTaxonomy, categories), [categoryTaxonomy, categories]);

  function addCategoryPreset(category: string) {
    setCategories((current) => appendResearchCategory(categoryTaxonomy, current, category));
  }

  async function submit() {
    setError('');
    setMessage('');
    if (!compoundName.trim() || !rationale.trim()) {
      setError('Compound name and research rationale are required.');
      return;
    }
    setSubmitting(true);
    try {
      const response = await fetch('/api/research/request-compound', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ compoundName, rationale, notes, categories, requesterId, priority }),
      });
      const payload = await response.json().catch(() => null) as { savedFilename?: string; error?: string } | null;
      if (!response.ok) throw new Error(payload?.error ?? `Research request failed (${response.status}).`);
      setCompoundName('');
      setRationale('');
      setNotes('');
      setCategories('');
      setMessage(`Queued ${payload?.savedFilename ?? 'research request'} and regenerated artifacts.`);
      await onRequested();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Research request failed.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="rounded-2xl border border-violet-400/15 bg-violet-500/[0.035] p-3">
      <p className="text-[10px] font-bold uppercase tracking-widest text-violet-200/70">Request Research</p>
      <p className="mt-1 text-[11px] leading-5 text-white/40">Queue a new compound for initial evidence research.</p>
      <div className="mt-3 flex flex-col gap-2">
        <input value={compoundName} onChange={e => setCompoundName(e.target.value)} placeholder="Compound name" className="rounded-lg border border-white/10 bg-black/20 px-3 py-2 text-sm text-white outline-none placeholder:text-white/25 focus:border-violet-300/40" />
        <div className="grid gap-2 md:grid-cols-2">
          <input value={requesterId} onChange={e => setRequesterId(e.target.value)} placeholder="Requester ID" className="rounded-lg border border-white/10 bg-black/20 px-3 py-2 text-sm text-white outline-none placeholder:text-white/25 focus:border-violet-300/40" />
          <select value={priority} onChange={e => setPriority(e.target.value as 'low' | 'normal' | 'high' | 'urgent')} className="rounded-lg border border-white/10 bg-black/20 px-3 py-2 text-sm text-white outline-none focus:border-violet-300/40">
            <option value="normal">Priority: normal</option>
            <option value="high">Priority: high</option>
            <option value="urgent">Priority: urgent</option>
            <option value="low">Priority: low</option>
          </select>
        </div>
        <input list="research-category-presets" value={categories} onChange={e => setCategories(e.target.value)} placeholder="Categories (comma-separated, e.g. Nootropics)" className="rounded-lg border border-white/10 bg-black/20 px-3 py-2 text-sm text-white outline-none placeholder:text-white/25 focus:border-violet-300/40" />
        <datalist id="research-category-presets">
          {categoryPresets.map((category) => (
            <option key={category} value={category} />
          ))}
        </datalist>
        <div className="rounded-xl border border-white/10 bg-white/[0.03] px-3 py-3">
          <p className="text-[9px] font-bold uppercase tracking-widest text-white/35">Category presets</p>
          <div className="mt-2 flex flex-wrap gap-2">
            {categoryPresets.map((category) => (
              <button
                key={category}
                type="button"
                onClick={() => addCategoryPreset(category)}
                className="rounded-full border border-violet-400/20 bg-violet-500/10 px-2.5 py-1 text-[10px] text-violet-200 transition-colors hover:bg-violet-500/20"
              >
                {category}
              </button>
            ))}
          </div>
          <p className="mt-2 text-[10px] leading-5 text-white/40">Freeform categories are still allowed, but presets are normalized to reduce taxonomy drift.</p>
          {normalizedCategories.length > 0 && (
            <div className="mt-3 flex flex-wrap gap-2">
              {normalizedCategories.map((category) => (
                <span key={category} className="rounded-full border border-violet-400/20 bg-violet-500/10 px-2.5 py-1 text-[10px] text-violet-200">
                  {category}
                </span>
              ))}
            </div>
          )}
        </div>
        <textarea value={rationale} onChange={e => setRationale(e.target.value)} placeholder="Why should BioStack research it?" rows={3} className="rounded-lg border border-white/10 bg-black/20 px-3 py-2 text-sm text-white outline-none placeholder:text-white/25 focus:border-violet-300/40" />
        <textarea value={notes} onChange={e => setNotes(e.target.value)} placeholder="Optional notes for the evidence agent" rows={2} className="rounded-lg border border-white/10 bg-black/20 px-3 py-2 text-sm text-white outline-none placeholder:text-white/25 focus:border-violet-300/40" />
        <button onClick={submit} disabled={submitting} className="rounded-lg bg-violet-600 px-3 py-2 text-[11px] font-semibold text-white transition-colors hover:bg-violet-500 disabled:cursor-not-allowed disabled:opacity-50">
          {submitting ? 'Queuing…' : 'Queue Research Request'}
        </button>
      </div>
      {message && <p className="mt-2 text-[11px] text-emerald-200/75">{message}</p>}
      {error && <p className="mt-2 text-[11px] text-rose-300">{error}</p>}
    </div>
  );
}
