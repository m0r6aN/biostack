import { getStatusColor } from '@/lib/utils';

interface CompoundStatusBadgeProps {
  status: string;
}

export function CompoundStatusBadge({ status }: CompoundStatusBadgeProps) {
  return (
    <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${getStatusColor(status)}`}>
      {status}
    </span>
  );
}
