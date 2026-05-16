'use client';

import { useState } from 'react';

import { cn } from '@/lib/utils';
import type { WitnessNarrative, WitnessEntry } from '@/lib/types';

interface WitnessNarrativePanelProps {
  narrative: WitnessNarrative;
  className?: string;
}

type FilterOption = 'all' | 'proposed' | 'challenged' | 'survived' | 'blocked';

const FILTER_OPTIONS: FilterOption[] = ['all', 'proposed', 'challenged', 'survived', 'blocked'];

const ROLE_COLORS: Record<string, string> = {
  Optimizer:    'bg-emerald-500/10 text-emerald-300 border-emerald-400/20',
  Skeptic:      'bg-amber-500/10 text-amber-300 border-amber-400/20',
  Regulator:    'bg-sky-500/10 text-sky-300 border-sky-400/20',
  Historian:    'bg-violet-500/10 text-violet-300 border-violet-400/20',
  Contradiction: 'bg-orange-500/10 text-orange-300 border-orange-400/20',
};

const EVENT_COLORS: Record<string, string> = {
  proposed:   'bg-blue-500/10 text-blue-300 border-blue-400/20',
  challenged: 'bg-red-500/10 text-red-300 border-red-400/20',
  survived:   'bg-emerald-500/10 text-emerald-300 border-emerald-400/20',
  blocked:    'bg-slate-500/10 text-slate-300 border-slate-400/20',
  escalated:  'bg-orange-500/10 text-orange-300 border-orange-400/20',
};

function roleChipClass(role: string): string {
  return ROLE_COLORS[role] ?? 'bg-slate-500/10 text-slate-300 border-slate-400/20';
}

function eventChipClass(eventType: string): string {
  return EVENT_COLORS[eventType] ?? 'bg-slate-500/10 text-slate-300 border-slate-400/20';
}

function formatTimestamp(iso: string): string {
  try {
    return new Date(iso).toLocaleTimeString(undefined, {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  } catch {
    return iso;
  }
}

function TimelineEntry({ entry }: { entry: WitnessEntry }) {
  return (
    <div className="relative pl-6 pb-6 last:pb-0">
      {/* Vertical connector */}
      <span className="absolute left-0 top-2 bottom-0 w-px bg-white/8" aria-hidden />
      {/* Dot */}
      <span className="absolute left-[-3px] top-[7px] w-1.5 h-1.5 rounded-full bg-white/20" aria-hidden />

      <div className="flex flex-wrap items-center gap-2 mb-1.5">
        <span className={cn('inline-flex items-center text-[10px] font-semibold px-2 py-0.5 rounded-full border', roleChipClass(entry.role))}>
          {entry.role}
        </span>
        <span className={cn('inline-flex items-center text-[10px] font-medium px-2 py-0.5 rounded-full border', eventChipClass(entry.eventType))}>
          {entry.eventType}
        </span>
        <span className="ml-auto flex items-center gap-1 text-[10px] text-white/30 font-mono">
          <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
            <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
          </svg>
          {formatTimestamp(entry.timestamp)}
        </span>
      </div>

      <p className="text-[12px] text-white/70 leading-relaxed">{entry.summary}</p>

      {entry.findingIds.length > 0 && (
        <p className="mt-1 text-[10px] font-mono text-white/30">
          References: {entry.findingIds.join(', ')}
        </p>
      )}
    </div>
  );
}

export function WitnessNarrativePanel({ narrative, className }: WitnessNarrativePanelProps) {
  const [expanded, setExpanded] = useState(false);
  const [activeFilter, setActiveFilter] = useState<FilterOption>('all');

  const filtered = activeFilter === 'all'
    ? narrative.entries
    : narrative.entries.filter(e => e.eventType === activeFilter);

  return (
    <div className={cn('rounded-2xl border border-white/10 bg-[#0a0a0c] overflow-hidden', className)}>
      {/* Header / toggle */}
      <button
        onClick={() => setExpanded(v => !v)}
        className="w-full flex items-center justify-between px-5 py-4 text-left hover:bg-white/[0.02] transition-colors"
      >
        <div className="flex items-center gap-2.5">
          <span className="text-[11px] font-mono text-white/30 uppercase tracking-widest">KE-7</span>
          <span className="text-sm font-semibold text-white/80">Witness Narrative</span>
          <span className="text-[10px] text-white/30 bg-white/5 border border-white/8 px-2 py-0.5 rounded-full font-mono">
            {narrative.entries.length} events
          </span>
        </div>
        <svg className="w-4 h-4 text-white/30 transition-transform" style={{ transform: expanded ? 'rotate(180deg)' : undefined }} fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
          <polyline points="6 9 12 15 18 9"/>
        </svg>
      </button>

      {expanded && (
        <div className="border-t border-white/8">
          {/* Filter bar */}
          <div className="flex items-center gap-1.5 px-5 py-3 border-b border-white/8">
            <svg className="w-3.5 h-3.5 text-white/20 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
            <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"/>
          </svg>
            {FILTER_OPTIONS.map(opt => (
              <button
                key={opt}
                onClick={() => setActiveFilter(opt)}
                className={cn(
                  'text-[10px] font-semibold px-2.5 py-1 rounded-full border transition-all',
                  activeFilter === opt
                    ? 'bg-white/10 text-white/80 border-white/20'
                    : 'bg-transparent text-white/30 border-white/8 hover:bg-white/5 hover:text-white/50',
                )}
              >
                {opt}
              </button>
            ))}
          </div>

          {/* Timeline */}
          <div className="px-5 py-4">
            {filtered.length === 0 ? (
              <p className="text-[12px] text-white/30 text-center py-6">
                No events match this filter.
              </p>
            ) : (
              filtered.map((entry, idx) => (
                <TimelineEntry key={`${entry.role}-${idx}`} entry={entry} />
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}
