'use client';

import { CatalogOffers } from '@/components/catalog/CatalogLookupOffers';
import { DoseLogForm } from '@/components/protocol-portal/DoseLogForm';
import { apiClient } from '@/lib/api';
import type { CatalogLookup, CompoundRecord, ScheduleItem, TakingItem, TakingResponse } from '@/lib/types';
import { formatDoseAmount } from '@/lib/userDose';
import { describeFrequency } from '@/lib/userFrequency';
import Link from 'next/link';
import { useEffect, useState } from 'react';

type LogEntry = { amount: number; unit: string; note: string };

type FilterId = 'all' | 'research' | 'hormonal' | 'supplements' | 'vitamins' | 'unverified';

const RESEARCH = ['Peptide', 'ResearchCompound', 'SmallMolecule', 'Biologic', 'Compound', 'Oligonucleotide', 'GeneTherapy', 'EpigeneticTherapy', 'CellTherapy'];
const HORMONAL = ['Sarm', 'Serm', 'Hormone'];
const SUPPLEMENTS = ['Supplement', 'Nutraceutical', 'Coenzyme', 'AminoAcid'];
const VITAMINS = ['Vitamin', 'Mineral'];

const FILTERS: { id: FilterId; label: string; matches: (item: CompoundRecord) => boolean }[] = [
  { id: 'all', label: 'All', matches: () => true },
  { id: 'research', label: 'Peptides & research compounds', matches: (i) => RESEARCH.includes(i.category) },
  { id: 'hormonal', label: 'SARMs/SERMs/hormones', matches: (i) => HORMONAL.includes(i.category) },
  { id: 'supplements', label: 'Supplements', matches: (i) => SUPPLEMENTS.includes(i.category) },
  { id: 'vitamins', label: 'Vitamins & minerals', matches: (i) => VITAMINS.includes(i.category) },
  { id: 'unverified', label: 'Unverified', matches: (i) => !isLinked(i) },
];

/** Legacy rows (no state) read as linked only when they carry a catalog slug. */
const isLinked = (item: CompoundRecord) => (item.verificationState ?? (item.catalogSlug ? 'catalog' : 'unverified')) === 'catalog' && Boolean(item.catalogSlug);

/** Lookups started for older items during this page visit, so reloading the list never asks twice. */
const lookupsStarted = new Set<string>();
const MAX_LAZY_LOOKUPS = 5;
/** Waits between re-checks while a lookup is still running, then it rests until the note arrives. */
const OPEN_LOOKUP_RECHECK_MS = [1000, 2000, 3000, 3000, 3000];

/** The PUT body for a change to one record. Response-only fields are dropped; catalogSlug is left out so a link is kept. */
function updateBody(item: CompoundRecord, changes: Partial<CompoundRecord>): Omit<CompoundRecord, 'id'> {
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const { id, verificationState, categorySource, catalogSlug, ...rest } = item;
  return { ...rest, ...changes };
}

/** A view of the user's own records. Linking to the catalog is always the user's own confirmed action. */
export function TakingTab({ profileId, onLogAmount }: {
  profileId: string;
  onLogAmount: (item: ScheduleItem, entry: LogEntry) => Promise<void>;
}) {
  const [taking, setTaking] = useState<TakingResponse | null>(null);
  const [error, setError] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [filter, setFilter] = useState<FilterId>('all');
  const [attempt, setAttempt] = useState(0);
  const [lookups, setLookups] = useState<CatalogLookup[]>([]);

  useEffect(() => {
    let current = true;
    apiClient.getTaking(profileId).then(
      (response) => { if (current) { setTaking(response); setError(false); } },
      () => { if (current) { setTaking(null); setError(true); } },
    );
    return () => { current = false; };
  }, [profileId, attempt]);

  // The internal "is this already in BioStack?" check. It only adds offers the person can confirm or dismiss; a failure
  // here never affects the list itself.
  useEffect(() => {
    if (!taking) return;
    let current = true;
    let timer: ReturnType<typeof setTimeout> | undefined;
    const load = async (round: number, start: boolean) => {
      try {
        let open = await apiClient.listOpenCatalogLookups(profileId);
        if (!Array.isArray(open)) open = [];
        if (start) {
          // Older free-text items that never had a check: ask once per visit, a handful at a time, for items with no offer yet.
          const covered = new Set(open.map((l) => l.compoundId));
          const legacy = taking.items
            .filter(({ compound, suggestions }) => ['active', 'paused'].includes(compound.status.toLowerCase())
              && !isLinked(compound) && suggestions.length === 0 && !covered.has(compound.id) && !lookupsStarted.has(compound.id))
            .slice(0, MAX_LAZY_LOOKUPS);
          for (const { compound } of legacy) {
            lookupsStarted.add(compound.id);
            try { await apiClient.createCatalogLookup(profileId, compound.name, compound.id); } catch { /* try again next visit */ }
          }
          if (legacy.length > 0) open = await apiClient.listOpenCatalogLookups(profileId);
        }
        if (!current) return;
        setLookups(open);
        if (open.some((l) => l.status === 'pending') && round < OPEN_LOOKUP_RECHECK_MS.length) {
          timer = setTimeout(() => { void load(round + 1, false); }, OPEN_LOOKUP_RECHECK_MS[round]);
        }
      } catch {
        if (current) setLookups([]);
      }
    };
    void load(0, true);
    return () => { current = false; if (timer) clearTimeout(timer); };
  }, [taking, profileId]);

  const answerLookup = async (item: CompoundRecord, action: () => Promise<unknown>) => {
    setBusyId(item.id);
    setActionError(null);
    try {
      await action();
      setAttempt((n) => n + 1);
    } catch {
      setActionError(`Couldn't update ${item.name}. Nothing was changed.`);
    } finally {
      setBusyId(null);
    }
  };

  const change = async (item: CompoundRecord, changes: Partial<CompoundRecord>) => {
    setBusyId(item.id);
    setActionError(null);
    try {
      await apiClient.updateCompound(profileId, item.id, updateBody(item, changes));
      setAttempt((n) => n + 1);
    } catch {
      setActionError(`Couldn't update ${item.name}. Nothing was changed.`);
    } finally {
      setBusyId(null);
    }
  };

  const active = FILTERS.find((f) => f.id === filter) ?? FILTERS[0];
  const visible = (taking?.items ?? []).filter(({ compound }) =>
    ['active', 'paused'].includes(compound.status.toLowerCase()) && active.matches(compound));
  const cap = taking?.cap;

  return (
    <section className="space-y-4" aria-label="Everything I'm Taking">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-xl font-semibold text-white">Everything I&apos;m Taking</h2>
          <p className="text-sm text-white/60">Only what you entered. BioStack doesn&apos;t suggest amounts.</p>
        </div>
        <Link href="/compounds" className="rounded-lg border border-white/20 px-3 py-2 text-sm text-white">Add or manage items</Link>
      </div>
      {cap?.limit != null && (
        <p className="text-xs text-white/55" data-testid="cap-line">
          Research or unconfirmed items: {cap.counted} of {cap.limit}. Confirmed supplements, vitamins, minerals and amino acids don&apos;t count.
        </p>
      )}
      <div role="group" aria-label="Filter items" className="flex flex-wrap gap-2">
        {FILTERS.map((f) => (
          <button
            key={f.id}
            type="button"
            aria-pressed={filter === f.id}
            onClick={() => setFilter(f.id)}
            className={`rounded-full border px-3 py-1 text-xs ${filter === f.id ? 'border-white/60 text-white' : 'border-white/15 text-white/60'}`}
          >
            {f.label}
          </button>
        ))}
      </div>
      {actionError && <p role="alert" className="text-sm text-amber-200">{actionError}</p>}
      {error ? (
        <div role="alert" className="text-sm text-white/70">
          Items could not be loaded.{' '}
          <button type="button" className="underline" onClick={() => { setError(false); setAttempt((n) => n + 1); }}>Try again</button>
        </div>
      ) : taking === null ? (
        <p className="text-sm text-white/60">Loading your items…</p>
      ) : visible.length === 0 ? (
        <p className="text-sm text-white/60">{filter === 'all' ? 'No active or paused items yet.' : 'No items match this filter.'}</p>
      ) : (
        <div className="space-y-3">
          {visible.map((entry) => (
            <TakingCard
              key={entry.compound.id}
              entry={entry}
              busy={busyId === entry.compound.id}
              onLink={(slug) => change(entry.compound, { catalogSlug: slug })}
              lookup={lookups.find((l) => l.compoundId === entry.compound.id && l.status === 'matched')}
              pendingLookup={lookups.some((l) => l.compoundId === entry.compound.id && l.status === 'pending')}
              onLinkLookup={(lookup, slug) => answerLookup(entry.compound, () => apiClient.linkCatalogLookup(lookup.id, slug))}
              onDeclineLookup={(lookup) => answerLookup(entry.compound, () => apiClient.declineCatalogLookup(lookup.id))}
              onToggleSummary={(next) => change(entry.compound, { includeInProviderSummary: next })}
              onLogAmount={onLogAmount}
            />
          ))}
        </div>
      )}
    </section>
  );
}

function TakingCard({ entry, busy, onLink, lookup, pendingLookup, onLinkLookup, onDeclineLookup, onToggleSummary, onLogAmount }: {
  entry: TakingItem;
  busy: boolean;
  onLink: (slug: string) => void;
  lookup?: CatalogLookup;
  pendingLookup: boolean;
  onLinkLookup: (lookup: CatalogLookup, slug: string) => void;
  onDeclineLookup: (lookup: CatalogLookup) => void;
  onToggleSummary: (next: boolean) => void;
  onLogAmount: (item: ScheduleItem, entry: LogEntry) => Promise<void>;
}) {
  const item = entry.compound;
  const scheduleItem: ScheduleItem = {
    name: item.name, time: '', detail: item.category,
    compoundId: item.id, doseAmount: item.doseAmount, doseUnit: item.doseUnit,
  };
  const times = item.frequencyTimesOfDay?.join(', ') || item.doseTime;
  const included = item.includeInProviderSummary !== false;
  return (
    <article className="rounded-xl border border-white/10 p-4">
      <div className="flex flex-wrap items-center gap-2">
        <h3 className="font-semibold text-white">{item.name}</h3>
        <span className="text-xs text-white/60">{item.status}</span>
        {isLinked(item) ? (
          <Link href={`/knowledge/${item.catalogSlug}`} className="text-xs text-emerald-300 underline">In BioStack</Link>
        ) : (
          <span
            title="We couldn't confirm this item. For Observer plans it counts toward your research limit until it's confirmed."
            className="text-xs text-white/60"
          >
            Unverified
          </span>
        )}
      </div>
      <p className="mt-1 text-sm text-white/60">{item.category}</p>
      <p className="mt-1 text-sm text-white/75">
        {[formatDoseAmount(item), item.doseFrequency, describeFrequency(item), times].filter(Boolean).join(' · ')}
      </p>
      {entry.suggestions.map((s) => (
        <p key={s.slug} className="mt-2 text-sm text-white/70">
          Same as <strong>{s.name}</strong>{s.matchedOn === 'alias' ? ' (matches its alternate name)' : ''}?{' '}
          <button
            type="button"
            disabled={busy}
            aria-label={`Link ${item.name} to ${s.name}`}
            className="underline disabled:opacity-50"
            onClick={() => onLink(s.slug)}
          >
            Link it
          </button>
        </p>
      ))}
      {lookup && !isLinked(item) && (
        <CatalogOffers
          itemName={item.name}
          candidates={lookup.candidates}
          busy={busy}
          hideSlugs={entry.suggestions.map((s) => s.slug)}
          onLink={(slug) => onLinkLookup(lookup, slug)}
          onDecline={() => onDeclineLookup(lookup)}
        />
      )}
      {pendingLookup && !lookup && !isLinked(item) && (
        <p role="status" className="mt-2 text-xs text-white/55">Please wait while we look this up…</p>
      )}
      <label className="mt-2 flex items-center gap-2 text-xs text-white/60">
        <input
          type="checkbox"
          checked={included}
          disabled={busy}
          onChange={(e) => onToggleSummary(e.target.checked)}
        />
        Include {item.name} in provider summary
      </label>
      {item.status.toLowerCase() === 'active' && (
        <DoseLogForm item={scheduleItem} onLog={(logged) => onLogAmount(scheduleItem, logged)} />
      )}
    </article>
  );
}
