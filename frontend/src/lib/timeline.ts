import { TimelineEvent, TimelineEventType } from './types';

export type NormalizedTimelineEventType = TimelineEventType | 'unknown';

const TIMELINE_EVENT_TYPE_MAP: Record<string, NormalizedTimelineEventType> = {
  '1': 'compound_added',
  compoundadded: 'compound_added',
  compoundstarted: 'compound_added',
  compound_added: 'compound_added',
  compound_started: 'compound_added',
  '2': 'compound_ended',
  compoundended: 'compound_ended',
  compound_ended: 'compound_ended',
  '3': 'check_in',
  checkin: 'check_in',
  checkincreated: 'check_in',
  check_in: 'check_in',
  check_in_created: 'check_in',
  '4': 'phase_started',
  phasestarted: 'phase_started',
  protocolphasestarted: 'phase_started',
  phase_started: 'phase_started',
  protocol_phase_started: 'phase_started',
  '5': 'phase_ended',
  phaseended: 'phase_ended',
  protocolphaseended: 'phase_ended',
  phase_ended: 'phase_ended',
  protocol_phase_ended: 'phase_ended',
  knowledgeupdate: 'knowledge_update',
  knowledge_update: 'knowledge_update',
};

function normalizeLookupKey(eventType: string | number | null | undefined): string {
  if (eventType === null || eventType === undefined) {
    return '';
  }

  return String(eventType)
    .trim()
    .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
    .toLowerCase()
    .replace(/[^a-z0-9_]/g, '');
}

export function normalizeTimelineEventType(
  eventType: string | number | null | undefined
): NormalizedTimelineEventType {
  const lookupKey = normalizeLookupKey(eventType);

  return TIMELINE_EVENT_TYPE_MAP[lookupKey] ?? 'unknown';
}

export function normalizeTimelineEvent(event: TimelineEvent): TimelineEvent {
  return {
    ...event,
    eventType: normalizeTimelineEventType(event.eventType),
  };
}