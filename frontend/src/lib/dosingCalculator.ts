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
  shotsPerVialExact: number;
  shotsPerVialWhole: number;
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

/**
 * Calculators open empty: no amount, volume, concentration, target or schedule is
 * prefilled. Numeric fields use NaN to mean "not entered yet"; only units and the
 * dose basis carry a starting selection.
 */
export const DEFAULT_UNIFIED_DOSING_INPUT: UnifiedDosingInput = {
  powderAmount: Number.NaN,
  powderUnit: 'mg',
  diluentVolumeMl: Number.NaN,
  concentrationSource: 'reconstitution',
  knownConcentration: Number.NaN,
  concentrationUnit: 'mcg/mL',
  desiredDose: Number.NaN,
  desiredDoseUnit: 'mcg',
  doseBasis: 'per-dose',
  splitCount: Number.NaN,
};

/** Neutral placeholder shown in empty numeric calculator fields. */
export const EMPTY_NUMBER_PLACEHOLDER = 'Enter value';

/** Maps a raw number-input string to a number, treating an empty field as "not entered" (NaN) rather than 0. */
export function parseNumberInput(raw: string): number {
  return raw.trim() === '' ? Number.NaN : Number(raw);
}

/** True when every value the calculation needs has been entered (validity is still checked by calculateUnifiedDosing). */
export function isUnifiedDosingInputComplete(input: UnifiedDosingInput): boolean {
  const required = [input.powderAmount, input.diluentVolumeMl, input.desiredDose];
  if (input.concentrationSource === 'known') required.push(input.knownConcentration);
  // A per-dose amount is one administration, so the per-administration math does not need a schedule.
  if (input.doseBasis !== 'per-dose') required.push(input.splitCount);
  return required.every((value) => Number.isFinite(value));
}

/** Runs the calculation only once the user has entered every required value; empty inputs yield no result and no error. */
export function tryCalculateUnifiedDosing(input: UnifiedDosingInput): { result: UnifiedDosingResult | null; error: string } {
  if (!isUnifiedDosingInputComplete(input)) return { result: null, error: '' };
  // An unentered schedule on a per-dose basis does not change the per-administration result; 1 is only used
  // internally so validation passes, and callers should not present schedule totals unless splitCount was entered.
  const calcInput = Number.isFinite(input.splitCount) ? input : { ...input, splitCount: 1 };
  try {
    return { result: calculateUnifiedDosing(calcInput), error: '' };
  } catch (error) {
    return { result: null, error: error instanceof Error ? error.message : 'Calculation failed' };
  }
}

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
  const shotsPerVialExact = input.diluentVolumeMl / volumePerAdministrationMl;

  return {
    powderAmountMcg,
    concentrationMcgPerMl,
    concentrationMgPerMl: concentrationMcgPerMl / 1000,
    desiredDoseMcg,
    dosePerAdministrationMcg,
    dosePerAdministrationMg: dosePerAdministrationMcg / 1000,
    volumePerAdministrationMl,
    u100UnitsPerAdministration: volumePerAdministrationMl * 100,
    shotsPerVialExact,
    shotsPerVialWhole: Math.floor(shotsPerVialExact),
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
