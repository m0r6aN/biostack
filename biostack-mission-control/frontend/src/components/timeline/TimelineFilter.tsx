'use client';

import { TimelineEventType } from '@/lib/types';

interface TimelineFilterProps {
  activeFilter: string;
  onFilterChange: (filter: string) => void;
}

const filters = [
  { id: 'all', label: 'All Events' },
  { id: 'compound_added', label: 'Compounds Added' },
  { id: 'compound_ended', label: 'Compounds Ended' },
  { id: 'phase_started', label: 'Phases Started' },
  { id: 'check_in', label: 'Check-ins' },
  { id: 'knowledge_update', label: 'Knowledge Updates' },
];

export function TimelineFilter({ activeFilter, onFilterChange }: TimelineFilterProps) {
  return (
    <div className="flex flex-wrap gap-2">
      {filters.map((filter) => (
        <button
          key={filter.id}
          onClick={() => onFilterChange(filter.id)}
          className={`px-3 py-1 text-sm font-medium transition-all ${
            activeFilter === filter.id
              ? 'bg-emerald-500/10 text-emerald-300 border border-emerald-400/20 rounded-full'
              : 'bg-white/[0.04] text-white/50 hover:text-white/70 hover:bg-white/[0.06] border border-white/10 rounded-full'
          }`}
        >
          {filter.label}
        </button>
      ))}
    </div>
  );
}
