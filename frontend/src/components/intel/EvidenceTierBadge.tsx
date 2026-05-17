'use client';

import { EVIDENCE_TIER_TOKENS } from '@/styles/tokens';
import { cn } from '@/lib/utils';
import { HelpTip } from '@/components/ui/HelpTip';
import type { HelpTipKey } from '@/lib/helpTips';

interface EvidenceTierBadgeProps {
  tier: string;
  short?: boolean;
  className?: string;
}

function tierHelpKey(key: string): HelpTipKey {
  return key === 'mechanistic' ? 'mechanisticEvidence' : 'evidenceTier';
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
    >
      <HelpTip tipKey={tierHelpKey(key)}>{short ? t.short : t.label}</HelpTip>
    </span>
  );
}
