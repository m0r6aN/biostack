// The frequency is the user's own choice. Nothing in this module suggests, prefills or infers a
// frequency: it lists the kinds the user can pick from, checks the combination makes sense, and
// formats what they chose. A blank form stays blank; free text is never parsed into a kind.

export const FREQUENCY_KINDS = [
  { value: 'daily', label: 'Every day' },
  { value: 'every_n_days', label: 'Every N days' },
  { value: 'weekdays', label: 'Specific weekdays' },
  { value: 'times_per_week', label: 'N times per week' },
  { value: 'as_needed', label: 'As needed' },
] as const;
export type FrequencyKind = (typeof FREQUENCY_KINDS)[number]['value'];

/** Monday first, matching how people read a week. `value` is the .NET DayOfWeek number (0 = Sunday). */
export const WEEKDAYS = [
  { value: 1, short: 'Mon', long: 'Monday' },
  { value: 2, short: 'Tue', long: 'Tuesday' },
  { value: 3, short: 'Wed', long: 'Wednesday' },
  { value: 4, short: 'Thu', long: 'Thursday' },
  { value: 5, short: 'Fri', long: 'Friday' },
  { value: 6, short: 'Sat', long: 'Saturday' },
  { value: 0, short: 'Sun', long: 'Sunday' },
] as const;

export const MIN_INTERVAL_DAYS = 2;
export const MAX_INTERVAL_DAYS = 365;
export const MAX_TIMES_OF_DAY = 6;

export interface FrequencyFormValues {
  kind: string;
  intervalDays: string;
  weekdays: number[];
  timesPerWeek: string;
  /** YYYY-MM-DD from a date input, or ''. */
  startDate: string;
  /** HH:mm values; blank entries are dropped on submit. */
  times: string[];
}

export const EMPTY_FREQUENCY_FORM: FrequencyFormValues = {
  kind: '',
  intervalDays: '',
  weekdays: [],
  timesPerWeek: '',
  startDate: '',
  times: [],
};

export interface UserFrequencyFields {
  doseFrequency?: string | null;
  frequencyKind?: string | null;
  frequencyIntervalDays?: number | null;
  frequencyWeekdays?: number[] | null;
  frequencyTimesPerWeek?: number | null;
  frequencyStartDate?: string | null;
  frequencyTimesOfDay?: string[] | null;
}

export type FrequencyFormErrors = Partial<
  Record<'kind' | 'intervalDays' | 'weekdays' | 'timesPerWeek' | 'startDate' | 'times', string>
>;

const TIME_PATTERN = /^([01]\d|2[0-3]):[0-5]\d$/;
const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

function isWholeNumber(text: string): boolean {
  return /^\d+$/.test(text.trim());
}

export function validateFrequencyForm(values: FrequencyFormValues): FrequencyFormErrors {
  const errors: FrequencyFormErrors = {};
  const kind = values.kind.trim();
  const times = values.times.map((time) => time.trim()).filter((time) => time !== '');
  const hasAnything =
    values.intervalDays.trim() !== '' ||
    values.weekdays.length > 0 ||
    values.timesPerWeek.trim() !== '' ||
    values.startDate !== '' ||
    times.length > 0;

  if (kind === '') {
    if (hasAnything) errors.kind = 'Choose how often before adding days, a start date or times.';
    return errors;
  }
  if (!FREQUENCY_KINDS.some((option) => option.value === kind)) {
    errors.kind = 'Choose how often from the list.';
    return errors;
  }

  if (kind === 'every_n_days') {
    const days = Number(values.intervalDays);
    if (!isWholeNumber(values.intervalDays) || days < MIN_INTERVAL_DAYS || days > MAX_INTERVAL_DAYS) {
      errors.intervalDays = `Enter a whole number of days from ${MIN_INTERVAL_DAYS} to ${MAX_INTERVAL_DAYS}. Use "Every day" for daily.`;
    }
  }
  if (kind === 'weekdays' && values.weekdays.length === 0) {
    errors.weekdays = 'Choose at least one weekday.';
  }
  if (kind === 'times_per_week') {
    const count = Number(values.timesPerWeek);
    if (!isWholeNumber(values.timesPerWeek) || count < 1 || count > 7) {
      errors.timesPerWeek = 'Enter a whole number of times per week from 1 to 7.';
    }
  }
  if (kind !== 'as_needed') {
    if (values.startDate !== '' && !DATE_PATTERN.test(values.startDate)) {
      errors.startDate = 'Enter the start date as a calendar date.';
    }
    if (times.length > MAX_TIMES_OF_DAY) {
      errors.times = `Enter at most ${MAX_TIMES_OF_DAY} times of day.`;
    } else if (times.some((time) => !TIME_PATTERN.test(time))) {
      errors.times = 'Enter each time of day as HH:mm.';
    }
  }
  return errors;
}

export interface FrequencyPayload {
  frequencyKind: string | null;
  frequencyIntervalDays: number | null;
  frequencyWeekdays: number[] | null;
  frequencyTimesPerWeek: number | null;
  frequencyStartDate: string | null;
  frequencyTimesOfDay: string[] | null;
}

/** Wire fields for a validated form. A blank kind sends nulls so the API stores nothing. */
export function frequencyFormToPayload(values: FrequencyFormValues): FrequencyPayload {
  const kind = values.kind.trim();
  if (kind === '') {
    return {
      frequencyKind: null,
      frequencyIntervalDays: null,
      frequencyWeekdays: null,
      frequencyTimesPerWeek: null,
      frequencyStartDate: null,
      frequencyTimesOfDay: null,
    };
  }
  const times = values.times.map((time) => time.trim()).filter((time) => time !== '');
  return {
    frequencyKind: kind,
    frequencyIntervalDays: kind === 'every_n_days' ? Number(values.intervalDays) : null,
    frequencyWeekdays: kind === 'weekdays' ? [...values.weekdays].sort((a, b) => a - b) : null,
    frequencyTimesPerWeek: kind === 'times_per_week' ? Number(values.timesPerWeek) : null,
    frequencyStartDate: kind !== 'as_needed' && values.startDate !== '' ? `${values.startDate}T00:00:00Z` : null,
    frequencyTimesOfDay: kind !== 'as_needed' && times.length > 0 ? times : null,
  };
}

/** Form values for editing an existing record; blank when the user never chose a structured frequency. */
export function frequencyFormFromCompound(compound: UserFrequencyFields): FrequencyFormValues {
  return {
    kind: compound.frequencyKind ?? '',
    intervalDays: compound.frequencyIntervalDays == null ? '' : String(compound.frequencyIntervalDays),
    weekdays: compound.frequencyWeekdays ? [...compound.frequencyWeekdays] : [],
    timesPerWeek: compound.frequencyTimesPerWeek == null ? '' : String(compound.frequencyTimesPerWeek),
    startDate: compound.frequencyStartDate ? compound.frequencyStartDate.slice(0, 10) : '',
    times: compound.frequencyTimesOfDay ? [...compound.frequencyTimesOfDay] : [],
  };
}

/** True for an older entry that only has free text: the form should prompt the user to pick a frequency. */
export function needsStructuredFrequency(compound: UserFrequencyFields): boolean {
  return !compound.frequencyKind && Boolean(compound.doseFrequency?.trim());
}

/** "Mon, Thu", "Every 3 days", ... exactly the user's choice; null when none was chosen. */
export function describeFrequency(compound: UserFrequencyFields | null | undefined): string | null {
  if (!compound?.frequencyKind) return null;
  switch (compound.frequencyKind) {
    case 'daily':
      return 'Every day';
    case 'every_n_days':
      return compound.frequencyIntervalDays ? `Every ${compound.frequencyIntervalDays} days` : null;
    case 'weekdays': {
      const days = compound.frequencyWeekdays ?? [];
      if (days.length === 0) return null;
      return WEEKDAYS.filter((day) => days.includes(day.value))
        .map((day) => day.short)
        .join(', ');
    }
    case 'times_per_week': {
      const count = compound.frequencyTimesPerWeek;
      if (!count) return null;
      return count === 1 ? '1 time per week (weekly target)' : `${count} times per week (weekly target)`;
    }
    case 'as_needed':
      return 'As needed';
    default:
      return null;
  }
}

/** The browser's IANA time zone (the user's own), or undefined when it cannot be read. */
export function browserTimeZone(): string | undefined {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || undefined;
  } catch {
    return undefined;
  }
}
