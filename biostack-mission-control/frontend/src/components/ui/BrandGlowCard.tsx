import { cn } from '@/lib/utils';

interface BrandGlowCardProps extends React.HTMLAttributes<HTMLDivElement> {
  /**
   * Glow intensity:
   *   soft   — subtle brand ring, standard use (default)
   *   strong — prominent glow, hero / call-to-action contexts
   */
  glow?: 'soft' | 'strong';
}

/**
 * Opinionated card with brand green glow ring.
 * Use for the hero analysis panel, active-state cards, or CTA surfaces.
 * Do not use for every card — reserve for meaningful focal points.
 */
export function BrandGlowCard({
  glow = 'soft',
  className,
  ...props
}: BrandGlowCardProps) {
  return (
    <div
      className={cn(
        'glass-strong rounded-2xl',
        glow === 'soft'   && 'shadow-brand-glow',
        glow === 'strong' && 'shadow-brand-glow-strong',
        className
      )}
      {...props}
    />
  );
}
