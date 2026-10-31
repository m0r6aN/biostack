'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { compoundSlug } from '@/lib/compoundSlug';
import { redactDoseText } from '@/lib/doseRedaction';
import {
  COMMUNITY_EMPTY, COMMUNITY_HEADER, COMMUNITY_UNVERIFIED, type CommunityThemes,
  fetchCommunityThemes, hasCommunityThemes,
} from '@/lib/communityThemes';
import type { KnowledgeEntry } from '@/lib/types';

const unique = (xs: string[]) => Array.from(new Set(xs.map(x => redactDoseText(x).trim()).filter(Boolean)));

/**
 * Growth Phase 9 (03 §9): collapsed "What people report (anecdote)" fold. Clinical data and
 * aggregated community themes sit in visually separate columns; side effects come first at the
 * same type size as benefits. No individual reports, quotes, counts per theme or percentages.
 */
export function CommunityColumns({ entry, initialThemes }: { entry: KnowledgeEntry; initialThemes?: CommunityThemes }) {
  const slug = compoundSlug(entry.canonicalName);
  const [themes, setThemes] = useState<CommunityThemes | null>(initialThemes ?? null);

  useEffect(() => {
    if (initialThemes) return;
    let live = true;
    fetchCommunityThemes(slug).then(t => { if (live) setThemes(t); });
    return () => { live = false; };
  }, [slug, initialThemes]);

  const measured = unique((entry.details?.studiedUses ?? []).map(u => u.useCase)).slice(0, 6);
  const adverse = unique([...(entry.details?.safety?.adverseEffectsCommon ?? []), ...(entry.details?.safety?.adverseEffectsSerious ?? [])]).slice(0, 8);
  const shown = themes && hasCommunityThemes(themes);

  return (
    <details className="max-w-3xl mx-auto mt-6 rounded-2xl border border-white/10 bg-white/[0.02] p-4" data-testid="community-columns">
      <summary className="cursor-pointer text-sm font-semibold text-white/80">What people report (anecdote)</summary>
      <div className="mt-4 grid grid-cols-1 gap-4 md:grid-cols-2">
        <section aria-labelledby="clinical-col" className="rounded-xl bg-cyan-500/[0.05] p-4">
          <h3 id="clinical-col" className="text-xs font-semibold tracking-wide text-cyan-200">CLINICAL DATA <span className="font-normal text-white/50">(from trials)</span></h3>
          <p className="mt-3 text-sm text-white/60">Adverse events:</p>
          {adverse.length > 0 ? <p className="text-sm text-white/85">{adverse.join(' · ')}</p> : <p className="text-sm text-white/50">None recorded in our sources.</p>}
          <p className="mt-3 text-sm text-white/60">Effects measured:</p>
          {measured.length > 0 ? <p className="text-sm text-white/85">{measured.join(' · ')}</p> : <p className="text-sm text-white/50">Human trials: none found.</p>}
        </section>
        <section aria-labelledby="community-col" className="rounded-xl border border-dashed border-amber-300/40 bg-amber-500/[0.04] p-4">
          <p className="text-[10px] font-bold tracking-widest text-amber-200">ANECDOTE</p>
          <h3 id="community-col" className="text-xs font-semibold tracking-wide text-white/80">{COMMUNITY_HEADER}</h3>
          {themes === null ? (
            <p className="mt-3 text-sm text-white/50">Loading…</p>
          ) : shown ? (
            <>
              <p className="mt-3 text-sm text-white/60">Side effects reported:</p>
              <p className="text-sm text-white/85" data-testid="community-side-effects">{themes.sideEffects.length > 0 ? themes.sideEffects.map(t => t.label).join(' · ') : 'None above the reporting threshold.'}</p>
              <p className="mt-3 text-sm text-white/60">Benefits reported:</p>
              <p className="text-sm text-white/85" data-testid="community-benefits">{themes.benefits.length > 0 ? themes.benefits.map(t => t.label).join(' · ') : 'None above the reporting threshold.'}</p>
              <p className="mt-3 text-xs text-white/60">Based on {themes.submissions} submissions.</p>
              <p className="text-xs text-white/60">{COMMUNITY_UNVERIFIED}</p>
            </>
          ) : (
            <p className="mt-3 text-sm text-white/70">{COMMUNITY_EMPTY}</p>
          )}
          <Link href={`/submit?compound=${encodeURIComponent(slug)}`} className="mt-3 inline-block text-xs text-cyan-300 underline underline-offset-4">Submit a claim you saw</Link>
        </section>
      </div>
    </details>
  );
}
