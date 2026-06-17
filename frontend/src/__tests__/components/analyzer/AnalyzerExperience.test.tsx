import React from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ProtocolAnalyzerResult } from '@/lib/types';
import { STORAGE_KEY_V3, STORAGE_KEY_V4 } from '@/components/tools/analyzer/useAnalyzerSession';

// ── Mocks (analyzer test conventions) ─────────────────────────────────────────

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement> & { href: string }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

const pushMock = vi.fn();
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: pushMock }),
}));

// useAuth is overridable per-test via a mutable holder.
const authState: { user: unknown; loading: boolean } = { user: null, loading: false };
vi.mock('@/lib/AuthProvider', () => ({
  useAuth: () => authState,
}));

const analyzeProtocolMock = vi.fn();
const getProfilesMock = vi.fn();
vi.mock('@/lib/api', () => ({
  apiClient: {
    analyzeProtocol: (...args: unknown[]) => analyzeProtocolMock(...args),
    getProfiles: (...args: unknown[]) => getProfilesMock(...args),
  },
  ApiError: class ApiError extends Error {
    status: number;
    constructor(message: string, status = 500) {
      super(message);
      this.status = status;
    }
  },
}));

const saveAnalyzerAnalysisMock = vi.fn();
const saveAnalyzerProtocolDraftMock = vi.fn();
vi.mock('@/lib/analyzerStorage', () => ({
  saveAnalyzerAnalysis: (...args: unknown[]) => saveAnalyzerAnalysisMock(...args),
  saveAnalyzerProtocolDraft: (...args: unknown[]) => saveAnalyzerProtocolDraftMock(...args),
}));

// Make ScoreHero's gauge render its numeric score synchronously (no animation).
vi.mock('framer-motion', () => ({
  motion: {
    path: ({ children, ...props }: React.SVGProps<SVGPathElement>) => <path {...props}>{children}</path>,
    circle: ({ children, ...props }: React.SVGProps<SVGCircleElement>) => <circle {...props}>{children}</circle>,
    span: ({ children, ...props }: React.HTMLAttributes<HTMLSpanElement>) => <span {...props}>{children}</span>,
    div: ({ children, ...props }: React.HTMLAttributes<HTMLDivElement>) => <div {...props}>{children}</div>,
  },
  useReducedMotion: () => true,
  AnimatePresence: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

import { AnalyzerExperience } from '@/components/tools/analyzer/AnalyzerExperience';

// ── Fake result factory ───────────────────────────────────────────────────────

function makeResult(overrides: Partial<ProtocolAnalyzerResult> = {}): ProtocolAnalyzerResult {
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
    scoreExplanation: { baseScore: 50, synergy: 0, redundancy: -4, interference: 0 },
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
  } as unknown as ProtocolAnalyzerResult;
}

// ── Analytics event capture ───────────────────────────────────────────────────

function captureAnalyzerEvents() {
  const events: Array<Record<string, unknown>> = [];
  const handler = ((event: CustomEvent) => {
    events.push(event.detail as Record<string, unknown>);
  }) as EventListener;
  window.addEventListener('biostack:analyzer_event', handler);
  return {
    events,
    names: () => events.map((detail) => detail.eventName),
    dispose: () => window.removeEventListener('biostack:analyzer_event', handler),
  };
}

// Seed a v4 snapshot so the component restores directly onto the report stage.
function seedV4WithResult(result: ProtocolAnalyzerResult, primaryCategory: string | null = 'recovery') {
  window.localStorage.setItem(
    STORAGE_KEY_V4,
    JSON.stringify({
      mode: 'Paste',
      inputText: 'BPC-157 500mcg daily',
      linkUrl: '',
      goals: { primaryCategory, refinementGoalIds: [] },
      context: { sex: '', age: '', weight: '', existingStack: '' },
      result,
    }),
  );
}

beforeEach(() => {
  window.localStorage.clear();
  pushMock.mockClear();
  analyzeProtocolMock.mockReset();
  getProfilesMock.mockReset();
  getProfilesMock.mockResolvedValue([]);
  saveAnalyzerAnalysisMock.mockReset();
  saveAnalyzerAnalysisMock.mockReturnValue({ id: 'analysis_test_1' });
  saveAnalyzerProtocolDraftMock.mockReset();
  authState.user = null;
  authState.loading = false;
});

afterEach(() => {
  vi.clearAllMocks();
});

describe('AnalyzerExperience', () => {
  // 1 ─ Initial mount: input stage + analyzer_viewed
  it('renders the input stage (heading + four mode tabs) and fires analyzer_viewed', () => {
    const captured = captureAnalyzerEvents();
    render(<AnalyzerExperience />);

    expect(
      screen.getByRole('heading', { name: /Analyze any protocol, in any format you actually have/i }),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Paste' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Upload' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Scan' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Link' })).toBeInTheDocument();

    expect(captured.names()).toContain('analyzer_viewed');
    captured.dispose();
  });

  // 2 ─ v4 session restore lands on the report stage without re-analyzing
  it('restores a v4 snapshot with a result directly onto the report stage', async () => {
    seedV4WithResult(makeResult({ score: 72 }));

    render(<AnalyzerExperience />);

    // ScoreHero numeric score becomes visible once the session loads.
    expect(await screen.findByLabelText(/BioStack score 72 out of 100/i)).toBeInTheDocument();
    expect(screen.getByText('What this means')).toBeInTheDocument();
    // No re-analysis on restore.
    expect(analyzeProtocolMock).not.toHaveBeenCalled();
  });

  // 3 ─ v3 migration applies the goal and lands without crashing
  it('migrates a v3 session, selecting the Recovery category for goal=healing', async () => {
    window.localStorage.setItem(
      STORAGE_KEY_V3,
      JSON.stringify({ mode: 'Paste', inputText: 'x', goal: 'healing' }),
    );

    render(<AnalyzerExperience />);

    // v3 with no result → input stage; goal=healing migrates to the 'recovery'
    // category. The migration runs inside useAnalyzerSession's setTimeout(0), so
    // the first render shows the empty default snapshot. Wait for the migrated
    // value to propagate rather than reading it off the first findBy resolution
    // (which would race against the timer and flake as `expected '' to be 'x'`).
    let textarea!: HTMLTextAreaElement;
    await waitFor(() => {
      textarea = screen.getByRole('textbox') as HTMLTextAreaElement;
      expect(textarea.value).toBe('x');
    });
    // Recovery & Repair chip is present (category from migrated goal).
    expect(screen.getByRole('button', { name: 'Recovery & Repair' })).toBeInTheDocument();
  });

  // 4 ─ Analyze happy path: payload includes goal token, secondaryGoals, maxCompounds
  it('runs analysis from Paste mode and transitions to the report stage', async () => {
    const user = userEvent.setup();
    const captured = captureAnalyzerEvents();
    analyzeProtocolMock.mockResolvedValue(makeResult({ score: 72 }));

    render(<AnalyzerExperience />);

    // Select the Recovery & Repair goal so a token is sent.
    await user.click(screen.getByRole('button', { name: 'Recovery & Repair' }));

    const textarea = screen.getByRole('textbox');
    await user.type(textarea, 'BPC-157 500mcg daily');

    await user.click(screen.getByRole('button', { name: 'Analyze Protocol' }));

    await waitFor(() => expect(analyzeProtocolMock).toHaveBeenCalledTimes(1));

    const payload = analyzeProtocolMock.mock.calls[0][0] as Record<string, unknown>;
    expect(payload.inputType).toBe('Paste');
    expect(payload.goal).toBe('healing injury recovery tissue repair');
    expect(payload.secondaryGoals).toEqual([]);
    expect(typeof payload.maxCompounds).toBe('number');

    // The analysis-started analytics event fires on the analyze click.
    expect(captured.names()).toContain('analyzer_analysis_started');

    // Lands on the report stage.
    expect(await screen.findByLabelText(/BioStack score 72 out of 100/i)).toBeInTheDocument();
    captured.dispose();
  });

  // 4b ─ Analyze with no goal selected sends an empty goal token
  it('sends an empty goal when no category is selected', async () => {
    const user = userEvent.setup();
    analyzeProtocolMock.mockResolvedValue(makeResult({ score: 72 }));

    render(<AnalyzerExperience />);

    await user.type(screen.getByRole('textbox'), 'BPC-157 500mcg daily');
    await user.click(screen.getByRole('button', { name: 'Analyze Protocol' }));

    await waitFor(() => expect(analyzeProtocolMock).toHaveBeenCalledTimes(1));
    const payload = analyzeProtocolMock.mock.calls[0][0] as Record<string, unknown>;
    expect(payload.goal).toBe('');
    expect(payload.secondaryGoals).toEqual([]);
  });

  // 5 ─ Analyze failure: failure card + retry re-calls analyzeProtocol
  it('shows the failure card on rejection and retries on click', async () => {
    const user = userEvent.setup();
    analyzeProtocolMock.mockRejectedValueOnce(new Error('network down'));

    render(<AnalyzerExperience />);

    await user.type(screen.getByRole('textbox'), 'BPC-157 500mcg daily');
    await user.click(screen.getByRole('button', { name: 'Analyze Protocol' }));

    // Failure state renders with a retry affordance.
    expect(await screen.findByText('Analysis is temporarily unavailable.')).toBeInTheDocument();
    const retry = screen.getByRole('button', { name: 'Try again' });

    analyzeProtocolMock.mockResolvedValueOnce(makeResult({ score: 72 }));
    await user.click(retry);

    await waitFor(() => expect(analyzeProtocolMock).toHaveBeenCalledTimes(2));
    expect(await screen.findByLabelText(/BioStack score 72 out of 100/i)).toBeInTheDocument();
  });

  // 6 ─ Example load fires analyzer_example_loaded and triggers analysis
  it('loads an example, fires analyzer_example_loaded, and triggers analysis', async () => {
    const user = userEvent.setup();
    const captured = captureAnalyzerEvents();
    analyzeProtocolMock.mockResolvedValue(makeResult({ score: 72 }));

    render(<AnalyzerExperience />);

    await user.click(screen.getByRole('button', { name: 'Healing stack' }));

    await waitFor(() => expect(analyzeProtocolMock).toHaveBeenCalledTimes(1));
    expect(captured.names()).toContain('analyzer_example_loaded');
    expect(await screen.findByLabelText(/BioStack score 72 out of 100/i)).toBeInTheDocument();
    captured.dispose();
  });

  // 7 ─ Edit from report returns to input stage, preserving input + result
  it('returns to the input stage from the report when Edit is clicked, preserving input', async () => {
    const user = userEvent.setup();
    seedV4WithResult(makeResult({ score: 72 }));

    render(<AnalyzerExperience />);

    expect(await screen.findByLabelText(/BioStack score 72 out of 100/i)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Edit' }));

    // Back on the input stage with the preserved text.
    const textarea = (await screen.findByRole('textbox')) as HTMLTextAreaElement;
    expect(textarea.value).toBe('BPC-157 500mcg daily');
    // The analyze button (input stage marker) is present again.
    expect(screen.getByRole('button', { name: 'Analyze Protocol' })).toBeInTheDocument();
  });

  // 8 ─ Unlock click fires analyzer_unlock_clicked
  it('fires analyzer_unlock_clicked when the Unlock full analysis link is clicked', async () => {
    const user = userEvent.setup();
    const captured = captureAnalyzerEvents();
    seedV4WithResult(makeResult({ score: 72 }));

    render(<AnalyzerExperience />);
    await screen.findByLabelText(/BioStack score 72 out of 100/i);

    // NextSteps renders the unlock link (there may also be a mobile sticky one).
    const unlockLinks = screen.getAllByRole('link', { name: 'Unlock full analysis' });
    expect(unlockLinks.length).toBeGreaterThan(0);
    for (const link of unlockLinks) {
      expect(link).toHaveAttribute('href', '/pricing?intent=analyzer');
    }

    await user.click(unlockLinks[0]);
    expect(captured.names()).toContain('analyzer_unlock_clicked');
    captured.dispose();
  });

  // 9 ─ Convert unauthenticated → router.push to sign-in
  it('routes an unauthenticated convert through the sign-in callback', async () => {
    const user = userEvent.setup();
    authState.user = null;
    authState.loading = false;
    seedV4WithResult(makeResult({ score: 72 }));

    render(<AnalyzerExperience />);
    await screen.findByLabelText(/BioStack score 72 out of 100/i);

    // The NextSteps Convert button (the report-row CTA).
    const convertButtons = screen.getAllByRole('button', { name: 'Convert to BioStack Protocol' });
    await user.click(convertButtons[0]);

    expect(saveAnalyzerAnalysisMock).toHaveBeenCalledTimes(1);
    expect(saveAnalyzerProtocolDraftMock).toHaveBeenCalledTimes(1);
    expect(pushMock).toHaveBeenCalledWith('/auth/signin?callbackUrl=/protocol-console');
  });

  // 10 ─ Convert authenticated → router.push to the console
  it('routes an authenticated convert straight to the protocol console', async () => {
    const user = userEvent.setup();
    authState.user = { id: 'u1', email: 'a@b.co' };
    authState.loading = false;
    getProfilesMock.mockResolvedValue([]);
    seedV4WithResult(makeResult({ score: 72 }));

    render(<AnalyzerExperience />);
    await screen.findByLabelText(/BioStack score 72 out of 100/i);

    const convertButtons = screen.getAllByRole('button', { name: 'Convert to BioStack Protocol' });
    await user.click(convertButtons[0]);

    expect(saveAnalyzerAnalysisMock).toHaveBeenCalledTimes(1);
    expect(saveAnalyzerProtocolDraftMock).toHaveBeenCalledTimes(1);
    expect(pushMock).toHaveBeenCalledWith('/protocol-console');
  });

  // 11 ─ Save analysis (ported): NextSteps save persists + fires analyzer_save_clicked
  it('saves the analysis locally and fires analyzer_save_clicked', async () => {
    const user = userEvent.setup();
    const captured = captureAnalyzerEvents();
    seedV4WithResult(makeResult({ score: 72 }));

    render(<AnalyzerExperience />);
    await screen.findByLabelText(/BioStack score 72 out of 100/i);

    const saveButtons = screen.getAllByRole('button', { name: 'Save Analysis' });
    await user.click(saveButtons[0]);

    expect(saveAnalyzerAnalysisMock).toHaveBeenCalledTimes(1);
    expect(screen.getByText(/Analysis saved locally/)).toBeInTheDocument();
    expect(captured.names()).toContain('analyzer_save_clicked');
    captured.dispose();
  });

  // 13 ─ Clear button empties the input and drops the result (clearInput)
  it('clears the input text and result when Clear is clicked', async () => {
    const user = userEvent.setup();
    seedV4WithResult(makeResult({ score: 72 }));

    render(<AnalyzerExperience />);
    // Restore onto the report stage, then Edit back to the input stage where the
    // text (and the underlying result) are still present.
    await screen.findByLabelText(/BioStack score 72 out of 100/i);
    await user.click(screen.getByRole('button', { name: 'Edit' }));

    let textarea!: HTMLTextAreaElement;
    await waitFor(() => {
      textarea = screen.getByRole('textbox') as HTMLTextAreaElement;
      expect(textarea.value).toBe('BPC-157 500mcg daily');
    });

    await user.click(screen.getByRole('button', { name: 'Clear' }));

    // Input text is emptied.
    await waitFor(() => {
      expect((screen.getByRole('textbox') as HTMLTextAreaElement).value).toBe('');
    });
    // Result is dropped: the report stage (score gauge) is gone and we stay on
    // the input stage.
    expect(screen.queryByLabelText(/BioStack score/i)).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Analyze Protocol' })).toBeInTheDocument();
  });

  // 14 ─ Alternative-scenarios language guard (ported): no retired prescriptive labels
  it('never renders retired prescriptive labels in the report', async () => {
    seedV4WithResult(makeResult({ score: 72 }));

    render(<AnalyzerExperience />);
    await screen.findByLabelText(/BioStack score 72 out of 100/i);

    expect(screen.queryByText('Why this is better')).not.toBeInTheDocument();
    expect(screen.queryByText('Original vs BioStack Version')).not.toBeInTheDocument();
    expect(screen.queryByText(/Optimize the stack/)).not.toBeInTheDocument();
  });
});
