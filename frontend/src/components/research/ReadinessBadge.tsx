import type { PromotionReadiness } from '@/lib/research/types';

interface ReadinessBadgeProps {
  readiness: PromotionReadiness | string;
}

const config: Record<string, { label: string; classes: string }> = {
  'research-requested':       { label: 'Research Requested', classes: 'bg-violet-500/15 text-violet-300 border-violet-400/20' },
  'blocked':                 { label: 'Blocked',         classes: 'bg-rose-500/15 text-rose-400 border-rose-400/20' },
  'review-required':         { label: 'Review Required', classes: 'bg-amber-500/15 text-amber-400 border-amber-400/20' },
  'candidate-for-promotion': { label: 'Candidate',       classes: 'bg-emerald-500/15 text-emerald-400 border-emerald-400/20' },
};

export function ReadinessBadge({ readiness }: ReadinessBadgeProps) {
  const { label, classes } = config[readiness] ?? { label: readiness, classes: 'bg-white/10 text-white/50 border-white/15' };
  return (
    <span className={`text-[9px] font-bold tracking-widest uppercase px-2 py-1 rounded-full border ${classes}`}>
      {label}
    </span>
  );
}
