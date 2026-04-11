import { getEvidenceTierColor } from '@/lib/utils';

interface EvidenceTierBadgeProps {
  tier: string;
}

const labels: Record<string, string> = {
  strong: 'Strong Evidence',
  moderate: 'Moderate Evidence',
  limited: 'Limited Evidence',
  theoretical: 'Theoretical',
};

export function EvidenceTierBadge({ tier }: EvidenceTierBadgeProps) {
  return (
    <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${getEvidenceTierColor(tier)}`}>
      {labels[tier.toLowerCase()] ?? tier}
    </span>
  );
}
