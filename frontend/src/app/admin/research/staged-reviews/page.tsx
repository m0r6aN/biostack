'use client';

import { Header } from '@/components/Header';
import { GlassCard } from '@/components/ui/GlassCard';
import { SaveToBioStackPanel } from '@/components/research/SaveToBioStackPanel';
import { getApiBaseUrl } from '@/lib/apiBase';
import type { StagedTranscriptCandidateReview } from '@/lib/types';
import { useEffect, useRef, useState } from 'react';

// ── Types ─────────────────────────────────────────────────────────────────

type FilterKey = 'all' | 'approved' | 'promoted';

const FILTER_LABELS: Record<FilterKey, string> = {
  all: 'All',
  approved: 'Approved for Promotion',
  promoted: 'Already Promoted',
};

const REVIEW_STATE_APPROVED = 'review_approved_for_promotion';

// ── Sub-components ────────────────────────────────────────────────────────

function MetaRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex gap-2 items-baseline">
      <span className="text-white/30 shrink-0">{label}</span>
      <span className="text-white/60 truncate">{value}</span>
    </div>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────

export default function StagedReviewsPage() {
  const devTokenRef = useRef<string | null>(null);
  const [reviews, setReviews] = useState<StagedTranscriptCandidateReview[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState<FilterKey>('all');

  useEffect(() => {
    acquireToken().then(loadReviews);
  }, []);

  async function acquireToken(): Promise<void> {
    if (devTokenRef.current) return;
    try {
      const res = await fetch(`${getApiBaseUrl()}/api/v1/auth/dev-token`, { method: 'POST' });
      if (res.ok) {
        const { token } = await res.json();
        devTokenRef.current = token;
      }
    } catch {
      // Endpoint absent in production — silently skip
    }
  }

  function getAuthHeaders(): Record<string, string> {
    return devTokenRef.current
      ? { Authorization: `Bearer ${devTokenRef.current}` }
      : {};
  }

  async function loadReviews(): Promise<void> {
    setLoading(true);
    setError('');
    try {
      const res = await fetch(
        `${getApiBaseUrl()}/api/v1/admin/staged-transcript-candidate-reviews`,
        { headers: getAuthHeaders() }
      );
      if (!res.ok) {
        setError(`Failed to load: ${res.status}`);
        return;
      }
      const data: StagedTranscriptCandidateReview[] = await res.json();
      setReviews(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load reviews');
    } finally {
      setLoading(false);
    }
  }

  const filtered = reviews.filter(r => {
    if (filter === 'approved') return r.reviewState === REVIEW_STATE_APPROVED && !r.promotedKnowledgeEntryId;
    if (filter === 'promoted') return !!r.promotedKnowledgeEntryId;
    return true;
  });

  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header
        title="Staged Transcript Reviews"
        subtitle="Review, approve, and promote knowledge entries to BioStack"
      />
      <main className="flex-1 p-6 max-w-5xl mx-auto w-full flex flex-col gap-4">

        {/* Filter bar */}
        <div className="flex items-center gap-2">
          {(Object.keys(FILTER_LABELS) as FilterKey[]).map(key => (
            <button
              key={key}
              onClick={() => setFilter(key)}
              className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-colors ${
                filter === key
                  ? 'bg-white/10 text-white'
                  : 'text-white/40 hover:text-white/70'
              }`}
            >
              {FILTER_LABELS[key]}
            </button>
          ))}
          {!loading && (
            <span className="ml-auto text-xs text-white/30">
              {filtered.length} {filtered.length === 1 ? 'review' : 'reviews'}
            </span>
          )}
        </div>

        {/* Loading */}
        {loading && (
          <p className="text-sm text-white/40 animate-pulse">Loading staged reviews…</p>
        )}

        {/* Error */}
        {!loading && error && (
          <GlassCard className="p-4">
            <p className="text-sm text-rose-300">{error}</p>
            <button
              onClick={() => acquireToken().then(loadReviews)}
              className="mt-2 text-xs text-white/40 hover:text-white/60 underline"
            >
              Retry
            </button>
          </GlassCard>
        )}

        {/* Empty */}
        {!loading && !error && filtered.length === 0 && (
          <p className="text-sm text-white/30 italic">No reviews match the current filter.</p>
        )}

        {/* Review cards */}
        {!loading && !error && filtered.map(review => (
          <GlassCard key={review.artifactId} className="p-5 flex flex-col gap-4">

            {/* Review header */}
            <div className="flex items-start justify-between gap-4">
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-white/80 truncate">
                  {review.targetCanonicalName ?? (
                    <span className="text-amber-400 italic">Target not assigned</span>
                  )}
                </p>
                <p className="mt-0.5 font-mono text-[10px] text-white/30 truncate">
                  {review.artifactId}
                </p>
              </div>
              <span className={`shrink-0 rounded-full px-2 py-0.5 text-[9px] font-bold uppercase tracking-widest ${
                review.promotedKnowledgeEntryId
                  ? 'bg-emerald-500/15 text-emerald-400'
                  : review.reviewState === REVIEW_STATE_APPROVED
                    ? 'bg-blue-500/15 text-blue-400'
                    : 'bg-white/5 text-white/30'
              }`}>
                {review.promotedKnowledgeEntryId
                  ? 'promoted'
                  : review.reviewState.replace(/_/g, ' ')}
              </span>
            </div>

            {/* Review metadata */}
            <div className="grid grid-cols-2 gap-x-6 gap-y-1 text-[11px]">
              <MetaRow label="Source" value={review.sourceType} />
              <MetaRow label="Provider" value={review.provider} />
              <MetaRow label="Segments" value={String(review.segmentCount)} />
              <MetaRow label="Canonicality" value={review.canonicality} />
              {review.sourceUrl && (
                <div className="col-span-2">
                  <MetaRow
                    label="URL"
                    value={
                      <a
                        href={review.sourceUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="block max-w-xs truncate text-blue-400 underline underline-offset-2 hover:text-blue-300"
                      >
                        {review.sourceUrl}
                      </a>
                    }
                  />
                </div>
              )}
            </div>

            {/* Promotion panel */}
            <SaveToBioStackPanel
              artifactId={review.artifactId}
              review={review}
              authHeaders={getAuthHeaders()}
            />

          </GlassCard>
        ))}

      </main>
    </div>
  );
}
