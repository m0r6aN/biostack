import {
    GOAL_DEFINITIONS,
    getMockProfileGoalIds,
    resolveGoalDefinitions,
    setMockProfileGoalIds,
} from './goals';
import { getApiBaseUrl } from './apiBase';
import { normalizeTimelineEvent } from './timeline';
import {
    CalculatorResult,
    CheckIn,
    CompoundRecord,
    ConversionRequest,
    CreateCheckInRequest,
    CreateProfileRequest,
    GoalDefinition,
    InteractionFlag,
    KnowledgeEntry,
    PersonProfile,
    ProfileGoal,
    CurrentStackIntelligence,
    CurrentSubscription,
    ProtocolConsolePayload,
    Protocol,
    ProtocolComputationRecord,
    ProtocolDriftSnapshot,
    ProtocolPatternSnapshot,
    ProtocolSequenceExpectationSnapshot,
    ProtocolReview,
    ProtocolReviewCompletedEvent,
    ProtocolRun,
    ProtocolPhase,
    ReconstitutionRequest,
    TimelineEvent,
    VolumeRequest,
} from './types';

export class ApiError extends Error {
  status: number;
  code?: string;
  upgradeRequired?: boolean;
  limit?: number | null;
  tier?: string;

  constructor(
    status: number,
    message: string,
    details?: { code?: string; upgradeRequired?: boolean; limit?: number | null; tier?: string }
  ) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = details?.code;
    this.upgradeRequired = details?.upgradeRequired;
    this.limit = details?.limit;
    this.tier = details?.tier;
  }
}

export class ApiClient {
  private baseUrl: string;
  constructor(baseUrl: string = getApiBaseUrl()) {
    this.baseUrl = baseUrl;
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(options.headers as Record<string, string>),
    };

    const response = await fetch(url, { ...options, headers, credentials: 'include' });

    if (!response.ok) {
      let message = `API Error: ${response.status} ${response.statusText}`;
      let details: { code?: string; upgradeRequired?: boolean; limit?: number | null; tier?: string } | undefined;

      try {
        const body = await response.json();
        message = body.message || body.error || message;
        details = body;
      } catch {
      }

      throw new ApiError(response.status, message, details);
    }

    if (response.status === 204) {
      return undefined as T;
    }

    return response.json();
  }

  // Profiles
  async getProfiles(): Promise<PersonProfile[]> {
    return this.request<PersonProfile[]>('/api/v1/profiles');
  }

  async getProfile(id: string): Promise<PersonProfile> {
    return this.request(`/api/v1/profiles/${id}`);
  }

  async createProfile(
    profile: CreateProfileRequest
  ): Promise<PersonProfile> {
    return this.request('/api/v1/profiles', {
      method: 'POST',
      body: JSON.stringify(profile),
    });
  }

  async updateProfile(
    id: string,
    profile: Partial<PersonProfile>
  ): Promise<PersonProfile> {
    return this.request(`/api/v1/profiles/${id}`, {
      method: 'PUT',
      body: JSON.stringify(profile),
    });
  }

  async deleteProfile(id: string): Promise<void> {
    return this.request(`/api/v1/profiles/${id}`, {
      method: 'DELETE',
    });
  }


  // Compounds
  async getCompounds(profileId: string): Promise<CompoundRecord[]> {
    return this.request<CompoundRecord[]>(
      `/api/v1/profiles/${profileId}/compounds`
    );
  }

  async createCompound(
    profileId: string,
    compound: Omit<CompoundRecord, 'id'>
  ): Promise<CompoundRecord> {
    return this.request(`/api/v1/profiles/${profileId}/compounds`, {
      method: 'POST',
      body: JSON.stringify(compound),
    });
  }

  async updateCompound(
    compoundId: string,
    compound: Partial<CompoundRecord>
  ): Promise<CompoundRecord> {
    return this.request(`/api/v1/compounds/${compoundId}`, {
      method: 'PUT',
      body: JSON.stringify(compound),
    });
  }

  async deleteCompound(compoundId: string): Promise<void> {
    return this.request(`/api/v1/compounds/${compoundId}`, {
      method: 'DELETE',
    });
  }

  // Check-ins
  async getCheckIns(profileId: string): Promise<CheckIn[]> {
    return this.request<CheckIn[]>(
      `/api/v1/profiles/${profileId}/checkins`
    );
  }

  async createCheckIn(
    profileId: string,
    checkIn: CreateCheckInRequest
  ): Promise<CheckIn> {
    return this.request(`/api/v1/profiles/${profileId}/checkins`, {
      method: 'POST',
      body: JSON.stringify(checkIn),
    });
  }

  // Protocol Phases
  async getProtocolPhases(profileId: string): Promise<ProtocolPhase[]> {
    return this.request<ProtocolPhase[]>(
      `/api/v1/profiles/${profileId}/phases`
    );
  }

  async createProtocolPhase(
    profileId: string,
    phase: Omit<ProtocolPhase, 'id'>
  ): Promise<ProtocolPhase> {
    return this.request(`/api/v1/profiles/${profileId}/phases`, {
      method: 'POST',
      body: JSON.stringify(phase),
    });
  }

  // Protocols
  async getProtocols(profileId: string): Promise<Protocol[]> {
    return this.request<Protocol[]>(`/api/v1/profiles/${profileId}/protocols`);
  }

  async getProtocol(protocolId: string): Promise<Protocol> {
    return this.request<Protocol>(`/api/v1/protocols/${protocolId}`);
  }

  async getProtocolReview(protocolId: string): Promise<ProtocolReview> {
    return this.request<ProtocolReview>(`/api/v1/protocols/${protocolId}/review`);
  }

  async getProtocolPatterns(protocolId: string): Promise<ProtocolPatternSnapshot> {
    return this.request<ProtocolPatternSnapshot>(`/api/v1/protocols/${protocolId}/patterns`);
  }

  async getProtocolDrift(protocolId: string): Promise<ProtocolDriftSnapshot> {
    return this.request<ProtocolDriftSnapshot>(`/api/v1/protocols/${protocolId}/drift`);
  }

  async getProtocolSequenceExpectation(protocolId: string): Promise<ProtocolSequenceExpectationSnapshot> {
    return this.request<ProtocolSequenceExpectationSnapshot>(`/api/v1/protocols/${protocolId}/sequence-expectation`);
  }

  async completeProtocolReview(
    protocolId: string,
    runId?: string | null,
    notes?: string
  ): Promise<ProtocolReviewCompletedEvent> {
    return this.request<ProtocolReviewCompletedEvent>(`/api/v1/protocols/${protocolId}/review/complete`, {
      method: 'POST',
      body: JSON.stringify({ runId, notes }),
    });
  }

  async recordProtocolComputation(
    protocolId: string,
    payload: {
      runId?: string | null;
      type: string;
      inputSnapshot: string;
      outputResult: string;
    }
  ): Promise<ProtocolComputationRecord> {
    return this.request<ProtocolComputationRecord>(`/api/v1/protocols/${protocolId}/computations`, {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  }

  async saveCurrentStackAsProtocol(profileId: string, name: string): Promise<Protocol> {
    return this.request<Protocol>(`/api/v1/profiles/${profileId}/protocols`, {
      method: 'POST',
      body: JSON.stringify({ name }),
    });
  }

  async startProtocolRun(protocolId: string): Promise<ProtocolRun> {
    return this.request<ProtocolRun>(`/api/v1/protocols/${protocolId}/runs`, {
      method: 'POST',
    });
  }

  async completeProtocolRun(runId: string): Promise<ProtocolRun> {
    return this.request<ProtocolRun>(`/api/v1/protocols/runs/${runId}/complete`, {
      method: 'POST',
    });
  }

  async abandonProtocolRun(runId: string): Promise<ProtocolRun> {
    return this.request<ProtocolRun>(`/api/v1/protocols/runs/${runId}/abandon`, {
      method: 'POST',
    });
  }

  async evolveProtocolFromRun(runId: string, name?: string): Promise<Protocol> {
    return this.request<Protocol>(`/api/v1/protocols/runs/${runId}/evolve`, {
      method: 'POST',
      body: JSON.stringify({ name }),
    });
  }

  async getActiveProtocolRun(profileId: string): Promise<ProtocolRun | null> {
    const run = await this.request<ProtocolRun | undefined>(`/api/v1/profiles/${profileId}/protocols/active-run`);
    return run ?? null;
  }

  async getProtocolConsole(profileId: string): Promise<ProtocolConsolePayload> {
    return this.request<ProtocolConsolePayload>(`/api/v1/profiles/${profileId}/protocols/mission-control`);
  }

  async getCurrentStackIntelligence(profileId: string): Promise<CurrentStackIntelligence> {
    return this.request<CurrentStackIntelligence>(
      `/api/v1/profiles/${profileId}/protocols/current-stack-intelligence`
    );
  }

  // Timeline
  async getTimeline(profileId: string): Promise<TimelineEvent[]> {
    const events = await this.request<TimelineEvent[]>(`/api/v1/profiles/${profileId}/timeline`);
    return events.map(normalizeTimelineEvent);
  }

  // Knowledge
  async getAllKnowledgeCompounds(): Promise<KnowledgeEntry[]> {
    return this.request<KnowledgeEntry[]>('/api/v1/knowledge/compounds');
  }

  async getKnowledgeEntry(name: string): Promise<KnowledgeEntry> {
    return this.request(`/api/v1/knowledge/compounds/${encodeURIComponent(name)}`);
  }

  // Interaction / Overlap checking
  async checkOverlap(compoundNames: string[]): Promise<InteractionFlag[]> {
    const data = await this.request<{ overlaps: InteractionFlag[] }>(
      '/api/v1/knowledge/overlap-check',
      {
        method: 'POST',
        body: JSON.stringify({ compoundNames }),
      }
    );
    return data.overlaps;
  }

  // Calculators
  async calculateReconstitution(
    request: ReconstitutionRequest
  ): Promise<CalculatorResult> {
    return this.request('/api/v1/calculators/reconstitution', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async calculateVolume(request: VolumeRequest): Promise<CalculatorResult> {
    return this.request('/api/v1/calculators/volume', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async calculateConversion(
    request: ConversionRequest
  ): Promise<CalculatorResult> {
    const payload =
      request.conversionFactor && request.conversionFactor > 0
        ? request
        : {
            amount: request.amount,
            fromUnit: request.fromUnit,
            toUnit: request.toUnit,
          };

    return this.request('/api/v1/calculators/conversion', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  }

  async captureLead(email: string, source: string): Promise<void> {
    return this.request('/api/v1/leads/capture', {
      method: 'POST',
      body: JSON.stringify({ email, source }),
    });
  }

  async getCurrentSubscription(): Promise<CurrentSubscription> {
    return this.request<CurrentSubscription>('/api/v1/billing/subscription');
  }

  async createCheckoutSession(planCode: 'operator' | 'commander'): Promise<{ url: string }> {
    return this.request<{ url: string }>('/api/v1/billing/checkout', {
      method: 'POST',
      body: JSON.stringify({ planCode }),
    });
  }

  async createBillingPortalSession(): Promise<{ url: string }> {
    return this.request<{ url: string }>('/api/v1/billing/portal', {
      method: 'POST',
    });
  }

  // Goals
  async getGoalDefinitions(): Promise<GoalDefinition[]> {
    try {
      return await this.request<GoalDefinition[]>('/api/v1/goals');
    } catch {
      return GOAL_DEFINITIONS;
    }
  }

  async getProfileGoals(profileId: string): Promise<GoalDefinition[]> {
    try {
      const profileGoals = await this.request<ProfileGoal[]>(
        `/api/v1/profiles/${profileId}/goals`
      );
      return profileGoals
        .map(pg => pg.goalDefinition)
        .filter((g): g is GoalDefinition => g !== undefined);
    } catch {
      const ids = getMockProfileGoalIds(profileId);
      return resolveGoalDefinitions(ids);
    }
  }

  async setProfileGoals(profileId: string, goalIds: string[]): Promise<void> {
    try {
      await this.request(`/api/v1/profiles/${profileId}/goals`, {
        method: 'POST',
        body: JSON.stringify({ goalIds }),
      });
    } catch {
      setMockProfileGoalIds(profileId, goalIds);
    }
  }
}

export const apiClient = new ApiClient();
