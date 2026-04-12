'use client';

import { use, useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { Header } from '@/components/Header';
import { LoadingSkeleton } from '@/components/LoadingState';
import { ProtocolComparison } from '@/components/protocols/ProtocolComparison';
import { ProtocolContinuityStrip } from '@/components/protocols/ProtocolContinuityStrip';
import { ProtocolIntelligenceReview } from '@/components/protocols/ProtocolIntelligenceReview';
import { SimulationTimeline } from '@/components/protocols/SimulationTimeline';
import { StackScoreCard } from '@/components/protocols/StackScoreCard';
import { apiClient } from '@/lib/api';
import { Protocol, ProtocolReview } from '@/lib/types';

interface ProtocolDetailPageProps {
  params: Promise<{ id: string }>;
}

export default function ProtocolDetailPage({ params }: ProtocolDetailPageProps) {
  const { id } = use(params);
  const router = useRouter();
  const [protocol, setProtocol] = useState<Protocol | null>(null);
  const [review, setReview] = useState<ProtocolReview | null>(null);
  const [loading, setLoading] = useState(true);
  const [starting, setStarting] = useState(false);
  const [ending, setEnding] = useState(false);
  const [evolving, setEvolving] = useState(false);
  const [completingReview, setCompletingReview] = useState(false);
  const [toast, setToast] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadProtocol();
  }, [id]);

  async function loadProtocol() {
    try {
      setLoading(true);
      setError(null);
      const [protocolData, reviewData] = await Promise.all([
        apiClient.getProtocol(id),
        apiClient.getProtocolReview(id),
      ]);
      setProtocol(protocolData);
      setReview(reviewData);
    } catch (err) {
      setError('Failed to load protocol');
    } finally {
      setLoading(false);
    }
  }

  async function startRun() {
    try {
      setStarting(true);
      setError(null);
      await apiClient.startProtocolRun(id);
      const [protocolData, reviewData] = await Promise.all([
        apiClient.getProtocol(id),
        apiClient.getProtocolReview(id),
      ]);
      setProtocol(protocolData);
      setReview(reviewData);
      setToast('Protocol run started');
      window.setTimeout(() => setToast(null), 2600);
    } catch (err) {
      setError('Failed to start protocol run');
    } finally {
      setStarting(false);
    }
  }

  async function completeRun() {
    if (!protocol?.activeRun) {
      return;
    }

    try {
      setEnding(true);
      setError(null);
      await apiClient.completeProtocolRun(protocol.activeRun.id);
      const [protocolData, reviewData] = await Promise.all([
        apiClient.getProtocol(id),
        apiClient.getProtocolReview(id),
      ]);
      setProtocol(protocolData);
      setReview(reviewData);
      setToast('Run marked completed');
      window.setTimeout(() => setToast(null), 2600);
    } catch (err) {
      setError('Failed to complete protocol run');
    } finally {
      setEnding(false);
    }
  }

  async function abandonRun() {
    if (!protocol?.activeRun) {
      return;
    }

    try {
      setEnding(true);
      setError(null);
      await apiClient.abandonProtocolRun(protocol.activeRun.id);
      const [protocolData, reviewData] = await Promise.all([
        apiClient.getProtocol(id),
        apiClient.getProtocolReview(id),
      ]);
      setProtocol(protocolData);
      setReview(reviewData);
      setToast('Run marked abandoned');
      window.setTimeout(() => setToast(null), 2600);
    } catch (err) {
      setError('Failed to abandon protocol run');
    } finally {
      setEnding(false);
    }
  }

  async function evolveFromRun() {
    const run = protocol?.actualComparison?.run;
    if (!run || run.status === 'active') {
      return;
    }

    try {
      setEvolving(true);
      setError(null);
      const draft = await apiClient.evolveProtocolFromRun(run.id);
      router.push(`/protocols/${draft.id}`);
    } catch (err) {
      setError('Failed to create protocol draft from run observations');
    } finally {
      setEvolving(false);
    }
  }

  async function completeReview() {
    if (!protocol) {
      return;
    }

    try {
      setCompletingReview(true);
      setError(null);
      await apiClient.completeProtocolReview(
        protocol.id,
        protocol.actualComparison?.run?.id ?? protocol.activeRun?.id ?? null,
        'Protocol review completed from detail view.'
      );
      const reviewData = await apiClient.getProtocolReview(id);
      setReview(reviewData);
      setToast('Review completed');
      window.setTimeout(() => setToast(null), 2600);
    } catch (err) {
      setError('Failed to complete review');
    } finally {
      setCompletingReview(false);
    }
  }

  if (error) {
    return (
      <div className="w-full">
        <Header title="Protocol" />
        <div className="p-8">
          <ErrorState message={error} onRetry={loadProtocol} />
        </div>
      </div>
    );
  }

  return (
    <div className="w-full">
      <Header
        title={protocol?.name ?? 'Protocol'}
        subtitle={protocol ? `Version ${protocol.version}${protocol.isDraft ? ' draft' : ''}${protocol.isCurrentVersion ? ' · current' : ' · prior version'}` : undefined}
        actions={
          <div className="flex flex-wrap gap-2">
            {protocol?.actualComparison?.run && protocol.actualComparison.run.status !== 'active' && (
              <button
                onClick={evolveFromRun}
                disabled={evolving}
                className="rounded-lg border border-emerald-400/25 bg-emerald-500/10 px-4 py-2 text-sm font-semibold text-emerald-100 hover:border-emerald-300/45 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {evolving ? 'Creating draft' : 'Evolve from run'}
              </button>
            )}
            {protocol?.activeRun ? (
              <>
                <button
                  onClick={completeRun}
                  disabled={ending}
                  className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  Complete run
                </button>
                <button
                  onClick={abandonRun}
                  disabled={ending}
                  className="rounded-lg border border-white/[0.12] px-4 py-2 text-sm font-semibold text-white/70 hover:border-white/25 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  Abandon
                </button>
              </>
            ) : (
              <button
                onClick={startRun}
                disabled={starting || !protocol}
                className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {starting ? 'Starting' : 'Track this protocol'}
              </button>
            )}
          </div>
        }
      />

      <div className="max-w-6xl space-y-6 p-8">
        {toast && (
          <div className="rounded-lg border border-emerald-400/20 bg-emerald-500/10 px-4 py-3 text-sm font-semibold text-emerald-100">
            {toast}
          </div>
        )}
        {loading ? (
          <LoadingSkeleton />
        ) : protocol ? (
          <>
            <ProtocolContinuityStrip protocol={protocol} review={review} />

            <section className="grid gap-4 lg:grid-cols-[1fr_360px]">
              <div className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Protocol Lineage</p>
                <div className="mt-3 flex flex-wrap gap-2">
                  <span className="rounded-lg border border-emerald-400/20 bg-emerald-500/10 px-3 py-1.5 text-sm font-semibold text-emerald-100">
                    v{protocol.version} {protocol.isDraft ? 'draft' : 'snapshot'}
                  </span>
                  {protocol.priorVersions.map((version) => (
                    <Link
                      key={version.id}
                      href={`/protocols/${version.id}`}
                      className="rounded-lg border border-white/[0.08] px-3 py-1.5 text-sm text-white/60 hover:border-white/20"
                    >
                      v{version.version} {version.isDraft ? 'draft' : 'prior'}
                    </Link>
                  ))}
                </div>
                {protocol.evolutionContext && (
                  <p className="mt-4 whitespace-pre-line text-sm text-white/55">{protocol.evolutionContext}</p>
                )}
              </div>

              {protocol.versionDiff && (
                <div className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">What Changed</p>
                  <div className="mt-3 space-y-2">
                    {protocol.versionDiff.changes.slice(0, 5).map((change) => (
                      <div key={`${change.scope}-${change.subject}-${change.before}-${change.after}`} className="rounded-lg border border-white/[0.06] bg-white/[0.025] p-3">
                        <div className="flex items-center justify-between gap-2">
                          <p className="text-sm font-semibold text-white">{change.subject}</p>
                          <span className="rounded-lg border border-white/[0.08] px-2 py-1 text-xs text-white/45">{change.changeType}</span>
                        </div>
                        <p className="mt-1 text-xs text-white/45">
                          {change.before || 'none'} → {change.after || 'none'}
                        </p>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </section>

            <section id="run" className="grid scroll-mt-6 gap-6 lg:grid-cols-[360px_1fr]">
              <StackScoreCard score={protocol.stackScore} />
              <div className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Protocol Compounds</p>
                <div className="mt-4 space-y-3">
                  {protocol.items.map((item) => (
                    <div key={item.id} className="rounded-lg border border-white/[0.06] bg-white/[0.025] p-4">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <h3 className="font-semibold text-white">{item.compound?.name ?? 'Compound snapshot'}</h3>
                          <p className="mt-1 text-sm text-white/45">{item.compound?.category ?? item.compoundRecordId}</p>
                        </div>
                        {item.compound?.status && (
                          <span className="rounded-lg border border-white/[0.08] px-2 py-1 text-xs text-white/55">{item.compound.status}</span>
                        )}
                      </div>
                      {(item.compound?.startDate || item.compound?.notes) && (
                        <p className="mt-3 text-sm text-white/55">
                          {item.compound.startDate ? `Started ${item.compound.startDate}` : ''}
                          {item.compound.startDate && item.compound.notes ? ' · ' : ''}
                          {item.compound.notes}
                        </p>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            </section>

            <section id="simulation" className="scroll-mt-6">
              <SimulationTimeline simulation={protocol.simulation} />
            </section>
            <section id="comparison" className="scroll-mt-6">
              <ProtocolComparison comparison={protocol.actualComparison} />
            </section>
            <section id="review" className="scroll-mt-6">
              <div className="mb-3 flex justify-end">
                <button
                  onClick={completeReview}
                  disabled={completingReview || !review}
                  className="rounded-lg border border-lime-400/25 bg-lime-500/10 px-4 py-2 text-sm font-semibold text-lime-100 hover:border-lime-300/45 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {completingReview ? 'Completing review' : 'Complete review'}
                </button>
              </div>
              <ProtocolIntelligenceReview review={review} />
            </section>
          </>
        ) : (
          <EmptyState title="Protocol Not Found" description="This protocol could not be loaded." icon="🧬" />
        )}
      </div>
    </div>
  );
}
