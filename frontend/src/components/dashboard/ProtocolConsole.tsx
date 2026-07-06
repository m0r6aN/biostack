'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { useProfile } from '@/lib/context';
import { ApiError, apiClient } from '@/lib/api';
import {
  CheckIn,
  CompoundRecord,
  CurrentStackIntelligence,
  GoalDefinition,
  InteractionFlag,
  ProtocolConsolePayload,
  TimelineEvent,
} from '@/lib/types';
import { Header } from '@/components/Header';
import { LoadingSkeleton } from '@/components/LoadingState';
import { ErrorState } from '@/components/ErrorState';
import { EmptyState } from '@/components/EmptyState';
import { StatCard } from '@/components/dashboard/StatCard';
import { ActiveCompoundsCard } from '@/components/dashboard/ActiveCompoundsCard';
import { ActiveGoalsCard } from '@/components/dashboard/ActiveGoalsCard';
import { LatestCheckInCard } from '@/components/dashboard/LatestCheckInCard';
import { TimelineSnapshot } from '@/components/dashboard/TimelineSnapshot';
import { OverlapFlagsBanner } from '@/components/dashboard/OverlapFlagsBanner';
import { CohesionTimelinePanel } from '@/components/dashboard/CohesionTimelinePanel';
import { ProtocolConsoleOverview } from '@/components/dashboard/ProtocolConsoleOverview';
import { PatternMemoryPanel } from '@/components/dashboard/PatternMemoryPanel';
import { DriftRegimePanel } from '@/components/dashboard/DriftRegimePanel';
import { SequenceExpectationPanel } from '@/components/dashboard/SequenceExpectationPanel';
import { ObservationSignalsPanel } from '@/components/dashboard/ObservationSignalsPanel';
import { ProfileSwitcher } from '@/components/ProfileSwitcher';
// Mission Control 2.0 components
import { OperatingStateHero } from '@/components/mission/OperatingStateHero';
import { NextObservationCard } from '@/components/mission/NextObservationCard';
import { ProtocolWeather } from '@/components/mission/ProtocolWeather';
import { StackClarityMeter } from '@/components/mission/StackClarityMeter';
import { ObservationDebtInbox } from '@/components/mission/ObservationDebtInbox';
import { StackGraphMini } from '@/components/mission/StackGraphMini';
import { isEnabled } from '@/lib/flags';

export function ProtocolConsole() {
  const router = useRouter();
  const { currentProfileId, setProfiles } = useProfile();
  const [compounds, setCompounds] = useState<CompoundRecord[]>([]);
  const [checkIns, setCheckIns] = useState<CheckIn[]>([]);
  const [timeline, setTimeline] = useState<TimelineEvent[]>([]);
  const [overlaps, setOverlaps] = useState<InteractionFlag[]>([]);
  const [currentStack, setCurrentStack] = useState<CurrentStackIntelligence | null>(null);
  const [mission, setMission] = useState<ProtocolConsolePayload | null>(null);
  const [profileGoals, setProfileGoals] = useState<GoalDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [stackLockedMessage, setStackLockedMessage] = useState<string | null>(null);
  const [missionLockedMessage, setMissionLockedMessage] = useState<string | null>(null);

  useEffect(() => {
    loadProfiles();
  }, []);

  async function loadProfiles() {
    try {
      setLoading(true);
      const data = await apiClient.getProfiles();
      setProfiles(data);
    } catch (err) {
      setError('Failed to load profiles');
      console.error(err);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (!currentProfileId) {
      return;
    }

    loadProtocolConsoleData();
  }, [currentProfileId]);

  async function loadProtocolConsoleData() {
    if (!currentProfileId) {
      return;
    }

    try {
      setLoading(true);
      setError(null);
      setStackLockedMessage(null);
      setMissionLockedMessage(null);

      const [comp, chk, tl, goals] = await Promise.all([
        apiClient.getCompounds(currentProfileId),
        apiClient.getCheckIns(currentProfileId),
        apiClient.getTimeline(currentProfileId),
        apiClient.getProfileGoals(currentProfileId),
      ]);

      setCompounds(comp);
      setCheckIns(chk);
      setTimeline(tl);
      setProfileGoals(goals);

      try {
        setCurrentStack(await apiClient.getCurrentStackIntelligence(currentProfileId));
      } catch (err) {
        if (err instanceof ApiError && err.upgradeRequired) {
          setCurrentStack(null);
          setStackLockedMessage(err.message);
        } else {
          throw err;
        }
      }

      try {
        setMission(await apiClient.getProtocolConsole(currentProfileId));
      } catch (err) {
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
        setOverlaps(await apiClient.checkOverlap(activeCompoundNames));
      } else {
        setOverlaps([]);
      }
    } catch (err) {
      setError('Failed to load protocol console data');
      console.error(err);
    } finally {
      setLoading(false);
    }
  }

  if (!currentProfileId) {
    return (
      <div className="w-full">
        <Header title="Protocol Console" subtitle="Protocol Operations" />
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

  const latestCheckIn = checkIns.length > 0 ? checkIns[0] : null;
  const activeCompounds = compounds.filter((compound) => compound.status === 'Active').length;

  if (error) {
    return (
      <div className="w-full">
        <Header title="Protocol Console" subtitle="Protocol Operations" />
        <div className="p-8">
          <ErrorState message={error} onRetry={loadProtocolConsoleData} />
        </div>
      </div>
    );
  }

  const mc2 = isEnabled('missionControl2');

  return (
    <div className="w-full">
      <Header title="Mission Control" subtitle="Protocol Operations" actions={<ProfileSwitcher />} />

      <div className="p-8 space-y-6">
        {loading ? (
          <LoadingSkeleton />
        ) : (
          <>
            {mc2 ? (
              /* ── Mission Control 2.0 Layout ───────────────────────────── */
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

                {/* Observation Debt Inbox */}
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
                    title="Mission control is locked on this tier"
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
                    title="Mission control is locked on this tier"
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
