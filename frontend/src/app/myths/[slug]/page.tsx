import type { Metadata } from 'next';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { HYPE_METHOD_HREF, VerdictChip } from '@/components/knowledge/PublicClaimsPanel';
import { formatAsOf } from '@/lib/claimVerdicts';
import { fetchAllEntries, publishedLibrary } from '@/lib/claimLibrary';
import { redactDoseText } from '@/lib/doseRedaction';
import { mythKicker, publishedMyths } from '@/lib/editorialCalendar';
import { isAllowlistedSourceUrl } from '@/lib/outboundLinks';
import { createPublicPageMetadata } from '@/lib/site';
import { cardImageUrl } from '@/lib/verdictCard';

export const revalidate = 3600;

interface PageProps { params: Promise<{ slug: string }> }

async function load(slug: string) {
  return publishedMyths(publishedLibrary(await fetchAllEntries())).find(m => m.slot.slug === slug) ?? null;
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const myth = await load(slug);
  if (!myth) return { title: 'Myth of the Week | BioStack', robots: { index: false, follow: true } };
  const { claim } = myth.hit;
  return createPublicPageMetadata({
    title: `Myth of the Week: “${claim.statement}” | BioStack`,
    description: claim.note || `The receipts on “${claim.statement}”.`,
    path: `/myths/${slug}`,
    image: { url: cardImageUrl('claim', claim.id, claim.version), width: 1200, height: 630, alt: `BioStack claim card: “${claim.statement}”, ${claim.verdict}` },
  });
}

/**
 * Myth page (05 §3), built only from the published claim record and the compound record:
 * the receipts are the council-published note and sources, "what we don't know" is the
 * record's evidence gaps, and "risks on record" is its safety data. No dose anywhere.
 */
export default async function MythPage({ params }: PageProps) {
  const { slug } = await params;
  const myth = await load(slug);
  if (!myth) notFound();
  const { entry, claim, slug: compound } = myth.hit;
  const safety = entry.details?.safety;
  const risks = [
    ...(safety?.warnings ?? []).map(w => w.item),
    ...(safety?.adverseEffectsSerious ?? []),
    ...(safety?.adverseEffectsCommon ?? []),
  ].map(redactDoseText).filter(Boolean);
  const gaps = (entry.details?.evidenceGaps ?? []).map(redactDoseText).slice(0, 3);
  return (
    <main className="max-w-3xl mx-auto px-4 py-10 space-y-6">
      <p className="text-xs uppercase tracking-[0.2em] text-white/50">{mythKicker(claim.verdict)}</p>
      <h1 className="text-2xl font-semibold text-white">Myth of the Week: &ldquo;{claim.statement}&rdquo;</h1>
      <p className="text-sm text-white/70 flex flex-wrap gap-3 items-center">
        <VerdictChip verdict={claim.verdict} />
        {claim.hypeBand && <span>Hype Gap: {claim.hypeBand} <a href={HYPE_METHOD_HREF} className="underline underline-offset-4">How we measure hype</a></span>}
        <span>Evidence as of {formatAsOf(claim.publishedAt ?? claim.verifiedAt)} · v{claim.version}</span>
      </p>
      {claim.qualifier && <p className="text-sm text-emerald-200/85">{claim.qualifier}</p>}
      <section aria-label="The receipts">
        <h2 className="text-sm font-medium text-white/80 mb-1">The receipts</h2>
        {claim.note && <p className="text-sm text-white/75">{claim.note}</p>}
        <ol className="mt-2 list-decimal pl-5 text-xs text-white/60 space-y-0.5">
          {claim.sources.map(s => (
            <li key={s.id}>{isAllowlistedSourceUrl(s.url)
              ? <a href={s.url} target="_blank" rel="noopener noreferrer" className="text-cyan-300 underline underline-offset-4">{s.title}</a>
              : s.title}</li>
          ))}
        </ol>
      </section>
      <section aria-label="What we don't know">
        <h2 className="text-sm font-medium text-white/80 mb-1">What we don&rsquo;t know</h2>
        {gaps.length > 0 ? <ul className="list-disc pl-5 text-sm text-white/70">{gaps.map(g => <li key={g}>{g}</li>)}</ul>
          : <p className="text-sm text-white/55">The record lists no open questions yet.</p>}
      </section>
      <section aria-label="Risks on record">
        <h2 className="text-sm font-medium text-white/80 mb-1">Risks on record</h2>
        {risks.length > 0 ? <ul className="list-disc pl-5 text-sm text-white/70">{risks.map(r => <li key={r}>{r}</li>)}</ul>
          : <p className="text-sm text-white/55">No risks are listed in the record. That is not the same as none.</p>}
      </section>
      <nav className="flex flex-wrap gap-4 text-sm">
        <Link href={`/knowledge/${compound}/claims/${claim.id}`} className="text-cyan-300 underline underline-offset-4">Claim page</Link>
        <a href={cardImageUrl('claim', claim.id, claim.version, 'story')} download className="text-cyan-300 underline underline-offset-4">Story card</a>
        <Link href={`/knowledge/${compound}/changelog`} className="text-cyan-300 underline underline-offset-4">Changelog</Link>
      </nav>
      <p className="text-xs text-white/50">BioStack grades evidence. It doesn&rsquo;t tell you what to take.</p>
    </main>
  );
}
