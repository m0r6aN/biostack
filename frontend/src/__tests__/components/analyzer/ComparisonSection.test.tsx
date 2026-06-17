import React from 'react';
import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ComparisonSection } from '@/components/tools/analyzer/report/ComparisonSection';
import type { ProtocolAnalyzerResult } from '@/lib/types';
import type { OptimizedProtocolView } from '@/components/tools/analyzer/analyzerView';

function makeResult(overrides: Partial<ProtocolAnalyzerResult> = {}): ProtocolAnalyzerResult {
  return {
    protocol: [
      { compoundName: 'BPC-157', dose: 500, unit: 'mcg', frequency: 'daily', duration: '' },
      { compoundName: 'TB-500', dose: 2, unit: 'mg', frequency: '2x weekly', duration: '' },
    ],
    score: 68,
    scoreExplanation: { baseScore: 60, synergy: 8, redundancy: 0, interference: 0 },
    issues: [],
    suggestions: [],
    decomposedBlends: [],
    unknownCompounds: [],
    counterfactuals: {
      baselineScore: 68,
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
    extractedTextPreview: null,
    artifacts: [],
    ...overrides,
  };
}

const OPTIMIZED: OptimizedProtocolView = {
  label: 'BioStack simplified arrangement',
  protocol: [{ compoundName: 'BPC-157', dose: 500, unit: 'mcg', frequency: 'daily', duration: '' }],
  score: 80,
  removed: ['TB-500'],
};

describe('ComparisonSection', () => {
  it('renders "Original vs BioStack alternative" heading', () => {
    render(<ComparisonSection result={makeResult()} optimized={OPTIMIZED} />);
    expect(screen.getByText('Original vs BioStack alternative')).toBeInTheDocument();
  });

  it('renders the optimized label when an optimized view is passed', () => {
    render(<ComparisonSection result={makeResult()} optimized={OPTIMIZED} />);
    expect(screen.getByText('BioStack simplified arrangement')).toBeInTheDocument();
  });

  it('shows both original and alternative protocol columns', () => {
    render(<ComparisonSection result={makeResult()} optimized={OPTIMIZED} />);
    expect(screen.getByText('Original protocol')).toBeInTheDocument();
    expect(screen.getByText('BioStack simplified arrangement')).toBeInTheDocument();
  });

  it('renders with null optimized without crashing', () => {
    render(<ComparisonSection result={makeResult()} optimized={null} />);
    expect(screen.getByText('Original vs BioStack alternative')).toBeInTheDocument();
  });

  it('shows model delta in the score badge', () => {
    render(<ComparisonSection result={makeResult({ score: 68 })} optimized={OPTIMIZED} />);
    // delta = 80 - 68 = +12
    expect(screen.getByText(/model delta \+12/i)).toBeInTheDocument();
  });
});
