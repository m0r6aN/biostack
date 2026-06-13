import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { AnalyzerGoalPicker } from '@/components/tools/analyzer/AnalyzerGoalPicker';
import { GOAL_CATEGORIES } from '@/lib/goals';

const noSelection = { primaryCategory: null, refinementGoalIds: [] };

describe('AnalyzerGoalPicker', () => {
  it('renders Not sure yet plus every category', () => {
    render(<AnalyzerGoalPicker selection={noSelection} onChange={() => {}} />);
    expect(screen.getByRole('button', { name: 'Not sure yet' })).toBeInTheDocument();
    for (const category of GOAL_CATEGORIES) {
      expect(screen.getByRole('button', { name: category.label })).toBeInTheDocument();
    }
  });

  it('hides refinements until a primary is chosen', () => {
    render(<AnalyzerGoalPicker selection={noSelection} onChange={() => {}} />);
    expect(screen.queryByText('Refine (optional)')).not.toBeInTheDocument();
  });

  it('selects a primary category and clears refinements', () => {
    const onChange = vi.fn();
    render(
      <AnalyzerGoalPicker
        selection={{ primaryCategory: 'recovery', refinementGoalIds: ['recovery-injury'] }}
        onChange={onChange}
      />,
    );
    fireEvent.click(screen.getByRole('button', { name: 'Energy & Metabolism' }));
    expect(onChange).toHaveBeenCalledWith({ primaryCategory: 'energy', refinementGoalIds: [] });
  });

  it('shows refinement goals for the selected category and toggles up to two', () => {
    const onChange = vi.fn();
    render(
      <AnalyzerGoalPicker
        selection={{ primaryCategory: 'energy', refinementGoalIds: ['energy-levels', 'energy-fat-loss'] }}
        onChange={onChange}
      />,
    );
    expect(screen.getByText('Refine (optional)')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /Enhance mitochondrial function/ }));
    expect(onChange).not.toHaveBeenCalled(); // max 2 — ignored

    fireEvent.click(screen.getByRole('button', { name: /Improve energy levels/ }));
    expect(onChange).toHaveBeenCalledWith({ primaryCategory: 'energy', refinementGoalIds: ['energy-fat-loss'] });
  });

  it('resets to no goal', () => {
    const onChange = vi.fn();
    render(
      <AnalyzerGoalPicker selection={{ primaryCategory: 'energy', refinementGoalIds: [] }} onChange={onChange} />,
    );
    fireEvent.click(screen.getByRole('button', { name: 'Not sure yet' }));
    expect(onChange).toHaveBeenCalledWith({ primaryCategory: null, refinementGoalIds: [] });
  });
});
