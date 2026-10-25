'use client';

import { useState } from 'react';
import type { KnowledgeEntry } from '@/lib/types';
import { displayVerdict, formatAsOf, headlineClaim, isPublished, shareText } from '@/lib/claimVerdicts';
import { HYPE_METHOD_HREF, VerdictChip } from './PublicClaimsPanel';
import { cardImageUrl } from '@/lib/verdictCard';
import { iframeSnippet } from '@/lib/embed';
import { compoundSlug } from '@/lib/compoundSlug';

export const VERDICT_FOOTNOTE = 'A verdict describes the research, not what\u2019s right for you.';

/**
 * Growth spec Phase 2 above-the-fold block: compound, headline claim, verdict chip, PROVEN
 * qualifier, Hype Gap band, "as of" date and a share button; then the Trials / Internet /
 * Gap panel. No tracking CTA here (keeps the referee separate from the tracker).
 */
export function VerdictBlock({ entry, path }: { entry: KnowledgeEntry; path: string }) {
  const claims = entry.details?.publicClaims ?? [];
  const headline = headlineClaim(claims);
  const [shared, setShared] = useState('');
  const [copied, setCopied] = useState(false);
  const [embedCopied, setEmbedCopied] = useState(false);
  if (!headline) return null;

  const published = isPublished(headline);
  const verdict = displayVerdict(headline);
  const asOf = published ? formatAsOf(headline.publishedAt ?? headline.verifiedAt) : '';

  async function onCopy() {
    try {
      await navigator.clipboard.writeText(`${window.location.origin}${path}`);
      setCopied(true);
    } catch {
      setCopied(false);
    }
  }

  async function onEmbed() {
    try {
      await navigator.clipboard.writeText(iframeSnippet(headline!.id, `${entry.canonicalName}: ${headline!.statement}`));
      setEmbedCopied(true);
    } catch {
      setEmbedCopied(false);
    }
  }

  async function onShare() {
    const url = typeof window === 'undefined' ? `https://biostack.cc${path}` : `${window.location.origin}${path}`;
    const text = shareText(entry.canonicalName, headline!, path);
    try {
      if (typeof navigator.share === 'function') {
        await navigator.share({ title: `${entry.canonicalName} | BioStack`, text, url });
        setShared('Shared');
        return;
      }
      await navigator.clipboard.writeText(`${text}`);
      setShared('Link copied');
    } catch {
      setShared('');
    }
  }

  return (
    <section aria-label="Verdict" data-testid="verdict-block"
      className="max-w-3xl mx-auto mb-6 rounded-2xl border border-white/12 bg-white/[0.05] p-5 space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold text-white">{entry.canonicalName}</h1>
          <p className="text-base text-white/85" data-testid="headline-claim">&ldquo;{headline.statement}&rdquo;</p>
          <a href={`/knowledge/${compoundSlug(entry.canonicalName)}/claims/${headline.id}`} className="text-xs text-cyan-300 underline underline-offset-4">Claim page</a>
        </div>
        <div className="flex flex-wrap gap-2" data-testid="share-sheet">
          <button type="button" onClick={onShare}
            className="rounded-lg border border-white/20 px-3 py-1.5 text-xs font-medium text-white/85 hover:bg-white/10 focus-visible:outline-2 focus-visible:outline-cyan-300">
            {shared || 'Settle it'}
          </button>
          <button type="button" onClick={onCopy}
            className="rounded-lg border border-white/15 px-3 py-1.5 text-xs text-white/70 hover:bg-white/10 focus-visible:outline-2 focus-visible:outline-cyan-300">
            {copied ? 'Copied' : 'Copy link'}
          </button>
          <a href={cardImageUrl('compound', compoundSlug(entry.canonicalName), headline.version, 'story')}
            download={`${compoundSlug(entry.canonicalName)}-biostack-story.png`}
            className="rounded-lg border border-white/15 px-3 py-1.5 text-xs text-white/70 hover:bg-white/10 focus-visible:outline-2 focus-visible:outline-cyan-300">
            Story card
          </a>
          {published && (
            <button type="button" onClick={onEmbed} title="Copy an embed snippet that always shows the latest verdict"
              className="rounded-lg border border-white/15 px-3 py-1.5 text-xs text-white/70 hover:bg-white/10 focus-visible:outline-2 focus-visible:outline-cyan-300">
              {embedCopied ? 'Embed copied' : 'Embed'}
            </button>
          )}
        </div>
      </div>

      <div className="space-y-1.5">
        <div className="text-lg"><VerdictChip verdict={verdict} /></div>
        {published && headline.qualifier && <p className="text-sm text-emerald-200/85">{headline.qualifier}</p>}
        {published && headline.hypeBand && (
          <p className="text-sm text-white/70">
            Hype Gap: <span className="font-semibold text-white/90">{headline.hypeBand}</span>{' '}
            <a href={HYPE_METHOD_HREF} className="text-cyan-300 underline underline-offset-4">How we measure hype</a>
          </p>
        )}
        {asOf && <p className="text-xs text-white/45">Evidence as of {asOf} · v{headline.version}</p>}
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3" data-testid="trials-internet-gap">
        <div className="rounded-xl border border-white/10 p-3">
          <p className="text-[11px] uppercase tracking-[0.15em] text-white/45 mb-1">What the trials showed</p>
          <p className="text-sm text-white/75">{published && headline.note ? headline.note : 'Not graded yet. Sources are listed below.'}</p>
        </div>
        <div className="rounded-xl border border-white/10 p-3">
          <p className="text-[11px] uppercase tracking-[0.15em] text-white/45 mb-1">What the internet says</p>
          <p className="text-sm text-white/75">&ldquo;{headline.statement}&rdquo;</p>
        </div>
        <div className="rounded-xl border border-white/10 p-3">
          <p className="text-[11px] uppercase tracking-[0.15em] text-white/45 mb-1">The gap</p>
          <p className="text-sm text-white/75">
            {published ? `${verdict}.${headline.hypeBand ? ` Hype Gap: ${headline.hypeBand}.` : ''}` : 'Not measured until the claim is graded.'}
          </p>
        </div>
      </div>
      <p className="text-[11px] text-white/40">{VERDICT_FOOTNOTE}</p>
    </section>
  );
}
