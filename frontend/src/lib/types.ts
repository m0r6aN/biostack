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
  knowledgeEntryId?: string | null;
  canonicalName?: string;
  isCanonical?: boolean;
  category: string;
  startDate: string;
  endDate: string | null;
  status: string;
  notes: string;
  sourceType: string;
  goal?: string;
  source?: string;
  pricePaid?: number;
  calculatorResultId?: string | null;
}

export interface CheckIn {
  id: string;
  personId: string;
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
  id?: string;
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
  vialCompatibility?: string;
  recommendedDosage: string;
  standardDosageRange?: string;
  maxReportedDose?: string;
  frequency: string;
  preferredTimeOfDay: string;
  weeklyDosageSchedule: string[];
  incrementalEscalationSteps?: string[];
  tieredDosing?: {
    beginner?: DoseTier;
    moderate?: DoseTier;
    advanced?: DoseTier;
  } | null;
  drugInteractions: string[];
  optimizationProtein: string;
  optimizationCarbs: string;
  optimizationSupplements: string[] | string;
  optimizationSleep: string;
  optimizationExercise: string;
}

export interface DoseTier {
  startDose: string;
  escalation: string;
  maxDose: string;
  weeklySchedule: string[];
  safetyNotes: string;
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

export interface CalculatorResultRecord {
  id: string;
  personId: string;
  compoundRecordId: string | null;
  calculatorKind: string;
  inputs: Record<string, string>;
  outputs: Record<string, string>;
  unit: string;
  formula: string;
  displaySummary: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface SaveCalculatorResultRequest {
  calculatorKind: string;
  inputs: Record<string, string>;
  outputs: Record<string, string>;
  unit: string;
  formula: string;
  displaySummary: string;
  compoundRecordId?: string | null;
}

export interface SchedulePreview {
  label: string;
  source: string;
  frequency: string;
  preferredTimeOfDay: string;
  weeklyDosageSchedule: string[];
  incrementalEscalationSteps: string[];
  tieredDosingNotes: string[];
  standardDosageRange: string;
  maxReportedDose: string;
}

export interface StackCompoundIntelligence {
  compoundRecordId: string;
  name: string;
  status: string;
  startDate: string | null;
  endDate: string | null;
  isCanonical: boolean;
  knowledgeEntryId: string | null;
  canonicalName: string;
  pathways: string[];
  evidenceTier: string;
  schedulePreview: SchedulePreview | null;
}

export interface StackSignal {
  kind: string;
  severity: 'positive' | 'caution' | 'neutral' | string;
  title: string;
  detail: string;
  compoundNames: string[];
  source: string;
}

export interface PathwayOverlap {
  pathway: string;
  compoundNames: string[];
}

export interface EvidenceTierSummary {
  evidenceTier: string;
  count: number;
  compoundNames: string[];
}

export interface CurrentStackIntelligence {
  personId: string;
  activeCompounds: StackCompoundIntelligence[];
  signals: StackSignal[];
  sharedPathwayOverlap: PathwayOverlap[];
  evidenceTierSummary: EvidenceTierSummary[];
  unresolvedCompounds: StackCompoundIntelligence[];
  framing: string;
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
