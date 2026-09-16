'use client';

import { ActiveProfileChip } from '@/components/ActiveProfileChip';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { Header } from '@/components/Header';
import { LoadingSkeleton } from '@/components/LoadingState';
import { CompoundForm } from '@/components/compounds/CompoundForm';
import { CompoundEditForm } from '@/components/compounds/CompoundEditForm';
import { CompoundList } from '@/components/compounds/CompoundList';
import { CompoundIntelligenceCard } from '@/components/knowledge/CompoundIntelligenceCard';
import { ApiError, apiClient } from '@/lib/api';
import { compoundGoalDisplay } from '@/lib/compoundGoalLabels';
import { useProfile } from '@/lib/context';
import { cn } from '@/lib/utils';
import { useSidebarCollapsed } from '@/lib/useSidebarCollapsed';
import { CompoundRecord, KnowledgeEntry } from '@/lib/types';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useRef, useState } from 'react';

const CONSENT_RETURN_TO = '/compounds';

function CompoundsPageContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  // Deep link from a dossier's "Add to protocol" action (see
  // CompoundIntelligenceCard) — opens the add form with the compound
  // preselected once the knowledge base loads inside CompoundForm.
  const prefillSlug = searchParams.get('compound');
  const { currentProfileId } = useProfile();
  const [sidebarCollapsed] = useSidebarCollapsed();
  const [compounds, setCompounds] = useState<CompoundRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [addError, setAddError] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);
  // A dossier deep link (?compound=<slug>) opens straight to the add form
  // instead of leaving the visitor to find "Add Compound" themselves. Read
  // once at init rather than in an effect — searchParams is already known
  // on first render.
  const [showForm, setShowForm] = useState(() => Boolean(prefillSlug));
  const [selectedCompound, setSelectedCompound] = useState<CompoundRecord | null>(null);
  const [knowledgeEntry, setKnowledgeEntry] = useState<KnowledgeEntry | null>(null);
  const [loadingKnowledge, setLoadingKnowledge] = useState(false);
  const [editingCompound, setEditingCompound] = useState<CompoundRecord | null>(null);
  const [savingEdit, setSavingEdit] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [confirmingDeleteId, setConfirmingDeleteId] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const editButtonRef = useRef<HTMLButtonElement | null>(null);
  const deleteButtonRef = useRef<HTMLButtonElement | null>(null);
  const profileEpochRef = useRef(0);
  const selectionVersionRef = useRef(0);
  const wasEditingRef = useRef(false);
  const wasConfirmingDeleteRef = useRef(false);
  const pendingDeleteFailureFocusRef = useRef(false);

  useEffect(() => {
    // An old profile's delete must never restore records into a new profile.
    profileEpochRef.current += 1;
    selectionVersionRef.current += 1;
    if (currentProfileId) {
      loadCompounds();
    }
  }, [currentProfileId]);

  // Return keyboard focus to the action that opened the edit/confirm-delete
  // panel once it closes, instead of leaving it stranded on a removed element.
  useEffect(() => {
    if (wasEditingRef.current && !editingCompound) {
      editButtonRef.current?.focus();
    }
    wasEditingRef.current = editingCompound !== null;
  }, [editingCompound]);

  useEffect(() => {
    if (wasConfirmingDeleteRef.current && !confirmingDeleteId) {
      deleteButtonRef.current?.focus();
    }
    wasConfirmingDeleteRef.current = confirmingDeleteId !== null;
  }, [confirmingDeleteId]);

  // A failed delete rolls the record (and, if it was the open one, the
  // selection) back on a later render than the confirm-panel-closed effect
  // above already fired on, so that effect's focus() call lands on a Delete
  // button that is about to be unmounted by the optimistic removal, not the
  // one that reappears once the rollback restores the selection. Re-focus
  // it once the restored panel (and its Delete button) is back in the DOM.
  useEffect(() => {
    if (pendingDeleteFailureFocusRef.current && selectedCompound && !confirmingDeleteId) {
      pendingDeleteFailureFocusRef.current = false;
      deleteButtonRef.current?.focus();
    }
  }, [selectedCompound, confirmingDeleteId]);

  const loadCompounds = async () => {
    try {
      setLoading(true);
      setError(null);
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
      setAdding(true);
      setAddError(null);
      const newCompound = await apiClient.createCompound(currentProfileId!, data);
      setCompounds(previous => [...previous, newCompound]);
      setShowForm(false);
    } catch (err) {
      setAddError(err instanceof ApiError ? err.message : 'Failed to add compound');
      throw err;
    } finally {
      setAdding(false);
    }
  };

  const loadKnowledgeEntry = async (name: string) => {
    const trimmedName = name.trim();
    // A record with no name (e.g. recovered from the accidental-Enter bug)
    // has nothing to look up — skip the request rather than hitting a route
    // that was never meant to receive an empty segment.
    if (!trimmedName) {
      setKnowledgeEntry(null);
      setLoadingKnowledge(false);
      return;
    }
    setLoadingKnowledge(true);
    try {
      const entry = await apiClient.getKnowledgeEntry(trimmedName);
      setKnowledgeEntry(entry);
    } catch (err) {
      setKnowledgeEntry(null);
    } finally {
      setLoadingKnowledge(false);
    }
  };

  const handleSelectCompound = (compound: CompoundRecord) => {
    selectionVersionRef.current += 1;
    setSelectedCompound(compound);
    setEditingCompound(null);
    setEditError(null);
    setConfirmingDeleteId(null);
    setDeleteError(null);
    void loadKnowledgeEntry(compound.name ?? '');
  };

  const handleStartEdit = (compound: CompoundRecord) => {
    setEditingCompound(compound);
    setEditError(null);
    setConfirmingDeleteId(null);
  };

  const handleCancelEdit = () => {
    setEditingCompound(null);
    setEditError(null);
  };

  const handleSaveEdit = async (data: Omit<CompoundRecord, 'id'>) => {
    if (!editingCompound || !currentProfileId) return;
    try {
      setSavingEdit(true);
      setEditError(null);
      const updated = await apiClient.updateCompound(currentProfileId, editingCompound.id, data);
      setCompounds(previous => previous.map(c => (c.id === updated.id ? updated : c)));
      selectionVersionRef.current += 1;
      setSelectedCompound(updated);
      setEditingCompound(null);
      await loadKnowledgeEntry(updated.name ?? '');
    } catch (err) {
      if (err instanceof ApiError && err.code === 'consent_required') {
        router.push(`/onboarding/consent?returnTo=${encodeURIComponent(CONSENT_RETURN_TO)}`);
        return;
      }
      setEditError(err instanceof ApiError ? err.message : 'Failed to update compound');
      throw err;
    } finally {
      setSavingEdit(false);
    }
  };

  const handleRequestDelete = (compoundId: string) => {
    setConfirmingDeleteId(compoundId);
    setDeleteError(null);
  };

  const handleCancelDelete = () => {
    setConfirmingDeleteId(null);
  };

  const handleConfirmDelete = async (compound: CompoundRecord) => {
    if (!currentProfileId) return;
    const profileEpoch = profileEpochRef.current;
    const originalIndex = compounds.findIndex(c => c.id === compound.id);
    const selectionVersion = ++selectionVersionRef.current;
    const wasSelected = selectedCompound?.id === compound.id;
    const previousSelected = selectedCompound;
    const previousKnowledge = knowledgeEntry;

    setDeletingId(compound.id);
    setDeleteError(null);
    setConfirmingDeleteId(null);
    // Optimistic removal so the list (and the Observer active-compound count
    // it feeds) reflects the deletion immediately; rolled back on failure.
    setCompounds(previous => previous.filter(c => c.id !== compound.id));
    if (wasSelected) {
      setSelectedCompound(null);
      setKnowledgeEntry(null);
    }

    try {
      await apiClient.deleteCompound(currentProfileId, compound.id);
      if (profileEpochRef.current !== profileEpoch) return;
      if (selectionVersionRef.current === selectionVersion && editingCompound?.id === compound.id) {
        setEditingCompound(null);
      }
    } catch (err) {
      if (profileEpochRef.current !== profileEpoch) return;
      setCompounds(current => {
        if (current.some(c => c.id === compound.id)) return current;
        const restored = [...current];
        restored.splice(Math.min(Math.max(originalIndex, 0), restored.length), 0, compound);
        return restored;
      });
      const restoresSelection = wasSelected && selectionVersionRef.current === selectionVersion;
      if (restoresSelection) {
        setSelectedCompound(previousSelected);
        setKnowledgeEntry(previousKnowledge);
      }
      if (err instanceof ApiError && err.code === 'consent_required') {
        router.push(`/onboarding/consent?returnTo=${encodeURIComponent(CONSENT_RETURN_TO)}`);
        return;
      }
      if (restoresSelection) {
        // The confirm-panel-closed effect already returned focus to the
        // Delete button that is about to disappear along with the
        // optimistic removal above; hand off to the effect that waits for
        // the restored panel's Delete button to exist.
        pendingDeleteFailureFocusRef.current = true;
      }
      setDeleteError(err instanceof ApiError ? err.message : 'Failed to delete compound');
    } finally {
      if (profileEpochRef.current === profileEpoch) {
        setDeletingId(current => current === compound.id ? null : current);
      }
    }
  };

  if (!currentProfileId) {
    return (
      <div className="w-full">
        <Header title="Compounds" />
        <div className="p-8">
          <EmptyState
            title="Let's set up your first profile"
            description="Your profile personalizes overlap checks and keeps your protocol in one place."
            icon="👤"
            action={{ label: 'Create profile', onClick: () => router.push('/profiles') }}
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
        <ActiveProfileChip />

        {showForm && (
          <div className="p-6 bg-[#121923]/90 border border-white/[0.08] rounded-2xl shadow-[0_8px_24px_rgba(0,0,0,0.35)]">
            <h2 className="text-lg font-semibold text-white mb-4">Add New Compound</h2>
            {addError && <p role="alert" className="mb-4 text-sm text-red-300">{addError}</p>}
            <CompoundForm
              personId={currentProfileId}
              onSubmit={handleAddCompound}
              isLoading={adding}
              initialCompoundSlug={prefillSlug ?? undefined}
            />
          </div>
        )}

        {loading ? (
          <LoadingSkeleton />
        ) : compounds.length === 0 ? (
          <EmptyState
            title="No Compounds Yet"
            description="Browse the library to see what the research says before adding anything."
            icon="🧪"
            action={{
              label: 'Add Your First Compound',
              onClick: () => setShowForm(true),
            }}
            secondaryAction={{
              label: 'Browse the library',
              href: '/knowledge',
            }}
          />
        ) : (
          <div
            data-testid="compounds-detail-grid"
            className={cn(
              'grid grid-cols-1 gap-6',
              // With the sidebar collapsed at lg+, give the detail column the
              // reclaimed width instead of leaving it at a fixed 1/3 share.
              sidebarCollapsed ? 'lg:grid-cols-5' : 'lg:grid-cols-3'
            )}
          >
            <div className="lg:col-span-2">
              <h2 className="text-lg font-semibold text-white mb-4">Compound List</h2>
              <CompoundList
                compounds={compounds}
                onSelect={handleSelectCompound}
              />
            </div>

            <div className={cn(sidebarCollapsed && 'lg:col-span-3')}>
              {selectedCompound ? (
                <div className="space-y-4">
                  <div className="p-4 bg-[#121923]/90 border border-white/[0.08] rounded-2xl shadow-[0_8px_24px_rgba(0,0,0,0.35)]">
                    {editingCompound?.id === selectedCompound.id ? (
                      <>
                        <h3 className="font-semibold text-white mb-4">Edit Compound</h3>
                        {editError && <p role="alert" className="mb-4 text-sm text-red-300">{editError}</p>}
                        <CompoundEditForm
                          compound={editingCompound}
                          onSubmit={handleSaveEdit}
                          onCancel={handleCancelEdit}
                          isLoading={savingEdit}
                        />
                      </>
                    ) : (
                      <>
                        <div className="flex items-start justify-between gap-3 mb-2">
                          <h3 className="font-semibold text-white break-words">
                            {selectedCompound.name?.trim() ? selectedCompound.name : 'Unnamed compound'}
                          </h3>
                          <div className="flex items-center gap-2 shrink-0">
                            <button
                              ref={editButtonRef}
                              type="button"
                              onClick={() => handleStartEdit(selectedCompound)}
                              aria-label={`Edit ${selectedCompound.name?.trim() || 'this compound'}`}
                              className="px-3 py-1.5 text-xs font-medium rounded-lg bg-white/5 hover:bg-white/10 text-white/70 transition-all"
                            >
                              Edit
                            </button>
                            <button
                              ref={deleteButtonRef}
                              type="button"
                              onClick={() => handleRequestDelete(selectedCompound.id)}
                              aria-label={`Delete ${selectedCompound.name?.trim() || 'this compound'}`}
                              className="px-3 py-1.5 text-xs font-medium rounded-lg bg-red-500/10 hover:bg-red-500/20 text-red-300 transition-all"
                            >
                              Delete
                            </button>
                          </div>
                        </div>

                        {deleteError && <p role="alert" className="mb-3 text-sm text-red-300">{deleteError}</p>}

                        {confirmingDeleteId === selectedCompound.id && (
                          <div role="group" aria-label="Confirm delete" className="mb-3 p-3 rounded-xl border border-red-500/30 bg-red-500/10 space-y-2">
                            <p className="text-sm text-white/80">Delete this compound? This can&apos;t be undone.</p>
                            <div className="flex gap-2">
                              <button
                                type="button"
                                onClick={() => handleConfirmDelete(selectedCompound)}
                                disabled={deletingId === selectedCompound.id}
                                className="px-3 py-1.5 text-xs font-semibold rounded-lg bg-red-500 hover:bg-red-400 disabled:bg-white/10 disabled:text-white/30 text-slate-950 transition-all"
                              >
                                {deletingId === selectedCompound.id ? 'Deleting...' : 'Confirm delete'}
                              </button>
                              <button
                                type="button"
                                onClick={handleCancelDelete}
                                disabled={deletingId === selectedCompound.id}
                                className="px-3 py-1.5 text-xs font-medium rounded-lg bg-white/5 hover:bg-white/10 text-white/70 transition-all"
                              >
                                Cancel
                              </button>
                            </div>
                          </div>
                        )}

                        <div className="space-y-2 text-sm text-white/65">
                          <p><span className="text-white/40">Category:</span> {selectedCompound.category}</p>
                          {selectedCompound.goal && (
                            <div>
                              <p><span className="text-white/40">Goal:</span> {compoundGoalDisplay(selectedCompound.goal).label}</p>
                              {compoundGoalDisplay(selectedCompound.goal).context && (
                                <p className="mt-1 break-words text-xs leading-relaxed text-white/55">{compoundGoalDisplay(selectedCompound.goal).context}</p>
                              )}
                            </div>
                          )}
                          {selectedCompound.source && <p><span className="text-white/40">Source:</span> {selectedCompound.source}</p>}
                          {selectedCompound.pricePaid && <p><span className="text-white/40">Price:</span> ${selectedCompound.pricePaid}</p>}
                          <p><span className="text-white/40">Status:</span> {selectedCompound.status}</p>
                          <p><span className="text-white/40">Start Date:</span> {selectedCompound.startDate}</p>
                          {selectedCompound.notes && (
                            <p><span className="text-white/40">Notes:</span> {selectedCompound.notes}</p>
                          )}
                        </div>
                      </>
                    )}
                  </div>

                  {!editingCompound && (
                    loadingKnowledge ? (
                      <div className="p-4 bg-[#121923]/90 border border-white/[0.08] rounded-2xl shadow-[0_8px_24px_rgba(0,0,0,0.35)] text-center text-sm text-white/50">
                        Loading knowledge...
                      </div>
                    ) : knowledgeEntry ? (
                      <CompoundIntelligenceCard entry={knowledgeEntry} recommendationSurface="compound-detail" />
                    ) : (
                      <div className="p-4 bg-[#121923]/90 border border-white/[0.08] rounded-2xl shadow-[0_8px_24px_rgba(0,0,0,0.35)] text-center text-sm text-white/50">
                        No reference entry for this compound
                      </div>
                    )
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

export default function CompoundsPage() {
  return (
    <Suspense fallback={<div className="w-full"><Header title="Compounds" /></div>}>
      <CompoundsPageContent />
    </Suspense>
  );
}
