'use client';

import { getEvidenceTierColor } from '@/lib/utils';
import { HelpTip } from '@/components/ui/HelpTip';
import type { HelpTipKey } from '@/lib/helpTips';
import { METHODOLOGY_TIERS, tierKey, tierLabel } from '@/lib/evidenceTiers';

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
  return lower === 'mechanistic' || lower === 'theoretical' || lower === 'plausibletheory' ? 'mechanisticEvidence' : 'evidenceTier';
}

export function EvidenceTierBadge({ tier, variant = 'default' }: EvidenceTierBadgeProps) {
  const lower = tier.toLowerCase();
  const key = tierKey(tier);
  const map = variant === 'research' ? researchTierLabels : labels;
  const isMethodologyTier = METHODOLOGY_TIERS.some(t => t.key === key) || key === 'unknown' || key === 'notgraded';
  const label = isMethodologyTier ? tierLabel(tier) : (map[lower] ?? tier);
  return (
    <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${getEvidenceTierColor(key)}`}>
      <HelpTip tipKey={tierHelpKey(key)}>{label}</HelpTip>
    </span>
  );
}
