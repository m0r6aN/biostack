import React from 'react';
import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { NextSteps } from '@/components/tools/analyzer/report/NextSteps';
import type { ProtocolAnalyzerResult } from '@/lib/types';

// Mock next/link (same pattern as other analyzer tests).
vi.mock('next/link', () => ({
  default: ({ href, children, onClick, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement> & { href: string }) => (
    <a href={href} onClick={onClick} {...props}>{children}</a>
  ),
}));

function makeResult(overrides: Partial<ProtocolAnalyzerResult> = {}): ProtocolAnalyzerResult {
  return {
    protocol: [],
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
    extractedTextPreview: null,
    artifacts: [],
    ...overrides,
  };
}

function makeProps(overrides: Partial<React.ComponentProps<typeof NextSteps>> = {}): React.ComponentProps<typeof NextSteps> {
  return {
    result: makeResult(),
    savedAnalysisId: '',
    showSaveNotice: false,
    isAuthenticated: true,
    hasProfile: true,
    onSave: vi.fn(),
    onConvert: vi.fn(),
    onUnlockClicked: vi.fn(),
    ...overrides,
  };
}

describe('NextSteps', () => {
  it('renders all three CTAs', () => {
    render(<NextSteps {...makeProps()} />);
    expect(screen.getByRole('button', { name: /save analysis/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /convert to biostack protocol/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /unlock full analysis/i })).toBeInTheDocument();
  });

  it('clicking Save Analysis fires onSave', () => {
    const onSave = vi.fn();
    render(<NextSteps {...makeProps({ onSave })} />);
    fireEvent.click(screen.getByRole('button', { name: /save analysis/i }));
    expect(onSave).toHaveBeenCalledOnce();
  });

  it('clicking Convert to BioStack Protocol fires onConvert', () => {
    const onConvert = vi.fn();
    render(<NextSteps {...makeProps({ onConvert })} />);
    fireEvent.click(screen.getByRole('button', { name: /convert to biostack protocol/i }));
    expect(onConvert).toHaveBeenCalledOnce();
  });

  it('clicking Unlock full analysis fires onUnlockClicked', () => {
    const onUnlockClicked = vi.fn();
    render(<NextSteps {...makeProps({ onUnlockClicked })} />);
    fireEvent.click(screen.getByRole('link', { name: /unlock full analysis/i }));
    expect(onUnlockClicked).toHaveBeenCalledOnce();
  });

  it('Unlock full analysis link points to /pricing?intent=analyzer', () => {
    render(<NextSteps {...makeProps()} />);
    const link = screen.getByRole('link', { name: /unlock full analysis/i });
    expect(link).toHaveAttribute('href', '/pricing?intent=analyzer');
  });

  it('shows save notice when showSaveNotice=true', () => {
    render(<NextSteps {...makeProps({ showSaveNotice: true, savedAnalysisId: 'abc123' })} />);
    expect(screen.getByText(/analysis saved locally as abc123/i)).toBeInTheDocument();
  });

  it('does NOT show save notice when showSaveNotice=false', () => {
    render(<NextSteps {...makeProps({ showSaveNotice: false })} />);
    expect(screen.queryByText(/analysis saved locally/i)).not.toBeInTheDocument();
  });

  it('renders the profile nudge when hasProfile=false (authenticated)', () => {
    render(<NextSteps {...makeProps({ isAuthenticated: true, hasProfile: false })} />);
    expect(screen.getByText(/create a profile/i)).toBeInTheDocument();
    // Authenticated with no profile → link to /profiles
    const nudgeLink = screen.getByRole('link', { name: /create a profile/i });
    expect(nudgeLink).toHaveAttribute('href', '/profiles');
  });

  it('renders the profile nudge when isAuthenticated=false', () => {
    render(<NextSteps {...makeProps({ isAuthenticated: false, hasProfile: false })} />);
    expect(screen.getByText(/create a profile/i)).toBeInTheDocument();
    // Anonymous → sign-in link
    const nudgeLink = screen.getByRole('link', { name: /create a profile/i });
    expect(nudgeLink).toHaveAttribute('href', '/auth/signin?callbackUrl=/tools/analyzer');
  });

  it('does NOT render the profile nudge when hasProfile=true', () => {
    render(<NextSteps {...makeProps({ isAuthenticated: true, hasProfile: true })} />);
    expect(screen.queryByText(/create a profile/i)).not.toBeInTheDocument();
  });
});
