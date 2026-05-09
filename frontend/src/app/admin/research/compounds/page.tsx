'use client';
import { useEffect, useRef, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Header } from '@/components/Header';
import { FilterBar } from '@/components/research/FilterBar';
import { CompoundCard } from '@/components/research/CompoundCard';
import { getApiBaseUrl } from '@/lib/apiBase';
import { fetchResearchSummary, fetchPromotionManifest } from '@/lib/research/loader';
import { toSlug } from '@/lib/research/slugs';
import type { ResearchSummary, PromotionManifest, ResearchSummaryCompound } from '@/lib/research/types';

function sortCompounds(compounds: ResearchSummaryCompound[], sort: string): ResearchSummaryCompound[] {
  const copy = [...compounds];
  if (sort === 'name') return copy.sort((a, b) => a.name.localeCompare(b.name));
  if (sort === 'tier') return copy.sort((a, b) => a.overallEvidenceTier.localeCompare(b.overallEvidenceTier));
  if (sort === 'completeness') return copy.sort((a, b) => a.completeness.localeCompare(b.completeness));
  const order: Record<string, number> = { 'blocked': 0, 'review-required': 1, 'candidate-for-promotion': 2 };
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

  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header title="Compound Review" subtitle="Research pipeline triage queue · Internal" />
      <main className="flex-1 p-4 max-w-7xl mx-auto w-full flex gap-4 overflow-hidden" style={{ height: 'calc(100vh - 80px)' }}>
        <div className="w-80 flex-shrink-0 flex flex-col gap-3 overflow-y-auto">
          {summary && manifest && (
            <FilterBar
              blockedCount={manifest.counts.blocked}
              reviewCount={manifest.counts.reviewRequired}
              candidateCount={manifest.counts.candidatesForPromotion}
              categories={summary.reviewCategories}
            />
          )}
          {loading && <p className="text-sm text-white/30 px-1">Loading...</p>}
          {error && <p className="text-sm text-rose-300 px-1">{error}</p>}
          {!loading && sorted.length === 0 && (
            <p className="text-sm text-white/30 px-1">No compounds match the current filters.</p>
          )}
          <div className="flex flex-col gap-2">
            {sorted.map(compound => (
              <CompoundCard
                key={compound.name}
                compound={compound}
                selected={false}
                onClick={() => router.push(`/admin/research/compounds/${toSlug(compound.name)}`)}
              />
            ))}
          </div>
        </div>
        <div className="flex-1 flex items-center justify-center text-white/20 text-sm">
          Select a compound to review
        </div>
      </main>
    </div>
  );
}
