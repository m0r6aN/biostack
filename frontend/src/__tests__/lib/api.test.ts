import { ApiClient } from '@/lib/api';
import { GOAL_DEFINITIONS } from '@/lib/goals';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('ApiClient', () => {
  const fetchMock = vi.fn();
  let client: ApiClient;

  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
    localStorage.clear();
    client = new ApiClient('https://api.example.test');
  });

  it('sends cookie credentials with API requests', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [],
    });

    await client.getProfiles();

    expect(fetchMock).toHaveBeenCalledWith(
      'https://api.example.test/api/v1/profiles',
      expect.objectContaining({
        credentials: 'include',
        headers: expect.objectContaining({
          'Content-Type': 'application/json',
        }),
      })
    );
  });

  it('throws a descriptive error when the API request fails', async () => {
    fetchMock.mockResolvedValue({
      ok: false,
      status: 500,
      statusText: 'Server Error',
    });

    await expect(client.getProfiles()).rejects.toThrow('API Error: 500 Server Error');
  });

  it('omits a non-positive conversionFactor from the conversion payload', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ input: 1, output: 1000, unit: 'mcg', formula: '', disclaimer: '' }),
    });

    await client.calculateConversion({
      amount: 1,
      fromUnit: 'mg',
      toUnit: 'mcg',
      conversionFactor: 0,
    });

    expect(fetchMock).toHaveBeenCalledWith(
      'https://api.example.test/api/v1/calculators/conversion',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          amount: 1,
          fromUnit: 'mg',
          toUnit: 'mcg',
        }),
      })
    );
  });

  it('preserves an explicit conversionFactor when provided', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ input: 1, output: 1000, unit: 'mcg', formula: '', disclaimer: '' }),
    });

    await client.calculateConversion({
      amount: 1,
      fromUnit: 'mg',
      toUnit: 'mcg',
      conversionFactor: 1000,
    });

    expect(fetchMock).toHaveBeenCalledWith(
      'https://api.example.test/api/v1/calculators/conversion',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          amount: 1,
          fromUnit: 'mg',
          toUnit: 'mcg',
          conversionFactor: 1000,
        }),
      })
    );
  });

  it('posts lead captures to the public leads endpoint', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 204,
    });

    await client.captureLead('user@example.com', 'reconstitution-calculator');

    expect(fetchMock).toHaveBeenCalledWith(
      'https://api.example.test/api/v1/leads/capture',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          email: 'user@example.com',
          source: 'reconstitution-calculator',
        }),
      })
    );
  });

  it('posts provider access requests to the public provider queue endpoint', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 202,
      json: async () => ({ requestId: 'request-1', status: 'pending', submittedAtUtc: '2026-07-11T12:00:00Z' }),
    });

    const payload = {
      email: 'provider@example.com',
      name: 'Provider Name',
      organization: 'Example Practice',
      role: 'Owner',
      consent: true,
    };
    await client.requestProviderAccess(payload);

    expect(fetchMock).toHaveBeenCalledWith(
      'https://api.example.test/api/v1/provider-access/requests',
      expect.objectContaining({ method: 'POST', body: JSON.stringify(payload) })
    );
  });

  it('falls back to local goal definitions when the goals endpoint fails', async () => {
    fetchMock.mockResolvedValue({
      ok: false,
      status: 404,
      statusText: 'Not Found',
    });

    const goals = await client.getGoalDefinitions();

    expect(goals).toEqual(GOAL_DEFINITIONS);
  });

  it('returns nested profile goal definitions from the authenticated API contract', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [{
        id: 'profile-goal-1',
        profileId: 'profile-1',
        goalDefinitionId: GOAL_DEFINITIONS[0].id,
        goalDefinition: GOAL_DEFINITIONS[0],
        createdAtUtc: '2026-08-28T12:00:00Z',
      }],
    });

    const goals = await client.getProfileGoals('profile-1');

    expect(goals).toEqual([GOAL_DEFINITIONS[0]]);
  });

  it('posts the complete profile goal set to the authenticated API contract', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [],
    });

    await client.setProfileGoals('profile-1', [GOAL_DEFINITIONS[1].id]);

    expect(fetchMock).toHaveBeenCalledWith(
      'https://api.example.test/api/v1/profiles/profile-1/goals',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ goalIds: [GOAL_DEFINITIONS[1].id] }),
      })
    );
  });

  it('does not mask profile goal authorization or availability failures with local state', async () => {
    fetchMock.mockResolvedValue({
      ok: false,
      status: 403,
      statusText: 'Forbidden',
      json: async () => ({ code: 'consent_required' }),
    });

    await expect(client.setProfileGoals('profile-1', [GOAL_DEFINITIONS[1].id]))
      .rejects.toMatchObject({ status: 403, code: 'consent_required' });
    expect(localStorage.getItem('biostack_profile_goals')).toBeNull();
  });

  describe('analyzeProtocol', () => {
    const okAnalyzeResponse = {
      ok: true,
      status: 200,
      json: async () => ({ compounds: [], warnings: [], summary: '' }),
    };

    it('sends goals + context fields on the JSON path', async () => {
      fetchMock.mockResolvedValue(okAnalyzeResponse);

      await client.analyzeProtocol({
        inputType: 'Paste',
        inputText: 'x',
        goal: 'healing',
        secondaryGoals: ['fat loss'],
        sex: 'male',
        age: 40,
        weight: 90,
        existingStackContext: ['creatine'],
      });

      const [, options] = fetchMock.mock.calls[0] as [string, RequestInit];
      const body = JSON.parse(options.body as string);
      expect(body.goal).toBe('healing');
      expect(body.secondaryGoals).toEqual(['fat loss']);
      expect(body.sex).toBe('male');
      expect(body.age).toBe(40);
      expect(body.weight).toBe(90);
      expect(body.existingStackContext).toEqual(['creatine']);
    });

    it('appends goals + context fields to FormData on the file path', async () => {
      fetchMock.mockResolvedValue(okAnalyzeResponse);

      const file = new File(['data'], 'test.pdf', { type: 'application/pdf' });

      await client.analyzeProtocol({
        inputType: 'FileUpload',
        file,
        goal: 'healing',
        secondaryGoals: ['fat loss'],
        sex: 'male',
        age: 40,
        weight: 90,
        existingStackContext: ['creatine'],
      });

      const [, options] = fetchMock.mock.calls[0] as [string, RequestInit];
      const formData = options.body as FormData;
      expect(formData.get('secondaryGoals')).toBe('fat loss');
      expect(formData.get('sex')).toBe('male');
      expect(formData.get('age')).toBe('40');
      expect(formData.get('weight')).toBe('90');
      expect(formData.get('existingStackContext')).toBe('creatine');
    });

    it('does NOT append optional context fields to FormData when absent', async () => {
      fetchMock.mockResolvedValue(okAnalyzeResponse);

      const file = new File(['data'], 'test.pdf', { type: 'application/pdf' });

      await client.analyzeProtocol({ inputType: 'FileUpload', file });

      const [, options] = fetchMock.mock.calls[0] as [string, RequestInit];
      const formData = options.body as FormData;
      expect(formData.get('sex')).toBeNull();
      expect(formData.get('age')).toBeNull();
      expect(formData.get('weight')).toBeNull();
      expect(formData.get('secondaryGoals')).toBeNull();
      expect(formData.get('existingStackContext')).toBeNull();
    });
  });

});
