'use client';

import Link from 'next/link';
import { type FormEvent, useMemo, useState } from 'react';
import { apiClient } from '@/lib/api';
import { CalculatorResult, ConversionRequest, ReconstitutionRequest, VolumeRequest } from '@/lib/types';

type CalculatorKind = 'reconstitution' | 'volume' | 'conversion';

interface PublicCalculatorExperienceProps {
  kind: CalculatorKind;
}

export function PublicCalculatorExperience({ kind }: PublicCalculatorExperienceProps) {
  const [reconstitution, setReconstitution] = useState<ReconstitutionRequest>({
    peptideAmountMg: 5,
    diluentVolumeMl: 2,
  });
  const [volume, setVolume] = useState<VolumeRequest>({
    desiredDoseMcg: 250,
    concentrationMcgPerMl: 2500,
  });
  const [conversion, setConversion] = useState<ConversionRequest>({
    amount: 1,
    fromUnit: 'mg',
    toUnit: 'mcg',
    conversionFactor: 0,
  });
  const [result, setResult] = useState<CalculatorResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isCalculating, setIsCalculating] = useState(false);
  const [email, setEmail] = useState('');
  const [leadState, setLeadState] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');

  const copy = useMemo(() => {
    if (kind === 'reconstitution') {
      return {
        title: 'Reconstitution Calculator',
        description:
          'Resolve dilution math instantly. Enter the powder amount and diluent volume and BioStack returns concentration with transparent math.',
        source: 'public-reconstitution-calculator',
      };
    }

    if (kind === 'volume') {
      return {
        title: 'Volume Calculator',
        description:
          'Translate target dose into exact draw volume. Use the concentration you already know and remove the mental arithmetic.',
        source: 'public-volume-calculator',
      };
    }

    return {
      title: 'Unit Converter',
      description:
        'Convert protocol units without context switching. Built for the mg, mcg, and gram conversions that keep showing up in stack management.',
      source: 'public-unit-converter',
    };
  }, [kind]);

  async function handleCalculate() {
    try {
      setIsCalculating(true);
      setError(null);

      let nextResult: CalculatorResult;
      if (kind === 'reconstitution') {
        nextResult = await apiClient.calculateReconstitution(reconstitution);
      } else if (kind === 'volume') {
        nextResult = await apiClient.calculateVolume(volume);
      } else {
        nextResult = await apiClient.calculateConversion(conversion);
      }

      setResult(nextResult);
      setLeadState('idle');
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Calculation failed';
      setError(message);
      setResult(null);
    } finally {
      setIsCalculating(false);
    }
  }

  async function handleLeadCapture(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!email.trim()) {
      return;
    }

    try {
      setLeadState('saving');
      await apiClient.captureLead(email.trim(), copy.source);
      setLeadState('saved');
    } catch {
      setLeadState('error');
    }
  }

  const derivedNote =
    kind === 'reconstitution' && result
      ? `Each 0.1mL withdrawal = ${(result.output / 10).toFixed(0)}mcg`
      : kind === 'volume' && result
        ? `At ${volume.concentrationMcgPerMl}mcg/mL, your target dose requires ${result.output.toFixed(3)}mL`
        : kind === 'conversion' && result
          ? `${conversion.amount}${conversion.fromUnit} converts directly to ${result.output}${result.unit}`
          : null;

  return (
    <section className="mx-auto grid max-w-7xl gap-10 px-5 py-14 sm:px-8 lg:grid-cols-[1.1fr_0.9fr] lg:py-20">
      <div className="space-y-8">
        <div className="space-y-4">
          <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
            Precision Tools
          </p>
          <h1 className="max-w-3xl text-4xl font-semibold tracking-tight text-white sm:text-5xl">
            {copy.title}
          </h1>
          <p className="max-w-2xl text-lg leading-8 text-white/62">{copy.description}</p>
        </div>

        <div className="grid gap-5 border-y border-white/8 py-6 text-sm text-white/55 sm:grid-cols-3">
          <div>
            <p className="mb-1 text-white/90">Transparent math</p>
            <p>Formula output stays visible after every calculation.</p>
          </div>
          <div>
            <p className="mb-1 text-white/90">No account required</p>
            <p>Use the tool first. Save or deepen later if it earns the right.</p>
          </div>
          <div>
            <p className="mb-1 text-white/90">Research boundary</p>
            <p>Calculated result only. Verify against your source material before use.</p>
          </div>
        </div>
      </div>

      <div className="rounded-[2rem] border border-white/10 bg-white/[0.03] p-6 shadow-[0_20px_80px_rgba(0,0,0,0.35)] backdrop-blur-xl sm:p-8">
        <div className="space-y-5">
          {kind === 'reconstitution' && (
            <>
              <Field
                label="Powder amount (mg)"
                value={reconstitution.peptideAmountMg}
                onChange={(value) => setReconstitution((current) => ({ ...current, peptideAmountMg: value }))}
              />
              <Field
                label="Diluent volume (mL)"
                value={reconstitution.diluentVolumeMl}
                onChange={(value) => setReconstitution((current) => ({ ...current, diluentVolumeMl: value }))}
              />
            </>
          )}

          {kind === 'volume' && (
            <>
              <Field
                label="Desired dose (mcg)"
                value={volume.desiredDoseMcg}
                onChange={(value) => setVolume((current) => ({ ...current, desiredDoseMcg: value }))}
              />
              <Field
                label="Concentration (mcg/mL)"
                value={volume.concentrationMcgPerMl}
                onChange={(value) => setVolume((current) => ({ ...current, concentrationMcgPerMl: value }))}
              />
            </>
          )}

          {kind === 'conversion' && (
            <>
              <Field
                label="Amount"
                value={conversion.amount}
                onChange={(value) => setConversion((current) => ({ ...current, amount: value }))}
              />
              <div className="grid gap-4 sm:grid-cols-2">
                <SelectField
                  label="From unit"
                  value={conversion.fromUnit}
                  onChange={(value) => setConversion((current) => ({ ...current, fromUnit: value }))}
                />
                <SelectField
                  label="To unit"
                  value={conversion.toUnit}
                  onChange={(value) => setConversion((current) => ({ ...current, toUnit: value }))}
                />
              </div>
            </>
          )}

          <button
            type="button"
            onClick={handleCalculate}
            disabled={isCalculating}
            className="w-full rounded-full bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5 disabled:translate-y-0 disabled:opacity-60"
          >
            {isCalculating ? 'Calculating...' : 'Calculate'}
          </button>
        </div>

        {error && (
          <div className="mt-5 rounded-2xl border border-red-400/20 bg-red-500/10 px-4 py-3 text-sm text-red-200">
            {error}
          </div>
        )}

        {result && (
          <div className="mt-6 space-y-5">
            <div className="rounded-[1.6rem] border border-emerald-400/16 bg-emerald-500/10 px-5 py-5">
              <p className="text-xs uppercase tracking-[0.22em] text-white/40">Result</p>
              <p className="mt-2 text-3xl font-semibold text-white">
                {result.output} {result.unit}
              </p>
              {derivedNote && <p className="mt-3 text-sm text-emerald-200/80">{derivedNote}</p>}
            </div>

            <div className="rounded-2xl border border-white/10 bg-black/20 px-4 py-4 text-sm text-white/60">
              <p className="mb-2 text-xs uppercase tracking-[0.2em] text-white/35">Formula</p>
              <p className="font-mono text-white/72">{result.formula}</p>
            </div>

            <div className="rounded-2xl border border-white/10 bg-white/[0.02] px-4 py-4">
              <p className="text-sm text-white/72">
                Save this calculation and get the Reconstitution &amp; Dosing Reference Card.
              </p>
              <form onSubmit={handleLeadCapture} className="mt-4 flex flex-col gap-3 sm:flex-row">
                <input
                  type="email"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  placeholder="you@example.com"
                  className="min-w-0 flex-1 rounded-full border border-white/12 bg-black/20 px-4 py-3 text-sm text-white outline-none transition-colors placeholder:text-white/30 focus:border-emerald-400/40"
                />
                <button
                  type="submit"
                  disabled={leadState === 'saving'}
                  className="rounded-full border border-white/12 px-5 py-3 text-sm font-medium text-white transition-colors hover:border-white/24 hover:text-emerald-200 disabled:opacity-60"
                >
                  {leadState === 'saving' ? 'Saving...' : 'Save This Result'}
                </button>
              </form>

              {leadState === 'saved' && (
                <p className="mt-3 text-sm text-emerald-200/80">
                  Saved. Your next step is creating a free account so calculations stay attached to your protocol.
                </p>
              )}

              {leadState === 'error' && (
                <p className="mt-3 text-sm text-amber-200/80">
                  Lead capture failed. The calculator still works, and you can continue without saving.
                </p>
              )}
            </div>

            <p className="text-xs leading-6 text-white/38">
              Calculated result. Verify against your source material before use. BioStack calculations are mathematical utilities, not clinical guidance.
            </p>
          </div>
        )}

        <div className="mt-6 flex flex-wrap items-center gap-3 text-sm text-white/55">
          <Link href="/auth/signin" className="text-emerald-200 transition-colors hover:text-white">
            Create free account
          </Link>
          <span className="text-white/20">/</span>
          <Link href="/pricing" className="transition-colors hover:text-white">
            See pricing
          </Link>
        </div>
      </div>
    </section>
  );
}

function Field({
  label,
  value,
  onChange,
}: {
  label: string;
  value: number;
  onChange: (value: number) => void;
}) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm text-white/62">{label}</span>
      <input
        type="number"
        step="0.1"
        value={value}
        onChange={(event) => onChange(Number(event.target.value))}
        className="w-full rounded-2xl border border-white/10 bg-black/20 px-4 py-3 text-white outline-none transition-colors placeholder:text-white/30 focus:border-emerald-400/40"
      />
    </label>
  );
}

function SelectField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm text-white/62">{label}</span>
      <select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="w-full rounded-2xl border border-white/10 bg-black/20 px-4 py-3 text-white outline-none transition-colors focus:border-emerald-400/40"
      >
        <option value="mg">mg</option>
        <option value="mcg">mcg</option>
        <option value="g">g</option>
      </select>
    </label>
  );
}
