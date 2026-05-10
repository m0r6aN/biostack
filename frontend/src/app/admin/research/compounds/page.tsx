'use client';
import { Header } from '@/components/Header';
import { CompoundCard } from '@/components/research/CompoundCard';
import { FilterBar } from '@/components/research/FilterBar';
import { getApiBaseUrl } from '@/lib/apiBase';
import { fetchPromotionManifest, fetchResearchSummary } from '@/lib/research/loader';
import { toSlug } from '@/lib/research/slugs';
import type { PromotionManifest, ResearchSummary, ResearchSummaryCompound } from '@/lib/research/types';
import { useRouter, useSearchParams } from 'next/navigation';
import { useEffect, useRef, useState } from 'react';

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
      const [s, m] = await Promise.all([
        fetchResearchSummary(tokenRef.current ?? ''),
        fetchPromotionManifest(tokenRef.current ?? ''),
      ]);
      setSummary(s); setManifest(m);
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

  function openCompound(compound: ResearchSummaryCompound) {
    router.push(`/admin/research/compounds/${toSlug(compound.name)}`);
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
            <ResearchRequestForm onRequested={load} />
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
            empty="No filtered compounds are waiting on initial research."
            onOpen={openCompound}
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

function CompoundLane({ title, subtitle, count, tone, compounds, empty, onOpen }: { title: string; subtitle: string; count: number; tone: 'requested' | 'review' | 'rereview' | 'processing'; compounds: ResearchSummaryCompound[]; empty: string; onOpen: (compound: ResearchSummaryCompound) => void }) {
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
            />
          ))}
        </div>
      )}
    </div>
  );
}

function ResearchRequestForm({ onRequested }: { onRequested: () => Promise<void> }) {
  const [compoundName, setCompoundName] = useState('');
  const [rationale, setRationale] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

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
        body: JSON.stringify({ compoundName, rationale }),
      });
      const payload = await response.json().catch(() => null) as { savedFilename?: string; error?: string } | null;
      if (!response.ok) throw new Error(payload?.error ?? `Research request failed (${response.status}).`);
      setCompoundName('');
      setRationale('');
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
        <textarea value={rationale} onChange={e => setRationale(e.target.value)} placeholder="Why should BioStack research it?" rows={3} className="rounded-lg border border-white/10 bg-black/20 px-3 py-2 text-sm text-white outline-none placeholder:text-white/25 focus:border-violet-300/40" />
        <button onClick={submit} disabled={submitting} className="rounded-lg bg-violet-600 px-3 py-2 text-[11px] font-semibold text-white transition-colors hover:bg-violet-500 disabled:cursor-not-allowed disabled:opacity-50">
          {submitting ? 'Queuing…' : 'Queue Research Request'}
        </button>
      </div>
      {message && <p className="mt-2 text-[11px] text-emerald-200/75">{message}</p>}
      {error && <p className="mt-2 text-[11px] text-rose-300">{error}</p>}
    </div>
  );
}
