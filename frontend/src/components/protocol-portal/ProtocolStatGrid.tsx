import type { ProtocolStat } from '@/lib/types';
import { cn } from '@/lib/utils';
import { accentClasses } from './accents';

interface ProtocolStatGridProps {
  stats: ProtocolStat[];
}

/**
 * The four KPI tiles. Mirrors the glass styling of components/dashboard/StatCard
 * but takes a free-form `caption` (StatCard's `trend` renders a fixed
 * "% from last period" string that doesn't fit these stats).
 */
export function ProtocolStatGrid({ stats }: ProtocolStatGridProps) {
  return (
    <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
      {stats.map((stat) => (
        <ProtocolStatCard key={stat.label} stat={stat} />
      ))}
    </div>
  );
}

function ProtocolStatCard({ stat }: { stat: ProtocolStat }) {
  const accent = accentClasses(stat.accent);

  return (
    <div className="relative overflow-hidden rounded-2xl border border-white/[0.08] bg-white/[0.04] p-5 shadow-[0_8px_32px_rgba(0,0,0,0.4)] backdrop-blur-xl">
      {/* Ambient corner glow tuned to the accent */}
      <div className={cn('pointer-events-none absolute -top-6 -right-6 h-24 w-24 rounded-full blur-2xl', accent.bg)} />
      <p className="relative text-xs uppercase tracking-[0.15em] text-white/40">{stat.label}</p>
      <div className="relative mt-2 flex items-baseline gap-1.5">
        <span className="text-3xl font-semibold tabular-nums text-white">{stat.value}</span>
        {stat.unit && <span className="text-sm text-white/40">{stat.unit}</span>}
      </div>
      {stat.caption && <p className={cn('relative mt-2 text-xs', accent.text)}>{stat.caption}</p>}
    </div>
  );
}
