import { ApiClient } from '@/lib/api';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('ApiClient timeline normalization', () => {
  const fetchMock = vi.fn();
  let client: ApiClient;

  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
    client = new ApiClient('https://api.example.test');
  });

  it('normalizes backend timeline event types for UI filters and icons', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [
        {
          id: 'timeline-1',
          personId: 'profile-1',
          eventType: 'CompoundStarted',
          title: 'Started BPC-157',
          description: 'Added to active stack.',
          occurredAtUtc: '2026-04-06T00:00:00Z',
          relatedEntityId: 'compound-1',
          relatedEntityType: 'CompoundRecord',
        },
      ],
    });

    const events = await client.getTimeline('profile-1');

    expect(events[0].eventType).toBe('compound_added');
  });
});