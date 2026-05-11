import type { ProtocolRun, ProtocolRunObservation, CheckIn, ProtocolDriftSnapshot, ProtocolPatternSnapshot, SimulationResult, ProtocolReviewTimelineEvent, CompoundRecord } from '@/lib/types';
import { deriveTag } from './timelineTag';

export type LaneEventStatus = 'aligned' | 'late' | 'diverging' | 'regime-shift' | 'expected-pending' | 'neutral';

export interface LaneEvent {
  date: string;
  day: number;
  status: LaneEventStatus;
  label: string;
  value?: number | string | null;
  detail?: string;
}

export interface TelemetryLane {
  id: string;
  label: string;
  color: string;
  events: LaneEvent[];
  kind: 'protocol' | 'compound' | 'expected-signal' | 'actual' | 'pattern';
}

export interface RunTelemetry {
  run: ProtocolRun | null;
  lanes: TelemetryLane[];
  startDate: string | null;
  endDate: string | null;
}

function dayOf(baseDate: string, targetDate: string): number {
  return Math.round((new Date(targetDate).getTime() - new Date(baseDate).getTime()) / 86_400_000) + 1;
}

export function buildRunTelemetry(
  run: ProtocolRun | null,
  observations: ProtocolRunObservation[],
  checkIns: CheckIn[],
  drift: ProtocolDriftSnapshot | null,
  pattern: ProtocolPatternSnapshot | null,
  simulation: SimulationResult | null,
  timelineEvents: ProtocolReviewTimelineEvent[],
  compounds: CompoundRecord[],
): RunTelemetry {
  if (!run) return { run: null, lanes: [], startDate: null, endDate: null };

  const startDate = run.startedAtUtc;
  const endDate = run.endedAtUtc;

  // ── Lane 1: Protocol events ──────────────────────────────────────────────
  const protocolLane: TelemetryLane = {
    id: 'protocol',
    label: 'Protocol',
    color: 'emerald',
    kind: 'protocol',
    events: timelineEvents
      .filter((e) => e.runId === run.id || e.eventType.startsWith('run_'))
      .map((e) => ({
        date: e.occurredAtUtc,
        day: dayOf(startDate, e.occurredAtUtc),
        status: 'neutral',
        label: e.label,
        detail: e.detail,
      })),
  };

  // ── Lane 2: Compound changes ─────────────────────────────────────────────
  const compoundLane: TelemetryLane = {
    id: 'compounds',
    label: 'Compounds',
    color: 'blue',
    kind: 'compound',
    events: compounds.map((c) => ({
      date: c.startDate,
      day: dayOf(startDate, c.startDate),
      status: 'neutral' as LaneEventStatus,
      label: `${c.name} ${c.status === 'Active' ? 'started' : c.status.toLowerCase()}`,
      value: c.status,
    })).filter((e) => !isNaN(e.day)),
  };

  // ── Lane 3: Expected signal (simulation bands) ───────────────────────────
  const expectedLane: TelemetryLane = {
    id: 'expected-signal',
    label: 'Expected Signal',
    color: 'purple',
    kind: 'expected-signal',
    events: (simulation?.timeline ?? []).map((t, i) => ({
      date: startDate, // relative — use day ranges as labels
      day: i * 3 + 1,
      status: 'neutral' as LaneEventStatus,
      label: t.dayRange,
      detail: t.signals.join('; '),
    })),
  };

  // ── Lane 4: Actual telemetry (check-ins) ─────────────────────────────────
  const metricLanes: TelemetryLane[] = [
    { id: 'sleep', label: 'Sleep', color: 'blue', kind: 'actual', events: [] },
    { id: 'energy', label: 'Energy', color: 'emerald', kind: 'actual', events: [] },
    { id: 'recovery', label: 'Recovery', color: 'amber', kind: 'actual', events: [] },
    { id: 'focus', label: 'Focus', color: 'purple', kind: 'actual', events: [] },
  ];

  for (const obs of observations) {
    const day = dayOf(startDate, obs.date);
    const checkin = checkIns.find((c) => c.date === obs.date);
    metricLanes[0].events.push({ date: obs.date, day, status: 'neutral', label: `Day ${day}`, value: obs.sleepQuality });
    metricLanes[1].events.push({ date: obs.date, day, status: 'neutral', label: `Day ${day}`, value: obs.energy });
    metricLanes[2].events.push({ date: obs.date, day, status: 'neutral', label: `Day ${day}`, value: obs.recovery });
    if (checkin?.focus != null) {
      metricLanes[3].events.push({ date: obs.date, day, status: 'neutral', label: `Day ${day}`, value: checkin.focus });
    }
  }

  // ── Lane 5: Pattern / Drift ───────────────────────────────────────────────
  const patternLane: TelemetryLane = {
    id: 'pattern',
    label: 'Pattern / Drift',
    color: 'amber',
    kind: 'pattern',
    events: timelineEvents
      .filter((e) => e.runId === run.id)
      .map((e) => {
        const tag = deriveTag(e, null, drift);
        return {
          date: e.occurredAtUtc,
          day: dayOf(startDate, e.occurredAtUtc),
          status: (tag ?? 'neutral') as LaneEventStatus,
          label: e.label,
          detail: e.detail,
        };
      })
      .filter((e) => e.status !== 'neutral'),
  };

  return {
    run,
    startDate,
    endDate,
    lanes: [
      protocolLane,
      compoundLane,
      expectedLane,
      ...metricLanes.filter((l) => l.events.length > 0),
      patternLane,
    ],
  };
}
