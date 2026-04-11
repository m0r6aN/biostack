import { normalizeTimelineEvent, normalizeTimelineEventType } from '@/lib/timeline';
import { describe, expect, it } from 'vitest';

describe('timeline normalization', () => {
  it('maps backend enum names to frontend timeline event types', () => {
    expect(normalizeTimelineEventType('CompoundStarted')).toBe('compound_added');
    expect(normalizeTimelineEventType('CompoundEnded')).toBe('compound_ended');
    expect(normalizeTimelineEventType('CheckInCreated')).toBe('check_in');
    expect(normalizeTimelineEventType('ProtocolPhaseStarted')).toBe('phase_started');
  });

  it('maps numeric enum values to frontend timeline event types', () => {
    expect(normalizeTimelineEventType(1)).toBe('compound_added');
    expect(normalizeTimelineEventType(2)).toBe('compound_ended');
    expect(normalizeTimelineEventType(4)).toBe('phase_started');
  });

  it('normalizes timeline events without changing the rest of the payload', () => {
    const event = normalizeTimelineEvent({
      id: 'timeline-1',
      personId: 'profile-1',
      eventType: 'CompoundStarted',
      title: 'Started TB-500',
      description: 'Added to the active stack.',
      occurredAtUtc: '2026-04-06T00:00:00Z',
      relatedEntityId: 'compound-1',
      relatedEntityType: 'CompoundRecord',
    });

    expect(event).toEqual(
      expect.objectContaining({
        eventType: 'compound_added',
        title: 'Started TB-500',
      })
    );
  });
});