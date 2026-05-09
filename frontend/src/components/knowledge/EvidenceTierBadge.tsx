import { getEvidenceTierColor } from '@/lib/utils';

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

export function EvidenceTierBadge({ tier, variant = 'default' }: EvidenceTierBadgeProps) {
  const lower = tier.toLowerCase();
  const map = variant === 'research' ? researchTierLabels : labels;
  return (
    <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${getEvidenceTierColor(lower)}`}>
      {map[lower] ?? tier}
    </span>
  );
}
