'use client';

import Link from 'next/link';
import { use, useEffect, useState } from 'react';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { Header } from '@/components/Header';
import { LoadingSkeleton } from '@/components/LoadingState';
import { ProtocolComparison } from '@/components/protocols/ProtocolComparison';
import { SimulationTimeline } from '@/components/protocols/SimulationTimeline';
import { StackScoreCard } from '@/components/protocols/StackScoreCard';
import { apiClient } from '@/lib/api';
import { Protocol } from '@/lib/types';

interface ProtocolDetailPageProps {
  params: Promise<{ id: string }>;
}

export default function ProtocolDetailPage({ params }: ProtocolDetailPageProps) {
  const { id } = use(params);
  const [protocol, setProtocol] = useState<Protocol | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadProtocol();
  }, [id]);

  async function loadProtocol() {
    try {
      setLoading(true);
      setError(null);
      setProtocol(await apiClient.getProtocol(id));
    } catch (err) {
      setError('Failed to load protocol');
    } finally {
      setLoading(false);
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
        subtitle={protocol ? `Version ${protocol.version}` : undefined}
        actions={<Link href="/checkins" className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-400">Track this protocol</Link>}
      />

      <div className="max-w-6xl space-y-6 p-8">
        {loading ? (
          <LoadingSkeleton />
        ) : protocol ? (
          <>
            <section className="grid gap-6 lg:grid-cols-[360px_1fr]">
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

            <SimulationTimeline simulation={protocol.simulation} />
            <ProtocolComparison comparison={protocol.actualComparison} />
          </>
        ) : (
          <EmptyState title="Protocol Not Found" description="This protocol could not be loaded." icon="🧬" />
        )}
      </div>
    </div>
  );
}
