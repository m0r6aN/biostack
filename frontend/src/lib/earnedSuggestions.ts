import type { Day7Review, InteractionFlag } from './types';

export type EarnedSuggestionType = 'tighten_stack' | 'clarify_signal' | 'balance_stack';

export type EarnedSuggestion = {
  type: EarnedSuggestionType;
  title: string;
  explanation: string;
  reasoning: string;
  actionLabel?: string;
};

export type StackShapeInput = {
  activeInputCount?: number;
  categoryCounts?: Record<string, number>;
};

export type EarnedSuggestionInput = {
  day7Review?: Day7Review | null;
  overlaps?: InteractionFlag[];
  stackShape?: StackShapeInput;
};

const BALANCE_STACK_MIN_INPUTS = 5;
const BALANCE_STACK_MIN_CATEGORY_COUNT = 4;
const BALANCE_STACK_MIN_CATEGORY_RATIO = 0.8;

export function getEarnedSuggestion(input: EarnedSuggestionInput): EarnedSuggestion | null {
  const day7Review = input.day7Review ?? null;
  const overlaps = input.overlaps ?? [];
  const stackShape = input.stackShape ?? {};
  const hasEarnedDay7Review = day7Review?.isEarned === true;
  const hasOverlap = overlaps.length > 0;
  const hasWeakOrUnclearSignal = hasEarnedDay7Review && isWeakOrUnclearReview(day7Review);

  if (hasOverlap || (hasWeakOrUnclearSignal && isPotentiallyMuddyStack(stackShape))) {
    return {
      type: 'tighten_stack',
      title: 'Clarify the stack signal',
      explanation: 'You may be tracking overlapping or crowded inputs, which can make the next signal harder to isolate.',
      reasoning: hasOverlap && hasWeakOrUnclearSignal
        ? 'Based on overlapping inputs and a weak or unclear 7-day review.'
        : hasOverlap
          ? 'Based on overlapping inputs in your current stack.'
          : 'Based on a weak or unclear 7-day review and a crowded current stack.',
      actionLabel: 'Review stack',
    };
  }

  if (hasWeakOrUnclearSignal) {
    return {
      type: 'clarify_signal',
      title: 'Keep the signal steady',
      explanation: 'The signal is still early or mixed, so continuing the same check-in pattern may make the next review more useful.',
      reasoning: 'Based on an unclear or weak 7-day review.',
      actionLabel: 'Keep tracking',
    };
  }

  if (hasEarnedDay7Review && isNarrowlyImbalancedStack(stackShape)) {
    return {
      type: 'balance_stack',
      title: 'Notice the stack shape',
      explanation: 'Your current stack is concentrated in one area. That may be fine; it is just context for reading future signals.',
      reasoning: 'Based on your current stack shape.',
      actionLabel: 'Not now',
    };
  }

  return null;
}

function isWeakOrUnclearReview(review: Day7Review) {
  const trends = [review.sleepTrend, review.energyTrend, review.recoveryTrend];
  const hasImproving = trends.includes('improving');
  const hasDeclining = trends.includes('declining');

  return review.signalStrength === 'weak' || review.nextStep === 'track_longer' || (hasImproving && hasDeclining);
}

function isPotentiallyMuddyStack(stackShape: StackShapeInput) {
  return (stackShape.activeInputCount ?? 0) >= 3;
}

function isNarrowlyImbalancedStack(stackShape: StackShapeInput) {
  const activeInputCount = stackShape.activeInputCount ?? 0;

  if (activeInputCount < BALANCE_STACK_MIN_INPUTS) {
    return false;
  }

  const categoryCounts = Object.values(stackShape.categoryCounts ?? {});
  const dominantCategoryCount = Math.max(0, ...categoryCounts);

  return (
    dominantCategoryCount >= BALANCE_STACK_MIN_CATEGORY_COUNT &&
    dominantCategoryCount / activeInputCount >= BALANCE_STACK_MIN_CATEGORY_RATIO
  );
}
