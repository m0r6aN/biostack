'use client';

import { apiClient } from '@/lib/api';
import { CalculatorResult, Protocol } from '@/lib/types';
import { SafetyDisclaimer } from '@/components/SafetyDisclaimer';
import { ReconstitutionCalc } from '@/components/calculators/ReconstitutionCalc';
import { VolumeCalc } from '@/components/calculators/VolumeCalc';
import { ConversionCalc } from '@/components/calculators/ConversionCalc';
import { useProfile } from '@/lib/context';
import { useEffect, useState } from 'react';
import { Header } from '@/components/Header';

export default function CalculatorsPage() {
  const [activeTab, setActiveTab] = useState<'reconstitution' | 'volume' | 'conversion'>('reconstitution');
  const { currentProfileId } = useProfile();
  const [protocols, setProtocols] = useState<Protocol[]>([]);
  const [selectedProtocolId, setSelectedProtocolId] = useState('');
  const [attachedMessage, setAttachedMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!currentProfileId) {
      setProtocols([]);
      setSelectedProtocolId('');
      return;
    }

    apiClient.getProtocols(currentProfileId)
      .then((items) => {
        setProtocols(items);
        setSelectedProtocolId((current) => current || items[0]?.id || '');
      })
      .catch(() => {
        setProtocols([]);
        setSelectedProtocolId('');
      });
  }, [currentProfileId]);

  async function calculateAndMaybeAttach<TInput>(
    type: 'reconstitution' | 'volume' | 'conversion',
    input: TInput,
    calculate: (request: TInput) => Promise<CalculatorResult>
  ) {
    const result = await calculate(input);
    if (selectedProtocolId) {
      const protocol = protocols.find((item) => item.id === selectedProtocolId);
      await apiClient.recordProtocolComputation(selectedProtocolId, {
        runId: protocol?.activeRun?.id ?? null,
        type,
        inputSnapshot: JSON.stringify(input),
        outputResult: `${result.output} ${result.unit} using ${result.formula}`,
      });
      setAttachedMessage(`${computationLabel(type)} attached to ${protocol?.name ?? 'protocol'}.`);
      window.setTimeout(() => setAttachedMessage(null), 2800);
    }

    return result;
  }

  return (
    <div className="w-full">
      <Header title="Calculators" subtitle="Dosage · Reconstitution · Conversion" />

      <div className="p-8 max-w-4xl">
        <SafetyDisclaimer type="calculation" />

        <section className="mt-6 rounded-lg border border-white/[0.08] bg-[#121923]/90 p-4">
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/35">Protocol Binding</p>
              <p className="mt-1 text-sm text-white/50">
                {protocols.length > 0
                  ? 'Attach calculator math to protocol lineage.'
                  : 'Save a protocol to attach calculator math to its lineage.'}
              </p>
            </div>
            <select
              value={selectedProtocolId}
              onChange={(event) => setSelectedProtocolId(event.target.value)}
              disabled={protocols.length === 0}
              className="min-w-0 rounded-lg border border-white/[0.1] bg-[#0F141B] px-3 py-2 text-sm text-white outline-none disabled:cursor-not-allowed disabled:opacity-50 md:min-w-72"
            >
              {protocols.length === 0 ? (
                <option value="">No protocols available</option>
              ) : (
                protocols.map((protocol) => (
                  <option key={protocol.id} value={protocol.id}>
                    {protocol.name} v{protocol.version}{protocol.activeRun ? ' · active run' : ''}
                  </option>
                ))
              )}
            </select>
          </div>
          {attachedMessage && (
            <p className="mt-3 rounded-lg border border-emerald-400/20 bg-emerald-500/10 px-3 py-2 text-sm font-semibold text-emerald-100">
              {attachedMessage}
            </p>
          )}
        </section>

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
              onCalculate={(req) => calculateAndMaybeAttach('reconstitution', req, (request) => apiClient.calculateReconstitution(request))}
            />
          )}

          {activeTab === 'volume' && (
            <VolumeCalc
              onCalculate={(req) => calculateAndMaybeAttach('volume', req, (request) => apiClient.calculateVolume(request))}
            />
          )}

          {activeTab === 'conversion' && (
            <ConversionCalc
              onCalculate={(req) => calculateAndMaybeAttach('conversion', req, (request) => apiClient.calculateConversion(request))}
            />
          )}
        </div>

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

function computationLabel(type: 'reconstitution' | 'volume' | 'conversion') {
  if (type === 'reconstitution') return 'Reconstitution calculation';
  if (type === 'volume') return 'Dosage math';
  return 'Unit conversion';
}
