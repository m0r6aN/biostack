'use client';

import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { Header } from '@/components/Header';
import { LoadingSkeleton } from '@/components/LoadingState';
import { CompoundForm } from '@/components/compounds/CompoundForm';
import { CompoundList } from '@/components/compounds/CompoundList';
import { CompoundIntelligenceCard } from '@/components/knowledge/CompoundIntelligenceCard';
import { apiClient } from '@/lib/api';
import { useProfile } from '@/lib/context';
import { CompoundRecord, KnowledgeEntry } from '@/lib/types';
import { useEffect, useState } from 'react';

export default function CompoundsPage() {
  const { currentProfileId } = useProfile();
  const [compounds, setCompounds] = useState<CompoundRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [selectedCompound, setSelectedCompound] = useState<CompoundRecord | null>(null);
  const [knowledgeEntry, setKnowledgeEntry] = useState<KnowledgeEntry | null>(null);
  const [loadingKnowledge, setLoadingKnowledge] = useState(false);

  useEffect(() => {
    if (currentProfileId) {
      loadCompounds();
    }
  }, [currentProfileId]);

  const loadCompounds = async () => {
    try {
      setLoading(true);
      const data = await apiClient.getCompounds(currentProfileId!);
      setCompounds(data);
    } catch (err) {
      setError('Failed to load compounds');
    } finally {
      setLoading(false);
    }
  };

  const handleAddCompound = async (data: Omit<CompoundRecord, 'id'>) => {
    try {
      const newCompound = await apiClient.createCompound(currentProfileId!, data);
      setCompounds([...compounds, newCompound]);
      setShowForm(false);
    } catch (err) {
      setError('Failed to add compound');
    }
  };

  const handleSelectCompound = async (compound: CompoundRecord) => {
    setSelectedCompound(compound);
    setLoadingKnowledge(true);
    try {
      const entry = await apiClient.getKnowledgeEntry(compound.canonicalName || compound.name);
      setKnowledgeEntry(entry);
    } catch (err) {
      setKnowledgeEntry(null);
    } finally {
      setLoadingKnowledge(false);
    }
  };

  if (!currentProfileId) {
    return (
      <div className="w-full">
        <Header title="Compounds" />
        <div className="p-8">
          <EmptyState
            title="No Profile Selected"
            description="Select a profile to view and manage compounds"
            icon="👤"
          />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="w-full">
        <Header title="Compounds" />
        <div className="p-8">
          <ErrorState message={error} onRetry={loadCompounds} />
        </div>
      </div>
    );
  }

  return (
    <div className="w-full">
      <Header
        title="Compounds"
        actions={
          <button
            onClick={() => setShowForm(!showForm)}
            className="px-4 py-2 bg-emerald-500 hover:bg-emerald-400 text-slate-950 rounded-xl text-sm font-medium transition-all duration-150"
          >
            {showForm ? 'Cancel' : 'Add Compound'}
          </button>
        }
      />

      <div className="p-8 space-y-8 max-w-6xl">
        {showForm && (
          <div className="p-6 bg-[#121923]/90 border border-white/[0.08] rounded-2xl shadow-[0_8px_24px_rgba(0,0,0,0.35)]">
            <h2 className="text-lg font-semibold text-white mb-4">Add New Compound</h2>
            <CompoundForm
              personId={currentProfileId}
              onSubmit={handleAddCompound}
            />
          </div>
        )}

        {loading ? (
          <LoadingSkeleton />
        ) : compounds.length === 0 ? (
          <EmptyState
            title="No Compounds Yet"
            description="Start tracking compounds and supplements"
            icon="🧪"
            action={{
              label: 'Add Your First Compound',
              onClick: () => setShowForm(true),
            }}
          />
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div className="lg:col-span-2">
              <h2 className="text-lg font-semibold text-white mb-4">Compound List</h2>
              <CompoundList
                compounds={compounds}
                onSelect={handleSelectCompound}
              />
            </div>

            <div>
              {selectedCompound ? (
                <div className="space-y-4">
                  <div className="p-4 bg-[#121923]/90 border border-white/[0.08] rounded-2xl shadow-[0_8px_24px_rgba(0,0,0,0.35)]">
                    <h3 className="font-semibold text-white mb-2">{selectedCompound.name}</h3>
                    <div className="space-y-2 text-sm text-white/65">
                      <p><span className="text-white/40">Category:</span> {selectedCompound.category}</p>
                      <p>
                        <span className="text-white/40">Knowledge link:</span>{' '}
                        {selectedCompound.isCanonical
                          ? selectedCompound.canonicalName || 'Canonical'
                          : 'Manual / unresolved'}
                      </p>
                      {selectedCompound.goal && <p><span className="text-white/40">Goal:</span> {selectedCompound.goal}</p>}
                      {selectedCompound.source && <p><span className="text-white/40">Source:</span> {selectedCompound.source}</p>}
                      {selectedCompound.pricePaid && <p><span className="text-white/40">Price:</span> ${selectedCompound.pricePaid}</p>}
                      <p><span className="text-white/40">Status:</span> {selectedCompound.status}</p>
                      <p><span className="text-white/40">Start Date:</span> {selectedCompound.startDate}</p>
                      {selectedCompound.notes && (
                        <p><span className="text-white/40">Notes:</span> {selectedCompound.notes}</p>
                      )}
                    </div>
                  </div>

                  {loadingKnowledge ? (
                    <div className="p-4 bg-[#121923]/90 border border-white/[0.08] rounded-2xl shadow-[0_8px_24px_rgba(0,0,0,0.35)] text-center text-sm text-white/50">
                      Loading knowledge...
                    </div>
                  ) : knowledgeEntry ? (
                    <>
                      <div className="p-4 bg-[#121923]/90 border border-white/[0.08] rounded-2xl shadow-[0_8px_24px_rgba(0,0,0,0.35)]">
                        <h3 className="font-semibold text-white mb-3">Reference schedule from knowledge base</h3>
                        <div className="space-y-2 text-sm text-white/65">
                          {knowledgeEntry.frequency && <p><span className="text-white/40">Frequency:</span> {knowledgeEntry.frequency}</p>}
                          {knowledgeEntry.preferredTimeOfDay && <p><span className="text-white/40">Timing:</span> {knowledgeEntry.preferredTimeOfDay}</p>}
                          {knowledgeEntry.weeklyDosageSchedule?.slice(0, 3).map((item) => (
                            <p key={item}>{item}</p>
                          ))}
                          {knowledgeEntry.incrementalEscalationSteps?.[0] && (
                            <p><span className="text-white/40">Escalation reference:</span> {knowledgeEntry.incrementalEscalationSteps[0]}</p>
                          )}
                        </div>
                      </div>
                      <CompoundIntelligenceCard entry={knowledgeEntry} />
                    </>
                  ) : (
                    <div className="p-4 bg-[#121923]/90 border border-white/[0.08] rounded-2xl shadow-[0_8px_24px_rgba(0,0,0,0.35)] text-center text-sm text-white/50">
                      No knowledge entry available
                    </div>
                  )}
                </div>
              ) : (
                <div className="p-6 bg-[#121923]/90 border border-white/[0.08] rounded-2xl shadow-[0_8px_24px_rgba(0,0,0,0.35)] text-center">
                  <p className="text-sm text-white/50">Select a compound to view details</p>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
