'use client';

import type {
    InteractionFinding,
    InteractionResult,
    Protocol,
    ProtocolDriftSnapshot,
    ProtocolItem,
    ProtocolPatternSnapshot,
    ProtocolReview,
    ProtocolRunObservation,
    ProtocolSequenceExpectationSnapshot,
} from '@/lib/types';
import { useState, type ReactNode } from 'react';

interface ProviderObservationalSummaryProps {
  protocol: Protocol;
  review?: ProtocolReview | null;
  patterns?: ProtocolPatternSnapshot | null;
  drift?: ProtocolDriftSnapshot | null;
  sequence?: ProtocolSequenceExpectationSnapshot | null;
  generatedAt?: Date;
}

const NOT_RECORDED = 'Not recorded in this saved protocol snapshot.';
const NO_ACTIVE_SUBSTANCES = 'No active substances recorded';
const NO_RECENT_CHECK_INS = 'No recent check-ins recorded';
const NO_OBSERVED_PATTERNS = 'No observed patterns available yet';
const NO_INTERACTION_NOTES = 'No interaction or overlap notes available';
const NO_EVIDENCE_CONTEXT = 'No evidence context available';
const SAFETY_BOUNDARY_LINES = [
  'For discussion with a qualified professional.',
  'Not medical advice.',
  'Does not recommend starting, stopping, combining, or dosing any substance.',
  'Observational and educational use only.',
];

export function ProviderObservationalSummary({
  protocol,
  review,
  patterns,
  drift,
  sequence,
  generatedAt,
}: ProviderObservationalSummaryProps) {
  const generatedDate = formatGeneratedAt(generatedAt ?? new Date());
  const activeItems = protocol.items.filter(isActiveItem);
  const goals = unique(protocol.items.map((item) => item.compound?.goal).filter(Boolean) as string[]);
  const observations = recentObservations(protocol.actualComparison?.observations ?? []);
  const trends = protocol.actualComparison?.actualTrends ?? [];
  const run = protocol.actualComparison?.run ?? protocol.activeRun;
  const findings = protocol.interactionIntelligence.topFindings.slice(0, 4);
  const interactions = protocol.interactionIntelligence.interactions
    .filter((interaction) => interaction.type !== 'Neutral')
    .slice(0, 4);
  const interactionNoteCount = findings.length + interactions.length;
  const patternNotes = collectPatternNotes(patterns, drift, sequence, review);
  const [copyState, setCopyState] = useState<'idle' | 'success' | 'error'>('idle');
  const summaryText = buildProviderSummaryText({
    protocol,
    activeItems,
    goals,
    observations,
    trends,
    run,
    findings,
    interactions,
    patternNotes,
    generatedDate,
  });

  const handleCopySummary = async () => {
    if (!navigator.clipboard?.writeText) {
      setCopyState('error');
      return;
    }

    try {
      await navigator.clipboard.writeText(summaryText);
      setCopyState('success');
    } catch {
      setCopyState('error');
    }
  };

  return (
    <section
      aria-labelledby="provider-summary-title"
      className="rounded-2xl border border-white/[0.08] bg-[#f8fafc] p-0 text-slate-950 shadow-2xl shadow-black/20 print:rounded-none print:border-slate-300 print:bg-white print:text-black print:shadow-none"
    >
      <div className="border-b border-slate-200 p-6 print:break-inside-avoid print:p-4">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.18em] text-slate-500 print:text-slate-700">Observational summary</p>
            <h2 id="provider-summary-title" className="mt-2 text-2xl font-black text-slate-950 print:text-xl print:text-black">
              Provider-ready observational summary
            </h2>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-600 print:text-xs print:leading-5 print:text-slate-700">
              A factual snapshot of what you are tracking, what changed, and what BioStack observed. Bring it to a qualified professional for review.
            </p>
          </div>
          <div className="text-left sm:text-right">
            <p className="text-xs uppercase tracking-[0.14em] text-slate-500 print:text-slate-700">Generated</p>
            <p className="mt-1 text-sm font-semibold text-slate-800 print:text-xs print:text-black">{generatedDate}</p>
            <div aria-label="Provider summary actions" className="mt-3 flex flex-wrap items-center gap-2 sm:justify-end print:hidden">
              <button
                type="button"
                onClick={handleCopySummary}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100 print:hidden"
              >
                Copy summary
              </button>
              <button
                type="button"
                onClick={() => window.print()}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100 print:hidden"
              >
                Print summary
              </button>
            </div>
            {copyState === 'success' && <p role="status" className="mt-2 text-xs font-semibold text-emerald-700 print:hidden">Summary copied.</p>}
            {copyState === 'error' && <p role="alert" className="mt-2 text-xs font-semibold text-amber-700 print:hidden">Copy failed. Select and copy the summary manually.</p>}
          </div>
        </div>
      </div>

      <div className="grid gap-0 divide-y divide-slate-200 print:block lg:grid-cols-2 lg:divide-x lg:divide-y-0">
        <SummarySection title="Stack overview">
          <Fact label="Protocol" value={protocol.name} />
          <Fact label="Version" value={`v${protocol.version}${protocol.isDraft ? ' draft' : ' snapshot'}`} />
          <Fact label="Snapshot state" value={protocol.isCurrentVersion ? 'Current version' : 'Prior version'} />
          <Fact label="Run state" value={run ? `${run.status} run` : 'No active run in this snapshot'} />
        </SummarySection>

        <SummarySection title="Goals and timeline">
          <Fact label="Stated goals" value={goals.length > 0 ? goals.join(', ') : NOT_RECORDED} />
          <Fact label="Protocol created" value={formatDate(protocol.createdAtUtc)} />
          <Fact label="Protocol updated" value={formatDate(protocol.updatedAtUtc)} />
          {run && <Fact label="Run window" value={`${formatDate(run.startedAtUtc)} — ${run.endedAtUtc ? formatDate(run.endedAtUtc) : 'ongoing'}`} />}
        </SummarySection>
      </div>

      <SummarySection title="Active substances" metadata={formatCount(activeItems.length, 'active substance')} fullWidth>
        {activeItems.length === 0 ? (
          <Placeholder>{NO_ACTIVE_SUBSTANCES}</Placeholder>
        ) : (
          <div className="grid gap-3 md:grid-cols-2">
            {activeItems.map((item) => (
              <SubstanceCard key={item.id} item={item} />
            ))}
          </div>
        )}
      </SummarySection>

      <div className="grid gap-0 divide-y divide-slate-200 border-t border-slate-200 print:block lg:grid-cols-2 lg:divide-x lg:divide-y-0">
        <SummarySection title="Recent check-ins" metadata={formatCount(observations.length, 'recent check-in')}>
          {observations.length === 0 ? (
            <Placeholder>{NO_RECENT_CHECK_INS}</Placeholder>
          ) : (
            <div className="space-y-3">
              {observations.map((observation) => (
                <CheckInRow key={observation.checkInId} observation={observation} />
              ))}
            </div>
          )}
        </SummarySection>

        <SummarySection title="Observed patterns">
          {trends.length === 0 && patternNotes.length === 0 ? (
            <Placeholder>{NO_OBSERVED_PATTERNS}</Placeholder>
          ) : (
            <div className="space-y-2">
              {trends.map((trend) => (
                <p key={`${trend.metric}-${trend.direction}`} className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700">
                  <span className="font-semibold text-slate-950">{trend.metric}</span>: {trend.direction} ({formatAverage(trend.beforeAverage)} before · {formatAverage(trend.afterAverage)} after)
                </p>
              ))}
              {patternNotes.map((note) => (
                <p key={note} className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700">{note}</p>
              ))}
            </div>
          )}
        </SummarySection>
      </div>

      <div className="grid gap-0 divide-y divide-slate-200 border-t border-slate-200 print:block lg:grid-cols-2 lg:divide-x lg:divide-y-0">
        <SummarySection title="Interaction and overlap notes" metadata={formatCount(interactionNoteCount, 'interaction/overlap note')}>
          {findings.length === 0 && interactions.length === 0 ? (
            <Placeholder>{NO_INTERACTION_NOTES}</Placeholder>
          ) : (
            <div className="space-y-3">
              {findings.map((finding) => <FindingRow key={`${finding.type}-${finding.compounds.join('-')}`} finding={finding} />)}
              {interactions.map((interaction) => <InteractionRow key={`${interaction.type}-${interaction.compoundA}-${interaction.compoundB}`} interaction={interaction} />)}
            </div>
          )}
        </SummarySection>

        <SummarySection title="Evidence context">
          <Fact label="Stack evidence score" value={`${protocol.stackScore.breakdown.evidence}`} />
          <Placeholder>{NO_EVIDENCE_CONTEXT}</Placeholder>
        </SummarySection>
      </div>

      <SummarySection title="Safety boundary" fullWidth>
        <div className="grid gap-3 md:grid-cols-2">
          {SAFETY_BOUNDARY_LINES.map((line) => <BoundaryText key={line}>{line}</BoundaryText>)}
        </div>
      </SummarySection>

      <footer className="hidden border-t border-slate-200 p-4 text-[10px] font-semibold uppercase tracking-[0.14em] text-slate-600 print:block">
        Generated by BioStack · {generatedDate} · Observational summary. Not medical advice.
      </footer>
    </section>
  );
}

function SummarySection({ title, metadata, fullWidth = false, children }: { title: string; metadata?: string; fullWidth?: boolean; children: ReactNode }) {
  return (
    <section className={`${fullWidth ? 'border-t border-slate-200' : ''} p-6 print:break-inside-avoid print:px-4 print:py-3`}>
      <h3 className="text-xs font-bold uppercase tracking-[0.16em] text-slate-500 print:text-[11px] print:text-slate-700">{title}</h3>
      {metadata && <p className="mt-1 text-xs font-semibold text-slate-500 print:text-[11px] print:text-slate-700">{metadata}</p>}
      <div className="mt-4 print:mt-3">{children}</div>
    </section>
  );
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div className="mb-3 last:mb-0">
      <p className="text-[11px] font-bold uppercase tracking-[0.12em] text-slate-400 print:text-slate-600">{label}</p>
      <p className="mt-1 text-sm font-semibold text-slate-800 print:text-xs print:text-black">{value || NOT_RECORDED}</p>
    </div>
  );
}

function SubstanceCard({ item }: { item: ProtocolItem }) {
  const compound = item.compound;
  const scheduleText = item.notes || compound?.notes || NOT_RECORDED;

  return (
    <article className="rounded-xl border border-slate-200 bg-white p-4 print:break-inside-avoid print:p-3">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h4 className="font-bold text-slate-950">{compound?.name ?? 'Compound snapshot'}</h4>
          <p className="mt-1 text-sm text-slate-500">{compound?.category ?? item.compoundRecordId}</p>
        </div>
        <span className="rounded-full border border-slate-200 px-2.5 py-1 text-xs font-semibold text-slate-600">
          {compound?.status ?? 'snapshot'}
        </span>
      </div>
      <dl className="mt-4 grid gap-2 text-sm text-slate-700 print:mt-3 print:text-xs sm:grid-cols-2">
        <MiniFact label="Start" value={formatDate(compound?.startDate)} />
        <MiniFact label="Stop" value={compound?.endDate ? formatDate(compound.endDate) : 'Not recorded'} />
        <MiniFact label="Goal" value={compound?.goal || NOT_RECORDED} />
        <MiniFact label="User-entered schedule/frequency" value={scheduleText} />
      </dl>
    </article>
  );
}

function MiniFact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-[11px] font-bold uppercase tracking-[0.12em] text-slate-400">{label}</dt>
      <dd className="mt-1 text-sm text-slate-700">{value}</dd>
    </div>
  );
}

function CheckInRow({ observation }: { observation: ProtocolRunObservation }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-3 print:break-inside-avoid">
      <div className="flex items-center justify-between gap-3">
        <p className="font-semibold text-slate-950">{formatDate(observation.date)}</p>
        <span className="text-xs font-semibold text-slate-500">Day {observation.day}</span>
      </div>
      <p className="mt-2 text-sm text-slate-600">
        Energy {observation.energy}/10 · Sleep {observation.sleepQuality}/10 · Recovery {observation.recovery}/10 · Appetite {observation.appetite}/10
      </p>
    </div>
  );
}

function FindingRow({ finding }: { finding: InteractionFinding }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-3 print:break-inside-avoid">
      <div className="flex flex-wrap items-center gap-2">
        <FlagBadge type={finding.type} />
        <span className="text-sm font-semibold text-slate-900">{finding.compounds.join(' + ')}</span>
        <span className="text-xs text-slate-500">{Math.round(finding.confidence * 100)}% confidence</span>
      </div>
    </div>
  );
}

function InteractionRow({ interaction }: { interaction: InteractionResult }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-3 print:break-inside-avoid">
      <div className="flex flex-wrap items-center gap-2">
        <FlagBadge type={interaction.type} />
        <span className="text-sm font-semibold text-slate-900">{interaction.compoundA} + {interaction.compoundB}</span>
        <span className="text-xs text-slate-500">{Math.round(interaction.confidence * 100)}% confidence</span>
      </div>
      {interaction.sharedPathways.length > 0 && (
        <p className="mt-2 text-xs text-slate-500">Shared pathways: {interaction.sharedPathways.join(', ')}</p>
      )}
    </div>
  );
}

function FlagBadge({ type }: { type: string }) {
  return (
    <span className="rounded-full border border-slate-300 bg-slate-100 px-2.5 py-1 text-xs font-bold uppercase tracking-[0.1em] text-slate-600">
      {flagLabel(type)}
    </span>
  );
}

function Placeholder({ children }: { children: ReactNode }) {
  return <p className="rounded-lg border border-dashed border-slate-300 bg-white px-4 py-3 text-sm text-slate-500 print:break-inside-avoid print:text-xs print:text-slate-700">{children}</p>;
}

function BoundaryText({ children }: { children: ReactNode }) {
  return <p className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-900 print:break-inside-avoid print:border-slate-300 print:bg-white print:text-xs print:text-black">{children}</p>;
}

interface ProviderSummaryTextInput {
  protocol: Protocol;
  activeItems: ProtocolItem[];
  goals: string[];
  observations: ProtocolRunObservation[];
  trends: ProtocolActualTrend[];
  run: Protocol['activeRun'];
  findings: InteractionFinding[];
  interactions: InteractionResult[];
  patternNotes: string[];
  generatedDate: string;
}

type ProtocolActualTrend = NonNullable<Protocol['actualComparison']>['actualTrends'][number];

function buildProviderSummaryText({
  protocol,
  activeItems,
  goals,
  observations,
  trends,
  run,
  findings,
  interactions,
  patternNotes,
  generatedDate,
}: ProviderSummaryTextInput) {
  return [
    'Observational summary',
    'Provider-ready observational summary',
    'For discussion with a qualified professional.',
    'Not medical advice.',
    `Generated timestamp: ${generatedDate}`,
    '',
    formatTextSection('Stack overview', [
      `Protocol: ${protocol.name}`,
      `Version: v${protocol.version}${protocol.isDraft ? ' draft' : ' snapshot'}`,
      `Snapshot state: ${protocol.isCurrentVersion ? 'Current version' : 'Prior version'}`,
      `Run state: ${run ? `${run.status} run` : 'No active run in this snapshot'}`,
    ]),
    formatTextSection('Active substances', buildActiveSubstanceLines(activeItems), formatCount(activeItems.length, 'active substance')),
    formatTextSection('Goals and timeline', [
      `Stated goals: ${goals.length > 0 ? goals.join(', ') : NOT_RECORDED}`,
      `Protocol created: ${formatDate(protocol.createdAtUtc)}`,
      `Protocol updated: ${formatDate(protocol.updatedAtUtc)}`,
      ...(run ? [`Run window: ${formatDate(run.startedAtUtc)} — ${run.endedAtUtc ? formatDate(run.endedAtUtc) : 'ongoing'}`] : []),
    ]),
    formatTextSection('Recent check-ins', buildObservationLines(observations), formatCount(observations.length, 'recent check-in')),
    formatTextSection('Observed patterns', buildPatternLines(trends, patternNotes)),
    formatTextSection('Interaction and overlap notes', buildInteractionLines(findings, interactions), formatCount(findings.length + interactions.length, 'interaction/overlap note')),
    formatTextSection('Evidence context', [
      `Stack evidence score: ${protocol.stackScore.breakdown.evidence}`,
      NO_EVIDENCE_CONTEXT,
    ]),
    formatTextSection('Safety boundary', SAFETY_BOUNDARY_LINES),
  ].join('\n\n');
}

function buildActiveSubstanceLines(activeItems: ProtocolItem[]) {
  if (activeItems.length === 0) return [NO_ACTIVE_SUBSTANCES];

  return activeItems.map((item) => {
    const compound = item.compound;
    const scheduleText = item.notes || compound?.notes || NOT_RECORDED;
    return `${compound?.name ?? 'Compound snapshot'} (${compound?.category ?? item.compoundRecordId}; ${compound?.status ?? 'snapshot'}) — Start: ${formatDate(compound?.startDate)}; Stop: ${compound?.endDate ? formatDate(compound.endDate) : 'Not recorded'}; Goal: ${compound?.goal || NOT_RECORDED}; User-entered schedule/frequency: ${scheduleText}`;
  });
}

function buildObservationLines(observations: ProtocolRunObservation[]) {
  if (observations.length === 0) return [NO_RECENT_CHECK_INS];

  return observations.map((observation) => `${formatDate(observation.date)} (Day ${observation.day}): Energy ${observation.energy}/10 · Sleep ${observation.sleepQuality}/10 · Recovery ${observation.recovery}/10 · Appetite ${observation.appetite}/10`);
}

function buildPatternLines(trends: ProtocolActualTrend[], patternNotes: string[]) {
  if (trends.length === 0 && patternNotes.length === 0) return [NO_OBSERVED_PATTERNS];

  return [
    ...trends.map((trend) => `${trend.metric}: ${trend.direction} (${formatAverage(trend.beforeAverage)} before · ${formatAverage(trend.afterAverage)} after)`),
    ...patternNotes,
  ];
}

function buildInteractionLines(findings: InteractionFinding[], interactions: InteractionResult[]) {
  if (findings.length === 0 && interactions.length === 0) return [NO_INTERACTION_NOTES];

  return [
    ...findings.map((finding) => `${flagLabel(finding.type)}: ${finding.compounds.join(' + ')} (${Math.round(finding.confidence * 100)}% confidence)`),
    ...interactions.map((interaction) => `${flagLabel(interaction.type)}: ${interaction.compoundA} + ${interaction.compoundB} (${Math.round(interaction.confidence * 100)}% confidence)${interaction.sharedPathways.length > 0 ? `; Shared pathways: ${interaction.sharedPathways.join(', ')}` : ''}`),
  ];
}

function formatTextSection(title: string, lines: string[], metadata?: string) {
  return [title, ...(metadata ? [`- ${metadata}`] : []), ...lines.map((line) => `- ${line}`)].join('\n');
}

function collectPatternNotes(
  patterns?: ProtocolPatternSnapshot | null,
  drift?: ProtocolDriftSnapshot | null,
  sequence?: ProtocolSequenceExpectationSnapshot | null,
  review?: ProtocolReview | null
) {
  return [
    ...(patterns?.metricPatterns ?? []).map((pattern) => `${pattern.metric}: ${pattern.observation}`),
    ...(patterns?.eventPatterns ?? []).map((pattern) => `${pattern.eventType}: ${pattern.timingPattern}`),
    ...(patterns?.currentRunComparison?.matchingSignals ?? []),
    ...(patterns?.currentRunComparison?.divergentSignals ?? []),
    ...(drift?.signals ?? []).map((signal) => `${signal.severity} drift signal: ${signal.description}`),
    ...(sequence?.currentStatus?.notes ?? []),
    ...(review?.safetyNotes ?? []),
  ].slice(0, 8);
}

function recentObservations(observations: ProtocolRunObservation[]) {
  return [...observations]
    .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
    .slice(0, 5);
}

function isActiveItem(item: ProtocolItem) {
  const status = item.compound?.status?.toLowerCase();
  return !status || status === 'active';
}

function unique(values: string[]) {
  return Array.from(new Set(values.map((value) => value.trim()).filter(Boolean)));
}

function formatAverage(value: number | null) {
  return value === null ? 'n/a' : value.toString();
}

function formatCount(count: number, singular: string) {
  return `${count} ${singular}${count === 1 ? '' : 's'}`;
}

function formatDate(value?: string | null) {
  if (!value) return NOT_RECORDED;
  return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric', timeZone: 'UTC' }).format(new Date(value));
}

function formatGeneratedAt(value: Date) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    timeZone: 'UTC',
  }).format(value);
}

function flagLabel(type: string) {
  if (type === 'Synergistic') return 'synergy';
  if (type === 'Complementary') return 'overlap';
  if (type === 'Redundant') return 'redundancy';
  if (type === 'Interfering') return 'interference';
  return type.toLowerCase();
}