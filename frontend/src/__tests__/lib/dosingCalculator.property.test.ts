import { calculateUnifiedDosing, convertMass, type MassUnit, type UnifiedDosingInput } from '@/lib/dosingCalculator';
import { describe, expect, it } from 'vitest';

// Deterministic PRNG (mulberry32) so a failure is reproducible.
function rng(seed: number) {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/** Log-uniform positive number between 1e-4 and 1e6, so every order of magnitude is exercised. */
function logUniform(r: () => number): number {
  return 10 ** (r() * 10 - 4);
}

const units: MassUnit[] = ['mcg', 'mg', 'g'];
const N = 500;

function base(overrides: Partial<UnifiedDosingInput>): UnifiedDosingInput {
  return {
    powderAmount: 5,
    powderUnit: 'mg',
    diluentVolumeMl: 2,
    concentrationSource: 'reconstitution',
    knownConcentration: Number.NaN,
    concentrationUnit: 'mcg/mL',
    desiredDose: 250,
    desiredDoseUnit: 'mcg',
    doseBasis: 'per-dose',
    splitCount: 1,
    ...overrides,
  };
}

const close = (a: number, b: number, rel = 1e-9) => Math.abs(a - b) <= Math.abs(b) * rel;

describe('calculator properties (independent oracles, seeded random inputs)', () => {
  it('mass conversion round-trips for every unit pair', () => {
    const r = rng(1);
    for (let i = 0; i < N; i++) {
      const x = logUniform(r);
      const from = units[Math.floor(r() * 3)];
      const to = units[Math.floor(r() * 3)];
      expect(close(convertMass(convertMass(x, from, to), to, from), x)).toBe(true);
    }
  });

  it('mass conversion agrees with the definition 1 g = 1e3 mg = 1e6 mcg', () => {
    const perGram: Record<MassUnit, number> = { g: 1, mg: 1e3, mcg: 1e6 };
    const r = rng(2);
    for (let i = 0; i < N; i++) {
      const x = logUniform(r);
      const from = units[Math.floor(r() * 3)];
      const to = units[Math.floor(r() * 3)];
      const expected = (x / perGram[from]) * perGram[to];
      expect(close(convertMass(x, from, to), expected)).toBe(true);
    }
  });

  it('concentration * volume gives back the requested dose', () => {
    const r = rng(3);
    for (let i = 0; i < N; i++) {
      const res = calculateUnifiedDosing(
        base({ powderAmount: logUniform(r), diluentVolumeMl: logUniform(r), desiredDose: logUniform(r) })
      );
      expect(close(res.concentrationMcgPerMl * res.volumePerAdministrationMl, res.dosePerAdministrationMcg)).toBe(true);
    }
  });

  it('U-100 units are exactly 100 x the mL volume', () => {
    const r = rng(4);
    for (let i = 0; i < N; i++) {
      const res = calculateUnifiedDosing(base({ powderAmount: logUniform(r), diluentVolumeMl: logUniform(r), desiredDose: logUniform(r) }));
      expect(close(res.u100UnitsPerAdministration, res.volumePerAdministrationMl * 100)).toBe(true);
    }
  });

  it('volume doubles when the dose doubles and halves when the diluent doubles', () => {
    const r = rng(5);
    for (let i = 0; i < N; i++) {
      const powderAmount = logUniform(r);
      const diluentVolumeMl = logUniform(r);
      const desiredDose = logUniform(r);
      const v = calculateUnifiedDosing(base({ powderAmount, diluentVolumeMl, desiredDose })).volumePerAdministrationMl;
      const vDoubleDose = calculateUnifiedDosing(base({ powderAmount, diluentVolumeMl, desiredDose: desiredDose * 2 })).volumePerAdministrationMl;
      const vDoubleDiluent = calculateUnifiedDosing(base({ powderAmount, diluentVolumeMl: diluentVolumeMl * 2, desiredDose })).volumePerAdministrationMl;
      expect(close(vDoubleDose, v * 2)).toBe(true);
      expect(close(vDoubleDiluent, v * 2)).toBe(true); // more liquid = weaker solution = bigger draw
    }
  });

  it('the same mass entered in mcg, mg or g gives the same result', () => {
    const r = rng(6);
    for (let i = 0; i < N; i++) {
      const mg = logUniform(r);
      const a = calculateUnifiedDosing(base({ powderAmount: mg, powderUnit: 'mg' }));
      const b = calculateUnifiedDosing(base({ powderAmount: mg * 1000, powderUnit: 'mcg' }));
      const c = calculateUnifiedDosing(base({ powderAmount: mg / 1000, powderUnit: 'g' }));
      expect(close(b.concentrationMcgPerMl, a.concentrationMcgPerMl)).toBe(true);
      expect(close(c.concentrationMcgPerMl, a.concentrationMcgPerMl)).toBe(true);
    }
  });

  it('daily and weekly totals are consistent for any whole split count', () => {
    const r = rng(7);
    for (let i = 0; i < N; i++) {
      const splitCount = 1 + Math.floor(r() * 14);
      const desiredDose = logUniform(r);
      const daily = calculateUnifiedDosing(base({ doseBasis: 'daily-total', desiredDose, desiredDoseUnit: 'mcg', splitCount }));
      expect(close(daily.dosePerAdministrationMcg * splitCount, desiredDose)).toBe(true);
      expect(close(daily.weeklyTotalMcg, desiredDose * 7)).toBe(true);
      const weekly = calculateUnifiedDosing(base({ doseBasis: 'weekly-total', desiredDose, desiredDoseUnit: 'mcg', splitCount }));
      expect(close(weekly.dosePerAdministrationMcg * splitCount, desiredDose)).toBe(true);
      expect(close(weekly.dailyAverageMcg, desiredDose / 7)).toBe(true);
    }
  });

  it('any non-integer split count is rejected, any positive integer accepted', () => {
    const r = rng(8);
    for (let i = 0; i < 200; i++) {
      const whole = 1 + Math.floor(r() * 50);
      expect(() => calculateUnifiedDosing(base({ doseBasis: 'daily-total', splitCount: whole }))).not.toThrow();
      expect(() => calculateUnifiedDosing(base({ doseBasis: 'daily-total', splitCount: whole + 0.01 + r() * 0.98 }))).toThrow(/whole number/);
    }
  });

  it('never returns a non-finite or non-positive volume: it either computes or throws', () => {
    const r = rng(9);
    for (let i = 0; i < 2000; i++) {
      const wild = () => 10 ** (r() * 700 - 350); // 1e-350 .. 1e350 (includes under/overflow)
      try {
        const res = calculateUnifiedDosing(base({ powderAmount: wild(), diluentVolumeMl: wild(), desiredDose: wild() }));
        expect(Number.isFinite(res.volumePerAdministrationMl) && res.volumePerAdministrationMl > 0).toBe(true);
        expect(Number.isFinite(res.u100UnitsPerAdministration)).toBe(true);
        expect(Number.isFinite(res.concentrationMcgPerMl) && res.concentrationMcgPerMl > 0).toBe(true);
      } catch (e) {
        expect(e).toBeInstanceOf(Error);
      }
    }
  });
});
