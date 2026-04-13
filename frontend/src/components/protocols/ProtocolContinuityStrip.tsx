'use client';

import Link from 'next/link';
import { Protocol, ProtocolPatternSnapshot, ProtocolReview, ProtocolSequenceExpectationSnapshot, ProtocolDriftSnapshot } from '@/lib/types';

interface ProtocolContinuityStripProps {
  protocol: Protocol;
  review: ProtocolReview | null;
  patterns?: ProtocolPatternSnapshot | null;
  drift?: ProtocolDriftSnapshot | null;
  sequence?: ProtocolSequenceExpectationSnapshot | null;
}

export function ProtocolContinuityStrip({ protocol, review, patterns, drift, sequence }: ProtocolContinuityStripProps) {
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

      <div className="mt-4 grid gap-4 xl:grid-cols-3">
        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.12em] text-white/35">Lineage</p>
          <div className="mt-2 grid gap-2 sm:grid-cols-2">
        <ContinuityCell label="Current" value={`v${protocol.version}`} detail={protocol.isCurrentVersion ? 'Current version' : 'Prior version'} />
        <ContinuityCell
          label="Parent"
          value={prior ? `v${prior.version}` : 'None'}
          detail={prior ? prior.name : 'Root snapshot'}
          href={prior ? `/protocols/${prior.id}` : undefined}
        />
          </div>
        </div>
        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.12em] text-white/35">State</p>
          <div className="mt-2 grid gap-2 sm:grid-cols-2">
        <ContinuityCell
          label="Run"
          value={run ? run.status : 'No run'}
          detail={run ? `${run.protocolName} v${run.protocolVersion}` : 'Track this protocol to observe.'}
        />
        <ContinuityCell
          label="Review"
          value={latestSection?.type ?? 'Required'}
          detail={latestSection?.summary ?? 'Review available after multiple runs.'}
        />
          <ContinuityCell
            label="Regime"
            value={drift?.regimeClassification?.state ?? 'stable'}
            detail={drift?.signals[0]?.description ?? 'No drift signals detected.'}
          />
          </div>
        </div>
        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.12em] text-white/35">Intelligence</p>
          <div className="mt-2 grid gap-2 sm:grid-cols-2">
        <ContinuityCell
          label="Changed"
          value={changes.length > 0 ? `${changes.length} item${changes.length === 1 ? '' : 's'}` : 'No diff'}
          detail={changes[0] ? `${changes[0].changeType} ${changes[0].subject}` : 'No deterministic change detected.'}
        />
        <ContinuityCell
          label="Patterns"
          value={`${patterns?.historicalRunCount ?? 0} run${patterns?.historicalRunCount === 1 ? '' : 's'}`}
          detail={patternDetail(patterns)}
        />
        <ContinuityCell
          label="Sequence"
          value={sequence?.expectedNextEvent ? formatSequenceEvent(sequence.expectedNextEvent.eventType) : 'No pattern'}
          detail={sequence?.expectedNextEvent?.timingWindow ?? 'Sequence patterns will appear after multiple runs.'}
        />
        <ContinuityCell
          label="Deviation"
          value={sequence?.currentStatus?.state ?? patterns?.currentRunComparison?.similarity ?? 'unknown'}
          detail={sequence?.currentStatus?.notes[0] ?? patterns?.currentRunComparison?.divergentSignals[0] ?? 'Current state comparison pending.'}
        />
          </div>
        </div>
      </div>

      {protocol.evolvedFromRunId && (
        <p className="mt-3 rounded-lg border border-sky-400/15 bg-sky-500/[0.06] px-3 py-2 text-sm text-sky-100/80">
          This draft was evolved from an observed run.
        </p>
      )}
    </section>
  );
}

function formatSequenceEvent(value: string) {
  return value
    .replace('RunStarted', 'run start')
    .replace('FirstCheckIn', 'first check-in')
    .replace('ComputationRecorded', 'computation')
    .replace('RunClosed', 'run close')
    .replace('ReviewCompleted', 'review completion')
    .replace('EvolutionEvent', 'evolution event');
}

function patternDetail(patterns?: ProtocolPatternSnapshot | null) {
  if (!patterns || patterns.historicalRunCount < 2) {
    return 'Insufficient completed run history.';
  }

  const cadence = patterns.metricPatterns.find((pattern) => pattern.metric === 'Check-in cadence');
  const deviation = patterns.currentRunComparison?.divergentSignals[0] ?? patterns.currentRunComparison?.matchingSignals[0];
  return deviation ?? cadence?.observation ?? `${patterns.patternConfidence} confidence historical recall.`;
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
