'use client';

import Link from 'next/link';
import { Protocol, ProtocolReview } from '@/lib/types';

interface ProtocolContinuityStripProps {
  protocol: Protocol;
  review: ProtocolReview | null;
}

export function ProtocolContinuityStrip({ protocol, review }: ProtocolContinuityStripProps) {
  const prior = protocol.priorVersions[0] ?? null;
  const latestSection = review?.sections.find((section) => section.type !== 'gap') ?? review?.sections[0] ?? null;
  const latestRun = review?.versions
    .flatMap((version) => version.runs)
    .sort((a, b) => new Date(b.run.startedAtUtc).getTime() - new Date(a.run.startedAtUtc).getTime())[0]?.run;
  const run = protocol.activeRun ?? protocol.actualComparison?.run ?? latestRun ?? null;
  const changes = protocol.versionDiff?.changes ?? [];

  return (
    <section className="rounded-lg border border-emerald-400/15 bg-[#0f171f] p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-200/55">Continuity</p>
          <h2 className="mt-1 text-lg font-bold text-white">
            v{protocol.version} {protocol.isDraft ? 'draft' : 'snapshot'}
            {prior ? ` follows v${prior.version}` : ' starts this lineage'}
          </h2>
        </div>
        <div className="flex flex-wrap gap-2">
          <Link href="#simulation" className="rounded-lg border border-white/[0.1] px-3 py-1.5 text-sm text-white/65 hover:border-white/25">Simulate</Link>
          <Link href="#run" className="rounded-lg border border-white/[0.1] px-3 py-1.5 text-sm text-white/65 hover:border-white/25">Run</Link>
          <Link href="#comparison" className="rounded-lg border border-white/[0.1] px-3 py-1.5 text-sm text-white/65 hover:border-white/25">Compare</Link>
          <Link href="#review" className="rounded-lg border border-white/[0.1] px-3 py-1.5 text-sm text-white/65 hover:border-white/25">Review</Link>
        </div>
      </div>

      <div className="mt-4 grid gap-2 md:grid-cols-5">
        <ContinuityCell label="Current" value={`v${protocol.version}`} detail={protocol.isCurrentVersion ? 'Current version' : 'Prior version'} />
        <ContinuityCell
          label="Parent"
          value={prior ? `v${prior.version}` : 'None'}
          detail={prior ? prior.name : 'Root snapshot'}
          href={prior ? `/protocols/${prior.id}` : undefined}
        />
        <ContinuityCell
          label="Run"
          value={run ? run.status : 'No run'}
          detail={run ? `${run.protocolName} v${run.protocolVersion}` : 'Track this protocol to observe.'}
        />
        <ContinuityCell
          label="Review"
          value={latestSection?.type ?? 'Pending'}
          detail={latestSection?.summary ?? 'Review available after multiple runs.'}
        />
        <ContinuityCell
          label="Changed"
          value={changes.length > 0 ? `${changes.length} item${changes.length === 1 ? '' : 's'}` : 'No diff'}
          detail={changes[0] ? `${changes[0].changeType} ${changes[0].subject}` : 'No deterministic change detected.'}
        />
      </div>

      {protocol.evolvedFromRunId && (
        <p className="mt-3 rounded-lg border border-sky-400/15 bg-sky-500/[0.06] px-3 py-2 text-sm text-sky-100/80">
          This draft was evolved from an observed run.
        </p>
      )}
    </section>
  );
}

function ContinuityCell({
  label,
  value,
  detail,
  href,
}: {
  label: string;
  value: string;
  detail: string;
  href?: string;
}) {
  const content = (
    <>
      <p className="text-[11px] font-semibold uppercase tracking-[0.12em] text-white/35">{label}</p>
      <p className="mt-2 font-bold text-white">{value}</p>
      <p className="mt-1 line-clamp-2 text-xs leading-5 text-white/45">{detail}</p>
    </>
  );

  if (href) {
    return (
      <Link href={href} className="min-h-28 rounded-lg border border-white/[0.07] bg-white/[0.025] p-3 hover:border-emerald-400/25">
        {content}
      </Link>
    );
  }

  return <div className="min-h-28 rounded-lg border border-white/[0.07] bg-white/[0.025] p-3">{content}</div>;
}
