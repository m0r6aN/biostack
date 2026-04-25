import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ProtocolAnalyzerExperience } from '@/components/tools/ProtocolAnalyzerExperience';
import { ANALYZER_ANALYSIS_HISTORY_KEY, ANALYZER_PROTOCOL_DRAFT_KEY } from '@/lib/analyzerStorage';
import type { ProtocolAnalyzerResult } from '@/lib/types';

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
    expect(screen.getByText('No obvious removal surfaced.')).toBeInTheDocument();
    expect(screen.getByText('No strong swap surfaced.')).toBeInTheDocument();
    expect(screen.getByText('No cleaner version beat the current stack.')).toBeInTheDocument();
    expect(screen.getByText('Goal-aware optimization needs more context.')).toBeInTheDocument();
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
      name: 'healing optimized protocol',
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

function restoreAnalyzerResult(result: ProtocolAnalyzerResult) {
  window.localStorage.setItem(
    'biostack.analyzer.session.v3',
    JSON.stringify({
      mode: 'Paste',
      inputText: 'BPC-157 500mcg daily',
      goal: 'healing',
      result,
    }),
  );
}

function restorableResult({ withOptimizedVariant = true }: { withOptimizedVariant?: boolean } = {}): ProtocolAnalyzerResult {
  return {
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
      {
        compoundName: 'TB-500',
        dose: 2,
        unit: 'mg',
        frequency: 'twice weekly',
        duration: '8 weeks',
        goal: 'healing',
        sourceText: 'TB-500 2mg twice weekly',
      },
    ],
    score: 72,
    scoreExplanation: {
      positives: ['Compact protocol'],
      penalties: [],
    },
    issues: [
      {
        type: 'redundancy',
        severity: 'medium',
        compounds: ['BPC-157', 'TB-500'],
        message: 'Some recovery overlap is present.',
      },
    ],
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
