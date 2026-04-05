import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiClient } from '@/lib/api';
import { GOAL_DEFINITIONS } from '@/lib/goals';

describe('ApiClient', () => {
  const fetchMock = vi.fn();
  let client: ApiClient;

  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
    localStorage.clear();
    client = new ApiClient('https://api.example.test');
  });

  it('adds the bearer token when one is set', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [],
    });

    client.setAccessToken('test-token');
    await client.getProfiles();

    expect(fetchMock).toHaveBeenCalledWith(
      'https://api.example.test/api/v1/profiles',
      expect.objectContaining({
        headers: expect.objectContaining({
          Authorization: 'Bearer test-token',
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

  it('falls back to local goal definitions when the goals endpoint fails', async () => {
    fetchMock.mockResolvedValue({
      ok: false,
      status: 404,
      statusText: 'Not Found',
    });

    const goals = await client.getGoalDefinitions();

    expect(goals).toEqual(GOAL_DEFINITIONS);
  });

  it('falls back to localStorage-backed profile goals when the API is unavailable', async () => {
    fetchMock.mockResolvedValue({
      ok: false,
      status: 503,
      statusText: 'Unavailable',
    });

    localStorage.setItem('biostack_mock_goals_profile-1', JSON.stringify([GOAL_DEFINITIONS[0].id]));

    const goals = await client.getProfileGoals('profile-1');

    expect(goals).toEqual([GOAL_DEFINITIONS[0]]);
  });

  it('stores profile goals locally when the API write fails', async () => {
    fetchMock.mockResolvedValue({
      ok: false,
      status: 503,
      statusText: 'Unavailable',
    });

    await client.setProfileGoals('profile-1', [GOAL_DEFINITIONS[1].id]);

    expect(localStorage.getItem('biostack_mock_goals_profile-1')).toBe(
      JSON.stringify([GOAL_DEFINITIONS[1].id])
    );
  });
});
