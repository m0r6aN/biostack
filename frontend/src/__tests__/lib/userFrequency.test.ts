import { describe, expect, it, vi } from 'vitest';
import {
  EMPTY_FREQUENCY_FORM,
  FREQUENCY_KINDS,
  WEEKDAYS,
  browserTimeZone,
  describeFrequency,
  frequencyFormFromCompound,
  frequencyFormToPayload,
  needsStructuredFrequency,
  validateFrequencyForm,
  type FrequencyFormValues,
} from '@/lib/userFrequency';

const form = (patch: Partial<FrequencyFormValues>): FrequencyFormValues => ({ ...EMPTY_FREQUENCY_FORM, ...patch });

describe('frequency kinds', () => {
  it('offers exactly the agreed kinds', () => {
    expect(FREQUENCY_KINDS.map((kind) => kind.value)).toEqual(['daily', 'every_n_days', 'weekdays', 'times_per_week', 'as_needed']);
  });

  it('lists Monday first and Sunday last, using the .NET day numbers', () => {
    expect(WEEKDAYS.map((day) => day.short)).toEqual(['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']);
    expect(WEEKDAYS.map((day) => day.value)).toEqual([1, 2, 3, 4, 5, 6, 0]);
  });

  it('starts blank: nothing is chosen, defaulted or inferred', () => {
    expect(EMPTY_FREQUENCY_FORM).toEqual({ kind: '', intervalDays: '', weekdays: [], timesPerWeek: '', startDate: '', times: [] });
    expect(validateFrequencyForm(EMPTY_FREQUENCY_FORM)).toEqual({});
    expect(frequencyFormToPayload(EMPTY_FREQUENCY_FORM)).toEqual({
      frequencyKind: null,
      frequencyIntervalDays: null,
      frequencyWeekdays: null,
      frequencyTimesPerWeek: null,
      frequencyStartDate: null,
      frequencyTimesOfDay: null,
    });
  });
});

describe('validateFrequencyForm', () => {
  it('rejects extra detail with no kind chosen', () => {
    expect(validateFrequencyForm(form({ weekdays: [1, 4] })).kind).toBeTruthy();
    expect(validateFrequencyForm(form({ startDate: '2026-10-05' })).kind).toBeTruthy();
    expect(validateFrequencyForm(form({ times: ['08:00'] })).kind).toBeTruthy();
  });

  it('rejects an unknown kind', () => {
    expect(validateFrequencyForm(form({ kind: 'twice weekly' })).kind).toBeTruthy();
  });

  it.each(['', '1', '0', '366', '2.5', '-3', 'abc'])('rejects %s as an every-N-days interval', (interval) => {
    expect(validateFrequencyForm(form({ kind: 'every_n_days', intervalDays: interval })).intervalDays).toBeTruthy();
  });

  it.each(['2', '3', '365'])('accepts %s as an every-N-days interval', (interval) => {
    expect(validateFrequencyForm(form({ kind: 'every_n_days', intervalDays: interval }))).toEqual({});
  });

  it('requires at least one weekday for specific weekdays', () => {
    expect(validateFrequencyForm(form({ kind: 'weekdays' })).weekdays).toBeTruthy();
    expect(validateFrequencyForm(form({ kind: 'weekdays', weekdays: [1] }))).toEqual({});
  });

  it.each(['', '0', '8', '1.5', 'x'])('rejects %s as times per week', (count) => {
    expect(validateFrequencyForm(form({ kind: 'times_per_week', timesPerWeek: count })).timesPerWeek).toBeTruthy();
  });

  it.each(['1', '2', '7'])('accepts %s times per week', (count) => {
    expect(validateFrequencyForm(form({ kind: 'times_per_week', timesPerWeek: count }))).toEqual({});
  });

  it('checks times of day as HH:mm, ignoring blank rows, at most six', () => {
    expect(validateFrequencyForm(form({ kind: 'daily', times: ['08:00', '', '20:30'] }))).toEqual({});
    expect(validateFrequencyForm(form({ kind: 'daily', times: ['8am'] })).times).toBeTruthy();
    expect(validateFrequencyForm(form({ kind: 'daily', times: ['24:00'] })).times).toBeTruthy();
    expect(validateFrequencyForm(form({ kind: 'daily', times: ['01:00', '02:00', '03:00', '04:00', '05:00', '06:00', '07:00'] })).times).toBeTruthy();
  });
});

describe('frequencyFormToPayload', () => {
  it('sends the 250 mcg twice-weekly shape: weekdays Mon and Thu, sorted', () => {
    expect(frequencyFormToPayload(form({ kind: 'weekdays', weekdays: [4, 1] }))).toEqual({
      frequencyKind: 'weekdays',
      frequencyIntervalDays: null,
      frequencyWeekdays: [1, 4],
      frequencyTimesPerWeek: null,
      frequencyStartDate: null,
      frequencyTimesOfDay: null,
    });
  });

  it('keeps only the fields that belong to the chosen kind', () => {
    const everyN = frequencyFormToPayload(form({ kind: 'every_n_days', intervalDays: '3', weekdays: [1], timesPerWeek: '2' }));
    expect(everyN.frequencyIntervalDays).toBe(3);
    expect(everyN.frequencyWeekdays).toBeNull();
    expect(everyN.frequencyTimesPerWeek).toBeNull();

    const perWeek = frequencyFormToPayload(form({ kind: 'times_per_week', timesPerWeek: '2', intervalDays: '5' }));
    expect(perWeek.frequencyTimesPerWeek).toBe(2);
    expect(perWeek.frequencyIntervalDays).toBeNull();
  });

  it('sends the start date as a UTC calendar date and drops blank times', () => {
    const payload = frequencyFormToPayload(form({ kind: 'daily', startDate: '2026-10-05', times: ['08:00', ' ', '20:00'] }));
    expect(payload.frequencyStartDate).toBe('2026-10-05T00:00:00Z');
    expect(payload.frequencyTimesOfDay).toEqual(['08:00', '20:00']);
  });

  it('sends no start date or times for "as needed"', () => {
    const payload = frequencyFormToPayload(form({ kind: 'as_needed', startDate: '2026-10-05', times: ['08:00'] }));
    expect(payload.frequencyKind).toBe('as_needed');
    expect(payload.frequencyStartDate).toBeNull();
    expect(payload.frequencyTimesOfDay).toBeNull();
  });
});

describe('frequencyFormFromCompound', () => {
  it('is blank for an older record with only free text (the text is never parsed into a kind)', () => {
    expect(frequencyFormFromCompound({ doseFrequency: 'twice weekly' })).toEqual(EMPTY_FREQUENCY_FORM);
    expect(frequencyFormFromCompound({ doseFrequency: 'every Monday and Thursday' }).kind).toBe('');
  });

  it('loads a saved structured frequency', () => {
    expect(
      frequencyFormFromCompound({
        frequencyKind: 'weekdays',
        frequencyWeekdays: [1, 4],
        frequencyStartDate: '2026-10-05T00:00:00Z',
        frequencyTimesOfDay: ['08:00'],
      }),
    ).toEqual(form({ kind: 'weekdays', weekdays: [1, 4], startDate: '2026-10-05', times: ['08:00'] }));
  });
});

describe('needsStructuredFrequency', () => {
  it('is true only for free text without a structured kind', () => {
    expect(needsStructuredFrequency({ doseFrequency: 'twice weekly' })).toBe(true);
    expect(needsStructuredFrequency({ doseFrequency: 'twice weekly', frequencyKind: 'weekdays' })).toBe(false);
    expect(needsStructuredFrequency({ doseFrequency: '  ' })).toBe(false);
    expect(needsStructuredFrequency({})).toBe(false);
  });
});

describe('describeFrequency', () => {
  it('describes exactly what the user chose', () => {
    expect(describeFrequency({ frequencyKind: 'daily' })).toBe('Every day');
    expect(describeFrequency({ frequencyKind: 'every_n_days', frequencyIntervalDays: 3 })).toBe('Every 3 days');
    expect(describeFrequency({ frequencyKind: 'weekdays', frequencyWeekdays: [4, 0, 1] })).toBe('Mon, Thu, Sun');
    expect(describeFrequency({ frequencyKind: 'times_per_week', frequencyTimesPerWeek: 2 })).toBe('2 times per week (weekly target)');
    expect(describeFrequency({ frequencyKind: 'as_needed' })).toBe('As needed');
  });

  it('says nothing when no structured frequency was chosen', () => {
    expect(describeFrequency({ doseFrequency: 'twice weekly' })).toBeNull();
    expect(describeFrequency(null)).toBeNull();
    expect(describeFrequency({ frequencyKind: 'weekdays', frequencyWeekdays: [] })).toBeNull();
  });
});

describe('browserTimeZone', () => {
  it('returns the IANA zone the browser reports', () => {
    vi.spyOn(Intl, 'DateTimeFormat').mockImplementation(
      () => ({ resolvedOptions: () => ({ timeZone: 'America/New_York' }) }) as unknown as Intl.DateTimeFormat,
    );
    expect(browserTimeZone()).toBe('America/New_York');
    vi.restoreAllMocks();
  });

  it('returns undefined when the zone cannot be read', () => {
    vi.spyOn(Intl, 'DateTimeFormat').mockImplementation(() => {
      throw new Error('no Intl');
    });
    expect(browserTimeZone()).toBeUndefined();
    vi.restoreAllMocks();
  });
});
