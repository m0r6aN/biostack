import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { ReportSummaryBar } from '@/components/tools/analyzer/ReportSummaryBar';
import type { ProtocolAnalyzerResult } from '@/lib/types';

describe('ReportSummaryBar', () => {
  const createMockResult = (protocolLength: number = 2): ProtocolAnalyzerResult => {
    const baseResult: Partial<ProtocolAnalyzerResult> = {
      protocol: Array.from({ length: protocolLength }, (_, i) => ({
        compoundName: `Compound ${i + 1}`,
        dose: 100,
        unit: 'mg',
        frequency: 'daily',
        duration: '8 weeks',
      })),
      inputType: 'Paste',
      sourceName: null,
      score: 75,
      scoreExplanation: { baseScore: 75, synergy: 10, redundancy: -5, interference: 0 },
      issues: [],
      suggestions: [],
      decomposedBlends: [],
      unknownCompounds: [],
      counterfactuals: {
        baselineScore: 75,
        bestRemoveOne: [],
        bestSwapOne: [],
        bestSimplifiedProtocol: null,
        goalAwareOptions: [],
      },
      extractionWarnings: [],
      parserWarnings: [],
      lowConfidenceExtraction: false,
      extractedTextPreview: null,
      artifacts: [],
    };
    return baseResult as ProtocolAnalyzerResult;
  };

  it('renders source type, compound count, and goal label', () => {
    const result = createMockResult(2);
    const onEdit = vi.fn();
    render(
      <ReportSummaryBar result={result} primaryCategory="energy" onEdit={onEdit} />,
    );

    expect(screen.getByText(/Pasted text/)).toBeInTheDocument();
    expect(screen.getByText(/2 compounds/)).toBeInTheDocument();
    expect(screen.getByText(/Energy & Metabolism/)).toBeInTheDocument();
  });

  it('shows singular compound when protocol has 1 item', () => {
    const result = createMockResult(1);
    const onEdit = vi.fn();
    render(
      <ReportSummaryBar result={result} primaryCategory="energy" onEdit={onEdit} />,
    );

    expect(screen.getByText(/1 compound/)).toBeInTheDocument();
  });

  it('shows "No goal selected" when primaryCategory is null', () => {
    const result = createMockResult(2);
    const onEdit = vi.fn();
    render(
      <ReportSummaryBar result={result} primaryCategory={null} onEdit={onEdit} />,
    );

    expect(screen.getByText(/No goal selected/)).toBeInTheDocument();
  });

  it('calls onEdit when Edit button is clicked', () => {
    const result = createMockResult(2);
    const onEdit = vi.fn();
    render(
      <ReportSummaryBar result={result} primaryCategory="energy" onEdit={onEdit} />,
    );

    const editButton = screen.getByRole('button', { name: 'Edit' });
    fireEvent.click(editButton);
    expect(onEdit).toHaveBeenCalledTimes(1);
  });

  it('displays the correct category label for different categories', () => {
    const result = createMockResult(3);
    const onEdit = vi.fn();

    const { rerender } = render(
      <ReportSummaryBar result={result} primaryCategory="recovery" onEdit={onEdit} />,
    );
    expect(screen.getByText(/Recovery & Repair/)).toBeInTheDocument();

    rerender(
      <ReportSummaryBar result={result} primaryCategory="cognitive" onEdit={onEdit} />,
    );
    expect(screen.getByText(/Cognitive & Neurological/)).toBeInTheDocument();

    rerender(
      <ReportSummaryBar result={result} primaryCategory="longevity" onEdit={onEdit} />,
    );
    expect(screen.getByText(/Longevity & Aging/)).toBeInTheDocument();
  });
});
