'use client';

import { useMemo, useState } from 'react';
import { SafetyDisclaimer } from '@/components/SafetyDisclaimer';
import {
  calculateUnifiedDosing,
  DEFAULT_UNIFIED_DOSING_INPUT,
  formatDose,
  formatNumber,
  type ConcentrationUnit,
  type DoseBasis,
  type MassUnit,
  type UnifiedDosingInput,
  type UnifiedDosingResult,
} from '@/lib/dosingCalculator';

interface UnifiedDosingCalculatorProps {
  title?: string;
  description?: string;
  attachedMessage?: string | null;
  isRecording?: boolean;
  onRecord?: (input: UnifiedDosingInput, result: UnifiedDosingResult) => Promise<void>;
}

const massUnits: MassUnit[] = ['mcg', 'mg', 'g'];
const concentrationUnits: ConcentrationUnit[] = ['mcg/mL', 'mg/mL'];

export function UnifiedDosingCalculator({
  title = 'Reconstitution & Dosing Calculator',
  description = 'Calculate concentration, dose volume, unit conversions, and daily or weekly split math in one place.',
  attachedMessage,
  isRecording = false,
  onRecord,
}: UnifiedDosingCalculatorProps) {
  const [input, setInput] = useState<UnifiedDosingInput>(DEFAULT_UNIFIED_DOSING_INPUT);
  const [recordError, setRecordError] = useState<string | null>(null);

  const calculation = useMemo(() => {
    try {
      return { result: calculateUnifiedDosing(input), error: null };
    } catch (error) {
      return {
        result: null,
        error: error instanceof Error ? error.message : 'Calculation failed',
      };
    }
  }, [input]);

  async function handleRecord() {
    if (!onRecord || !calculation.result) {
      return;
    }

    try {
      setRecordError(null);
      await onRecord(input, calculation.result);
    } catch (error) {
      setRecordError(error instanceof Error ? error.message : 'Could not attach calculation');
    }
  }

  const result = calculation.result;

  return (
    <section className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-5 shadow-[0_16px_48px_rgba(0,0,0,0.24)]">
      <div className="flex flex-col gap-4 border-b border-white/[0.06] pb-5 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-300/70">Calculator</p>
          <h2 className="mt-2 text-2xl font-semibold tracking-tight text-white">{title}</h2>
          <p className="mt-2 max-w-3xl text-sm leading-6 text-white/55">{description}</p>
        </div>

        {onRecord && (
          <button
            type="button"
            onClick={handleRecord}
            disabled={!result || isRecording}
            className="rounded-lg bg-emerald-400 px-4 py-2.5 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-300 disabled:cursor-not-allowed disabled:bg-white/10 disabled:text-white/35"
          >
            {isRecording ? 'Attaching...' : 'Attach to Protocol'}
          </button>
        )}
      </div>

      <div className="mt-5 grid gap-5 xl:grid-cols-[1.15fr_0.85fr]">
        <div className="space-y-5">
          <fieldset className="space-y-4">
            <legend className="text-sm font-semibold text-white">Solution</legend>
            <div className="grid gap-4 md:grid-cols-2">
              <NumberWithUnitField
                label="Powder amount"
                value={input.powderAmount}
                unit={input.powderUnit}
                units={massUnits}
                onValueChange={(value) => setInput((current) => ({ ...current, powderAmount: value }))}
                onUnitChange={(unit) => setInput((current) => ({ ...current, powderUnit: unit }))}
              />
              <NumberField
                label="Diluent volume"
                suffix="mL"
                value={input.diluentVolumeMl}
                onChange={(value) => setInput((current) => ({ ...current, diluentVolumeMl: value }))}
              />
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <ToggleButton
                active={input.concentrationSource === 'reconstitution'}
                label="Calculate concentration"
                note="Use powder plus diluent"
                onClick={() => setInput((current) => ({ ...current, concentrationSource: 'reconstitution' }))}
              />
              <ToggleButton
                active={input.concentrationSource === 'known'}
                label="Use known concentration"
                note="Enter label concentration"
                onClick={() => setInput((current) => ({ ...current, concentrationSource: 'known' }))}
              />
            </div>

            {input.concentrationSource === 'known' && (
              <NumberWithUnitField
                label="Concentration"
                value={input.knownConcentration}
                unit={input.concentrationUnit}
                units={concentrationUnits}
                onValueChange={(value) => setInput((current) => ({ ...current, knownConcentration: value }))}
                onUnitChange={(unit) => setInput((current) => ({ ...current, concentrationUnit: unit }))}
              />
            )}
          </fieldset>

          <fieldset className="space-y-4">
            <legend className="text-sm font-semibold text-white">Dose Plan</legend>
            <NumberWithUnitField
              label="Desired dose"
              value={input.desiredDose}
              unit={input.desiredDoseUnit}
              units={massUnits}
              onValueChange={(value) => setInput((current) => ({ ...current, desiredDose: value }))}
              onUnitChange={(unit) => setInput((current) => ({ ...current, desiredDoseUnit: unit }))}
            />

            <div className="grid gap-3 lg:grid-cols-3">
              <DoseBasisButton
                basis="per-dose"
                current={input.doseBasis}
                label="Per dose"
                note="Dose amount is one administration"
                onClick={(basis) => setInput((current) => ({ ...current, doseBasis: basis }))}
              />
              <DoseBasisButton
                basis="daily-total"
                current={input.doseBasis}
                label="Daily total"
                note="Split across doses per day"
                onClick={(basis) => setInput((current) => ({ ...current, doseBasis: basis }))}
              />
              <DoseBasisButton
                basis="weekly-total"
                current={input.doseBasis}
                label="Weekly total"
                note="Split across doses per week"
                onClick={(basis) => setInput((current) => ({ ...current, doseBasis: basis }))}
              />
            </div>

            <NumberField
              label={splitLabel(input.doseBasis)}
              suffix={splitSuffix(input.doseBasis)}
              step="1"
              value={input.splitCount}
              onChange={(value) => setInput((current) => ({ ...current, splitCount: value }))}
            />
          </fieldset>
        </div>

        <div className="space-y-4">
          {calculation.error && (
            <div className="rounded-lg border border-red-400/25 bg-red-500/10 px-4 py-3 text-sm text-red-200">
              {calculation.error}
            </div>
          )}

          {result && (
            <>
              <div className="rounded-lg border border-emerald-400/20 bg-emerald-500/10 p-5">
                <p className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-200/70">
                  Draw volume
                </p>
                <p className="mt-2 text-4xl font-semibold tracking-tight text-white">
                  {formatNumber(result.volumePerAdministrationMl, 4)} mL
                </p>
                <p className="mt-2 text-sm text-emerald-100/75">
                  {formatNumber(result.u100UnitsPerAdministration, 1)} units on a U-100 syringe
                </p>
              </div>

              <div className="grid gap-3 sm:grid-cols-2">
                <Metric label="Concentration" value={`${formatNumber(result.concentrationMcgPerMl)} mcg/mL`} detail={`${formatNumber(result.concentrationMgPerMl, 4)} mg/mL`} />
                <Metric label="Per dose" value={formatDose(result.dosePerAdministrationMcg)} detail={`${formatNumber(result.dosePerTenthMlMcg)} mcg per 0.1 mL`} />
                <Metric label="Daily total" value={formatDose(result.dailyTotalMcg)} detail={`Average: ${formatDose(result.dailyAverageMcg)} / day`} />
                <Metric label="Weekly total" value={formatDose(result.weeklyTotalMcg)} detail={`${result.administrationsPerWeek} administrations / week`} />
              </div>

              <div className="rounded-lg border border-white/[0.08] bg-black/20 p-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">Formula</p>
                <p className="mt-2 text-xs leading-5 text-white/65">{result.formula}</p>
              </div>

              <SafetyDisclaimer type="calculation" />
            </>
          )}

          {attachedMessage && (
            <p className="rounded-lg border border-emerald-400/20 bg-emerald-500/10 px-3 py-2 text-sm font-semibold text-emerald-100">
              {attachedMessage}
            </p>
          )}

          {recordError && (
            <p className="rounded-lg border border-amber-400/20 bg-amber-500/10 px-3 py-2 text-sm text-amber-100">
              {recordError}
            </p>
          )}
        </div>
      </div>
    </section>
  );
}

function NumberField({
  label,
  value,
  onChange,
  suffix,
  step = '0.1',
}: {
  label: string;
  value: number;
  onChange: (value: number) => void;
  suffix?: string;
  step?: string;
}) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm text-white/62">{label}</span>
      <div className="flex overflow-hidden rounded-lg border border-white/10 bg-[#0F141B] focus-within:border-emerald-400/45">
        <input
          type="number"
          min="0"
          step={step}
          value={Number.isNaN(value) ? '' : value}
          onChange={(event) => onChange(Number(event.target.value))}
          className="min-w-0 flex-1 bg-transparent px-4 py-3 text-white outline-none placeholder:text-white/30"
        />
        {suffix && <span className="border-l border-white/10 px-3 py-3 text-sm text-white/50">{suffix}</span>}
      </div>
    </label>
  );
}

function NumberWithUnitField<TUnit extends string>({
  label,
  value,
  unit,
  units,
  onValueChange,
  onUnitChange,
}: {
  label: string;
  value: number;
  unit: TUnit;
  units: TUnit[];
  onValueChange: (value: number) => void;
  onUnitChange: (unit: TUnit) => void;
}) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm text-white/62">{label}</span>
      <div className="flex overflow-hidden rounded-lg border border-white/10 bg-[#0F141B] focus-within:border-emerald-400/45">
        <input
          type="number"
          min="0"
          step="0.1"
          value={Number.isNaN(value) ? '' : value}
          onChange={(event) => onValueChange(Number(event.target.value))}
          className="min-w-0 flex-1 bg-transparent px-4 py-3 text-white outline-none placeholder:text-white/30"
        />
        <select
          value={unit}
          onChange={(event) => onUnitChange(event.target.value as TUnit)}
          className="border-l border-white/10 bg-[#111821] px-3 py-3 text-sm text-white outline-none"
        >
          {units.map((item) => (
            <option key={item} value={item}>
              {item}
            </option>
          ))}
        </select>
      </div>
    </label>
  );
}

function ToggleButton({
  active,
  label,
  note,
  onClick,
}: {
  active: boolean;
  label: string;
  note: string;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      aria-pressed={active}
      onClick={onClick}
      className={`rounded-lg border p-4 text-left transition-colors ${
        active
          ? 'border-emerald-400/35 bg-emerald-500/10 text-emerald-100'
          : 'border-white/[0.08] bg-white/[0.02] text-white/70 hover:border-white/16'
      }`}
    >
      <span className="block text-sm font-semibold">{label}</span>
      <span className="mt-1 block text-xs opacity-70">{note}</span>
    </button>
  );
}

function DoseBasisButton({
  basis,
  current,
  label,
  note,
  onClick,
}: {
  basis: DoseBasis;
  current: DoseBasis;
  label: string;
  note: string;
  onClick: (basis: DoseBasis) => void;
}) {
  return <ToggleButton active={basis === current} label={label} note={note} onClick={() => onClick(basis)} />;
}

function Metric({ label, value, detail }: { label: string; value: string; detail: string }) {
  return (
    <div className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      <p className="text-xs uppercase tracking-[0.16em] text-white/35">{label}</p>
      <p className="mt-2 text-lg font-semibold text-white">{value}</p>
      <p className="mt-1 text-xs text-white/45">{detail}</p>
    </div>
  );
}

function splitLabel(basis: DoseBasis): string {
  if (basis === 'daily-total') return 'Doses per day';
  if (basis === 'weekly-total') return 'Doses per week';
  return 'Doses per week';
}

function splitSuffix(basis: DoseBasis): string {
  if (basis === 'daily-total') return '/ day';
  return '/ week';
}
