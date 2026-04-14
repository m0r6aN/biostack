import { calculateUnifiedDosing } from '@/lib/dosingCalculator';
import { describe, expect, it } from 'vitest';

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
