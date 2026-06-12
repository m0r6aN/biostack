import { ProtocolAnalyzerExperience } from '@/components/tools/ProtocolAnalyzerExperience';
import { ANALYZER_ANALYSIS_HISTORY_KEY, ANALYZER_PROTOCOL_DRAFT_KEY } from '@/lib/analyzerStorage';
import type { ProtocolAnalyzerResult } from '@/lib/types';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const pushMock = vi.fn();

vi.mock('next/navigation', () => ({
  useRouter: () => ({
    push: pushMock,
  }),
}));

vi.mock('@/lib/AuthProvider', () => ({
  useAuth: () => ({
    user: null,
    loading: false,
  }),
}));

describe('ProtocolAnalyzerExperience', () => {
  beforeEach(() => {
    window.localStorage.clear();
    pushMock.mockClear();
  });

  it('renders multi-source analyzer tabs', () => {
    render(<ProtocolAnalyzerExperience />);

    expect(screen.getByRole('button', { name: 'Paste' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Upload' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Scan' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Link' })).toBeInTheDocument();
    expect(screen.getByText('Analyze any protocol, in any format you actually have')).toBeInTheDocument();
  });

  it('renders a restored result without counterfactuals instead of crashing', async () => {
    const resultWithoutCounterfactuals = {
      protocol: [
        {
          compoundName: 'BPC-157',
          dose: 500,
          unit: 'mcg',
          frequency: 'daily',
          duration: '8 weeks',
          goal: 'healing',
          sourceText: 'BPC-157 500mcg daily',
        },
      ],
      score: 62,
      scoreExplanation: {
        positives: ['Compact protocol'],
        penalties: [],
      },
      issues: [],
      suggestions: [],
      decomposedBlends: [],
      unknownCompounds: [],
      inputType: 'Paste',
      sourceName: null,
      extractionWarnings: [],
      parserWarnings: [],
      lowConfidenceExtraction: false,
      extractedTextPreview: 'BPC-157 500mcg daily',
      artifacts: [],
    } as unknown as ProtocolAnalyzerResult;

    window.localStorage.setItem(
      'biostack.analyzer.session.v3',
      JSON.stringify({
        mode: 'Paste',
        inputText: 'BPC-157 500mcg daily',
        goal: 'healing',
        result: resultWithoutCounterfactuals,
      }),
    );

    render(<ProtocolAnalyzerExperience />);

    await waitFor(() => {
      expect(screen.getByText('What this means')).toBeInTheDocument();
    });
    // BioStack must not present an "Alternative scenarios" section when no
    // counterfactual variant beats the baseline. Empty cards would create the
    // false impression that the comparison ran.
    expect(screen.queryByText('Alternative scenarios')).not.toBeInTheDocument();
    expect(screen.queryByText('Original vs BioStack alternative')).not.toBeInTheDocument();
    // The retired prescriptive labels must never reappear.
    expect(screen.queryByText('Why this is better')).not.toBeInTheDocument();
    expect(screen.queryByText('Original vs BioStack Version')).not.toBeInTheDocument();
  });

  it('saves an anonymous analysis locally without a session', async () => {
    const user = userEvent.setup();
    restoreAnalyzerResult(restorableResult());
    const events: string[] = [];
    window.addEventListener('biostack:analyzer_event', ((event: CustomEvent) => {
      events.push(event.detail.eventName);
    }) as EventListener);

    render(<ProtocolAnalyzerExperience />);

    await waitFor(() => {
      expect(screen.getByText('What this means')).toBeInTheDocument();
    });
    await user.click(screen.getAllByRole('button', { name: 'Save Analysis' })[0]);

    const history = JSON.parse(window.localStorage.getItem(ANALYZER_ANALYSIS_HISTORY_KEY) ?? '[]');
    expect(history).toHaveLength(1);
    expect(history[0]).toMatchObject({
      inputType: 'Paste',
      rawInput: 'BPC-157 500mcg daily',
      score: 72,
    });
    expect(screen.getByText(/Analysis saved locally/)).toBeInTheDocument();
    expect(events).toContain('analyzer_save_clicked');
  });

  it('saves anonymous conversion handoff state before sign-in redirect', async () => {
    const user = userEvent.setup();
    restoreAnalyzerResult(restorableResult());
    const eventDetails: Array<Record<string, unknown>> = [];
    window.addEventListener('biostack:analyzer_event', ((event: CustomEvent) => {
      eventDetails.push(event.detail);
    }) as EventListener);

    render(<ProtocolAnalyzerExperience />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Convert to BioStack Protocol' })).toBeEnabled();
    });
    await user.click(screen.getByRole('button', { name: 'Convert to BioStack Protocol' }));

    const history = JSON.parse(window.localStorage.getItem(ANALYZER_ANALYSIS_HISTORY_KEY) ?? '[]');
    const draft = JSON.parse(window.localStorage.getItem(ANALYZER_PROTOCOL_DRAFT_KEY) ?? 'null');
    expect(history).toHaveLength(1);
    expect(draft).toMatchObject({
      sourceAnalysisId: history[0].id,
      goal: 'healing',
      name: 'healing alternative protocol',
    });
    expect(pushMock).toHaveBeenCalledWith('/auth/signin?callbackUrl=/protocol-console');
    expect(eventDetails).toContainEqual(
      expect.objectContaining({
        eventName: 'analyzer_convert_clicked',
        inputType: 'Paste',
        scoreBand: 'strong_fit',
        goal: 'healing',
        issueCount: 1,
        recommendationCount: 1,
        locked: true,
      }),
    );
  });

  it('renders alternative-scenario labels and never the retired prescriptive labels', async () => {
    restoreAnalyzerResult(restorableResult());

    render(<ProtocolAnalyzerExperience />);

    await waitFor(() => {
      expect(screen.getByText('Alternative scenarios')).toBeInTheDocument();
    });
    expect(screen.getByText('Original vs BioStack alternative')).toBeInTheDocument();
    // Visible language guard for this surface.
    expect(screen.queryByText('Why this is better')).not.toBeInTheDocument();
    expect(screen.queryByText('Best swap')).not.toBeInTheDocument();
    expect(screen.queryByText('Best removal')).not.toBeInTheDocument();
    expect(screen.queryByText(/Optimize the stack/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Strong base with room to tighten/)).not.toBeInTheDocument();
    expect(screen.queryByText(/cleaner ways to structure/)).not.toBeInTheDocument();
    expect(screen.queryByText(/cleaner version/)).not.toBeInTheDocument();
    // Recommendation-style "Replace X →" / "Swap X -> Y" patterns must not appear.
    expect(screen.queryByText(/^Swap .+ -> /)).not.toBeInTheDocument();
    expect(screen.queryByText(/^Replace .+ → /)).not.toBeInTheDocument();
  });

  it('routes full-analysis upgrade CTAs through pricing before billing', async () => {
    restoreAnalyzerResult(restorableResult());

    render(<ProtocolAnalyzerExperience />);

    await waitFor(() => {
      expect(screen.getByText('What this means')).toBeInTheDocument();
    });

    const unlockLinks = screen.getAllByRole('link', { name: 'Unlock full analysis' });
    expect(unlockLinks.length).toBeGreaterThan(0);
    for (const link of unlockLinks) {
      expect(link).toHaveAttribute('href', '/pricing?intent=analyzer');
    }
  });

  it('surfaces positive score signals as findings instead of saying no issues exist', async () => {
    restoreAnalyzerResult(
      restorableResult({
        withOptimizedVariant: false,
        issueMessages: [],
        scoreExplanation: { baseScore: 50, synergy: 15, redundancy: 0, interference: 0 },
      }),
    );

    render(<ProtocolAnalyzerExperience />);

    await waitFor(() => {
      expect(screen.getByText('Positive signal')).toBeInTheDocument();
    });
    expect(screen.getByText('Synergy +15')).toBeInTheDocument();
    expect(screen.queryByText('No issues yet.')).not.toBeInTheDocument();
  });

  it('uses neutral fit copy when no analyzer goal is selected', async () => {
    restoreAnalyzerResult(
      restorableResult({
        withOptimizedVariant: false,
        goal: '',
        issueMessages: [],
      }),
      '',
    );

    render(<ProtocolAnalyzerExperience />);

    await waitFor(() => {
      expect(screen.getByText(/Select a goal to personalize the fit assessment\./)).toBeInTheDocument();
    });
    expect(screen.queryByText(/selected goal/i)).not.toBeInTheDocument();
  });

  it('converts the current analysis when no optimized variant exists', async () => {
    const user = userEvent.setup();
    restoreAnalyzerResult(restorableResult({ withOptimizedVariant: false }));

    render(<ProtocolAnalyzerExperience />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Convert to BioStack Protocol' })).toBeEnabled();
    });
    await user.click(screen.getByRole('button', { name: 'Convert to BioStack Protocol' }));

    const history = JSON.parse(window.localStorage.getItem(ANALYZER_ANALYSIS_HISTORY_KEY) ?? '[]');
    const draft = JSON.parse(window.localStorage.getItem(ANALYZER_PROTOCOL_DRAFT_KEY) ?? 'null');
    expect(history).toHaveLength(1);
    expect(draft.optimizedProtocol).toEqual(draft.protocol);
    expect(pushMock).toHaveBeenCalledWith('/auth/signin?callbackUrl=/protocol-console');
  });
});

function restoreAnalyzerResult(result: ProtocolAnalyzerResult, goal = 'healing') {
  window.localStorage.setItem(
    'biostack.analyzer.session.v3',
    JSON.stringify({
      mode: 'Paste',
      inputText: 'BPC-157 500mcg daily',
      goal,
      result,
    }),
  );
}

function restorableResult({
  withOptimizedVariant = true,
  goal = 'healing',
  issueMessages = ['Some recovery overlap is present.'],
  scoreExplanation = { baseScore: 50, synergy: 0, redundancy: -4, interference: 0 },
}: {
  withOptimizedVariant?: boolean;
  goal?: string;
  issueMessages?: string[];
  scoreExplanation?: {
    baseScore: number;
    synergy: number;
    redundancy: number;
    interference: number;
  };
} = {}): ProtocolAnalyzerResult {
  return {
    protocol: [
      {
        compoundName: 'BPC-157',
        dose: 500,
        unit: 'mcg',
        frequency: 'daily',
        duration: '8 weeks',
        goal,
        sourceText: 'BPC-157 500mcg daily',
      },
      {
        compoundName: 'TB-500',
        dose: 2,
        unit: 'mg',
        frequency: 'twice weekly',
        duration: '8 weeks',
        goal,
        sourceText: 'TB-500 2mg twice weekly',
      },
    ],
    score: 72,
    scoreExplanation,
    issues: issueMessages.map((message) => ({
      type: 'redundancy',
      severity: 'medium',
      compounds: ['BPC-157', 'TB-500'],
      message,
    })),
    suggestions: [],
    decomposedBlends: [],
    unknownCompounds: [],
    counterfactuals: {
      baselineScore: 72,
      bestRemoveOne: [],
      bestSwapOne: [],
      bestSimplifiedProtocol: withOptimizedVariant
        ? {
            score: 78,
            removed: ['TB-500'],
            compounds: [
              {
                compoundName: 'BPC-157',
                dose: 500,
                unit: 'mcg',
                frequency: 'daily',
                duration: '8 weeks',
                goal: 'healing',
                sourceText: 'BPC-157 500mcg daily',
              },
            ],
            reasons: ['Simpler attribution.'],
          }
        : null,
      goalAwareOptions: [],
    },
    inputType: 'Paste',
    sourceName: null,
    extractionWarnings: [],
    parserWarnings: [],
    lowConfidenceExtraction: false,
    extractedTextPreview: 'BPC-157 500mcg daily',
    artifacts: [],
  } as unknown as ProtocolAnalyzerResult;
}
