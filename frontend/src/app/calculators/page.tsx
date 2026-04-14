'use client';

import { apiClient } from '@/lib/api';
import { Protocol } from '@/lib/types';
import { SafetyDisclaimer } from '@/components/SafetyDisclaimer';
import { UnifiedDosingCalculator } from '@/components/calculators/UnifiedDosingCalculator';
import { formatDose, formatNumber, type UnifiedDosingInput, type UnifiedDosingResult } from '@/lib/dosingCalculator';
import { useProfile } from '@/lib/context';
import { useEffect, useState } from 'react';
import { Header } from '@/components/Header';

export default function CalculatorsPage() {
  const { currentProfileId } = useProfile();
  const [protocols, setProtocols] = useState<Protocol[]>([]);
  const [selectedProtocolId, setSelectedProtocolId] = useState('');
  const [attachedMessage, setAttachedMessage] = useState<string | null>(null);
  const [isRecording, setIsRecording] = useState(false);

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

  async function attachCalculation(input: UnifiedDosingInput, result: UnifiedDosingResult) {
    if (selectedProtocolId) {
      const protocol = protocols.find((item) => item.id === selectedProtocolId);
      setIsRecording(true);
      try {
        await apiClient.recordProtocolComputation(selectedProtocolId, {
          runId: protocol?.activeRun?.id ?? null,
          type: 'dosage',
          inputSnapshot: JSON.stringify(input),
          outputResult: `${formatDose(result.dosePerAdministrationMcg)} per dose, ${formatNumber(result.volumePerAdministrationMl, 4)} mL draw, ${formatNumber(result.concentrationMcgPerMl)} mcg/mL concentration`,
        });
        setAttachedMessage(`Dosing calculation attached to ${protocol?.name ?? 'protocol'}.`);
        window.setTimeout(() => setAttachedMessage(null), 2800);
      } finally {
        setIsRecording(false);
      }
    }
  }

  return (
    <div className="w-full">
      <Header title="Calculators" subtitle="Reconstitution · Dosing · Conversion" />

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

        <div className="mt-6">
          <UnifiedDosingCalculator
            attachedMessage={attachedMessage}
            isRecording={isRecording}
            onRecord={selectedProtocolId ? attachCalculation : undefined}
          />
        </div>

        {/* Info Section */}
        <div className="mt-8 p-5 rounded-2xl border border-blue-400/15 bg-blue-500/10">
          <h3 className="text-sm font-semibold text-blue-300 mb-2">About These Calculators</h3>
          <p className="text-sm text-blue-200/70">
            This mathematical tool calculates concentrations, volumes, unit conversions, and split schedules. It provides reference calculations only and is not intended for medical decision-making.
          </p>
        </div>
      </div>
    </div>
  );
}
