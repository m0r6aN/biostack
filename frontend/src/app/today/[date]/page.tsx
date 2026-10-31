import type { Metadata } from 'next';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { ClaimRow } from '@/components/knowledge/PublicClaimsPanel';
import { fetchAllEntries, publishedLibrary } from '@/lib/claimLibrary';
import { todaysClaim } from '@/lib/editorialCalendar';
import { cardImageUrl } from '@/lib/verdictCard';

export const revalidate = 3600;

interface PageProps { params: Promise<{ date: string }> }

async function load(date: string) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(date)) return undefined;
  return todaysClaim(publishedLibrary(await fetchAllEntries()), date);
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { date } = await params;
  const hit = await load(date);
  if (!hit) return { title: `Today's claim | BioStack`, robots: { index: false, follow: true } };
  return {
    title: `Today's claim, ${date}: “${hit.claim.statement}” | BioStack`,
    // The claim page is canonical; the daily permalink just points at it.
    alternates: { canonical: `/knowledge/${hit.slug}/claims/${hit.claim.id}` },
    openGraph: { images: [{ url: cardImageUrl('claim', hit.claim.id, hit.claim.version), width: 1200, height: 630 }] },
  };
}

export default async function TodayPage({ params }: PageProps) {
  const { date } = await params;
  const hit = await load(date);
  if (hit === undefined) notFound();
  return (
    <main className="max-w-3xl mx-auto px-4 py-10 space-y-4">
      <p className="text-xs uppercase tracking-[0.2em] text-white/50">Today&rsquo;s claim · {date}</p>
      {hit ? (
        <>
          <h1 className="text-2xl font-semibold text-white">{hit.entry.canonicalName}</h1>
          <ul><ClaimRow claim={hit.claim} href={`/knowledge/${hit.slug}/claims/${hit.claim.id}`} /></ul>
          <Link href={`/knowledge/${hit.slug}`} className="text-sm text-cyan-300 underline underline-offset-4">Full {hit.entry.canonicalName} record</Link>
        </>
      ) : (
        <p className="text-sm text-white/60">No graded claims yet. Verdicts appear here once the evidence council publishes them.</p>
      )}
    </main>
  );
}
