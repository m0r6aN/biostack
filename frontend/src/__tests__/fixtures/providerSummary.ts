import type { InteractionIntelligence, Protocol, ProtocolActualComparison, ProtocolPatternSnapshot, StackScore } from '@/lib/types';

export const providerSummaryGeneratedAt = new Date('2026-02-10T15:30:00Z');

const makeProviderSummaryStackScore = (overrides: Partial<StackScore> = {}): StackScore => ({
  score: 78,
  breakdown: { synergy: 25, redundancy: 10, conflicts: 5, evidence: 38 },
  chips: ['Synergistic Stack', 'Recovery Optimized'],
  ...overrides,
});

export const makeProviderSummaryInteractionIntelligence = (overrides: Partial<InteractionIntelligence> = {}): InteractionIntelligence => ({
  summary: { synergies: 1, redundancies: 0, interferences: 0 },
  score: { synergyScore: 0.8, redundancyPenalty: 0, interferencePenalty: 0 },
  compositeScore: 82,
  topFindings: [
    {
      type: 'Synergistic',
      compounds: ['Magnesium glycinate', 'Glycine'],
      message: 'Overlapping sleep-support observations were captured in the saved data.',
      confidence: 0.72,
    },
  ],
  interactions: [
    {
      compoundA: 'Magnesium glycinate',
      compoundB: 'Glycine',
      type: 'Interfering',
      confidence: 0.42,
      sharedPathways: ['sleep architecture'],
      reason: 'Shared pathway overlap was detected in BioStack observations.',
      hintBacked: true,
    },
  ],
  counterfactuals: [],
  swaps: [],
  ...overrides,
});

export const makeProviderSummaryActualComparison = (overrides: Partial<ProtocolActualComparison> = {}): ProtocolActualComparison => ({
  simulation: { timeline: [], insights: [] },
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
  runSummary: null,
  observations: [{ checkInId: 'check-1', date: '2026-01-06T00:00:00Z', day: 4, energy: 6, sleepQuality: 7, appetite: 5, recovery: 8 }],
  actualTrends: [{ metric: 'Energy', beforeAverage: 5, afterAverage: 6, direction: 'up' }],
  insights: [],
  highlights: [],
  ...overrides,
});

export const makeSavedProviderSummaryProtocol = (overrides: Partial<Protocol> = {}): Protocol => ({
  id: 'protocol-2',
  personId: 'person-1',
  name: 'Recovery Protocol',
  version: 2,
  parentProtocolId: null,
  originProtocolId: null,
  evolvedFromRunId: null,
  isDraft: false,
  evolutionContext: '',
  isCurrentVersion: true,
  priorVersions: [],
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-01-08T00:00:00Z',
  items: [
    {
      id: 'item-1',
      protocolId: 'protocol-2',
      compoundRecordId: 'compound-1',
      calculatorResultId: null,
      notes: 'User-entered schedule/frequency: evening check-in log.',
      compound: {
        id: 'compound-1',
        personId: 'person-1',
        name: 'Magnesium glycinate',
        category: 'Supplement',
        startDate: '2026-01-03T00:00:00Z',
        endDate: null,
        status: 'Active',
        notes: 'User-entered frequency: nightly',
        sourceType: 'Manual',
        goal: 'Sleep consistency',
      },
    },
  ],
  stackScore: makeProviderSummaryStackScore(),
  simulation: { timeline: [], insights: [] },
  interactionIntelligence: makeProviderSummaryInteractionIntelligence(),
  activeRun: null,
  versionDiff: null,
  actualComparison: makeProviderSummaryActualComparison(),
  ...overrides,
});

export const makeSavedProviderSummaryProtocolWithMissingData = (overrides: Partial<Protocol> = {}): Protocol => makeSavedProviderSummaryProtocol({
  items: [],
  actualComparison: null,
  interactionIntelligence: makeProviderSummaryInteractionIntelligence({
    summary: { synergies: 0, redundancies: 0, interferences: 0 },
    score: { synergyScore: 0, redundancyPenalty: 0, interferencePenalty: 0 },
    compositeScore: 0,
    topFindings: [],
    interactions: [],
  }),
  ...overrides,
});

export const makeSavedProviderSummaryProtocolWithUnsafeGeneratedFields = (overrides: Partial<Protocol> = {}): Protocol => makeSavedProviderSummaryProtocol({
  interactionIntelligence: makeProviderSummaryInteractionIntelligence({
    topFindings: [
      { type: 'Synergistic', compounds: ['Magnesium glycinate', 'Glycine'], message: 'you should use the optimal dose', confidence: 0.72 },
    ],
    interactions: [
      { compoundA: 'Magnesium glycinate', compoundB: 'Glycine', type: 'Interfering', confidence: 0.42, sharedPathways: ['sleep architecture'], reason: 'clinically approved treatment plan', hintBacked: true },
    ],
    counterfactuals: [
      { removedCompound: 'Glycine', variantScore: 80, deltaScore: 1, deltaPercent: 1, verdict: 'improves', recommendation: 'recommended dose change', summary: { synergies: 0, redundancies: 0, interferences: 0 }, topFindings: [] },
    ],
  }),
  ...overrides,
});

export const makeProviderSummaryPatternSnapshot = (): ProtocolPatternSnapshot => ({
  protocolId: 'protocol-2',
  historicalRunCount: 2,
  patternConfidence: 'moderate',
  metricPatterns: [{ metric: 'Check-in cadence', observation: 'Check-ins usually arrive every other day.' }],
  eventPatterns: [],
  sequencePatterns: [],
  currentRunComparison: { similarity: 'moderate', matchingSignals: ['Check-in timing aligns with prior runs.'], divergentSignals: [] },
});