import React from 'react';
import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { FindingsSection } from '@/components/tools/analyzer/report/FindingsSection';
import type { ProtocolAnalyzerResult } from '@/lib/types';

function makeResult(overrides: Partial<ProtocolAnalyzerResult> = {}): ProtocolAnalyzerResult {
  return {
    protocol: [
      { compoundName: 'BPC-157', dose: 500, unit: 'mcg', frequency: 'daily', duration: '8 weeks' },
    ],
    score: 72,
    scoreExplanation: { baseScore: 60, synergy: 12, redundancy: 0, interference: 0 },
    issues: [],
    suggestions: [],
    decomposedBlends: [],
    unknownCompounds: [],
    counterfactuals: {
      baselineScore: 72,
      bestRemoveOne: [],
      bestSwapOne: [],
      bestSimplifiedProtocol: null,
      goalAwareOptions: [],
    },
    inputType: 'Paste',
    sourceName: null,
    extractionWarnings: [],
    parserWarnings: [],
    lowConfidenceExtraction: false,
    extractedTextPreview: 'BPC-157 500mcg daily',
    artifacts: [],
    ...overrides,
  };
}

describe('FindingsSection', () => {
  it('renders "What BioStack found" heading', () => {
    render(
      <FindingsSection
        result={makeResult()}
        showExtractedText={false}
        onToggleExtractedText={vi.fn()}
      />,
    );
    // The heading appears twice (section title + FindingList title), both should be in DOM.
    const headings = screen.getAllByText('What BioStack found');
    expect(headings.length).toBeGreaterThan(0);
  });

  it('renders the confidence label', () => {
    render(
      <FindingsSection
        result={makeResult()}
        showExtractedText={false}
        onToggleExtractedText={vi.fn()}
      />,
    );
    // With no warnings and no lowConfidence → 'High'
    expect(screen.getByText('High')).toBeInTheDocument();
  });

  it('shows Source type label from sourceTypeLabel', () => {
    render(
      <FindingsSection
        result={makeResult({ inputType: 'Paste' })}
        showExtractedText={false}
        onToggleExtractedText={vi.fn()}
      />,
    );
    expect(screen.getByText('Pasted text')).toBeInTheDocument();
  });

  it('shows items inferred count', () => {
    render(
      <FindingsSection
        result={makeResult()}
        showExtractedText={false}
        onToggleExtractedText={vi.fn()}
      />,
    );
    // 1 protocol item, 0 unknown → "1 found, 1 normalized"
    expect(screen.getByText('1 found, 1 normalized')).toBeInTheDocument();
  });

  it('calls onToggleExtractedText when the toggle button is clicked', () => {
    const onToggle = vi.fn();
    render(
      <FindingsSection
        result={makeResult()}
        showExtractedText={false}
        onToggleExtractedText={onToggle}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /view extracted text/i }));
    expect(onToggle).toHaveBeenCalledOnce();
  });

  it('shows extracted text preview when showExtractedText=true', () => {
    render(
      <FindingsSection
        result={makeResult({ extractedTextPreview: 'BPC-157 500mcg daily' })}
        showExtractedText={true}
        onToggleExtractedText={vi.fn()}
      />,
    );
    expect(screen.getByText('BPC-157 500mcg daily')).toBeInTheDocument();
  });

  it('shows low-confidence warning when lowConfidenceExtraction=true', () => {
    render(
      <FindingsSection
        result={makeResult({ lowConfidenceExtraction: true })}
        showExtractedText={false}
        onToggleExtractedText={vi.fn()}
      />,
    );
    expect(screen.getByText(/low-confidence extraction/i)).toBeInTheDocument();
  });
});
