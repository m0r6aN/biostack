import { Beaker, FlaskConical, Pill, Syringe } from 'lucide-react';

interface ScheduleIconProps {
  /** Icon key from ScheduleItem.icon. */
  iconKey?: string;
  className?: string;
}

/**
 * Renders the lucide icon for a schedule item. Declared at module scope and
 * returns concrete elements (no component-created-during-render), so it satisfies
 * the react-hooks/static-components rule. The source template used Font Awesome;
 * these are the app-wide lucide equivalents.
 */
export function ScheduleIcon({ iconKey, className }: ScheduleIconProps) {
  switch (iconKey) {
    case 'syringe':
      return <Syringe className={className} />;
    case 'flask':
      return <FlaskConical className={className} />;
    case 'pill':
      return <Pill className={className} />;
    case 'beaker':
    default:
      return <Beaker className={className} />;
  }
}
