import type { Metadata } from 'next';
import { SubmitClaimForm } from '@/components/check/SubmitClaimForm';
import { fetchAllEntries } from '@/lib/claimLibrary';
import { compoundSlug } from '@/lib/compoundSlug';
import { createPublicPageMetadata } from '@/lib/site';

export const revalidate = 3600;

export const metadata: Metadata = createPublicPageMetadata({
  title: 'Submit a claim you saw | BioStack',
  description: 'Saw a peptide claim online? Send it in and we\u2019ll check the receipts.',
  path: '/submit',
});

interface PageProps { searchParams: Promise<{ claim?: string; compound?: string }> }

export default async function SubmitPage({ searchParams }: PageProps) {
  const { claim = '', compound = '' } = await searchParams;
  const entries = await fetchAllEntries();
  const compounds = entries.map(e => ({ slug: compoundSlug(e.canonicalName), name: e.canonicalName })).sort((a, b) => a.name.localeCompare(b.name));
  return (
    <main className="max-w-2xl mx-auto px-4 py-10 space-y-6">
      <h1 className="text-2xl font-semibold text-white">Submit a claim you saw</h1>
      <p className="text-white/70">Tell us what you read or heard. An editor checks it against the evidence. We don&rsquo;t publish individual submissions.</p>
      <SubmitClaimForm compounds={compounds} initialClaim={claim.slice(0, 280)} initialCompound={compound} />
    </main>
  );
}
