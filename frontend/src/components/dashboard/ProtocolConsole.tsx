'use client';

import { ActiveCompoundsCard } from '@/components/dashboard/ActiveCompoundsCard';
import { ActiveGoalsCard } from '@/components/dashboard/ActiveGoalsCard';
import { AnalyzerDraftReviewPanel } from '@/components/dashboard/AnalyzerDraftReviewPanel';
import { CohesionTimelinePanel } from '@/components/dashboard/CohesionTimelinePanel';
import { DriftRegimePanel } from '@/components/dashboard/DriftRegimePanel';
import { LatestCheckInCard } from '@/components/dashboard/LatestCheckInCard';
import { ObservationSignalsPanel } from '@/components/dashboard/ObservationSignalsPanel';
import { OverlapFlagsBanner } from '@/components/dashboard/OverlapFlagsBanner';
import { PatternMemoryPanel } from '@/components/dashboard/PatternMemoryPanel';
import { ProtocolConsoleOverview } from '@/components/dashboard/ProtocolConsoleOverview';
import { SequenceExpectationPanel } from '@/components/dashboard/SequenceExpectationPanel';
import { StatCard } from '@/components/dashboard/StatCard';
import { TimelineSnapshot } from '@/components/dashboard/TimelineSnapshot';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { Header } from '@/components/Header';
import { LoadingSkeleton } from '@/components/LoadingState';
import { ProfileSwitcher } from '@/components/ProfileSwitcher';
import { trackAnalyzerEvent } from '@/lib/analyzerAnalytics';
import {
  ANALYZER_DRAFT_COMPOUND_SOURCE,
  buildAnalyzerDraftCompoundImports,
  getAnalyzerProtocolDraftRevision,
  hasPendingAnalyzerProtocolDraft,
  markAnalyzerProtocolDraftImported,
} from '@/lib/analyzerStorage';
import { useAnalyzerProtocolDraft } from '@/lib/useAnalyzerProtocolDraft';
import { ApiError, apiClient } from '@/lib/api';
import { useProfile } from '@/lib/context';
import {
  CheckIn,
  CompoundRecord,
  CurrentStackIntelligence,
  GoalDefinition,
  InteractionFlag,
  ProtocolConsolePayload,
  TimelineEvent,
} from '@/lib/types';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useCallback, useEffect, useRef, useState } from 'react';
// Protocol Dashboard 2.0 components
import { NextObservationCard } from '@/components/mission/NextObservationCard';
import { ObservationDebtInbox } from '@/components/mission/ObservationDebtInbox';
import { OperatingStateHero } from '@/components/mission/OperatingStateHero';
import { ProtocolWeather } from '@/components/mission/ProtocolWeather';
import { StackClarityMeter } from '@/components/mission/StackClarityMeter';
import { StackGraphMini } from '@/components/mission/StackGraphMini';
import { isEnabled } from '@/lib/flags';

export function ProtocolConsole() {
  const router = useRouter();
  const { currentProfileId, profiles, setProfiles, setCurrentProfileId } = useProfile();
  const [compounds, setCompounds] = useState<CompoundRecord[]>([]);
  const [checkIns, setCheckIns] = useState<CheckIn[]>([]);
  const [timeline, setTimeline] = useState<TimelineEvent[]>([]);
  const [overlaps, setOverlaps] = useState<InteractionFlag[]>([]);
  const [currentStack, setCurrentStack] = useState<CurrentStackIntelligence | null>(null);
  const [mission, setMission] = useState<ProtocolConsolePayload | null>(null);
  const [profileGoals, setProfileGoals] = useState<GoalDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [profilesLoading, setProfilesLoading] = useState(true);
  const [profilesLoaded, setProfilesLoaded] = useState(false);
  const [loadedProfileId, setLoadedProfileId] = useState<string | null>(null);
  const [consoleRefresh, setConsoleRefresh] = useState(0);
  const consoleRequestRef = useRef(0);
  const [error, setError] = useState<string | null>(null);
  const [stackLockedMessage, setStackLockedMessage] = useState<string | null>(null);
  const [missionLockedMessage, setMissionLockedMessage] = useState<string | null>(null);
  const analyzerDraft = useAnalyzerProtocolDraft();
  const [dismissedDraftRevision, setDismissedDraftRevision] = useState<string | null>(null);
  const [draftImporting, setDraftImporting] = useState(false);
  const draftImportingRef = useRef(false);
  const [draftImportError, setDraftImportError] = useState<string | null>(null);
  const [draftImportConfirmation, setDraftImportConfirmation] = useState<string | null>(null);
  const recoveredDraftIdRef = useRef<string | null>(null);

  useEffect(() => {
    if (!hasPendingAnalyzerProtocolDraft(analyzerDraft) || recoveredDraftIdRef.current === getAnalyzerProtocolDraftRevision(analyzerDraft)) {
      return;
    }
    recoveredDraftIdRef.current = getAnalyzerProtocolDraftRevision(analyzerDraft);
    trackAnalyzerEvent('analyzer_draft_recovered', {
      entryCount: analyzerDraft.protocol.length,
      goal: analyzerDraft.goal,
    });
  }, [analyzerDraft]);

  useEffect(() => {
    let active = true;
    async function loadProfiles() {
      try {
        const data = await apiClient.getProfiles();
        if (active) {
          setProfiles(data);
          setProfilesLoaded(true);
        }
      } catch (err) {
        if (active) setError('Failed to load profiles');
        console.error(err);
      } finally {
        if (active) setProfilesLoading(false);
      }
    }
    void loadProfiles();
    return () => { active = false; };
  }, [setProfiles]);

  useEffect(() => {
    // Device storage can remember a deleted profile or another account's ID.
    // Reconcile only after successful discovery, and use the current selection
    // so a delayed response cannot clear a valid profile chosen in the meantime.
    if (profilesLoaded && currentProfileId && !profiles.some(profile => profile.id === currentProfileId)) {
      setCurrentProfileId(null);
    }
  }, [profilesLoaded, profiles, currentProfileId, setCurrentProfileId]);

  const loadProtocolConsoleData = useCallback(async (profileId: string) => {
    const requestId = ++consoleRequestRef.current;
    const isCurrent = () => requestId === consoleRequestRef.current;

    try {
      setLoading(true);
      setLoadedProfileId(null);
      setError(null);
      setStackLockedMessage(null);
      setMissionLockedMessage(null);

      const [comp, chk, tl, goals] = await Promise.all([
        apiClient.getCompounds(profileId),
        apiClient.getCheckIns(profileId),
        apiClient.getTimeline(profileId),
        apiClient.getProfileGoals(profileId),
      ]);

      if (!isCurrent()) return;
      setCompounds(comp);
      setCheckIns(chk);
      setTimeline(tl);
      setProfileGoals(goals);

      try {
        const stack = await apiClient.getCurrentStackIntelligence(profileId);
        if (!isCurrent()) return;
        setCurrentStack(stack);
      } catch (err) {
        if (!isCurrent()) return;
        if (err instanceof ApiError && err.upgradeRequired) {
          setCurrentStack(null);
          setStackLockedMessage(err.message);
        } else {
          throw err;
        }
      }

      try {
        const consoleData = await apiClient.getProtocolConsole(profileId);
        if (!isCurrent()) return;
        setMission(consoleData);
      } catch (err) {
        if (!isCurrent()) return;
        if (err instanceof ApiError && err.upgradeRequired) {
          setMission(null);
          setMissionLockedMessage(err.message);
        } else {
          throw err;
        }
      }

      const activeCompoundNames = comp
        .filter((compound) => compound.status === 'Active')
        .map((compound) => compound.name);

      if (activeCompoundNames.length > 1) {
        const flags = await apiClient.checkOverlap(activeCompoundNames);
        if (!isCurrent()) return;
        setOverlaps(flags);
      } else {
        setOverlaps([]);
      }
      setLoadedProfileId(profileId);
    } catch (err) {
      if (!isCurrent()) return;
      setError('Failed to load protocol console data');
      console.error(err);
    } finally {
      if (isCurrent()) setLoading(false);
    }
  }, []);

  useEffect(() => {
    // Invalidate the previous profile's dedupe snapshot before the async load;
    // exposing it during a profile switch could duplicate recorded compounds.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    if (currentProfileId) void loadProtocolConsoleData(currentProfileId);
    return () => { consoleRequestRef.current += 1; };
  }, [currentProfileId, consoleRefresh, loadProtocolConsoleData]);

  function refreshConsole() {
    // Invalidate the dedupe snapshot immediately, before the reload effect runs.
    setLoadedProfileId(null);
    setConsoleRefresh((value) => value + 1);
  }

  const pendingDraft = hasPendingAnalyzerProtocolDraft(analyzerDraft) &&
    getAnalyzerProtocolDraftRevision(analyzerDraft) !== dismissedDraftRevision ? analyzerDraft : null;
  const currentProfile = profiles.find((profile) => profile.id === currentProfileId) ?? null;
  const draftImports = pendingDraft
    ? buildAnalyzerDraftCompoundImports(pendingDraft, loadedProfileId === currentProfileId ? compounds.map((compound) => compound.name) : [])
    : [];

  function dismissDraft() {
    if (pendingDraft) setDismissedDraftRevision(getAnalyzerProtocolDraftRevision(pendingDraft));
    setDraftImportError(null);
    trackAnalyzerEvent('analyzer_draft_dismissed', { entryCount: pendingDraft?.protocol.length ?? 0 });
  }

  async function importDraftIntoCurrentProfile() {
    if (!pendingDraft || !currentProfile || !currentProfileId ||
        loadedProfileId !== currentProfileId || loading || profilesLoading ||
        draftImportingRef.current || draftImports.length === 0) {
      return;
    }

    const targetProfileId = currentProfileId;
    const confirmedDraft = pendingDraft;
    const targetName = currentProfile?.displayName ?? 'this profile';
    setDraftImporting(true);
    draftImportingRef.current = true;
    setDraftImportError(null);

    try {
      for (const item of draftImports) {
        await apiClient.createCompound(targetProfileId, {
          personId: targetProfileId,
          name: item.name,
          category: 'Unknown',
          startDate: new Date().toISOString(),
          endDate: null,
          status: 'Active',
          notes: item.notes,
          sourceType: 'Manual',
          goal: pendingDraft.goal,
          source: ANALYZER_DRAFT_COMPOUND_SOURCE,
        });
      }

      if (!markAnalyzerProtocolDraftImported(targetProfileId, confirmedDraft)) {
        // Hide only this completed draft if storage refused the write. A newer
        // pending draft saved during the request must remain reviewable.
        setDismissedDraftRevision(getAnalyzerProtocolDraftRevision(confirmedDraft));
      }
      setDraftImportConfirmation(
        `Added ${draftImports.length} ${draftImports.length === 1 ? 'compound' : 'compounds'} from your analysis to ${targetName}, as you entered them.`
      );
      trackAnalyzerEvent('analyzer_draft_imported', { entryCount: draftImports.length, goal: pendingDraft.goal });
    } catch (err) {
      // Draft stays pending; the reload below refreshes the dedupe list so any
      // compounds that were created before the failure are not added twice.
      setDraftImportError('Some compounds could not be added. Your analysis is still saved — try again.');
      console.error(err);
    } finally {
      refreshConsole();
      setDraftImporting(false);
      draftImportingRef.current = false;
    }
  }

  if (!currentProfileId) {
    const hasProfiles = profiles.length > 0;
    return (
      <div className="w-full">
        <Header
          title="Protocol Console"
          subtitle="Protocol Operations"
          actions={hasProfiles ? <ProfileSwitcher /> : undefined}
        />
        <div className="p-8 space-y-6">
          {pendingDraft && (
            <AnalyzerDraftReviewPanel
              draft={pendingDraft}
              imports={draftImports}
              target={hasProfiles ? { kind: 'choose-profile' } : { kind: 'new-profile' }}
              onConfirm={() => router.push('/profiles?bootstrap=analyzer')}
              onDismiss={dismissDraft}
            />
          )}
          {!pendingDraft && (
            <EmptyState
              title={hasProfiles ? 'Choose a profile' : "Let's set up your first profile"}
              description={hasProfiles
                ? 'Select a profile above to open its protocol console.'
                : 'Your profile personalizes overlap checks and keeps your protocol in one place.'}
              icon="👤"
              action={{ label: hasProfiles ? 'View profiles' : 'Create profile', onClick: () => router.push('/profiles') }}
            />
          )}
        </div>
      </div>
    );
  }

  const latestCheckIn = checkIns.length > 0 ? checkIns[0] : null;
  const activeCompounds = compounds.filter((compound) => compound.status === 'Active').length;

  if (error) {
    return (
      <div className="w-full">
        <Header title="Protocol Console" subtitle="Protocol Operations" />
        <div className="p-8">
          <ErrorState message={error} onRetry={refreshConsole} />
        </div>
      </div>
    );
  }

  const mc2 = isEnabled('missionControl2');

  return (
    <div className="w-full">
      <Header title="Protocol Dashboard" subtitle="Protocol Operations" actions={<ProfileSwitcher />} />

      <div className="p-8 space-y-6">
        {loading || profilesLoading || loadedProfileId !== currentProfileId ? (
          <LoadingSkeleton />
        ) : (
          <>
            {draftImportConfirmation && (
              <div
                role="status"
                className="rounded-lg border border-emerald-300/15 bg-emerald-500/[0.07] px-4 py-3 text-sm font-semibold text-emerald-100/85"
              >
                {draftImportConfirmation}
              </div>
            )}
            {pendingDraft && (
              <AnalyzerDraftReviewPanel
                draft={pendingDraft}
                imports={draftImports}
                target={{ kind: 'profile', profileName: currentProfile?.displayName ?? 'this profile' }}
                onConfirm={importDraftIntoCurrentProfile}
                onDismiss={dismissDraft}
                isSubmitting={draftImporting}
                error={draftImportError}
              />
            )}
            {mc2 ? (
              /* ── Protocol Dashboard 2.0 Layout ───────────────────────────── */
              <>
                {/* Hero: single operating state */}
                <OperatingStateHero payload={mission} compounds={compounds} />

                {/* Top row: Weather + Clarity + Next Observation */}
                <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
                  <ProtocolWeather driftSnapshot={mission?.driftSnapshot ?? null} />
                  <StackClarityMeter
                    stackIntelligence={currentStack}
                    compounds={compounds}
                    checkInCount={checkIns.length}
                    hasActiveRun={!!mission?.activeRun}
                  />
                  <NextObservationCard
                    payload={mission}
                    checkIns={checkIns}
                    compounds={compounds}
                    goals={profileGoals}
                  />
                </div>

                {/* Stack Graph mini (SG-3) — only when stack graph flag is on */}
                {isEnabled('stackGraph') && (
                  <StackGraphMini
                    intelligence={currentStack?.interactionIntelligence ?? null}
                    compounds={compounds}
                    activeProtocolId={mission?.activeRun?.protocolId ?? null}
                  />
                )}

                {/* Check-ins Due Inbox */}
                <ObservationDebtInbox
                  payload={mission}
                  checkIns={checkIns}
                  compounds={compounds}
                  goals={profileGoals}
                />

                {/* Upgrade notices */}
                {missionLockedMessage && (
                  <UpgradeNotice
                    eyebrow="Commander — Longitudinal Intelligence"
                    title="Protocol Console is locked on this tier"
                    detail={missionLockedMessage}
                  />
                )}
                {stackLockedMessage && (
                  <UpgradeNotice
                    eyebrow="Operator — Track & Analyze"
                    title="Live stack intelligence is locked on Observer"
                    detail={stackLockedMessage}
                  />
                )}

                {/* Overlap flags */}
                {overlaps.length > 0 && <OverlapFlagsBanner flags={overlaps} />}

                {/* Pattern + Drift panels */}
                <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
                  <PatternMemoryPanel snapshot={mission?.patternSnapshot ?? null} />
                  <DriftRegimePanel drift={mission?.driftSnapshot ?? null} patterns={mission?.patternSnapshot ?? null} />
                  <SequenceExpectationPanel snapshot={mission?.sequenceExpectationSnapshot ?? null} />
                  <ObservationSignalsPanel signals={mission?.observationSignals ?? []} />
                </div>

                {/* Goals */}
                {profileGoals.length > 0 && (
                  <ActiveGoalsCard goals={profileGoals} profileId={currentProfileId} />
                )}

                {/* Compound + Check-in grid */}
                <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
                  <div className="lg:col-span-2">
                    <ActiveCompoundsCard compounds={compounds} />
                  </div>
                  <LatestCheckInCard checkIn={latestCheckIn} />
                </div>

                {/* Annotated timeline */}
                <CohesionTimelinePanel
                  events={mission?.cohesionTimeline ?? []}
                  sequence={mission?.sequenceExpectationSnapshot ?? null}
                  drift={mission?.driftSnapshot ?? null}
                />
                <TimelineSnapshot events={timeline} />
              </>
            ) : (
              /* ── Legacy Layout ────────────────────────────────────────── */
              <>
                <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
                  <StatCard title="Active Compounds" value={activeCompounds} icon="🧪" color="emerald" />
                  <StatCard title="Total Check-ins" value={checkIns.length} icon="📊" color="blue" />
                  <StatCard
                    title="Pathway Flags"
                    value={overlaps.length}
                    icon="⚠️"
                    color={overlaps.length > 0 ? 'amber' : 'default'}
                  />
                  <StatCard
                    title="Protocol Score"
                    value={currentStack ? currentStack.stackScore.score : '—'}
                    icon="🎯"
                    color={currentStack && currentStack.stackScore.score < 60 ? 'amber' : 'emerald'}
                  />
                </div>
                <ProtocolConsoleOverview mission={mission} />
                {missionLockedMessage && (
                  <UpgradeNotice
                    eyebrow="Commander — Longitudinal Intelligence"
                    title="Protocol Console is locked on this tier"
                    detail={missionLockedMessage}
                  />
                )}
                <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
                  <PatternMemoryPanel snapshot={mission?.patternSnapshot ?? null} />
                  <DriftRegimePanel drift={mission?.driftSnapshot ?? null} patterns={mission?.patternSnapshot ?? null} />
                  <SequenceExpectationPanel snapshot={mission?.sequenceExpectationSnapshot ?? null} />
                  <ObservationSignalsPanel signals={mission?.observationSignals ?? []} />
                </div>
                {stackLockedMessage && (
                  <UpgradeNotice
                    eyebrow="Operator — Track & Analyze"
                    title="Live stack intelligence is locked on Observer"
                    detail={stackLockedMessage}
                  />
                )}
                {overlaps.length > 0 && <OverlapFlagsBanner flags={overlaps} />}
                {profileGoals.length > 0 && (
                  <ActiveGoalsCard goals={profileGoals} profileId={currentProfileId} />
                )}
                <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
                  <div className="lg:col-span-2">
                    <ActiveCompoundsCard compounds={compounds} />
                  </div>
                  <LatestCheckInCard checkIn={latestCheckIn} />
                </div>
                <CohesionTimelinePanel
                  events={mission?.cohesionTimeline ?? []}
                  sequence={mission?.sequenceExpectationSnapshot ?? null}
                  drift={mission?.driftSnapshot ?? null}
                />
                <TimelineSnapshot events={timeline} />
              </>
            )}
          </>
        )}
      </div>
    </div>
  );
}

function UpgradeNotice({
  eyebrow,
  title,
  detail,
}: {
  eyebrow: string;
  title: string;
  detail: string;
}) {
  return (
    <section className="rounded-lg border border-amber-300/15 bg-amber-400/[0.06] p-5">
      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-amber-100/75">{eyebrow}</p>
      <h2 className="mt-2 text-lg font-semibold text-white">{title}</h2>
      <p className="mt-2 text-sm leading-6 text-white/65">{detail}</p>
      <div className="mt-4 flex flex-wrap gap-3">
        <Link
          href="/billing"
          className="rounded-lg bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-300"
        >
          Upgrade plan
        </Link>
        <Link
          href="/pricing"
          className="rounded-lg border border-white/[0.1] px-4 py-2 text-sm font-semibold text-white/75 hover:border-white/20"
        >
          Compare tiers
        </Link>
      </div>
    </section>
  );
}
