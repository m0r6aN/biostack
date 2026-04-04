import { cn } from '@/lib/utils';

interface GlassPanelProps extends React.HTMLAttributes<HTMLDivElement> {
  /**
   * Glass intensity:
   *   light  — nav shell, secondary surfaces
   *   medium — header cards, standard panels (default)
   *   strong — hero analysis panel, feature focus areas
   */
  intensity?: 'light' | 'medium' | 'strong';
}

export function GlassPanel({
  intensity = 'medium',
  className,
  ...props
}: GlassPanelProps) {
  return (
    <div
      className={cn(
        'rounded-2xl shadow-brand-card',
        intensity === 'light'  && 'glass-light',
        intensity === 'medium' && 'glass-medium',
        intensity === 'strong' && 'glass-strong',
        className
      )}
      {...props}
    />
  );
}
