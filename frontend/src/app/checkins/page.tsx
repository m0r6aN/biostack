'use client';

import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { Header } from '@/components/Header';
import { LoadingSkeleton } from '@/components/LoadingState';
import { CheckInForm } from '@/components/checkins/CheckInForm';
import { CheckInHistory } from '@/components/checkins/CheckInHistory';
import { TrendChart } from '@/components/checkins/TrendChart';
import { apiClient } from '@/lib/api';
import { useProfile } from '@/lib/context';
import { CheckIn, GoalDefinition } from '@/lib/types';
import { useEffect, useState } from 'react';

export default function CheckInsPage() {
  const { currentProfileId } = useProfile();
  const [checkIns, setCheckIns] = useState<CheckIn[]>([]);
  const [profileGoals, setProfileGoals] = useState<GoalDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (currentProfileId) {
      loadCheckIns();
    }
  }, [currentProfileId]);

  const loadCheckIns = async () => {
    try {
      setLoading(true);
      const [checkInData, goalsData] = await Promise.all([
        apiClient.getCheckIns(currentProfileId!),
        apiClient.getProfileGoals(currentProfileId!)
      ]);
      setCheckIns(checkInData);
      setProfileGoals(goalsData);
    } catch (err) {
      setError('Failed to load check-ins');
    } finally {
      setLoading(false);
    }
  };

  const handleAddCheckIn = async (data: any) => {
    try {
      setIsSubmitting(true);
      const newCheckIn = await apiClient.createCheckIn(currentProfileId!, data);
      setCheckIns([newCheckIn, ...checkIns]);
      setShowForm(false);
    } catch (err) {
      setError('Failed to record check-in');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!currentProfileId) {
    return (
      <div className="w-full">
        <Header title="Check-ins" />
        <div className="p-8">
          <EmptyState
            title="No Profile Selected"
            description="Select a profile to record check-ins"
            icon="📊"
          />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="w-full">
        <Header title="Check-ins" />
        <div className="p-8">
          <ErrorState message={error} onRetry={loadCheckIns} />
        </div>
      </div>
    );
  }

  return (
    <div className="w-full">
      <Header
        title="Check-ins"
        actions={
          <button
            onClick={() => setShowForm(!showForm)}
            className="px-4 py-2 bg-emerald-500 hover:bg-emerald-400 text-slate-950 rounded-xl text-sm font-medium transition-all duration-150"
          >
            {showForm ? 'Cancel' : 'New Check-in'}
          </button>
        }
      />

      <div className="p-8 space-y-8 max-w-6xl">
        {showForm && (
          <div className="p-8 bg-[#121923]/90 border border-white/[0.08] rounded-2xl shadow-[0_12px_40px_rgba(0,0,0,0.4)] animate-in fade-in zoom-in-95 duration-200">
            <div className="flex items-center justify-between mb-8">
              <div>
                <h2 className="text-2xl font-black text-white">Daily Check-In</h2>
                <p className="text-sm text-white/40 mt-1">Capture your biomarkers and qualitative state.</p>
              </div>
              <button
                onClick={() => setShowForm(false)}
                className="p-2 text-white/20 hover:text-white transition-all"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
            <CheckInForm
              personId={currentProfileId}
              profileGoals={profileGoals}
              onSubmit={handleAddCheckIn}
              isLoading={isSubmitting}
            />
          </div>
        )}

        {loading ? (
          <LoadingSkeleton />
        ) : checkIns.length === 0 ? (
          <EmptyState
            title="Your protocol has no observations yet"
            description="Record the first check-in so runs have reality to compare against."
            icon="📊"
            action={{
              label: 'Record First Check-in',
              onClick: () => setShowForm(true),
            }}
          />
        ) : (
          <>
            {/* Trend Charts */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <TrendChart
                checkIns={checkIns}
                metric="weight"
                title="Weight Trend"
              />
              <TrendChart
                checkIns={checkIns}
                metric="energy"
                title="Energy Levels"
              />
              <TrendChart
                checkIns={checkIns}
                metric="sleepQuality"
                title="Sleep Quality"
              />
              <TrendChart
                checkIns={checkIns}
                metric="recovery"
                title="Recovery Status"
              />
            </div>

            {/* History */}
            <div>
              <h2 className="text-lg font-semibold text-white mb-4">Check-in History</h2>
              <CheckInHistory checkIns={checkIns} />
            </div>
          </>
        )}
      </div>
    </div>
  );
}
