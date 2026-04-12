'use client';

import { CalculatorResult, ConversionRequest } from '@/lib/types';
import { SafetyDisclaimer } from '../SafetyDisclaimer';
import { useState } from 'react';

interface ConversionCalcProps {
  onCalculate: (request: ConversionRequest) => Promise<CalculatorResult>;
  onResult?: (kind: string, inputs: Record<string, string>, result: CalculatorResult) => void;
}

const units = ['mg', 'mcg', 'IU', 'g'];

export function ConversionCalc({ onCalculate, onResult }: ConversionCalcProps) {
  const [inputs, setInputs] = useState({
    amount: 1000,
    fromUnit: 'mcg',
    toUnit: 'mg',
  });
  const [result, setResult] = useState<CalculatorResult | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleCalculate = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const res = await onCalculate({ ...inputs, conversionFactor: 0 });
      setResult(res);
      onResult?.('conversion', {
        amount: String(inputs.amount),
        fromUnit: inputs.fromUnit,
        toUnit: inputs.toUnit,
      }, res);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Calculation failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90 shadow-[0_8px_24px_rgba(0,0,0,0.35)]">
        <h3 className="text-lg font-semibold text-white mb-4">Unit Conversion</h3>

        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-white/70 mb-2">Value</label>
            <input
              type="number"
              step="0.1"
              value={inputs.amount}
              onChange={(e) =>
                setInputs({ ...inputs, amount: parseFloat(e.target.value) })
              }
              className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 focus:shadow-[0_0_0_1px_rgba(34,197,94,0.2)] transition-all"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-white/70 mb-2">From</label>
              <select
                value={inputs.fromUnit}
                onChange={(e) =>
                  setInputs({ ...inputs, fromUnit: e.target.value })
                }
                className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 focus:shadow-[0_0_0_1px_rgba(34,197,94,0.2)] transition-all"
              >
                {units.map((unit) => (
                  <option key={unit} value={unit}>
                    {unit}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-white/70 mb-2">To</label>
              <select
                value={inputs.toUnit}
                onChange={(e) =>
                  setInputs({ ...inputs, toUnit: e.target.value })
                }
                className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 focus:shadow-[0_0_0_1px_rgba(34,197,94,0.2)] transition-all"
              >
                {units.map((unit) => (
                  <option key={unit} value={unit}>
                    {unit}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <button
            onClick={handleCalculate}
            disabled={isLoading}
            className="w-full px-5 py-3 bg-emerald-500 hover:bg-emerald-400 disabled:bg-white/10 disabled:text-white/30 text-slate-950 rounded-xl font-medium transition-all"
          >
            {isLoading ? 'Converting...' : 'Convert'}
          </button>
        </div>

        {error && (
          <div className="mt-4 p-3 bg-red-500/10 border border-red-500/30 rounded-xl">
            <p className="text-sm text-red-300">{error}</p>
          </div>
        )}

        {result && (
          <div className="mt-6 space-y-4">
            <div className="p-4 bg-purple-500/10 border border-purple-400/15 rounded-2xl">
              <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-1">Result</p>
              <p className="text-2xl font-bold text-purple-300">
                {result.output} {result.unit}
              </p>
            </div>

            {result.formula && (
              <div className="p-3 bg-white/[0.03] border border-white/[0.08] rounded-xl">
                <p className="text-xs text-white/35 mb-1">Conversion</p>
                <p className="text-xs text-white/65 font-mono">{result.formula}</p>
              </div>
            )}

            <SafetyDisclaimer type="calculation" />
          </div>
        )}
      </div>
    </div>
  );
}
