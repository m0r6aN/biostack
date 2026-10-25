import type { ClaimVerdict, KnowledgePublicClaim } from '@/lib/types';
import { formatAsOf, STATUS_COPY } from '@/lib/claimVerdicts';
import { isAllowlistedSourceUrl } from '@/lib/outboundLinks';

export { formatAsOf };

/**
 * Growth spec Phase 1/2: claims as people state them, each with its verdict. A claim shows
 * a verdict, note and Hype Gap band only after the two-reviewer AI council signs off
 * (or Clint resolves an escalation); until then it reads NOT YET GRADED with its sources.
 * Never shows a dose.
 */
export const HYPE_METHOD_HREF = '/knowledge/methodology#hype-gap';
export const VERDICT_METHOD_HREF = '/knowledge/methodology#verdicts';

const VERDICT_STYLES: Record<ClaimVerdict, string> = {
  'PROVEN IN HUMANS': 'border-emerald-400/40 text-emerald-200',
  PROMISING: 'border-cyan-400/40 text-cyan-200',
  'ANIMAL ONLY': 'border-violet-400/40 text-violet-200',
  THIN: 'border-amber-400/40 text-amber-200',
  MYTH: 'border-rose-400/40 text-rose-200',
  'NOT YET GRADED': 'border-white/20 text-white/60',
};

function safeHref(value: string): string | null {
  try {
    const url = new URL(value);
    if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password) return null;
    // 06 §B: only allowlisted source domains become outbound links.
    if (!isAllowlistedSourceUrl(url.href)) return null;
    return url.href;
  } catch {
    return null;
  }
}

export function VerdictChip({ verdict }: { verdict: ClaimVerdict }) {
  return (
    <span data-testid="verdict-chip"
      className={`inline-block rounded border px-1.5 py-0.5 text-[11px] font-semibold tracking-wide ${VERDICT_STYLES[verdict] ?? VERDICT_STYLES['NOT YET GRADED']}`}>
      {verdict}
    </span>
  );
}

export function ClaimRow({ claim, href }: { claim: KnowledgePublicClaim; href?: string }) {
  const published = claim.published && claim.verdict !== 'NOT YET GRADED';
  const asOf = published ? formatAsOf(claim.publishedAt ?? claim.verifiedAt) : '';
  return (
    <li className="rounded-xl border border-white/10 bg-white/[0.02] p-3 space-y-2" data-testid="public-claim">
      <div className="flex flex-wrap items-center gap-2">
        <VerdictChip verdict={published ? claim.verdict : 'NOT YET GRADED'} />
        <p className="text-sm text-white/85">&ldquo;{claim.statement}&rdquo;</p>
        {href && <a href={href} className="text-xs text-cyan-300 underline underline-offset-4">Claim page</a>}
      </div>
      {published && claim.qualifier && <p className="text-xs text-emerald-200/80">{claim.qualifier}</p>}
      {published && claim.note && <p className="text-sm text-white/65">{claim.note}</p>}
      {!published && <p className="text-xs text-white/45">{STATUS_COPY[claim.status ?? 'pending-council'] ?? STATUS_COPY['pending-council']}</p>}
      {published && claim.hypeBand && (
        <p className="text-xs text-white/60">
          Hype Gap: <span className="font-medium text-white/80">{claim.hypeBand}</span>{' '}
          <a href={HYPE_METHOD_HREF} className="text-cyan-300 underline underline-offset-4">How we measure hype</a>
        </p>
      )}
      {claim.sources.length > 0 && (
        <ul className="text-xs text-white/55 space-y-0.5">
          {claim.sources.map(src => {
            const href = safeHref(src.url);
            return (
              <li key={src.id}>
                {href ? (
                  <a href={href} target="_blank" rel="noopener noreferrer" className="text-cyan-300/90 underline underline-offset-4 [overflow-wrap:anywhere]">
                    {src.title || href}<span className="sr-only"> (opens in a new tab)</span>
                  </a>
                ) : <span>{src.title}</span>}
              </li>
            );
          })}
        </ul>
      )}
      {asOf && (
        <p className="text-[11px] text-white/40">
          Evidence as of {asOf} · v{claim.version}
          {claim.reviewedBy === 'ai-council' && claim.reviewers.length > 0 && <> · Reviewed by the AI evidence council ({claim.reviewers.join(', ')})</>}
          {claim.reviewedBy === 'owner' && <> · Decided by the owner after council disagreement</>}
        </p>
      )}
    </li>
  );
}

export function PublicClaimsPanel({ claims }: { claims: KnowledgePublicClaim[] }) {
  if (claims.length === 0) return null;
  return (
    <section aria-label="What people claim" data-testid="public-claims">
      <h4 className="text-sm font-medium text-white/80 mb-1">What people claim, and what the evidence says</h4>
      <p className="text-xs text-white/45 mb-2">
        <a href={VERDICT_METHOD_HREF} className="underline underline-offset-4">How verdicts work</a>
      </p>
      <ul className="space-y-2">{claims.map(c => <ClaimRow key={c.id} claim={c} />)}</ul>
    </section>
  );
}
