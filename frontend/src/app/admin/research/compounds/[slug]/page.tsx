'use client';
import { useEffect, useRef, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { Header } from '@/components/Header';
import { GlassCard } from '@/components/ui/GlassCard';
import { ReadinessBadge } from '@/components/research/ReadinessBadge';
import { BlockerCard } from '@/components/research/BlockerCard';
import { ResolutionPlanItem } from '@/components/research/ResolutionPlanItem';
import { ReviewDecisionForm } from '@/components/research/ReviewDecisionForm';
import { getApiBaseUrl } from '@/lib/apiBase';
import { fetchResearchSummary, fetchPromotionManifest, fetchReviewResolutionPlan } from '@/lib/research/loader';
import { buildSlugMap } from '@/lib/research/slugs';
import { cn } from '@/lib/utils';
import type { ResearchSummaryCompound, PromotionManifestCandidate, ReviewResolutionPlan } from '@/lib/research/types';

type Tab = 'overview' | 'claims' | 'resolution' | 'decision';

const STATUS_COLOR: Record<string, string> = {
  Strong: 'text-emerald-400', Moderate: 'text-blue-400', Limited: 'text-amber-400',
  Insufficient: 'text-rose-400', Unknown: 'text-white/40', Anecdotal: 'text-white/40',
  substantial: 'text-emerald-400', complete: 'text-emerald-400',
  partial: 'text-amber-400', minimal: 'text-rose-400',
  blocked: 'text-rose-400', 'review-required': 'text-amber-400',
  'candidate-for-promotion': 'text-emerald-400',
};

export default function CompoundDetail() {
  const params = useParams<{ slug: string }>();
  const slug = params?.slug ?? '';
  const router = useRouter();
  const tokenRef = useRef<string | null>(null);
  const [compound, setCompound] = useState<ResearchSummaryCompound | null>(null);
  const [candidate, setCandidate] = useState<PromotionManifestCandidate | null>(null);
  const [plan, setPlan] = useState<ReviewResolutionPlan | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [tab, setTab] = useState<Tab>('overview');

  useEffect(() => {
    acquireToken().then(load);
  }, [slug]);

  async function acquireToken() {
    try {
      const res = await fetch(`${getApiBaseUrl()}/api/v1/auth/dev-token`, { method: 'POST' });
      if (res.ok) tokenRef.current = (await res.json()).token;
    } catch { /* no-op */ }
  }

  async function load() {
    const t = tokenRef.current ?? '';
    try {
      const [summary, manifest, resolutionPlan] = await Promise.all([
        fetchResearchSummary(t),
        fetchPromotionManifest(t),
        fetchReviewResolutionPlan(t),
      ]);
      const slugMap = buildSlugMap(summary.compounds);
      const canonicalName = slugMap.get(slug);
      if (!canonicalName) { setNotFound(true); return; }

      const found = summary.compounds.find(c => c.name === canonicalName);
      if (!found) { setNotFound(true); return; }
      setCompound(found);

      const allCandidates = [...manifest.blocked, ...manifest.reviewRequired, ...manifest.candidatesForPromotion];
      setCandidate(allCandidates.find(c => c.name === canonicalName) ?? null);
      setPlan(resolutionPlan);
    } catch {
      setNotFound(true);
    }
  }

  const planItems = plan?.items.filter(i => i.compoundName === compound?.name) ?? [];

  if (notFound) {
    return (
      <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
        <Header title="Compound Not Found" subtitle="" />
        <main className="flex-1 p-6 max-w-4xl mx-auto w-full">
          <p className="text-white/50 mb-4">Compound not found in current research run.</p>
          <button onClick={() => router.push('/admin/research/compounds')} className="text-emerald-400 text-sm hover:text-emerald-300">
            ← Back to compounds
          </button>
        </main>
      </div>
    );
  }

  if (!compound) {
    return (
      <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
        <Header title="Loading…" subtitle="" />
        <main className="flex-1 p-6 flex items-center justify-center">
          <p className="text-white/30 text-sm">Loading compound data…</p>
        </main>
      </div>
    );
  }

  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header title={compound.name} subtitle="Compound Review Detail · Internal" />
      <main className="flex-1 p-4 max-w-5xl mx-auto w-full flex flex-col gap-4">
        <div className="flex items-center gap-2 text-[11px]">
          <button onClick={() => router.push('/admin/research/compounds')} className="text-emerald-400 hover:text-emerald-300">
            ← Compounds
          </button>
          <span className="text-white/30">/ {compound.name}</span>
        </div>

        <div className="flex gap-1 bg-white/[0.04] rounded-xl p-1 w-fit">
          {([
            { key: 'overview',   label: 'Overview' },
            { key: 'claims',     label: 'Claims' },
            { key: 'resolution', label: `Resolution Plan (${planItems.length})` },
            { key: 'decision',   label: 'Review Decision' },
          ] as { key: Tab; label: string }[]).map(t => (
            <button
              key={t.key}
              onClick={() => setTab(t.key)}
              className={cn(
                'text-[11px] px-3 py-1.5 rounded-lg transition-all',
                tab === t.key ? 'bg-white/12 text-white' : 'text-white/40 hover:text-white/70'
              )}
            >
              {t.label}
            </button>
          ))}
        </div>

        {tab === 'overview' && (
          <div className="flex flex-col gap-4">
            <GlassCard className="p-5">
              <div className="flex items-start justify-between mb-4">
                <div>
                  <h2 className="text-xl font-bold text-white">{compound.name}</h2>
                  <p className="text-[11px] text-white/40 mt-0.5">Classification: {compound.classification}</p>
                </div>
                <ReadinessBadge readiness={compound.promotionReadiness} />
              </div>
              <div className="grid grid-cols-3 gap-3">
                {[
                  { label: 'Evidence Tier', value: compound.overallEvidenceTier || 'Unknown' },
                  { label: 'Completeness',  value: compound.completeness || 'Unknown' },
                  { label: 'Readiness',     value: compound.promotionReadiness.replace(/-/g, ' ') },
                  { label: 'Review Queue',  value: `${compound.reviewQueueItemCount} items` },
                  { label: 'Needs Review',  value: compound.needsReview ? 'Yes' : 'No' },
                  { label: 'Decisions',     value: compound.reviewDecisionIds.length > 0 ? String(compound.reviewDecisionIds.length) : 'None' },
                ].map(({ label, value }) => (
                  <div key={label} className="rounded-xl bg-white/[0.03] border border-white/[0.06] px-3 py-2">
                    <p className="text-[9px] text-white/35 uppercase tracking-widest">{label}</p>
                    <p className={`text-[12px] font-semibold mt-0.5 ${STATUS_COLOR[value.split(' ')[0]] ?? 'text-white/80'}`}>
                      {value}
                    </p>
                  </div>
                ))}
              </div>
            </GlassCard>

            {compound.promotionBlockers.length > 0 && (
              <GlassCard className="p-5">
                <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35 mb-3">Promotion Blockers</h3>
                <div className="flex flex-col gap-2">
                  {compound.promotionBlockers.map(b => <BlockerCard key={b} blocker={b} />)}
                </div>
              </GlassCard>
            )}

            {candidate && candidate.requiredNextActions.length > 0 && (
              <GlassCard className="p-5">
                <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35 mb-3">Required Next Actions</h3>
                <div className="flex flex-col gap-2">
                  {candidate.requiredNextActions.map(action => (
                    <div key={action} className="rounded-xl bg-blue-900/15 border border-blue-400/20 px-3 py-2">
                      <p className="text-[11px] text-blue-300">→ {action}</p>
                    </div>
                  ))}
                </div>
              </GlassCard>
            )}

            {compound.qualityFlags.length > 0 && (
              <GlassCard className="p-5">
                <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35 mb-3">Quality Flags</h3>
                <div className="flex flex-wrap gap-2">
                  {compound.qualityFlags.map(flag => (
                    <span key={flag} className={`text-[10px] px-2.5 py-1 rounded-full border ${
                      flag.includes('authoritative') || flag.includes('source-registry')
                        ? 'bg-rose-500/10 border-rose-400/20 text-rose-300'
                        : 'bg-white/[0.05] border-white/10 text-white/50'
                    }`}>{flag}</span>
                  ))}
                </div>
              </GlassCard>
            )}
          </div>
        )}

        {tab === 'claims' && (
          <GlassCard className="p-5">
            <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35 mb-4">Evidence Claims</h3>
            <p className="text-sm text-white/30">
              Evidence packet loading from per-compound files is a v2 feature. Wire up{' '}
              <code className="text-white/50">fetchArtifact(&apos;evidence-packet/&#123;slug&#125;&apos;)</code>{' '}
              when individual packets are available.
            </p>
          </GlassCard>
        )}

        {tab === 'resolution' && (
          <GlassCard className="p-5">
            <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35 mb-4">
              Resolution Plan ({planItems.length})
            </h3>
            {planItems.length === 0 && <p className="text-sm text-white/30">No resolution items for this compound.</p>}
            <div className="flex flex-col gap-3">
              {planItems.map(item => <ResolutionPlanItem key={item.itemId} item={item} />)}
            </div>
          </GlassCard>
        )}

        {tab === 'decision' && candidate && (
          <GlassCard className="p-5">
            <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35 mb-4">
              Review Decision — {compound.name}
            </h3>
            <ReviewDecisionForm candidate={candidate} />
          </GlassCard>
        )}
        {tab === 'decision' && !candidate && (
          <GlassCard className="p-5">
            <p className="text-sm text-white/30">Promotion manifest entry not found for this compound.</p>
          </GlassCard>
        )}
      </main>
    </div>
  );
}
