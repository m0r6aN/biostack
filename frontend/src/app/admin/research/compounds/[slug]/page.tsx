'use client';
import { Header } from '@/components/Header';
import { BlockerCard } from '@/components/research/BlockerCard';
import { CompoundRelationshipPanel, countRelationshipEdgesForCompound } from '@/components/research/CompoundRelationshipPanel';
import { ReadinessBadge } from '@/components/research/ReadinessBadge';
import { ResolutionPlanItem } from '@/components/research/ResolutionPlanItem';
import { ReviewDecisionForm } from '@/components/research/ReviewDecisionForm';
import { GlassCard } from '@/components/ui/GlassCard';
import { getApiBaseUrl } from '@/lib/apiBase';
import { useReviewDecision } from '@/lib/research/ReviewDecisionContext';
import { fetchCompoundGraph, fetchEvidencePacket, fetchPromotionManifest, fetchResearchSummary, fetchResearchTaskQueue, fetchReviewQueue, fetchReviewResolutionPlan } from '@/lib/research/loader';
import { buildSlugMap, toSlug } from '@/lib/research/slugs';
import type { CompoundGraph, EvidencePacket, PromotionManifestCandidate, ResearchReviewQueueItem, ResearchSummaryCompound, ResearchTaskQueue, ReviewDecision, ReviewResolutionPlan, ReviewResolutionPlanItem } from '@/lib/research/types';
import { cn } from '@/lib/utils';
import { useParams, useRouter } from 'next/navigation';
import { useEffect, useRef, useState } from 'react';

type Tab = 'overview' | 'claims' | 'resolution' | 'decision' | 'relationships';

const STATUS_COLOR: Record<string, string> = {
  Strong: 'text-emerald-400', Moderate: 'text-blue-400', Limited: 'text-amber-400',
  Insufficient: 'text-rose-400', Unknown: 'text-white/40', Anecdotal: 'text-white/40',
  substantial: 'text-emerald-400', complete: 'text-emerald-400',
  partial: 'text-amber-400', minimal: 'text-rose-400', requested: 'text-violet-300',
  blocked: 'text-rose-400', 'review-required': 'text-amber-400', 'research-requested': 'text-violet-300',
  'candidate-for-promotion': 'text-emerald-400',
};

export default function CompoundDetail() {
  const params = useParams<{ slug: string }>();
  const slug = params?.slug ?? '';
  const router = useRouter();
  const { batch, reviewerId, setReviewerId, addToSession, removeFromSession } = useReviewDecision();
  const tokenRef = useRef<string | null>(null);
  const [compound, setCompound] = useState<ResearchSummaryCompound | null>(null);
  const [candidate, setCandidate] = useState<PromotionManifestCandidate | null>(null);
  const [evidencePacket, setEvidencePacket] = useState<EvidencePacket | null>(null);
  const [evidencePacketMissing, setEvidencePacketMissing] = useState(false);
  const [reviewQueue, setReviewQueue] = useState<ResearchReviewQueueItem[]>([]);
  const [researchTaskQueue, setResearchTaskQueue] = useState<ResearchTaskQueue | null>(null);
  const [plan, setPlan] = useState<ReviewResolutionPlan | null>(null);
  const [compoundGraph, setCompoundGraph] = useState<CompoundGraph | null>(null);
  const [knownCompoundSlugs, setKnownCompoundSlugs] = useState<ReadonlySet<string>>(new Set());
  const [notFound, setNotFound] = useState(false);
  const [tab, setTab] = useState<Tab>('overview');

  useEffect(() => {
    async function acquireToken() {
      try {
        const res = await fetch(`${getApiBaseUrl()}/api/v1/auth/dev-token`, { method: 'POST' });
        if (res.ok) tokenRef.current = (await res.json()).token;
      } catch { /* no-op */ }
    }

    async function load() {
      const t = tokenRef.current ?? '';
      setNotFound(false);
      setEvidencePacket(null);
      setEvidencePacketMissing(false);
      setReviewQueue([]);
      setResearchTaskQueue(null);
      setCompoundGraph(null);
      try {
        const [summary, manifest, resolutionPlan, queue, taskQueue, graph] = await Promise.all([
          fetchResearchSummary(t),
          fetchPromotionManifest(t),
          fetchReviewResolutionPlan(t),
          fetchReviewQueue(t).catch(() => [] as ResearchReviewQueueItem[]),
          fetchResearchTaskQueue(t).catch(() => null),
          fetchCompoundGraph(t).catch(() => null),
        ]);
        const slugMap = buildSlugMap(summary.compounds);
        setKnownCompoundSlugs(new Set(slugMap.keys()));
        const canonicalName = slugMap.get(slug);
        if (!canonicalName) { setNotFound(true); return; }

        const found = summary.compounds.find(c => c.name === canonicalName);
        if (!found) { setNotFound(true); return; }
        setCompound(found);

        const allCandidates = [...manifest.blocked, ...manifest.reviewRequired, ...(manifest.researchRequested ?? []), ...manifest.candidatesForPromotion];
        setCandidate(allCandidates.find(c => c.name === canonicalName) ?? null);
        setPlan(resolutionPlan);
        setReviewQueue(queue);
        setResearchTaskQueue(taskQueue);
        setCompoundGraph(graph);

        try {
          setEvidencePacket(await fetchEvidencePacket(slug, t));
          setEvidencePacketMissing(false);
        } catch {
          setEvidencePacket(null);
          setEvidencePacketMissing(true);
        }
      } catch {
        setNotFound(true);
      }
    }

    acquireToken().then(load);
  }, [slug]);

  const planItems = plan?.items.filter(i => i.compoundName === compound?.name) ?? [];
  const relationshipCount = compound
    ? countRelationshipEdgesForCompound(compoundGraph, { canonicalName: compound.name, slug })
    : 0;
  const compoundQueueItems = reviewQueue.filter(item => item.compoundName === compound?.name);
  const queuedResearchTask = researchTaskQueue?.items.find(item => item.compoundName === compound?.name) ?? null;
  const consumedResearchTask = (researchTaskQueue?.resolvedItems ?? []).find(item => item.compoundName === compound?.name) ?? null;
  const taskForPlanItem = (itemId: string) => researchTaskQueue?.items.find(item =>
    item.compoundName === compound?.name && (item.remediationPlanItemIds ?? []).includes(itemId)
  ) ?? null;
  const pendingDecisionForPlanItem = (itemId: string) => batch.decisions.find(decision =>
    decision.compoundName === compound?.name
    && decision.decision === 'request-changes'
    && (decision.scope.remediationPlanItemIds ?? []).includes(itemId)
  ) ?? null;

  function toggleNextRoundRemediation(item: ReviewResolutionPlanItem) {
    const pendingDecision = pendingDecisionForPlanItem(item.itemId);
    if (pendingDecision) {
      removeFromSession(pendingDecision.decisionId);
      return;
    }

    const effectiveReviewerId = reviewerId.trim() || 'research-ui';
    if (!reviewerId.trim()) setReviewerId(effectiveReviewerId);
    const decision: ReviewDecision = {
      decisionId: `apply-remediation-${item.itemId}`,
      compoundName: item.compoundName,
      decision: 'request-changes',
      reviewerId: effectiveReviewerId,
      reviewedAt: new Date().toISOString(),
      scope: {
        claimIds: [],
        reviewQueueItemIds: item.relatedReviewQueueItemIds,
        qualityFlags: item.relatedQualityFlags,
        reviewCategories: [],
        promotionBlockers: item.relatedBlockers,
        remediationPlanItemIds: [item.itemId],
        remediationResolutionTypes: [item.resolutionType],
      },
      clearsSoftPromotionBlockers: false,
      expiresAt: null,
      notes: [`Apply remediation in next agent round: ${item.itemId} (${item.resolutionType}). ${item.recommendedAction}`],
    };
    addToSession(decision);
  }

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
            { key: 'resolution', label: `Remediation Plan (${planItems.length})` },
            { key: 'decision',   label: 'Review Decision' },
            { key: 'relationships', label: `Relationships (${relationshipCount})` },
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

            {queuedResearchTask && (
              <GlassCard className="p-5">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35">Queued Evidence Task</h3>
                    <p className="mt-2 text-sm text-white/70">
                      This compound is still waiting for its first evidence packet. Use the queued task below for agent pickup.
                    </p>
                  </div>
                  <div className="flex flex-col items-end gap-2">
                    <span className="rounded-full border border-violet-400/20 bg-violet-500/10 px-2.5 py-1 text-[10px] font-semibold uppercase tracking-[0.18em] text-violet-200">
                      {queuedResearchTask.priority}
                    </span>
                    <button
                      type="button"
                      onClick={() => router.push(`/admin/research/tasks?compound=${encodeURIComponent(toSlug(compound.name))}`)}
                      className="text-[10px] font-semibold uppercase tracking-[0.16em] text-violet-100/80 transition-colors hover:text-violet-100"
                    >
                      Open in task board →
                    </button>
                  </div>
                </div>
                <div className="mt-4 grid gap-3 md:grid-cols-2">
                  <TaskMeta label="Target evidence path" value={queuedResearchTask.targetEvidencePath} />
                  <TaskMeta label="Required schema" value={queuedResearchTask.requiredSchema} />
                  <TaskMeta label="Request IDs" value={queuedResearchTask.requestIds.join(', ') || 'None'} />
                  <TaskMeta label="Requesters" value={queuedResearchTask.requesterIds.join(', ') || 'Unknown'} />
                </div>
                {(queuedResearchTask.categories?.length ?? 0) > 0 && (
                  <div className="mt-4 flex flex-wrap gap-2">
                    {queuedResearchTask.categories?.map((category) => (
                      <span key={category} className="rounded-full border border-violet-400/20 bg-violet-500/10 px-2.5 py-1 text-[10px] text-violet-200">
                        {category}
                      </span>
                    ))}
                  </div>
                )}
                {queuedResearchTask.rationales.length > 0 && (
                  <div className="mt-4 rounded-xl border border-violet-400/15 bg-violet-500/[0.04] px-3 py-3">
                    <p className="text-[9px] font-bold uppercase tracking-widest text-violet-200/70">Rationale</p>
                    <div className="mt-2 flex flex-col gap-2 text-[11px] leading-5 text-white/70">
                      {queuedResearchTask.rationales.map(rationale => <p key={rationale}>{rationale}</p>)}
                    </div>
                  </div>
                )}
                {queuedResearchTask.suggestedResearchDirectives.length > 0 && (
                  <div className="mt-4">
                    <p className="text-[9px] font-bold uppercase tracking-widest text-white/35">Suggested Research Directives</p>
                    <ul className="mt-2 flex flex-col gap-2 text-[11px] leading-5 text-white/65">
                      {queuedResearchTask.suggestedResearchDirectives.slice(0, 3).map(directive => (
                        <li key={directive} className="rounded-xl border border-white/[0.06] bg-white/[0.03] px-3 py-2">
                          → {directive}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </GlassCard>
            )}

            {!queuedResearchTask && consumedResearchTask && (
              <GlassCard className="p-5">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35">Research Task Consumed</h3>
                    <p className="mt-2 text-sm text-white/70">
                      The initial research request cleared from the active queue on the latest run because evidence is now in the pipeline.
                    </p>
                  </div>
                  <span className="rounded-full border border-emerald-400/20 bg-emerald-500/10 px-2.5 py-1 text-[10px] font-semibold uppercase tracking-[0.18em] text-emerald-200">
                    {consumedResearchTask.currentReadiness}
                  </span>
                </div>
                <div className="mt-4 grid gap-3 md:grid-cols-2">
                  <TaskMeta label="Target evidence path" value={consumedResearchTask.targetEvidencePath} />
                  <TaskMeta label="Resolution" value={consumedResearchTask.resolution} />
                  <TaskMeta label="Request IDs" value={consumedResearchTask.requestIds.join(', ') || 'None'} />
                  <TaskMeta label="Requesters" value={consumedResearchTask.requesterIds.join(', ') || 'Unknown'} />
                </div>
                {(consumedResearchTask.categories?.length ?? 0) > 0 && (
                  <div className="mt-4 flex flex-wrap gap-2">
                    {consumedResearchTask.categories?.map((category) => (
                      <span key={category} className="rounded-full border border-emerald-400/20 bg-emerald-500/10 px-2.5 py-1 text-[10px] text-emerald-200">
                        {category}
                      </span>
                    ))}
                  </div>
                )}
                <div className="mt-4 rounded-xl border border-emerald-400/15 bg-emerald-500/[0.04] px-3 py-3">
                  <p className="text-[9px] font-bold uppercase tracking-widest text-emerald-200/70">Consumption signal</p>
                  <p className="mt-2 text-[11px] leading-5 text-white/70">{consumedResearchTask.resolutionReason}</p>
                </div>
              </GlassCard>
            )}

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
          <EvidenceClaimsPanel packet={evidencePacket} missing={evidencePacketMissing} />
        )}

        {tab === 'resolution' && (
          <GlassCard className="p-5">
            <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35 mb-4">
              Suggested Remediation Plan ({planItems.length})
            </h3>
            <p className="mb-4 rounded-xl border border-blue-400/20 bg-blue-500/10 px-3 py-2 text-[11px] leading-5 text-blue-100/70">
              These generated remediation items explain what must be fixed before promotion. Items with a checked next-round control are already carried by a generated research task.
            </p>
            {planItems.length === 0 && <p className="text-sm text-white/30">No resolution items for this compound.</p>}
            <div className="flex flex-col gap-3">
              {planItems.map(item => {
                const queuedTask = taskForPlanItem(item.itemId);
                const pendingDecision = pendingDecisionForPlanItem(item.itemId);
                return <ResolutionPlanItem key={item.itemId} item={item} queuedTask={queuedTask} pendingDecisionId={pendingDecision?.decisionId ?? null} onToggleNextRound={queuedTask ? undefined : () => toggleNextRoundRemediation(item)} />;
              })}
            </div>
          </GlassCard>
        )}

        {tab === 'decision' && candidate && (
          <GlassCard className="p-5">
            <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35 mb-4">
              Review Decision — {compound.name}
            </h3>
            <ReviewDecisionForm candidate={candidate} compound={compound} evidencePacket={evidencePacket} planItems={planItems} reviewQueueItems={compoundQueueItems} />
          </GlassCard>
        )}
        {tab === 'decision' && !candidate && (
          <GlassCard className="p-5">
            <p className="text-sm text-white/30">Promotion manifest entry not found for this compound.</p>
          </GlassCard>
        )}

        {tab === 'relationships' && (
          <CompoundRelationshipPanel
            graph={compoundGraph}
            compound={{ canonicalName: compound.name, slug }}
            knownSlugs={knownCompoundSlugs}
          />
        )}
      </main>
    </div>
  );
}

function TaskMeta({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl bg-white/[0.03] border border-white/[0.06] px-3 py-2">
      <p className="text-[9px] text-white/35 uppercase tracking-widest">{label}</p>
      <p className="mt-1 break-all text-[11px] font-medium text-white/75">{value}</p>
    </div>
  );
}

function EvidenceClaimsPanel({ packet, missing }: { packet: EvidencePacket | null; missing: boolean }) {
  if (missing) {
    return (
      <GlassCard className="p-5">
        <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35 mb-4">Evidence Claims</h3>
        <p className="text-sm text-white/30">Research artifacts not yet generated for this compound evidence packet.</p>
      </GlassCard>
    );
  }

  if (!packet) {
    return (
      <GlassCard className="p-5">
        <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35 mb-4">Evidence Claims</h3>
        <p className="text-sm text-white/30">Loading evidence packet…</p>
      </GlassCard>
    );
  }

  return (
    <GlassCard className="p-5">
      <div className="mb-4 flex items-start justify-between gap-4">
        <div>
          <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35">Evidence Claims</h3>
          <p className="mt-1 text-[11px] text-white/35">{packet.claims.length} claims · {packet.sources.length} sources</p>
        </div>
        <span className={`text-[10px] font-semibold ${STATUS_COLOR[packet.ops.completeness] ?? 'text-white/50'}`}>
          {packet.ops.completeness}
        </span>
      </div>

      <div className="mb-4 rounded-xl border border-blue-400/20 bg-blue-500/10 px-3 py-2 text-[11px] leading-5 text-blue-100/70">
        Evidence support describes the strength of the underlying evidence. Extraction confidence describes how confident the packet is that the statement accurately reflects its sources. “Insufficient + high confidence” means the evidence gap is clear, not that the therapeutic claim is proven.
      </div>

      <div className="flex flex-col gap-3">
        {packet.claims.map(claim => {
          const sources = claim.sourceRefs
            .map(ref => packet.sources.find(source => source.sourceId === ref))
            .filter(Boolean);

          return (
            <div key={claim.claimId} className="rounded-2xl border border-white/[0.06] bg-white/[0.03] p-4">
              <div className="mb-3 flex flex-wrap items-center gap-2">
                <span className="rounded-full border border-white/10 bg-white/[0.05] px-2 py-1 text-[10px] text-white/55">{claim.claimType}</span>
                <span className={`text-[10px] font-semibold ${STATUS_COLOR[claim.evidenceTier] ?? 'text-white/50'}`}>Evidence support: {claim.evidenceTier}</span>
                <span className="text-[10px] text-white/35">Extraction confidence: {claim.confidence}</span>
              </div>
              <p className="mb-2 font-mono text-[10px] text-white/30">Claim ID: {claim.claimId}</p>
              <p className="text-sm leading-6 text-white/75">{claim.statement}</p>

              {claim.extractedEvidence.length > 0 && (
                <div className="mt-3 space-y-2">
                  {claim.extractedEvidence.map((evidence, index) => (
                    <p key={`${claim.claimId}-evidence-${index}`} className="border-l border-emerald-300/25 pl-3 text-[11px] leading-5 text-white/45">
                      {evidence.quote ?? evidence.pageOrSection ?? evidence.sourceRef}
                    </p>
                  ))}
                </div>
              )}

              {sources.length > 0 && (
                <div className="mt-3 flex flex-wrap gap-2">
                  <span className="w-full text-[9px] font-bold uppercase tracking-widest text-white/25">Sources used by this claim</span>
                  {sources.map(source => source && (
                    <span key={source.sourceId} className="rounded-full bg-blue-900/15 px-2 py-1 text-[10px] text-blue-200/80">
                      {source.authorityTier} · {source.title}
                    </span>
                  ))}
                </div>
              )}

              {claim.reviewFlags.length > 0 && (
                <div className="mt-3 flex flex-wrap gap-2">
                  <span className="w-full text-[9px] font-bold uppercase tracking-widest text-amber-200/50">Reviewer guardrails — not auto-approved output</span>
                  {claim.reviewFlags.map(flag => <span key={flag} className="rounded-full border border-amber-300/20 bg-amber-500/10 px-2 py-1 text-[10px] text-amber-200/80">{flag}</span>)}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </GlassCard>
  );
}
