'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useEffect, useRef, useState } from 'react';
import { ActiveProfileChip } from '@/components/ActiveProfileChip';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { Header } from '@/components/Header';
import { LoadingSkeleton } from '@/components/LoadingState';
import { ProfileSwitcher } from '@/components/ProfileSwitcher';
import { InteractionIntelligenceCard } from '@/components/protocols/InteractionIntelligenceCard';
import { SimulationTimeline } from '@/components/protocols/SimulationTimeline';
import { StackScoreCard } from '@/components/protocols/StackScoreCard';
import { ApiError, apiClient } from '@/lib/api';
import { useProfile } from '@/lib/context';
import { CurrentStackIntelligence, Protocol } from '@/lib/types';

function LockedTierCard({ eyebrow, title, detail }: { eyebrow: string; title: string; detail: string }) {
  return (
    <div className="rounded-lg border border-amber-300/15 bg-amber-400/[0.06] p-5">
      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-amber-100/75">{eyebrow}</p>
      <h3 className="mt-2 text-xl font-semibold text-white">{title}</h3>
      <p className="mt-3 max-w-2xl text-sm leading-6 text-white/65">{detail}</p>
      <div className="mt-5 flex flex-wrap gap-3">
        <Link
          href="/billing"
          className="rounded-lg bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-300"
        >
          Upgrade plan
        </Link>
        <Link
          href="/pricing"
          className="rounded-lg border border-white/[0.1] px-4 py-2 text-sm font-semibold text-white/75 hover:border-white/20"
        >
          Compare tiers
        </Link>
      </div>
    </div>
  );
}

function compoundSummary(protocol: Protocol): string {
  const names = protocol.items
    .map((item) => item.compound?.name)
    .filter((name): name is string => Boolean(name));

  if (names.length === 0) {
    return protocol.items.length === 0
      ? 'No items yet'
      : `${protocol.items.length} item${protocol.items.length === 1 ? '' : 's'} without compound records`;
  }

  const shown = names.slice(0, 3).join(', ');
  const more = names.length - 3;
  return `${protocol.items.length} item${protocol.items.length === 1 ? '' : 's'}: ${shown}${more > 0 ? ` +${more} more` : ''}`;
}

function ProtocolCard({ protocol }: { protocol: Protocol }) {
  return (
    <article className="flex flex-col rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5 transition-colors hover:border-emerald-400/30">
      <div className="flex items-start justify-between gap-3">
        <h3 className="min-w-0 text-base font-bold text-white">
          <Link href={`/protocols/${protocol.id}`} className="break-words hover:text-emerald-200">
            {protocol.name}
          </Link>
        </h3>
        <span className="shrink-0 rounded-lg border border-emerald-400/20 bg-emerald-500/10 px-2.5 py-1 text-sm font-semibold text-emerald-200">
          {protocol.stackScore.score}
        </span>
      </div>

      <div className="mt-3 flex flex-wrap items-center gap-2 text-xs">
        <span className="rounded-lg border border-white/[0.08] bg-white/[0.04] px-2 py-1 font-semibold text-white/70">
          v{protocol.version}
        </span>
        <span
          className={`rounded-lg border px-2 py-1 font-semibold ${
            protocol.isDraft
              ? 'border-sky-400/20 bg-sky-500/10 text-sky-200'
              : 'border-emerald-400/20 bg-emerald-500/10 text-emerald-200'
          }`}
        >
          {protocol.isDraft ? 'Draft' : 'Active'}
        </span>
        <span className="rounded-lg border border-white/[0.08] bg-white/[0.04] px-2 py-1 font-semibold text-white/60">
          {protocol.isCurrentVersion ? 'Current version' : 'Prior version'}
        </span>
        {protocol.priorVersions.length > 0 && (
          <span className="text-white/45">
            {protocol.priorVersions.length} prior version{protocol.priorVersions.length === 1 ? '' : 's'}
          </span>
        )}
      </div>

      <p className="mt-3 break-words text-sm leading-6 text-white/60">{compoundSummary(protocol)}</p>

      <p className="mt-2 text-xs text-white/45">
        {protocol.activeRun
          ? `Run active since ${new Date(protocol.activeRun.startedAtUtc).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric', timeZone: 'UTC' })}`
          : 'No active run yet'}
      </p>

      {protocol.evolvedFromRunId && (
        <p className="mt-2 text-xs text-white/40">Derived from an observed run. Compare changes before tracking.</p>
      )}

      {protocol.stackScore.chips.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-2">
          {protocol.stackScore.chips.slice(0, 3).map((chip) => (
            <span key={chip} className="rounded-lg border border-white/[0.08] px-2 py-1 text-xs text-white/50">
              {chip}
            </span>
          ))}
        </div>
      )}

      <div className="mt-4 flex flex-wrap gap-2 pt-1">
        <Link
          href={`/protocols/${protocol.id}`}
          className="inline-flex rounded-lg bg-emerald-500/90 px-3 py-1.5 text-xs font-semibold text-slate-950 transition-colors hover:bg-emerald-400"
        >
          Review protocol
        </Link>
        <Link
          href={`/protocols/${protocol.id}#provider-summary`}
          className="inline-flex rounded-lg border border-white/[0.08] px-3 py-1.5 text-xs font-semibold text-white/60 transition-colors hover:border-sky-300/25 hover:text-sky-100"
        >
          Provider summary
        </Link>
      </div>
    </article>
  );
}

export default function ProtocolsPage() {
  const router = useRouter();
  const { currentProfileId } = useProfile();
  const [protocols, setProtocols] = useState<Protocol[]>([]);
  const [currentStack, setCurrentStack] = useState<CurrentStackIntelligence | null>(null);
  const [name, setName] = useState('');
  const nameInputRef = useRef<HTMLInputElement>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [stackLockedMessage, setStackLockedMessage] = useState<string | null>(null);

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
      setStackLockedMessage(null);
      const protocolData = await apiClient.getProtocols(currentProfileId);
      setProtocols(protocolData);

      try {
        const stackData = await apiClient.getCurrentStackIntelligence(currentProfileId);
        setCurrentStack(stackData);
      } catch (err) {
        if (err instanceof ApiError && err.upgradeRequired) {
          setCurrentStack(null);
          setStackLockedMessage(err.message);
        } else {
          throw err;
        }
      }
    } catch (err) {
      setError('Failed to load protocols');
    } finally {
      setLoading(false);
    }
  }

  function requestTracking() {
    nameInputRef.current?.focus({ preventScroll: true });
    nameInputRef.current?.scrollIntoView({ block: 'center' });
  }

  async function saveCurrentStack() {
    if (!currentProfileId || !name.trim() || saving) {
      return;
    }

    try {
      setSaving(true);
      setSaveError(null);
      const saved = await apiClient.saveCurrentStackAsProtocol(currentProfileId, name);
      setProtocols([saved, ...protocols]);
      setName('');
      router.push(`/protocols/${saved.id}`);
    } catch (err) {
      setSaveError('Save failed. Confirm the current stack has active compounds.');
    } finally {
      setSaving(false);
    }
  }

  const featuredProtocol =
    protocols.find((protocol) => protocol.activeRun) ??
    protocols.find((protocol) => protocol.isCurrentVersion) ??
    protocols[0];

  const saveForm = (
    <div className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5">
      <h3 className="text-lg font-bold text-white">Save the current stack as a protocol</h3>
      <p className="mt-2 text-sm leading-6 text-white/50">
        Give the snapshot a name to keep it as a version you can revisit, compare, and review later.
      </p>
      <label htmlFor="protocol-name" className="mt-5 block text-sm text-white/70">
        Protocol name
      </label>
      <div className="mt-2 flex gap-2">
        <input
          id="protocol-name"
          ref={nameInputRef}
          aria-describedby={saveError ? 'protocol-save-error' : undefined}
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="Recovery stack v1"
          className="min-w-0 flex-1 rounded-lg border border-white/[0.08] bg-black/20 px-3 py-2 text-sm text-white outline-none focus:border-emerald-400/50"
        />
        <button
          onClick={saveCurrentStack}
          disabled={saving || !name.trim()}
          className="shrink-0 rounded-lg bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {saving ? 'Saving' : 'Save'}
        </button>
      </div>
      {saveError && (
        <p id="protocol-save-error" role="alert" className="mt-3 text-sm text-rose-200">
          {saveError}
        </p>
      )}
    </div>
  );

  if (!currentProfileId) {
    return (
      <div className="w-full">
        <Header
          title="Protocols"
          subtitle="Versioned plans of compounds and phases — see what each version contains and where it stands"
          actions={<ProfileSwitcher />}
        />
        <div className="p-8">
          <EmptyState
            title="Create a profile to start building protocols"
            description="Protocols live on a profile: each one is a versioned plan of the compounds and phases that profile is running. Set up a profile first and every version will stay organized in one place."
            icon="🧬"
            action={{ label: 'Create profile', onClick: () => router.push('/profiles') }}
          />
        </div>
      </div>
    );
  }

  if (error && !loading) {
    return (
      <div className="w-full">
        <Header
          title="Protocols"
          subtitle="Versioned plans of compounds and phases — see what each version contains and where it stands"
          actions={<ProfileSwitcher />}
        />
        <div className="p-8">
          <ErrorState title="Couldn't load protocols" message={error} onRetry={loadProtocols} />
        </div>
      </div>
    );
  }

  return (
    <div className="w-full">
      <Header
        title="Protocols"
        subtitle="Versioned plans of compounds and phases — see what each version contains and where it stands"
        actions={<ProfileSwitcher />}
      />

      <main className="max-w-6xl space-y-8 p-8">
        <ActiveProfileChip />

        {loading ? (
          <LoadingSkeleton />
        ) : (
          <>
            <section className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-6">
              <h2 className="text-xl font-bold text-white">Protocols for this profile</h2>
              <p className="mt-2 max-w-3xl text-sm leading-6 text-white/55">
                A protocol is a versioned plan of compounds and phases for a profile. Each saved version
                records what it contains, whether it is still a draft or in play, and whether a run is
                tracking it — so you can revisit the plan and compare versions over time.
              </p>
              <div className="mt-5 flex flex-wrap gap-3">
                <Link
                  href="/my-protocol"
                  className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-400"
                >
                  Open My Protocol
                </Link>
                {featuredProtocol && (
                  <Link
                    href={`/protocols/${featuredProtocol.id}`}
                    className="rounded-lg border border-white/[0.1] px-4 py-2 text-sm font-semibold text-white/75 transition-colors hover:border-white/20"
                  >
                    Review a protocol
                  </Link>
                )}
              </div>
            </section>

            <section>
              <h2 className="text-lg font-semibold text-white">Saved protocols</h2>
              <p className="mt-1 max-w-3xl text-sm leading-6 text-white/50">
                New snapshots appear here when you save the current stack. Open any protocol to review its
                compounds, compare versions, or read the provider summary.
              </p>

              {protocols.length === 0 ? (
                <div className="mt-4">
                  <EmptyState
                    icon="📋"
                    title="No protocols saved yet"
                    description="A protocol keeps a named, versioned snapshot of what this profile is running — its compounds, phases, and how each version tracked. Save the current stack below to create the first one."
                    action={{ label: 'Save the current stack', onClick: requestTracking }}
                    secondaryAction={{ label: 'Open My Protocol', href: '/my-protocol' }}
                  />
                </div>
              ) : (
                <div className="mt-4 grid gap-4 sm:grid-cols-2">
                  {protocols.map((protocol) => (
                    <ProtocolCard key={protocol.id} protocol={protocol} />
                  ))}
                </div>
              )}
            </section>

            <section>
              <h2 className="text-lg font-semibold text-white">Current stack</h2>
              <p className="mt-1 max-w-3xl text-sm leading-6 text-white/50">
                A live read of the compounds this profile is tracking right now — before it becomes a saved
                protocol. Save it above or below to give the snapshot a name and start versioning it.
              </p>

              {currentStack ? (
                <div className="mt-4 grid gap-6 lg:grid-cols-2">
                  <div className="min-w-0 space-y-6">
                    {saveForm}
                    <StackScoreCard score={currentStack.stackScore} />
                  </div>
                  <div className="min-w-0 space-y-6">
                    <InteractionIntelligenceCard
                      intelligence={currentStack.interactionIntelligence}
                      title="Current stack read"
                      showTrackingCta
                      onTrackingRequest={requestTracking}
                    />
                    <SimulationTimeline simulation={currentStack.simulation} />
                  </div>
                </div>
              ) : (
                <div className="mt-4 max-w-3xl space-y-6">
                  <LockedTierCard
                    eyebrow="Operator — Track & Analyze"
                    title="See how your current stack fits together"
                    detail={
                      stackLockedMessage ??
                      'Score your active stack, surface synergies and conflicts, and run counterfactual scenarios — all included in Operator.'
                    }
                  />
                  {saveForm}
                </div>
              )}
            </section>
          </>
        )}
      </main>
    </div>
  );
}
