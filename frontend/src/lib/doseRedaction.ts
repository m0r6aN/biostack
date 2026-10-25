/**
 * 06 §A blocker: trial and reported doses render only inside the Study details fold.
 * Outside it, any "[number] [unit]" in record text is replaced with a pointer to the fold.
 * Dose reports themselves (and URLs) are left intact; the fold renders them with source and grade.
 */
export const DOSE_TOKEN = /\b\d+(?:[.,]\d+)?(?:\s?(?:[-–/]|to)\s?\d+(?:[.,]\d+)?)*\s?(?:mg|mcg|µg|ug|iu|IU|units?|ml|mL)\b/gi;
export const DOSE_REDACTION = '[dose: see Study details]';

const KEEP_KEYS = new Set(['doseReports', 'labelDosing', 'url', 'sourceUrl', 'href', 'id', 'slug']);

export function redactDoseText(text: string): string {
  return text.replace(DOSE_TOKEN, DOSE_REDACTION);
}

export function redactDoses<T>(value: T): T {
  if (typeof value === 'string') return redactDoseText(value) as unknown as T;
  if (Array.isArray(value)) return value.map(v => redactDoses(v)) as unknown as T;
  if (value && typeof value === 'object') {
    const out: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(value as Record<string, unknown>)) {
      out[k] = KEEP_KEYS.has(k) ? v : redactDoses(v);
    }
    return out as T;
  }
  return value;
}
