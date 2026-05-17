'use client';

import { getEvidenceTierColor } from '@/lib/utils';
import { HelpTip } from '@/components/ui/HelpTip';
import type { HelpTipKey } from '@/lib/helpTips';

interface EvidenceTierBadgeProps {
  tier: string;
  variant?: 'default' | 'research';
}

const labels: Record<string, string> = {
  strong: 'Strong Evidence',
  moderate: 'Moderate Evidence',
  limited: 'Limited Evidence',
  theoretical: 'Theoretical',
};

const researchTierLabels: Record<string, string> = {
  strong: 'Strong',
  moderate: 'Moderate',
  limited: 'Limited',
  insufficient: 'Insufficient',
  unknown: 'Unknown',
  anecdotal: 'Anecdotal',
};

function tierHelpKey(lower: string): HelpTipKey {
  return lower === 'mechanistic' || lower === 'theoretical' ? 'mechanisticEvidence' : 'evidenceTier';
}

export function EvidenceTierBadge({ tier, variant = 'default' }: EvidenceTierBadgeProps) {
  const lower = tier.toLowerCase();
  const map = variant === 'research' ? researchTierLabels : labels;
  const label = map[lower] ?? tier;
  return (
    <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${getEvidenceTierColor(lower)}`}>
      <HelpTip tipKey={tierHelpKey(lower)}>{label}</HelpTip>
    </span>
  );
}
