import { CONFIDENCE_TOKENS, type ConfidenceLevel } from '@/styles/tokens';
import { cn } from '@/lib/utils';

interface ConfidenceChipProps {
  level: ConfidenceLevel | string;
  showLabel?: boolean;
  size?: 'sm' | 'md';
  className?: string;
}

function normalizeLevel(raw: string): ConfidenceLevel {
  const map: Record<string, ConfidenceLevel> = {
    high: 'high',
    strong: 'high',
    moderate: 'moderate',
    medium: 'moderate',
    low: 'low',
    weak: 'low',
    insufficient: 'insufficient',
    none: 'insufficient',
    unknown: 'insufficient',
    'review-required': 'review-required',
    'review_required': 'review-required',
  };
  return map[raw.toLowerCase()] ?? 'insufficient';
}

export function ConfidenceChip({ level, showLabel = true, size = 'sm', className }: ConfidenceChipProps) {
  const key = normalizeLevel(level);
  const t = CONFIDENCE_TOKENS[key];

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full border font-medium',
        size === 'sm' ? 'text-[10px] px-2 py-0.5' : 'text-xs px-2.5 py-1',
        t.bg, t.text, t.border,
        className,
      )}
      title={t.label}
    >
      <span className={cn('rounded-full shrink-0', size === 'sm' ? 'w-1 h-1' : 'w-1.5 h-1.5', t.dot)} />
      {showLabel && t.label}
    </span>
  );
}
