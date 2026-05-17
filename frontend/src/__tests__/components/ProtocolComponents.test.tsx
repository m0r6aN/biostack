import { InteractionIntelligenceCard } from '@/components/protocols/InteractionIntelligenceCard';
import { ProtocolComparison } from '@/components/protocols/ProtocolComparison';
import { ProtocolContinuityStrip } from '@/components/protocols/ProtocolContinuityStrip';
import { ProviderObservationalSummary } from '@/components/protocols/ProviderObservationalSummary';
import { StackScoreCard } from '@/components/protocols/StackScoreCard';
import type { InteractionIntelligence, Protocol, ProtocolActualComparison, ProtocolReview, StackScore } from '@/lib/types';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
    makeProviderSummaryActualComparison,
    makeProviderSummaryPatternSnapshot,
    makeSavedProviderSummaryProtocol,
    makeSavedProviderSummaryProtocolWithMissingData,
    makeSavedProviderSummaryProtocolWithUnsafeGeneratedFields,
    providerSummaryGeneratedAt,
} from '../fixtures/providerSummary';

const originalClipboardDescriptor = Object.getOwnPropertyDescriptor(window.navigator, 'clipboard');

afterEach(() => {
  vi.restoreAllMocks();
  if (originalClipboardDescriptor) {
    Object.defineProperty(window.navigator, 'clipboard', originalClipboardDescriptor);
    return;
  }

  Object.defineProperty(window.navigator, 'clipboard', { configurable: true, value: undefined });
});

const makeStackScore = (overrides: Partial<StackScore> = {}): StackScore => ({
  score: 78,
  breakdown: { synergy: 25, redundancy: 10, conflicts: 5, evidence: 38 },
  chips: ['Synergistic Stack', 'Low redundancy'],
  ...overrides,
});

describe('StackScoreCard', () => {
  it('renders the stack score value', () => {
    render(<StackScoreCard score={makeStackScore()} />);
    expect(screen.getByText('78')).toBeInTheDocument();
  });

  it('renders the Stack Score label', () => {
    render(<StackScoreCard score={makeStackScore()} />);
    expect(screen.getByText(/stack score/i)).toBeInTheDocument();
  });

  it('renders chips', () => {
    render(<StackScoreCard score={makeStackScore()} />);
    expect(screen.getByText('Synergistic Stack')).toBeInTheDocument();
    expect(screen.getByText('Low redundancy')).toBeInTheDocument();
  });

  it('renders breakdown labels', () => {
    render(<StackScoreCard score={makeStackScore()} />);
    expect(screen.getByText('Synergy')).toBeInTheDocument();
    expect(screen.getByText('Redundancy')).toBeInTheDocument();
    expect(screen.getByText('Conflicts')).toBeInTheDocument();
    expect(screen.getByText('Evidence')).toBeInTheDocument();
  });

  it('renders breakdown values', () => {
    render(<StackScoreCard score={makeStackScore()} />);
    expect(screen.getByText('25')).toBeInTheDocument();
    expect(screen.getByText('10')).toBeInTheDocument();
    expect(screen.getByText('5')).toBeInTheDocument();
    expect(screen.getByText('38')).toBeInTheDocument();
  });

  it('renders with score 0 without crashing', () => {
    expect(() =>
      render(<StackScoreCard score={makeStackScore({ score: 0, chips: [] })} />)
    ).not.toThrow();
  });

  it('renders with no chips without crashing', () => {
    render(<StackScoreCard score={makeStackScore({ chips: [] })} />);
    expect(screen.getByText('78')).toBeInTheDocument();
  });
});

const makeComparison = (overrides: Partial<ProtocolActualComparison> = {}): ProtocolActualComparison => ({
  simulation: { timeline: [], insights: [] },
  run: null,
  runSummary: null,
  observations: [],
  actualTrends: [],
  insights: [],
  highlights: [],
  ...overrides,
});

describe('ProtocolComparison', () => {
  it('renders empty state when comparison is null', () => {
    render(<ProtocolComparison comparison={null} />);
    expect(screen.getByText(/simulate or start your first run/i)).toBeInTheDocument();
  });

  it('renders run intelligence heading when comparison is provided', () => {
    render(<ProtocolComparison comparison={makeComparison()} />);
    expect(screen.getByText('Run intelligence')).toBeInTheDocument();
  });

  it('shows no-observations message when run exists but no observations', () => {
    render(<ProtocolComparison comparison={makeComparison({
      run: {
        id: 'run-1', protocolId: 'p-1', personId: 'u-1',
        protocolName: 'Test', protocolVersion: 1,
        startedAtUtc: '2025-01-01T00:00:00Z', endedAtUtc: null,
        status: 'active', notes: '',
      },
    })} />);
    expect(screen.getByText(/no observations yet/i)).toBeInTheDocument();
  });

  it('renders trend cards when actualTrends are present', () => {
    render(<ProtocolComparison comparison={makeComparison({
      actualTrends: [
        { metric: 'Energy', beforeAverage: 5, afterAverage: 8, direction: 'up' },
      ],
    })} />);
    // 'Energy' appears in both legend and trend card — use getAllByText
    expect(screen.getAllByText('Energy').length).toBeGreaterThan(0);
    expect(screen.getByText('up')).toBeInTheDocument();
  });

  it('renders highlights when provided', () => {
    render(<ProtocolComparison comparison={makeComparison({
      highlights: ['Energy improved significantly.'],
    })} />);
    expect(screen.getByText('Energy improved significantly.')).toBeInTheDocument();
  });

  it('renders insights when provided', () => {
    render(<ProtocolComparison comparison={makeComparison({
      insights: [{ type: 'alignment', message: 'Sleep aligned with projection.', relatedSignals: [] }],
    })} />);
    expect(screen.getByText('Sleep aligned with projection.')).toBeInTheDocument();
  });

  it('renders the metric color legend', () => {
    render(<ProtocolComparison comparison={makeComparison()} />);
    expect(screen.getByText('Energy')).toBeInTheDocument();
    expect(screen.getByText('Sleep')).toBeInTheDocument();
  });
});

const makeProtocol = (overrides: Partial<Protocol> = {}): Protocol => ({
  id: 'protocol-2',
  personId: 'person-1',
  name: 'Recovery Protocol',
  version: 2,
  parentProtocolId: 'protocol-1',
  originProtocolId: 'protocol-1',
  evolvedFromRunId: null,
  isDraft: true,
  evolutionContext: '',
  isCurrentVersion: true,
  priorVersions: [{ id: 'protocol-1', name: 'Original Protocol', version: 1, isDraft: false, createdAtUtc: '2026-01-01T00:00:00Z' }],
  createdAtUtc: '2026-01-02T00:00:00Z',
  updatedAtUtc: '2026-01-02T00:00:00Z',
  items: [],
  stackScore: makeStackScore(),
  simulation: { timeline: [], insights: [] },
  interactionIntelligence: {
    summary: { synergies: 0, redundancies: 0, interferences: 0 },
    score: { synergyScore: 0, redundancyPenalty: 0, interferencePenalty: 0 },
    compositeScore: 0,
    topFindings: [],
    interactions: [],
    counterfactuals: [],
    swaps: [],
  },
  activeRun: null,
  versionDiff: {
    fromProtocolId: 'protocol-1',
    toProtocolId: 'protocol-2',
    changes: [{ changeType: 'added', scope: 'compound', subject: 'BPC-157', before: '', after: 'BPC-157' }],
  },
  actualComparison: null,
  ...overrides,
});

const makeReview = (): ProtocolReview => ({
  lineageRootProtocolId: 'protocol-1',
  requestedProtocolId: 'protocol-2',
  lineageName: 'Recovery Protocol',
  versions: [
    {
      protocolId: 'protocol-2',
      name: 'Recovery Protocol',
      version: 2,
      isDraft: true,
      parentProtocolId: 'protocol-1',
      evolvedFromRunId: null,
      evolutionContext: '',
      createdAtUtc: '2026-01-02T00:00:00Z',
      versionDiff: null,
      runs: [
        {
          run: {
            id: 'run-1',
            protocolId: 'protocol-2',
            personId: 'person-1',
            protocolName: 'Recovery Protocol',
            protocolVersion: 2,
            startedAtUtc: '2026-01-03T00:00:00Z',
            endedAtUtc: null,
            status: 'active',
            notes: '',
          },
          summary: null,
          observations: [],
          trends: [],
          insights: [],
        },
      ],
    },
  ],
  sections: [{ type: 'alignment', title: 'Alignment', summary: 'Energy matched the expected sequence.', evidence: [] }],
  timeline: [],
  safetyNotes: [],
});

describe('ProtocolContinuityStrip', () => {
  it('renders lineage, run state, review signal, and intelligence summaries', () => {
    render(
      <ProtocolContinuityStrip
        protocol={makeProtocol()}
        review={makeReview()}
        patterns={{
          protocolId: 'protocol-2',
          historicalRunCount: 2,
          patternConfidence: 'moderate',
          metricPatterns: [{ metric: 'Check-in cadence', observation: 'Check-ins usually arrive every other day.' }],
          eventPatterns: [],
          sequencePatterns: [],
          currentRunComparison: { similarity: 'moderate', matchingSignals: ['Cadence is aligned.'], divergentSignals: [] },
        }}
        drift={{
          protocolId: 'protocol-2',
          driftState: 'mild',
          baselineSource: 'historical_runs',
          signals: [{ type: 'checkin_timing', severity: 'mild', description: 'Check-ins are slightly later.' }],
          regimeClassification: { state: 'drifting', contributingFactors: ['check-in timing'] },
        }}
        sequence={{
          protocolId: 'protocol-2',
          baselineSource: 'historical_runs',
          historicalRunCount: 2,
          expectedNextEvent: {
            eventType: 'FirstCheckIn',
            description: 'First check-in usually follows run start.',
            timingWindow: 'within 48 hours',
            confidence: 'moderate',
          },
          commonTransitions: [],
          currentStatus: { state: 'pending', notes: ['Waiting for first check-in.'] },
        }}
      />
    );

    expect(screen.getByText('v2 draft follows v1')).toBeInTheDocument();
    expect(screen.getByText('Original Protocol')).toBeInTheDocument();
    expect(screen.getByText('active')).toBeInTheDocument();
    expect(screen.getByText('Energy matched the expected sequence.')).toBeInTheDocument();
    expect(screen.getByText('drifting')).toBeInTheDocument();
    expect(screen.getByText('1 item')).toBeInTheDocument();
    expect(screen.getByText('2 runs')).toBeInTheDocument();
    expect(screen.getByText('first check-in')).toBeInTheDocument();
    expect(screen.getByText('pending')).toBeInTheDocument();
  });
});

const makeIntelligence = (overrides: Partial<InteractionIntelligence> = {}): InteractionIntelligence => ({
  summary: { synergies: 1, redundancies: 0, interferences: 0 },
  score: { synergyScore: 0.8, redundancyPenalty: 0, interferencePenalty: 0 },
  compositeScore: 82,
  topFindings: [],
  interactions: [],
  counterfactuals: [],
  swaps: [],
  ...overrides,
});

describe('InteractionIntelligenceCard', () => {
  it('applies the teal tone to Complementary findings', () => {
    render(
      <InteractionIntelligenceCard
        intelligence={makeIntelligence({
          topFindings: [
            {
              type: 'Complementary',
              compounds: ['BPC-157', 'TB-500'],
              message: 'Distinct mechanisms converging on tissue repair.',
              confidence: 0.86,
            },
          ],
        })}
      />
    );

    const badge = screen.getByText('complementary');
    expect(badge.className).toContain('border-teal-400/20');
    expect(badge.className).toContain('bg-teal-500/10');
    expect(badge.className).toContain('text-teal-100');
    expect(screen.getByText('BPC-157 + TB-500')).toBeInTheDocument();
  });
});

function mockClipboardWrite(writeText: (text: string) => Promise<void>) {
  const writeTextMock = vi.fn(writeText);
  Object.defineProperty(window.navigator, 'clipboard', { configurable: true, value: { writeText: writeTextMock } });
  return writeTextMock;
}

function getCopiedText(writeText: ReturnType<typeof mockClipboardWrite>) {
  return writeText.mock.calls[0]?.[0] ?? '';
}

describe('ProviderObservationalSummary', () => {
  it('renders a factual provider-ready summary from protocol fixture data', () => {
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} patterns={makeProviderSummaryPatternSnapshot()} generatedAt={providerSummaryGeneratedAt} />);

    expect(screen.getByRole('heading', { name: 'Provider-ready observational summary' })).toBeInTheDocument();
    expect(screen.getByText('Magnesium glycinate')).toBeInTheDocument();
    expect(screen.getAllByText('Sleep consistency').length).toBeGreaterThan(0);
    expect(screen.getByText('User-entered schedule/frequency: evening check-in log.')).toBeInTheDocument();
    expect(screen.getByText(/Energy 6\/10 · Sleep 7\/10 · Recovery 8\/10 · Appetite 5\/10/)).toBeInTheDocument();
    expect(screen.getByText(/Check-in cadence: Check-ins usually arrive every other day\./)).toBeInTheDocument();
    expect(screen.getByText('synergy')).toBeInTheDocument();
    expect(screen.getByText('interference')).toBeInTheDocument();
    expect(screen.getByText('Shared pathways: sleep architecture')).toBeInTheDocument();
    expect(screen.getByText('No evidence context available')).toBeInTheDocument();
  });

  it('frames the intro as observational and for professional discussion', () => {
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} generatedAt={providerSummaryGeneratedAt} />);

    const intro = screen.getByText('A factual, observational snapshot of saved protocol data, user-entered check-ins, and BioStack-observed signals for discussion with a qualified professional.');

    expect(intro).toBeInTheDocument();
    expect(intro).toHaveTextContent('observational');
    expect(intro).toHaveTextContent('discussion with a qualified professional');
  });

  it('does not add advice wording to the provider summary intro', () => {
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} generatedAt={providerSummaryGeneratedAt} />);

    const introText = screen.getByText(/A factual, observational snapshot/).textContent?.toLowerCase() ?? '';
    for (const phrase of ['clinical approval', 'diagnosis', 'recommendation', 'recommended', 'start', 'stop', 'combine', 'dosing', 'treatment']) {
      expect(introText).not.toContain(phrase);
    }
  });

  it('renders populated section counts for available summary data', () => {
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} generatedAt={providerSummaryGeneratedAt} />);

    expect(screen.getByText('1 active substance')).toBeInTheDocument();
    expect(screen.getByText('1 recent check-in')).toBeInTheDocument();
    expect(screen.getByText('2 interaction/overlap notes')).toBeInTheDocument();
  });

  it('renders informational scanability labels for data provenance and missing data', () => {
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} generatedAt={providerSummaryGeneratedAt} />);

    expect(screen.getAllByText('User-entered data').length).toBeGreaterThanOrEqual(2);
    expect(screen.getAllByText('BioStack-observed data').length).toBeGreaterThanOrEqual(2);
    expect(screen.getAllByText('Missing data').length).toBeGreaterThanOrEqual(1);
  });

  it('keeps scanability labels informational without advice or clinical wording', () => {
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocolWithMissingData()} generatedAt={providerSummaryGeneratedAt} />);

    for (const label of ['User-entered data', 'BioStack-observed data', 'Missing data']) {
      const text = screen.getAllByText(label)[0].textContent?.toLowerCase() ?? '';
      for (const phrase of ['advice', 'clinical', 'risk', 'danger', 'unsafe', 'should', 'recommend', 'treatment']) {
        expect(text).not.toContain(phrase);
      }
    }
  });

  it('renders a copy summary button', () => {
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} generatedAt={providerSummaryGeneratedAt} />);

    const copyButton = screen.getByRole('button', { name: 'Copy summary' });
    const printButton = screen.getByRole('button', { name: 'Print summary' });

    expect(copyButton).toBeInTheDocument();
    expect(copyButton).toHaveClass('print:hidden');
    expect(printButton).toHaveClass('print:hidden');
    expect(screen.getByLabelText('Provider summary actions')).toHaveClass('print:hidden');
  });

  it('keeps print layout safety boundary and footer rendered', () => {
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} generatedAt={providerSummaryGeneratedAt} />);

    expect(screen.getAllByText(/Feb 10, 2026/).length).toBeGreaterThan(0);
    expect(screen.getByText('For discussion with a qualified professional.')).toBeInTheDocument();
    expect(screen.getByText('Not medical advice.')).toBeInTheDocument();
    expect(screen.getByText(/Generated by BioStack/)).toHaveClass('hidden', 'print:block');
  });

  it('keeps scanability labels screen-only for stable print output', () => {
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} generatedAt={providerSummaryGeneratedAt} />);

    for (const label of ['User-entered data', 'BioStack-observed data', 'Missing data']) {
      for (const badge of screen.getAllByText(label)) {
        expect(badge).toHaveClass('print:hidden');
      }
    }
    expect(screen.getByText(/Generated by BioStack/)).toHaveClass('hidden', 'print:block');
  });

  it('copies factual summary sections as plain text', async () => {
    const user = userEvent.setup();
    const writeText = mockClipboardWrite(async () => undefined);
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} patterns={makeProviderSummaryPatternSnapshot()} generatedAt={providerSummaryGeneratedAt} />);

    await user.click(screen.getByRole('button', { name: 'Copy summary' }));

    expect(screen.getByRole('status')).toHaveTextContent('Summary copied.');
    const copiedText = getCopiedText(writeText);
    expect(copiedText).toContain('Generated timestamp:');
    for (const section of ['Stack overview', 'Active substances', 'Goals and timeline', 'Recent check-ins', 'Observed patterns', 'Interaction and overlap notes', 'Evidence context', 'Safety boundary']) {
      expect(copiedText).toContain(section);
    }
    expect(copiedText).toContain('Magnesium glycinate');
    expect(copiedText).toContain('1 active substance');
    expect(copiedText).toContain('1 recent check-in');
    expect(copiedText).toContain('2 interaction/overlap notes');
    expect(copiedText).toContain('Energy 6/10 · Sleep 7/10 · Recovery 8/10 · Appetite 5/10');
    expect(copiedText).toContain('Shared pathways: sleep architecture');
  });

  it('keeps visual scanability labels out of copied summary text', async () => {
    const user = userEvent.setup();
    const writeText = mockClipboardWrite(async () => undefined);
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} patterns={makeProviderSummaryPatternSnapshot()} generatedAt={providerSummaryGeneratedAt} />);

    await user.click(screen.getByRole('button', { name: 'Copy summary' }));

    const copiedText = getCopiedText(writeText);
    expect(copiedText).not.toContain('User-entered data');
    expect(copiedText).not.toContain('BioStack-observed data');
    expect(copiedText).not.toContain('Missing data');
  });

  it('copied text contains the safety boundary framing', async () => {
    const user = userEvent.setup();
    const writeText = mockClipboardWrite(async () => undefined);
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} generatedAt={providerSummaryGeneratedAt} />);

    await user.click(screen.getByRole('button', { name: 'Copy summary' }));

    const copiedText = getCopiedText(writeText);
    expect(copiedText).toContain('Observational summary');
    expect(copiedText).toContain('For discussion with a qualified professional.');
    expect(copiedText).toContain('Not medical advice.');
    expect(copiedText).toContain('Does not recommend starting, stopping, combining, or dosing any substance.');
  });

  it('copied text excludes unsafe generated advice fields', async () => {
    const user = userEvent.setup();
    const writeText = mockClipboardWrite(async () => undefined);
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocolWithUnsafeGeneratedFields()} generatedAt={providerSummaryGeneratedAt} />);

    await user.click(screen.getByRole('button', { name: 'Copy summary' }));

    const copiedText = getCopiedText(writeText).toLowerCase();
    for (const phrase of ['recommended', 'clinically approved', 'optimal dose', 'you should', 'treatment plan', 'dose change', 'recommendation']) {
      expect(copiedText).not.toContain(phrase);
    }
  });

  it('renders a graceful fallback when copy fails', async () => {
    const user = userEvent.setup();
    mockClipboardWrite(async () => {
      throw new Error('clipboard blocked');
    });
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} generatedAt={providerSummaryGeneratedAt} />);

    await user.click(screen.getByRole('button', { name: 'Copy summary' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Copy failed. Select and copy the summary manually.');
  });

  it('keeps print button behavior unchanged', async () => {
    const user = userEvent.setup();
    const print = vi.spyOn(window, 'print').mockImplementation(() => undefined);
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} generatedAt={providerSummaryGeneratedAt} />);

    await user.click(screen.getByRole('button', { name: 'Print summary' }));

    expect(print).toHaveBeenCalledTimes(1);
  });

  it('renders the required safety framing copy', () => {
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} generatedAt={providerSummaryGeneratedAt} />);

    expect(screen.getByText('Observational summary')).toBeInTheDocument();
    expect(screen.getByText('For discussion with a qualified professional.')).toBeInTheDocument();
    expect(screen.getByText('Not medical advice.')).toBeInTheDocument();
    expect(screen.getByText('Does not recommend starting, stopping, combining, or dosing any substance.')).toBeInTheDocument();
  });

  it('does not render banned advice phrases from generated interaction fields', () => {
    const { container } = render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocolWithUnsafeGeneratedFields()} generatedAt={providerSummaryGeneratedAt} />);
    const text = container.textContent?.toLowerCase() ?? '';

    for (const phrase of ['recommended', 'clinically approved', 'optimal dose', 'you should']) {
      expect(text).not.toContain(phrase);
    }
    expect(text).not.toMatch(/\bsafe\b/);
  });

  it('does not emit generated dosing or treatment recommendation language', () => {
    const { container } = render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocolWithUnsafeGeneratedFields()} generatedAt={providerSummaryGeneratedAt} />);
    const text = container.textContent?.toLowerCase() ?? '';

    expect(text).not.toContain('treatment plan');
    expect(text).not.toContain('dose change');
    expect(text).not.toContain('recommendation');
  });

  it('renders graceful placeholders for empty or missing data', () => {
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocolWithMissingData()} generatedAt={providerSummaryGeneratedAt} />);

    expect(screen.getByText('No active substances recorded')).toBeInTheDocument();
    expect(screen.getByText('No recent check-ins recorded')).toBeInTheDocument();
    expect(screen.getByText('No observed patterns available yet')).toBeInTheDocument();
    expect(screen.getByText('No interaction or overlap notes available')).toBeInTheDocument();
    expect(screen.getByText('No evidence context available')).toBeInTheDocument();
  });

  it('keeps missing section placeholders neutral and factual', () => {
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocolWithMissingData()} generatedAt={providerSummaryGeneratedAt} />);

    const placeholders = [
      'No active substances recorded',
      'No recent check-ins recorded',
      'No observed patterns available yet',
      'No interaction or overlap notes available',
      'No evidence context available',
    ];

    for (const placeholder of placeholders) {
      const text = screen.getByText(placeholder).textContent?.toLowerCase() ?? '';
      for (const phrase of ['advice', 'clinical', 'risk', 'danger', 'unsafe', 'should', 'recommend', 'treatment']) {
        expect(text).not.toContain(phrase);
      }
    }
  });

  it('copies neutral missing-data text when sections are empty', async () => {
    const user = userEvent.setup();
    const writeText = mockClipboardWrite(async () => undefined);
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocolWithMissingData()} generatedAt={providerSummaryGeneratedAt} />);

    await user.click(screen.getByRole('button', { name: 'Copy summary' }));

    const copiedText = getCopiedText(writeText);
    expect(copiedText).toContain('No active substances recorded');
    expect(copiedText).toContain('No recent check-ins recorded');
    expect(copiedText).toContain('No observed patterns available yet');
    expect(copiedText).toContain('No interaction or overlap notes available');
    expect(copiedText).toContain('No evidence context available');
  });

  it('keeps base protocol facts visible when gated historical snapshots are unavailable', () => {
    const protocol = makeSavedProviderSummaryProtocol({
      actualComparison: makeProviderSummaryActualComparison({ run: null, observations: [], actualTrends: [] }),
    });

    render(<ProviderObservationalSummary protocol={protocol} review={null} patterns={null} drift={null} sequence={null} generatedAt={providerSummaryGeneratedAt} />);

    const overview = screen.getByText('Stack overview').closest('section');
    expect(overview).not.toBeNull();
    expect(within(overview as HTMLElement).getByText('Recovery Protocol')).toBeInTheDocument();
    expect(screen.getByText('No observed patterns available yet')).toBeInTheDocument();
  });
});
