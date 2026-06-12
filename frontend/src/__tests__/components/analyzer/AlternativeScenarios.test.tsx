import React from 'react';
import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { AlternativeScenarios } from '@/components/tools/analyzer/report/AlternativeScenarios';
import type { ProtocolAnalyzerResult } from '@/lib/types';
import type { OptimizedProtocolView } from '@/components/tools/analyzer/analyzerView';

function makeResult(overrides: Partial<ProtocolAnalyzerResult> = {}): ProtocolAnalyzerResult {
  return {
    protocol: [],
    score: 70,
    scoreExplanation: { baseScore: 60, synergy: 10, redundancy: 0, interference: 0 },
    issues: [],
    suggestions: [],
    decomposedBlends: [],
    unknownCompounds: [],
    counterfactuals: {
      baselineScore: 70,
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
  protocol: [],
  score: 82,
  removed: ['TB-500'],
};

describe('AlternativeScenarios', () => {
  it('renders null when optimized=null and no counterfactuals', () => {
    const { container } = render(
      <AlternativeScenarios result={makeResult()} optimized={null} />,
    );
    expect(container.firstChild).toBeNull();
  });

  it('renders "Alternative scenarios" heading when an optimized view is passed', () => {
    render(<AlternativeScenarios result={makeResult()} optimized={OPTIMIZED} />);
    expect(screen.getByText('Alternative scenarios')).toBeInTheDocument();
  });

  it('renders the optimized label card', () => {
    render(<AlternativeScenarios result={makeResult()} optimized={OPTIMIZED} />);
    expect(screen.getByText('BioStack simplified arrangement')).toBeInTheDocument();
  });

  it('renders "Alternative scenarios" when primaryRemoval is present', () => {
    render(
      <AlternativeScenarios
        result={makeResult({
          counterfactuals: {
            baselineScore: 70,
            bestRemoveOne: [
              {
                removedCompound: 'TB-500',
                variantScore: 78,
                deltaScore: 8,
                deltaPercent: 11,
                verdict: 'Positive',
                recommendation: 'Remove TB-500.',
              },
            ],
            bestSwapOne: [],
            bestSimplifiedProtocol: null,
            goalAwareOptions: [],
          },
        })}
        optimized={null}
      />,
    );
    expect(screen.getByText('Alternative scenarios')).toBeInTheDocument();
    expect(screen.getByText('Remove-one scenario')).toBeInTheDocument();
  });

  it('renders "Alternative scenarios" when primarySwap is present', () => {
    render(
      <AlternativeScenarios
        result={makeResult({
          counterfactuals: {
            baselineScore: 70,
            bestRemoveOne: [],
            bestSwapOne: [
              {
                originalCompound: 'NAD+',
                candidateCompound: 'MOTS-C',
                baselineScore: 70,
                variantScore: 76,
                deltaScore: 6,
                deltaPercent: 8,
                verdict: 'Positive',
                recommendation: 'Consider swapping.',
                reasons: [],
              },
            ],
            bestSimplifiedProtocol: null,
            goalAwareOptions: [],
          },
        })}
        optimized={null}
      />,
    );
    expect(screen.getByText('Alternative scenarios')).toBeInTheDocument();
    expect(screen.getByText('What-if comparison')).toBeInTheDocument();
  });
});
