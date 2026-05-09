'use client';
import { useEffect, useState } from 'react';
import { Header } from '@/components/Header';
import { GlassCard } from '@/components/ui/GlassCard';
import { ResearchStatChip } from '@/components/research/ResearchStatChip';
import { fetchResearchSummary, fetchPromotionManifest, fetchReviewResolutionPlan } from '@/lib/research/loader';
import type { ResearchSummary, PromotionManifest, ReviewResolutionPlan } from '@/lib/research/types';
import Link from 'next/link';

const CATEGORY_COLORS: Record<string, string> = {
  'Safety Critical':               'text-rose-400 bg-rose-400/8 border-rose-400/20',
  'Regulatory / Approval':         'text-violet-400 bg-violet-400/8 border-violet-400/20',
  'Misinformation / Vendor Claims': 'text-orange-400 bg-orange-400/8 border-orange-400/20',
  'Weak or Emerging Human Evidence':'text-amber-400 bg-amber-400/8 border-amber-400/20',
  'Route / Formulation Ambiguity': 'text-blue-400 bg-blue-400/8 border-blue-400/20',
  'Source Registry Authorization': 'text-slate-400 bg-slate-400/8 border-slate-400/20',
};

export default function ResearchDashboard() {
  const [summary, setSummary] = useState<ResearchSummary | null>(null);
  const [manifest, setManifest] = useState<PromotionManifest | null>(null);
  const [plan, setPlan] = useState<ReviewResolutionPlan | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    load();
  }, []);

  async function load() {
    try {
      const [s, m, p] = await Promise.all([
        fetchResearchSummary(''),
        fetchPromotionManifest(''),
        fetchReviewResolutionPlan(''),
      ]);
      setSummary(s); setManifest(m); setPlan(p);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load research data');
    }
  }

  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header title="Research Runs" subtitle="Compound Pipeline Review · Internal" />
      <main className="flex-1 p-6 max-w-6xl mx-auto w-full space-y-6">
        {error && (
          <div className="rounded-xl bg-rose-500/10 border border-rose-400/20 px-4 py-3 text-sm text-rose-300">{error}</div>
        )}

        {/* Stat bar */}
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
          <ResearchStatChip label="Total Drafts"    value={summary?.draftSubstanceCount ?? '—'}          color="neutral" />
          <ResearchStatChip label="Blocked"          value={manifest?.counts.blocked ?? '—'}              color="red" />
          <ResearchStatChip label="Review Req."      value={manifest?.counts.reviewRequired ?? '—'}       color="amber" />
          <ResearchStatChip label="Candidates"       value={manifest?.counts.candidatesForPromotion ?? '—'} color="green" />
          <ResearchStatChip label="Resolution Items" value={plan?.counts.totalItems ?? '—'}               color="blue" />
          <ResearchStatChip label="Queue Items"      value={summary?.reviewQueueItemCount ?? '—'}         color="neutral" />
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Left: Review Categories */}
          <GlassCard className="p-5 flex flex-col gap-3">
            <h3 className="text-[10px] font-bold uppercase tracking-widest text-white/40">Review Categories</h3>
            {!summary && <p className="text-sm text-white/30">Loading...</p>}
            {summary?.reviewCategories.map(cat => (
              <Link
                key={cat.name}
                href={`/admin/research/compounds?category=${encodeURIComponent(cat.name)}`}
                className="flex items-center justify-between hover:opacity-80 transition-opacity"
              >
                <span className="text-[12px] text-white/80">{cat.name}</span>
                <span className={`text-[10px] font-bold px-2 py-0.5 rounded-full border ${CATEGORY_COLORS[cat.name] ?? 'text-white/50 bg-white/5 border-white/10'}`}>
                  {cat.count}
                </span>
              </Link>
            ))}
            {summary?.reviewCategories.length === 0 && (
              <p className="text-sm text-white/30">No categories — all compounds clear.</p>
            )}
          </GlassCard>

          {/* Right: Promotion Readiness + Resolution Summary */}
          <div className="flex flex-col gap-4">
            <GlassCard className="p-5 flex flex-col gap-3">
              <h3 className="text-[10px] font-bold uppercase tracking-widest text-white/40">Promotion Readiness</h3>
              {summary?.promotionReadiness.map(bucket => (
                <div key={bucket.name} className="flex items-center gap-3">
                  <div
                    className={`h-2 rounded-full flex-shrink-0 ${
                      bucket.name === 'blocked' ? 'bg-rose-500'
                      : bucket.name === 'review-required' ? 'bg-amber-500'
                      : 'bg-emerald-500'
                    }`}
                    style={{ width: `${Math.max(8, (bucket.count / (summary.draftSubstanceCount || 1)) * 120)}px` }}
                  />
                  <span className="text-[11px] text-white/60 capitalize">
                    {bucket.name.replace(/-/g, ' ')} ({bucket.count})
                  </span>
                </div>
              ))}
              {!summary && <p className="text-sm text-white/30">Loading...</p>}
            </GlassCard>

            <GlassCard className="p-5 flex flex-col gap-2">
              <h3 className="text-[10px] font-bold uppercase tracking-widest text-white/40">Resolution Plan</h3>
              {plan ? (
                <>
                  <div className="flex gap-4 text-[12px]">
                    <span className="text-white/60">Total: <strong className="text-white">{plan.counts.totalItems}</strong></span>
                    <span className="text-rose-400">Blocked: {plan.counts.blockedItems}</span>
                    <span className="text-amber-400">Review: {plan.counts.reviewRequiredItems}</span>
                  </div>
                  {plan.counts.resolutionTypes.map(rt => (
                    <div key={rt.name} className="flex justify-between text-[11px]">
                      <span className="text-white/50">{rt.name}</span>
                      <span className="text-white/70">{rt.count}</span>
                    </div>
                  ))}
                </>
              ) : (
                <p className="text-sm text-white/30">Loading...</p>
              )}
            </GlassCard>
          </div>
        </div>

        <div className="flex justify-end">
          <Link href="/admin/research/compounds" className="text-[12px] text-emerald-400 hover:text-emerald-300 transition-colors">
            View all compounds →
          </Link>
        </div>
      </main>
    </div>
  );
}
