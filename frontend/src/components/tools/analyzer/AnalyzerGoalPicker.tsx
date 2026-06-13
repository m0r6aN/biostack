'use client';

import { GOAL_CATEGORIES, getGoalsByCategory } from '@/lib/goals';
import type { AnalyzerGoalSelection } from '@/lib/analyzerGoals';
import { trackAnalyzerEvent } from '@/lib/analyzerAnalytics';

const MAX_REFINEMENTS = 2;

export function AnalyzerGoalPicker({
  selection,
  onChange,
}: {
  selection: AnalyzerGoalSelection;
  onChange: (selection: AnalyzerGoalSelection) => void;
}) {
  const goalsByCategory = getGoalsByCategory();
  const refinements = selection.primaryCategory
    ? goalsByCategory.get(selection.primaryCategory) ?? []
    : [];

  function selectPrimary(categoryKey: string | null) {
    onChange({ primaryCategory: categoryKey, refinementGoalIds: [] });
    trackAnalyzerEvent('analyzer_goal_selected', { goal: categoryKey ?? 'none', isPrimary: true });
  }

  function toggleRefinement(goalId: string) {
    const current = selection.refinementGoalIds;
    if (current.includes(goalId)) {
      onChange({ ...selection, refinementGoalIds: current.filter((id) => id !== goalId) });
      return;
    }
    if (current.length >= MAX_REFINEMENTS) {
      return;
    }
    onChange({ ...selection, refinementGoalIds: [...current, goalId] });
    trackAnalyzerEvent('analyzer_goal_selected', { goal: goalId, isPrimary: false });
  }

  return (
    <div>
      <p className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-white/42">Goal</p>
      <div className="flex flex-wrap gap-2">
        <GoalChip
          label="Not sure yet"
          selected={selection.primaryCategory === null}
          onClick={() => selectPrimary(null)}
        />
        {GOAL_CATEGORIES.map((category) => (
          <GoalChip
            key={category.key}
            label={category.label}
            selected={selection.primaryCategory === category.key}
            onClick={() => selectPrimary(category.key)}
          />
        ))}
      </div>

      {refinements.length > 0 && (
        <div className="mt-4">
          <p className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-white/42">
            Refine (optional)
          </p>
          <div className="flex flex-wrap gap-2">
            {refinements.map((goal) => (
              <button
                key={goal.id}
                type="button"
                onClick={() => toggleRefinement(goal.id)}
                className={`rounded-full border px-3 py-1.5 text-left text-sm transition-colors ${
                  selection.refinementGoalIds.includes(goal.id)
                    ? 'border-emerald-300/45 bg-emerald-400/14 text-emerald-100'
                    : 'border-white/10 text-white/65 hover:border-white/20 hover:text-white'
                }`}
              >
                {goal.name}
              </button>
            ))}
          </div>
          <p className="mt-2 text-xs leading-5 text-white/38">
            Pick up to {MAX_REFINEMENTS}. Refinements sharpen the goal-aware alternatives.
          </p>
        </div>
      )}
    </div>
  );
}

function GoalChip({ label, selected, onClick }: { label: string; selected: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`rounded-full border px-4 py-2 text-sm font-semibold transition-colors ${
        selected
          ? 'border-emerald-300/45 bg-emerald-400/14 text-emerald-100'
          : 'border-white/10 text-white/65 hover:border-white/20 hover:text-white'
      }`}
    >
      {label}
    </button>
  );
}
