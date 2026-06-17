import React from 'react';
import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { ScoreHero } from '@/components/tools/analyzer/report/ScoreHero';
import type { ProtocolAnalyzerResult } from '@/lib/types';

// Mock framer-motion to avoid jsdom SVG animation issues.
vi.mock('framer-motion', () => ({
  motion: {
    path: ({ children, ...props }: React.SVGProps<SVGPathElement>) => <path {...props}>{children}</path>,
    circle: ({ children, ...props }: React.SVGProps<SVGCircleElement>) => <circle {...props}>{children}</circle>,
    span: ({ children, ...props }: React.HTMLAttributes<HTMLSpanElement>) => <span {...props}>{children}</span>,
  },
  useReducedMotion: () => true, // always reduced so tests are synchronous
  AnimatePresence: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

// ── Fixture ───────────────────────────────────────────────────────────────────

function makeResult(overrides: Partial<ProtocolAnalyzerResult> = {}): ProtocolAnalyzerResult {
  return {
    protocol: [],
    score: 75,
    scoreExplanation: { baseScore: 60, synergy: 10, redundancy: -5, interference: -3 },
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

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('ScoreHero', () => {
  it('renders the numeric score as plain text', () => {
    render(
      <ScoreHero
        result={makeResult({ score: 75 })}
        scoreInsight="Some insight text"
        whatThisMeans="Some what-this-means text"
      />,
    );

    // The gauge container has aria-label with the numeric score.
    expect(screen.getByLabelText(/BioStack score 75 out of 100/i)).toBeInTheDocument();
    // The score itself renders as a plain span — queryable without animation.
    expect(screen.getByText('75')).toBeInTheDocument();
    // The denominator.
    expect(screen.getByText('/ 100')).toBeInTheDocument();
  });

  it('renders the score label for the given score', () => {
    render(
      <ScoreHero
        result={makeResult({ score: 75 })}
        scoreInsight="Insight"
        whatThisMeans=""
      />,
    );
    // score 75 → getScoreLabel returns 'Strong fit'
    expect(screen.getByText('Strong fit')).toBeInTheDocument();
  });

  it('renders the scoreInsight sentence', () => {
    render(
      <ScoreHero
        result={makeResult({ score: 75 })}
        scoreInsight="Useful coverage is present."
        whatThisMeans=""
      />,
    );
    expect(screen.getByText('Useful coverage is present.')).toBeInTheDocument();
  });

  it('renders whatThisMeans callout when non-empty', () => {
    render(
      <ScoreHero
        result={makeResult({ score: 75 })}
        scoreInsight="Insight"
        whatThisMeans="This stack may be harder to evaluate."
      />,
    );
    expect(screen.getByText('What this means')).toBeInTheDocument();
    expect(screen.getByText('This stack may be harder to evaluate.')).toBeInTheDocument();
  });

  it('does NOT render the whatThisMeans callout when empty', () => {
    render(
      <ScoreHero
        result={makeResult({ score: 75 })}
        scoreInsight="Insight"
        whatThisMeans=""
      />,
    );
    expect(screen.queryByText('What this means')).not.toBeInTheDocument();
  });

  it('clicking "Why this score?" reveals the four ScoreChips', () => {
    render(
      <ScoreHero
        result={makeResult({ score: 75 })}
        scoreInsight="Insight"
        whatThisMeans=""
      />,
    );

    // Chips should not be visible yet.
    expect(screen.queryByText('Base')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /why this score/i }));

    // All four chip labels should now be present.
    expect(screen.getByText('Base')).toBeInTheDocument();
    // HelpTip wraps 'Synergy', 'Redundancy', 'Interference' in a span with underline;
    // the text content is still queryable.
    expect(screen.getByText('Synergy')).toBeInTheDocument();
    expect(screen.getByText('Redundancy')).toBeInTheDocument();
    expect(screen.getByText('Interference')).toBeInTheDocument();
  });

  it('chip values match the scoreExplanation', () => {
    render(
      <ScoreHero
        result={makeResult({
          score: 75,
          scoreExplanation: { baseScore: 60, synergy: 10, redundancy: -5, interference: -3 },
        })}
        scoreInsight="Insight"
        whatThisMeans=""
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /why this score/i }));

    // Base value: 60 — ScoreChip renders +60 because value > 0
    expect(screen.getByText('+60')).toBeInTheDocument();
    // Synergy: +10
    expect(screen.getByText('+10')).toBeInTheDocument();
    // Redundancy: -5
    expect(screen.getByText('-5')).toBeInTheDocument();
    // Interference: -3
    expect(screen.getByText('-3')).toBeInTheDocument();
  });

  it('clicking toggle again hides the chips', () => {
    render(
      <ScoreHero
        result={makeResult({ score: 75 })}
        scoreInsight="Insight"
        whatThisMeans=""
      />,
    );

    const toggleBtn = screen.getByRole('button', { name: /why this score/i });
    fireEvent.click(toggleBtn);
    expect(screen.getByText('Base')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /hide score breakdown/i }));
    expect(screen.queryByText('Base')).not.toBeInTheDocument();
  });
});
