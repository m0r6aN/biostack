'use client';

import { CalculatorResult, VolumeRequest } from '@/lib/types';
import { SafetyDisclaimer } from '../SafetyDisclaimer';
import { useState } from 'react';

interface VolumeCalcProps {
  onCalculate: (request: VolumeRequest) => Promise<CalculatorResult>;
}

export function VolumeCalc({ onCalculate }: VolumeCalcProps) {
  const [inputs, setInputs] = useState({
    desiredDoseMcg: 100,
    concentrationMcgPerMl: 100,
  });
  const [result, setResult] = useState<CalculatorResult | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleCalculate = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const res = await onCalculate(inputs);
      setResult(res);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Calculation failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90 shadow-[0_8px_24px_rgba(0,0,0,0.35)]">
        <h3 className="text-lg font-semibold text-white mb-4">Volume Calculator</h3>

        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-white/70 mb-2">
              Desired Amount (mcg)
            </label>
            <input
              type="number"
              step="0.1"
              value={inputs.desiredDoseMcg}
              onChange={(e) =>
                setInputs({ ...inputs, desiredDoseMcg: parseFloat(e.target.value) })
              }
              className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 focus:shadow-[0_0_0_1px_rgba(34,197,94,0.2)] transition-all"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-white/70 mb-2">
              Concentration (mcg/mL)
            </label>
            <input
              type="number"
              step="0.1"
              value={inputs.concentrationMcgPerMl}
              onChange={(e) =>
                setInputs({ ...inputs, concentrationMcgPerMl: parseFloat(e.target.value) })
              }
              className="w-full px-4 py-3 bg-[#0F141B] border border-white/10 rounded-xl text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/50 focus:shadow-[0_0_0_1px_rgba(34,197,94,0.2)] transition-all"
            />
          </div>

          <button
            onClick={handleCalculate}
            disabled={isLoading}
            className="w-full px-5 py-3 bg-emerald-500 hover:bg-emerald-400 disabled:bg-white/10 disabled:text-white/30 text-slate-950 rounded-xl font-medium transition-all"
          >
            {isLoading ? 'Calculating...' : 'Calculate'}
          </button>
        </div>

        {error && (
          <div className="mt-4 p-3 bg-red-500/10 border border-red-500/30 rounded-xl">
            <p className="text-sm text-red-300">{error}</p>
          </div>
        )}

        {result && (
          <div className="mt-6 space-y-4">
            <div className="p-4 bg-blue-500/10 border border-blue-400/15 rounded-2xl">
              <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-1">Volume Required</p>
              <p className="text-2xl font-bold text-blue-300">
                {result.output} {result.unit}
              </p>
            </div>

            {result.formula && (
              <div className="p-3 bg-white/[0.03] border border-white/[0.08] rounded-xl">
                <p className="text-xs text-white/35 mb-1">Formula</p>
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
