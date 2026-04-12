// Core data types for BioStack Mission Control

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
  detail: string;
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
