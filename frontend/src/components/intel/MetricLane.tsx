import { cn } from '@/lib/utils';

interface MetricLaneEvent {
  date: string;
  value: number | null;
  label?: string;
  tag?: string;
  tagColor?: string;
}

interface MetricLaneProps {
  label: string;
  events: MetricLaneEvent[];
  min?: number;
  max?: number;
  color?: string;
  className?: string;
}

export function MetricLane({ label, events, min = 1, max = 10, color = 'emerald', className }: MetricLaneProps) {
  const filled = events.filter((e) => e.value !== null);
  const range = max - min || 1;

  const colorMap: Record<string, { line: string; dot: string; bar: string }> = {
    emerald: { line: 'stroke-emerald-400/60', dot: 'fill-emerald-400', bar: 'bg-emerald-400' },
    blue: { line: 'stroke-blue-400/60', dot: 'fill-blue-400', bar: 'bg-blue-400' },
    amber: { line: 'stroke-amber-400/60', dot: 'fill-amber-400', bar: 'bg-amber-400' },
    rose: { line: 'stroke-rose-400/60', dot: 'fill-rose-400', bar: 'bg-rose-400' },
    purple: { line: 'stroke-purple-400/60', dot: 'fill-purple-400', bar: 'bg-purple-400' },
  };

  const c = colorMap[color] ?? colorMap.emerald;

  if (filled.length === 0) {
    return (
      <div className={cn('flex items-center gap-3 h-10', className)}>
        <span className="text-[10px] font-medium text-white/30 w-24 shrink-0 truncate">{label}</span>
        <span className="text-[10px] text-white/20 italic">No data</span>
      </div>
    );
  }

  const w = 240;
  const h = 32;
  const pts = filled.map((e, i) => {
    const x = filled.length > 1 ? (i / (filled.length - 1)) * w : w / 2;
    const y = h - ((((e.value ?? min) - min) / range) * h * 0.85 + h * 0.075);
    return { x, y, e };
  });

  const d = pts.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ');

  return (
    <div className={cn('flex items-center gap-3', className)}>
      <span className="text-[10px] font-medium text-white/40 w-24 shrink-0 truncate">{label}</span>
      <div className="relative flex-1 min-w-0">
        <svg width="100%" viewBox={`0 0 ${w} ${h}`} className="overflow-visible" preserveAspectRatio="none">
          <polyline points={pts.map((p) => `${p.x},${p.y}`).join(' ')} fill="none" strokeWidth="1.5" className={c.line} />
          {pts.map((p, i) => (
            <circle key={i} cx={p.x} cy={p.y} r="2.5" className={c.dot}>
              {p.e.label && <title>{p.e.label}: {p.e.value}</title>}
            </circle>
          ))}
        </svg>
      </div>
      <span className="font-mono text-[10px] text-white/50 w-6 text-right shrink-0">
        {filled[filled.length - 1]?.value ?? '—'}
      </span>
    </div>
  );
}
