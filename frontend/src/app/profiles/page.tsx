'use client';

import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { GoalBadge } from '@/components/goals/GoalBadge';
import { Header } from '@/components/Header';
import { LoadingSkeleton } from '@/components/LoadingState';
import { ProfileForm } from '@/components/profiles/ProfileForm';
import { apiClient } from '@/lib/api';
import { useProfile } from '@/lib/context';
import { useSettings } from '@/lib/settings';
import { GoalDefinition } from '@/lib/types';
import { formatDate, formatWeight } from '@/lib/utils';
import Link from 'next/link';
import { useEffect, useState } from 'react';

export default function ProfilesPage() {
  const { profiles, setProfiles, setCurrentProfileId } = useProfile();
  const { settings } = useSettings();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [formData, setFormData] = useState({
    displayName: '',
    sex: 'Male' as const,
    weight: 70,   // always stored in kg; display converts if imperial
    notes: '',
  });
  const [selectedGoalIds, setSelectedGoalIds] = useState<string[]>([]);
  const [profileGoalMap, setProfileGoalMap] = useState<Record<string, GoalDefinition[]>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    loadProfiles();
  }, []);

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

  const loadProfiles = async () => {
    try {
      setLoading(true);
      const data = await apiClient.getProfiles();
      setProfiles(data);
    } catch (err) {
      setError('Failed to load profiles');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateProfile = async (data: any) => {
    try {
      setIsSubmitting(true);
      const { selectedGoalIds: goalIds, ...profileData } = data;
      const newProfile = await apiClient.createProfile(profileData);
      
      if (goalIds && goalIds.length > 0) {
        await apiClient.setProfileGoals(newProfile.id, goalIds);
      }
      
      setProfiles([...profiles, newProfile]);
      setCurrentProfileId(newProfile.id);
      setShowForm(false);
    } catch (err) {
      setError('Failed to create profile');
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
    } catch (err) {
      alert('Failed to delete profile');
    }
  };

  if (error) {
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
            className="px-4 py-2 bg-emerald-500 hover:bg-emerald-400 text-slate-950 rounded-xl text-sm font-medium transition-all duration-150"
          >
            {showForm ? 'Cancel' : 'New Profile'}
          </button>
        }
      />

      <div className="p-4 sm:p-8 max-w-4xl">
        {showForm && (
          <div className="mb-8">
            <ProfileForm
              onSubmit={handleCreateProfile}
              onCancel={() => setShowForm(false)}
              isSubmitting={isSubmitting}
            />
          </div>
        )}

        {loading ? (
          <LoadingSkeleton />
        ) : profiles.length === 0 ? (
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
