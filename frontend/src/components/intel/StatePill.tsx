import { DRIFT_TOKENS } from '@/styles/tokens';
import { cn } from '@/lib/utils';

interface StatePillProps {
  state: string;
  label?: string;
  className?: string;
}

export function StatePill({ state, label, className }: StatePillProps) {
  const t = DRIFT_TOKENS[state.toLowerCase()] ?? DRIFT_TOKENS.unknown;

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full border text-[11px] font-semibold px-2.5 py-1',
        t.bg, t.color, t.border,
        className,
      )}
    >
      <span className="text-[10px] leading-none">{t.emoji}</span>
      {label ?? t.label}
    </span>
  );
}
