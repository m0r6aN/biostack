import React from 'react';
import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ParsedProtocolSection } from '@/components/tools/analyzer/report/ParsedProtocolSection';
import type { ProtocolAnalyzerResult } from '@/lib/types';

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

const SMALL_PROTOCOL: ProtocolAnalyzerResult['protocol'] = [
  { compoundName: 'BPC-157', dose: 500, unit: 'mcg', frequency: 'daily', duration: '8 weeks' },
  { compoundName: 'TB-500', dose: 2, unit: 'mg', frequency: '2x weekly', duration: '8 weeks' },
];

const LARGE_PROTOCOL: ProtocolAnalyzerResult['protocol'] = [
  { compoundName: 'BPC-157', dose: 500, unit: 'mcg', frequency: 'daily', duration: '' },
  { compoundName: 'TB-500', dose: 2, unit: 'mg', frequency: '2x weekly', duration: '' },
  { compoundName: 'NAD+', dose: 100, unit: 'mg', frequency: 'daily', duration: '' },
  { compoundName: 'Semaglutide', dose: 0.25, unit: 'mg', frequency: 'weekly', duration: '' },
  { compoundName: 'MOTS-C', dose: 5, unit: 'mg', frequency: '3x weekly', duration: '' },
  { compoundName: 'CoQ10', dose: 200, unit: 'mg', frequency: 'daily', duration: '' },
  { compoundName: 'GHK-Cu', dose: 1, unit: 'mg', frequency: 'daily', duration: '' },
];

describe('ParsedProtocolSection', () => {
  it('renders the "Parsed Protocol" heading', () => {
    render(<ParsedProtocolSection result={makeResult({ protocol: SMALL_PROTOCOL })} />);
    expect(screen.getByText('Parsed Protocol')).toBeInTheDocument();
  });

  it('with <=6 items the table rows are visible by default (expanded)', () => {
    render(<ParsedProtocolSection result={makeResult({ protocol: SMALL_PROTOCOL })} />);
    // Both compound names should be visible (may appear in both table and mobile cards).
    expect(screen.getAllByText('BPC-157').length).toBeGreaterThan(0);
    expect(screen.getAllByText('TB-500').length).toBeGreaterThan(0);
  });

  it('with >6 items the table is collapsed by default', () => {
    render(<ParsedProtocolSection result={makeResult({ protocol: LARGE_PROTOCOL })} />);
    // First compound should NOT appear (collapsed).
    expect(screen.queryByText('GHK-Cu')).not.toBeInTheDocument();
  });

  it('renders compound names visible in expanded state', () => {
    render(<ParsedProtocolSection result={makeResult({ protocol: SMALL_PROTOCOL })} />);
    // BPC-157 appears at least once (table row + mobile card both render in jsdom).
    expect(screen.getAllByText('BPC-157').length).toBeGreaterThan(0);
  });

  it('renders null result without crashing', () => {
    render(<ParsedProtocolSection result={null} />);
    expect(screen.getByText('Parsed Protocol')).toBeInTheDocument();
  });

  it('shows blend badge when decomposedBlends is non-empty', () => {
    render(
      <ParsedProtocolSection
        result={makeResult({
          protocol: SMALL_PROTOCOL,
          decomposedBlends: [{ blendName: 'GLOW Blend', components: ['GHK-Cu', 'BPC-157'] }],
        })}
      />,
    );
    expect(screen.getByText('Blend detected')).toBeInTheDocument();
  });
});
