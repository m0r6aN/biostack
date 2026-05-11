import { cn } from '@/lib/utils';

interface DeltaBadgeProps {
  value: number;
  unit?: string;
  reason?: string;
  size?: 'sm' | 'md';
  className?: string;
}

export function DeltaBadge({ value, unit = '', reason, size = 'sm', className }: DeltaBadgeProps) {
  const positive = value > 0;
  const neutral = value === 0;

  const color = neutral
    ? 'text-white/40 border-white/10 bg-white/5'
    : positive
    ? 'text-emerald-300 border-emerald-400/20 bg-emerald-500/10'
    : 'text-rose-300 border-rose-400/20 bg-rose-500/10';

  const arrow = neutral ? '–' : positive ? '▲' : '▼';
  const formatted = neutral ? '0' : `${positive ? '+' : ''}${value.toFixed(1)}`;

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full border font-medium',
        size === 'sm' ? 'text-[10px] px-2 py-0.5' : 'text-xs px-2.5 py-1',
        color,
        className,
      )}
      title={reason}
    >
      <span className="text-[9px] leading-none">{arrow}</span>
      {formatted}{unit}
      {reason && <span className="hidden sm:inline text-white/40 font-normal">· {reason}</span>}
    </span>
  );
}
