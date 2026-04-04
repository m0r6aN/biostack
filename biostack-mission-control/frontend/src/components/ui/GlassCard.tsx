import { cn } from '@/lib/utils';

interface GlassCardProps {
  children: React.ReactNode;
  className?: string;
  variant?: 'default' | 'hero' | 'base';
}

/**
 * Glass surface system:
 * - default: Standard elevated glass (nav, header, key panels)
 * - hero:    Feature-focus glass with stronger blur + deeper shadow
 * - base:    Solid elevated card (lists, standard panels)
 */
export function GlassCard({ children, className, variant = 'default' }: GlassCardProps) {
  return (
    <div
      className={cn(
        variant === 'hero' &&
          'rounded-2xl border border-white/15 bg-white/[0.07] shadow-[0_8px_40px_rgba(0,0,0,0.6)] backdrop-blur-2xl',
        variant === 'default' &&
          'rounded-2xl border border-white/10 bg-white/[0.04] shadow-[0_8px_40px_rgba(0,0,0,0.45)] backdrop-blur-xl',
        variant === 'base' &&
          'rounded-2xl border border-white/[0.08] bg-[#121923]/90 shadow-[0_8px_24px_rgba(0,0,0,0.35)]',
        className
      )}
    >
      {children}
    </div>
  );
}
