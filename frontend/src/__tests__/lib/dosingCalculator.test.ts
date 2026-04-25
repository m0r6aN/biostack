import { calculateUnifiedDosing, formatDose, formatNumber, toMcg } from '@/lib/dosingCalculator';
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
