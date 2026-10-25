import type { Metadata } from 'next';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { WatchButton } from '@/components/alerts/WatchButton';
import { formatAsOf, isPublished } from '@/lib/claimVerdicts';
import { compoundSlug } from '@/lib/compoundSlug';
import { changelogRows } from '@/lib/changelog';
import { fetchKnowledgeEntry } from '@/lib/knowledgeServer';
import { createPublicPageMetadata } from '@/lib/site';

export const revalidate = 3600;

interface PageProps { params: Promise<{ slug: string }> }

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const entry = await fetchKnowledgeEntry(slug);
  if (!entry) return { title: 'Changelog | BioStack', robots: { index: false, follow: true } };
  return createPublicPageMetadata({
    title: `${entry.canonicalName} verdict changelog | BioStack`,
    description: `Every published verdict change on ${entry.canonicalName} claims, with the reason and date.`,
    path: `/knowledge/${compoundSlug(entry.canonicalName)}/changelog`,
  });
}

export default async function ChangelogPage({ params }: PageProps) {
  const { slug } = await params;
  const entry = await fetchKnowledgeEntry(slug);
  if (!entry) notFound();
  const base = `/knowledge/${compoundSlug(entry.canonicalName)}`;
  const rows = changelogRows(entry);
  const anyPublished = (entry.details?.publicClaims ?? []).some(isPublished);
  return (
    <main className="max-w-3xl mx-auto px-4 py-10 space-y-6">
      <p className="text-xs uppercase tracking-[0.2em] text-white/45">Changelog</p>
      <h1 className="text-2xl font-semibold text-white">{entry.canonicalName}: what changed and why</h1>
      <div className="flex flex-wrap items-center gap-4">
        <WatchButton targetType="compound" targetId={compoundSlug(entry.canonicalName)} returnTo={`${base}/changelog`} />
        <Link href={base} className="text-sm text-cyan-300 underline underline-offset-4">Full {entry.canonicalName} record</Link>
      </div>
      {rows.length === 0 ? (
        <p data-testid="changelog-empty" className="text-white/60">
          {anyPublished ? 'First verdict. Nothing has moved it yet.' : 'No published verdicts on this record yet.'}
        </p>
      ) : (
        <ol className="space-y-4" data-testid="changelog">
          {rows.map(r => (
            <li key={`${r.claimId}-${r.version}`} className="rounded-xl border border-white/10 bg-white/[0.02] p-4 space-y-1">
              <p className="text-xs text-white/50">{formatAsOf(r.publishedAt)} · v{r.version}</p>
              <p className="text-white">
                <Link href={`${base}/claims/${r.claimId}`} className="underline underline-offset-4">&ldquo;{r.statement}&rdquo;</Link>: <strong>{r.verdict}</strong>
              </p>
              {r.reason && <p className="text-sm text-white/70">{r.reason}</p>}
              {r.reviewers.length > 0 && <p className="text-xs text-white/45">Reviewed by {r.reviewers.join(', ')}</p>}
            </li>
          ))}
        </ol>
      )}
      <p className="text-xs text-white/40">BioStack grades evidence. It doesn&rsquo;t tell you what to take.</p>
    </main>
  );
}
