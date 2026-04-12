'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { Header } from '@/components/Header';
import { LoadingSkeleton } from '@/components/LoadingState';
import { ProfileSwitcher } from '@/components/ProfileSwitcher';
import { SimulationTimeline } from '@/components/protocols/SimulationTimeline';
import { StackScoreCard } from '@/components/protocols/StackScoreCard';
import { apiClient } from '@/lib/api';
import { useProfile } from '@/lib/context';
import { CurrentStackIntelligence, Protocol } from '@/lib/types';

export default function ProtocolsPage() {
  const { currentProfileId } = useProfile();
  const [protocols, setProtocols] = useState<Protocol[]>([]);
  const [currentStack, setCurrentStack] = useState<CurrentStackIntelligence | null>(null);
  const [name, setName] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (currentProfileId) {
      loadProtocols();
    }
  }, [currentProfileId]);

  async function loadProtocols() {
    if (!currentProfileId) {
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const [protocolData, stackData] = await Promise.all([
        apiClient.getProtocols(currentProfileId),
        apiClient.getCurrentStackIntelligence(currentProfileId),
      ]);
      setProtocols(protocolData);
      setCurrentStack(stackData);
    } catch (err) {
      setError('Failed to load protocols');
    } finally {
      setLoading(false);
    }
  }

  async function saveCurrentStack() {
    if (!currentProfileId || !name.trim()) {
      return;
    }

    try {
      setSaving(true);
      setError(null);
      const saved = await apiClient.saveCurrentStackAsProtocol(currentProfileId, name);
      setProtocols([saved, ...protocols]);
      setName('');
    } catch (err) {
      setError('Save failed. Confirm the current stack has active compounds.');
    } finally {
      setSaving(false);
    }
  }

  if (!currentProfileId) {
    return (
      <div className="w-full">
        <Header title="Protocols" actions={<ProfileSwitcher />} />
        <div className="p-8">
          <EmptyState title="No Profile Selected" description="Select a profile to save and simulate protocols." icon="🧬" />
        </div>
      </div>
    );
  }

  if (error && !loading) {
    return (
      <div className="w-full">
        <Header title="Protocols" actions={<ProfileSwitcher />} />
        <div className="p-8">
          <ErrorState message={error} onRetry={loadProtocols} />
        </div>
      </div>
    );
  }

  return (
    <div className="w-full">
      <Header title="Protocols" subtitle="Simulate, save, track, compare, evolve" actions={<ProfileSwitcher />} />

      <div className="max-w-6xl space-y-6 p-8">
        {loading ? (
          <LoadingSkeleton />
        ) : (
          <>
            <section className="grid gap-6 lg:grid-cols-[360px_1fr]">
              <div className="space-y-4">
                <div className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
                  <h2 className="text-xl font-bold text-white">Save Current Stack as Protocol</h2>
                  <p className="mt-2 text-sm text-white/45">Name the active stack and keep it as a protocol snapshot.</p>
                  <div className="mt-5 flex gap-2">
                    <input
                      value={name}
                      onChange={(event) => setName(event.target.value)}
                      placeholder="Cut phase v1"
                      className="min-w-0 flex-1 rounded-lg border border-white/[0.08] bg-black/20 px-3 py-2 text-sm text-white outline-none focus:border-emerald-400/50"
                    />
                    <button
                      onClick={saveCurrentStack}
                      disabled={saving || !name.trim()}
                      className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                      {saving ? 'Saving' : 'Save'}
                    </button>
                  </div>
                </div>

                {currentStack && <StackScoreCard score={currentStack.stackScore} />}
              </div>

              {currentStack && <SimulationTimeline simulation={currentStack.simulation} />}
            </section>

            <section>
              <h2 className="mb-4 text-lg font-semibold text-white">Saved Protocols</h2>
              {protocols.length === 0 ? (
                <div className="rounded-lg border border-emerald-400/15 bg-[#121923]/90 p-6">
                  <p className="text-xs font-semibold uppercase tracking-[0.16em] text-emerald-200/55">Start your first protocol</p>
                  <h3 className="mt-2 text-xl font-bold text-white">Save the current stack when it is ready to observe.</h3>
                  <p className="mt-2 max-w-2xl text-sm leading-6 text-white/50">
                    Name the active stack above, review the simulation, then save it as the first lineage snapshot.
                  </p>
                </div>
              ) : (
                <div className="grid gap-4 md:grid-cols-2">
                  {protocols.map((protocol) => (
                    <Link
                      key={protocol.id}
                      href={`/protocols/${protocol.id}`}
                      className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5 transition-colors hover:border-emerald-400/30"
                    >
                      <div className="flex items-start justify-between gap-4">
                        <div>
                          <h3 className="font-bold text-white">{protocol.name}</h3>
                          <p className="mt-1 text-sm text-white/45">
                            {protocol.items.length} compound{protocol.items.length === 1 ? '' : 's'} · v{protocol.version} · {protocol.isCurrentVersion ? 'current version' : 'prior version'}
                          </p>
                        </div>
                        <div className="flex flex-col items-end gap-2">
                          <span className="rounded-lg border border-emerald-400/20 bg-emerald-500/10 px-2.5 py-1 text-sm font-semibold text-emerald-200">
                            {protocol.stackScore.score}
                          </span>
                          {protocol.isDraft && (
                            <span className="rounded-lg border border-sky-400/20 bg-sky-500/10 px-2 py-1 text-xs font-semibold text-sky-200">
                              draft
                            </span>
                          )}
                        </div>
                      </div>
                      {protocol.evolvedFromRunId && (
                        <p className="mt-3 text-xs text-white/40">Derived from an observed run. Compare changes before tracking.</p>
                      )}
                      <div className="mt-4 flex flex-wrap gap-2">
                        {protocol.stackScore.chips.slice(0, 3).map((chip) => (
                          <span key={chip} className="rounded-lg border border-white/[0.08] px-2 py-1 text-xs text-white/50">{chip}</span>
                        ))}
                      </div>
                    </Link>
                  ))}
                </div>
              )}
            </section>
          </>
        )}
      </div>
    </div>
  );
}
