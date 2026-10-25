import type { Metadata } from 'next';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { VerdictChip } from '@/components/knowledge/PublicClaimsPanel';
import { formatAsOf } from '@/lib/claimVerdicts';
import { claimLibrary, fetchAllEntries } from '@/lib/claimLibrary';
import { publishedMyths, receiptsForMonth } from '@/lib/editorialCalendar';
import { publishedLibrary } from '@/lib/claimLibrary';

export const revalidate = 3600;

interface PageProps { params: Promise<{ month: string }> }

function monthLabel(ym: string) {
  const [y, m] = ym.split('-').map(Number);
  return new Date(Date.UTC(y, m - 1, 15)).toLocaleDateString('en-US', { month: 'long', year: 'numeric', timeZone: 'UTC' });
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { month } = await params;
  return { title: `Receipts drop: ${/^\d{4}-\d{2}$/.test(month) ? monthLabel(month) : month} | BioStack` };
}

/** Web version of the monthly Receipts drop (03 §5): every published verdict change + new myths. */
export default async function ReceiptsDropPage({ params }: PageProps) {
  const { month } = await params;
  if (!/^\d{4}-(0[1-9]|1[0-2])$/.test(month)) notFound();
  const entries = await fetchAllEntries();
  const changes = receiptsForMonth(claimLibrary(entries), month);
  const myths = publishedMyths(publishedLibrary(entries)).filter(m => m.slot.publishAt.startsWith(month));
  return (
    <main className="max-w-3xl mx-auto px-4 py-10 space-y-6">
      <h1 className="text-2xl font-semibold text-white">Receipts drop: {monthLabel(month)}</h1>
      <section aria-label="Verdict changes">
        <h2 className="text-sm font-medium text-white/80 mb-2">Verdicts that moved</h2>
        {changes.length === 0 ? <p className="text-sm text-white/55">No verdict changes this month.</p> : (
          <ul className="space-y-2 text-sm text-white/75">
            {changes.map(c => (
              <li key={`${c.claim.id}-${c.change.version}`}>
                <Link href={`/knowledge/${c.slug}/claims/${c.claim.id}`} className="underline underline-offset-4">{c.entry.canonicalName}: &ldquo;{c.claim.statement}&rdquo;</Link>{' '}
                <VerdictChip verdict={c.change.verdict} /> · v{c.change.version} · {formatAsOf(c.change.publishedAt)} · {c.change.reason}
              </li>
            ))}
          </ul>
        )}
      </section>
      {myths.length > 0 && (
        <section aria-label="Myths">
          <h2 className="text-sm font-medium text-white/80 mb-2">Myths this month</h2>
          <ul className="space-y-1 text-sm">{myths.map(m => <li key={m.slot.slug}><Link href={`/myths/${m.slot.slug}`} className="text-cyan-300 underline underline-offset-4">&ldquo;{m.hit.claim.statement}&rdquo;</Link></li>)}</ul>
        </section>
      )}
      <p className="text-xs text-white/40">BioStack grades evidence. It doesn&rsquo;t tell you what to take.</p>
    </main>
  );
}
