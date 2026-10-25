import Link from 'next/link';
import { VerdictChip } from '@/components/knowledge/PublicClaimsPanel';
import { tierLabel } from '@/lib/evidenceTiers';
import { redactDoseText } from '@/lib/doseRedaction';
import { compoundSlug } from '@/lib/compoundSlug';
import { sharedClaims, sharedPathways, type VsSide } from '@/lib/vsPairs';
import type { KnowledgeEntry } from '@/lib/types';

export const NOT_CURATED_COPY = "We haven't put these two head to head yet. Here's each one's record on its own.";
export const OVERLAP_FRAMING = 'Shared pathways. Not a stacking guide.';
export const NO_RULING_COPY = 'No ruling yet. A ruling needs a published verdict on both records.';

function regulatory(entry: KnowledgeEntry): string {
  const d = entry.details;
  const status = entry.regulatoryStatus?.trim() || (d?.requiresPrescription ? 'Prescription only' : '');
  const when = d?.lastReviewedAt ? ` (as of ${new Date(d.lastReviewedAt).toISOString().slice(0, 10)})` : '';
  return status ? redactDoseText(`${status}${when}`) : 'n/a';
}

function adverse(entry: KnowledgeEntry): string {
  const s = entry.details?.safety;
  if (!s) return 'n/a';
  const serious = s.adverseEffectsSerious.length;
  const common = s.adverseEffectsCommon.length;
  return serious + common === 0 ? 'None listed in the record' : `${serious} serious, ${common} common listed`;
}

export function VsLedger({ a, b, left, right, ruling, curated }: {
  a: KnowledgeEntry; b: KnowledgeEntry; left: VsSide; right: VsSide; ruling: string | null; curated: boolean;
}) {
  const rows: Array<[string, React.ReactNode, React.ReactNode]> = [
    ['Headline verdict', <VerdictChip key="l" verdict={left.verdict} />, <VerdictChip key="r" verdict={right.verdict} />],
    ['Headline claim', left.claim ? `“${left.claim.statement}”` : 'n/a', right.claim ? `“${right.claim.statement}”` : 'n/a'],
    ['Evidence grade', tierLabel(a.evidenceTier), tierLabel(b.evidenceTier)],
    ['Regulatory status', regulatory(a), regulatory(b)],
    ['Hype Gap', left.hypeBand || 'n/a', right.hypeBand || 'n/a'],
    ['Adverse effects documented', adverse(a), adverse(b)],
  ];
  const shared = sharedClaims(a, b);
  const pathways = sharedPathways(a, b);
  const gapsA = (a.details?.evidenceGaps ?? []).map(redactDoseText);
  const gapsB = (b.details?.evidenceGaps ?? []).map(redactDoseText);

  return (
    <div className="space-y-6">
      <section aria-label="The ruling" className="rounded-2xl border border-white/12 bg-white/[0.05] p-5">
        <p className="text-[11px] uppercase tracking-[0.2em] text-white/45 mb-1">The ruling (evidence only)</p>
        <p className="text-lg text-white" data-testid="vs-ruling">{curated ? (ruling ?? NO_RULING_COPY) : NOT_CURATED_COPY}</p>
      </section>
      <section aria-label="Evidence ledger">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-white/50">
              <th className="py-2 pr-3 font-medium">Evidence ledger</th>
              <th className="py-2 pr-3 font-medium"><Link href={`/knowledge/${compoundSlug(a.canonicalName)}`} className="underline underline-offset-4">{a.canonicalName}</Link></th>
              <th className="py-2 font-medium"><Link href={`/knowledge/${compoundSlug(b.canonicalName)}`} className="underline underline-offset-4">{b.canonicalName}</Link></th>
            </tr>
          </thead>
          <tbody>
            {rows.map(([label, l, r]) => (
              <tr key={label} className="border-t border-white/10 align-top">
                <th scope="row" className="py-2 pr-3 text-left font-normal text-white/55">{label}</th>
                <td className="py-2 pr-3 text-white/80">{l}</td>
                <td className="py-2 text-white/80">{r}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
      {shared.length > 0 && (
        <section aria-label="Claim by claim">
          <h2 className="text-sm font-medium text-white/80 mb-2">Claim by claim</h2>
          <ul className="space-y-1 text-sm text-white/75">
            {shared.map(s => (
              <li key={s.outcome}>{s.outcome}: <VerdictChip verdict={s.left.verdict} /> · <VerdictChip verdict={s.right.verdict} /></li>
            ))}
          </ul>
        </section>
      )}
      {pathways.length > 0 && (
        <section aria-label="Overlap">
          <h2 className="text-sm font-medium text-white/80 mb-1">Overlap</h2>
          <p className="text-xs text-white/50 mb-2">{OVERLAP_FRAMING}</p>
          <p className="text-sm text-white/75">{pathways.join(' · ')}</p>
        </section>
      )}
      {(gapsA.length > 0 || gapsB.length > 0) && (
        <section aria-label="What's unknown">
          <h2 className="text-sm font-medium text-white/80 mb-2">What&rsquo;s unknown</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm text-white/70">
            <ul className="list-disc pl-4 space-y-1">{gapsA.map(g => <li key={g}>{g}</li>)}</ul>
            <ul className="list-disc pl-4 space-y-1">{gapsB.map(g => <li key={g}>{g}</li>)}</ul>
          </div>
        </section>
      )}
    </div>
  );
}
