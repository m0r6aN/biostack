import { REGULATORY_TOKENS } from '@/styles/tokens';
import { cn } from '@/lib/utils';

interface RegulatoryBoundaryBadgeProps {
  boundary: string;
  short?: boolean;
  className?: string;
}

export function RegulatoryBoundaryBadge({ boundary, short = false, className }: RegulatoryBoundaryBadgeProps) {
  const key = boundary.toLowerCase().replace(/\s+/g, '-');
  const t = REGULATORY_TOKENS[key] ?? REGULATORY_TOKENS.unknown;

  return (
    <span
      className={cn(
        'inline-flex items-center text-[11px] font-medium px-2.5 py-1 rounded-full border',
        t.bg, t.color, t.border,
        className,
      )}
      title={t.label}
    >
      {short ? t.short : t.label}
    </span>
  );
}
