export type MassUnit = 'mcg' | 'mg' | 'g';
export type ConcentrationUnit = 'mcg/mL' | 'mg/mL';
export type DoseBasis = 'per-dose' | 'daily-total' | 'weekly-total';
export type ConcentrationSource = 'reconstitution' | 'known';

export interface UnifiedDosingInput {
  powderAmount: number;
  powderUnit: MassUnit;
  diluentVolumeMl: number;
  concentrationSource: ConcentrationSource;
  knownConcentration: number;
  concentrationUnit: ConcentrationUnit;
  desiredDose: number;
  desiredDoseUnit: MassUnit;
  doseBasis: DoseBasis;
  splitCount: number;
}

export interface UnifiedDosingResult {
  powderAmountMcg: number;
  concentrationMcgPerMl: number;
  concentrationMgPerMl: number;
  desiredDoseMcg: number;
  dosePerAdministrationMcg: number;
  dosePerAdministrationMg: number;
  volumePerAdministrationMl: number;
  u100UnitsPerAdministration: number;
  dosePerTenthMlMcg: number;
  splitCount: number;
  administrationsPerWeek: number;
  dailyTotalMcg: number;
  weeklyTotalMcg: number;
  dailyAverageMcg: number;
  formula: string;
}

const MASS_TO_MCG: Record<MassUnit, number> = {
  mcg: 1,
  mg: 1000,
  g: 1_000_000,
};

export const DEFAULT_UNIFIED_DOSING_INPUT: UnifiedDosingInput = {
  powderAmount: 5,
  powderUnit: 'mg',
  diluentVolumeMl: 2,
  concentrationSource: 'reconstitution',
  knownConcentration: 2500,
  concentrationUnit: 'mcg/mL',
  desiredDose: 250,
  desiredDoseUnit: 'mcg',
  doseBasis: 'per-dose',
  splitCount: 2,
};

export function calculateUnifiedDosing(input: UnifiedDosingInput): UnifiedDosingResult {
  const powderAmountMcg = toMcg(input.powderAmount, input.powderUnit);
  const desiredDoseMcg = toMcg(input.desiredDose, input.desiredDoseUnit);
  const concentrationMcgPerMl =
    input.concentrationSource === 'known'
      ? concentrationToMcgPerMl(input.knownConcentration, input.concentrationUnit)
      : powderAmountMcg / input.diluentVolumeMl;

  validatePositive(powderAmountMcg, 'Powder amount');
  validatePositive(input.diluentVolumeMl, 'Diluent volume');
  validatePositive(concentrationMcgPerMl, 'Concentration');
  validatePositive(desiredDoseMcg, 'Desired dose');
  validatePositive(input.splitCount, 'Split count');

  const splitCount = Math.max(1, Math.floor(input.splitCount));
  const dosePerAdministrationMcg = resolveDosePerAdministration(desiredDoseMcg, input.doseBasis, splitCount);
  const administrationsPerWeek = resolveAdministrationsPerWeek(input.doseBasis, splitCount);
  const dailyTotalMcg = resolveDailyTotal(desiredDoseMcg, input.doseBasis, dosePerAdministrationMcg, splitCount);
  const weeklyTotalMcg = resolveWeeklyTotal(desiredDoseMcg, input.doseBasis, dailyTotalMcg);
  const volumePerAdministrationMl = dosePerAdministrationMcg / concentrationMcgPerMl;

  return {
    powderAmountMcg,
    concentrationMcgPerMl,
    concentrationMgPerMl: concentrationMcgPerMl / 1000,
    desiredDoseMcg,
    dosePerAdministrationMcg,
    dosePerAdministrationMg: dosePerAdministrationMcg / 1000,
    volumePerAdministrationMl,
    u100UnitsPerAdministration: volumePerAdministrationMl * 100,
    dosePerTenthMlMcg: concentrationMcgPerMl / 10,
    splitCount,
    administrationsPerWeek,
    dailyTotalMcg,
    weeklyTotalMcg,
    dailyAverageMcg: weeklyTotalMcg / 7,
    formula: buildFormula(input),
  };
}

export function toMcg(amount: number, unit: MassUnit): number {
  return amount * MASS_TO_MCG[unit];
}

export function formatDose(mcg: number): string {
  if (mcg >= 1000) {
    return `${formatNumber(mcg / 1000)} mg`;
  }

  return `${formatNumber(mcg)} mcg`;
}

export function formatNumber(value: number, maximumFractionDigits = 3): string {
  return new Intl.NumberFormat('en-US', {
    maximumFractionDigits,
  }).format(value);
}

function concentrationToMcgPerMl(amount: number, unit: ConcentrationUnit): number {
  return unit === 'mg/mL' ? amount * 1000 : amount;
}

function resolveDosePerAdministration(doseMcg: number, basis: DoseBasis, splitCount: number): number {
  if (basis === 'per-dose') {
    return doseMcg;
  }

  return doseMcg / splitCount;
}

function resolveAdministrationsPerWeek(basis: DoseBasis, splitCount: number): number {
  if (basis === 'daily-total') {
    return splitCount * 7;
  }

  return splitCount;
}

function resolveDailyTotal(doseMcg: number, basis: DoseBasis, dosePerAdministrationMcg: number, splitCount: number): number {
  if (basis === 'daily-total') {
    return doseMcg;
  }

  if (basis === 'weekly-total') {
    return doseMcg / 7;
  }

  return (dosePerAdministrationMcg * splitCount) / 7;
}

function resolveWeeklyTotal(doseMcg: number, basis: DoseBasis, dailyTotalMcg: number): number {
  if (basis === 'weekly-total') {
    return doseMcg;
  }

  return dailyTotalMcg * 7;
}

function buildFormula(input: UnifiedDosingInput): string {
  const concentrationFormula =
    input.concentrationSource === 'known'
      ? 'Concentration = known concentration'
      : 'Concentration = powder amount / diluent volume';

  if (input.doseBasis === 'daily-total') {
    return `${concentrationFormula}; dose per administration = daily total / daily splits; volume = dose per administration / concentration`;
  }

  if (input.doseBasis === 'weekly-total') {
    return `${concentrationFormula}; dose per administration = weekly total / weekly splits; volume = dose per administration / concentration`;
  }

  return `${concentrationFormula}; volume = desired dose / concentration`;
}

function validatePositive(value: number, label: string) {
  if (!Number.isFinite(value) || value <= 0) {
    throw new Error(`${label} must be greater than 0`);
  }
}
