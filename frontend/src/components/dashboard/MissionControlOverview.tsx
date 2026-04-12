'use client';

import Link from 'next/link';
import { MissionControl } from '@/lib/types';

interface MissionControlOverviewProps {
  mission: MissionControl | null;
}

export function MissionControlOverview({ mission }: MissionControlOverviewProps) {
  const activeRun = mission?.activeRun;
  const latestClosedRun = mission?.latestClosedRun;
  const review = mission?.latestReviewSummary;
  const evolution = mission?.recentEvolution;
  const checkInSignal = mission?.latestCheckInSignal;
  const observationSignals = mission?.observationSignals ?? [];

  return (
    <section className="rounded-lg border border-emerald-400/15 bg-[#0f171f] p-5">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-200/55">Where Am I Now?</p>
          <h2 className="mt-2 text-2xl font-black text-white">
            {activeRun ? `${activeRun.protocolName} v${activeRun.protocolVersion} is running` : 'No protocol run is active'}
          </h2>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-white/55">
            {activeRun
              ? checkInSignal?.cue ?? 'Comparison is available for the active lineage once observations are attached.'
              : latestClosedRun
                ? `${latestClosedRun.protocolName} v${latestClosedRun.protocolVersion} was ${latestClosedRun.status}.`
                : 'Simulate, save, and track a protocol to begin the closed-loop workflow.'}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Link
            href="/protocols"
            className="rounded-lg border border-white/[0.1] px-3 py-2 text-sm font-semibold text-white/70 hover:border-white/25"
          >
            Simulate + save
          </Link>
          {activeRun && (
            <Link
              href={`/protocols/${activeRun.protocolId}#comparison`}
              className="rounded-lg bg-emerald-500 px-3 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-400"
            >
              Observe run
            </Link>
          )}
        </div>
      </div>

      <div className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        <OverviewCell
          label="Active run"
          value={activeRun ? `v${activeRun.protocolVersion} ${activeRun.status}` : 'None'}
          detail={activeRun ? `Started ${formatDate(activeRun.startedAtUtc)}` : 'Track a saved protocol to start.'}
          href={activeRun ? `/protocols/${activeRun.protocolId}` : '/protocols'}
        />
        <OverviewCell
          label="Latest closed run"
          value={latestClosedRun ? `v${latestClosedRun.protocolVersion} ${latestClosedRun.status}` : 'No closed run'}
          detail={latestClosedRun ? `Ended ${formatDate(latestClosedRun.endedAtUtc ?? latestClosedRun.startedAtUtc)}` : 'Complete or abandon a run to compare.'}
          href={latestClosedRun ? `/protocols/${latestClosedRun.protocolId}#comparison` : '/protocols'}
        />
        <OverviewCell
          label="Review signal"
          value={review ? review.signalType : 'Pending'}
          detail={review?.cue ?? 'Review available after multiple runs.'}
          href={review ? `/protocols/${review.protocolId}#review` : '/protocols'}
        />
        <OverviewCell
          label="Recent evolution"
          value={evolution?.label ?? 'No draft'}
          detail={evolution?.summary ?? 'Evolution draft appears after a completed or abandoned run.'}
          href={evolution ? `/protocols/${evolution.protocolId}` : '/protocols'}
        />
      </div>

      {observationSignals.length > 0 && (
        <div className="mt-5 rounded-lg border border-white/[0.07] bg-white/[0.025] p-4">
          <p className="text-[11px] font-semibold uppercase tracking-[0.14em] text-white/35">Observation signals</p>
          <div className="mt-3 grid gap-2 md:grid-cols-2">
            {observationSignals.map((signal, index) => (
              <div key={`${signal.type}-${signal.metric ?? 'none'}-${index}`} className="rounded-lg border border-white/[0.06] bg-black/10 p-3">
                <div className="flex items-center justify-between gap-3">
                  <p className="text-sm font-semibold text-white">{signalLabel(signal.type, signal.metric)}</p>
                  <span className={`rounded-lg border px-2 py-1 text-xs font-semibold ${severityClass(signal.severity)}`}>
                    {signal.severity}
                  </span>
                </div>
                <p className="mt-1 text-xs leading-5 text-white/50">{signal.detail}</p>
              </div>
            ))}
          </div>
        </div>
      )}
    </section>
  );
}

function OverviewCell({
  label,
  value,
  detail,
  href,
}: {
  label: string;
  value: string;
  detail: string;
  href: string;
}) {
  return (
    <Link
      href={href}
      className="min-h-36 rounded-lg border border-white/[0.07] bg-white/[0.025] p-4 transition-colors hover:border-emerald-400/25"
    >
      <p className="text-[11px] font-semibold uppercase tracking-[0.14em] text-white/35">{label}</p>
      <p className="mt-3 text-lg font-bold text-white">{value}</p>
      <p className="mt-2 line-clamp-3 text-sm leading-5 text-white/50">{detail}</p>
    </Link>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' }).format(new Date(value));
}

function signalLabel(type: string, metric: string | null) {
  if (type === 'gap') return 'Observation gap';
  if (type === 'trend_shift') return `${metric ?? 'Metric'} trend shift`;
  return type.replaceAll('_', ' ');
}

function severityClass(severity: string) {
  if (severity === 'high') return 'border-red-400/25 bg-red-500/10 text-red-200';
  if (severity === 'medium') return 'border-amber-400/25 bg-amber-500/10 text-amber-100';
  return 'border-sky-400/25 bg-sky-500/10 text-sky-100';
}
