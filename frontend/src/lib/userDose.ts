// The dose is the user's own entry. Nothing in this module suggests, prefills, computes or ranges a
// dose: it only (a) lists the allowed units, (b) checks the number is positive and finite, and
// (c) formats what the user typed. Keep it that way: no recommended ranges, no "typical" values.

export const DOSE_UNITS = ['mcg', 'mg', 'g', 'IU', 'mL', 'units', 'capsule', 'tablet', 'softgel', 'scoop', 'drop', 'serving', 'gummy'] as const;
export type DoseUnit = (typeof DOSE_UNITS)[number];

export const DOSE_FREQUENCY_MAX_LENGTH = 100;
export const DOSE_TIME_MAX_LENGTH = 100;

export interface DoseFormValues {
  amount: string;
  unit: string;
  frequency: string;
  time: string;
}

export const EMPTY_DOSE_FORM: DoseFormValues = { amount: '', unit: '', frequency: '', time: '' };

export interface UserDoseFields {
  doseAmount?: number | null;
  doseUnit?: string | null;
  doseFrequency?: string | null;
  doseTime?: string | null;
}

export type DoseFormErrors = Partial<Record<'amount' | 'unit' | 'frequency' | 'time', string>>;

/** Positive-and-finite checks only. There is deliberately no minimum or maximum "reasonable" dose. */
export function validateDoseForm(values: DoseFormValues): DoseFormErrors {
  const errors: DoseFormErrors = {};
  const amountText = values.amount.trim();
  const hasUnit = values.unit.trim() !== '';

  if (amountText === '') {
    if (hasUnit) errors.amount = 'Enter an amount, or clear the unit.';
  } else {
    const amount = Number(amountText);
    if (!Number.isFinite(amount) || amount <= 0) {
      errors.amount = 'Amount must be a number greater than zero.';
    } else if (!hasUnit) {
      errors.unit = 'Choose a unit for the amount.';
    }
  }

  if (hasUnit && !(DOSE_UNITS as readonly string[]).includes(values.unit)) {
    errors.unit = `Unit must be one of: ${DOSE_UNITS.join(', ')}.`;
  }
  if (values.frequency.trim().length > DOSE_FREQUENCY_MAX_LENGTH) {
    errors.frequency = `Frequency must be ${DOSE_FREQUENCY_MAX_LENGTH} characters or fewer.`;
  }
  if (values.time.trim().length > DOSE_TIME_MAX_LENGTH) {
    errors.time = `Time or schedule must be ${DOSE_TIME_MAX_LENGTH} characters or fewer.`;
  }
  return errors;
}

/** Wire fields for a validated form. Blank inputs become null so the API stores nothing. */
export function doseFormToPayload(values: DoseFormValues): Required<UserDoseFields> {
  const amountText = values.amount.trim();
  return {
    doseAmount: amountText === '' ? null : Number(amountText),
    doseUnit: amountText === '' ? null : values.unit.trim() || null,
    doseFrequency: values.frequency.trim() || null,
    doseTime: values.time.trim() || null,
  };
}

/** Form values for editing an existing record; blank when the user never entered a dose. */
export function doseFormFromCompound(compound: UserDoseFields): DoseFormValues {
  return {
    amount: compound.doseAmount == null ? '' : String(compound.doseAmount),
    unit: compound.doseAmount == null ? '' : compound.doseUnit ?? '',
    frequency: compound.doseFrequency ?? '',
    time: compound.doseTime ?? '',
  };
}

/** "250 mcg" (amount and unit exactly as entered), or null when the user entered none. */
export function formatDoseAmount(compound: UserDoseFields): string | null {
  if (compound.doseAmount == null || !compound.doseUnit) return null;
  return `${compound.doseAmount} ${compound.doseUnit}`;
}

/** "250 mcg · twice weekly" (the user's own words), or null when nothing was entered. */
export function formatUserDose(compound: UserDoseFields | null | undefined): string | null {
  if (!compound) return null;
  const parts = [formatDoseAmount(compound), compound.doseFrequency?.trim() || null].filter(
    (part): part is string => Boolean(part),
  );
  return parts.length > 0 ? parts.join(' · ') : null;
}

/** Full user-entered dose including the time or schedule, for detail views. */
export function formatUserDoseWithTime(compound: UserDoseFields | null | undefined): string | null {
  const dose = formatUserDose(compound);
  const time = compound?.doseTime?.trim() || null;
  if (dose && time) return `${dose} · ${time}`;
  return dose ?? time;
}
