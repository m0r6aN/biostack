'use client';

import { apiClient } from '@/lib/api';
import { CalculatorResult, ReconstitutionRequest, VolumeRequest, ConversionRequest } from '@/lib/types';
import { SafetyDisclaimer } from '@/components/SafetyDisclaimer';
import { ReconstitutionCalc } from '@/components/calculators/ReconstitutionCalc';
import { VolumeCalc } from '@/components/calculators/VolumeCalc';
import { ConversionCalc } from '@/components/calculators/ConversionCalc';
import { useState } from 'react';
import { Header } from '@/components/Header';

export default function CalculatorsPage() {
  const [activeTab, setActiveTab] = useState<'reconstitution' | 'volume' | 'conversion'>('reconstitution');

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
            />
          )}

          {activeTab === 'volume' && (
            <VolumeCalc
              onCalculate={(req) => apiClient.calculateVolume(req)}
            />
          )}

          {activeTab === 'conversion' && (
            <ConversionCalc
              onCalculate={(req) => apiClient.calculateConversion(req)}
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
