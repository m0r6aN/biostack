'use client';

import { Header } from '@/components/Header';
import { LoadingState } from '@/components/LoadingState';
import { LockedInsightCard } from '@/components/monetization/LockedInsightCard';
import { UpgradeCard } from '@/components/monetization/UpgradeCard';
import { apiClient } from '@/lib/api';
import { useEntitlements } from '@/lib/entitlements';
import { InteractionFlag, KnowledgeEntry } from '@/lib/types';
import { useMemo, useState } from 'react';

export default function MapMyStackPage() {
  const { entitlements } = useEntitlements();
  const [input, setInput] = useState('BPC-157, TB-500, NAD+');
  const [compounds, setCompounds] = useState<string[]>([]);
  const [entries, setEntries] = useState<KnowledgeEntry[]>([]);
  const [flags, setFlags] = useState<InteractionFlag[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const visibleRelationships = useMemo(() => {
    if (flags.length > 0) {
      return flags.slice(0, entitlements.isPro ? flags.length : 2).map((flag) => ({
        label: flag.compoundNames.join(' + '),
        detail: `${flag.pathwayTag} pathway overlap`,
      }));
    }

    const paired = entries.flatMap((entry) =>
      entry.pairsWellWith
        .filter((pair) => compounds.some((name) => pair.toLowerCase().includes(name.toLowerCase())))
        .map((pair) => ({
          label: `${entry.canonicalName} + ${pair}`,
          detail: 'Documented pairing in the compound library',
        }))
    );

    return paired.slice(0, entitlements.isPro ? paired.length : 2);
  }, [compounds, entries, entitlements.isPro, flags]);

  async function mapStack() {
    const names = parseCompounds(input);
    if (names.length < 2) {
      setError('Enter at least two compounds to map stack relationships.');
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const [library, overlapResults] = await Promise.all([
        apiClient.getAllKnowledgeCompounds(),
        apiClient.checkOverlap(names),
      ]);

      const normalized = names.map((name) => name.toLowerCase());
      setCompounds(names);
      setFlags(overlapResults);
      setEntries(
        library.filter((entry) =>
          normalized.some((name) =>
            entry.canonicalName.toLowerCase() === name ||
            entry.aliases.some((alias) => alias.toLowerCase() === name)
          )
        )
      );
    } catch {
      setError('Could not map this stack right now.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="w-full">
      <Header title="Map My Stack" subtitle="Paste compounds, preview relationships, unlock full stack intelligence" />

      <div className="max-w-6xl space-y-8 p-8">
        <section className="grid gap-6 lg:grid-cols-[1fr_380px]">
          <div className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-6">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-200/65">Premium stack funnel</p>
            <h1 className="mt-3 text-3xl font-bold tracking-tight text-white">Turn a list into an intelligence map.</h1>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-white/55">
              Paste your current stack. BioStack will compute real library relationships first, then Pro opens the pathway map, compatibility analysis, and optimization signals.
            </p>

            <textarea
              value={input}
              onChange={(event) => setInput(event.target.value)}
              rows={6}
              className="mt-6 w-full rounded-lg border border-white/[0.08] bg-black/20 px-4 py-3 text-sm leading-6 text-white outline-none transition-colors placeholder:text-white/25 focus:border-emerald-400/45"
              placeholder="BPC-157, TB-500, NAD+"
            />

            {error && <p className="mt-3 rounded-lg border border-red-400/20 bg-red-500/10 px-3 py-2 text-sm text-red-100/80">{error}</p>}

            <button
              onClick={mapStack}
              disabled={loading}
              className="mt-5 rounded-lg bg-emerald-400 px-5 py-3 text-sm font-bold text-slate-950 transition-colors hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {loading ? 'Mapping...' : 'Map my stack'}
            </button>
          </div>

          <UpgradeCard
            title="$19/mo for full stack intelligence"
            description="Free shows the first real relationships. Pro explains what they mean and how to optimize the stack."
            cta="Unlock optimization signals"
            returnPath="/map-my-stack"
            bullets={['Full pathway map', 'Compatibility and caution signals', 'Missing support compounds']}
          />
        </section>

        {loading && <LoadingState />}

        {compounds.length >= 2 && !loading && (
          <section className="grid gap-6 lg:grid-cols-[1fr_420px]">
            <div className="rounded-lg border border-white/[0.08] bg-[#101820]/95 p-6">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Stack visualization</p>
                  <h2 className="mt-2 text-xl font-bold text-white">{compounds.length} compounds mapped</h2>
                </div>
                <span className="rounded-lg border border-white/[0.08] px-3 py-1.5 text-xs font-semibold uppercase tracking-[0.12em] text-white/45">
                  {entitlements.isPro ? 'full map' : 'free preview'}
                </span>
              </div>

              <div className="mt-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {compounds.map((compound) => (
                  <div key={compound} className="rounded-lg border border-emerald-300/15 bg-emerald-400/[0.06] p-4">
                    <p className="font-bold text-white">{compound}</p>
                    <p className="mt-1 text-xs text-white/40">
                      {entries.find((entry) => entry.canonicalName.toLowerCase() === compound.toLowerCase())?.classification ?? 'Manual input'}
                    </p>
                  </div>
                ))}
              </div>

              <div className="mt-6 space-y-3">
                {visibleRelationships.length > 0 ? (
                  visibleRelationships.map((relationship) => (
                    <div key={`${relationship.label}-${relationship.detail}`} className="rounded-lg border border-amber-300/20 bg-amber-400/[0.08] p-4">
                      <p className="font-semibold text-amber-100">{relationship.label}</p>
                      <p className="mt-1 text-sm text-amber-100/65">{relationship.detail}</p>
                    </div>
                  ))
                ) : (
                  <p className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4 text-sm text-white/50">
                    No direct library relationships were detected for these exact names.
                  </p>
                )}
              </div>
            </div>

            {!entitlements.isPro ? (
              <LockedInsightCard
                title="Locked stack map"
                cta="Unlock full stack intelligence"
                returnPath="/map-my-stack"
                lockedItems={[
                  'Full pathway map',
                  'Compatibility analysis',
                  'Missing support compounds',
                  'Optimization suggestions',
                  'Avoid / caution signals',
                  'Retail bundle insertion points',
                ]}
              />
            ) : (
              <div className="rounded-lg border border-emerald-300/20 bg-emerald-400/[0.06] p-6">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-200/70">Pro analysis</p>
                <h2 className="mt-3 text-xl font-bold text-white">Full intelligence unlocked</h2>
                <div className="mt-5 space-y-3">
                  {flags.map((flag) => (
                    <div key={`${flag.compoundNames.join('-')}-${flag.pathwayTag}`} className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
                      <p className="font-semibold text-white">{flag.compoundNames.join(' + ')}</p>
                      <p className="mt-1 text-sm text-white/60">{flag.description}</p>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </section>
        )}
      </div>
    </div>
  );
}

function parseCompounds(value: string) {
  return Array.from(
    new Set(
      value
        .split(/[\n,;]+/)
        .map((item) => item.trim())
        .filter(Boolean)
    )
  );
}
