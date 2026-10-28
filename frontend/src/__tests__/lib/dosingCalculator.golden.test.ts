import {
  calculateUnifiedDosing,
  convertMass,
  validateSyringeType,
  UNITS_PER_ML_U100,
  type MassUnit,
  type UnifiedDosingInput,
} from '@/lib/dosingCalculator';
import { describe, expect, it } from 'vitest';
// Shared with the backend: backend/tests/BioStack.Application.Tests/Services/CalculatorGoldenVectorTests.cs
import vectors from '../../../../shared/calculator/golden-vectors.json';

const REL = vectors.tolerance.relative;

function expectClose(actual: number, expected: number) {
  const bound = Math.abs(expected) * REL;
  expect(Math.abs(actual - expected)).toBeLessThanOrEqual(bound);
}

function input(overrides: Partial<UnifiedDosingInput>): UnifiedDosingInput {
  return {
    powderAmount: 1,
    powderUnit: 'mg',
    diluentVolumeMl: 1,
    concentrationSource: 'reconstitution',
    knownConcentration: Number.NaN,
    concentrationUnit: 'mcg/mL',
    desiredDose: 1,
    desiredDoseUnit: 'mcg',
    doseBasis: 'per-dose',
    splitCount: 1,
    ...overrides,
  };
}

describe('golden vectors (shared with backend)', () => {
  it('loads a non-empty vector file', () => {
    expect(vectors.version).toBe(1);
    expect(vectors.massConversion.length).toBeGreaterThan(0);
    expect(vectors.pipeline.length).toBeGreaterThan(0);
  });

  it.each(vectors.massConversion)('mass conversion: $id', (v) => {
    expectClose(convertMass(v.amount, v.from as MassUnit, v.to as MassUnit), v.expected);
  });

  it.each(vectors.reconstitution)('reconstitution: $id', (v) => {
    const r = calculateUnifiedDosing(input({ powderAmount: v.powderMg, diluentVolumeMl: v.diluentMl }));
    expectClose(r.concentrationMcgPerMl, v.expectedMcgPerMl);
  });

  it.each(vectors.volume)('volume: $id', (v) => {
    const r = calculateUnifiedDosing(
      input({ concentrationSource: 'known', knownConcentration: v.concentrationMcgPerMl, desiredDose: v.doseMcg })
    );
    expectClose(r.volumePerAdministrationMl, v.expectedMl);
  });

  it.each(vectors.u100)('U-100 units: $id', (v) => {
    // Draw volume ml for a dose chosen so that dose / concentration == ml (concentration 1000 mcg/mL).
    const r = calculateUnifiedDosing(
      input({ concentrationSource: 'known', knownConcentration: 1000, desiredDose: v.ml * 1000 })
    );
    expectClose(r.u100UnitsPerAdministration, v.expectedUnits);
    expect(UNITS_PER_ML_U100).toBe(100);
  });

  it.each(vectors.pipeline)('pipeline (powder → concentration → mL → U-100): $id', (v) => {
    const r = calculateUnifiedDosing(
      input({ powderAmount: v.powderMg, diluentVolumeMl: v.diluentMl, desiredDose: v.doseMcg })
    );
    expectClose(r.concentrationMcgPerMl, v.expectedMcgPerMl);
    expectClose(r.volumePerAdministrationMl, v.expectedMl);
    expectClose(r.u100UnitsPerAdministration, v.expectedU100Units);
  });

  it.each(vectors.rejections.filter((v) => v.kind === 'reconstitution'))('rejects: $id', (v) => {
    expect(() => calculateUnifiedDosing(input({ powderAmount: v.powderMg, diluentVolumeMl: v.diluentMl }))).toThrow();
  });

  it.each(vectors.rejections.filter((v) => v.kind === 'volume'))('rejects: $id', (v) => {
    expect(() =>
      calculateUnifiedDosing(
        input({ concentrationSource: 'known', knownConcentration: v.concentrationMcgPerMl, desiredDose: v.doseMcg })
      )
    ).toThrow();
  });

  it.each(vectors.unsupportedSyringes)('unsupported syringe %j is rejected', (syringe) => {
    expect(() => validateSyringeType(syringe)).toThrow(/Only U-100 syringes are supported/);
  });

  it.each(vectors.supportedSyringes)('supported syringe %j is accepted', (syringe) => {
    expect(() => validateSyringeType(syringe)).not.toThrow();
  });
});
