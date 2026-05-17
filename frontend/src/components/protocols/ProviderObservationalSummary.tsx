'use client';

import { HelpTip } from '@/components/ui/HelpTip';
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

interface ProviderObservationalSummaryProps {
  protocol: Protocol;
  review?: ProtocolReview | null;
  patterns?: ProtocolPatternSnapshot | null;
  drift?: ProtocolDriftSnapshot | null;
  sequence?: ProtocolSequenceExpectationSnapshot | null;
  generatedAt?: Date;
}

const NOT_RECORDED = 'Not recorded in this saved protocol snapshot.';

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
  const patternNotes = collectPatternNotes(patterns, drift, sequence, review);

  return (
    <section
      aria-labelledby="provider-summary-title"
      className="rounded-2xl border border-white/[0.08] bg-[#f8fafc] p-0 text-slate-950 shadow-2xl shadow-black/20 print:border-slate-200 print:bg-white print:shadow-none"
    >
      <div className="border-b border-slate-200 p-6 print:p-5">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Observational summary</p>
            <h2 id="provider-summary-title" className="mt-2 text-2xl font-black text-slate-950">
              Provider-ready observational summary
            </h2>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-600">
              A factual snapshot of what you are tracking, what changed, and what BioStack observed. Bring it to a qualified professional for review.
            </p>
          </div>
          <div className="text-left sm:text-right">
            <p className="text-xs uppercase tracking-[0.14em] text-slate-500">Generated</p>
            <p className="mt-1 text-sm font-semibold text-slate-800">{generatedDate}</p>
            <button
              type="button"
              onClick={() => window.print()}
              className="mt-3 rounded-lg border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100 print:hidden"
            >
              Print summary
            </button>
          </div>
        </div>
      </div>

      <div className="grid gap-0 divide-y divide-slate-200 lg:grid-cols-2 lg:divide-x lg:divide-y-0">
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

      <SummarySection title="Active substances" fullWidth>
        {activeItems.length === 0 ? (
          <Placeholder>No active substances were present in this saved protocol snapshot.</Placeholder>
        ) : (
          <div className="grid gap-3 md:grid-cols-2">
            {activeItems.map((item) => (
              <SubstanceCard key={item.id} item={item} />
            ))}
          </div>
        )}
      </SummarySection>

      <div className="grid gap-0 divide-y divide-slate-200 border-t border-slate-200 lg:grid-cols-2 lg:divide-x lg:divide-y-0">
        <SummarySection title="Recent check-ins">
          {observations.length === 0 ? (
            <Placeholder>No protocol check-ins are attached to this snapshot yet.</Placeholder>
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
            <Placeholder>Historical pattern snapshots are not available for this view.</Placeholder>
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

      <div className="grid gap-0 divide-y divide-slate-200 border-t border-slate-200 lg:grid-cols-2 lg:divide-x lg:divide-y-0">
        <SummarySection title="Interaction and overlap notes">
          {findings.length === 0 && interactions.length === 0 ? (
            <Placeholder>No overlap, synergy, redundancy, or interference flags are present in this snapshot.</Placeholder>
          ) : (
            <div className="space-y-3">
              {findings.map((finding) => <FindingRow key={`${finding.type}-${finding.compounds.join('-')}`} finding={finding} />)}
              {interactions.map((interaction) => <InteractionRow key={`${interaction.type}-${interaction.compoundA}-${interaction.compoundB}`} interaction={interaction} />)}
            </div>
          )}
        </SummarySection>

        <SummarySection title="Evidence context">
          <Fact label="Stack evidence score" value={`${protocol.stackScore.breakdown.evidence}`} />
          <p className="mt-3 text-sm leading-6 text-slate-600">
            <HelpTip tipKey="evidenceTier">Evidence tier labels</HelpTip> are shown when they are stored with the saved snapshot. Compound-level tier labels are not stored on this protocol snapshot.
          </p>
        </SummarySection>
      </div>

      <SummarySection title="Safety boundary" fullWidth>
        <div className="grid gap-3 md:grid-cols-2">
          <BoundaryText>For discussion with a qualified professional.</BoundaryText>
          <BoundaryText>Not medical advice.</BoundaryText>
          <BoundaryText>Does not recommend starting, stopping, combining, or dosing any substance.</BoundaryText>
          <BoundaryText>Observational and educational use only.</BoundaryText>
        </div>
      </SummarySection>
    </section>
  );
}

function SummarySection({ title, fullWidth = false, children }: { title: string; fullWidth?: boolean; children: React.ReactNode }) {
  return (
    <section className={`${fullWidth ? 'border-t border-slate-200' : ''} p-6 print:p-5`}>
      <h3 className="text-xs font-bold uppercase tracking-[0.16em] text-slate-500">{title}</h3>
      <div className="mt-4">{children}</div>
    </section>
  );
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div className="mb-3 last:mb-0">
      <p className="text-[11px] font-bold uppercase tracking-[0.12em] text-slate-400">{label}</p>
      <p className="mt-1 text-sm font-semibold text-slate-800">{value || NOT_RECORDED}</p>
    </div>
  );
}

function SubstanceCard({ item }: { item: ProtocolItem }) {
  const compound = item.compound;
  const scheduleText = item.notes || compound?.notes || NOT_RECORDED;

  return (
    <article className="rounded-xl border border-slate-200 bg-white p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h4 className="font-bold text-slate-950">{compound?.name ?? 'Compound snapshot'}</h4>
          <p className="mt-1 text-sm text-slate-500">{compound?.category ?? item.compoundRecordId}</p>
        </div>
        <span className="rounded-full border border-slate-200 px-2.5 py-1 text-xs font-semibold text-slate-600">
          {compound?.status ?? 'snapshot'}
        </span>
      </div>
      <dl className="mt-4 grid gap-2 text-sm text-slate-700 sm:grid-cols-2">
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
    <div className="rounded-lg border border-slate-200 bg-white p-3">
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
    <div className="rounded-lg border border-slate-200 bg-white p-3">
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
    <div className="rounded-lg border border-slate-200 bg-white p-3">
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

function Placeholder({ children }: { children: React.ReactNode }) {
  return <p className="rounded-lg border border-dashed border-slate-300 bg-white px-4 py-3 text-sm text-slate-500">{children}</p>;
}

function BoundaryText({ children }: { children: React.ReactNode }) {
  return <p className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-900">{children}</p>;
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