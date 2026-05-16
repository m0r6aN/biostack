'use client';

import { useEffect, useState, useCallback } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { apiClient } from '@/lib/api';
import { isEnabled } from '@/lib/flags';
import { SafetyHierarchy } from '@/components/intel/SafetyHierarchy';
import { CognitiveHeatGauge } from '@/components/governance/CognitiveHeatGauge';
import { ConfidenceProfileCard } from '@/components/governance/ConfidenceProfileCard';
import { ClaimBadgeStack } from '@/components/governance/ClaimBadgeStack';
import { GovernedSentence } from '@/components/governance/GovernedSentence';
import { ReceiptBadge } from '@/components/governance/ReceiptBadge';
import { ReceiptDrawer } from '@/components/governance/ReceiptDrawer';
import { WitnessNarrativePanel } from '@/components/governance/WitnessNarrativePanel';
import { ReasoningGraphViewer } from '@/components/governance/ReasoningGraphViewer';
import { cn } from '@/lib/utils';
import type {
  Protocol,
  SrbEnvelopeResponse,
  SrbEnvelopeDeterministic,
  SrbEnvelopeFinding,
  ProtocolReviewCompletedEvent,
} from '@/lib/types';

// ── Helpers ─────────────────────────────────────────────────────────────────

function parseProfileValue(v: string): number {
  const n = parseFloat(v);
  if (!isNaN(n)) return Math.min(1, Math.max(0, n));
  const map: Record<string, number> = {
    high: 0.85, elevated: 0.7, moderate: 0.55, low: 0.25, minimal: 0.1, none: 0, unknown: 0.5,
  };
  return map[v.toLowerCase()] ?? 0.5;
}

function deriveHeat(contradictionDensity: string) {
  const v = parseProfileValue(contradictionDensity);
  const band = v >= 0.8 ? 'critical' : v >= 0.6 ? 'high' : v >= 0.4 ? 'elevated' : 'nominal';
  return { value: v, band, throttlingActive: v >= 0.6 } as const;
}

function severityColor(severity: string): string {
  switch (severity.toLowerCase()) {
    case 'critical': return 'text-red-400';
    case 'warning':  return 'text-amber-400';
    default:         return 'text-white/50';
  }
}

// ── Sub-components ───────────────────────────────────────────────────────────

function DeterministicSection({ findings }: { findings: SrbEnvelopeDeterministic[] }) {
  if (findings.length === 0) {
    return (
      <p className="text-xs text-white/30 italic py-4">
        No deterministic findings for this stack.
      </p>
    );
  }
  return (
    <div className="space-y-3">
      {findings.map(f => (
        <div
          key={f.findingId}
          className="rounded-xl border border-white/8 bg-white/[0.02] px-4 py-3"
        >
          <div className="flex items-start gap-3">
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2 mb-1">
                <span className="text-[9px] font-mono font-bold text-white/30 uppercase">{f.code}</span>
                <span className="text-[9px] text-white/20">{f.category}</span>
                <span className="text-[9px] text-white/20">· {f.evidenceTier}</span>
              </div>
              <p className="text-xs text-white/70 leading-relaxed">{f.narrative}</p>
            </div>
            <ClaimBadgeStack badges={['commentary-only', 'not-executable']} size="xs" />
          </div>
        </div>
      ))}
    </div>
  );
}

function PerspectiveFindingRow({ finding }: { finding: SrbEnvelopeFinding }) {
  return (
    <div className="rounded-xl border border-white/8 bg-white/[0.02] px-4 py-3 space-y-2">
      <div className="flex items-center gap-2">
        <span className={cn('text-[10px] font-semibold uppercase tracking-wide', severityColor(finding.severity))}>
          {finding.severity}
        </span>
        <span className="text-[10px] text-white/20">· {finding.category}</span>
        <ClaimBadgeStack badges={['commentary-only', 'not-executable']} size="xs" className="ml-auto" />
      </div>
      <GovernedSentence text={finding.narrative} context="srb-finding" />
    </div>
  );
}

function RoleTab({ role, findings, summary }: {
  role: string;
  findings: SrbEnvelopeFinding[];
  summary: string;
}) {
  return (
    <div className="space-y-4">
      {summary && (
        <div className="rounded-xl border border-white/8 bg-emerald-500/5 px-4 py-3">
          <p className="text-[9px] font-bold uppercase tracking-widest text-white/20 mb-1">
            {role} Summary
          </p>
          <GovernedSentence text={summary} context="srb-finding" />
        </div>
      )}
      {findings.length === 0 ? (
        <p className="text-xs text-white/30 italic py-4">
          No findings from the {role} perspective.
        </p>
      ) : (
        <div className="space-y-3">
          {findings.map(f => (
            <PerspectiveFindingRow key={f.findingId} finding={f} />
          ))}
        </div>
      )}
    </div>
  );
}

// ── Tab types ─────────────────────────────────────────────────────────────────

type Tab = 'Optimizer' | 'Skeptic' | 'Regulator' | 'Historian' | 'Challenge' | 'Confidence' | 'Reasoning' | 'Narrative';

const ROLE_TABS: Tab[] = ['Optimizer', 'Skeptic', 'Regulator', 'Historian'];
const ALL_TABS: Tab[] = [...ROLE_TABS, 'Challenge', 'Confidence', 'Reasoning', 'Narrative'];

// ── Main page ─────────────────────────────────────────────────────────────────

export default function DecisionTheaterPage() {
  const params = useParams();
  const protocolId = params?.id as string | undefined;

  const [protocol, setProtocol] = useState<Protocol | null>(null);
  const [envelope, setEnvelope] = useState<SrbEnvelopeResponse | null>(null);
  const [loadingProtocol, setLoadingProtocol] = useState(true);
  const [loadingEnvelope, setLoadingEnvelope] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [activeTab, setActiveTab] = useState<Tab>('Optimizer');
  const [completing, setCompleting] = useState(false);
  const [completedEvent, setCompletedEvent] = useState<ProtocolReviewCompletedEvent | null>(null);
  const [receiptDrawerOpen, setReceiptDrawerOpen] = useState(false);

  // Load protocol
  useEffect(() => {
    if (!protocolId) return;
    let cancelled = false;
    setLoadingProtocol(true);

    apiClient.getProtocol(protocolId)
      .then(p => { if (!cancelled) setProtocol(p); })
      .catch(err => { if (!cancelled) setError(err instanceof Error ? err.message : 'Failed to load protocol'); })
      .finally(() => { if (!cancelled) setLoadingProtocol(false); });

    return () => { cancelled = true; };
  }, [protocolId]);

  // Generate deliberation envelope once protocol is loaded
  const generateEnvelope = useCallback(async (p: Protocol) => {
    setLoadingEnvelope(true);
    setError(null);
    try {
      const compounds = p.items
        .filter(item => item.compound)
        .map(item => ({
          slug: item.compound!.name.toLowerCase().replace(/\s+/g, '-'),
          displayName: item.compound!.name,
          form: 'supplement',
          category: item.compound!.category,
          evidenceTier: 'None',
        }));

      const deterministicFindings = p.interactionIntelligence.topFindings.map((f, i) => ({
        code: `F${String(i + 1).padStart(3, '0')}`,
        category: f.type,
        narrative: f.message,
        compoundSlugs: f.compounds.map((c: string) => c.toLowerCase().replace(/\s+/g, '-')),
        riskScoreContribution: f.confidence,
      }));

      const result = await apiClient.postStackReviewEnvelope({
        protocolId: null,
        payload: {
          goal: p.name,
          compounds,
          pathways: [],
          deterministicFindings,
          knownPatternNames: [],
          providerReviewPressure: 0,
        },
      });
      setEnvelope(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate deliberation envelope');
    } finally {
      setLoadingEnvelope(false);
    }
  }, []);

  useEffect(() => {
    if (protocol) generateEnvelope(protocol);
  }, [protocol, generateEnvelope]);

  // Complete review
  async function handleCompleteReview() {
    if (!protocolId || !protocol) return;
    setCompleting(true);
    try {
      const event = await apiClient.completeProtocolReview(protocolId, protocol.activeRun?.id ?? null, 'Reviewed via Decision Theater');
      setCompletedEvent(event);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to complete review');
    } finally {
      setCompleting(false);
    }
  }

  // Export review memo
  function handleExportMemo() {
    if (!envelope || !protocol) return;
    const memo = {
      protocolId,
      protocolName: protocol.name,
      generatedAtUtc: new Date().toISOString(),
      effectStatus: 'commentary-only',
      receiptUri: completedEvent?.receiptUri ?? null,
      deterministicFindings: envelope.deterministicFindings,
      perspectiveReviews: envelope.perspectiveReviews,
      contradictionReview: envelope.contradictionReview,
      confidenceProfile: envelope.confidenceProfile,
    };
    const blob = new Blob([JSON.stringify(memo, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `srb-memo-${protocolId}-${new Date().toISOString().split('T')[0]}.json`;
    a.click();
    URL.revokeObjectURL(url);
  }

  if (!isEnabled('decisionTheater')) return null;

  if (loadingProtocol) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-[#070809]">
        <div className="text-white/30 text-sm">Loading protocol…</div>
      </div>
    );
  }

  if (error && !envelope) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-[#070809]">
        <div className="text-red-400 text-sm max-w-sm text-center">{error}</div>
      </div>
    );
  }

  const heat = envelope
    ? deriveHeat(envelope.confidenceProfile.contradictionDensity)
    : { value: 0, band: 'nominal' as const, throttlingActive: false };

  const confidenceProfile = envelope
    ? {
        model: parseProfileValue(envelope.confidenceProfile.model),
        epistemic: parseProfileValue(envelope.confidenceProfile.epistemic),
        contradictionDensity: parseProfileValue(envelope.confidenceProfile.contradictionDensity),
        evidenceSupport: parseProfileValue(envelope.confidenceProfile.evidenceSupport),
        calibrationVersion: envelope.confidenceProfile.calibrationVersion,
      }
    : null;

  return (
    <div className="min-h-screen bg-[#070809] text-white">
      {/* Page header */}
      <div className="sticky top-0 z-30 border-b border-white/8 bg-[#070809]/95 backdrop-blur-sm">
        <div className="max-w-5xl mx-auto px-6 py-4 flex items-center gap-4">
          <Link
            href={`/protocols/${protocolId}`}
            className="text-[10px] font-mono text-white/30 hover:text-white/60 transition-colors"
          >
            ← protocol
          </Link>
          <div className="flex-1 min-w-0">
            <p className="text-[9px] font-bold uppercase tracking-widest text-white/20">Decision Theater</p>
            <h1 className="text-sm font-semibold text-white/90 truncate">
              {protocol?.name ?? 'Loading…'}
            </h1>
          </div>

          {/* Cognitive Heat — always visible */}
          {envelope && (
            <CognitiveHeatGauge heat={heat} compact />
          )}

          {/* Complete Review button */}
          {completedEvent ? (
            <div className="flex items-center gap-2">
              <ReceiptBadge
                receiptUri={completedEvent.receiptUri ?? 'keon://receipt/pending'}
                effectStatus="commentary-only"
                onClick={() => setReceiptDrawerOpen(true)}
              />
              <span className="text-[10px] text-white/30">Review complete</span>
            </div>
          ) : (
            <button
              onClick={handleCompleteReview}
              disabled={completing || !envelope}
              className={cn(
                'px-4 py-2 rounded-lg text-[11px] font-semibold transition-all',
                'border border-emerald-400/30 bg-emerald-500/10 text-emerald-300',
                'hover:bg-emerald-500/20 hover:border-emerald-400/50',
                'disabled:opacity-40 disabled:cursor-not-allowed',
              )}
            >
              {completing ? 'Completing…' : 'Complete Review'}
            </button>
          )}

          <button
            onClick={handleExportMemo}
            disabled={!envelope}
            className="px-3 py-2 rounded-lg text-[11px] font-semibold border border-white/10 text-white/40 hover:text-white/70 hover:border-white/20 transition-all disabled:opacity-40 disabled:cursor-not-allowed"
          >
            Export Memo
          </button>
        </div>
      </div>

      {/* Body */}
      <div className="max-w-5xl mx-auto px-6 py-8">
        {loadingEnvelope && (
          <div className="flex items-center justify-center py-24 text-white/30 text-sm">
            Generating deliberation envelope…
          </div>
        )}

        {!loadingEnvelope && envelope && (
          <SafetyHierarchy
            deterministic={
              <section>
                <h2 className="text-[9px] font-bold uppercase tracking-widest text-white/25 mb-4">
                  Deterministic Findings — BioStack Native · Always Rendered First
                </h2>
                <DeterministicSection findings={envelope.deterministicFindings} />
              </section>
            }
            commentary={
              <div className="space-y-6">
                {/* Tab strip */}
                <div className="flex flex-wrap gap-1.5">
                  {ALL_TABS.map(tab => (
                    <button
                      key={tab}
                      onClick={() => setActiveTab(tab)}
                      className={cn(
                        'px-3 py-1.5 rounded-lg text-[11px] font-semibold border transition-all',
                        activeTab === tab
                          ? 'bg-white/10 border-white/20 text-white/90'
                          : 'bg-transparent border-white/8 text-white/35 hover:bg-white/5 hover:text-white/60',
                      )}
                    >
                      {tab}
                    </button>
                  ))}
                </div>

                {/* Tab content */}
                <div>
                  {/* Role tabs */}
                  {ROLE_TABS.includes(activeTab) && (() => {
                    const review = envelope.perspectiveReviews[activeTab];
                    return review ? (
                      <RoleTab
                        role={activeTab}
                        findings={review.findings}
                        summary={review.summary}
                      />
                    ) : (
                      <p className="text-xs text-white/30 italic py-4">
                        No data for {activeTab} perspective.
                      </p>
                    );
                  })()}

                  {/* Challenge tab */}
                  {activeTab === 'Challenge' && (
                    <div className="space-y-3">
                      <div className="rounded-xl border border-orange-400/20 bg-orange-500/5 px-4 py-4">
                        <div className="flex items-center gap-2 mb-3">
                          <span className="text-[9px] font-bold uppercase tracking-widest text-orange-400/60">
                            Active Contradiction — Non-Executable
                          </span>
                          <ClaimBadgeStack badges={['not-executable', 'commentary-only']} size="xs" className="ml-auto" />
                        </div>
                        <GovernedSentence
                          text={envelope.contradictionReview.counterPlanNarrative}
                          context="srb-finding"
                        />
                      </div>
                    </div>
                  )}

                  {/* Confidence tab */}
                  {activeTab === 'Confidence' && confidenceProfile && (
                    <ConfidenceProfileCard profile={confidenceProfile} />
                  )}

                  {/* Reasoning tab */}
                  {activeTab === 'Reasoning' && (
                    <ReasoningGraphViewer
                      graph={envelope.reasoningGraphFull}
                      className="mt-2"
                    />
                  )}

                  {/* Narrative tab */}
                  {activeTab === 'Narrative' && (
                    <WitnessNarrativePanel
                      narrative={envelope.witnessNarrative}
                      className="mt-2"
                    />
                  )}
                </div>
              </div>
            }
          />
        )}
      </div>

      {/* Receipt drawer */}
      {completedEvent?.receiptUri && (
        <ReceiptDrawer
          receiptUri={completedEvent.receiptUri}
          open={receiptDrawerOpen}
          onClose={() => setReceiptDrawerOpen(false)}
        />
      )}
    </div>
  );
}
