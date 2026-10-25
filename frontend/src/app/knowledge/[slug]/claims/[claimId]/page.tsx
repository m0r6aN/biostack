import type { Metadata } from 'next';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { ClaimRow } from '@/components/knowledge/PublicClaimsPanel';
import { VERDICT_FOOTNOTE } from '@/components/knowledge/VerdictBlock';
import { isPublished } from '@/lib/claimVerdicts';
import { compoundSlug } from '@/lib/compoundSlug';
import { fetchKnowledgeEntry } from '@/lib/knowledgeServer';
import { createPublicPageMetadata } from '@/lib/site';
import { buildCardModel, cardImageUrl } from '@/lib/verdictCard';
import type { KnowledgeEntry, KnowledgePublicClaim } from '@/lib/types';

export const revalidate = 3600;

interface PageProps {
  params: Promise<{ slug: string; claimId: string }>;
}

async function load(slug: string, claimId: string): Promise<{ entry: KnowledgeEntry; claim: KnowledgePublicClaim } | null> {
  const entry = await fetchKnowledgeEntry(slug);
  const claim = entry?.details?.publicClaims?.find(c => c.id === claimId);
  return entry && claim ? { entry, claim } : null;
}

function alt(entry: KnowledgeEntry, claim: KnowledgePublicClaim): string {
  try {
    return buildCardModel({ kind: 'claim', entry, claim }).alt;
  } catch {
    return `BioStack verdict card for ${entry.canonicalName}`;
  }
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug, claimId } = await params;
  const hit = await load(slug, claimId);
  if (!hit) return { title: 'Claim | BioStack', robots: { index: false, follow: true } };
  const { entry, claim } = hit;
  const published = isPublished(claim);
  const verdict = published ? claim.verdict : 'NOT YET GRADED';
  const meta = createPublicPageMetadata({
    title: `“${claim.statement}”: ${verdict} | BioStack`,
    description: published && claim.note ? claim.note : `What the evidence says about this ${entry.canonicalName} claim, with sources.`,
    path: `/knowledge/${compoundSlug(entry.canonicalName)}/claims/${claim.id}`,
    image: { url: cardImageUrl('claim', claim.id, claim.version), width: 1200, height: 630, alt: alt(entry, claim) },
  });
  // Ungraded claim pages are thin until the council publishes a verdict.
  return published ? meta : { ...meta, robots: { index: false, follow: true } };
}

export default async function ClaimPage({ params }: PageProps) {
  const { slug, claimId } = await params;
  const hit = await load(slug, claimId);
  if (!hit) notFound();
  const { entry, claim } = hit;
  const compoundHref = `/knowledge/${compoundSlug(entry.canonicalName)}`;
  return (
    <main className="max-w-3xl mx-auto px-4 py-10 space-y-6">
      <p className="text-xs uppercase tracking-[0.2em] text-white/45">The claim</p>
      <h1 className="text-2xl font-semibold text-white">{entry.canonicalName}: &ldquo;{claim.statement}&rdquo;</h1>
      <ul><ClaimRow claim={claim} /></ul>
      <p className="text-xs text-white/45">{VERDICT_FOOTNOTE}</p>
      <nav className="flex flex-wrap gap-4 text-sm">
        <Link href={compoundHref} className="text-cyan-300 underline underline-offset-4">Full {entry.canonicalName} record</Link>
        <Link href={`${compoundHref}/changelog`} className="text-cyan-300 underline underline-offset-4">Changelog</Link>
        <a href={cardImageUrl('claim', claim.id, claim.version, 'story')} download className="text-cyan-300 underline underline-offset-4">Story card</a>
      </nav>
      <p className="text-xs text-white/40">BioStack grades evidence. It doesn&rsquo;t tell you what to take.</p>
    </main>
  );
}
