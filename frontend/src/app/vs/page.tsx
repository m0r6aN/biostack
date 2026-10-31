import Link from 'next/link';
import { createPublicPageMetadata } from '@/lib/site';
import { CURATED_PAIRS, vsPath } from '@/lib/vsPairs';
import { fetchAllEntries } from '@/lib/claimLibrary';
import { compoundSlug } from '@/lib/compoundSlug';
import { PageMain } from '@/components/ui/PageMain';

export const revalidate = 3600;

export const metadata = createPublicPageMetadata({
  title: 'Head to head: evidence ledgers | BioStack',
  description: 'Two compounds, one evidence ledger. Verdicts, evidence grade and Hype Gap side by side. Evidence only.',
  path: '/vs',
});

export default async function VsIndex() {
  const names = new Map((await fetchAllEntries()).map(e => [compoundSlug(e.canonicalName), e.canonicalName]));
  const label = (slug: string) => names.get(slug) ?? slug;
  return (
    <PageMain className="max-w-3xl mx-auto px-4 py-10 space-y-4">
      <h1 className="text-2xl font-semibold text-white">Head to head</h1>
      <p className="text-sm text-white/60">Evidence volume and quality, side by side. Never which one to take.</p>
      <ul className="space-y-2">
        {CURATED_PAIRS.map(([a, b]) => (
          <li key={`${a}-${b}`}><Link href={vsPath(a, b)} className="text-cyan-300 underline underline-offset-4">{label(a)} vs {label(b)}</Link></li>
        ))}
      </ul>
    </PageMain>
  );
}
