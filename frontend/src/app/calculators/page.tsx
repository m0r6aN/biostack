'use client';

import { apiClient } from '@/lib/api';
import { CalculatorResult, CalculatorResultRecord, CompoundRecord, SaveCalculatorResultRequest } from '@/lib/types';
import { SafetyDisclaimer } from '@/components/SafetyDisclaimer';
import { ReconstitutionCalc } from '@/components/calculators/ReconstitutionCalc';
import { VolumeCalc } from '@/components/calculators/VolumeCalc';
import { ConversionCalc } from '@/components/calculators/ConversionCalc';
import { useEffect, useState } from 'react';
import { Header } from '@/components/Header';
import { useProfile } from '@/lib/context';

export default function CalculatorsPage() {
  const { currentProfileId } = useProfile();
  const [activeTab, setActiveTab] = useState<'reconstitution' | 'volume' | 'conversion'>('reconstitution');
  const [compounds, setCompounds] = useState<CompoundRecord[]>([]);
  const [pendingSave, setPendingSave] = useState<SaveCalculatorResultRequest | null>(null);
  const [savedResult, setSavedResult] = useState<CalculatorResultRecord | null>(null);
  const [selectedCompoundId, setSelectedCompoundId] = useState('');
  const [attachStatus, setAttachStatus] = useState<string | null>(null);

  useEffect(() => {
    if (!currentProfileId) {
      setCompounds([]);
      return;
    }

    apiClient.getCompounds(currentProfileId)
      .then(setCompounds)
      .catch(() => setCompounds([]));
  }, [currentProfileId]);

  const handleCalculatorResult = (kind: string, inputs: Record<string, string>, result: CalculatorResult) => {
    setSavedResult(null);
    setAttachStatus(null);
    setPendingSave({
      calculatorKind: kind,
      inputs,
      outputs: {
        input: String(result.input),
        output: String(result.output),
        unit: result.unit,
      },
      unit: result.unit,
      formula: result.formula,
      displaySummary: `${kind} result: ${result.output} ${result.unit}`,
      compoundRecordId: null,
    });
  };

  const handleSaveResult = async () => {
    if (!currentProfileId || !pendingSave) {
      setAttachStatus('Select a profile to save this calculation in-app.');
      return;
    }

    const result = await apiClient.saveCalculatorResult(currentProfileId, pendingSave);
    setSavedResult(result);
    setAttachStatus('Saved as a profile calculation.');
  };

  const handleAttachResult = async () => {
    if (!currentProfileId || !savedResult || !selectedCompoundId) {
      return;
    }

    const attached = await apiClient.attachCalculatorResult(currentProfileId, savedResult.id, selectedCompoundId);
    setSavedResult(attached);
    setAttachStatus('Attached to compound record.');
  };

  return (
    <div className="w-full">
      <Header title="Calculators" subtitle="Dosage · Reconstitution · Conversion" />

      <div className="p-8 max-w-4xl">
        <SafetyDisclaimer type="calculation" />

        {/* Tabs */}
        <div className="mt-6 flex gap-2 border-b border-white/[0.06] mb-6">
          {[
            { id: 'reconstitution', label: 'Reconstitution' },
            { id: 'volume', label: 'Volume' },
            { id: 'conversion', label: 'Conversion' },
          ].map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id as any)}
              className={`px-4 py-2 font-medium text-sm border-b-2 transition-all ${
                activeTab === tab.id
                  ? 'border-emerald-400 text-emerald-300'
                  : 'border-transparent text-white/45 hover:text-white/70'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Tab Content */}
        <div className="space-y-6">
          {activeTab === 'reconstitution' && (
            <ReconstitutionCalc
              onCalculate={(req) => apiClient.calculateReconstitution(req)}
              onResult={handleCalculatorResult}
            />
          )}

          {activeTab === 'volume' && (
            <VolumeCalc
              onCalculate={(req) => apiClient.calculateVolume(req)}
              onResult={handleCalculatorResult}
            />
          )}

          {activeTab === 'conversion' && (
            <ConversionCalc
              onCalculate={(req) => apiClient.calculateConversion(req)}
              onResult={handleCalculatorResult}
            />
          )}
        </div>

        {pendingSave && (
          <div className="mt-6 rounded-2xl border border-emerald-400/15 bg-emerald-500/10 p-5">
            <h3 className="text-sm font-semibold text-emerald-200">Attach calculation to compound</h3>
            <p className="mt-1 text-sm text-emerald-100/65">
              Save this reference calculation to the selected profile, then attach it to an existing compound record.
            </p>
            <div className="mt-4 flex flex-col gap-3 md:flex-row">
              <button
                onClick={handleSaveResult}
                className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-400"
              >
                {savedResult ? 'Saved' : 'Save Result'}
              </button>
              <select
                value={selectedCompoundId}
                onChange={(event) => setSelectedCompoundId(event.target.value)}
                className="min-w-0 flex-1 rounded-lg border border-white/10 bg-[#0F141B] px-3 py-2 text-sm text-white"
              >
                <option value="">Select compound to attach...</option>
                {compounds.map((compound) => (
                  <option key={compound.id} value={compound.id}>
                    {compound.canonicalName || compound.name}
                  </option>
                ))}
              </select>
              <button
                onClick={handleAttachResult}
                disabled={!savedResult || !selectedCompoundId}
                className="rounded-lg border border-white/10 px-4 py-2 text-sm font-semibold text-white/80 disabled:opacity-40"
              >
                Attach to Compound
              </button>
            </div>
            {attachStatus && <p className="mt-3 text-xs text-emerald-100/65">{attachStatus}</p>}
          </div>
        )}

        {/* Info Section */}
        <div className="mt-8 p-5 rounded-2xl border border-blue-400/15 bg-blue-500/10">
          <h3 className="text-sm font-semibold text-blue-300 mb-2">About These Calculators</h3>
          <p className="text-sm text-blue-200/70">
            These are mathematical tools for calculating concentrations, volumes, and unit conversions. They provide reference calculations only and are not intended for medical decision-making.
          </p>
        </div>
      </div>
    </div>
  );
}
