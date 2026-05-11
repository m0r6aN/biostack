'use client';

import { useState } from 'react';
import { MetricLane } from '@/components/intel/MetricLane';
import { TIMELINE_TAG_TOKENS } from '@/styles/tokens';
import { cn } from '@/lib/utils';
import { track } from '@/lib/telemetry';
import type { RunTelemetry, TelemetryLane, LaneEvent } from '@/lib/derive/runTelemetry';

interface FlightRecorderProps {
  telemetry: RunTelemetry;
  className?: string;
}

const LANE_COLOR_MAP: Record<string, string> = {
  protocol: 'emerald',
  compounds: 'blue',
  'expected-signal': 'purple',
  sleep: 'blue',
  energy: 'emerald',
  recovery: 'amber',
  focus: 'purple',
  pattern: 'amber',
};

const LANE_KIND_LABEL: Record<string, string> = {
  protocol: 'Protocol Events',
  compound: 'Compound Changes',
  'expected-signal': 'Expected Signal (Simulation)',
  actual: 'Actual Telemetry',
  pattern: 'Pattern / Drift',
};

function formatDate(d: string) {
  return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' }).format(new Date(d));
}

export function FlightRecorder({ telemetry, className }: FlightRecorderProps) {
  const [selectedEvent, setSelectedEvent] = useState<LaneEvent | null>(null);
  const [expandedLane, setExpandedLane] = useState<string | null>(null);

  if (!telemetry.run) {
    return (
      <div className={cn('rounded-3xl border border-white/8 bg-[#0F141B] p-6', className)}>
        <p className="text-[10px] font-bold text-white/20 uppercase tracking-widest mb-3">Flight Recorder</p>
        <div className="text-center py-8">
          <p className="text-sm text-white/40">No active run to record.</p>
          <p className="text-xs text-white/25 mt-1">Start a protocol run to begin telemetry recording.</p>
        </div>
      </div>
    );
  }

  const { run, lanes, startDate, endDate } = telemetry;

  // Group lanes by kind
  const laneGroups: Record<string, TelemetryLane[]> = {};
  for (const lane of lanes) {
    const group = lane.kind === 'actual' ? lane.id : lane.kind;
    laneGroups[group] = laneGroups[group] ?? [];
    laneGroups[group].push(lane);
  }

  return (
    <div className={cn('rounded-3xl border border-white/8 bg-[#0F141B]', className)}>
      {/* Header */}
      <div className="flex items-start justify-between px-6 pt-5 pb-4 border-b border-white/5">
        <div>
          <p className="text-[10px] font-bold text-white/20 uppercase tracking-widest mb-1">Flight Recorder</p>
          <h2 className="text-base font-bold text-white/90">{run.protocolName}</h2>
          <div className="flex items-center gap-3 mt-1">
            {startDate && (
              <span className="text-xs text-white/40 font-mono">
                {formatDate(startDate)}
                {endDate ? ` → ${formatDate(endDate)}` : ' → ongoing'}
              </span>
            )}
            <span className={cn(
              'text-[10px] font-semibold px-2 py-0.5 rounded-full border',
              run.status === 'active'
                ? 'text-emerald-300 bg-emerald-500/10 border-emerald-400/20'
                : run.status === 'completed'
                ? 'text-blue-300 bg-blue-500/10 border-blue-400/20'
                : 'text-white/40 bg-white/5 border-white/10',
            )}>
              {run.status}
            </span>
          </div>
        </div>
      </div>

      <div className="flex gap-0 min-h-0">
        {/* Lanes panel */}
        <div className="flex-1 px-6 py-5 space-y-6 overflow-x-auto min-w-0">
          {lanes.map((lane) => (
            <LaneSection
              key={lane.id}
              lane={lane}
              color={LANE_COLOR_MAP[lane.id] ?? 'emerald'}
              expanded={expandedLane === lane.id}
              onToggle={() => setExpandedLane((prev) => prev === lane.id ? null : lane.id)}
              onEventClick={(e) => {
                setSelectedEvent(e);
                track({ name: 'flight_recorder_event_click', eventType: e.status });
              }}
            />
          ))}

          {lanes.length === 0 && (
            <div className="text-center py-8">
              <p className="text-sm text-white/30">No telemetry data for this run yet.</p>
              <p className="text-xs text-white/20 mt-1">Log observations to populate the recorder.</p>
            </div>
          )}
        </div>

        {/* Event detail side panel */}
        {selectedEvent && (
          <div className="w-72 border-l border-white/5 px-5 py-5 shrink-0">
            <div className="flex items-center justify-between mb-4">
              <p className="text-[10px] font-bold text-white/20 uppercase tracking-widest">Event Detail</p>
              <button onClick={() => setSelectedEvent(null)} className="p-1 hover:bg-white/5 rounded-lg text-white/30 hover:text-white/50">
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <div className="space-y-3">
              <div>
                <p className="text-[10px] text-white/30 mb-0.5">Day</p>
                <p className="text-sm font-semibold text-white/80">Day {selectedEvent.day} · {formatDate(selectedEvent.date)}</p>
              </div>

              <div>
                <p className="text-[10px] text-white/30 mb-0.5">Event</p>
                <p className="text-sm text-white/70">{selectedEvent.label}</p>
              </div>

              {selectedEvent.status && selectedEvent.status !== 'neutral' && (
                <div>
                  <p className="text-[10px] text-white/30 mb-0.5">Alignment</p>
                  {(() => {
                    const t = TIMELINE_TAG_TOKENS[selectedEvent.status as keyof typeof TIMELINE_TAG_TOKENS];
                    return t ? (
                      <span className={cn('text-xs font-semibold px-2 py-0.5 rounded-full border', t.bg, t.color, t.border)}>
                        {t.label}
                      </span>
                    ) : null;
                  })()}
                </div>
              )}

              {selectedEvent.value != null && (
                <div>
                  <p className="text-[10px] text-white/30 mb-0.5">Value</p>
                  <p className="text-lg font-bold text-white/80 font-mono">{selectedEvent.value}</p>
                </div>
              )}

              {selectedEvent.detail && (
                <div>
                  <p className="text-[10px] text-white/30 mb-0.5">Detail</p>
                  <p className="text-xs text-white/50 leading-relaxed">{selectedEvent.detail}</p>
                </div>
              )}

              <div className="pt-2 border-t border-white/5">
                <p className="text-[10px] text-white/20 leading-relaxed">
                  Values are observed and logged — not computed outcomes or causal claims.
                </p>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function LaneSection({
  lane, color, expanded, onToggle, onEventClick,
}: {
  lane: TelemetryLane;
  color: string;
  expanded: boolean;
  onToggle: () => void;
  onEventClick: (e: LaneEvent) => void;
}) {
  const isMetric = lane.kind === 'actual';

  return (
    <div>
      {/* Lane header */}
      <button
        onClick={onToggle}
        className="w-full flex items-center justify-between gap-3 mb-2 group"
      >
        <div className="flex items-center gap-2">
          <div className={cn('w-1 h-4 rounded-full', `bg-${color}-400/60`)} />
          <span className="text-[11px] font-semibold text-white/50 group-hover:text-white/70 transition-colors">{lane.label}</span>
          <span className="text-[9px] text-white/25">{lane.events.length} events</span>
        </div>
        <svg className={cn('w-3.5 h-3.5 text-white/20 transition-transform', expanded ? 'rotate-180' : '')} fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
          <path strokeLinecap="round" strokeLinejoin="round" d="m19.5 8.25-7.5 7.5-7.5-7.5" />
        </svg>
      </button>

      {/* Metric lane (sparkline) */}
      {isMetric && (
        <MetricLane
          label=""
          color={color}
          events={lane.events.map((e) => ({
            date: e.date,
            value: typeof e.value === 'number' ? e.value : null,
            label: e.label,
          }))}
        />
      )}

      {/* Event list (expanded) */}
      {expanded && (
        <div className="mt-2 space-y-1.5 pl-3">
          {lane.events.map((e, i) => {
            const tagToken = e.status !== 'neutral' ? TIMELINE_TAG_TOKENS[e.status as keyof typeof TIMELINE_TAG_TOKENS] : null;
            return (
              <button
                key={i}
                onClick={() => onEventClick(e)}
                className="w-full flex items-start gap-2.5 text-left rounded-xl px-2 py-1.5 hover:bg-white/[0.03] transition-colors group"
              >
                <span className="text-[10px] font-mono text-white/25 pt-0.5 shrink-0 w-12">D{e.day}</span>
                <span className="text-[11px] text-white/60 flex-1 leading-snug">{e.label}</span>
                {tagToken && (
                  <span className={cn('text-[9px] font-semibold px-1.5 py-0.5 rounded-full border shrink-0', tagToken.bg, tagToken.color, tagToken.border)}>
                    {tagToken.label}
                  </span>
                )}
                {typeof e.value === 'number' && (
                  <span className="font-mono text-[10px] text-white/40 shrink-0">{e.value}</span>
                )}
              </button>
            );
          })}
          {lane.events.length === 0 && (
            <p className="text-[11px] text-white/25 italic pl-2">No events in this lane.</p>
          )}
        </div>
      )}
    </div>
  );
}
