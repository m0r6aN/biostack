import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { VsLedger } from '@/components/vs/VsLedger';
import { VsShare } from '@/components/vs/VsShare';
import { WatchButton } from '@/components/alerts/WatchButton';
import { fetchKnowledgeEntry } from '@/lib/knowledgeServer';
import { createPublicPageMetadata } from '@/lib/site';
import { isCuratedPair, rulingFor, vsAsOf, vsPath, vsSide } from '@/lib/vsPairs';
import { PageMain } from '@/components/ui/PageMain';

export const revalidate = 3600;

interface PageProps { params: Promise<{ a: string; b: string }> }

async function load(a: string, b: string) {
  const [ea, eb] = await Promise.all([fetchKnowledgeEntry(a), fetchKnowledgeEntry(b)]);
  if (!ea || !eb) return null;
  const left = vsSide(ea);
  const right = vsSide(eb);
  return { ea, eb, left, right, ruling: rulingFor(left, right), curated: isCuratedPair(a, b) };
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { a, b } = await params;
  const data = await load(a, b);
  if (!data) return { title: 'Head to head | BioStack', robots: { index: false, follow: true } };
  const { ea, eb, left, right, curated } = data;
  const path = vsPath(a, b);
  const version = [left.claim?.version ?? 0, right.claim?.version ?? 0].join('-');
  const meta = createPublicPageMetadata({
    title: `${ea.canonicalName} vs ${eb.canonicalName}: the evidence ledger | BioStack`,
    description: `${ea.canonicalName} and ${eb.canonicalName}, side by side: verdicts, evidence grade, regulatory status and Hype Gap. Evidence only.`,
    path,
    image: { url: `/og${path}?v=${version}`, width: 1200, height: 630, alt: `BioStack evidence ledger: ${ea.canonicalName} vs ${eb.canonicalName}` },
  });
  // Only curated pairs are indexable (03 §4).
  return curated ? meta : { ...meta, robots: { index: false, follow: true } };
}

export default async function VsPage({ params }: PageProps) {
  const { a, b } = await params;
  const data = await load(a, b);
  if (!data) notFound();
  const { ea, eb, left, right, ruling, curated } = data;
  const asOf = vsAsOf(left, right);
  return (
    <PageMain className="max-w-3xl mx-auto px-4 py-10 space-y-6">
      <h1 className="text-2xl font-semibold text-white">{ea.canonicalName} vs {eb.canonicalName}</h1>
      <VsLedger a={ea} b={eb} left={left} right={right} ruling={ruling} curated={curated} />
      <VsShare a={ea.canonicalName} b={eb.canonicalName} path={vsPath(a, b)} />
      <WatchButton targetType="vs" targetId={`${a}/${b}`} returnTo={vsPath(a, b)} />
      {asOf && <p className="text-xs text-white/50">Evidence as of {asOf}</p>}
      <p className="text-xs text-white/50">BioStack grades evidence. It doesn&rsquo;t tell you what to take.</p>
    </PageMain>
  );
}
