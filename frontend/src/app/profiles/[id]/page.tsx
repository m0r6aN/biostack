'use client';

import { ErrorState } from '@/components/ErrorState';
import { GoalDisplay } from '@/components/goals/GoalDisplay';
import { Header } from '@/components/Header';
import { LoadingState } from '@/components/LoadingState';
import { ProfileForm } from '@/components/profiles/ProfileForm';
import { apiClient } from '@/lib/api';
import { useProfile } from '@/lib/context';
import { useSettings } from '@/lib/settings';
import { CheckIn, CompoundRecord, CreateProfileRequest, GoalDefinition, PersonProfile, ProtocolPhase } from '@/lib/types';
import { formatDate, formatWeight } from '@/lib/utils';
import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';

export default function ProfileDetailPage() {
  const params = useParams();
  const id = params.id as string;
  const [profile, setProfile] = useState<PersonProfile | null>(null);
  const [compounds, setCompounds] = useState<CompoundRecord[]>([]);
  const [checkIns, setCheckIns] = useState<CheckIn[]>([]);
  const [phases, setPhases] = useState<ProtocolPhase[]>([]);
  const [profileGoals, setProfileGoals] = useState<GoalDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { setCurrentProfileId, profiles, setProfiles } = useProfile();
  const { settings } = useSettings();
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [importConfirmation, setImportConfirmation] = useState('');

  useEffect(() => {
    loadData();
    setCurrentProfileId(id);
    if (new URLSearchParams(window.location.search).get('imported') === 'tools') {
      setImportConfirmation('We imported your saved calculations and setups from this device.');
    }
  }, [id]);

  const loadData = async () => {
    try {
      setLoading(true);
      const [p, c, ch, ph, goals] = await Promise.all([
        apiClient.getProfile(id),
        apiClient.getCompounds(id),
        apiClient.getCheckIns(id),
        apiClient.getProtocolPhases(id),
        apiClient.getProfileGoals(id),
      ]);
      setProfile(p);
      setCompounds(c);
      setCheckIns(ch);
      setPhases(ph);
      setProfileGoals(goals);
    } catch (err) {
      setError('Failed to load profile');
    } finally {
      setLoading(false);
    }
  };

  const handleUpdate = async (data: CreateProfileRequest & { selectedGoalIds?: string[] }) => {
    try {
      setIsSubmitting(true);
      const { selectedGoalIds: goalIds, ...profileData } = data;
      const updatedProfile = await apiClient.updateProfile(id, profileData);
      
      if (goalIds) {
        await apiClient.setProfileGoals(id, goalIds);
      }
      
      setProfile(updatedProfile);
      setProfiles(profiles.map(p => p.id === id ? updatedProfile : p));
      setIsEditing(false);
      loadData(); // Reload goals and other related data
    } catch (err) {
      alert('Failed to update profile');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async () => {
    if (!confirm('Are you sure you want to delete this profile? All associated data will be lost.')) {
      return;
    }

    try {
      await apiClient.deleteProfile(id);
      setProfiles(profiles.filter(p => p.id !== id));
      router.push('/profiles');
    } catch (err) {
      alert('Failed to delete profile');
    }
  };

  if (error) {
    return (
      <div className="w-full">
        <Header title="Profile" />
        <div className="p-4 sm:p-8">
          <ErrorState message={error} onRetry={loadData} />
        </div>
      </div>
    );
  }

  if (loading || !profile) {
    return (
      <div className="w-full">
        <Header title="Profile" />
        <div className="p-4 sm:p-8">
          <LoadingState />
        </div>
      </div>
    );
  }

  const currentPhase = phases.find(p => !p.endDate);

  return (
    <div className="w-full">
      <Header title={profile.displayName} subtitle="Profile details" />

      <div className="p-4 sm:p-8 max-w-7xl mx-auto">
        {importConfirmation && (
          <div className="mb-6 rounded-lg border border-emerald-300/15 bg-emerald-500/[0.07] px-4 py-3 text-sm font-semibold text-emerald-100/85">
            {importConfirmation}
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 sm:gap-8">
          {/* Main Column */}
          <div className="lg:col-span-2 space-y-4 sm:space-y-8">
            {/* Profile Summary */}
            <div className="p-4 sm:p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90 shadow-[0_8px_40px_rgba(0,0,0,0.5)] backdrop-blur-xl transition-all hover:border-white/[0.12]">
              {isEditing ? (
                <ProfileForm
                  initialData={profile}
                  onSubmit={handleUpdate}
                  onCancel={() => setIsEditing(false)}
                  isSubmitting={isSubmitting}
                />
              ) : (
                <>
                  <div className="flex items-center justify-between mb-6">
                    <h2 className="text-xl font-bold bg-gradient-to-r from-white to-white/60 bg-clip-text text-transparent italic tracking-tight">Profile Summary</h2>
                    <div className="flex items-center gap-1.5">
                      <button
                        onClick={() => setIsEditing(true)}
                        className="p-2.5 rounded-xl bg-white/[0.03] text-white/40 hover:text-emerald-400 hover:bg-emerald-400/10 transition-all active:scale-95"
                        title="Edit Profile"
                      >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                        </svg>
                      </button>
                      <button
                        onClick={handleDelete}
                        className="p-2.5 rounded-xl bg-white/[0.03] text-white/40 hover:text-red-400 hover:bg-red-400/10 transition-all active:scale-95"
                        title="Delete Profile"
                      >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                      </button>
                    </div>
                  </div>
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                    <div className="relative group p-4 rounded-xl bg-white/[0.02] border border-white/[0.05]">
                      <div className="absolute -top-3 left-4 px-2 bg-[#121923] text-[10px] font-bold uppercase tracking-widest text-emerald-400/70">Sex</div>
                      <p className="text-lg font-semibold text-white/90">{profile.sex}</p>
                    </div>
                    <div className="relative group p-4 rounded-xl bg-white/[0.02] border border-white/[0.05]">
                      <div className="absolute -top-3 left-4 px-2 bg-[#121923] text-[10px] font-bold uppercase tracking-widest text-blue-400/70">Weight</div>
                      <p className="text-lg font-semibold text-white/90">{formatWeight(profile.weight, settings.weightUnit)}</p>
                    </div>
                    {currentPhase && (
                      <div className="relative group p-4 rounded-xl bg-white/[0.02] border border-white/[0.05] col-span-2 md:col-span-1">
                        <div className="absolute -top-3 left-4 px-2 bg-[#121923] text-[10px] font-bold uppercase tracking-widest text-purple-400/70">Current Phase</div>
                        <p className="text-lg font-semibold text-white/90">{currentPhase.name}</p>
                      </div>
                    )}
                  </div>
                  {(profileGoals.length > 0 || profile.goalSummary) && (
                    <div className="mt-8 pt-8 border-t border-white/5">
                      <h3 className="text-sm font-bold uppercase tracking-[0.2em] text-white/30 mb-6 flex items-center gap-2">
                        <span className="w-8 h-[1px] bg-white/10"></span>
                        Active Goals
                      </h3>
                      {profileGoals.length > 0 ? (
                        <GoalDisplay goals={profileGoals} />
                      ) : (
                        <p className="p-5 rounded-xl bg-white/[0.02] border border-white/[0.05] text-sm leading-relaxed text-white/60">{profile.goalSummary}</p>
                      )}
                    </div>
                  )}
                  {profile.notes && (
                    <div className="mt-8 pt-8 border-t border-white/5">
                      <h3 className="text-sm font-bold uppercase tracking-[0.2em] text-white/30 mb-4 flex items-center gap-2">
                        <span className="w-8 h-[1px] bg-white/10"></span>
                        Physiology Notes
                      </h3>
                      <p className="p-5 rounded-xl bg-white/[0.02] border border-white/[0.05] text-sm leading-relaxed text-white/60 whitespace-pre-wrap">{profile.notes}</p>
                    </div>
                  )}
                </>
              )}
            </div>
          </div>

          {/* Sidebar Column */}
          <div className="space-y-6">
            {/* Compounds Card */}
            <div className="p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90 shadow-[0_8px_24px_rgba(0,0,0,0.35)] hover:border-white/[0.12] transition-colors">
              <div className="flex items-center justify-between mb-6">
                <h2 className="text-base font-bold text-white tracking-tight italic">Compounds</h2>
                <Link
                  href="/compounds"
                  className="px-3 py-1.5 text-xs font-semibold text-emerald-400 hover:text-emerald-300 hover:bg-emerald-400/10 rounded-lg transition-all"
                >
                  Manage →
                </Link>
              </div>
              {compounds.length === 0 ? (
                <div className="p-10 text-center rounded-xl bg-white/[0.01] border border-dashed border-white/10">
                  <p className="text-xs text-white/30">No active compounds recorded</p>
                </div>
              ) : (
                <div className="space-y-3">
                  {compounds.slice(0, 5).map((comp) => (
                    <div key={comp.id} className="flex items-center justify-between p-3.5 bg-white/[0.02] border border-white/[0.06] rounded-xl hover:bg-white/[0.04] transition-colors group">
                      <div className="min-w-0 flex-1">
                        <p className="text-sm font-semibold text-white/80 truncate group-hover:text-white transition-colors">{comp.name}</p>
                        <p className="text-[10px] uppercase font-bold tracking-wider text-white/20 mt-0.5">{comp.category}</p>
                      </div>
                      <span className={`flex-shrink-0 text-[10px] font-bold px-2 py-0.5 rounded-md border ${
                        comp.status === 'Active'
                          ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20'
                          : comp.status === 'Completed'
                            ? 'bg-blue-500/10 text-blue-400 border-blue-500/20'
                            : 'bg-amber-500/10 text-amber-400 border-amber-500/20'
                      }`}>
                        {comp.status.toUpperCase()}
                      </span>
                    </div>
                  ))}
                  {compounds.length > 5 && (
                    <Link href="/compounds" className="block text-center text-[10px] font-bold uppercase tracking-widest text-white/20 hover:text-white/40 pt-2 transition-colors">
                      + {compounds.length - 5} More Compounds
                    </Link>
                  )}
                </div>
              )}
            </div>

            {/* Check-ins Card */}
            <div className="p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90 shadow-[0_8px_24px_rgba(0,0,0,0.35)] hover:border-white/[0.12] transition-colors">
              <div className="flex items-center justify-between mb-6">
                <h2 className="text-base font-bold text-white tracking-tight italic">Check-ins</h2>
                <Link
                  href="/checkins"
                  className="px-3 py-1.5 text-xs font-semibold text-emerald-400 hover:text-emerald-300 hover:bg-emerald-400/10 rounded-lg transition-all"
                >
                  View all
                </Link>
              </div>
              {checkIns.length === 0 ? (
                <div className="p-10 text-center rounded-xl bg-white/[0.01] border border-dashed border-white/10">
                  <p className="text-xs text-white/30">No check-in history yet</p>
                </div>
              ) : (
                <div className="space-y-3">
                  {checkIns.slice(-3).reverse().map((checkin) => (
                    <div key={checkin.id} className="p-4 bg-white/[0.02] border border-white/[0.06] rounded-xl hover:bg-white/[0.04] transition-colors">
                      <div className="flex justify-between items-start mb-2">
                        <p className="text-xs font-bold text-white/70">{formatDate(checkin.date)}</p>
                        <span className="text-[10px] font-bold text-emerald-400 bg-emerald-400/10 px-1.5 py-0.5 rounded">
                          {checkin.energy}/10 E
                        </span>
                      </div>
                      <div className="flex items-center gap-3 text-[11px] text-white/40">
                        <span className="flex items-center gap-1">
                          <span className="w-1.5 h-1.5 rounded-full bg-blue-500/50"></span>
                          {formatWeight(checkin.weight, settings.weightUnit)}
                        </span>
                        <span className="flex items-center gap-1">
                          <span className="w-1.5 h-1.5 rounded-full bg-amber-500/50"></span>
                          {checkin.sleepQuality}/10 S
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Phases Card */}
            <div className="p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90 shadow-[0_8px_24px_rgba(0,0,0,0.35)] hover:border-white/[0.12] transition-colors">
              <h2 className="text-base font-bold text-white tracking-tight italic mb-6">Execution Phases</h2>
              {phases.length === 0 ? (
                <p className="text-xs text-white/30 text-center py-4">Structure your progress with phases</p>
              ) : (
                <div className="space-y-3">
                  {phases.map((phase) => (
                    <div key={phase.id} className="p-3.5 bg-white/[0.02] border border-white/[0.06] rounded-xl relative overflow-hidden group">
                      {!phase.endDate && <div className="absolute top-0 right-0 w-1 h-full bg-emerald-500/50 shadow-[0_0_10px_rgba(16,185,129,0.3)]"></div>}
                      <p className="text-sm font-semibold text-white/80 group-hover:text-white transition-colors">{phase.name}</p>
                      <div className="flex items-center gap-2 mt-1">
                        <span className="text-[10px] font-bold text-white/20 uppercase tracking-tighter">Timeline:</span>
                        <span className="text-[10px] font-medium text-white/40 italic">
                          {formatDate(phase.startDate)} {phase.endDate ? `→ ${formatDate(phase.endDate)}` : '(Current)'}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
