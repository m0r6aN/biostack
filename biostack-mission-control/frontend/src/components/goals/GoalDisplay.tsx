import { GoalDefinition } from '@/lib/types';
import { GOAL_CATEGORIES, getCategoryMeta } from '@/lib/goals';
import { GoalBadge } from './GoalBadge';

interface GoalDisplayProps {
  goals: GoalDefinition[];
}

export function GoalDisplay({ goals }: GoalDisplayProps) {
  if (goals.length === 0) {
    return <p className="text-sm text-white/50">No goals set</p>;
  }

  const grouped = new Map<string, GoalDefinition[]>();
  for (const goal of goals) {
    const list = grouped.get(goal.category) ?? [];
    list.push(goal);
    grouped.set(goal.category, list);
  }

  return (
    <div className="space-y-3">
      {GOAL_CATEGORIES.filter(cat => grouped.has(cat.key)).map(cat => {
        const meta = getCategoryMeta(cat.key);
        const categoryGoals = grouped.get(cat.key)!;
        return (
          <div key={cat.key}>
            <div className="flex items-center gap-2 mb-1.5">
              <span className={`w-2 h-2 rounded-full ${meta.dotColor}`} />
              <span className="text-xs uppercase tracking-[0.15em] text-white/40">{cat.label}</span>
            </div>
            <div className="flex flex-wrap gap-1.5">
              {categoryGoals.map(goal => (
                <GoalBadge key={goal.id} goal={goal} />
              ))}
            </div>
          </div>
        );
      })}
    </div>
  );
}
