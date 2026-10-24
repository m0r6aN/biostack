'use client';

import type { KnowledgeDoseReport, KnowledgeEntryDetails, KnowledgeSafetyItem } from '@/lib/types';

/**
 * Full record detail for one compound: known vs unknown, dangers, doses found on both
 * sides (studies vs online reports) each with source and grade, claims, gaps and titled
 * sources. BioStack shows what sources say; it never prescribes. A side with no rows
 * renders as "n/a".
 */

export const DOSE_FRAMING = 'Doses as found in each source, with its grade. What was found, not a direction.';
export const NOT_AVAILABLE = 'n/a';

const REPORTED_SOURCE_LABELS: Record<string, string> = {
  reddit: 'Reddit',
  facebook: 'Facebook',
  instagram: 'Instagram',
  forum: 'Forum',
  blog: 'Blog',
};

function safeHref(value: string): string | null {
  try {
    const url = new URL(value);
    if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password) return null;
    return url.href;
  } catch {
    return null;
  }
}

function SourceLink({ title, url }: { title: string; url: string }) {
  const href = safeHref(url);
  if (!href) return <span>{title || url}</span>;
  return (
    <a href={href} target="_blank" rel="noopener noreferrer"
      className="text-cyan-300 underline underline-offset-4 [overflow-wrap:anywhere] focus-visible:outline-2 focus-visible:outline-cyan-300">
      {title || href}<span className="sr-only"> (opens in a new tab)</span>
    </a>
  );
}

function Heading({ children }: { children: React.ReactNode }) {
  return <h4 className="text-sm font-medium text-white/80 mb-2">{children}</h4>;
}

function formatDose(d: KnowledgeDoseReport): string {
  const amount = d.amount === null || d.amount === undefined ? '' : `${d.amount} ${d.unit}`.trim();
  return [amount, d.frequency, d.route, d.duration].filter(Boolean).join(' · ') || NOT_AVAILABLE;
}

function DoseColumn({ title, rows, testId }: { title: string; rows: KnowledgeDoseReport[]; testId: string }) {
  return (
    <div data-testid={testId} className="rounded-xl border border-white/10 bg-white/[0.02] p-3">
      <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-2">{title}</p>
      {rows.length === 0 ? (
        <p className="text-sm text-white/50">{NOT_AVAILABLE}</p>
      ) : (
        <ul className="space-y-3">
          {rows.map((d, i) => (
            <li key={i} className="text-sm text-white/70 space-y-0.5">
              <p className="font-medium text-white/85">{formatDose(d)}</p>
              {d.population && <p className="text-xs text-white/50">{d.population}</p>}
              {d.outcome && <p className="text-xs text-white/60">{d.outcome}</p>}
              <p className="text-xs text-white/50">
                <span className="mr-2 rounded border border-white/15 px-1.5 py-0.5">Grade: {d.grade || 'Not graded'}</span>
                {d.side === 'reported' && REPORTED_SOURCE_LABELS[d.sourceType] && (
                  <span className="mr-2">{REPORTED_SOURCE_LABELS[d.sourceType]}</span>
                )}
                <SourceLink title={d.sourceTitle} url={d.sourceUrl} />
              </p>
              {d.notes && <p className="text-xs text-white/45">{d.notes}</p>}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function SafetyList({ label, items }: { label: string; items: KnowledgeSafetyItem[] }) {
  if (items.length === 0) return null;
  return (
    <div>
      <p className="text-[10px] uppercase tracking-wider text-rose-400/70 mb-1">{label}</p>
      <ul className="space-y-1.5">
        {items.map((s, i) => (
          <li key={i} className="text-sm text-rose-100/90">
            {s.item}{s.severity && s.severity !== 'unknown' ? <span className="text-xs text-rose-300/70"> ({s.severity})</span> : null}
            {s.notes && <span className="block text-xs text-white/55">{s.notes}</span>}
          </li>
        ))}
      </ul>
    </div>
  );
}

export function knownAndUnknown(details: KnowledgeEntryDetails): { known: string[]; unknown: string[] } {
  const known = details.claims
    .filter(c => ['strong', 'moderate', 'established'].includes(c.tier.toLowerCase()))
    .map(c => c.claim);
  const unknown = [...details.evidenceGaps];
  return { known, unknown };
}

export function CompoundRecordDetails({ details }: { details: KnowledgeEntryDetails }) {
  const studyDoses = details.doseReports.filter(d => d.side !== 'reported');
  const reportedDoses = details.doseReports.filter(d => d.side === 'reported');
  const { known, unknown } = knownAndUnknown(details);
  const s = details.safety;
  const hasDangers = s.contraindications.length + s.warnings.length + s.precautions.length
    + s.adverseEffectsCommon.length + s.adverseEffectsSerious.length > 0;
  const unofficial = details.unofficialSourceFields ?? [];

  return (
    <div className="space-y-4" data-testid="compound-record-details">
      <section aria-label="Known and unknown" className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div className="rounded-xl border border-emerald-400/15 bg-emerald-500/[0.04] p-3">
          <p className="text-xs uppercase tracking-[0.15em] text-emerald-300/70 mb-2">Known</p>
          {known.length === 0 ? <p className="text-sm text-white/50">{NOT_AVAILABLE}</p> : (
            <ul className="list-disc pl-4 space-y-1 text-sm text-white/70">{known.map((k, i) => <li key={i}>{k}</li>)}</ul>
          )}
        </div>
        <div className="rounded-xl border border-amber-400/15 bg-amber-500/[0.04] p-3">
          <p className="text-xs uppercase tracking-[0.15em] text-amber-300/70 mb-2">Unknown</p>
          {unknown.length === 0 ? <p className="text-sm text-white/50">{NOT_AVAILABLE}</p> : (
            <ul className="list-disc pl-4 space-y-1 text-sm text-white/70">{unknown.map((k, i) => <li key={i}>{k}</li>)}</ul>
          )}
        </div>
      </section>

      {details.studiedUses.length > 0 && (
        <section aria-label="Studied uses">
          <Heading>Studied uses</Heading>
          <ul className="space-y-2">
            {details.studiedUses.map((u, i) => (
              <li key={i} className="text-sm text-white/70">
                <span className="font-medium text-white/80">{u.useCase}</span>
                <span className="ml-2 text-xs text-white/45">{u.labelStatus.replace(/-/g, ' ')}</span>
                {u.summary && <span className="block">{u.summary}</span>}
              </li>
            ))}
          </ul>
        </section>
      )}

      {hasDangers && (
        <section aria-label="Dangers" className="rounded-xl border border-rose-500/20 bg-rose-500/[0.05] p-4 space-y-3">
          <Heading>Dangers and side effects</Heading>
          {unofficial.some(f => ['contraindications', 'warnings', 'monitoring'].includes(f)) && (
            <p className="text-xs text-amber-300/80">Unofficial source: no regulator or label source backs these items.</p>
          )}
          <SafetyList label="Contraindications" items={s.contraindications} />
          <SafetyList label="Warnings" items={s.warnings} />
          <SafetyList label="Precautions" items={s.precautions} />
          {s.adverseEffectsCommon.length > 0 && (
            <p className="text-sm text-white/70"><span className="text-xs uppercase tracking-wider text-white/45">Common: </span>{s.adverseEffectsCommon.join(', ')}</p>
          )}
          {s.adverseEffectsSerious.length > 0 && (
            <ul className="text-sm text-white/70 space-y-1">
              <li className="text-xs uppercase tracking-wider text-white/45">Serious</li>
              {s.adverseEffectsSerious.map((x, i) => <li key={i}>{x}</li>)}
            </ul>
          )}
        </section>
      )}

      <section aria-label="Doses found" className="space-y-2">
        <Heading>Doses found</Heading>
        <p className="text-xs text-white/50">{DOSE_FRAMING}</p>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <DoseColumn testId="doses-studies" title="Studies, trials and professional sources" rows={studyDoses} />
          <DoseColumn testId="doses-reported" title="Reddit, Facebook, Instagram, forums and blogs" rows={reportedDoses} />
        </div>
      </section>

      {details.claims.length > 0 && (
        <section aria-label="Claims and grades">
          <Heading>Claims and grades</Heading>
          <ul className="space-y-2">
            {details.claims.map((c, i) => (
              <li key={i} className="text-sm text-white/70">
                <span className="mr-2 rounded border border-white/15 px-1.5 py-0.5 text-xs text-white/60">{c.tier || 'Not graded'}</span>
                {c.claim}
              </li>
            ))}
          </ul>
        </section>
      )}

      {details.controversies.length > 0 && (
        <section aria-label="Contested">
          <Heading>Contested</Heading>
          <ul className="list-disc pl-4 space-y-1 text-sm text-white/70">{details.controversies.map((c, i) => <li key={i}>{c}</li>)}</ul>
        </section>
      )}

      {details.sources.length > 0 && (
        <section aria-label="Record sources">
          <Heading>Sources ({details.sources.length})</Heading>
          <ul className="space-y-1.5 text-sm text-white/65">
            {details.sources.map((src, i) => (
              <li key={i} className="[overflow-wrap:anywhere]">
                <SourceLink title={src.title} url={src.url} />
                <span className="text-xs text-white/40"> · {[src.publisher, src.sourceType, src.publishedAt?.slice(0, 10)].filter(Boolean).join(' · ')}</span>
              </li>
            ))}
          </ul>
        </section>
      )}
    </div>
  );
}
