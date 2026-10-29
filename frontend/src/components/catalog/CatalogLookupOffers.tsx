'use client';

import { apiClient } from '@/lib/api';
import type { CatalogCandidate, CatalogLookup } from '@/lib/types';
import Link from 'next/link';
import { useCallback, useEffect, useRef, useState } from 'react';

/** Delays between status checks while the server looks a name up: about 13 seconds in all, then it waits for the note. */
export const LOOKUP_POLL_DELAYS_MS = [1000, 1000, 2000, 3000, 3000, 3000];

/** "Same as" for an exact, alternate-name or slug match; "Similar" for anything close but not certain. Never an automatic link. */
export function candidateLead(candidate: CatalogCandidate): string {
  return candidate.label === 'same_as' ? 'Same as' : 'Similar';
}

/** The offers for one item. Linking and declining are always the person's own action. */
export function CatalogOffers({ itemName, candidates, busy, onLink, onDecline, hideSlugs = [] }: {
  itemName: string;
  candidates: CatalogCandidate[];
  busy: boolean;
  onLink: (slug: string) => void;
  onDecline: () => void;
  /** Slugs another offer on the same card already shows. */
  hideSlugs?: string[];
}) {
  const shown = candidates.filter((c) => !hideSlugs.includes(c.slug));
  if (shown.length === 0) return null;
  return (
    <div className="mt-2 space-y-1" data-testid="catalog-offers">
      {shown.map((c) => (
        <p key={c.slug} className="text-sm text-white/70">
          {candidateLead(c)} <strong>{c.name}</strong>?{' '}
          <button
            type="button"
            disabled={busy}
            aria-label={`Link ${itemName} to ${c.name}`}
            className="underline disabled:opacity-50"
            onClick={() => onLink(c.slug)}
          >
            Link it
          </button>
        </p>
      ))}
      <button
        type="button"
        disabled={busy}
        aria-label={`${itemName} is not the same`}
        className="text-xs text-white/55 underline disabled:opacity-50"
        onClick={onDecline}
      >
        Not the same
      </button>
    </div>
  );
}

/**
 * Starts nothing itself: follows one lookup that already exists, polling briefly for the live result, and shows what it found.
 * `onSettled` fires after the person links or declines so the parent can reload its list.
 */
export function CatalogLookupPanel({ lookupId, itemName, onSettled }: {
  lookupId: string;
  itemName: string;
  onSettled?: (outcome: 'linked' | 'declined') => void;
}) {
  const [lookup, setLookup] = useState<CatalogLookup | null>(null);
  const [failed, setFailed] = useState(false);
  const [waitedOut, setWaitedOut] = useState(false);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const mounted = useRef(true);

  useEffect(() => {
    mounted.current = true;
    return () => { mounted.current = false; };
  }, []);

  useEffect(() => {
    let cancelled = false;
    let timer: ReturnType<typeof setTimeout> | undefined;
    const check = async (attempt: number) => {
      try {
        const current = await apiClient.getCatalogLookup(lookupId);
        if (cancelled) return;
        setLookup(current);
        setFailed(false);
        if (current.status !== 'pending') return;
        if (attempt >= LOOKUP_POLL_DELAYS_MS.length) { setWaitedOut(true); return; }
        timer = setTimeout(() => { void check(attempt + 1); }, LOOKUP_POLL_DELAYS_MS[attempt]);
      } catch {
        if (!cancelled) setFailed(true);
      }
    };
    void check(0);
    return () => { cancelled = true; if (timer) clearTimeout(timer); };
  }, [lookupId]);

  const answer = useCallback(async (action: () => Promise<CatalogLookup>, outcome: 'linked' | 'declined') => {
    setBusy(true);
    setActionError(null);
    try {
      const next = await action();
      if (!mounted.current) return;
      setLookup(next);
      onSettled?.(outcome);
    } catch {
      if (mounted.current) setActionError(`Couldn't update ${itemName}. Nothing was changed.`);
    } finally {
      if (mounted.current) setBusy(false);
    }
  }, [itemName, onSettled]);

  if (failed && !lookup) {
    return <p role="status" className="mt-2 text-xs text-white/55">We couldn&apos;t check the library just now. Your item is saved as you entered it.</p>;
  }
  if (!lookup || lookup.status === 'pending') {
    return (
      <p role="status" className="mt-2 text-sm text-white/65">
        {waitedOut
          ? 'This is taking longer than usual. Your item is saved. We\u2019ll leave a note here if we find a match.'
          : 'Please wait while we look this up\u2026'}
      </p>
    );
  }
  if (lookup.status === 'matched') {
    return (
      <div>
        <CatalogOffers
          itemName={itemName}
          candidates={lookup.candidates}
          busy={busy}
          onLink={(slug) => { void answer(() => apiClient.linkCatalogLookup(lookup.id, slug), 'linked'); }}
          onDecline={() => { void answer(() => apiClient.declineCatalogLookup(lookup.id), 'declined'); }}
        />
        {actionError && <p role="alert" className="mt-1 text-sm text-amber-200">{actionError}</p>}
      </div>
    );
  }
  if (lookup.status === 'linked') {
    const linked = lookup.candidates.length === 1 ? lookup.candidates[0] : undefined;
    return (
      <p role="status" className="mt-2 text-sm text-emerald-300">
        Linked{linked ? <> to <Link href={`/knowledge/${linked.slug}`} className="underline">{linked.name}</Link></> : ''}.
      </p>
    );
  }
  if (lookup.status === 'declined') {
    return <p role="status" className="mt-2 text-xs text-white/55">Okay, it stays as you entered it.</p>;
  }
  return (
    <p role="status" className="mt-2 text-xs text-white/55">
      We didn&apos;t find this in the BioStack library yet. It stays in your list as Unverified.
    </p>
  );
}
