import {
  calculateUnifiedDosing,
  DEFAULT_UNIFIED_DOSING_INPUT,
  formatDose,
  formatNumber,
  isUnifiedDosingInputComplete,
  parseNumberInput,
  toMcg,
  tryCalculateUnifiedDosing,
  UNSUPPORTED_SYRINGE_MESSAGE,
} from '@/lib/dosingCalculator';
import { describe, expect, it } from 'vitest';

// ── toMcg ─────────────────────────────────────────────────────────────────────
describe('toMcg', () => {
  it('converts mcg to mcg (identity)', () => {
    expect(toMcg(500, 'mcg')).toBe(500);
  });
  it('converts mg to mcg', () => {
    expect(toMcg(5, 'mg')).toBe(5000);
  });
  it('converts g to mcg', () => {
    expect(toMcg(1, 'g')).toBe(1_000_000);
  });
});

// ── formatDose ────────────────────────────────────────────────────────────────
describe('formatDose', () => {
  it('formats values under 1000 as mcg', () => {
    expect(formatDose(250)).toBe('250 mcg');
  });
  it('formats values >= 1000 as mg', () => {
    expect(formatDose(2000)).toBe('2 mg');
  });
  it('formats fractional mg values', () => {
    expect(formatDose(1500)).toBe('1.5 mg');
  });
});

// ── formatNumber ──────────────────────────────────────────────────────────────
describe('formatNumber', () => {
  it('formats an integer without decimals', () => {
    expect(formatNumber(1000)).toBe('1,000');
  });
  it('formats a float with up to 3 decimal places by default', () => {
    expect(formatNumber(3.14159)).toBe('3.142');
  });
  it('respects custom maximumFractionDigits', () => {
    expect(formatNumber(3.14159, 1)).toBe('3.1');
  });
});

// ── validatePositive (via calculateUnifiedDosing) ─────────────────────────────
describe('calculateUnifiedDosing – validation', () => {
  it('throws when powder amount is zero', () => {
    expect(() =>
      calculateUnifiedDosing({
        powderAmount: 0,
        powderUnit: 'mg',
        diluentVolumeMl: 2,
        concentrationSource: 'reconstitution',
        knownConcentration: 0,
        concentrationUnit: 'mcg/mL',
        desiredDose: 250,
        desiredDoseUnit: 'mcg',
        doseBasis: 'per-dose',
        splitCount: 2,
      })
    ).toThrow('Powder amount must be greater than 0');
  });
});

describe('calculateUnifiedDosing', () => {
  it('calculates concentration and per-dose draw volume from powder and diluent', () => {
    const result = calculateUnifiedDosing({
      powderAmount: 5,
      powderUnit: 'mg',
      diluentVolumeMl: 2,
      concentrationSource: 'reconstitution',
      knownConcentration: 0,
      concentrationUnit: 'mcg/mL',
      desiredDose: 250,
      desiredDoseUnit: 'mcg',
      doseBasis: 'per-dose',
      splitCount: 2,
    });

    expect(result.concentrationMcgPerMl).toBe(2500);
    expect(result.volumePerAdministrationMl).toBe(0.1);
    expect(result.u100UnitsPerAdministration).toBe(10);
    expect(result.weeklyTotalMcg).toBe(500);
    expect(result.shotsPerVialExact).toBe(20);
    expect(result.shotsPerVialWhole).toBe(20);
  });

  it('calculates exact and whole shots per vial, including a non-integer case', () => {
    const result = calculateUnifiedDosing({
      powderAmount: 60,
      powderUnit: 'mg',
      diluentVolumeMl: 2,
      concentrationSource: 'reconstitution',
      knownConcentration: 0,
      concentrationUnit: 'mcg/mL',
      desiredDose: 2,
      desiredDoseUnit: 'mg',
      doseBasis: 'per-dose',
      splitCount: 1,
    });

    expect(result.concentrationMcgPerMl).toBe(30000);
    expect(result.volumePerAdministrationMl).toBeCloseTo(0.0667, 4);
    expect(result.u100UnitsPerAdministration).toBeCloseTo(6.67, 2);
    expect(result.shotsPerVialExact).toBe(30);
    expect(result.shotsPerVialWhole).toBe(30);
  });

  it('splits a weekly total into per-administration volume', () => {
    const result = calculateUnifiedDosing({
      powderAmount: 10,
      powderUnit: 'mg',
      diluentVolumeMl: 2,
      concentrationSource: 'reconstitution',
      knownConcentration: 0,
      concentrationUnit: 'mcg/mL',
      desiredDose: 1,
      desiredDoseUnit: 'mg',
      doseBasis: 'weekly-total',
      splitCount: 4,
    });

    expect(result.dosePerAdministrationMcg).toBe(250);
    expect(result.volumePerAdministrationMl).toBe(0.05);
    expect(result.dailyAverageMcg).toBeCloseTo(142.857, 3);
  });

  it('uses known concentration with mg per mL conversion', () => {
    const result = calculateUnifiedDosing({
      powderAmount: 1,
      powderUnit: 'mg',
      diluentVolumeMl: 1,
      concentrationSource: 'known',
      knownConcentration: 2.5,
      concentrationUnit: 'mg/mL',
      desiredDose: 0.25,
      desiredDoseUnit: 'mg',
      doseBasis: 'daily-total',
      splitCount: 2,
    });

    expect(result.concentrationMcgPerMl).toBe(2500);
    expect(result.dosePerAdministrationMcg).toBe(125);
    expect(result.volumePerAdministrationMl).toBe(0.05);
    expect(result.weeklyTotalMcg).toBe(1750);
  });
});

// ── empty defaults ────────────────────────────────────────────────────────────
describe('calculator defaults', () => {
  it('prefills no amount, volume, concentration, dose or schedule', () => {
    expect(Number.isNaN(DEFAULT_UNIFIED_DOSING_INPUT.powderAmount)).toBe(true);
    expect(Number.isNaN(DEFAULT_UNIFIED_DOSING_INPUT.diluentVolumeMl)).toBe(true);
    expect(Number.isNaN(DEFAULT_UNIFIED_DOSING_INPUT.knownConcentration)).toBe(true);
    expect(Number.isNaN(DEFAULT_UNIFIED_DOSING_INPUT.desiredDose)).toBe(true);
    expect(Number.isNaN(DEFAULT_UNIFIED_DOSING_INPUT.splitCount)).toBe(true);
  });

  it('returns no result and no error until required values are entered', () => {
    expect(isUnifiedDosingInputComplete(DEFAULT_UNIFIED_DOSING_INPUT)).toBe(false);
    expect(tryCalculateUnifiedDosing(DEFAULT_UNIFIED_DOSING_INPUT)).toEqual({ result: null, error: '' });
  });

  it('computes a per-dose result without a schedule, and requires one for totals', () => {
    const entered = { ...DEFAULT_UNIFIED_DOSING_INPUT, powderAmount: 5, diluentVolumeMl: 2, desiredDose: 250 };
    expect(tryCalculateUnifiedDosing(entered).result?.u100UnitsPerAdministration).toBeCloseTo(10);
    expect(tryCalculateUnifiedDosing({ ...entered, doseBasis: 'weekly-total' }).result).toBeNull();
  });

  it('still reports invalid entered values', () => {
    const entered = { ...DEFAULT_UNIFIED_DOSING_INPUT, powderAmount: 5, diluentVolumeMl: 0, desiredDose: 250 };
    expect(tryCalculateUnifiedDosing(entered).error).not.toBe('');
  });

  it('parses an empty field as not entered', () => {
    expect(Number.isNaN(parseNumberInput(''))).toBe(true);
    expect(Number.isNaN(parseNumberInput('  '))).toBe(true);
    expect(parseNumberInput('2.5')).toBe(2.5);
  });
});

// ── split count (whole number of doses) ───────────────────────────────────────
const baseInput = {
  powderAmount: 10,
  powderUnit: 'mg' as const,
  diluentVolumeMl: 2,
  concentrationSource: 'reconstitution' as const,
  knownConcentration: Number.NaN,
  concentrationUnit: 'mcg/mL' as const,
  desiredDose: 1,
  desiredDoseUnit: 'mg' as const,
  doseBasis: 'weekly-total' as const,
  splitCount: 4,
};

describe('split count handling', () => {
  it('uses a whole split count exactly as entered', () => {
    expect(calculateUnifiedDosing({ ...baseInput, splitCount: 3 }).splitCount).toBe(3);
  });

  it('rejects a fractional split count instead of silently flooring it', () => {
    expect(() => calculateUnifiedDosing({ ...baseInput, splitCount: 2.9 })).toThrow('Split count must be a whole number of doses');
    expect(() => calculateUnifiedDosing({ ...baseInput, splitCount: 0.5 })).toThrow('Split count must be a whole number of doses');
  });

  it.each([0, -1, Number.NaN, Number.POSITIVE_INFINITY])('rejects split count %s', (splitCount) => {
    expect(() => calculateUnifiedDosing({ ...baseInput, splitCount })).toThrow('Split count must be greater than 0');
  });

  it('surfaces the fractional split error through tryCalculateUnifiedDosing', () => {
    const out = tryCalculateUnifiedDosing({ ...baseInput, splitCount: 2.5 });
    expect(out.result).toBeNull();
    expect(out.error).toMatch(/whole number/);
  });
});

// ── syringe scale ─────────────────────────────────────────────────────────────
describe('syringe scale (U-100 only)', () => {
  it('computes normally when no syringe is named or U-100 is named', () => {
    expect(calculateUnifiedDosing({ ...baseInput }).u100UnitsPerAdministration).toBeCloseTo(5);
    expect(calculateUnifiedDosing({ ...baseInput, syringeType: 'U-100' }).u100UnitsPerAdministration).toBeCloseTo(5);
  });

  it.each(['U-40', 'U-50', 'u40', '50'])('rejects %s with an explicit unsupported-syringe error', (syringeType) => {
    expect(() => calculateUnifiedDosing({ ...baseInput, syringeType })).toThrow(UNSUPPORTED_SYRINGE_MESSAGE);
  });

  it('reports an unsupported syringe even before the other fields are filled in', () => {
    const out = tryCalculateUnifiedDosing({ ...DEFAULT_UNIFIED_DOSING_INPUT, syringeType: 'U-40' });
    expect(out.result).toBeNull();
    expect(out.error).toBe(UNSUPPORTED_SYRINGE_MESSAGE);
  });
});

// ── nonfinite and out-of-range values ─────────────────────────────────────────
describe('nonfinite and extreme values', () => {
  const badNumbers = [Number.NaN, Number.POSITIVE_INFINITY, Number.NEGATIVE_INFINITY, 0, -1, -1e-12];
  it.each(badNumbers)('rejects %s for powder amount, diluent volume and desired dose', (bad) => {
    expect(() => calculateUnifiedDosing({ ...baseInput, powderAmount: bad })).toThrow();
    expect(() => calculateUnifiedDosing({ ...baseInput, diluentVolumeMl: bad })).toThrow();
    expect(() => calculateUnifiedDosing({ ...baseInput, desiredDose: bad })).toThrow();
  });

  it.each(badNumbers)('rejects %s for a known concentration', (bad) => {
    expect(() => calculateUnifiedDosing({ ...baseInput, concentrationSource: 'known', knownConcentration: bad })).toThrow();
  });

  it('never returns Infinity or 0 for a dose volume at extreme magnitudes', () => {
    expect(() => calculateUnifiedDosing({ ...baseInput, powderAmount: 1e-300, desiredDose: 1e300 })).toThrow(/outside the range/);
    expect(() => calculateUnifiedDosing({ ...baseInput, diluentVolumeMl: 1e-320 })).toThrow();
    expect(() => calculateUnifiedDosing({ ...baseInput, desiredDose: 1e-320, desiredDoseUnit: 'mcg' })).toThrow();
  });

  it('handles tiny but valid values', () => {
    const r = calculateUnifiedDosing({ ...baseInput, powderAmount: 0.001, diluentVolumeMl: 1, desiredDose: 1, desiredDoseUnit: 'mcg', doseBasis: 'per-dose', splitCount: 1 });
    expect(r.concentrationMcgPerMl).toBeCloseTo(1, 12);
    expect(r.volumePerAdministrationMl).toBeCloseTo(1, 12);
    expect(r.u100UnitsPerAdministration).toBeCloseTo(100, 10);
  });

  it('handles very large but valid values', () => {
    const r = calculateUnifiedDosing({ ...baseInput, powderAmount: 1e6, powderUnit: 'g', diluentVolumeMl: 1000, desiredDose: 1e6, desiredDoseUnit: 'mcg', doseBasis: 'per-dose', splitCount: 1 });
    expect(r.powderAmountMcg).toBe(1e12);
    expect(r.concentrationMcgPerMl).toBe(1e9);
    expect(r.volumePerAdministrationMl).toBeCloseTo(1e-3, 15);
  });
});

// ── per-dose / daily / weekly accounting ──────────────────────────────────────
describe('dose basis accounting', () => {
  it('daily total: splits the daily dose and multiplies out to the week', () => {
    const r = calculateUnifiedDosing({ ...baseInput, doseBasis: 'daily-total', desiredDose: 1, splitCount: 2 });
    expect(r.dosePerAdministrationMcg).toBe(500);
    expect(r.administrationsPerWeek).toBe(14);
    expect(r.dailyTotalMcg).toBe(1000);
    expect(r.weeklyTotalMcg).toBe(7000);
  });

  it('weekly total: splits the weekly dose across the weekly doses', () => {
    const r = calculateUnifiedDosing({ ...baseInput, doseBasis: 'weekly-total', desiredDose: 2, splitCount: 4 });
    expect(r.dosePerAdministrationMcg).toBe(500);
    expect(r.administrationsPerWeek).toBe(4);
    expect(r.weeklyTotalMcg).toBe(2000);
  });
});
