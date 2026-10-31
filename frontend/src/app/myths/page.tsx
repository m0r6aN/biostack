import Link from 'next/link';
import { VerdictChip } from '@/components/knowledge/PublicClaimsPanel';
import { fetchAllEntries, publishedLibrary } from '@/lib/claimLibrary';
import { mythKicker, publishedMyths } from '@/lib/editorialCalendar';
import { createPublicPageMetadata } from '@/lib/site';

export const revalidate = 3600;

export const metadata = createPublicPageMetadata({
  title: 'Myth of the Week | BioStack',
  description: 'One claim, one ruling, and the receipts. A new one every Tuesday.',
  path: '/myths',
});

export default async function MythsIndex() {
  const myths = publishedMyths(publishedLibrary(await fetchAllEntries()));
  return (
    <main className="max-w-3xl mx-auto px-4 py-10 space-y-4">
      <h1 className="text-2xl font-semibold text-white">Myth of the Week</h1>
      <p className="text-sm text-white/60">One claim, one ruling, and the receipts. Published Tuesdays at 9:00 AM ET.</p>
      {myths.length === 0 ? (
        <p className="text-sm text-white/55" data-testid="myths-empty">No myth published yet.</p>
      ) : (
        <ul className="space-y-3">
          {myths.map(({ slot, hit }) => (
            <li key={slot.slug} className="text-sm text-white/80">
              <span className="text-[11px] uppercase tracking-[0.2em] text-white/50 mr-2">{mythKicker(hit.claim.verdict)}</span>
              <Link href={`/myths/${slot.slug}`} className="underline underline-offset-4">&ldquo;{hit.claim.statement}&rdquo;</Link>{' '}
              <VerdictChip verdict={hit.claim.verdict} />
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
