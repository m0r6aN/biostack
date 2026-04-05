import {
    GOAL_DEFINITIONS,
    getMockProfileGoalIds,
    resolveGoalDefinitions,
    setMockProfileGoalIds,
} from './goals';
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
    ProtocolPhase,
    ReconstitutionRequest,
    TimelineEvent,
    VolumeRequest,
} from './types';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

export class ApiClient {
  private baseUrl: string;
  private accessToken: string | null = null;

  constructor(baseUrl: string = API_URL) {
    this.baseUrl = baseUrl;
  }

  /** Call this from client components after reading useSession() */
  setAccessToken(token: string | null) {
    this.accessToken = token;
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

    if (this.accessToken) {
      headers['Authorization'] = `Bearer ${this.accessToken}`;
    }

    const response = await fetch(url, { ...options, headers });

    if (!response.ok) {
      throw new Error(`API Error: ${response.status} ${response.statusText}`);
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

  // Timeline
  async getTimeline(profileId: string): Promise<TimelineEvent[]> {
    return this.request<TimelineEvent[]>(
      `/api/v1/profiles/${profileId}/timeline`
    );
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
