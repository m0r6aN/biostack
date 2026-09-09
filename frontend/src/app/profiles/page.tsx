'use client';

import { AnalyzerDraftReviewPanel } from '@/components/dashboard/AnalyzerDraftReviewPanel';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { GoalBadge } from '@/components/goals/GoalBadge';
import { Header } from '@/components/Header';
import { LoadingSkeleton } from '@/components/LoadingState';
import { ProfileForm } from '@/components/profiles/ProfileForm';
import { trackAnalyzerEvent } from '@/lib/analyzerAnalytics';
import {
  ANALYZER_DRAFT_COMPOUND_SOURCE,
  buildAnalyzerDraftCompoundImports,
  getAnalyzerProtocolDraftRevision,
  hasPendingAnalyzerProtocolDraft,
  markAnalyzerProtocolDraftImported,
  readAnalyzerProtocolDraft,
} from '@/lib/analyzerStorage';
import { apiClient } from '@/lib/api';
import {
  buildImportedProfileNotes,
  hasPendingAnonymousToolData,
  markAnonymousToolPayloadImported,
  readAnonymousToolPayload,
  type AnonymousToolPayload,
} from '@/lib/anonymousTools';
import { useProfile } from '@/lib/context';
import {
  clearOnboardingPreview,
  emptyOnboardingPreview,
  readOnboardingPreview,
  type OnboardingPreview,
} from '@/lib/onboardingPreview';
import { useSettings } from '@/lib/settings';
import { getProfilesContinuationStatuses } from '@/lib/systemStatus';
import { useAnalyzerProtocolDraft } from '@/lib/useAnalyzerProtocolDraft';
import { CreateProfileRequest, GoalDefinition, PersonProfile } from '@/lib/types';
import { formatDate, formatWeight } from '@/lib/utils';
import Link from 'next/link';
import { useCallback, useEffect, useRef, useState } from 'react';

export default function ProfilesPage() {
  const { profiles, setProfiles, setCurrentProfileId } = useProfile();
  const { settings } = useSettings();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [onboardingPreview, setOnboardingPreview] = useState<OnboardingPreview>(emptyOnboardingPreview);
  const [toolPayload, setToolPayload] = useState<AnonymousToolPayload | null>(null);
  const [importConfirmation, setImportConfirmation] = useState('');
  const [profileGoalMap, setProfileGoalMap] = useState<Record<string, GoalDefinition[]>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const createStartedRef = useRef(false);
  const [createdProfile, setCreatedProfile] = useState<PersonProfile | null>(null);
  const analyzerDraft = useAnalyzerProtocolDraft();
  const [includeAnalyzerDraft, setIncludeAnalyzerDraft] = useState(true);
  const [dismissedDraftRevision, setDismissedDraftRevision] = useState<string | null>(null);
  const continuationStatuses = getProfilesContinuationStatuses(onboardingPreview.compounds.length > 0);
  const hasPendingToolData = hasPendingAnonymousToolData(toolPayload);
  const pendingAnalyzerDraft =
    hasPendingAnalyzerProtocolDraft(analyzerDraft) &&
    getAnalyzerProtocolDraftRevision(analyzerDraft) !== dismissedDraftRevision ? analyzerDraft : null;
  const analyzerDraftImports = pendingAnalyzerDraft
    ? buildAnalyzerDraftCompoundImports(pendingAnalyzerDraft, [
        ...onboardingPreview.compounds,
        ...(hasPendingToolData ? toolPayload?.draftStackItems.map((item) => item.name) ?? [] : []),
      ])
    : [];

  const loadProfiles = useCallback(async () => {
    try {
      setLoading(true);
      const data = await apiClient.getProfiles();
      setProfiles(data);
    } catch {
      setError('Failed to load profiles');
    } finally {
      setLoading(false);
    }
  }, [setProfiles]);

  useEffect(() => {
    loadProfiles();
    setOnboardingPreview(readOnboardingPreview());
    const pendingToolPayload = readAnonymousToolPayload();
    setToolPayload(pendingToolPayload);
    const bootstrap = new URLSearchParams(window.location.search).get('bootstrap');
    if (bootstrap === 'tools' && hasPendingAnonymousToolData(pendingToolPayload)) {
      setShowForm(true);
    }
    if (bootstrap === 'analyzer' && hasPendingAnalyzerProtocolDraft(readAnalyzerProtocolDraft())) {
      setShowForm(true);
    }
  }, [loadProfiles]);

  const handleCreateProfile = async (data: CreateProfileRequest & { selectedGoalIds?: string[] }) => {
    if (loading || createStartedRef.current) return;
    createStartedRef.current = true;
    let newProfile: PersonProfile | null = null;
    try {
      setIsSubmitting(true);
      setError(null);
      const { selectedGoalIds: goalIds, ...profileData } = data;
      newProfile = await apiClient.createProfile(profileData);
      // Publish the created target before any follow-up writes can fail.
      // A partial import continues on this profile, never by creating it again.
      setCreatedProfile(newProfile);
      setProfiles([...profiles, newProfile]);
      setCurrentProfileId(newProfile.id);
      const previewGoalIds = goalIds && goalIds.length > 0 ? goalIds : onboardingPreview.goals;
      
      if (previewGoalIds.length > 0) {
        await apiClient.setProfileGoals(newProfile.id, previewGoalIds);
      }

      for (const compoundName of onboardingPreview.compounds) {
        await apiClient.createCompound(newProfile.id, {
          personId: newProfile.id,
          name: compoundName,
          category: 'Unknown',
          startDate: new Date().toISOString(),
          endDate: null,
          status: 'Active',
          notes: 'Added from first-run onboarding.',
          sourceType: 'Manual',
          goal: '',
          source: 'Onboarding',
        });
      }

      const importedSources: string[] = [];
      const pendingToolPayload = toolPayload;
      if (pendingToolPayload && hasPendingAnonymousToolData(pendingToolPayload) && !pendingToolPayload.importStatus.importedProfileIds.includes(newProfile.id)) {
        const importedNames = Array.from(
          new Set(
            pendingToolPayload.draftStackItems
              .map((item) => item.name.trim())
              .filter((name) => Boolean(name) && !onboardingPreview.compounds.some((compound) => compound.toLowerCase() === name.toLowerCase()))
          )
        );

        for (const compoundName of importedNames) {
          await apiClient.createCompound(newProfile.id, {
            personId: newProfile.id,
            name: compoundName,
            category: 'Unknown',
            startDate: new Date().toISOString(),
            endDate: null,
            status: 'Active',
            notes: 'Imported from saved tool work on this device.',
            sourceType: 'Manual',
            goal: '',
            source: 'Local device bootstrap',
          });
        }

        const markedPayload = markAnonymousToolPayloadImported(newProfile.id);
        setToolPayload(markedPayload);
        setImportConfirmation('We imported your saved calculations and setups from this device.');
        importedSources.push('tools');
      }

      // Analyzer continuation: only the user's ORIGINAL entries, only after the
      // explicit review step (checkbox), never the analyzer-generated alternative.
      if (
        pendingAnalyzerDraft &&
        includeAnalyzerDraft &&
        !pendingAnalyzerDraft.importStatus.importedProfileIds.includes(newProfile.id)
      ) {
        for (const item of analyzerDraftImports) {
          await apiClient.createCompound(newProfile.id, {
            personId: newProfile.id,
            name: item.name,
            category: 'Unknown',
            startDate: new Date().toISOString(),
            endDate: null,
            status: 'Active',
            notes: item.notes,
            sourceType: 'Manual',
            goal: pendingAnalyzerDraft.goal,
            source: ANALYZER_DRAFT_COMPOUND_SOURCE,
          });
        }

        markAnalyzerProtocolDraftImported(newProfile.id, pendingAnalyzerDraft);
        trackAnalyzerEvent('analyzer_draft_imported', {
          entryCount: analyzerDraftImports.length,
          goal: pendingAnalyzerDraft.goal,
        });
        importedSources.push('analyzer');
      }

      clearOnboardingPreview();
      setOnboardingPreview(emptyOnboardingPreview());
      setShowForm(false);
      // Existing behaviour kept: the tools path always lands with `imported=tools`.
      const importedParam = importedSources.includes('analyzer') ? importedSources.join(',') : 'tools';
      window.location.href = `/profiles/${newProfile.id}?imported=${importedParam}`;
    } catch {
      if (newProfile) {
        setShowForm(false);
        setError(`Profile ${newProfile.displayName} was created, but setup could not finish. Completed additions are saved. Open this profile to review what remains.`);
      } else {
        createStartedRef.current = false;
        setError('Failed to create profile');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDeleteProfile = async (e: React.MouseEvent, id: string) => {
    e.preventDefault();
    e.stopPropagation();
    
    if (!confirm('Are you sure you want to delete this profile? All associated data (compounds, check-ins, phases) will be lost.')) {
      return;
    }

    try {
      await apiClient.deleteProfile(id);
      setProfiles(profiles.filter(p => p.id !== id));
    } catch {
      alert('Failed to delete profile');
    }
  };

  useEffect(() => {
    // Load goals for each profile (for card display)
    if (profiles.length === 0) return;
    const loadGoals = async () => {
      const map: Record<string, GoalDefinition[]> = {};
      for (const p of profiles) {
        try {
          map[p.id] = await apiClient.getProfileGoals(p.id);
        } catch {
          map[p.id] = [];
        }
      }
      setProfileGoalMap(map);
    };
    loadGoals();
  }, [profiles]);

  if (error && !createdProfile && !hasPendingToolData && !pendingAnalyzerDraft) {
    return (
      <div className="w-full">
        <Header title="Profiles" />
        <div className="p-8">
          <ErrorState message={error} onRetry={loadProfiles} />
        </div>
      </div>
    );
  }

  return (
    <div className="w-full">
      <Header
        title="Profiles"
        actions={
          <button
            onClick={() => setShowForm(!showForm)}
            disabled={isSubmitting || createdProfile !== null}
            className="px-4 py-2 bg-emerald-500 hover:bg-emerald-400 text-slate-950 rounded-xl text-sm font-medium transition-all duration-150"
          >
            {showForm ? 'Cancel' : 'New Profile'}
          </button>
        }
      />

      <div className="p-4 sm:p-8 max-w-4xl">
        {importConfirmation && (
          <div className="mb-6 rounded-lg border border-emerald-300/15 bg-emerald-500/[0.07] px-4 py-3 text-sm font-semibold text-emerald-100/85">
            {importConfirmation}
          </div>
        )}

        {error && (
          <div role="alert" className="mb-6 rounded-lg border border-red-300/20 bg-red-500/10 px-4 py-3 text-sm text-red-100/80">
            {error}
            {createdProfile && (
              <Link
                href="/protocol-console"
                onClick={() => setCurrentProfileId(createdProfile.id)}
                className="mt-3 block font-semibold underline"
              >
                Continue setup on {createdProfile.displayName}
              </Link>
            )}
          </div>
        )}

        {pendingAnalyzerDraft && !createdProfile && (
          <div className="mb-6">
            <AnalyzerDraftReviewPanel
              draft={pendingAnalyzerDraft}
              imports={analyzerDraftImports}
              target={{ kind: 'new-profile' }}
              includeOnCreate={includeAnalyzerDraft}
              onIncludeOnCreateChange={setIncludeAnalyzerDraft}
              isSubmitting={isSubmitting || loading}
              onConfirm={showForm ? undefined : () => setShowForm(true)}
              onDismiss={() => {
                setDismissedDraftRevision(getAnalyzerProtocolDraftRevision(pendingAnalyzerDraft));
                trackAnalyzerEvent('analyzer_draft_dismissed', { entryCount: pendingAnalyzerDraft.protocol.length });
              }}
            />
          </div>
        )}

        {hasPendingToolData && !showForm && !createdProfile && (
          <div className="mb-6 rounded-lg border border-emerald-300/15 bg-emerald-500/[0.06] p-5">
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-200/75">
              Saved on this device
            </p>
            <h2 className="mt-2 text-xl font-semibold text-white">Your saved calculations are ready for this profile.</h2>
            <p className="mt-2 text-sm leading-6 text-white/58">
              BioStack can bring over compounds, calculations, setups, instructions, and overlap checks from this browser.
            </p>
            <button
              type="button"
              onClick={() => setShowForm(true)}
              className="mt-5 rounded-lg bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-300"
            >
              Continue Profile Setup
            </button>
          </div>
        )}

        {onboardingPreview.compounds.length > 0 && !showForm && !createdProfile && (
          <div className="mb-6 rounded-lg border border-emerald-300/15 bg-emerald-500/[0.06] p-5">
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-200/75">
              {continuationStatuses?.recovered.eyebrow}
            </p>
            <h2 className="mt-2 text-xl font-semibold text-white">{continuationStatuses?.recovered.title}</h2>
            <p className="mt-2 text-sm leading-6 text-white/58">
              {continuationStatuses?.profile.title}
            </p>
            <div className="mt-4 flex flex-wrap gap-2">
              {onboardingPreview.compounds.map((compound) => (
                <span
                  key={compound}
                  className="rounded-full border border-emerald-300/18 bg-emerald-500/10 px-3 py-1.5 text-sm text-emerald-100/90"
                >
                  {compound}
                </span>
              ))}
            </div>
            <button
              type="button"
              onClick={() => setShowForm(true)}
              className="mt-5 rounded-lg bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-300"
            >
              Continue Profile Setup
            </button>
          </div>
        )}

        {showForm && (
          <div className="mb-8">
            <ProfileForm
              initialNotes={buildImportedProfileNotes(toolPayload)}
              onSubmit={handleCreateProfile}
              onCancel={() => setShowForm(false)}
              isSubmitting={isSubmitting}
            />
          </div>
        )}

        {loading ? (
          <LoadingSkeleton />
        ) : profiles.length === 0 && onboardingPreview.compounds.length > 0 ? (
          <div className="rounded-lg border border-white/[0.08] bg-[#121923]/90 p-6 shadow-[0_16px_45px_rgba(0,0,0,0.3)]">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-300/70">
                  {continuationStatuses?.profile.eyebrow}
                </p>
                <h3 className="mt-3 text-2xl font-semibold text-white">{continuationStatuses?.profile.title}</h3>
                <p className="mt-3 max-w-xl text-sm leading-6 text-white/58">
                  {continuationStatuses?.persistence.title}
                </p>
              </div>
              <button
                type="button"
                onClick={() => setShowForm(true)}
                className="rounded-lg bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-300"
              >
                Continue Profile Setup
              </button>
            </div>

            <div className="mt-6 grid gap-3 sm:grid-cols-3">
              {[
                ['Recovered inputs', onboardingPreview.compounds.join(', ')],
                ['Profile state', continuationStatuses?.profile.title ?? 'Pending'],
                ['Persistence', continuationStatuses?.persistence.title ?? 'Ready'],
              ].map(([label, value]) => (
                <div key={label} className="rounded-lg border border-white/8 bg-black/20 p-4">
                  <p className="text-[10px] font-semibold uppercase tracking-[0.16em] text-white/35">{label}</p>
                  <p className="mt-2 text-sm font-medium leading-6 text-white/78">{value}</p>
                </div>
              ))}
            </div>
          </div>
        ) : profiles.length === 0 && pendingAnalyzerDraft ? null : profiles.length === 0 ? (
          <EmptyState
            title="No Profiles Yet"
            description="Create your first profile to get started"
            icon="👤"
            action={{
              label: 'Create Profile',
              onClick: () => setShowForm(true),
            }}
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {profiles.map((profile) => (
              <Link
                key={profile.id}
                href={`/profiles/${profile.id}`}
                className="group relative p-4 sm:p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90 hover:border-white/[0.15] hover:-translate-y-[1px] transition-all duration-150 cursor-pointer"
              >
                <div className="flex items-start justify-between">
                  <h3 className="text-lg font-semibold text-white">{profile.displayName}</h3>
                  <button
                    onClick={(e) => handleDeleteProfile(e, profile.id)}
                    className="opacity-60 sm:opacity-0 sm:group-hover:opacity-100 p-2 text-white/20 hover:text-red-400 active:text-red-400 transition-all"
                    title="Delete Profile"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
                <div className="mt-2 space-y-1 text-sm text-white/65">
                  <p>Sex: {profile.sex}</p>
                  <p>Weight: {formatWeight(profile.weight, settings.weightUnit)}</p>
                  {(profileGoalMap[profile.id]?.length > 0) && (
                    <div className="mt-2 flex flex-wrap gap-1">
                      {profileGoalMap[profile.id].slice(0, 3).map(goal => (
                        <GoalBadge key={goal.id} goal={goal} compact />
                      ))}
                      {profileGoalMap[profile.id].length > 3 && (
                        <span className="text-xs text-white/35 px-1.5 py-0.5">
                          +{profileGoalMap[profile.id].length - 3} more
                        </span>
                      )}
                    </div>
                  )}
                  {!profileGoalMap[profile.id]?.length && profile.goalSummary && (
                    <p className="mt-2">{profile.goalSummary}</p>
                  )}
                </div>
                <p className="text-xs text-white/35 mt-4">
                  Created {formatDate(profile.createdAtUtc)}
                </p>
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
