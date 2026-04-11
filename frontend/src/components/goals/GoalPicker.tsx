'use client';

import { useState } from 'react';
import { GoalDefinition } from '@/lib/types';
import { GOAL_CATEGORIES, getGoalsByCategory, getCategoryMeta } from '@/lib/goals';
import { cn } from '@/lib/utils';

interface GoalPickerProps {
  selectedGoalIds: string[];
  onChange: (ids: string[]) => void;
  customGoalNote?: string;
  onCustomGoalNoteChange?: (note: string) => void;
}

export function GoalPicker({
  selectedGoalIds,
  onChange,
  customGoalNote,
  onCustomGoalNoteChange,
}: GoalPickerProps) {
  const [collapsedCategories, setCollapsedCategories] = useState<Set<string>>(new Set());
  const goalsByCategory = getGoalsByCategory();

  const toggleCategory = (key: string) => {
    setCollapsedCategories(prev => {
      const next = new Set(prev);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  };

  const toggleGoal = (goalId: string) => {
    if (selectedGoalIds.includes(goalId)) {
      onChange(selectedGoalIds.filter(id => id !== goalId));
    } else {
      onChange([...selectedGoalIds, goalId]);
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <label className="block text-sm font-medium text-white/70">Goals</label>
        <span className="text-xs text-white/35">
          {selectedGoalIds.length} selected
        </span>
      </div>

      <div className="space-y-3">
        {GOAL_CATEGORIES.map(cat => {
          const meta = getCategoryMeta(cat.key);
          const goals = goalsByCategory.get(cat.key) ?? [];
          const isCollapsed = collapsedCategories.has(cat.key);
          const selectedInCategory = goals.filter(g => selectedGoalIds.includes(g.id)).length;

          return (
            <div key={cat.key} className="rounded-xl border border-white/[0.08] bg-white/[0.025] overflow-hidden">
              <button
                type="button"
                onClick={() => toggleCategory(cat.key)}
                className="w-full flex items-center justify-between px-4 py-2.5 hover:bg-white/[0.04] transition-all"
              >
                <div className="flex items-center gap-2">
                  <span className={`w-2 h-2 rounded-full ${meta.dotColor}`} />
                  <span className="text-sm font-medium text-white/80">{cat.label}</span>
                  {selectedInCategory > 0 && (
                    <span className={`text-xs px-1.5 py-0.5 rounded-full ${meta.pillClasses}`}>
                      {selectedInCategory}
                    </span>
                  )}
                </div>
                <span className="text-white/35 text-xs">{isCollapsed ? '+' : '-'}</span>
              </button>

              {!isCollapsed && (
                <div className="px-3 pb-3 grid grid-cols-1 sm:grid-cols-2 gap-2">
                  {goals.map(goal => {
                    const isSelected = selectedGoalIds.includes(goal.id);
                    return (
                      <button
                        key={goal.id}
                        type="button"
                        onClick={() => toggleGoal(goal.id)}
                        className={cn(
                          'text-left p-3 rounded-xl border transition-all',
                          isSelected
                            ? 'border-emerald-500/30 bg-emerald-500/10'
                            : 'border-white/[0.08] bg-white/[0.025] hover:bg-white/[0.04]'
                        )}
                      >
                        <p className={cn(
                          'text-sm font-medium',
                          isSelected ? 'text-emerald-300' : 'text-white/80'
                        )}>
                          {goal.name}
                        </p>
                        <p className="text-xs text-white/35 mt-0.5">{goal.description}</p>
                      </button>
                    );
                  })}
                </div>
              )}
            </div>
          );
        })}
      </div>

      {onCustomGoalNoteChange && (
        <div>
          <label className="block text-sm font-medium text-white/70 mb-2">
            Custom goal (optional)
          </label>
          <textarea
            value={customGoalNote ?? ''}
            onChange={(e) => onCustomGoalNoteChange(e.target.value)}
            placeholder="Anything else you're working toward..."
            rows={2}
            className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 focus:shadow-[0_0_0_1px_rgba(34,197,94,0.2)] transition-all"
          />
        </div>
      )}
    </div>
  );
}
