import { cn } from '@/lib/utils';

const pathwayStyles: Record<string, string> = {
  'Tissue repair': 'bg-emerald-500/10 text-emerald-300 border-emerald-400/20',
  'Inflammation': 'bg-amber-500/10 text-amber-300 border-amber-400/20',
  'Recovery': 'bg-sky-500/10 text-sky-300 border-sky-400/20',
  'Mobility': 'bg-cyan-500/10 text-cyan-300 border-cyan-400/20',
  'Mitochondrial': 'bg-violet-500/10 text-violet-300 border-violet-400/20',
  'Metabolism': 'bg-blue-500/10 text-blue-300 border-blue-400/20',
  'Exercise signaling': 'bg-fuchsia-500/10 text-fuchsia-300 border-fuchsia-400/20',
  'Cellular energy': 'bg-indigo-500/10 text-indigo-300 border-indigo-400/20',
  'Longevity': 'bg-teal-500/10 text-teal-300 border-teal-400/20',
  'Appetite': 'bg-orange-500/10 text-orange-300 border-orange-400/20',
  'Glucose regulation': 'bg-yellow-500/10 text-yellow-300 border-yellow-400/20',
};

interface PathwayBadgeProps {
  label: string;
  className?: string;
}

export function PathwayBadge({ label, className }: PathwayBadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-medium',
        pathwayStyles[label] ?? 'bg-white/10 text-white/70 border-white/15',
        className
      )}
    >
      {label}
    </span>
  );
}

export { pathwayStyles };
