import { GoalDefinition } from '@/lib/types';
import { getCategoryMeta } from '@/lib/goals';

interface GoalBadgeProps {
  goal: GoalDefinition;
  onRemove?: () => void;
  compact?: boolean;
}

export function GoalBadge({ goal, onRemove, compact }: GoalBadgeProps) {
  const meta = getCategoryMeta(goal.category);

  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full font-medium ${meta.pillClasses} ${
        compact ? 'text-xs px-1.5 py-0.5' : 'text-xs px-2.5 py-1'
      }`}
    >
      {goal.name}
      {onRemove && (
        <button
          type="button"
          onClick={(e) => {
            e.preventDefault();
            e.stopPropagation();
            onRemove();
          }}
          className="ml-0.5 hover:opacity-70 transition-opacity"
        >
          x
        </button>
      )}
    </span>
  );
}
