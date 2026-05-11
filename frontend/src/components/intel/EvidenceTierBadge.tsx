// Canonical EvidenceTierBadge — replaces the one in knowledge/ which can be deleted.
import { EVIDENCE_TIER_TOKENS } from '@/styles/tokens';
import { cn } from '@/lib/utils';

interface EvidenceTierBadgeProps {
  tier: string;
  short?: boolean;
  className?: string;
}

export function EvidenceTierBadge({ tier, short = false, className }: EvidenceTierBadgeProps) {
  const key = tier.toLowerCase();
  const t = EVIDENCE_TIER_TOKENS[key] ?? EVIDENCE_TIER_TOKENS.unknown;

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
