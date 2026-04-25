// Core data types for BioStack Protocol Console

export interface GoalDefinition {
  id: string;
  name: string;
  category: string;
  description: string;
  isActive: boolean;
}

export interface ProfileGoal {
  id: string;
  profileId: string;
  goalDefinitionId: string;
  goalDefinition?: GoalDefinition;
  createdAtUtc: string;
}

export interface PersonProfile {
  id: string;
  displayName: string;
  sex: string;
  age?: number;
  weight: number;
  goalSummary?: string;
  goals?: ProfileGoal[];
  notes: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}


export interface CreateProfileRequest {
  displayName: string;
  sex: string;
  age?: number;
  weight: number;
  notes: string;
  goalSummary?: string;
}


export interface CompoundRecord {
  id: string;
  personId: string;
  name: string;
  category: string;
  startDate: string;
  endDate: string | null;
  status: string;
  notes: string;
  sourceType: string;
  goal?: string;
  source?: string;
  pricePaid?: number;
}

export interface CurrentSubscription {
  tier: string;
  status: string;
  productCode: string;
  isPaid: boolean;
  cancelAtPeriodEnd: boolean;
  currentPeriodEndUtc: string | null;
  features: Record<string, boolean>;
  limits: Record<string, number | null>;
}

export interface CheckIn {
  id: string;
  personId: string;
  protocolRunId: string | null;
  date: string;
  weight: number;
  sleepQuality: number;
  energy: number;
  appetite: number;
  recovery: number;
  focus?: number;
  thoughtClarity?: number;
  skinQuality?: number;
  digestiveHealth?: number;
  strength?: number;
  endurance?: number;
  jointPain?: number;
  eyesight?: number;
  sideEffects?: string;
  taggedPhotoUrls?: string[];
  giSymptoms: string;
  mood: string;
  notes: string;
}

export interface CreateCheckInRequest {
  date: string;
  weight: number;
  sleepQuality: number;
  energy: number;
  appetite: number;
  recovery: number;
  focus?: number;
  thoughtClarity?: number;
  skinQuality?: number;
  digestiveHealth?: number;
  strength?: number;
  endurance?: number;
  jointPain?: number;
  eyesight?: number;
  sideEffects?: string;
  photoUrls?: string[];
  giSymptoms?: string;
  mood?: string;
  notes?: string;
}

export type Day7ReviewTrend =
  | 'improving'
  | 'flat'
  | 'declining'
  | 'insufficient_data';

export interface Day7Review {
  isEarned: boolean;
  coveredDays: number;
  requiredDays: number;
  sleepTrend: Day7ReviewTrend;
  energyTrend: Day7ReviewTrend;
  recoveryTrend: Day7ReviewTrend;
  trendSummary: string;
  signalStrength: 'weak' | 'moderate' | 'clear';
  alignmentWithExpected: 'yes' | 'no' | 'unclear';
  nextStep: 'continue' | 'reassess' | 'track_longer';
  confidenceNote: string;
}

export interface ProtocolPhase {
  id: string;
  personId: string;
  name: string;
  startDate: string;
  endDate: string | null;
  notes: string;
}

export interface StackScore {
  score: number;
  breakdown: {
    synergy: number;
    redundancy: number;
    conflicts: number;
    evidence: number;
  };
  chips: string[];
}

export interface SimulationResult {
  timeline: Array<{
    dayRange: string;
    signals: string[];
  }>;
  insights: string[];
}

export interface InteractionFinding {
  type: 'Neutral' | 'Synergistic' | 'Complementary' | 'Redundant' | 'Interfering' | string;
  compounds: string[];
  message: string;
  confidence: number;
}

export interface InteractionResult {
  compoundA: string;
  compoundB: string;
  type: 'Neutral' | 'Synergistic' | 'Complementary' | 'Redundant' | 'Interfering' | string;
  confidence: number;
  sharedPathways: string[];
  reason: string;
  hintBacked: boolean;
}

export interface InteractionIntelligence {
  summary: {
    synergies: number;
    redundancies: number;
    interferences: number;
  };
  score: {
    synergyScore: number;
    redundancyPenalty: number;
    interferencePenalty: number;
  };
  compositeScore: number;
  topFindings: InteractionFinding[];
  interactions: InteractionResult[];
  counterfactuals: Array<{
    removedCompound: string;
    variantScore: number;
    deltaScore: number;
    deltaPercent: number;
    verdict: 'improves' | 'worsens' | 'no_meaningful_change' | string;
    recommendation: string;
    summary: {
      synergies: number;
      redundancies: number;
      interferences: number;
    };
    topFindings: InteractionFinding[];
  }>;
  swaps: Array<{
    originalCompound: string;
    candidateCompound: string;
    baselineScore: number;
    variantScore: number;
    deltaScore: number;
    deltaPercent: number;
    verdict: 'likely_improves' | 'little_expected_change' | 'likely_worsens' | string;
    reasons: string[];
    recommendation: string;
    similarityScore: number;
    summary: {
      synergies: number;
      redundancies: number;
      interferences: number;
    };
    topFindings: InteractionFinding[];
  }>;
}

export interface ProtocolActualComparison {
  simulation: SimulationResult;
  run: ProtocolRun | null;
  runSummary: ProtocolRunSummary | null;
  observations: ProtocolRunObservation[];
  actualTrends: Array<{
    metric: string;
    beforeAverage: number | null;
    afterAverage: number | null;
    direction: string;
  }>;
  insights: ProtocolRunInsight[];
  highlights: string[];
}

export interface ProtocolReview {
  lineageRootProtocolId: string;
  requestedProtocolId: string;
  lineageName: string;
  versions: ProtocolReviewVersion[];
  sections: ProtocolReviewSection[];
  timeline: ProtocolReviewTimelineEvent[];
  safetyNotes: string[];
}

export interface ProtocolReviewVersion {
  protocolId: string;
  name: string;
  version: number;
  isDraft: boolean;
  parentProtocolId: string | null;
  evolvedFromRunId: string | null;
  evolutionContext: string;
  createdAtUtc: string;
  versionDiff: ProtocolVersionDiff | null;
  runs: ProtocolReviewRun[];
}

export interface ProtocolReviewRun {
  run: ProtocolRun;
  summary: ProtocolRunSummary;
  observations: ProtocolRunObservation[];
  trends: Array<{
    metric: string;
    beforeAverage: number | null;
    afterAverage: number | null;
    direction: string;
  }>;
  insights: ProtocolRunInsight[];
}

export interface ProtocolReviewSection {
  type: 'alignment' | 'divergence' | 'neutral' | 'change' | 'gap' | string;
  title: string;
  summary: string;
  evidence: string[];
}

export interface ProtocolReviewTimelineEvent {
  occurredAtUtc: string;
  eventType: string;
  label: string;
  protocolId: string | null;
  runId: string | null;
  checkInId: string | null;
  computationId: string | null;
  reviewCompletedEventId: string | null;
  detail: string;
}

export interface ProtocolPatternSnapshot {
  protocolId: string;
  historicalRunCount: number;
  patternConfidence: 'none' | 'low' | 'moderate' | string;
  metricPatterns: MetricPatternSummary[];
  eventPatterns: EventPatternSummary[];
  sequencePatterns: SequencePatternSummary[];
  currentRunComparison: PatternComparisonSummary | null;
}

export interface MetricPatternSummary {
  metric: string;
  observation: string;
}

export interface EventPatternSummary {
  eventType: string;
  timingPattern: string;
}

export interface SequencePatternSummary {
  sequence: string[];
  description: string;
}

export interface PatternComparisonSummary {
  similarity: 'none' | 'low' | 'moderate' | 'high' | string;
  matchingSignals: string[];
  divergentSignals: string[];
}

export interface ProtocolDriftSnapshot {
  protocolId: string;
  driftState: 'none' | 'mild' | 'moderate' | 'regime_shift' | string;
  baselineSource: 'insufficient_history' | 'historical_runs' | string;
  signals: DriftSignalSummary[];
  regimeClassification: RegimeClassificationSummary | null;
}

export interface ProtocolSequenceExpectationSnapshot {
  protocolId: string;
  baselineSource: 'insufficient_history' | 'historical_runs' | string;
  historicalRunCount: number;
  expectedNextEvent: ExpectedNextEventSummary | null;
  commonTransitions: ExpectedTransitionSummary[];
  currentStatus: CurrentSequenceStatusSummary | null;
}

export interface ExpectedNextEventSummary {
  eventType: string;
  description: string;
  timingWindow: string;
  confidence: 'none' | 'low' | 'moderate' | string;
}

export interface ExpectedTransitionSummary {
  fromState: string;
  toEventType: string;
  timingPattern: string;
  observedCount: number;
}

export interface CurrentSequenceStatusSummary {
  state: 'unknown' | 'aligned' | 'pending' | 'late' | 'diverging' | string;
  notes: string[];
}

export interface DriftSignalSummary {
  type: 'checkin_timing' | 'run_duration' | 'computation_timing' | 'review_timing' | 'sequence_break' | 'signal_density' | string;
  severity: 'mild' | 'moderate' | string;
  description: string;
}

export interface RegimeClassificationSummary {
  state: 'stable' | 'drifting' | 'shifted' | string;
  contributingFactors: string[];
}

export interface ProtocolConsolePayload {
  activeRun: ProtocolRun | null;
  latestClosedRun: ProtocolRun | null;
  latestReviewSummary: ProtocolConsoleReviewSummary | null;
  recentEvolution: ProtocolConsoleEvolution | null;
  latestCheckInSignal: ProtocolConsoleCheckInSignal;
  observationSignals: ProtocolConsoleObservationSignal[];
  patternSnapshot: ProtocolPatternSnapshot | null;
  driftSnapshot: ProtocolDriftSnapshot | null;
  sequenceExpectationSnapshot: ProtocolSequenceExpectationSnapshot | null;
  cohesionTimeline: ProtocolReviewTimelineEvent[];
}

export interface ProtocolConsoleReviewSummary {
  protocolId: string;
  lineageRootProtocolId: string;
  lineageName: string;
  cue: string;
  signalType: string;
  versionCount: number;
  runCount: number;
  checkInCount: number;
}

export interface ProtocolConsoleEvolution {
  protocolId: string;
  parentProtocolId: string | null;
  evolvedFromRunId: string | null;
  label: string;
  summary: string;
  occurredAtUtc: string;
  changes: ProtocolVersionChange[];
}

export interface ProtocolConsoleCheckInSignal {
  checkInId: string | null;
  protocolRunId: string | null;
  date: string | null;
  cue: string;
  attachedCheckInCount: number;
  hasObservationGap: boolean;
}

export interface ProtocolConsoleObservationSignal {
  type: 'gap' | 'trend_shift' | string;
  severity: 'low' | 'medium' | 'high' | string;
  metric: string | null;
  detail: string;
}

export interface ProtocolComputationRecord {
  id: string;
  protocolId: string;
  runId: string | null;
  type: string;
  inputSnapshot: string;
  outputResult: string;
  timestampUtc: string;
}

export interface ProtocolReviewCompletedEvent {
  id: string;
  protocolId: string;
  runId: string | null;
  completedAtUtc: string;
  notes: string;
}

export interface ProtocolRun {
  id: string;
  protocolId: string;
  personId: string;
  protocolName: string;
  protocolVersion: number;
  startedAtUtc: string;
  endedAtUtc: string | null;
  status: 'active' | 'completed' | 'abandoned';
  notes: string;
}

export interface ProtocolRunInsight {
  type: 'alignment' | 'divergence' | 'neutral';
  message: string;
  relatedSignals: string[];
}

export interface ProtocolRunSignalSummary {
  metric: string;
  direction: string;
  magnitude: string;
}

export interface ProtocolRunSummary {
  run: ProtocolRun;
  daysActive: number;
  signals: ProtocolRunSignalSummary[];
  alignedCount: number;
  divergingCount: number;
}

export interface ProtocolRunObservation {
  checkInId: string;
  date: string;
  day: number;
  energy: number;
  sleepQuality: number;
  appetite: number;
  recovery: number;
}

export interface ProtocolItem {
  id: string;
  protocolId: string;
  compoundRecordId: string;
  calculatorResultId: string | null;
  notes: string;
  compound: CompoundRecord | null;
}

export interface Protocol {
  id: string;
  personId: string;
  name: string;
  version: number;
  parentProtocolId: string | null;
  originProtocolId: string | null;
  evolvedFromRunId: string | null;
  isDraft: boolean;
  evolutionContext: string;
  isCurrentVersion: boolean;
  priorVersions: ProtocolVersionSummary[];
  createdAtUtc: string;
  updatedAtUtc: string;
  items: ProtocolItem[];
  stackScore: StackScore;
  simulation: SimulationResult;
  interactionIntelligence: InteractionIntelligence;
  activeRun: ProtocolRun | null;
  versionDiff: ProtocolVersionDiff | null;
  actualComparison: ProtocolActualComparison | null;
}

export interface ProtocolVersionSummary {
  id: string;
  name: string;
  version: number;
  isDraft: boolean;
  createdAtUtc: string;
}

export interface ProtocolVersionDiff {
  fromProtocolId: string;
  toProtocolId: string;
  changes: ProtocolVersionChange[];
}

export interface ProtocolVersionChange {
  changeType: 'added' | 'removed' | 'edited' | 'unchanged';
  scope: 'compound' | 'schedule' | 'structure' | string;
  subject: string;
  before: string;
  after: string;
}

export interface CurrentStackIntelligence {
  stackScore: StackScore;
  simulation: SimulationResult;
  interactionIntelligence: InteractionIntelligence;
}

export type TimelineEventType =
  | 'compound_added'
  | 'compound_ended'
  | 'phase_started'
  | 'phase_ended'
  | 'check_in'
  | 'knowledge_update';

export interface TimelineEvent {
  id: string;
  personId: string;
  eventType: string;
  title: string;
  description: string;
  occurredAtUtc: string;
  relatedEntityId: string | null;
  relatedEntityType: string | null;
}

export interface KnowledgeEntry {
  canonicalName: string;
  aliases: string[];
  classification: string;
  regulatoryStatus: string;
  mechanismSummary: string;
  evidenceTier: string;
  sourceReferences: string[];
  notes: string;
  pathways: string[];
  benefits: string[];
  pairsWellWith: string[];
  avoidWith: string[];
  compatibleBlends: string[];
  recommendedDosage: string;
  frequency: string;
  preferredTimeOfDay: string;
  weeklyDosageSchedule: string[];
  drugInteractions: string[];
  optimizationProtein: string;
  optimizationCarbs: string;
  optimizationSupplements: string;
  optimizationSleep: string;
  optimizationExercise: string;
}

export interface InteractionFlag {
  compoundNames: string[];
  overlapType: string;
  pathwayTag: string;
  description: string;
  evidenceConfidence: string;
}

export interface ProtocolAnalyzerEntry {
  compoundName: string;
  dose: number;
  unit: string;
  frequency: string;
  duration: string;
}

export interface ProtocolAnalyzerIssue {
  type: 'redundancy' | 'overlap' | 'inefficiency' | 'excessive_compounds' | string;
  message: string;
  compounds: string[];
}

export interface ProtocolAnalyzerSuggestion {
  type: 'remove' | 'swap' | 'simplify' | 'clarify' | 'maintain' | string;
  message: string;
  compounds: string[];
}

export interface ProtocolAnalyzerScoreExplanation {
  baseScore: number;
  synergy: number;
  redundancy: number;
  interference: number;
}

export interface ProtocolAnalyzerBlendExpansion {
  blendName: string;
  components: string[];
}

export interface ProtocolAnalyzerCounterfactual {
  removedCompound: string;
  variantScore: number;
  deltaScore: number;
  deltaPercent: number;
  verdict: string;
  recommendation: string;
}

export interface ProtocolAnalyzerSwap {
  originalCompound: string;
  candidateCompound: string;
  baselineScore: number;
  variantScore: number;
  deltaScore: number;
  deltaPercent: number;
  verdict: string;
  recommendation: string;
  reasons: string[];
}

export interface ProtocolAnalyzerSimplifiedProtocol {
  compounds: ProtocolAnalyzerEntry[];
  score: number;
  removed: string[];
  reasons: string[];
}

export interface ProtocolAnalyzerGoalAwareOption {
  goal: string;
  compounds: ProtocolAnalyzerEntry[];
  score: number;
  reasons: string[];
}

export interface ProtocolAnalyzerCounterfactuals {
  baselineScore: number;
  bestRemoveOne: ProtocolAnalyzerCounterfactual[];
  bestSwapOne: ProtocolAnalyzerSwap[];
  bestSimplifiedProtocol: ProtocolAnalyzerSimplifiedProtocol | null;
  goalAwareOptions: ProtocolAnalyzerGoalAwareOption[];
}

export type ProtocolAnalyzerInputType = 'Paste' | 'FileUpload' | 'CameraScan' | 'Link';

export interface ProtocolAnalyzerArtifact {
  kind: string;
  label: string;
  preview: string;
}

export interface ProtocolAnalyzerResult {
  protocol: ProtocolAnalyzerEntry[];
  score: number;
  scoreExplanation: ProtocolAnalyzerScoreExplanation;
  issues: ProtocolAnalyzerIssue[];
  suggestions: ProtocolAnalyzerSuggestion[];
  decomposedBlends: ProtocolAnalyzerBlendExpansion[];
  unknownCompounds: string[];
  counterfactuals: ProtocolAnalyzerCounterfactuals;
  inputType: ProtocolAnalyzerInputType;
  sourceName: string | null;
  extractionWarnings: string[];
  parserWarnings: string[];
  lowConfidenceExtraction: boolean;
  extractedTextPreview: string | null;
  artifacts: ProtocolAnalyzerArtifact[];
}

export interface CalculatorResult {
  input: number;
  output: number;
  unit: string;
  formula: string;
  disclaimer: string;
}

export interface ReconstitutionRequest {
  peptideAmountMg: number;
  diluentVolumeMl: number;
}

export interface VolumeRequest {
  desiredDoseMcg: number;
  concentrationMcgPerMl: number;
}

export interface ConversionRequest {
  amount: number;
  fromUnit: string;
  toUnit: string;
  conversionFactor: number;
}

// API Response types
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
}

export interface ListResponse<T> {
  items: T[];
  total: number;
}
