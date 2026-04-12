'use client';

import { useEffect, useState } from 'react';
import { useProfile } from '@/lib/context';
import { apiClient } from '@/lib/api';
import {
  CheckIn,
  CompoundRecord,
  CurrentStackIntelligence,
  GoalDefinition,
  InteractionFlag,
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
import { ProfileSwitcher } from '@/components/ProfileSwitcher';

export function MissionControlDashboard() {
  const { currentProfileId, setProfiles } = useProfile();
  const [compounds, setCompounds] = useState<CompoundRecord[]>([]);
  const [checkIns, setCheckIns] = useState<CheckIn[]>([]);
  const [timeline, setTimeline] = useState<TimelineEvent[]>([]);
  const [overlaps, setOverlaps] = useState<InteractionFlag[]>([]);
  const [currentStack, setCurrentStack] = useState<CurrentStackIntelligence | null>(null);
  const [profileGoals, setProfileGoals] = useState<GoalDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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

    loadDashboardData();
  }, [currentProfileId]);

  async function loadDashboardData() {
    if (!currentProfileId) {
      return;
    }

    try {
      setLoading(true);
      setError(null);

      const [comp, chk, tl, goals, stack] = await Promise.all([
        apiClient.getCompounds(currentProfileId),
        apiClient.getCheckIns(currentProfileId),
        apiClient.getTimeline(currentProfileId),
        apiClient.getProfileGoals(currentProfileId),
        apiClient.getCurrentStackIntelligence(currentProfileId),
      ]);

      setCompounds(comp);
      setCheckIns(chk);
      setTimeline(tl);
      setProfileGoals(goals);
      setCurrentStack(stack);

      const activeCompoundNames = comp
        .filter((compound) => compound.status === 'Active')
        .map((compound) => compound.name);

      if (activeCompoundNames.length > 1) {
        setOverlaps(await apiClient.checkOverlap(activeCompoundNames));
      } else {
        setOverlaps([]);
      }
    } catch (err) {
      setError('Failed to load dashboard data');
      console.error(err);
    } finally {
      setLoading(false);
    }
  }

  if (!currentProfileId) {
    return (
      <div className="w-full">
        <Header title="Mission Control" subtitle="Dashboard" />
        <div className="p-8">
          <EmptyState
            title="No Profile Selected"
            description="Create or select a profile to start observing your protocol."
            icon="👤"
          />
        </div>
      </div>
    );
  }

  const latestCheckIn = checkIns.length > 0 ? checkIns[checkIns.length - 1] : null;
  const activeCompounds = compounds.filter((compound) => compound.status === 'Active').length;

  if (error) {
    return (
      <div className="w-full">
        <Header title="Mission Control" subtitle="Dashboard" />
        <div className="p-8">
          <ErrorState message={error} onRetry={loadDashboardData} />
        </div>
      </div>
    );
  }

  return (
    <div className="w-full">
      <Header title="Mission Control" subtitle="Dashboard" actions={<ProfileSwitcher />} />

      <div className="p-8 space-y-6">
        {loading ? (
          <LoadingSkeleton />
        ) : (
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
                title="Stack Score"
                value={currentStack ? currentStack.stackScore.score : '—'}
                icon="🎯"
                color={currentStack && currentStack.stackScore.score < 60 ? 'amber' : 'emerald'}
              />
            </div>

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

            <TimelineSnapshot events={timeline} />
          </>
        )}
      </div>
    </div>
  );
}
