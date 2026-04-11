import { cn } from '@/lib/utils';

interface StatusChipProps {
  icon?: React.ReactNode;
  children: React.ReactNode;
  className?: string;
}

export function StatusChip({ icon, children, className }: StatusChipProps) {
  return (
    <div
      className={cn(
        'inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/[0.05] px-3 py-1.5 text-xs text-white/70',
        className
      )}
    >
      {icon && <span className="text-emerald-400">{icon}</span>}
      <span>{children}</span>
    </div>
  );
}
