import type { Metadata } from 'next';
import Link from 'next/link';
import { CheckLogger } from '@/components/check/CheckLogger';
import { SubmitClaimForm } from '@/components/check/SubmitClaimForm';
import { VerdictChip } from '@/components/knowledge/PublicClaimsPanel';
import { NO_MATCH_COPY, PERSONAL_ADVICE_COPY, checkClaim } from '@/lib/claimCheck';
import { fetchAllEntries, publishedLibrary } from '@/lib/claimLibrary';
import { displayVerdict, formatAsOf } from '@/lib/claimVerdicts';
import { compoundSlug } from '@/lib/compoundSlug';
import { NO_DOSING_RESPONSE } from '@/lib/copyLint';
import { redactDoseText } from '@/lib/doseRedaction';
import { createPublicPageMetadata } from '@/lib/site';
import { cardImageUrl } from '@/lib/verdictCard';

interface PageProps { searchParams: Promise<{ q?: string | string[] }> }

const first = (v?: string | string[]) => (Array.isArray(v) ? v[0] : v) ?? '';

export async function generateMetadata({ searchParams }: PageProps): Promise<Metadata> {
  const q = first((await searchParams).q).trim();
  const meta = createPublicPageMetadata({
    title: 'Check a claim | BioStack',
    description: 'Paste a peptide or compound claim you saw online and see the reviewed verdict, with receipts.',
    path: '/check',
  });
  // /check?q=... is shareable but never indexed (03 §7).
  return q ? { ...meta, robots: { index: false, follow: true } } : meta;
}

export default async function CheckPage({ searchParams }: PageProps) {
  const q = first((await searchParams).q);
  const entries = await fetchAllEntries();
  const result = checkClaim(q, publishedLibrary(entries), entries);
  const best = result.matches[0];
  const compounds = entries.map(e => ({ slug: compoundSlug(e.canonicalName), name: e.canonicalName })).sort((a, b) => a.name.localeCompare(b.name));

  return (
    <main className="max-w-3xl mx-auto px-4 py-10 space-y-6">
      <h1 className="text-2xl font-semibold text-white">Check a claim</h1>
      <form action="/check" method="get" role="search" className="flex flex-wrap gap-2">
        <label htmlFor="check-q" className="sr-only">The claim you saw</label>
        <input id="check-q" name="q" type="search" defaultValue={result.query} maxLength={500}
          placeholder='Paste it: "MK-677 has no side effects"'
          className="min-w-0 flex-1 rounded-lg border border-white/15 bg-black/30 p-2 text-white" />
        <button type="submit" className="rounded-lg border border-cyan-300/30 px-4 py-2 text-sm text-cyan-100">Check it</button>
      </form>

      {result.query && <CheckLogger query={result.query} matchedClaimId={best?.item.claim.id ?? null} doseIntent={result.doseIntent} />}

      {result.query && result.doseIntent && (
        <section data-testid="no-dosing" className="rounded-xl border border-amber-300/20 bg-amber-400/[0.05] p-4 space-y-2">
          <p className="text-white/85">{NO_DOSING_RESPONSE}</p>
          {result.compound && (
            <Link href={`/knowledge/${compoundSlug(result.compound.canonicalName)}#study-details`} className="text-sm text-cyan-300 underline underline-offset-4">
              Trial conditions for {result.compound.canonicalName}
            </Link>
          )}
        </section>
      )}
      {result.query && result.personalAdvice && <p data-testid="personal-advice" className="text-white/75">{PERSONAL_ADVICE_COPY}</p>}

      {result.query && result.matches.length > 0 && (
        <ol className="space-y-3" data-testid="check-results">
          {result.matches.map(({ item }, i) => {
            const href = `/knowledge/${item.slug}/claims/${item.claim.id}`;
            return (
              <li key={item.claim.id} className="rounded-xl border border-white/10 bg-white/[0.02] p-4 space-y-2">
                <p className="text-xs text-white/50">{item.entry.canonicalName} · evidence as of {formatAsOf(item.claim.publishedAt ?? item.claim.verifiedAt)}</p>
                <p className="text-white"><Link href={href} className="underline underline-offset-4">&ldquo;{redactDoseText(item.claim.statement)}&rdquo;</Link></p>
                <VerdictChip verdict={displayVerdict(item.claim)} />
                {item.claim.verdict === 'PROVEN IN HUMANS' && item.claim.qualifier && <p className="text-xs text-white/60">{item.claim.qualifier}</p>}
                {item.claim.note && <p className="text-sm text-white/70">{redactDoseText(item.claim.note)}</p>}
                {i === 0 && (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={cardImageUrl('claim', item.claim.id, item.claim.version)} alt={`BioStack verdict card for ${item.entry.canonicalName}`}
                    width={600} height={315} loading="lazy" className="w-full rounded-lg border border-white/10" />
                )}
              </li>
            );
          })}
        </ol>
      )}

      {result.query && result.matches.length === 0 && (
        <section data-testid="no-match" className="space-y-4">
          <p className="text-white/80">{NO_MATCH_COPY}</p>
          <SubmitClaimForm compounds={compounds} initialClaim={result.query} initialCompound={result.compound ? compoundSlug(result.compound.canonicalName) : ''} />
        </section>
      )}

      <p className="text-xs text-white/50">The checker only returns verdicts our reviewers have already published. It never makes one up.</p>
    </main>
  );
}
