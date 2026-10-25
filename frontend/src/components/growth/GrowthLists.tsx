import Link from 'next/link';
import { VerdictChip, HYPE_METHOD_HREF } from '@/components/knowledge/PublicClaimsPanel';
import { HYPE_LIST_METHOD_NOTE, TRENDING_LABEL } from '@/lib/editorialCalendar';
import type { GrowthFeed } from '@/lib/growthFeed';

/**
 * Homepage / library lists (03 §5). Each block renders only when it has published claims
 * behind it, so nothing unverified shows. No streaks, points, badges or leaderboards.
 */
export function GrowthLists({ feed, showSearch = true }: { feed: GrowthFeed; showSearch?: boolean }) {
  return (
    <section aria-label="Settle it" className="mx-auto max-w-5xl px-5 py-8 sm:px-8 space-y-6" data-testid="growth-lists">
      {showSearch && (
        <form action="/check" method="get" role="search" className="flex flex-wrap items-center gap-3">
          <label htmlFor="ask-biostack" className="sr-only">Ask BioStack</label>
          <input id="ask-biostack" name="q" type="search" placeholder="Ask BioStack: paste a compound or a claim"
            className="min-w-0 flex-1 rounded-xl border border-white/15 bg-white/[0.04] px-4 py-3 text-white placeholder:text-white/40" />
          <button type="submit" className="rounded-xl border border-emerald-300/40 px-4 py-3 text-sm font-semibold text-emerald-100">Settle it.</button>
        </form>
      )}
      {feed.today && (
        <p className="text-sm text-white/80" data-testid="todays-claim">
          <span className="text-[11px] uppercase tracking-[0.2em] text-white/45 mr-2">Today&rsquo;s claim</span>
          <Link href={feed.today.permalink} className="underline underline-offset-4">&ldquo;{feed.today.statement}&rdquo;</Link>{' '}
          <VerdictChip verdict={feed.today.verdict} />
        </p>
      )}
      {feed.myth && (
        <p className="text-sm text-white/80" data-testid="myth-of-the-week">
          <span className="text-[11px] uppercase tracking-[0.2em] text-white/45 mr-2">Myth of the week</span>
          &ldquo;{feed.myth.statement}&rdquo; <VerdictChip verdict={feed.myth.verdict} />{' '}
          <Link href={feed.myth.mythHref} className="text-cyan-300 underline underline-offset-4">Read the receipts</Link>
        </p>
      )}
      {feed.hypeGap.length > 0 && (
        <div data-testid="hype-gap-list">
          <h2 className="text-sm font-medium text-white/80 mb-1">Biggest Hype Gap this week</h2>
          <ol className="list-decimal pl-5 space-y-1 text-sm text-white/75">
            {feed.hypeGap.map(c => (
              <li key={c.id}>
                <Link href={c.href} className="underline underline-offset-4">{c.compound}: &ldquo;{c.statement}&rdquo;</Link> · Hype Gap: {c.hypeBand}
              </li>
            ))}
          </ol>
          <p className="mt-1 text-xs text-white/45">{HYPE_LIST_METHOD_NOTE} <a href={HYPE_METHOD_HREF} className="underline underline-offset-4">How we measure hype</a></p>
        </div>
      )}
      {feed.trending.length > 0 && (
        <div data-testid="trending-list">
          <h2 className="text-sm font-medium text-white/80 mb-1">{TRENDING_LABEL}</h2>
          <ul className="flex flex-wrap gap-3 text-sm">
            {feed.trending.map(t => <li key={t.slug}><Link href={t.href} className="text-cyan-300 underline underline-offset-4">{t.name}</Link></li>)}
          </ul>
          <p className="mt-1 text-xs text-white/45">Unique visitors to each dossier over the last 7 days. It counts lookups, not use.</p>
        </div>
      )}
      {feed.headToHead.length > 0 && (
        <nav aria-label="Head to head" className="flex flex-wrap gap-3 text-sm text-white/70">
          <span className="text-white/50">Head to head:</span>
          {feed.headToHead.map(h => <Link key={h.href} href={h.href} className="text-cyan-300 underline underline-offset-4">{h.label}</Link>)}
        </nav>
      )}
    </section>
  );
}
